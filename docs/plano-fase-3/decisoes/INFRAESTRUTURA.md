# Infraestrutura, Gateway e Lambda — D01/D09.2

[Índice central](../../../PLANO_FASE_3.md)

> Decisões preservadas com suas datas. Para estado/etapa atual, consultar o índice central; complementos posteriores prevalecem sobre pendências antigas. Ler somente o assunto necessário.

<a id="d01-1"></a>

**D01.1 — Continuidade AWS/EKS (aceita em 2026-09-13):**

- **Origem:** usuário confirmou manutenção de AWS EKS com reaproveitamento do Terraform atual.
- **Motivação:** aproveitar a infraestrutura já desenvolvida para a Fase 2.
- **Alternativa descartada neste escopo:** migrar nuvem ou substituir o serviço Kubernetes.
- **Consequência:** evoluir os recursos existentes e sua automação; não recriar ou destruir o ambiente apenas por separar repositórios. Topologia de rede, ambientes e divisão de estados Terraform continuam a definir.
- **Estado:** decisão aceita; banco posteriormente definido em D01.2.

**Critério vigente de validação futura (D01.2):** com probes atuais, retomar e verificar o banco antes dos testes/gravação; confirmar prontidão e operação durante a janela. Depois de encerrar os consumidores e suas conexões, comprovar pausa real e computação a zero, registrando capacidade faturada e custos remanescentes. Não exigir pausa enquanto as probes estiverem consultando o banco. Nenhum ajuste de readiness foi implementado.

Referências: [auto-pause, conexões, proxy e retomada do Aurora](https://docs.aws.amazon.com/AmazonRDS/latest/AuroraUserGuide/aurora-serverless-v2-auto-pause.html), [preços Aurora Standard](https://aws.amazon.com/rds/aurora/pricing/), [pooling e timeouts do Npgsql](https://www.npgsql.org/doc/connection-string-parameters.html) e [semântica das probes Kubernetes](https://kubernetes.io/docs/concepts/workloads/pods/probes/).

<a id="d01-2"></a>

**D01.2 — Aurora PostgreSQL Serverless v2 com probes preservadas (aceita em 2026-09-13):**

- **Origem:** usuário escolheu Aurora pelo foco serverless da fase, capacidade elástica e uso eventual, determinando não alterar as probes.
- **Escolha:** Aurora PostgreSQL-Compatible Serverless v2, provisionado por Terraform no repositório de infraestrutura do banco. Substitui a proposta anterior de RDS PostgreSQL com instância de tamanho fixo.
- **Probes:** preservar endpoints, implementação dos health checks e parâmetros atuais. Readiness continua verificando PostgreSQL a cada 10 segundos, com timeout de conexão de 2 segundos e timeout de probe de 3 segundos. Não implementar o modo de readiness independente do banco nem criar diagnóstico separado como parte desta escolha.
- **Operação:** banco ativo durante testes e gravação. Executar preflight que retome o banco e aguarde uma conexão bem-sucedida antes de considerar o ambiente pronto para uso; esse procedimento não depende de alongar as probes. Ao terminar, encerrar os consumidores e suas conexões, coordenando a API com o HPA atual, que possui mínimo de uma réplica. Definir esse ciclo na entrega por ambiente.
- **Pausa e custo:** planejar capacidade mínima zero e versão compatível com auto-pause. Enquanto as probes/conexões mantiverem o banco ativo, haverá cobrança de computação. Depois de fechadas as conexões e transcorrido o intervalo configurado, a computação pode pausar; armazenamento e outros itens continuam cobrados. Economia depende do uso efetivo, não é garantia de custo inferior ao RDS em qualquer cenário.
- **Dimensionamento:** não escolher classe fixa como db.t4g.micro. Ainda provisionar o cluster e recurso(s) de instância db.serverless, definir mínimo/máximo de ACUs, intervalo de pausa e topologia. Serverless automatiza a capacidade dentro desses limites; não elimina a configuração da infraestrutura.
- **Alternativas descartadas neste escopo:** RDS PostgreSQL provisionado, Lightsail/Neon e alteração de readiness para permitir pausa mantendo a API ligada. Evitar RDS Proxy na configuração destinada a pausar, pois ele mantém conexões abertas; garantir que a função e os demais consumidores não impeçam a pausa entre sessões.
- **Estado:** banco e preservação das probes decididos; provisionamento, inicialização e validação ainda pendentes. D08.1 determina banco descartável sem migração de dados antigos; auto-pause continua útil enquanto o ambiente existir. D01 continua aberta para configurações detalhadas.

<a id="d01-3"></a>

**D01.3 — AWS API Gateway REST API Regional (serviço aceito em 2026-09-13; tipo aceito em 2026-09-14):**

- **Escolha do usuário:** utilizar AWS API Gateway REST API, seguindo a proposta Regional, pelos benefícios de tracing, logs, possibilidade de cache e criação por importação de OpenAPI. A topologia de integração permanece pendente.
- **OpenAPI:** versionar uma definição compatível com OpenAPI 3.0 e usá-la para criar/atualizar o Gateway pela pipeline/Terraform. Incluir as extensões AWS de integração e os valores de ambiente; o documento da API .NET, isoladamente, não configura os destinos no EKS e na função. Incluir também `POST /customers/validate`, servido pela função. Definir propriedade e sincronização desse contrato em E0.1/E0.4. Importação OpenAPI também existe em HTTP API; é uma facilidade adotada, não um diferencial exclusivo da REST API.
- **Tracing, logs e cache:** planejar tracing nativo X-Ray no Gateway e logs de acesso/execução, coordenando instrumentação da API/função com D04. Cache é uma possibilidade de evolução, sem habilitação automática nesta decisão; avaliar rotas, TTL, isolamento por usuário e custo antes de adotá-lo.
- **Contrato já definido:** `POST /customers/validate` encaminhado à função serverless; `/authentication` e operações da aplicação encaminhados à API no EKS. Preservar os contratos D02/D03, incluindo métodos, caminhos, status, corpo e cabeçalho Authorization.
- **Autenticação preservada:** API .NET valida JWT HS256 e aplica permissões por endpoint. O JWT authorizer nativo de HTTP API suporta algoritmos RSA, portanto não atende ao token atual. Não incluir troca de algoritmo ou authorizer adicional nesta decisão.

Definições ainda pendentes, em ordem de discussão:

| Tema | Proposta para avaliação | O que falta fechar |
|---|---|---|
| Tipo do Gateway | REST API Regional aceita em 2026-09-14; criação/atualização por OpenAPI | Tipo definido; detalhar configuração e propriedade do contrato |
| Conexão com EKS | Gateway público HTTPS -> VPC Link -> NLB interno -> API, aceita em D01.4 | Detalhar controlador, destinos, regras de rede e outputs para o Gateway |
| Publicação e ambientes | Usar inicialmente o endereço HTTPS fornecido pela AWS | Definir isolamento de homologação/produção, stages e necessidade de domínio/CORS conforme consumidores |
| Operação e observabilidade | Logs de acesso estruturados e de execução, métricas e tracing nativo do Gateway | Definir retenção, amostragem, limites de requisições e timeouts; coordenar traces da API/função e propagação de contexto com D04 |
| Terraform e pipelines | Versionar Gateway, rotas, integrações e VPC Link nos quatro repositórios previstos | Definir proprietário único de cada recurso, outputs compartilhados e ordem de implantação, evitando dependências circulares |

**Cuidados de implementação após as decisões:** validar os caminhos enviados ao backend na integração REST escolhida, preservando as rotas da aplicação; não transpor automaticamente a sintaxe de mapeamento de HTTP API. Definir HTTP/HTTPS entre Gateway e balanceador. Integrar o preflight do Aurora (D01.2) ao teste do fluxo completo. A integração privada não torna o endpoint público do Gateway uma API privada.

**Estado:** AWS API Gateway REST API Regional e uso de OpenAPI aceitos; VPC Link e NLB interno posteriormente aceitos em D01.4. Configurações detalhadas pendentes. E0.1/E0.4 continuam em andamento; nenhum recurso criado ou código alterado.

Referências oficiais consultadas em 2026-09-13: [HTTP API versus REST API](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-vs-rest.html), [integrações privadas e mapeamento de caminho](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-develop-integrations-private.html) e [algoritmos do JWT authorizer](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-jwt-authorizer.html).

Referências da decisão de 2026-09-14: [importação OpenAPI para REST API](https://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-import-api.html) e [suporte OpenAPI em HTTP API](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-open-api.html).

<a id="d01-4"></a>

**D01.4 — Integração privada do EKS e imagens no ECR (aceita em 2026-09-14):**

- **Escolha do usuário:** seguir a topologia proposta e passar a usar Amazon ECR para as imagens. Entrada: `Cliente -> API Gateway REST API Regional público -> VPC Link -> NLB interno -> pods da API`.
- **Reaproveitamento:** manter EKS, Deployment, modelo Service `type: LoadBalancer`, HPA e probes. Configurar explicitamente NLB interno e o controlador que o provisiona/atualiza. Não adicionar ALB ou balanceador à frente do Gateway. Tipo dos destinos (IP/nós) e configuração do controlador ainda serão detalhados.
- **Rede:** manter a VPC e ampliar o Terraform para subnets privadas destinadas aos nós/workloads, balanceador interno e Aurora. Configurar rotas e security groups para os fluxos necessários. Não interpretar como remoção automática de todas as subnets públicas nem fechamento automático do endpoint administrativo do EKS; ambos dependem da estratégia de saída e acesso da pipeline.
- **Registro:** ECR privado substitui Docker Hub na publicação e implantação das imagens da aplicação. Provisionar repositório(s) por Terraform, definir tags imutáveis por versão/commit ou digest e retenção que preserve versões necessárias para rollback. Atualizar build/push, referência de imagem no deploy e permissões de publicação/pull. A política ECR de pull já existe no IAM dos nós; conferir sua adequação na implementação.
- **Conectividade:** ECR permite pulls privados com endpoints `ecr.api`, `ecr.dkr` e acesso às camadas via S3. Detalhar essa configuração junto dos demais serviços AWS e dependências externas antes de decidir dispensar NAT. Inventariar imagens de add-ons e serviços externos (por exemplo SMTP); escolher ECR para a aplicação não elimina automaticamente todas as dependências de internet. Definir também como a pipeline acessará o endpoint administrativo do EKS.
- **Escopo:** decisão de registro não determina empacotamento da futura função (ZIP ou imagem); tratar no componente serverless. Não alterar o desenvolvimento local como consequência da mudança de registro.
- **Estado:** topologia, reaproveitamento e ECR definidos; implementação na E2/E3/E6. E0 permanece em andamento para função, rede detalhada, ambientes e propriedade de recursos.

Referência: [endpoints privados do ECR e acesso às camadas no S3](https://docs.aws.amazon.com/AmazonECR/latest/userguide/vpc-endpoints.html).

<a id="d01-5"></a>

**D01.5 — Função de validação AWS Lambda (aceita em 2026-09-14):**

- **Origem:** usuário concordou com as sugestões apresentadas e determinou sua adoção.
- **Execução:** AWS Lambda em C#/.NET 10, alinhada ao projeto atual e ao runtime gerenciado `dotnet10`. Handler dedicado com integração Lambda proxy da REST API para `POST /customers/validate`; implementação pequena, separando entrada HTTP, validação, consulta e emissão do JWT. Não precisa subir toda a API ASP.NET na função.
- **Empacotamento:** ZIP com runtime gerenciado, publicado pela pipeline do repositório da função. ECR permanece o registro das imagens da aplicação; função em ZIP não precisa de imagem. Container foi avaliado e não adotado para esta função. Sem Native AOT ou provisioned concurrency nesta primeira implementação.
- **Acesso:** consulta direta e parametrizada ao Aurora PostgreSQL via Npgsql/Dapper, função conectada à VPC/subnets privadas, TLS e credencial de banco com leitura limitada aos dados necessários. Consultar CPF por igualdade, retornando ID, nome e status para emitir token de cliente ativo. Reutilizar a regra e o formato canônico existentes de CPF (hoje o domínio armazena com máscara); não modificar o formato persistido implicitamente.
- **Tradeoff:** consulta direta evita dependência de disponibilidade da API EKS e uma chamada HTTP, mas acopla a função ao esquema de clientes. Mudanças desse contrato exigem coordenação entre repositórios e script de criação do banco. Consulta por API interna centraliza o acesso ao cadastro, porém exige endpoint interno autenticado e faz a validação depender do EKS; não está recomendada inicialmente.
- **Regras reaproveitadas:** preservar contrato D02.2 e validar comportamento com os mesmos casos de CPF do domínio. Na separação de repositórios, definir como distribuir/versionar o pequeno código de validação e contrato JWT sem dependência de checkout local de outro repositório ou de todo o serviço de aplicação.
- **Conexões e custo:** inicialmente sem RDS Proxy, pois impede auto-pause do Aurora. Usar `Pooling=false` apenas na função e conexão assíncrona fechada ao final de cada invocação, inclusive em erro, para não deixar conexões ociosas em ambientes Lambda congelados; aceitar custo de abertura por chamada neste perfil de uso. Limitar concorrência conforme capacidade do Aurora e testes. Preservar pooling/probes atuais da API e preflight de D01.2. Memória, timeout e limite numérico de concorrência serão calibrados nos testes; falhas técnicas continuam 5xx.
- **Segredos:** Secrets Manager para credencial de leitura e chave HS256 compartilhada com a API, com permissões IAM e conectividade adequadas. Issuer/audience/expiração e claims seguem D02.2, incluindo ID estável e Role Customer. Não usar cache de respostas/tokens na rota de validação.
- **Observabilidade:** logs JSON com correlação, duração e resultado; integração com tracing do Gateway e spans de consulta/emissão, sem registrar CPF, segredo ou token. Testar 400/401/200, compatibilidade do token com a API, falha de banco e liberação de conexões.
- **Estado:** componente consolidado como especificação; implementação pendente em E2/E3/E5/E6. Dimensionamento, distribuição do código reutilizado e configuração por ambiente permanecem no detalhamento de E0; E0.1/E0.4 não estão concluídas.

Referências consultadas: [runtime .NET no Lambda](https://docs.aws.amazon.com/lambda/latest/dg/lambda-csharp.html), [acesso Lambda ao RDS/Aurora](https://docs.aws.amazon.com/lambda/latest/dg/services-rds.html), [condições que impedem auto-pause](https://docs.aws.amazon.com/AmazonRDS/latest/AuroraUserGuide/aurora-serverless-v2-auto-pause.html) e [pooling Npgsql](https://www.npgsql.org/doc/connection-string-parameters.html).

<a id="d09-2"></a>

**D09.2 — Acesso administrativo e saída de rede (aceita em 2026-09-15):**

- **Decisão para o uso educacional eventual:** manter runners hospedados pelo GitHub e habilitar os endpoints público e privado do servidor Kubernetes do EKS. A pipeline usa o público com OIDC, role autorizada no cluster e permissões Kubernetes por escopo; nós/workloads usam o privado. O endpoint administrativo é distinto da API da oficina: o tráfego de negócio continua Gateway -> VPC Link -> NLB interno -> pods privados.
- **Tradeoff de acesso aceito:** o endpoint administrativo continua alcançável pela internet, protegido por autenticação/autorização. Runners comuns do GitHub não têm IP de saída fixo; esta decisão não inclui uma allowlist restrita a um IP nem recomenda liberar todas as faixas do GitHub. OIDC por si só não concede acesso Kubernetes: configurar o vínculo da role e as permissões do cluster. Administração exclusivamente privada fica como alternativa futura, fora da implementação escolhida.
- **Alternativa não adotada — administração exclusivamente privada:** executar os jobs que acessam Kubernetes em runners efêmeros do CodeBuild dentro da VPC, mantendo GitHub Actions como orquestrador. Exige projeto/integração GitHub, permissões, rede e mudança de runs-on; bootstrap da base/runner deve funcionar sem depender do próprio cluster. CodeBuild precisa de saída para GitHub e dependências. Referência Linux build.general1.small: US$ 0,005/minuto; 100 minutos = US$ 0,50 de computação antes de franquias, rede, logs e impostos. Vantagem: retirar o endpoint administrativo da internet; custo de execução baixo para uso eventual, mas maior configuração.
- **Saída definida:** um NAT Gateway zonal por ambiente provisionado em subnet pública, com Elastic IP, e rotas de saída das subnets privadas de workloads. Permite acesso TLS a New Relic, serviços AWS e demais dependências externas; SMTP depende do provedor/portas a definir. O NAT não permite iniciar conexões da internet para os workloads. Aurora fica em subnets privadas de banco, sem rota de saída à internet por padrão. O isolamento físico entre ambientes ainda será detalhado em E0.4.
- **S3 e ECR:** adicionar gateway endpoint do S3, sem tarifa adicional do endpoint, para que esse tráfego (incluindo camadas das imagens ECR) não passe pelo NAT. Inicialmente acessar APIs ECR/Secrets Manager e outros serviços pelo NAT, evitando multiplicar endpoints de interface pagos. Repositório ECR privado usa autorização IAM e não obriga usar PrivateLink. Reavaliar endpoints de interface se volume, requisitos de rede ou custos justificarem; eles não resolvem a saída para New Relic.
- **Custo e disponibilidade:** referência us-east-1: NAT US$ 0,045/h + US$ 0,045/GB processado, mais US$ 0,005/h pelo IPv4 público. Um NAT e um IP por 20 horas = US$ 1,00 de base; por 730 horas = US$ 36,50. Acrescentar tráfego processado, eventual transferência entre AZs/saída à internet e impostos; estes valores não são o custo total do ambiente. Um NAT único reduz custo, mas sua indisponibilidade afeta a saída de outras AZs; tradeoff aceito para demonstração, sem promessa de alta disponibilidade de saída.
- **Ciclo operacional:** criar a saída antes dos consumidores e removê-la após limpeza dos recursos que dela dependem; excluir NAT e liberar Elastic IP no descarte para interromper suas cobranças. Preservar backend/identidade conforme D09.1. Encerrar workloads/conexões também continua necessário para a pausa do Aurora, sem alterar probes.
- **Validação planejada:** executar deploy pelo runner, inicialização SQL via job, pull ECR por nó/pod novo, envio de telemetria e teste de notificações; conferir que NLB/banco/pods não recebem entrada pública direta e que o descarte elimina NAT/Elastic IP. Decisão consolidada como especificação; E0.4 permanece aberta para isolamento/capacidade e demais configurações operacionais. Nenhuma configuração aplicada.

Fontes: [endpoints EKS](https://docs.aws.amazon.com/eks/latest/userguide/cluster-endpoint.html), [IPs dos runners GitHub](https://docs.github.com/en/actions/reference/runners/github-hosted-runners), [runners CodeBuild](https://docs.aws.amazon.com/codebuild/latest/userguide/action-runner.html), [rede CodeBuild](https://docs.aws.amazon.com/codebuild/latest/userguide/vpc-support.html), [preços CodeBuild](https://aws.amazon.com/codebuild/pricing/), [gateway endpoint S3](https://docs.aws.amazon.com/vpc/latest/privatelink/gateway-endpoints.html), [preços VPC](https://aws.amazon.com/vpc/pricing/) e [exemplo AWS us-east-1 de NAT/IPv4](https://aws.amazon.com/blogs/networking-and-content-delivery/identify-and-optimize-public-ipv4-address-usage-on-aws/).
