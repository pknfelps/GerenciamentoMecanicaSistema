# Roteiro consolidado de validação da E5

Preparado em **19/10/2026**. Ambiente inicial: **hom**, AWS `121754142617/us-east-1`, New Relic US `8626152`. Este arquivo prepara a execução; não registra novos testes AWS como realizados nem autoriza alterações no ambiente.

**Execução de 19/10 à noite (20/10 UTC):** ver [resultados por cenário](VALIDACAO_E5_HOM_2026-10-19.md). A regressão funcional passou, mas os três sinais OTLP da API não foram exportados por configuração incorreta dos endpoints individuais. Lambda, Kubernetes e as 13 estatísticas CloudWatch foram conferidos; logs Gateway chegaram parcialmente. A correção local foi validada posteriormente; publicar a API antes de repetir os critérios afetados. Experimentos com interrupção e disparos dos alertas não foram executados.

## 1. Situação de partida e ordem

| Entrega | Evidência disponível | O que falta nesta rodada |
|---|---|---|
| OTLP API/Lambda, X-Ray e CloudWatch | [Conferência de 18/10](VALIDACAO_CORRELACAO_HOM_2026-10-18.md) | Revalidar as revisões novas e os acessos às integrações |
| Métricas Kubernetes | [Conferência de 17/10](VALIDACAO_KUBERNETES_HOM_2026-10-17.md) | Conferir continuidade, enriquecimento e recursos do Collector |
| OS, notificações e referência SQL | [Conferência funcional de 17/10](VALIDACAO_HOM_2026-10-17.md) | Repetir cenários controlados com contagens/durações da nova revisão |
| Três dashboards | Publicados e conferidos com histórico; atualização de operação pela run `39303143995`, sem diferenças posteriores | Publicar os quatro painéis novos de integrações e conferir com dados novos |
| Cinco alertas | Publicados pela run `39314212127`; dez condições hom/prd e roteamento conferidos | Disparo, recebimento e encerramento reais ainda não comprovados |
| Contadores PostgreSQL/SSM/Secrets Manager | [Implementação e regras](METRICAS_INTEGRACOES.md); 1.187 testes API e 245 Lambda, sem falhas/ignorados | Implantação, ingestão e interpretação dos pontos reais |

O mantenedor está provisionando o ambiente. **Não disparar workflows em paralelo a essa atividade.** Quando ele informar a conclusão:

1. Registrar os resultados do provisionamento, endpoint atual, revisões e versões efetivamente implantadas.
2. Confirmar que `api-deploy` incluiu as métricas novas; `auth-publish`/`auth-provision` publicaram a revisão correspondente; o Gateway consome a nova **versão numérica** da Lambda.
3. Confirmar `newrelic-provision` com o dashboard atualizado. Para esta entrega, o plano esperado altera o dashboard de integrações; não cria alertas novos nem altera os outros dashboards.
4. Se o ambiente inteiro estiver sendo recriado, seguir o mapa de provisionamento existente. As métricas novas, isoladamente, não exigem alteração de IAM, Collector ou reaplicação de base/banco.
5. Executar as etapas 2–5 abaixo. Só depois considerar os cenários com interrupções, mediante autorização específica. Não descartar recursos antes da coleta final.

Não fixar o endpoint antigo ou a versão 8 da Lambda como se fossem os atuais. Usar os summaries/contratos da implantação desta rodada. Os toggles antigos de telemetria/X-Ray da Lambda foram removidos; não orientar seu preenchimento.

## 2. Preparação e evidências

Registrar antes das chamadas:

- Início/fim UTC da sessão e de cada cenário; para indicadores diários, usar também `America/Sao_Paulo`, início inclusivo/fim exclusivo.
- SHAs da API/autenticação, digest da imagem, versão numérica da função e revisão dos manifestos/dashboard.
- Namespace/Deployment/pod/nó do Collector, estado das probes, reinícios, réplicas desejadas/disponíveis, HPA e baseline de consumo.
- IDs dos workflows/planos aprovados pelo mantenedor; não executar novo apply para conferir.
- Janela absoluta no New Relic. Comparações devem usar a mesma janela, ambiente, serviço e revisão, sem misturar execuções anteriores.

Usar dados fictícios autorizados e os requests Postman versionados. Tokens/credenciais ficam somente em memória; não registrar senhas, chave de ingestão, conteúdo de Secrets ou respostas contendo JWT. Não pedir uma chave nova: usar a sessão New Relic e o acesso AWS já autorizados; autenticação expirada precisa ser restabelecida pelo fluxo próprio.

Guardar evidências sanitizadas em `artifacts/e5-validacao-integrada/<data-hora-UTC>/`, sem scripts novos: resumos HTTP sem corpos sensíveis, consultas/janelas, contagens, IDs técnicos, snapshots de recursos e resultados por cenário. Registrar quantidade observada, não somente capturas de tela. O relatório final deve ser versionável sem esses artefatos privados.

Status de cada cenário: **aprovado**, **falhou**, **não executado** ou **inconclusivo**, sempre com motivo. Ausência de sinal não equivale a zero ou aprovação. Consulta prd sem séries não comprova operação de prd.

## 3. Saúde, sinais técnicos e autenticação

| ID | Procedimento | Resultado esperado / evidência |
|---|---|---|
| V01 | Conferir Deployment/pods da API/Collector, HPA, probes internas e RDS; ler somente logs da revisão nova | Recursos prontos, sem novos erros de RBAC/TLS/Secrets/exportação; registrar consumo e reinícios. Não inferir capacidade de carga dessas amostras |
| V02 | Login Admin/Mechanic; uma consulta permitida; ausência de token, perfil errado e login inexistente | Respostas conforme o contrato (200/401/403); HTTP, métricas, spans e logs correspondentes. Negativa de negócio não vira falha de integração por definição |
| V03 | Chamar validação Customer pelo Gateway: elegível, não elegível, documento inválido e corpo inválido; repetir chamada válida com runtime aquecido | Contratos e categorias atuais preservados; invocação → PostgreSQL → logs correlacionados. Corpo inválido não inicializa o runtime. Documento inválido pode inicializar o runtime, mas não consultar clientes |
| V04 | Comparar `service.name`, `service.version`, ambiente, IDs de trace/span e dados Kubernetes | API/auth identificadas corretamente; API enriquecida com pod/namespace/Deployment, sem atribuir métricas de nós ao serviço da API. Nenhum UUID/documento/placa nas dimensões |
| V05 | Consultar OTLP da janela, incluindo probes e negativas | Sem spans de probes/SQL associado; métricas de saúde preservadas. Sem SQL, parâmetros, URL/query recebidos, JWT, mensagens originais de exceção ou stack trace. Native X-Ray tem política distinta e não entra nessa conclusão |

Não obter volume HTTP por contagem de spans/logs. Usar `http.server.request.duration`, excluindo as três probes como no dashboard. Não exigir uma quantidade fixa de conexões por resposta 400: a resolução de dependências pode abrir conexão antes da validação; contar somente o acesso realmente ocorrido.

## 4. Novas métricas das integrações — prioridade da rodada

| ID | Procedimento | Critério |
|---|---|---|
| V06 | Consultar a janela desde o startup da API e a primeira inicialização efetiva da Lambda | `ssm/get_parameters/bootstrap` e `secretsmanager/get_secret_value/bootstrap` presentes com a revisão correta. Uma inicialização bem-sucedida realiza uma chamada lógica SSM e duas a Secrets; tentativas de inicialização adicionais devem ser explicadas |
| V07 | Comparar inicialização com chamadas aquecidas | Runtime reutilizado não repete o bootstrap por request. Retentativas internas do SDK permanecem dentro de uma operação lógica, sem pretender reconstruir tentativas físicas |
| V08 | Executar consultas HTTP que acessam o banco e acompanhar os ciclos de OS | `postgresql/connection.open` e `command.execute` com purpose `application`; begin/commit/rollback quando efetivamente usados. Abertura é aquisição lógica, inclusive pool; não precisa corresponder a um span ou conexão física novos |
| V09 | Chamar readiness e concluir uma etapa que mede duração pelo histórico | Conexão da probe em `readiness`; conexão/comando da leitura do histórico em `telemetry`. Nenhum desses pontos entra nos painéis principais application/bootstrap |
| V10 | Conferir as quatro visualizações novas no ambiente/período/revisão corretos | Última amostra, total, falhas/timeouts e percentual coerentes. Cancelamentos ficam separados; denominador ausente permanece sem percentual. Uma exceção SQL pode virar conflito HTTP e não é, por si só, indisponibilidade do banco |

Exemplo de consulta de conferência para a API; substituir o SHA e repetir para `mecanica-auth` com seu próprio SHA:

```sql
SELECT sum(`mecanica.integrations.operations`)
FROM Metric
WHERE metricName = 'mecanica.integrations.operations'
  AND `service.name` = 'mecanica-api'
  AND `service.version` = '<SHA-DA-API>'
  AND `deployment.environment.name` = 'hom'
FACET integration, operation, purpose, outcome
LIMIT 100
```

Usar período absoluto no Query Builder. Fazer uma consulta adicional restrita a `purpose IN ('readiness', 'telemetry')`, e outra a `outcome='canceled'`, conforme [as regras](METRICAS_INTEGRACOES.md). Os painéis somam versões, mas a investigação desta implantação precisa separá-las.

Não provocar falha de startup ou indisponibilidade AWS para preencher o painel nesta etapa. As negativas de configuração e o descarte limitado já foram testados localmente; comprovação implantada de falhas fica no bloco autorizado. Um comando bem-sucedido seguido de erro de domínio, ou resposta AWS válida seguida de configuração inválida, não deve ser reclassificado como falha do acesso.

## 5. Fluxos de OS, dashboards e coleta AWS

| ID | Procedimento | Critério |
|---|---|---|
| V11 | Criar uma OS aprovada, completar diagnóstico, aprovação Customer, execução, conclusão e entrega | Uma criação; uma amostra de diagnóstico, uma de execução, uma de espera por entrega/ServiceCompleted e uma de serviço total. Tempo total termina na conclusão, antes da entrega |
| V12 | Criar outra OS, concluir diagnóstico, recusar orçamento e entregar | Uma criação e diagnóstico; espera por entrega/BudgetRejected separada. Sem amostra de execução/serviço concluído e sem fabricar duração zero |
| V13 | Criar e excluir uma terceira OS; consultar histórico por acesso SQL autorizado, se disponível | Criação não é decrementada; histórico permanece após exclusão individual. Sem sessão SQL, registrar reconciliação como não executada, nunca como aprovada |
| V14 | Repetir negativas de vínculo/transição/payload pertinentes aos fluxos | 400/401/403/404/409/422 esperados não aumentam `mecanica.orders.processing.failures`; conferir ausência de efeitos indevidos no estado/histórico/estoque |
| V15 | Conferir notificações dos fluxos, dashboard de ordens e SQL de leitura | Quantidade de eventos calculada pelas transições realmente executadas; modo simulated em hom, sem tentativa SMTP. SQL é referência para contagens e instantes; durações OTLP em segundos, widgets de OS em minutos |
| V16 | Conferir dashboard de operação e Kubernetes | HTTP por Metric, latência em ms; CPU em cores, memória em MiB; réplicas/readiness/reinícios por recurso. Comparar com estado/metrics Kubernetes, respeitando janelas e definições diferentes de memória |
| V17 | Conferir CloudWatch e dashboard de integrações | As 13 métricas/estatísticas previstas e dados auth/notificações continuam chegando; comparar períodos completos de 60 s com AWS, após atraso de dez minutos mais publicação/coleta/exportação |
| V18 | Comparar request IDs Gateway na origem e no New Relic, com IDs originais dos eventos | Quantificar enviados/encontrados/ausentes/duplicados. Não transformar a cobertura histórica 19/28 em garantia atual, nem ausência momentânea em perda definitiva |
| V19 | Conferir parent IDs e traces OTLP/nativos das mesmas chamadas amostradas | Propagação Gateway → API/Lambda → PostgreSQL preservada. IDs nativos/OTLP diferentes no New Relic continuam registrados como limite, não árvore única comprovada |

Antes de cada fluxo, capturar baseline; não somar todo o ambiente como se o tráfego fosse exclusivo. Usar o [SQL já documentado](OBSERVABILIDADE_NEGOCIO.md#conferência-sql), com datas da rodada. Médias são soma/quantidade de amostras, não média de médias entre réplicas. Dias parcialmente selecionados representam períodos parciais. Diferenças na fronteira das janelas OTLP não são automaticamente erro de negócio.

Se for necessária a conexão SQL temporária, revisar a forma de acesso e autorizar sua abertura separadamente. Manter TLS, leitura apenas para conferência e resolver credenciais no ambiente autorizado, sem expô-las. Remover pod/port-forward próprios ao terminar. Não modificar status/cadastros reais para fabricar resultados.

CloudWatch usa gauges de estatísticas por minuto, não contadores cumulativos. Não somar médias para obter latência total nem calcular percentis sobre médias. Usar consultas existentes dos dashboards; aguardar de forma espaçada, sem polling contínuo. X-Ray tem polling e amostragem próprios; uma chamada OTLP pode não possuir segmento nativo.

## 6. Cinco alertas — conferir antes de induzir qualquer falha

Ler as dez condições hom/prd e comparar com as definições Terraform publicadas. Destino/canais/workflows devem estar ativos, restritos às policies corretas, notificando somente abertura/encerramento e respeitando silenciamento. Usar o endereço configurado por `NEW_RELIC_ALERT_EMAIL`, sem versioná-lo neste roteiro. Destino ativo ou mensagem de teste não comprova disparo da condição; recebimento precisa ser confirmado pelo destinatário.

| ID | Alerta existente | Regra que precisa ser preservada | Comprovação operacional |
|---|---|---|---|
| A01 | Falha técnica de OS | Soma ≥1; janela 300 s, event_timer 60 s, AT_LEAST_ONCE | Falha técnica de uma ação de OS produz HTTP 500 e incremento da métrica; incidente/e-mail de abertura e encerramento |
| A02 | Falha de notificação de OS | Soma de outcome failure ≥1; mesmos parâmetros de A01 | Falha do handler após commit incrementa notifications/failure; OS permanece persistida. Não afirmar entrega SMTP no modo simulated |
| A03 | CPU elevada do nó | Média usada / alocável mais recente da janela >80%; 60 s/event_flow/atraso 60 s, ALL por 300 s; por nó | Comparar as cinco janelas e a faceta do nó, incidente e e-mail se houver violação real. Exatamente 80% e pico isolado não bastam |
| A04 | Memória elevada do nó | Working set médio / alocável mais recente >85%; mesmos tempos de A03; por nó | Mesma conferência, sem equiparar a capacidade física, OOM ou MemoryPressure. Exatamente 85% não viola |
| A05 | API sem réplica disponível | max(available)<1; janela 60 s/event_flow/atraso 60 s, ALL por 120 s; Deployment exato | Deployment existente medindo zero em duas janelas; Collector permanece ativo. Exclusão do Deployment/cluster não substitui zero |

Todos: sem preencher lacunas; expiração de 600 s encerra sem abrir por ausência; limite nativo de 30 dias. Registrar se o encerramento ocorreu por retorno ao limiar ou por expiração. Se continuarem chegando pontos zero, o encerramento por limiar não comprova a regra de ausência de sinal. Encerrar por expiração não comprova recuperação.

**Não há alerta novo nesta entrega.** Para CPU/memória, validar configuração/medição não é o mesmo que provar disparo. Não criar teste de carga, estressar o nó, emitir métricas sintéticas ou baixar limiares para declarar sucesso. Sem violação real observada, marcar o disparo/recebimento como **não executado**, deixando eventual experimento para autorização e planejamento próprios.

## 7. Interrupções e falhas controladas: autorização antes de executar

Preparar cada experimento isoladamente: ambiente/recursos afetados, ação exata, janela máxima, pré-condições, restauração exata e critério de abortar. Obter autorização para esse experimento; aprovar este roteiro não aprova automaticamente executar as alterações abaixo. Não encadear experimentos enquanto houver estado degradado.

| Experimento | Preparação e observação | Restauração / critério de interrupção |
|---|---|---|
| F01 — falha técnica de OS | Escolher acesso real de persistência que possa falhar reversivelmente numa ação de OS. Revisar o mecanismo antes; não usar payload inválido, erro de domínio ou 5xx do Gateway como substituto de HTTP 500 da aplicação | Restaurar dependência, comprovar nova operação bem-sucedida e ausência de alteração parcial no histórico/estoque. Sem mecanismo isolável e restauração verificável, não executar |
| F02 — falha da notificação | Identificar consulta/montagem posterior ao commit e revisar como causar falha somente nessa fase. Não criar SMTP AWS ou endpoint de falha | OS/transição confirmadas devem permanecer; restaurar a dependência e validar uma notificação nova. Não prometer reenvio da notificação falha. Se a falha atingir a transação antes do commit, não comprova A02 |
| F03 — zero réplicas | Registrar Deployment/HPA e estado anterior; manter Deployment e Collector existentes; revisar como impedir que reconciliação invalide a janela. Depois de autorizado, medir zero por dois minutos | Restaurar imediatamente réplicas/configuração originais, aguardar Ready e probes. Não excluir Deployment nem alterar HPA sem autorização. Durante a janela, validar a Lambda via Gateway/RDS pode demonstrar independência do EKS, mas não gera falha de processamento de OS dentro da API |
| F04 — restart/lookback do Collector | Selecionar eventos Gateway fictícios com timestamp na janela anterior de cinco minutos, confirmar existência na origem e autorizar restart; comparar antes/depois por ID de evento | Aguardar rollout/Ready; registrar retomada, duplicações/lacunas e ausência de erros novos. Não exigir deduplicação ou recuperar eventos fora da janela; não acrescentar checkpoints |
| F05 — exportação indisponível | Revisar uma interrupção curta e reversível do caminho de telemetria, independente das dependências de negócio, com limite de tempo; não derrubar RDS ou remover a credencial para simular falha do destino | Confirmar continuidade HTTP/transações/readiness e exportação após restauração. Não provocar esgotamento de fila/carga. Perda possível é limite aceito, não promessa de replay |
| F06 — falhas de bootstrap AWS | Se necessário para completar os resultados failure/timeout/canceled, revisar método reversível por consumidor e a restauração antes de autorizar; não alterar IAM/Secrets automaticamente | Manter Collector saudável para observar a tentativa final. API não deve aceitar tráfego sem configuração válida; bootstrap Lambda deve permitir nova tentativa. Sem segurança de restauração, manter somente a evidência local e registrar a pendência implantada |

O teste de zero réplicas interrompe o tráfego da API e pode produzir 5xx do Gateway; isso não comprova A01. Na manutenção planejada fora do experimento de A05, usar silenciamento manual. No experimento de A05, verificar que o silenciamento não impede justamente a notificação que se deseja comprovar; não removê-lo sem combinar a janela.

Falhas PostgreSQL/SSM/Secrets devem ser classificadas pelos acessos realmente concluídos. Falhas tratadas de notificação não revertem a OS. Falhas técnicas de medição pós-commit ficam em `mecanica.orders.telemetry.failures`, separadas do processamento HTTP e da integração que falhou. Não provocar todas as combinações só para preencher gráficos.

## 8. Lambda, capacidade e encerramento da sessão

Conferir a primeira invocação isolada da nova versão e chamadas aquecidas: resposta correta, três sinais recebidos sem depender de uma segunda chamada para exportar, identidade/correlação e reports de tempo/memória da função mais extensão. Inicialização fria precisa de evidência no report; esperar um tempo arbitrário não comprova congelamento/descongelamento. Sem evidência do ciclo, registrar essa limitação em E5.13.

Comparar consumo do Collector/API e métricas do nó antes/depois dos fluxos. Kubeletstats cobre o nó do Collector; não afirmar cobertura de todos os nós transitórios. Não realizar teste de carga ou declarar capacidade/HPA sob saturação. Conferir no New Relic ingestão usada/limite vigente, filtros, cardinalidade e retenção configurada; 100 GB informados não significam bloqueio automático de custos. Retenção de sete dias vale para os grupos CloudWatch enquanto existirem, não automaticamente para o armazenamento New Relic.

Antes de o mantenedor descartar o ambiente: encerrar tráfego, aguardar os períodos finais de coleta/exportação (inclusive atraso CloudWatch), guardar snapshots e conferir os últimos pontos. Registrar até que instante a entrega foi observada; não prometer completude apenas porque aguardou dez minutos. Destruir Gateway remove seu grupo; destruir banco encerra acesso ao histórico. Não executar destroy como parte deste roteiro sem pedido explícito. Seguir o mapa de descarte já existente.

## 9. Fechamento do relatório e do checklist

Preencher uma linha por ID V01–V19, A01–A05 e F01–F06 com status, janela, revisão, expectativa, resultado, evidência e restauração (quando aplicável). Para prd, registrar separadamente o que foi executado; não transportar resultados hom como prova de prd.

O relatório deve distinguir:

1. Implementação/testes locais concluídos: 1.187 testes API e 245 Lambda, builds/publish/SAM/Kustomize/Terraform/OpenAPI já conferidos.
2. Publicação/conferência históricas dos três dashboards e cinco alertas, sem confundir com execução dos disparos.
3. Comprovação desta revisão com dados novos e confirmação de recebimento pelo destinatário.
4. Cenários não executados ou inconclusivos, incluindo falta de autorização, falta de violação real ou ausência de acesso SQL.
5. Pendências separadas: união automática dos IDs X-Ray/OTLP e cobertura parcial dos logs Gateway. Os dados históricos não justificam inventar spans, alterar IDs, duplicar coleta ou ampliar permissões como correção automática.

Atualizar os checkboxes somente no acompanhamento local `docs/plano-fase-3/etapas/E5.md` (fora do versionamento no fluxo atual), sem marcar E5 inteira por existirem configurações ou por ausência de erros. Evidência anterior continua válida para a revisão em que foi produzida; não é uma nova regressão desta implantação. Nenhum novo fluxo foi executado ao preparar este roteiro.
