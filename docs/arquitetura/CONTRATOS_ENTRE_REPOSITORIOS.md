# Contratos operacionais entre repositórios

## Fontes e convenções

Conta 121754142617, região us-east-1; environment hom|prd validado. Metadados base/database/auth/gateway usam v2; email usa `/mecanica/<ambiente>/email/v1/<campo>`. Todos são String; listas usam JSON. Não há terraform_remote_state entre repositórios, release/candidato/geração v1 ou validade de prontidão por timestamp. SSM configura destinos; Job/rollout/HTTP comprovam operação.

| Produtor | Fonte do catálogo |
|---|---|
| Base | Infraestrutura/terraform/metadata.tf |
| Banco | BancoDados/terraform/metadata.tf |
| Auth | Serverless/templates/auth.yaml, AWS::SSM::Parameter |
| Email | Serverless/templates/email.yaml, AWS::SSM::Parameter |
| Gateway | Infraestrutura/gateway/metadata.tf |

**Não há publicador `/api/v2/` no código entregue.** A API consome base/database/email; imagem/contrato da API são selecionados por digest/commit dos workflows. Campos de observabilidade são declarados nos manifestos/SAM e não se tornam parâmetros SSM apenas por terem sido planejados anteriormente.

## Base v2

| Campos | Uso |
|---|---|
| vpc-id, workload-subnet-ids, database-subnet-ids | Rede e listas JSON de subnets |
| cluster-name, cluster-arn | Cluster EKS |
| namespace, init-namespace | default e database-init |
| api-security-group-id, init-security-group-id | SG efetivo do EKS; podem coincidir |
| auth-security-group-id, email-security-group-id, gateway-security-group-id | SGs reservados auth/email e VPC Link V2 |
| smtp-host, smtp-port | DNS do NLB interno e porta SMTP TCP 2525; usados somente por email-provision |
| api-service-name, api-service-port, api-node-port | svc-gerenciamento-api, 80 e 30080 |
| nlb-arn, nlb-dns-name, nlb-security-group-id, nlb-target-group-arn | NLB interno e destino |
| nlb-listener-port, nlb-listener-protocol, api-integration-uri | 80, TCP e URI HTTP privada |
| ecr-repository-url | Repositório compartilhado mecanica/api |
| jwt-secret-arn, jwt-issuer, jwt-audience | Referência JWT, mecanica-<ambiente>-auth e mecanica-<ambiente>-api |

Secret JWT é JSON com key, gerado via valores ephemeral/write-only e revisão fixa, sem valor em SSM/planos/estado. API usa Pod Identity default/gerenciamento-api; Lambda usa auth-runtime persistente. Cada runtime lê AWSCURRENT no bootstrap e mantém em memória; alteração de chave exige reinicialização. API consulta configuração e Secret do banco API; função consulta somente a credencial auth. EmailFunction não recebe banco/JWT/SSM/Secrets em runtime; SMTP é configuração resolvida no deploy. Sem fallback local em modo AWS ou cópia JWT para Secret Kubernetes.

O Collector usa endpoint interno fixado nos overlays, namespace observability e Pod Identity própria. Sua License Key está no Secret persistente `/mecanica/<ambiente>/observability/newrelic-license-key`; não existe newrelic-secret-arn/otel-collector-endpoint publicado automaticamente em base/v2. Overlay API hom habilita OTel; prd mantém false. SAM habilita permanentemente os sinais da função.

## Banco v2

| Campos | Uso |
|---|---|
| instance-arn, endpoint, port, database-name, security-group-id | RDS, 5432/mecanica e SG |
| api-secret-arn, api-db-user | Credencial mecanica_api |
| auth-secret-arn, auth-db-user | Credencial mecanica_auth |
| schema-version, sql-sha256, ssl-mode | 1.0.0, SHA-256 do Init.sql e verify-full |

Referências não demonstram que o Job concluiu. O Job cria esquema/seeds/marcador atomicamente em banco vazio, exige versão/hash compatível em reexecução e provisiona grants. ConfigMap contém SQL/host/CA/hash; Secrets temporários admin/API/auth são write-only e removidos ao final inclusive em erro. Senha AWSCURRENT/revisão fixa não é rotacionada em todo apply. API/função não recebem acesso ao Secret administrativo.

Credencial API tem DML nas nove tabelas de domínio, inclusive histórico; credencial auth tem SELECT somente id/name/document/status/email de customers e SELECT/INSERT/UPDATE/DELETE nas duas tabelas de desafios/limites. Nenhuma recebe o marcador operacional. Endpoint/porta/database são lidos do SSM, não de campos antigos eventualmente persistidos no Secret. [Modelo/grants](BANCO_E_MODELO.md).

## Auth v2

| Campo | Uso |
|---|---|
| function-name | Nome mecanica-<ambiente>-auth |
| function-version | Versão numérica publicada |
| invocation-arn | ARN qualificado pela versão numérica |
| zip-key | Chave nativa SAM do ZIP |
| openapi-key | contracts/auth/<commit>/openapi.json |
| auth-contracts-version | Pacote JWT Auth.Contracts 1.0.0; distinto do OpenAPI HTTP auth 2.0.0 |

Stack SAM administra esses parâmetros e o alias live auxiliar. Gateway consome a versão numérica, não live ou $LATEST; mudança de versão exige novo plano Gateway. Auth-runtime pertence ao bootstrap, SG à base e ingresso PostgreSQL ao banco; auth administra somente seus egress, função/versão/alias/logs/SSM. [ADR 009](adrs/009-SAM-AUTENTICACAO.md).

## Email v1

| Campo | Uso |
|---|---|
| function-name | mecanica-<ambiente>-email |
| function-version | Versão numérica publicada, somente referência operacional |
| invocation-arn | ARN do alias live exato do ambiente |
| contract-version | String 1, contrato Invoke v1 |
| zip-key | Chave nativa SAM do ZIP email |

Stack email é independente de auth. API/auth carregam invocation-arn e contract-version na inicialização reutilizada, sem lookup por mensagem. Pipelines consumidoras conferem essas duas chaves antes do deploy; isso não comprova saúde SMTP. Runtime usa IAM Invoke somente do alias live correspondente, sem URL/Gateway público ou endpoint override AWS. Compatibilidade v1 permite atualizar o alias sem recompilar consumidores; mudança incompatível exige contrato/migração de consumidor próprios, não adaptação automática de dados SQL.

A base mantém SMTP4dev efêmero no namespace smtp4dev, listener TCP 2525 no NLB existente e NodePort 30025 no mesmo ASG. Web somente ClusterIP e port-forward autorizado; sem relay/PVC. SMTP privado demonstrativo não usa TLS/autenticação; não o descrever como entrega externa ou criptografia. Perda da caixa exige novo pedido respeitando limites.

## Gateway v2

| Campo | Uso |
|---|---|
| rest-api-id, vpc-link-id, stage-name | Identidade do Gateway/VPC Link/stage |
| endpoint | URL atual de negócio, incluindo stage |
| api-openapi-key, auth-openapi-key | Contratos selecionados pelos commits |
| auth-invocation-arn | Versão Lambda integrada |

Endpoint muda após destroy/recreate; não é URL estável a ser hardcoded no consumidor ou evidência de disponibilidade enquanto o ambiente está desligado. Gateway valida OpenAPI 3.0/formato/rotas/identidade da versão e compõe HTTP_PROXY/AWS_PROXY. Não é validação semântica completa de todos os DTOs. Health/Swagger/OpenAPI não são publicados.

## Artefatos e integridade

| Artefato | Seleção/proteção |
|---|---|
| Imagem API | ECR mecanica/api, tag por commit/run/attempt, deploy por digest |
| Contrato API | contracts/api/<commit>/openapi.json e checksum |
| Contrato auth | contracts/auth/<commit>/openapi.json e checksum |
| Template SAM | contracts/auth/<commit>/sam-<ambiente>.json e checksum |
| ZIP auth/email | sam/<auth ou email>/<ambiente>/<commit>/chave-de-conteudo; Bucket/Key/Version fixos no template |
| Template email | contracts/email/<commit>/sam-<ambiente>.json e checksum; sem OpenAPI público |
| Auth.Contracts | packages/GerenciamentoMecanica.Auth.Contracts/1.0.0/ e checksum; versão exata no consumidor |

OpenAPI/template/NuGet usam escrita condicional e comparação de bytes em reexecução, sem sobrescrever conteúdo divergente. ZIP é exceção de gravação condicional do SAM; VersionId fixa a seleção, sem tornar a chave imutável. Republicação idêntica conserva a versão original depois de comparar os bytes do ZIP/template. Templates antigos sem Version exigem commit novo; provision usa head-object --version-id e SAM Code.S3ObjectVersion.

API gera OpenAPI 3.0 da mesma revisão via OpenApiGenerateDocuments=true, sem inicializar AWS/JWT/banco. Auth publica contrato 3.0 estático sem destino/extensões AWS. NuGet 1.0.0 tem hash fixado no workflow consumidor; API referencia o projeto local. Contratos são globais por commit/versão, não Secret ou prova de exclusividade de ambiente. S3 é armazenamento do nupkg, não feed NuGet nativo.

## Ordem e responsabilidade operacional

Bootstrap aprovado → base/Collector/SMTP4dev → banco/Job e email-publish/email-provision (independentes) → API e auth-publish/auth-provision → Gateway com commits fixos e OpenAPI auth 2.0.0. Terraform base/banco/Gateway e New Relic usam plano salvo/aprovação/apply exato; API/auth/email usam seus fluxos manuais sem aprovação intermediária. CI não faz deploy automático após merge; diferença R06 registrada e análise adiada.

Destroy: Gateway → API/auth → banco/email → base, com checagens diretas dos consumidores. Erros/ENIs pendentes bloqueiam; sem novo marcador SSM, exclusão forçada ou espera automática. Email-destroy exige API/auth ausentes; base-destroy exige banco/email ausentes. SAM CLI 1.167.0 preserva ZIP com CodeUri objeto; contratos, ECR, estados, IAM bootstrap e recursos New Relic persistem.
