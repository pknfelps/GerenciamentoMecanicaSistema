# Decisões de acesso e autenticação — D02/D03

[Índice central](../../../PLANO_FASE_3.md)

> Decisões preservadas com suas datas. Para estado/etapa atual, consultar o índice central; complementos posteriores prevalecem sobre pendências antigas. Ler somente o assunto necessário.

<a id="d03-1"></a>

**D03.1 — Abertura de OS exclusiva da oficina (aceita em 2026-09-13):**

- **Origem:** definição explícita do usuário nesta sessão: a abertura continua exclusiva da oficina; não houve solicitação de criação pelo cliente.
- **Regra vigente:** abertura por usuário da oficina com o perfil operacional Mechanic (D03.5). O JWT de cliente não concede permissão para criar OS. A atribuição inicial a Admin foi substituída pela separação de perfis definida posteriormente nesta sessão.
- **Motivação:** manter o fluxo de negócio atual e o escopo solicitado.
- **Alternativa descartada:** disponibilizar abertura de OS ao cliente autenticado por CPF.
- **Consequências:** manter `POST /Orders` restrito à oficina, refletir esse ator no diagrama de abertura de OS e incluir na E4/E6 a verificação de rejeição de criação com token de cliente.
- **Estado:** decisão de escopo aceita; permissões especificadas em E0.3; implementação pendente.

<a id="d03-2"></a>

**D03.2 — Estados do cliente e elegibilidade para autenticação (aceita em 2026-09-13):**

- **Origem:** usuário confirmou que Ativo e Inativo são suficientes, com as regras de autenticação propostas nesta sessão.
- **Estados:** somente **Ativo** e **Inativo**.
- **Regra:** cliente Ativo pode autenticar por CPF e receber JWT, atendidas a validação do CPF e a existência no cadastro. Cliente Inativo não recebe novos tokens.
- **Motivação:** atender à consulta de status exigida no desafio com os dois estados considerados suficientes para o negócio.
- **Alternativa descartada:** acrescentar estados adicionais sem necessidade identificada.
- **Consequências:** refletir os estados no domínio, persistência e contratos; a função consulta o status antes de emitir o token; validar autenticação de ativo e rejeição de inativo em E3/E4.
- **Complemento aceito:** inativação não afeta sessão existente (D02.1). D03.6 define estado inicial Ativo e adia a operação de inativação; D08.1 substitui a migração de clientes pela criação/seeds em banco vazio.
- **Estado:** regra especificada; implementação pendente em E3/E4. Não criar endpoint de alteração de status agora.

<a id="d03-3"></a>

**D03.3 — Aprovação/recusa exclusiva do cliente (aceita em 2026-09-13):**

- **Origem e motivação:** usuário determinou que a mecânica não pode atuar na decisão do orçamento.
- **Regra:** somente o cliente dono da OS pode aprovar ou recusar seu orçamento; perfil administrativo não concede essa permissão.
- **Alternativa descartada:** permitir que a oficina aprove/recuse em nome do cliente.
- **Consequências:** proteger `PATCH /Orders/{id}/budget` por identidade de cliente e vínculo com a OS; testar rejeição de tokens Admin, Mechanic e de outro cliente. Separar autorização de gestão da OS da autorização para decidir o orçamento.
- **Estado:** regra aceita; implementação pendente.

<a id="d02-1"></a>

**D02.1 — Sessões existentes e alterações cadastrais (aceita em 2026-09-13):**

- **Origem e motivação:** usuário escolheu preservar sessões existentes e evitar a complexidade de revogação para estes casos.
- **Cliente inativado:** não recebe novos tokens, mas o token anterior permanece válido até expirar. Não bloquear requisições apenas porque o status mudou depois da emissão.
- **Usuário da oficina removido:** não pode realizar novo login com a conta removida; tokens anteriores permanecem válidos até expirar. Não condicionar toda validação de JWT à existência atual dessa conta.
- **Usuário atualizado:** não implementar revogação deliberada em decorrência do Update. Se a alteração tornar o token inválido pela estratégia adotada, exigir novo login; se continuar válido, preservá-lo.
- **Alternativa descartada:** acrescentar blacklist, versionamento de sessão ou outra infraestrutura de revogação para essas operações.
- **Limites:** assinatura, expiração e permissões do token continuam sendo verificadas. Preservar a sessão não recria um registro removido nem garante sucesso em operações que precisam desse registro. Não adicionar renovação automática da sessão como consequência destas regras.
- **Estado:** comportamento aceito; D02.2 determina seguir o JWT atual, incluindo expiração emitida em 10 minutos também para clientes. A operação de inativação foi adiada por D03.6, preservando sua regra futura de sessão.

<a id="d03-4"></a>

**D03.4 — Update e Remove de usuários da oficina (aceita e complementada em 2026-09-13):**

- **Origem e motivação:** usuário solicitou completar as operações de cadastro da mecânica com Update e Remove, mantendo a solução simples.
- **Update:** somente o próprio usuário autenticado pode alterar sua conta/login; nenhum usuário pode alterar o login de outro. Aplicar D02.1 ao comportamento do token após a atualização.
- **Remove:** operação exclusiva de Admin para remover usuários da oficina; Mechanic não pode remover usuários, inclusive a própria conta. Preservar tokens existentes conforme D02.1. Permissão definida pelo usuário em D03.5, encerrando P04.
- **Consequência técnica derivada:** identificar o titular pelo ID estável do usuário, obtido da identidade autenticada, sem confiar em nome/login ou ID informado no corpo como prova de propriedade. O JWT atual tem apenas nome/perfil; planejar inclusão do ID e atualização dos contratos de geração/validação e testes. Isso evita que troca ou reutilização de login mude o titular da sessão.
- **Alternativas descartadas:** Update de outra conta e revogação deliberada de sessões para Remove/Update.
- **Contrato consolidado:** `PATCH /users` recebe nome/senha opcionais (ao menos um), com alvo no ID autenticado; `DELETE /users?id=<uuid>` identifica a conta a remover. Ambos retornam 204/401/400/500 conforme a especificação. Update próprio não altera perfil.
- **Estado:** escopo, permissões e contratos definidos; implementação prevista na E4.

<a id="d03-5"></a>

**D03.5 — Separação de Admin e mecânicos e nomenclatura das roles (definida em 2026-09-13):**

- **Origem:** usuário determinou que Admin registre/apague usuários e que outro perfil opere as OSs e possa atualizar o próprio registro; delegou a sugestão do nome desse perfil.
- **Nome sugerido e adotado neste plano:** `Mechanic`, por identificar diretamente o mecânico e acompanhar a nomenclatura em inglês usada no código.
- **Admin:** responsável por cadastrar e remover usuários da oficina. Não herda as permissões operacionais de Mechanic nem pode aprovar/recusar orçamento. A regra anterior de atualização da própria conta continua aplicável, sem permitir alterar o login de terceiros.
- **Mechanic:** responsável pelo gerenciamento operacional das OSs existente: abertura, consultas operacionais/detalhadas, diagnóstico, itens, orçamento gerado ao finalizar diagnóstico, execução, finalização, entrega e exclusão conforme as regras de negócio. Pode atualizar somente o próprio cadastro; não pode cadastrar/remover usuários nem decidir o orçamento do cliente.
- **Cliente:** identidade separada dos usuários internos; aprovação/recusa restrita à própria OS. D02.2 especifica claim de perfil Customer, sem cadastro na tabela de usuários internos nem concessão de Admin/Mechanic.
- **Roles atuais verificadas:** `User`, `Manager` e `Admin`, em `Domain.Interface/User/Roles.cs`; os controllers protegidos atualmente exigem `Admin`.
- **Ajuste de nomenclatura proposto:** manter `Admin`, substituir `User` por `Mechanic` e retirar `Manager` do modelo de usuários internos, pois não há responsabilidade distinta atribuída a esse perfil. O modelo alvo interno terá duas roles; o cliente não é cadastrado como usuário da oficina.
- **Atualização derivada:** atualizar enum, validações, claims, políticas, contratos, seeds, Postman e testes. A proposta anterior de converter cadastros existentes foi dispensada por D08.1; criar dados novos com Admin/Mechanic explicitamente definidos, preservando a distinção administrativa.
- **Alternativa substituída:** Admin com acesso geral a todas as operações. Esta decisão atualiza D03.1 e resolve a permissão pendente de D03.4/P04.
- **Limites:** D02.1 permanece válido. Na matriz completa, Admin consulta usuários; Mechanic consulta somente a própria conta e gerencia cadastros operacionais auxiliares.
- **Estado:** separação de responsabilidades definida pelo usuário; nome Mechanic e simplificação das roles registrados como proposta de implementação; código ainda não alterado.

**Resumo da matriz consolidada:**

| Operação | Admin | Mechanic | Cliente | Estado |
|---|---|---|---|---|
| Cadastrar usuários da oficina | Permitido | Negado | Negado | Aceito — D03.5 |
| Remover usuários da oficina | Permitido | Negado | Negado | Aceito — D03.4/D03.5 |
| Atualizar usuário da oficina | Somente a própria conta | Somente a própria conta | Negado | D03.4/D03.5; sem alteração de role |
| Abrir e gerenciar OSs (diagnóstico, itens, execução, entrega e exclusão) | Negado | Permitido | Negado | D03.1 atualizada por D03.5; regras de negócio preservadas |
| Aprovar/recusar orçamento | Negado | Negado | Somente da própria OS | Aceito — D03.3 |
| Consultar status da OS | Negado | Permitido | Somente da própria OS | Detalhado na especificação por endpoint |
| Gerenciar clientes, veículos, estoque e catálogo | Negado | Permitido | Negado | Distribuição operacional aplicada à matriz por endpoint |

<a id="d02-2"></a>

**D02.2 — Validate serverless e JWT atual (definida em 2026-09-13):**

- **Origem:** usuário definiu validação de CPF pela função, sem login formal de cliente; retorno 200 com token ou 401/400 e JWT seguindo o projeto atual.
- **Contrato:** `POST /customers/validate`, corpo com `cpf`; aceita CPF com/sem máscara, reutiliza validação do domínio e consulta existência/status. Gateway encaminha para a função.
- **JWT:** HS256, expiração UTC + 10 minutos, configurações issuer/audience/key compatíveis e validação Bearer na API como hoje. Preservar Name/Role e acrescentar ID estável conforme D03.4; perfil Customer para clientes. Sem refresh token ou revogação adicional.
- **Alternativa descartada:** login de cliente com senha ou outra etapa além da validação serverless. O login interno em `/authentication` continua para Admin/Mechanic.
- **Users confirmado no mesmo avanço:** PATCH/DELETE em `/users`, com 204/401/400/500; campos, alvo e casos de erro concretizados na especificação.
- **Estado:** contrato definido; integração privada da API definida em D01.4 e acesso direto da função ao Aurora em D01.5; detalhamento operacional por ambiente em E0.1/E0.4.

<a id="d03-6"></a>

**D03.6 — Clientes Ativos; inativação adiada (definida em 2026-09-13):**

- **Origem:** usuário definiu estado inicial Ativo e que a invalidação não será implementada agora; no contexto de cadastro, a operação de inativação fica fora desta etapa. Revogação de sessões já estava excluída em D02.1.
- **Regra:** criar clientes como Ativo. A migração de registros existentes, inicialmente prevista, foi dispensada por D08.1; aplicar o estado padrão ao esquema/seeds de banco vazio. Manter estados Ativo/Inativo e consulta de status pela função.
- **Limite:** não criar endpoint de inativação nem permitir mudança de status pelo PATCH genérico. Testar rejeição de Inativo com fixture. O DELETE de cliente mantém seu significado de exclusão.
- **Alternativa adiada:** operação administrativa de alteração de status. Preservar a regra futura de sessão existente.
- **Estado:** especificado; implementar estado inicial no esquema, criação e seeds na E4, sem migração de registros antigos (D08.1).

**Evidência de consolidação de E0.2/E0.3:** [Acesso e autenticação](../../arquitetura/ACESSO_E_AUTENTICACAO.md), com matriz dos endpoints existentes e novos, contratos de users, Validate e JWT. Próximo trabalho: E0.1/E0.4. RFCs, ADRs e demais diagramas serão consolidados em `docs/arquitetura/` durante E0.7.
