# Contratos entre repositórios

**Versão:** 2.6.0 — 2026-10-12. ADR 008 define infraestrutura declarativa; [ADR 009](adrs/009-SAM-AUTENTICACAO.md) implementa SAM/ZIP e disparo manual auth sem aprovação intermediária. Bootstrap CI/SAM aplicado e conferido; publicação/deploy e invocação direta auth hom validados sem reserva de concorrência. Ciclo hom base/banco/JWT/API/destroy validado. Geração/publicação do OpenAPI da API validadas localmente e na AWS em 12/10/2026; contrato/checksum/commit versus imagem hom conferidos. Gateway REST/VPC Link V2 implementado e validado localmente ([ADR 010](adrs/010-GATEWAY-REST-V2.md)); bootstrap/apply e integração HTTP/JWT pendentes.

## Propriedade e ambientes

| Componente | Proprietário | Responsabilidade |
|---|---|---|
| api | GerenciamentoMecanicaSistema | Negócio/testes, imagem ECR, Deployment/HPA, OpenAPI, Auth.Contracts |
| base | GerenciamentoMecanicaInfraestrutura | Rede/EKS/add-ons, SGs, NLB Terraform, Service NodePort e namespace reservado |
| gateway | GerenciamentoMecanicaInfraestrutura | Estado separado para REST API/VPC Link, OpenAPI composto e permissão Lambda |
| database | GerenciamentoMecanicaBancoDados | RDS, credenciais API/auth, esquema/seeds e Job próprio |
| auth | GerenciamentoMecanicaAutenticacao | Código/testes/contrato Validate e SAM com ZIP para função/versão/alias/logs/egress/SSM; implantação pendente |
| bootstrap | GerenciamentoMecanicaInfraestrutura | Estado/artefatos/ECR/OIDC e IAM persistente, incluindo role/policy auth-runtime e permissões OIDC auth |

API não reaplica o Service. A AWS associa instâncias do ASG ao target group; não há controller. Não consumir estado Terraform de outro repositório.

Conta 121754142617, região us-east-1; develop/hom e main/prd. Bootstrap persistente em shared/bootstrap/terraform.tfstate; unidades Terraform em <ambiente>/<base|gateway|database>/terraform.tfstate, workspace default e locking S3. Auth usa stacks mecanica-hom-auth/mecanica-prd-auth. Inventário AWS não encontrou recursos ou objetos/versões históricos nos backends hom/auth e prd/auth; código Terraform auth retirado sem destroy ou exclusão de estado. Não administrar um recurso nos dois gerenciadores.

Buckets `mecanica-tfstate-121754142617-us-east-1` e `mecanica-artifacts-121754142617-us-east-1`; ECR `121754142617.dkr.ecr.us-east-1.amazonaws.com/mecanica/api`. Descarte preserva bootstrap e outro ambiente.

Roles OIDC: `arn:aws:iam::121754142617:role/mecanica/pipelines/mecanica-<ambiente>-<componente>-github`. Infra usa `AWS_BASE_ROLE_ARN`/`AWS_GATEWAY_ROLE_ARN`; banco/API/auth usam `AWS_ROLE_ARN`. Manter `AWS_REGION=us-east-1`, `TF_STATE_BUCKET` e `ARTIFACTS_BUCKET` onde aplicáveis. Environments hom/prd autorizam OIDC; hom-approval/prd-approval exigem revisão humana e não recebem credenciais AWS.

Publicação/deploy da API reutiliza somente hom/prd e AWS_REGION/AWS_ROLE_ARN, sem Environment/job de aprovação. O disparo manual autoriza o fluxo completo; as aprovações de provisionamento Terraform permanecem separadas.

## SSM v2

Namespace `/mecanica/<ambiente>/<componente>/v2/<campo>`, String/Standard. Produtores Terraform usam `aws_ssm_parameter`; o destino auth usará recursos CloudFormation para seus parâmetros. Escalares usam strings, números decimal, listas arrays JSON. Valores secretos ficam exclusivamente no Secrets Manager. Consumidores Terraform usam `data.aws_ssm_parameter`, `with_decryption=false` e `jsondecode` para listas. Como IDs públicos retornam sensíveis pelo provider, pode-se usar `nonsensitive` apenas nesses campos públicos. O workflow SAM usa AWS CLI para os quatro identificadores de rede da base/banco; jq converte apenas o array JSON das subnets para o parâmetro de lista, sem consumo de estado remoto.

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
| gateway-security-group-id | SG reservado do VPC Link V2; base possui também suas regras |
| api-service-name, api-service-port, api-node-port | svc-gerenciamento-api, 80, 30080 |
| nlb-arn, nlb-dns-name, nlb-security-group-id, nlb-target-group-arn | Referências do NLB Terraform |
| nlb-listener-port, nlb-listener-protocol, api-integration-uri | 80, TCP, URI HTTP privada para Gateway |
| ecr-repository-url | ECR compartilhado do bootstrap |
| jwt-secret-arn | Secret /mecanica/<ambiente>/base/jwt, JSON com somente key |
| jwt-issuer, jwt-audience | mecanica-<ambiente>-auth e mecanica-<ambiente>-api |

NLB interno TCP 80 → instâncias TCP 30080 → Service/Pod 8080. Health check HTTP `/health/ready` na porta 30080; `externalTrafficPolicy: Cluster`. Targets só ficarão saudáveis depois do deploy da API e acesso ao banco. A base não depende da API para criar NLB/target group.

JWT: Secret próprio de cada ambiente, chave HS256 de 64 caracteres alfanuméricos gerada por ephemeral Random e persistida por secret_string_wo com revisão fixa 1. Não há rotação a cada execução nem chave em SSM/estado/plano/outputs. O descarte autorizado da base exclui o Secret sem janela de recuperação; consumidores devem ser descartados antes. Infraestrutura JWT hom aplicada e conferida em 08/10; prd e consumo dos runtimes pendentes.

A base administra a role mecanica-<ambiente>-api-runtime e a associação Pod Identity para default/gerenciamento-api. A confiança exige o cluster ARN, namespace e ServiceAccount exatos. A policy read-jwt permite o Secret JWT e os três parâmetros JWT; a nova read-database permite somente o Secret database/api e seis parâmetros database/v2 (endpoint, port, database-name, api-secret-arn, api-db-user, ssl-mode) do ambiente. A segunda policy está implementada; o mantenedor confirmou o sucesso do deploy da infra hom em 08/10; sem leitura da credencial auth, do Secret administrativo RDS ou de outro ambiente. Os ARNs do banco são construídos na base sem data sources SSM do banco, preservando base → banco → API. O repositório da API implementa ServiceAccount/Deployment nos overlays hom/prd. Não conceder leitura JWT às pipelines API/auth/base. A role Lambda pertence ao bootstrap persistente, conforme a seção auth abaixo.

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

### API, autenticação e Gateway

API mantém Deployment/probes/testes de negócio; runtime resolve credencial restrita e JWT em default. Função consome subnets/SG da base, endpoint e auth-secret-arn do banco e JWT; Gateway consome nlb-arn/URI e ARN qualificado da versão Lambda. Campos futuros da API: image-uri/deployment-name/openapi-key/auth-contracts-version; campos implementados do Gateway estão descritos abaixo. Sem releases ou tentativas.

#### Auth — SAM/CloudFormation com ZIP e IAM persistente

template.yaml e samconfig.toml estáticos no repositório auth. Stacks mecanica-hom-auth/mecanica-prd-auth em us-east-1, conta 121754142617. Código Terraform auth retirado após conferência de ausência de recursos/estado atual ou histórico. Nenhuma adoção/importação/destroy necessária.

Lambda .NET 10/x86_64, ZIP, 512 MB, timeout quinze segundos, sem concorrência reservada ou provisionada; capacidade compartilhada da conta. Variável própria somente Runtime__Environment; AWS_REGION vem do runtime. Log group /aws/lambda/mecanica-<ambiente>-auth com JSON/sete dias. AutoPublishAlias live/AllProperties true com LanguageExtensions antes de SAM. O alias é auxiliar: consumidores usam versão numérica.

AWS CLI lê quatro parâmetros String v2: base/vpc-id, workload-subnet-ids e auth-security-group-id; database/security-group-id. jq converte array JSON de subnets para lista separada por vírgulas. Conferir duas subnets privadas em AZs distintas e SGs da VPC do ambiente. Base possui SG auth; stack administra saída 5432 ao SG PostgreSQL e 443 IPv4/NAT; banco possui ingresso. Sem novo SG/NAT/endpoint/proxy.

| Parâmetro auth/v2 | Valor |
|---|---|
| function-name | Nome da função |
| function-version | Versão numérica publicada |
| invocation-arn | ARN Lambda qualificado por :<versão>, sem $LATEST ou alias |
| zip-key | Chave nativa SAM em sam/auth/<ambiente>/<commit>/ |
| openapi-key | contracts/auth/<commit>/openapi.json |
| auth-contracts-version | 1.0.0 |

Bootstrap possui role/policy auth-runtime persistente, confiança Lambda, GetParameters nos nove nomes do carregador e GetSecretValue somente AWSCURRENT de JWT/database-auth do ambiente. Logs próprios, ENIs e Deny lambda:SourceFunctionArn para operações EC2 pelo código. Pipeline não lê Secrets nem modifica IAM runtime; PassRole exclusivo ao runtime/Lambda. Gateway possui a permissão de invocação futura.

Novo bootstrap acrescenta permissões CloudFormation da stack/transforms, alias e upload SAM por ambiente, remove acesso ao backend/artefatos lambda antigos e mantém roles/buckets existentes. Apenas sam/auth/hom/ e sam/auth/prd/ ficam fora da escrita condicional do bucket; demais contratos/pacote preservados. Aplicar plano salvo revisado antes de publicar.

Sequência: bootstrap → configuração GitHub → base/banco/Job → publicação → deploy SAM manual → hom. Em 2026-10-12, quota total/não reservada dez e zero funções conferidos por GetAccountSettings. Reservas cinco/dez falharam por exigência de preservar dez não reservadas; mantenedor autorizou retirar a reserva do template. Função sujeita à capacidade compartilhada, sem limite próprio; pedido de aumento continua pendente. E3.1/E3.2/E3.6/E3.9 e E2.12 abertas até comprovação real. [Procedimento](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/blob/develop/docs/DEPLOY.md).
Gateway é planejado/aprovado/aplicado manualmente depois dos backends, usando versões fixas dos dois contratos. Não haverá chamada automática a um workflow para promover releases. Permissão de invocação pertence ao Gateway; função usa versão numérica, não `$LATEST`.

## Artefatos e contrato de negócio

Imagem ECR por digest; OpenAPI em contracts/api|auth/<commit>/openapi.json; ZIP em sam/auth/<ambiente>/<commit>/; pacote em packages/GerenciamentoMecanica.Auth.Contracts/<versão>/. OpenAPI, template SAM empacotado e NuGet mantêm SHA-256/escrita condicional/bytes imutáveis; ZIP SAM usa deduplicação/versionamento no prefixo próprio, sem garantia de gravação única. Template: contracts/auth/<commit>/sam-<ambiente>.json e checksum. Pacote independente da infraestrutura; S3 não é feed NuGet nativo. [Procedimento](PACOTE_AUTH_CONTRACTS.md).

HTTP/JWT, CPF/CNPJ, claims, códigos, roles e vínculo da OS permanecem no [contrato de acesso](ACESSO_E_AUTENTICACAO.md). Esta mudança não altera regras de negócio nem remove testes da aplicação/função. Formatos OpenAPI e pacote mantêm versionamento próprio, independente do segmento v2 do SSM.

API: o contrato é gerado dos controllers pelo gerador oficial .NET, somente `v1`, OpenAPI 3.0, título `API da oficina` e versão inicial `1.0.0`. A geração explícita `OpenApiGenerateDocuments=true` escreve `artifacts/openapi/openapi.json` sem inicializar AWS/JWT/persistência; builds comuns/Docker não geram. Development serve formato 3.0. Esquema Bearer HTTP/JWT e segurança por operação refletem `Authorize`/`AllowAnonymous`, sem requisito global ou mudança de permissões. Não inclui destinos/extensões AWS; `/customers/validate` pertence exclusivamente ao produtor auth. O futuro Gateway comporá os contratos e aplicará os destinos por ambiente.

API publica `contracts/api/<commit>/openapi.json` e `openapi.json.sha256` no bucket compartilhado; sidecar contém SHA-256 e nome relativo `openapi.json`. Ambos usam checksum S3, proprietário esperado e escrita condicional. Só `PreconditionFailed` permite baixar/comparar bytes existentes; divergência ou outro erro falham. Objetos permanecem após falha de imagem/deploy ou descarte da API. Resumo contém chaves/hashes/versões S3, commit e digest ECR; disponibilidade do contrato não representa readiness. Conferência AWS concluída em 12/10/2026: execução 38262895558/c96e86d840f35616f1dcc666f45269233e831896, objetos/checksums/metadados corretos e commit correspondente ao digest ECR/Deployment/pod; saúde HTTP/RDS/NLB comprovada. Nenhuma publicação de SSM API v2 nesta entrega.

## Operação e migração

Infra/banco: disparo manual → plan salvo e textual → aprovação protegida sem AWS → apply do mesmo artefato/commit, retenção sete dias. Terraform recusa plano obsoleto do próprio estado; gerar outra execução para revisão. CI somente fmt/validate/renderização. Aprovação deve revisar remoções intencionais e preservação de VPC/EKS/RDS; não é autorização geral para replanejar/aplicar.

API: `.github/workflows/api-deploy.yml`, manual, dois jobs Publicar → Implantar, sem aprovação intermediária. Exige develop/hom ou main/prd, conta/região/role/repositório exatos e checkout do SHA da execução em ambos os jobs; concurrency por ambiente sem cancelar execução em curso. Lê somente ecr-repository-url, cluster-name e namespace da base v2. Gera o OpenAPI no build Release antes dos testes e publica contrato/checksum após testes/OIDC, antes do push da imagem. Publica no ECR compartilhado mecanica/api, tag imutável sha-<commit completo>-<run_id>-<run_attempt>; transmite URI por digest via output da action de build. Deploy confirma BatchGetImage, cluster mecanica-<ambiente>-eks e namespace default, substitui pending com kustomize edit set image no checkout temporário e usa kubectl apply -k no overlay. Rollout até 600 segundos, conferência da imagem desejada e diagnóstico limitado em falha, sem rollback automático. Pipeline não lê Secrets; Pod Identity entrega credencial/JWT ao runtime. Nenhuma publicação de campos API v2 ou Gateway nesta entrega. Publicar o workflow também em main para disponibilizar Run workflow; primeira execução desta alteração develop/hom. [Procedimento](../DEPLOY_API.md).

Migrar base v2 e namespace; adotar banco/credenciais v2 e executar Job; migrar consumidores; só então importar parâmetros v1 ativos em plano dedicado e retirá-los em outro plano aprovado. `legacy_parameter_names` vazio depois da adoção programa sua remoção. Excluir históricos de attempts da lista. Procedimentos de NLB legado e SSM estão em Infra/docs/MIGRACAO_DECLARATIVA.md.

Descarte em ordem Gateway/consumidores → banco → base. API usa api-destroy.yml: um job manual, sem aprovação intermediária, OIDC existente, develop/hom ou main/prd, mesmo grupo de concurrency do API deploy. Lê cluster-name/namespace da base v2, remove o overlay com kubectl delete -k/cascade foreground e confirma ausência de recursos/pods; executá-lo antes de database-destroy e base-destroy, enquanto SSM/cluster existem. Não remove Service/NLB, Secrets, IAM ou ECR. Auth remove função/versões, logs, parâmetros próprios e duas regras de saída; preserva SG/base e role/policy no bootstrap. Aguardar limpeza das ENIs antes de retirar rede, pois DeleteFunction pode concluir antes dessa limpeza. Banco remove Job antes do apply de destroy; base remove manifestos antes de EKS. NLB/associação/SSM v2 são removidos pelo Terraform. Bootstrap nunca integra esse descarte. Validação de destroy somente quando houver descarte solicitado, sem destruir para testar a reformulação.

## Workflows da autenticação — SAM

CI PR/manual: role mecanica-auth-ci-github com AWS_CI_ROLE_ARN, sem Environment de deploy; OIDC restrito ao repositório e PR/develop/main, dois objetos NuGet 1.0.0. Preservar hash/feed, todos os testes .NET/PostgreSQL, SAM lint e actionlint, sem lint OpenAPI. Não usar pull_request_target.

auth-publish: testar/build SAM com role CI → publicar nativamente ZIP/template e separadamente OpenAPI com role auth, mesmo SHA. Build transferido por ID de artefato GitHub da mesma execução/sete dias. Templates/OpenAPI/checksums usam If-None-Match: * e comparação de bytes idênticos em reexecução; ZIP usa deduplicação SAM no prefixo versionado.

auth-provision: environment e artifact_commit ancestral da branch; código/template juntos. Um job confere template/hash/ZIP e rede pública v2, executa sam deploy --no-confirm-changeset --no-fail-on-empty-changeset e aguarda conclusão. Disparo manual autoriza todo o deploy; sem aprovação, ARN de change set ou comandos CloudFormation diretos. Log/template retidos sete dias; rollback nativo e conferência funcional posterior.

auth-destroy: somente environment, um job e nenhuma aprovação intermediária. sam delete --no-prompts reutiliza samconfig.toml: remove ZIP referenciado pelo deploy atual, templates operacionais em sam/auth/<ambiente>/changesets e recursos da stack. Preservar ZIPs de outras revisões, contracts/auth/, pacote, bucket versionado, IAM runtime/CI/deploy, SG da base e outro ambiente. Stack ausente segue sucesso nativo; erros interrompem o workflow. DeleteObject exclusivo do prefixo SAM do ambiente, sem excluir versões históricas; republicar antes de recriar o deploy. Uma falha de stack pode ocorrer após remover artefatos, sem restauração automática. Conferir ENIs via EC2 pelo nome do SG antes de banco/base, sem depender de configuração da stack/SSM. Não há comandos CloudFormation diretos no workflow.

Workflows auth usam hom/prd com OIDC/variáveis e develop/hom ou main/prd, fila auth-<ambiente> sem cancelamento. Não referenciam Environments de aprovação; disparo manual autoriza operação completa. Bootstrap/base/banco/Gateway continuam com revisão/aprovação Terraform. Quota é pré-requisito administrativo; conferir Lambda/SSM/JWT/RDS após implantação. [Operação](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/blob/develop/docs/DEPLOY.md).

## Gateway — Terraform e contratos fixos

`gateway/` no repositório infra administra REST API Regional, VPC Link V2, stage hom/prd, permissão Lambda da versão numérica, logs e sete parâmetros gateway/v2. A base possui SG reservado sem ingresso, saída TCP 80 ao SG NLB e ingresso NLB correspondente. Bootstrap possui service-linked role, role de logs e configuração regional API Gateway persistentes.

| Campo gateway/v2 | Valor |
|---|---|
| rest-api-id, vpc-link-id | IDs do ambiente |
| stage-name, endpoint | hom/prd e URL HTTPS pública |
| api-openapi-key | contracts/api/<api_commit>/openapi.json |
| auth-openapi-key | contracts/auth/<auth_commit>/openapi.json |
| auth-invocation-arn | ARN da versão numérica auth utilizada |

Consumidor lê oito campos base/v2: vpc-id, workload-subnet-ids, gateway-security-group-id, nlb-arn, nlb-security-group-id, nlb-listener-port, nlb-listener-protocol e api-integration-uri; cinco auth/v2: function-name, function-version, invocation-arn, openapi-key e auth-contracts-version. Sem estado remoto ou Secrets. Auth precisa estar implantada; API precisa do contrato publicado e estar saudável para a validação HTTP.

Workflows manuais separados gateway-provision/gateway-destroy, com inputs environment/api_commit/auth_commit; develop/hom ou main/prd. SHA da execução fixa Terraform; dois SHAs completos fixam contratos de outros repositórios. Plan baixa contratos/sidecars S3, confere SHA-256 e publica tfplan/tfplan.txt/contratos no mesmo artefato GitHub sete dias. Aprovação protegida sem AWS; apply usa mesmo checkout e artefato por ID, sem replan ou novo download S3.

Terraform projeta somente roteamento AWS por jsondecode/jsonencode: métodos/caminhos/parametrização e descrições de respostas, HTTP_PROXY à API e AWS_PROXY à Lambda. Original OpenAPI/Bearer/schemas preservado no S3; importador REST não suporta HTTP Bearer. Sem authorizer ou mudança de autorização da API. Excluir os três health checks; não criar ANY/{proxy+}. Logs básicos JSON sete dias, sem corpo/token/documento/IP; observabilidade E5 posterior.

Mudanças nas dependências exigem novo plano Gateway. Descarte Gateway → auth/API → banco → base, conferindo liberação de ENIs antes da rede. Código validado localmente; import/deploy/HTTP/JWT na AWS pendentes. [ADR 010](adrs/010-GATEWAY-REST-V2.md) · [Operação](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/GATEWAY.md).
