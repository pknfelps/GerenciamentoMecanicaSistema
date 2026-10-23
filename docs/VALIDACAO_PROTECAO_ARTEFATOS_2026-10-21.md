# Proteção dos artefatos — 21/10/2026

**2026-10-21 — Escopo final da E6 definido pelo mantenedor:** não serão testadas publicação concorrente (E6.12) nem coexistência hom/prd (E6.5a); análise da diferença workflows manuais/R06 adiada. Esses itens encerrados no escopo atual por decisão, sem afirmar validação dos cenários omitidos ou atendimento integral do requisito original. Quinze itens encerrados por comprovação ou decisão de escopo; somente E6.11 aguarda descarte final e custos/créditos. O mantenedor executará a destruição; nenhuma execução autorizada ao agente por esta declaração. Rollback permanece dispensado. [Checklist atualizado](plano-fase-3/etapas/E6.md).

**Conferência concluída às 14:55 de São Paulo / 17:55 UTC.** Leitura AWS, revisão dos workflows e avaliação local de condições originais com Terraform 1.15.9. Não houve PutObject, DeleteObject, PutImage, publicação, workflow, provisionamento, alteração IAM ou rollback. O mantenedor dispensou explicitamente o teste de rollback; dispensa não é execução aprovada.

Conta 121754142617, região us-east-1. Inspecionadas as roles hom API/auth/Gateway e recursos persistentes compartilhados, sem executar aplicações de prd. A sessão AWS existente estava válida. Nenhum valor de Secret, token ou chave de ingestão foi lido.

## Armazenamento e acesso implantados

| Controle | Resultado observado na AWS |
|---|---|
| Bucket mecanica-artifacts-121754142617-us-east-1 | Versionamento Enabled; quatro bloqueios de acesso público ativos; criptografia AES256 |
| Transporte | Policy DenyInsecureTransport nega uso sem HTTPS |
| Criação de contratos/pacotes | Policy RequireConditionalArtifactCreation exige If-None-Match nas operações de criação fora de sam/auth/hom e sam/auth/prd |
| Imagens API | Repositório mecanica/api com imageTagMutability=IMMUTABLE; workflow entrega imagem por digest e tag por commit/run/attempt |
| Gateway hom | Policy de identidade permite ler contratos API/auth; não concede escrita/exclusão de artefatos |
| API hom | Leitura/publicação somente em contracts/api e packages/GerenciamentoMecanica.Auth.Contracts; sem DeleteObject nesses prefixos |
| Auth hom | Leitura/publicação de contracts/auth; ZIPs em sam/auth/hom. Permite DeleteObject nesse último prefixo para o descarte SAM; não estende esse acesso a sam/auth/prd |

Foram lidas todas as policies inline/gerenciadas das três roles e preservados os statements S3/ECR relevantes. SimulatePrincipalPolicy avaliou quatro ações S3 sobre cinco ARNs por role, total de sessenta decisões por recurso. ARNs de checagem sob sam não foram criados. Os resultados de identidade correspondem aos prefixos acima; auth → sam/auth/prd e Gateway → escrita/exclusão retornam implicitDeny. Leituras/publicações nos prefixos próprios retornam allowed conforme a ação.

**Limite da simulação:** não executa as operações e não combina, nesta avaliação de roles, a policy do bucket como teste efetivo de escrita. Algumas negativas incluem MissingContextValues de condições de outras permissões das policies (EC2/tags/PassRole/ListBucket); não são uma tentativa real recusada pelo S3. A restrição também foi revisada nos statements S3, separadamente. O resultado agregado de uma ação sobre vários recursos não foi confundido com a decisão de cada recurso; estas estão em ResourceSpecificResults. [Referência da AWS](https://docs.aws.amazon.com/IAM/latest/APIReference/API_SimulatePrincipalPolicy.html).

## Integridade e republicações existentes

Baixados somente os OpenAPI, template SAM hom e respectivos sidecars. SHA-256 calculado localmente coincide em todos:

| Objeto | SHA-256 |
|---|---|
| API 9d1c0625…/openapi.json | d784c3b554a65c4f8cfe732c0975b8bfedaf78d67f9c6ed69e2b07220c5f5d84 |
| Auth bf879cfe…/openapi.json | 31193f934fcba6a66ab64129d7e039188f22f3c936a888ef5c43ce6abebe3995 |
| Auth bf879cfe…/sam-hom.json | f933ccd80a436b79a598ef0ed694b3c28e21e3eb95cc9e16a8938c4f615e5a82 |

Uma alteração de um byte, somente em memória, produziu hash diferente; não foi alterado o objeto remoto. O template publicado referencia ZIP em sam/auth/hom/bf879cfe…; metadados SSM apontam para os mesmos contratos e Lambda hom versão 12. Nenhum ZIP foi baixado/executado nesta conferência.

ListObjectVersions encontrou uma versão por objeto dos dois OpenAPI/sidecars e do template SAM/sidecar, mesmo com republicações aprovadas na madrugada e ao meio-dia, já vinculadas no [índice de pipelines](EVIDENCIAS_CICD_E_PROTECOES_2026-10-21.md). Isso é evidência de preservação nas republicações inspecionadas, não de uma corrida entre publicadores.

Os workflows usam If-None-Match='*' nos contratos; em PreconditionFailed, comparam o conteúdo já publicado e falham quando diferente. O publicador existente do NuGet aplica o mesmo princípio e exige incremento da versão para conteúdo divergente. A política do bucket reforça o uso da condição; o ECR reforça a imutabilidade da tag. O mecanismo nativo evita substituir um objeto corrente com a mesma chave, conforme [documentação S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/conditional-writes.html). Não foi feita uma publicação concorrente real.

### Exceção SAM preservada

O upload nativo SAM está excluído da exigência de If-None-Match pela policy já implantada. O ZIP selecionado tem duas versões e um delete marker intermediário, compatíveis com publicação → destroy → republicação; ETags e tamanhos das duas versões coincidem. Isso **não comprova identidade SHA-256 dos ZIPs nem imutabilidade da chave**: não foram baixados os bytes das duas versões.

Na revisão AWS inspecionada, o provisionamento valida bucket/prefixo de ambiente/commit e existência do ZIP, mas não fixa S3 VersionId nem compara um SHA-256 independente do ZIP nesse passo. Portanto, não descrever ZIP SAM como tendo a mesma garantia dos contratos. Versionamento preserva versões anteriores; não proíbe nova escrita. Esta diferença era a exceção existente da integração SAM; o ajuste local posterior está descrito abaixo, sem alterar retroativamente a evidência AWS.

### Ajuste autorizado após a conferência — 21/10/2026

Implementado localmente na autenticação: auth-publish fixa CodeUri em Bucket/Key/Version antes do checksum, mantendo upload SAM e publicação condicional do template. Em republicação, compara os bytes do ZIP com a versão original e conserva esse VersionId; template divergente, ZIP divergente ou template legado sem versão exigem commit novo. Auth-provision aceita somente referência versionada no bucket/prefixo correto e confere head-object --version-id, com correspondência da versão e tamanho positivo. O resumo registra chave e versão. GetObjectVersion já está permitido; nenhuma alteração IAM, de aplicação ou de workflow remoto foi executada.

Actionlint/ShellCheck, SAM validate --lint hom/prd e transformação nativa para Code.S3ObjectVersion aprovados. Treze entradas dos guards reais de provisionamento avaliadas localmente, incluindo hom/prd válidos e rejeição de bucket/ambiente/commit/versão/tipo inválidos. Trechos reais jq/cmp da publicação conferiram fixação da versão, reexecução idêntica e rejeição de ZIP/template divergentes e referência legada. Sem mock AWS, nova suíte ou script versionado. Evidências locais ignoradas em artifacts/sam-version-pin/validation/.

No SAM CLI 1.167.0, a limpeza nativa de CodeUri objeto preserva o ZIP; não foi acrescentada exclusão manual. Uma versão fixada permanece selecionável mesmo com versão corrente diferente ou delete marker posterior. Isso estabiliza a seleção do deploy, sem tornar a chave do ZIP imutável nem comprovar uma publicação concorrente real. Publicação de commit novo, provisionamento e atualização do Gateway permanecem com o mantenedor; a implantação desse ajuste ainda não foi conferida na AWS.

### VersionId comprovado na AWS — 21/10/2026, 17:00 de São Paulo

Após publicação/provisionamento pelo mantenedor, conferência somente leitura na conta 121754142617/us-east-1, ambiente hom:

| Evidência | Resultado |
|---|---|
| Revisão auth | 01225acd7c45fe03a48ee7e6b228c069b77b4bdd |
| Stack mecanica-hom-auth | UPDATE_COMPLETE; atualização iniciada às 19:56:06 UTC |
| VersionId do ZIP | k__qUFPcWLwlrFOL4YDhVJFLN2QiOiAd |
| Template publicado / stack original / stack processada | Mesmos Bucket/Key/Version; a stack processada fixa Code.S3ObjectVersion |
| SHA-256 do template / sidecar | baf78e2ac0798b5e4487f388c12c5fdb934cbb156377fbc5d4f9e6f6d44f5f88; coincide |
| Lambda | Versão numérica 13, Active; descrição corresponde à revisão publicada |
| ZIP da versão exata / código da Lambda | 2.579.841 bytes; SHA-256 calculado coincide com CodeSha256 |

Chave: sam/auth/hom/01225acd7c45fe03a48ee7e6b228c069b77b4bdd/ddd3c33eb2788a47143aadc137e33c9d. O ZIP foi obtido com get-object --version-id; SHA-256 hexadecimal 26de904ef00e2d44355a93578026bc23e7612ae21bd9a9ec6e57150fa27740be, equivalente ao CodeSha256 em Base64 Jt6QTvAOLUQ1WpNXgCa8I+dhKuIb2ansblcVD6J3QL4=. O ZIP local foi removido após a comparação.

Isso comprova a implantação da referência versionada e a correspondência dos bytes com a Lambda 13 em hom. Não houve invocação, alteração de recursos, workflow, teste de republicação, destroy/recreate ou corrida entre publicadores nesta conferência. Prd não foi consultado. Não amplia a conclusão para esses cenários nem confirma qual versão o Gateway está consumindo. E6.12 continua parcial pela comprovação concorrente não exercitada; rollback permanece dispensado.

Evidências locais ignoradas em artifacts/sam-versionid-aws-20261021/: stack.json, stack-original.json, stack-processed.json, parsed-original.json, parsed-processed.json, sam-hom.json, seu checksum, zip-metadata.json, lambda.json e validation-summary.json. A resposta original CloudFormation continha YAML serializado dentro de JSON; após interpretação local com o leitor SAM, as referências original/processada foram conferidas. Nenhum valor de Secret foi lido.

## Guards de ambiente e formato avaliados localmente

Infraestrutura local sem alterações, HEAD 333e2a8…; autenticação sem alterações, HEAD bf879cfe…, correspondentes às revisões conferidas. Foram extraídos os locals de operações e as duas preconditions de identidade/rotas do Gateway, sem reimplementar seus predicados. Dados positivos: OpenAPI reais baixados e metadados públicos SSM atuais.

Em configuração descartável sem provider/backend, Terraform init -backend=false e validate passaram; console avaliou:

| Entrada | Resultado da condição |
|---|---|
| Contratos/referências publicados | true |
| API OpenAPI 3.1 | false |
| Auth OpenAPI 3.1 | false |
| ARN Lambda de prd usado com hom | false |
| Nome da função prd usado com hom | false |
| Commit auth divergente da referência SSM | false |
| Operação auth duplicada | false |
| Método auth GET em vez de POST | false |

As negativas foram substituições locais de entrada das expressões originais, sem mock de serviço AWS ou plano contra ambiente real. Demonstram o resultado dos guards que integram as preconditions; não são execuções de workflow/plan deliberadamente inválido.

Os guards de auth-provision também foram revisados: branch/ambiente, ancestralidade do commit, checksum do template e CodeUri no bucket/prefixo do ambiente/commit. Contratos OpenAPI são compartilhados por commit, não secretos nem exclusivos de um ambiente. O vínculo de execução vem de SSM/destinos/roles; não se afirma que um OpenAPI global de API seja proibido somente por também ter sido usado em prd.

**Escopo da compatibilidade:** verificam OpenAPI 3.0, identidade/versão Lambda e formato/conflitos de rotas exigidos. Não fazem comparação semântica completa entre DTOs/versões da API nem atestam que um OpenAPI arbitrário corresponda à imagem API ativa. Checksums comprovam integridade dos bytes, não toda a compatibilidade de negócio.

## Checklist e limites

- Rollback dispensado pelo mantenedor em E6.6a/E6.12. Não será exigida sua execução. E6.6a aproveita ECR/pull em pod privado novo/revisão/NLB/probes/HPA já comprovados; não acrescenta ensaio de troca de nó.
- Proteções de armazenamento e identidade implantadas conferidas; integridade e condições negativas avaliadas no escopo acima.
- E6.12 mantém somente a comprovação operacional de publicação concorrente sem perda de artefatos como não exercitada, além dos limites explícitos da exceção SAM e da validação de formato. Configuração nativa/filas existentes conferidas não equivalem a uma corrida real concluída.
- A conferência original não alterou workflows e usou Terraform nativo e arquivos locais ignorados. O ajuste posterior autorizado altera somente publish/provision da autenticação e documentação, sem novas suítes, mocks, scripts operacionais versionados ou bibliotecas.

Artefatos em `artifacts/e6-artifact-protection-20261021/`: aws-config.json, iam-policies.json, downloaded-metadata.json, identity-simulation.json, versions-and-initial-simulation.json, terraform-guard-results.json e validation-summary.json. A primeira extração resumida da simulação agrupava ações; o resultado por recurso, preservado em identity-simulation.json, é a referência. Tentativas iniciais do console com argumentos PowerShell mal interpretados falharam antes da avaliação; a execução final dos oito casos é a referência aprovada.
