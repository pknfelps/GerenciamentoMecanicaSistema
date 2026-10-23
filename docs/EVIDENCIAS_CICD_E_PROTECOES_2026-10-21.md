# Evidências de CI/CD e proteções dos repositórios — 21/10/2026

**Descarte final posterior — 21/10:** cinco execuções/jobs e ausência AWS conferidos após a implantação final. [Links, ordem e custos observados](ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md). As execuções antigas abaixo permanecem evidência histórica.

**Correção posterior autorizada e aplicada às 17:40 UTC:** o ruleset de autenticação agora exige **configuration e dotnet**, ambos do GitHub Actions, em **main e develop**. GitHub confirmou Ruleset updated; GETs posteriores do ruleset e regras efetivas das duas branches comprovaram aplicação. As regras anteriores, alvos, PR obrigatório, bloqueio de exclusão/force-push e bypass vazio foram preservados. Nenhum workflow executado. A alteração exigiu reautenticação do mantenedor na própria interface, sem senha/código enviado ao assistente. E6.7 e E1.4 concluídas no escopo da conferência de governança; E6 agora onze itens concluídos/cinco abertos ou parciais.

Conferência **somente de leitura** dos quatro repositórios GitHub, iniciada às 16:20 UTC. Consultados metadados de PRs, execuções/jobs, branches, rulesets e regras efetivas. Listas de bypass verificadas também na interface autenticada de Settings. Nenhum workflow executado, PR aberto/integrado, configuração alterada ou recurso AWS consultado nesta conferência.

**Resultado atual:** os quatro repositórios possuem CI aprovado, PR integrado e publicação/provisionamento hom vinculados aos commits abaixo. Main/develop exigem PR e bloqueiam exclusão/force-push; listas de bypass vazias. A ausência dos checks obrigatórios de autenticação foi identificada na primeira conferência e corrigida após solicitação explícita. Execução aprovada não equivale, por si, à obrigatoriedade do check.

## PR → CI → implantação em hom

As execuções selecionadas terminaram em `success`. O CI corresponde ao SHA da branch do PR; o deploy corresponde ao SHA resultante do merge em develop. São commits distintos por causa do merge, não divergência de revisão.

| Repositório | PR integrado em develop | CI aprovado nesse PR | Publicação/provisionamento posterior |
|---|---|---|---|
| API | [PR #26](https://github.com/pknfelps/GerenciamentoMecanicaSistema/pull/26), integrado em 21/10 às 00:16:04 UTC | [pipeline 39519567293](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions/runs/39519567293): unit-tests, code-analysis e build-image aprovados antes do merge. [auth-contracts 39519567347](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions/runs/39519567347): verify aprovado; publish skipped, esperado no PR | [API deploy 39600883942](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions/runs/39600883942), 21/10: Publicar e Implantar aprovados |
| Autenticação | [PR #19](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/pull/19), integrado em 21/10 às 00:11:01 UTC | [auth-ci 39519585387](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/actions/runs/39519585387): configuration e dotnet aprovados antes do merge | [auth-publish 39601553302](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/actions/runs/39601553302): package/publish aprovados; [auth-provision 39602420530](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/actions/runs/39602420530): deploy aprovado, ambos em 21/10 |
| Banco | [PR #22](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/pull/22), integrado em 17/10 às 23:25:23 UTC | [ci 39078429063](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/actions/runs/39078429063): validate aprovado antes do merge | [database-provision 39599408999](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/actions/runs/39599408999), 21/10: plan/approval/apply aprovados; Initialize schema e Remove temporary credentials aprovados |
| Infraestrutura | [PR #42](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/pull/42), integrado em 20/10 às 00:15:49 UTC | [ci 39346339064](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/actions/runs/39346339064): validate aprovado antes do merge | [base-provision 39596520819](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/actions/runs/39596520819) e [gateway-provision 39603164632](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/actions/runs/39603164632), 21/10: plan/approval/apply aprovados nos dois |

| Repositório | SHA do head do PR / CI | SHA do merge / implantação hom |
|---|---|---|
| API | `a23b8c7e1bde195c07be7b13d47112442904152a` | `9d1c0625e7c0ada58a110e2f833b21853d3a0b61` |
| Autenticação | `4db91a76a3188b84780138bed366a71cfec587a9` | `bf879cfe5491d5966b556de930c6ca52ed489060` |
| Banco | `41d457194cce807afaf328310341f03085f81783` | `5ef95df52c037d940ffad07a07175fea0209ab0a` |
| Infraestrutura | `5582dd8f6455594b195c2fb5bcb69a9a27ae37f7` | `333e2a8feb33385ddb248076f486f6fe1a295140` |

As revisões API/Lambda e a integração qualificada da Lambda 12 correspondem à [validação AWS já registrada em 21/10](VALIDACAO_TRACING_W3C_HOM_2026-10-21.md). Não houve nova consulta AWS nesta revisão. Sucesso do job por si não certifica todos os cenários funcionais; estes mantêm suas evidências próprias.

## Proteções efetivas de main e develop

Nos quatro repositórios, o ruleset `Block direct commits` está **active**, inclui a branch padrão (`main`) e `develop`, sem exclusões. Ambas as branches retornaram proteção ativa e regras efetivas de PR, exclusão e force-push. As listas de bypass estão vazias na interface autenticada; a ausência desse campo na resposta pública da API, sozinha, não foi usada como prova.

| Repositório / ruleset | PR obrigatório | Exclusão/force-push bloqueados | Bypass | Checks obrigatórios |
|---|---|---|---|---|
| [API — 23791629](https://github.com/pknfelps/GerenciamentoMecanicaSistema/rules/23791629) | Sim | Sim | Vazio | unit-tests, code-analysis, build-image |
| [Autenticação — 23791642](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/rules/23791642) | Sim | Sim | Vazio | configuration, dotnet — correção aplicada e conferida às 17:40 UTC |
| [Banco — 23791635](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/rules/23791635) | Sim | Sim | Vazio | validate |
| [Infraestrutura — 23791639](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/rules/23791639) | Sim | Sim | Vazio | validate |

Os checks exigidos dos quatro repositórios estão vinculados ao GitHub Actions, integration_id 15368, e seus nomes correspondem aos jobs aprovados da seleção. Em autenticação, `configuration` e `dotnet` foram selecionados da fonte GitHub Actions, sem aceitar Any source. A conferência pública posterior confirmou ambos na regra required_status_checks de main/develop. A comparação com o snapshot anterior confirmou que nenhuma outra regra ou condição de alvo mudou.

Características observadas, sem acrescentar requisitos:

- Os quatro exigem PR, com **zero aprovações mínimas**. Isso não permite dispensar o PR; não foi exigida aprovação de outra pessoa neste escopo.
- Métodos permitidos pelo ruleset: merge. Dismiss stale reviews on push está habilitado; code owner, aprovação do último push e resolução obrigatória de todas as conversas não são exigidos.
- Os quatro não exigem branch atualizada com a base antes do merge (`strict_required_status_checks_policy=false`). A correção auth manteve esse padrão; não é ausência dos checks obrigatórios. Do not require status checks on creation permaneceu desmarcado.
- O check `verify` de auth-contracts da API não é obrigatório no ruleset; ficou registrado como CI complementar aprovado, sem ampliar o escopo de proteção solicitado.

Não foi tentado push direto, force-push, exclusão ou merge para testar os bloqueios. A comprovação é de configuração efetiva, não um experimento destrutivo.

## Disparo manual e situação das branches

As publicações/provisionamentos selecionados têm evento **workflow_dispatch**. A aprovação e execução dos jobs depois do acionamento estão comprovadas; isso não comprova disparo automático após merge. Mantém-se a ressalva R06 da [revisão E6](REVISAO_E6_2026-10-21.md), sem mudar gatilhos.

As últimas promoções para main observadas foram [API PR #13](https://github.com/pknfelps/GerenciamentoMecanicaSistema/pull/13), em 09/10; [auth PR #8](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/pull/8), em 12/10; [banco PR #14](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/pull/14), em 06/10. Seus heads main ainda são, respectivamente, `e2d9a5b…`, `cd4bb406…` e `4b7fc894…`, anteriores às revisões hom acima. Não apresentar os últimos ajustes desses três como já integrados em main ou validados em prd.

A infraestrutura tem [PR #43 develop → main](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/pull/43), integrado em 20/10, merge `31e4bfe273df4146b9f91c3a3681b45c343970d7`, cujo head foi `333e2a8…`. Essa promoção também não comprova execução em prd. Não foram abertos PRs de promoção nem executados deploys.

## Ciclo de destroy já existente

Também foram localizadas execuções **anteriores a esta conferência**, todas aprovadas em hom em 21/10 UTC. O ciclo ocorreu na madrugada, antes da recriação usada na validação pública das 13h UTC. Não representa descarte do ambiente posterior ou prova de seu estado atual.

| Ordem | Execução | Início do primeiro job → término do último, UTC | Etapas conferidas |
|---|---|---|---|
| 1 | [gateway-destroy 39525696921](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/actions/runs/39525696921) | 01:09:59 → 01:11:24 | plan/approval/apply aprovados |
| 2a | [API destroy 39525875521](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions/runs/39525875521) | 01:11:47 → 01:12:08 | Conferir remoção do Gateway; remover overlay/aguardar dependentes; conferir remoção da API aprovados |
| 2b | [auth-destroy 39525882590](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/actions/runs/39525882590) | 01:11:51 → 01:32:57 | Check Gateway removal, SAM delete e Check authentication network removal aprovados |
| 3 | [database-destroy 39528050539](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/actions/runs/39528050539) | 01:33:57 → 01:37:52 | Checagens API/auth aprovadas em plan e apply; plano aprovado aplicado |
| 4 | [base-destroy 39528615997](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/actions/runs/39528615997) | 01:39:52 → 01:53:42 | Check database removal aprovado em plan/apply; remoção Collector/manifestos e aplicação aprovadas |

A ordem temporal respeita Gateway → API/auth → banco → base. Comprova caminho normal desses workflows e passagem das checagens com consumidores removidos; **não comprova rejeição de cada destroy quando houver um consumidor presente**. Não foram baixados logs brutos/planos/estados ou lidos valores de Secrets. Ausência histórica AWS/NAT/EIP também tem a evidência independente da recuperação de 16/10; não se declara uma nova consulta de ausência em 21/10.

## Resultado no checklist

- E6.6: links hom consolidados; permanece parcial somente pelos limites R06/prd, não por falta de implementar ou executar as pipelines hom.
- E6.7: concluída após correção solicitada dos checks obrigatórios de autenticação e conferência das regras efetivas. Revisão final da redação/links dos procedimentos continua E7.
- E6.6b: evidências de OIDC/Job/rede/exportação previamente comprovadas complementadas pelo ciclo normal dos workflows atuais; concluída no escopo hom documentado, sem extrapolar para prd ou nova ausência AWS.
- E6.6a/E6.12: os runs não comprovaram rollback nem corrida de publicação. Posteriormente, o mantenedor dispensou rollback e [as proteções de artefatos foram conferidas](VALIDACAO_PROTECAO_ARTEFATOS_2026-10-21.md) via AWS/IAM/integridade/guards locais. E6.6a concluída no escopo pod privado; E6.12 conserva corrida real não exercitada e limites documentados.
- E6.11: custo/créditos e encerramento final da entrega não ficam automaticamente concluídos por um descarte histórico seguido de recriação.

Metadados detalhados em `artifacts/e6-github-review-20261021/`: quatro snapshots de repositório, quatro arquivos de jobs e summary.json, sem credenciais. PRs consultados em uma página de até cem (todos os encerrados retornados em cada repositório); runs em até cem recentes por repositório, suficientes para as referências selecionadas. Não é auditoria de todo o histórico de execuções. Dezesseis runs tiveram jobs/etapas detalhados conferidos. Não foram inspecionados Secrets nem alterados Environments; a conferência de proteção abrange os rulesets/regras efetivas de branches descritos acima.

Os snapshots/summary.json preservam a leitura inicial, anterior à correção. `auth-protection-change.json` contém o estado aplicado, as regras efetivas posteriores e a comparação de preservação; `auth-protection-saved.png` registra os dois checks salvos. Não foi tentado merge com CI falhando ou push direto para testar a proteção.
