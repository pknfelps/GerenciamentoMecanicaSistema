# E5 — Tentativa de timeout/cancelamento PostgreSQL — 20/10/2026

**Timeout/cancelamento da readiness comprovados localmente, 23:06–23:07 UTC:** [relatório](VALIDACAO_CANCELAMENTO_POSTGRESQL_LOCAL_2026-10-20.md). Revisão a8ba261 em API/Collector/PostgreSQL descartáveis, rede Docker interna e pausa somente desse banco. Baseline/recuperação 200; espera 503 em 2,032 s e cliente cancelado em 0,302 s. OTLP real no Collector: connection.open/readiness success=2, timeout=1, canceled=1; sem spans de probes. Containers/rede removidos, AWS/RDS/New Relic/código intactos. Não amplia a comprovação para comandos, OS, Lambda ou AWS. Achado separado: o middleware ainda registra o cancelamento esperado como erro inesperado/StatusCode=500, embora o cliente não receba resposta; registrado para avaliação, sem correção nesta etapa. Stack trace do bootstrap continua adiado; CPU/memória/carga dispensados.

**Resultado: inconclusivo para timeout e cancelamento.** As duas aberturas terminaram como falha antes do limite do cliente. O New Relic recebeu somente `connection.open/readiness/failure=2`; não houve comprovação de `timeout` ou `canceled`.

## Método e limites

Na janela iniciada às **22:57:22 UTC (19:57:22 em São Paulo)**, foi usado um segundo processo do mesmo binário publicado da API a8ba261, no pod deploy-gerenciamento-api-589bb58bf7-bqnmr. Configuração local fictícia somente no filho, recurso hom e service.instance.id=e5-cancel-81c0b347.

O HTTP desse processo ouviu apenas em 127.0.0.1:18082. Sua conexão PostgreSQL fictícia apontou para essa mesma porta HTTP de loopback, preservando VerifyFull e o certificado publicado. A intenção era que a negociação PostgreSQL aguardasse, permitindo comparar o prazo nativo da readiness (dois segundos) com o encerramento do cliente em 300 ms. O endpoint HTTP não sustentou a espera: a abertura terminou rapidamente. Não é uma indisponibilidade real do RDS, nem prova de um handshake TLS bem-sucedido.

Não foram criados servidor auxiliar, mock, script em arquivo, executável, endpoint de falhas ou métricas sintéticas. Não houve acesso ao RDS pelo processo isolado, uso de credenciais reais ou mudança de Deployment, IAM, Gateway, Lambda, Collector ou código.

## Resultado observado

| Chamada à readiness isolada | HTTP | Duração | Saída curl |
|---|---:|---:|---:|
| Cliente com limite de seis segundos | 503 | 0,197840 s | 0 |
| Cliente com limite de 300 ms | 503 | 0,007448 s | 0 |

A segunda chamada concluiu antes dos 300 ms. Portanto, não houve cancelamento pelo cliente. O guard do experimento esperava saída curl 28 e interrompeu a execução com código 1 ao obter 0; o trap executou a limpeza. A falha do guard indica método inadequado para o objetivo, não erro comprovado da classificação na aplicação.

Consulta New Relic, janela **22:56–23:00 UTC**:

```sql
SELECT sum(`mecanica.integrations.operations`)
FROM Metric
WHERE metricName = 'mecanica.integrations.operations'
  AND `service.instance.id` = 'e5-cancel-81c0b347'
SINCE '2026-10-20 22:56:00+0000'
UNTIL '2026-10-20 23:00:00+0000'
FACET integration, operation, purpose, outcome
LIMIT 20
```

Único resultado: postgresql / connection.open / readiness / failure, soma **2**. Não reclassificar esses pontos como timeout/canceled e não misturá-los com os painéis principais application/bootstrap.

## Limpeza e situação final

Confirmadas a ausência dos processos temporários 452/454 e do arquivo /tmp/e5-cancel-81c0b347.log. API principal permaneceu Ready, zero reinícios, live/ready/startup 200/Healthy. Collector com uma réplica Ready.

Timeout/canceled continuam pendentes de um método distinto e controlado; não repetir este método como se comprovasse a espera. A falha de abertura anteriormente validada continua válida em seu próprio escopo. Não foram executados testes de carga ou disparos de CPU/memória; a correção do logging de inicialização continua adiada por decisão do mantenedor.

Evidência técnica local ignorada pelo Git: artifacts/e5-validacao-integrada/20261020-postgresql-cancellation/validation-summary.json.

