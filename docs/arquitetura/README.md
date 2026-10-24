# Arquitetura do sistema

Visão dos componentes, contratos, modelo de dados e decisões arquiteturais da oficina.

[README da API](../../README.md) · [Postman](../../postman/README.md)

## Visão vigente

- Entrada REST Regional → VPC Link V2 → NLB interno Terraform → NodePort → API .NET no EKS. POST request-code/verify-code usa AuthFunction .NET 10/SAM e versão numérica.
- Um nó t3.small, HPA 1–3 pods, NAT único e RDS PostgreSQL 16.15/db.t3.micro Single-AZ privado com TLS VerifyFull. Sem Aurora/auto-pause, Load Balancer Controller ou escalador de nós.
- Admin/Mechanic internos; Customer emitido após documento e código de uso único. JWT HS256/UUID; autorização e vínculo na API, sem authorizer JWT no Gateway.
- SQL 1.0.0/Job em banco vazio; histórico transacional preservado após DELETE de OS, não após destruição do banco.
- EmailFunction privada concentra MIME/SMTP em todos os modos; API/auth invocam por SDK/IAM. SMTP4dev efêmero e privado, sem relay/entrega externa. Handler é aguardado após commit; sem fila/outbox.
- OTel API/Lambda → Collectors → New Relic. Kubernetes/CloudWatch no Collector EKS; X-Ray por integração nativa separada. Somente W3C seleciona pai OTLP.
- Três dashboards e cinco alertas como código, persistentes; New Relic administrado por workflow manual independente do descarte AWS.
- SSM base/database/auth/gateway v2 e email/v1 configuram consumidores, sem protocolo de releases/prontidão. Base/banco/Gateway usam plano salvo/aprovação/apply; API/auth/email são manuais sem aprovação intermediária. Não há deploy automático após merge.

## Documentos

| Documento | Fonte/objetivo |
|---|---|
| [Componentes](diagramas/COMPONENTES.md) | Rede, proprietários, identidades e caminhos de observabilidade |
| [Validação CPF/CNPJ](diagramas/VALIDACAO_CPF.md) | Parsing, bootstrap reutilizável, JWT e autorização das próprias OSs |
| [Abertura de OS](diagramas/ABERTURA_OS.md) | Login, transação, notificação aguardada e métricas após retorno |
| [ER completo](diagramas/DADOS.md) | Doze tabelas, chaves físicas e correlação histórica sem FK |
| [Banco e modelo](BANCO_E_MODELO.md) | Justificativa PostgreSQL/RDS, alternativas, alterações, relações e limites |
| [Contratos HTTP/JWT/Invoke](CONTRATOS_AUTENTICACAO_E_EMAIL.md) | Regras de autenticação e transporte interno |
| [Acesso/autenticação](ACESSO_E_AUTENTICACAO.md) | Matriz HTTP, códigos, claims e sessões |
| [Contratos operacionais](CONTRATOS_ENTRE_REPOSITORIOS.md) | Catálogo SSM v2 real, artefatos, Secrets e ordem entre repositórios |
| [Auth.Contracts](PACOTE_AUTH_CONTRACTS.md) | Produção/consumo do nupkg exato, checksum e compatibilidade |
| [RFC 001](rfcs/001-EXECUCAO.md) | Execução integrada |
| [RFC 002](rfcs/002-ENTREGA.md) | Pipelines, integridade e ciclo de vida |
| [RFC 003](rfcs/003-DADOS-E-OBSERVABILIDADE.md) | Indicadores, coleta, alertas e limites |

## Decisões e histórico

| Decisão | Registro vigente |
|---|---|
| Identidade/permissões | [ADR 001](adrs/001-IDENTIDADE.md) |
| Entrada/rede privada | [ADR 002](adrs/002-REDE-AWS.md) e [ADR 010](adrs/010-GATEWAY-REST-V2.md) |
| Histórico e banco descartável | [ADR 003](adrs/003-BANCO-E-HISTORICO.md), com Aurora substituída pelo [ADR 007](adrs/007-RDS-FREE-PLAN.md) |
| Repositórios/ambientes | [ADR 004](adrs/004-ENTREGA-E-AMBIENTES.md) |
| Observabilidade | [ADR 005](adrs/005-OBSERVABILIDADE.md) |
| Conteúdo e transporte das mensagens | [ADR 006](adrs/006-NOTIFICACOES.md) |
| Infraestrutura declarativa | [ADR 008](adrs/008-INFRAESTRUTURA-DECLARATIVA.md) |
| Código de acesso e envio central | [ADR 011](adrs/011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md) |
| Auth SAM e ZIP versionado | [ADR 009](adrs/009-SAM-AUTENTICACAO.md) |

Os ADRs mantêm registros anteriores em seções de histórico explicitamente sem vigência operacional. Menções históricas a Aurora, Service LoadBalancer/controller, SSM v1/releases, precedência X-Ray ou publicação ainda pendente não descrevem a configuração final. O texto vigente vem antes desse histórico.

## Limites

Topologia de um nó/NAT único/RDS Single-AZ, SMTP4dev efêmero sem entrega externa e telemetria sem garantia única. Workflows de implantação são manuais; preparação hom/prd não certifica operação em produção. X-Ray e OTLP permanecem separados.
