# E5 — Falhas controladas e alertas em hom — 20/10/2026

**Dois ajustes de logging implementados e validados localmente:** [relatório](AJUSTES_LOGS_API_2026-10-20.md). Main retorna código 1 com mensagem fixa/tipo na falha, sem impressão automática da stack; descarte/exportação das métricas preservados. Cancelamento por RequestAborted gera Information, sem Problem Details e com status técnico 499 antes da resposta começar; resposta iniciada preservada e cancelamento interno continua 500. 1.197 testes completos aprovados, zero falhas/ignorados; Docker comprovou cancelamento e falha real SSM em loopback, com saída 1/zero frames e métrica failure=1. OpenAPI idêntico, publish/certificado/Kustomize conferidos; recursos temporários removidos. Nada foi implantado: publicar API e reconferir somente a nova revisão. Não alterar/checkar como concluída a comprovação AWS de E5.2 ainda.

**Resultado:** falhas técnicas de OS, falhas de notificação e ausência de réplicas abriram e encerraram incidentes. O mantenedor confirmou os seis e-mails correspondentes. A API continuou operando sem o Collector e retomou a exportação depois da restauração. Foram comprovados novos resultados de falha nas integrações, mas foi encontrado **stack trace no console em falhas de inicialização da API**. E5 permanece parcial.

Complemento autorizado à [validação funcional, SQL e dos sinais](VALIDACAO_E5_HOM_2026-10-20.md). Não houve mudança de código, workflows, IAM, conteúdo de Secrets, regras de alerta ou limiares. As interrupções temporárias abaixo foram restauradas; não houve deploy, publicação de versão Lambda ou destroy.

## Escopo e evidências

- Conta AWS `121754142617`, região `us-east-1`, ambiente hom; New Relic US `8626152`.
- Gateway `https://v5qvpym6i7.execute-api.us-east-1.amazonaws.com/hom`.
- API `a8ba2615d9fe918fc161d975fabd406b54fd0ff2`; autenticação publicada v10, revisão `0446a19b4888921f1d9b4032d034de7d4009ab24`.
- Janela geral: **17:58–18:36 UTC**, ou **14:58–15:36 em São Paulo**. Instantes de locks e eventos New Relic usam os respectivos servidores. O relógio do cliente estava alguns segundos atrás; não calcular latências entre fontes por essa diferença.
- Evidências técnicas locais, ignoradas pelo Git: `artifacts/e5-validacao-integrada/20261020-controlled-failures/`, com `http-results.json`, `validation-summary.json`, `bootstrap-ssm.json`, `bootstrap-secretsmanager.json` e `hpa-spec-before.json`. Não contêm senhas, JWTs ou valores de Secrets.

## Resultado por cenário

| Cenário | Resultado | Limite |
|---|---|---|
| F01 / A01 — falha técnica de OS | aprovado | HTTP 500, rollback conferido por SQL, métrica, incidente e dois e-mails |
| F02 / A02 — falha de notificação | aprovado | Transição 204 persistida; notificação falha, incidente e dois e-mails; modo simulado |
| A03 — CPU elevada | disparo não executado | Medição/configuração conferidas; não houve violação sustentada nem carga |
| A04 — memória elevada | disparo não executado | Medição/configuração conferidas; não houve violação sustentada nem pressão de memória |
| F03 / A05 — API sem réplicas | aprovado | Zero observado, Collector ativo, Lambda 200, recuperação e dois e-mails |
| F04 — restart/lookback | aprovado no evento selecionado | Releitura de cinco minutos e duplicação com mesmo ID original; não comprova cobertura completa |
| F05 — Collector indisponível | aprovado no escopo | Criação/transição/leitura preservadas e exportação posterior retomada; houve ausência dos três traces da interrupção na consulta |
| F06 — bootstrap AWS | parcial, com defeito encontrado | Métricas de falha API SSM/Secrets e Lambda SSM; stack trace no console da API; falha Secrets da Lambda não executada |

## Falha técnica de OS e rollback

Foi criada a OS fictícia `e586788e-5e70-4091-9a53-6cc1ebc80738`, com um serviço e sem materiais. SQL inicial: estado Received, um evento, estoque 9 e reservas 0. O pod temporário de acesso utilizou `mecanica_api`, TLS `verify-full` e a conexão autorizada.

A sessão controladora adquiriu lock exclusivo em `order_status_history`, localizou somente o comando da API bloqueado por ela e executou `pg_cancel_backend` nesse único backend. A seleção exigiu tabela, usuário, IP de pod da API e relação de bloqueio. Havia limites de espera, statement timeout e idle-in-transaction timeout; o lock foi liberado por ROLLBACK. Não foram alteradas permissões PostgreSQL.

| Evento | UTC do servidor |
|---|---|
| Aquisição do lock | 17:59:51,509653 |
| Cancelamento do comando | 17:59:55,496048 |
| Liberação do lock | 17:59:55,497485 |

`PATCH /Orders/{id}/diagnosis/start` retornou **500**, request ID `dc8b4651-2e10-4497-82d4-c7695c602d50`, trace `6ad7ac1b373214613a8535bb44837f42`. SQL posterior confirmou Received, somente o evento inicial, nenhum material e estoque/reservas inalterados. Nenhuma sessão permaneceu bloqueada pelo controlador.

O New Relic recebeu **um** incremento de `mecanica.orders.processing.failures`, operação StartDiagnosis, e a falha do comando PostgreSQL. Isso comprova o alerta específico da ação de OS; difere do teste anterior de GET Catalog.

## Falha de notificação após commit

Na mesma OS, o lock foi aplicado em `customers`, consultada pelo handler de notificação depois do commit da transição. Somente seu comando bloqueado foi cancelado. Lock às 18:01:01,753519 UTC; cancelamento às 18:01:05,515831; liberação às 18:01:05,517271.

A ação retornou **204**, request `9b5d992c-71c1-4aac-883f-0a7121538d61`, trace `6ad7ac615e7f969024f8c01c02b9c533`. SQL confirmou InDiagnosis e dois eventos encadeados. A operação persistida não foi desfeita pela falha tratada da notificação.

Na janela 17:57–18:04 UTC, `mecanica.orders.notifications` registrou:

| Tipo | Resultado / modo | Quantidade |
|---|---|---:|
| status_changed | success / simulated | 1 |
| status_changed | failure / simulated | 1 |
| budget_available | success / simulated | 1 |

A conclusão posterior do diagnóstico retornou 204 e deixou WaitingForApproval, com três eventos. É uma nova notificação bem-sucedida; a anterior não foi reenviada. A OS foi removida ao concluir o teste: SQL confirmou ausência do cadastro e preservação dos três eventos, estoque 9 e reservas 0. A simulação não constitui envio SMTP nem entrega ao cliente.

## Ciclo dos três alertas exercitados

| Alerta | Policy / condição hom | Incidente | Evento de abertura UTC | Encerramento |
|---|---|---|---|---|
| Falha técnica de OS | 8216641 / 68982961 | `cd342817-ebf5-489c-9994-eb22e794e281` | 18:01:43,533 | LOSS_OF_SIGNAL; closeTime 18:10:07,846, evento 18:11:08,819 |
| Falha de notificação | 8216644 / 68982967 | `f4d65ed9-a11d-49fa-97d5-fc6dacd2eac5` | 18:03:11,050 | LOSS_OF_SIGNAL; closeTime 18:11:37,173, evento 18:12:23,998 |
| API sem réplica | 8216645 / 68982964 | `53c40345-3c65-4022-8a72-3dd7ae2f5455` | 18:18:09,315 | EVALUATOR; bucket closeTime 18:18:00, evento 18:22:02,774 |

Os horários de evento do New Relic não devem ser confundidos com o início do bucket de avaliação. Os dois primeiros encerraram por ausência de sinal, que **não comprova recuperação**. O terceiro encerrou pela avaliação da disponibilidade restaurada. O mantenedor confirmou abertura e encerramento dos três, totalizando seis e-mails. Não houve fechamento manual, teste de e-mail, alteração de workflow New Relic ou de silenciamento.

## PostgreSQL da Lambda: timeout e recuperação

Um lock temporário em `customers` manteve bloqueada a leitura da autenticação até o timeout nativo do comando. Não foi cancelado backend do usuário da Lambda pela sessão `mecanica_api`. O lock foi adquirido às 18:10:33,404736 UTC; o backend alvo foi observado às 18:10:37,066849 e o ROLLBACK ocorreu às 18:10:43,599267.

A chamada pelo Gateway/v10 retornou **500 em 5,631 s**, request `3f7147b1-e1a5-47a3-96a2-49a85976846e`, trace `6ad7ae9c518142746ee4abd02a0ec634`. A chamada seguinte retornou **200 em 0,618 s**. O New Relic recebeu **um `postgresql/command.execute/application/timeout`**; na janela 18:07–18:12 também constam cinco aberturas e quatro comandos bem-sucedidos. Duas tentativas preliminares terminaram com 200 e foram preservadas nas evidências, sem serem apresentadas como falha.

Timeout do driver não equivale ao prazo global do handler nem comprova `canceled` pelo chamador. Uma consulta sem elegibilidade continua sendo sucesso PostgreSQL. Não foram exercitadas falhas de abertura ou das operações transacionais.

Nos três traces de falha selecionados — processamento, notificação e timeout da Lambda — foram encontrados 14 spans e quatro logs OTLP correlacionados. A inspeção não encontrou os atributos consultados de SQL, string de conexão, URL completa ou stack trace. Essa conclusão é restrita à amostra e não inclui console de bootstrap ou segmentos nativos AWS.

## Bootstrap SSM e Secrets Manager

Na API, foram executados processos adicionais do mesmo binário dentro de um pod existente, com override de endpoint do SDK restrito ao processo: SSM e Secrets Manager, separadamente, para `http://127.0.0.1:9`. O serviço que atendia tráfego não recebeu o override. Cada processo tinha limite externo de 55 s, core dump desabilitado e os limites internos existentes. Um pod isolado tentado antes ficou Pending por insuficiência de memória/limite de pods e foi removido sem executar; não gerou medição.

O New Relic recebeu, na janela 18:04–18:08 UTC:

- SSM / get_parameters / bootstrap / failure: **1**.
- Secrets Manager / get_secret_value / bootstrap / failure: **1**.
- SSM / get_parameters / bootstrap / success: **1**, anterior à falha de Secrets.

Isso comprova a tentativa final de exportação de métricas na inicialização malsucedida, com o Collector disponível.

**Defeito encontrado:** ambos os processos terminaram com código 134 e exceção não tratada. A mensagem fixa esperada apareceu, porém o console imprimiu **cinco frames de stack trace em cada caso**. A falha ocorre antes do middleware HTTP; a política de ausência de stack trace não está atendida nesse caminho. Os resumos locais registram apenas resultado, mensagem fixa presente e número de frames. Nenhuma correção foi aplicada nesta validação.

Na Lambda, o Gateway foi conferido apontando para a versão numérica 10. Somente `$LATEST` recebeu temporariamente o override SSM; uma invocação direta com documento inválido retornou **500**. A restauração usou RevisionId atual, aguardou LastUpdateStatus=Successful e confirmou igualdade do ambiente original. Nova chamada de `$LATEST`, sem override, retornou **400** por documento inválido, demonstrando bootstrap recuperado. Não houve versão nova ou alteração de alias/Gateway.

Na janela 18:25–18:35 UTC, a autenticação exportou **SSM failure=1, SSM success=1 e Secrets success=2**, todos bootstrap. Marcadores técnicos: `e5-bootstrap-ssm-36ecf628-b1f6-4988-ab8d-a4e21fdd83d6` e `e5-bootstrap-recovery-77f57f0d-912e-4daa-b327-b1dd5e555598`.

A falha de Secrets Manager da Lambda **não foi executada**: o override também afetaria o provider AWS da extensão que lê a License Key, comprometendo o Collector necessário à observação isolada. Não foram alterados IAM, Secret ou configuração persistente para contornar esse limite. Resultados `canceled` também permanecem sem comprovação implantada.

## API sem réplicas e independência da autenticação

O Deployment foi escalado de duas para zero réplicas às 18:13:26 UTC do cliente, mantendo o Collector ativo. A especificação do HPA foi preservada e comparada antes/depois, sem diferenças. Com replicas=0 e minReplicas positivo, o próprio HPA suspendeu a escala até a restauração manual para valor positivo.

Com zero pods da API, a autenticação pelo Gateway retornou **200**; GET Catalog retornou **500 do Gateway**. Esse erro externo não foi usado como métrica de processamento de OS. O New Relic mediu réplicas desejadas/disponíveis em zero e abriu A05.

A restauração para duas réplicas iniciou às 18:17:58 e ambas ficaram prontas por volta de 18:18:10, horários do cliente. Um JWT anterior havia expirado e retornou 401; depois de novo login autorizado, GET Catalog retornou 200 e as três probes ficaram Healthy. Posteriormente o HPA reduziu autonomamente para **uma réplica**; isso foi mantido, sem forçar a escala anterior.

CPU/memória foram consultadas por nó em janelas de um minuto, 17:57–18:13 UTC. Máximos observados sobre recursos alocáveis: **CPU 4,186065%** e **working set 76,984133%**, abaixo de 80%/85%. A03/A04 não tiveram disparo ou recebimento comprovados. Não houve carga, mudança de denominadores ou redução de limiares.

## Collector: releitura e indisponibilidade

### Restart e janela de cinco minutos

Antes do restart, o request Gateway `84597261-4c34-42fc-856d-37182a48114a` tinha um registro no New Relic e um ID original no CloudWatch: `39974539634069053776950261870704236716899231604336033792`, timestamp **18:19:12,042 UTC**, stream `7bf2cdddca28466ef0fbff3290435af4`.

O Collector foi reiniciado às 18:22:19 UTC do cliente. Depois, o New Relic mostrou **dois registros com o mesmo ID original**: `count=2`, `uniqueCount(id)=1`. A releitura e a duplicação previstas ficaram comprovadas nesse evento. Não foi acrescentado checkpoint ou deduplicação. Isso não resolve nem explica os dois logs ausentes da amostra funcional anterior de 43/45.

### Interrupção curta da exportação

O Collector ficou com zero réplicas entre **18:31:06,871 e 18:31:59,458 UTC do cliente**, com restauração em bloco finally e rollout conferido. Durante a interrupção, uma segunda OS fictícia, sem materiais, foi criada com 201, iniciou diagnóstico com 204 e foi consultada com 200/InDiagnosis. A API permaneceu pronta e sem reinícios.

Após a recuperação, a conclusão do diagnóstico retornou **204**, request `58149db9-9bac-4948-984c-2774161a2e6b`. O New Relic recebeu **11 spans e dois logs** do trace `6ad7b3d866a378eb7ab2ce5b245f5ae1`, na consulta 18:30–18:36 UTC. Os três traces das operações executadas durante a interrupção não apareceram nessa consulta; não se presume replay nem entrega completa das métricas/logs daquele intervalo.

A OS temporária `65670fb6-1485-4d8f-a39c-e3b37b0c1c8c` foi consultada em WaitingForApproval e removida com 204. As três probes retornaram 200/Healthy. O teste interrompeu API → Collector; não simulou respostas New Relic 401/429/5xx ou indisponibilidade prolongada.

## Encerramento e pendências

Conferência final: API **1/1 Ready**, HPA current=desired=1, Collector **1/1 Ready**, ambos com zero reinícios nos pods atuais. Não restaram os pods temporários database-access/e5-bootstrap-ssm, locks ou sessões SQL da validação. O cliente HTTP com JWT em memória e o port-forward foram encerrados. A configuração da Lambda foi restaurada; a integração do Gateway permanece na versão 10.

E5.5 está comprovada em hom, incluindo recebimento. E5.8 avançou com falhas PostgreSQL, SSM e Secrets nos escopos descritos, mas permanece parcial. Antes de outra comprovação do bootstrap da API, corrigir o stack trace automático encontrado. Permanecem separados: falha Secrets da Lambda, cancelamento e demais acessos não exercitados, disparos CPU/memória, congelamento real da Lambda, consumo/retenção New Relic, cobertura dos logs Gateway, união automática X-Ray/OTLP e operação em prd. Nenhum desses itens foi declarado concluído por ausência de dados.

**Decisão posterior do mantenedor:** o ajuste do log de startup será feito após finalizar as validações; não impede continuar as demais conferências. Não serão executados testes de disparo de CPU/memória nem teste de carga na API. A03/A04 permanecem sem comprovação de disparo, com esse escopo explicitamente dispensado pelo mantenedor, preservando condições e limiares publicados.

## Complemento — consumo, retenção e limites New Relic

Conferência somente leitura na sessão autenticada de **20/10/2026**, conta `8626152`. Não houve chamadas de negócio, mudanças no plano, retenção, orçamento de cardinalidade ou limites, nem alterações AWS. Evidência técnica local: `newrelic-consumption-retention.json` no diretório desta rodada.

### Consumo e franquia

O [Plan Summary](https://one.newrelic.com/admin-portal/plan-management/plan-summary?account=8626152) exibiu **Free: 0.089044843 / 100 GB ingested**, aproximadamente **0,089% da franquia**. O [Data Ingestion](https://one.newrelic.com/admin-portal/data-ingestion/home?account=8626152), filtrado para a conta do projeto, mostrou outubro acumulado **0,09 GB** e projeção de fim de mês **0,16 GB**. A tabela na janela móvel de um mês mostrou total arredondado **0,10 GB**: MetricsBytes 0,10 GB, TracingBytes e LoggingBytes individualmente inferiores a 0,01 GB. São períodos/arredondamentos distintos; não constituem divergência.

A projeção usa o histórico disponível de um ambiente intermitente; não estima custo ou volume sob operação contínua/carga. O snapshot não acrescenta bloqueio automático ao atingir a franquia nem garante consumo futuro. Não foi contratado plano ou add-on.

### Retenção

Na [tabela de Data Retention](https://one.newrelic.com/admin-portal/limits-data-retention/data-retention?account=8626152), escopo da conta:

| Fonte | Retenção efetiva exibida | Extensão |
|---|---|---|
| Logging | **30 dias** | 0 dias |
| Distributed traces | **8 dias** | 0 dias |
| Distributed tracing error info | **8 dias** | 0 dias |

Essas configurações da interface não se aplicam às métricas dimensionais. Para os sinais `Metric` utilizados pelos dashboards, a [documentação oficial](https://docs.newrelic.com/docs/data-apis/manage-data/manage-data-retention/) estabelece **30 dias de pontos brutos** e **13 meses de agregados**, a partir de um minuto; esses prazos não são editáveis. Essa parte é conferência documental, não teste de expiração aguardando os respectivos períodos.

A retenção New Relic é independente dos sete dias do grupo CloudWatch e não é garantia de recuperação do que não chegou ao destino. As janelas históricas devem respeitar o prazo de cada tipo de sinal; métricas agregadas podem permanecer consultáveis depois de os logs/spans correspondentes expirarem.

### Limites e cardinalidade

Na [página Limits](https://one.newrelic.com/admin-portal/limits-data-retention/data-limits?account=8626152), janela de um dia, as oito categorias exibidas tinham **zero violações** e indicador de utilização **inferior a 1%**: DPM Lambda/X-Ray, requisições Metric API, requisições Trace API, DPM Trace API, requisições Log API, bytes de logs, pontos correspondidos por consultas de alerta e operações de varredura das consultas de alerta. A própria interface informa que, em janelas superiores a seis horas, o campo Max limit usage representa uma média; não foi apresentado como pico exato.

Na [Cardinality Management](https://one.newrelic.com/admin-portal/cardinality-management/home?account=8626152), a amostra visível mostrou orçamento de 100.000 por métrica, com 4 para desired/available do Deployment, 3 para orders.created, 2 para as métricas de nó exibidas e 1 para orders.processing.failures/service.duration. Uso individual inferior a 0,01% nessa amostra. Não é auditoria de todas as séries/dimensões, nem prova sob carga ou saturação.

**Resultado:** consumo, franquia, retenção e indicadores de limites conferidos no escopo disponível. A correção do console de bootstrap permanece anotada para depois de finalizar as demais validações. Não reabrir testes de disparo CPU/memória ou de carga, dispensados pelo mantenedor.
