# Requisitos, escopo e inventário da Fase 3

[Índice central](../../PLANO_FASE_3.md)

## 2. Objetivo e limites de escopo

Evoluir a solução da Fase 2 para operar com API Gateway, autenticação serverless por CPF, banco gerenciado, Kubernetes escalável, provisionamento com Terraform, quatro repositórios com CI/CD, observabilidade e documentação arquitetural completa.

A aplicação .NET e suas regras de negócio existentes são a base de evolução. O PDF exige separar responsabilidades em quatro repositórios; não exige decompor todos os módulos de negócio em microsserviços.

A expansão para múltiplas oficinas é contexto do desafio. Cadastro de filiais, isolamento por unidade e outras funcionalidades associadas não são requisitos explícitos e só devem entrar no escopo por decisão registrada.

**Abertura de OS:** permanece exclusiva da oficina, conforme decisão explícita do usuário em 2026-09-13. A autenticação por CPF não acrescenta criação de OS pelo cliente ao escopo da Fase 3.

**Aprovação de orçamento:** aprovar ou recusar é atribuição exclusiva do cliente dono da OS; a oficina não pode decidir em seu lugar (D03.3).

**Extensão solicitada pelo usuário em 2026-09-13:** adicionar Update e Remove para usuários da mecânica. Update é exclusivo da própria conta. Remove não encerra sessões existentes. Esses métodos são escopo adicional desta sessão, não requisitos explícitos do PDF (U01/U02 e D03.4).

**Separação de perfis internos:** Admin registra e remove usuários; Mechanic é o nome sugerido para o perfil dos mecânicos que opera as OSs e atualiza o próprio cadastro. A administração de usuários não concede acesso automático à operação de OSs. Aprovação/recusa continua exclusiva do cliente (D03.5).

**Validação de cliente:** `POST /customers/validate` é atendido pela função serverless via Gateway; valida CPF/cadastro/status e retorna 200 com token ou 401/400, sem login formal de cliente. JWT segue o padrão atual (D02.2).

**Estado inicial:** clientes começam Ativos. Não implementar operação de inativação nesta etapa; manter o modelo Ativo/Inativo e a verificação de elegibilidade na função (D03.6).

**Notificações serverless:** o contexto do PDF menciona autenticação e notificações serverless, mas a seção de requisitos obrigatórios detalha somente a função de autenticação. O usuário definiu manter notificações na API e apresentar sua migração como melhoria futura, fora da implementação desta entrega (D06.1).

## 3. Inventário inicial: o que pode ser aproveitado

| Área | Evidência local | Lacuna para a Fase 3 |
|---|---|---|
| API e domínio | Solução .NET com camadas de domínio, aplicação, infraestrutura e testes | Adaptar autenticação de clientes e instrumentar os fluxos |
| Autenticação | `Service/AuthenticationService.cs`, `Infrastructure/Authentication/JwtTokenGenerator.cs` | Login atual é de usuário com senha e JWT emitido na aplicação |
| Usuários da oficina (reconferido em 2026-09-13) | `UsersController`, `IUserService`, `UserService`, `IUserRepository` | Somente cadastro/consulta; faltam Update/Remove. JWT administrativo contém nome/perfil e expira em 10 minutos; não inclui o ID do usuário |
| Autorização de OS | `GerenciamentoMecanicaSistema/Controllers/OrdersController.cs` | Predomínio de `Admin`; consulta de status e aprovação de orçamento permitem acesso anônimo |
| CPF e clientes | `Domain/Customer/Cpf.cs`, `Domain/Customer/Customer.cs` | Há validação de CPF, mas não há status de cliente no modelo atual |
| Ciclo de OS | `Domain/WorkOrder/Order.cs` | Criação e finalização não bastam para calcular duração por status |
| Banco | `deploy/kubernetes/DbEntrypoint/Init.sql` e manifestos PostgreSQL | Banco executado dentro do cluster; falta banco gerenciado e inicialização reproduzível do esquema em banco vazio (D08.1) |
| Kubernetes | `deploy/kubernetes/` | Existem probes e HPA; validar operação e integrar a nova arquitetura |
| Terraform | `deploy/terraform/` | Infraestrutura EKS existente; execução manual e backend local |
| CI/CD | `.github/workflows/pipeline.yml` | Pipeline única; falta automação dos quatro repositórios e distinção de ambientes |
| Logs e notificações | Middleware de exceções, `Service/Events/OrderNotificationEventHandler.cs` | Manter envio de e-mail na API (D06.1); integrar observabilidade conforme D04.1 |
| Documentação | `README.md`, `postman/` | Atualizar para Fase 3 e complementar diagramas, RFCs, ADRs e evidências |

## 4. Matriz de requisitos e rastreabilidade

| ID | Requisito obrigatório do PDF | Etapas | Evidência esperada |
|---|---|---|---|
| R01 | API Gateway para controle e roteamento | E0, E3, E6 | Configuração versionada e requisições pelo Gateway |
| R02 | Função valida CPF, consulta existência/status e emite JWT | E0, E2, E3 | Código, testes e demonstração do fluxo |
| R03 | Rotas sensíveis protegidas pela autenticação por CPF | E0, E4, E6 | Matriz de permissões e testes de acesso |
| R04 | Quatro repositórios separados, cada um com CI/CD e deploy automático | E1, E2, E3, E6 | Links e execuções das quatro pipelines |
| R05 | Main/master protegida, sem commits diretos, merge por PR | E1, E6 | Configuração de proteção e PRs de exemplo |
| R06 | Deploy automático de homologação e produção | E1, E2, E3, E6 | Mapeamento branch/ambiente e execuções de deploy |
| R07 | Banco gerenciado provisionado por Terraform | E2 | Terraform e banco acessível pela aplicação |
| R08 | Kubernetes escalável e provisionamento com Terraform | E2, E6 | Terraform, workloads saudáveis e evidência de escala |
| R09 | Latência, CPU/memória, healthchecks, uptime e alertas de falhas de OS | E5, E6 | Monitoramento e alerta validado |
| R10 | Logs JSON com correlação entre requisições | E5, E6 | Logs correlacionados; traces também demonstrados no vídeo |
| R11 | Dashboards de volume diário de OS, tempo por status e falhas de integração | E4, E5, E6 | Dashboards alimentados por fluxos reais de teste |
| R12 | Componentes, sequências, RFCs, ADRs, justificativa do banco e ER | E0 a E7 | Documentação consistente com a implementação |
| R13 | README por repositório, instruções, arquitetura, Swagger/Postman; Dockerfiles e links de deploy quando aplicáveis | E1 a E7 | Revisão dos quatro repositórios |
| R14 | Vídeo de até 15 minutos com os fluxos e a operação exigidos | E7 | Link de YouTube/Vimeo público ou não listado |
| R15 | PDF único com links e confirmação de acesso de soat-architecture | E7 | PDF final e confirmação nos quatro repositórios |

Requisitos adicionais definidos pelo usuário nesta sessão:

| ID | Requisito | Etapas | Evidência esperada |
|---|---|---|---|
| U01 | Update de usuário da oficina exclusivo da própria conta; não revogar tokens deliberadamente; exigir novo login se o token deixar de ser válido | E0, E4, E6 | Contrato, teste de atualização própria e rejeição de alteração de outro usuário, comportamento do token demonstrado |
| U02 | Remove de usuário da oficina exclusivo de Admin, sem efeito sobre sessões existentes | E0, E4, E6 | Rejeição de remoção por Mechanic, remoção persistida por Admin, login posterior negado e token anterior ainda aceito até expirar |
| U03 | Separar Admin (cadastro/remoção de usuários) e perfil de mecânicos (operação de OSs e atualização própria); revisar as roles legadas | E0, E4, E6 | Roles, políticas, dados e contratos atualizados; testes da matriz de permissões |
