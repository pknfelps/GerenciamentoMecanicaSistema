# Postman

Recursos YAML no formato já usado pelo workspace (`.postman/resources.yaml`). Abrir a Local View/sincronização Git do Postman; não importar os YAMLs como se fossem uma collection JSON v2.1.

## Ambientes e segurança

Dev aponta somente a API para localhost:8080; a autenticação local integrada é conferida pelo handler com bootstrap injetado, sem endpoint HTTP alternativo. Hom/Prd começam sem URLs. Após implantação autorizada, copiar o endpoint atual com stage do catálogo gateway/v2/endpoint para **base_url e gateway_url** do ambiente correspondente. Não reutilizar URL de ambiente destruído ou misturar ambientes.

Preservar o casing do contrato publicado: `/Authentication`, `/Customers`, `/Vehicles`, `/Catalog`, `/Stock`, `/Users` e `/Orders`. O Gateway diferencia maiúsculas de minúsculas. As rotas da autenticação Customer continuam `/customers/auth/request-code` e `/customers/auth/verify-code`; probes continuam `/health/...`.

CreateOrder mantém 201 e o corpo com id. O Location é relativo à URL da requisição e aponta para a consulta de status: `Orders/<id>/status` (ou `<id>/status` quando a criação usa `/Orders/`). Ao resolvê-lo contra a URL chamada, o cliente preserva domínio, HTTPS e stage, por exemplo `/hom/Orders/<id>/status`. Não concatenar Location diretamente à base_url nem tratá-lo como URL absoluta.

Tokens, desafio, código e IDs começam vazios. Manter valores capturados apenas no contexto local do Postman; não exportar/sincronizar valores de sessão ou console/relatórios com corpos/headers sensíveis para o Git. Nenhum script imprime código/token. O documento preenchido é o seed educacional fictício; substituir pelos dados fictícios do cenário quando necessário.

## Sequência manual

1. Authentication/LoginMechanic salva mechanic_token; Login salva admin_token para operações Admin. São seeds educacionais, sem login de cliente por senha.
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

Não rodar a pasta Lifecycle inteira como se fosse um cenário linear: ela inclui decisões alternativas e DELETE. Scripts de resposta descrevem o caminho positivo; uma negativa intencional gera falha nesses asserts, sem indicar erro de instrumentação. A coleção não faz chamadas administrativas ao SMTP4dev nem captura automaticamente códigos.

## Formato versionado

Requisições normalizadas ao schema oficial Postman Collection Format v3: name explícito, headers/credentials como listas key/value, JSON de corpo legível e sem order legado ignorado pelo schema. A sequência de negócio é a descrita acima; não inferir cenário linear pela ordenação visual de arquivos. [Schema oficial](https://schema.postman.com/collection/json/v3.0.0/draft-07/collection.json).
