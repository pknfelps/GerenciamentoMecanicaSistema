# Componentes e fronteiras

[Arquitetura](../README.md) · [RFC de execução](../rfcs/001-EXECUCAO.md) · [RFC de entrega](../rfcs/002-ENTREGA.md)

Arquitetura alvo. O conjunto abaixo existe separadamente em **hom** e **prd**, na mesma conta AWS e região us-east-1. A coexistência é suportada; ativar somente um ambiente é uma opção de economia.

```mermaid
flowchart LR
    U["Oficina / cliente"] --> GW["API Gateway REST Regional"]
    GW -->|"POST /customers/validate"| FN["Lambda .NET 10"]
    GW -->|"Rotas da aplicação"| VL["VPC Link"]
    subgraph VPC["VPC exclusiva do ambiente — duas AZs"]
        subgraph PRIV["Subnets privadas de workloads"]
            NLB["NLB interno"] --> SVC["Service LoadBalancer"]
            SVC --> API["API .NET no EKS"]
            HPA["HPA 1–10 pods"] -.-> API
            NODE["Um nó t3.medium"] -.-> API
            LNET["Conectividade VPC da Lambda"]
        end
        subgraph DATA["Subnets privadas de banco"]
            DB[("Aurora PostgreSQL Serverless v2 — um writer")]
        end
        subgraph PUB["Subnet pública de suporte"]
            NAT["Um NAT Gateway + EIP"] --> IGW["Internet Gateway"]
        end
        API --> DB
        LNET -->|"SELECT de cliente"| DB
        API -->|"Saída externa"| NAT
        LNET -->|"Saída externa"| NAT
        S3EP["Gateway endpoint S3"]
        API -.-> S3EP
    end
    VL --> NLB
    FN -.-> LNET
    IGW --> EXT["New Relic / APIs AWS / SMTP"]
    SM["Secrets Manager por ambiente"] -.-> API
    SM -.-> FN
    GH["GitHub Actions via OIDC"] --> CTRL["Endpoint administrativo EKS público + privado"]
    CTRL -.-> API
    ECR["ECR privado compartilhado"] -.->|"Imagem por digest"| API
```

O Service representa o recurso Kubernetes que solicita o NLB ao AWS Load Balancer Controller, não um segundo balanceador físico. O destino exato do NLB (nó/pod) e os protocolos serão fixados em E2/E3. A Lambda é um serviço gerenciado: o bloco de conectividade representa seu acesso à VPC, não uma execução dentro do EKS.

O Gateway é a entrada pública de negócio. O endpoint administrativo do EKS é outra superfície, autenticada por IAM e autorizada no cluster. NAT atende saída; Aurora não recebe rota de internet. O HPA escala pods dentro da capacidade disponível: o nó único não oferece alta disponibilidade nem escalonamento automático de nós.

S3 de estado, identidade OIDC, ECR e bucket separado de artefatos pertencem ao bootstrap compartilhado. VPC, EKS, Aurora, função, Gateway, NLB e segredos de cada ambiente são independentes. A destruição de hom não remove prd nem o bootstrap.

## Caminhos de observabilidade

```mermaid
flowchart LR
    API["API: traces, métricas e logs OTel"] -->|"OTLP"| COL["Collector Contrib no EKS — uma réplica"]
    K8S["Kubelet + API Kubernetes"] -->|"CPU, memória e estado"| COL
    FN["Lambda instrumentada"] --> LCOL["Extensão Collector local"]
    GW["API Gateway"] --> CW["CloudWatch: logs do Gateway e métricas AWS"]
    AWS["Lambda / Aurora: métricas AWS"] --> CW
    CW -->|"Polling pelo receiver"| COL
    GW --> XR["X-Ray: tracing nativo do Gateway"]
    XR -->|"Integração oficial com polling"| NR["New Relic"]
    COL -->|"OTLP HTTP/protobuf TLS"| NR
    LCOL -->|"OTLP HTTP/protobuf TLS"| NR
```

Logs da aplicação/função têm um único caminho remoto via OTel; não duplicar sua ingestão por stdout/CloudWatch. O caminho CloudWatch do Gateway depende do Collector ativo. X-Ray possui atraso de polling e não alimenta alertas imediatos. A continuidade entre contextos W3C/AWS deve ser demonstrada, não presumida. Compatibilidade do receiver CloudWatch e da extensão Lambda será comprovada em E5.
