# Inicialização da API e conexão RDS

Primeira parte da E2.6 implementada em 2026-10-08. O código está preparado; a etapa permanece aberta até configurar IAM/manifestos e validar o deploy no EKS. Nenhum provisionamento ou deploy faz parte desta entrega.

## Ativação

Sem `Runtime__Environment`, a API usa sua configuração local e não cria clientes AWS. Para ativar o carregamento remoto, definir `Runtime__Environment=hom` ou `prd` e `AWS_REGION=us-east-1`. Campo presente vazio, nulo ou com outro valor impede a inicialização.

O SDK utiliza a cadeia padrão de credenciais. No EKS, a integração prevista é Pod Identity para o ServiceAccount `default/gerenciamento-api`; não fornecer chaves de acesso no Deployment. A associação existe na base, mas o ServiceAccount/Deployment e as permissões de leitura do banco ainda precisam ser preparados na próxima entrega. Não habilitar o modo AWS no Deployment atual antes disso.

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

Os registros de conexão/repositórios/transações, health checks, redirecionamento HTTPS e regras JWT permanecem como estavam. A prontidão continua verificando acesso ao PostgreSQL; a inicialização do carregador não abre conexão com o banco.

## Validação desta entrega

Testes .NET usam clientes AWS substituídos e valores fictícios. Cobrem modo local, configuração hom/prd, precedência, parâmetros/Secrets inválidos, TLS, erros sem exposição de valores, cancelamento e prazo real de 45 segundos. Build/publish e testes de compatibilidade JWT verificam a integração local e a inclusão do certificado. Não acessam Secrets ou RDS reais.

A próxima entrega deve ampliar a role da API para os seis parâmetros e o Secret de aplicação do banco, criar ServiceAccount/overlays, publicar/deployar a imagem e validar Pod Identity, conexão TLS e probes. E2.6 permanece aberta até essa validação.
