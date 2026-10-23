> **AE.9, 23/10/2026:** auth mantém versão numérica/Gateway; envio usa template/stack/config/alias próprios de EmailFunction no mesmo repositório. [ADR 011](011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md) e [workflows AE.8](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE8_WORKFLOWS_E_GATEWAY.md). Novo fluxo preparado localmente, não implantado na AWS.

# ADR 009 — Provisionamento da autenticação com SAM

**Estado:** aceito em 11/10/2026; implementação/integração hom comprovadas. VersionId do ZIP da Lambda 13 conferido em 21/10; ambiente removido depois.

**Contexto:** simplificar manutenção do provisionamento auth e usar o formato nativo Lambda ZIP/CloudFormation, preservando contrato/rede/IAM persistente.

**Decisão vigente:** SAM CLI 1.167.0 executa build/package/deploy/delete com templates/auth.yaml e samconfig.auth.toml. .NET 10/x86_64, 512 MB/15 s, sem concorrência reservada/provisionada. AWS::LanguageExtensions precede SAM e AutoPublishAliasAllProperties=true; live é auxiliar, Gateway consome ARN de versão numérica publicado no SSM.

## Responsabilidades e operação

| Proprietário | Recursos |
|---|---|
| Bootstrap | Buckets/OIDC/ECR, role CI/deploy e auth-runtime persistente |
| Base | VPC/subnets/NAT, SG reservado auth e JWT/base/v2 |
| Banco | RDS/credenciais, ingresso PostgreSQL, SQL/Job/database/v2 |
| Auth SAM | Função/versão/alias, logs, duas saídas do SG reservado e seis parâmetros auth/v2 |
| Gateway | Entrada REST/VPC Link e permissão de invocação da versão Lambda |

Auth cria somente egress 5432 ao SG do banco e 443 para saída via NAT no SG pertencente à base. A role persistente permite limpeza posterior das ENIs. Sem IAM duplicado na stack ou permissão de Secret administrativo para a função.

Auth-ci verifica pacote exato, testes .NET/PostgreSQL, SAM/actionlint; auth-publish tem testes/build SAM → publicação com artefato por ID da mesma execução. auth-provision e auth-destroy têm um job manual, sem Environment de aprovação intermediária. O disparo manual autoriza cada operação completa; SAM usa rollback padrão CloudFormation. Não há comandos CloudFormation diretos nem deploy por merge.

## Seleção imutável do código

SAM package publica ZIP em sam/auth/<ambiente>/<commit>/ com deduplicação e bucket versionado, sem force-upload. O prefixo continua gravável e é exceção à exigência If-None-Match. Antes da publicação, jq fixa somente CodeUri no objeto nativo **Bucket/Key/Version**; não recompila ou substitui o empacotamento SAM.

Template e sidecar em contracts/auth/<commit>/sam-<ambiente>.json são imutáveis por criação condicional/comparação de bytes, assim como OpenAPI. Checksum é calculado depois de fixar Version. Em republicação, PreconditionFailed permite ler o template existente; ZIP idêntico conserva o VersionId original e template divergente falha. Artefato antigo sem Version ou código divergente exige commit novo, sem sobrescrever a revisão existente. Erros de permissão não são interpretados como ausência.

Provision confere checksum, bucket/prefixo/commit e head-object --version-id com tamanho positivo, e sam deploy consome Code.S3ObjectVersion. GetObjectVersion já era permitido. A nova versão corrente da chave não altera a seleção publicada; isso não transforma a chave em write-once.

Conferência AWS da revisão 01225acd…: Lambda 13 Active, stack UPDATE_COMPLETE, mesma versão no template publicado/original/processado e SHA-256 do ZIP igual a CodeSha256. [Evidência detalhada](../../VALIDACAO_PROTECAO_ARTEFATOS_2026-10-21.md). Republicação/concorrência reais não foram exercitadas como testes; concorrência foi dispensada.

## Descarte e observabilidade

sam delete usa o prefixo operacional sam/auth/<ambiente>/changesets. No SAM CLI 1.167.0, CodeUri objeto preserva o ZIP; versões históricas/contratos/bucket/IAM ficam fora do descarte normal. Templates antigos com URI string seguem limpeza da chave corrente, sem DeleteObjectVersion. A versão fixada permite recriar sem republicar apenas para restaurar a chave corrente. Aguardar ENIs/egress antes de banco/base; não excluir interfaces ou versões manualmente sem autorização.

SAM fixa OTel/X-Ray/New Relic sempre ativos, layer Collector 0.22.0 e YAML empacotado. Função não recebe License Key em variáveis; extensão a lê pelo Secrets Manager e compartilha a role da função. W3C da aplicação e X-Ray nativo ficam separados. [ADR 005](005-OBSERVABILIDADE.md).

[Operação auth](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/DEPLOY.md) e [encerramento hom](../../ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md). Sem novo build/deploy/destroy nesta revisão; prd não validado.

Revisão documental de 21/10/2026. Implementação e validações de hom não comprovam prd ou testes dispensados. [Arquitetura](../README.md) · [Evidências E6](../../REVISAO_E6_2026-10-21.md).

<details>
<summary>Histórico anterior à revisão E7 — registros preservados, sem vigência operacional</summary>

Os estados locais, planos pendentes e alternativas abaixo registram o momento em que foram escritos. A decisão e o alcance atuais estão acima; este histórico não autoriza operações nem reabre pendências aceitas.

**Estado:** aceito pelo mantenedor em 2026-10-11; migração implementada e validada localmente. Bootstrap CI/SAM delete aprovado/aplicado em 2026-10-11, com plano posterior No changes e conferência IAM/bucket. Tentativas de deploy com reservas cinco/dez falharam por quota; remoção da reserva autorizada em 2026-10-12. Implantação funcional continua pendente.

**Contexto:** o provisionamento Terraform da Lambda e a automação de artefatos aumentaram a complexidade de manutenção. O mantenedor escolheu SAM com ZIP, upload nativo em prefixo próprio do bucket existente, código/template da mesma revisão e rollback padrão CloudFormation.

**Decisão:** GerenciamentoMecanicaAutenticacao declara recursos em template.yaml e destinos em samconfig.toml. SAM CLI 1.167.0 executa build/package/deploy/delete; CloudFormation administra a stack internamente. Por decisão adicional do mantenedor, o disparo manual autoriza deploy/descarte completos, sem aprovação intermediária. Deploy/descarte sem comandos CloudFormation diretos; limpeza autorizada somente do ZIP atual e templates operacionais SAM. Não construir templates por scripts.

## Responsabilidades

| Proprietário | Recursos |
|---|---|
| Bootstrap Terraform | Buckets/OIDC/ECR, role CI, permissões OIDC e role/policies auth-runtime persistentes |
| Base Terraform | VPC/subnets/NAT, SG reservado auth, EKS/NLB, JWT e base/v2 |
| Banco Terraform | RDS/credenciais/ingresso PostgreSQL, SQL/Job e database/v2 |
| Auth SAM/CloudFormation | Função, versão/alias live, log group, duas saídas do SG reservado e seis parâmetros auth/v2 |
| Gateway Terraform | REST API/VPC Link, integrações e permissão para invocar a versão Lambda |

A stack reutiliza IAM runtime persistente, permitindo limpeza posterior das ENIs. A base continua proprietária do SG; auth possui somente saída TCP 5432 ao SG do banco e TCP 443 IPv4 via NAT. Nenhum recurso tem dupla propriedade.

Manter .NET 10/x86_64, 512 MB, timeout 15 segundos, sem concorrência reservada ou provisionada, usando a capacidade compartilhada da conta e logs JSON/sete dias. AutoPublishAlias live e AutoPublishAliasAllProperties true publicam versões; AWS::LanguageExtensions antecede SAM para considerar parâmetros resolvidos. O Gateway consome ARN de versão numérica publicado no SSM, sem $LATEST ou alias.

## Artefatos e configuração

- ZIP nativo SAM em sam/auth/<ambiente>/<commit>/, com deduplicação/versionamento, sem --force-upload. Somente sam/auth/hom/ e sam/auth/prd/ ficam fora da exigência de escrita condicional do bucket; isso não oferece a garantia de gravação única dos contratos. Complemento autorizado em 21/10/2026: antes da publicação, jq fixa CodeUri no formato nativo Bucket/Key/Version. O template imutável e seu checksum selecionam uma versão S3 estável; provisionamento confere essa versão com head-object --version-id e SAM a transforma em Code.S3ObjectVersion.
- Template empacotado em contracts/auth/<commit>/sam-<ambiente>.json; OpenAPI no mesmo prefixo. Ambos e seus .sha256 preservam If-None-Match: *, checksum SHA-256 e comparação de bytes em reexecução.
- artifact_commit seleciona código e template publicados juntos. github.sha fixa o workflow/configuração de destino; não existe revisão Terraform auth separada.
- SSM v2 público continua sendo contrato entre repositórios. AWS CLI resolve os quatro identificadores de rede da base/banco; jq adapta apenas o array JSON das subnets ao parâmetro de lista. Validar VPC, duas subnets em AZs distintas e SGs antes de preparar o change set.
- JWT/credencial auth continuam resolvidos no runtime .NET; nenhuma chave/senha vai para template, parâmetros, outputs ou logs da pipeline.

## Workflows e operação

CI preserva pacote Auth.Contracts 1.0.0/checksum, testes .NET/PostgreSQL, SAM lint e actionlint, sem lint OpenAPI. CI usa role de leitura exclusiva do pacote; roles de deploy não ficam disponíveis aos testes de PR. Manter hom/develop, prd/main, OIDC e disparos manuais.

Publicação tem dois jobs: verificar/testar/build → publicar. Transferir build da mesma execução por artefato GitHub retido sete dias e usar sam package nativo, sem reconstrução de YAML. A alteração localizada de CodeUri ocorre no JSON já empacotado. Republicação do mesmo commit reutiliza a versão fixada somente após comparar os bytes dos ZIPs e do template. Conteúdo divergente ou template antigo sem Version exige commit novo. A criação condicional do template antecede o checksum; PreconditionFailed permite conferir o existente, sem interpretar acesso negado como ausência ou ampliar IAM.

Provisionamento tem um job: conferir artefatos/rede e executar sam deploy --no-confirm-changeset --no-fail-on-empty-changeset. SAM cria/aplica alterações, aguarda conclusão e mostra eventos/outputs. Sem job de aprovação, captura de ARN ou execute-change-set direto. Reter template/log nativo sete dias, inclusive em falha; não recompilar/publicar ZIP no deploy. Alterações vazias encerram com sucesso.

Rollback padrão CloudFormation habilitado. Falhas exibem diagnóstico limitado, sem rollback próprio ou sam sync. O bootstrap permanece administrativo com Terraform: plano salvo → aprovação → apply.

Destroy recebe somente ambiente e tem um job, sem aprovação. Executar sam delete --config-env <ambiente> --config-file samconfig.toml --stack-name mecanica-<ambiente>-auth --region us-east-1 --no-prompts. A configuração existente delimita o prefixo sam/auth/<ambiente>/changesets. SAM exclui o ZIP referenciado pelo deploy atual, templates operacionais e recursos da stack; não ampliar a limpeza para outras revisões. Stack ausente segue sucesso nativo; erros interrompem o workflow. Sem comandos CloudFormation diretos.

Preservar contracts/auth/, pacote NuGet, ZIPs de outras revisões, bucket versionado, IAM persistente, SG da base e outro ambiente. DeleteObject cria marcadores, sem apagar versões históricas ou introduzir lifecycle. No SAM CLI 1.167.0, CodeUri objeto preserva também o ZIP atual; a limpeza descrita acima aplica-se aos templates antigos com CodeUri string. A versão fixada permite recriar o deploy sem republicar somente para restaurar a chave corrente. Falha de exclusão da stack pode ocorrer após remover outros artefatos e não os restaura automaticamente. Conferir ENIs via EC2 pelo nome do SG, sem depender de stack/SSM, antes de descartar banco/base.

Bootstrap concede DeleteObject somente em sam/auth/<ambiente>/*, ListBucket condicionado ao mesmo prefixo e ListChangeSets na própria stack para estados parciais. A inspeção DescribeStacks da stack auxiliar de nome calculado pelo SAM não autoriza sua exclusão nem ações ECR. Não conceder DeleteObjectVersion ou DeleteBucket. Aplicação IAM exige novo plano salvo aprovado.

## Migração e comprovação

Conferência AWS somente leitura em 2026-10-11, conta 121754142617/us-east-1: zero funções e nenhum recurso auth hom/prd, stack, log group, parâmetro auth/v2, estado atual ou versão histórica nos prefixos auth. Unidade Terraform auth retirada sem importação/destroy ou exclusão de estado. Roles runtime/deploy preservadas; role CI ainda ausente. Rede/base/banco devem ser recriados.

Validação local: 217 testes sem ignorados, build Release/SAM, template/workflows e bootstrap válidos, certificado/deps/runtimeconfig no pacote e ZIP nativo repetível para o mesmo build. Não comprova implantação SAM, TLS/IAM RDS ou integração real JWT/API.

Consulta AWS em 2026-10-12 na conta 121754142617/us-east-1: quota total dez, não reservada dez e zero funções. Tentativas com reservas cinco e dez falharam por exigência de preservar dez não reservadas; segunda revisão 85ca14b331dc15bd9034f065502df6e4496f83a4, rollback concluído. Após a explicação dos valores, o mantenedor autorizou remover ReservedConcurrentExecutions do template hom/prd. A função usará capacidade compartilhada, sem reserva exclusiva ou limite próprio; a quota regional continua dez e o pedido de aumento permanece pendente. Não há fallback automático ou aumento de quota nesta alteração. Bootstrap aprovado aplicado; reimplantar nova revisão após excluir a stack ROLLBACK_COMPLETE e republicar artefatos. E3.1/E3.2/E3.6/E3.9 e E2.12 permanecem abertas até comprovação real; nenhum deploy ou destroy foi executado nesta implementação. Apply do bootstrap concluído: seis criações, cinco alterações, nenhuma exclusão.

**Substitui:** Terraform auth de ADR 004/008, D01.5/D05/D09, RFC 002 e E3.6. Demais decisões declarativas e propriedade persistente do IAM permanecem.

Referências: [SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html), [deploy/change sets](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-cli-command-reference-sam-deploy.html), [versões SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-resource-function.html), [descarte SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-cli-command-reference-sam-delete.html), [S3 versionado](https://docs.aws.amazon.com/AmazonS3/latest/userguide/DeletingObjectVersions.html).

</details>
