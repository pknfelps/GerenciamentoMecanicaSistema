# Encerramento operacional e custos da E6 — 21/10/2026

E6.11 concluída no escopo hom: descarte final realizado pelo mantenedor, ausência dos recursos conferida e custos/créditos observados registrados. E6 encerrada no escopo acordado, sem afirmar comprovação dos testes dispensados ou do requisito original de deploy após merge. Conferência AWS somente leitura na conta 121754142617, região us-east-1; consolidação em 2026-10-21T21:34:31.363151+00:00. Nenhum destroy, apply, workflow, exclusão residual ou nova chamada à aplicação foi executado pelo agente.

## Execuções de descarte do mantenedor

| Componente | Execução GitHub | Início UTC | Fim UTC | Resultado |
|---|---|---|---|---|
| Gateway | [39668293847](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/actions/runs/39668293847) | 2026-10-21T20:12:36Z | 2026-10-21T20:14:07Z | success |
| API | [39668864993](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions/runs/39668864993) | 2026-10-21T20:16:27Z | 2026-10-21T20:16:49Z | success |
| Autenticação | [39668881429](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/actions/runs/39668881429) | 2026-10-21T20:16:34Z | 2026-10-21T20:38:27Z | success |
| Banco | [39672081838](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/actions/runs/39672081838) | 2026-10-21T20:38:43Z | 2026-10-21T20:42:17Z | success |
| Base | [39672890747](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/actions/runs/39672890747) | 2026-10-21T20:44:20Z | 2026-10-21T21:07:01Z | success |

Os onze jobs das cinco execuções retornam success. Gateway terminou antes de API/auth; API e auth terminaram antes do banco; banco terminou antes da base. Gateway/banco/base preservaram plan, approval e apply. API/auth mantiveram seus jobs únicos. A ausência dos recursos foi confirmada por consultas AWS independentes do resultado dos workflows.

## Recursos consultados

| Grupo | Resultado atual em us-east-1 |
|---|---|
| REST APIs, VPC Links V2 e Lambda | Ausentes |
| Clusters EKS e balanceadores ELBv2 | Ausentes |
| Instâncias RDS e DB subnet groups | Ausentes |
| Stacks CloudFormation ativas | Ausentes |
| EC2 | i-063d3dfee68df6002 em terminated; nenhuma instância em estado ativo |
| NAT Gateway | nat-077f59a43784e25b2 em deleted; nenhum NAT ativo |
| VPCs, endpoints VPC, ENIs, EIPs e volumes EBS | Ausentes |
| Snapshots EBS próprios | Ausentes |

A listagem ainda exibir EC2 terminated e NAT deleted não significa que os recursos continuam provisionados. O escopo de inventário é a região utilizada pelo projeto, não uma auditoria de todas as regiões da conta. Com o EKS ausente, não há API Kubernetes para nova consulta dos antigos Deployment/pods/Collector.

## Persistências e resíduos

Preservados conforme arquitetura: buckets mecanica-tfstate-121754142617-us-east-1 e mecanica-artifacts-121754142617-us-east-1, repositório ECR mecanica/api (IMMUTABLE), roles persistentes e metadados dos Secrets New Relic hom/prd. Nenhum valor de Secret foi lido. Os estados hom/base, hom/database e hom/gateway permanecem no bucket, atualizados ao fim dos respectivos destroys; nenhum arquivo .tflock corrente foi listado sob hom/. Somente metadados dos objetos foram lidos; não se afirma que o conteúdo de todos os estados foi auditado nesta rodada. Dashboards/New Relic pertencem à unidade persistente independente; não foram modificados ou novamente consultados nesta conferência.

Resíduos históricos identificados e não excluídos na conferência de 21/10:

- Snapshot RDS manual mecanica-hom-postgres-snapshot, available, criptografado, alocação informada de 20 GiB. Criado em 08/10/2026 às 01:55:11 UTC, a partir de instância criada em 06/10. É anterior ao descarte atual; terraform/rds.tf do banco mantém skip_final_snapshot=true. Não é evidência de que o destroy atual deixou a instância RDS ativa.
- Logs /aws/apigateway/welcome (108 bytes), /aws/eks/api-cluster/cluster (141.142.143 bytes) e /aws/eks/gerenciamento-mecanica-cluster/cluster (28.575.727 bytes). Somam 169.717.978 bytes (161,86 MiB), calculados pelos metadados; sem retentionInDays definido. Os grupos correntes mecanica-hom de Gateway/Lambda/EKS não permanecem.
- Dezesseis parâmetros SSM v1 de attempts de base/banco, listados apenas por metadados. Nenhum parâmetro v2 dos componentes descartados foi encontrado.

Esses resíduos e as persistências de armazenamento podem manter consumo/custos após o descarte do ambiente. A conferência não autoriza sua exclusão, não os apresenta como necessários à execução atual e não promete custo zero da conta. Limpeza seria uma ação separada explicitamente autorizada.

**Atualização em 22/10/2026:** o mantenedor confirmou a exclusão do snapshot manual `mecanica-hom-postgres-snapshot`. Essa pendência foi encerrada com base na confirmação do mantenedor, sem nova consulta AWS pelo agente. A observação histórica acima permanece como evidência do inventário de 21/10; não representa a situação posterior desse snapshot. A confirmação não altera os custos já observados nem comprova ausência de outros backups ou resíduos da conta.

## Custos e créditos observados

Consulta Cost Explorer UnblendedCost, DAILY, agrupada por SERVICE e RECORD_TYPE, de 01/10/2026 inclusive até 22/10/2026 exclusivo. Escopo da conta inteira, incluindo serviços globais/outras regiões; não representa custo exclusivo de hom, desta última execução ou de um recurso individual. Não foi ativada alocação por tags ou granularidade adicional. Os períodos retornam Estimated=true, inclusive o dia 21 incompleto.

| Indicador | USD |
|---|---|
| Consumo Usage visível | 18.98 |
| Créditos aplicados visíveis | 18.98 |
| Líquido visível estimado | 0.00 |
| Créditos restantes informados por Free Tier | 129.61 |

Valores brutos: Usage 18.9780357720 USD; Credit -18.9780358105 USD; Tax 0. Diferença líquida de -0.0000000385 USD, exibida como 0,00 após arredondamento, sem interpretá-la como crédito reembolsável. Todos os totais/agregações e conversões foram calculados com Decimal a partir das respostas, sem soma manual de valores arredondados.

Consumo bruto por serviço, antes de créditos:

| Serviço | USD |
|---|---|
| Amazon Elastic Container Service for Kubernetes | 8.70 |
| EC2 - Other | 4.86 |
| Amazon Elastic Compute Cloud - Compute | 1.76 |
| Amazon Relational Database Service | 1.56 |
| Amazon Elastic Load Balancing | 1.19 |
| Amazon Virtual Private Cloud | 0.46 |
| AWS Secrets Manager | 0.29 |
| AmazonCloudWatch | 0.14 |
| Amazon Simple Storage Service | 0.01 |
| Amazon EC2 Container Registry (ECR) | 0.01 |
| Amazon API Gateway | 0.00 |

Valor exibido 0,00 pode representar consumo positivo inferior a um centavo. Não somar as linhas arredondadas para reconstruir o total bruto.

Free Tier get-account-plan-state retornou FREE/ACTIVE e 129.61 USD de créditos restantes. accountPlanExpirationDate=2027-01-24T22:40:41.736000+00:00 é a expiração do plano informada pela API; não foi tratada como data de validade individual de todos os créditos. O saldo e o Cost Explorer são observações com atualização própria, não uma reconciliação de fatura definitiva.

O faturamento pode atualizar após o descarte por atraso de processamento e por persistências/resíduos. Recomenda-se reconferir quando os dados do dia 21 forem atualizados; o registro atual atende ao critério de custos/créditos observados, sem certificar fechamento da fatura. [Atualização do Cost Explorer](https://docs.aws.amazon.com/cost-management/latest/userguide/ce-what-is.html).

## Conclusão e limites aceitos

E6.11 encerrada com descarte do ambiente comprovado e situação financeira observada, incluindo os recursos remanescentes. E6 fica concluída no escopo hom acordado: rollback, coexistência e teste de publicação concorrente dispensados; análise workflows/R06 adiada. Não foram aprovados testes omitidos, validado prd ou alterados requisitos originais. E5 permanece encerrada por aceite anterior.

Próxima etapa prevista: E7, documentação e entrega final. A consolidação não executa nem aprova automaticamente alterações, provisionamentos, limpezas ou submissão.

Evidências locais ignoradas em artifacts/e6-encerramento-20261021/: inventários JSON AWS, metadados do snapshot/backends, execuções/jobs GitHub, cost-daily-service-recordtype.json, cost-summary.json, free-tier-account-plan.json e validation-summary.json. Nenhum valor de Secret, chave, token ou corpo de log foi coletado.
