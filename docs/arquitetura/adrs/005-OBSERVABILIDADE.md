# ADR 005 — OpenTelemetry e New Relic

**Estado:** aceito e implementado; alcance e limitações descritos nesta decisão.

**Contexto:** reunir métricas técnicas/negócio, traces e logs correlacionados, preservando sanitização e configuração declarativa. Não confundir telemetria agregada com fonte transacional exata.

## Decisão vigente

- API compõe OTel na entrada, com manifesto observability.json; nenhum projeto de domínio/serviço depende do SDK OTel. Meter nativo identifica acontecimentos; configuração seleciona coleta/dimensões/exportação.
- API envia OTLP ao Collector EKS Contrib 0.161.0. O provider de métricas nasce antes do bootstrap e é reutilizado; bootstrap malsucedido tem encerramento/exportação final limitado a 3.000 ms. Local é opt-in; overlay hom habilita e prd permanece false.
- Lambda reutiliza providers por handler, com invocação/PostgreSQL e dois instrumentos de autenticação, além do meter de integrações. SAM habilita OTel/X-Ray/New Relic permanentemente. Layer Collector 0.22.0 inclui Collector 0.151.0, loopback, decouple e sem batch periódico adicional. Flush da aplicação compartilha até 500 ms entre sinais e preserva reserva de resposta; isso não garante entrega após freeze/encerramento.
- Logs da API mantêm console textual; Lambda mantém uma linha JSON técnica. Mensagem fixa/tipo, sem stack trace ou mensagem original de exceção. OTLP é o único caminho remoto dos logs da aplicação, sem recoletar stdout/CloudWatch. Atributos/nomes são sanitizados antes de exportar, sem SQL, payload, credenciais ou identidade de negócio nas dimensões.
- **W3C somente:** traceparent válido é pai OTLP; sem ele, nova raiz da aplicação. PostgreSQL/logs se relacionam com essa raiz. Baggage/tracestate são descartados. Sampler coleta chamadas da aplicação mesmo com pai não amostrado; probes/SQL associado são excluídos dos traces da API.
- **X-Ray separado:** Gateway/Lambda emitem segmentos nativos por amostragem AWS; integração oficial New Relic faz polling. União automática X-Ray/OTLP foi retirada do escopo, não corrigida. Segmentos nativos não passam pela sanitização da aplicação e não são fonte de alerta imediato.
- Collector EKS coleta 23 métricas Kubernetes e enriquece sinais API por pod/namespace/Deployment. Kubeletstats cobre apenas o nó do Collector. CloudWatch consulta 13 estatísticas Gateway/Lambda/RDS por minuto, com atraso de dez minutos; logs Gateway retomam cinco minutos por restart, sem checkpoint/deduplicação/backfill.
- Collectors leem License Key do Secrets Manager pelo provider nativo alpha; valor não vai para Git, imagem/ZIP, variáveis Lambda, Kubernetes Secret ou estado Terraform. Pod Identity controla o Collector EKS; extensão compartilha a role da função. Rotação exige nova inicialização. Falha inicial de leitura impede o Collector; filas/retries externos são limitados e perdas são possíveis.

## Indicadores e publicação

Criação/durações/falhas de OS são observadas pelo fluxo HTTP após operações confirmadas; notificações são contadas no handler e falhas de medição são separadas. Durações usam histórico persistido, sem cronômetro de request ou zero artificial. Sem outbox/replay/entrega única. SQL continua referência por ocorrência/dia; OTLP não reconcilia automaticamente dados perdidos.

mecanica.integrations.operations mede cada acesso concluído a PostgreSQL/SSM/Secrets Manager, separando operation/purpose/outcome. Uma chamada lógica AWS conta depois das retentativas internas, não por retry de transporte. Indicadores principais usam application/bootstrap; readiness/telemetry e cancelamentos ficam separados. [Métricas das integrações](../rfcs/003-DADOS-E-OBSERVABILIDADE.md) e [RFC 003](../rfcs/003-DADOS-E-OBSERVABILIDADE.md).

Três dashboards JSON/Terraform: Operação da API e Kubernetes, Ordens de serviço, Integrações e autenticação. Cinco alertas: falha técnica OS, falha de notificação OS, CPU >80% da alocável/5 min, working set >85% da memória alocável/5 min e API sem réplica disponível/2 min. Hom/prd têm condições separadas; notificações New Relic enviam abertura/encerramento ao destinatário configurado, respeitando silenciamentos. Dez minutos sem pontos encerram incidentes sem abrir por ausência; não comprovam recuperação.

A unidade persistente observability/newrelic tem backend próprio, prevent_destroy e newrelic-provision manual somente main: plano salvo → newrelic-approval sem credenciais → apply exato/conferência sem reparo. User API Key fica em Secret do Environment newrelic; não é a License Key. Os destroys dos ambientes não removem dashboards/alertas/estado.

## Alternativas e consequências

Configuração declarativa foi preferida a listas operacionais duplicadas no código. Console textual foi preservado, sem novo stack de logs. Não foi adotado agente adicional, coleta duplicada, métrica de negócio por contagem de spans ou recuperação robusta. Filas em memória e timeouts não substituem garantias transacionais.

Filas/retries limitados podem perder sinais. Logs Gateway podem ter lacunas; encerramento de incidente por ausência não comprova recuperação. A topologia atual não comprova capacidade sob carga ou operação de prd.
