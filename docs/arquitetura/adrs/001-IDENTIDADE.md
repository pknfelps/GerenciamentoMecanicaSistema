> **Revisão AE.9 — 23/10/2026:** [ADR 011](011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md) substitui emissão por documento isolado. Não há Authorizer ou token por OS.

# ADR 001 — Separar identidade interna e acesso do cliente

**Estado:** aceito; base anterior validada em hom. Refinamento AE implementado/validado localmente, publicação AWS pendente na AE.10. Formalização original: 16/09/2026.

**Contexto:** gestão de usuários, operação da oficina e decisão do cliente exigem permissões diferentes. A validação cadastral do cliente deve ocorrer na função serverless.

**Decisão vigente:** users contém somente Admin/Mechanic. Admin cadastra/consulta/remove usuários; ambos atualizam somente a própria conta pelo UUID do JWT. Mechanic opera cadastros e OS; Admin não herda esse acesso. Customer é emitido pela Lambda após CPF/CNPJ e código de uso único para customers Active, sem senha, e só consulta status/decide orçamento das próprias OSs. A API verifica JWT/perfil/vínculo, sem authorizer adicional no Gateway.

NameIdentifier UUID é emitido nos dois tipos de token. O UUID obrigatório é conferido nas operações próprias; outras rotas preservam autenticação por assinatura/perfil sem consulta global ao cadastro. Senha é hash PBKDF2, JWT usa HS256/dez minutos e a validação usa ClockSkew zero. PATCH/DELETE não implementam revogação centralizada; as duas operações Customer verificam cadastro Active e vínculo, bloqueando inativação após emissão. Recriar nome não transfere identidade.

**Alternativas:** Admin para tudo foi substituído. Login de cliente com senha, refresh/revogação centralizada e criação de OS por cliente não pertencem ao escopo.

**Consequências e prova:** autorização e isolamento são responsabilidade da API. Negativas, PATCH próprio, DELETE Admin, tokens anteriores e vínculo Customer/OS foram conferidos em hom. Não há proteção contra autoexclusão/último Admin nem endpoint de inativação. [Contrato normativo](../ACESSO_E_AUTENTICACAO.md) e [sequência](../diagramas/VALIDACAO_CPF.md).

Revisão documental de 21/10/2026. Implementação e validações de hom não comprovam prd ou testes dispensados. [Arquitetura](../README.md) · [Evidências E6](../../REVISAO_E6_2026-10-21.md).

<details>
<summary>Histórico anterior à revisão E7 — registros preservados, sem vigência operacional</summary>

Os estados locais, planos pendentes e alternativas abaixo registram o momento em que foram escritos. A decisão e o alcance atuais estão acima; este histórico não autoriza operações nem reabre pendências aceitas.

**Estado:** aceito como arquitetura; implementação pendente. **Formalização:** 2026-09-16. [Índice](../README.md)

**Contexto:** o uso de Admin para toda operação não distingue gestão de usuários, trabalho da oficina e decisão do cliente. A fase exige validação de CPF em função serverless.

**Decisão:** adotar Admin e Mechanic para usuários internos e role Customer no JWT do cliente. Admin cadastra/remove usuários; usuários internos atualizam somente o próprio registro. Mechanic gerencia a operação, incluindo abertura de OS. Aprovação/recusa é exclusiva de Customer, cujo acesso às OS se limita às próprias. Lambda valida documento (CPF/CNPJ)/cadastro Ativo diretamente no RDS PostgreSQL (D01.6) e emite JWT compatível com a API, que valida identidade, role e propriedade. Preservar sessões sem mecanismo adicional de revogação.

**Alternativas:** manter Admin para tudo foi substituído pela separação de permissões. Login de cliente com senha, criação de OS pelo cliente e revogação centralizada não fazem parte do escopo aceito.

**Consequências:** é necessário ID estável no JWT, pacote compartilhado de CPF/CNPJ e JWT e aplicação da matriz a cada endpoint. Update/Remove/inativação não encerram automaticamente tokens existentes; operações continuam sujeitas à existência e ao vínculo dos recursos.

**Comprovação:** implementar em E3/E4 e testar em E6 emissão/consumo do token, isolamento entre clientes e atualização exclusiva do próprio usuário. [Contrato normativo](../ACESSO_E_AUTENTICACAO.md) · [RFC 001](../rfcs/001-EXECUCAO.md).

</details>

## Extra AE.7 — implementação local de 22/10/2026

[Envio centralizado e acesso Customer](../../AE7_API_ENVIO_E_ACESSO_CUSTOMER.md): API usa Invoke v1 em todos os modos, sem transporte SMTP próprio ou logging de simulação. Cadastro Active/titularidade são conferidos nas operações Customer, orçamento aceita somente approved booleano e retorna decisão após commit, e ClockSkew é zero. Falhas de notificação continuam tratadas após persistência. Captura SMTP4dev não comprova entrega externa; mode=smtp identifica a captura central. Preparado/comprovado localmente com 1.212 testes; publicação/Gateway e aceite AWS permanecem nas etapas extras posteriores, sem reabrir aceites oficiais.
