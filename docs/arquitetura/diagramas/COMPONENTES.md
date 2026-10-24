# Componentes e fronteiras

[Arquitetura](../README.md) · [Execução](../rfcs/001-EXECUCAO.md) · [Entrega](../rfcs/002-ENTREGA.md)

```mermaid
flowchart LR
    U["Oficina / cliente"] -->|HTTPS| GW["Gateway REST Regional"]
    GW -->|"POST request-code / verify-code"| AUTH["AuthFunction .NET 10 - versao numerica"]
    GW -->|"HTTP_PROXY rotas da API"| VL["VPC Link V2"]
    API -->|"SDK Invoke - IAM / alias live"| EMAIL["EmailFunction .NET 10 - privada"]
    AUTH -->|"SDK Invoke - IAM / alias live"| EMAIL
    subgraph VPC["VPC do ambiente - duas AZs"]
        subgraph WORK["Subnets privadas de workloads"]
            NLB["Mesmo NLB interno - TCP 80 e 2525"]
            NLB -->|"ASG - TCP 30080"| SVC["Service API NodePort"]
            SVC --> API["API .NET 10 no EKS"]
            NLB -->|"ASG - TCP 30025"| SMTPSVC["Service SMTP NodePort"]
            SMTPSVC -->|"TCP 2525"| SMTP["SMTP4dev efemero - namespace smtp4dev"]
            WEB["Service web ClusterIP - porta 80"] -->|"HTTP 8080"| SMTP
            ENIA["Conectividade VPC auth"]
            ENIE["Conectividade VPC email"] -->|"SG email para SG NLB - TCP 2525"| NLB
            HPA["HPA 1 a 3 pods / um no t3.small"] -.-> API
            COL["Collector - observability"]
            JOB["Job temporario - database-init"]
        end
        DB[("RDS PostgreSQL 16.15 - Single-AZ")]
        API -->|"mecanica_api - TLS VerifyFull"| DB
        ENIA -->|"Cliente / desafios - mecanica_auth - TLS"| DB
        JOB -->|"Inicializar SQL e grants - TLS"| DB
        API -->|"OTLP interno"| COL
        NAT["NAT unico"] --> INTERNET["APIs AWS / New Relic"]
        API -.-> NAT
        ENIA -.-> NAT
        COL -.-> NAT
    end
    VL -->|"SG Gateway para NLB - TCP 80"| NLB
    AUTH -.-> ENIA
    EMAIL -.-> ENIE
    OP["Operador com kubeconfig autorizado"] -->|"port-forward localhost"| WEB
    SSM["SSM base/database/auth/Gateway v2 + email v1"] -.-> API
    SSM -.-> AUTH
    SSM -.->|"Resolvido somente no deploy"| EMAIL
    SM["Secrets Manager"] -.-> API
    SM -.-> AUTH
    SM -.-> COL
```

Funções são serviços Lambda com conectividade VPC, não pods. Email não possui HTTP público/Function URL/Gateway nem acesso RDS/JWT/Secrets/SSM em runtime. API/auth invocam o alias live por IAM; Gateway integra a versão numérica auth. Web SMTP é somente ClusterIP/port-forward autorizado, sem acesso pelo JWT Mechanic. SMTP demonstrativo privado sem TLS/autenticação; SG não cifra conteúdo. Caixa em memória, sem PVC/relay.

NLB/listeners/targets/ASG pertencem à base, sem Load Balancer Controller. HTTP usa 80 → 30080 → API 8080; SMTP usa 2525 → 30025 → pod 2525. SMTP web não tem listener NLB. Nó único, NAT único e RDS Single-AZ não oferecem alta disponibilidade entre AZs; HPA escala pods, não nós.

## Propriedade e ciclo

| Proprietário | Recursos |
|---|---|
| Bootstrap persistente | Estado/artefatos/ECR/OIDC/IAM e metadados New Relic |
| Base Terraform/Kubernetes | Rede/EKS/SG/NLB/ASG, SMTP4dev efêmero, JWT/Pod Identity/Collector e base/v2 |
| Banco Terraform/Job | RDS, credenciais, esquema/seeds/grants e database/v2 |
| API | Imagem/Deployment/HPA/ServiceAccount e regras de OS |
| Serverless auth SAM | AuthFunction/versões/alias/logs/egress e auth/v2 |
| Serverless email SAM | EmailFunction/versões/alias/logs/egress SMTP e email/v1 |
| Gateway Terraform | REST/VPC Link/duas permissões Invoke e logs/gateway/v2 |

Provisionar bootstrap/base → banco e email → API/auth → Gateway. Descartar Gateway → API/auth → banco/email → base. Banco vazio e mensagens são descartáveis; artefatos/IAM/estado compartilhados persistem. [Contratos e bloqueios](../CONTRATOS_ENTRE_REPOSITORIOS.md).

## Observabilidade preservada

```mermaid
flowchart LR
    API["mecanica-api"] -->|OTLP| COL["Collector EKS 0.161.0"]
    K8S["Kubelet / API Kubernetes - 30 s"] --> COL
    AUTH["mecanica-auth"] -->|"OTLP loopback / flush limitado"| EXT["Extensao Collector 0.151.0 - layer 0.22.0"]
    GW["Gateway REST"] -->|"Access logs"| CW["CloudWatch"]
    METRICS["13 metricas Gateway / auth Lambda / RDS"] --> CW
    CW -->|Polling| COL
    GW --> XR["X-Ray nativo"]
    AUTH --> XR
    XR -->|Polling separado| NR["New Relic US"]
    COL -->|"OTLP HTTPS"| NR
    EXT -->|"OTLP HTTPS"| NR
    SM["Secrets Manager"] -.->|"License Key"| EXT
    SM -.->|"License Key / Pod Identity"| COL
    EMAIL["EmailFunction - JSON sanitizado"] --> EL["Seu log group CloudWatch"]
```

EmailFunction possui somente logs JSON no CloudWatch, sem providers OTel, dashboard, alerta ou coleta CloudWatch própria. API/auth mantêm W3C, SQL/logs correlacionados, sanitização e exportação existentes. Não propagar X-Ray como pai OTLP ou afirmar união automática. Código/destinatário/MIME ficam na caixa privada, não nos logs/telemetria.

Coletores leem License Key no Secrets Manager, nunca via API/pipeline. Coleta kubelet cobre o nó do Collector; métricas CloudWatch têm atraso de dez minutos, logs retomam cinco minutos sem checkpoint/deduplicação.
