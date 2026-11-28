# Postman

Recursos YAML no formato já usado pelo workspace (`.postman/resources.yaml`). Abrir a Local View/sincronização Git do Postman; não importar os YAMLs como se fossem uma collection JSON v2.1.

## Ambientes e segurança

`base_url` atende as operações de negócio. `gateway_url` atende `Authentication`, `Users` e as rotas de código Customer. Em Hom/Prd, preencher as duas variáveis com o endpoint atual do mesmo ambiente, incluindo o stage, disponível em `/mecanica/<ambiente>/gateway/v2/endpoint`. A configuração Dev usa a API direta em `localhost:8080`. A autenticação integrada em Dev é exercitada pelos testes do handler com bootstrap injetado.

Preservar o casing do contrato publicado: `/Authentication`, `/Customers`, `/Vehicles`, `/Catalog`, `/Stock`, `/Users` e `/Orders`. O Gateway diferencia maiúsculas de minúsculas. As rotas da autenticação Customer continuam `/customers/auth/request-code` e `/customers/auth/verify-code`; probes continuam `/health/...`.

CreateOrder mantém 201 e o corpo com id. O Location é relativo à URL da requisição e aponta para a consulta de status: `Orders/<id>/status` (ou `<id>/status` quando a criação usa `/Orders/`). Ao resolvê-lo contra a URL chamada, o cliente preserva domínio, HTTPS e stage, por exemplo `/hom/Orders/<id>/status`. Não concatenar Location diretamente à base_url nem tratá-lo como URL absoluta.

Preencher `admin_password`, `mechanic_password`, senhas da conta de teste e valores de sessão apenas como valores locais do ambiente Postman. Senhas, tokens, desafio, código e IDs começam vazios nos YAMLs. Manter capturas SMTP4dev, corpos e cabeçalhos sensíveis fora do Git e de relatórios compartilhados. Os documentos de exemplo são dados educacionais.

## Login e gestão da conta de teste

1. Selecionar Hom ou Prd do ambiente reconstruído. Preencher as senhas locais dos usuários Admin e Mechanic dos dados iniciais. Enviar `Authentication/Login` e `Authentication/LoginMechanic`. Os requests pedem JSON e armazenam a string JWT em `admin_token` e `mechanic_token`. Executar `Customers/GetCustomers` com o token Mechanic.
2. Preencher `test_user_name` e `test_user_password` com os dados de uma conta descartável. Executar `Users/RegisterUser` com Admin e confirmar `201`. Executar `Users/GetUser` para salvar seu UUID em `user_id`.
3. Executar `Authentication/LoginTestUser` e `Users/GetOwnUser`. A sessão separada `test_user_token` identifica a conta descartável. Admin pode consultar contas, e Mechanic consulta somente a própria.
4. Definir `test_user_updated_name` e/ou `test_user_updated_password` e executar `Users/UpdateOwnUserMechanic`. Campos vazios no ambiente são omitidos da requisição. Após o `204`, copiar os campos alterados para `test_user_name` e/ou `test_user_password`, emitir um novo JWT em `LoginTestUser` e comparar o UUID em `GetOwnUser`.
5. Executar `Users/DeleteUser` com Admin e o `user_id` salvo. Após `204`, o script limpa o UUID e o token de teste. Verificar que as credenciais excluídas retornam `401` em `LoginTestUser`.

`UpdateOwnUserAdmin` é uma verificação opcional em ambiente descartável. Preencher `admin_updated_name` e/ou `admin_updated_password` para alterar a conta do JWT Admin. Fazer essa verificação após o roteiro, pois ela muda as credenciais usadas nas demais chamadas.

## Sequência manual

1. Usar o `mechanic_token` do login. Para verificar a sincronização de um novo cliente, executar `Customers/RegisterCustomer` com `new_customer_document` livre. O `201` confirma o cadastro e o registro de acesso. Em `503 CUSTOMER_ACCESS_SYNC_PENDING`, repetir manualmente o mesmo cadastro. Após `201`, usar esse documento em `customer_document` para o pedido de código. Para exercitar a OS com cliente e veículo dos dados iniciais, usar o `customer_document` original.
2. Como Mechanic, obter customer_id, vehicle_id, service_id e material_id. CreateOrder salva order_id no ambiente ativo. Consultar status com Customer não exige que o orçamento exista.
3. Authentication/RequestCustomerCode envia somente document. Em 202 salva customer_challenge_id; **202 não confirma existência do cliente nem captura**. Cadastro desconhecido/Inactive mantém a resposta genérica sem escrita/envio. Novo pedido limpa código/desafio/token anteriores na sessão.
4. Operador autorizado abre SMTP4dev privado via port-forward e consulta o código de oito dígitos da mensagem mais recente. Copiar somente para customer_code local. Isso simula acesso à caixa; não é envio externo nem autorização pelo JWT Mechanic. Não consultar código no banco/logs ou criar endpoint debug. Respeitar 60 s entre pedidos e cinco pedidos/hora.
5. Authentication/VerifyCustomerCode envia challengeId, code e o mesmo document. Em 200 guarda a string JWT em customer_token, válida por dez minutos. A variável do código é apagada após qualquer resposta; se precisar repetir uma tentativa, preenchê-la novamente. Uma confirmação falha limpa o token anterior. Código: cinco minutos, cinco tentativas e uso único.
6. Orders/Queries/GetOrderStatus usa Bearer customer_token; também é permitido ao Mechanic trocando explicitamente o token da chamada. Customer só acessa suas OSs enquanto estiver Active.
7. Mechanic executa StartDiagnosis, itens e CompleteDiagnosis. Diagnóstico continua 204 e não devolve token.
8. Executar **ApproveBudget ou RefuseBudget**, em OSs separadas ou escolhendo uma decisão. Corpo somente approved booleano; sucesso 200 com decision Approved/Refused. Repetir decisão é 422; não rodar ambos esperando sucesso na mesma OS.
9. GetOrderStatus confirma o estado posterior. Aprovada: Mechanic segue execução/conclusão/entrega. Recusada: segue entrega conforme regras atuais. Notificações são capturadas pelo mesmo transporte.

## Negativas essenciais

Sem Bearer nas rotas de OS → 401; Admin/Mechanic no orçamento → 403; Customer de outro titular → 404; Customer Inactive → 403 nas operações da API. Corpo de orçamento ausente/null/texto/extra/duplicado → 400. Código errado, expirado, consumido ou documento divergente → 401 genérico na confirmação. JWT expirado → 401 sem tolerância adicional. A quinta tentativa invalida; não há reenvio/reprocessamento automático.

Para verificar as negativas de identidade, duplicar uma requisição para o ensaio. Login com corpo ou perfil inválido retorna `400` e credenciais incorretas retornam `401`. Em `/Users`, remover o Bearer para ver `401` ou usar o JWT Mechanic em cadastro, exclusão ou consulta de outra conta para ver `403`.

Não rodar a pasta Lifecycle inteira como se fosse um cenário linear: ela inclui decisões alternativas e DELETE. Scripts de resposta descrevem o caminho positivo; uma negativa intencional gera falha nesses asserts, sem indicar erro de instrumentação. A coleção não faz chamadas administrativas ao SMTP4dev nem captura automaticamente códigos.

## Formato versionado

Requisições normalizadas ao schema oficial Postman Collection Format v3: name explícito, headers/credentials como listas key/value, JSON de corpo legível e sem order legado ignorado pelo schema. A sequência de negócio é a descrita acima; não inferir cenário linear pela ordenação visual de arquivos. [Schema oficial](https://schema.postman.com/collection/json/v3.0.0/draft-07/collection.json).
