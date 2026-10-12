# RFC 002 — Entrega e contratos entre componentes

> **Gateway — 12/10/2026:** unidade e workflows gateway-provision/gateway-destroy separados no repositório infra; environment + api_commit + auth_commit. Plan com contratos S3 verificados → Environment protegido → apply do mesmo SHA/plano/contratos baixados por ID, sete dias, sem replan. Bootstrap → base → banco/Job → API/auth → Gateway. Descarte inverso e conferência de ENIs antes da base. [ADR 010](../adrs/010-GATEWAY-REST-V2.md).

**Estado:** arquitetura aceita; reformulação autorizada em 2026-10-07. [ADR 008](../adrs/008-INFRAESTRUTURA-DECLARATIVA.md) substitui a operação por releases/controller. [Contratos v2](../CONTRATOS_ENTRE_REPOSITORIOS.md) são a referência operacional.

## Responsabilidades

API mantém negócio/testes, imagem, Deployment/HPA, OpenAPI e Auth.Contracts. Infra mantém Terraform para rede/EKS/add-ons/SGs/NLB e Gateway; Service NodePort/namespace em manifestos. Banco mantém RDS/secrets/SQL/Job. Auth usa SAM com ZIP para função/versão/alias/logs/egress/SSM ([ADR 009](../adrs/009-SAM-AUTENTICACAO.md)), implantação pendente. Bootstrap mantém S3/OIDC/ECR, role CI e IAM runtime persistentes.

NLB TCP 80 encaminha para instâncias do ASG na porta 30080. Service NodePort encaminha a 8080; health HTTP `/health/ready`. AWS acompanha entrada/saída das instâncias. Sem controller/Helm e sem EBS CSI sem uso; manter Pod Identity Agent/Metrics Server e t3.small 1/1/1.

## Pipelines e configuração

Unidades Terraform usam estados próprios S3 versionados e locking nativo; auth terá stacks CloudFormation. Hom/prd independentes, OIDC por componente. Configuração SSM v2 em recursos Terraform, exceto auth/v2 com destino CloudFormation; consumidores usam somente parâmetros públicos, sem estado remoto de outro repositório. Secrets apenas referenciados, valores resolvidos em runtime ou em campos ephemeral/write-only nas unidades Terraform.

Provisionamento e destroy das unidades Terraform manuais, três jobs: plan salva binário e texto, aprovação em hom-approval/prd-approval sem AWS, apply baixa o artefato daquela execução e usa o mesmo commit. Retenção sete dias; plano obsoleto requer nova execução. Nenhum replan, fingerprint, publicação por scripts ou apply automático em push. CI de infra/banco somente fmt/validate/Kustomize. Bootstrap administrativo usa comandos Terraform diretos e revisão do plano salvo.

Auth: template estático, código/infraestrutura da mesma revisão e publicação em dois jobs: testes/build SAM → upload nativo ZIP/contratos. Provisionamento tem um job sam deploy, sem aprovação intermediária; disparo manual autoriza todas as alterações e o SAM aguarda conclusão. Descarte tem um job sam delete --no-prompts: remove ZIP atual e templates operacionais, preservando outras revisões/contratos/bucket/IAM. O prefixo operacional é sam/auth/<ambiente>/changesets. Exclusão S3 mantém versões históricas; republicar antes de recriar o deploy. Não há comandos CloudFormation diretos. Rollback padrão e logs SAM; alterações vazias têm sucesso. Quota permanece pré-requisito; Terraform auth retirado após inventário sem recursos/estado. Demais unidades mantêm plan/aprovação/apply.

API: [workflow manual](../../../.github/workflows/api-deploy.yml) separado do CI, com dois jobs Publicar → Implantar. O disparo autoriza ambos, sem aprovação intermediária; usar develop/hom ou main/prd e Environments hom/prd existentes. Checkout do mesmo SHA, testes Release, push ECR linux/amd64 com tag sha-<commit>-<run_id>-<run_attempt> e deploy pelo digest retornado. O segundo job confirma o digest por BatchGetImage, troca a imagem via Kustomize nativo e aplica o overlay diretamente, aguardando rollout por 600 segundos. Falha exibe diagnóstico limitado sem rollback automático. Nenhuma leitura de Secrets pela pipeline, publicação SSM da API ou OpenAPI nessa entrega. [Operação](../../DEPLOY_API.md).

Banco: apply entrega ConfigMap e Secrets temporários no namespace reservado; kubectl remove Job antigo, aplica YAML e aguarda Complete. Falha mostra diagnóstico/logs. Etapa always remove Secrets temporários; próximo refresh planeja recriação. Preservar senhas existentes na primeira adoção, gerar novas para ambientes novos sem rotacionar em cada apply. Init.sql/marcador/grants mantidos; sem smokes operacionais. Testes de negócio permanecem.

OpenAPI/NuGet e template SAM empacotado continuam imutáveis/versionados com SHA-256 e escrita condicional no S3. ZIP nativo usa sam/auth/<ambiente>/<commit>/, com deduplicação/versionamento e exceção localizada à escrita condicional; não tem garantia de gravação única. Não há protocolo de releases de infraestrutura. Gateway será aplicado manualmente após implantação dos backends.

## Ordem e aceitação

1. Bootstrap: revisar/aplicar permissões, mantendo apenas descarte legado durante migração.
2. Se houver controller/NLB legado, remover Service, esperar exclusão e só então remover controller.
3. Base: plan/aprovação/apply AWS; kubectl aplica namespace e NodePort.
4. Banco: plan/adotar credenciais/aprovação/apply; executar Job e limpar Secrets.
5. Migrar consumidores para v2; aplicar runtimes/JWT/Gateway nas respectivas etapas.
6. Adotar e retirar SSM v1 ativo em planos dedicados, preservando histórico; retirar permissões de limpeza legada.

Mantenedor coordena operações sequenciais por ambiente. Dependência alterada exige novo plano dos consumidores; não haverá outro mecanismo de coordenação. SSM é configuração, não prontidão. Validar health depois da API, marcador/hash do Job, ausência de senhas em estados/logs e isolamento dos Secrets.

Descarte: consumidores/Gateway → banco → base. Job sai antes do banco, manifestos antes de EKS; Terraform remove NLB e associação. Preservar bootstrap e outro ambiente. Não executar destroy para testar esta reformulação.
