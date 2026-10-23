# Plano extra — Autenticação do cliente e envio centralizado de e-mails

**Decisão vigente: 22/10/2026. Estado: AE.1 e AE.2 concluídas.** GitHub, pasta raiz e remote usam GerenciamentoMecanicaServerless. Projetos, solução e caminhos auth organizados, preservando o funcionamento atual. Planos OIDC inicial/final aprovados e aplicados, CI do novo nome aprovado e confiança antiga removida; Terraform posterior sem diferenças. Não houve publish/provision/deploy. Alterações locais seguem para PR do mantenedor. [Contratos AE.1](CONTRATOS_EXTRA_AUTENTICACAO_E_EMAIL.md) definem as próximas etapas.

Este extra tem sequência própria **AE.1–AE.10** e é independente do [plano oficial da Fase 3](../PLANO_FASE_3.md). Não reabre nem altera o aceite E5/E6/E7. Substitui o [plano de Lambda Authorizer](PLANO_EXTRA_AUTHORIZER_ORCAMENTO.md), mantido como histórico. Seus resultados LA.1 de 320/369 testes comprovam somente aquele código local; não comprovam este fluxo.

## Objetivo e decisões aprovadas

- O cliente solicita um código e o confirma para obter JWT Customer. O JWT identifica o cliente e permite acessar suas próprias OSs, inclusive consultar status antes de existir orçamento.
- Não usar Lambda Authorizer, credencial por OS, token emitido na conclusão do diagnóstico ou lista de rotas dentro do JWT do cliente.
- Usar duas rotas HTTP na mesma Lambda de autenticação: solicitar código e confirmar código. Não enviar outro código automaticamente quando uma confirmação falhar.
- Centralizar o transporte SMTP em outra Lambda, invocada internamente pela API e pela autenticação. Não criar serviço de envio público, fila ou terceiro repositório de função.
- Executar SMTP4dev no EKS, por ambiente, sem relay externo e **sem volume persistente de mensagens**. Não entregar mensagens a caixas de e-mail reais.
- O cliente inicia o fluxo. O operador consulta o e-mail capturado na ferramenta de testes; o mecânico não gera acesso em nome do cliente e não recebe código pela API pública.
- Repositório renomeado para **GerenciamentoMecanicaServerless** e Function para **AuthFunction** na AE.2. EmailFunction tem estrutura própria; handler, ZIP de execução e testes funcionais serão implementados nas próximas etapas.
- Manter um repositório/solução .NET, mas **dois templates SAM e duas stacks independentes**, com workflows específicos e reaproveitamento nativo por workflow_call.
- Preparar hom/prd com o mesmo modelo de captura. O ambiente denominado prd no projeto continuará sendo demonstrativo quanto ao e-mail; SMTP4dev não comprova acesso ao e-mail pessoal do cliente.

Não serão criadas páginas. Postman e interface do SMTP4dev serão as ferramentas de demonstração. A consulta da caixa será por acesso operacional privado, inicialmente port-forward autenticado no cluster; isso não é uma funcionalidade autorizada pelo JWT Mechanic. Não criar endpoint de debug que devolva códigos ou caixa pública.

## Estado atual e trabalho a substituir

- A API atual aceita Customer em GET /Orders/{id}/status e PATCH /Orders/{id}/budget. Titularidade já é conferida pelo UUID Customer e cadastro/OS; Mechanic também pode consultar status.
- Aprovação ainda exige customerDocument + approved no body e retorna 204. O booleano atual precisa distinguir campo ausente de false.
- A adaptação LA.1 foi abandonada e seu código local foi descartado pelo mantenedor, preservando a documentação histórica. A base atual voltou ao customers/validate por documento; esse caminho será substituído pelas duas rotas de código, sem implementar REQUEST/Authorizer ou order_id.
- EmailFunction/EmailFunctionTests já têm estrutura local na AE.2, mas ainda não há handler/transporte/testes funcionais/stack de envio. templates/auth.yaml preserva somente AuthFunction; os workflows atuais ainda publicam explicitamente esse recurso e seu ZIP.
- O Gateway atual permite somente POST /customers/validate na autenticação, com Lambda permission limitada a essa rota. Não basta acrescentar endpoints no código.
- A role SQL mecanica_auth tem grants versionados para SELECT id/name/document/status/email de customers e DML somente nas duas tabelas de autenticação. [Conferência local concluída](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/docs/AE3_PERMISSOES_AUTENTICACAO_2026-10-22.md); aplicação AWS pendente.
- A API usa SMTP local e LoggingEmailSender em AWS. O modo AWS passará a invocar a função de envio; não duplicar o transporte SMTP na API e na autenticação.

## Fluxo proposto

```text
Cliente → Gateway → AuthFunction: solicitar código com CPF/CNPJ
AuthFunction → PostgreSQL: localizar cliente Active e registrar desafio protegido
AuthFunction → EmailFunction (invocação IAM síncrona): mensagem com código
EmailFunction → SMTP4dev privado: transmitir e capturar mensagem
Cliente ← resposta genérica com identificador do desafio, sem código

Operador → acesso privado ao SMTP4dev: consultar mensagem para o teste
Cliente → Gateway → AuthFunction: confirmar desafio + código + CPF/CNPJ
AuthFunction → PostgreSQL: validar/consumir desafio atomicamente e conferir cliente
Cliente ← JWT Customer com UUID, perfil e expiração, sem OS ou documento

Cliente → Gateway → API: JWT Customer + consulta de status ou decisão
API → PostgreSQL: verificar cliente Active, titularidade e regras atuais

API → EmailFunction → SMTP4dev: notificações de orçamento/status
```

A Lambda de autenticação não consulta orders nem decide orçamento. A função de envio não consulta clientes/desafios/ordens, não autentica usuários e não emite JWT. Cada origem monta seu conteúdo; apenas conexão, MIME/transporte SMTP e tratamento técnico do envio são centralizados. As mensagens capturadas podem conter dados de negócio e códigos; proteger a caixa e usar dados fictícios na validação.

## Contratos HTTP e JWT

Contratos consolidados na [AE.1](CONTRATOS_EXTRA_AUTENTICACAO_E_EMAIL.md), ainda a implementar:

| Operação | Entrada | Resultado esperado |
|---|---|---|
| POST /customers/auth/request-code | Body com document | 202 com mensagem genérica e challengeId opaco; sem código, token ou destinatário |
| POST /customers/auth/verify-code | Body com challengeId, code e document | 200 com JWT Customer após confirmação válida; sem renovação automática |
| GET /Orders/{id}/status | JWT Customer; Mechanic continua com seu JWT | 200 com ID/status somente quando autorizado |
| PATCH /Orders/{id}/budget | JWT Customer e body somente approved obrigatório | 200 com decision Approved ou Refused depois do commit |

Solicitação inexistente/inelegível usa o mesmo 202 com identificador opaco gerado em memória; esse identificador não autoriza acesso nem confirma envio. Consultar o cliente antes de qualquer gravação: se ausente, registrar somente log técnico fixo e retornar, sem persistir desafio, limite ou auditoria no banco e sem invocar envio/limpeza. Confirmação inválida usa 401 genérico; corpo inválido, 400; limite/cooldown de solicitação para cliente existente elegível, 429; falha técnica conhecida, 500. Pending só vira Ready após captura confirmada. Código de oito dígitos, cinco minutos/cinco tentativas; reenvio após 60 segundos e cinco solicitações/hora por UUID do cliente. O contrato não promete indistinguibilidade temporal.

Retirar o antigo fluxo customers/validate de emissão por documento ou por credencial de orçamento, sem manter um caminho alternativo que contorne o código. A migração é incompatível e deve ser coordenada entre contrato, função, Gateway e Postman.

O JWT Customer terá UUID persistido do cliente e perfil Customer, preservando compatibilidade de issuer/audience/algoritmo e claim de nome já emitida. Não conterá CPF, e-mail, order_id ou finalidade exclusiva de orçamento. Validade de dez minutos, sem refresh token e ClockSkew zero na API; isso também torna estrita a expiração dos tokens internos na configuração compartilhada. Sucesso verify-code mantém a string JSON do JWT. A API valida cada operação; não acrescentar chamada prévia à Lambda para verificar sessão nem enviar código automaticamente em resposta a 401.

O cadastro Active será conferido na confirmação e também na API usando a leitura já necessária à titularidade. Inativação após login impede operações Customer com 403 fixo; preservar Mechanic e o 404 genérico para OS ausente/de outro titular/cliente ausente. Aprovação passa a body approved estrito e 200 com decision Approved ou Refused, conforme AE.1.

## Desafios e armazenamento

Criar customer_auth_challenges e customer_auth_request_limits diretamente no Init.sql, junto das tabelas atuais, para cada banco vazio. Não criar migrations. O código não será salvo em texto puro, logs ou dimensões de telemetria. PBKDF2-SHA256 com salt por desafio protege o código; limites usam customer_id do cadastro encontrado, sem chave HMAC/HKDF por documento. Estados, validade, tentativas, consistência e consumo são regras C# da autenticação; SQL novo mantém estrutura e índices comuns, sem CHECKs, unicidade por estado ou cascata. Limpeza após cada solicitação admitida e confirmada: até 100 desafios expirados e 100 limites cuja janela de uma hora e cooldown de 60 segundos terminaram, sem retenção extra de 24 horas, agendamento ou garantia de eliminação pontual.

Vincular a confirmação ao cliente do desafio e comparar CPF/CNPJ normalizado, conferindo Active novamente. Consumir somente uma vez em transação; persistir tentativas inválidas mesmo quando a resposta for negativa. Duas confirmações concorrentes não podem emitir duas sessões a partir do mesmo consumo. Não usar memória de instância da Lambda como armazenamento de desafio. Reenvio deve invalidar o anterior de forma consistente.

Versionar as definições das tabelas no Init.sql e os grants no repositório de banco. O ambiente demonstrativo é descartado integralmente e o banco é inicializado vazio em cada novo provisionamento; sem mecanismo de evolução de banco existente. O hash do Init.sql será atualizado pelo cálculo já existente do Terraform/Job. Auth terá acesso mínimo às tabelas novas e SELECT de email em customers, além das colunas existentes; sem acesso a orders, phone, usuários internos ou administração. EmailFunction não recebe credencial de banco.

Sem persistência refere-se **somente às mensagens do SMTP4dev**. Perda/recriação do pod pode apagar a caixa sem apagar o desafio no PostgreSQL. Nesse caso, solicitar novo código; não recuperar o código por endpoint de debug, HMAC reversível ou log. O ambiente destruído também perde o banco conforme o ciclo de vida atual.

## Repositório, projetos e stacks

Estrutura-alvo. A solução e pastas AuthFunction/AuthFunctionTests/EmailFunction/EmailFunctionTests já foram organizadas localmente na AE.2; GitHub, pasta raiz e remote já usam GerenciamentoMecanicaServerless. O template email foi preparado com o handler na AE.5; sua integração aos workflows/publicação permanece na AE.8, sem publicar uma função vazia:

```text
GerenciamentoMecanicaServerless/
├── Domain/                    # Domínio da autenticação existente
├── Application/
├── Infrastructure/
├── AuthFunction/              # Atual Function/
├── EmailFunction/             # Composição própria, sem depender do domínio/banco auth
├── ApplicationTests/
├── InfrastructureTests/
├── AuthFunctionTests/         # Atual FunctionTests/
├── EmailFunctionTests/
├── templates/
│   ├── auth.yaml
│   └── email.yaml
└── .github/workflows/         # Entradas específicas e workflows reutilizáveis
```

Preservar inicialmente nomes físicos da função/stack auth, catálogos existentes e identidade dos sinais. Renomear repositório/pastas não exige recriar recursos AWS, renomear todos os namespaces ou mudar o pacote Auth.Contracts. Ajustar solução, referências, CodeUri relativo ao template, caminhos de build/teste/publish e handler quando aplicável; revisar credenciais de origem e git remote após a renomeação.

Usar stack existente mecanica-<ambiente>-auth para autenticação e stack nova mecanica-<ambiente>-email para envio, com configurações SAM próprias. Nenhuma função deve depender de uma stack comum para seu descarte. Cada uma possui ZIP/VersionId, versões Lambda, role, logs, configuração e recursos próprios. Não copiar dependências Npgsql/SQL/JWT para o projeto de envio.

Para o Gateway, continuar consumindo versão numérica da autenticação. Para os consumidores do envio, adotar ARN de alias estável do ambiente, apontado à versão publicada pela stack email, com IAM restrito a esse alias. Assim uma atualização do transporte pode alcançar API/auth sem republicá-las somente para trocar um número de versão. Expor no catálogo email o ARN de invocação e a versão numérica para auditoria; não confundir esse consumo com o contrato auth/Gateway.

## SMTP4dev, rede e envio

- Implantar SMTP4dev como serviço separado na base/EKS, preparado para hom/prd e com relay externo explicitamente desabilitado. Não incorporar seu processo à API.
- Não criar PVC ou armazenamento durável da caixa. Limitar recursos/retenção em memória/armazenamento efêmero, testar capacidade no nó e não prometer preservação após recriação do pod.
- Reutilizar NLB interno com listener SMTP TCP 2525 e target group/NodePort 30025 próprios; SG permite origem somente da EmailFunction. Preservar listener/health checks da API. Captura demonstrativa usa SMTP interno sem TLS/autenticação, sem apresentar rede privada como criptografia ou desabilitar validação de certificados. AE.1 define catálogo e limite; AE.4 implementará/revisará regras e conectividade, sem endpoint público ou IP fixo.
- Interface inicialmente por port-forward autenticado; sem novo Ingress público ou painel personalizado. A autorização Kubernetes operacional é distinta do JWT Mechanic.
- Invocação EmailFunction por SDK AWS e IAM, sem Function URL/Gateway público. API/auth recebem somente a permissão necessária ao alias do ambiente; a role email recebe SMTP/configuração/logs necessários e nenhum acesso a banco/JWT.
- Invocar sincronamente, com prazo total limitado; verificar também erro da execução Lambda, não somente status HTTP 200 do serviço Invoke. Não acrescentar fila, outbox, retry próprio ou garantia de entrega única.
- Preservar que falha de notificação de negócio não desfaz uma OS já persistida. Na autenticação, falha conhecida de envio deve impedir afirmar sucesso do desafio conforme contrato definido; não expor código como fallback.
- Reutilizar conteúdo e IEmailSender da API com adaptador de invocação. A função auth monta sua mensagem do código, e a função email concentra o transporte SMTP. Não copiar o cliente SMTP para ambas.
- Execução local usa SAM local start-lambda e contrato Invoke v1 com SMTP4dev do Compose, sem outro transporte SMTP na API. Compose usa caixa em memória desde a AE.5, preservando o volume PostgreSQL. Configuração local explícita de endpoint é proibida em hom/prd; auth mantém bootstrap injetado nos testes, sem configuração alternativa de banco/JWT. Comandos finais/conectividade Docker serão comprovados em AE.5/AE.7.

## Workflows e migração de identidade GitHub

Entradas independentes por componente:

| Autenticação | Envio | Reaproveitamento |
|---|---|---|
| auth-ci | email-ci | Setup/build/teste comum quando equivalente; testes/arquivos específicos |
| auth-publish | email-publish | SAM build/package, checksums, imutabilidade e VersionId S3 |
| auth-provision | email-provision | OIDC, validações comuns, deploy do template publicado e resumo |
| auth-destroy | email-destroy | Delete/diagnóstico comum depois dos bloqueios específicos |

Usar workflow_call nativo em arquivos reusable-*.yml, sem gerar YAML por scripts e sem aceitar comandos shell arbitrários como inputs. Wrappers fixam componente, template/stack/prefixos permitidos e precondições. Validar parâmetros no reutilizável; reaproveitar mecanismo comum sem duplicar transporte/regras de segurança.

Jobs dos reutilizáveis devem selecionar o Environment para usar suas variáveis/secrets. Preservar branches hom/develop e prd/main, autenticação OIDC e políticas atuais de execução/aprovação; não criar uma nova aprovação por conveniência. Roles/permissões e stacks são específicas de componente. Separar arquivos não é, por si só, isolamento de autorização OIDC; revisar confiança/Environments se a política de acesso exigir essa distinção.

Manter checkout por SHA, revisão de artefato explícita, checksums e VersionId exato por ZIP, publicação imutável e conferência do conteúdo. Separar prefixos/catálogos de auth e email, sem acesso cruzado desnecessário. CI de cada função deve cobrir suas dependências compartilhadas e concluir os checks exigidos; não deixar required check pendente por filtros de caminho.

Renomeação do GitHub exige revisar confiança OIDC no bootstrap, URLs/uses/checkout, origem e cache de artefatos, remotes, Environments, variáveis, README e required checks. Planejar/aplicar previamente a confiança de transição necessária, renomear mediante autorização e remover a confiança antiga ao concluir. Não ampliar para outro owner ou acesso irrestrito. Redirecionamento de URL Git não substitui essa revisão.

## Sequência AE — implementação futura

- [x] **AE.1 — Contratos e revisão de dependências.** [Consolidados em 22/10/2026](CONTRATOS_EXTRA_AUTENTICACAO_E_EMAIL.md): formatos HTTP/Invoke v1, limites/armazenamento/consumo, erros, JWT/Active, alias/catálogos, rede SMTP e caminho local. Revisado o código-base após descarte LA; retirada de customers/validate e demais mudanças funcionais continuam pendentes em AE.6–AE.8. Sem renomeação, implementação ou execução AWS nesta etapa.
- [x] **AE.2 — Migração do repositório e organização.** Solução Serverless, AuthFunction/AuthFunctionTests, estrutura EmailFunction/EmailFunctionTests e caminhos auth concluídos. Restore/build Release e 269 testes passaram, sem falhas/ignorados, também após a renomeação da raiz. Publish linux-x64, SAM validate/build, actionlint/ShellCheck e Terraform validados; fontes C# e OpenAPI preservados. GitHub/raiz/remote renomeados, proteções/Environments conferidos e [auth-ci #16 aprovado](https://github.com/pknfelps/GerenciamentoMecanicaServerless/actions/runs/39812817320). Planos OIDC inicial e final aprovados/aplicados: subjects antigos ausentes nas três roles, demais condições preservadas e plano posterior sem diferenças. [Encerramento administrativo](AE2_FINALIZACAO_OIDC_2026-10-22.md). Estrutura local ainda depende do PR do mantenedor; envio funcional permanece nas etapas futuras.
- [x] **AE.3 — Banco (preparado e validado localmente).** Duas tabelas e três índices comuns no Init.sql do banco vazio, sem migrations ou regras de negócio SQL; grants restritos preparados e conferidos. [Estrutura/Job/grants](VALIDACAO_AE3_POSTGRESQL_LOCAL_2026-10-22.md). Operações C# transacionais e [limpeza oportunista](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE3_LIMPEZA_EXPIRADOS.md) implementadas: até 100 expirados por tabela após solicitação admitida/commit, sem 24 horas extras e preservando janela/cooldown dos limites. Falha/timeout da limpeza não desfaz o pedido. Regressão completa final: 328 testes aprovados, zero falhas/ignorados; restore/build Release e publish linux-x64 aprovados. Sem aplicação AWS ou alterações às regras/seeds de OS. Composição HTTP/envio continua na AE.6; implantação/comprovação na AE.10.
- [x] **AE.4 — Infraestrutura/IAM/SMTP4dev (preparado e validado localmente).** [IAM preparado e validado localmente](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/AE4_IAM_EMAIL.md): runtime/pipeline email hom/prd, consumo do alias live por API/auth, escopos de SSM/artefatos e autorização da base. [Manifestos SMTP4dev e captura efêmera conferidos localmente](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/AE4_SMTP4DEV_MANIFESTOS.md): Kustomization incluída na base, SMTP NodePort e web ClusterIP, banco em memória, retenção 100/100, sem root/relay/PVC. Docker confirmou captura, sanitização do stdout e caixa vazia após reinício; renderização e schemas Kubernetes aprovados. [Rede NLB/SG e metadados SSM preparados e validados localmente](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/AE4_REDE_SMTP.md): listener 2525 no NLB existente, target group/NodePort 30025, mesmo ASG, SG reservado email e três chaves base/v2; HTTP da API preservado. [Inspeção View/default e bloqueio base-destroy por email preparados e validados localmente](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/AE4_INSPECAO_E_DESCARTE.md): função, ENIs e egress conferidos antes de plan/apply, SG isolado permitido, consulta falha bloqueia. Base/bootstrap e actionlint/ShellCheck aprovados. Sem plano/apply AWS. Aplicação dos recursos e comprovação de acesso/capacidade continuam na AE.10; email-destroy e suas checagens próprias API/auth serão criados junto dos workflows da AE.8. Egress SAM email e Compose efêmero foram preparados/validados localmente na AE.5; integração aos workflows/consumidores segue na AE.8/AE.7. Recursos reais dependem de planos/aprovação posteriores.
- [x] **AE.5 — EmailFunction (preparada e validada localmente).** [Implementação e evidências](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE5_EMAIL_FUNCTION.md): contrato Invoke v1 estrito para os três tipos, MailKit/remetente fixo, deadline compartilhado de três segundos, reserva de resposta, erros e conclusão JSON sanitizados. SAM privado, alias live e catálogo email/v1 preparados; integração aos workflows na AE.8. Restore/build Release, 419 testes aprovados (91 novos), SAM validate/build de ambos e ZIPs linux-x64 conferidos. Primeira chamada isolada pelo SAM, reutilização, mensagem inválida e SMTP indisponível conferidos; captura no Compose e caixa vazia após restart, sem relay ou volume SMTP, preservando PostgreSQL. API/auth ainda serão conectadas nas AE.7/AE.6; sem endpoint público, deploy ou envio externo.
- [x] **AE.6 — Autenticação (preparada e validada localmente).** [Fluxo e evidências](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE6_AUTENTICACAO_POR_CODIGO.md): request-code/verify-code estritos, sem emissão por documento isolado; store AE.3 integrado ao Invoke v1, ativação somente após captura, invalidação best effort e confirmação/consumo atômicos. Descoberta SSM em dois lotes 9+2, alias/contrato validados, cliente sem retry/override de endpoint. JWT atual de cliente com UUID, sem vínculo a OS; logs/rotas/categorias adaptados preservando limites de bootstrap/flush. Restore/build sem avisos/erros, 438 testes aprovados, fluxo PostgreSQL/SMTP real com AWS injetada, SAM/pacotes Linux de ambos conferidos. OpenAPI Serverless 2.0.0 preparado para publicação coordenada com Gateway na AE.8. API segue na AE.7; sem SQL/IAM/workflows/deploy nesta etapa.
- [x] **AE.7 — API (preparada e comprovada localmente).** [Implementação e evidências](AE7_API_ENVIO_E_ACESSO_CUSTOMER.md): Invoke v1 em todos os modos, SMTP/logging e MailKit retirados da API, SSM em lotes 9+2, alvo/contrato/endpoint validados e cancelamento/prazo de cinco segundos. Active antes da OS para Customer, titularidade/Mechanic preservados, ClockSkew zero e orçamento estrito approved/200 Approved ou Refused após commit. 1.212 testes aprovados sem falhas/ignorados; API container → SDK → SAM → EmailFunction → SMTP4dev comprovado, inclusive falha SMTP preservando criação 201. OpenAPI oficial/publish com certificado e Kustomize conferidos. Sem token em diagnosis/complete ou alteração de SQL/IAM/workflows/AWS. Publicação coordenada segue na AE.8.
- [x] **AE.8 — SAM, workflows e Gateway (preparados e validados localmente).** [Implementação, pré-requisitos e evidências](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE8_WORKFLOWS_E_GATEWAY.md): entradas auth/email e quatro reutilizáveis nativos, configurações/stacks/prefixos separados, checksum e VersionId imutáveis preservados; guards email/v1 antes de API/auth, email-destroy exige API/auth removidas e confere ENIs/egress. Gateway exige duas rotas/OpenAPI auth 2.0.0, com duas permissões específicas e sem Authorizer. IAM completou escopos mínimos de publicação/preflight e ref da trust email. Restore/build Release sem avisos/erros, 438 testes aprovados, SAM validate/build/transformação hom/prd, actionlint/ShellCheck, Terraform e Kustomize aprovados. Identidade pública/GitHub ruleset e branches dos Environments reconferidos em somente leitura, checks configuration/dotnet preservados. Sem workflow remoto, configuração GitHub ou aplicação AWS; bootstrap/AWS_EMAIL_ROLE_ARN e publicação coordenada seguem na AE.10. Postman/diagramas/roteiro conjunto na AE.9.
- [ ] **AE.9 — Validação local e documentação.** Executar verificações e fluxos abaixo; atualizar Postman, contratos de acesso, diagramas, procedimentos e links nos quatro repositórios, distinguindo estado atual/alvo. Nenhum teste LA anterior certifica esta entrega.
- [ ] **AE.10 — Publicação e validação integrada.** Após autorização, aplicar bootstrap/base/banco necessários, publicar/provisionar email e auth/API na ordem abaixo, atualizar Gateway e conferir o fluxo completo em hom. Publicação, comprovação e prd são estados distintos.

As etapas têm dependências; a numeração não autoriza execução AWS automática. Documentação e preparação local antecedem qualquer plano/apply ou renomeação remota autorizados.

**AE.2 administrativa, 22/10/2026:** [transição inicial](AE2_TRANSICAO_OIDC_2026-10-22.md) e [plano final](AE2_FINALIZACAO_OIDC_2026-10-22.md) aprovados/aplicados, cada um com 0 criações, 3 alterações de confiança e 0 exclusões. Renomeação, proteções e OIDC conferidos; confiança antiga retirada e Terraform posterior sem diferenças. AE.2 concluída, sem deploy funcional.

## Ordem operacional e bloqueios

Provisionamento: bootstrap aprovado/aplicado e identidade GitHub ajustada → base com SMTP4dev/endpoint privado → banco inicializado e email (independentes entre si) → API e autenticação (depois de suas dependências) → Gateway com contratos e versão auth novos.

Descarte: Gateway → API e autenticação → banco e email → base/SMTP4dev. API/auth podem ser retirados independentemente após Gateway; banco/email podem ser retirados independentemente depois de seus consumidores. Não executar esta sequência durante a implementação documental.

- Auth-destroy mantém checagem Gateway e retirada das próprias dependências de rede; não destrói email.
- Email-destroy exige API e função auth removidas; depois confere remoção dos próprios recursos/ENIs/egress conforme a política vigente de bloqueio imediato. Não apaga a stack auth.
- Database-destroy mantém checagem API/auth, que acessam o RDS. Email não recebe dependência de banco artificial.
- Base-destroy exige banco **e email** removidos, porque envio depende da rede/SMTP4dev da base sem depender de RDS. Preservar falha fechada em erros de consulta; ausência de banco não comprova ausência da nova função.
- Envio indisponível preserva transações de OS já confirmadas. A nova dependência funcional precisa ser observada antes de descartar, sem automatizar tráfego, destroy, recuperação manual de ENIs ou state rm.

## Validação e critérios de conclusão

Testes pequenos e descritivos, reaproveitando fixtures PostgreSQL/Testcontainers e testes HTTP. Conferir cliente inexistente sem INSERT/UPDATE/DELETE, sem envio e com log sanitizado. Cobrir geração/expiração/limites, negativas sem enumeração pelo contrato, código incorreto/documento divergente, consumo único concorrente, cliente Inactive antes/depois do JWT, cancelamento e falhas. Conferir persistência de tentativas inválidas e estado de desafio quando envio falha.

Exercitar request-code → mensagem capturada → verify-code → JWT → status antes do orçamento → aprovação/recusa → status posterior. Outro titular continua sem acesso; código/JWT não permitem outras roles. Campo approved ausente/null/tipo inválido ou extra retorna 400; decisão repetida mantém rejeição do domínio e rollback/estoque/histórico atuais.

Validar envio central usado por API e auth, falha da função/SMTP sem falsa confirmação, ausência de conteúdo em logs, alias/Invoke IAM e contrato estrito. Conferir perda admitida da caixa após recriação do SMTP4dev, solicitação de novo código e ausência de relay para destino externo. A caixa não é prova de entrega a destinatário real.

Executar restore/build Release, todos os testes afetados, OpenAPI sem AWS, publish linux-x64 com configuração/deps/runtimeconfig e certificados necessários por função, SAM validate/build para ambos, Kustomize, Terraform fmt/init sem backend/validate e actionlint/ShellCheck nos workflows envolvidos. Docker deverá estar ativo; não iniciá-lo automaticamente nem ignorar testes indisponíveis. Não criar suítes de telemetria, mocks AWS operacionais, scripts auxiliares ou novo executável.

Na implantação autorizada, conferir as duas stacks/versões/roles, consumo privado SMTP, mensagens de código e notificações, confirmação/JWT, ambas as rotas Customer, acesso Mechanic, erro/expiração/inativação, logs sanitizados e funcionamento das proteções/ordem de descarte. Não executar destroy apenas para testar sem autorização própria. Não afirmar funcionamento de prd com evidências apenas de hom.

Conclusão local: **fluxo implementado e comprovado localmente**. Conclusão integrada: **captura SMTP4dev e autenticação/API comprovadas na AWS no ambiente validado**, sem afirmar entrega de e-mail pessoal ou autenticação autônoma de produção real. Preservar console/sanitização e sinais existentes; novos dashboards/alertas, testes de carga, refresh/revogação de sessões, filas/outbox e persistência de mensagens ficam fora do escopo.

## Prazo e limites aceitos

Prazo de calendário informado: até quatro dias, com jornadas superiores a oito horas. Estimativa registrada: 32–48 horas de trabalho para o conjunto, sujeita a rede, migração GitHub/IAM, publicação e conferência AWS. Planejar marcos verificáveis; não transformar a estimativa em garantia ou dispensar validações para caber no calendário.

Não haverá e-mail real, nova página de debug ou geração de acesso pelo mecânico. O operador lê a captura privada para simular a entrega do código. Um repositório, dois projetos de função, dois ZIPs, dois templates/stacks e workflows independentes com reaproveitamento comum são a decisão vigente.

## Histórico e referências

- [Plano LA substituído](PLANO_EXTRA_AUTHORIZER_ORCAMENTO.md); manter suas evidências como histórico e retirar seu código/contrato no desenvolvimento AE, não nesta atualização.
- [Evidência local LA.1](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/LA1_AUTHORIZER_ORCAMENTO_2026-10-22.md) e [contrato de orçamento substituído](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/CREDENCIAL_ORCAMENTO.md).
- [Contrato HTTP/JWT atual](arquitetura/ACESSO_E_AUTENTICACAO.md), distinto do alvo planejado.
- [GitHub: renomeação](https://docs.github.com/en/repositories/creating-and-managing-repositories/renaming-a-repository), [workflows reutilizáveis](https://docs.github.com/en/actions/how-tos/reuse-automations/reuse-workflows) e [OIDC](https://docs.github.com/en/actions/reference/security/oidc).
- [SAM Function](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-resource-function.html), [Invoke síncrono](https://docs.aws.amazon.com/lambda/latest/dg/invocation-sync.html), [SMTP4dev](https://github.com/rnwood/smtp4dev) e [controles de códigos temporários](https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html).

Links operacionais atualizados para GerenciamentoMecanicaServerless após a renomeação. Referências históricas datadas podem conservar o nome anterior como evidência; o repository ID 1375105446 permanece o mesmo.
