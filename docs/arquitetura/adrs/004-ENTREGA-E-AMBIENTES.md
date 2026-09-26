# ADR 004 — Quatro repositórios e ambientes independentes

**Estado:** aceito como arquitetura; implementação pendente. **Formalização:** 2026-09-16. [Índice](../README.md)

**Contexto:** a entrega exige quatro repositórios/pipelines e hom/prd capazes de coexistir. Desligamento para economia não pode destruir dependências compartilhadas nem o outro ambiente.

**Decisão:** separar aplicação, infraestrutura, banco e autenticação; develop entrega em hom, main em prd. Usar estados S3 versionados com lock nativo por componente/ambiente, OIDC, metadados SSM e secrets no Secrets Manager. ECR e bucket separado de artefatos guardam versões fixas; contratos OpenAPI e pacote CPF/CNPJ e JWT têm produtores definidos. Bootstrap compartilhado tem ciclo independente.

**Alternativas:** estado local dificulta coordenação/auditoria; credenciais AWS duráveis no CI foram substituídas por OIDC; DynamoDB não é necessário para o locking escolhido. Reutilizar os mesmos recursos para hom/prd ou impor exclusão global entre ambientes contraria a coexistência. Docker Hub foi substituído por ECR.

**Consequências:** contratos/versionamento e ordem de deploy devem ser explícitos. Lock S3 não coordena componentes diferentes, e concurrency do GitHub não atravessa repositórios. Há custos de armazenamento/operações e de cada ambiente ativo; coexistência não significa manter ambos ligados continuamente.

**Comprovação:** E1/E2/E6 devem validar proteções, identidade restrita, versões consumidas, manifesto real e destruição isolada. [RFC 002](../rfcs/002-ENTREGA.md).
