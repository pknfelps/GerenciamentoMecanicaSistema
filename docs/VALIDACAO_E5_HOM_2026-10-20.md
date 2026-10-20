# Validação integrada E5 — hom — 20/10/2026

**Logs Gateway reconferidos e lacuna diagnosticada:** [relatório](VALIDACAO_LOGS_GATEWAY_2026-10-20.md). Janela hom 13:10–13:19 UTC continua com 45 eventos no CloudWatch e 43 no New Relic, sem duplicação/request ID inesperado e com campos técnicos/sanitização conferidos. CloudTrail mostra consulta do Collector às 13:17:37 até 13:17:37.421; os dois eventos de 13:17:20–21 só foram ingeridos às 13:17:41. A consulta seguinte já começa em 13:17:37.421, deixando os eventos tardios atrás do cursor. Causa sustentada pelos horários/código, sem corpo da resposta CloudTrail disponível. Não é recuperação dos registros nem explicação universal de lacunas antigas. Sem restart/replay/tráfego ou configuração alterada; E5.14 mantém cobertura parcial e união X-Ray/OTLP separada.

**União X-Ray/OTLP verificada na interface New Relic:** [relatório](VALIDACAO_UNIAO_XRAY_OTLP_2026-10-20.md). Critério de árvore única não atendido nos dois caminhos selecionados de hom: API e Lambda aparecem como Fragmented/Missing parent. Consulta histórica 13:10–13:19 UTC encontrou seis spans OTLP e nove nativos, sob quatro trace.id; pais/componentes coincidem, mas campos integrais de vínculo diferem. PostgreSQL permanece ligado corretamente à aplicação. Propagação está comprovada; união automática exige tratamento próprio, não novo tráfego/espera de polling. Nenhuma configuração, código ou recurso alterado; aba temporária fechada. E5.3/E5.14 continuam parciais, agora com falha do critério evidenciada.

**Resultado:** a correção dos exporters está comprovada na AWS. API e autenticação exportaram traces, métricas e logs ao New Relic; os fluxos HTTP e os indicadores de OS conferem, incluindo a reconciliação SQL direta concluída posteriormente nesta sessão. A coleta de logs do Gateway permanece parcial: **43/45 request IDs**, sem duplicação na amostra funcional original.

**Complemento posterior, 17:58–18:36 UTC:** a [conferência de falhas e alertas](VALIDACAO_FALHAS_E5_HOM_2026-10-20.md) comprovou A01/A02/A05, incluindo abertura/encerramento e seis e-mails recebidos, falhas de integrações, rollback, independência da Lambda e continuidade da API sem Collector. Restart comprovou releitura/duplicação de um evento; exportação posterior retomada. Foi encontrado stack trace no console em falhas de bootstrap da API. CPU/memória não tiveram disparo; demais limites estão no complemento. As seções abaixo preservam o resultado de cada janela anterior, não o estado consolidado posterior.

## Ambiente e janela

| Item | Revisão / identificação |
|---|---|
| AWS / New Relic | `121754142617/us-east-1` / conta US `8626152` |
| Gateway | `v5qvpym6i7`, stage `hom`, tracing habilitado |
| Endpoint desta rodada | `https://v5qvpym6i7.execute-api.us-east-1.amazonaws.com/hom` |
| API | `a8ba2615d9fe918fc161d975fabd406b54fd0ff2` |
| Imagem | `sha256:50ec93314db96c99a4014b07ac1a8860ef0174fe65940c8fdec44e30954dd9df` |
| Autenticação | versão numérica **10**, revisão `0446a19b4888921f1d9b4032d034de7d4009ab24`, tracing Active |
| Collector EKS | Contrib `0.161.0`, uma réplica, sem reinícios no snapshot |
| RDS | PostgreSQL `16.15`, available |

Janela funcional e de sinais: **20/10/2026, 13:10–13:19 UTC**, correspondente a **10:10–10:19 em São Paulo**. Bootstrap: **12:40–13:19 UTC**, incluindo a criação dos dois processos da API. Comparações CloudWatch usam os minutos específicos abaixo; não somam versões ou tráfego da rodada anterior.

Recursos, revisões, chamadas e parte das consultas AWS foram conferidos antes da expiração do perfil administrativo. A conclusão das consultas aos consoles ocorreu pelas sessões autenticadas AWS e New Relic. Não foi lida chave de ingestão. Na conferência SQL posterior, a credencial fornecida pelo mantenedor foi utilizada somente na autenticação psql, fora dos comandos/arquivos/resultados. Tentativas de renovar o CLI administrativo foram encerradas sem concluir o login; o acesso Kubernetes existente permitiu a conferência SQL sem depender dessa renovação.

## Saúde e regressão funcional

- **45/45 chamadas ao Gateway** com os códigos esperados: 14 × 200, 3 × 201, 11 × 204, 4 × 400, 5 × 401, 5 × 403, 1 × 404 e 2 × 422. Nenhum 500.
- Logins internos Admin/Mechanic e validação de cliente elegível; negativas de documento/corpo, elegibilidade, login inexistente, ausência de token, perfil e filtro de outra conta preservadas.
- OS aprovada percorreu diagnóstico, orçamento, execução e entrega. Outra foi recusada e entregue; uma terceira foi criada e excluída, com consulta posterior 404. Não foram excluídos cadastros anteriores.
- Transição prematura e documento divergente retornaram 422. Estoque começou com 10 unidades/zero reservas e terminou com 9/zero reservas: somente o serviço aprovado consumiu uma unidade.
- As três probes internas retornaram 200/Healthy. `/health/live` pelo Gateway retornou o 403 previsto para uma rota não exposta.
- API e Collector estavam Running/Ready, sem reinícios. No baseline: API 2m CPU/84 MiB; Collector 2m/45 MiB; nó 48m/900 MiB pelo Metrics Server. São snapshots, não prova de capacidade sob carga.
- O HPA escalou automaticamente de uma para duas réplicas às **13:13:25 UTC**, por utilização de CPU relativa ao request. Ambos os pods ficaram prontos. Nenhuma escala ou configuração foi aplicada pelo assistente.

A distribuição `http.server.request.duration`, excluindo as probes, registrou **38 requisições da API**. As demais foram seis invocações da autenticação e a negativa de rota no Gateway. Média HTTP 47,043 ms e p95 aproximado 216,797 ms na janela. A comparação precisou incluir o início do bucket OTLP, às 13:10:00; uma consulta iniciada às 13:10:55 cortava amostras do primeiro intervalo. Isso não foi registrado como perda de requisições.

## Integrações e ciclo de vida

Totais da API entre **12:40 e 13:19 UTC**, exclusivamente na revisão acima:

| Integração / operação | Finalidade | Resultado | Quantidade |
|---|---|---|---:|
| SSM / get_parameters | bootstrap | success | 2 |
| Secrets Manager / get_secret_value | bootstrap | success | 4 |
| PostgreSQL / connection.open | application | success | 34 |
| PostgreSQL / command.execute | application | success | 157 |
| PostgreSQL / transaction.begin | application | success | 16 |
| PostgreSQL / transaction.commit | application | success | 14 |
| PostgreSQL / transaction.rollback | application | success | 2 |
| PostgreSQL / connection.open | telemetry | success | 5 |
| PostgreSQL / command.execute | telemetry | success | 5 |
| PostgreSQL / connection.open | readiness | success | 622 |

O agrupamento por pod confirmou **uma chamada SSM e duas Secrets por processo**, incluindo o segundo pod criado pelo HPA. Não houve repetição de bootstrap por requisição. A API registrou 223 acessos PostgreSQL de aplicação; os dez acessos de histórico e as probes ficaram separados dos painéis principais.

Na Lambda: **uma SSM, duas Secrets, três aberturas e três comandos PostgreSQL**, todos success. Consultas sem elegibilidade não viraram falhas PostgreSQL. Seis invocações concluídas: duas authenticated, duas invalid-request, uma invalid-document e uma not-eligible; seis amostras de duração. Não houve resultado técnico failure/timeout/canceled nessa amostra, portanto sua emissão implantada continua sem teste de falha controlada.

Os dois rollbacks da API correspondem às negativas de negócio esperadas. O acesso rollback terminou com sucesso; não representa falha técnica de transporte nem HTTP 500. Os logs existentes de transação preservam seu nível Error mesmo nesses casos, com mensagem fixa e tipo da exceção.

Os quatro painéis novos do dashboard **Integrações e autenticação** foram conferidos na interface: última amostra por serviço/integração, totais, falhas/timeouts e percentual. Na janela de bootstrap mais requisições, mostraram API PostgreSQL=223/Secrets=4/SSM=2 e auth PostgreSQL=6/Secrets=2/SSM=1. Falhas não tinham séries e os percentuais foram zero para grupos com denominador positivo; isso não comprova funcionamento dos caminhos de falha.

## OS e notificações

| Indicador | Amostras / quantidade | Soma em segundos |
|---|---:|---:|
| Criações | 3 | — |
| Diagnóstico | 2 | 0,844289 |
| Execução | 1 | 0,418936 |
| Espera por entrega / service_completed | 1 | 0,442770 |
| Espera por entrega / budget_rejected | 1 | 0,425929 |
| Serviço completo | 1 | 5,016595 |
| Notificações / status_changed | 9 | — |
| Notificações / budget_available | 2 | — |

As cinco amostras de etapas somam **2,131924 s**. As onze notificações foram `success/simulated`, conferindo com onze mensagens fixas de simulação no log da API. Não houve envio SMTP ou afirmação de entrega ao cliente. Processamento e medição de OS não apresentaram incremento de falhas na consulta desta janela.

O dashboard de ordens exibiu três criações e as médias/amostras em minutos, separadas pelos motivos. O tempo de serviço também confere com `duration` devolvido pela API. A reconciliação SQL abaixo confirmou independentemente os instantes persistidos e a preservação do histórico após exclusão.

### Conferência SQL direta — 14:07:57 UTC / 11:07:57 São Paulo

Após autorização específica, foi utilizado o manifesto existente de `database-access`, no namespace `database-init`, com usuário restrito `mecanica_api`, TLS `verify-full` e certificado RDS. O resolvedor seguro não conseguiu resolver a referência; a credencial fornecida pelo mantenedor foi utilizada no prompt sem eco do psql, sem senha em argumentos, arquivos ou resultados. Não foi recuperado valor do Secrets Manager pelo agente.

A sessão comprovou **TLS 1.3** e `transaction_read_only=on`. Os SELECTs foram executados numa única transação READ ONLY, com statement timeout de 15 s, lock timeout de 3 s e ROLLBACK ao final. O resultado contém somente identificadores técnicos, estados, sequência, motivos, instantes e quantidades de estoque, sem documentos, nomes ou placas.

Para os três UUIDs desta validação:

- **Aprovada:** sete eventos, Received → InDiagnosis → WaitingForApproval → WaitingForExecution → InExecution → Finished/ServiceCompleted → Delivered. Cadastro atual Delivered.
- **Recusada:** cinco eventos, Received → InDiagnosis → WaitingForApproval → Finished/BudgetRejected → Delivered. Cadastro atual Delivered.
- **Excluída:** linha ausente de orders e order_materials; evento Received preservado em order_status_history.
- **Treze eventos no total**, com sequência contínua, estados encadeados, motivos válidos e instantes finitos/não decrescentes. Sem eventos extras relativos às negativas exercitadas. Estoque final 9, reserved_amount=0, coerente com o baseline e o único serviço aprovado.
- **Três criações** dos UUIDs no intervalo 13:10–13:19 UTC. As cinco combinações de etapa/motivo da tabela anterior coincidiram com SQL em quantidade e soma das durações; serviço completo também coincidiu em 5,016595 s. O dia local das amostras foi 20/10/2026 em America/Sao_Paulo.

Asserções sobre TLS/read-only, existência/ausência dos cadastros, estados/sequências/motivos, preservação do histórico, estoque e durações passaram. O escopo é a massa de três OSs desta rodada e sua janela; não constitui auditoria de todos os cadastros nem reconstrução de um snapshot imediatamente anterior a cada negativa HTTP.

Resultados locais: `sql-snapshot.json` e `sql-reconciliation.json` no diretório de evidências desta rodada. A comparação utilizou decimais, com tolerância inferior a um nanossegundo; não houve divergência entre os valores persistidos e recebidos. A conexão foi encerrada e o pod temporário removido, com ausência confirmada. Nenhum deploy, alteração de configuração/IAM, concessão de acesso PostgreSQL ou escrita de dados foi executado.

## Traces, logs e propagação

- **203 spans OTLP da API e 12 da Lambda**, com 215 IDs distintos no total. Nenhum dos atributos inspecionados de SQL, URL/query, stack trace, baggage/tracestate ou span de health/probe apareceu nessa amostra.
- **19 logs OTLP da API e seis da Lambda**, todos com TraceId/SpanId. Os campos/marcadores inspecionados não continham documento fictício, senha de teste, SQL ou stack trace. Isso se limita aos registros e atributos conferidos; não é teste implantado de exceções técnicas com todos os conteúdos possíveis.
- Identidades de serviço, versão e hom corretas. Os spans da API foram enriquecidos com namespace `default`, pod e Deployment; a identidade da Lambda foi preservada.
- Duas chamadas adicionais enviaram contexto X-Ray fictício com `Sampled=0`. Ambas produziram OTLP com o Root recebido; Gateway/runtime estabeleceram novos pais, preservados pelos spans da aplicação. Nenhum contexto recebido foi usado como dimensão de métrica.
- A consulta `xray.polling` encontrou nove spans nativos relativos aos dois traces amostrados selecionados. Os campos nativos no New Relic continuam com representação X-Ray `1-...`; os OTLP usam 32 caracteres hexadecimais. Não foi comprovada uma única árvore automática entre essas representações.

Parentesco conferido com os documentos nativos obtidos na AWS:

| Caminho | Trace OTLP | Span OTLP → pai nativo |
|---|---|---|
| Gateway → API / Authentication | `6ad7686a14cde6997a691ac14fbf7813` | `c952ff161f839133` → `1e510261445ed9ac`, integração do Gateway |
| Runtime Lambda → autenticação | `6ad7686b104d5dc750e1f27a021ac58e` | `136a462e26d3a44c` → `f89d6ebac806cfa6`, AWS::Lambda::Function |

Os spans PostgreSQL selecionados eram filhos dos respectivos spans HTTP/invocação. Segmentos nativos AWS não passam pela sanitização da aplicação e não foram incluídos na conclusão de ausência de URL/IP.

Primeira chamada da Lambda teve report de inicialização de **997,289 ms**, duração de **4.006,335 ms**, faturamento de **5.004 ms** e pico de memória de **182 MiB**, em 512 MiB/15 s configurados. Nos quatro reports seguintes capturados, durações de 113,227–206,032 ms e pico até 185 MiB; overhead da extensão de aproximadamente 67–86 ms. A primeira chamada teve overhead de aproximadamente 215 ms. Reutilização foi observada, mas não se infere congelamento/descongelamento ou capacidade máxima a partir disso. Não foi isolada uma primeira chamada para verificar exportação antes de qualquer segunda invocação nesta rodada.

## Kubernetes, CloudWatch e logs do Gateway

As **23 métricas Kubernetes selecionadas** chegaram com cluster/ambiente corretos. API 2/2 e Collector 1/1 apareceram no dashboard; CPU, working set, requests/limits, fases/readiness e recursos alocáveis foram conferidos. No intervalo de nove janelas completas de um minuto, os maiores percentuais sobre os recursos alocáveis do nó foram aproximadamente **6,755% CPU e 68,073% memória**: não houve violação real dos limiares de 80%/85%. A cobertura kubelet continua limitada ao nó do Collector.

As treze estatísticas abaixo **coincidiram com os resultados AWS dos mesmos minutos**. Os dados da origem foram salvos antes da expiração administrativa; a conclusão da leitura do destino ocorreu pelo navegador, após o atraso de coleta. São gauges por período, não totais da sessão.

| Minuto UTC | Métrica / estatística | AWS = New Relic |
|---|---|---:|
| 13:12–13:13 | Gateway Count / SampleCount | 19 |
| 13:12–13:13 | Gateway 4XXError / Sum | 10 |
| 13:12–13:13 | Gateway 5XXError / Sum | 0 |
| 13:12–13:13 | Gateway Latency / Average, ms | 354,89473684210526 |
| 13:12–13:13 | Gateway IntegrationLatency / Average, ms | 350,7894736842105 |
| 13:11–13:12 | Lambda Invocations / Sum | 5 |
| 13:11–13:12 | Lambda Errors / Sum | 0 |
| 13:11–13:12 | Lambda Throttles / Sum | 0 |
| 13:11–13:12 | Lambda Duration / Average, ms | 938,66 |
| 13:11–13:12 | Lambda ConcurrentExecutions / Maximum | 1 |
| 13:11–13:12 | RDS CPUUtilization / Average, % | 4,964267270819104 |
| 13:11–13:12 | RDS FreeableMemory / Average, bytes | 176.267.264 |
| 13:11–13:12 | RDS DatabaseConnections / Average | 2 |

Os logs Gateway tiveram **43/45 request IDs encontrados**, 43 IDs de evento distintos e nenhum ID inesperado. Os dois últimos ficaram ausentes no New Relic mesmo ampliando a pesquisa até 13:45 UTC; ambos existem no CloudWatch, conferidos pelo console:

| Request ID | Stream | Timestamp UTC / resposta |
|---|---|---|
| `dc186d9c-fc81-402b-a475-07006c2ca4c2` | `885ff8d4948b57517f8988b4438c9999` | 13:17:20,995 / 401 |
| `71a7f355-ed6b-437c-b51f-081e87a92c40` | `8e622a68889d990aa23f8d01644f4b9e` | 13:17:21,627 / 400 |

Os primeiros 43 eventos também constam da evidência JSON da origem. A causa específica da ausência dos dois últimos não foi estabelecida nesta conferência. Não foi executado restart, replay ou mudança no receiver; a limitação de cobertura permanece aberta. Retenção de uma semana do grupo Gateway foi conferida no console; não equivale à retenção New Relic.

## Resultado por cenário e próximos passos

| ID | Status | Evidência / limite |
|---|---|---|
| V01 | aprovado | Saúde/probes, revisão corrigida e snapshots; sem teste de carga |
| V02 | aprovado | Logins, conta própria, autorização e negativas |
| V03 | aprovado | Seis chamadas Customer com resultados esperados; runtime reutilizado |
| V04 | aprovado | Identidade e enriquecimento dos sinais novos |
| V05 | aprovado | Sanitização dos atributos/registros inspecionados; probes sem spans |
| V06 | aprovado | Bootstrap por processo: API dois pods, Lambda uma inicialização |
| V07 | aprovado | Chamadas aquecidas sem repetição indevida do bootstrap |
| V08 | aprovado | Conexões/comandos/transações e dois rollbacks bem-sucedidos |
| V09 | aprovado | Readiness e cinco leituras do histórico com purpose próprio |
| V10 | aprovado | Quatro painéis publicados e conferidos; sem séries reais de falha |
| V11 | aprovado | Fluxo aprovado, amostras e tempo de serviço |
| V12 | aprovado | Recusa/entrega e motivo separados, sem serviço concluído fictício |
| V13 | aprovado | Três criações; OS excluída ausente e seu evento preservado, comprovados por SQL |
| V14 | aprovado | Negativas exercitadas sem eventos extras; estados, estoque e contadores coerentes; limite de snapshot documentado |
| V15 | aprovado | Onze simulações e todas as quantidades/durações reconciliadas independentemente com SQL |
| V16 | aprovado | Dashboard HTTP e 23 métricas Kubernetes; cobertura limitada ao nó |
| V17 | aprovado | Treze estatísticas comparadas e métricas auth/notificações presentes |
| V18 | inconclusivo | Cobertura parcial 43/45; sem duplicação nessa amostra |
| V19 | aprovado | Propagação/pais dos dois caminhos; árvore única no New Relic não comprovada |
| A01–A05 | não executado | Dez condições habilitadas visíveis; configuração detalhada/roteamento mantêm a evidência anterior. Sem falha, escala zero, carga ou teste de e-mail nesta rodada |
| F01–F06 | não executado | Nenhuma interrupção, mudança de configuração, restart ou falha induzida |

Prioridades restantes: planejar e autorizar individualmente falhas/disparos/recebimentos dos cinco alertas e demais experimentos; tratar a cobertura dos access logs; investigar separadamente a união das representações X-Ray/OTLP. Falhas/timeout/cancelamento implantados, retenção/consumo New Relic, recuperação de exportação, ciclo de congelamento e prd não foram comprovados nesta sessão. A conferência SQL direta desta massa está concluída. **E5 continua parcial.**

As leituras desta rodada não alteraram configurações AWS/New Relic nem executaram workflows, deploy ou destroy. A massa fictícia autorizada deixou duas OSs entregues e consumo de uma unidade; somente a terceira OS, criada para exclusão, foi removida. Port-forward próprio e sessão HTTP com tokens em memória foram encerrados. Artefatos técnicos locais, ignorados pelo Git: `artifacts/e5-validacao-integrada/20261020-recreated/`.

## Complemento — falha PostgreSQL autorizada

Após a conferência normal e a reconciliação SQL, o mantenedor autorizou validar uma falha técnica pelo PostgreSQL. Foi usada somente a leitura `GET /Catalog` em hom, com o login fictício Mechanic já autorizado. Nenhuma ação de OS, alteração de cadastro, permissão, esquema, configuração AWS ou New Relic foi executada neste complemento.

O pod temporário `database-access` abriu uma sessão TLS `verify-full` como `mecanica_api`. A sessão adquiriu um lock exclusivo em `catalog`, aguardou uma única leitura da API bloqueada por ela e cancelou somente esse comando com `pg_cancel_backend`. A identificação exigiu o mesmo usuário, tabela, lock pendente, sessão bloqueadora e IP de um dos pods da API; nenhuma outra sessão foi encerrada. O controlador tinha `idle_in_transaction_session_timeout=15s`, `statement_timeout=12s` e espera máxima de oito segundos para localizar o alvo. O rollback foi explícito. Duas tentativas preliminares terminaram com 200, sem cancelamento, e seus locks foram liberados; uma delas durou 7,842 s. Não são falhas comprovadas nem foram omitidas da evidência.

Na tentativa concluída, o lock foi adquirido às **15:00:44,981385 UTC**, o backend cancelado às **15:00:48,455316 UTC** e o rollback liberou o lock às **15:00:48,457343 UTC** — cerca de 3,476 s de bloqueio. Esses instantes vêm do banco; o relógio de parede do cliente estava defasado em relação ao banco/telemetria e não foi usado para calcular intervalos entre fontes.

| Conferência | Resultado |
|---|---|
| Leitura anterior / falha / leitura posterior | 200 / 500 / 200 |
| Request ID da falha | `61a34fd3-8bd1-4f05-b253-1175f55653a7` |
| Problem Details | JSON de erro; sem SQL ou código interno consultados no corpo |
| Catálogo antes/depois | Uma linha; resposta com SHA-256 idêntico |
| Restauração SQL | Nenhum lock exclusivo do controlador e nenhuma sessão bloqueada por ele |
| Probes posteriores | live, ready e startup: 200 / Healthy |
| Pods da API | Dois Ready/Running, zero reinícios |

Na janela **14:57–15:03 UTC**, o New Relic recebeu `mecanica.integrations.operations`, filtrada por `service.name=mecanica-api`, ambiente hom, `integration=postgresql` e `purpose=application`:

| Operação | Resultado | Quantidade |
|---|---|---:|
| connection.open | success | 6 |
| command.execute | success | 5 |
| command.execute | failure | 1 |

São doze acessos técnicos e uma falha; a fórmula do painel retornou **8,333333333333334%**. Esse percentual usa todos os acessos técnicos elegíveis, incluindo aberturas, e não representa percentual de requisições HTTP. O cancelamento foi solicitado pelo servidor sem cancelar o token do chamador: `PostgresException/57014` foi corretamente classificado como `failure`, sem comprovar os resultados `timeout` ou `canceled`.

O trace `6ad782200908096731040cab355d299f` contém o span HTTP `2029aa24183ce624` com 500 e o filho PostgreSQL `d4d68fb919eadc07`, ambos com status ERROR e descrições fixas. O log correlacionado registra `Npgsql.PostgresException`, rota Catalog e a mensagem fixa da operação. Nos dois spans e no log consultados não foram encontrados `db.statement`, `db.query.text`, `db.connection_string`, `url.full`, `exception.stacktrace` ou a mensagem original de cancelamento. Não foram encontrados eventos separados `SpanEvent` nessa consulta; isso não equivale a auditoria irrestrita de todos os atributos ou erros.

`mecanica.orders.processing.failures` permaneceu em zero na janela. O teste exercitou Catalog, portanto **não comprova F01/A01 nem envio de e-mail de alerta**. Também não cobre falhas na Lambda, SSM/Secrets Manager, abertura de conexão, transações, timeout ou cancelamento pelo chamador. E5.8 continua parcial, agora com `command.execute/failure` da API comprovado.

A sessão SQL e o cliente HTTP foram encerrados, o port-forward foi interrompido e a ausência do pod temporário foi confirmada. Senha e JWT não integram os arquivos de evidência. Resultados técnicos locais: `artifacts/e5-validacao-integrada/20261020-postgresql-failure/http-evidence.json` e `validation-summary.json`.
