# RFC 002 — Entrega e contratos entre componentes

**Estado:** arquitetura aceita; implementação parcial. **Registro:** 2026-09-16; detalhamento em 2026-09-24. [Índice](../README.md)

Os [contratos entre repositórios](../CONTRATOS_ENTRE_REPOSITORIOS.md) especificam nomes, tipos, produtores/consumidores, checksums e publicação de releases. Bootstrap e diagnóstico OIDC estão disponíveis; a integração dos componentes continua pendente. O documento de contratos é a fonte do detalhamento operacional, sem duplicação das tabelas nesta RFC.

## Responsabilidades

| Repositório | Produz e mantém |
|---|---|
| GerenciamentoMecanicaSistema | API, domínio, testes, imagem ECR por digest, Deployment/HPA, OpenAPI e pacote CPF/CNPJ e JWT |
| GerenciamentoMecanicaInfraestrutura | Rede/EKS, add-ons, Collector, Service e configuração que solicita NLB; unidade separada de Gateway/VPC Link/OpenAPI/permissão de invocação Lambda |
| GerenciamentoMecanicaBancoDados | Aurora, acesso e secrets de banco, esquema/seeds e Job de inicialização |
| GerenciamentoMecanicaAutenticacao | Código/testes/ZIP da Lambda, Terraform/IAM da função e contrato Validate |

Terraform não gerencia em duplicidade o NLB criado pelo AWS Load Balancer Controller. A aplicação não cria um segundo Service de entrada. O bootstrap compartilhado mantém identidade OIDC, backend de estado, ECR e bucket separado de artefatos.

## Integração das pipelines

Estados Terraform ficam no S3 versionado, separados por componente/ambiente, com locking nativo. GitHub Actions assume roles AWS por OIDC, restritas ao contexto de repositório/ambiente. O endpoint administrativo EKS público e privado permite operação pelo runner hospedado, com autorização IAM/EKS/RBAC.

Metadados prontos para consumo são publicados no SSM em `/mecanica/<hom|prd>/<component>/v1/<field>`; valores secretos permanecem no Secrets Manager, referenciados por ARN. Publicar uma revisão somente após recursos prontos e invalidar referências ao destruir. Existência de parâmetro não comprova que o recurso existe.

API/função publicam OpenAPI do mesmo commit do backend em `contracts/api|auth/<commit>/openapi.json`. A infraestrutura compõe versões fixas e extensões AWS. Workflow reutilizável da infraestrutura, fixado por SHA, é chamado após o deploy de cada produtor, preservando a versão já implantada do outro. O contexto de permissões/OIDC do chamador deve ser tratado explicitamente.

ZIP, pacote NuGet e contratos no bucket de artefatos têm versão, checksum e prevenção de sobrescrita. O S3 não é um feed NuGet nativo: a função baixa a versão fixa do pacote para um feed local antes do restore. Sua publicação é independente do deploy e da existência do banco. O manifesto de implantação registra revisões de código/infraestrutura/esquema, contratos e artefatos efetivamente implantados; falha não promove um manifesto para sucesso.

## Ativação, atualização e destruição

1. Preparar bootstrap persistente de estado/identidade.
2. Provisionar base AWS, acesso ao cluster, add-ons e Service/NLB.
3. Provisionar Aurora e executar Job de esquema/seeds em banco vazio.
4. Implantar API e função após suas dependências.
5. Aplicar unidade Gateway e permissão de invocação, após os dois backends iniciais.
6. Executar verificações funcionais e observar os sinais antes da demonstração.

Destruir em ordem inversa das dependências, removendo Service e aguardando limpeza do NLB enquanto o controller ainda funciona. Preservar bootstrap e outro ambiente. Desligar alertas da janela e drenar telemetria antes de encerrar coletores. O banco educacional não exige snapshot final, mas isso não autoriza remover snapshots existentes sem avaliar sua origem.

`develop` entrega em **hom** e `main` em **prd**, com PR/checks. Os ambientes podem coexistir e não se bloqueiam globalmente. Deploy com ambiente desligado fica pendente até ativação explícita; não criar infraestrutura implicitamente em um CI de PR. Rollback de código exige compatibilidade com o esquema e não promete reversão de dados.

## Validação e trabalho seguinte

Concretizar repositórios, contratos operacionais e governança; implementar provisionamento; comprovar entrega e isolamento. Antes de operações concorrentes no mesmo ambiente, definir coordenação entre repositórios: lock S3 protege somente um estado, e concurrency do GitHub é local ao repositório.

**Justificativa:** [ADR 004](../adrs/004-ENTREGA-E-AMBIENTES.md). O [documento operacional](../CONTRATOS_ENTRE_REPOSITORIOS.md) amplia estas interfaces com campos, formatos e referências do bootstrap. IDs dos workloads serão publicados quando os recursos existirem.
