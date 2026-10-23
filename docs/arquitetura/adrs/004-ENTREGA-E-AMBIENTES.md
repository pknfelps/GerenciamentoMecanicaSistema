# ADR 004 — Quatro repositórios e configuração por ambiente

**Estado:** aceito e implementado, com fluxo hom comprovado. Coexistência/prd não exercitados; análise da diferença R06 adiada pelo mantenedor.

**Contexto:** separar código, plataforma, banco e autenticação, preservando contratos/versionamento e descarte sem remover recursos compartilhados.

**Decisão vigente:** API, infraestrutura, banco e autenticação têm repositórios/pipelines próprios. Develop corresponde a hom e main a prd. Recursos/SSM/roles e estados são separados por componente/ambiente; o bootstrap é persistente. Terraform usa S3 versionado/locking nativo, sem DynamoDB; auth usa stack SAM/CloudFormation. Metadados SSM v2 são configuração, não release/prontidão; consumidores não leem estados de outros repositórios.

CI valida PRs/manual. Publicações/provisionamentos são manuais: base/banco/Gateway/New Relic preservam plano salvo → aprovação → apply exato; API e auth seguem seus fluxos manuais sem aprovação intermediária. Isso não comprova deploy automático após merge do R06 original. Não houve autorização para modificar gatilhos nesta revisão.

Imagens usam ECR/digest; OpenAPI/NuGet/template SAM são publicados com checksum e escrita condicional. ZIP SAM tem chave versionada gravável, mas o template fixa VersionId. Estado/dashboards/alertas New Relic são compartilhados e independentes dos destroys AWS. [RFC 002](../rfcs/002-ENTREGA.md).

**Alternativas e consequências:** OIDC substitui chaves duráveis no GitHub. Bootstrap e artefatos persistem entre ativações. Locks/concurrency de um repositório não serializam todos os componentes; o mantenedor coordena a ordem. Não há candidatos/release v1/gerações no fluxo vigente; os registros v1 abaixo são históricos.

**Comprovação:** PRs/CI/CD, proteções e cadeia de destroy hom em [evidências](../../EVIDENCIAS_CICD_E_PROTECOES_2026-10-21.md), com [custos/ausência final](../../ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md). Coexistência/concorrência/rollback dispensados não viram testes aprovados.

Revisão documental de 21/10/2026. Implementação e validações de hom não comprovam prd ou testes dispensados. [Arquitetura](../README.md) · [Evidências E6](../../REVISAO_E6_2026-10-21.md).

<details>
<summary>Histórico anterior à revisão E7 — registros preservados, sem vigência operacional</summary>

Os estados locais, planos pendentes e alternativas abaixo registram o momento em que foram escritos. A decisão e o alcance atuais estão acima; este histórico não autoriza operações nem reabre pendências aceitas.

> **Revisão de 2026-10-11:** auth usa SAM/ZIP conforme [ADR 009](009-SAM-AUTENTICACAO.md), com deploy/descarte manuais completos e sem aprovação intermediária. SAM conduz deploy/descarte; sam delete remove ZIP atual/templates operacionais, preservando outras revisões/contratos/bucket/IAM. Sem comandos CloudFormation diretos. Bootstrap/base/banco/Gateway continuam Terraform/plan aprovado. Inventário sem recursos/estado auth permitiu retirar seu código Terraform sem destroy. Bootstrap CI/SAM aplicado e conferido; primeira implantação SAM pendente.

> **Revisão vigente (2026-10-07):** [ADR 008](008-INFRAESTRUTURA-DECLARATIVA.md) substitui controller e protocolo de releases/prontidão por NLB Terraform, NodePort, SSM v2 e planos salvos com aprovação. Os complementos anteriores abaixo são histórico; demais decisões permanecem.

**Estado:** aceito como arquitetura; implementação pendente. **Formalização:** 2026-09-16. [Índice](../README.md)

**Contexto:** a entrega exige quatro repositórios/pipelines e hom/prd capazes de coexistir. Desligamento para economia não pode destruir dependências compartilhadas nem o outro ambiente.

**Decisão:** separar aplicação, infraestrutura, banco e autenticação; develop entrega em hom, main em prd. Usar estados S3 versionados com lock nativo por componente/ambiente, OIDC, metadados SSM e secrets no Secrets Manager. ECR e bucket separado de artefatos guardam versões fixas; contratos OpenAPI e pacote CPF/CNPJ e JWT têm produtores definidos. Bootstrap compartilhado tem ciclo independente.

**Alternativas:** estado local dificulta coordenação/auditoria; credenciais AWS duráveis no CI foram substituídas por OIDC; DynamoDB não é necessário para o locking escolhido. Reutilizar os mesmos recursos para hom/prd ou impor exclusão global entre ambientes contraria a coexistência. Docker Hub foi substituído por ECR.

**Consequências:** contratos/versionamento e ordem de deploy devem ser explícitos. Lock S3 não coordena componentes diferentes, e concurrency do GitHub não atravessa repositórios. Há custos de armazenamento/operações e de cada ambiente ativo; coexistência não significa manter ambos ligados continuamente.

**Complemento em 2026-10-01 — prontidão da base por consumidor:** acrescentar `base/v1/database-release` para o banco, mantendo `base/v1/release` completo para API/função/Gateway. O contrato anterior obrigava o banco a aguardar NLB/JWT, embora dependa de rede, EKS, namespace, SGs e acesso ao Job. A extensão 1.1.0 permite implementar Aurora/esquema antes desses componentes sem reduzir a garantia da release existente. Ambos os manifestos pertencem à infraestrutura, usam a mesma geração e são invalidados antes de mutações/descarte; a coordenação por ambiente continua necessária. Alternativas descartadas: publicar release completa com campos faltantes ou criar outro componente/estado apenas para separar a prontidão. Especificação concluída; implementação e evidências pendentes na E2.14.

**Revisão em 2026-10-02:** o banco alvo passou a RDS PostgreSQL `db.t3.micro` ([ADR 007](007-RDS-FREE-PLAN.md)). O perfil `database-release`, seus campos de EKS e a ordem de prontidão permanecem; no release do banco, a identidade planejada muda de ARN de cluster Aurora para ARN de instância RDS antes da primeira publicação.

**Comprovação:** E1/E2/E6 devem validar proteções, identidade restrita, versões consumidas, manifesto real e destruição isolada. [RFC 002](../rfcs/002-ENTREGA.md).

</details>
