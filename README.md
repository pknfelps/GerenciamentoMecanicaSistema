# Sistema de Gerenciamento de Mecânica

API .NET da oficina: usuários, clientes, veículos, catálogo, estoque e ciclo de ordens de serviço. Reúne regras de negócio, persistência PostgreSQL, autenticação interna, notificações e observabilidade. AuthFunction autentica clientes por documento e código; EmailFunction centraliza o transporte SMTP privado.

## Estado e evidências

E4/E5/E6 foram concluídas no **escopo hom acordado**, preservando limites e dispensas. Em 23/10/2026, hom foi reprovisionado e a extra AE foi concluída: autenticação por código, captura privada, autorização, SQL/limites/inativação e recuperação SMTP. [Consolidação e fontes](docs/CONSOLIDACAO_ENTREGA_2026-10-23.md). O descarte/custo de 21/10 é histórico, sem certificar ausência atual ou prd. Workflows são manuais; a diferença R06 foi adiada.

- [Revisão integrada e fontes](docs/REVISAO_E6_2026-10-21.md).
- [PRs, CI/CD e proteções](docs/EVIDENCIAS_CICD_E_PROTECOES_2026-10-21.md).
- [Observabilidade: aceite e limitações](docs/ENCERRAMENTO_E5_2026-10-21.md).
- [VersionId SAM e proteção de artefatos](docs/VALIDACAO_PROTECAO_ARTEFATOS_2026-10-21.md).
- [Descarte final, persistências e custos estimados](docs/ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md).

A documentação está consolidada; faltam vídeo, publicação, PDF e conferência/submissão. Avisos transitivos de dependências/analisadores e limites aceitos não foram corrigidos ou ocultados por esta revisão documental.

## Componentes e repositórios

```mermaid
flowchart LR
    HTTP["Controllers ASP.NET Core"] --> SERVICE["Service - casos de uso"]
    SERVICE --> DOMAIN["Domain - regras"]
    SERVICE --> PORTS["Service.Interface - contratos"]
    PORTS --> INFRA["Infrastructure - adaptadores"]
    INFRA --> DB[("PostgreSQL via Npgsql e Dapper")]
    INFRA --> EMAIL["Invoke → EmailFunction → captura SMTP4dev"]
    DI["DependencyInjection e Program"] -.-> HTTP
    DI -.-> INFRA
    HTTP --> OTEL["Observability - composição e métricas HTTP"]
```

| Repositório | Responsabilidade |
|---|---|
| Este | API/camadas/testes, Dockerfile/Compose, Deployment/HPA/ServiceAccount, OpenAPI e Auth.Contracts |
| [Infraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/tree/develop) | Bootstrap/rede/EKS/NLB/Service, Gateway, Collector Kubernetes e recursos New Relic |
| [Banco](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/tree/develop) | RDS/credenciais, SQL/seeds e Job de inicialização |
| [Serverless](https://github.com/pknfelps/GerenciamentoMecanicaServerless/tree/develop) | AuthFunction/documento+código/JWT e EmailFunction/transporte SMTP privado |

Domain.Interface/Service.Interface definem contratos. Infrastructure implementa PostgreSQL, JWT/hash e envio; DependencyInjection compõe os serviços. Consultas públicas de usuários não retornam senha/hash. [Componentes AWS](docs/arquitetura/diagramas/COMPONENTES.md), [sequências](docs/arquitetura/README.md) e [banco/ER](docs/arquitetura/BANCO_E_MODELO.md).

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
3. Preparar banco **vazio** com o SQL do [repositório de banco](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/README.md#execução-local-opcional). Compose não cria tabelas/seeds nem sincroniza arquivos entre repositórios.
4. Iniciar a EmailFunction no SAM local conforme o [procedimento AE.7](docs/AE7_API_ENVIO_E_ACESSO_CUSTOMER.md), usando a rede do Compose e listener acessível ao container. Depois executar `docker compose up --build -d api`; conferir `docker compose ps` e `docker compose logs -f api`.

O SMTP4dev usa a [configuração local](deploy/smtp4dev/appsettings.json): caixa em memória, no máximo 100 mensagens/sessões, relay desabilitado e conteúdo ausente do stdout. Reiniciar perde a caixa. O volume SMTP antigo deixa de ser montado, sem exclusão automática; o volume db_data permanece.

O [Compose](docker-compose.yaml) publica portas somente em loopback. Sem Runtime__Environment, usa conexão/JWT locais e Invoke no SAM/EmailFunction para captura SMTP4dev. Definir runtime hom/prd ativa o carregamento AWS e exige alias email/v1 desse ambiente, sem override de endpoint ou fallback de simulação. Não cria modo local alternativo de AWS.

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

Seeds educacionais: Admin/Admin@123 e Mechanic/Mechanic@123, com hash PBKDF2. Login `POST /Authentication` recebe name/password/role e retorna JWT como string JSON. Perfis internos aceitos: Admin/Mechanic; credenciais incorretas retornam 401 e request/perfil inválido 400. Usar `Authorization: Bearer <token>` nas rotas protegidas.

| Identidade | Operações |
|---|---|
| Admin | Criar/consultar/remover usuários internos e PATCH da própria conta |
| Mechanic | Cadastros operacionais/OS, GET/PATCH da própria conta e consulta de status |
| Customer | JWT emitido pela Lambda; status e decisão de orçamento apenas das próprias OSs |

Admin não herda acesso operacional. Customer não é linha de users. JWT contém Name/Role/NameIdentifier UUID, HS256 e expiração UTC + dez minutos; a validação usa ClockSkew zero. Não há refresh/revogação por atualização, remoção ou inativação; operações próprias ainda dependem do cadastro/vínculo.

PATCH /Users aceita somente name/password, pelo menos um presente; omitido preserva, nulo/vazio/desconhecido é inválido. UUID vem exclusivamente do JWT; não altera id/role. Sucesso 204, request 400, autenticação 401, perfil 403, conta ausente 404 e conflito de nome no mesmo perfil 409. DELETE /Users?id=<uuid> exige Admin, remove somente users, retorna 204/404; sem proteção de autoexclusão/último Admin. Recriar nome não transfere identidade de token anterior.

Clientes nascem Active; PATCH cadastral não altera status e não há endpoint de inativação. Cliente Inactive não obtém novo JWT e é bloqueado nas duas operações Customer mesmo com token ainda válido. OS de terceiro/ausente ou cliente necessário ausente retorna 404 genérico; orçamento exige Customer da própria OS e corpo somente approved booleano; retorna 200 Approved/Refused após commit e 422 para repetição/estado incompatível.

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
| Runtime__Environment | Ausente: local/SAM; hom/prd: bootstrap AWS/alias email; vazio/inválido rejeitado |
| AWS_REGION | us-east-1 no modo AWS |
| Email__InvocationEndpoint / Email__FunctionName | Local: SAM HTTP permitido / EmailFunction; AWS: alias via catálogo, sem endpoint override |
| Telemetry__Enabled | Opt-in local; hom true e prd false nos overlays atuais |
| OTEL_EXPORTER_OTLP_ENDPOINT | Destino OTLP absoluto HTTP/HTTPS, sem credenciais/query/fragmento |

Bootstrap lê SSM v2/AWSCURRENT de JWT e database/api, com prazo total de 45 s, identidade por Pod Identity e valores em memória. TLS VerifyFull/CA RDS; sem fallback local em modo AWS ou Secret Kubernetes JWT. Erro inicial encerra com código 1 e mensagem fixa/tipo no stderr, sem stack trace. [Inicialização](docs/INICIALIZACAO_AWS.md).

Probes anônimas: /health/startup, /health/ready e /health/live; não são publicadas pelo Gateway. Swagger/OpenAPI só são habilitados em Development, não em Staging/Production.

OTel usa o [manifesto](deploy/kubernetes/base/observability.json), ConfigMap estável read-only e um provider por sinal. Métricas identificam acontecimentos; configuração seleciona atributos/dimensões/coleta. Console textual é preservado, sem escopos/caminho bruto, mensagem original de exceção, stack trace, SQL, documento, token ou credenciais. Atributos técnicos e IDs de correlação são permitidos; UUID/OS não são dimensões de métricas.

W3C traceparent é o único pai OTLP, sem baggage/tracestate. HTTP/PostgreSQL/logs correlacionados; probes/SQL associado sem spans, preservando métricas. X-Ray/polling é separado. Exportação usa Collector da infraestrutura e não recebe License Key na API. [Procedimento local](docs/OBSERVABILIDADE_LOCAL.md), [indicadores](docs/OBSERVABILIDADE_NEGOCIO.md), [integrações](docs/METRICAS_INTEGRACOES.md), [tracing](docs/TRACING_W3C_E_XRAY.md) e [aceite/limites E5](docs/ENCERRAMENTO_E5_2026-10-21.md).

## OpenAPI, pipelines e deploy

```bash
dotnet build GerenciamentoMecanicaSistema.slnx -c Release -p:OpenApiGenerateDocuments=true
```

Gera artifacts/openapi/openapi.json, OpenAPI 3.0, sem AWS/JWT/persistência. Build comum/Docker não gera. Bearer por operação reflete Authorize/AllowAnonymous. A API não implementa as duas rotas /customers/auth/*, atendidas pela AuthFunction; customers/validate foi retirado do contrato novo.

| Workflow | Comportamento |
|---|---|
| [pipeline](.github/workflows/pipeline.yml) | PR/manual: testes, cobertura/SonarCloud e build de imagem; sem push/deploy |
| [auth-contracts](.github/workflows/auth-contracts.yml) | Verifica/empacota nupkg; publicação manual condicionada ao input publish |
| [api-deploy](.github/workflows/api-deploy.yml) | Manual: Publicar → Implantar, sem aprovação intermediária; Release/OpenAPI/ECR/digest/Kustomize/rollout |
| [api-destroy](.github/workflows/api-destroy.yml) | Manual: checar Gateway, remover overlay/recursos da API e conferir ausência |
| [aws-oidc-check](.github/workflows/aws-oidc-check.yml) | Diagnóstico manual de identidade/acesso, não prova completa de deploy |

Deploy exige develop/hom ou main/prd, OIDC/Environments, bootstrap/base/Collector/banco/Job e email/v1 disponíveis e License Key cadastrada pelo mantenedor. Mesma revisão nos dois jobs; tag sha-commit-run-attempt e deploy por digest. Contrato/checksum S3 são imutáveis por commit. Não publica /api/v2/ nem usa estado de outro repositório. Aguarda rollout por 600 s; diagnóstico sem rollback automático próprio.

[Kustomize base/hom/prd](deploy/kubernetes) e [procedimento completo](docs/DEPLOY_API.md). Deployment/HPA/ServiceAccount pertencem à API; Service/NLB à infraestrutura. Base local Kustomize mantém referências locais anteriores; overlays AWS removem db-secrets e usam identidade/runtime AWS. Docker Hub dessa base não é o caminho de publicação AWS.

Provisionar: bootstrap → base/Collector/SMTP4dev → banco/Job e email → API/auth → Gateway. Descartar: Gateway → API/auth → banco/email → base, com [bloqueios de dependência](docs/DESTRUICAO_AMBIENTE.md). ECR/artefatos/estados e recursos New Relic persistem. Sem deploy automático por push/merge ou aprovação implícita de operações futuras.

O endpoint ativo é obtido de /mecanica/<ambiente>/gateway/v2/endpoint após provisionamento. Não usar URLs de rodadas destruídas. [GitHub Actions](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions) · [Arquitetura/ADRs/RFCs](docs/arquitetura/README.md) · [Operação New Relic](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/NEW_RELIC.md). Vídeo/PDF ainda pertencem à próxima parte da E7. Por decisão do mantenedor, o acesso ocorre pelos quatro repositórios públicos, sem convite individual de avaliadores; visibilidade confirmada no GitHub em 21/10/2026.


## Autenticação por código e envio centralizado — concluídos em hom

**23/10/2026:** AE.1–AE.10 concluídas nos respectivos escopos. Cliente solicita e confirma documento/desafio/código; AuthFunction emite JWT Customer por UUID. API exige Active/titularidade, permite status antes do orçamento e retorna 200/Approved ou Refused na decisão. Código/JWT nunca são devolvidos no orçamento nem nos logs.

API/auth invocam EmailFunction por IAM; somente ela implementa MIME/SMTP. SMTP4dev privado no EKS captura mensagens sem relay externo e perde a caixa ao recriar o pod. O operador consulta a interface por port-forward; não há login de cliente por senha, Authorizer, token por OS ou endpoint de debug do mecânico.

[Fluxo AWS e correções](docs/AE10_VALIDACAO_HOM_2026-10-23.md) · [SQL, inativação e limites](docs/AE10_CONFERENCIAS_FINAIS_HOM_2026-10-23.md) · [Plano extra](docs/PLANO_EXTRA_AUTENTICACAO_E_EMAIL.md) · [Ensaio local AE.9](docs/AE9_VALIDACAO_LOCAL_E_DOCUMENTACAO.md).

A regressão da correção final aprovou 1.213 testes API, sem falhas/ignorados, e preservou o OpenAPI. Na AWS: auth:14/email:1, Approved/Refused até Delivered, isolamento entre clientes, falha/recuperação SMTP, consumo/tentativas/grants/limpeza SQL, limite horário e Inactive antes/depois do JWT conferidos. Location de criação foi corrigido e reconferido pelo Gateway. Não é comprovação de prd, e-mail externo ou capacidade sob carga.

O transporte serverless já está implementado; processamento assíncrono independente de notificações de OS permanece evolução futura. Sem fila/outbox, retry próprio ou garantia única. [ADR 006](docs/arquitetura/adrs/006-NOTIFICACOES.md) · [ADR 011](docs/arquitetura/adrs/011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md).

Novas operações AWS e a publicação dos documentos dependem do mantenedor; esta consolidação não executou workflows, deploys ou destroys.
