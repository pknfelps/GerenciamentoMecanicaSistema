# Observabilidade de negócio — E5

A API observa criações e transições concluídas pelo fluxo HTTP. O filtro MVC executa depois do retorno do serviço e de suas transações; o middleware existente conta respostas 500 de ações de OS. Chamadas diretas aos serviços não são outro canal instrumentado. Notificações são medidas no handler existente, inclusive falhas nas consultas necessárias à notificação.

## Coleta e configuração

O meter `Mecanica.Api.Business` usa a identidade, provider e exportação OTLP existentes. `Telemetry__Enabled` continua false por padrão. `Metrics:EnabledMetrics:Mecanica.Api.Business=false` remove o filtro, o emissor e as consultas adicionais, mantendo o adaptador de notificações sem efeito. A geração oficial do OpenAPI não registra instrumentação.

A fonte é `deploy/kubernetes/base/observability.json`, compartilhada pelo ConfigMap e execução local/publicação:

| Configuração | Padrão |
|---|---|
| `Telemetry:BusinessMetrics:HistoryQueryTimeoutSeconds` | 1 segundo; aceita de 1 a 5 |
| `Telemetry:BusinessMetrics:DurationBoundariesSeconds` | 1, 5, 15, 30, 60, 300, 900, 1800, 3600, 14400, 28800, 86400, 604800 |
| `Telemetry:BusinessMetrics:Attributes` | operation, stage, completion.reason, notification.type, outcome, mode |
| `Metrics:EnabledMetrics:Mecanica.Api.Business` | true quando a telemetria global for ativada |

As views aplicam limites/dimensões antes da agregação. Nomes/unidades dos instrumentos e identificação dos acontecimentos são contratos do código. Não acrescentar dados pessoais, UUID, datas ou mensagens de erro às dimensões. A configuração é lida na inicialização, sem recarga automática.

| Instrumento | Tipo/unidade | Significado e dimensões |
|---|---|---|
| `mecanica.orders.created` | Counter / ordem | Criação confirmada; sem dimensões de negócio |
| `mecanica.orders.stage.duration` | Histogram / segundos | `stage=diagnosis/execution/delivery_wait`; na entrega, `completion.reason=service_completed/budget_rejected` |
| `mecanica.orders.service.duration` | Histogram / segundos | Criação até conclusão efetiva, sem esperar entrega |
| `mecanica.orders.processing.failures` | Counter / falha | Resposta 500; `operation` é o método reconhecido da ação MVC |
| `mecanica.orders.notifications` | Counter / tentativa concluída | `notification.type=budget_available/status_changed`, `outcome=success/failure`, `mode=smtp/simulated` |
| `mecanica.orders.telemetry.failures` | Counter / falha | Leitura/cálculo da medição falhou após operação persistida |

400/401/403/404/409/422 e cancelamento pelo cliente não são falha técnica de processamento. Falha de notificação não altera a OS nem decrementa seu volume. Sucesso simulado em hom/prd não significa entrega ao destinatário; SMTP funcional permanece local.

## Durações e impacto

Somente diagnóstico concluído, execução concluída e entrega consultam o histórico: no máximo uma leitura parametrizada por ação, em conexão própria descartável, após o commit. O prazo inclui abrir conexão e executar consulta, sem retries próprios. A leitura pode acrescentar até o prazo configurado à resposta. Erro, timeout ou histórico incompatível descartam a medição e geram aviso fixo com tipo do erro, mantendo o resultado HTTP persistido.

O cálculo valida sequência consecutiva desde 1, encadeamento, transições, motivos e instantes UTC finitos não decrescentes. Usa o intervalo da transição concluída mesmo que outra já tenha sido gravada. Não depende de a OS ainda existir em `orders`.

Diagnóstico: InDiagnosis → WaitingForApproval. Execução: InExecution → Finished/ServiceCompleted. Espera pela entrega: Finished → Delivered, separando conclusão de recusa. Total: Received → Finished/ServiceCompleted. Intervalos abertos/recusados não viram execução de duração zero. Instantes válidos iguais permitem zero. Médias usam `sum/count`, inclusive ao agregar réplicas; não calcular média de médias nem somar médias de etapas.

## Conferência SQL

Execute com conexão SQL autorizada e ferramentas padrão. São consultas de leitura documentadas, sem novos scripts operacionais. O histórico é a referência persistida, inclusive depois de excluir uma OS.

Volume de criações no dia escolhido:

```sql
WITH period AS (
    SELECT timestamp '2026-10-14 00:00:00' AT TIME ZONE 'America/Sao_Paulo' AS start_at,
           timestamp '2026-10-15 00:00:00' AT TIME ZONE 'America/Sao_Paulo' AS end_at
)
SELECT count(DISTINCT order_id) AS created_orders
FROM order_status_history, period
WHERE sequence = 1 AND previous_status IS NULL AND new_status = 'Received'
  AND reason IS NULL AND isfinite(occurred_at)
  AND occurred_at >= start_at AND occurred_at < end_at;
```

Quantidade, soma e média das permanências concluídas; ausência de amostras não produz zero artificial:

```sql
WITH period AS (
    SELECT timestamp '2026-10-14 00:00:00' AT TIME ZONE 'America/Sao_Paulo' AS start_at,
           timestamp '2026-10-15 00:00:00' AT TIME ZONE 'America/Sao_Paulo' AS end_at
), ordered AS (
    SELECT *, row_number() OVER w AS position,
           lag(new_status) OVER w AS preceding_status,
           lag(occurred_at) OVER w AS entered_at,
           lag(reason) OVER w AS preceding_reason,
           first_value(occurred_at) OVER w AS created_at
    FROM order_status_history
    WINDOW w AS (PARTITION BY order_id ORDER BY sequence)
), valid_orders AS (
    SELECT order_id FROM ordered GROUP BY order_id
    HAVING bool_and(coalesce(
        sequence = position AND isfinite(occurred_at)
        AND (position = 1 OR occurred_at >= entered_at)
        AND previous_status IS NOT DISTINCT FROM preceding_status
        AND reason IS NOT DISTINCT FROM CASE WHEN new_status = 'Finished'
            THEN CASE WHEN previous_status = 'InExecution' THEN 'ServiceCompleted' ELSE 'BudgetRejected' END END
        AND ((position = 1 AND previous_status IS NULL AND new_status = 'Received')
            OR (position > 1 AND (previous_status, new_status) IN (
                ('Received', 'InDiagnosis'), ('InDiagnosis', 'WaitingForApproval'),
                ('WaitingForApproval', 'WaitingForExecution'), ('WaitingForExecution', 'InExecution'),
                ('InExecution', 'Finished'), ('WaitingForApproval', 'Finished'), ('Finished', 'Delivered')))), false))
), samples AS (
    SELECT CASE new_status WHEN 'WaitingForApproval' THEN 'diagnosis'
               WHEN 'Finished' THEN 'execution' ELSE 'delivery_wait' END AS stage,
           CASE WHEN new_status = 'Delivered' THEN preceding_reason END AS completion_reason,
           occurred_at AS completed_at, extract(epoch FROM occurred_at - entered_at) AS seconds
    FROM ordered JOIN valid_orders USING (order_id)
    WHERE new_status IN ('WaitingForApproval', 'Delivered')
       OR (new_status = 'Finished' AND reason = 'ServiceCompleted')
    UNION ALL
    SELECT 'service_total', NULL, occurred_at, extract(epoch FROM occurred_at - created_at)
    FROM ordered JOIN valid_orders USING (order_id)
    WHERE new_status = 'Finished' AND reason = 'ServiceCompleted'
)
SELECT (completed_at AT TIME ZONE 'America/Sao_Paulo')::date AS local_day,
       stage, completion_reason, count(*) AS samples,
       sum(seconds) AS sum_seconds, avg(seconds) AS average_seconds
FROM samples, period
WHERE completed_at >= start_at AND completed_at < end_at
GROUP BY local_day, stage, completion_reason
ORDER BY local_day, stage, completion_reason;
```

Edite as datas do período. Instantes UTC são agrupados em America/Sao_Paulo com início inclusivo e fim exclusivo. Destruir o banco encerra a disponibilidade desse histórico.

## Entrega e limites

A emissão ocorre por ação confirmada, sem replay na inicialização. Nova requisição de criação que gere outra OS representa outra criação; não há idempotência HTTP nova. Métricas independem da amostragem de traces. Transição inválida ou concorrente rejeitada não gera duração concluída.

Não há entrega exatamente uma vez, fila persistente ou recuperação automática. Encerramento abrupto, filas cheias, falhas de leitura e Collector indisponível podem causar perdas. A conferência SQL identifica divergências, mas não repõe dados. Janelas de agregação/exportação OTLP podem atravessar meia-noite; o SQL determina a data exata do acontecimento, sem incluir datas nas dimensões. Falhas/notificações não são reconstruídas pelo histórico de estados.

Use os comandos de [observabilidade local](OBSERVABILIDADE_LOCAL.md), gere fluxos de OS pelo Postman e acompanhe `docker compose -f compose.observability.yaml logs -f collector`. Espere o intervalo de métricas ou finalize a API normalmente para permitir flush. Pare somente o Collector para conferir continuidade das operações. Não é necessário New Relic ou AWS.

Dashboards/alertas, ingestão New Relic, contadores específicos de integrações e validação AWS permanecem pendentes. Esta instrumentação não conclui integralmente E5.5–E5.8/E5.11.

## Validação local — 14/10/2026

Regressão completa: **1.194 testes aprovados, zero falhas/ignorados** (48 contratos, 616 controllers, 183 domínio, 152 infraestrutura e 195 serviços). Inclui 52 casos novos, além das asserções ampliadas em cenários existentes. PostgreSQL real comprovou leitura/cancelamento/descarte, fluxos aprovado/recusado, concorrência, rollback, exclusão e retomada após reiniciar a API sem replay.

A prova OTLP foi repetida após ampliar o cenário para ler histórico PostgreSQL e enviar os dois histogramas de negócio, contadores e sinais técnicos ao Collector Contrib 0.161.0. Dados sensíveis não chegaram ao exporter; criação/readiness continuaram com Collector parado. As duas consultas deste documento foram executadas em PostgreSQL 16.15 com dados fictícios e fronteiras do dia: volume 2, diagnóstico 2 amostras/240 s, execução 1/300 s, entrega separada por motivo 1/120 s cada e total 1/600 s.

Restore/build Release, duas gerações oficiais OpenAPI idênticas ao contrato anterior, publish com certificado RDS e configuração idênticos à origem, ausência de exporter de teste no publish e renderizações Kustomize base/hom/prd aprovados. Evidências em `artifacts/e5-business/validation.json`, logs/TRX e `IMPLEMENTACAO.md`. A regressão completa e a repetição específica do Collector são registradas separadamente, sem somar duas vezes o mesmo teste.

Avisos NU1902/NU1903 e S6667 continuam visíveis, sem supressões. Os pontos novos de catch seguem a política vigente de mensagem fixa/tipo, sem passar o objeto Exception ao logger. Não houve login, operação AWS, deploy ou alteração de esquema/permissões/workflows. A validação local não comprova ingestão New Relic ou conexão real com o RDS privado.
