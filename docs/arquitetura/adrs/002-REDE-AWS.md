# ADR 002 — Gateway REST com backend privado no EKS

**Estado:** aceito e validado em hom; detalhamento vigente no [ADR 010](010-GATEWAY-REST-V2.md).

**Contexto:** oferecer entrada gerenciada mantendo aplicação/banco privados, com uso em janelas educacionais e reutilização do EKS/Terraform.

**Decisão vigente:** REST API Regional → VPC Link V2 com SG reservado → NLB interno Terraform TCP 80 → target de instâncias do ASG em 30080 → Service NodePort/API 8080. Não há Service LoadBalancer ou controller na operação vigente. Gateway oferece HTTPS público; o trecho privado até a API usa HTTP/TCP. Health do target usa /health/ready; não publicar health/Swagger/OpenAPI no Gateway.

Cada ambiente tem VPC, duas AZs com subnets públicas/workloads/banco, NAT/EIP único e endpoint gateway S3. EKS possui endpoint administrativo público/privado, IAM/access entries e autorização Kubernetes; não confundir isso com a entrada pública da aplicação. Um nó t3.small e HPA 1–3 pods; HPA não escala nós. Lambda SAM é gerenciada, com conectividade nas subnets privadas; RDS Single-AZ é privado e usa TLS VerifyFull.

**Alternativas e consequências:** HTTP API não foi adotada; REST atende à composição OpenAPI e tracing/logs escolhidos. Cache/execution logs/data tracing ficam desligados. Um nó/NAT e banco Single-AZ limitam disponibilidade; não representam capacidade produtiva validada ou alta disponibilidade entre AZs. Gateway/NLB/EKS/NAT e armazenamento têm custos próprios.

**Comprovação:** entrada Gateway, JWT, rede privada, pull ECR, probes e HPA foram conferidos nas evidências de hom. [Descarte final e custos](../../ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md). Coexistência/prd e carga não foram testados. [Componentes](../diagramas/COMPONENTES.md).

Revisão documental de 21/10/2026. Implementação e validações de hom não comprovam prd ou testes dispensados. [Arquitetura](../README.md) · [Evidências E6](../../REVISAO_E6_2026-10-21.md).

<details>
<summary>Histórico anterior à revisão E7 — registros preservados, sem vigência operacional</summary>

Os estados locais, planos pendentes e alternativas abaixo registram o momento em que foram escritos. A decisão e o alcance atuais estão acima; este histórico não autoriza operações nem reabre pendências aceitas.

> **Revisão vigente — 12/10/2026:** [ADR 010](010-GATEWAY-REST-V2.md): Gateway REST/VPC Link V2 em unidade Terraform separada; NLB Terraform/NodePort, SG reservado da base e ingresso TCP 80 restrito. Os trechos anteriores de Service LoadBalancer/VPC Link V1 são históricos.

> **Revisão auth — 2026-10-11:** [ADR 009](009-SAM-AUTENTICACAO.md) implementa SAM/CloudFormation com ZIP, mantendo conectividade privada e propriedade Terraform do Gateway/rede. O workflow resolve quatro parâmetros públicos e confere VPC, subnets e SGs antes de preparar o change set; implantação ainda pendente.

> **Revisão vigente (2026-10-07):** [ADR 008](008-INFRAESTRUTURA-DECLARATIVA.md) substitui controller e protocolo de releases/prontidão por NLB Terraform, NodePort, SSM v2 e planos salvos com aprovação. Os complementos anteriores abaixo são histórico; demais decisões permanecem.

**Estado:** aceito como arquitetura; implementação pendente. **Formalização:** 2026-09-16. [Índice](../README.md)

**Contexto:** reaproveitar EKS/Terraform e oferecer entrada gerenciada, mantendo workloads e banco privados. O ambiente será usado em janelas de testes/gravação.

**Decisão:** API Gateway REST Regional, OpenAPI composto, VPC Link e NLB interno solicitado por Service LoadBalancer. Usar VPC por ambiente com duas AZs, um nó t3.small, um NAT zonal e endpoint gateway S3. Manter HPA/probes. Acesso administrativo EKS público e privado, autenticado/autorizado para CI via OIDC. Lambda em ZIP gerenciado usa conectividade privada ao RDS PostgreSQL (D01.6).

**Alternativas:** HTTP API foi preterida pelos recursos de tracing, logs e cache potencial da REST API, além do fluxo OpenAPI escolhido. Cache permanece desligado. Remover o balanceador do EKS não atende à integração privada escolhida. Escala automática de nós, vários nós/NATs e endpoints de interface adicionais não foram adotados inicialmente.

**Consequências:** REST API, NLB, EKS e NAT têm custos próprios mesmo com pouco tráfego; desligar ambientes exige destruição coordenada. Um nó/NAT limita disponibilidade. HPA não cria capacidade de nós. Tipo de target e protocolos serão fixados e testados em E2/E3.

**Comprovação:** demonstrar entrada pelo Gateway, backend privado, acesso do CI e independência hom/prd em E2/E3/E6. [Componentes](../diagramas/COMPONENTES.md) · [RFC 001](../rfcs/001-EXECUCAO.md).

**Ajuste de capacidade em 2026-09-28:** usuário escolheu t3.small (2 vCPU, 2 GiB, x86_64) após falha de criação do t3.medium por elegibilidade ao Free Tier. Mantido um nó On-Demand por ambiente. Validar memória disponível e saúde dos add-ons/API/observabilidade no provisionamento.

</details>
