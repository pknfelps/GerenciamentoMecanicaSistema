# ADR 005 — OpenTelemetry com centralização no New Relic

**Estado:** aceito como arquitetura; compatibilidade a comprovar em E5. **Formalização:** 2026-09-16. **Origem:** [D04](../../plano-fase-3/decisoes/OBSERVABILIDADE.md). [Índice](../README.md)

**Contexto:** demonstrar métricas, logs correlacionados, traces, indicadores de OS e alertas com padrão OpenTelemetry. Gateway possui caminhos nativos AWS distintos da instrumentação da aplicação.

**Decisão:** API → Collector Contrib no EKS → OTLP/New Relic; Lambda → extensão Collector local → New Relic. Collector também coleta Kubernetes e consulta CloudWatch para logs do Gateway/métricas AWS. Traces nativos do Gateway usam integração oficial X-Ray/New Relic. Manter três dashboards e alertas separados por ambiente/janela ativa.

**Alternativas:** centralização exclusiva em CloudWatch não atende à ferramenta escolhida. Firehose para encaminhar logs e integração alternativa para coleta AWS não foram adotados neste desenho. Não adicionar Prometheus, Operator ou sidecar por pod inicialmente.

**Consequências:** precisam ser comprovados o receiver CloudWatch em versão fixada, a layer compatível da Lambda e a correlação W3C/AWS. X-Ray sofre atraso de polling; Collector desligado interrompe encaminhamento de CloudWatch. Filas limitadas podem perder sinais. Evitar duplicação, dados pessoais e cardinalidade alta; telemetria não bloqueia negócio. Custos/limites de ingestão serão conferidos antes do deploy.

**Comprovação:** E5/E6 validam sinais conhecidos, ausência de duplicação, erro controlado e isolamento de ambientes; falha de compatibilidade exige revisão explícita antes de trocar o caminho. [RFC 003](../rfcs/003-DADOS-E-OBSERVABILIDADE.md) · [Fluxo dos sinais](../diagramas/COMPONENTES.md).
