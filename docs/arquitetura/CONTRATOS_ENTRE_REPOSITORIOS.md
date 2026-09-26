# Contratos entre repositórios

**Versão da especificação:** 1.0.0. **Data:** 2026-09-24.

[Arquitetura](README.md) · [RFC de entrega](rfcs/002-ENTREGA.md) · [HTTP/JWT e permissões](ACESSO_E_AUTENTICACAO.md)

Este documento é a fonte central das interfaces entre os quatro repositórios. Define o que implementar; não comprova que os publicadores ou consumidores já existem. Bootstrap S3/ECR/OIDC disponível e testes positivos de autenticação confirmados; metadados SSM, secrets de aplicação, manifestos, publicação de artefatos e deploys dos componentes ainda serão implementados. Detalhes de campos definidos aqui complementam as decisões da RFC de entrega.

**Implementação parcial em 2026-09-24:** pacote Auth.Contracts 1.0.0, integração local à API, consumidor NuGet independente e workflows/scripts preparados. [Procedimentos e limites](PACOTE_AUTH_CONTRACTS.md). Publicação/consumo reais no S3 ainda pendentes; demais contratos continuam como especificação.

## 1. Escopo e propriedade

| Componente lógico | Repositório proprietário | Publica | Consome |
|---|---|---|---|
| api | GerenciamentoMecanicaSistema | Imagem ECR, OpenAPI, pacote Auth.Contracts, release da API | Base, banco, referências JWT/SMTP, configuração OTel |
| base | GerenciamentoMecanicaInfraestrutura | Rede/EKS, namespace/Service/NLB, referências compartilhadas, release da base | Outputs do bootstrap e configuração operacional do ambiente |
| gateway | GerenciamentoMecanicaInfraestrutura | OpenAPI composto, Gateway/VPC Link, permissão de invocação Lambda, release de entrada | Base, releases e OpenAPI da API e da função |
| database | GerenciamentoMecanicaBancoDados | Aurora, credenciais de banco, SQL/seeds, versão/hash do esquema, release do banco | Rede, cluster, grupos de segurança e identidade do Job da base |
| auth | GerenciamentoMecanicaAutenticacao | ZIP, OpenAPI Validate, Lambda/versionamento, release da função | Base, banco, JWT, New Relic e pacote Auth.Contracts |

A infraestrutura tem duas unidades independentes: base e gateway. Service pertence à base; Deployment/HPA pertencem à API. O controller gerencia o NLB solicitado pelo Service; Terraform não cria uma segunda cópia desse NLB. A unidade gateway é dona da permissão de invocação Lambda. Não consumir o estado Terraform de outro componente como interface pública.

## 2. Identificadores, ambientes e bootstrap

| Identificador | Valor |
|---|---|
| Conta / região | 121754142617 / us-east-1 |
| Ambiente / branch | hom / develop; prd / main |
| Bucket de estado | mecanica-tfstate-121754142617-us-east-1 |
| Bucket de artefatos | mecanica-artifacts-121754142617-us-east-1 |
| Repositório ECR | 121754142617.dkr.ecr.us-east-1.amazonaws.com/mecanica/api |
| Estado do bootstrap | shared/bootstrap/terraform.tfstate |
| Estados dos componentes | <ambiente>/<base ou gateway ou database ou auth>/terraform.tfstate |
| Namespace SSM | /mecanica/<ambiente>/<componente>/v1/<campo> |

API não tem estado Terraform. O workspace é default; hom/prd são chaves independentes, não workspaces. Backends usam encrypt=true e use_lockfile=true. Bootstrap é persistente; recriação/descarte de um ambiente preserva buckets, imagens, OIDC, roles das pipelines e o outro ambiente.

Variáveis dos GitHub Environments:

| Repositório | Variáveis |
|---|---|
| API | AWS_REGION, AWS_ROLE_ARN, ARTIFACTS_BUCKET |
| Infraestrutura | AWS_REGION, AWS_BASE_ROLE_ARN, AWS_GATEWAY_ROLE_ARN, TF_STATE_BUCKET, ARTIFACTS_BUCKET |
| Banco | AWS_REGION, AWS_ROLE_ARN, TF_STATE_BUCKET |
| Autenticação | AWS_REGION, AWS_ROLE_ARN, TF_STATE_BUCKET, ARTIFACTS_BUCKET |

As roles têm ARN arn:aws:iam::121754142617:role/mecanica/pipelines/mecanica-<ambiente>-<componente>-github. Base/gateway usam as duas variáveis específicas da infraestrutura; os demais usam AWS_ROLE_ARN.

Gateway chamado por API/auth deve assumir a role gateway do mesmo ambiente, diretamente via OIDC do chamador, sem encadear a role da aplicação. Seu ARN é derivado do padrão acima e conferido pelo workflow reutilizável; não depende de uma variável extra nos repositórios chamadores. O workflow reutilizável e o checkout de seu código devem usar o mesmo SHA da infraestrutura.

## 3. Convenções de dados

- Campos SSM são String, tier Standard. Inteiros usam representação decimal; listas usam arrays JSON, sem valores separados por vírgula. Valores secretos nunca ficam nesses parâmetros.
- Strings obrigatórias não podem estar vazias nem conter placeholders. ARNs, IDs e endpoints de recursos serão publicados a partir dos recursos efetivamente criados.
- Datas são UTC em RFC 3339; revisões de código são SHAs completos; versões de contrato/pacote/esquema seguem SemVer. O segmento v1 representa o major do formato de metadados.
- Hash de arquivo é SHA-256 dos bytes publicados, em hexadecimal minúsculo com 64 caracteres. Digest ECR inclui o prefixo sha256:. ETag S3 não substitui SHA-256.
- Cada produtor escreve somente seu namespace. Cada consumidor usa seu próprio ambiente e valida conta, região, versão do formato e dependências.
- Nas tabelas, R = obrigatório para publicar release ready; C = condicional, com condição explícita. Ausência de campo C não equivale a uma string vazia.

## 4. Catálogo SSM

Cada linha abaixo é um campo relativo ao namespace da seção 2. Valores tipados também fazem parte de exports no manifesto da seção 7, que é a referência consistente para implantação. Parâmetros individuais servem para consulta/adaptação; ler campos isolados não comprova uma release pronta.

### 4.1. Base — proprietário: infraestrutura

| Campo | Tipo lógico | Regra | Consumidores / uso |
|---|---|---|---|
| vpc-id | string ID | R | Banco e função; validar VPC do ambiente |
| workload-subnet-ids | array de IDs | R, duas AZs | Função; conectividade privada |
| database-subnet-ids | array de IDs | R, duas AZs | Banco; DB subnet group |
| cluster-name | string | R | API e Job do banco; obtenção de kubeconfig |
| cluster-arn | ARN EKS | R | Identificação e validação do cluster |
| namespace | string | R | API e Job de inicialização |
| api-security-group-id | ID SG | R | Banco; grupo efetivamente usado pela origem das conexões dos pods |
| auth-security-group-id | ID SG | R | Função e banco; origem das conexões da Lambda |
| init-security-group-id | ID SG | R | Banco; origem das conexões do Job |
| api-service-name | string | R | API; alinhamento Deployment/Service |
| api-service-port | inteiro 1–65535 | R | API e infraestrutura; porta do Service |
| nlb-arn | ARN ELB | R | Gateway; destino do VPC Link |
| nlb-dns-name | hostname | R | Gateway; destino privado da integração |
| nlb-listener-port | inteiro 1–65535 | R | Gateway; integração com listener real |
| nlb-listener-protocol | TCP ou TLS | R | Gateway; conferir compatibilidade da integração |
| api-integration-uri | URI HTTP(S) privada | R | Gateway; endereço base do NLB, sem credenciais |
| ecr-repository-url | string | R | API; referência ao ECR compartilhado |
| jwt-secret-arn | ARN Secrets Manager | R | API e função; assinatura/validação |
| jwt-issuer | string | R, distinto entre ambientes | API e função; mesma configuração no ambiente |
| jwt-audience | string | R, distinto entre ambientes | API e função; mesma configuração no ambiente |
| newrelic-secret-arn | ARN Secrets Manager | C: observabilidade habilitada | Collector e extensão da função |
| newrelic-otlp-endpoint | URI HTTPS | C: observabilidade habilitada | Exportadores; região real da conta New Relic |
| otel-collector-endpoint | URI interna | C: OTel da API habilitado | API; Collector no EKS |

Grupos de segurança podem coincidir se as conexões de API/Job saírem pelos mesmos nós; publicar o ID efetivo, sem prometer isolamento por pod que não foi implementado. São os recursos da base que escolhem portas/protocolos e os publicam; o consumidor não adivinha esses valores. Prontidão da base exige NLB/listener disponíveis, controller saudável e cluster acessível; não exige API já implantada nem targets saudáveis, evitando dependência circular.

Issuer/audience de hom/prd serão inputs não secretos da base, com validação de diferença entre ambientes; não reutilizar implicitamente os valores locais de appsettings. A resolução/injeção dos valores JWT ocorre no runtime, preservando HS256 e os contratos HTTP existentes.

### 4.2. Banco — proprietário: banco de dados

| Campo | Tipo lógico | Regra | Consumidores / uso |
|---|---|---|---|
| cluster-arn | ARN RDS | R | Validação de identidade do Aurora |
| endpoint | hostname do writer | R | API e função; sem usuário/senha |
| port | inteiro | R, inicialmente 5432 | Consumidores PostgreSQL |
| database-name | string | R | API/função/Job |
| security-group-id | ID SG | R | Diagnóstico de conectividade |
| ssl-mode | string | R: VerifyFull | API/função/Job; cadeia de CA RDS confiável no runtime |
| api-secret-arn | ARN Secrets Manager | R | API; credencial de leitura/escrita |
| auth-secret-arn | ARN Secrets Manager | R | Função; leitura limitada a clientes |
| admin-secret-arn | ARN Secrets Manager | R | Somente Job de esquema/credenciais |
| schema-version | SemVer | R | API/função; compatibilidade SQL |
| schema-sha256 | hash SHA-256 | R | SQL aplicado; ordem e bytes do bundle definidos abaixo |
| initialized-at | timestamp UTC | R | Evidência de inicialização bem-sucedida |

A primeira versão formal do SQL será definida e versionada junto da implementação; o SQL legado não ganha versão 1.0.0 só por existir. No script único atual, schema-sha256 é o hash de sql/Init.sql. Ao dividir o SQL, produzir um bundle com ordem explícita e calcular seu hash; mudar a regra exige atualizar o contrato. Banco só publica ready após esquema/seeds/credenciais e verificações SQL concluídos, não apenas Aurora available. Não reaplicar seeds no startup da API.

### 4.3. API — proprietário: aplicação

| Campo | Tipo lógico | Regra | Consumidores / uso |
|---|---|---|---|
| image-uri | URL ECR com @sha256: | R | Implantação/rollback da API |
| deployment-name | string Kubernetes | R | Diagnóstico e verificação do rollout |
| openapi-version | SemVer | R | Gateway; contrato HTTP implantado |
| openapi-key | chave S3 | R | Gateway; contracts/api/<commit>/openapi.json |
| openapi-sha256 | hash SHA-256 | R | Gateway; conferir os bytes |
| auth-contracts-version | SemVer exata | R após extração do pacote | Compatibilidade JWT/documentos |
| smtp-secret-arn | ARN Secrets Manager | C: SMTP autenticado | Runtime da API |

Release pronta exige rollout concluído, readiness existente aprovada e imagem/digest verificados. A API não cria o Service, não publica ARN do NLB, não inicializa SQL e não precisa de URL pública do Gateway para seu primeiro deploy.

### 4.4. Função — proprietário: autenticação

| Campo | Tipo lógico | Regra | Consumidores / uso |
|---|---|---|---|
| function-name | string | R | Diagnóstico da Lambda |
| function-arn | ARN Lambda não qualificado | R | Identidade do recurso |
| invocation-arn | ARN Lambda qualificado por versão numérica | R | Gateway; destino exato, sem $LATEST |
| function-version | string numérica | R | Conferir qualificador de invocation-arn |
| zip-key | chave S3 | R | lambda/<commit>/function.zip |
| zip-sha256 | hash SHA-256 | R | Conferir bytes do ZIP; não confundir com CodeSha256 em base64 |
| openapi-version | SemVer | R | Gateway |
| openapi-key | chave S3 | R | contracts/auth/<commit>/openapi.json |
| openapi-sha256 | hash SHA-256 | R | Gateway |
| auth-contracts-version | SemVer exata | R | Pacote efetivamente consumido |

Publicar ready após atualização da função concluída, versão numérica publicada, conectividade verificada e testes de conformidade aprovados. Verificações não registram CPF real, token ou corpo de requisição. Gateway cria a permissão de invocação para essa mesma versão e restringe a origem à REST API/rota correspondente.

### 4.5. Gateway — proprietário: infraestrutura

| Campo | Tipo lógico | Regra | Consumidores / uso |
|---|---|---|---|
| rest-api-id | string ID | R | Diagnóstico |
| stage-name | hom ou prd | R | Identificação do ambiente |
| base-url | URI HTTPS | R | Clientes, testes e apresentação |
| vpc-link-id | string ID | R | Diagnóstico da entrada privada |
| deployment-id | string ID AWS | R | Deployment do API Gateway; diferente do deploymentId do manifesto |
| openapi-key | chave S3 | R | contracts/gateway/<ambiente>/<deploymentId>/openapi.json |
| openapi-sha256 | hash SHA-256 | R | Evidência da definição efetivamente aplicada |

Release pronta exige VPC Link disponível, integrações/permissão Lambda configuradas e verificação pelo Gateway. O manifesto registra as releases exatas de API/auth usadas na composição, não somente a versão mais recente encontrada no S3.

## 5. Segredos e contratos de runtime

| Segredo lógico / nome planejado | Proprietário | Campos mínimos do JSON | Leitores de valores |
|---|---|---|---|
| /mecanica/<ambiente>/shared/jwt | Base | key | Runtime API e Lambda |
| /mecanica/<ambiente>/shared/newrelic | Base | licenseKey | Collector EKS e extensão Collector da função |
| /mecanica/<ambiente>/database/api | Banco | username, password | Runtime API |
| /mecanica/<ambiente>/database/auth | Banco | username, password | Runtime Lambda |
| Credencial administrativa Aurora | Banco; preferir secret gerenciado pelo RDS | username, password; campos extras do RDS permitidos | Job de inicialização |
| /mecanica/<ambiente>/api/smtp | API, se SMTP autenticado | username, password | Runtime API |

Sempre consumir o ARN publicado; o ARN de secret tem sufixo gerado pela AWS e não deve ser montado por concatenação. Nome da credencial administrativa pode ser gerado pelo RDS. Nomes acima são a especificação alvo, não secrets já criados.

Host/porta/nome do banco ficam nos metadados; a conexão é montada no runtime. A API mapeia a conexão para ConnectionStrings__DefaultConnection, key para Jwt__Key e issuer/audience para Jwt__Issuer/Jwt__Audience. A função usa os mesmos valores JWT e sua própria credencial PostgreSQL, sem pooling. Usuário da função só recebe SELECT dos campos necessários de customers; API não utiliza a credencial administrativa.

Host/porta/TLS/remetente SMTP são configuração não secreta da API, definidos quando o provedor for escolhido. A API publica a referência SMTP em seu próprio namespace; a base não depende dela. New Relic exige região/endpoint reais antes de habilitar a exportação.

Roles de execução de pods/Job/Lambda têm acesso somente aos secrets necessários. Roles das pipelines não recebem leitura geral de valores; autorizações concretas de criação/injeção serão implementadas por componente. Não colocar senhas, chaves JWT, connection strings autenticadas, tokens ou chaves New Relic em Git, SSM String, outputs, manifestos, ZIPs, imagens ou logs. Marcar um output sensitive não elimina sua presença no estado Terraform; evitar geração/leitura de valores secretos pelo Terraform quando isso os persiste no estado.

Bootstrap atual não concede leitura de Secrets Manager nem provisionamento dos workloads. Este contrato descreve os acessos que serão implementados, não amplia as políticas existentes.

## 6. Artefatos, versões e compatibilidade

| Artefato | Produtor | Identificador imutável | Consumidor |
|---|---|---|---|
| Imagem API | API | ECR mecanica/api@sha256:<digest>; tag auxiliar sha-<commit> | Deploy da API |
| OpenAPI API | API | contracts/api/<commit>/openapi.json | Gateway |
| OpenAPI Validate | Função | contracts/auth/<commit>/openapi.json | Gateway |
| ZIP Lambda | Função | lambda/<commit>/function.zip | Deploy da função |
| NuGet | API / pipeline independente do pacote | packages/GerenciamentoMecanica.Auth.Contracts/<versao>/GerenciamentoMecanica.Auth.Contracts.<versao>.nupkg | Build da função |
| OpenAPI composto | Gateway | contracts/gateway/<ambiente>/<deploymentId>/openapi.json | Auditoria/rollback da entrada |

As chaves S3 são relativas ao bucket de artefatos da seção 2. Um arquivo .sha256 ao lado de cada artefato (nome completo mais sufixo .sha256) contém somente o hash minúsculo e uma quebra de linha. A release registra também esse hash; validar os bytes baixados contra a release e o sidecar. Não usar ETag como checksum.

Publicar com If-None-Match: *. Se a chave já existir, comparar checksum e reutilizar somente se idêntico; divergência encerra a publicação. Rerun não sobrescreve um ZIP recompilado diferente sob o mesmo commit. Em ECR, reutilizar imagem já publicada e seu digest; não mover uma tag imutável. Não publicar release ready antes de todos os objetos/sidecars estarem disponíveis.

Contratos e ZIPs por commit são independentes do ambiente: configurações e secrets são injetados na implantação. O composto do Gateway contém destinos específicos e tem chave por ambiente/tentativa. Não utilizar latest nem selecionar implicitamente o arquivo mais novo. S3 versionado não substitui essa regra de escrita.

SemVer:
- Contrato HTTP usa info.version no OpenAPI; major quebra compatibilidade, minor acrescenta de forma compatível, patch corrige sem quebrar.
- Metadados v1 têm schemaVersion 1.x.y; quebra cria namespace v2, mantendo transição explícita.
- Consumidores rejeitam major não suportado, campos obrigatórios ausentes e tipos inválidos; campos adicionais opcionais podem ser ignorados. Tornar obrigatório um campo antes opcional é quebra, não atualização minor.
- Pacote NuGet é consumido por versão exata; API referencia o projeto local, função baixa o nupkg para feed local antes do restore. Build não exige checkout de outro repositório nem banco disponível.
- API/função declaram intervalos SQL/HTTP/pacote suportados em configuração versionada junto ao código. A release registra o intervalo aceito e a versão efetivamente consumida. SemVer não substitui testes.
- Mudanças incompatíveis exigem atualização coordenada; rollback de código só com SQL compatível. Não há promessa de rollback de dados.

O Gateway compõe o OpenAPI dos dois backends implantados e adiciona extensões AWS. POST /customers/validate pertence à função; colisões de path/method e schemas incompatíveis fazem a composição falhar. Os códigos/payloads/JWT vêm de ACESSO_E_AUTENTICACAO.md e dos contratos produzidos pelo código; não duplicar aqui outra definição HTTP. Alteração da API não atualiza a função se os contratos forem compatíveis.

## 7. Manifesto, prontidão e registro de tentativas

Cada componente publica:
- /mecanica/<ambiente>/<componente>/v1/release: JSON da última implantação pronta para consumo.
- /mecanica/<ambiente>/<componente>/v1/attempts/<deploymentId>: JSON da tentativa, incluindo falha, implantação bloqueada ou sucesso.
- Os campos individuais da seção 4 no mesmo namespace.

deploymentId é <github-run-id>-<run-attempt>-<componente>; o repositório produtor diferencia tentativas entre componentes. Para gateway reutilizável, acrescentar o nome do repositório chamador ao ID, evitando colisões entre runs de repositórios diferentes. Ele é um identificador de operação, não uma versão SemVer.

### 7.1. Estrutura mínima

| Propriedade JSON | Regra |
|---|---|
| schemaVersion | SemVer; formato inicial 1.0.0 |
| environment / component | hom ou prd / nome lógico da seção 1 |
| accountId / region | Conta e região da seção 2 |
| deploymentId | Identificador único da tentativa |
| generation | UUID da geração da base; novo ao recriar o ambiente, preservado em atualização comum |
| source | Objeto repository + commit; gateway reutilizável registra também callerRepository, callerCommit e workflowCommit |
| status | release aceita somente ready; tentativa aceita running, ready, failed, blocked ou destroyed |
| recordedAt | Timestamp UTC |
| exports | Objeto com campos da seção 4 e tipos lógicos, sem valores secretos |
| dependencies | Mapa componente -> deploymentId, generation e sourceCommit consumidos; base usa objeto vazio |
| artifacts | Lista: kind, version quando aplicável, uri, sha256; para imagem, uri contém o digest e sha256 é o hash sem o prefixo sha256: |
| compatibility | Mapa contrato -> intervalo SemVer suportado; vazio quando não aplicável |
| verification | Lista de nomes dos checks aprovados; falha em tentativa usa errorCode e mensagem sanitizada |

Cada JSON deve caber em 4 KB serializado em UTF-8 sem espaços de formatação (limite Standard); o publicador verifica o tamanho antes de escrever e falha se exceder. Não promover automaticamente para Advanced nem truncar campos. Evoluir o formato antes de ultrapassar o limite. Logs completos ficam na execução da pipeline, não no manifesto.

Exemplo ilustrativo de tentativa bloqueada (não é uma release pronta nem um recurso existente):

~~~json
{
  "schemaVersion": "1.0.0",
  "environment": "hom",
  "component": "api",
  "accountId": "121754142617",
  "region": "us-east-1",
  "deploymentId": "100001-1-api",
  "generation": null,
  "source": {
    "repository": "pknfelps/GerenciamentoMecanicaSistema",
    "commit": "1111111111111111111111111111111111111111"
  },
  "status": "blocked",
  "recordedAt": "2026-09-24T12:00:00Z",
  "exports": {},
  "dependencies": {},
  "artifacts": [],
  "compatibility": {},
  "verification": [],
  "errorCode": "BASE_NOT_READY",
  "message": "Base do ambiente ausente; deploy nao executado."
}
~~~

generation null só é permitido em tentativa bloqueada antes de obter a base. Tentativas running/failed podem conter exports parciais; release pronta exige generation, exports obrigatórios e evidências. Referências/digests do exemplo não devem ser copiados para configuração real.

### 7.2. Protocolo de publicação e consumo

1. Adquirir a coordenação do ambiente/operação; capturar releases das dependências e validar formato/compatibilidade/geração e recursos reais.
2. Registrar tentativa running. Antes de iniciar alteração de recursos, remover o parâmetro release do próprio componente para impedir que outra implantação consuma metadados antigos como prontos.
3. Executar a mudança, verificar recursos e publicar campos individuais e artefatos. Conferir novamente as revisões das dependências.
4. Após sucesso, gravar a tentativa ready e publicar o manifesto completo em release por último. Exports nesse JSON é o snapshot autoritativo; os consumidores não montam snapshots a partir de leituras soltas.
5. Em falha após início da mudança, registrar failed e manter release ausente até recuperação verificada. Falha de build antes de alterar recursos não invalida a release em execução. Ambiente ausente gera blocked, não sucesso de deploy.
6. No descarte, invalidar release antes de remover recursos e registrar destroyed. Remover campos ativos obsoletos, preservando registros de tentativa para diagnóstico. Não limpar o namespace de outro produtor/ambiente.

Antes de agir, consumidor relê release e confirma o mesmo deploymentId/generation, consulta os recursos necessários e verifica dependências. Isso detecta referências obsoletas, mas não oferece trava transacional entre repositórios. O mecanismo de coordenação ainda será implementado antes de permitir deploys concorrentes. Até lá, operações de provisionamento/deploy/descarte do mesmo ambiente são explicitamente sequenciais; hom/prd permanecem independentes. SSM não será usado como mutex improvisado; lock Terraform e concurrency por repositório não são trava global.

Histórico de tentativas pode crescer; definir retenção operacional sem apagar a release atual nem evidência necessária para rollback. Nenhuma política automática de limpeza é criada por esta especificação.

## 8. Dependências e prontidão por consumidor

| Consumidor | Dependências mínimas | Condição adicional |
|---|---|---|
| Banco | Base ready | Rede/cluster disponíveis e Job autorizado; secrets preparados pelo banco |
| API | Base + banco ready | Esquema compatível; imagem por digest; secrets e namespace disponíveis |
| Função | Base + banco ready; pacote exato disponível | Schema compatível e pacote testado; versão Lambda publicada |
| Gateway | Base + API + função ready | Contratos e destinos correspondem às releases; IAM de invocação da versão |
| Build/pacote | Código, dependências de build e bootstrap quando publicar | Não depende de Aurora/EKS/Gateway |

Primeira implantação: bootstrap -> base -> banco -> API/função -> gateway -> teste integrado. O pacote pode ser publicado antes da criação do banco. Base não aguarda aplicações; API/função não aguardam Gateway. As pipelines podem construir artefatos em paralelo; a mutação do mesmo ambiente segue a coordenação da seção 7.

Gateway inclui no manifesto final as revisões transitivas de base/banco/API/função e as referências exatas de imagem, ZIP, OpenAPI e pacote necessárias à rastreabilidade; obter esses valores dos manifestos validados. Se esse registro ultrapassar o limite Standard, revisar o formato antes de implementar, sem descartar informações silenciosamente.

No descarte: gateway -> consumidores -> banco -> base. Remover Service e aguardar limpeza do NLB enquanto controller/cluster ainda existem. Preservar bootstrap e o outro ambiente. Banco de demonstração pode ser recriado sem migração de dados; isso não autoriza excluir backups/snapshots alheios.

## 9. Permissões atuais e trabalho de implementação

O bootstrap já separa estados e namespaces SSM por ambiente/componente. Base e banco não recebem acesso ao bucket de artefatos; por isso seus manifestos usam SSM e seu SQL segue versionado no próprio repositório. API publica contracts/api e packages/GerenciamentoMecanica.Auth.Contracts; função lê o pacote e publica contracts/auth e lambda; gateway lê contracts/api e contracts/auth e publica contracts/gateway. As chaves desta especificação respeitam esses prefixos.

Consulta de metadados está limitada ao ambiente, mas não deve ser usada para descobrir secrets em claro. Escrita somente no namespace do produtor. Permissões para recursos de workloads, leitura runtime de secrets, controle de publicação e coordenação serão detalhadas nos respectivos Terraform/workflows.

| Entrega | Responsável |
|---|---|
| Pacote CPF/CNPJ e JWT, versão exata e testes de compatibilidade | API; consumidor no repo da função |
| Publicadores SSM, verificação de manifests e preparação de secrets | Cada produtor |
| Infraestrutura privada, NLB, acesso EKS e roles de execução | Infraestrutura |
| Aurora, usuários/permissões SQL, versão/hash, Job e CA/TLS | Banco |
| Build/deploy de imagem e adaptação de configuração | API |
| Build/deploy ZIP, configuração e versão numérica | Autenticação |
| Composição OpenAPI, workflow reutilizável e coordenação de concorrência | Infraestrutura, integrada aos chamadores |
| Testes de recusa OIDC, Gateway via API/auth e permissões efetivas | Workflows dos componentes |

Critério documental: campos, produtor/consumidor, formatos, versões, falhas e referências explícitos, com READMEs apontando para esta fonte. Critério de implementação posterior: demonstrar publicações reais, consumo independente, recusa de contrato incompatível/referência obsoleta e implantação/descarte isolados. Esta documentação não equivale à aprovação desses testes.

## Referências técnicas

- [Responsabilidades e ciclo de entrega](rfcs/002-ENTREGA.md).
- [Decisão de ambientes e estados](adrs/004-ENTREGA-E-AMBIENTES.md).
- [Tier Standard e limite dos parâmetros SSM](https://docs.aws.amazon.com/systems-manager/latest/userguide/parameter-store-advanced-parameters.html).
- [Escrita condicional S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/conditional-writes.html).
