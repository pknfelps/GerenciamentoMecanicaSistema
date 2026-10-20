# E5 — Logs do Gateway: cobertura e causa da lacuna — 20/10/2026

**Resultado: entrega parcial confirmada, com causa da lacuna identificada por evidências convergentes de CloudWatch, CloudTrail, New Relic e código da versão fixada do receiver.** Permanecem **43/45 eventos** da amostra. Os dois ausentes foram ingeridos pelo CloudWatch depois da consulta de sua janela; o cursor seguinte já começava além do horário desses eventos.

A formatação/sanitização e a identificação dos registros entregues foram conferidas no escopo abaixo. Não foi realizada correção, restart, replay ou geração de tráfego.

## Escopo e reconciliação

Consulta em 20/10/2026, com consolidação às 23:24 UTC. Hom, conta AWS 121754142617/us-east-1, grupo /aws/apigateway/mecanica-hom-gateway e New Relic US 8626152. Janela histórica da amostra: **13:10–13:19 UTC**.

A existência do grupo e a retenção de sete dias foram confirmadas. FilterLogEvents, com paginação automática e sem limit na conferência final, retornou 45 eventos. Os request IDs correspondem à massa da rodada integrada de hom.

| Conferência | Resultado |
|---|---:|
| Eventos na origem / JSON válido | 45 / 45 |
| Registros no New Relic | 43 |
| Request IDs distintos no destino | 43 |
| IDs originais distintos no destino | 43 |
| Correspondências com os request IDs da origem | 43 |
| Request IDs inesperados | 0 |
| Ausentes | 2 |
| Duplicações nessa janela | 0 |

Os ausentes continuam sem resultado na busca ampliada por requestId ou ID original, restrita a mecanica-gateway/hom, desde 13:00 UTC até o instante da conferência. O limite solicitado na consulta foi 00:00 UTC de 21/10; o resultado cobre somente os dados existentes até a consulta, não eventos futuros.

Uma tentativa de busca sem restrição de serviço/ambiente foi rejeitada pela revisão automática e não foi executada. A alternativa restrita ao escopo autorizado concluiu; esta conferência não audita outras fontes de logs da conta.

## Dois registros ausentes

| Request ID | Operação / status | Horário do evento UTC | Ingestão CloudWatch UTC |
|---|---|---|---|
| dc186d9c-fc81-402b-a475-07006c2ca4c2 | POST /Authentication / 401 | 13:17:20.995 | 13:17:41.131 |
| 71a7f355-ed6b-437c-b51f-081e87a92c40 | POST /customers/validate / 400 | 13:17:21.627 | 13:17:41.740 |

IDs originais:
- 39974135744224628164503584334758474012935649895004438528.
- 39974135758318699129974938160945037053934406110574936064.

Ambos contêm exatamente os nove campos técnicos previstos e JSON válido. O atraso entre evento e ingestão foi 20,136 s e 20,113 s. Nos 45 eventos, os atrasos ficaram entre 20,076 s e 20,245 s. Respostas 401/400 são negativas esperadas; não explicam um descarte pelo pipeline.

## Causa: evento tardio atrás do cursor

CloudTrail LookupEvents confirmou três chamadas FilterLogEvents da role mecanica-hom-collector-runtime, para o grupo exato, sem errorCode:

| Chamada UTC | startTime UTC | endTime UTC |
|---|---|---|
| 13:16:37 | 13:15:37.421 | 13:16:37.421 |
| **13:17:37** | **13:16:37.421** | **13:17:37.421** |
| **13:18:37** | **13:17:37.421** | **13:18:37.421** |

A consulta de 13:17:37 abrangia os horários 13:17:20–21, mas os eventos só foram ingeridos às 13:17:41. A próxima consulta começou em 13:17:37.421, portanto não voltou a cobrir seus timestamps. O CloudTrail não fornece aqui o corpo da resposta; a conclusão decorre da combinação entre os limites efetivamente usados, horários de ingestão e algoritmo do receiver, não de uma resposta vazia capturada.

No [código do receiver 0.161.0](https://github.com/open-telemetry/opentelemetry-collector-contrib/blob/v0.161.0/receiver/awscloudwatchreceiver/logs.go#L274-L323), uma janela sem eventos avança o próximo início para o fim da janela. A [API FilterLogEvents](https://docs.aws.amazon.com/AmazonCloudWatchLogs/latest/APIReference/API_FilterLogEvents.html) seleciona pelo timestamp do evento. Esse comportamento permite que dados disponibilizados tardiamente fiquem atrás do cursor, mesmo que estejam presentes no CloudWatch.

Esta etapa fortalece a hipótese anterior com as chamadas reais auditadas. Não há evidência de falta de permissão nessas consultas nem necessidade de atribuir a lacuna à API, ao JWT, às respostas 400/401 ou à sanitização.

## Configuração e registros entregues

ConfigMap implantado conferido somente por leitura:
- Collector Contrib 0.161.0, uma réplica Ready.
- poll_interval=60s, initial_lookback=5m e max_events_per_request=1000.
- Grupo explícito de hom; sem start_from/storage persistente configurados.
- Pipeline logs/cloudwatch com transform/filter/allowlist, identificação AWS/Gateway e exportador existente.

O lookback de cinco minutos é usado na inicialização, não uma sobreposição contínua entre consultas. Os restarts posteriores de 18:22/18:32 não cobrem acontecimentos de 13:17. O teste anterior de releitura/duplicação permanece válido e independente; não foi repetido.

Nos 43 logs:
- Mensagem única e fixa: **Requisição processada pelo Gateway.**
- Status, duração, responseLength e integrationMs quando disponível chegaram como números.
- integrationMs indisponível na negativa da probe ficou ausente/null, sem transformar o marcador - em zero.
- RequestId, extendedRequestId, xrayTraceId, ID original e stream presentes; serviço mecanica-gateway, hom, conta/região/grupo corretos.
- trace.id e span.id ausentes, como previsto: xrayTraceId permanece um campo próprio.
- Keyset recebido somente com campos técnicos e metadados de ingestão; nenhum campo de corpo HTTP, header, token, senha, documento ou conteúdo de negócio identificado.
- Nas 45 rotas da origem, nenhuma query ou UUID do recurso apareceu. Rotas parametrizadas mantêm {path2}; a negativa de rota da probe registrou /hom/health/live. Não foram testados caminhos desconhecidos arbitrários ou JSON malformado nesta etapa.

Essas conclusões são da amostra inspecionada, não garantia universal sobre qualquer mensagem/caminho futuro.

Os logs do pod atual incluem um timeout de exportação de **métricas** às 21:27, além de avisos de inicialização/aliases. Eles pertencem a outro sinal e horário; não foram usados como causa dos access logs perdidos às 13:17. O pod atual começou depois dos experimentos anteriores, portanto seu log não substitui o histórico de polling recuperado do CloudTrail.

## Situação final

**Validação executada; cobertura integral não atendida.** O desenho escolhido já aceita lacunas e duplicações, sem checkpoint/reprocessamento. A lacuna específica de dois eventos agora tem diagnóstico sustentado pelos horários e cursores auditados. Não significa que os eventos tenham sido recuperados nem que todas as lacunas históricas tenham a mesma causa.

Não houve acesso a valores de Secrets, alteração de IAM/manifestos/código/configuração New Relic, restart do Collector, replay ou tráfego novo. E5.14 permanece parcial pela cobertura e pela união X-Ray/OTLP não atendida, separadamente documentada.

Evidências sanitizadas locais: artifacts/e5-validacao-integrada/20261020-gateway-logs/cloudwatch-source.json, cloudtrail-polls.json e validation-summary.json. ConfigMap somente de leitura salvo no diretório da rodada recriada. Conferência prd não executada.

