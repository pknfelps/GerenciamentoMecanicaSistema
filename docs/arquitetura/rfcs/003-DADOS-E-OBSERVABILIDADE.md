# RFC 003 — Histórico de OS, indicadores e observabilidade

**Estado:** arquitetura aceita; implementação pendente. **Registro:** 2026-09-16. [Índice](../README.md)

## Persistência e cálculo

Manter `orders.Status` como estado atual e criar `order_status_history` com ID único, ID da OS, status anterior/novo, instante UTC e motivo. Criação registra entrada em Received com anterior nulo. Mudança de estado e histórico participam da mesma transação, com proteção de concorrência e ordenação determinística.

Excluir uma OS individualmente preserva seu histórico; não usar exclusão em cascata para esses registros. Excluir o banco inteiro encerra naturalmente esse histórico. Remover `date_created`, `date_finished` e `duration` persistidos em orders; projeções de API derivam datas/duração do histórico, usando ausência de valor para etapas incompletas. Consultas de listagem/ordenação devem evitar N+1.

| Indicador | Regra aceita |
|---|---|
| OS por dia | Criação confirmada, agrupada em America/Sao_Paulo; recusa/exclusão posterior não reduz volume histórico |
| Diagnóstico | InDiagnosis até WaitingForApproval |
| Execução | InExecution até Finished por serviço concluído; recusa não produz amostra zero |
| Finalização | Finished até Delivered; separar conclusão do serviço e recusa pelo motivo |
| Tempo total de atendimento | Criação até Finished por serviço concluído; inclui esperas anteriores, exclui retirada e recusas |
| Erros de integração | Tentativas técnicas com falha sobre tentativas totais; cada retry conta; CPF inválido e recusa não são erro técnico |

Intervalos usam tempo contínuo, incluindo período desligado, e pertencem ao dia de saída da etapa. Médias usam intervalos encerrados; ausência de amostra não significa zero.

Não há migração/backfill: versionar SQL e seeds para banco vazio, executados por Job da pipeline do banco. Não resetar o esquema ao iniciar pods. ER, índices e mecanismo de preservação do vínculo histórico serão detalhados em E2/E4.

## Coleta e correlação

New Relic centraliza sinais via OpenTelemetry/OTLP. A API envia ao Collector Contrib no EKS; a Lambda usa extensão Collector local e não depende do cluster para exportar. O Collector coleta métricas Kubernetes e, com permissões restritas ao ambiente, consulta CloudWatch para logs do Gateway e métricas AWS. O tracing nativo do Gateway chega pela integração oficial X-Ray/New Relic.

Usar nomes de serviço `mecanica-api` e `mecanica-auth`, versão do commit e `deployment.environment.name` hom/prd. Correlacionar logs/traces; ID de OS não é dimensão de métrica. Não exportar CPF, JWT, credenciais ou payloads sensíveis. Não duplicar logs de aplicação por diferentes coletores. Probes mantêm métricas de saúde, sem traces rotineiros e consultas associadas.

Amostragem das spans de negócio produzidas pela API/função será de 100% nas janelas curtas, respeitando a configuração de contexto acordada. Isso não garante 100% de tracing nativo do Gateway. Métricas de negócio independem de traces amostrados. A integração X-Ray tem atraso e não é fonte para alertas imediatos.

## Operação e comprovação

Três dashboards cobrem operação, OS e integrações, com filtro por ambiente. Alertas iniciais: CPU de nó acima de 80% e memória acima de 85% por cinco minutos, Deployment da API sem réplica disponível por dois minutos durante janela ativa e ao menos uma falha técnica de OS em cinco minutos; falhas de SMTP ficam separadas. Confirmar destinatário e testar alerta por falha controlada, sem teste de carga.

A exportação possui filas limitadas em memória: pode perder dados e não participa da transação de negócio. Publicar métricas após commit e implementar/verificar deduplicação e reconciliação com o histórico antes de considerá-las evidência confiável. Não foi escolhido outbox ou um novo serviço para isso.

A implementação abrange modelo/negócio, instrumentação, compatibilidade do receiver CloudWatch e layer Lambda, dashboards e correlação, seguida da demonstração integrada. Comparar amostras conhecidas no banco com indicadores e verificar ausência de duplicação/dados pessoais.

**Justificativas:** [ADR 003](../adrs/003-BANCO-E-HISTORICO.md) e [ADR 005](../adrs/005-OBSERVABILIDADE.md).
