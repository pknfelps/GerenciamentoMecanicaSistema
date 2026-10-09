# RFC 002 — Entrega e contratos entre componentes

**Estado:** arquitetura aceita; reformulação autorizada em 2026-10-07. [ADR 008](../adrs/008-INFRAESTRUTURA-DECLARATIVA.md) substitui a operação por releases/controller. [Contratos v2](../CONTRATOS_ENTRE_REPOSITORIOS.md) são a referência operacional.

## Responsabilidades

API mantém negócio/testes, imagem, Deployment/HPA, OpenAPI e Auth.Contracts. Infra mantém AWS em Terraform: rede/EKS/add-ons/SGs/NLB e unidade Gateway separada; Service NodePort e namespace são manifestos próprios. Banco mantém RDS/secrets/SQL/Job. Autenticação mantém código/testes/ZIP/Terraform da função. Bootstrap mantém S3/OIDC/ECR persistentes.

NLB TCP 80 encaminha para instâncias do ASG na porta 30080. Service NodePort encaminha a 8080; health HTTP `/health/ready`. AWS acompanha entrada/saída das instâncias. Sem controller/Helm e sem EBS CSI sem uso; manter Pod Identity Agent/Metrics Server e t3.small 1/1/1.

## Pipelines e configuração

Estados próprios S3 versionados, locking nativo, hom/prd independentes, OIDC por componente. Configuração SSM v2 em recursos Terraform, consumo por data sources; secrets apenas referenciados, valores resolvidos em runtime ou em campos ephemeral/write-only. Não usar estado remoto de outro repositório como interface.

Provisionamento e destroy manuais, três jobs: plan salva binário e texto, aprovação em hom-approval/prd-approval sem AWS, apply baixa o artefato daquela execução e usa o mesmo commit. Retenção sete dias; plano obsoleto requer nova execução. Nenhum replan, fingerprint, publicação por scripts ou apply automático em push. CI de infra/banco somente fmt/validate/Kustomize. Bootstrap administrativo usa comandos Terraform diretos e revisão do plano salvo.

Banco: apply entrega ConfigMap e Secrets temporários no namespace reservado; kubectl remove Job antigo, aplica YAML e aguarda Complete. Falha mostra diagnóstico/logs. Etapa always remove Secrets temporários; próximo refresh planeja recriação. Preservar senhas existentes na primeira adoção, gerar novas para ambientes novos sem rotacionar em cada apply. Init.sql/marcador/grants mantidos; sem smokes operacionais. Testes de negócio permanecem.

OpenAPI/ZIP/NuGet continuam imutáveis e versionados com SHA-256 no S3. Não há protocolo de releases de infraestrutura. Gateway é planejado e aplicado manualmente com contratos fixos após implantação dos backends.

## Ordem e aceitação

1. Bootstrap: revisar/aplicar permissões, mantendo apenas descarte legado durante migração.
2. Se houver controller/NLB legado, remover Service, esperar exclusão e só então remover controller.
3. Base: plan/aprovação/apply AWS; kubectl aplica namespace e NodePort.
4. Banco: plan/adotar credenciais/aprovação/apply; executar Job e limpar Secrets.
5. Migrar consumidores para v2; aplicar runtimes/JWT/Gateway nas respectivas etapas.
6. Adotar e retirar SSM v1 ativo em planos dedicados, preservando histórico; retirar permissões de limpeza legada.

Mantenedor coordena operações sequenciais por ambiente. Dependência alterada exige novo plano dos consumidores; não haverá outro mecanismo de coordenação. SSM é configuração, não prontidão. Validar health depois da API, marcador/hash do Job, ausência de senhas em estados/logs e isolamento dos Secrets.

Descarte: consumidores/Gateway → banco → base. Job sai antes do banco, manifestos antes de EKS; Terraform remove NLB e associação. Preservar bootstrap e outro ambiente. Não executar destroy para testar esta reformulação.
