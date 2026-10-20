# Validação de hom — propagação X-Ray/OTLP e CloudWatch

Conferência em 18/10/2026, após os deploys do mantenedor, no Gateway `ehrzyhusae`, stage `hom`, AWS `121754142617/us-east-1` e New Relic US `8626152`. Chamadas controladas, consultas AWS/Kubernetes e NRQL pela sessão do navegador. Sem workflows, deploy, restart, alterações IAM ou de cadastros. Tokens apenas em memória; nenhuma leitura da License Key.

## Revisões e saúde

- API `d3d4e26b8646bcce6b455741954ad9719ef5519a`, imagem `sha256:c17d650fbe06c4740d1770902ab39f0d2574acb9d296cb69f79f84e4ac294a1b`.
- Autenticação `313df3040513be95b783af638dd698142186c3af`, versão numérica **8**, consumida pelo Gateway. X-Ray Active, Collector layer 0.22.0, OpenTelemetry habilitado. Os dois SHAs chegaram como `service.version` no New Relic.
- Oito reports da Lambda com status success: duração de 73,92 a 1828,95 ms e máximo de memória de 186 MB/512 MB. A primeira chamada teve faturamento incluindo inicialização. Não constitui teste de carga, de congelamento prolongado ou de indisponibilidade.
- API e Collector Contrib 0.161.0: uma réplica Ready, zero reinícios. Amostras de consumo: API 2m CPU/91 MiB; Collector 2m/45–47 MiB. São amostras de baixa carga.
- Probes internas live/ready/startup: 200/Healthy por port-forward temporário, encerrado depois. `/health/live` pelo Gateway: 403, sem exposição da probe.
- RDS PostgreSQL 16.15 disponível, privado e criptografado. Login interno e elegibilidade da Lambda exercitaram persistência real; não foi aberta sessão SQL direta nesta conferência.

## HTTP e perfis

Dezenove cenários dirigidos com resultado esperado, incluindo login Admin/Mechanic, emissão Customer pela Lambda com seed fictício autorizado, consultas de clientes/conta própria, ausência de token, perfil indevido, filtro de outra conta, OS inexistente, JSON/documento inválido, cliente não elegível e login inexistente. Respostas 200/400/401/403/404; nenhuma 5xx.

O Gateway registrou 28 chamadas no total: 7 respostas 200, 9 respostas 400, 6 respostas 401, 4 respostas 403 e 2 respostas 404. Inclui sete chamadas preliminares e dois casos cuja massa inválida retornou corretamente 400; esses dois foram repetidos com login fictício válido e inexistente, obtendo 401. Uma tentativa local de interpretar JWT como JSON também exigiu ajustar o cliente para `Accept: application/json`; não foi falha da API.

Não foram repetidos CRUD, ciclo completo de OS, histórico SQL ou inativação. Esses fluxos possuem [evidência anterior](VALIDACAO_HOM_2026-10-17.md), que não deve ser apresentada como nova regressão integral desta revisão.

## Propagação comprovada e limite no New Relic

Os IDs recebidos pela instrumentação correspondem aos segmentos nativos AWS. Exemplos:

| Caminho | Trace OTLP | Pai nativo do span de entrada | Span de entrada OTLP | Filho PostgreSQL |
|---|---|---|---|---|
| Gateway → API, GET Customers | `6ad4ea175547631f6075ad6766d88727` | `53c36496c25ec942` | `90567c176adfd279` | `f79e37323775c22a` |
| Gateway → Lambda, documento elegível | `6ad4ea14254a813a154fb2b76da238b2` | `b1becb2a1dda6bfe` | `e6f7d464860f955c` | `3070cc4d88b8ab4e` |

Na API o pai é o subsegmento de integração HTTP do Gateway; na Lambda é o segmento `AWS::Lambda::Function`. Outros dois traces da API também apresentaram pais corretos. Isso comprova chegada do contexto X-Ray e seleção pelo propagador, inclusive pelo runtime gerenciado da Lambda.

**A união automática dos dois caminhos no New Relic permanece pendente.** O polling entrega, por exemplo, `trace.id=1-6ad4ea14-254a813a154fb2b76da238b2` e `id=<trace X-Ray>:b1becb2a1dda6bfe`; o OTLP entrega o trace de 32 dígitos e o span de 16 dígitos. O polling também prefixa `parent.id`. A identidade é correlacionável pelos componentes dos IDs, mas os valores armazenados não são iguais. Não declarar árvore única concluída nem alterar IDs/Collectors sem uma entrega própria para analisar essa representação da integração New Relic.

Uma chamada com W3C não amostrado e contexto adicional manteve o trace X-Ray criado pelo Gateway: dois spans OTLP sob `6ad4ec0d2afc3c2b059d434a764024ae`, nenhum sob o W3C enviado. O teste verifica precedência neste caminho real; não comprova o fallback W3C na ausência de X-Ray, pois o Gateway fornece o contexto nativo.

Outra chamada forneceu X-Ray `Sampled=0`: o Gateway preservou o root, três spans OTLP chegaram sob `6ad4ed44f959997144374092b07db56c` e a consulta AWS não encontrou trace nativo correspondente. Confirma a independência da coleta OTLP AlwaysOn nesse caso. Às 16:03:17 UTC havia 50 spans OTLP e 50 IDs distintos.

## Sinais e sanitização

Na consulta das 16:00:51 UTC: 47 spans OTLP e 47 IDs distintos; nenhum dos atributos `db.statement`, `db.query.text`, `exception.stacktrace`, `url.full`, `url.query` ou `http.url`, e nenhum span com nome de probe. Não houve duplicação por ID nessa amostra; dois spans PostgreSQL da Lambda representam abertura/consulta, não duas invocações.

Na consulta das 16:01:37 UTC: oito logs da autenticação e três da API, todos com `trace.id` e `span.id`. Mensagens da API com operação/tipo fixos e modelo da rota, sem query/UUID do caminho; sem stack trace nas mensagens inspecionadas. A inspeção é limitada aos casos/atributos consultados, não auditoria de todos os cenários de erro.

Na consulta das 15:56:58 UTC: 90 spans `xray.polling` e 42 `api.traces.otlp`. Os segmentos nativos incluem URLs/caminhos e não passam pela sanitização da aplicação. Não aplicar a conclusão de sanitização OTLP aos registros nativos.

## CloudWatch

As 13 séries selecionadas foram recebidas no New Relic. Comparação do período **15:47:00–15:48:00 UTC**, com as mesmas estatísticas, coincidiu com o snapshot AWS disponível no momento:

| Métrica | Estatística | Valor |
|---|---|---:|
| Gateway Count | SampleCount | 7 |
| Gateway 4XXError / 5XXError | Sum | 5 / 0 |
| Gateway Latency / IntegrationLatency | Average, ms | 848,285714 / 843,714286 |
| Lambda Invocations / Errors / Throttles | Sum | 4 / 0 / 0 |
| Lambda Duration | Average, ms | 250,1975 |
| Lambda ConcurrentExecutions | Maximum | 1 |
| RDS CPUUtilization | Average, % | 4,474776261186941 |
| RDS FreeableMemory | Average, bytes | 188665856 |
| RDS DatabaseConnections | Average | 2 |

São valores publicados/coletados por período, não contagem reconstruída das 28 chamadas. Dados CloudWatch podem chegar depois; não preencher ausências com zero ou prometer recuperação de revisões tardias. O atraso de coleta de dez minutos foi respeitado.

Logs Gateway recebidos com mensagem fixa, status/durações numéricos, `requestId`, ID original CloudWatch e `xrayTraceId`; `trace.id`/`span.id` permanecem ausentes por decisão de configuração. Na consulta das 16:01:37 UTC havia **18 logs e 18 IDs/request IDs distintos**, enquanto a AWS já continha as chamadas preliminares e posteriores adicionais. A entrega não está comprovada como completa: registrar a lacuna e comparar novamente antes de atribuir causa ou alterar a retomada. Não houve restart para tentar recuperar os eventos.

Conferência final às **16:04:29 UTC**: **19/28** request IDs AWS encontrados no New Relic, 19 IDs de evento distintos, sem duplicação nessa amostra. Chegou o teste mais recente com Sampled=0; ainda não apareceram os sete eventos preliminares nem os dois logins negativos repetidos. Isso mostra coleta ativa, mas não entrega completa. A causa não foi estabelecida; o comportamento aceito sem checkpoint não oferece recuperação automática desses eventos. A lista exata dos nove não observados ficou na evidência local.

O Collector estava Ready e sem erros/avisos recentes na amostra de dez minutos encerrada às 16:05:50 UTC. Ocorreram `ResourceNotFoundException` anteriores à criação do grupo do Gateway, compatíveis com provisionar a base antes do Gateway; não confundir esses registros antigos com falha atual de autorização.

## Continuidade

E5.14 permanece parcial: transporte/parentesco X-Ray e as 13 métricas CloudWatch comprovados em hom; união dos IDs no New Relic, cobertura dos logs Gateway, restart/lookback autorizado e prd pendentes. Dashboards, alertas, carga/falhas e descarte com última coleta continuam separados. Sem induzir 500, saturação, falha IAM ou indisponibilidade real para testar.

Evidências técnicas locais ignoradas em `artifacts/validation-20261018-correlation/`: respostas HTTP sem corpos/tokens, probes, metadados AWS/Kubernetes, eventos Gateway, logs Lambda selecionados, segmentos X-Ray selecionados e snapshots CloudWatch. A sessão do navegador foi usada somente para consultar o New Relic; nenhuma configuração ou integração foi modificada.

## Investigação histórica após o descarte — 18/10/2026

Nova conferência aproximadamente às 15:30–15:34 de São Paulo (18:30–18:34 UTC), sem reprovisionar a API nem gerar chamadas. Usada janela absoluta 15:40–16:10 UTC para não perder as evidências ao avançar o relógio. A consulta AWS `DescribeLogGroups` pelo prefixo exato não encontrou o grupo do Gateway; não foi possível recuperar agora horários de ingestão ou repetir o polling contra a origem.

**Access logs:** continuam os mesmos 19 registros, com 19 request IDs e IDs de evento distintos. Busca adicional pelos nove request IDs ausentes em toda a coleção Log, sem filtro de serviço e desde 15:00 UTC até 18:33:09 UTC, retornou zero. Portanto, não se tratava apenas da janela relativa antiga ou de atraso de poucos minutos; os nove continuam sem comprovação de entrega. O snapshot AWS anterior preserva sua existência, mas não contém `ingestionTime`. A causa de cada perda não pode ser afirmada retrospectivamente.

Na versão fixada [0.161.0, logs.go](https://github.com/open-telemetry/opentelemetry-collector-contrib/blob/v0.161.0/receiver/awscloudwatchreceiver/logs.go), o receiver usa o timestamp do evento em `FilterLogEvents`. Após uma janela inteiramente vazia, avança a posição para o fim dessa janela. Se um evento com timestamp anterior só se tornar consultável depois, ele pode ficar para trás; não existe sobreposição contínua na configuração adotada. O lookback de cinco minutos é aplicado somente na inicialização. Esta é uma hipótese compatível com as lacunas observadas e um limite já aceito no desenho, não prova da causa dos nove eventos.

O [bug de paginação vazia #48577](https://github.com/open-telemetry/opentelemetry-collector-contrib/issues/48577) teve [correção #48771](https://github.com/open-telemetry/opentelemetry-collector-contrib/pull/48771). O código 0.161.0 inspecionado já preserva o último timestamp quando uma página posterior está vazia. Não atribuir automaticamente o incidente àquele bug antigo nem propor atualização como correção comprovada. A vulnerabilidade a eventos tardios em uma janela totalmente vazia continua distinta. A [API AWS](https://docs.aws.amazon.com/AmazonCloudWatchLogs/latest/APIReference/API_FilterLogEvents.html) filtra pelo timestamp do evento, não pelo seu horário de ingestão.

Não há evidência que justifique alterar permissões, filtros de sanitização ou recursos do Collector. Persistência, aumentar apenas o lookback inicial ou reiniciar depois de o grupo ter sido removido não recuperariam estes registros. Para garantia maior, será necessário decidir outro comportamento de coleta/entrega e comprová-lo em implantação futura; isso não foi implementado nesta investigação.

**X-Ray/OTLP:** a consulta histórica da mesma invocação manteve cinco spans `xray.polling` e três OTLP. O ID de trace do polling tem `1-` e separadores; seus `id` e `parent.id` possuem ainda o prefixo `<trace X-Ray>:`. O pai OTLP `b1becb2a1dda6bfe` corresponde ao componente do segmento nativo, mas não ao valor integral armazenado `1-6ad4ea14-254a813a154fb2b76da238b2:b1becb2a1dda6bfe`. Não é somente diferença visual na apresentação do root: os campos de vínculo persistidos também diferem.

A [documentação OTLP do New Relic](https://docs.newrelic.com/docs/opentelemetry/best-practices/opentelemetry-best-practices-traces/) mapeia os campos reais do protocolo para `trace.id`, `id` e `parent.id`, com precedência sobre atributos comuns. Acrescentar um atributo de mesmo nome no manifesto/Collector não substitui esses campos. A [integração X-Ray](https://docs.newrelic.com/docs/infrastructure/amazon-integrations/aws-integrations-list/aws-x-ray-monitoring-integration/) documenta ingestão por polling, sem oferecer nesse procedimento uma opção de normalização/união com OTLP.

Não foi localizada uma configuração nativa documentada para construir automaticamente uma árvore única entre esses formatos. Isso não equivale a uma declaração do fornecedor de ausência absoluta de suporte: eventual mecanismo específico exige confirmação do New Relic antes de planejar uma migração. A relação atual pode ser consultada pelos pares de IDs conhecidos, preservando os dois caminhos. Árvore única exigiria suporte de normalização na ingestão ou outra abordagem para trazer os segmentos nativos ao mesmo formato; não deve ser resolvida inventando spans, duplicando a coleta ou mudando IDs da aplicação sem planejamento.

Conclusão: investigação possível com os dados históricos, sem necessidade de subir o ambiente. Propagação permanece comprovada; entrega completa de logs e união automática no New Relic não foram comprovadas. Nenhuma alteração de produção foi feita.
