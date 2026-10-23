# RFC 003 — Histórico de OS, indicadores e observabilidade

**Estado:** implementada; E5 encerrada por aceite e E6 concluída no escopo hom. Revisão de 21/10/2026; registro original 16/09. [Arquitetura](../README.md) · [Modelo e justificativa](../BANCO_E_MODELO.md)

## Persistência e durações

orders mantém estado/orçamento; datas/duração são projetadas do histórico. Criação e evento Received/sequence=1 são atômicos; transições bloqueiam a OS e gravam estado/evento juntos. Histórico sem FK para orders sobrevive à exclusão individual, mas não à destruição do banco. Itens guardam seus valores, sem depender de preços atuais do catálogo.

| Indicador | Medição |
|---|---|
| OS criada | Uma criação confirmada pelo fluxo HTTP; exclusão/recusa posterior não decrementa |
| Diagnóstico | InDiagnosis → WaitingForApproval |
| Execução | InExecution → Finished/ServiceCompleted |
| Espera pela entrega | Finished → Delivered, separando ServiceCompleted de BudgetRejected |
| Serviço completo | Criação → Finished/ServiceCompleted; inclui esperas anteriores, exclui entrega/recusa |
| Falha de processamento | Uma falha técnica da ação HTTP de OS com resultado 500, sem contar negativas de negócio/cancelamento do cliente |
| Notificação | Tentativa concluída, incluindo consultas/montagem, por tipo, resultado e modo smtp/simulated |
| Falha de medição | Falha ao ler/calcular duração após operação persistida, sem alterar seu resultado |

O filtro MVC mede após retorno bem-sucedido do controller. Durações usam instantes UTC persistidos, não cronômetro da requisição. Intervalos ausentes/abertos/inválidos não geram zero artificial; duração real zero é válida. O leitor específico consulta uma vez por ação de duração, com prazo padrão de um segundo, incluindo abertura; falha descarta a amostra e registra diagnóstico fixo. Desabilitar o meter também desabilita essa leitura adicional.

SQL de leitura é referência por ocorrência/dia em America/Sao_Paulo. Métricas são agregadas por janelas de coleta, podendo atravessar meia-noite. Médias usam soma/quantidade de amostras entre réplicas; não média de médias nem soma de médias de etapas. Sem outbox, replay, deduplicação automática, backfill ou garantia de entrega única. Interrupção abrupta pode perder sinais já relativos a commit confirmado.

## Acessos às integrações

O contador mecanica.integrations.operations registra PostgreSQL/SSM/Secrets Manager com integration, operation, purpose e outcome restritos. Conexão, comando e begin/commit/rollback são acessos distintos; finalidade separa application/bootstrap/readiness/telemetry. Cada chamada lógica AWS é contada após as retentativas internas existentes do SDK, não uma vez por tentativa de transporte. Resposta AWS bem-sucedida com configuração incompleta não é falha de transporte.

Falhas técnicas são failure + timeout; canceled fica separado. O percentual do dashboard exclui cancelamentos do denominador: (failure + timeout)/(success + failure + timeout), nulo sem denominador. Comando SQL pode falhar e ser convertido depois em conflito HTTP; isso não implica indisponibilidade do banco ou 500. Não há SQL, Secret/ARN, documento ou mensagem original nas dimensões. Leitura de License Key pelos Collectors não faz parte desse contador.

## Coleta, segurança e ciclo de vida

API exporta HTTP/PostgreSQL, métricas técnicas/negócio/integrações e logs ao Collector EKS Contrib 0.161.0. Configuração está no manifesto; código identifica eventos/aplica segurança. Telemetria é opt-in local; overlay hom habilita e prd permanece false. Um provider de métricas nasce antes do bootstrap e é reutilizado pelo host, com encerramento limitado de 3.000 ms em falha inicial. Traces/logs preservam composição e console textual existentes.

Lambda compõe providers antes do runtime de autenticação e os reutiliza por handler; SAM habilita permanentemente OTel/X-Ray/New Relic. Layer Collector 0.22.0/Collector 0.151.0 local recebe OTLP loopback, com decouple e sem batch periódico adicional. A aplicação finaliza sinais antes do flush de até 500 ms compartilhados, preservando pelo menos 500 ms para resposta. Encerramento/freeze/filas não fornecem entrega garantida.

Somente W3C traceparent seleciona pai OTLP, descartando baggage/tracestate; demais requisições de aplicação são coletadas mesmo com pai não amostrado. Probes da API e suas operações SQL não geram spans, mas preservam respostas/métricas. Não há spans manuais de casos de uso. X-Ray nativo/polling permanece separado e tem amostragem/atraso próprios; união com OTLP foi retirada do escopo, não corrigida.

Mensagens fixas e tipo da exceção, sem stack trace/mensagem original. Sem SQL, payload, documento, tokens ou credenciais; logs OTLP de aplicação não são duplicados por coleta stdout/CloudWatch. Atributos/dimensões permitidos estão nos manifestos, sem identidade de negócio nas métricas. Segmentos nativos AWS não passam pela sanitização da aplicação.

Collector EKS coleta 23 métricas Kubernetes e 13 estatísticas CloudWatch Gateway/Lambda/RDS; kubeletstats cobre somente o nó do Collector. CloudWatch usa período/coleta de 60 s e atraso de dez minutos; gauges representam estatísticas do período, não contadores cumulativos. Logs Gateway retomam últimos cinco minutos por inicialização, sem persistência/deduplicação/backfill. Os dados originais são reduzidos aos campos técnicos; cobertura com lacunas permanece aceita.

## Dashboards, alertas e alcance da validação

Três dashboards como código: operação/API/Kubernetes, ordens de serviço e integrações/autenticação. Estado Terraform persistente e publicação newrelic-provision; não são destruídos com a base. User API Key administra recursos; License Key é exclusiva de ingestão pelos Collectors.

Cinco alertas, com condições hom/prd separadas: falha técnica de OS, falha de notificação de OS, CPU do nó acima de 80% da alocável por cinco minutos, working set acima de 85% da memória alocável por cinco minutos e API com menos de uma réplica disponível por dois minutos. Falhas OS/notificação disparam com soma ≥1 na janela de 300 s. Sem preenchimento de lacunas; dez minutos sem sinal encerram incidentes sem abrir por ausência, sem comprovar recuperação. Em hom/prd, falha de notificação é falha do fluxo simulado, não entrega SMTP real.

Recebimento de abertura/encerramento dos três cenários exercitados (processamento, notificação e disponibilidade) foi confirmado; disparos CPU/memória e carga foram dispensados. Não foram acrescentados alertas fora dos cinco aprovados. [Encerramento E5 e ressalvas](../../ENCERRAMENTO_E5_2026-10-21.md): logs Gateway parciais, cancelamento AWS inconclusivo, aviso de descarte da métrica nativa de threads e limites de filas/retentativas. Essas limitações não são próximos testes obrigatórios nem certificação de prd.

SQL/indicadores, integrações e continuidade W3C foram conferidos nas fontes da [E6](../../REVISAO_E6_2026-10-21.md). Ambiente removido depois da validação; [custos e persistências](../../ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md). Nenhuma nova coleta/teste/deploy ou alteração de produção nesta revisão documental.

Fundamentação: [ADR 003](../adrs/003-BANCO-E-HISTORICO.md), [ADR 005](../adrs/005-OBSERVABILIDADE.md) e [ADR 006](../adrs/006-NOTIFICACOES.md).
