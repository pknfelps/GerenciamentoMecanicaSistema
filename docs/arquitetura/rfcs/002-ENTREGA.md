# RFC 002 — Entrega e contratos entre componentes

**Estado:** fluxos AE preparados e validados localmente; revisão AE.9 de 23/10/2026. Evidências hom anteriores não comprovam o contrato novo. [Arquitetura](../README.md) · [Contrato SSM/artefatos](../CONTRATOS_ENTRE_REPOSITORIOS.md)

## Responsabilidades

| Repositório/unidade | Entrega |
|---|---|
| API | Negócio/testes, Dockerfile, imagem ECR por digest, Deployment/HPA/ServiceAccount, OpenAPI e Auth.Contracts |
| Infra/base | VPC/EKS, add-ons, SGs, NLB/ASG, JWT/Pod Identity, Service/namespace, SMTP4dev efêmero e Collector Kubernetes |
| Infra/Gateway | Entrada REST/VPC Link V2, contratos projetados, permissão da versão Lambda e logs |
| Banco | RDS/credenciais/SQL, ConfigMap/Secrets temporários e Job database-init |
| Serverless auth/email | Dois SAMs/ZIPs/stacks/roles, função/versão/alias/logs/egress e auth/v2 ou email/v1 |
| Bootstrap persistente | S3/OIDC/ECR/IAM, metadados New Relic e identidades compartilhadas |
| Infra/observability/newrelic | Três dashboards e cinco alertas, estado independente dos destroys AWS |

## Gatilhos, aprovação e estado

CI roda em PR/manual; não publica automaticamente a aplicação após merge. `develop/hom` e `main/prd` são conferidos nos fluxos de ambiente. Provisionamentos/destroys são manuais, com OIDC e versões de actions conforme cada workflow versionado. A diferença em relação ao R06 original está registrada e sua análise foi adiada pelo mantenedor; esta revisão não altera gatilhos nem declara atendimento ao deploy automático após merge.

Base/banco/Gateway: plan salva binário/texto → Environment de aprovação sem AWS → apply do mesmo SHA/plano/execução. Artefato por ID, sete dias; plano obsoleto exige nova execução/aprovação, sem replan no apply. Estados S3 próprios por componente/ambiente usam versionamento e lock nativo. Consumidores leem parâmetros públicos SSM v2, sem terraform_remote_state. SSM configura recursos; sua presença não comprova prontidão.

API: api-deploy manual Publicar → Implantar, sem aprovação intermediária. Testa Release/gera OpenAPI, publica contrato/checksum imutáveis, publica imagem linux/amd64 com tag por commit/run/attempt e aplica o digest conferido ao overlay. Aguarda rollout 600 s, com diagnóstico e sem rollback automático próprio. api-destroy tem job único; não administra Service/NLB/IAM/ECR.

Auth/email: wrappers auth-*/email-* usam reusable-ci/publish/provision/destroy com component validado, templates/configs/prefixos fixos e Environment no job chamado. Os checks obrigatórios configuration/dotnet são gates do auth-ci e abrangem solução/ambos os SAMs, sem filtros de caminho. Roles específicas compartilham subject de Environment; não há isolamento OIDC por arquivo. auth-publish testa/builda e transfere artefato por ID ao job de publicação. SAM package envia ZIP ao prefixo do ambiente/commit. O template publicado fixa CodeUri Bucket/Key/Version antes do checksum. Republicação idêntica conserva a versão original após comparar ZIP/template; divergência ou template antigo sem Version exige commit novo. auth-provision valida a versão S3 e executa sam deploy num job; auth-destroy usa sam delete num job. Disparo manual autoriza cada operação completa; rollback nativo CloudFormation permanece habilitado. No SAM CLI 1.167.0, CodeUri objeto preserva o ZIP no descarte; contratos/templates publicados e IAM persistem.

Banco: apply entrega ConfigMap/Secrets temporários; kubectl executa Job estático e aguarda Complete por 900 s. Marcador/hash compatível evita reinicialização; banco incompatível falha. Cleanup remove Secrets temporários inclusive em erro. Credenciais ephemeral/write-only não são persistidas nos planos/estados; a revisão de escrita fixa evita rotação a cada apply.

New Relic: newrelic-provision manual somente main, sem input de ambiente, com plan → newrelic-approval → apply salvo/conferência sem reparo. Dashboards/condições possuem filtro ou condições hom/prd. Estado shared/newrelic, User API Key e ciclo de vida são separados dos Collectors/License Key e dos destroys AWS.

## Ordem de implantação e descarte

Provisionar: **bootstrap aprovado → base/Collector/SMTP4dev → banco/Job e email → API/auth → Gateway com revisões fixas e OpenAPI auth 2.0.0**. Metadados/License Key do Collector são pré-requisitos; API não entrega essa chave. Nova versão numérica auth exige atualizar o Gateway. A base não depende da aplicação para existir.

Descartar: **Gateway → API e auth → banco e email → base**. API/auth consultam REST API/VPC Link; banco consulta recursos API/Lambda/ENIs/egress; email consulta API/auth antes do sam delete; base consulta RDS/subnet group/SG do banco e função/ENIs/egress email. Consultas inválidas negam avanço, sem tratar erro como ausência. ENIs pendentes bloqueiam imediatamente; aguardar externamente e repetir, sem exclusão forçada. Banco precisa do EKS para a operação normal.

Base remove Collector/manifestos antes do EKS. Banco remove Job antes do RDS; skip_final_snapshot=true para dados descartáveis. Bootstrap, artefatos, IAM persistente e unidade New Relic ficam fora do descarte normal. O mantenedor coordena componentes; locks/concurrency locais não são coordenação global entre repositórios.

## Integridade e evidências

OpenAPI/NuGet/template SAM usam SHA-256 e escrita condicional; conteúdo divergente não sobrescreve versão existente. ECR tem tags imutáveis e deploy por digest. ZIP SAM permanece em prefixo gravável/versionado: fixar VersionId estabiliza a seleção, sem tornar a chave write-once. [Conferência implantada](../../VALIDACAO_PROTECAO_ARTEFATOS_2026-10-21.md).

[PRs/CI/CD/proteções](../../EVIDENCIAS_CICD_E_PROTECOES_2026-10-21.md) e [descarte final/custos](../../ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md) comprovam hom no alcance descrito. Prd/coexistência, concorrência de publicação e rollback não foram exercitados como novos testes; dispensas/adiamento não equivalem a aprovação desses cenários.

Decisões: [ADR 004](../adrs/004-ENTREGA-E-AMBIENTES.md), [ADR 008](../adrs/008-INFRAESTRUTURA-DECLARATIVA.md), [ADR 009](../adrs/009-SAM-AUTENTICACAO.md) e [ADR 010](../adrs/010-GATEWAY-REST-V2.md).

[AE.8: prefixos, VersionId, cadastro de AWS_EMAIL_ROLE_ARN e bloqueios](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE8_WORKFLOWS_E_GATEWAY.md). [AE.9: validação local](../../AE9_VALIDACAO_LOCAL_E_DOCUMENTACAO.md). Nenhuma execução remota ou plano/apply AWS nesta revisão.

**Consolidação posterior — 23/10/2026:** AE.10 concluída em hom, com [fluxo integrado](../../AE10_VALIDACAO_HOM_2026-10-23.md), [SQL/inativação/limites](../../AE10_CONFERENCIAS_FINAIS_HOM_2026-10-23.md) e [estado da entrega](../../CONSOLIDACAO_ENTREGA_2026-10-23.md). As menções anteriores à ausência de operações AWS descrevem a revisão documental/local; a comprovação posterior está nessas fontes. Não certifica prd, envio externo ou processamento assíncrono independente.
