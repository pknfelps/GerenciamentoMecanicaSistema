# E5 — Timeout e cancelamento PostgreSQL locais — 20/10/2026

**Dois ajustes de logging implementados e validados localmente:** [relatório](AJUSTES_LOGS_API_2026-10-20.md). Main retorna código 1 com mensagem fixa/tipo na falha, sem impressão automática da stack; descarte/exportação das métricas preservados. Cancelamento por RequestAborted gera Information, sem Problem Details e com status técnico 499 antes da resposta começar; resposta iniciada preservada e cancelamento interno continua 500. 1.197 testes completos aprovados, zero falhas/ignorados; Docker comprovou cancelamento e falha real SSM em loopback, com saída 1/zero frames e métrica failure=1. OpenAPI idêntico, publish/certificado/Kustomize conferidos; recursos temporários removidos. Nada foi implantado: publicar API e reconferir somente a nova revisão. Não alterar/checkar como concluída a comprovação AWS de E5.2 ainda.

**Classificação comprovada no caminho real de abertura da readiness da API:** o Collector local recebeu exatamente **success=2, timeout=1 e canceled=1**, todos postgresql/connection.open/readiness. Não foi necessário alterar código. O PostgreSQL descartável foi retomado, a API recuperou 200/Healthy e todos os containers e a rede de validação foram removidos.

**Achado separado:** embora a métrica classifique corretamente canceled, o middleware registra esse cancelamento como erro inesperado/StatusCode=500. O cliente já havia desconectado e não recebeu uma resposta HTTP 500. O achado fica registrado para avaliação; não foi corrigido nesta validação.

## Escopo e preparação

Execução principal em **20/10/2026, 23:06:44–23:07:01 UTC**, ou **20:06:44–20:07:01 em São Paulo**. Instância local e descartável, sem acessar AWS, RDS ou New Relic.

- Revisão: a8ba2615d9fe918fc161d975fabd406b54fd0ff2.
- Publish Release linux-x64, sem self-contained, com restore existente; versão do assembly confirmada como 1.0.0+a8ba2615d9fe918fc161d975fabd406b54fd0ff2.
- SHA-256 do DLL: 106301E635A9BD1CA9DA1D9E89D4DB42BE668A8801BD4A2AB1B131F5F1CB93FF.
- PostgreSQL real postgres:16.15, dados em tmpfs e credenciais fictícias. Pooling desabilitado para cada abertura alcançar o servidor.
- Runtime mcr.microsoft.com/dotnet/aspnet:10.0, publish montado somente leitura, usuário não root e filesystem somente leitura.
- Collector Contrib 0.161.0, YAML local existente com exporter debug/detailed; nenhuma chave ou destino externo.
- Rede Docker interna, sem porta publicada na execução principal. Chamadas por curl de uma imagem já disponível, executando somente essa ferramenta.
- service.name=mecanica-api, ambiente local, service.instance.id=e5-local-pg-947a.
- Manifesto preservado; somente intervalo nativo de exportação de métricas reduzido para 1.000 ms no container temporário.

O PostgreSQL local foi usado sem TLS, como configuração descartável desta conferência. Isso não altera nem valida o TLS do RDS. A composição não definiu Runtime__Environment e não carregou SSM/Secrets Manager. Não foram usados dados de negócio, tokens ou credenciais reais.

A publicação local concluiu com os avisos de dependência/analisador já existentes, inclusive NU1902/NU1903 e S6667; não houve atualização de pacotes, supressão ou correção de código. A primeira execução de publish sem elevação terminou sem produzir o binário; a repetição com acesso autorizado concluiu.

## Método e resultados

Primeiro, a readiness foi conferida com o banco ativo. Depois, docker pause suspendeu somente os processos do PostgreSQL descartável, mantendo o container e sua rede existentes. Diferentemente do método anterior com uma porta HTTP, a conexão encontrou um servidor PostgreSQL cuja negociação ficou pendente.

A primeira chamada aguardou o prazo nativo. A segunda teve o cliente encerrado antes desse prazo. Após uma pequena janela para a medição terminar, docker unpause retomou o servidor e foi feita uma nova chamada de recuperação.

| Fase | Resultado do cliente | Duração HTTP | Medição recebida |
|---|---|---:|---|
| Baseline, banco ativo | 200 | 0,296386 s | success=1 |
| Banco pausado, cliente aguarda até 6 s | 503 | 2,031987 s | timeout=1 |
| Banco pausado, cliente encerra após 300 ms | Sem resposta HTTP; curl exit 28 | 0,302234 s | canceled=1 |
| Banco retomado | 200 | 0,006453 s | success=1 |

O valor 000 do curl na chamada cancelada significa ausência de resposta HTTP, não um código HTTP emitido pela API.

PostgreSQL pausado às 23:06:49.728 UTC e retomado às 23:06:54.147 UTC. A execução foi sequencial, sem carga, concorrência artificial ou falha em dependência compartilhada.

## Exportação e sanitização observadas

Foram inspecionados os quatro pontos delta do instrumento mecanica.integrations.operations no debug do Collector:

| integration | operation | purpose | outcome | Total |
|---|---|---|---|---:|
| postgresql | connection.open | readiness | success | 2 |
| postgresql | connection.open | readiness | timeout | 1 |
| postgresql | connection.open | readiness | canceled | 1 |

Cada acesso concluído gerou uma medição. Não houve failure nem mistura com application/bootstrap/telemetry. A exportação foi OTLP real para o Collector existente, sem MeterListener, exporter fictício ou métricas sintéticas.

A amostra contém zero spans, coerentemente com exclusão das probes, e um log da aplicação. Não apareceu instrumento de falha de processamento de OS; nenhuma ação de OS foi chamada. Nos registros exportados inspecionados não apareceram SQL, string de conexão, credencial fictícia, nome de banco/host PostgreSQL ou stack trace. Esta conclusão é limitada ao conteúdo capturado, sem auditoria irrestrita de outras fontes.

## Achado de logging do cancelamento

O cancelamento da readiness propagou TaskCanceledException ao catch genérico de ExceptionHandlingMiddleware. Ele registrou a mensagem fixa **“Erro inesperado durante a requisição.”**, nível Error e propriedade StatusCode=500, com tipo/IDs técnicos, sem mensagem original ou stack trace.

Log: request 0HNPEVR8Q306F:00000001, trace 65eafd9e99ce2deec376f04aa99404e6, span 13c34ad78ae4d6d7. A classificação canceled do contador está correta, mas esse log pode induzir a interpretação de falha inesperada quando o chamador interrompeu a requisição.

Não afirmar que o cliente recebeu HTTP 500: o resultado real foi desconexão/curl 28. Não foi executada ação de OS; essa amostra não prova cancelamento de persistência em uma ação de OS. O middleware já condiciona o contador de falhas de OS à ausência de RequestAborted, conforme revisão do código, mas isso não foi exercitado aqui.

Nenhum ajuste foi aplicado. Este achado é distinto do stack trace no bootstrap da API, cuja correção continua adiada até finalizar as validações, conforme decisão do mantenedor.

## Preparação inicial e limpeza

Uma preparação anterior, 23:04:51–23:06:22 UTC, publicou uma porta de uma rede Docker interna que não ficou acessível pelo Windows. A aplicação respondeu 200 por acesso interno ao container; nenhuma falha PostgreSQL foi induzida nessa preparação. O erro local “API not listening” descreve o guard de acesso do Windows, não uma falha comprovada do servidor. Os containers/rede foram removidos e as evidências preservadas em connectivity-attempt.

A execução principal corrigiu somente a localização do cliente, levando-o para a mesma rede interna. Após a recuperação:

- API encerrada graciosamente, com logs e último envio capturados.
- Containers de API, PostgreSQL e Collector removidos; nenhum volume persistente criado.
- Rede de teste removida com código zero.
- Consultas Docker independentes confirmaram ausência dos containers/rede rotulados; não havia outros containers em execução.
- Nenhuma alteração AWS, workflow, código de produção, manifesto versionado ou contrato HTTP.

Artefatos locais ignorados pelo Git: artifacts/e5-validacao-integrada/20261020-local-pg-cancellation, incluindo publish, execution-summary.json, validation-summary.json e logs do Collector/API/PostgreSQL. Nenhum script permanente foi criado.

## Conclusão e limites

Timeout e cancelamento da abertura da readiness estão comprovados **localmente**, com a revisão atual e transporte OTLP real. O método anterior inconclusivo permanece documentado como histórico e não invalida esta amostra.

Não comprova cancelamento de comandos/ações de OS, todos os propósitos ou integrações, abertura da Lambda, falhas de begin/commit/rollback, condições de rede/TLS do RDS ou ingestão desses pontos no New Relic. E5.8 permanece parcial nos demais cenários e na comprovação AWS que ainda falta. Disparos de CPU/memória e testes de carga continuam dispensados pelo mantenedor.

