# Mapa das decisões da Fase 3

[Índice central](../../PLANO_FASE_3.md)

## 5. Arquitetura de referência e decisões abertas

Fluxos consolidados como especificação na E0; diagramas completos no [índice de arquitetura](../arquitetura/README.md):

```text
Cliente -> API Gateway -> função de autenticação -> consulta cliente no banco
Cliente <- JWT válido <- função de autenticação
Cliente com JWT -> API Gateway -> API no Kubernetes -> banco gerenciado
Gateway / função / API / Kubernetes -> observabilidade
```

A API mantém validação do JWT e autorização por rota. A REST API do Gateway direciona `/customers/validate` à Lambda .NET 10, que consulta diretamente o Aurora (D01.5); operações da aplicação passam por VPC Link e NLB interno até o EKS (D01.4). E0.1/E0.4 estão concluídas como especificação; configurações executáveis seguem nas etapas de implementação.

| ID | Decisão | Situação atual | Onde registrar o resultado |
|---|---|---|---|
| [D01](decisoes/INFRAESTRUTURA.md) | Nuvem e serviços de Gateway, função e banco | Serviços e topologia definidos em D01.1–D01.5, incluindo Lambda .NET 10/ZIP, Aurora e Secrets Manager; parâmetros/ambientes e propriedade consolidados em D05/D09, implementação pendente | D01.1–D01.5; [RFC 001](../arquitetura/rfcs/001-EXECUCAO.md), ADRs 002/003/004 no índice de arquitetura |
| [D02](decisoes/ACESSO.md) | Contrato de autenticação, emissão/validação JWT e acesso ao cadastro | Contrato consolidado: Validate serverless, JWT atual e preservação de sessões; consulta direta ao Aurora aceita em D01.5 | D02.1/D02.2, D01.5, [especificação](../arquitetura/ACESSO_E_AUTENTICACAO.md) e [ADR 001](../arquitetura/adrs/001-IDENTIDADE.md) |
| [D03](decisoes/ACESSO.md) | Estados do cliente e permissões de cliente/usuários da oficina | Consolidada como especificação: matriz por endpoint, Admin/Mechanic/cliente, users e estado inicial Ativo sem inativação agora | D03.1–D03.6 e [especificação](../arquitetura/ACESSO_E_AUTENTICACAO.md) |
| [D04](decisoes/OBSERVABILIDADE.md) | Ferramenta de observabilidade e forma de exportação | New Relic/OTLP, Collector EKS, extensão Lambda e coleta CloudWatch/X-Ray aceitos (D04.1/D04.2); compatibilidade e correlação a validar em E5/E6 | D04.1/D04.2; [RFC 003](../arquitetura/rfcs/003-DADOS-E-OBSERVABILIDADE.md) e [ADR 005](../arquitetura/adrs/005-OBSERVABILIDADE.md) |
| [D05](decisoes/REPOSITORIOS_E_AMBIENTES.md) | Nomes dos quatro repositórios, branches e isolamento dos ambientes | Responsabilidades, develop/homologação, main/produção e ordem de deploy aceitos (D05.1); isolamento/coexistência e capacidade inicial hom/prd aceitos em D05.2; nomes aceitos em D05.3, criação na E1 e contratos operacionais pendentes | D05.1–D05.3; READMEs e documentação de entrega |
| [D06](decisoes/NEGOCIO_E_PERSISTENCIA.md) | Alcance de notificações serverless | Resolvida: manter envio atual na API; migração serverless como melhoria futura a apresentar na entrega (D06.1) | D06.1; [ADR 006](../arquitetura/adrs/006-NOTIFICACOES.md) e RFC 001; apresentação final em E7.11 |
| [D07](decisoes/NEGOCIO_E_PERSISTENCIA.md) | Métricas de negócio, tempo por status e tempo total de atendimento | Regras aceitas em D07.1; inclui indicador adicional solicitado pelo usuário, da abertura à conclusão do serviço | D07.1; modelo de dados e inicialização em D08 |
| [D08](decisoes/NEGOCIO_E_PERSISTENCIA.md) | Modelo, histórico de OS, índices/restrições e inicialização do banco | Banco descartável e modelo com histórico aceitos (D08.1/D08.2); remover datas/duração persistidas de orders. Detalhamento SQL/ER e execução da inicialização pendentes | [RFC 003](../arquitetura/rfcs/003-DADOS-E-OBSERVABILIDADE.md) e [ADR 003](../arquitetura/adrs/003-BANCO-E-HISTORICO.md); ER e scripts nas etapas seguintes |
| [D09](decisoes/REPOSITORIOS_E_AMBIENTES.md) | Backend/locking do Terraform e contratos entre repositórios | S3 versionado com locking nativo, estados separados e OIDC aceitos (D09.1); SSM para metadados e Secrets Manager para segredos. Conectividade aceita em D09.2; distribuição/versionamento de contratos aceitos em D09.3, implementação pendente | D09.1–D09.3; [RFC 002](../arquitetura/rfcs/002-ENTREGA.md) e [ADR 004](../arquitetura/adrs/004-ENTREGA-E-AMBIENTES.md) |

Toda decisão aceita deve informar data, motivação, alternativas relevantes, consequências e link do documento correspondente.

Acesso de rede D09.2: [infraestrutura](decisoes/INFRAESTRUTURA.md#d09-2). Custos e propostas anteriores: [avaliações históricas](historico/AVALIACOES.md). O quadro e o próximo passo vigentes ficam no índice central.
