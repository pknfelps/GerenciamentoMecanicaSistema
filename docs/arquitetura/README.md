# Arquitetura da Fase 3

[Plano vivo](../../PLANO_FASE_3.md) · [Decisões detalhadas](../plano-fase-3/DECISOES.md)

**Estado em 2026-09-16:** arquitetura aceita e documentação inicial da E0.7 concluída. Estes documentos descrevem o destino da implementação; não comprovam recursos implantados. A revisão contra o sistema entregue permanece na E7.

## Consultar por necessidade

| Documento | Conteúdo |
|---|---|
| [Acesso e autenticação](ACESSO_E_AUTENTICACAO.md) | Fonte dos contratos HTTP, JWT e permissões por endpoint |
| [Componentes](diagramas/COMPONENTES.md) | Rede, serviços, ambientes e caminhos de observabilidade |
| [Validação de CPF](diagramas/VALIDACAO_CPF.md) | Emissão do token do cliente e autorização da consulta de OS |
| [Abertura de OS](diagramas/ABERTURA_OS.md) | Login interno, criação transacional e notificação |
| [RFC 001 — Execução](rfcs/001-EXECUCAO.md) | Integração entre Gateway, função, aplicação e banco |
| [RFC 002 — Entrega](rfcs/002-ENTREGA.md) | Responsabilidades, contratos entre pipelines e ciclo dos ambientes |
| [RFC 003 — Dados e observabilidade](rfcs/003-DADOS-E-OBSERVABILIDADE.md) | Histórico, métricas, coleta e critérios de validação |

As RFCs organizam como a solução deve funcionar. Os ADRs registram por que as escolhas foram feitas. Parâmetros e regras detalhadas continuam nos documentos de decisão vinculados; não manter cópias integrais desses contratos aqui.

## Rastreabilidade das decisões

| Decisões | Registro | Requisitos relacionados |
|---|---|---|
| D02/D03 | [ADR 001 — Identidade e permissões](adrs/001-IDENTIDADE.md) | R02/R03; U01–U03 |
| D01/D09.2 | [ADR 002 — Entrada e rede AWS](adrs/002-REDE-AWS.md) | R01/R08 |
| D01.2/D07/D08 | [ADR 003 — Banco e histórico](adrs/003-BANCO-E-HISTORICO.md) | R07/R11/R12 |
| D01.3/D05/D09.1/D09.3 | [ADR 004 — Entrega e ambientes](adrs/004-ENTREGA-E-AMBIENTES.md) | R04/R05/R06 |
| D04 | [ADR 005 — Observabilidade](adrs/005-OBSERVABILIDADE.md) | R09/R10/R11 |
| D06 | [ADR 006 — Notificações](adrs/006-NOTIFICACOES.md) | Escopo interpretado; E7.11 |

Os diagramas, RFCs e ADRs iniciam as evidências de R12. O ER definitivo e a comparação com a implementação ainda serão produzidos; R12 não está integralmente comprovado nesta etapa.

## Detalhes a resolver durante a implementação

| Detalhe | Responsável / etapa | Impacto e condição de avanço |
|---|---|---|
| URLs, permissões reais, proteções e CI dos quatro repositórios | E1 | Verificar recursos existentes antes de distribuir artefatos; documentar contratos operacionais em E1.8 |
| Coordenação de alterações do mesmo ambiente entre repositórios | E1/E2 | Lock de estado e concurrency local do GitHub não bastam para serializar quatro pipelines; concretizar coordenação antes de deploys concorrentes |
| Versões de Terraform/providers, EKS/add-ons, Aurora com pausa e integração Gateway/NLB | E2/E3 | Fixar versões suportadas, target do NLB e protocolos/TLS; comprovar conectividade antes de publicar rotas |
| SQL, índices, restrições, preservação do histórico após excluir OS e ER | E2/E4 | Esquema inicial reproduzível e transações corretas antes da validação integrada |
| Exposição de documentação/health no Gateway | E2/E3 | Probes anônimas não implicam publicação dessas rotas na entrada pública |
| Layer/extensão OTel da Lambda, receiver CloudWatch e correlação X-Ray | E5 | Fixar versões/ARN compatíveis e comprovar coleta; revisar a decisão se o experimento falhar |
| Publicação/reconciliação de métricas após commit, deduplicação e falhas de exportação | E4/E5 | Não prometer entrega exatamente uma vez; comparar dashboard com histórico persistido |
| Conta/região New Relic, limites e destinatário de alertas | E5/E6 | Configurar sem publicar credenciais; validar notificações durante janela ativa |

Mudanças arquiteturais devem atualizar a decisão de origem, o ADR correspondente e os diagramas afetados. Tarefas e evidências continuam exclusivamente no [plano](../../PLANO_FASE_3.md).
