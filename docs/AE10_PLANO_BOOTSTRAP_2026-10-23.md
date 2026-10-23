# AE.10 — Plano salvo do bootstrap de e-mail

**23/10/2026 — aprovado e aplicado. Plano posterior sem diferenças.**

Plano da unidade persistente `bootstrap` do GerenciamentoMecanicaInfraestrutura, conta `121754142617`, região `us-east-1`, workspace `default`, revisão `09da8a288343475185a9e1af3c537874e6951dd7`. Backend existente `shared/bootstrap/terraform.tfstate`, lockfile preservado; Account ID New Relic informado somente ao processo. Nenhum valor de Secret consultado.

## Resultado revisado

**24 criações, 5 alterações in-place, nenhuma exclusão ou substituição**, cobrindo hom/prd.

| Mudança | Quantidade | Escopo |
|---|---:|---|
| Roles email-runtime e email-github | 4 | Uma de cada por ambiente |
| Policies gerenciadas auth-email, base-email, email-provision e email-sam | 8 | Consumo pela autenticação, preparação da base e provisionamento SAM |
| Policies inline runtime e metadados da pipeline email | 4 | Transporte privado, logs e catálogo do próprio ambiente |
| Associações das policies gerenciadas | 8 | Roles auth-runtime, base e email-github |
| Policies inline existentes das pipelines API/auth | 4 alterações | Leitura dos dois parâmetros email/v1: invocation-arn e contract-version |
| Policy do bucket de artefatos | 1 alteração | Exceção de upload nativo SAM nos prefixos sam/email/hom/* e sam/email/prd/* |

Outputs novos: `email_runtime_role_arns` e `email_github_environment_variables`. A permissão do runtime da API para invocar e-mail pertence ao plano posterior da base.

Não há alterações planejadas de VPC, EKS, NLB, RDS, funções Lambda, Gateway, Collectors, versões de Secrets ou dashboards. O bootstrap prepara identidades e permissões; não implanta as funções.

## Conferências

- Terraform 1.15.9, provider AWS 6.58.0: init com lockfile readonly, fmt -check e validate aprovados.
- Trust OIDC das novas pipelines restrito ao repositório Serverless, audience e Environment exatos; ref develop/hom e main/prd. Runtime confia no serviço Lambda.
- Policies de consumo auth limitadas aos dois parâmetros e ao alias email:live do próprio ambiente. Email runtime sem leitura de banco, JWT ou Secrets.
- Policies gerenciadas novas entre 481 e 3.112 caracteres, abaixo de 6.144; policies inline novas de 939 e 1.683 caracteres. Trusts revisados.
- Cota real da conta: 20 policies gerenciadas por role. Base hom/prd possui oito associações e passará a nove.
- As quatro alterações das pipelines acrescentam somente os dois ARNs SSM do ambiente; as demais permissões permanecem. Na policy S3, TLS obrigatório e proteção dos demais prefixos permanecem.
- Plano gerado sem target, refresh desabilitado ou aplicação. Nenhum workflow remoto executado.

## Arquivos do plano aprovado

Arquivos locais ignorados pelo Git, em `artifacts/ae10/bootstrap/` deste repositório:

- `email-bootstrap.tfplan`: binário aprovado e aplicado; plano consumido, não reaplicar.
- `email-bootstrap.txt`: plano legível.
- `email-bootstrap.tfplan.json`: exportação estruturada local.
- `review.json`: inventário resumido e identificação do plano.
- `plan.log`: saída da geração.

SHA-256 do binário: `850CC2B901CEB7CD2A90739CE836771974BF887ECA6592FCCB3D55E2148416B7`.

## Aplicação e conferência

Após aprovação explícita, hash e conta AWS conferidos novamente. Aplicado exclusivamente o binário identificado acima: **24 added, 5 changed, 0 destroyed**. Plano de conferência com refresh e detailed-exitcode retornou **0 / No changes**; nenhum segundo apply realizado.

Leitura direta IAM confirmou em hom/prd as novas roles, trust runtime Lambda, trust OIDC de repositório/Environment/ref exatos e associações auth-email/base-email. Cada role da base passou a nove policies gerenciadas. Isso comprova a configuração implantada; execução OIDC pelos workflows e invocação SMTP permanecem para a etapa integrada.

Evidências adicionais locais: `apply.log`, `post-apply-plan.log`, `iam-post-apply-review.json` e `email-environment-variables.json`. Nenhum valor de Secret consultado ou workflow remoto executado.

## Próxima etapa

**23/10/2026 — variável `AWS_EMAIL_ROLE_ARN` cadastrada e conferida pela interface autenticada do GitHub**, nos Environments existentes do GerenciamentoMecanicaServerless, após autorização explícita. Valores:

| Environment | Valor |
|---|---|
| hom | `arn:aws:iam::121754142617:role/mecanica/pipelines/mecanica-hom-email-github` |
| prd | `arn:aws:iam::121754142617:role/mecanica/pipelines/mecanica-prd-email-github` |

Cada Environment passou de quatro para cinco variáveis, preservando os valores anteriores de ARTIFACTS_BUCKET, AWS_REGION, AWS_ROLE_ARN e TF_STATE_BUCKET. Restrições develop/hom e main/prd preservadas; nenhum Secret, proteção ou workflow foi alterado/executado. Evidência local em `artifacts/ae10/bootstrap/serverless-environments.jpg`.

Seguir o [roteiro AE.10](AE9_VALIDACAO_LOCAL_E_DOCUMENTACAO.md), começando pelo base-provision; provisionamento de funções, rede SMTP e validação integrada permanecem pendentes. Mudanças posteriores no bootstrap exigem novo plano salvo e aprovação.

**Atualização posterior — 23/10/2026:** o mantenedor provisionou hom e a [conferência integrada](AE10_VALIDACAO_HOM_2026-10-23.md) comprovou o uso de auth:14/email:1, rede privada, invocação/captura e fluxo Customer da API. Consultar esse registro para os ajustes e limites remanescentes; a seção anterior conserva a sequência após o apply do bootstrap, sem afirmar pendência atual de implantação de hom.
