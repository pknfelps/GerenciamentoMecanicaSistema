# ADR 004 — Quatro repositórios e ambientes independentes

> **Revisão vigente (2026-10-07):** [ADR 008](008-INFRAESTRUTURA-DECLARATIVA.md) substitui controller e protocolo de releases/prontidão por NLB Terraform, NodePort, SSM v2 e planos salvos com aprovação. Os complementos anteriores abaixo são histórico; demais decisões permanecem.

**Estado:** aceito como arquitetura; implementação pendente. **Formalização:** 2026-09-16. [Índice](../README.md)

**Contexto:** a entrega exige quatro repositórios/pipelines e hom/prd capazes de coexistir. Desligamento para economia não pode destruir dependências compartilhadas nem o outro ambiente.

**Decisão:** separar aplicação, infraestrutura, banco e autenticação; develop entrega em hom, main em prd. Usar estados S3 versionados com lock nativo por componente/ambiente, OIDC, metadados SSM e secrets no Secrets Manager. ECR e bucket separado de artefatos guardam versões fixas; contratos OpenAPI e pacote CPF/CNPJ e JWT têm produtores definidos. Bootstrap compartilhado tem ciclo independente.

**Alternativas:** estado local dificulta coordenação/auditoria; credenciais AWS duráveis no CI foram substituídas por OIDC; DynamoDB não é necessário para o locking escolhido. Reutilizar os mesmos recursos para hom/prd ou impor exclusão global entre ambientes contraria a coexistência. Docker Hub foi substituído por ECR.

**Consequências:** contratos/versionamento e ordem de deploy devem ser explícitos. Lock S3 não coordena componentes diferentes, e concurrency do GitHub não atravessa repositórios. Há custos de armazenamento/operações e de cada ambiente ativo; coexistência não significa manter ambos ligados continuamente.

**Complemento em 2026-10-01 — prontidão da base por consumidor:** acrescentar `base/v1/database-release` para o banco, mantendo `base/v1/release` completo para API/função/Gateway. O contrato anterior obrigava o banco a aguardar NLB/JWT, embora dependa de rede, EKS, namespace, SGs e acesso ao Job. A extensão 1.1.0 permite implementar Aurora/esquema antes desses componentes sem reduzir a garantia da release existente. Ambos os manifestos pertencem à infraestrutura, usam a mesma geração e são invalidados antes de mutações/descarte; a coordenação por ambiente continua necessária. Alternativas descartadas: publicar release completa com campos faltantes ou criar outro componente/estado apenas para separar a prontidão. Especificação concluída; implementação e evidências pendentes na E2.14.

**Revisão em 2026-10-02:** o banco alvo passou a RDS PostgreSQL `db.t3.micro` ([ADR 007](007-RDS-FREE-PLAN.md)). O perfil `database-release`, seus campos de EKS e a ordem de prontidão permanecem; no release do banco, a identidade planejada muda de ARN de cluster Aurora para ARN de instância RDS antes da primeira publicação.

**Comprovação:** E1/E2/E6 devem validar proteções, identidade restrita, versões consumidas, manifesto real e destruição isolada. [RFC 002](../rfcs/002-ENTREGA.md).
