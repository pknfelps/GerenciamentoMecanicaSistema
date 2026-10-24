# Revisão do projeto contra o enunciado da Fase 3

**24/10/2026 — revisão local, sem alterações de implementação ou operações remotas.**

## Parecer

A solução implementa a arquitetura e os fluxos principais pedidos: Gateway, autenticação serverless por CPF/código, consulta de existência/status, JWT, proteção das operações de cliente, RDS, Kubernetes/HPA, observabilidade New Relic, dashboards e alerta de falha de OS. A extra AE também implementou transporte serverless centralizado de notificações.

**Isso não equivale a atendimento integral do enunciado.** Há duas diferenças de escopo conhecidas — deploy acionado manualmente e substituição do convite a `soat-architecture` pela visibilidade pública —, lacunas de apresentação/documentação e os entregáveis finais ainda não produzidos. O aceite das etapas E5/E6 e as dispensas do mantenedor continuam válidos no escopo combinado; não alteram os requisitos do documento da faculdade.

A consolidação de 23/10 organizou as fontes, mas não eliminou todas as inconsistências dos documentos. Esta revisão complementa aquele relatório e identifica as correções restantes; não as aplica.

## Fonte e alcance

Fonte primária: **13SOAT - Fase 3 - Tech Challenge.pdf**, fornecido pelo mantenedor. As páginas 2–5 foram extraídas e conferidas visualmente. SHA-256 do PDF: `6604c5a705315f647ebf8b199df8bd2892c371265d4a4fa25082ab2122b181c4`.

Os IDs R01–R15 abaixo são os identificadores internos de rastreabilidade do projeto, não uma numeração existente no PDF.

| Checkout revisado | Branch / HEAD |
|---|---|
| GerenciamentoMecanicaSistema | develop / `5aaef741ecff9436e3075ceb1117779fa358bfd4` |
| GerenciamentoMecanicaServerless | develop / `c7628bcc4dc6e28268047d10788a656ffcd9b9c3` |
| GerenciamentoMecanicaBancoDados | develop / `5126d8b319399da0461166c09aa31bc0164aa5cb` |
| GerenciamentoMecanicaInfraestrutura | develop / `09da8a288343475185a9e1af3c537874e6951dd7` |

Foram consideradas também as alterações documentais locais ainda não publicadas. A revisão confrontou código, configuração, contratos, workflows, documentação e evidências datadas. Não refez a suíte .NET, build/publish, Terraform/SAM ou consultas AWS/GitHub/New Relic; não certifica o estado remoto atual, novas execuções de CI ou disponibilidade do endpoint em 24/10.

## Achados que afetam a entrega

### 1. Deploy das branches ainda depende de acionamento manual

**Prioridade alta — R04/R06, página 3.** O PDF pede deploy automático das branches de homologação e produção. Os workflows de publicação/provisionamento usam `workflow_dispatch`; o CI executa em PR. Há automação das etapas depois do acionamento e separação develop/hom e main/prd, mas não um gatilho automático de implantação a partir dessas branches.

Evidências: [api-deploy](../.github/workflows/api-deploy.yml), [auth-publish](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/.github/workflows/auth-publish.yml), [auth-provision](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/.github/workflows/auth-provision.yml), [database-provision](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/.github/workflows/database-provision.yml) e [base-provision](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/.github/workflows/base-provision.yml).

Essa diferença já foi adiada pelo mantenedor. Não é defeito recém-descoberto nem autorização para alterar workflows. Na entrega, não chamar o disparo manual de deploy automático das branches. Operação de prd permanece sem comprovação, conforme a decisão de não validá-la nesta etapa.

### 2. Visibilidade pública não é a confirmação literal do convite solicitado

**Prioridade média — R15, página 5.** O PDF pede confirmação de que `soat-architecture` foi adicionado aos quatro repositórios. A decisão vigente foi manter os repositórios públicos, com comprovação histórica da visibilidade em 21/10.

Um repositório público permite leitura, mas não comprova que o usuário foi adicionado. Não se pode registrar no PDF final um convite que não ocorreu. A escolha foi do mantenedor; esta revisão não convida ninguém nem altera visibilidade. A equivalência acadêmica dessa alternativa não foi confirmada com a instituição.

### 3. Faltam diagramas específicos em três READMEs

**Prioridade média — R13, página 5.** O enunciado pede, em cada README, diagrama da arquitetura específica daquele repositório.

- A [API](../README.md) possui diagrama Mermaid das suas camadas/adaptadores.
- [Serverless](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/README.md) possui tabela de projetos e descrição dos fluxos, mas não diagrama próprio.
- [BancoDados](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/README.md) remete à documentação central de modelo/ER, sem apresentar seu diagrama específico no README.
- [Infraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/README.md) descreve unidades/rede e remete ao desenho central, sem diagrama específico no README.

Os desenhos centrais existem; o problema é a apresentação distribuída exigida pelo PDF. A correção pode reutilizar a arquitetura atual, mostrando somente a responsabilidade de cada repositório, sem criar novos componentes.

### 4. Textos atuais ainda contradizem as evidências mais recentes

**Prioridade média — R12/R13 e clareza operacional.** Não confundir histórico explicitamente preservado com texto de orientação ainda apresentado como atual.

| Local | Inconsistência encontrada | Ajuste necessário |
|---|---|---|
| [Componentes](arquitetura/diagramas/COMPONENTES.md), abertura | Afirma que o fluxo ainda não foi implantado/comprovado na AWS | Referenciar a AE.10 e deixar claro o caráter datado da prova |
| [Sequência de autenticação](arquitetura/diagramas/VALIDACAO_CPF.md) e [abertura de OS](arquitetura/diagramas/ABERTURA_OS.md), introduções | Apresentam publicação AWS da extra como etapa seguinte | Separar a revisão local AE.9 da comprovação posterior AE.10 |
| [Banco e modelo](arquitetura/BANCO_E_MODELO.md) e [ER](arquitetura/diagramas/DADOS.md) | Tabelas auth aparecem como ainda sem aplicação AWS; há referência ao RDS removido como se fosse o atual | Atualizar o estado e preservar o descarte de 21/10 somente como histórico |
| [README Infraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/README.md), cadeia de destroy | Diz que email entrará na cadeia quando função/workflow forem publicados, embora isso já tenha ocorrido | Usar a cadeia vigente já registrada no final do próprio README |
| [README Serverless](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/README.md), inicialização/persistência | Descreve somente o lookup antigo de quatro colunas; não explica nesse trecho a leitura de email/desafios/limites nem o complemento email/v1 | Descrever o fluxo real do PostgreSqlCustomerAccessStore e bootstrap atual |
| [Alerta de notificações](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/observability/newrelic/order-notification-alerts.tf), descrição e mensagem | Afirma que hom/prd usam envio simulado sem tentativa SMTP | Explicar Invoke → EmailFunction → captura SMTP4dev; captura continua sem entrega externa |
| [Dashboard de integrações](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/observability/newrelic/dashboards/integrations.json.tftpl), texto de limites | Diz que comprovação implantada das integrações ainda está pendente e união X-Ray/OTLP continua pendente | Referenciar as provas/limites E5 e a decisão vigente de manter os caminhos separados; acrescentar a interpretação do modo smtp atual |

A consulta do alerta inclui `mode IN ('smtp', 'simulated')`, enquanto a API atual registra `smtp`. Portanto, a descrição desatualizada não demonstra quebra da seleção de falhas. O texto do dashboard sobre o significado de registros históricos `simulated` pode ser preservado, desde que seja distinguido do modo atual. Atualizar strings Terraform/JSON exigiria publicação posterior pelo fluxo normal; nada disso foi alterado nesta revisão.

### 5. Vídeo e PDF continuam pendentes

**Entregáveis obrigatórios — R14/R15, página 5.** Faltam gravação/publicação do vídeo de até 15 minutos, PDF único, conferência de acesso aos links finais e registro de submissão.

O vídeo precisa demonstrar CPF, pipeline, deploy automatizado nas etapas implementadas, APIs protegidas, dashboards com análise ao vivo e logs/traces em execução. Deve explicar os gatilhos manuais sem apresentá-los como atendimento integral da exigência de deploy automático das branches. Relatórios históricos ajudam na preparação, mas não substituem a demonstração ao vivo solicitada.

## Matriz dos requisitos

**Atendido** significa implementação localizada e evidência existente no alcance indicado, não nova certificação remota nesta revisão. **Parcial** identifica diferença de implementação, apresentação ou comprovação. **Pendente** identifica entregável ainda não produzido.

| ID / página | Requisito | Parecer e fontes |
|---|---|---|
| R01 / 2–3 | API Gateway para controle/roteamento | **Atendido em hom nas evidências.** REST Regional, integrações explícitas HTTP proxy para API privada e AWS proxy para as duas rotas auth. [Gateway Terraform](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/gateway/main.tf) e [AE.10](AE10_VALIDACAO_HOM_2026-10-23.md) |
| R02 / 2–3 | Função valida CPF, consulta existência/status e emite JWT | **Atendido no fluxo atual em hom.** Request/verify usam normalização oficial do pacote, cadastro/elegibilidade, desafio consumido atomicamente e token Customer. O código de acesso acrescenta uma verificação, sem retirar o CPF. [Casos de uso](https://github.com/pknfelps/GerenciamentoMecanicaServerless/tree/develop/Application/Authentication), [store](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/Infrastructure/Persistence/PostgreSqlCustomerAccessStore.cs), [JWT compartilhado](../GerenciamentoMecanica.Auth.Contracts/JwtIssuer.cs) e [SQL/negativas](AE10_CONFERENCIAS_FINAIS_HOM_2026-10-23.md) |
| R03 / 2 | Rotas sensíveis protegidas por autenticação via CPF | **Atendido para operações Customer em hom.** JWT emitido após documento/código, identidade UUID, status/decisão somente do titular e Active verificado na API. Admin/Mechanic têm autenticação interna própria, conforme matriz acordada. [OrdersController](../GerenciamentoMecanicaSistema/Controllers/OrdersController.cs), [OrdersService](../Service/OrdersService.cs), [matriz](arquitetura/ACESSO_E_AUTENTICACAO.md) e AE.10 |
| R04 / 3–4 | Quatro repositórios, CI/CD e deploy automático | **Estrutura e pipelines presentes; parcela de deploy automático parcial por R06.** Duas funções no mesmo repositório Serverless preservam os quatro repositórios. CI de PR, publicação/provisionamento e evidências hom existem; operação completa não é disparada automaticamente pelas branches |
| R05 / 3 | Main protegida e merge obrigatório por PR | **Comprovação histórica existente.** Rulesets/PRs/checks conferidos e auth corrigido; renomeação para Serverless preservou o repositório. [Proteções](EVIDENCIAS_CICD_E_PROTECOES_2026-10-21.md). Estado remoto não reconsultado hoje |
| R06 / 3 | Deploy automático das branches hom/prd | **Parcial.** Mapeamento/configuração existem; gatilho manual e prd sem comprovação. Ver achado 1 |
| R07 / 3 | Banco gerenciado provisionado por Terraform | **Atendido nas evidências hom.** [RDS PostgreSQL privado/Terraform](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/terraform/rds.tf), SQL/grants/Job e consumo real por API/auth. Sem exigência textual de Aurora ou banco serverless |
| R08 / 3 | Kubernetes com escalabilidade e Terraform | **Atendido no alcance da escala de pods demonstrada.** [EKS/node group](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/terraform/eks.tf), [HPA 1–3](../deploy/kubernetes/base/hpa-gerenciamento-api.yaml) e escala 1→2 registrada em [hom](VALIDACAO_E5_HOM_2026-10-20.md). Nó único e ausência de autoscaling de nós não equivalem a alta disponibilidade |
| R09 / 3 | New Relic; latência, CPU/memória, healthchecks, uptime e alerta de falha de OS | **Atendido no escopo E5 aceito em hom.** Providers/Collectors, três probes, métricas Kubernetes/uptime, dashboards e alerta de OS com abertura/encerramento/e-mail. [Encerramento E5](ENCERRAMENTO_E5_2026-10-21.md), [probes](../GerenciamentoMecanicaSistema/Controllers/HealthController.cs), [alerta](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/observability/newrelic/orders-alerts.tf). CPU/memória sem teste de disparo, conforme dispensa |
| R10 / 4 | Logs estruturados JSON e correlação | **Atendido pelo caminho estruturado e evidências de correlação em hom, com limites.** Auth/email têm JSON; API exporta logs estruturados OTLP com IDs e mantém console textual. Não apresentar stdout da API como JSON nem afirmar cobertura completa dos logs Gateway. [Tracing W3C](VALIDACAO_TRACING_W3C_HOM_2026-10-21.md), [composição API](../GerenciamentoMecanicaSistema/Observability/ObservabilityDependencyInjection.cs), [AE.10](AE10_VALIDACAO_HOM_2026-10-23.md) |
| R11 / 4 | Dashboards: volume diário, tempo por status, erros de integrações | **Atendido no escopo demonstrado, com textos a corrigir.** Ordens usa volume diário/fuso e histogramas de diagnóstico/execução/espera pela entrega, serviço completo; integrações usa resultados PostgreSQL/SSM/Secrets e notificações. [Templates](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/tree/develop/observability/newrelic/dashboards), [durações](../GerenciamentoMecanicaSistema/Observability/OrderDurationMeasurements.cs) e [E5](ENCERRAMENTO_E5_2026-10-21.md) |
| R12 / 4 | Componentes, sequências, RFCs, ADRs, banco/ER | **Conteúdo presente; parcialmente consolidado.** Três RFCs, onze ADRs, componentes/sequências/modelo/ER. Doze tabelas do ER correspondem às do Init.sql. Textos de implantação/estado ainda precisam das correções do achado 4 |
| R13 / 4–5 | README por repo, tecnologias, execução/deploy, diagrama, contratos; Dockerfiles/links aplicáveis | **Parcial.** Propósito, tecnologias/instruções e contratos estão presentes; há Dockerfile da API. SAM ZIP, imagens oficiais e Terraform não exigem Dockerfile próprio. Faltam diagramas específicos em três READMEs e revisão textual. Contrato auth estático existe, mas o README Serverless só aponta genericamente Swagger/Postman da API; incluir link direto à coleção ajuda a cumprir a exigência literalmente. URLs AWS datadas não certificam disponibilidade atual |
| R14 / 5 | Vídeo público/não listado YouTube/Vimeo, até 15 min, demonstrações | **Pendente.** Não localizado como entregue no checklist vigente |
| R15 / 5 | PDF único: quatro repos, vídeo, documentos e confirmação de acesso | **Pendente**, com diferença do acesso público descrita no achado 2. Não declarar adição de `soat-architecture` sem evidência |

## Revisão por repositório

| Repositório | Implementação confrontada | Conclusão |
|---|---|---|
| Sistema | Controllers/JWT, matriz de perfis, titularidade/Active, decisão explícita Approved/Refused; transações/histórico; medições após commit, integrações, sanitização/probes; Deployment/HPA e workflows | Cobertura funcional principal localizada e apoiada pelas evidências. Sem defeito funcional novo identificado nos caminhos examinados; isso não substitui auditoria exaustiva ou reexecução dos testes. Ajustes restantes concentrados em documentação e diferença de gatilho |
| Serverless | Request/verify, parser/documento, cadastro/status/email, limites/desafios/consumo, JWT compartilhado, bootstrap reutilizável, Invoke de envio; EmailFunction/SMTP, SAMs e workflows separados/reutilizáveis | Fluxo atende autenticação serverless pedida e acrescenta captura demonstrativa. Sem Authorizer ou acesso da Lambda às OSs, por decisão vigente. README precisa do diagrama e descrição atualizada da persistência/configuração |
| BancoDados | RDS privado, Init.sql/seeds/Job, duas tabelas auth/índices comuns, grants mínimos e ER | Doze tabelas correspondentes no ER; dados descartáveis e inicialização em banco vazio conforme decisão. Sem migrations novas ou exigência inventada de CHECKs de negócio. README precisa do diagrama próprio |
| Infraestrutura | Terraform/bootstrap, Gateway, EKS/rede/NLB, SMTP4dev privado; Collector/Kubernetes/CloudWatch; três dashboards/cinco alertas; CI/provision/destroy | Recursos e coleta presentes. Sem alta disponibilidade multinó/Multi-AZ prometida. Corrigir diagrama do README, orientações históricas apresentadas como atuais e textos do alerta/dashboard |

## O que não vira nova exigência nesta revisão

- **União X-Ray/OTLP:** o PDF pede logs/traces/correlação, mas não exige unir polling X-Ray aos traces OTLP no New Relic. A solução W3C e X-Ray separado permanece uma decisão válida, com seu alcance declarado.
- **E-mail externo:** o PDF não exige entrega real ao destinatário. A captura privada SMTP4dev demonstra o fluxo escolhido, sem comprovar posse da caixa pessoal ou entrega externa.
- **Notificação assíncrona/fila/outbox:** o objetivo de adotar serverless para notificações já é contemplado pela EmailFunction. Assincronia é evolução futura, não requisito textual adicional.
- **Carga, incidentes de CPU/memória, rollback, concorrência de publicação e coexistência:** cenários dispensados continuam dispensados; não transformá-los em testes aprovados ou reabri-los automaticamente.
- **HA:** um nó, NAT único e RDS Single-AZ são limites reais diante do objetivo de disponibilidade. O enunciado não prescreve quantidade de nós/AZs ou SLA; não inventar uma nova entrega obrigatória nem prometer HA que a topologia não oferece.
- **JSON em todo console:** a API possui saída textual e exportação estruturada. Apresentar a evidência da saída estruturada/correlação solicitada, sem atribuir formato JSON à saída textual. Não é necessário fingir que todos os destinos têm o mesmo formato.
- **Prod:** permanece fora da validação acordada. Preparação declarativa não é comprovação operacional de produção.

## Conferências locais realizadas agora

- Leitura integral das páginas de requisitos/entregáveis e conferência visual das quatro páginas relevantes.
- Inventário de 28 workflows nos quatro checkouts: 5 API, 12 Serverless, 4 banco e 7 infraestrutura. Identificação dos gatilhos locais, sem disparos.
- Renderização por substituição do Account ID dos três templates de dashboard e parsing JSON: 22 widgets operação, 17 ordens, 30 integrações; uma página e PUBLIC_READ_ONLY em cada definição. Isso não substitui validação de provider ou execução NRQL.
- Confronto das doze tabelas declaradas no Init.sql com as do ER: conjuntos correspondentes. Não equivale à inspeção de catálogo AWS ou validação de todas as propriedades do SQL.
- Leitura do OpenAPI auth 2.0.0: duas rotas request-code/verify-code.
- Inspeção de implementação e testes existentes de autorização, titularidade/Inactive, transações e métricas de integrações; sem reexecução ou nova afirmação de quantidade de testes aprovados.

Inventário técnico local em `artifacts/revisao-requisitos-20261024/conferencia-local.json` e páginas extraídas/renderizadas na mesma pasta ignorada. Sem credenciais, dados de requests ou conteúdo de Secrets.

## Encaminhamento

1. Corrigir as lacunas documentais/diagramas e descrições identificadas antes de anunciar E7 documental integralmente consolidada.
2. Manter explícitas as decisões pendentes perante o enunciado: gatilho automático das branches e convite ao usuário acadêmico. Qualquer mudança depende de instrução do mantenedor; não foi implementada nesta revisão.
3. Depois preparar vídeo, publicação, PDF e submissão usando a arquitetura atual e as evidências corretas.

Somente este relatório e os artefatos locais de leitura/conferência foram acrescentados. Alterações locais anteriores foram preservadas; nenhum recurso, workflow, código de produção, contrato, esquema ou configuração existente foi corrigido como consequência da revisão.
