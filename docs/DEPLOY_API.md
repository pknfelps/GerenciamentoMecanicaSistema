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

### Compatibilidade das ordens com o esquema 1.0.0

A API grava em `orders` somente identidade, cliente, veículo, orçamento e status. A abertura vem do primeiro evento de `order_status_history`; a conclusão vem do evento `Finished` com motivo `ServiceCompleted`. A duração continua calculada, sem coluna própria. Os instantes são enviados em UTC, e criação/transição e histórico são gravados na mesma transação, reutilizando a transação do serviço quando existente. Recusa usa `BudgetRejected` e não é tratada como serviço concluído; excluir a ordem preserva o histórico.

Sem conclusão do serviço, `dateFinished` e `duration` agora são `null`, inclusive em recusas; não são substituídos por data mínima ou duração artificial. Instantes iguais de abertura/conclusão podem produzir uma duração real zero. As listagens usam abertura e UUID como desempate, preservando a prioridade operacional. Alterações/exclusão bloqueiam a OS antes da leitura de decisão; estoque bloqueia o material na mesma transação. Falhas revertem todos os efeitos, e notificações são disparadas após a transação da operação.

Esta alteração de nulabilidade afeta os consumidores do contrato. Após o PR, executar `api-deploy` e atualizar o Gateway pelo workflow próprio com o novo commit OpenAPI da API e a revisão de autenticação vigente. Conferir as projeções nulas, o ciclo e as negativas pelo Gateway. Não é necessário alterar ou reinicializar o banco, modificar IAM ou mudar workflows para esta entrega.

### Sanitização dos logs — E4

Os logs de erro da aplicação usam somente a mensagem fixa da operação, o tipo da exceção e propriedades técnicas permitidas. Não enviar o objeto Exception ao logger, nem Message/InnerException/Data/ToString ou stack trace. Notificações não registram CPF/CNPJ, placa, destinatário, assunto ou conteúdo; o diagnóstico não registra o valor do orçamento; SMTP não registra configuração ou credenciais. A execução e o descarte das transações, a propagação das falhas e o tratamento das notificações permanecem iguais.

O middleware registra RequestId gerado pelo servidor, TraceId da atividade atual (ou unavailable), código HTTP, verbo reconhecido (outros: OTHER) e o modelo do endpoint (sem endpoint: unmatched). Não copiar caminho, query, headers ou corpo. Não são criados cabeçalhos nem eventos extras por requisição.

O formato permanece textual/simple. CaptureScopes e IncludeScopes ficam false para não transportar escopos automáticos com RequestPath; a correlação está nas propriedades do próprio evento. Os filtros Information/Warning existentes são preservados. Não há filtro None nem supressão do analisador. Os avisos S6667 ficam visíveis, pois a regra recomenda passar a exceção completa, contrariando a política de sanitização aceita; avaliar o resultado do quality gate sem ocultar o diagnóstico.

Após o PR, executar somente o api-deploy normal. Esta entrega não altera contratos HTTP/OpenAPI, recursos AWS ou configuração funcional do SMTP. Não é necessário reaplicar base, banco, Job ou Gateway para disponibilizar os novos logs. A validação local não comprova a revisão na AWS.

Na conferência posterior, identificar o commit/digest e o pod da nova revisão, delimitar a leitura ao seu início e exercitar uma negativa de negócio e uma notificação com dados fictícios/example.invalid. Conferir mensagem fixa, tipo e IDs técnicos, ausência de stack trace, caminho bruto, documentos, e-mail, tokens e credenciais. Fazer a busca em memória e registrar somente contagens/IDs técnicos, sem reproduzir os valores. Não corromper banco nem induzir indisponibilidade AWS para testar logs. Registros históricos e pendências de Inativo/histórico SQL e E5 permanecem preservados; o envio de e-mail em hom/prd passa à simulação descrita a seguir.

### SMTP local e envio simulado na AWS

Sem Runtime__Environment, o envio SMTP e o smtp4dev do Compose permanecem iguais. Em Runtime hom/prd, a API registra somente LoggingEmailSender para IEmailSender e não lê EmailSettings nem registra ISmtpClient, SmtpConnection ou MailKitEmailSender. O Program fornece à composição o resultado do modo AWS já validado; não há variável nova, leitura de e-mail no SSM/Secrets, seleção por ASPNETCORE_ENVIRONMENT ou fallback SMTP. Campos de Runtime vazios/nulos/inválidos continuam sendo rejeitados na inicialização.

O fluxo de OS e as consultas/montagem das notificações permanecem. O adaptador simulado conclui cada chamada com um único log Information, `Envio de e-mail simulado.`, sem destinatário/assunto/conteúdo. Nenhum e-mail é entregue. MailKit continua no pacote para uso local, sem builds distintos. SMTP funcional na AWS não é mais uma pendência de implementação, por decisão do mantenedor; não confundir simulação com entrega comprovada.

Após PR, executar somente api-deploy; base, banco/Job e Gateway não precisam ser reaplicados para esta alteração. Conferir commit/digest/pod e somente logs a partir da nova revisão, exercitando criação/mudança de estado e orçamento com dados fictícios. Esperar a mensagem de simulação e nenhuma tentativa/falha SMTP ou exposição de dados, com respostas e estado da OS preservados. Não apagar logs históricos nem considerar a revisão anterior como prova. As verificações locais de composição/notificações não comprovam o deploy; Inativo/histórico SQL e E5 continuam pendentes.

## Executar

1. Publicar as alterações via PR nas branches necessárias, incluindo `main` para disponibilizar o disparo manual, conforme a [documentação do GitHub](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow).
2. Em **Actions → API deploy → Run workflow**, selecionar branch `develop` e input `environment=hom`. Para produção, usar branch `main` e `environment=prd` após provisionar as dependências de prd.
3. Acompanhar os dois jobs e guardar o link da execução e seu resumo com commit, tag e URI por digest.

Cada job faz checkout do SHA da execução, mesmo que a branch avance enquanto o workflow estiver rodando. As execuções deste workflow são agrupadas por ambiente com `cancel-in-progress: false`; o mantenedor continua coordenando operações entre repositórios. A configuração padrão de concurrency do GitHub admite somente uma execução pendente por grupo: um novo disparo pode substituir outro que ainda não começou. Evitar disparos duplicados. [Semântica da concurrency](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency).

**Publicar:** restore/build da solução `.slnx` em Release com .NET 10 e `-p:OpenApiGenerateDocuments=true`, testes, autenticação OIDC e conferência STS/SSM. Publica OpenAPI/checksum no S3 antes do build Docker `linux/amd64` e push no ECR. A tag é `sha-<commit completo>-<run_id>-<run_attempt>`. O digest retornado pela action de build é entregue ao job seguinte; a imagem não é reconstruída. O CI de PR, cobertura e Sonar permanecem separados.

### Publicação do contrato

`Microsoft.Extensions.ApiDescription.Server 10.0.9` gera somente `v1`, em OpenAPI 3.0, como `artifacts/openapi/openapi.json`. A geração é optativa; builds comuns/Docker permanecem desabilitados por padrão. O `Program` reconhece o entry assembly oficial `GetDocument.Insider` e registra somente controllers/OpenAPI, sem carregar AWS, configurar credenciais JWT ou registrar persistência. A inicialização normal mantém todas as validações. Development serve o mesmo formato 3.0.

O contrato tem título **API da oficina** e versão **1.0.0**, sem destinos AWS, extensões de integração ou exemplos de tokens. Bearer JWT é descrito por operação com `Authorize`, inclusive herdado do controller; `AllowAnonymous` prevalece. Não há exigência global. Autenticação interna e health checks são públicos. Status exige Mechanic/Customer e orçamento exige Customer, com vínculo da própria OS. `/customers/validate` continua no contrato da Lambda.

No bucket `mecanica-artifacts-121754142617-us-east-1`, a revisão da execução determina as chaves:

| Objeto | Chave |
|---|---|
| Contrato | `contracts/api/<commit>/openapi.json` |
| Checksum | `contracts/api/<commit>/openapi.json.sha256` |

`sha256sum openapi.json` produz o sidecar com hash e nome relativo. A AWS CLI envia os dois arquivos com proprietário esperado, checksum SHA-256 e `--if-none-match '*'`. Em `PreconditionFailed`, baixa o objeto existente e compara os bytes; divergência ou qualquer outro erro interrompe o fluxo. Repetir após um upload parcial aceita o arquivo já publicado somente se idêntico. O commit não tem prefixo de ambiente: hom/prd compartilham o mesmo contrato da revisão. Uma alteração de conteúdo exige nova revisão.

As permissões atuais da role API já cobrem leitura/escrita em `contracts/api`; não há novo IAM, Secret de pipeline ou parâmetro SSM. O resumo registra commit, chaves, hashes, versões S3 e digest. Contratos publicados permanecem se o build da imagem ou implantação falhar e também após API destroy. Sua existência representa um artefato disponível, sem declarar prontidão operacional.

**Implantar:** nova autenticação OIDC, leitura de cluster/namespace e confirmação do digest com `BatchGetImage`. Instala kubectl `1.36.1` e Kustomize `5.8.3`; no checkout descartável do runner, substitui a imagem provisória com o comando nativo:

```bash
cd deploy/kubernetes/overlays/hom
kustomize edit set image "felipejesusoliveira/gerenciamentomecanicasistema=121754142617.dkr.ecr.us-east-1.amazonaws.com/mecanica/api@sha256:<digest>"
kubectl apply -k .
kubectl rollout status deployment/deploy-gerenciamento-api --namespace default --timeout=600s
```

O workflow confere a imagem desejada do container `gerenciamento-api` no Deployment. Não gera um manifesto intermediário e não altera os manifestos versionados: `:pending` permanece no repositório, para exigir a seleção da imagem em cada deploy. ServiceAccount, HPA, probes e demais ajustes vêm do overlay. A identidade EKS da pipeline está restrita ao namespace `default`; o Service pertence à base.

## Falhas e nova tentativa

Falha de geração/teste/autenticação/configuração/publicação impede o job de implantação. Digest ausente ou configuração inesperada impede o apply. Falha de apply/rollout/conferência encerra a execução com erro e mostra Deployment, pods, últimos eventos e logs atuais/anteriores limitados. Não há rollback automático. Uma imagem publicada pode permanecer no ECR mesmo quando a implantação falha; nenhuma retenção ECR é alterada.

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

O ciclo hom de deploy/conexão/descarte foi validado anteriormente. A publicação do OpenAPI foi validada localmente em 12/10/2026: restore/build Release, 600 testes aprovados (incluindo PostgreSQL em Docker e 16 testes do documento), geração sem AWS/banco com Runtime hom e configurações locais inválidas, repetição com bytes idênticos, build da imagem e actionlint/ShellCheck. O contrato gerado é OpenAPI 3.0.4; SHA-256 local `60885f42d4c54ee600ee20ad08065bb1e06484319846d44c6a6dfdadb381d59b`. Não houve upload ou deploy AWS nesta entrega.

Após os PRs, o mantenedor executará API deploy em hom. Conferir no resumo as duas chaves e versões S3; baixar contrato/sidecar com AWS CLI e executar `sha256sum --check openapi.json.sha256` na pasta dos arquivos. Conferir também metadado `commit` por `aws s3api head-object` e sua correspondência com o SHA e a tag/digest da imagem implantada. Aplicar proprietário esperado `121754142617` nas consultas S3. Conferência AWS concluída em 12/10/2026 para c96e86d840f35616f1dcc666f45269233e831896, execução 38262895558: dois objetos/checksums/metadados corretos, digest ECR igual ao Deployment/pod, startup/live/ready 200 Healthy e target NLB healthy. Pod Ready sem reinícios e HPA com métricas válidas. Não houve alterações de recursos pelo agente. Parâmetros SSM do componente API, Gateway, integração HTTP, aceitação real do JWT da Lambda pela API e validação prd continuam nas etapas posteriores; E3.11 permanece aberta. Este workflow não provisiona AWS nem inicializa o banco.

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

## Integração com o Gateway

Após publicar/implantar a API, informar seu commit completo em api_commit do workflow gateway-provision no repositório de infraestrutura. auth_commit corresponde à autenticação implantada; ambos fixam contratos publicados. Gateway tem estado/workflow próprio e não é disparado pelo api-deploy. Mudanças de contrato exigem novo plano Gateway. A rede exige novo SG reservado na base, conforme [ADR 010](arquitetura/adrs/010-GATEWAY-REST-V2.md). Descartar Gateway antes da API/auth e antes de banco/base.
