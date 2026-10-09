# Publicação e deploy manual da API — E2.11

O [workflow API deploy](../.github/workflows/api-deploy.yml) executa **Publicar → Implantar**. O disparo manual autoriza o fluxo completo, sem job de aprovação. Os Environments existentes `hom` e `prd` fornecem OIDC e variáveis; não acrescentar revisores obrigatórios nesses Environments para este fluxo. Provisionamento Terraform de infra/banco continua com revisão do plano salvo e aprovação próprias.

## Pré-requisitos

- Workflow publicado também na branch padrão `main`, para aparecer **Run workflow**. Manter a versão que será executada em `develop` para hom e `main` para prd. A primeira execução será `develop/hom`.
- Environment com `AWS_REGION=us-east-1` e `AWS_ROLE_ARN=arn:aws:iam::121754142617:role/mecanica/pipelines/mecanica-<ambiente>-api-github`. Não são necessárias chaves AWS, senhas de banco ou JWT nas variáveis/secrets do GitHub.
- Base e banco do ambiente provisionados: ECR compartilhado com tags imutáveis, EKS/acesso da pipeline, Service NodePort, Pod Identity Agent, associação `default/gerenciamento-api`, policies de leitura JWT/banco e parâmetros SSM v2. Schema, seeds e usuário `mecanica_api` devem estar inicializados. O sucesso do deploy da infra hom foi informado pelo mantenedor; a conexão real do runtime ainda requer validação.

O workflow exige conta `121754142617`, região `us-east-1`, repositório GitHub `pknfelps/GerenciamentoMecanicaSistema`, role exata do ambiente e combinação `develop/hom` ou `main/prd`. Consulta somente três parâmetros públicos da base:

| Parâmetro `/mecanica/<ambiente>/base/v2/` | Valor exigido |
|---|---|
| `ecr-repository-url` | `121754142617.dkr.ecr.us-east-1.amazonaws.com/mecanica/api` |
| `cluster-name` | `mecanica-<ambiente>-eks` |
| `namespace` | `default` |

A aplicação lê seus parâmetros e Secrets usando o SDK .NET e Pod Identity, conforme [inicialização AWS](INICIALIZACAO_AWS.md). A pipeline não lê seus valores.

## Executar

1. Publicar as alterações via PR nas branches necessárias, incluindo `main` para disponibilizar o disparo manual, conforme a [documentação do GitHub](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow).
2. Em **Actions → API deploy → Run workflow**, selecionar branch `develop` e input `environment=hom`. Para produção, usar branch `main` e `environment=prd` após provisionar as dependências de prd.
3. Acompanhar os dois jobs e guardar o link da execução e seu resumo com commit, tag e URI por digest.

Cada job faz checkout do SHA da execução, mesmo que a branch avance enquanto o workflow estiver rodando. As execuções deste workflow são agrupadas por ambiente com `cancel-in-progress: false`; o mantenedor continua coordenando operações entre repositórios. A configuração padrão de concurrency do GitHub admite somente uma execução pendente por grupo: um novo disparo pode substituir outro que ainda não começou. Evitar disparos duplicados. [Semântica da concurrency](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency).

**Publicar:** restore/build/test da solução `.slnx` em Release com .NET 10, autenticação OIDC, conferência STS/SSM, build Docker `linux/amd64` e push no ECR. A tag é `sha-<commit completo>-<run_id>-<run_attempt>`. O digest retornado pela action de build é entregue ao job seguinte; a imagem não é reconstruída. O CI de PR, cobertura e Sonar permanecem separados.

**Implantar:** nova autenticação OIDC, leitura de cluster/namespace e confirmação do digest com `BatchGetImage`. Instala kubectl `1.36.1` e Kustomize `5.8.3`; no checkout descartável do runner, substitui a imagem provisória com o comando nativo:

```bash
cd deploy/kubernetes/overlays/hom
kustomize edit set image "felipejesusoliveira/gerenciamentomecanicasistema=121754142617.dkr.ecr.us-east-1.amazonaws.com/mecanica/api@sha256:<digest>"
kubectl apply -k .
kubectl rollout status deployment/deploy-gerenciamento-api --namespace default --timeout=600s
```

O workflow confere a imagem desejada do container `gerenciamento-api` no Deployment. Não gera um manifesto intermediário e não altera os manifestos versionados: `:pending` permanece no repositório, para exigir a seleção da imagem em cada deploy. ServiceAccount, HPA, probes e demais ajustes vêm do overlay. A identidade EKS da pipeline está restrita ao namespace `default`; o Service pertence à base.

## Falhas e nova tentativa

Falha de teste/autenticação/configuração/publicação impede o job de implantação. Digest ausente ou configuração inesperada impede o apply. Falha de apply/rollout/conferência encerra a execução com erro e mostra Deployment, pods, últimos eventos e logs atuais/anteriores limitados. Não há rollback automático. Uma imagem publicada pode permanecer no ECR mesmo quando a implantação falha; nenhuma retenção ECR é alterada.

Corrigir a causa e iniciar nova execução. Ao repetir apenas o job de implantação na mesma execução, ele reutiliza os outputs da publicação; ao repetir a publicação, a tag inclui o novo `run_attempt`. Não editar/reutilizar tags imutáveis. Diagnósticos não devem incluir consultas a valores dos Secrets, dumps de configuração ou strings de conexão.

## Validação após o primeiro deploy

A implementação é validada localmente com actionlint e renderização da base/hom/prd, incluindo substituição por digest em cópia temporária. A execução AWS será disparada pelo mantenedor. Somente após ela é possível comprovar Pod Identity, acesso RDS, readiness e capacidade:

```bash
aws eks update-kubeconfig --name mecanica-hom-eks --region us-east-1
kubectl get pods --namespace default -l app=gerenciamento-api -o wide
kubectl get deployment deploy-gerenciamento-api --namespace default -o jsonpath='{.spec.template.spec.containers[?(@.name=="gerenciamento-api")].image}'
kubectl get pods --namespace default -l app=gerenciamento-api -o jsonpath='{.items[*].status.containerStatuses[*].imageID}'
kubectl describe deployment deploy-gerenciamento-api --namespace default
kubectl get hpa --namespace default
kubectl top pods --namespace default
kubectl top nodes
kubectl port-forward --namespace default deployment/deploy-gerenciamento-api 8080:8080
```

Em outro terminal, `curl --fail http://localhost:8080/health/startup`, `/health/live` e `/health/ready`. A prontidão verifica conexão com PostgreSQL; conferir também os pods Ready, reinícios, probes e digest em execução. Para prd, trocar o cluster e confirmar o ambiente.

Para NLB, ler `nlb-arn` da base v2 e usar `aws elbv2 describe-target-groups --load-balancer-arn <arn>` e `aws elbv2 describe-target-health --target-group-arn <arn>`. Targets devem estar saudáveis na porta `30080`. O NLB é interno: conferir `/health/ready` por seu DNS a partir de uma origem com acesso à VPC, usando `curl --fail`. Essas verificações usam credenciais de operador com as permissões correspondentes; não ampliar a role da pipeline para executá-las. Ajustes de recursos/réplicas/HPA pertencem à E2.7.

E2.6/E2.11 permanecem abertas até comprovação do deploy e conexão reais. OpenAPI, parâmetros SSM do componente API, Gateway, integração Lambda e validação prd continuam nas etapas posteriores. Este workflow não provisiona AWS nem inicializa o banco.

## Réplicas e validação de capacidade

O HPA mantém mínimo de uma e máximo de três réplicas em base/hom/prd; o Deployment inicia com uma réplica e o HPA ajusta conforme as métricas. A alteração deve ser publicada e aplicada pelo workflow API deploy. Três é o limite escolhido para a configuração atual de um nó t3.small, considerando as reservas de memória observadas na validação hom de 08/10; não representa capacidade comprovada sob carga. Por decisão do mantenedor, não serão realizados testes de carga com a API hospedada na AWS. Acompanhar métricas, probes, eventos e pods pendentes durante o uso.

## Descartar o ambiente

O [workflow API destroy](../.github/workflows/api-destroy.yml) remove o consumidor antes do banco, mantendo o descarte completo acessível por workflows. Tem um único job manual, sem aprovação intermediária; o disparo autoriza a remoção. Exige develop/hom ou main/prd, autentica por OIDC no Environment existente e valida repositório, conta, região, role, cluster e namespace default. Reutiliza as permissões SSM/EKS da API. Publicar o arquivo também em main para disponibilizar Run workflow.

Com kubectl 1.36.1, executa `kubectl delete -k` do overlay, ignora somente recursos já ausentes e aguarda até 600 segundos com cascade foreground para remover também ReplicaSets/pods dependentes. Confere ausência de Deployment/HPA/ServiceAccount e de pods com o seletor da API. Falhas de acesso, SSM ou cluster não são tratadas como sucesso; em falha Kubernetes, mostra recursos/eventos limitados. Repetir com o cluster existente e overlay já removido é permitido. API deploy e API destroy compartilham o mesmo grupo de concurrency por ambiente, sem cancelar a execução em curso.

O workflow remove apenas os objetos do overlay, sem Terraform, leitura de Secrets ou exclusão de recursos AWS. Service/NLB, RDS, JWT/credenciais, IAM e ECR permanecem com seus proprietários. Para descartar todo o ambiente:

1. Interromper novos disparos de deploy/provisionamento do ambiente e aguardar as execuções em curso.
2. Executar **API destroy** neste repositório, selecionando develop/hom (ou main/prd), e aguardar sucesso. Deployment, HPA, ServiceAccount e pods dependentes são removidos; Service/NLB continuam pertencendo à base.
3. Executar **database-destroy** no repositório de banco, no mesmo ambiente: revisar e aprovar seu plano salvo. Ele remove o Job e descarta RDS/configuração/credenciais administrados pelo Terraform.
4. Após concluir o banco, executar **base-destroy** no repositório de infraestrutura, revisando/aprovando o plano salvo. Ele remove seus manifestos Kubernetes antes de excluir EKS/rede/NLB e demais recursos da base.

A ordem é **api-destroy → database-destroy → base-destroy**, coordenada pelo mantenedor; não há disparo automático entre repositórios. Executar API destroy antes de remover a base: ele depende dos parâmetros SSM e do cluster existentes. Para remover somente a aplicação, executar apenas API destroy; API deploy pode implantá-la novamente usando as dependências preservadas. Bootstrap, backend, repositório ECR/imagens e o outro ambiente são preservados. Se Gateway/Lambda ou outros consumidores forem implantados depois, eles devem ser descartados antes do banco/base conforme seus procedimentos. Nenhum workflow foi disparado para validar esta implementação.
