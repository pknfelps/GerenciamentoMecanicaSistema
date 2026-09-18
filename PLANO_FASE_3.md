# Plano vivo de implementação — Tech Challenge Fase 3

> Índice central e estado da sessão. Os detalhes estão em documentos menores; começar aqui e abrir somente a etapa e o assunto necessários.

- **Estado:** E0 concluída como especificação; E1 em andamento, com E1.1 concluída e E1.2 implementada/validada em branches para revisão.
- **Última atualização:** 2026-09-18 — removida inicialização SQL local da API a pedido do usuário; build de imagem restaurado no CI. PRs de API/banco já marcados pelo usuário como prontos para revisão; publicação ECR/deploy pendentes. Compose revalidado; build local bloqueado por falha de inicialização do Docker Desktop. Suíte anterior: 468 testes aprovados.
- **Etapa atual:** E1 — Quatro repositórios e governança de entrega.
- **Próxima ação:** revisar os [quatro PRs da E1.2](docs/plano-fase-3/RESULTADO_E1_2.md), abertos em rascunho após autorização explícita para uso do navegador. Próxima tarefa de implementação: E1.3, completar READMEs; nenhum merge ou deploy realizado.
- **Arquitetura consolidada:** [Diagramas, RFCs e ADRs](docs/arquitetura/README.md) — arquitetura alvo, justificativas e detalhes adiados com etapa de resolução.
- **Especificação de acesso:** [Acesso e autenticação](docs/arquitetura/ACESSO_E_AUTENTICACAO.md) — matriz por endpoint, contratos de users, validação serverless do CPF e JWT.
- **Perfil de uso definido:** ambiente implantado somente para testes e gravação da apresentação, sem operação contínua. Comparar bancos pelo ciclo completo de ativação/inatividade, não apenas por 730 horas de computação.
- **Ciclo dos dados:** banco educacional descartável, apagado para reduzir custos; recriar esquema e dados de demonstração em banco vazio. Sem preservação/conversão de dados de versões anteriores ou framework de migrações incremental nesta fase (D08.1).
- **Uso local:** foco na API publicada na AWS. Docker é opcional e o banco local será preparado manualmente; SQL e inicialização ficam no repositório de banco, sem cópia/sincronizador na API.
- **Banco escolhido:** Aurora PostgreSQL Serverless v2, mantendo probes e health checks atuais. Pausa planejada fora das janelas de uso, após encerramento dos consumidores/conexões (D01.2).
- **Fonte de requisitos:** `13SOAT - Fase 3 - Tech Challenge.pdf`, páginas 2 a 5, fornecido pelo usuário. Local original: `C:/Users/felip/Downloads/13SOAT - Fase 3 - Tech Challenge.pdf`.
- **Base da análise:** arquivos locais do projeto em 2026-09-05. A existência e o funcionamento dos recursos na nuvem, das pipelines e das proteções no GitHub ainda precisam ser comprovados.

## Leitura e atualização seletivas

1. Ler este índice; depois abrir apenas o arquivo da etapa ativa e as decisões relacionadas, seguindo os links abaixo. Usar busca por ID (E/D/R/U) para localizar trechos. Não ler toda a pasta por padrão.
2. Registrar o item em andamento no quadro de execução. Usar os IDs deste documento em PRs, decisões e evidências quando útil.
3. Marcar uma tarefa como concluída somente após atender seu critério e registrar evidência verificável: arquivo, PR, execução de pipeline, teste, diagrama ou demonstração.
4. Atualizar os checkboxes no arquivo da etapa, a decisão no arquivo do assunto e este índice quando mudar o estado/próximo passo. Registrar evidências/pendências em ACOMPANHAMENTO.md e acrescentar uma linha ao histórico sem precisar relê-lo inteiro.
5. Quando o escopo mudar, registrar o motivo e o impacto nas etapas seguintes. Preservar o histórico das decisões substituídas.
6. Não registrar tokens, senhas, chaves, dados pessoais reais ou conteúdo de secrets nos documentos. Os IDs E/D/R/U permanecem estáveis; não duplicar checklists nem manter uma cópia integral paralela do plano.

**Estados do quadro:** Não iniciada · Em andamento · Bloqueada · Concluída.

**Classificação de escopo:**

- **Obrigatório:** exigência explícita do PDF.
- **Derivado:** trabalho necessário para atender uma exigência no projeto atual.
- **Proposta:** escolha de implementação a avaliar; pode ser substituída por alternativa que atenda ao requisito.
- **A esclarecer:** ponto cujo detalhamento no enunciado não é suficiente para fixar a implementação.

Este plano registra o trabalho e seu contexto. Ele não substitui RFCs, ADRs, READMEs ou as evidências finais. As tarefas listadas são planejamento, e não comprovação de que foram executadas.

## Etapas, dependências e estado

| Etapa | Objetivo | Dependências | Estado | Evidência de conclusão |
|---|---|---|---|---|
| [E0](docs/plano-fase-3/etapas/E0.md) | Fechar arquitetura e contratos essenciais | Análise inicial | Concluída como especificação | [Arquitetura e contratos](docs/arquitetura/README.md) |
| [E1](docs/plano-fase-3/etapas/E1.md) | Organizar repositórios, proteções e estrutura de CI/CD | E0: limites e contratos | Em andamento | [Distribuição E1.2 validada](docs/plano-fase-3/RESULTADO_E1_2.md); revisão/integração pendente |
| [E2](docs/plano-fase-3/etapas/E2.md) | Provisionar banco e Kubernetes pelas pipelines | E1; D01, D08, D09 | Não iniciada | — |
| [E3](docs/plano-fase-3/etapas/E3.md) | Implementar autenticação serverless e Gateway | Contratos E0; E1; E2 para validação em nuvem | Não iniciada | — |
| [E4](docs/plano-fase-3/etapas/E4.md) | Adaptar aplicação, autorização e dados de OS | D02, D03, D07, D08; E2/E3 para integração | Não iniciada | — |
| [E5](docs/plano-fase-3/etapas/E5.md) | Implantar observabilidade e dashboards | D04; escopo de notificações consolidado em D06.1; E2 a E4 para integração completa | Não iniciada | — |
| [E6](docs/plano-fase-3/etapas/E6.md) | Validar sistema integrado e entrega por ambiente | E1 a E5 | Não iniciada | — |
| [E7](docs/plano-fase-3/etapas/E7.md) | Consolidar documentação, vídeo e PDF final | [E6](docs/plano-fase-3/etapas/E6.md) | Não iniciada | — |

Os contratos de E0 permitem desenvolver função e aplicação localmente antes de a infraestrutura estar disponível. A conclusão dessas etapas depende da validação integrada. Documentação e evidências devem ser produzidas durante todas as etapas.

## Onde consultar cada assunto

| Documento | Consultar quando |
|---|---|
| [Requisitos, escopo e inventário](docs/plano-fase-3/REQUISITOS.md) | Conferir o PDF, R01–R15 e U01–U03 |
| [Mapa das decisões](docs/plano-fase-3/DECISOES.md) | Localizar D01–D09 e suas fontes |
| [Arquitetura](docs/arquitetura/README.md) | Consultar diagramas, RFCs, ADRs e detalhes a resolver na implementação |
| [Acesso — D02/D03](docs/plano-fase-3/decisoes/ACESSO.md) | Recuperar decisões de perfis, sessões e autenticação |
| [Infraestrutura — D01/D09.2](docs/plano-fase-3/decisoes/INFRAESTRUTURA.md) | Trabalhar com EKS, Aurora, Gateway, Lambda e rede |
| [Observabilidade — D04](docs/plano-fase-3/decisoes/OBSERVABILIDADE.md) | Instrumentar/coletar sinais, dashboards e alertas |
| [Negócio e persistência — D06/D07/D08](docs/plano-fase-3/decisoes/NEGOCIO_E_PERSISTENCIA.md) | Consultar métricas, histórico de OS e escopo das notificações |
| [Repositórios e ambientes — D05/D09](docs/plano-fase-3/decisoes/REPOSITORIOS_E_AMBIENTES.md) | Trabalhar com hom/prd, CI/CD, S3/SSM, contratos e artefatos |
| [Acompanhamento](docs/plano-fase-3/ACOMPANHAMENTO.md) | Atualizar URLs reais, evidências e pendências |
| [Histórico de progresso](docs/plano-fase-3/historico/PROGRESSO.md) | Registrar avanços ou recuperar a sequência de decisões |
| [Avaliações históricas](docs/plano-fase-3/historico/AVALIACOES.md) | Recuperar alternativas/custos discutidos anteriormente |

A matriz detalhada de endpoints permanece em [Acesso e autenticação](docs/arquitetura/ACESSO_E_AUTENTICACAO.md). O plano foi dividido em 2026-09-15 para permitir leitura seletiva. A E0 foi concluída como especificação em 2026-09-16; a revisão dos documentos contra a implementação entregue permanece na E7.
