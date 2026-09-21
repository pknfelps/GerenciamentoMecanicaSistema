# Sistema de Gerenciamento de Mecânica

API do sistema de oficina: usuários, clientes, veículos, catálogo, estoque e ciclo de ordens de serviço (OS). Mantém as regras de negócio, persistência, autenticação interna, notificações SMTP, testes, imagem Docker e Deployment/HPA.

## Estado da implementação

A separação em quatro repositórios está integrada. A API e os testes existentes são executáveis; a pipeline valida testes/cobertura, SonarCloud e build da imagem. A arquitetura AWS da Fase 3 está documentada, mas sua implantação ainda está pendente.

| Disponível | A implementar |
|---|---|
| API .NET, JWT interno, PostgreSQL, SMTP e health checks | Validação serverless de CPF e permissões Admin/Mechanic/Customer |
| Dockerfile, Compose opcional e Deployment/HPA | Imagem no ECR, entrega por OIDC e ambientes hom/prd |
| CI com testes, cobertura, SonarCloud e build Docker | Aurora, API Gateway e observabilidade OpenTelemetry/New Relic |

O foco de uso será a API na AWS. A URL do Gateway ainda não foi publicada. A [arquitetura alvo](docs/arquitetura/README.md) descreve contratos futuros; as [collections](postman/collections) e o OpenAPI gerado pela API representam as operações atuais.

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
| [Infraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/tree/develop) | Terraform, plataforma Kubernetes, Service e futura entrada Gateway/NLB |
| [Banco](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/tree/develop) | SQL/seeds e futura infraestrutura Aurora/Job de inicialização |
| [Autenticação](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/tree/develop) | Futura Lambda de validação de CPF e emissão de JWT |

Os projetos `Domain.Interface` e `Service.Interface` definem contratos; `Infrastructure` implementa persistência PostgreSQL, JWT, hash de senha e SMTP; `DependencyInjection` registra os componentes. Consultas comuns de usuários não retornam senha/hash; os fluxos de credenciais usam `UserCredentials`.

## Tecnologias e pré-requisitos

- .NET SDK 10 / ASP.NET Core 10.
- PostgreSQL 16 para uso local; acesso por Npgsql/Dapper.
- Docker com engine Linux e Compose v2; smtp4dev para e-mails locais.
- Docker ativo para a suíte completa: os testes de persistência usam Testcontainers e criam seus próprios containers PostgreSQL.
- kubectl com Kustomize para renderizar os manifestos.
- GitHub Actions e SonarCloud no CI.

AWS EKS, Aurora, ECR, API Gateway e Lambda compõem o destino da Fase 3. Terraform pertence ao repositório de infraestrutura.

## Build e testes

Na raiz, com .NET 10 e Docker ativo para os testes de persistência:

```bash
dotnet restore GerenciamentoMecanicaSistema.slnx
dotnet build GerenciamentoMecanicaSistema.slnx --no-restore
dotnet test GerenciamentoMecanicaSistema.slnx --no-build --no-restore
```

Restore/build não precisam de um banco em execução. Os testes de persistência preparam suas próprias tabelas e não usam o banco do Compose.

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
| `EmailSettings__Host`, `EmailSettings__Port` | Servidor SMTP |
| `EmailSettings__Username`, `EmailSettings__Password`, `EmailSettings__UseTls` | Autenticação/TLS do SMTP |
| `EmailSettings__SenderName`, `EmailSettings__SenderEmail` | Identificação do remetente |

O Compose mapeia essas configurações a partir do `.env` e dos serviços locais. E-mails enviados ao smtp4dev ficam no painel local. Para outro destino, configure o servidor SMTP apropriado.

| Rota | Finalidade |
|---|---|
| `/health/startup` | Inicialização da API |
| `/health/ready` | Prontidão e acesso ao banco |
| `/health/live` | Processo ativo |

As probes do Deployment usam essas rotas. Não houve alteração de probes na separação dos repositórios.

## Kubernetes, CI e deploy

[deploy/kubernetes](deploy/kubernetes) contém Deployment e HPA. O Service pertence à infraestrutura. Para conferir a composição sem acessar o cluster:

```bash
kubectl kustomize deploy/kubernetes
```

O Deployment ainda referencia uma imagem da fase anterior no Docker Hub e o Secret `db-secrets`, com `CONNECTION_STRING` e `JWT_KEY`. O [exemplo de Secret](deploy/kubernetes/db-secrets.example.yaml) contém placeholders. O deploy requer plataforma/namespace, banco com esquema, Secret, imagem e Service preparados; a entrega da Fase 3 ainda não está operacional.

A [pipeline](.github/workflows/pipeline.yml) executa em PRs e pushes para `develop`/`main`, além de acionamento manual. Os jobs são `unit-tests`, `code-analysis` e `build-image`; a análise usa `SONAR_TOKEN`. O build Docker ocorre após testes/análise e gera tag `sha-<commit>` no runner. Não há push de imagem, artefato de imagem disponível para download ou deploy nesse workflow.

A entrega planejada publicará no ECR e implantará por OIDC, após provisionar dependências. Consulte a [RFC de entrega](docs/arquitetura/rfcs/002-ENTREGA.md) para a ordem entre os quatro repositórios.

## Desenvolvimento e ambientes

Crie branches de trabalho a partir da `develop` atualizada e direcione os PRs para `develop`. A promoção `develop -> main` ocorre quando a entrega estiver concluída. `develop` corresponde a **hom** e `main` a **prd** na arquitetura planejada; os ambientes poderão coexistir. Proteções, Environments e deploy automático ainda precisam ser configurados.

## Referências

- [Arquitetura, diagramas, RFCs e ADRs](docs/arquitetura/README.md).
- [Contrato de autenticação e permissões](docs/arquitetura/ACESSO_E_AUTENTICACAO.md).
- [GitHub Actions](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions).
- Demonstração final e vídeo: pendentes.
