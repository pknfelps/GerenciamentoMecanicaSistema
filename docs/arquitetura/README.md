# Arquitetura da Fase 3

> **Operação vigente:** [ADR 008 — Infraestrutura declarativa](adrs/008-INFRAESTRUTURA-DECLARATIVA.md) e [contratos SSM v2](CONTRATOS_ENTRE_REPOSITORIOS.md). NLB Terraform/NodePort, Job próprio, plan salvo → aprovação → apply. Registros datados de releases/candidatos v1 abaixo são históricos e foram substituídos. Aplicação da reformulação ainda pendente.

[README do projeto](../../README.md)

**Estado em 2026-09-16:** arquitetura aceita e documentação inicial da E0.7 concluída. Estes documentos descrevem o destino da implementação; não comprovam recursos implantados. A revisão contra o sistema entregue permanece na E7.

## Consultar por necessidade

| Documento | Conteúdo |
|---|---|
| [Pacote Auth.Contracts](PACOTE_AUTH_CONTRACTS.md) | CPF/CNPJ e JWT compartilhados, testes, empacotamento e publicação/consumo independente |
| [Acesso e autenticação](ACESSO_E_AUTENTICACAO.md) | Fonte dos contratos HTTP, JWT e permissões por endpoint |
| [Contratos entre repositórios](CONTRATOS_ENTRE_REPOSITORIOS.md) | Catálogo SSM v2, secrets por referência, artefatos, operação declarativa e responsabilidades de cada produtor/consumidor |
| [Componentes](diagramas/COMPONENTES.md) | Rede, serviços, ambientes e caminhos de observabilidade |
| [Validação de documento](diagramas/VALIDACAO_CPF.md) | Emissão do token do cliente e autorização da consulta de OS |
| [Abertura de OS](diagramas/ABERTURA_OS.md) | Login interno, criação transacional e notificação |
| [RFC 001 — Execução](rfcs/001-EXECUCAO.md) | Integração entre Gateway, função, aplicação e banco |
| [RFC 002 — Entrega](rfcs/002-ENTREGA.md) | Responsabilidades, contratos entre pipelines e ciclo dos ambientes |
| [RFC 003 — Dados e observabilidade](rfcs/003-DADOS-E-OBSERVABILIDADE.md) | Histórico, métricas, coleta e critérios de validação |

As RFCs organizam como a solução deve funcionar. Os ADRs registram por que as escolhas foram feitas. O contrato de acesso detalha os formatos HTTP, JWT e as permissões por endpoint.

**Complemento em 2026-09-24:** contratos operacionais v1 documentados e referenciados pelos quatro repositórios. Bootstrap e diagnóstico positivo OIDC estão disponíveis; os publicadores SSM, manifestos e deploys descritos no contrato continuam como trabalho de implementação.

**Complemento em 2026-10-01:** contrato operacional 1.1.0 distingue a prontidão da base para o banco (`database-release`) da base completa (`release`). A ordem passa a permitir Aurora/esquema antes de Service/NLB/JWT; API/função continuam dependendo da base completa. Fonte dos campos e checks: seção 4.1.1 de [Contratos entre repositórios](CONTRATOS_ENTRE_REPOSITORIOS.md). Implementação pendente na E2.14.

**Implementação local em 2026-10-02:** perfil database e invalidação SSM implementados na infraestrutura; workflow OIDC do banco registra evidência com sua própria role, ligada ao candidato/geração da base. Testes locais aprovados; publicação/validação AWS, perfil full e coordenação entre repositórios continuam pendentes. Protocolo operacional na seção 4.1.2 do contrato.

**Replanejamento em 2026-10-02:** Aurora falhou no Free Plan. A decisão vigente é RDS PostgreSQL `db.t3.micro` Single-AZ ([ADR 007](adrs/007-RDS-FREE-PLAN.md)). As menções anteriores a Aurora registram a sequência histórica; o contrato operacional e as RFCs abaixo refletem a arquitetura alvo. Recuperar os recursos parciais de hom antes de novo apply.

## Rastreabilidade das decisões

| Decisões | Registro | Requisitos relacionados |
|---|---|---|
| D02/D03 | [ADR 001 — Identidade e permissões](adrs/001-IDENTIDADE.md) | R02/R03; U01–U03 |
| D01/D09.2 | [ADR 002 — Entrada e rede AWS](adrs/002-REDE-AWS.md) | R01/R08 |
| D01.2/D07/D08 | [ADR 003 — Decisão anterior de banco e histórico](adrs/003-BANCO-E-HISTORICO.md) | R07/R11/R12 |
| D01/D09 (revisão) | [ADR 008 — Infraestrutura declarativa](adrs/008-INFRAESTRUTURA-DECLARATIVA.md) | E2.4/E2.10/E2.14 |
| D01.6 | [ADR 007 — RDS no Free Plan](adrs/007-RDS-FREE-PLAN.md) | R01/R07/R12 |
| D01.3/D05/D09.1/D09.3 | [ADR 004 — Entrega e ambientes](adrs/004-ENTREGA-E-AMBIENTES.md) | R04/R05/R06 |
| D04 | [ADR 005 — Observabilidade](adrs/005-OBSERVABILIDADE.md) | R09/R10/R11 |
| D06 | [ADR 006 — Notificações](adrs/006-NOTIFICACOES.md) | Escopo interpretado; E7.11 |

Os diagramas, RFCs e ADRs iniciam as evidências de R12. O ER definitivo e a comparação com a implementação ainda serão produzidos; R12 não está integralmente comprovado nesta etapa.

## Detalhes a resolver durante a implementação

| Detalhe | Responsável / etapa | Impacto e condição de avanço |
|---|---|---|
| Publicadores/consumidores de contratos, permissões de workloads e CI da função | E1/E2/E3 | Implementar a especificação v2; diagnóstico OIDC não comprova deploy ou consumo dos metadados |
| Coordenação de alterações do mesmo ambiente entre repositórios | E1/E2 | Lock de estado e concurrency local do GitHub não bastam para serializar quatro pipelines; manter operações sequenciais por ambiente, coordenadas pelo mantenedor |
| Versões de Terraform/providers, EKS/add-ons, RDS PostgreSQL e integração Gateway/NLB | E2/E3 | Fixar minor PostgreSQL disponível, target do NLB e protocolos/TLS; comprovar conectividade antes de publicar rotas |
| SQL, índices, restrições, preservação do histórico após excluir OS e ER | E2/E4 | Esquema inicial reproduzível e transações corretas antes da validação integrada |
| Exposição de documentação/health no Gateway | E2/E3 | Probes anônimas não implicam publicação dessas rotas na entrada pública |
| Layer/extensão OTel da Lambda, receiver CloudWatch e correlação X-Ray | E5 | Fixar versões/ARN compatíveis e comprovar coleta; revisar a decisão se o experimento falhar |
| Publicação/reconciliação de métricas após commit, deduplicação e falhas de exportação | E4/E5 | Não prometer entrega exatamente uma vez; comparar dashboard com histórico persistido |
| Conta/região New Relic, limites e destinatário de alertas | E5/E6 | Configurar sem publicar credenciais; validar notificações durante janela ativa |

Mudanças arquiteturais devem atualizar o ADR correspondente, as RFCs, os contratos e os diagramas afetados.
