# ADR 001 — Separar identidade interna e acesso do cliente

**Estado:** aceito como arquitetura; implementação pendente. **Formalização:** 2026-09-16. [Índice](../README.md)

**Contexto:** o uso de Admin para toda operação não distingue gestão de usuários, trabalho da oficina e decisão do cliente. A fase exige validação de CPF em função serverless.

**Decisão:** adotar Admin e Mechanic para usuários internos e role Customer no JWT do cliente. Admin cadastra/remove usuários; usuários internos atualizam somente o próprio registro. Mechanic gerencia a operação, incluindo abertura de OS. Aprovação/recusa é exclusiva de Customer, cujo acesso às OS se limita às próprias. Lambda valida CPF/cadastro Ativo diretamente no Aurora e emite JWT compatível com a API, que valida identidade, role e propriedade. Preservar sessões sem mecanismo adicional de revogação.

**Alternativas:** manter Admin para tudo foi substituído pela separação de permissões. Login de cliente com senha, criação de OS pelo cliente e revogação centralizada não fazem parte do escopo aceito.

**Consequências:** é necessário ID estável no JWT, pacote compartilhado de CPF/JWT e aplicação da matriz a cada endpoint. Update/Remove/inativação não encerram automaticamente tokens existentes; operações continuam sujeitas à existência e ao vínculo dos recursos.

**Comprovação:** implementar em E3/E4 e testar em E6 emissão/consumo do token, isolamento entre clientes e atualização exclusiva do próprio usuário. [Contrato normativo](../ACESSO_E_AUTENTICACAO.md) · [RFC 001](../rfcs/001-EXECUCAO.md).
