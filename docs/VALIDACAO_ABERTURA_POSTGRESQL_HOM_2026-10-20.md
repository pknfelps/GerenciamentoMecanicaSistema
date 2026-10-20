# E5 — Falha de abertura PostgreSQL da API — 20/10/2026

**Resultado: medição de falha de abertura comprovada no cliente real da API, em processo isolado da imagem implantada em hom.** O New Relic recebeu um `connection.open/application/failure` e um `connection.open/readiness/failure`, com HTTP 500/503 no processo de validação. A API que atende o Gateway manteve suas três probes 200/Healthy, Ready e zero reinícios. O processo temporário e seu arquivo foram removidos.

**Limite do método:** o processo temporário utilizou configuração local fictícia, com destino PostgreSQL em loopback, enquanto o processo original continuou usando a configuração AWS/RDS. Isso comprova os caminhos reais de medição e tratamento de abertura da API executando no EKS; não simula indisponibilidade do RDS nem falha de abertura na Lambda.

## Ambiente e preparação

- AWS `121754142617/us-east-1`, hom, New Relic US `8626152`.
- API `a8ba2615d9fe918fc161d975fabd406b54fd0ff2`, imagem `sha256:50ec93314db96c99a4014b07ac1a8860ef0174fe65940c8fdec44e30954dd9df`.
- Pod `deploy-gerenciamento-api-589bb58bf7-bqnmr`, principal em 8080; Collector 1/1 Ready.
- Antes do teste: live/ready/startup 200/Healthy, API 2m CPU/117 MiB e nó 50m CPU/980 MiB no snapshot. Não são prova de capacidade sob carga.
- Porta temporária 18082 livre; certificado e manifesto publicados presentes.

O mantenedor autorizou seguir as validações. Não houve alteração de Deployment, ConfigMap, Service, Gateway, Lambda, IAM, rede, banco, credenciais reais ou código. Nenhum teste de carga/disparo CPU/memória foi realizado, e a correção do logging de bootstrap da API permanece adiada.

## Execução isolada

Entre **20:09:26 e 20:09:28 UTC**, ou **17:09:26–17:09:28 em São Paulo**, um segundo processo do mesmo DLL foi iniciado como o usuário normal do container. Ouviu somente em `http://127.0.0.1:18082`; o processo principal e a porta 8080 permaneceram intactos.

Somente no ambiente do filho foi removido Runtime__Environment, para usar uma conexão local de validação. Os valores de banco/usuário/senha/JWT eram fictícios; o endpoint de banco foi `127.0.0.1:9`, com pooling desabilitado e timeout de dois segundos. A porta foi conferida fechada, com erro de conexão curl/exit 7. Nenhum comando alcançou o RDS. Foi mantido VerifyFull e o certificado publicado; a recusa TCP ocorre antes da negociação TLS, portanto não comprova novo handshake TLS.

A exportação reutilizou o Collector e o manifesto existentes. O Resource manteve serviço mecanica-api, versão do assembly e ambiente hom, com `service.instance.id=e5-open-1549a46a` para distinguir o processo de validação. O host ASP.NET Core usou Production; isso não muda o limite explícito de configuração local de bootstrap desse processo. A seleção local de SMTP não foi exercitada: login falhou na abertura da conexão antes de qualquer envio.

O processo tinha prazo externo de 40 s e restauração em trap. Não foi criado arquivo de script ou executável auxiliar. Após as chamadas, recebeu SIGTERM, encerrou com código zero e teve remoção/fechamento conferidos. O breve arquivo de console em /tmp foi removido.

## Resultados HTTP e métricas

| Chamada no processo isolado | Resultado | Duração do cliente |
|---|---:|---:|
| POST /Authentication, login fictício | **500** | 0,199011 s |
| GET /health/ready | **503** | 0,176834 s |

A persistência abre IDbConnection ao resolver os repositórios. A falha ocorreu nesse ponto, antes de consultar usuários, executar SQL ou emitir token. Não foi apresentada como senha incorreta ou erro de negócio. A readiness utilizou sua própria abertura assíncrona e retornou Unhealthy sem alterar a prontidão do processo principal, cujas probes usam 8080.

Na janela New Relic **20:08–20:12 UTC**, filtrada pela instância temporária:

| integration | operation | purpose | outcome | Soma |
|---|---|---|---|---:|
| postgresql | connection.open | application | failure | **1** |
| postgresql | connection.open | readiness | failure | **1** |

Ambos chegaram como mecanica-api/hom, sem misturar finalidade de probe com acessos da aplicação. Não houve incremento sintético, exporter fictício ou instrumentação alternativa. O provider nativo coletou/exportou as medições no encerramento gracioso; não foi necessário aguardar o intervalo periódico de 30 s.

## Trace, log e sanitização

Trace recebido W3C `8a0f70a5ce8aae092800000000000000`, com pai fictício `b3376b45a819f040`:

- Span HTTP `33ea36c4f06e3e5d`, nome POST Authentication, ERROR, descrição fixa **“Falha durante a requisição HTTP.”**
- Filho PostgreSQL `1567824319424282`, pai igual ao span HTTP, ERROR, descrição fixa **“Falha na operação PostgreSQL.”**
- Log correlacionado ao span HTTP: mensagem fixa da operação, StatusCode=500, ExceptionType=Npgsql.NpgsqlException, RequestId=`0HNPESO5JFQR8:00000001` e modelo Authentication.

A consulta de todos os sinais dessa instância encontrou **dois spans e um log**, correspondentes à requisição de aplicação; nenhum span adicional da probe. Nos registros/atributos consultados não apareceram mensagem original de exceção, stack trace, SQL, string de conexão ou URL completa. A conclusão é limitada à amostra inspecionada e não inclui uma auditoria irrestrita de todos os campos/eventos.

O console preservou mensagens fixas/tipo sem stack trace nesse erro HTTP. As advertências do framework sobre a porta HTTP configurada para o filho e ausência de porta HTTPS pertencem à execução local isolada; não alteraram contratos ou a configuração do processo principal.

## Restauração e alcance

Depois do encerramento: PIDs temporários 245/247 ausentes, porta 18082 fechada e arquivo /tmp removido. API original Running/Ready, zero reinícios, live/ready/startup **200/Healthy**. Collector continuou com uma réplica Ready; snapshot posterior da API 1m CPU/119 MiB. Nenhuma credencial real ou estado de negócio foi alterado.

**Comprovado:** falha da abertura síncrona de aplicação e assíncrona de readiness no Npgsql real, contagem única por acesso, separação de finalidade, HTTP e sinais sanitizados/correlacionados, encerramento e preservação do serviço principal. Não comprova timeout/cancelamento da abertura, abertura da Lambda, falhas de início/commit/rollback, indisponibilidade do RDS, tráfego do Gateway nesse processo ou prd.

E5.8 permanece parcial nos resultados ainda não exercitados. A comprovação de Secrets Manager da Lambda por IAM continua válida e independente. Artefato técnico local ignorado pelo Git: `artifacts/e5-validacao-integrada/20261020-postgresql-open/validation-summary.json`, sem credenciais reais ou tokens.
