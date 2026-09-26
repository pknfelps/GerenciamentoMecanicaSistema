# E0 — Especificação de acesso e autenticação

- **Data:** 2026-09-13.
- **Estado:** especificação consolidada para implementação em E3/E4; não representa funcionalidade já implantada.
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
| PATCH | `/users` | Próprio | Próprio | Não | Não | API — nova |
| DELETE | `/users` | Sim | Não | Não | Não | API — nova |
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
- **GET users:** Admin mantém a consulta atual por `name/role`. Para Mechanic, a consulta é vinculada ao ID autenticado; filtros que tentem selecionar outra conta são rejeitados e não retornam dados de terceiros.
- **OS do cliente:** carregar a OS e verificar seu vínculo com o cliente identificado no token. Como hoje a OS referencia o documento do cliente, resolver a identidade pelo ID e conferir o documento correspondente; a existência dessa consulta não implica bloquear a sessão pelo status atual.
- **Aprovação:** manter o campo `approved`. O `customerDocument` existente, enquanto mantido por compatibilidade, deve corresponder à identidade autenticada e nunca substitui a verificação do titular. Admin e Mechanic são rejeitados mesmo que informem o documento correto.

## 3. PATCH /users — atualizar a própria conta

**Autorização:** Admin ou Mechanic, somente a própria conta.

```http
PATCH /users
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "name": "mecanico.atualizado",
  "password": "NovaSenha@123"
}
```

- `name` e `password` são campos opcionais de atualização; pelo menos um deve ser informado. Omitido significa preservar o valor atual; nulo/vazio explícito é inválido.
- Não aceitar `id` ou `role` como campos editáveis. Identidade vem do token; role permanece a do cadastro.
- Reutilizar validações de nome, senha e hash existentes. Não retornar senha/hash.
- Preservar a verificação de unicidade de login/perfil aplicada pelo cadastro atual, excluindo a própria conta dessa busca.
- Usar ID estável para a atualização; trocar nome/login não transfere a sessão a outra pessoa.

| Código | Significado |
|---|---|
| 204 | Atualização concluída, sem corpo |
| 401 | Token ausente/inválido/expirado, perfil não permitido ou tentativa de operar sobre outra identidade |
| 400 | Payload inválido, campos proibidos, nenhum campo alterável informado, login em conflito ou conta alvo já inexistente |
| 500 | Falha inesperada de execução/persistência |

Após mudar nome/senha, o token assinado permanece válido até expirar na estratégia aqui definida; não consultar senha ou nome atual para revalidar cada requisição. O ID mantém a identificação da conta. O próximo login usa os dados novos. Se o token já for inválido por outra razão, exigir novo login, sem renovação automática.

## 4. DELETE /users — remover usuário

**Autorização:** somente Admin. Para manter a rota `/users`, identificar o alvo por query obrigatória `id` (UUID), sem corpo.

```http
DELETE /users?id=<uuid-do-usuario>
Authorization: Bearer <token-admin>
```

| Código | Significado |
|---|---|
| 204 | Remoção concluída, sem corpo |
| 401 | Token ausente/inválido/expirado ou perfil diferente de Admin |
| 400 | ID ausente/inválido ou usuário não encontrado |
| 500 | Falha inesperada de execução/persistência |

Mechanic não pode remover outros usuários nem a própria conta. A remoção impede nova autenticação da conta removida, mas não revoga tokens anteriores. Uma operação que dependa de um registro removido pode falhar por ausência do recurso, sem invalidar a sessão inteira. Não acrescentar regras de proteção do último Admin ou de autoexclusão sem requisito adicional.

### Adequação das respostas dos novos métodos de users

Os códigos 204/401/400/500 foram definidos explicitamente pelo usuário. O middleware atual também possui mapeamentos 404/409, e a autorização por perfil pode produzir resposta de acesso negado distinta de 401. Na implementação, adequar **estas novas operações** ao contrato: ausência/conflito de alvo como 400 e negativas de acesso como 401. Preservar os contratos existentes das outras rotas. Não transformar falhas de infraestrutura em erro de credencial.

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
3. Consultar diretamente o cliente e seu status no Aurora PostgreSQL, via Lambda .NET 10 com credencial de leitura limitada ([integração da função](rfcs/001-EXECUCAO.md)). Usar o mesmo formato canônico de documento do domínio.
4. Cliente existente e Ativo recebe JWT de perfil Customer, conforme seção 6. Não exigir senha nem cadastrar um usuário interno para ele.

| Código | Significado |
|---|---|
| 200 | Token válido no corpo, como string, seguindo a resposta atual de autenticação |
| 401 | Documento válido sem cliente elegível: cadastro inexistente ou Inativo |
| 400 | Corpo ou documento inválido |

Exemplo lógico do valor retornado em 200: `"<jwt>"`. Reutilizar a convenção de resposta de token da autenticação interna, sem introduzir envelope com campos de sessão/refresh token. Falhas inesperadas da função ou de sua integração continuam sujeitas a respostas 5xx de infraestrutura; não mascará-las como 401/400.

## 6. JWT e autenticação interna

O JWT segue `Infrastructure/Authentication/JwtTokenGenerator.cs` e a validação de `GerenciamentoMecanicaSistema/Program.cs`:

- Assinatura simétrica **HS256** com a configuração `Jwt:Key`; manter a exigência atual de chave com pelo menos 32 caracteres na API.
- Emissão com expiração em **UTC + 10 minutos**, também para clientes. Preservar as opções de validação atuais, incluindo a tolerância de relógio do middleware; 10 minutos é o `exp` emitido, sem introduzir mudança implícita dessa tolerância.
- Validar assinatura, issuer, audience e lifetime como hoje. Função e API devem ter configurações compatíveis por ambiente.
- A API mantém a validação do Bearer token e a autorização por rota. A REST API do Gateway roteia a validação de documento à Lambda e as operações à API via VPC Link/NLB interno (D01.3–D01.5). Não introduzir authorizer JWT nativo incompatível com HS256. A chave compartilhada fica no Secrets Manager; detalhar sua disponibilização à API/função na infraestrutura.
- Preservar as claims Name/Role e acrescentar o ID estável como NameIdentifier, ajuste mínimo derivado da regra de Update próprio já registrada em D03.4. Para internos: ID da tabela users, nome e role Admin/Mechanic. Para cliente: ID da tabela customers, nome e role Customer.
- Gerar claims a partir do cadastro consultado. Payload de validação de documento não escolhe ID, role, issuer, audience ou expiração.
- Não adicionar refresh token, blacklist, armazenamento de sessão nem revogação por atualização, remoção ou inativação.

O login **interno** continua em `POST /authentication`, com `name/password/role`, verificação do hash de senha e resposta 200 com token ou 401/400 conforme o fluxo atual. Passa a atender Admin e Mechanic; não é o caminho de validação de clientes. A ampliação de claims não altera algoritmo, formato JWT ou estratégia de sessão.

## 7. Cadastro e status de clientes

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

Atualização em 2026-09-16: a E0 foi concluída como especificação. Componentes, comunicação, observabilidade e responsabilidades estão no [índice de arquitetura](README.md); o próximo trabalho é a E1. A consulta direta da Lambda ao Aurora foi consolidada em D01.5. Essas decisões de infraestrutura não reabrem as regras de negócio deste documento.

Atualização em 2026-09-25, solicitada pelo usuário: a validação é do documento do cliente (CPF ou CNPJ), ampliando o requisito mínimo de CPF. O campo planejado da requisição passa de cpf para document. Esta definição substitui a restrição anterior a CPF; cadastro/status, JWT e permissões permanecem iguais.
