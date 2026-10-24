# Mapa de descarte do ambiente AWS

Consolidado em 23/10/2026: workflows auth/email e seus bloqueios já estão implementados. A sequência abaixo descreve o procedimento atual; o ciclo anterior sem EmailFunction foi comprovado na E6.11, mas o destroy da nova função não foi executado como ensaio na AE.10. Nenhuma exclusão está autorizada por este documento.

Para o ambiente atual, selecionar **branch `develop` e environment `hom`** em todos os workflows. Para produção, a correspondência é `main`/`prd`. Não misturar ambientes nem iniciar provisionamentos/deploys enquanto o descarte estiver em andamento.

```mermaid
flowchart TD
    G["1. gateway-destroy · Infraestrutura"] --> A["2. API destroy · Sistema"]
    G --> L["2. auth-destroy · Serverless"]
    A --> E["Aguardar ambos e conferir liberação das ENIs"]
    L --> E
    E --> D["3. database-destroy · BancoDados"]
    E --> M["3. email-destroy · Serverless"]
    D --> B["4. base-destroy · Infraestrutura"]
    M --> B
```

## Workflows e entradas

| Fase | Repositório | Workflow | Entradas | Aprovação |
|---|---|---|---|---|
| 1 | GerenciamentoMecanicaInfraestrutura | `gateway-destroy` | `environment`, `api_commit`, `auth_commit` | Plano Terraform → aprovação → apply salvo |
| 2, em paralelo | GerenciamentoMecanicaSistema | **API destroy** (`api-destroy.yml`) | `environment` | O disparo manual autoriza a execução completa |
| 2, em paralelo | GerenciamentoMecanicaServerless | `auth-destroy` | `environment` | O disparo manual autoriza o `sam delete` |
| 3 | GerenciamentoMecanicaBancoDados | `database-destroy` | `environment` | Plano Terraform → aprovação → apply salvo |
| 3, em paralelo com banco após API/auth | GerenciamentoMecanicaServerless | `email-destroy` | `environment` | O disparo manual autoriza SAM delete e inspeção |
| 4 | GerenciamentoMecanicaInfraestrutura | `base-destroy` | `environment` | Plano Terraform → aprovação → apply salvo |

No `gateway-destroy`, usar os mesmos **SHAs completos dos contratos** informados na última execução bem-sucedida de `gateway-provision`. Consultar os inputs/resumo daquela execução. Não usar digest da imagem, hash SHA-256 de arquivo ou ID da execução. Os contratos e checksums precisam continuar disponíveis no S3 até terminar esse destroy. A base e os parâmetros necessários da autenticação também devem continuar disponíveis durante seu plan/apply.

O `auth-destroy` atual não pede `artifact_commit`; não é necessário executar `auth-publish` antes do descarte. Também não é preciso executar CI ou publicação da API para destruir o ambiente já implantado.

## O que pode rodar em paralelo

Depois de **concluir com sucesso o gateway-destroy**, `API destroy` e `auth-destroy` podem executar simultaneamente: removem consumidores independentes, usam filas distintas e não compartilham estado Terraform.

Antes de avançar ao banco ou email, aguardar o sucesso de API destroy e auth-destroy. Depois, database-destroy e email-destroy podem seguir independentemente, com a base preservada até ambos terminarem. Após sam delete, auth-destroy agora falha imediatamente se houver ENIs ou regras de saída no SG reservado da autenticação. Não há espera automática: aguardar a limpeza da AWS e repetir o workflow. A existência isolada do SG é normal, pois ele pertence à base. Não excluir ENIs gerenciadas manualmente; preservar a role de execução persistente.

Não executar `database-destroy` enquanto API ou autenticação estiverem ativas. Não executar `base-destroy` em paralelo com nenhum dos demais: ele remove as dependências de rede, cluster e configurações usadas pelos descartes anteriores. Os grupos de concurrency dos workflows não coordenam repositórios diferentes; essa ordem depende do mantenedor.

## Bloqueios automáticos por dependência

| Destroy | Recursos que precisam estar ausentes |
|---|---|
| API e autenticação | REST API e VPC Link mecanica-<ambiente>-gateway |
| Banco | Deployment/HPA/ServiceAccount da API, pods e ReplicaSets; função auth, ENIs e regras de saída do SG auth |
| Email | Recursos da API no EKS e função auth/ENIs/regras de saída do SG auth |
| Base | RDS, DB subnet group e SG do banco; função email, ENIs e regras de saída de seu SG |

AWS CLI consulta os nomes exatos em us-east-1, com paginação automática e resultados JSON. A ausência de parâmetros SSM ou o status de um workflow não são usados como prova de remoção. Resposta inválida, erro de consulta ou acesso negado interrompem o fluxo.

Cada destroy verifica somente suas dependências diretas: API/auth checam Gateway; banco checa API/auth; base checa banco e email. Email-destroy checa API/auth, sem depender do banco. A ordem é aplicada em cadeia pelos workflows, sem repetir as consultas das etapas anteriores.

Quando o EKS existe, o banco consulta os objetos da API em default, mesmo que a API tenha zero réplicas. EKS deve permanecer até o final. Cluster comprovadamente ausente dispensa somente essa consulta Kubernetes; a autenticação continua sendo verificada pelo banco.

Banco/base repetem a conferência antes do plan e no apply, antes de qualquer exclusão, preservando o plano salvo aprovado. Falha nessa conferência não executa a limpeza always dos Secrets do banco. API/auth verificam Gateway antes de excluir seus recursos.

Pré-requisito de publicação: aplicar o bootstrap revisado e a base com AmazonEKSViewPolicy em default para a pipeline do banco, preservando AmazonEKSEditPolicy somente em database-init. Depois utilizar os workflows atualizados. Metadados regionais podem ser listados entre ambientes, mas os bloqueios filtram o ambiente exato; nenhuma permissão de escrita adicional é concedida aos consumidores.

Recuperação automática de banco sem EKS/SSM não integra esta entrega. Não ignorar falhas nem remover referências do estado automaticamente. Preparar recuperação específica se a cadeia já estiver quebrada.

### Conferência operacional posterior

Durante a próxima utilização autorizada do ambiente, conferir:

- Gateway existente bloqueia API/auth antes de suas exclusões.
- Deployment da API com zero réplicas, HPA/ServiceAccount ou pods/ReplicaSets remanescentes bloqueiam o banco.
- Função auth ausente, mas ENIs ou regras de saída ainda presentes, continua bloqueando o banco; auth-destroy também falha na conferência final de rede.
- RDS em deleting, DB subnet group ou SG do banco remanescente bloqueiam base antes de remover Collector/SMTP4dev/manifestos.
- Função email, ENIs ou egress remanescentes também bloqueiam a base; SG email isolado é permitido porque pertence à base. O bloqueio não aguarda automaticamente liberação de rede.
- Acesso negado ou erro de consulta interrompe a execução; ausência dos consumidores permite prosseguir pelo fluxo normal.
- Recursos de prd não bloqueiam hom, nem o contrário: todas as buscas usam nomes exatos do ambiente.

Esses cenários ainda não foram executados na AWS. Não criar recursos, provocar falhas ou executar destroys somente para testar esta implementação.

## Dependência email — preparação local AE.4

[Inspeção e bloqueios](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/AE4_INSPECAO_E_DESCARTE.md): base-destroy verifica banco e email antes do plan e novamente no apply, antes de remover Collector, namespace SMTP4dev ou qualquer outro manifesto da base. ListFunctions usa paginação/JSON e nome exato mecanica-<ambiente>-email; SG e interfaces são inspecionados sem transformar erros em ausência. A policy da base foi revisada no bootstrap para leitura Lambda regional; aplicar o plano aprovado dessa revisão antes de usar o workflow.

Preparada a access entry da pipeline email com AmazonEKSViewPolicy/default, sem editar API, ler Secrets ou acessar a caixa SMTP. Email-destroy será criado junto dos workflows da AE.8: exigirá API/auth ausentes antes do SAM e conferirá ENIs/egress depois. Ainda não foi executado nem substituído por um workflow incompleto.

Depois de remover API/auth, banco e email poderão ser descartados independentemente. Se email nunca tiver sido implantada, função ausente e SG sem resíduos permitem a base continuar, sem exigir execução de um destroy fictício. A existência do novo SG não cria dependência email → RDS. Base só pode ser retirada depois de ambos; isso preserva a rede usada por email e a caixa até o final. A ordem futura complementa o mapa anterior; comprovação de bloqueios na AWS continua pendente na AE.10. Não executar destroys para testar durante a implementação.

## Collector, recursos persistentes e recriação

- Não há destroy separado para o Collector EKS. `base-destroy` remove `kubernetes/observability/<ambiente>`, aguarda a exclusão e depois remove os manifestos da base e aplica o plano Terraform.
- O Collector da Lambda desaparece com a função em `auth-destroy`; a layer pública é preservada.
- `database-destroy` remove o RDS e as credenciais/configurações administradas pelo estado do banco. O histórico SQL deixa de estar disponível após excluir o banco; não presumir recuperação automática dos dados.
- Bootstrap, backends, roles persistentes, bucket compartilhado, ECR/imagens e Secrets New Relic do bootstrap ficam fora do descarte normal. Não executar destroy do bootstrap nem apagar a License Key no New Relic para encerrar hom.
- SAM delete remove a stack e templates operacionais configurados. Com SAM CLI 1.167.0 e CodeUri objeto versionado, os ZIPs publicados são preservados; não confundir com o comportamento histórico de CodeUri string. Contratos, bucket, IAM e outras revisões permanecem. Seguir os guards e o procedimento atual de publicação/provisionamento para reutilizar uma revisão, sem exclusão manual de versões.
- Telemetria já recebida no New Relic segue a retenção da conta; esses workflows não a apagam. O outro ambiente não faz parte do descarte selecionado.

Se algum workflow falhar, interromper a sequência e analisar a falha antes de descartar sua dependência. Não cancelar um apply Terraform em andamento nem usar force-unlock sem conferir quem mantém o lock.

## Referências

- [API destroy](../.github/workflows/api-destroy.yml)
- [gateway-destroy](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/.github/workflows/gateway-destroy.yml)
- [auth-destroy](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/.github/workflows/auth-destroy.yml)
- [email-destroy](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/.github/workflows/email-destroy.yml)
- [database-destroy](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/.github/workflows/database-destroy.yml)
- [base-destroy](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/.github/workflows/base-destroy.yml)
