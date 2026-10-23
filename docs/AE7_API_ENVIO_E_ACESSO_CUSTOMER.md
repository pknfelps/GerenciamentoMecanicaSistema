# AE.7 — Envio centralizado e acesso Customer na API

Preparada e comprovada localmente em 22/10/2026. Complementa as AE.5/AE.6; não altera os checklists oficiais concluídos E5/E6/E7. Nenhum deploy, plano/apply, mudança IAM/SQL/Gateway ou workflow remoto foi executado.

## Operações Customer e orçamento

GET /Orders/{id}/status preserva resposta, titularidade e acesso Mechanic. Para Customer, a API usa somente o UUID do JWT: procura o cadastro e exige Active **antes de consultar a OS**. Cadastro ausente, OS ausente ou outro titular recebem 404 genérico “Ordem não encontrada”; cadastro existente Inactive recebe 403 “Acesso não autorizado”. Mechanic mantém o caminho atual, sem adquirir permissão para decidir orçamento.

PATCH /Orders/{id}/budget aceita exatamente um booleano explícito:

```json
{"approved":true}
```

Ausência, null, tipo incorreto, array, campo extra ou propriedade duplicada são 400. O conversor específico mantém comparação sem distinção de maiúsculas e rejeita customerDocument/customerId; headers/query não selecionam o titular. Não aplicar essa restrição a outros contratos indiscriminadamente.

Depois do commit, responder 200 com `{"decision":"Approved"}` ou `{"decision":"Refused"}`. Repetição/estado incompatível continua 422; conflitos e falhas técnicas preservam o mapeamento atual. Transações, locks de OS/estoque, reserva/restauração/consumo e histórico foram preservados. Falha de commit não pode produzir decisão de sucesso. Diagnóstico continua 204 sem emitir JWT.

JWT mantém os contratos e durações de emissão existentes. ClockSkew agora é zero, incluindo tokens de usuários internos: a API rejeita imediatamente tokens expirados, sem a tolerância anterior de cinco minutos. Cliente inativado depois de receber token continua com assinatura válida, mas é bloqueado pela consulta de cadastro nas duas operações Customer. Não há Authorizer, chamada prévia à auth por operação ou status anônimo.

## Envio e composição

O único transporte SMTP fica na EmailFunction. Removidos da API MailKitEmailSender, SmtpConnection, EmailSettings, LoggingEmailSender e a dependência MailKit. EmailService mantém conteúdo/regras de notificação, informa type budget_available/status_changed e faz encoding de valores no HTML antes de chamar IEmailSender.

LambdaEmailSender usa AWSSDK.Lambda 4.0.108.1, RequestResponse/LogType=None, sem retry, payload logging ou leitura SSM por mensagem. A resposta só confirma captura quando status Invoke=200, FunctionError está ausente e o schema é exatamente version=1/outcome=captured. Falha, payload inválido/vazio/excessivo ou timeout produz exceção sanitizada, sem conteúdo remoto/inner exception. Captura SMTP não significa entrega externa ou persistência.

O prazo total de cinco segundos é vinculado ao cancelamento da requisição HTTP. Não há fila própria ou recuperação automática. O handler de notificações continua tratando falha após commit: registra mensagem fixa/tipo técnico e não desfaz a OS nem transforma a persistência bem-sucedida em falha HTTP. Repetir manualmente uma operação pode duplicar mensagens; não há entrega única.

Na AWS, o bootstrap mantém o prazo de 45 segundos e as leituras existentes. Agora são dois lotes SSM (nove parâmetros base/database e dois email/v1) e duas leituras Secrets Manager. Catálogo exige contrato 1 e alias exato `arn:aws:lambda:us-east-1:121754142617:function:mecanica-<ambiente>-email:live`. Overrides de endpoint são rejeitados; o cliente ignora endpoints configurados externamente pelo SDK. Sem fallback para logging ou SMTP direto.

Localmente, Email:InvocationEndpoint aceita HTTP sem credenciais/query/fragmento apenas em localhost, 127.0.0.1 ou host.docker.internal; FunctionName é EmailFunction. Padrão para API no host: http://127.0.0.1:3001. Compose usa http://host.docker.internal:3001. Cliente usa somente credenciais fictícias local/local, sem buscar credenciais AWS da máquina.

## Métricas e logs

Providers, identidade, filtros, sanitização e métricas atuais permanecem. Tentativas concluídas de notificação continuam separadas da persistência da OS. `mode=smtp` identifica agora a captura SMTP4dev via EmailFunction em todos os ambientes; **não é entrega ao destinatário**. Pontos históricos simulated/smtp mantêm seu significado original. Não foram acrescentadas dimensões, exporters ou alertas; filtros existentes que aceitam smtp/simulated continuam abrangendo os pontos novos.

A falha do adaptador não registra mensagem original do SDK, configurações, payload, destinatário ou MIME. O diagnóstico existente do handler preserva seus campos técnicos. Conteúdo de e-mail permanece somente na captura privada/efêmera. Não coletar stdout como corpo de mensagem.

## Execução local

Docker Linux precisa estar ativo. Não iniciar Docker Desktop automaticamente. Com .env local preparado conforme README:

1. Iniciar somente db/smtp pelo Compose e inicializar **banco vazio** com o Init.sql do repositório de banco. Não há migrations ou reaplicação destrutiva sobre banco existente.
2. Build da EmailFunction pelo [procedimento AE.5](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE5_EMAIL_FUNCTION.md), em terminal com configuração/credenciais AWS fictícias e endpoint AWS inacessível, conforme o procedimento.
3. Iniciar SAM com a mesma rede do Compose e SmtpHost=smtp/SmtpPort=25. Para API no host, listener 127.0.0.1:3001; para API no Docker Desktop, usar listener acessível ao container (`--host 0.0.0.0`) **somente durante a execução local**. O listener de desenvolvimento não autentica clientes IAM e não é serviço de produção.
4. Iniciar API, executar login interno com Accept: application/json e realizar o fluxo de OS. A resposta de login permanece conforme negociação atual; não alteramos sua emissão.
5. Consultar a caixa na interface do SMTP4dev. Encerrar SAM com Ctrl+C e Compose com down, sem --volumes para preservar o banco local do mantenedor. Reiniciar SMTP perde a caixa.

Exemplo mantendo a mesma identificação de projeto nos dois terminais:

```powershell
# Raiz da API; .env local já preparado.
docker compose -p mecanica-email-local up -d db smtp
# Inicializar banco vazio pelo procedimento do repositório BancoDados.
# No terminal SAM da raiz Serverless, reutilizar os parâmetros AE.5 e executar:
# sam local start-lambda --template-file artifacts/ae5/sam-email/template.yaml --host 0.0.0.0 --port 3001 --docker-network mecanica-email-local_default --warm-containers LAZY --parameter-overrides $parameters
# Voltar à raiz da API, após o SAM estar disponível:
docker compose -p mecanica-email-local up --build -d api
# Encerrar SAM com Ctrl+C, depois:
docker compose -p mecanica-email-local down
```

EMAIL_INVOCATION_ENDPOINT no .env pode alterar o endpoint local do Compose dentro dos destinos permitidos; não copiar essa configuração para hom/prd. Não há scripts auxiliares ou executável novo.

## Evidências locais

- Restore/build Release aprovados. Avisos anteriores de nulabilidade/analisadores foram preservados; nenhuma supressão acrescentada.
- **1.212 testes aprovados, zero falhas/ignorados**: 596 Controller, 190 Infrastructure, 195 Service, 183 Domain e 48 Auth.Contracts. Relatórios ignorados pelo Git: `<projeto>/TestResults/ae7-final.trx`.
- Invoke: contrato estrito, status/FunctionError, resposta inválida, timeout/cancelamento, erros sanitizados, nenhum retry e destinos hom/prd/local permitidos.
- HTTP/OpenAPI: approved obrigatório/booleano/único, documento extra rejeitado, resposta 200, expiração sem ClockSkew para Customer/Mechanic e perfil/titularidade preservados.
- PostgreSQL/Testcontainers: inativação após emissão bloqueia status/decisão sem escrita; aprovação/recusa e concorrência preservam estoque, histórico e rollback. Testes de notificações e sanitização existentes foram adaptados ao novo transporte.
- Comprovação manual com **API real em container → SDK Invoke real → SAM oficial → EmailFunction real → SMTP4dev real**, usando banco descartável criado pelo Init.sql e dados fictícios. Capturadas três notificações de criação/diagnóstico/orçamento; relay não observado. A decisão retornou 200/Approved.
- Com o SMTP parado, outra criação retornou 201 e a falha de notificação foi tratada, preservando a operação já persistida. Nenhum token ou conteúdo de mensagem foi salvo como evidência.
- SAM local aquecido com envelope inválido antes da conferência do SDK; esse cenário não certifica cold start AWS. Artefatos/evidência técnica em artifacts/ae7, incluindo docker-evidence.json.
- OpenAPI oficial gerado com configurações runtime/telemetria deliberadamente inválidas e ignoradas na geração, sem AWS. Mudanças de orçamento são intencionais; demais contratos não foram redesenhados.
- Publish conferido com certificado RDS original, JSON OTel e SDK Lambda; sem MailKit/MimeKit ou dependências de teste. Overlays hom/prd renderizados; infraestrutura de produção não alterada.

O projeto Docker da conferência foi mecanica-ae7-validation. Seus containers/rede e volume de teste foram removidos após conferir a propriedade; volumes do mantenedor foram preservados. SAM/runtime encerrados. Não houve coleta externa ou operações AWS.

## Próximo passo

**AE.8 — SAM, workflows e Gateway:** publicar email com artefato/versionamento/catálogo próprios; integrar publish/provision/destroy e permissões preparadas; adaptar Gateway às duas rotas auth e nova revisão numérica, sem Authorizer. Aplicação depende das etapas e autorizações posteriores.

AE.9 atualizará Postman/diagramas e fará o roteiro conjunto; coleções antigas com customerDocument no orçamento precisam ser alinhadas antes de usá-las no novo contrato. AE.10 comprovará IAM, SG/NLB/VPC, cold start e o fluxo implantado após autorização. O teste local não certifica execução AWS, entrega pessoal de e-mail, retenção ou garantia de envio único.
