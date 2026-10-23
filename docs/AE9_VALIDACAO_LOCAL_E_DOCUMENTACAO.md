# AE.9 — Postman, contratos e validação local conjunta

**23/10/2026 — concluída no escopo local.** Etapa do [plano extra AE](PLANO_EXTRA_AUTENTICACAO_E_EMAIL.md), separada dos aceites oficiais E5/E6/E7. Publicação, IAM/rede/Gateway e comprovação AWS do novo fluxo permanecem na AE.10. Nenhum workflow remoto, plano/apply/deploy/destroy AWS ou envio externo foi executado.

## O que foi alinhado

- Postman: RequestCustomerCode e VerifyCustomerCode substituem ValidateCustomerDocument. Código é lido somente na caixa privada; scripts guardam desafio/JWT, limpam código/token anterior e não os imprimem. ApproveBudget e RefuseBudget usam somente approved e conferem 200/decision. CreateOrder guarda order_id no mesmo ambiente das demais variáveis.
- 47 requisições YAML normalizadas ao schema oficial Postman v3: nomes explícitos, listas key/value em headers/credenciais, corpos JSON legíveis; campo order legado retirado. Sem mudança das demais rotas/roles/DTOs. Três ambientes Dev/Hom/Prd sem sessões ou URLs AWS hardcoded. [Uso e sequência](../postman/README.md).
- Contratos de acesso/SSM, RFCs, modelo, componentes e sequências atualizados. [ADR 011](arquitetura/adrs/011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md) registra a decisão de documento+código/sessão Customer e EmailFunction privada, substituindo a emissão por documento isolado e o transporte SMTP/simulação da API. Decisões anteriores permanecem como histórico explícito.
- Procedimentos dos quatro repositórios distinguem código preparado, comprovação local e implantação AWS. Não foram alterados C#, SQL/seeds/grants versionados, IAM, templates, Collectors, workflows ou contratos HTTP nesta etapa.

## Composição do ensaio

Foi criado somente o projeto Docker descartável **mecanica-ae9-validation**, com PostgreSQL e SMTP4dev do Compose da API. Em banco vazio, executados o Init.sql real e ProvisionApiUser.sql/ProvisionAuthUser.sql reais do repositório BancoDados. API usou mecanica_api; auth usou mecanica_auth, com grants restritos. Dados e credenciais foram exclusivamente fictícios; endereços locais foram trocados para example.invalid apenas na fixture descartável. Nada foi aplicado no banco do mantenedor ou em RDS.

A imagem real da API executou em container; a EmailFunction real executou no SAM oficial, conectado à mesma rede/SMTP4dev. O handler real AuthFunction, casos de uso e persistência executaram no runtime .NET local com **bootstrap/configuração injetados**, conforme o caminho de testes existente. O transporte de ambas as origens foi **SDK Invoke real → SAM local → EmailFunction → SMTP4dev**, sem substituir a resposta Invoke por um mock.

A composição manual existiu apenas em memória no PowerShell/.NET. Não foi criado projeto, executável, script auxiliar versionado, fallback produtivo de banco/JWT ou servidor HTTP alternativo para autenticação. Os construtores internos de configuração foram acessados somente no ensaio; o handler padrão continua exigindo configuração AWS hom/prd. O teste não simula IAM ou o runtime gerenciado da AWS. SAM foi aquecido antes do ensaio, portanto não comprova cold start.

Código e JWT permaneceram em memória e foram usados nas chamadas, sem gravação em arquivos/evidências. A caixa contém intencionalmente a mensagem para consulta privada. Resultados persistidos contêm somente nomes de conferências, códigos HTTP/estados técnicos e contagens.

## Fluxo positivo e negativas — 34 conferências

| Cenário | Resultado observado |
|---|---|
| Login interno Mechanic e criação por API real | 200 e 201 |
| Pedido de código por cliente elegível | 202, challengeId, sem code na resposta; captura real |
| Documento divergente na confirmação | 401 e tentativa incorreta persistida |
| Confirmação com código capturado | 200 com JWT do handler real; desafio Consumed no SQL |
| Reutilização do código | 401, sem segundo JWT |
| Mesmo JWT em status antes do orçamento | 200 pela API real |
| Status sem token | 401 |
| Pedido durante cooldown | 429 com Retry-After |
| Documento válido de cadastro ausente | 202 genérico; nenhuma linha/mensagem nova |
| Outro cliente autentica e acessa OS alheia | Confirmação 200; status/orçamento 404 |
| Diagnóstico Mechanic | Início/conclusão 204, sem emitir JWT |
| Mechanic tenta decidir orçamento | 403 |
| Documento extra no corpo do orçamento | 400 |
| Aprovação Customer titular | 200/Approved e status WaitingForExecution |
| Decisão repetida | 422 |
| Recusa em outra OS | 200/Refused e histórico Finished/BudgetRejected no SQL |
| Cliente inativado após emitir JWT | Status e orçamento 403 |
| Runtime auth reutilizado | Uma inicialização para as invocações do ensaio |
| Relay e logs | Nenhuma mensagem retransmitida; logs sem código/documento/email/JWT |

O JWT não foi forjado para a API: foi emitido pelo caminho real verify-code após consumo no banco. Isso comprova compatibilidade local do token com assinatura/issuer/audience e validação da API, além de titularidade/Active. A fixture usa TLS local desabilitado apenas na configuração injetada; não comprova VerifyFull/RDS, propagação Gateway ou autenticação por e-mail pessoal.

## SMTP indisponível e perda da caixa — 13 conferências

1. Parado somente o SMTP4dev de teste; criação da OS continuou 201 e consulta 200. API registrou mensagem fixa e tipo técnico TimeoutException no handler de notificação.
2. Pedido de código com SMTP indisponível retornou 500; o desafio do ensaio foi invalidado. Nenhum sucesso de autenticação foi afirmado. A implementação mantém invalidação best effort; esta observação não garante invalidação em toda falha/cancelamento.
3. SMTP reiniciado com caixa vazia; novo pedido retornou 202 e capturou mensagem. Outro reinício eliminou essa captura, conforme retenção em memória.
4. Novo pedido após a perda substituiu o desafio; confirmação do anterior retornou 401. Código novo retornou JWT aceito pela API em status 200.
5. Logs de API/auth/email/SAM/SMTP conferidos sem valores de código, JWT, documento ou destinatário.

Contador e timestamps do cooldown foram ajustados **somente na fixture descartável**, mantendo valores válidos, para não consumir esperas e janelas entre repetições do ensaio. Isso não testa passagem real de 60 minutos nem altera limites/relógio de produção. Os testes existentes cobrem validade, contagem, tentativas, expiração, consumo concorrente e limpeza.

## Regressão e artefatos

| Verificação | Resultado |
|---|---|
| API restore/build Release | Aprovados; avisos existentes preservados, sem supressões novas |
| API: Auth.Contracts/Domain/Service/Controller/Infrastructure | **1.212 aprovados**, zero falhas/ignorados: 48/183/195/596/190 |
| Serverless restore/build Release | Aprovados, zero avisos/erros |
| Serverless: Application/Contract/Function/Email/Infrastructure | **438 aprovados**, zero falhas/ignorados: 32/10/74/91/231 |
| SAM validate --lint/build de auth e email | Aprovados; Linux x64/framework-dependent, arquivos/dependências por função |
| API OpenAPI oficial | Gerado com runtime/telemetria inválidos ignorados; idêntico ao arquivo anterior, sem bootstrap AWS |
| API publish linux-x64 | Certificado RDS/configuração OTel/SDK Invoke presentes; sem MailKit/testes |
| Postman | 47 requisições válidas no schema oficial v3, 11 scripts com sintaxe Node válida, sessões vazias |
| Diagramas | Cinco diagramas Mermaid renderizados com ferramenta local e Chrome headless instalado |
| Terraform/Kustomize/actionlint/ShellCheck | Conferidos offline nos projetos envolvidos, sem plano ou aplicação |

Primeira tentativa Serverless herdou DOTNET_ROOT da cópia temporária incompleta do SDK e falhou por ausência de System.Net.Mail. Nova execução apontou explicitamente para o runtime instalado e aprovou os 438 testes, sem modificar código ou ignorar casos. O ensaio em memória emitiu avisos de unificação de referência .NET 8 do SDK AWS com .NET 10; são da compilação ad hoc, sem supressão ou alteração dos projetos.

O cache Terraform anterior ainda continha backend S3 e tentou carregar credenciais expiradas. A conferência offline foi isolada com TF_DATA_DIR novo, backend desabilitado e credenciais fictícias; não houve login, plano ou consulta autenticada de recursos AWS. Isso não comprova IAM implantado.

Evidências locais ignoradas pelo Git: API artifacts/ae9/joint-validation.json (34), failures-and-restart.json (13), cleanup.json, postman-schema-review.json, openapi-validation.json, logs de build/publish e TRX nos projetos; Serverless artifacts/ae9/test-results/ae9-regression-installed-runtime.trx e saídas SAM. Nenhum código/JWT/corpo de e-mail foi salvo nesses relatórios.

## Encerramento local

SAM encerrado após validar o PID e sua árvore de processos. Runtime container identificado por montagem exclusiva e rede do ensaio; containers, rede e volume **somente do projeto AE.9** removidos após conferir labels. Nenhum container ou volume desse projeto permaneceu. Volumes locais do mantenedor foram preservados. Imagens de build podem permanecer como cache local, sem processo em execução.

O procedimento usual do mantenedor continua sendo compose down **sem --volumes**; a remoção com volumes foi limitada ao projeto descartável criado pelo agente. Não houve destroy de ambiente real.

## AE.10 — publicação e conferência posteriores

1. Revisar os PRs dos quatro repositórios e publicar os workflows também na main. Banco vazio deve usar Init.sql/grants atuais; sem migrations/reaplicação destrutiva de dados existentes.
2. Gerar/revisar/aprovar/aplicar plano salvo do bootstrap com as permissões novas. Cadastrar AWS_EMAIL_ROLE_ARN hom/prd após o apply; manter AWS_ROLE_ARN auth e AWS_CI_ROLE_ARN. Essa autorização não está implícita na conclusão AE.9.
3. Base-provision: SMTP4dev/listener/rede/SSM, Collector e acesso de inspeção. Conferir rollout SMTP, targets e web privada via port-forward; não publicar a caixa.
4. Banco/database-init e email-publish/email-provision podem seguir independentemente após a base. Conferir ZIP/template/VersionId, stack/alias live e catálogo email/v1.
5. Publicar/implantar API e auth-publish/auth-provision após banco/email. Conferir guards, configuração, versões e logs sanitizados.
6. Gateway-provision com commits dos OpenAPIs novos e versão numérica auth; revisar/aprovar/aplicar exclusivamente o plano salvo. Conferir duas rotas públicas e ausência de customers/validate/Authorizer/endpoint email.
7. Em hom, seguir [Postman](../postman/README.md): request-code → caixa privada → verify-code → status antes do orçamento → diagnóstico → Approved ou Refused em OSs distintas → status posterior. Conferir mesmas negativas e perda admitida da caixa somente mediante autorização para reinício.
8. Conferir IAM real, VPC/SG/NLB, saúde/consumo no nó, cold start/congelamento/prazos reais e sanitização nos serviços AWS. Nenhum teste local certifica essas propriedades ou prd. Descarte segue Gateway → API/auth → banco/email → base e requer autorização própria.

Não acrescentar testes de carga, concorrência/coexistência de publicação, alertas/dashboards, união X-Ray/OTLP, email externo, fila/outbox ou persistência de mensagens como requisito desta etapa. Limites/dispensas oficiais permanecem registrados; o aceite extra AWS ainda está aberto.

**Atualização posterior — 23/10/2026:** a implantação e a conferência integrada de hom foram realizadas e estão registradas separadamente na [AE.10](AE10_VALIDACAO_HOM_2026-10-23.md), com resultados, ajustes encontrados e limites. As pendências AWS acima descrevem o encerramento original da AE.9; não substituem o registro posterior nem certificam prd.
