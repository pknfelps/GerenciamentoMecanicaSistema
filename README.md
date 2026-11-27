# Sistema de Gerenciamento de Mecânica

API .NET da oficina para clientes, veículos, catálogo, estoque e ordens de serviço. A API valida JWTs e aplica as permissões de negócio. A AuthFunction emite tokens, administra contas internas e autentica clientes por documento e código. A EmailFunction concentra o transporte SMTP privado.

## Componentes e repositórios

~~~mermaid
flowchart LR
    GW["Gateway / cliente HTTP"] --> HTTP["Controllers e JWT"]
    GW -->|"Login, Users e código Customer"| AUTH["AuthFunction"]
    subgraph APP["Repositorio da API"]
        HTTP --> SERVICE["Service: casos de uso"]
        SERVICE --> DOMAIN["Domain: regras de negocio"]
        SERVICE --> PORTS["Interfaces de persistencia"]
        INFRA["Infrastructure: PostgreSQL"] -.->|implementa| PORTS
        SERVICE --> EMAIL["Adaptador Invoke"]
        DI["Program e DependencyInjection"] -.-> HTTP
        DI -.-> INFRA
        HTTP -.-> OTEL["OpenTelemetry"]
        INFRA -.-> OTEL
    end
    INFRA -->|TLS| DB[("RDS / mecanica")]
    SERVICE -->|"Sincronizar acesso via IAM / alias live"| AUTH
    AUTH -->|TLS| IDENTITY[("Mesmo RDS / mecanica_identity")]
    EMAIL -->|IAM / alias live| FN["EmailFunction privada"]
    OTEL -->|OTLP| COL["Collector da infraestrutura"]
~~~

| Repositório | Responsabilidade |
|---|---|
| Este | API/camadas/testes, Dockerfile/Compose, Deployment/HPA/ServiceAccount, OpenAPI e Auth.Contracts |
| [Infraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/tree/develop) | Bootstrap/rede/EKS/NLB/Service, Gateway, Collector Kubernetes e recursos New Relic |
| [Banco](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/tree/develop) | RDS/credenciais, SQL/seeds e Job de inicialização |
| [Serverless](https://github.com/pknfelps/GerenciamentoMecanicaServerless/tree/develop) | AuthFunction para login, contas internas e códigos Customer. EmailFunction para SMTP |

Setas contínuas representam chamadas e integrações. Setas pontilhadas representam implementação, composição ou instrumentação. Domain.Interface e Service.Interface definem contratos. Infrastructure implementa a persistência operacional e as chamadas às Lambdas. [Componentes AWS](docs/arquitetura/diagramas/COMPONENTES.md), [sequências](docs/arquitetura/README.md) e [banco/ER](docs/arquitetura/BANCO_E_MODELO.md).

## Tecnologias e build

.NET SDK 10/ASP.NET Core, PostgreSQL 16, Npgsql/Dapper, JWT HS256, OpenTelemetry, Docker/Compose, Kubernetes/Kustomize, GitHub Actions e SonarCloud. Docker com engine Linux deve estar ativo para a suíte completa PostgreSQL/Testcontainers; os testes não usam o banco do Compose e não devem ser ignorados por indisponibilidade.

```bash
dotnet restore GerenciamentoMecanicaSistema.slnx
dotnet build GerenciamentoMecanicaSistema.slnx -c Release --no-restore
dotnet test GerenciamentoMecanicaSistema.slnx -c Release --no-build --no-restore
```

Restore/build não precisam de banco ou AWS ativos. [Dockerfile](GerenciamentoMecanicaSistema/Dockerfile) usa build/publish SDK 10 e runtime ASP.NET 10, porta 8080 e usuário não root; inclui certificado público RDS. Para conferir apenas o build da imagem:

```bash
docker build --file GerenciamentoMecanicaSistema/Dockerfile --tag gerenciamento-mecanica-api:local .
```

## Execução local opcional

1. Copiar [.env.example](.env.example) para .env; definir POSTGRES_PASSWORD e JWT_KEY com pelo menos 32 caracteres. .env é ignorado pelo Git.
2. Executar `docker compose up -d db smtp`.
3. Preparar banco **vazio** com o SQL do [repositório de banco](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/README.md#execução-local). Compose não cria tabelas/seeds nem sincroniza arquivos entre repositórios.
4. Iniciar a EmailFunction no SAM local para exercitar a captura de notificações. Depois executar `docker compose up --build -d api` e conferir `docker compose ps`. Para a autenticação integrada e as alterações de clientes que sincronizam Identidade, usar os testes Serverless com configuração injetada ou o ambiente Hom reconstruído. [README Serverless](https://github.com/pknfelps/GerenciamentoMecanicaServerless#emailfunction-local)

O SMTP4dev usa a [configuração local](deploy/smtp4dev/appsettings.json): caixa em memória, no máximo 100 mensagens/sessões, relay desabilitado e conteúdo ausente do stdout. Reiniciar perde a caixa. O volume SMTP antigo deixa de ser montado, sem exclusão automática; o volume db_data permanece.

O Compose publica portas somente em loopback. O modo local usa conexão e JWT locais, Invoke da EmailFunction em `3001` e endpoint de sincronização da AuthFunction em `3002` configurado para ensaios. O ambiente AWS usa o carregamento de configuração em `hom` ou `prd`. [Compose](docker-compose.yaml)

| Serviço | URL/porta padrão |
|---|---|
| API | http://localhost:8080 |
| Swagger UI, somente Development | http://localhost:8080/swagger |
| OpenAPI, somente Development | http://localhost:8080/openapi/v1.json |
| Readiness | http://localhost:8080/health/ready |
| PostgreSQL | localhost:5432 |
| smtp4dev / SMTP | http://localhost:3000 / localhost:2525 |

As portas são ajustáveis no .env. `docker compose down` preserva volumes; `docker compose down --volumes` apaga dados locais e exige preparar o banco vazio novamente. Não usar esse fluxo para apagar dados AWS.

## Autenticação, autorização e Postman

Os dados iniciais de Identidade incluem as contas educacionais `Admin` / `Admin@123` e `Mechanic` / `Mechanic@123`, com hash PBKDF2. O Gateway encaminha `POST /Authentication` à AuthFunction. O corpo recebe `name`, `password` e `role`. Com `Accept: application/json`, o `200` retorna o JWT como string JSON. O padrão é texto puro. Credenciais incorretas retornam `401` e corpo ou perfil inválido retorna `400`.

| Identidade | Operações |
|---|---|
| Admin | Criar/consultar/remover usuários internos e PATCH da própria conta |
| Mechanic | Cadastros operacionais/OS, GET/PATCH da própria conta e consulta de status |
| Customer | JWT emitido pela Lambda; status e decisão de orçamento apenas das próprias OSs |

As operações de `/Users` usam Bearer e são autorizadas na AuthFunction. A API valida os JWTs e a titularidade das OSs. Os tokens têm assinatura HS256, UUID, nome, perfil e validade de dez minutos. A API usa `ClockSkew` zero.

PATCH /Users aceita somente name/password, pelo menos um presente; omitido preserva, nulo/vazio/desconhecido é inválido. UUID vem exclusivamente do JWT; não altera id/role. Sucesso 204, request 400, autenticação 401, perfil 403, conta ausente 404 e conflito de nome no mesmo perfil 409. DELETE /Users?id=<uuid> exige Admin, remove somente users, retorna 204/404; sem proteção de autoexclusão/último Admin. Recriar nome não transfere identidade de token anterior.

Clientes nascem Active; PATCH cadastral não altera status e não há endpoint de inativação. Cliente Inactive não obtém novo JWT e é bloqueado nas duas operações Customer mesmo com token ainda válido. OS de terceiro/ausente ou cliente necessário ausente retorna 404 genérico; orçamento exige Customer da própria OS e corpo somente approved booleano; retorna 200 Approved/Refused após commit e 422 para repetição/estado incompatível.

A API grava o cliente em `mecanica`. Após confirmar a transação, solicita à AuthFunction a atualização do acesso em `mecanica_identity` com o mesmo UUID. Se a confirmação se perder, `503 CUSTOMER_ACCESS_SYNC_PENDING` indica que a operação cadastral já foi registrada. Repetir a mesma operação para sincronizar o acesso.

Abrir a [Local View do Postman](postman/README.md) com os YAMLs existentes. RequestCustomerCode salva desafio; operador consulta SMTP4dev privado; VerifyCustomerCode salva JWT para status e orçamento. Ambientes Dev/Hom/Prd começam com sessões vazias, sem URLs AWS fixadas. Não há código em resposta pública, consulta de código pelo mecânico ou Authorizer. [Contrato completo](docs/arquitetura/ACESSO_E_AUTENTICACAO.md).

## OS, histórico e notificações

Mechanic abre a OS; diagnóstico → orçamento Customer → execução → entrega, com ramo de recusa. Estoque/itens/OS/eventos confirmam na mesma transação. Mutações bloqueiam a OS e materiais seguem ordem de UUID. Histórico sobrevive ao DELETE individual, mas não à destruição do banco; não há CHECK/índice/UNIQUE de sequência no SQL por decisão registrada.

Datas são derivadas: abertura do evento inicial e conclusão Finished/ServiceCompleted. Sem conclusão, dateFinished/duration são null; recusa não é serviço concluído e duração real zero é válida. Entrega não aumenta o tempo do serviço. [Modelo e ER](docs/arquitetura/BANCO_E_MODELO.md).

Após commit o serviço publica e aguarda o handler; ele consulta dados/monta a mensagem e trata falhas sem desfazer a OS. Todos os modos invocam EmailFunction, que transmite ao SMTP4dev sem relay externo. A API não mantém SMTP direto ou logging de simulação. Sucesso significa captura, sem entrega pessoal ao destinatário. Não há fila/outbox/reprocessamento/garantia única. [Sequência](docs/arquitetura/diagramas/ABERTURA_OS.md) e [melhoria futura](docs/arquitetura/adrs/006-NOTIFICACOES.md).

## Configuração AWS, saúde e observabilidade

| Configuração | Comportamento |
|---|---|
| ConnectionStrings__DefaultConnection | PostgreSQL local |
| Jwt__Issuer / Jwt__Audience / Jwt__Key | JWT local |
| Runtime__Environment | Ausente para execução local. Em AWS, usar `hom` ou `prd` para selecionar a configuração e os aliases do ambiente |
| AWS_REGION | us-east-1 no modo AWS |
| Email__InvocationEndpoint / Email__FunctionName | Local: SAM HTTP permitido / EmailFunction; AWS: alias via catálogo, sem endpoint override |
| CustomerAccess__InvocationEndpoint | Endpoint local de Invoke da AuthFunction. Em AWS, a API usa o alias `live` do ambiente com IAM |
| Telemetry__Enabled | Opt-in local; habilitada nos overlays hom e prd |
| OTEL_EXPORTER_OTLP_ENDPOINT | Destino OTLP absoluto HTTP/HTTPS, sem credenciais/query/fragmento |

Bootstrap lê SSM v2/AWSCURRENT de JWT e database/api, com prazo total de 45 s, identidade por Pod Identity e valores em memória. TLS VerifyFull/CA RDS; sem fallback local em modo AWS ou Secret Kubernetes JWT. Erro inicial encerra com código 1 e mensagem fixa/tipo no stderr, sem stack trace.

Probes anônimas: /health/startup, /health/ready e /health/live; não são publicadas pelo Gateway. Swagger/OpenAPI só são habilitados em Development, não em Staging/Production.

OTel usa o [manifesto](deploy/kubernetes/base/observability.json), ConfigMap estável read-only e um provider por sinal. Métricas identificam acontecimentos; configuração seleciona atributos/dimensões/coleta. Console textual é preservado, sem escopos/caminho bruto, mensagem original de exceção, stack trace, SQL, documento, token ou credenciais. Atributos técnicos e IDs de correlação são permitidos; UUID/OS não são dimensões de métricas.

W3C traceparent é o único pai OTLP, sem baggage/tracestate. HTTP/PostgreSQL/logs correlacionados; probes/SQL associado sem spans, preservando métricas. X-Ray/polling é separado. Exportação usa Collector da infraestrutura e não recebe License Key na API. [Indicadores, coleta e limites](docs/arquitetura/rfcs/003-DADOS-E-OBSERVABILIDADE.md).

## OpenAPI, pipelines e deploy

```bash
dotnet build GerenciamentoMecanicaSistema.slnx -c Release -p:OpenApiGenerateDocuments=true
```

O build gera `artifacts/openapi/openapi.json` com as operações da API em OpenAPI 3.0. O Gateway combina esse arquivo com o contrato 2.1.0 da AuthFunction, que contém `/Authentication`, `/Users` e as duas operações de código Customer.

| Workflow | Comportamento |
|---|---|
| [pipeline](.github/workflows/pipeline.yml) | PR/manual: testes, cobertura/SonarCloud e build de imagem; sem push/deploy |
| [auth-contracts](.github/workflows/auth-contracts.yml) | Verifica/empacota nupkg; publicação manual condicionada ao input publish |
| [api-deploy](.github/workflows/api-deploy.yml) | Push/merge ou manual: Publicar → Implantar, sem aprovação intermediária; Release/OpenAPI/ECR/digest/Kustomize/rollout |
| [api-destroy](.github/workflows/api-destroy.yml) | Manual: checar Gateway, remover overlay/recursos da API e conferir ausência |
| [aws-oidc-check](.github/workflows/aws-oidc-check.yml) | Diagnóstico manual de identidade/acesso, não prova completa de deploy |

O deploy em develop/hom ou main/prd verifica o Job dos dois bancos e a versão da AuthFunction no alias `live`. Publicação e deploy usam o mesmo SHA. O contrato e seu checksum são imutáveis por commit, e o deploy usa o digest da imagem. Após o rollout de até 600 segundos, o workflow confirma SHA, digest e VPC em `/api/v2/deployment`.

[Kustomize base/hom/prd](deploy/kubernetes) e [workflow de deploy](.github/workflows/api-deploy.yml). Deployment/HPA/ServiceAccount pertencem à API; Service/NLB à infraestrutura. Base local Kustomize mantém referências locais anteriores; overlays AWS removem db-secrets e usam identidade/runtime AWS. Docker Hub dessa base não é o caminho de publicação AWS.

Para reconstruir o ambiente, seguir bootstrap → base → banco/Job → Serverless (EmailFunction antes de AuthFunction) → API → Gateway. Para descarte, seguir Gateway → API e AuthFunction → banco e EmailFunction → base. [Contratos entre repositórios](docs/arquitetura/CONTRATOS_ENTRE_REPOSITORIOS.md)

O endpoint do ambiente é publicado em /mecanica/<ambiente>/gateway/v2/endpoint. [GitHub Actions](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions) · [Arquitetura/ADRs/RFCs](docs/arquitetura/README.md) · [Dashboards e alertas](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/observability/newrelic/README.md).
