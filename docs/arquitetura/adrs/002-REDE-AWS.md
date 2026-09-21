# ADR 002 — Gateway REST com backend privado no EKS

**Estado:** aceito como arquitetura; implementação pendente. **Formalização:** 2026-09-16. [Índice](../README.md)

**Contexto:** reaproveitar EKS/Terraform e oferecer entrada gerenciada, mantendo workloads e banco privados. O ambiente será usado em janelas de testes/gravação.

**Decisão:** API Gateway REST Regional, OpenAPI composto, VPC Link e NLB interno solicitado por Service LoadBalancer. Usar VPC por ambiente com duas AZs, um nó t3.medium, um NAT zonal e endpoint gateway S3. Manter HPA/probes. Acesso administrativo EKS público e privado, autenticado/autorizado para CI via OIDC. Lambda em ZIP gerenciado usa conectividade privada ao Aurora.

**Alternativas:** HTTP API foi preterida pelos recursos de tracing, logs e cache potencial da REST API, além do fluxo OpenAPI escolhido. Cache permanece desligado. Remover o balanceador do EKS não atende à integração privada escolhida. Escala automática de nós, vários nós/NATs e endpoints de interface adicionais não foram adotados inicialmente.

**Consequências:** REST API, NLB, EKS e NAT têm custos próprios mesmo com pouco tráfego; desligar ambientes exige destruição coordenada. Um nó/NAT limita disponibilidade. HPA não cria capacidade de nós. Tipo de target e protocolos serão fixados e testados em E2/E3.

**Comprovação:** demonstrar entrada pelo Gateway, backend privado, acesso do CI e independência hom/prd em E2/E3/E6. [Componentes](../diagramas/COMPONENTES.md) · [RFC 001](../rfcs/001-EXECUCAO.md).
