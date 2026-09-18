# Avaliações históricas e alternativas da Fase 3

[Índice central](../../../PLANO_FASE_3.md)

> Referência histórica: propostas e estados anteriores podem ter sido substituídos. Não carregar este arquivo por padrão; consultar para recuperar justificativas ou evolução das decisões.

**E0.1/E0.4 — Pauta de componentes iniciada em 2026-09-13:**

Inspeção dos arquivos locais de Terraform, Kubernetes e eventos de notificação; não constitui verificação de recursos ativos na AWS. AWS/EKS, Aurora Serverless v2, AWS API Gateway, integração privada com NLB, ECR e Lambda/Secrets Manager aceitos em D01.1–D01.5; demais componentes e configurações ainda a definir.

| Componente | Base atual reconferida | Definição pendente |
|---|---|---|
| Aplicação principal | API .NET em Kubernetes, probes, HPA e acesso PostgreSQL | Reaproveitar esse componente e concretizar a nova entrada pelo Gateway |
| Nuvem e cluster | Terraform AWS/EKS com VPC, subnets públicas, node group e add-ons | Reaproveitar Terraform; nós/workloads em subnets privadas (D01.4); detalhar saída/endpoints e acesso administrativo |
| Gateway | Service Kubernetes do tipo LoadBalancer, sem configuração explícita de balanceador interno | REST API Regional com OpenAPI (D01.3), VPC Link e NLB interno; preservar modelo Service LoadBalancer (D01.4) |
| Registro de imagens | Pipeline publica API no Docker Hub; manifest referencia essa imagem; IAM dos nós já inclui política de pull ECR | Migrar imagens da aplicação para ECR privado (D01.4); definir repositórios, retenção, permissões e conectividade |
| Função de validação | Responsabilidade e contrato definidos, função ainda inexistente | D01.5 aceita: AWS Lambda .NET 10/ZIP, consulta direta ao Aurora, sem proxy e pooling na função; calibrar capacidade e detalhar permissões |
| Banco gerenciado | PostgreSQL no cluster com inicialização por SQL e volume EBS | Aurora PostgreSQL Serverless v2 escolhido (D01.2); definir versão compatível com pausa, faixa de ACUs, rede, topologia e inicialização em banco vazio (D08.1) |
| Observabilidade | Metrics Server para HPA e logs locais; não há integração completa | New Relic + OpenTelemetry/OTLP aceitos (D04.1); detalhar Collector, coleta Kubernetes/Lambda e sinais nativos AWS |
| Notificações | Dispatcher executa handlers no processo da API; envio por SMTP | Manter o fluxo atual; migração serverless documentada como melhoria futura para apresentação (D06.1) |
| Configuração e entrega | Secrets Kubernetes, Terraform e CI/CD existentes | Definir distribuição de segredos/configuração entre API e função, dono de cada recurso entre os quatro repositórios e isolamento por ambiente |

Pontos de integração a detalhar antes de fechar o diagrama: Gateway -> VPC Link -> NLB interno -> API (topologia aceita); função -> cadastro; API/função -> banco; componentes -> observabilidade. O Terraform atual cria somente subnets públicas, portanto a topologia aceita exige ampliar a rede. O EBS CSI existe para o banco atual; revisar sua necessidade depois da migração conforme os workloads restantes, sem presumir sua remoção.

Referências técnicas consultadas para avaliar as propostas: [integrações privadas de HTTP API com ALB/NLB e VPC Link](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-develop-integrations-private.html), [Lambda com RDS, acesso direto ou por proxy](https://docs.aws.amazon.com/lambda/latest/dg/services-rds.html) e [RDS PostgreSQL](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_GettingStarted.CreatingConnecting.PostgreSQL.html). Escolher um serviço não implica provisioná-lo nesta etapa.

**Avaliação inicial de custo do banco — 2026-09-13 (proposta, sem contratação):**

Região de referência: `us-east-1`, conforme `deploy/terraform/terraform.tfvars.example`. Estimativa com 730 horas/mês, preço On-Demand, 20 GiB gp3, sem créditos, impostos ou serviços adicionais. Fonte: [catálogo oficial AmazonRDS us-east-1](https://pricing.us-east-1.amazonaws.com/offers/v1.0/aws/AmazonRDS/current/us-east-1/index.json), publicação consultada de 2026-09-11T12:45:02Z.

| Configuração | Computação | Armazenamento | Base estimada/mês |
|---|---|---|---|
| PostgreSQL db.t4g.micro Single-AZ | 730 × US$ 0,016 = US$ 11,68 | 20 × US$ 0,115 = US$ 2,30 | US$ 13,98 |
| PostgreSQL db.t4g.small Single-AZ | 730 × US$ 0,032 = US$ 23,36 | US$ 2,30 | US$ 25,66 |
| PostgreSQL db.t4g.micro Multi-AZ, um standby | 730 × US$ 0,032 = US$ 23,36 | 20 × US$ 0,23 = US$ 4,60 | US$ 27,96 |

- Duas instâncias micro Single-AZ independentes para homologação/produção: base de US$ 27,96/mês. Compartilhar uma instância reduziria custo, mas também isolamento; isso não foi decidido.
- Custos acima são somente banco, não o total AWS nem a diferença líquida após retirar o PostgreSQL/EBS atual. Excluem tráfego, backups excedentes, créditos de CPU excedentes, proxy, observabilidade, rede, endereços públicos e suporte estendido quando aplicáveis. Não pressupor gratuidade/créditos sem verificar a conta.
- RDS micro Single-AZ foi a proposta econômica inicial para estudo e carga pequena no cenário de funcionamento contínuo; reavaliar com o uso eventual definido abaixo. Não fornece standby/failover Multi-AZ. Capacidade e disponibilidade desejadas permanecem decisões explícitas.
- Aurora PostgreSQL Serverless v2 Standard: catálogo indica US$ 0,12/ACU-h. Se permanecer em 0,5 ACU durante 730 horas, são US$ 43,80 só de computação, mais armazenamento/I/O. Com auto-pause, computação pode zerar nos intervalos pausados; isso depende da ausência de conexões e da versão/configuração compatível.
- O readiness atual abre conexão ao banco a cada 10 segundos por pod, com timeout de 2 segundos. Inferência: esse acesso periódico impede contar com economia por pausa enquanto a API permanecer operando assim; o tempo de retomada de um banco pausado também exige avaliar os timeouts.
- Alternativas avaliadas: Lightsail Database parte de US$ 15/mês no plano Standard; Neon oferece plano gratuito limitado, mas acrescenta fornecedor externo e exige validar conectividade/provisionamento Terraform. Avaliação preservada como histórico; escolha final registrada em D01.2: Aurora PostgreSQL Serverless v2.
- Parar RDS suspende cobrança da instância enquanto parado, mas mantém armazenamento/backup e a AWS reinicia automaticamente após sete dias. Evitar assumir que parar elimina toda cobrança.

Fontes complementares: [preços e adicionais do RDS PostgreSQL](https://aws.amazon.com/rds/postgresql/pricing/), [auto-pause do Aurora](https://docs.aws.amazon.com/AmazonRDS/latest/AuroraUserGuide/aurora-serverless-v2-auto-pause.html), [Lightsail](https://aws.amazon.com/lightsail/pricing/), [limites gratuitos do Neon](https://neon.com/blog/how-to-make-the-most-of-neons-free-plan), [parada temporária do RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_StopInstance.html) e [regras de créditos/Free Tier](https://aws.amazon.com/rds/faqs/).

**Revisão para uso eventual e readiness — 2026-09-13:**

- **Definição do usuário:** deploy somente durante testes e gravação. Na avaliação inicial a escolha ficou aberta; posteriormente o usuário escolheu Aurora Serverless v2 em D01.2.
- **Comparação correta:** Aurora com auto-pause versus RDS também parado fora das janelas, se automatizarmos seu ciclo de vida. Não comparar somente Aurora usado por poucas horas com RDS ligado o mês inteiro.
- **Exemplo ilustrativo, não previsão de uso:** 20 horas de computação no mês. RDS micro: `20 × US$ 0,016 + 20 GiB × US$ 0,115 = US$ 2,62`, mantendo armazenamento o mês inteiro. Aurora Standard a uma média hipotética de 0,5 ACU nas 20 horas: `20 × 0,5 × US$ 0,12 = US$ 1,20` de computação, mais armazenamento, I/O e demais adicionais. Contabilizar no Aurora o tempo até pausar, eventuais retomadas e a capacidade efetivamente usada. Se fossem 20 GB-mês faturados de armazenamento Aurora Standard, acrescentariam US$ 2,00, resultando em US$ 3,20 antes de I/O e adicionais; não tratar esses 20 GB como mínimo provisionado do Aurora.
- **Interpretação:** Aurora pode compensar para manter os dados disponíveis para retomada automática com longos períodos ociosos, especialmente comparado a RDS esquecido ligado. RDS micro pode continuar mais barato se seu início/parada for controlado nas mesmas janelas. Recalcular conforme horas e armazenamento previstos. RDS parado reinicia após sete dias; Aurora em auto-pause mantém cobrança de armazenamento e pode retomar em manutenções/conexões.
- **Sem pods, sem probes:** readiness não impede pausa quando os pods da API estão ausentes. Encerrar aplicações e suas conexões ao finalizar os testes é o caminho de menor mudança para o padrão de uso informado. Não pressupor escala automática a zero: o HPA atual declara `minReplicas: 1`; coordenar seu ciclo de vida com o desligamento da aplicação.

**Alternativa histórica não adotada — alterar readiness para permitir pausa com API ligada:**

A proposta abaixo foi substituída por D01.2. Não executar a separação/reconfiguração de probes ou health checks descrita nela. Manter o registro como alternativa avaliada; aplicar o ciclo operacional e o preflight de D01.2.

1. Preservar `/health/live` e `/health/startup` sem consultas periódicas ao banco.
2. Tornar explícita a política de readiness por ambiente. No modo que permite pausa, `/health/ready` verifica inicialização/configuração e capacidade local de aceitar e tratar requisições, sem abrir conexão PostgreSQL a cada probe. Isso não declara o banco saudável nem garante sucesso imediato da operação; a aplicação precisa tratar retomada e falhas reais. Manter o modo que verifica banco durante janelas de teste caso seja a opção operacional escolhida.
3. Separar diagnóstico ativo do banco, executado sob demanda ou no preflight da sessão/gravação. Não ligar um monitor SQL periódico a esse diagnóstico no período ocioso; preservar observabilidade por erros, logs e métricas do serviço sem gerar conexões artificiais.
4. Ajustar pooling e fechamento de conexões: Npgsql usa pooling por padrão; Dispose devolve a conexão ao pool e não garante fechamento físico imediato. Avaliar pool mínimo zero, expiração curta de conexões ociosas e ausência de keepalive. Na função serverless, assegurar fechamento real das conexões ociosas, considerando a suspensão do processo entre invocações. RDS Proxy não é apropriado para o objetivo de auto-pause, pois mantém conexões abertas.
5. Tratar retomada: timeout atual do health check é 2 segundos, a probe tem timeout de 3 segundos e a abertura de conexão de negócio é síncrona no registro de persistência. Planejar abertura assíncrona com timeout e cancelamento adequados e retentativas limitadas de estabelecimento de conexão, sem repetir escritas/transações não idempotentes. Coordenar o limite total com a API, Gateway e função. A retomada típica do Aurora é da ordem de 15 segundos e pode passar de 30 segundos após pausas longas; não prometer retomada transparente dentro de qualquer limite HTTP.
6. Antes da gravação, executar preflight explícito que retome o banco e confirme o fluxo de ponta a ponta; durante demonstração, tolerar o banco ativo. A pausa ocorre após fechar a última conexão e transcorrer o intervalo de inatividade, cujo mínimo configurável é 5 minutos em versões compatíveis.

**Esclarecimento histórico de integração e escalonamento (2026-09-14; anterior ao aceite de NLB em D01.4):**

- API Gateway disponibiliza seu próprio endpoint HTTPS gerenciado; não provisionar ALB/NLB à frente dele. Na proposta, haverá um balanceador de aplicação/backend na VPC, entre Gateway e pods, e não um balanceador adicional para expor o Gateway.
- REST API com integração privada por VPC Link V2 suporta ALB ou NLB. O Gateway não descobre automaticamente pods nem acessa diretamente um Service ClusterIP por essa integração.
- Alternativa NLB: manter o modelo Service `type: LoadBalancer`, configurando explicitamente NLB interno e seu controlador. Alternativa ALB: usar Ingress gerenciado pelo AWS Load Balancer Controller e Service ClusterIP com targets IP. Isso remove o tipo LoadBalancer do Service, mas mantém um ALB real na AWS. Não criar ALB e NLB em série para a mesma API sem necessidade.
- Repositório reconferido: Service atual LoadBalancer sem configuração explícita de tipo/scheme do balanceador; HPA com 1–10 réplicas e metas de CPU/memória de 50%. ALB/NLB não substituem nem controlam esse HPA; o controlador de balanceamento acompanha as alterações dos destinos. Novos pods precisam estar prontos e passar pelos health checks aplicáveis antes de receber tráfego. Capacidade dos nós continua sendo uma preocupação separada.
- Manter o balanceador público e integrá-lo como endpoint HTTP é outra possibilidade, mas exigiria restringir acesso direto para garantir entrada pelo Gateway. A proposta continua sendo balanceador interno; o usuário ainda não escolheu a topologia.

Referências: [endpoint de invocação do Gateway](https://docs.aws.amazon.com/apigateway/latest/developerguide/how-to-call-api.html), [REST API com VPC Link V2 e ALB/NLB](https://docs.aws.amazon.com/apigateway/latest/developerguide/set-up-private-integration.html), [ALB e tipos de destino no EKS](https://docs.aws.amazon.com/eks/latest/userguide/alb-ingress.html), [NLB no EKS](https://docs.aws.amazon.com/eks/latest/userguide/network-load-balancing.html) e [HPA](https://kubernetes.io/docs/concepts/workloads/autoscaling/horizontal-pod-autoscale/).

**Avaliação histórica de custo-benefício HTTP API versus REST API (2026-09-13; REST API escolhida em 2026-09-14):**

Ambas atendem à API REST da aplicação, integração com função e backend privado. HTTP API oferece configuração mais enxuta, métricas e logs de acesso; REST API acrescenta logs de execução, tracing nativo X-Ray, validação de requisições, transformação de corpo, respostas personalizadas do Gateway, integração WAF, cache opcional e planos de uso por API key. HTTP API também permite throttling por rota; não confundir isso com planos por consumidor. API keys não substituem o JWT.

Preços de referência em USD para us-east-1, faixa inicial, consultados na página oficial AWS; chamadas pequenas (até 512 KB para evitar múltiplas unidades na HTTP API), sem créditos/Free Tier, impostos, transferência ou serviços adicionais:

| Chamadas mensais | HTTP API (US$ 1,00/milhão) | REST API (US$ 3,50/milhão) | Diferença |
|---|---|---|---|
| 10 mil | US$ 0,01 | US$ 0,035 | US$ 0,025 |
| 100 mil | US$ 0,10 | US$ 0,35 | US$ 0,25 |
| 1 milhão | US$ 1,00 | US$ 3,50 | US$ 2,50 |
| 10 milhões | US$ 10,00 | US$ 35,00 | US$ 25,00 |

- **Ciclo de uso:** o Gateway básico não tem cobrança horária por ficar criado. Balanceador para EKS cobra por hora e capacidade; desligar pods não remove esse custo. Logs/traces, Lambda, transferência e recursos opcionais como cache/WAF têm cobrança própria. O total depende da topologia e dos recursos efetivamente habilitados.
- **Benefício para a fase:** REST API permite incluir o Gateway no tracing X-Ray e oferece logs de execução para diagnosticar integrações. A API no EKS e as dependências continuam precisando de instrumentação; habilitar tracing no Gateway não instrumenta automaticamente o sistema inteiro.
- **Recomendação revisada:** para testes/gravação com poucas chamadas, não decidir pela economia de centavos. Preferir REST API Regional se aproveitarmos diagnóstico e tracing nativos; manter HTTP API como opção mais simples se a observabilidade escolhida em D04 cobrir API/função sem necessidade desses recursos do Gateway. Não habilitar cache/WAF apenas por estarem disponíveis. Tipo ainda depende de decisão do usuário.

Fontes: [preços API Gateway](https://aws.amazon.com/api-gateway/pricing/), [comparação de recursos](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-vs-rest.html), [tracing REST API](https://docs.aws.amazon.com/apigateway/latest/developerguide/apigateway-xray.html), [throttling HTTP API](https://docs.aws.amazon.com/apigateway/latest/developerguide/http-api-throttling.html) e [cobrança dos balanceadores](https://aws.amazon.com/elasticloadbalancing/pricing/).

**Esclarecimento de custos e deploy — D09 (2026-09-15; avaliação anterior ao aceite registrado acima):**

- **Base atual:** pipeline autentica AWS com AWS_ACCESS_KEY_ID/AWS_ACCESS_KEY_SECRET armazenados no GitHub, executa update-kubeconfig e kubectl. O código Terraform inspecionado não configura backend remoto. O deploy atual da aplicação pode ser reaproveitado; backend Terraform e OIDC resolvem necessidades distintas.
- **Estado e locking:** estado guarda o vínculo entre recursos Terraform e IDs reais da AWS. S3 permite compartilhar esse registro entre execuções; locking impede duas operações concorrentes sobre o mesmo estado. Versionamento mantém versões recuperáveis. Separação significa arquivos/chaves por unidade e ambiente (base, banco, função e Gateway), podendo usar um único bucket; não significa multiplicar recursos de aplicação. Lock de um estado não coordena dependências entre estados diferentes, que continuam exigindo ordenação das pipelines.
- **OIDC:** proposta de substituir credenciais AWS duradouras no GitHub por credenciais temporárias obtidas ao assumir uma role com confiança restrita ao repositório/branch/ambiente. IAM/STS não cobram adicional por essa autenticação; execução das pipelines e recursos acessados têm seus próprios custos. OIDC não é requisito do PDF nem condição técnica para continuar o deploy atual. Autenticação não substitui acesso de rede ao endpoint EKS; nós privados não exigem automaticamente endpoint administrativo exclusivamente privado.
- **Secrets Manager:** referência pública AWS: US$ 0,40 por segredo/mês e US$ 0,05 por 10 mil chamadas. Exemplo hipotético: quatro segredos por um mês e dez mil chamadas = US$ 1,65. Armazenamento proporcional ao tempo; segredos mantidos após destruir banco continuam com cobrança. Quantidade final depende de separação de credenciais/ambientes. Rede privada, chaves KMS próprias e eventual rotação podem acrescentar custos.
- **S3 Standard em us-east-1:** referência US$ 0,023/GB-mês, US$ 0,005 por mil PUT/COPY/POST/LIST e US$ 0,0004 por mil GET. Exemplo de 0,1 GB incluindo versões, mil gravações e dez mil leituras: US$ 0,0113 antes de transferência, impostos e extras. Não é estimativa da fatura completa; o estado pequeno tende a ter custo reduzido. Locking nativo não requer DynamoDB e consome operações/armazenamento S3 usuais.
- **Condições dos exemplos:** sem créditos/Free Tier, valores em USD, volume hipotético e custos básicos dos serviços; não afirmar gratuidade nem custo fixo total. Secrets Manager já foi aceito em D01.5; S3/OIDC e contratos entre pipelines seguem em discussão.

Fontes: [Secrets Manager](https://aws.amazon.com/secrets-manager/pricing/), [S3](https://aws.amazon.com/s3/pricing/), [referência de armazenamento/PUT S3](https://docs.aws.amazon.com/solutions/latest/automated-security-response-on-aws/cost.html), [backend e locking](https://developer.hashicorp.com/terraform/language/backend/s3), [OIDC GitHub/AWS](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-aws) e [custos IAM/STS](https://docs.aws.amazon.com/IAM/latest/UserGuide/introduction.html).
