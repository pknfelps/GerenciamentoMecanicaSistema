# Arquitetura implementada — Fase 3

Revisão E7 de 21/10/2026, baseada no código/configuração dos quatro repositórios e nas evidências da E4/E5/E6. O escopo comprovado é **hom**. A configuração de prd existe, mas prd/coexistência não foram validados. O ambiente hom está **destruído**; os desenhos representam a implantação validada, não disponibilidade atual.

[README da API](../../README.md) · [Revisão E6](../REVISAO_E6_2026-10-21.md) · [Encerramento e custos](../ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md)

## Visão vigente

- Entrada REST Regional → VPC Link V2 → NLB interno Terraform → NodePort → API .NET no EKS. POST /customers/validate usa Lambda .NET 10/SAM e versão numérica.
- Um nó t3.small, HPA 1–3 pods, NAT único e RDS PostgreSQL 16.15/db.t3.micro Single-AZ privado com TLS VerifyFull. Sem Aurora/auto-pause, Load Balancer Controller ou escalador de nós.
- Admin/Mechanic internos; Customer emitido do cadastro pelo documento. JWT HS256/UUID; autorização e vínculo na API, sem authorizer JWT no Gateway.
- SQL 1.0.0/Job em banco vazio; histórico transacional preservado após DELETE de OS, não após destruição do banco.
- SMTP local; hom/prd simulam envio e registram mensagem fixa. Handler é aguardado após commit; não há fila ou função de notificações.
- OTel API/Lambda → Collectors → New Relic. Kubernetes/CloudWatch no Collector EKS; X-Ray por integração nativa separada. Somente W3C seleciona pai OTLP.
- Três dashboards e cinco alertas como código, persistentes; New Relic administrado por workflow manual independente do descarte AWS.
- SSM v2 configura consumidores, sem protocolo de releases/prontidão. Base/banco/Gateway usam plano salvo/aprovação/apply; API/auth são manuais sem aprovação intermediária. Não há deploy automático após merge.

## Documentos

| Documento | Fonte/objetivo |
|---|---|
| [Componentes](diagramas/COMPONENTES.md) | Rede, proprietários, identidades e caminhos de observabilidade |
| [Validação CPF/CNPJ](diagramas/VALIDACAO_CPF.md) | Parsing, bootstrap reutilizável, JWT e autorização das próprias OSs |
| [Abertura de OS](diagramas/ABERTURA_OS.md) | Login, transação, notificação aguardada e métricas após retorno |
| [ER completo](diagramas/DADOS.md) | Dez tabelas, chaves físicas e correlação histórica sem FK |
| [Banco e modelo](BANCO_E_MODELO.md) | Justificativa PostgreSQL/RDS, alternativas, alterações, relações e limites |
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
| Notificações e melhoria futura | [ADR 006](adrs/006-NOTIFICACOES.md) |
| Infraestrutura declarativa | [ADR 008](adrs/008-INFRAESTRUTURA-DECLARATIVA.md) |
| Auth SAM e ZIP versionado | [ADR 009](adrs/009-SAM-AUTENTICACAO.md) |

Os ADRs mantêm registros anteriores em seções de histórico explicitamente sem vigência operacional. Menções históricas a Aurora, Service LoadBalancer/controller, SSM v1/releases, precedência X-Ray ou publicação ainda pendente não descrevem a configuração final. O texto vigente vem antes desse histórico.

## Comprovação e limites aceitos

[E5 encerrada por aceite](../ENCERRAMENTO_E5_2026-10-21.md): três sinais, indicadores/SQL, Collectors, dashboards e alertas demonstrados no alcance registrado. X-Ray/OTLP separados; logs Gateway parciais, cancelamento AWS inconclusivo e aviso de métrica nativa preservados. CPU/memória/carga dispensados não são testes aprovados nem novas pendências automáticas.

[E6 encerrada no escopo acordado](../ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md): fontes funcionais, PRs/CI/CD/proteções, VersionId SAM, descarte final e custos estimados. Concorrência de publicação/coexistência/rollback dispensados; análise da diferença workflows manuais/R06 adiada. Isso não altera o requisito original ou certifica prd.

Sem promessa de entrega única para notificações/telemetria, recuperação de sinais, alta disponibilidade ou capacidade sob carga. Ativação futura exige a ordem documentada e autorização do mantenedor; nenhuma operação AWS foi executada na revisão E7.
