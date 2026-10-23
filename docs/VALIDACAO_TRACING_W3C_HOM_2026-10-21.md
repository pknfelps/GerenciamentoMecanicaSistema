# Validação pública de tracing W3C — hom — 21/10/2026

**Caminhos públicos Gateway → API e Gateway → Lambda aprovados no escopo da política W3C.** A recriação do ambiente publicou a Lambda 12 e atualizou a integração do Gateway. A aplicação continua usando OTLP; X-Ray/polling permanece uma fonte independente. Isso não comprova união entre as duas árvores nem conclui integralmente a E5.

## Implantação e procedimento

| Item | Evidência |
|---|---|
| Conta/região | `121754142617` / `us-east-1` |
| Endpoint | `https://18af09dzw7.execute-api.us-east-1.amazonaws.com/hom` |
| Stage/deployment | `hom` / `ywot12`; X-Ray ativo, execution logs OFF e data tracing desabilitado |
| Integração de autenticação | AWS_PROXY, URI qualificada `mecanica-hom-auth:12/invocations` |
| Lambda | `12`, `bf879cfe5491d5966b556de930c6ca52ed489060`, dotnet10, Active, telemetria habilitada |
| API / service.version | `9d1c0625e7c0ada58a110e2f833b21853d3a0b61` |
| Pod identificado nos spans | `deploy-gerenciamento-api-7b4b6f79db-wk8qs` |

Rodada principal entre **13:07–13:12 UTC (10:07–10:12 em São Paulo)**, com consultas posteriores para permitir ingestão. Houve vinte chamadas públicas: nove respostas 200, oito 401, uma 400, uma 403 e uma 404; nenhum 500. Foram usados somente os seeds educacionais e dados fictícios previamente autorizados. Tokens foram consumidos em memória, sem impressão ou persistência. Nenhum cadastro, OS ou recurso AWS foi alterado pelo agente.

A sessão AWS do conector foi renovada e permitiu consultar implantação, CloudWatch e X-Ray. A sessão AWS CLI permaneceu expirada, e o kubeconfig anterior aponta para o cluster removido: não houve leitura direta dos pods/logs do EKS nesta rodada. Disponibilidade e enriquecimento Kubernetes abaixo são evidências recebidas no New Relic, não saída de kubectl.

## Propagação, PostgreSQL e logs

Sete chamadas à autenticação de cliente retornaram 200, com os cenários abaixo; a mesma matriz no login interno inexistente retornou sete 401.

| Cenário | Resultado nos dois serviços |
|---|---|
| Sem contexto | Raiz própria, sem pai |
| Somente X-Ray | Raiz própria; X-Ray não fornece pai OTLP |
| W3C válido | Trace `5a86bd3713d3478aa6ad6602eaeb0969`, pai `0000000000000202` |
| W3C e X-Ray diferentes | Trace W3C `7d7bb068449547c4b6511200e393136f`, pai `0000000000000303` |
| W3C inválido com X-Ray válido | Raiz própria, sem fallback X-Ray |
| W3C não amostrado | Trace `154208db9c264ef88cb43ddad42732f9`, pai `0000000000000404`; OTLP recebido |
| Nova chamada sem contexto | Nova raiz, sem reutilizar a anterior |

Os pais W3C são fictícios e não foram exportados pelo cliente; sua ausência como ancestral externo é esperada. Os mesmos contextos foram usados nos dois serviços para conferir a extração, sem afirmar uma chamada interna Lambda → API.

A consulta isolada encontrou **45 spans: vinte HTTP/invocação e 25 PostgreSQL**, com 25 vínculos corretos de trace/pai e zero spans SQL sem o respectivo pai da aplicação. As oito invocações da Lambda produziram 22 spans; as doze requisições da API produziram 23. Os contextos nativos de runtime estavam presentes e não foram adotados como pais OTLP.

A interface Distributed tracing confirmou o trace sem contexto `6bd3cd9913777759c58be6a97b01d6e3`: **três spans**, mapa mecanica-auth → postgresql, dois acessos PostgreSQL, um log e ausência de Fragmented/Missing parent. O span de invocação `23621fd70741b012` é raiz. A API já tinha sua árvore de raiz própria conferida visualmente na [rodada anterior](VALIDACAO_TRACING_W3C_HOM_2026-10-20.md); os vínculos da revisão atual foram reconferidos nesta amostra.

Foram recebidos **nove logs de aplicação**: oito conclusões da Lambda e um 404 da API, todos com trace.id/span.id correspondentes aos spans consultados. O 404 usa mensagem fixa, tipo técnico e modelo `Orders/{id}/status`. Os 45 spans inspecionados não contêm SQL, parâmetros, URL/query brutos, corpos, headers, baggage/tracestate, stack trace ou os marcadores fictícios enviados. Isso não amplia a prova para exceções técnicas não provocadas nesta rodada.

## Métricas e execução gerenciada

A conferência final sem FACET registrou **oito invocações e oito amostras de duração**: sete authenticated e uma invalid-request. O corpo inválido foi a primeira chamada da rodada e retornou 400; platform.report registra sucesso, inicialização de 987,914 ms, memória máxima de 148 MB e versão 12. As chamadas seguintes reutilizaram o mesmo stream gerenciado, com memória máxima observada de 185 MB. Reutilização não comprova diretamente todos os estados internos de congelamento/descongelamento.

Integrações da Lambda: **dezessete operações**, com sete connection.open, sete command.execute, um get_parameters e dois get_secret_value. O bootstrap foi reutilizado. Os logs lidos não apresentaram erro de exportação; há três avisos sobre o alias depreciado `otlphttp`, um por sinal.

Na janela principal, as métricas da API registraram **90 readiness e trinta liveness, todas 200**, sem spans health na amostra de traces. Startup ocorreu fora desse intervalo. As métricas Kubernetes posteriores reportaram API e Collector com desejadas/disponíveis 1/1, CoreDNS e Metrics Server 2/2; o enriquecimento dos spans preservou serviço, versão, pod, namespace e Deployment.

Em complemento às **13:26 UTC**, três chamadas comprovaram o uso do JWT da Lambda nova na API: autenticação 200, consulta de OS inexistente 404 e GET /Users 403 para Customer. Essas chamadas ficam fora dos totais da janela principal. Os logins Admin/Mechanic da rodada principal retornaram 200; Admin recebeu 403 na consulta de status, conforme o contrato, e Mechanic recebeu 404. GET /Users sem token retornou 401.

## CloudWatch e X-Ray independentes

As treze estatísticas abaixo coincidiram entre AWS e New Relic. A comparação respeitou o timestamp nativo da fonte: Gateway em **13:10–13:11 UTC**; Lambda/RDS em **13:09–13:10 UTC**. Ausência inicial de Gateway na consulta do minuto 13:09 não foi tratada como falha do Collector: o CloudWatch publicou os pontos em 13:10.

| Fonte | Estatísticas comparadas |
|---|---|
| Gateway | Count/SampleCount 14; 4XXError/Sum 7; 5XXError/Sum 0; Latency/Average 322,142857 ms; IntegrationLatency/Average 318 ms |
| Lambda | Invocations/Sum 7; Errors/Sum 0; Throttles/Sum 0; Duration/Average 636,038571 ms; ConcurrentExecutions/Maximum 1 |
| RDS | CPUUtilization/Average 5,088105%; FreeableMemory/Average 192.884.736 bytes; DatabaseConnections/Average 1 |

AWS X-Ray confirmou segmentos de Gateway, Lambda e Function para as chamadas controladas. O polling também recebeu os traces nativos `1-6ad8b90d-5becc72f0d20799756c7847c` e `1-6ad8b99b-05be987f31b66dfc29a33bf4`. Seus IDs são distintos das raízes OTLP; não há expectativa de união automática. Os segmentos nativos não passam pela sanitização da aplicação.

## Limites e pendências preservadas

- Logs Gateway: **vinte eventos na AWS e quinze no New Relic**, na janela principal. Os quinze request IDs e IDs originais coincidem, com mensagem fixa, campos técnicos e sem duplicação nessa amostra. `id` preserva o ID CloudWatch; `messageId` é outro campo e não foi usado como substituto. Os cinco eventos das chamadas funcionais não foram encontrados nessa consulta. A lacuna anterior por eventos tardios está documentada; esta rodada não atribui automaticamente a mesma causa aos cinco eventos novos, não recupera dados nem altera o receiver.
- NrIntegrationError identificou descarte de pontos de `dotnet.thread_pool.thread.count`, com diagnóstico de valores monotônicos não positivos. Não é evidência de perda das invocações, cuja contagem foi reconciliada; registrar investigação separada, sem alterar coleta durante a validação.
- Falha de bootstrap e cancelamento da API têm implementação e comprovação local registradas; seus caminhos de falha não foram repetidos na AWS. Prd, falhas adicionais de transações/abertura Lambda, limites completos do ciclo gerenciado e cenários de destino mantêm as limitações anteriores.
- Disparos CPU/memória e teste de carga continuam dispensados pelo mantenedor. Não foram repetidos experimentos de falha, alertas, restart, replay ou conferência SQL direta. Os fluxos de OS e alertas comprovados anteriormente não ganham nova comprovação por esta rodada de tracing.

**E5.3: política W3C comprovada pelos caminhos públicos de hom.** E5.2/E5.8/E5.13/E5.14 preservam os limites específicos. Evidências técnicas sanitizadas em `artifacts/e5-public-w3c-20261021`; somente documentação/checklist foram atualizados.
