# Repositórios, evidências e pendências da Fase 3

[Índice central](../../PLANO_FASE_3.md)

## 8. Registro de repositórios e ambientes

Preencher com referências reais conforme forem criadas. Não considerar valores pendentes como configuração existente.

| Responsabilidade | Repositório | Pipeline | Homologação | Produção |
|---|---|---|---|---|
| Função serverless | [pknfelps/GerenciamentoMecanicaAutenticacao](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao) — público, criado em 2026-09-17 | A implementar | A definir | A definir |
| Infraestrutura Kubernetes | [pknfelps/GerenciamentoMecanicaInfraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura) — público, criado em 2026-09-17 | A implementar | A definir | A definir |
| Infraestrutura do banco | [pknfelps/GerenciamentoMecanicaBancoDados](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados) — público, criado em 2026-09-17 | A implementar | A definir | A definir |
| Aplicação principal | [pknfelps/GerenciamentoMecanicaSistema](https://github.com/pknfelps/GerenciamentoMecanicaSistema) — público, main padrão, acesso admin confirmado em 2026-09-17 | Pipeline local existente; adaptação e execuções remotas a verificar | A verificar | A verificar |

## 9. Evidências e pendências

| Evidência | Requisito/etapa | Referência | Data | Resultado |
|---|---|---|---|---|
| Análise inicial | Planejamento | PDF da Fase 3 e inventário da seção 3 | 2026-09-05 | Requisitos e lacunas identificados; sem validação em nuvem |
| Especificação de acesso | E0.2/E0.3; R02/R03; U01–U03 | [Matriz e contratos](../arquitetura/ACESSO_E_AUTENTICACAO.md) | 2026-09-13 | Comparação com controllers: 39 endpoints atuais cobertos e 3 novos, sem omissões; links locais verificados; implementação pendente |
| Arquitetura inicial consolidada | E0.7; evidência inicial de R12 | [Diagramas, três RFCs e seis ADRs](../arquitetura/README.md) | 2026-09-16 | D01–D09 rastreadas; fluxos confrontados com contratos e criação/notificação atuais; links e estrutura Markdown conferidos. Renderização Mermaid e comprovação em nuvem não executadas; revisão final em E7 |
| Inventário de repositório/acessos e artefatos | E1.1, preparação de E1.2 | [Inventário E1](INVENTARIO_E1.md) | 2026-09-17 | Atual confirmado pelo conector; destinos dos artefatos identificados. Criação dos novos inicialmente aguardou login, concluído na retomada |
| Criação dos três repositórios | E1.1; parte inicial de R04 | URLs acima e [commits iniciais](INVENTARIO_E1.md) | 2026-09-17 | Interface GitHub confirmou README/main; conector confirmou existência, visibilidade pública e admin nos três. Sem código distribuído, pipelines ou deploy; E1.1 concluída |
| Distribuição dos artefatos | E1.2 | [Resultado, branches e validação](RESULTADO_E1_2.md) | 2026-09-17 | 17 arquivos transferidos com origem/hash; Compose e Kustomize válidos; 468 testes aprovados. Branches para revisão; PRs/integração pendentes; sem deploy |
| Abertura de PRs | E1.2 / P06 | [Quatro PRs e bases](RESULTADO_E1_2.md) | 2026-09-18 | Criados pela sessão autenticada do navegador após autorização explícita; Draft e bases confirmados na interface. Aplicação para develop, demais para main; sem merge/auto-merge |
| Revisão do uso local e CI | E1.2 | [Resultado atualizado](RESULTADO_E1_2.md) | 2026-09-18 | Init.sql/sincronização removidos da API; Compose revalidado; build de imagem restaurado no CI. Build local aprovado após reinício do Docker, imagem linux/amd64 confirmada; `.dockerignore` corrigido. Validação remota pendente |

| ID | Pendência ou bloqueio | Impacto | Próxima ação | Estado |
|---|---|---|---|---|
| P01 | Consolidação documental E0.7 | Resolvido: arquitetura alvo, fluxos e justificativas registrados | Manter documentos durante implementação e revisar contra a entrega em E7 | Resolvida em 2026-09-16 |
| P02 | Operação em nuvem e configurações remotas não verificadas | Base existente pode exigir ajustes adicionais | Inventariar recursos e acessos ao iniciar E1/E2 | Aberta |
| P03 | Escopo de notificações serverless pouco detalhado no enunciado | Resolvido: migração fora da implementação desta entrega | Apresentar como melhoria futura em E7.11 conforme D06.1 | Resolvida em 2026-09-14 |
| P04 | Alcance da permissão de Remove de usuário da oficina | Resolvido: operação exclusiva de Admin, conforme D03.5 | Implementar e testar na E4 | Resolvida em 2026-09-13 |
| P05 | Navegador integrado sem sessão GitHub para criar repositórios; conector não oferece criação | Resolvido: sessão autenticada e três repositórios criados pela interface | Prosseguir para distribuição de código na E1.2 | Resolvida em 2026-09-17 |
| P06 | Criação de PRs pelo conector negada com 403; navegador inicialmente bloqueado pela revisão automática | Resolvido após autorização explícita para uso da sessão autenticada: quatro PRs em rascunho criados | Revisar o conjunto; permissões do conector não foram alteradas | Resolvida em 2026-09-18 |
