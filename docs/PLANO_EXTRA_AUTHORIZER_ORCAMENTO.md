# Histórico — Plano extra de Lambda Authorizer

> **SUBSTITUÍDO em 22/10/2026.** A decisão vigente está no [plano de autenticação e envio centralizado](PLANO_EXTRA_AUTENTICACAO_E_EMAIL.md), sequência AE.1–AE.10. Não executar as tarefas LA pendentes abaixo: Authorizer, token de orçamento e acesso vinculado à OS foram abandonados. O conteúdo preserva o desenho anterior e suas evidências locais; não comprova o fluxo novo.

**Data:** 22/10/2026. **Estado:** LA.1 inicial e adaptação token de orçamento + documento implementadas e validadas localmente; emissão pela API e integração Gateway ainda pendentes. Demais etapas pendentes, sem publicação.

Este plano é independente do [plano oficial da Fase 3](../PLANO_FASE_3.md). Usa a sequência própria LA.1–LA.8; não altera os checklists E5/E6/E7, seu aceite ou as validações já registradas. Registrar a decisão não executa implementação, operações AWS ou workflows.

## Objetivo e decisão vigente

Permitir que o cliente aprove ou recuse o orçamento em uma única chamada ao Gateway, apresentando **token de orçamento, CPF/CNPJ e decisão**, sem página de login e sem chamar `/customers/validate` separadamente.

A API emitirá a credencial na resposta da conclusão do diagnóstico que disponibiliza o orçamento, acessível ao Mechanic autorizado. Nos testes internos, o Postman capturará essa credencial e a apresentará na aprovação. Não criar páginas ou envio de e-mail para distribuir o token.

**Decisão do mantenedor em 22/10/2026:** incluir no token o **UUID do cliente e o UUID da OS**. O documento continua obrigatório, mas não será incluído no JWT. A Lambda consultará somente o cadastro do cliente pelo UUID assinado, comparando seu documento com o CPF/CNPJ informado e verificando Active. Não consultará `orders`, não resolverá o cliente a partir da OS e não precisará de novos grants sobre ordens.

Conferir o UUID da OS do token com o UUID da rota é uma validação do escopo da credencial, sem leitura da OS no banco. A verificação autoritativa do vínculo cliente–OS, do estado e da decisão permanece na API, dentro do fluxo transacional existente.

## Situação atual e diferença para o alvo

- `PATCH /Orders/{id}/budget` ainda recebe `customerDocument` e `approved`, exige JWT Customer e responde 204.
- A autenticação HTTP foi adaptada: POST /customers/validate exige credencial de orçamento no Authorization e documento no body. Documento sozinho já não emite uma credencial de acesso; esta mudança ainda não foi publicada.
- A primeira LA.1 implementou o despacho HTTP/REQUEST, validação de documento/elegibilidade, runtime compartilhado e policy restrita. A adaptação posterior já valida token de orçamento + CPF/CNPJ e consulta pelo UUID assinado. Os 320 testes registram a versão inicial; a nova regressão tem 369 testes aprovados.
- SAM/Gateway ainda não ativam esse authorizer. A integração atual da aprovação segue diretamente para a API por HTTP proxy/VPC Link/NLB.
- A API já resolve o cliente pelo UUID autenticado e confere a titularidade. As referências físicas por documento cadastrado permanecem internas.

A posse da credencial e a correspondência do documento não comprovam, por si sós, a identidade civil do portador. A distribuição pelo Postman simula o cenário interno; não será descrita como autenticação por canal verificado ou entrega real ao cliente.

## Credenciais e responsabilidades

| Componente | Responsabilidade |
|---|---|
| API, ao disponibilizar o orçamento | Emitir credencial assinada com UUID do cliente, UUID da OS, finalidade de decisão e expiração; não usar CPF recebido para compor a identidade |
| Lambda Authorizer | Validar assinatura, emissor, audiência, finalidade/tipo e validade; comparar OS assinada com a rota; buscar cliente pelo UUID assinado; validar documento e status Active |
| Gateway | Executar o authorizer sem cache e substituir o Bearer de entrada pelo JWT interno autorizado para a integração |
| API, ao decidir | Validar JWT interno, cliente e escopo da OS/ação; conferir titularidade e estado atuais; confirmar a decisão em transação |

A credencial pública de orçamento e o JWT interno Customer terão finalidades distintas. O token público não deverá ser aceito diretamente como JWT genérico da API; um JWT comum de usuário/cliente também não substituirá a credencial de orçamento na Lambda. Validar essa separação explicitamente por audiência/finalidade/tipo, sem alterar o pacote Auth.Contracts. O contrato de validação já foi fixado: HS256, typ=mecanica-budget+jwt, iss do ambiente, `aud=<audience do ambiente>/budget-decision`, sub=UUID do cliente, order_id=UUID da OS, purpose=budget-decision e exp obrigatório, sem tolerância adicional no vencimento. A validade emitida será definida junto ao emissor na LA.3; o JWT interno expira no menor prazo entre dez minutos e a credencial pública. [Contrato completo](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/blob/develop/docs/CREDENCIAL_ORCAMENTO.md).

O JWT interno carregará o UUID do cliente e o escopo da OS/ação autorizado, sem CPF. A API não confiará em UUIDs enviados em headers públicos. Onde um JWT Customer restrito à OS for aceito, o backend deverá respeitar esse escopo; ele não dará acesso às demais ordens do mesmo cliente.

Não haverá armazenamento de tokens, revogação individual, tabela de uso único ou garantia de consumo único do JWT. A rejeição de uma segunda decisão dependerá do estado persistido e do bloqueio transacional atuais. Token ainda válido não permite decidir novamente uma OS que já saiu de WaitingForApproval. Não prometer invalidação criptográfica após uso.

## Contratos desejados

### Disponibilização do orçamento

`PATCH /Orders/{id}/diagnosis/complete`, pelo Mechanic autorizado, passará de 204 para 200, retornando o identificador da OS, a credencial de decisão e sua expiração. Exemplo de campos a consolidar no contrato:

```json
{
  "orderId": "<UUID da OS>",
  "decisionToken": "<credencial de orçamento>",
  "expiresAt": "<instante UTC de expiração>"
}
```

Emitir somente para orçamento disponibilizado com sucesso, sem expor token antes da confirmação da operação. A identidade vem do cliente vinculado à OS no banco, nunca de um UUID arbitrário da requisição. Definir o tratamento de falha de emissão sem informar sucesso falso nem presumir rollback de uma transação já confirmada. O token aparece na resposta protegida, não nos logs; impedir cache dessa resposta. O orçamento/fluxo de notificações existente permanece, com envio AWS simulado.

### Decisão pelo Gateway

```http
PATCH /hom/Orders/{id}/budget
Authorization: Bearer <credencial de orçamento>
X-Customer-Document: <CPF ou CNPJ>
Content-Type: application/json
```

```json
{ "approved": true }
```

CPF/CNPJ somente no header dessa operação; body somente com `approved`. O authorizer REQUEST não recebe o JSON. Os formatos do documento seguem a normalização compartilhada, incluindo máscaras válidas; não enviar documento/token em URL ou query.

A Lambda valida a correspondência **token do cliente A + documento cadastrado de A**. Token de A com documento de B será recusado, mesmo que ambos os cadastros estejam ativos. Também recusa token de outra OS, vencido ou de finalidade incorreta. A comparação com a rota não pressupõe consultar `orders`.

Resposta após commit:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{ "decision": "Approved" }
```

Recusa confirmada retorna `{ "decision": "Refused" }`, também com 200. A decisão não renomeia o estado: aprovação leva a WaitingForExecution; recusa leva a Finished/BudgetRejected.

| Situação | Resposta pública esperada |
|---|---|
| Aprovação ou recusa confirmada | 200 e decisão correspondente |
| Credencial/documento ausente, inválido, ambíguo, vencido ou incompatível; cliente inelegível; token de outra OS | 401 genérico do Gateway, sem chamar a API |
| Falha técnica de bootstrap/SQL/authorizer ou timeout | 5xx do Gateway, sem converter em 401 |
| Corpo inválido após autorização | 400 da API |
| OS ausente ou sem titularidade válida no backend | 404 genérico da API |
| Estado incompatível com a decisão | 422 da API |
| Falha técnica na aprovação | 500 sanitizado da API |

Gateway e API são fronteiras distintas: a saída do authorizer não é APIGatewayProxyResponse e não garante Problem Details da API. Conferir códigos/formatos efetivos na AWS. A API direta continua exigindo JWT interno válido; CPF/header não substitui essa autenticação e a credencial pública de orçamento não será um Bearer genérico do backend.

O body exige booleano explícito: ausência, null ou tipo inválido retorna 400, nunca recusa implícita. O antigo `customerDocument` e demais propriedades extras também serão rejeitados com 400.

### Revisão de `/customers/validate`

Manter a rota, mas revisar o contrato para exigir credencial de orçamento válida **e** documento obrigatório. Documento sozinho não poderá emitir JWT Customer. Reutilizar as mesmas verificações de token/cliente/documento e restringir eventual JWT retornado à OS/finalidade autorizadas. Documentar a mudança incompatível e atualizar seus testes/contrato; a aprovação pelo Gateway não exigirá chamar essa rota separadamente. O contrato HTTP revisado exige Authorization Bearer e body document, sem duplicar documento em header/body. A validação já foi implementada localmente; emissão do token pela API continua pendente.

## Fluxo e fronteira de confiança

```text
Mechanic → API: concluir diagnóstico autenticado
         ← 200 com OS + token de orçamento + expiração
Postman: guardar token para simular sua apresentação pelo cliente
Cliente → Gateway: token Bearer + documento no header + approved no JSON
        → Lambda Authorizer: validar token e OS da rota
        → customers: buscar UUID assinado, comparar documento e Active
        ← Allow exato + principal UUID + JWT interno restrito no contexto
Gateway → API: substituir Authorization pelo JWT interno autorizado
API     → verificar escopo, cliente, vínculo da OS e estado; confirmar transação
        ← 200 { decision: Approved | Refused }
```

A Lambda não consulta ordens, não altera a OS e não chama a API. O Gateway mantém a integração privada atual. Não registrar credenciais, documento, contexto completo ou respostas de emissão em access logs/evidências.

## LA.1 — Autenticação: entrada do authorizer e revisão aprovada

**Versão inicial concluída localmente:**

- [x] Handler REQUEST e entry point de despacho com HTTP, mantendo uma única função e o FunctionHandler HTTP delegado.
- [x] Runtime/providers/data source compartilhados, reutilização, nova tentativa após falha e descarte.
- [x] Leitura de documento sem distinção de maiúsculas e rejeição de valores ambíguos, com normalização compartilhada.
- [x] Elegibilidade por cadastro Active, UUID como principalId, policy limitada ao methodArn e JWT no contexto escalar customerAuthorization.
- [x] Conferência de conta/região/stage/método/rota e falhas técnicas distintas de Unauthorized, com limites atuais de execução/conexão.

**Adaptação concluída localmente para a decisão vigente:**

- [x] Definir contrato comum da credencial de orçamento: assinatura, emissor/audiência/finalidade, UUID do cliente, UUID da OS e expiração. Não alterar Auth.Contracts ou adicionar CPF ao JWT.
- [x] Exigir e validar token Bearer além de X-Customer-Document, rejeitando ambiguidades dos dois headers.
- [x] Comparar UUID da OS assinado com a rota e consultar customers pelo UUID do cliente assinado. Comparar CPF/CNPJ normalizado e Active; não consultar orders nem pedir grants sobre ordens.
- [x] Emitir contexto/JWT interno restritos ao cliente, à OS e à ação, somente após validar o par token/documento; preservar policy exata, cancelamento, timeouts e sanitização.
- [x] Revisar `/customers/validate` para impedir emissão por documento isolado e reutilizar a validação da credencial com CPF/CNPJ obrigatório.

## LA.2 — Gateway: autorização e encaminhamento

- [ ] Declarar REQUEST authorizer somente para PATCH /Orders/{id}/budget, com fontes Authorization e X-Customer-Document e cache zero, sem toggle.
- [ ] Usar versão numérica selecionada no catálogo auth/v2; preservar a rota HTTP de validação com seu contrato revisado.
- [ ] Mapear integration.request.header.Authorization para context.authorizer.customerAuthorization nessa operação, substituindo o token público de orçamento; não concatenar Bearers nem aceitar identidade por headers alternativos.
- [ ] Preservar corpo e parâmetros de rota, inclusive normalização pathN existente; a Lambda não interpreta approved.
- [ ] Acrescentar permissão da versão Lambda para o authorizer específico, com SourceAccount/SourceArn; preservar permissão HTTP existente.
- [ ] Incluir authorizer/mapeamento/permissão nos gatilhos de redeploy e definir um único proprietário Terraform.
- [ ] Revisar permissões da pipeline; qualquer lacuna demanda ajuste mínimo explícito no bootstrap e aprovação posterior, sem ampliação preventiva.
- [ ] Preservar VPC Link/NLB/SGs, execution logs OFF, data tracing/cache OFF, retenção e logs técnicos sem tokens/documento/contexto.

## LA.3 — API: emissão da credencial e retorno da decisão

- [ ] Emitir credencial de orçamento com UUIDs do cliente/OS a partir do vínculo persistido; responder 200 com token/expiração na conclusão do diagnóstico autorizada. Definir emissão e tratamento de falha junto à confirmação da operação, sem sucesso antecipado.
- [ ] Separar validação do token de orçamento da autenticação JWT interna; manter implementação fora de Domain/serviços de negócio e sem alterar o pacote compartilhado.
- [ ] Responder 200/Approved ou Refused somente após a transação da decisão terminar com sucesso, sem SELECT apenas para montar o retorno.
- [ ] Remover customerDocument do DTO/comando/assinatura do serviço da decisão. Body somente approved obrigatório, sem campos extras ou decisão padrão.
- [ ] Preservar UUID Customer autenticado, resolução do cliente e titularidade. Validar também o escopo da OS/ação do JWT interno; não aceitar token público de orçamento como JWT genérico ou confiar em UUID/header público.
- [ ] Impedir uso do JWT restrito em outra OS, inclusive em rotas que já aceitam Customer. Não ampliar endpoints de consulta nem afetar o acesso Mechanic autorizado.
- [ ] Preservar bloqueio, transação, histórico, estoque, erros e rejeição de decisão repetida/incompatível; sem idempotência/retry de mutação ou tabela de tokens.

## LA.4 — Contratos, SAM e publicação

- [ ] Selecionar o entry point no SAM, preservando runtime, memória, timeout, layer, IAM/rede, VersionId do ZIP e publicação de versões.
- [ ] Atualizar OpenAPI: conclusão do diagnóstico com credencial/200; aprovação com body approved/200; contrato revisado de customers/validate. Documentar incompatibilidades com consumidores antigos.
- [ ] No contrato público do Gateway, declarar credencial de orçamento e documento obrigatórios; distinguir o Bearer público do JWT interno da API.
- [ ] Atualizar Postman: Mechanic conclui diagnóstico, captura decisionToken/expiração, apresenta token + documento para decidir, sem chamada separada de login Customer. Não salvar credencial real em exemplos versionados.
- [ ] Preservar workflows e publicação por commit/checksum/VersionId; alterar checks apenas se necessário ao entry point, sem novos workflows/inputs/Environments/aprovações.

## LA.5 — Logs, métricas e tracing

- [ ] Preservar mensagem fixa/tipo e ausência de stack trace/SQL/documento/tokens, inclusive na emissão da credencial. Não serializar requests, responses, policies ou contexto completo para logs.
- [ ] Nome técnico fixo para authorizer, distinto de POST /customers/validate, sem CPF/UUID nas dimensões.
- [ ] Reutilizar observabilidade/flush limitado e encerrar spans também em negativas/erro; não tratar REQUEST como resposta HTTP.
- [ ] Preservar métricas HTTP de customers/validate, sem misturar silenciosamente invocações do authorizer; reutilizar métricas de integração existentes, sem novos dashboards/alertas.
- [ ] Documentar que AWS/Lambda Invocations/Errors abrange ambas as entradas e pode incluir negativas esperadas por exceção. Conferir logs do runtime gerenciado; não suprimir mensagens da plataforma.

## LA.6 — Testes e validação local

Testes pequenos, com nomes descritivos, nos projetos existentes. Não criar scripts auxiliares, mocks AWS operacionais, suítes de Collector ou exporters fictícios.

- [x] Token/CPF do mesmo cliente permitem; token de A com CPF de B nega, inclusive com ambos Active. Documento continua obrigatório mesmo com token válido.
- [x] Token ausente, assinatura/audiência/emissor/finalidade inválidos, expirado, UUID inválido/ausente, OS diferente da rota e headers ambíguos negam. JWT genérico de usuário não substitui credencial de orçamento.
- [x] Consulta de cliente pelo UUID assinado, ausente/Inactive, sem consulta orders; preservar falha SQL/bootstrap/timeout sem Allow e escopo exato da policy/JWT interno.
- [x] Revisão de customers/validate: documento isolado não emite JWT; credencial + documento válido produz somente escopo permitido. Preservar despacho, reuso/retry/descarte e isolamento entre invocações.
- [ ] API: emissão somente ao disponibilizar orçamento autorizado, UUIDs derivados do cadastro/OS e expiração coerentes; sem token em logs ou cache da resposta.
- [ ] Aprovação/recusa retornam 200 após persistência; outro titular 404, estado incompatível 422, approved ausente/null/inválido ou campos extras 400. Token de orçamento não autentica diretamente como JWT genérico; JWT interno restrito não acessa outra OS.
- [ ] PostgreSQL: estado/histórico/estoque e rollback preservados; decisão repetida rejeitada pelo estado, sem declarar revogação individual da credencial.
- [ ] Conferir representação contexto → Authorization localmente; comportamento efetivo do Gateway exige comprovação AWS posterior.
- [ ] Restore/build Release, todos os testes, OpenAPI sem AWS, publishes com certificado RDS, SAM validate/build, Kustomize, Terraform fmt/init sem backend/validate e actionlint/ShellCheck se workflows mudarem.

Docker deverá estar ativo para Testcontainers; não iniciá-lo automaticamente nem ignorar testes indisponíveis. Mudanças de contrato devem ficar restritas aos pontos explicitamente descritos. Nenhuma validação da versão inicial comprova o novo fluxo token + documento.

## LA.7 — Publicação e conferência em hom

Somente mediante autorização posterior:

1. Revisar PRs de API/auth/infra; aplicar previamente eventual ajuste mínimo de bootstrap por plano salvo aprovado. Não alterar banco/grants para esta decisão.
2. Disponibilizar base/banco se destruídos; não recriá-los apenas para documentação/implementação local.
3. Publicar API e autenticação com contratos novos, em janela coordenada de hom; atualizar consumidores.
4. Provisionar Gateway com versão numérica nova e authorizer/mapeamento; plano salvo revisado/aprovado sem substituição de VPC/EKS/NLB/RDS.
5. Concluir diagnóstico como Mechanic, obter token e decidir com token + documento: comprovar Approved/Refused e persistência, sem login Customer separado.
6. Conferir token/CPF incompatíveis, ausência de cada credencial, expiração, escopo de outra OS, cliente Inactive, JWT genérico e body inválido. Negativa não chama API nem altera dados. Conferir também a checagem de titularidade da API.
7. Confirmar cache zero, isolamento de identidades, substituição de Authorization e ausência de tokens/documentos nos logs; conferir contrato revisado de customers/validate e demais rotas preservadas.

Não executar destroy, alterar cliente real ou provocar falhas sem autorização específica. Validação local não comprova mapeamento/erros do Gateway nem reabre E5/E6.

## LA.8 — Documentação e encerramento deste extra

- [ ] Atualizar contrato de acesso, diagramas, READMEs e procedimentos dos três repositórios afetados na implementação, distinguindo comportamento atual e alvo; banco sem alteração de esquema/seeds/grants.
- [ ] Registrar resultados locais e evidências de hom em documentos LA separados do checklist oficial.
- [ ] Declarar preparação local e comprovação integrada em momentos distintos; publicação e execução remota dependem de autorização.

## Estimativa e limites

Estimativa revisada para a ampliação: **16–32 horas de trabalho (2–4 dias)**, sujeita à emissão/validação das credenciais, revisão de contratos e integração Gateway. Pipelines, aprovações/provisionamento e conferência AWS acrescentam espera; não é prazo garantido.

Fora do escopo: páginas, login/envio por e-mail, nova função/serviço, alteração de SQL/seeds/grants, acesso da Lambda a orders, SMTP funcional AWS, fila/outbox, revogação individual/uso único persistido, idempotência, carga, novos alertas/dashboards, sampling e união X-Ray/OTLP. Não criar endpoint novo de detalhes/status; apenas respeitar escopo onde Customer já for aceito.

## Progresso e histórico — 22/10/2026

A LA.1 inicial foi concluída localmente: entry point HTTP/REQUEST, validação por documento, UUID interno, policy restrita, JWT no contexto e runtime compartilhado. Restore/build Release, 320 testes sem falhas/ignorados, publish linux-x64 e SAM validate/build aprovados. SAM continua com o handler atual; sem ativação no Gateway. [Evidência e limites](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/blob/develop/docs/LA1_AUTHORIZER_ORCAMENTO_2026-10-22.md).

Posteriormente, o mantenedor decidiu retirar a duplicação do documento no body e adotar credencial de orçamento apresentada junto com CPF/CNPJ. Considerou-se resolver cliente pela OS na Lambda, mas essa alternativa foi descartada: **UUID do cliente e da OS no token; Lambda consulta somente customers; API verifica a OS**. Esta é a decisão vigente e substitui o alvo inicial baseado somente em documento.

A decisão documental foi seguida pela adaptação LA.1: validação de token + documento, consulta somente customers por UUID e JWT interno com escopo da OS. Restore/build Release e 369 testes completos aprovados, sem falhas/ignorados. Próximas etapas: LA.2 (Gateway) e LA.3 (emissão e enforcement do escopo na API), antes da ativação completa. Não houve deploy ou alteração dos checklists oficiais; a API ainda não emite a credencial nem aplica o novo escopo.

## Fontes

- [Contrato HTTP/JWT vigente](arquitetura/ACESSO_E_AUTENTICACAO.md), distinto do alvo deste extra.
- [Controller de OS](../GerenciamentoMecanicaSistema/Controllers/OrdersController.cs) e [request atual](../GerenciamentoMecanicaSistema/Contracts/Requests/Order/ApproveOrderRequest.cs).
- [Entrada REQUEST](https://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-lambda-authorizer-input.html), [policy/contexto](https://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-lambda-authorizer-output.html) e [mapeamento da integração](https://docs.aws.amazon.com/apigateway/latest/developerguide/rest-api-parameter-mapping-sources.html).
- [Regras distintas de validação por tipo de JWT](https://www.rfc-editor.org/rfc/rfc8725.html#section-3.12) e [autorização de transações](https://cheatsheetseries.owasp.org/cheatsheets/Transaction_Authorization_Cheat_Sheet.html).
