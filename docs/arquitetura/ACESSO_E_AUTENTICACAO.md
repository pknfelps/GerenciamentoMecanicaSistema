# E0 — Especificação de acesso e autenticação

- **Data:** 2026-09-13.
- **Estado:** especificação consolidada; autorização das rotas existentes e vínculo da OS implementados/testados localmente na E4 em 13/10/2026. PATCH próprio e DELETE implementados/testados localmente; deploy/conferência AWS desta entrega pendentes.
- **Referências:** [arquitetura](README.md), [identidade e permissões](adrs/001-IDENTIDADE.md) e [integração em execução](rfcs/001-EXECUCAO.md).
- **Origem:** regras de negócio definidas pelo usuário nesta sessão; detalhes de contrato abaixo concretizam essas regras com base no código atual.

## 1. Identidades e responsabilidades

| Identidade | Responsabilidade |
|---|---|
| Admin | Cadastrar, consultar e remover usuários internos; atualizar somente a própria conta |
| Mechanic | Gerenciar OSs e cadastros operacionais (clientes, veículos, estoque e catálogo); consultar/atualizar somente a própria conta |
| Cliente | Validar o documento (CPF/CNPJ) pela função serverless; consultar o status e aprovar/recusar o orçamento da própria OS |

O modelo de usuários internos passa de `User/Manager/Admin` para `Mechanic/Admin`. `Customer` será o valor da claim de perfil para tokens de cliente, sem transformar clientes em registros de usuários internos. Admin não herda permissões de Mechanic. Nenhum dos dois pode aprovar/recusar orçamento, nem alterar o login de outra conta. Update próprio não altera role.

A escolha de `Mechanic` segue a sugestão registrada em D03.5. A consulta própria de usuário e a atribuição dos cadastros auxiliares a Mechanic são detalhamentos da separação de responsabilidades para os endpoints existentes.

## 2. Matriz de permissões por endpoint

As rotas são apresentadas em minúsculas para uniformizar o contrato; as atuais usam nomes de controllers no roteamento. Parâmetros de query existentes permanecem, salvo o detalhamento dos novos métodos de users. `Próprio` significa validar o vínculo com a identidade autenticada no servidor, não confiar na identificação enviada pelo consumidor.

| Método | Rota | Admin | Mechanic | Cliente | Sem token | Execução |
|---|---|---|---|---|---|---|
| POST | `/authentication` | Sim | Sim | Não se aplica | Sim | API — autenticação interna |
| POST | `/customers/validate` | Não se aplica | Não se aplica | Sim | Sim | Função serverless via Gateway — nova |
| POST | `/users` | Sim | Não | Não | Não | API |
| GET | `/users` | Sim | Próprio | Não | Não | API |
| PATCH | `/users` | Próprio | Próprio | Não | Não | API — implementada localmente |
| DELETE | `/users` | Sim | Não | Não | Não | API — implementada localmente |
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

Rotas de emissão de token não exigem token prévio: `/authentication` valida credenciais internas; `/customers/validate` valida documento/cadastro/status. Enviar um token a elas não dispensa essas verificações. A API mantém as validações de domínio e de estado da OS após a autorização.

O OpenAPI `/openapi/v1.json` e a interface `/swagger` permanecem sem autenticação quando habilitados em desenvolvimento, como hoje. A exposição dessas ferramentas fora desse ambiente será tratada na arquitetura de deploy. Health checks continuam sem JWT para as probes; isso não determina exposição pública de todas essas rotas no Gateway.

### Verificação de propriedade

- **Update próprio:** obter o ID do usuário do token validado; nenhuma entrada pode selecionar outra conta.
- **GET users:** Admin mantém a consulta atual por `name/role`. Para Mechanic, a consulta parametrizada é vinculada exclusivamente ao UUID autenticado, sem usar o nome do token. Filtros omitidos retornam a própria conta; filtros informados devem corresponder ao cadastro atual, ou retornam 403 sem consultar outra conta. Conta própria ausente retorna 404; senha/hash não são retornados.
- **OS do cliente:** carregar a OS e verificar seu vínculo com o cliente identificado no token. A API resolve o cliente pelo UUID do token e consulta a OS por ID/documento cadastrado, verificando o vínculo antes da decisão de estado ou de alterações. OS inexistente, de outro cliente ou cliente ausente retornam o mesmo 404 com detail `Ordem não encontrada`, sem divulgar documentos. Não revalidar Active/Inactive para bloquear uma sessão já emitida.
- **Aprovação:** manter o campo `approved`. O `customerDocument` existente, enquanto mantido por compatibilidade, deve corresponder à identidade autenticada e nunca substitui a verificação do titular. Admin e Mechanic recebem 403 mesmo que informem o documento correto. Documento inválido retorna 400; divergência na OS própria ou transição inválida retorna 422. O titular obrigatório é obtido do JWT, não de campos adicionais do request.

### Respostas das rotas existentes — E4

| Situação | Resposta |
|---|---|
| JWT ausente/inválido/expirado; identidade obrigatória ausente, duplicada, inválida ou vazia | 401 |
| Perfil sem permissão; filtros próprios incompatíveis | 403 |
| OS ausente/de outro cliente; cadastro necessário ausente | 404 |
| Request inválido | 400 |
| Documento divergente na OS própria ou transição inválida | 422 |
| Falha inesperada | 500 |

O helper HTTP exige um único NameIdentifier UUID válido em GET Users de Mechanic, PATCH Users de Admin/Mechanic e status/orçamento de Customer. As outras rotas preservam a validação JWT existente, sem exigência global de ID ou leitura de cadastro para cada token. ClaimsPrincipal fica nos controllers; serviços recebem UUID. Negativas de challenge/forbid preservam seus formatos atuais; filtros de conta usam AccessDeniedException/Problem Details. Testes percorrem as 37 ações protegidas e exercitam HTTP/PostgreSQL, incluindo estoque/histórico intactos nas negativas e token anterior à inativação. A conferência pelo Gateway continua pendente de deploy.

## 3. PATCH /users — atualizar a própria conta

**Implementação local E4.11 em 13/10/2026:** Admin ou Mechanic alteram exclusivamente sua própria conta, selecionada pelo UUID do JWT. Publicação e conferência AWS desta entrega permanecem pendentes.

```http
PATCH /users
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "name": "mecanico atualizado",
  "password": "NovaSenha@123"
}
```

- `name` e `password` são opcionais; informar pelo menos um. Omitir preserva o valor atual. Nulo/vazio/whitespace explícito é inválido. Não usar JSON Patch.
- Aceitar somente essas duas propriedades, mantendo nomes JSON sem distinção de maiúsculas. Rejeitar id, role e demais campos desconhecidos; indicadores de presença são privados e não integram o contrato JSON.
- Nome segue a validação já existente (ao menos três caracteres, letras/espaços) e limite de 100 caracteres; senha segue as regras atuais e é gravada como hash, nunca retornada.
- Exigir exatamente um NameIdentifier UUID válido e não vazio antes do serviço. Nome/perfil do token não selecionam o cadastro; perfil persistido e ID permanecem inalterados.
- Reutilizar a checagem atual de nome/perfil, excluindo a própria conta da detecção de conflito. O mesmo nome em outro perfil não conflita.
- Atualizar name/password por SQL fixo parametrizado pelo UUID, com COALESCE para campos omitidos. Nenhuma linha atualizada significa conta ausente. Não modificar esquema nem criar operação de revogação.

| Código | Significado |
|---|---|
| 204 | Atualização concluída, sem corpo |
| 401 | Token ausente/inválido/expirado ou identidade obrigatória inválida |
| 403 | Perfil sem permissão |
| 404 | Conta identificada pelo JWT inexistente |
| 400 | Corpo/valores inválidos, campos proibidos ou nenhum campo informado |
| 409 | Nome já utilizado por outro usuário do mesmo perfil |
| 500 | Falha inesperada de execução/persistência |

Contrato de erros alinhado à API por escolha do mantenedor em 13/10/2026, substituindo os códigos antigos desta operação. Preservar challenge/forbid e Problem Details. Mensagens não incluem senha, hash ou valores enviados.

Após mudar nome/senha, o token assinado permanece válido até expirar; não exigir senha atual, gerar token novo ou revalidar cada requisição contra nome/senha. O UUID preserva a identificação da conta, inclusive se outro usuário reutilizar o login antigo. O próximo login usa os dados novos.

## 4. DELETE /users — remover usuário

**Implementação local E4.12 em 13/10/2026:** somente Admin. O alvo é selecionado pela query obrigatória `id` (UUID não vazio), sem corpo. Publicação e conferência AWS desta entrega permanecem pendentes.

```http
DELETE /users?id=<uuid-do-usuario>
Authorization: Bearer <token-admin>
```

| Código | Significado |
|---|---|
| 204 | Remoção concluída, sem corpo |
| 401 | Token ausente/inválido/expirado |
| 403 | Perfil diferente de Admin |
| 400 | ID ausente/inválido/vazio |
| 404 | Usuário inexistente, inclusive numa segunda exclusão |
| 500 | Falha inesperada de execução/persistência |

Contrato de erros alinhado à API por escolha do mantenedor em 13/10/2026, substituindo os códigos antigos desta operação. Preservar challenge/forbid e Problem Details, sem credenciais ou dados cadastrais nas mensagens.

O controller exige Admin pelo atributo Authorize, sem exigir NameIdentifier do autor nem consultar seu cadastro a cada requisição. A query identifica o alvo. O serviço valida UUID e executa DELETE fixo parametrizado; quantidade zero de linhas afetadas resulta em 404, sem SELECT prévio. Nenhuma tabela além de users é alterada.

Mechanic não pode remover outros usuários nem a própria conta. Admin pode excluir Admin/Mechanic, inclusive sua própria conta e o último Admin; não há proteção adicional definida. A remoção impede novo login, mas não revoga JWTs anteriores. Rotas permitidas pelo perfil continuam aceitando o token até expirar; GET próprio de Mechanic e PATCH próprio retornam 404 ao resolver a conta removida. Reutilizar o nome em uma nova conta não transfere a identidade do token anterior.

No Postman, `DeleteUser` usa admin_token e user_id inicialmente vazios. Preencher user_id com o UUID do cadastro a excluir, obtido na consulta de usuários.

## 5. POST /customers/validate — validação serverless do documento (CPF/CNPJ)

**Responsável:** função serverless. O Gateway direciona essa rota para a função, sem criar um login de cliente com senha ou uma segunda etapa de autenticação na API principal. O nome da rota não significa que a emissão será implementada no CustomersController da API.

```http
POST /customers/validate
Content-Type: application/json
```

```json
{
  "document": "529.982.247-25"
}
```

Documento ilustrativo para contrato/teste, sem associação a pessoa real. O mesmo campo document aceita CNPJ numérico, por exemplo 10.359.666/0001-94. Não há endpoint implementado a migrar.

1. Validar corpo e document obrigatório em string. Aceitar CPF de 11 dígitos ou CNPJ numérico de 14 dígitos, com ou sem as respectivas máscaras do projeto. Rejeitar letras, espaços, formato incorreto, tamanho não suportado e dígitos verificadores inválidos.
2. Reutilizar DocumentRules do pacote compartilhado, que identifica CPF/CNPJ e normaliza para comparação/consulta compatível com o cadastro: `XXX.XXX.XXX-XX` ou `XX.XXX.XXX/XXXX-XX`. Validar o formato HTTP antes da normalização; não restringir o fluxo somente a CPF.
3. Consultar diretamente o cliente e seu status no RDS PostgreSQL, via Lambda .NET 10 com credencial de leitura limitada ([integração da função](rfcs/001-EXECUCAO.md)). Usar o mesmo formato canônico de documento do domínio.
4. Cliente existente e Ativo recebe JWT de perfil Customer, conforme seção 6. Não exigir senha nem cadastrar um usuário interno para ele.

| Código | Significado |
|---|---|
| 200 | Token válido no corpo, como string, seguindo a resposta atual de autenticação |
| 401 | Documento válido sem cliente elegível: cadastro inexistente ou Inativo |
| 400 | Corpo ou documento inválido |
| 500 | Falha técnica tratada pelo handler, incluindo configuração, persistência e cancelamento pelo prazo disponível |

Exemplo lógico do valor retornado em 200: `"<jwt>"`. Reutilizar a convenção de resposta de token da autenticação interna, sem introduzir envelope com campos de sessão/refresh token. Falhas inesperadas da função ou de sua integração continuam sujeitas a respostas 5xx de infraestrutura; não mascará-las como 401/400.

**Contrato do handler — parte 4:** exigir `application/json`, aceitando `charset=utf-8` e nomes de cabeçalhos sem distinção de maiúsculas. O corpo contém exatamente uma propriedade `document`, em string obrigatória; rejeitar propriedades extras/duplicadas, nomes diferentes, valores nulos ou de outro tipo e JSON malformado. O transporte Base64 do evento REST proxy é aceito com UTF-8 estrito; erros de parsing retornam 400 antes de carregar configuração ou consultar o banco. Formato/dígitos do documento continuam validados pelo caso de uso.

200 usa `application/json` com token como string JSON. 400/401/500 usam `application/problem+json` com `type: about:blank`, `status` correspondente, `instance: /customers/validate` e mensagens fixas:

| Código | title | detail |
|---|---|---|
| 400 | Requisição inválida | O corpo ou documento informado é inválido. |
| 401 | Autenticação não autorizada | Não foi possível autenticar o cliente. |
| 500 | Erro interno do servidor | Não foi possível processar a autenticação. |

Cadastro inexistente e Inativo produzem exatamente a mesma negativa. Todas as respostas do handler incluem `Cache-Control: no-store` e `isBase64Encoded: false`. Não expor documento, token, chave, senha, configuração ou exceções nos logs/erros. Falhas técnicas e cancelamento nunca são convertidos em documento inválido ou cliente inelegível. Respostas produzidas pelo próprio Gateway/runtime podem seguir outros formatos 5xx.

O contrato estático do produtor fica em [contracts/openapi.json](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/blob/develop/contracts/openapi.json), sem extensões AWS ou destinos de ambiente nesta entrega. Handler e fluxo estão preparados para validação local; IAM/rede/deploy/Gateway e aceitação real do JWT pela API permanecem pendentes.

## 6. JWT e autenticação interna

**Implementação E4 — primeira entrega:** cadastro/login internos aceitam somente Admin/Mechanic, com validação nominal antes do banco; perfis incompatíveis no corpo retornam 400, credenciais incorretas/cadastro ausente retornam 401. O emissor interno acrescenta NameIdentifier usando o UUID persistido, como já faz a Lambda para Customer. Por revisão solicitada em 13/10/2026, os mapeadores constroem o domínio diretamente, sem try/catch: identidade/perfil persistidos inválidos propagam DomainValidationException (400 pelo middleware), sem emitir JWT. Algoritmo, expiração e tolerância são preservados; a validação global ainda aceita tokens anteriores sem ID. A matriz das rotas existentes e o vínculo da OS estão implementados nesta entrega: Mechanic opera a oficina, Admin administra o cadastro de usuários e Customer acessa status/decisão de sua própria OS. Admin não herda permissões operacionais; PATCH próprio e DELETE estão implementados localmente. Validação desta entrega é local; PR/deploy e conferência AWS ficam com o mantenedor.

O JWT segue `Infrastructure/Authentication/JwtTokenGenerator.cs` e a validação de `GerenciamentoMecanicaSistema/Program.cs`:

- Assinatura simétrica **HS256** com a configuração `Jwt:Key`; manter a exigência atual de chave com pelo menos 32 caracteres na API.
- Emissão com expiração em **UTC + 10 minutos**, também para clientes. Preservar as opções de validação atuais, incluindo a tolerância de relógio do middleware; 10 minutos é o `exp` emitido, sem introduzir mudança implícita dessa tolerância.
- Validar assinatura, issuer, audience e lifetime como hoje. Função e API devem ter configurações compatíveis por ambiente.
- A API mantém a validação do Bearer token e a autorização por rota. A REST API do Gateway roteia a validação de documento à Lambda e as operações à API via VPC Link/NLB interno (D01.3–D01.5). Não introduzir authorizer JWT nativo incompatível com HS256. A chave compartilhada fica no Secrets Manager, com referências públicas SSM v2. A infraestrutura da E2.12 prepara Pod Identity da API; API/função carregarão AWSCURRENT pelo SDK .NET na inicialização e manterão em memória, sem fallback local ou cópia para Secret Kubernetes. Implementação da infraestrutura preparada; aplicação e consumo dos runtimes permanecem pendentes. [Detalhamento](CONTRATOS_ENTRE_REPOSITORIOS.md).
- Preservar as claims Name/Role e acrescentar o ID estável como NameIdentifier, ajuste mínimo derivado da regra de Update próprio já registrada em D03.4. Para internos: ID da tabela users, nome e role Admin/Mechanic. Para cliente: ID da tabela customers, nome e role Customer.
- Gerar claims a partir do cadastro consultado. Payload de validação de documento não escolhe ID, role, issuer, audience ou expiração.
- Não adicionar refresh token, blacklist, armazenamento de sessão nem revogação por atualização, remoção ou inativação.

O login **interno** continua em `POST /authentication`, com `name/password/role`, verificação do hash de senha e resposta 200 com token ou 401/400 conforme o fluxo atual. Passa a atender Admin e Mechanic; não é o caminho de validação de clientes. A ampliação de claims não altera algoritmo, formato JWT ou estratégia de sessão.

## 7. Cadastro e status de clientes

**Implementação E4 — primeira entrega:** domínio e persistência da API representam Active/Inactive. `GET /Customers` retorna o campo `status` como string, incluindo clientes em ambos os estados. O cadastro define Active; a atualização preserva o estado carregado e não escreve a coluna status. Status ausente/desconhecido nos dados persistidos é falha técnica, sem fallback para Active. POST/PATCH não incluem status em seus contratos; um campo extra enviado é ignorado e não altera o estado. Fixtures PostgreSQL locais preparam Inactive para validar a preservação, sem operação pública de inativação. Conferência de Inactive real na AWS continua pendente.

- Estados do modelo: Ativo e Inativo.
- Novos clientes são criados como **Ativo** no servidor, independentemente de campo enviado pelo consumidor.
- Criar esquema e seeds de banco vazio com clientes Ativos por padrão. D08.1 do plano dispensa a migração de clientes antigos porque o banco educacional é descartável; Inativo permanece disponível para fixtures de teste.
- **Não implementar operação de inativação nesta etapa**, nem permitir que o PATCH genérico de clientes altere o status indiretamente. Não acrescentar campo editável de status aos contratos atuais.
- A função continua consultando o status e rejeitando Inativo, atendendo ao requisito do PDF. Esse caso pode ser preparado por fixture de teste, sem adicionar endpoint administrativo.
- A regra futura de inativação preserva sessões existentes. Isso não exige implementar a operação agora.
- O DELETE de cliente já existente continua sendo exclusão, sujeito às regras atuais; não convertê-lo implicitamente em inativação.

## 8. Verificações planejadas para E3/E4/E6

- Percorrer a matriz por endpoint, incluindo acesso sem token e perfis sem permissão.
- Comprovar que apenas Admin cadastra/remove usuários e que Admin não opera OSs.
- Verificar Update próprio, tentativa de editar outra conta, alteração de role, login duplicado e troca/reutilização de login com token anterior.
- Verificar todos os códigos definidos para PATCH/DELETE users sem alterar os contratos de erro dos endpoints existentes.
- Validar CPF e CNPJ numérico com/sem máscara, formato/dígitos inválidos para ambos, cadastro ausente, Ativo e fixture Inativo.
- Comprovar que `/customers/validate` executa na função e que seu token é aceito pela API com a configuração JWT escolhida.
- Verificar assinatura, issuer/audience, expiração e isolamento dos perfis internos/cliente.
- Rejeitar consulta/decisão sobre OS de outro cliente e aprovação/recusa com Admin ou Mechanic.
- Validar remoção/Update com token anterior conforme as regras de preservação de sessão.
- Verificar clientes novos/seeds como Ativo e ausência de alteração de status pelos contratos públicos.

## 9. Conclusão desta parte da E0

E0.2 e E0.3 ficam concluídas **como especificação**, com esta matriz e estes contratos. A implementação e a comprovação operacional permanecem em E3/E4/E6.

Atualização em 2026-09-16: a E0 foi concluída como especificação. Componentes, comunicação, observabilidade e responsabilidades estão no [índice de arquitetura](README.md); o próximo trabalho é a E1. A consulta direta da Lambda ao banco foi consolidada em D01.5. Em 2026-10-02, D01.6 mudou o motor para RDS PostgreSQL sem reabrir as regras de negócio deste documento.

Atualização em 2026-09-25, solicitada pelo usuário: a validação é do documento do cliente (CPF ou CNPJ), ampliando o requisito mínimo de CPF. O campo planejado da requisição passa de cpf para document. Esta definição substitui a restrição anterior a CPF; cadastro/status, JWT e permissões permanecem iguais.
