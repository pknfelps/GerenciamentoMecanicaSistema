# E1.2 — Distribuição de código e artefatos

[Plano](../../PLANO_FASE_3.md) · [Etapa E1](etapas/E1.md) · [Inventário de origem](INVENTARIO_E1.md)

**2026-09-17:** separação implementada e validada na branch `codex/e1-2-separacao` dos quatro repositórios. Integração nas branches principais ainda pendente; não houve merge nem deploy AWS.

**Ajuste em 2026-09-18:** a pedido do usuário, removidos da API o SQL local, manifesto e sincronizador; banco local passa a ser preparado manualmente. Restaurado build da imagem no CI, mantendo publicação ECR/deploy pendentes.

## Resultado por repositório

| Repositório / branch para revisão | Resultado |
|---|---|
| [Sistema](https://github.com/pknfelps/GerenciamentoMecanicaSistema/tree/codex/e1-2-separacao) | Camadas, testes, Dockerfile, Compose e Deployment/HPA preservados. Banco local manual, sem SQL na API; CI com testes/Sonar/build de imagem; publicação/deploy pendentes |
| [Infraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/tree/codex/e1-2-separacao) | 12 arquivos transferidos: Terraform/lock, Service da API e Metrics Server opcional; Kustomize inclui somente o Service. Commit `b8aaf52acd9347582fdef19ce4a383d476610761` |
| [BancoDados](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/tree/codex/e1-2-separacao) | SQL como fonte canônica e quatro manifestos antigos PostgreSQL/EBS arquivados. Commit `64a1a38abfff425abea91fab1491df62a0a2ff2a` |
| [Autenticacao](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/tree/codex/e1-2-separacao) | README de escopo, gitignore e atributos de texto. Não havia função a transferir. Commit `51d96300ad1dc41d09c9b790b0bd0b5f13cc76dd` |

Clones locais ficam em pastas irmãs, sob `C:/Users/felip/source/repos`. Nenhuma camada .NET foi movida para infraestrutura, banco ou autenticação. A biblioteca CPF/JWT ainda será extraída em E1.9.

Infraestrutura e banco registram cada origem/destino/hash em `docs/origem-fase2.json`, referenciando o commit `bd40b14eb90818616533b0ea6043973f240cd0c3` da aplicação. Conteúdo transferido preservado, com normalização apenas de BOM e fim de linha. Arquivos locais tfstate/tfvars e caches não foram transferidos nem apagados; verificar seu vínculo com recursos antes de aplicar Terraform de outro diretório.

## Propriedade do SQL e uso local opcional

O esquema/seeds fica exclusivamente em `sql/Init.sql` no repositório de banco. Em 2026-09-18, o usuário rejeitou a cópia local inicialmente criada na E1.2. Foram removidos `deploy/local/database/`, `scripts/Sync-DatabaseSnapshot.ps1`, sua verificação no CI e a montagem automática de SQL no Compose.

O foco é consumir a API na AWS. Se Docker local for necessário, preparar o banco manualmente a partir do repositório responsável antes de usar a API. O Compose continua disponível, sem recriar esquema ou alterar volumes automaticamente. A inicialização AWS por Job/pipeline do banco permanece prevista em E2.

## Pipeline e recursos de nuvem

A pipeline executa testes/cobertura/Sonar e build da imagem em pushes para main/develop, PRs destinados a essas branches e acionamento manual. O build depende de testes e análise aprovados. Jobs de publicação Docker Hub e deploy com chaves AWS foram retirados durante a separação. Publicação ECR exige infraestrutura, role OIDC e parâmetros de ambiente das próximas etapas; nenhum desses gatilhos publica ou implanta recursos nesta branch.

Terraform transferido, Service e imagem de Deployment ainda refletem a base da Fase 2. Rede privada, NLB interno, Aurora, ECR/OIDC, Gateway e entregas hom/prd serão implementados nas etapas previstas. Manifestos antigos do PostgreSQL ficam em `legacy/kubernetes/` no banco, fora da composição ativa. O Metrics Server alternativo não deve ser aplicado junto com o add-on existente.

## Validação realizada

- 17 arquivos comparados com a origem, normalizando BOM/LF; hashes de destino conferidos.
- Deployment/HPA, probes, recursos e referências de projetos preservados.
- `kubectl kustomize` renderizou a composição da aplicação (Deployment/HPA) e da infraestrutura (Service).
- `docker compose --env-file .env.example config --quiet` passou na separação inicial.
- Em 2026-09-18, configuração Compose revalidada após remoção do SQL automático. A primeira tentativa de build foi bloqueada pela inicialização do Docker Desktop; após reinício pelo usuário, o build local passou. Comando: `docker build --file GerenciamentoMecanicaSistema/Dockerfile --tag gerenciamento-mecanica-api:e1-2-review .`. Imagem `linux/amd64` confirmada via `docker image inspect`; log em `output/e1-2/build-image-review.log` (não versionado). Quatro avisos existentes de análise estática (três S2139 e um S1118), sem erros. `.dockerignore` ajustado para excluir caches/configurações Terraform e ferramentas/relatórios locais. Nenhuma alteração no código .NET, sem repetição da suíte de 468 testes nesta revisão. Esse resultado valida o build local, não comprova execução remota do novo job nem funcionamento integrado da API/banco.
- A sincronização e integridade do snapshot SQL passaram na implementação inicial; essa solução foi removida por decisão posterior do usuário em 2026-09-18, não é requisito da versão atual.
- Build da solução realizado; suíte completa existente: **468 testes aprovados** (142 domínio, 152 serviço, 108 controllers, 66 infraestrutura), sem falhas. Resultado local em `output/e1-2/full-test-results/` (não versionado).
- Testes inicialmente afetados pelo acesso ao Event Log no sandbox e Docker desligado; execução final fora do sandbox, com Docker Desktop iniciado, passou. Não foram alteradas regras da aplicação para contornar essas condições.
- Terraform não foi executado: binário indisponível no ambiente. A conferência desta transferência compara os arquivos originais; validação de providers/recursos permanece em E2. Não houve provisionamento, teste de carga ou deploy de aplicação.

## Revisão e integração

Em 2026-09-18, após autorização explícita do usuário para usar sua sessão autenticada no navegador, os quatro PRs foram criados e verificados como **rascunhos**. O conector continuou retornando `403`; a restrição não foi alterada. A pendência P06 foi resolvida pelo caminho autorizado, sem merge ou ativação de auto-merge.

| Repositório | PR | Base ← branch de trabalho | Estado na criação |
|---|---|---|---|
| Sistema | [PR #1](https://github.com/pknfelps/GerenciamentoMecanicaSistema/pull/1) | develop ← codex/e1-2-separacao | Rascunho |
| Infraestrutura | [PR #1](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/pull/1) | main ← codex/e1-2-separacao | Rascunho |
| BancoDados | [PR #1](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/pull/1) | main ← codex/e1-2-separacao | Rascunho |
| Autenticacao | [PR #1](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/pull/1) | main ← codex/e1-2-separacao | Rascunho |

O PR da aplicação usa develop existente; nos três novos repositórios, main é a única base inicial. Configuração definitiva de branches/ambientes segue em E1.5. As descrições registram escopo e validações locais; checks remotos não foram considerados aprovados apenas pela criação dos PRs.

Na revisão de 2026-09-18, API e banco já estavam marcados pelo usuário como prontos para revisão; esse estado foi preservado. As descrições foram ajustadas para refletir SQL exclusivo do banco, preparação local manual e build de imagem no CI da API.

Na integração, disponibilizar primeiro os artefatos do banco/infraestrutura e depois a atualização da aplicação; manter os gatilhos sem publicação/deploy até a implementação das pipelines da Fase 3. E1.3–E1.9 continuam pendentes; READMEs introdutórios e ajustes mínimos de CI não concluem a governança de entrega.
