# E1.2 — Distribuição de código e artefatos

[Plano](../../PLANO_FASE_3.md) · [Etapa E1](etapas/E1.md) · [Inventário de origem](INVENTARIO_E1.md)

**2026-09-17:** separação implementada e validada na branch `codex/e1-2-separacao` dos quatro repositórios. Integração nas branches principais ainda pendente; não houve merge nem deploy AWS.

## Resultado por repositório

| Repositório / branch para revisão | Resultado |
|---|---|
| [Sistema](https://github.com/pknfelps/GerenciamentoMecanicaSistema/tree/codex/e1-2-separacao) | Camadas, testes, Dockerfile, Compose e Deployment/HPA preservados. SQL local com origem/hash; pipeline temporariamente somente CI; plano e arquitetura versionados |
| [Infraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/tree/codex/e1-2-separacao) | 12 arquivos transferidos: Terraform/lock, Service da API e Metrics Server opcional; Kustomize inclui somente o Service. Commit `b8aaf52acd9347582fdef19ce4a383d476610761` |
| [BancoDados](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/tree/codex/e1-2-separacao) | SQL como fonte canônica e quatro manifestos antigos PostgreSQL/EBS arquivados. Commit `64a1a38abfff425abea91fab1491df62a0a2ff2a` |
| [Autenticacao](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/tree/codex/e1-2-separacao) | README de escopo, gitignore e atributos de texto. Não havia função a transferir. Commit `51d96300ad1dc41d09c9b790b0bd0b5f13cc76dd` |

Clones locais ficam em pastas irmãs, sob `C:/Users/felip/source/repos`. Nenhuma camada .NET foi movida para infraestrutura, banco ou autenticação. A biblioteca CPF/JWT ainda será extraída em E1.9.

Infraestrutura e banco registram cada origem/destino/hash em `docs/origem-fase2.json`, referenciando o commit `bd40b14eb90818616533b0ea6043973f240cd0c3` da aplicação. Conteúdo transferido preservado, com normalização apenas de BOM e fim de linha. Arquivos locais tfstate/tfvars e caches não foram transferidos nem apagados; verificar seu vínculo com recursos antes de aplicar Terraform de outro diretório.

## SQL local e execução independente

O Compose usa `deploy/local/database/Init.sql`. Essa cópia deriva de `sql/Init.sql` do banco, fixada ao commit acima em [source.json](../../deploy/local/database/source.json), com SHA-256. O [script de sincronização](../../scripts/Sync-DatabaseSnapshot.ps1) importa um commit do clone local; `-Verify` permite validar a cópia sem outro checkout. A origem deve ser alterada primeiro, seguida de uma atualização explícita do snapshot na aplicação.

O CI executa essa verificação antes do build. Não há leitura dinâmica de main, download durante startup, alteração de schema ou dependência de rede adicionada ao Compose. Isso atende ao desenvolvimento local; contratos de entrega AWS via S3/SSM seguem nas tarefas próprias.

## Pipeline e recursos de nuvem

A pipeline da aplicação agora executa testes/cobertura/Sonar em pushes para main/develop, PRs destinados a essas branches e acionamento manual. Jobs de publicação Docker Hub e deploy com chaves AWS foram retirados durante a separação. Nenhum desses gatilhos implanta recursos nesta branch.

Terraform transferido, Service e imagem de Deployment ainda refletem a base da Fase 2. Rede privada, NLB interno, Aurora, ECR/OIDC, Gateway e entregas hom/prd serão implementados nas etapas previstas. Manifestos antigos do PostgreSQL ficam em `legacy/kubernetes/` no banco, fora da composição ativa. O Metrics Server alternativo não deve ser aplicado junto com o add-on existente.

## Validação realizada

- 17 arquivos comparados com a origem, normalizando BOM/LF; hashes de destino conferidos.
- Deployment/HPA, probes, recursos e referências de projetos preservados.
- `kubectl kustomize` renderizou a composição da aplicação (Deployment/HPA) e da infraestrutura (Service).
- `docker compose --env-file .env.example config --quiet` passou; caminho e conteúdo do SQL local conferidos.
- Sincronização por commit e `Sync-DatabaseSnapshot.ps1 -Verify` passaram.
- Build da solução realizado; suíte completa existente: **468 testes aprovados** (142 domínio, 152 serviço, 108 controllers, 66 infraestrutura), sem falhas. Resultado local em `output/e1-2/full-test-results/` (não versionado).
- Testes inicialmente afetados pelo acesso ao Event Log no sandbox e Docker desligado; execução final fora do sandbox, com Docker Desktop iniciado, passou. Não foram alteradas regras da aplicação para contornar essas condições.
- Terraform não foi executado: binário indisponível no ambiente. A conferência desta transferência compara os arquivos originais; validação de providers/recursos permanece em E2. Não houve provisionamento, teste de carga ou deploy de aplicação.

## Revisão e integração

As branches permitem revisar o conjunto sem merge automático. A criação dos PRs pelo conector retornou `403 Resource not accessible by integration` nos três novos repositórios. A revisão automática bloqueou a tentativa de usar o navegador após essa negativa; uso desse caminho depende de confirmação específica do usuário. Nenhum PR foi criado nesta execução.

Na integração, disponibilizar primeiro os artefatos do banco/infraestrutura e depois a atualização da aplicação; manter o snapshot fixo e os gatilhos sem deploy até a implementação das pipelines da Fase 3. E1.3–E1.9 continuam pendentes; READMEs introdutórios e ajustes mínimos de CI não concluem a governança de entrega.
