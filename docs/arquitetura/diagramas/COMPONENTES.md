# Componentes e fronteiras

[Arquitetura](../README.md) · [Execução](../rfcs/001-EXECUCAO.md) · [Entrega](../rfcs/002-ENTREGA.md)

Revisão de 21/10/2026 contra os quatro repositórios. O desenho representa a configuração implantada e validada em **hom**, removida ao encerrar a E6. Há configuração própria para prd, mas sua operação e a coexistência dos ambientes não foram comprovadas. [Evidências e estado final](../../ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md).

```mermaid
flowchart LR
    U["Oficina e cliente"] -->|HTTPS| GW["API Gateway REST Regional"]
    GW -->|"POST /customers/validate"| FN["Lambda .NET 10 - versão numérica"]
    GW -->|"HTTP proxy das rotas da API"| VL["VPC Link V2"]
    subgraph VPC["VPC do ambiente - duas AZs"]
        subgraph WORK["Subnets privadas de workloads"]
            NLB["NLB interno - TCP 80"] -->|"ASG - TCP 30080"| SVC["Service NodePort - 30080 para 8080"]
            SVC --> API["API .NET 10 no EKS"]
            HPA["HPA - 1 a 3 pods"] -.-> API
            NODE["Um nó t3.small - min/desired/max 1"] -.-> API
            LNET["Conectividade VPC da Lambda"]
            COL["Collector - namespace observability"]
            JOB["Job temporário - namespace database-init"]
        end
        subgraph DATA["Subnets privadas de banco"]
            DB[("RDS PostgreSQL 16.15 - db.t3.micro Single-AZ")]
        end
        NAT["Um NAT Gateway e EIP"] --> IGW["Internet Gateway"]
        API -->|"PostgreSQL com TLS VerifyFull"| DB
        LNET -->|"SELECT de cliente com TLS"| DB
        JOB -->|"Inicializar SQL e grants com TLS"| DB
        API -->|"OTLP HTTP interno"| COL
        API -->|"APIs AWS"| NAT
        LNET -->|"APIs AWS e New Relic"| NAT
        COL -->|"APIs AWS e New Relic"| NAT
        S3EP["Gateway endpoint S3"] -.-> WORK
    end
    VL -->|"SG Gateway para SG NLB - TCP 80"| NLB
    FN -.-> LNET
    API --> SIM["Notificações hom/prd - log simulado"]
    ECR["ECR privado - imagem por digest"] -.->|"Pull pelo nó"| NODE
    GH["GitHub Actions via OIDC"] --> CTL["Endpoint administrativo EKS - público e privado"]
    CTL -.-> API
    CTL -.-> JOB
    SSM["SSM v2 - configuração pública"] -.-> API
    SSM -.-> FN
    SM["Secrets Manager - referências por ambiente"] -.-> API
    SM -.-> FN
    SM -.-> COL
```

O Job está no namespace `database-init`, no mesmo nó privado do EKS. A Lambda é um serviço gerenciado com conectividade VPC, não um pod. RDS não é publicamente acessível. Terraform da base administra NLB/listener/target group e associação ao ASG; não há Load Balancer Controller. O health do target usa HTTP `/health/ready` em 30080; o Service usa `externalTrafficPolicy: Cluster`.

HPA escala pods, não nós. Nó único, NAT único e RDS Single-AZ não oferecem alta disponibilidade entre AZs. Gateway encaminha o Bearer sem authorizer JWT; a API valida assinatura, emissor, audiência, prazo, perfil e vínculo quando exigido. Probes, Swagger e OpenAPI não são publicados pelo Gateway. O endpoint de negócio fica em `/mecanica/<ambiente>/gateway/v2/endpoint` e muda após recriação.

## Propriedade e ciclo de vida

| Responsável | Recursos |
|---|---|
| Bootstrap persistente | Estado/artefatos S3, ECR, OIDC, IAM das pipelines/runtime, integração X-Ray e metadados dos Secrets New Relic |
| Base Terraform | VPC/EKS, SGs reservados, NAT/S3 endpoint, NLB/ASG, JWT e Pod Identity API/Collector |
| Banco Terraform e Job | RDS, SG/credenciais de banco, SQL/seeds e grants |
| API | Deployment/HPA/ServiceAccount, imagem, contratos e negócio |
| Auth SAM | Função/versão/alias, logs, egress do SG reservado e SSM auth/v2 |
| Gateway Terraform | REST API, VPC Link V2, integrações, permissão de invocação e logs |

Notificações permanecem no fluxo da API após commit. Hom/prd usam `LoggingEmailSender`, com mensagem fixa e sem entrega real. Localmente, sem `Runtime__Environment`, a API mantém SMTP/MailKit e smtp4dev. [ADR 006](../adrs/006-NOTIFICACOES.md).

## Caminhos de observabilidade

```mermaid
flowchart LR
    API["mecanica-api - HTTP e PostgreSQL, métricas e logs"] -->|OTLP| COL["Collector EKS - Contrib 0.161.0"]
    K8S["Kubelet e API Kubernetes - 30 s"] --> COL
    FN["mecanica-auth - invocação e PostgreSQL, métricas e logs"] -->|"OTLP loopback e flush limitado"| EXT["Extensão Collector 0.151.0 - layer 0.22.0"]
    GW["Gateway - access logs JSON"] --> CW["CloudWatch"]
    AWS["Métricas Gateway, Lambda e RDS"] --> CW
    CW -->|"Polling de logs e 13 métricas"| COL
    GW --> XR["X-Ray nativo"]
    FN --> XR
    XR -->|"Integração AWS por polling"| NR["New Relic US"]
    COL -->|"OTLP HTTP/protobuf com TLS"| NR
    EXT -->|"OTLP HTTP/protobuf com TLS"| NR
    SM["Secrets Manager - License Key"] -.-> COL
    SM -.-> EXT
```

API/Lambda propagam somente W3C `traceparent`: HTTP/invocação → PostgreSQL e logs correlacionados. X-Ray/polling é diagnóstico separado; a árvore única X-Ray/OTLP foi retirada do escopo, não corrigida. A License Key é lida pelos Collectors, não pela API nem pelo workflow. Logs de aplicação não são recoletados por stdout/CloudWatch.

Collector EKS coleta Kubernetes; kubeletstats cobre apenas o nó onde ele roda. CloudWatch tem atraso de dez minutos para métricas e retoma logs pelos últimos cinco minutos após restart, sem checkpoint/replay/deduplicação. Há lacunas aceitas. Três dashboards e cinco alertas pertencem à unidade persistente New Relic. [ADR 005 e limites](../adrs/005-OBSERVABILIDADE.md).
