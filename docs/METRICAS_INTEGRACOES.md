# Métricas dos acessos às integrações

API e autenticação usam o meter `Mecanica.Integrations` e o contador `mecanica.integrations.operations`. Cada ponto representa **um acesso técnico concluído**, não uma requisição HTTP. Uma requisição pode abrir uma conexão, executar vários comandos e concluir uma transação. Um processo interrompido pode não registrar o acesso em andamento.

| Dimensão | Valores |
|---|---|
| integration | postgresql, ssm, secretsmanager |
| operation | connection.open, command.execute, transaction.begin, transaction.commit, transaction.rollback, get_parameters, get_secret_value |
| purpose | application, bootstrap, readiness, telemetry |
| outcome | success, failure, timeout, canceled |

O total soma todos os resultados. O percentual de falhas considera `(failure + timeout) / (success + failure + timeout)`; cancelamentos ficam separados e denominador ausente não vira zero. Uma exceção SQL conta como falha do comando, inclusive se posteriormente convertida em conflito HTTP. Isso não equivale necessariamente a HTTP 500 ou indisponibilidade do banco.

## Limites da medição

- A abertura mede a aquisição lógica da conexão, inclusive quando atendida pelo pool; não representa necessariamente uma nova conexão física.
- Cada chamada Dapper é uma operação; um lote com vários statements continua sendo uma chamada. Criação dos parâmetros/modelos e mapeamento do domínio ficam fora dessa medição. Consulta sem linhas é sucesso.
- Início, commit e rollback são operações separadas. Não medir o método inteiro do repositório nem descartar conexões como operações.
- SSM mede uma chamada lógica em lote a GetParameters. Secrets Manager mede cada GetSecretValue. O resultado incorpora as retentativas atuais do SDK, sem contar cada tentativa física.
- Resposta AWS obtida seguida de configuração inválida continua sendo chamada bem-sucedida; a falha de configuração mantém seu diagnóstico próprio.
- TimeoutException, timeout interno do driver ou cancelamento pelo prazo próprio são timeout. Quando o token do chamador já está cancelado, OperationCanceledException é canceled; isso inclui o orçamento global propagado pela Lambda. A métrica de invocação continua classificando o timeout do handler independentemente.
- readiness identifica a conexão da probe; telemetry identifica conexão/comando da leitura do histórico dos indicadores de OS. Os painéis principais consideram apenas application/bootstrap.

Não exportar SQL, parâmetros, conexões, ARNs, documentos, nomes de Secrets, mensagens de exceção ou stack traces. Somente as quatro dimensões permitidas; serviço, versão e ambiente reutilizam o Resource existente. As métricas independem da amostragem de traces. As métricas nativas de pools do Npgsql não foram habilitadas.

## Inicialização e descarte

O único provider de métricas da API é criado antes de carregar SSM/Secrets Manager e reutilizado durante a execução. Traces/logs mantêm sua composição habitual. As views e meters selecionados continuam no `observability.json`, incluindo os indicadores HTTP, runtime e de negócio existentes.

`Telemetry:StartupShutdownTimeoutMilliseconds=3000` limita o encerramento nativo do provider, inclusive após falha de inicialização. Shutdown coleta as medições finais; não acrescentar outro ForceFlush ou retentativas. A indisponibilidade do Collector pode impedir o envio. O diagnóstico é fixo, sem conteúdo das exceções. Sem telemetria e na geração oficial OpenAPI não há novos instrumentos/providers.

Na Lambda, a instância existente de observabilidade disponibiliza o componente ao bootstrap e ao lookup PostgreSQL. Ele usa o mesmo provider e flush compartilhado de 500 ms, sem flush por acesso e sem novo exporter. Runtime bem-sucedido é reutilizado; nova inicialização após falha produz novas operações reais.

## Conferência local e posterior

Os testes usam MeterListener para observar o contador nativo e PostgreSQL/Testcontainers nas fixtures existentes. Não usam exporter fictício, destino New Relic ou suíte de Collector. Docker deve estar aberto para executar a regressão completa; nenhum teste indisponível deve ser ignorado.

Depois dos PRs: api-deploy → auth-publish/auth-provision → Gateway consumindo a nova versão numérica → newrelic-provision para atualizar o dashboard. Não há alteração de IAM, Collector, SQL ou workflows. Conferir séries hom e filtros prd sem interpretar ausência como saúde. A credencial lida pela extensão/Collector não faz parte dos acessos medidos pela aplicação.

Operações e percentuais são agregados de pontos recebidos, sem replay, persistência ou entrega única. Falhas controladas exigem autorização própria. Sucessos e os resultados de falha descritos abaixo foram comprovados em hom; E5.8 permanece parcial pelos acessos/resultados não exercitados e pelo defeito de logging do bootstrap encontrado.

### Complemento de falhas controladas — 20/10/2026

**Timeout/cancelamento da readiness comprovados localmente, 23:06–23:07 UTC:** [relatório](VALIDACAO_CANCELAMENTO_POSTGRESQL_LOCAL_2026-10-20.md). Revisão a8ba261 em API/Collector/PostgreSQL descartáveis, rede Docker interna e pausa somente desse banco. Baseline/recuperação 200; espera 503 em 2,032 s e cliente cancelado em 0,302 s. OTLP real no Collector: connection.open/readiness success=2, timeout=1, canceled=1; sem spans de probes. Containers/rede removidos, AWS/RDS/New Relic/código intactos. Não amplia a comprovação para comandos, OS, Lambda ou AWS. Achado separado: o middleware ainda registra o cancelamento esperado como erro inesperado/StatusCode=500, embora o cliente não receba resposta; registrado para avaliação, sem correção nesta etapa. Stack trace do bootstrap continua adiado; CPU/memória/carga dispensados.

**Tentativa de timeout/cancelamento PostgreSQL, 22:57 UTC:** [relatório](VALIDACAO_CANCELAMENTO_POSTGRESQL_HOM_2026-10-20.md). Processo isolado, conexão fictícia para porta HTTP de loopback; ambas as chamadas concluíram 503 antes dos limites. New Relic recebeu somente connection.open/readiness/failure=2. Resultado inconclusivo para timeout/canceled, sem erro de classificação comprovado. Guard interrompeu o método e trap removeu processo/arquivo; API principal Ready/zero reinícios/probes 200, Collector Ready. Não repetir o mecanismo como se comprovasse cancelamento. Sem mudança de código/RDS/IAM/Deployment; logging startup adiado e carga dispensada.

**Abertura PostgreSQL da API comprovada em processo isolado:** [relatório](VALIDACAO_ABERTURA_POSTGRESQL_HOM_2026-10-20.md), execução 20:09:26–20:09:28 UTC. Mesmo binário publicado no pod hom, com configuração local fictícia apenas no filho, porta HTTP loopback e PostgreSQL loopback fechado. Handler 500 e readiness isolada 503; New Relic recebeu um connection.open/application/failure e um connection.open/readiness/failure, dois spans HTTP → PostgreSQL e um log correlacionado/sanitizado. Processo encerrado, arquivo removido; API principal manteve probes 200/Healthy, Ready e zero reinícios, Collector Ready. Não é indisponibilidade do RDS nem teste de abertura da Lambda, timeout/cancelamento ou transações. Nenhum Deployment/RDS/IAM/código alterado; logging startup continua adiado e carga dispensada.

**Secrets Manager da Lambda comprovado posteriormente por IAM:** [relatório](VALIDACAO_SECRETS_LAMBDA_IAM_HOM_2026-10-20.md), janela 19:50–19:55 UTC. Role temporária somente em $LATEST negou JWT e preservou License Key do Collector. CloudTrail confirmou AccessDenied no JWT/leitura da License Key bem-sucedida; handler registrou 500/technical-failure, seguido de recuperação 400/invalid-document. New Relic: SSM success=2, Secrets success=2/failure=1, dois logs e dois spans correlacionados, erro com descrição fixa e sem atributos proibidos consultados. Role original/policy/código/ambiente restaurados e conferidos; role temporária removida, Gateway v10 intacto. A lacuna de Secrets failure da Lambda foi comprovada para acesso negado; timeout/indisponibilidade desse serviço e demais variantes continuam fora dessa prova. As tentativas de proxy abaixo são históricas, não a comprovação.

**Tentativa posterior Secrets Manager da Lambda:** [relatório](VALIDACAO_SECRETS_LAMBDA_HOM_2026-10-20.md), 19:18–19:24 UTC. Resultado inconclusivo: proxy temporário em $LATEST atingiu comunicação do runtime antes do handler, inclusive após tentar excluir o host observado. Configuração restaurada em ambas as tentativas; recuperações retornaram 400/invalid-document sem FunctionError. Não houve failure de Secrets comprovado, nem mudança de Gateway/v10, IAM, conteúdo de Secrets ou código. Essa pendência continua aberta, sem contabilizar o erro do experimento como acesso Secrets.

A [rodada de falhas e alertas](VALIDACAO_FALHAS_E5_HOM_2026-10-20.md), 17:58–18:36 UTC, comprovou:

- API PostgreSQL `command.execute/failure` em processamento de OS com rollback e em consulta de notificação após commit; esta última preservou HTTP 204 e o estado persistido.
- Lambda PostgreSQL `command.execute/timeout`: um comando atingiu o prazo do driver, retornando HTTP 500; a próxima consulta retornou 200.
- API bootstrap: SSM `get_parameters/failure=1` e Secrets Manager `get_secret_value/failure=1`, com coleta final observada no New Relic. SSM teve um sucesso antes da tentativa de Secrets.
- Lambda bootstrap SSM `failure=1`, seguido de inicialização recuperada com SSM `success=1` e Secrets `success=2`. O experimento usou somente `$LATEST`; Gateway/v10 foi preservado e o ambiente original restaurado/conferido.
- Alertas de processamento, notificação e disponibilidade completaram abertura/encerramento e tiveram seis e-mails confirmados pelo mantenedor. CPU/memória não tiveram disparos provocados.

**Pendência encontrada:** falhas de startup SSM/Secrets da API ainda produzem exceção não tratada com cinco frames no console em cada caso, apesar da mensagem fixa. O middleware HTTP não cobre essa etapa. Não houve correção de código nesta validação. As conclusões de sanitização dos spans/logs OTLP inspecionados não certificam esse console.

Falha Secrets da Lambda não foi executada porque o override de endpoint também atingiria a leitura da License Key pela extensão. Cancelamento pelo chamador, falhas de abertura/transações e demais combinações não foram comprovados. A ausência temporária do Collector preservou as operações da API e a exportação retomou, mas os três traces da interrupção não apareceram na consulta; não há replay garantido.

### Conferência implantada de 20/10/2026

A [validação de hom](VALIDACAO_E5_HOM_2026-10-20.md) comprovou API a8ba261 e autenticação versão 10/0446a19. Cada processo realizou uma chamada SSM e duas Secrets; o HPA criou um segundo pod da API, com seu próprio bootstrap. A API exportou 223 acessos PostgreSQL de aplicação, dois rollbacks bem-sucedidos e cinco pares connection.open/command.execute para leitura de histórico, separados das probes. A Lambda exportou seis acessos PostgreSQL e reutilizou seu runtime.

Os quatro novos painéis foram conferidos com os mesmos totais e últimas amostras. Não houve séries failure/timeout/canceled na rodada normal; nenhuma falha técnica foi induzida para preencher o dashboard. A correção dos endpoints dos exporters da API foi comprovada com traces, métricas e logs reais no New Relic. A reconciliação SQL direta foi concluída depois; disparos dos alertas permanecem pendentes.

Em complemento autorizado, uma leitura de catálogo foi cancelada no PostgreSQL sob lock temporário, seguido de rollback e recuperação 200 com catálogo idêntico. A janela 14:57–15:03 UTC de 20/10 contém seis aberturas bem-sucedidas, cinco comandos bem-sucedidos e um `command.execute/failure`, todos da API/application. A fórmula do painel retornou 1/12 = 8,333333333333334%; inclui acessos técnicos, não somente requisições. O log e os spans HTTP/PostgreSQL correlacionados preservaram mensagens fixas e o tipo/código do erro, sem os atributos proibidos consultados. Probes Healthy, zero reinícios e limpeza do pod confirmados. O cancelamento pelo servidor resultou em `PostgresException/57014`; não equivale a `canceled` pelo token do chamador. Falhas Lambda/SSM/Secrets, abertura de conexão, transações, timeout/canceled e alertas continuam fora dessa comprovação. Detalhes e limites no complemento do relatório de hom.

### Validação local de 19/10/2026

Build Release e publish da API/Lambda aprovados; Lambda conferida com SDK 10.0.402, RID linux-x64 e self-contained=false. Certificado RDS, manifesto com o novo meter, dependências/runtimeconfig e ZIP local conferidos, sem exporter de teste. SAM validate --lint/build aprovados; Kustomize hom/prd renderizado. Terraform fmt/init sem backend/validate e template renderizado como JSON válido; quatro painéis adicionados sem sobreposição e sem alterar os outros dashboards, cinco alertas ou lockfile.

Regressão completa concluída após o mantenedor abrir o Docker: **1.187 testes API e 245 testes Lambda aprovados, zero falhas e zero ignorados**, sem filtros. Inclui PostgreSQL/Testcontainers, abertura/consulta, consulta sem resultado, erro de domínio fora da medição SQL, commit/rollback, readiness, histórico de telemetria e lookup da autenticação. Os novos cenários passaram sem ajustes adicionais de código. A execução anterior sem Docker (1.021/205 testes) foi parcial e está substituída por esta conferência completa. Não houve inicialização automática do Docker.

OpenAPI idêntico: SHA-256 D784C3B554A65C4F8CFE732C0975B8BFEDAF78D67F9C6ED69E2B07220C5F5D84. Avisos anteriores de dependências/analisadores permanecem. Evidências locais em artifacts/e5-integrations na API e na autenticação. Nenhum workflow remoto, deploy, consulta AWS ou publicação New Relic foi executado. E5.8 permanece parcial.
