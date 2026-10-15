# Sistema de Gerenciamento de Mecânica

API do sistema de oficina: usuários, clientes, veículos, catálogo, estoque e ciclo de ordens de serviço (OS). Mantém as regras de negócio, persistência, autenticação interna, notificações SMTP locais e simuladas na AWS, testes, imagem Docker e Deployment/HPA.

## Estado da implementação

A separação em quatro repositórios está integrada. O CI valida testes/cobertura, SonarCloud e build da imagem. Base/banco/API, autenticação Lambda, Gateway e compatibilidade JWT foram validados em hom, incluindo o ciclo de OS e expiração real. A E4 acrescenta status consultável de clientes, perfis internos Admin/Mechanic, ID estável no JWT e autorização por perfil/vínculo com a OS. Esta entrega foi validada localmente; PR/deploy e conferência pelo Gateway serão conduzidos pelo mantenedor.

| Disponível | A implementar |
|---|---|
| API .NET, JWT interno, PostgreSQL, SMTP local, envio simulado AWS e health checks; Inativo/histórico SQL conferidos em hom | Tratamento dos avisos transitivos NU1902/NU1903 |
| Dockerfile, Compose opcional, Deployment/HPA e workflow manual ECR/EKS | Executar e validar deploy nos ambientes hom/prd |
| CI, autenticação serverless e API Gateway validados em hom; OpenTelemetry/Collector comprovados localmente | Validação prd, Collector EKS e ingestão New Relic |

O foco de uso é a API na AWS. O endpoint do ambiente é publicado em SSM `/mecanica/<ambiente>/gateway/v2/endpoint` e pode mudar após recriação. A [especificação de acesso](docs/arquitetura/ACESSO_E_AUTENTICACAO.md) distingue as regras alvo das permissões ainda pendentes; as [collections](postman/collections) e o OpenAPI gerado representam as operações atuais.

### Identidade, status e autorização — E4

- `POST /Authentication` mantém `name/password/role` e retorna o token no formato existente. Cadastro e login internos aceitam somente os nomes exatos `Admin` e `Mechanic`; perfis antigos, numéricos ou `Customer` retornam 400. Credenciais incorretas ou usuário inexistente retornam 401.
- O JWT interno contém Name, Role e NameIdentifier (UUID lido da tabela users). O consumidor não escolhe o ID. HS256, expiração de dez minutos, issuer/audience e tolerância de relógio permanecem; não há revogação ou nova exigência global de ID para tokens anteriores.
- `GET /Customers` acrescenta `status` como string `Active` ou `Inactive`. Novos clientes são Active; o PATCH preserva o estado existente. Status não faz parte dos requests: campos extras não permitem criar Inactive ou reativá-lo. DELETE continua excluindo o cadastro.
- O status persistido é convertido explicitamente, sem fallback para Active. Os mapeadores constroem o domínio diretamente, sem try/catch; validações de identidade/perfil do usuário propagam DomainValidationException (400 no middleware atual). Não há conversão de perfis antigos. O SQL do banco já contém os campos/seeds necessários.
- Mechanic opera clientes, veículos, catálogo, estoque e OS; Admin não herda essas permissões. POST Users exige Admin. GET Users permite consulta administrativa por name/role para Admin e consulta exclusivamente própria por UUID para Mechanic. Filtros próprios divergentes retornam 403; cadastro próprio ausente retorna 404. Senha/hash não são retornados.
- Status de OS exige Mechanic ou Customer; orçamento exige Customer. O cliente é resolvido pelo UUID do token, e a OS é consultada com seu documento cadastrado antes de qualquer alteração de ordem, histórico, estoque ou evento. OS ausente, de outro cliente ou cliente ausente produzem o mesmo 404 genérico. Documento divergente na OS própria retorna 422; documento inválido retorna 400.
- GET próprio de Mechanic, PATCH próprio de Admin/Mechanic e operações de Customer exigem exatamente um NameIdentifier UUID válido e não vazio, retornando 401 antes do serviço se inválido. Outras rotas não exigem ID globalmente. Um token emitido antes da inativação continua válido até expirar; essas operações não revalidam Active. Login interno e health checks continuam públicos. DELETE Users está implementado localmente; operação de inativação permanece pendente.

### Atualização da própria conta

`PATCH /Users` aceita Admin/Mechanic com exatamente um UUID válido no JWT. O corpo JSON contém somente `name` e/ou `password`; pelo menos um campo é obrigatório. Omitir preserva o valor atual; nulo/vazio explícito, campos desconhecidos (incluindo id/role) e valores inválidos retornam 400. O nome segue a validação atual e tem no máximo 100 caracteres; senha segue as regras do cadastro. ID e perfil são preservados. A atualização usa UUID, mesmo que o nome do token esteja desatualizado.

Sucesso retorna 204 sem corpo. Token/identidade inválidos retornam 401, perfil sem permissão 403, conta inexistente 404 e nome em conflito com outro usuário do mesmo perfil 409. A senha é gravada como hash; omitir password preserva o hash existente. Tokens já emitidos continuam válidos até expirar; o próximo login usa os dados novos. Reutilizar o nome antigo em outra conta não transfere o token anterior.

Os exemplos Postman `UpdateOwnUserAdmin`/`UpdateOwnUserMechanic` usam os tokens correspondentes, inicialmente vazios. Esta entrega foi preparada para validação local; PR/deploy/conferência AWS ficam com o mantenedor. O DELETE está descrito a seguir.

### Exclusão de usuários internos

`DELETE /Users?id=<uuid>` exige Admin e um UUID não vazio na query, sem corpo. Exclui somente o cadastro users e retorna 204 sem conteúdo; repetir a exclusão retorna 404. Token inválido/ausente/expirado retorna 401, perfil sem permissão 403 e ID ausente/malformado/vazio 400. NameIdentifier do Admin não é exigido nesta rota. Não há bloqueio de autoexclusão ou exclusão do último Admin.

Após excluir, o usuário não pode fazer novo login, mas JWTs anteriores continuam válidos até expirar nas rotas permitidas pelo perfil. GET próprio de Mechanic e PATCH próprio retornam 404 porque o cadastro não existe. Recriar uma conta com o mesmo nome não transfere a identidade do token antigo. O exemplo Postman `DeleteUser` usa admin_token e user_id inicialmente vazios; preencher user_id com o UUID alvo retornado pela consulta. Validação desta entrega é local; deploy/conferência AWS permanecem sob condução do mantenedor.

### Histórico e consistência das OSs

Conferência SQL independente em hom em 14/10/2026: 44 eventos de doze OSs, com sequência, encadeamento, UTC, motivos de conclusão/recusa e preservação após exclusão aprovados. Cliente fictício Inactive não obteve novo JWT; o token anterior continuou consultando/decidindo somente suas OSs enquanto válido. PATCH cadastral preservou Inactive. O acesso ocorreu por Pod psql temporário com TLS, removido ao final, sem novo deploy ou alteração de esquema/IAM. Procedimento no [repositório do banco](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados); prd permanece fora dessa conferência.

As alterações de estado, itens e exclusão bloqueiam a OS dentro da transação antes de carregar e validar seu estado atual. Estoque usa bloqueio por material, compartilhando a transação da OS; operações com vários materiais seguem a mesma ordem de UUIDs. Uma disputa de transição não repete reserva, devolução, consumo ou notificação. A exclusão é atômica em todos os estados e preserva o histórico.

`dateCreated` vem do evento inicial; `dateFinished` vem de `Finished/ServiceCompleted`. Sem conclusão do serviço, `dateFinished` e `duration` são **null**, inclusive na recusa. Uma duração efetivamente igual a zero continua sendo zero. A entrega preserva a conclusão e a duração do atendimento. O domínio normaliza instantes locais para UTC e rejeita instantes sem fuso, extremos ou conclusão anterior à abertura.

A lista detalhada ordena por abertura crescente e UUID; a operacional mantém prioridade de status, depois abertura e UUID. O [diagrama de dados](docs/arquitetura/diagramas/DADOS.md) registra as relações e a preservação do histórico. Os ajustes não alteram o esquema/seeds nem reintroduzem constraints ou índices. A conferência desta entrega é local; publicação e validação AWS dependem dos PRs/deploys do mantenedor.

### Logs da API

Os erros da aplicação registram uma mensagem fixa da operação e o tipo da exceção, sem stack trace, mensagem original, exceção interna ou `Data`. A política é a mesma em desenvolvimento e produção, inclusive para logs Debug da aplicação. Não registrar documentos, placa, nomes, e-mail, assunto/corpo das notificações, orçamento, tokens, senhas, hashes, strings de conexão ou configuração AWS/SMTP.

Os registros preservam dados técnicos: ID/estado da OS e tipo do evento, quando aplicável; no middleware, código HTTP, RequestId do servidor, TraceId da atividade atual, verbo reconhecido e modelo da rota. Caminho e query recebidos não são copiados. Sem endpoint reconhecido, usar `unmatched`; verbos não previstos usam `OTHER`; sem atividade, o TraceId é `unavailable`. Esses campos ficam no próprio evento, sem criar cabeçalhos ou novos logs por requisição.

O console mantém texto simples. `CaptureScopes=false` e `IncludeScopes=false` impedem a coleta/exibição de escopos automáticos que poderiam conter o caminho bruto. Os filtros de mensagens permanecem os originais: Information para a aplicação e Warning para Microsoft.AspNetCore. Não foi desabilitada categoria nem suprimida regra do analisador. O aviso S6667, que pede o objeto da exceção no logger, permanece visível para revisão; esse objeto não é registrado porque pode expor dados sensíveis e stack trace.

As respostas HTTP e o funcionamento das transações não mudam. Em 14/10/2026, a revisão `423ac1c` foi conferida em hom: três erros controlados mantiveram mensagem fixa, tipo e correlação, sem dados fictícios sensíveis, tokens ou stack trace nos logs do novo pod. A evidência cobre os cenários exercitados; falhas técnicas deliberadas e prd não foram testados. O [procedimento de deploy](docs/DEPLOY_API.md) descreve a conferência. Os registros antigos não são apagados. O SMTP funcional fica restrito ao modo local; hom/prd usam a simulação abaixo. A observabilidade E5 permanece separada.

### Observabilidade local — E5

A API pode exportar traces HTTP/PostgreSQL, métricas técnicas nativas do .NET 10 e logs da aplicação correlacionados por OpenTelemetry. A telemetria permanece **desabilitada por padrão**: `Telemetry__Enabled=true` exige `OTEL_EXPORTER_OTLP_ENDPOINT` HTTP/HTTPS absoluto, sem credenciais, query ou fragmento. `Telemetry__OtlpEndpoint` continua aceito como alternativa legada; a variável padrão tem precedência. Configuração inválida impede a inicialização com mensagem fixa; desabilitada, não cria providers nem exporta. A geração oficial do OpenAPI ignora essas configurações.

As regras e os ajustes ficam em [deploy/kubernetes/base/observability.json](deploy/kubernetes/base/observability.json), fonte do ConfigMap `mecanica-api-observability` pelo Kustomize e do arquivo copiado para execução local/publish. Inclui probes, métodos, atributos permitidos, dimensões, mensagens fixas, seleção de métricas, filtros de logs e parâmetros `OTEL_*`. Não há cópia dessas listas no C#. O arquivo é montado somente para leitura; alterações são consumidas na inicialização e podem exigir rollout restart quando não há troca de imagem.

`OTEL_SERVICE_NAME` e `OTEL_RESOURCE_ATTRIBUTES` definem a identidade por configuração/manifesto. Os padrões são `mecanica-api`, versão pelo commit do assembly (ou `local`) e ambiente do runtime (ou `local`); atributos explícitos podem substituir versão/ambiente. Os overlays hom/prd declaram serviço/ambiente, mantendo a telemetria desabilitada. Configurações da aplicação/ambiente podem substituir os valores do arquivo. O SDK registra a exportação dos três sinais com uma única chamada.

O código registra a instrumentação e aplica as regras antes de exportar/agregar: traces com verbo/modelo de rota, sem caminho/query, SQL, parâmetros, identificadores da conexão ou conteúdo original de exceções. Probes continuam produzindo respostas/métricas, sem spans HTTP/PostgreSQL. Logs preservam console textual e mensagens fixas; OTLP inclui TraceId/SpanId. Escopos continuam bloqueados em código; filtros de categorias e seleção das métricas usam configuração nativa do .NET. Métricas não contêm identidades e representam o processo, sem substituir métricas de nós/pods.

O [procedimento local](docs/OBSERVABILIDADE_LOCAL.md) usa um Compose separado com Collector Contrib `0.161.0`, porta OTLP somente em localhost e saída debug, sem New Relic ou credenciais. Falha do Collector não altera respostas/readiness. A instrumentação/exportação foi comprovada localmente; E5.1–E5.3 permanecem parciais até implantação/ingestão externa. Manifestos incluem identidade e ConfigMap público de observabilidade; workflows e recursos AWS não foram alterados.

### Notificações por ambiente

| Configuração de runtime | Envio de e-mail |
|---|---|
| `Runtime__Environment` ausente | `MailKitEmailSender`, com SMTP e `EmailSettings` existentes |
| `Runtime__Environment=hom` ou `prd` | `LoggingEmailSender`: uma mensagem Information, `Envio de e-mail simulado.`, por chamada |
| Campo presente vazio, nulo ou inválido | Inicialização rejeitada pelo carregador existente |

A escolha reutiliza o resultado do carregamento AWS validado no início da API; não há nova variável nem dependência do `ASPNETCORE_ENVIRONMENT` ou da máquina. Executar localmente com Runtime hom/prd também usa o modo AWS e a simulação. Nesse modo não são lidos os ajustes de e-mail nem registrados cliente/conexão SMTP. Não há fallback para SMTP nem entrega real ao destinatário. O log não inclui nomes, destinatário, assunto ou conteúdo.

`IEmailSender`, `EmailService`, eventos e handler permanecem: cliente/veículo continuam sendo consultados e a mensagem é montada antes de chegar ao adaptador. O SMTP e o smtp4dev do Compose continuam disponíveis no modo local; MailKit permanece na mesma imagem. Por decisão do mantenedor, não será configurado envio funcional na AWS. Em hom, em 14/10/2026, os ciclos de aprovação/entrega e recusa produziram nove logs Information de simulação, sem conexão/falha SMTP ou dados do envio. Essa entrega não exigiu reaplicar base, banco ou Gateway.

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
    INFRA --> EMAIL["Envio de e-mail"]
    EMAIL --> SMTP["SMTP local"]
    EMAIL --> SIM["Log simulado hom/prd"]
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

O documento servido em Development também usa OpenAPI 3.0. Bearer JWT é exigido somente nas operações protegidas por `Authorize`; `AllowAnonymous` prevalece. Autenticação interna e health checks permanecem públicos; status e orçamento agora exigem Bearer e descrevem as respostas 401/403/404/422 aplicáveis; `/customers/validate` pertence exclusivamente à autenticação.

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

Após aplicar os seeds, os usuários educacionais são `Admin` / `Admin@123` (Admin) e `Mechanic` / `Mechanic@123` (Mechanic). A senha é armazenada como hash PBKDF2. Esses dados são somente para demonstração.

O login atual é `POST /authentication`:

```json
{
  "name": "Admin",
  "password": "Admin@123",
  "role": "Admin"
}
```

Use o token retornado como `Authorization: Bearer <token>`. Importe as [collections](postman/collections) e o [ambiente Postman](postman/environments/Dev.environment.yaml); configure `base_url` e `gateway_url` (incluindo o stage). Os logins Admin/Mechanic preenchem `admin_token`/`mechanic_token`; a chamada `ValidateCustomerDocument` ao Gateway preenche `customer_token`. As três variáveis começam vazias. Operações da oficina usam Mechanic, administração usa Admin e status/orçamento usam Customer; `GetOwnUser` consulta a conta do Mechanic sem filtros.

A API cobre usuários, clientes, veículos, catálogo, materiais/estoque e criação, diagnóstico, orçamento, execução e entrega de OS. A rota `POST /customers/validate` é atendida exclusivamente pela Lambda via Gateway. As regras de usuários seguem o [contrato de acesso](docs/arquitetura/ACESSO_E_AUTENTICACAO.md).

## Configuração e saúde

| Configuração | Uso |
|---|---|
| `ConnectionStrings__DefaultConnection` | Conexão PostgreSQL |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key` | Emissão e validação JWT compatíveis no ambiente |
| `Runtime__Environment` | Ausente: configuração local e SMTP. `hom` ou `prd`: carregar JWT/conexão RDS pela AWS e simular envio de e-mail |
| `AWS_REGION` | Região dos clientes AWS quando o carregamento remoto estiver habilitado (`us-east-1` nos ambientes previstos) |
| `EmailSettings__Host`, `EmailSettings__Port` | Servidor SMTP, somente no modo local |
| `EmailSettings__Username`, `EmailSettings__Password`, `EmailSettings__UseTls` | Autenticação/TLS do SMTP local |
| `EmailSettings__SenderName`, `EmailSettings__SenderEmail` | Identificação do remetente local |

O Compose mapeia essas configurações a partir do `.env` e dos serviços locais. E-mails enviados ao smtp4dev ficam no painel local. Para outro destino local, configure o servidor SMTP apropriado. Hom/prd não usam essas configurações e não entregam e-mails.

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
| Publica em SSM | /mecanica/<ambiente>/api/v2/: configuração administrada pelo Terraform, sem releases/tentativas; sem parâmetro SMTP nesta entrega |
| Consome da base | Cluster/namespace/Service, ECR, referência JWT, issuer/audience e endpoint OTel quando habilitado |
| Consome do banco | Writer/porta/database/TLS, versão/hash SQL e api-secret-arn |
| Resolve no runtime | Sua credencial de banco e JWT; nunca a credencial administrativa do banco. SMTP somente por configuração local |
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
