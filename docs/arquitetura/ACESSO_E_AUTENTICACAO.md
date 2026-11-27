# Acesso e autenticação

## Identidades e responsabilidades

A AuthFunction administra as contas internas e emite os JWTs. Admin administra contas. Mechanic opera cadastros e OSs e acessa sua própria conta. Customer consulta status e decide orçamento de suas OSs. A API usa o UUID do JWT para verificar a titularidade, e cada componente aplica as permissões das suas operações.

O cliente solicita e confirma o código com seu CPF/CNPJ numérico. O operador consulta a captura privada SMTP4dev para simular a entrega; isso não comprova acesso à caixa pessoal. Não há e-mail externo. O pacote Auth.Contracts 1.0.0 permanece compartilhando documento/assinatura, sem contratos de SMTP ou entidades de OS.

## Matriz por endpoint

As rotas abaixo preservam o casing do OpenAPI publicado: o Gateway diferencia maiúsculas de minúsculas. Somente as rotas customers/auth e health permanecem em minúsculas. Próprio significa verificar a identidade no servidor, não confiar em CPF/UUID recebidos para escolher o titular.

| Método | Rota | Admin | Mechanic | Cliente | Sem token | Execução |
|---|---|---|---|---|---|---|
| POST | `/Authentication` | Sim | Sim | Não se aplica | Sim | AuthFunction — login interno |
| POST | `/customers/auth/request-code` | Não se aplica | Não se aplica | Sim | Sim | AuthFunction — solicitar código |
| POST | `/customers/auth/verify-code` | Não se aplica | Não se aplica | Sim | Sim | AuthFunction — confirmar código e emitir JWT |
| POST | `/Users` | Sim | Não | Não | Não | AuthFunction |
| GET | `/Users` | Sim | Próprio | Não | Não | AuthFunction |
| PATCH | `/Users` | Próprio | Próprio | Não | Não | AuthFunction |
| DELETE | `/Users` | Sim | Não | Não | Não | AuthFunction |
| POST | `/Customers` | Não | Sim | Não | Não | API |
| GET | `/Customers` | Não | Sim | Não | Não | API |
| PATCH | `/Customers/{id}` | Não | Sim | Não | Não | API |
| DELETE | `/Customers/{id}` | Não | Sim | Não | Não | API |
| POST | `/Vehicles` | Não | Sim | Não | Não | API |
| GET | `/Vehicles` | Não | Sim | Não | Não | API |
| PATCH | `/Vehicles/{id}` | Não | Sim | Não | Não | API |
| DELETE | `/Vehicles/{id}` | Não | Sim | Não | Não | API |
| POST | `/Catalog` | Não | Sim | Não | Não | API |
| GET | `/Catalog` | Não | Sim | Não | Não | API |
| PATCH | `/Catalog/{id}` | Não | Sim | Não | Não | API |
| DELETE | `/Catalog/{serviceId}` | Não | Sim | Não | Não | API |
| POST | `/Stock` | Não | Sim | Não | Não | API |
| GET | `/Stock` | Não | Sim | Não | Não | API |
| POST | `/Stock/amount/{id}` | Não | Sim | Não | Não | API |
| PATCH | `/Stock/amount/{id}` | Não | Sim | Não | Não | API |
| PATCH | `/Stock/price/{id}` | Não | Sim | Não | Não | API |
| DELETE | `/Stock/{id}` | Não | Sim | Não | Não | API |
| POST | `/Orders` | Não | Sim | Não | Não | API |
| GET | `/Orders/{id}/status` | Não | Sim | Própria OS | Não | API |
| GET | `/Orders/operational` | Não | Sim | Não | Não | API |
| GET | `/Orders/details` | Não | Sim | Não | Não | API |
| PATCH | `/Orders/{id}/diagnosis/start` | Não | Sim | Não | Não | API |
| POST | `/Orders/{id}/services` | Não | Sim | Não | Não | API |
| PATCH | `/Orders/{id}/services` | Não | Sim | Não | Não | API |
| POST | `/Orders/{id}/materials` | Não | Sim | Não | Não | API |
| PATCH | `/Orders/{id}/materials` | Não | Sim | Não | Não | API |
| PATCH | `/Orders/{id}/diagnosis/complete` | Não | Sim | Não | Não | API |
| PATCH | `/Orders/{id}/budget` | Não | Não | Própria OS | Não | API |
| PATCH | `/Orders/{id}/execution/start` | Não | Sim | Não | Não | API |
| PATCH | `/Orders/{id}/execution/complete` | Não | Sim | Não | Não | API |
| PATCH | `/Orders/{id}/delivery` | Não | Sim | Não | Não | API |
| DELETE | `/Orders/{id}` | Não | Sim | Não | Não | API |
| GET | `/health/live` | Sim | Sim | Sim | Sim | API — probes |
| GET | `/health/ready` | Sim | Sim | Sim | Sim | API — probes |
| GET | `/health/startup` | Sim | Sim | Sim | Sim | API — probes |

Probes são internas à API e não são publicadas pelo Gateway. Rotas de login/pedido/confirmação não exigem Bearer anterior; enviar JWT não evita verificação de credenciais ou código. Chamadas com sessão Customer válida reutilizam o Bearer nas operações de OS, sem repetir autenticação em cada endpoint.

## Login interno e gestão de contas

Enviar `POST /Authentication` pelo Gateway com `name`, `password` e `role` (`Admin` ou `Mechanic`). O login retorna `200` com JWT em texto puro por padrão ou como string JSON com `Accept: application/json`. Corpo ou perfil inválido retorna `400` e credenciais incorretas retornam `401` genérico.

Usar `Authorization: Bearer <token>` em todas as operações de `/Users`. A AuthFunction valida o JWT e o perfil.

| Operação | Entrada | Resultado |
|---|---|---|
| `POST /Users` | `name`, `password`, `role` com JWT Admin | `201` |
| `GET /Users` | Filtros opcionais `name` e `role`. Admin consulta contas e Mechanic consulta a própria | `200` com `id`, `name` e `role` |
| `PATCH /Users` | `name` e/ou `password` da conta identificada pelo JWT | `204`. Campos omitidos preservam seus valores |
| `DELETE /Users?id=<uuid>` | UUID na query e JWT Admin | `204` |

`400` indica entrada inválida, `401` JWT ausente ou inválido, `403` perfil ou consulta fora do escopo, `404` conta ausente e `409` conflito de nome no mesmo perfil. Use uma conta descartável para testar alterações e exclusão. [Roteiro Postman](../../postman/README.md#login-e-gestão-da-conta-de-teste)

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

Os JWTs da AuthFunction são assinados com HS256 e carregam UUID, nome e perfil. Emissor e audiência são definidos por ambiente e a validade é de dez minutos. A API valida assinatura, emissor, audiência e expiração com `ClockSkew` zero. Os dados do JWT Customer se limitam à identidade usada pela API para autorizar a operação.

Inativação após emitir JWT não altera a assinatura, mas as duas operações Customer consultam Active e negam acesso. Não há endpoint público de inativação, refresh, revogação central, sessão por orçamento ou bypass por JWT no pedido/confirmação. PATCH cadastral preserva status. Exclusão/recriação não transfere identidade nem titularidade para outro UUID.

## Cadastro operacional e registro de acesso

A API é proprietária de `mecanica.customers`. A AuthFunction administra `customer_access`, `users`, desafios e limites em `mecanica_identity`. Os dois bancos compartilham a instância RDS e cada serviço usa sua própria credencial.

Ao cadastrar, alterar nome ou e-mail ou excluir um cliente, a API confirma a alteração operacional e sua revisão de sincronização na mesma transação. Em seguida invoca o alias `live` da AuthFunction por IAM. A função usa o mesmo UUID para atualizar ou revogar o acesso. Mudança de e-mail e revogação invalidam os desafios abertos.

Quando a confirmação falha, a API retorna `503 CUSTOMER_ACCESS_SYNC_PENDING` com o UUID do cliente. Para cadastro, repetir o mesmo `POST` com os mesmos dados. Para alteração ou exclusão, repetir a operação pelo UUID. O reenvio da mesma revisão é idempotente. [Modelo e permissões](BANCO_E_MODELO.md)

## Execução e alcance

Auth inicializa uma vez por runtime saudável, com nova tentativa após falha. Produção exige hom/prd e us-east-1, bootstrap SSM/Secrets com limites existentes, TLS VerifyFull e usuário mecanica_auth. Localmente os testes/ensaio injetam configuração; não há fallback alternativo de banco/JWT no handler padrão.

EmailFunction recebe Invoke v1 privado e concentra MIME/SMTP. API e auth montam suas mensagens e só confirmam captured com envelope válido. Código/caixa são efêmeros; PostgreSQL persiste salt/verificador e limites. Falha de notificação da OS não desfaz o commit; falha do envio do código impede confirmar desafio utilizável. Sem outbox, fila, reenvio automático ou entrega única.

[Postman](../../postman/README.md) mantém desafio/código/JWT somente na sessão local. Testes locais não comprovam disponibilidade da implantação ou entrega externa de e-mail.

## Checklist do ambiente reconstruído

Aplicar em Hom e Prd após o Job inicializar os bancos com os dados educacionais e o Gateway apontar para as revisões implantadas.

- Emitir JWTs novos de Admin e Mechanic e verificar a autorização da API.
- Cadastrar, consultar, alterar e excluir uma conta de teste em `/Users`, incluindo as negativas por ausência de JWT ou perfil incorreto.
- Pedir e confirmar o código no SMTP4dev privado, verificar o uso único e consultar o status da OS com o cliente titular. Conferir a negativa para outro cliente.
- Verificar as gravações nos bancos de cada serviço e a versão numérica da AuthFunction no Gateway.
- Registrar os resultados e os links dos pipelines com as revisões da API, da AuthFunction, do banco e da Infraestrutura.

[Ordem de reconstrução](CONTRATOS_ENTRE_REPOSITORIOS.md#ordem-e-responsabilidade-operacional) · [Roteiro Postman](../../postman/README.md)

## Entregas futuras

Saga, uma nova política de tentativas e bloqueio para senha interna e assinatura assimétrica serão avaliadas em entregas futuras. A autenticação atual usa HS256 e mantém os limites de código Customer documentados acima.
