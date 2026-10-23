# Validação de tracing W3C — hom — 20/10/2026 (São Paulo)

**Registro da etapa inicial; situação atualizada na [validação pública de 21/10](VALIDACAO_TRACING_W3C_HOM_2026-10-21.md).** Na consulta inicial, a API foi aprovada e a Lambda nova ainda não estava implantada. A revisão nova da API gerou raízes próprias sem W3C válido, continuou os pais W3C recebidos e manteve PostgreSQL/logs correlacionados. A interface New Relic confirmou uma árvore da aplicação sem Fragmented/Missing parent.

Naquela consulta, a AWS apresentava somente `$LATEST` e a versão numérica **10** da autenticação, ambas da revisão antiga `0446a19b4888921f1d9b4032d034de7d4009ab24`, e o Gateway apontava para `mecanica-hom-auth:10`. A versão 11 foi conferida diretamente depois, mas o registro final foi interrompido. Após recriar o ambiente, a versão 12 da mesma revisão nova foi conferida pelo endpoint público no relatório de 21/10. Publicar um artefato em auth-publish, isoladamente, não implanta a função.

## Revisões e escopo

| Item | Conferência |
|---|---|
| Conta/região | `121754142617` / `us-east-1` |
| Gateway | `v5qvpym6i7`, stage `hom`, X-Ray habilitado |
| Endpoint | `https://v5qvpym6i7.execute-api.us-east-1.amazonaws.com/hom` |
| API / service.version | `9d1c0625e7c0ada58a110e2f833b21853d3a0b61` |
| Imagem | `sha256:f1785f86adccb6abcfd6dba094078e9baad151c8b712a5307546accfa9afe92b` |
| Pod | `deploy-gerenciamento-api-5ccbbdc95c-5hr2b`, Ready, zero reinícios |
| ConfigMap | `mecanica-api-observability`, `InboundFormats=["tracecontext"]` |
| Lambda / Gateway | Versão 10 antiga; URI da integração conferida pela API AWS |

Conferência realizada pela sessão AWS válida, kubeconfig existente e sessão autenticada New Relic US `8626152`. A matriz HTTP foi enviada em **21/10/2026, 00:31 UTC**, equivalente a **20/10, 21:31 em São Paulo**. A consulta dos seus spans usou a janela absoluta **00:30:55–00:32:30 UTC**. O log de OS foi exercitado às 00:34 UTC.

Somente leituras de infraestrutura e chamadas HTTP com dados educacionais/fictícios previamente autorizados. Não houve provisionamento, deploy, restart, alteração de permissões, gravação de cadastro/OS, leitura de Secret ou envio de notificação. Tokens foram tratados somente em memória, sem impressão ou persistência.

## Matriz de propagação da API

As oito chamadas de login inexistente retornaram 401. O login Admin adicional retornou 200, e a consulta de usuário inexistente retornou 404. Foram encontrados **20 spans na janela isolada: dez HTTP e dez PostgreSQL**, todos da revisão/pod novos. Cada span SQL tem o mesmo trace e o ID do respectivo span HTTP como pai; zero filhos SQL sem pai HTTP correspondente.

| Cenário | Trace OTLP | Pai HTTP |
|---|---|---|
| Sem contexto | `e2b2ea735939230729d43279b124f8fa` | ausente |
| Somente X-Ray | `a7366aa0238b26e8d3ea7919af4e419c` | ausente |
| Somente W3C | `a9677c70abf549e2afb91c9455749ebc` | `0000000000000202` |
| W3C + X-Ray diferentes | `0ef24ff2c5914102909145a63ef04194` | `0000000000000303` |
| W3C inválido + X-Ray | `e9a064026918dc08490a369af17cdbf3` | ausente |
| W3C não amostrado | `53cc9d41912948f88ed708acefbf4670` | `0000000000000404` |
| Nova chamada sem contexto | `692cdc1ab271266067262f30618dc00a` | ausente |
| W3C multivalorado/ambíguo + X-Ray | `315a58c91aad209b9cc7a4f8e711a2ea` | ausente |

Os três pares trace/pai W3C coincidem exatamente com os valores enviados. Os cinco cenários sem W3C válido geraram raízes distintas; nenhum herdou o trace/pai X-Ray ou o contexto anterior. O pai W3C não amostrado continuou produzindo OTLP. A verificação foi sequencial; concorrência desta revisão não foi provocada na AWS, permanecendo coberta pelos testes locais.

O trace sem contexto `e2b2ea735939230729d43279b124f8fa` foi aberto na interface Distributed tracing: **Spans: 2**, mapa mecanica-api → postgresql, sem Fragmented/Missing parent. O span HTTP `a62a17f68678cedc` é a raiz, e SQL `c75f9d5f4464559b` é seu filho.

Os pais dos testes W3C são fictícios e não foram exportados como spans externos. A ausência desses ancestrais na interface é esperada; não utilizar esses cenários para exigir árvore externa completa. X-Ray/polling permanecem uma fonte independente, conforme a [decisão vigente](TRACING_W3C_E_XRAY.md).

O Root automático do Gateway da primeira chamada, `1-6ad807cf-06c4f7e506222fee434b73d5`, chegou ao New Relic por `xray.polling`: **três spans e três IDs distintos**. Seu trace OTLP é a raiz própria `e2b2ea735939230729d43279b124f8fa`, sem união pretendida. Uma consulta ao Root fornecido manualmente não retornou spans naquele momento; não foi usada para concluir falha de configuração ou perda. A referência positiva desta conferência é o trace automático, preservando a amostragem nativa independente.

## Logs e proteção dos dados

Uma consulta somente de leitura ao status de OS inexistente retornou 404 e exercitou o tratamento de NotFoundException. Console do pod e Log no New Relic contêm a mensagem fixa, tipo, status e modelo `Orders/{id}/status`, sem UUID no modelo, conteúdo da exceção ou stack trace.

- Trace W3C: `5acaaf7f21834d998418c12712732688`; pai recebido: `0000000000000505`.
- Span HTTP e span.id do log: `3021cac69525d0dc`.
- Span SQL: `83c7139376babe56`, pai `3021cac69525d0dc`.
- RequestId do servidor: `0HNPF14R3FJ35:00000001`.
- Tipo: `Service.Interface.Exceptions.NotFoundException`; mensagem fixa `Erro de aplicação tratado.`.

O keyset dos spans selecionados contém somente campos técnicos e enriquecimento Kubernetes/New Relic; sem SQL, parâmetros, URL/query brutos, headers, baggage, tracestate, documentos, credenciais ou stack trace. Não havia SpanEvent nos traces consultados. Essa ausência não valida falhas técnicas de PostgreSQL não provocadas nesta rodada.

## Métricas e probes

O recorte de métricas da revisão nova apresentou **21 requisições de aplicação**: 16 × 401, três × 200 e duas × 404. Inclui nove chamadas preliminares, a matriz de dez chamadas e as duas chamadas da conferência de log. Na etapa preliminar foi ajustada somente a leitura local do JWT textual; não houve erro/correção na API nem token exposto.

No snapshot da mesma revisão, as métricas registraram 269 readiness, 87 liveness e uma startup, todas 200. A consulta de spans por rota/nome health retornou zero. Assim, as probes continuam sendo medidas sem produzir spans HTTP; a saúde do pod foi confirmada sem interromper o serviço.

## Pendências e limites

1. Pendência de publicação/Gateway desta etapa encerrada na rodada de 21/10: o mantenedor implantou a revisão nova como versão 12 e o agente comprovou sua política W3C pelo caminho público. Nenhum workflow foi executado pelo agente.
2. Falhas de bootstrap e cancelamento da API não foram provocados nesta rodada. Os ajustes estão na revisão publicada e passaram nos testes/Docker locais; a comprovação específica desses cenários na AWS permanece separada. Inicialização normal e 404 não substituem esse teste.
3. Não repetir testes de CPU/memória ou carga dispensados pelo mantenedor. Não declarar prd validado, corrigir automaticamente as lacunas aceitas do Gateway ou retomar a união X-Ray/OTLP retirada do escopo.

Esta etapa comprovou a política da API; a [rodada seguinte](VALIDACAO_TRACING_W3C_HOM_2026-10-21.md) completou a conferência pública da Lambda. E5.2/E5.14 mantêm seus limites específicos. Evidências técnicas desta etapa em `artifacts/e5-w3c-hom-validation`, incluindo resumo HTTP, vínculos verificados e console do pod.
