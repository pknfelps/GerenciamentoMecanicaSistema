# Acesso e autenticação — contrato AE

**Revisão de 23/10/2026, AE.9.** Descreve o código preparado nas AE.3–AE.8. A comprovação conjunta é local; a publicação AWS do contrato novo continua na AE.10. Os aceites hom anteriores à extra permanecem nas [evidências E6](../REVISAO_E6_2026-10-21.md), sem certificar a implantação destas alterações. [Contrato extra](../CONTRATOS_EXTRA_AUTENTICACAO_E_EMAIL.md) · [Roteiro AE.9](../AE9_VALIDACAO_LOCAL_E_DOCUMENTACAO.md).

## Identidades e responsabilidades

Admin administra usuários internos; Mechanic opera cadastros/OSs e sua própria conta; Customer representa o cadastro do cliente. Admin não herda Mechanic. Customer consulta status e decide orçamento somente de suas OSs. JWT carrega UUID do cliente, sem vínculo a uma OS; o relacionamento é validado pela API. Não há Authorizer, status anônimo ou código/token emitido pelo mecânico.

O cliente solicita e confirma o código com seu CPF/CNPJ numérico. O operador consulta a captura privada SMTP4dev para simular a entrega; isso não comprova acesso à caixa pessoal. Não há e-mail externo. O pacote Auth.Contracts 1.0.0 permanece compartilhando documento/assinatura, sem contratos de SMTP ou entidades de OS.

## Matriz por endpoint

As rotas da API usam nomes de controllers e são apresentadas em minúsculas para leitura; o Gateway projeta as operações do OpenAPI. Próprio significa verificar a identidade no servidor, não confiar em CPF/UUID recebidos para escolher o titular.

| Método | Rota | Admin | Mechanic | Cliente | Sem token | Execução |
|---|---|---|---|---|---|---|
| POST | `/authentication` | Sim | Sim | Não se aplica | Sim | API — autenticação interna |
| POST | `/customers/auth/request-code` | Não se aplica | Não se aplica | Sim | Sim | AuthFunction — solicitar código |
| POST | `/customers/auth/verify-code` | Não se aplica | Não se aplica | Sim | Sim | AuthFunction — confirmar código e emitir JWT |
| POST | `/users` | Sim | Não | Não | Não | API |
| GET | `/users` | Sim | Próprio | Não | Não | API |
| PATCH | `/users` | Próprio | Próprio | Não | Não | API — implementada e conferida em hom |
| DELETE | `/users` | Sim | Não | Não | Não | API — implementada e conferida em hom |
| POST | `/customers` | Não | Sim | Não | Não | API |
| GET | `/customers` | Não | Sim | Não | Não | API |
| PATCH | `/customers/{id}` | Não | Sim | Não | Não | API |
| DELETE | `/customers/{id}` | Não | Sim | Não | Não | API |
| POST | `/vehicles` | Não | Sim | Não | Não | API |
| GET | `/vehicles` | Não | Sim | Não | Não | API |
| PATCH | `/vehicles/{id}` | Não | Sim | Não | Não | API |
| DELETE | `/vehicles/{id}` | Não | Sim | Não | Não | API |
| POST | `/catalog` | Não | Sim | Não | Não | API |
| GET | `/catalog` | Não | Sim | Não | Não | API |
| PATCH | `/catalog/{id}` | Não | Sim | Não | Não | API |
| DELETE | `/catalog/{serviceId}` | Não | Sim | Não | Não | API |
| POST | `/stock` | Não | Sim | Não | Não | API |
| GET | `/stock` | Não | Sim | Não | Não | API |
| POST | `/stock/amount/{id}` | Não | Sim | Não | Não | API |
| PATCH | `/stock/amount/{id}` | Não | Sim | Não | Não | API |
| PATCH | `/stock/price/{id}` | Não | Sim | Não | Não | API |
| DELETE | `/stock/{id}` | Não | Sim | Não | Não | API |
| POST | `/orders` | Não | Sim | Não | Não | API |
| GET | `/orders/{id}/status` | Não | Sim | Própria OS | Não | API |
| GET | `/orders/operational` | Não | Sim | Não | Não | API |
| GET | `/orders/details` | Não | Sim | Não | Não | API |
| PATCH | `/orders/{id}/diagnosis/start` | Não | Sim | Não | Não | API |
| POST | `/orders/{id}/services` | Não | Sim | Não | Não | API |
| PATCH | `/orders/{id}/services` | Não | Sim | Não | Não | API |
| POST | `/orders/{id}/materials` | Não | Sim | Não | Não | API |
| PATCH | `/orders/{id}/materials` | Não | Sim | Não | Não | API |
| PATCH | `/orders/{id}/diagnosis/complete` | Não | Sim | Não | Não | API |
| PATCH | `/orders/{id}/budget` | Não | Não | Própria OS | Não | API |
| PATCH | `/orders/{id}/execution/start` | Não | Sim | Não | Não | API |
| PATCH | `/orders/{id}/execution/complete` | Não | Sim | Não | Não | API |
| PATCH | `/orders/{id}/delivery` | Não | Sim | Não | Não | API |
| DELETE | `/orders/{id}` | Não | Sim | Não | Não | API |
| GET | `/health/live` | Sim | Sim | Sim | Sim | API — probes |
| GET | `/health/ready` | Sim | Sim | Sim | Sim | API — probes |
| GET | `/health/startup` | Sim | Sim | Sim | Sim | API — probes |


Probes são internas à API e não são publicadas pelo Gateway. Rotas de login/pedido/confirmação não exigem Bearer anterior; enviar JWT não evita verificação de credenciais ou código. Chamadas com sessão Customer válida reutilizam o Bearer nas operações de OS, sem repetir autenticação em cada endpoint.

## Pedido e confirmação do código

| Operação | Corpo | Sucesso |
|---|---|---|
| POST /customers/auth/request-code | document | 202 com challengeId e mensagem genérica |
| POST /customers/auth/verify-code | challengeId, code, document | 200 com JWT como string JSON |

JSON application/json de até 4 KiB UTF-8, inclusive após Base64. Campos estritos, nomes sem distinção de maiúsculas; extras, duplicações, null/tipo incorreto ou documento inválido retornam 400 antes de bootstrap/SQL. Document aceita CPF de 11 ou CNPJ numérico de 14 dígitos, com as máscaras do DocumentRules, normalizados para consulta. Código tem oito dígitos ASCII; challengeId é UUID não vazio. Não transportar documento pelo header/query/JWT.

Pedido: cliente encontrado/Active/com e-mail utilizável → desafio Pending confirmado → Invoke síncrono EmailFunction → captura confirmada → Ready condicional → 202. Cadastro ausente/Inactive/sem e-mail utilizável recebe o mesmo 202 sem escrita, código, envio ou limpeza; apenas diagnóstico técnico fixo. 202 não confirma existência do cliente ou entrega. Sem consulta a OS na autenticação.

Cooldown de 60 segundos e cinco solicitações admitidas/hora, por UUID de cliente elegível. Limite retorna 429 com Retry-After. Validade do código: cinco minutos; cinco tentativas incorretas invalidam. Documento divergente também consome tentativa. Código correto só retorna JWT após consumo/commit atômicos; replay, expiração, Pending/Invalidated/Consumed, cadastro ausente/Inactive recebem 401 genérico. Falha conhecida no envio impede ativação e tenta invalidar Pending, sem reembolsar limite nem mascarar a falha original.

Todas as respostas têm Cache-Control: no-store. Erros usam problem+json com type=about:blank e instance de rota fixa, nunca caminho arbitrário. 400 inválido; 401 confirmação não autorizada; 429 limite; 500 erro técnico/timeout. Handler direto responde 404 na rota antiga/desconhecida e 405/Allow: POST no método incorreto; Gateway pode responder com seu próprio envelope quando a rota não existe. Não expor código, documento, destinatário, configuração ou exceção original em respostas de erro/logs.

## Operações Customer e decisão do orçamento

GET /Orders/{id}/status aceita Mechanic ou Customer. Customer exige cadastro Active antes de consultar a OS e vínculo da OS ao UUID do JWT. Ausência de cadastro, OS inexistente e outro titular produzem 404 genérico; Inactive produz 403. Mechanic mantém consulta operacional sem adquirir permissão de aprovar/recusar orçamento.

PATCH /Orders/{id}/budget aceita **somente approved booleano explícito**, por exemplo {"approved":true}. Ausência, null, texto, extras (inclusive customerDocument/customerId) e duplicação produzem 400. CPF é verificado na confirmação do código, não repetido no orçamento. Após commit, responde 200 com {"decision":"Approved"} ou {"decision":"Refused"}; repetição/estado incompatível permanece 422. Diagnóstico concluído segue 204, sem token. Estoque, locks, histórico e rollback são preservados.

## JWT, cadastro e sessões

HS256, issuer/audience por ambiente, UUID/nome/role e expiração UTC + dez minutos. Customer não contém CPF/CNPJ, e-mail, código, desafio ou ID da OS. API valida assinatura/algoritmo/emissor/audiência/prazo com **ClockSkew zero**, inclusive para Admin/Mechanic. Login interno mantém name/password/role e só emite Admin/Mechanic; Customer não possui login por senha.

Inativação após emitir JWT não altera a assinatura, mas as duas operações Customer consultam Active e negam acesso. Não há endpoint público de inativação, refresh, revogação central, sessão por orçamento ou bypass por JWT no pedido/confirmação. PATCH cadastral preserva status. Exclusão/recriação não transfere identidade nem titularidade para outro UUID.

## Execução e alcance

Auth inicializa uma vez por runtime saudável, com nova tentativa após falha. Produção exige hom/prd e us-east-1, bootstrap SSM/Secrets com limites existentes, TLS VerifyFull e usuário mecanica_auth. Localmente os testes/ensaio injetam configuração; não há fallback alternativo de banco/JWT no handler padrão.

EmailFunction recebe Invoke v1 privado e concentra MIME/SMTP. API e auth montam suas mensagens e só confirmam captured com envelope válido. Código/caixa são efêmeros; PostgreSQL persiste salt/verificador e limites. Falha de notificação da OS não desfaz o commit; falha do envio do código impede confirmar desafio utilizável. Sem outbox, fila, reenvio automático ou entrega única.

[Postman](../../postman/README.md) mantém desafio/código/JWT somente na sessão local. Fluxo conjunto, negativas e limites estão no [registro AE.9](../AE9_VALIDACAO_LOCAL_E_DOCUMENTACAO.md). Prd e execução AWS do novo contrato não são comprovadas pela validação local.
