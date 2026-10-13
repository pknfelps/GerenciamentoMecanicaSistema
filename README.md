# Sistema de Gerenciamento de Mecânica

API do sistema de oficina: usuários, clientes, veículos, catálogo, estoque e ciclo de ordens de serviço (OS). Mantém as regras de negócio, persistência, autenticação interna, notificações SMTP, testes, imagem Docker e Deployment/HPA.

## Estado da implementação

A separação em quatro repositórios está integrada. O CI valida testes/cobertura, SonarCloud e build da imagem. Base/banco/API, autenticação Lambda, Gateway e compatibilidade JWT foram validados em hom, incluindo o ciclo de OS e expiração real. A primeira entrega da E4 acrescenta status consultável de clientes, perfis internos Admin/Mechanic e ID estável no JWT; sua publicação e validação AWS ocorrerão pelo fluxo de PR/deploy do mantenedor.

| Disponível | A implementar |
|---|---|
| API .NET, JWT interno, PostgreSQL, adaptador SMTP e health checks | Aplicar a matriz de permissões Admin/Mechanic/Customer e validar SMTP na AWS |
| Dockerfile, Compose opcional, Deployment/HPA e workflow manual ECR/EKS | Executar e validar deploy nos ambientes hom/prd |
| CI, autenticação serverless e API Gateway validados em hom | Validação prd e observabilidade OpenTelemetry/New Relic |

O foco de uso é a API na AWS. O endpoint do ambiente é publicado em SSM `/mecanica/<ambiente>/gateway/v2/endpoint` e pode mudar após recriação. A [especificação de acesso](docs/arquitetura/ACESSO_E_AUTENTICACAO.md) distingue as regras alvo das permissões ainda pendentes; as [collections](postman/collections) e o OpenAPI gerado representam as operações atuais.

### Identidade e status — primeira entrega da E4

- `POST /Authentication` mantém `name/password/role` e retorna o token no formato existente. Cadastro e login internos aceitam somente os nomes exatos `Admin` e `Mechanic`; perfis antigos, numéricos ou `Customer` retornam 400. Credenciais incorretas ou usuário inexistente retornam 401.
- O JWT interno contém Name, Role e NameIdentifier (UUID lido da tabela users). O consumidor não escolhe o ID. HS256, expiração de dez minutos, issuer/audience e tolerância de relógio permanecem; não há revogação ou nova exigência global de ID para tokens anteriores.
- `GET /Customers` acrescenta `status` como string `Active` ou `Inactive`. Novos clientes são Active; o PATCH preserva o estado existente. Status não faz parte dos requests: campos extras não permitem criar Inactive ou reativá-lo. DELETE continua excluindo o cadastro.
- O status persistido é convertido explicitamente, sem fallback para Active. Os mapeadores constroem o domínio diretamente, sem try/catch; validações de identidade/perfil do usuário propagam DomainValidationException (400 no middleware atual). Não há conversão de perfis antigos. O SQL do banco já contém os campos/seeds necessários.
- As permissões das rotas ainda são as atuais: Mechanic pode autenticar, mas acesso operacional e vínculo do cliente com a OS serão implementados na entrega seguinte. Não existe endpoint de inativação nesta parte.

## Componentes e repositórios

Diagrama da aplicação existente:

```mermaid
flowchart LR
    CLIENT["Cliente HTTP"] --> API["ASP.NET Core / Controllers"]
    API --> SERVICE["Service: casos de uso"]
    SERVICE --> DOMAIN["Domain: regras"]
    SERVICE --> PORTS["Service.Interface: contratos"]
    PORTS --> INFRA["Infrastructure: adaptadores"]
    INFRA --> PG[("PostgreSQL")]
    INFRA --> SMTP["Servidor SMTP"]
    DI["DependencyInjection"] -.-> API
    DI -.-> INFRA
```

| Repositório | Responsabilidade |
|---|---|
| Este repositório | API, camadas, testes, Dockerfile/Compose e Deployment/HPA |
| [Infraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/tree/develop) | Terraform, plataforma Kubernetes, Service e entrada Gateway/NLB |
| [Banco](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/tree/develop) | SQL/seeds, infraestrutura RDS PostgreSQL e Job de inicialização |
| [Autenticação](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/tree/develop) | Lambda/SAM de validação CPF/CNPJ e emissão de JWT Customer |

Os projetos `Domain.Interface` e `Service.Interface` definem contratos; `Infrastructure` implementa persistência PostgreSQL, JWT, hash de senha e SMTP; `DependencyInjection` registra os componentes. Consultas comuns de usuários não retornam senha/hash; os fluxos de credenciais usam `UserCredentials`.

## Tecnologias e pré-requisitos

- .NET SDK 10 / ASP.NET Core 10.
- PostgreSQL 16 para uso local; acesso por Npgsql/Dapper.
- Docker com engine Linux e Compose v2; smtp4dev para e-mails locais.
- Docker ativo para a suíte completa: os testes de persistência usam Testcontainers e criam seus próprios containers PostgreSQL.
- kubectl com Kustomize para renderizar os manifestos.
- GitHub Actions e SonarCloud no CI.

AWS EKS, RDS PostgreSQL, ECR, API Gateway e Lambda compõem o destino da Fase 3, validado em hom. Terraform da base e entrada pertence ao repositório de infraestrutura; Terraform do banco pertence ao repositório de banco. A escolha RDS `db.t3.micro` Single-AZ substituiu Aurora após a restrição do AWS Free Plan ([ADR 007](docs/arquitetura/adrs/007-RDS-FREE-PLAN.md)).

## Build e testes

Na raiz, com .NET 10 e Docker ativo para os testes de persistência:

```bash
dotnet restore GerenciamentoMecanicaSistema.slnx
dotnet build GerenciamentoMecanicaSistema.slnx --no-restore
dotnet test GerenciamentoMecanicaSistema.slnx --no-build --no-restore
```

Restore/build não precisam de um banco em execução. Os testes de persistência preparam suas próprias tabelas e não usam o banco do Compose.

## Contrato OpenAPI

Para gerar o contrato a partir dos controllers, execute:

```bash
dotnet build GerenciamentoMecanicaSistema.slnx --configuration Release -p:OpenApiGenerateDocuments=true
```

O gerador oficial produz somente `artifacts/openapi/openapi.json`, em OpenAPI 3.0, com título **API da oficina** e versão **1.0.0**. A pasta é ignorada pelo Git e pelo Docker. Builds comuns e Docker não geram o documento. A geração registra controllers/OpenAPI sem carregar AWS, credenciais JWT ou persistência, inclusive com `Runtime__Environment=hom`; a inicialização normal mantém suas validações.

O documento servido em Development também usa OpenAPI 3.0. Bearer JWT é exigido somente nas operações protegidas por `Authorize`; `AllowAnonymous` prevalece. Autenticação interna, health checks, status e orçamento permanecem públicos conforme os atributos atuais. Permissões de cliente serão tratadas na E4; `/customers/validate` pertence exclusivamente à autenticação.

O [API deploy](docs/DEPLOY_API.md) gera e testa o contrato, publica `contracts/api/<commit>/openapi.json` e seu `.sha256` no S3 e então publica a imagem. Objetos existentes só são aceitos com bytes idênticos. O resumo registra commit, hashes, versões S3 e digest da imagem. A publicação não comprova prontidão; composição e implantação do Gateway permanecem pendentes.

Para validar somente a imagem, sem iniciar a aplicação:

```bash
docker build --file GerenciamentoMecanicaSistema/Dockerfile --tag gerenciamento-mecanica-api:local .
```

## Execução local opcional

1. Copie [.env.example](.env.example) para `.env`:

   ```powershell
   Copy-Item .env.example .env
   ```

   Em Bash, use `cp .env.example .env`. Defina `POSTGRES_PASSWORD` e uma `JWT_KEY` com pelo menos 32 caracteres. O arquivo local é ignorado pelo Git.

2. Inicie as dependências:

   ```bash
   docker compose up -d db smtp
   ```

3. Prepare manualmente o banco vazio seguindo o [README do banco](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/tree/develop#execução-local-opcional). O SQL fica exclusivamente nesse repositório. O Compose não cria tabelas/seeds e não sincroniza scripts.

4. Inicie a API:

   ```bash
   docker compose up --build -d api
   docker compose ps
   docker compose logs -f api
   ```

A disponibilidade do PostgreSQL não comprova que o esquema foi preparado. A API pode iniciar com banco vazio, mas as operações de negócio dependem das tabelas.

| Serviço | Endereço local padrão |
|---|---|
| API | http://localhost:8080 |
| Swagger UI (Development) | http://localhost:8080/swagger |
| OpenAPI (Development) | http://localhost:8080/openapi/v1.json |
| Readiness | http://localhost:8080/health/ready |
| PostgreSQL | localhost:5432 |
| Painel smtp4dev | http://localhost:3000 |
| SMTP | localhost:2525 |

As portas do host podem ser ajustadas no `.env`. O Compose usa `ASPNETCORE_ENVIRONMENT=Development`; Swagger/OpenAPI não são expostos pelo código atual em outros ambientes.

Para encerrar preservando os dados, use `docker compose down`. Se optar por apagar os dados locais, `docker compose down --volumes` remove os volumes de PostgreSQL e smtp4dev; depois repita os passos 2–4, incluindo a preparação manual do banco.

## Autenticação e uso da API

Após aplicar os seeds, o usuário educacional é `Admin`, senha `Admin@123`, role `Admin`. A senha é armazenada como hash PBKDF2. Esses dados são somente para demonstração.

O login atual é `POST /authentication`:

```json
{
  "name": "Admin",
  "password": "Admin@123",
  "role": "Admin"
}
```

Use o token retornado como `Authorization: Bearer <token>`. Importe as [collections](postman/collections) e o [ambiente Postman](postman/environments/Dev.environment.yaml); configure `base_url` e `token`.

A API cobre usuários, clientes, veículos, catálogo, materiais/estoque e criação, diagnóstico, orçamento, execução e entrega de OS. A futura rota `POST /customers/validate` será atendida pela Lambda. Novas permissões e operações de usuários ainda serão implementadas conforme o [contrato de acesso](docs/arquitetura/ACESSO_E_AUTENTICACAO.md).

## Configuração e saúde

| Configuração | Uso |
|---|---|
| `ConnectionStrings__DefaultConnection` | Conexão PostgreSQL |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key` | Emissão e validação JWT compatíveis no ambiente |
| `Runtime__Environment` | Ausente: configuração local. `hom` ou `prd`: carregar JWT e conexão RDS pela AWS na inicialização |
| `AWS_REGION` | Região dos clientes AWS quando o carregamento remoto estiver habilitado (`us-east-1` nos ambientes previstos) |
| `EmailSettings__Host`, `EmailSettings__Port` | Servidor SMTP |
| `EmailSettings__Username`, `EmailSettings__Password`, `EmailSettings__UseTls` | Autenticação/TLS do SMTP |
| `EmailSettings__SenderName`, `EmailSettings__SenderEmail` | Identificação do remetente |

O Compose mapeia essas configurações a partir do `.env` e dos serviços locais. E-mails enviados ao smtp4dev ficam no painel local. Para outro destino, configure o servidor SMTP apropriado.

O carregamento inicial AWS está implementado: lê os parâmetros SSM v2 e a versão `AWSCURRENT` dos Secrets JWT e da credencial `mecanica_api`, mantendo os valores em memória. A conexão usa TLS `VerifyFull` com o certificado público RDS incluído no publish. Falha de leitura, configuração inválida ou prazo de 45 segundos excedido impede a inicialização, sem fallback local. Sem `Runtime__Environment`, o fluxo local permanece; campo vazio ou diferente de `hom`/`prd` é inválido. Consulte [inicialização e conexão AWS](docs/INICIALIZACAO_AWS.md). Policy Terraform e ServiceAccount/overlays estão preparados; o mantenedor confirmou o deploy da infra hom. Publicação da API e validação real no EKS permanecem pendentes.

| Rota | Finalidade |
|---|---|
| `/health/startup` | Inicialização da API |
| `/health/ready` | Prontidão e acesso ao banco |
| `/health/live` | Processo ativo |

As probes do Deployment usam essas rotas. Não houve alteração de probes na separação dos repositórios.

## Kubernetes, CI e deploy

A [decisão declarativa](docs/arquitetura/adrs/008-INFRAESTRUTURA-DECLARATIVA.md) mantém Terraform para bootstrap/base/banco/Gateway e manifestos Kubernetes próprios. Auth usa SAM com ZIP ([ADR 009](docs/arquitetura/adrs/009-SAM-AUTENTICACAO.md)), template/workflows validados localmente e implantação pendente. O disparo manual autoriza deploy/descarte auth completos, sem aprovação intermediária; sam deploy executa diretamente. Infra/banco mantêm plan salvo, aprovação e apply. CI/testes de negócio da API preservados; Service NodePort 30080 ligado ao NLB Terraform.

[deploy/kubernetes/base](deploy/kubernetes/base) contém Deployment e HPA e é o ponto de entrada do Kustomize local. Os overlays hom/prd referenciam essa base e declaram diretamente os ajustes AWS de cada ambiente. Há somente três arquivos kustomization; o Service pertence à infraestrutura. Para conferir a composição sem acessar o cluster:

```bash
kubectl kustomize deploy/kubernetes/base
kubectl kustomize deploy/kubernetes/overlays/hom
kubectl kustomize deploy/kubernetes/overlays/prd
```

A composição local mantém a imagem anterior no Docker Hub e o Secret `db-secrets`, com `CONNECTION_STRING` e `JWT_KEY`; o [exemplo de Secret](deploy/kubernetes/db-secrets.example.yaml) contém placeholders. Os overlays hom/prd retiram essas referências, habilitam a leitura AWS e usam o ServiceAccount `default/gerenciamento-api`, com startup probe de 60 segundos. A imagem ECR `:pending` é substituída por digest pelo workflow no checkout temporário, antes de aplicar o overlay. O mantenedor confirmou o sucesso do deploy da infra hom com a policy de leitura do banco; publicação/deploy da API e validação real ainda estão pendentes, mantendo a E2.6 aberta.

A [pipeline](.github/workflows/pipeline.yml) executa em PRs para `develop`/`main`, além de acionamento manual. Os jobs são `unit-tests`, `code-analysis` e `build-image`; a análise usa `SONAR_TOKEN`. O build Docker ocorre após testes/análise e gera tag `sha-<commit>` no runner. Não há push de imagem, artefato de imagem disponível para download ou deploy nesse workflow.

O [API deploy](.github/workflows/api-deploy.yml) é manual e separado do CI: **Publicar → Implantar**, sem aprovação intermediária. Exige `develop/hom` ou `main/prd`, testa em Release, publica `linux/amd64` no ECR com tag `sha-<commit>-<run_id>-<run_attempt>` e aplica o overlay usando o digest retornado. Ambos os jobs usam o mesmo SHA e OIDC; a pipeline não lê Secrets. Aguarda rollout por 600 segundos e, em falha, exibe diagnóstico limitado sem rollback automático. Consulte o [procedimento de deploy](docs/DEPLOY_API.md) para pré-requisitos, publicação do workflow também em `main`, primeiro disparo `develop/hom` e validação RDS/probes/NLB. A [RFC de entrega](docs/arquitetura/rfcs/002-ENTREGA.md) mantém a ordem entre os repositórios.

O [API destroy](.github/workflows/api-destroy.yml) remove Deployment, HPA, ServiceAccount e pods dependentes em um único job manual, sem aprovação intermediária. Valida develop/hom ou main/prd e compartilha concurrency com API deploy. Para descartar o ambiente, executar API destroy, depois database-destroy e base-destroy; os dois últimos mantêm suas aprovações de planos Terraform. ECR/imagens e bootstrap são preservados. Consulte o [procedimento](docs/DEPLOY_API.md#descartar-o-ambiente).

## Contratos de integração

A [especificação central](docs/arquitetura/CONTRATOS_ENTRE_REPOSITORIOS.md) define campos, versões, artefatos, secrets e falhas. Este repositório é o produtor do componente lógico **api** e do pacote **GerenciamentoMecanica.Auth.Contracts**. A versão 1.0.0 foi publicada no S3 via OIDC; o sucesso do workflow de consumo na autenticação foi confirmado pelo mantenedor. O deploy manual e a publicação do OpenAPI estão implementados; publicação S3, checksum e correspondência com a imagem hom conferidos em 12/10/2026, execução 38262895558. Configuração SSM da API e Gateway permanecem posteriores. Consulte [operação do Auth.Contracts](docs/arquitetura/PACOTE_AUTH_CONTRACTS.md).

| Interface | Responsabilidade da API |
|---|---|
| Produz para implantação | Imagem ECR identificada por digest; o Deployment não usa latest |
| Produz para Gateway | contracts/api/<commit>/openapi.json e seu .sha256, gerados da mesma revisão implantada |
| Produz para a função | Pacote NuGet de versão fixa em packages/GerenciamentoMecanica.Auth.Contracts/<versao>/, com .sha256 |
| Publica em SSM | /mecanica/<ambiente>/api/v2/: configuração administrada pelo Terraform, sem releases/tentativas; smtp-secret-arn quando aplicável |
| Consome da base | Cluster/namespace/Service, ECR, referência JWT, issuer/audience e endpoint OTel quando habilitado |
| Consome do banco | Writer/porta/database/TLS, versão/hash SQL e api-secret-arn |
| Resolve no runtime | Sua credencial de banco, JWT e SMTP autenticado; nunca a credencial administrativa do banco |
| Solicita após deploy | Plano e aprovação do Gateway com versões fixas dos contratos API/função; operações sequenciais pelo mantenedor |

Build/testes e publicação do pacote são independentes de banco/EKS. Publicação e deploy são resultados distintos: dependência ausente deixa a implantação bloqueada, sem sinalizar sucesso nem criar infraestrutura implicitamente. Release pronta exige rollout/readiness aprovados e dependências compatíveis. A API não inicializa SQL nem administra o Service/NLB.

O [workflow manual OIDC](.github/workflows/aws-oidc-check.yml) verifica a role api do Environment selecionado, em develop/hom ou main/prd. Ele não testa permissões de deploy, publicação de artefatos ou consumo de secrets. As variáveis de entrada são AWS_REGION, AWS_ROLE_ARN e ARTIFACTS_BUCKET; os demais identificadores vêm dos contratos publicados.

## Desenvolvimento e ambientes

Crie branches de trabalho a partir da `develop` atualizada e direcione os PRs para `develop`. A promoção `develop -> main` ocorre quando a entrega estiver concluída. `develop` corresponde a **hom** e `main` a **prd**; os ambientes poderão coexistir. Proteções, Environments e autenticação OIDC foram preparados. O deploy da API é disparado manualmente, executando publicação e implantação completas; não ocorre por push. O arquivo do workflow também deve existir em `main` para disponibilizar Run workflow.

## Referências

- [Arquitetura, diagramas, RFCs e ADRs](docs/arquitetura/README.md).
- [Contrato de autenticação e permissões](docs/arquitetura/ACESSO_E_AUTENTICACAO.md).
- [GitHub Actions](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions).
- Demonstração final e vídeo: pendentes.

Os workflows de CI pipeline/auth-contracts validam PRs para develop/main e permitem execução manual; não repetem os checks no push da mesma revisão. Commits novos substituem checks antigos do mesmo PR, preservando nomes dos jobs e isolando publicações manuais.

### Infraestrutura JWT da E2.12

A base implementa Secret JWT por ambiente e publica jwt-secret-arn, jwt-issuer e jwt-audience em /mecanica/<ambiente>/base/v2/. Base JWT hom aplicada e conferida em 08/10: Secret/SSM/Pod Identity, No changes e isolamento IAM simulado. A API já implementa leitura inicial pelo SDK .NET, overlays com ServiceAccount e policy Terraform para leitura do banco. Deploy da infra hom com essa policy foi confirmado pelo mantenedor. Publicação/deploy da API, validação no EKS, Lambda e prd permanecem pendentes. A role e a associação Pod Identity para default/gerenciamento-api são administradas pela base. A chave AWSCURRENT fica em memória, sem fallback local ou cópia para Secret Kubernetes. HS256, dez minutos e tolerância de relógio são preservados. [Contrato](docs/arquitetura/CONTRATOS_ENTRE_REPOSITORIOS.md).

Gateway REST/VPC Link V2 implementado localmente no repositório de infraestrutura, com unidade/workflows separados e plano bootstrap para revisão. A API continua privada atrás do NLB e suas permissões não foram alteradas. [Decisão e dependências](docs/arquitetura/adrs/010-GATEWAY-REST-V2.md); deploy e validação HTTP/JWT pendentes.
