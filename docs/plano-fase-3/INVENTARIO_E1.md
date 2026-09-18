# E1 — Inventário inicial e preparação da separação

[Plano](../../PLANO_FASE_3.md) · [Tarefas da E1](etapas/E1.md) · [Registro de repositórios](ACOMPANHAMENTO.md)

**Verificação e criação em 2026-09-17.** Inventário local e metadados consultados pelo conector GitHub; três repositórios criados pela interface, cada um inicializado apenas com README. Não houve deploy, push do checkout local ou movimentação de código.

Este é o registro da situação anterior à separação. A distribuição posterior e a pipeline atualizada estão no [resultado da E1.2](RESULTADO_E1_2.md); caminhos e comportamentos abaixo descrevem a origem.

## Repositórios e acesso

- Conta autenticada no conector: `pknfelps`, com permissão administrativa no repositório atual.
- Origin local confirmado: [pknfelps/GerenciamentoMecanicaSistema](https://github.com/pknfelps/GerenciamentoMecanicaSistema), público, não arquivado, branch padrão remota `main`.
- Checkout local: `develop`, commit `bd40b14` (`Correções finais`). Plano e pasta docs ainda aparecem como arquivos não rastreados no Git; preservar e incluir em uma futura revisão, sem confundir documentação local com publicação remota.
- Os três novos nomes inicialmente retornaram HTTP 404 pelo conector; sua disponibilidade foi confirmada no formulário e os repositórios foram criados após o usuário autenticar o navegador integrado.
- O usuário confirmou **repositórios públicos na conta pknfelps**. Criação concluída pela interface; conector confirmou os três como públicos, main padrão, não arquivados e com acesso administrativo. Login no conector e sessão no navegador são independentes; o bloqueio inicial de login foi resolvido.
- Proteções de branches, GitHub Environments, secrets configurados e execuções reais das pipelines ainda não foram verificados. Não inferir essas configurações a partir da permissão administrativa.

| Novo repositório | ID GitHub | Commit inicial com README |
|---|---|---|
| [GerenciamentoMecanicaAutenticacao](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao) | 1375105446 | [5ee41c0](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao/commit/5ee41c025bcd26d07ec67c634cc37250dde9231a) |
| [GerenciamentoMecanicaInfraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura) | 1375105948 | [1a3b285](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/commit/1a3b28579127a0c047f052e4e35b7965eec65fb3) |
| [GerenciamentoMecanicaBancoDados](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados) | 1375106298 | [966594d](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/commit/966594d0f2c122d48411591b53a8e1a8635ac88b) |

Os READMEs iniciais contêm nome e descrição; não concluem E1.3. Não foram criados workflows ou recursos AWS nesses repositórios.

## Artefatos existentes e destino da E1.2

| Origem atual | Destino / tratamento |
|---|---|
| `deploy/terraform/` (rede, EKS, IAM, add-ons, outputs, versões e lock de providers) | Infraestrutura: reaproveitar como base, adaptando rede privada, ambientes e backend S3 em E2; não aplicar a configuração antiga como arquitetura final |
| `deploy/kubernetes/svc-gerenciamento-api.yaml` | Infraestrutura: Service responsável pela entrada/NLB, integrado ao controller; evitar uma segunda cópia gerenciada pela aplicação |
| `deploy/kubernetes/metrics-server.yaml` e recursos de plataforma necessários | Infraestrutura: consolidar propriedade dos add-ons e preservar Metrics Server para HPA |
| `deploy/kubernetes/deploy-gerenciamento-api.yaml` e `hpa-gerenciamento-api.yaml` | Aplicação: preservar probes/recursos/HPA; parametrizar imagem ECR, dependências e ambientes |
| `deploy/kubernetes/DbEntrypoint/Init.sql` | Banco: base para esquema/seeds Aurora, novo histórico e Job de inicialização; manter estratégia de execução local funcional |
| Deployment/Service/PVC do PostgreSQL e StorageClass EBS no Kustomize atual | Retirar da composição de nuvem quando Aurora estiver integrado; avaliar dependências antes de remover recursos/add-ons de armazenamento |
| `deploy/kubernetes/kustomization.yaml` | Separar composição de aplicação e plataforma; hoje combina API, banco, armazenamento e script de inicialização |
| `.github/workflows/pipeline.yml` | Aplicação: separar responsabilidades e adaptar CI/deploy; novos repositórios terão pipelines próprias |
| `GerenciamentoMecanicaSistema/Dockerfile` e `docker-compose.yaml` | Permanecem na aplicação; preservar execução local com PostgreSQL |
| `Domain/Customer/Cpf.cs` e `Infrastructure/Authentication/JwtTokenGenerator.cs` | Fontes para extração controlada de regras em E1.9; não copiar o domínio inteiro para a função |
| Lambda, Terraform Aurora e unidade Gateway | Novos componentes; não há implementação a mover no inventário de deploy atual |
| `PLANO_FASE_3.md` e `docs/` | Permanecem como fonte central na aplicação; os demais READMEs devem referenciá-los |

Essa tabela prepara a distribuição, mas não conclui E1.2. Cada transferência precisa atualizar referências e preservar o build; não apagar a origem antes de validar o destino.

## Pipeline local: diferenças em relação à fase nova

A [pipeline existente](../../.github/workflows/pipeline.yml) executa testes/cobertura, Sonar, build/push Docker e deploy EKS. O gatilho `push` não possui filtro de branch e também existe `workflow_dispatch`; publicar qualquer branch pode acionar o fluxo atual. A separação de CI/deploy e os gatilhos por ambiente precisam ser tratados antes de usar pushes de implantação da Fase 3.

Hoje a imagem usa Docker Hub; AWS usa chaves referenciadas por secrets do GitHub; o cluster tem nome fixo `api-cluster`; o deploy cria um Secret Kubernetes e usa o PostgreSQL interno `svc-gerenciamento-db`. Não há `environment: hom/prd` nesse workflow. E1/E2/E3 adaptarão esses pontos para ECR, OIDC, Secrets Manager, Aurora e componentes independentes. Não foi executada pipeline nem lido valor de secret nesta verificação.

O [Terraform na origem inventariada](https://github.com/pknfelps/GerenciamentoMecanicaSistema/blob/bd40b14eb90818616533b0ea6043973f240cd0c3/deploy/terraform/versions.tf) declara `~> 1.15.0` e provider AWS `~> 6.0`; isso é inventário do arquivo, não comprovação de compatibilidade da futura configuração.

## Continuação após E1.1

E1.1 concluída com os quatro repositórios identificados. Próxima tarefa: E1.2, distribuição controlada conforme a tabela acima, seguida da documentação e governança das demais tarefas da E1. Antes de publicar mudanças operacionais, ajustar os gatilhos atuais para evitar deploy inadvertido. Arquivos locais de credenciais e estado Terraform não fazem parte da distribuição.
