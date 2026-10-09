# Inicialização da API e conexão RDS

Inicialização/conexão e configuração declarativa da E2.6 implementadas em 2026-10-08. Policy Terraform e overlays hom/prd estão preparados; a etapa permanece aberta até aplicar o plano aprovado, publicar a imagem e validar o deploy no EKS. Nenhum apply ou deploy faz parte desta implementação.

## Ativação

Sem `Runtime__Environment`, a API usa sua configuração local e não cria clientes AWS. Para ativar o carregamento remoto, definir `Runtime__Environment=hom` ou `prd` e `AWS_REGION=us-east-1`. Campo presente vazio, nulo ou com outro valor impede a inicialização.

O SDK utiliza a cadeia padrão de credenciais. No EKS, a integração é Pod Identity para o ServiceAccount `default/gerenciamento-api`; não fornecer chaves de acesso no Deployment. A associação existe na base. Os overlays criam o ServiceAccount e configuram o Deployment; a nova policy de leitura do banco deve ser aplicada pelo Terraform da base antes do deploy. Não usar os overlays com a imagem provisória `:pending`.

## Leitura e configuração

Antes de registrar autenticação e persistência, a API faz um `GetParameters` com `WithDecryption=false` para nove parâmetros públicos String:

| Prefixo | Campos |
|---|---|
| `/mecanica/<ambiente>/base/v2/` | `jwt-secret-arn`, `jwt-issuer`, `jwt-audience` |
| `/mecanica/<ambiente>/database/v2/` | `endpoint`, `port`, `database-name`, `api-secret-arn`, `api-db-user`, `ssl-mode` |

Os dois ARNs indicam Secrets cuja versão `AWSCURRENT` é lida pelo SDK. JWT fornece `key`, com pelo menos 32 caracteres. A credencial de aplicação fornece `username` e `password`; o usuário deve corresponder ao parâmetro SSM e a `mecanica_api`, e a senha deve estar preenchida. Valores `host`/`port`/`dbname` presentes no Secret não são usados: o SSM fornece a localização atual do banco.

Todos os valores são validados antes de adicionar uma fonte em memória com `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` e `ConnectionStrings:DefaultConnection`. No modo AWS, essa fonte prevalece sobre a configuração local. Não há leitura AWS por requisição, recarga automática de credenciais ou fallback local. Uma mudança de configuração/versão requer reiniciar os pods.

O carregamento usa um prazo total de 45 segundos, cancelamento das chamadas e retry Standard com duas novas tentativas por operação. Erros interrompem a inicialização; mensagens não incluem respostas AWS, JSON dos Secrets, senhas, chaves ou a string de conexão completa.

## PostgreSQL

`NpgsqlConnectionStringBuilder` monta a conexão usando os valores atuais do SSM e a credencial da API, preservando os padrões existentes de pool e timeout. `ssl-mode` deve ser `verify-full`; a conexão exige TLS com verificação do certificado e hostname. `GssEncryptionMode=Disable` evita negociação GSS/Kerberos nesse caminho TLS.

`Infrastructure/Certificates/rds-ca.pem` é o certificado público já utilizado pelo Job do banco. O projeto o copia para `Certificates/rds-ca.pem` no build e publish, inclusive no container. O certificado precisa ser mantido atualizado junto à configuração de CA do RDS. A API não cria schema, seeds, usuários ou grants.

Os registros de conexão/repositórios/transações, health checks e regras JWT permanecem como estavam. A prontidão continua verificando acesso ao PostgreSQL; a inicialização do carregador não abre conexão com o banco. No modo AWS, não há redirecionamento HTTPS no pod: NLB e probes usam HTTP interno, e o TLS público pertence à futura integração do Gateway. O redirecionamento local permanece.

## Permissões e manifestos

O Terraform da base acrescenta `aws_iam_role_policy.api_database`, chamada `read-database`, à role existente `mecanica-<ambiente>-api-runtime`, preservando `read-jwt`, confiança e associação Pod Identity. Permite `ssm:GetParameter`/`ssm:GetParameters` apenas para os seis campos do banco listados acima e `secretsmanager:GetSecretValue` apenas para `/mecanica/<ambiente>/database/api`, com sufixo ARN de seis caracteres. Não inclui o Secret administrativo RDS, a credencial auth, parâmetros extras ou outro ambiente.

Os ARNs são montados a partir de conta/região/ambiente, sem ler SSM do banco durante o provisionamento da base; a ordem continua base → banco → API. O bootstrap já autoriza gestão de policies inline somente nessa role, portanto não exige mudança de código ou outro apply administrativo.

`deploy/kubernetes/base` contém Deployment e HPA comuns e é o ponto de entrada local, preservando o resultado anterior. Há somente três arquivos kustomization: base, hom e prd. Os overlays referenciam diretamente a base e declaram seus próprios ajustes AWS, sem uma camada common. Cada um inclui seu manifesto ServiceAccount `default/gerenciamento-api`, sem Postgres ou `db-secrets`, e configura `AWS_REGION=us-east-1`, `AWS_EC2_METADATA_DISABLED=true` e `Runtime__Environment` correspondente. Hom usa `ASPNETCORE_ENVIRONMENT=Staging`; prd usa `Production`. O Service NodePort continua administrado pela base.

A startup probe recebe `failureThreshold: 12`, com período de cinco segundos, para acomodar o carregamento AWS de até 45 segundos. Readiness, liveness, recursos e HPA permanecem; capacidade será tratada na E2.7. A imagem provisória é `121754142617.dkr.ecr.us-east-1.amazonaws.com/mecanica/api:pending`; substituir por digest na etapa de publicação E2.11 antes de aplicar os overlays.

Renderização local, também executada pelo CI, sem acesso ao cluster:

```bash
kubectl kustomize deploy/kubernetes/base
kubectl kustomize deploy/kubernetes/overlays/hom
kubectl kustomize deploy/kubernetes/overlays/prd
```

## Validação desta entrega

Testes .NET usam clientes AWS substituídos e valores fictícios. Cobrem modo local, configuração hom/prd, precedência, parâmetros/Secrets inválidos, TLS, erros sem exposição de valores, cancelamento e prazo real de 45 segundos. Build/publish e testes de compatibilidade JWT verificam a integração local e a inclusão do certificado. Não acessam Secrets ou RDS reais.

Para publicar: versionar as alterações dos dois repositórios, revisar/aplicar o plano da base com somente a nova policy, publicar a imagem no ECR e preparar o deploy com digest. Depois validar Pod Identity, conexão TLS, probes e targets do NLB. Esta implementação não cria workflow de deploy nem executa publicação, apply ou comandos de alteração Kubernetes. E2.6 permanece aberta até a validação real.
