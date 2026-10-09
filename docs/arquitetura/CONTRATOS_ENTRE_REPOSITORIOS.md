# Contratos entre repositórios

**Versão:** 2.1.0 — 2026-10-08. [ADR 008](adrs/008-INFRAESTRUTURA-DECLARATIVA.md) substitui o protocolo v1 de releases/prontidão. Base/banco/JWT hom aplicados; infraestrutura JWT conferida com No changes, versão preservada e isolamento IAM simulado. Runtimes, prd e migração dos consumidores permanecem pendentes.

## Propriedade e ambientes

| Componente | Proprietário | Responsabilidade |
|---|---|---|
| api | GerenciamentoMecanicaSistema | Negócio/testes, imagem ECR, Deployment/HPA, OpenAPI, Auth.Contracts |
| base | GerenciamentoMecanicaInfraestrutura | Rede/EKS/add-ons, SGs, NLB Terraform, Service NodePort e namespace reservado |
| gateway | GerenciamentoMecanicaInfraestrutura | Estado separado para REST API/VPC Link, OpenAPI composto e permissão Lambda |
| database | GerenciamentoMecanicaBancoDados | RDS, credenciais API/auth, esquema/seeds e Job próprio |
| auth | GerenciamentoMecanicaAutenticacao | Código/testes/ZIP, Terraform/IAM da função, contrato Validate |

API não reaplica o Service. A AWS associa instâncias do ASG ao target group; não há controller. Não consumir estado Terraform de outro repositório.

Conta `121754142617`, região `us-east-1`; `develop`/hom e `main`/prd. Bootstrap persistente em `shared/bootstrap/terraform.tfstate`; componentes em `<ambiente>/<base|gateway|database|auth>/terraform.tfstate`. Workspace default, backend S3 criptografado/versionado com `use_lockfile=true`.

Buckets `mecanica-tfstate-121754142617-us-east-1` e `mecanica-artifacts-121754142617-us-east-1`; ECR `121754142617.dkr.ecr.us-east-1.amazonaws.com/mecanica/api`. Descarte preserva bootstrap e outro ambiente.

Roles OIDC: `arn:aws:iam::121754142617:role/mecanica/pipelines/mecanica-<ambiente>-<componente>-github`. Infra usa `AWS_BASE_ROLE_ARN`/`AWS_GATEWAY_ROLE_ARN`; banco/API/auth usam `AWS_ROLE_ARN`. Manter `AWS_REGION=us-east-1`, `TF_STATE_BUCKET` e `ARTIFACTS_BUCKET` onde aplicáveis. Environments hom/prd autorizam OIDC; hom-approval/prd-approval exigem revisão humana e não recebem credenciais AWS.

Publicação/deploy da API reutiliza somente hom/prd e AWS_REGION/AWS_ROLE_ARN, sem Environment/job de aprovação. O disparo manual autoriza o fluxo completo; as aprovações de provisionamento Terraform permanecem separadas.

## SSM v2

Namespace `/mecanica/<ambiente>/<componente>/v2/<campo>`, recursos `aws_ssm_parameter`, String/Standard. Escalares usam strings, números decimal, listas arrays JSON. Valores secretos ficam exclusivamente no Secrets Manager. Consumidores Terraform usam `data.aws_ssm_parameter`, `with_decryption=false` e `jsondecode` para listas. Como IDs públicos retornam sensíveis pelo provider, pode-se usar `nonsensitive` apenas nesses campos públicos.

Exemplo de consumo:

```hcl
data "aws_ssm_parameter" "database_endpoint" {
  name            = "/mecanica/${var.environment}/database/v2/endpoint"
  with_decryption = false
}
```

Os parâmetros representam configuração, sem status ready, release, candidato, geração, tentativa, fingerprint, validade ou prova de acesso. O sucesso é o workflow concluído, o Job Complete e a validação do consumidor. O mantenedor serializa alterações de cada ambiente; mudanças de dependência exigem novo plano dos consumidores, inclusive quando eles aguardam aprovação.

### Base

| Campos publicados | Formato/uso |
|---|---|
| vpc-id | ID da VPC do ambiente |
| workload-subnet-ids, database-subnet-ids | Arrays JSON de duas subnets/AZs |
| cluster-name, cluster-arn | EKS; provider e kubeconfig |
| namespace, init-namespace | default para API; database-init para banco |
| api-security-group-id, init-security-group-id | SG efetivo do EKS, podem coincidir |
| auth-security-group-id | SG reservado para Lambda |
| api-service-name, api-service-port, api-node-port | svc-gerenciamento-api, 80, 30080 |
| nlb-arn, nlb-dns-name, nlb-security-group-id, nlb-target-group-arn | Referências do NLB Terraform |
| nlb-listener-port, nlb-listener-protocol, api-integration-uri | 80, TCP, URI HTTP privada para Gateway |
| ecr-repository-url | ECR compartilhado do bootstrap |
| jwt-secret-arn | Secret /mecanica/<ambiente>/base/jwt, JSON com somente key |
| jwt-issuer, jwt-audience | mecanica-<ambiente>-auth e mecanica-<ambiente>-api |

NLB interno TCP 80 → instâncias TCP 30080 → Service/Pod 8080. Health check HTTP `/health/ready` na porta 30080; `externalTrafficPolicy: Cluster`. Targets só ficarão saudáveis depois do deploy da API e acesso ao banco. A base não depende da API para criar NLB/target group.

JWT: Secret próprio de cada ambiente, chave HS256 de 64 caracteres alfanuméricos gerada por ephemeral Random e persistida por secret_string_wo com revisão fixa 1. Não há rotação a cada execução nem chave em SSM/estado/plano/outputs. O descarte autorizado da base exclui o Secret sem janela de recuperação; consumidores devem ser descartados antes. Infraestrutura JWT hom aplicada e conferida em 08/10; prd e consumo dos runtimes pendentes.

A base administra a role mecanica-<ambiente>-api-runtime e a associação Pod Identity para default/gerenciamento-api. A confiança exige o cluster ARN, namespace e ServiceAccount exatos. A policy read-jwt permite o Secret JWT e os três parâmetros JWT; a nova read-database permite somente o Secret database/api e seis parâmetros database/v2 (endpoint, port, database-name, api-secret-arn, api-db-user, ssl-mode) do ambiente. A segunda policy está implementada; o mantenedor confirmou o sucesso do deploy da infra hom em 08/10; sem leitura da credencial auth, do Secret administrativo RDS ou de outro ambiente. Os ARNs do banco são construídos na base sem data sources SSM do banco, preservando base → banco → API. O repositório da API implementa ServiceAccount/Deployment nos overlays hom/prd; sua aplicação será na publicação/deploy. Não conceder leitura JWT às pipelines API/auth/base. A role Lambda será criada na etapa própria da função.

Consumo pelo SDK .NET: resolver os três parâmetros SSM e AWSCURRENT na inicialização; preencher Jwt:Key, Jwt:Issuer e Jwt:Audience e manter a chave em memória, sem consulta por requisição. A API já implementa esse carregamento junto à credencial RDS, ativado por Runtime__Environment=hom|prd; IAM/manifestos estão preparados e o deploy da infra hom foi informado pelo mantenedor; publicação/deploy da API e validação real permanecem pendentes. Sem esse campo, preserva o modo local; campo presente inválido impede a inicialização. Falha de leitura ou configuração inválida impede inicialização, sem fallback local e sem cópia para Secrets Kubernetes. HS256, expiração, claims e tolerância de relógio permanecem iguais. [Inicialização da API](../INICIALIZACAO_AWS.md) e [procedimento JWT](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/JWT_COMPARTILHADO.md).

Campos futuros da base, adicionados somente quando implementados: `newrelic-secret-arn`, `newrelic-otlp-endpoint`, `otel-collector-endpoint`. Issuer/audience/chave JWT são distintos entre hom/prd e compatíveis entre API/função no ambiente. JWT e observabilidade não são pré-requisitos do Job do banco.

### Banco

| Campos publicados | Formato/uso |
|---|---|
| instance-arn, endpoint, port, database-name, security-group-id | RDS privado, 5432/mecanica, SG próprio |
| api-secret-arn, api-db-user | Secret persistente e mecanica_api |
| auth-secret-arn, auth-db-user | Secret persistente e mecanica_auth |
| schema-version, sql-sha256, ssl-mode | 1.0.0, hash de Init.sql, verify-full; configuração esperada, sem atestar execução |

Secret administrativo gerenciado pelo RDS é usado exclusivamente na inicialização. Não publicar essa referência para consumidores de aplicação. API/auth não usam o usuário administrativo. Secret JSON de aplicação contém engine/host/port/dbname/username/password; consumidores usam endpoint/port/database-name atuais do SSM e somente username/password do Secret. Assim uma mudança de endpoint não exige rotacionar senha.

Terraform importa metadados de credenciais existentes; lê seu valor somente como ephemeral para preservá-lo na primeira versão write-only. Em ambiente novo gera senha criptográfica de 40 caracteres uma vez. Revisão write-only fixa em 1: nenhuma rotação por execução; rotação futura exige alteração/revisão explícita e novo provisionamento SQL. Nunca importar versões comuns de secrets nem habilitar TF_LOG, pois podem expor valores.

ConfigMap `database-init-input` contém SQL/hash/host/CA pública. Secrets temporários `database-init-admin`, `database-init-api`, `database-init-auth` ficam apenas no namespace `database-init`, entregues com `data_wo` e removidos por kubectl ao final, também em falha. Refresh seguinte planeja recriação. API tem Edit em default; banco em database-init. Essa fronteira protege objetos Kubernetes; não é isolamento de tráfego PostgreSQL por namespace.

Job `k8s/database-init.yaml`: banco vazio cria esquema/seeds e marcador na mesma transação; marcador idêntico preserva data original e apenas provisiona roles/grants; SQL/hash/versão incompatível ou tabelas sem marcador interrompem. O ConfigMap e seu hash usam LF, como o SQL versionado no Git, preservando o marcador entre Windows/Linux sem modificar Init.sql. Não há SQL de smoke nem manifesto de release. Reiniciar API não inicializa banco.

### API, autenticação e Gateway (integração posterior)

API mantém Deployment/HPA/probes/testes de negócio; runtime resolve credencial restrita e JWT em default. Função consome subnets/SG de base, endpoint e auth-secret-arn do banco e JWT; Gateway consome nlb-arn/URI e ARN qualificado da versão Lambda. Seus Terraform futuros publicam campos v2 próprios: API image-uri/deployment-name/openapi-key/auth-contracts-version; auth function-name/function-version/invocation-arn/zip-key/openapi-key/auth-contracts-version; gateway API/VPC Link/endpoint. Sem releases ou tentativas.

Gateway é planejado/aprovado/aplicado manualmente depois dos backends, usando versões fixas dos dois contratos. Não haverá chamada automática a um workflow para promover releases. Permissão de invocação pertence ao Gateway; função usa versão numérica, não `$LATEST`.

## Artefatos e contrato de negócio

Imagem ECR por digest; OpenAPI em `contracts/api|auth/<commit>/openapi.json`; ZIP em `lambda/<commit>/`; pacote em `packages/GerenciamentoMecanica.Auth.Contracts/<versão>/`. Preservar distribuição imutável S3, SHA-256, escrita condicional e referências fixas já implementadas. O pacote é independente da ativação de infraestrutura; S3 não é feed NuGet nativo. [Procedimento do pacote](PACOTE_AUTH_CONTRACTS.md).

HTTP/JWT, CPF/CNPJ, claims, códigos, roles e vínculo da OS permanecem no [contrato de acesso](ACESSO_E_AUTENTICACAO.md). Esta mudança não altera regras de negócio nem remove testes da aplicação/função. Formatos OpenAPI e pacote mantêm versionamento próprio, independente do segmento v2 do SSM.

## Operação e migração

Infra/banco: disparo manual → plan salvo e textual → aprovação protegida sem AWS → apply do mesmo artefato/commit, retenção sete dias. Terraform recusa plano obsoleto do próprio estado; gerar outra execução para revisão. CI somente fmt/validate/renderização. Aprovação deve revisar remoções intencionais e preservação de VPC/EKS/RDS; não é autorização geral para replanejar/aplicar.

API: `.github/workflows/api-deploy.yml`, manual, dois jobs Publicar → Implantar, sem aprovação intermediária. Exige develop/hom ou main/prd, conta/região/role/repositório exatos e checkout do SHA da execução em ambos os jobs; concurrency por ambiente sem cancelar execução em curso. Lê somente ecr-repository-url, cluster-name e namespace da base v2. Publica no ECR compartilhado mecanica/api, tag imutável sha-<commit completo>-<run_id>-<run_attempt>; transmite URI por digest via output da action de build. Deploy confirma BatchGetImage, cluster mecanica-<ambiente>-eks e namespace default, substitui pending com kustomize edit set image no checkout temporário e usa kubectl apply -k no overlay. Rollout até 600 segundos, conferência da imagem desejada e diagnóstico limitado em falha, sem rollback automático. Pipeline não lê Secrets; Pod Identity entrega credencial/JWT ao runtime. Nenhuma publicação de campos API v2/OpenAPI/Gateway nesta entrega. Publicar o workflow também em main para disponibilizar Run workflow; primeira execução develop/hom. [Procedimento](../DEPLOY_API.md).

Migrar base v2 e namespace; adotar banco/credenciais v2 e executar Job; migrar consumidores; só então importar parâmetros v1 ativos em plano dedicado e retirá-los em outro plano aprovado. `legacy_parameter_names` vazio depois da adoção programa sua remoção. Excluir históricos de attempts da lista. Procedimentos de NLB legado e SSM estão em Infra/docs/MIGRACAO_DECLARATIVA.md.

Descarte em ordem Gateway/consumidores → banco → base. Banco remove Job antes do apply de destroy; base remove manifestos antes de EKS. NLB/associação/SSM v2 são removidos pelo Terraform. Bootstrap nunca integra esse descarte. Validação de destroy somente quando houver descarte solicitado, sem destruir para testar a reformulação.
