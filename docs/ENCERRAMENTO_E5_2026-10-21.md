# Encerramento da E5 — 21/10/2026

**E5 concluída por aceite do mantenedor no escopo de hom**, após a entrega de instrumentação, Collectors, ingestão, dashboards, alertas e conferências documentadas. Prd não será validado nesta etapa. O encerramento não transforma cenários inconclusivos, dispensados ou não exercitados em testes aprovados.

O mantenedor solicitou somente a reconferência AWS do logging e o encerramento da etapa, sem acrescentar a métrica de threads à entrega. A coleta automática de System.Runtime existente não foi alterada; não foi criada nova métrica, view, regra de conversão ou filtro para ocultar o aviso observado. O tratamento adicional dessa métrica fica fora do escopo aceito.

## Evidências principais

- API e Lambda instrumentadas, três sinais recebidos pelo New Relic, identidade de serviço/versão/ambiente e sanitização conferidas.
- Política W3C comprovada nos caminhos públicos de hom: API 9d1c0625… e Lambda 12/bf879cfe…, Gateway 18af09dzw7. Na matriz principal de 21/10: vinte respostas previstas, 45 spans, 25 filhos PostgreSQL corretos e nove logs correlacionados. JWT Customer emitido pela Lambda aceito pela API, com autorização preservada.
- Indicadores de OS e notificações simuladas, dashboards e conferência SQL já registrados nas rodadas anteriores. Isso não afirma entrega SMTP real em hom/prd.
- Treze estatísticas CloudWatch comparadas com a AWS; métricas/enriquecimento Kubernetes e X-Ray por polling recebidos. Tracing OTLP e X-Ray são fontes independentes por decisão do mantenedor.
- Alertas de falha de processamento de OS, falha de notificação e ausência de réplica comprovaram abertura/encerramento e recebimento dos seis e-mails, confirmados pelo mantenedor. CPU/memória tiveram configuração/medições conferidas; disparos e teste de carga foram dispensados explicitamente.

Referências: [validação pública de tracing](VALIDACAO_TRACING_W3C_HOM_2026-10-21.md), [rodada integrada anterior](VALIDACAO_E5_HOM_2026-10-20.md), [falhas e alertas](VALIDACAO_FALHAS_E5_HOM_2026-10-20.md), [ajustes locais de logging](AJUSTES_LOGS_API_2026-10-20.md).

## Última reconferência AWS de logging

Utilizado o binário implantado no pod `deploy-gerenciamento-api-7b4b6f79db-wk8qs`, imagem `sha256:28758cadb93e93699f1ae521136c5c1be86cd6d9b7a074c03c78049b6b9bd8cb`. A sessão CLI administrativa foi renovada; o acesso Kubernetes utilizou o perfil operador mecanica, que já tinha autorização. Não foram criadas permissões.

### Falha de inicialização: aprovada

Um processo isolado do mesmo DLL recebeu runtime hom, credenciais fictícias e endpoint SSM em loopback fechado. Não utilizou credenciais reais nem consultou Secrets. O prazo externo foi limitado a 55 segundos.

Resultado: **código de saída 1** e somente a linha:

```text
Falha na inicialização ou execução da API. ExceptionType: System.InvalidOperationException
```

Sem stack trace, mensagem original, endpoint ou credenciais no console capturado. O New Relic recebeu **ssm / get_parameters / bootstrap / failure = 1**, filtrado por `service.instance.id=e5-logging-bootstrap-20261021`. A exportação final do provider de métricas foi preservada. O arquivo remoto foi removido.

### Cancelamento pelo cliente: inconclusivo na AWS

Os processos de validação ouviram somente em loopback/18084, com configuração fictícia para PostgreSQL descartável no namespace database-init. Nenhum acesso desse teste atingiu o RDS. O PostgreSQL temporário utilizou volume efêmero, credenciais fictícias e foi removido após cada tentativa. TLS do RDS e configuração do processo principal não foram alterados.

A primeira tentativa não pausou efetivamente o servidor e concluiu 200 antes da desconexão; foi rejeitada pelo guard. Depois, o postmaster foi executado como filho, e seu estado parado foi confirmado antes da chamada. Foram exercitados limite do cliente HTTP/1, observação da conexão PostgreSQL seguida de desconexão, HTTP/2 apenas no filho e reset TCP explícito por ferramenta já instalada. Não foram criados endpoints de teste ou executáveis permanentes.

O cliente foi encerrado, mas o acesso de readiness terminou pelo prazo de aproximadamente **dois segundos**, com diagnóstico fixo de health check Unhealthy. **Não foi observado o log Information/499 esperado do middleware.** O New Relic registrou timeout nas tentativas consultadas; não classificar esses resultados como canceled ou como comprovação do caminho RequestAborted. A causa dessa diferença não foi estabelecida nesta entrega, e nenhuma correção de código foi feita durante a reconferência.

A comprovação de Information/499 e ausência de corpo/stack permanece a dos testes e do PostgreSQL/Docker locais documentados. Por decisão de encerramento do mantenedor, o cenário AWS fica registrado como inconclusivo, sem impedir o aceite da E5 e sem novas tentativas nesta etapa.

### Estado final

Conferência às **14:08:38 UTC / 11:08:38 em São Paulo**: API Ready, zero reinícios e live/ready/startup 200; Collector 1/1 Ready/Available. Somente o processo principal dotnet permaneceu no pod. Pods de validação e arquivos temporários explicitamente utilizados foram removidos. Não houve mudança persistente de Deployment, ConfigMap, Service, IAM, Gateway, Lambda, RDS, código, workflows ou Collectors.

Evidências locais sanitizadas: `artifacts/e5-logging-aws-20261021`, incluindo console do bootstrap, resultados dos guards de cancelamento e estado final. Os experimentos não enviaram notificações de negócio nem alteraram cadastros/OSs.

## Limitações aceitas no encerramento

- Prd não validado; não estender os resultados de hom a esse ambiente.
- Logs Gateway com lacunas aceitas: 15/20 nesta rodada e 43/45 na referência anterior, sem replay ou garantia de entrega única. A causa da amostra antiga não foi automaticamente atribuída aos cinco eventos novos.
- Aviso de descarte de `dotnet.thread_pool.thread.count` preservado, sem novo tratamento ou nova métrica nesta entrega.
- Cancelamento AWS inconclusivo, apesar da comprovação local; demais falhas de transações/abertura Lambda e estados completos de congelamento/descongelamento mantêm a cobertura descrita nos relatórios.
- Filas não foram saturadas e destino New Relic lento/401/429/5xx não foi exercitado; perdas e ausência de replay permanecem documentadas. CPU/memória/carga dispensados não são próximos testes obrigatórios da E5.

Os checkboxes da E5 representam **entrega e aceite do escopo**, acompanhados dessas ressalvas. Não representam certificação integral de todos os cenários operacionais possíveis. Novas correções ou ampliações exigirão uma entrega própria; não há provisionamento, destroy ou alteração automática como consequência deste encerramento.
