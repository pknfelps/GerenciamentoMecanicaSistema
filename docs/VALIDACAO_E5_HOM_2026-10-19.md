# Validação integrada E5 — hom, 19/10/2026

Sessão iniciada às 01:19 UTC de **20/10/2026** (noite de **19/10 em São Paulo**). Gateway `p5a9dooa10`, stage `hom`, AWS `121754142617/us-east-1`, New Relic US `8626152`. Execução do [roteiro E5](ROTEIRO_VALIDACAO_E5.md), com chamadas usando os seeds fictícios autorizados e consultas AWS/Kubernetes/New Relic.

**Resultado: a validação funcional passou, mas a observabilidade da API falhou nesta revisão.** A Lambda, Kubernetes e coleta AWS foram conferidos independentemente. Não considerar E5 concluída nem interpretar os gráficos vazios da API como ausência de falhas.

## Revisões e saúde

- API `706d21f26d8ae69e0f465f197f9af154aa47dd52`, imagem `sha256:2ee2e6bbb895ba718fe5e78b808a48bb18783db722c673e2a58f99fb2f91b8eb`. SHA confirmado pela tag ECR; arquivos dos exporters locais idênticos aos desse commit.
- Autenticação `0446a19b4888921f1d9b4032d034de7d4009ab24`, versão numérica **9**, efetivamente referenciada na integração do Gateway. Runtime dotnet10, 512 MB, 15 segundos, X-Ray Active e layer Collector 0.22.0/Collector 0.151.0.
- Gateway com tracing habilitado, execution logs OFF e data tracing false. Logs de acesso mantêm os nove campos técnicos previstos.
- EKS 1.36 ACTIVE; RDS PostgreSQL 16.15 available, privado e criptografado.
- API e Collector Contrib 0.161.0: uma réplica disponível cada, zero reinícios. HPA 1–3. Nó `ip-10-0-12-191.ec2.internal`.
- Probes internas live/ready/startup: **200/Healthy**. Probe do Collector: **200**. `/health/live` pelo Gateway: **403**, sem publicação da probe.
- Consumo inicial/final observado: API 7m/83 MiB → 2m/106 MiB; Collector 2m/45 MiB → 6m/66 MiB; nó 73m/965 MiB → 61m/1008 MiB. São amostras de baixa carga, sem prova de capacidade/HPA sob saturação.

O login administrativo estava expirado e foi renovado pelo mantenedor. O EKS foi consultado pelo usuário `mecanica`, que já possui access entry. Nenhuma permissão foi criada ou ampliada.

## Regressão da exportação OTLP da API

As consultas da janela atual não encontraram métricas, spans ou logs OTLP de `mecanica-api`, embora o pod tenha telemetria habilitada, o meter novo selecionado e o endpoint do Collector configurado. O dashboard de ordens apresentou contagens zero/sem durações, apesar das três criações HTTP bem-sucedidas; login interno e métricas HTTP da API também ficaram sem dados.

O código implantado atribui explicitamente o endereço **base** ao `Endpoint` dos exporters individuais:

- `GerenciamentoMecanicaSistema/Observability/ApiMetricProvider.cs`: exporter de métricas.
- `GerenciamentoMecanicaSistema/Observability/ObservabilityDependencyInjection.cs`: exporters de traces e logs.

Na versão 1.19.1, atribuir `Endpoint` explicitamente não acrescenta o caminho do sinal como faz a composição `UseOtlpExporter` usada na Lambda. A documentação XML do pacote instalado descreve a adição automática dos caminhos quando o Endpoint não foi definido. A configuração da API aponta para `http://otel-collector.observability.svc.cluster.local:4318`, sem o caminho.

Conferência pelo receiver real, por port-forward temporário e com envelopes protobuf **vazios**, sem métricas sintéticas:

| POST | Resposta |
|---|---:|
| `/` | 404 |
| `/v1/traces` | 200 |
| `/v1/metrics` | 200 |
| `/v1/logs` | 200 |

Esse uso do endpoint explica a ausência dos três sinais da API. O Collector está saudável e recebe os demais produtores. Nenhuma correção de código/configuração, restart ou novo deploy foi aplicado durante a validação. É necessário corrigir a composição dos endpoints por sinal e publicar a API antes de repetir os indicadores de negócio, bootstrap/readiness/telemetry da API e seu parentesco OTLP.

## Chamadas HTTP e OS

**45 chamadas, 45 códigos esperados, nenhuma 5xx**. Distribuição: 200=14, 201=3, 204=11, 400=4, 401=4, 403=5, 404=2, 422=2. Tokens mantidos somente na memória do cliente temporário; respostas contendo JWT não foram gravadas.

Conferidos logins Admin/Mechanic, conta própria, consulta de clientes/veículos/catálogo/estoque, ausência de token, Admin impedido nas operações Mechanic, Customer impedido no GET Users, filtro de outra conta, perfil inválido e login inexistente. Também documento/corpo inválidos, cliente não elegível, validação elegível fria/aquecida e um login inexistente com contexto não amostrado e marcadores fictícios de baggage/tracestate.

Os três fluxos usaram os cadastros fictícios existentes:

1. OS aprovada: criação, diagnóstico, orçamento pelo Customer, execução, conclusão e entrega. Estado final **Delivered**.
2. OS recusada: criação, diagnóstico, recusa pelo Customer e entrega.
3. OS criada e excluída: consulta posterior retornou **404**.

Transição antecipada de execução e documento divergente em orçamento próprio retornaram **422**; Mechanic decidindo orçamento retornou **403**; payload vazio retornou **400**. Estoque final: **uma unidade consumida, nenhuma reserva residual**, coerente com um serviço executado, uma recusa e uma exclusão. Não houve inspeção SQL direta para provar cada alteração histórica isoladamente.

Logs textuais da API registraram **onze mensagens de envio simulado**, sem tentativa SMTP, sem marcadores de stack trace e sem erro inesperado na amostra. Os dois rollbacks esperados das negativas 422 registraram apenas mensagem fixa/tipo; não são falhas técnicas HTTP 500. As notificações e transações não puderam ser reconciliadas com métricas da API, pois sua exportação falhou.

Não foi aberto pod SQL nem lida credencial de banco. Reconciliação exata de histórico/durações permanece **não executada nesta rodada**; a comprovação SQL anterior continua válida somente para sua revisão/janela. As OSs de aprovação/recusa permanecem como massa de evidência em hom.

## Lambda: novas métricas e correlação

Na janela das cinco invocações: authenticated=2, invalid-request=1, invalid-document=1 e not-eligible=1. Cinco amostras de duração e cinco logs OTLP, todos correlacionados por trace/span. Nenhuma falha técnica ou timeout foi provocado.

O contador `mecanica.integrations.operations` totalizou **nove acessos**, todos success, na revisão correta:

| Integração/operação/finalidade | Quantidade |
|---|---:|
| SSM/get_parameters/bootstrap | 1 |
| Secrets Manager/get_secret_value/bootstrap | 2 |
| PostgreSQL/connection.open/application | 3 |
| PostgreSQL/command.execute/application | 3 |

O runtime aquecido não repetiu o bootstrap. Documento/corpo inválidos não geraram consultas de clientes; o cliente não elegível produziu consulta bem-sucedida, sem classificá-la como falha PostgreSQL. O dashboard atualizado mostra PostgreSQL=6, Secrets=2, SSM=1 e percentual zero sobre essas operações recebidas; isso não valida os resultados failure/timeout/canceled.

Onze spans OTLP e onze IDs distintos; nenhum dos campos `db.statement`, `db.query.text`, `exception.stacktrace`, `url.full`, `url.query` ou `http.url`, e nenhum nome de probe na consulta. A conclusão de sanitização é limitada à amostra Lambda; ausência total da API não permite aprovar sua sanitização exportada.

Primeira chamada elegível:

- Trace OTLP: `6ad6c267345c89e171fea21a0e1c56b0`.
- Span de entrada: `dbe1552a440a5d81`; pai `56b4efe039b7d75f`, correspondente ao segmento nativo `AWS::Lambda::Function`.
- Dois spans PostgreSQL (`ef6ce879fae731dd`, `bab0c36f70211f47`) filhos do span de entrada.

O New Relic também recebeu os segmentos `xray.polling`. A representação nativa continua com trace `1-...-...` e IDs prefixados pelo trace; **não existe comprovação de árvore única automática OTLP/X-Ray**. Gateway → API nativo existe, mas o parentesco com o span OTLP da API não pode ser revalidado sem sua exportação.

Cinco reports da Lambda com status success. Primeira chamada: duração 3546,766 ms, init 1049,838 ms, faturamento 4597 ms, memória máxima 181 MB/512 MB; aquecidas: 78,259–201,405 ms, máximo 182 MB. Os reports têm formato JSON `platform.report`, não linhas textuais REPORT. Não constituem teste de congelamento prolongado ou independência com EKS indisponível. A extensão iniciou sem falhas de autorização/exportação na amostra; há aviso de alias `otlphttp` depreciado, sem alteração automática nesta sessão.

## Kubernetes, dashboards e alertas

As **23 métricas Kubernetes selecionadas** foram encontradas no New Relic, com ambiente/cluster corretos. API desejadas/disponíveis=1/1; os valores do dashboard de operação coincidiram com os Deployments/pods observados. Nó Ready=1, MemoryPressure=0; amostra de CPU 0,058292 cores / 1,93 alocáveis e working set 1047126016 / 1503227904 bytes. Diferenças pequenas em relação a `kubectl top` decorrem das janelas; não usar esses instantes como prova de violação sustentada dos alertas.

Os três dashboards publicados foram abertos no período atual, com filtro hom. Os quatro painéis novos de integrações estão publicados; dados Lambda presentes e API ausentes. Operação mostra Kubernetes; seus blocos HTTP/.NET e o dashboard de ordens não podem ser aprovados com esta revisão. Contagem zero nativa no widget de ordens não representa o resultado funcional observado.

Dez condições dos cinco alertas continuam habilitadas e com consultas/limiares previstos na listagem. Cinco workflows ativos, sem execuções de notificação. O roteamento da disponibilidade foi relido: policy exata 8216645, destinatário configurado, Activated/Closed e respeito a silenciamento total/parcial. Demais parâmetros avançados mantêm a evidência anterior de publicação/no-diff; não foram relidos individualmente todos os campos.

Nenhuma falha, carga, pressão de memória ou escala zero foi induzida; nenhum teste de e-mail foi enviado. Disparo, recebimento e encerramento continuam pendentes para todos os alertas. A regressão impede ainda comprovar os dois alertas baseados nas métricas da API.

## CloudWatch e cobertura dos access logs

Comparação do período completo **20/10/2026 01:24:00–01:25:00 UTC**, após o atraso configurado: **13/13 valores iguais no CloudWatch e no New Relic**.

| Série | Estatística | AWS = New Relic |
|---|---|---:|
| Gateway Count | SampleCount | 8 |
| Gateway 4XXError | Sum | 3 |
| Gateway 5XXError | Sum | 0 |
| Gateway Latency | Average, ms | 38.375 |
| Gateway IntegrationLatency | Average, ms | 33.75 |
| Lambda Invocations | Sum | 4 |
| Lambda Errors | Sum | 0 |
| Lambda Throttles | Sum | 0 |
| Lambda Duration | Average, ms | 143.125 |
| Lambda ConcurrentExecutions | Maximum | 1 |
| RDS CPUUtilization | Average, % | 4.8113869491129 |
| RDS FreeableMemory | Average, bytes | 196182016 |
| RDS DatabaseConnections | Average | 2 |

São estatísticas por minuto, não totais das 45 chamadas. A consulta conjunta de conferência não usa FACET stat: com agregações de métricas diferentes, a escolha de facetas pelo primeiro agregado pode ocultar as demais estatísticas. As consultas finais usaram o mesmo intervalo e filtros técnicos; ausência em uma consulta preliminar não foi tratada como perda antes dessa revisão.

Os **45 request IDs existem na origem CloudWatch**. Na consulta final encerrada às **01:39:34 UTC**, o New Relic continha **30/45**, com 30 request IDs e 30 IDs de evento distintos, sem duplicação nessa amostra. Os quinze ausentes correspondem ao lote de consultas/negativas/aquecimento entre os primeiros logins e o ciclo aprovado. A lista e os timestamps de evento/ingestão da origem estão nas evidências locais; nenhuma causa individual foi comprovada. A coleta sem checkpoint/sobreposição contínua não assegura recuperação de eventos tardios. Não foi executado restart para tentar reler os eventos.

## Resultado por cenário

Status referente ao critério completo do roteiro; as partes funcionais aprovadas não tornam aprovada uma medição ausente.

| ID | Status | Resultado/limite |
|---|---|---|
| V01 | aprovado | Workloads/probes/RDS saudáveis; exportação da API tratada separadamente |
| V02 | aprovado | Logins, conta própria, consultas e negativas previstas |
| V03 | aprovado | Elegível fria/aquecida, não elegível, corpo/documento inválidos |
| V04 | falhou | Identidade Lambda/Kubernetes correta; sinais API ausentes |
| V05 | falhou | Sanitização Lambda conferida; sem amostra OTLP da API |
| V06 | falhou | Bootstrap Lambda 1 SSM/2 Secrets; API não recebida |
| V07 | inconclusivo | Reutilização Lambda comprovada; API sem métricas |
| V08 | falhou | Persistência funcional exercitada; operações API não exportadas |
| V09 | falhou | Readiness/etapas executadas, métricas readiness/telemetry da API ausentes |
| V10 | inconclusivo | Quatro painéis publicados; somente Lambda tem dados |
| V11 | falhou | Fluxo aprovado/Delivered correto; indicadores ausentes |
| V12 | falhou | Recusa/entrega corretas; indicadores ausentes |
| V13 | inconclusivo | Exclusão/404 corretos; sem SQL nem contador recebido |
| V14 | inconclusivo | Negativas e estoque final corretos; histórico/counter não conferidos |
| V15 | falhou | Onze simulações nos logs locais; sem métricas nem referência SQL nova |
| V16 | falhou | Kubernetes aprovado; blocos técnicos OTLP da API ausentes |
| V17 | aprovado | 13 estatísticas iguais no minuto 01:24–01:25 UTC; notificações/API têm falha separada |
| V18 | inconclusivo | Cobertura parcial dos access logs; ver fechamento |
| V19 | inconclusivo | Lambda comprovada; API sem spans OTLP; IDs nativos continuam distintos |
| A01 | não executado | Configuração publicada; sem falha provocada/métrica API recebida |
| A02 | não executado | Configuração publicada; sem falha de notificação provocada |
| A03 | não executado | Sem violação real sustentada; sem carga/alteração de limiar |
| A04 | não executado | Sem violação real sustentada; sem pressão artificial |
| A05 | não executado | Uma réplica disponível; escala zero não autorizada nesta sessão |
| F01 | não executado | Não interrompida persistência |
| F02 | não executado | Não alterado fluxo de notificação |
| F03 | não executado | Sem alteração de Deployment/HPA |
| F04 | não executado | Collector não reiniciado |
| F05 | não executado | Nenhuma interrupção induzida; exportação API já falhava por regressão |
| F06 | não executado | Sem alteração de IAM/Secrets/bootstrap |

## Correção local posterior à validação

Após solicitação do mantenedor, removidas as três atribuições diretas de `OtlpExporterOptions.Endpoint`. O endereço base validado é disponibilizado na configuração padrão para o SDK acrescentar os caminhos de cada sinal, incluindo a alternativa legada. Nenhuma mudança no manifesto, Collector, Lambda, workflows ou recursos AWS.

Validação local: restore/build Release; 1.191 testes aprovados, zero falhas/ignorados, incluindo PostgreSQL/Testcontainers e quatro casos novos simples de configuração; OpenAPI idêntico ao SHA anterior; publish linux-x64 com certificado/configuração e Kustomize hom/prd aprovados.

Transporte real conferido manualmente com Collector 0.161.0 e PostgreSQL descartáveis, nos modos padrão e legado. Cada modo exportou dois spans HTTP e três PostgreSQL com pais corretos, um log correlacionado de falha SQL local e métricas HTTP/runtime/integrações. Dez IDs de span distintos, sem spans de probes nem os marcadores sensíveis inspecionados. Processos/containers próprios removidos. Evidências em `artifacts/e5-exporter-fix/`, sem nova suíte de telemetria, exporter fictício ou script permanente.

**A correção ainda não foi implantada.** Os resultados AWS acima continuam descrevendo a revisão com defeito; esta conferência local não substitui a repetição após o próximo api-deploy.

## Continuidade

Prioridade: publicar a correção local da API e repetir os critérios dependentes dos seus três sinais, preservando o endpoint base declarativo. Depois, reconciliar os fluxos com SQL por acesso autorizado e combinar individualmente os experimentos reversíveis dos alertas. Não alterar limiares nem publicar métricas sintéticas para fabricar disparos.

Prd não foi exercitado. Consumo/retenção da conta New Relic não foi auditado nesta rodada. A validação não criou workflows, não aplicou infraestrutura, não leu License Key e não executou destroy. Artefatos técnicos privados em `artifacts/e5-validacao-integrada/20261020/`; o diretório é ignorado. Checklist local E5 mantém conclusões históricas com seu escopo, registra esta regressão e não marca E5.8 como concluída. Os dois port-forwards e a sessão HTTP com tokens em memória foram encerrados ao finalizar.
