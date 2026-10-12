# ADR 009 — Provisionamento da autenticação com SAM

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

- ZIP nativo SAM em sam/auth/<ambiente>/<commit>/, com deduplicação/versionamento, sem --force-upload. Somente sam/auth/hom/ e sam/auth/prd/ ficam fora da exigência de escrita condicional do bucket; isso não oferece a garantia de gravação única dos contratos.
- Template empacotado em contracts/auth/<commit>/sam-<ambiente>.json; OpenAPI no mesmo prefixo. Ambos e seus .sha256 preservam If-None-Match: *, checksum SHA-256 e comparação de bytes em reexecução.
- artifact_commit seleciona código e template publicados juntos. github.sha fixa o workflow/configuração de destino; não existe revisão Terraform auth separada.
- SSM v2 público continua sendo contrato entre repositórios. AWS CLI resolve os quatro identificadores de rede da base/banco; jq adapta apenas o array JSON das subnets ao parâmetro de lista. Validar VPC, duas subnets em AZs distintas e SGs antes de preparar o change set.
- JWT/credencial auth continuam resolvidos no runtime .NET; nenhuma chave/senha vai para template, parâmetros, outputs ou logs da pipeline.

## Workflows e operação

CI preserva pacote Auth.Contracts 1.0.0/checksum, testes .NET/PostgreSQL, SAM lint e actionlint, sem lint OpenAPI. CI usa role de leitura exclusiva do pacote; roles de deploy não ficam disponíveis aos testes de PR. Manter hom/develop, prd/main, OIDC e disparos manuais.

Publicação tem dois jobs: verificar/testar/build → publicar. Transferir build da mesma execução por artefato GitHub retido sete dias e usar sam package nativo, sem reconstrução de YAML.

Provisionamento tem um job: conferir artefatos/rede e executar sam deploy --no-confirm-changeset --no-fail-on-empty-changeset. SAM cria/aplica alterações, aguarda conclusão e mostra eventos/outputs. Sem job de aprovação, captura de ARN ou execute-change-set direto. Reter template/log nativo sete dias, inclusive em falha; não recompilar/publicar ZIP no deploy. Alterações vazias encerram com sucesso.

Rollback padrão CloudFormation habilitado. Falhas exibem diagnóstico limitado, sem rollback próprio ou sam sync. O bootstrap permanece administrativo com Terraform: plano salvo → aprovação → apply.

Destroy recebe somente ambiente e tem um job, sem aprovação. Executar sam delete --config-env <ambiente> --config-file samconfig.toml --stack-name mecanica-<ambiente>-auth --region us-east-1 --no-prompts. A configuração existente delimita o prefixo sam/auth/<ambiente>/changesets. SAM exclui o ZIP referenciado pelo deploy atual, templates operacionais e recursos da stack; não ampliar a limpeza para outras revisões. Stack ausente segue sucesso nativo; erros interrompem o workflow. Sem comandos CloudFormation diretos.

Preservar contracts/auth/, pacote NuGet, ZIPs de outras revisões, bucket versionado, IAM persistente, SG da base e outro ambiente. DeleteObject cria marcadores, sem apagar versões históricas ou introduzir lifecycle. Republicar antes de recriar o deploy; falha de exclusão da stack pode ocorrer após remover artefatos e não os restaura automaticamente. Conferir ENIs via EC2 pelo nome do SG, sem depender de stack/SSM, antes de descartar banco/base.

Bootstrap concede DeleteObject somente em sam/auth/<ambiente>/*, ListBucket condicionado ao mesmo prefixo e ListChangeSets na própria stack para estados parciais. A inspeção DescribeStacks da stack auxiliar de nome calculado pelo SAM não autoriza sua exclusão nem ações ECR. Não conceder DeleteObjectVersion ou DeleteBucket. Aplicação IAM exige novo plano salvo aprovado.

## Migração e comprovação

Conferência AWS somente leitura em 2026-10-11, conta 121754142617/us-east-1: zero funções e nenhum recurso auth hom/prd, stack, log group, parâmetro auth/v2, estado atual ou versão histórica nos prefixos auth. Unidade Terraform auth retirada sem importação/destroy ou exclusão de estado. Roles runtime/deploy preservadas; role CI ainda ausente. Rede/base/banco devem ser recriados.

Validação local: 217 testes sem ignorados, build Release/SAM, template/workflows e bootstrap válidos, certificado/deps/runtimeconfig no pacote e ZIP nativo repetível para o mesmo build. Não comprova implantação SAM, TLS/IAM RDS ou integração real JWT/API.

Consulta AWS em 2026-10-12 na conta 121754142617/us-east-1: quota total dez, não reservada dez e zero funções. Tentativas com reservas cinco e dez falharam por exigência de preservar dez não reservadas; segunda revisão 85ca14b331dc15bd9034f065502df6e4496f83a4, rollback concluído. Após a explicação dos valores, o mantenedor autorizou remover ReservedConcurrentExecutions do template hom/prd. A função usará capacidade compartilhada, sem reserva exclusiva ou limite próprio; a quota regional continua dez e o pedido de aumento permanece pendente. Não há fallback automático ou aumento de quota nesta alteração. Bootstrap aprovado aplicado; reimplantar nova revisão após excluir a stack ROLLBACK_COMPLETE e republicar artefatos. E3.1/E3.2/E3.6/E3.9 e E2.12 permanecem abertas até comprovação real; nenhum deploy ou destroy foi executado nesta implementação. Apply do bootstrap concluído: seis criações, cinco alterações, nenhuma exclusão.

**Substitui:** Terraform auth de ADR 004/008, D01.5/D05/D09, RFC 002 e E3.6. Demais decisões declarativas e propriedade persistente do IAM permanecem.

Referências: [SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html), [deploy/change sets](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-cli-command-reference-sam-deploy.html), [versões SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-resource-function.html), [descarte SAM](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-cli-command-reference-sam-delete.html), [S3 versionado](https://docs.aws.amazon.com/AmazonS3/latest/userguide/DeletingObjectVersions.html).
