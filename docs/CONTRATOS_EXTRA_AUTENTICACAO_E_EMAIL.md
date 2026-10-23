# AE.1 — Contratos de autenticação do cliente e envio centralizado

**22/10/2026 — contratos revisados após orientação do mantenedor: inicialização de banco vazio, regras em C# e nenhuma gravação para cliente inexistente.** As duas tabelas da AE.3 ficam no Init.sql; componentes transacionais, grants e limpeza preparados e validados localmente; composição HTTP, rede e publicação continuam nas etapas próprias. Complementa o [plano extra AE](PLANO_EXTRA_AUTENTICACAO_E_EMAIL.md), sem alterar os aceites ou o contrato implantado da Fase 3. As escolhas numéricas são decisões deste projeto.

## 1. Fronteiras e compatibilidade

- AuthFunction autentica clientes por documento e código de uso único. Não consulta OSs nem envia SMTP diretamente.
- EmailFunction recebe mensagens prontas e concentra MIME/conexão/envio SMTP. Não consulta clientes, desafios ou OSs, não emite JWT e não tem endpoint HTTP público.
- API valida o JWT, o cadastro Active, a titularidade e as regras da OS. Não chama autenticação antes de cada operação.
- SMTP4dev captura mensagens em hom/prd demonstrativos; sem relay, entrega externa ou persistência da caixa. O acesso operacional à captura simula o recebimento pelo cliente.
- Remover POST /customers/validate no contrato e no Gateway na publicação coordenada. Não manter emissão por documento isolado, entrada REQUEST/Authorizer ou token vinculado a orçamento/OS.
- Não alterar o pacote GerenciamentoMecanica.Auth.Contracts 1.0.0 para transportar contratos de e-mail. Preservar seus formatos de identidade/assinatura na implementação do JWT.

## 2. Regras HTTP comuns

Rotas novas recebem POST com Content-Type application/json, objeto JSON UTF-8 de até 4 KiB e somente as propriedades definidas. Rejeitar propriedades duplicadas/desconhecidas, arrays, valores nulos, tipos incorretos e corpo inválido com 400. Nomes das propriedades seguem a comparação sem distinção de maiúsculas já usada no projeto; duplicações consideram essa mesma comparação. O parser aceita a representação base64 do evento Gateway quando corretamente declarada, aplicando o limite ao conteúdo decodificado.

document é string com CPF/CNPJ numérico válido, com ou sem as máscaras existentes do DocumentRules; normalizar pelo pacote compartilhado. Não aceitar outro documento por header/query nem selecionar cliente pelo JWT enviado a essas duas rotas. Authorization não evita as verificações nem renova uma sessão.

Todas as respostas da autenticação usam Cache-Control: no-store. Sucessos usam application/json; erros usam application/problem+json com type=about:blank, status, title, detail e instance, preservando a forma atual do envelope. instance usa somente a rota reconhecida fixa, nunca caminho/query recebidos; para rota desconhecida, usar marcador fixo /customers/auth. Nunca retornar documento, destinatário, código ou causa interna em erro.

| Código | Title | Detail |
|---|---|---|
| 400 | Requisição inválida | O corpo ou os campos informados são inválidos. |
| 401 | Autenticação não autorizada | Não foi possível autenticar o cliente. |
| 429 | Limite de solicitações atingido | Aguarde antes de solicitar outro código. |
| 500 | Erro interno do servidor | Não foi possível processar a autenticação. |

500 cobre bootstrap, persistência, assinatura e falha conhecida/timeout de envio. Preservar a classificação técnica timeout nos logs/métricas existentes, sem introduzir 504 público nesta entrega. Não converter falha técnica em 401. Rota retirada/desconhecida não pode emitir token; Gateway usa sua negativa nativa de rota ausente e o handler usa 404 fixo quando invocado diretamente. Método incorreto no handler usa 405 e Allow: POST.

## 3. Solicitação do código

POST /customers/auth/request-code

```json
{"document":"529.982.247-25"}
```

202:

```json
{
  "challengeId":"5da98725-e58b-4130-8ef4-22f714b04b6d",
  "message":"Se o cadastro estiver apto, um código será enviado."
}
```

Documento ilustrativo de teste. challengeId é UUID v4 aleatório não vazio, gerado no servidor. Não significa que o cliente existe, que uma mensagem foi capturada ou que o código pode ser confirmado. Após validar/normalizar o documento, consultar o cadastro antes de qualquer gravação. Cliente ausente recebe imediatamente o mesmo 202 e identificador opaco gerado somente em memória: não criar desafio, limite ou linha de auditoria no banco; não gerar código nem invocar envio. Registrar a tentativa no logging sanitizado com mensagem fixa “Solicitação de autenticação para cliente não encontrado.”, sem documento, e-mail ou conteúdo recebido. Cliente Inactive ou sem e-mail utilizável também recebe 202 sem desafio/envio/gravação. Não retornar e-mail mascarado, elegibilidade ou prazo calculado por cadastro. Falhas de consulta/transporte conhecidas continuam 500; não prometer indistinguibilidade temporal entre os caminhos.

Para cliente Active com e-mail válido, aplicar limite/cooldown por UUID do cliente e persistir desafio Pending, confirmar a transação e invocar EmailFunction. Somente depois de resposta Captured válida, atualizar para Ready e responder 202. Se o envio falhar, expirar ou o payload for inválido, invalidar o desafio quando possível e retornar 500. Falha nessa invalidação não mascara a falha original: Pending nunca autentica e expira em cinco minutos. Uma mensagem capturada antes de um timeout pode conter código inutilizável; solicitar outro código respeitando o limite.

Não manter transação/locks PostgreSQL durante Invoke/SMTP. Atualizar Ready condicionalmente ao desafio ainda estar Pending e não ter sido substituído; uma solicitação concorrente não pode reativar código antigo. Se foi substituído/expirou, manter o 202 genérico sem tornar o desafio utilizável. Erro SQL nessa atualização é 500.

## 4. Código, limites e armazenamento

| Parâmetro | Decisão |
|---|---|
| Código | Oito dígitos ASCII, gerados com RandomNumberGenerator; preservar zeros iniciais |
| Validade | Cinco minutos desde created_at UTC; expires_at <= agora é expirado |
| Confirmações incorretas | Máximo de cinco por desafio; a quinta invalida e responde 401 |
| Reenvio | Nova solicitação após pelo menos 60 segundos; invalida desafios anteriores não consumidos do cliente |
| Limite de solicitações | Cinco solicitações admitidas por cliente existente/elegível em uma janela de uma hora, iniciada na primeira solicitação |
| Ao exceder limite/cooldown | 429 genérico com Retry-After inteiro em segundos até a próxima solicitação permitida |
| Remoção dos registros | Desafios elegíveis ao expirar; limites somente após fim da janela de uma hora e do cooldown de 60 segundos; limpeza oportunista limitada, sem retenção extra de 24 horas |
| Sessão | JWT de dez minutos, sem refresh token |

Aplicar cooldown/limite somente depois de localizar um cliente Active com e-mail utilizável. O registro usa seu customer_id; cliente inexistente não gera gravação, inclusive na tabela de limites ou por limpeza oportunista. Solicitação admitida conta mesmo se o envio falhar; rejeição por cooldown não prorroga seu início. Essa proteção por cliente não substitui proteção volumétrica/IP.

O Init.sql cria customer_auth_challenges e customer_auth_request_limits junto das tabelas existentes em banco vazio; sem migrations ou caminho incremental. A primeira guarda id, customer_id, code_salt/code_verifier, created_at, expires_at, state, failed_attempts, consumed_at e invalidated_at. A segunda usa customer_id como chave e guarda início/fim da janela, quantidade e instante da última solicitação admitida. Não armazenar documento ou código em texto puro nessas tabelas. As novas tabelas mantêm chaves primárias, tipos/nulabilidade/defaults e índices comuns de busca. Não acrescentar CHECKs de negócio, UNIQUE condicionado ao estado, FK de cliente ou cascata. Validade, tamanhos criptográficos, estado, tentativas e sequência de operações são validados em C# pela autenticação antes da persistência.

Proteger o código com PBKDF2-HMAC-SHA256, salt aleatório de 16 bytes por desafio, 600.000 iterações e saída de 32 bytes; comparação com CryptographicOperations.FixedTimeEquals. O limite de oito dígitos e cinco tentativas também se aplica antes de executar a derivação. Usar APIs criptográficas nativas, sem pacote/framework próprio.

A tabela de limites é identificada pelo UUID do cliente encontrado na consulta. Não usar document_key, HMAC/HKDF do documento ou derivação adicional da chave JWT para esse índice. O cliente inexistente encerra o fluxo antes da criação/atualização do limite. O PBKDF2 do código permanece independente da chave JWT.

Estados: Pending → Ready → Consumed; Pending/Ready podem passar a Invalidated. Expiração é verificada pelo instante, sem tarefa necessária para desativar o registro. Desafios consumidos não voltam a Ready. Novo pedido admitido substitui os anteriores do mesmo cliente, mesmo quando seu envio falhar; não reutilizar código antigo como fallback.

Na camada de persistência C#, após localizar o cliente elegível, iniciar transação curta e adquirir pg_advisory_xact_lock com chave estável derivada do UUID no SQL parametrizado. Usar a mesma ordem em criação/substituição/ativação/consumo; depois bloquear as linhas de limite/desafios em ordem consistente e revalidar Active, estado e expiração em C#. Uma colisão apenas serializa clientes distintos, sem autorizar acesso. Não usar SELECT FOR UPDATE em customers nem conceder UPDATE nessa tabela para obter locks; cadastro continua somente leitura. Atualizações condicionais e transação devem impedir reativação e consumo duplicado, sem CHECK/trigger/índice UNIQUE de negócio no banco. Os [advisory locks transacionais nativos](https://www.postgresql.org/docs/current/explicit-locking.html#ADVISORY-LOCKS) funcionam entre instâncias. Não depender de memória da Lambda para exclusão mútua. Usar instantes UTC do banco nas transações e TimeProvider nos componentes sem persistência/testes.

Após o commit de cada solicitação admitida, a limpeza tenta remover até 100 registros de cada tabela: desafios com expires_at <= agora UTC do banco e limites com window_expires_at <= agora e last_requested_at <= agora menos 60 segundos. Não há retenção extra de 24 horas. Usa SQL parametrizado, índices existentes e outra transação curta, com FOR UPDATE SKIP LOCKED para deixar linhas ocupadas para depois; auth precisa DELETE somente nessas tabelas. O CancellationToken da limpeza tem prazo total de um segundo, vinculado ao chamador; em falha, rollback reutiliza o orçamento independente existente de um segundo. Limpeza é best effort com diagnóstico sanitizado e não muda o resultado já persistido. Sem job/agendamento: sem novas solicitações admitidas, registros podem permanecer indefinidamente. Isso é elegibilidade para limpeza, não garantia de eliminação pontual.

## 5. Confirmação e JWT

POST /customers/auth/verify-code

```json
{
  "challengeId":"5da98725-e58b-4130-8ef4-22f714b04b6d",
  "code":"01234567",
  "document":"529.982.247-25"
}
```

challengeId deve ser UUID não vazio; code é string com exatamente oito dígitos ASCII. Entrada malformada é 400 e não inicializa autenticação AWS/banco. Entrada válida com desafio ausente, Pending, expirado, Invalidated, Consumed, cliente ausente/Inactive ou documento divergente recebe o mesmo 401. Não reenviar código nem devolver tentativas restantes.

Desafio Ready ainda válido tem uma tentativa debitada por código incorreto ou documento válido divergente; persistir o incremento antes de responder 401. Não lançar erro de domínio que reverta essa gravação. Estados já encerrados/Pending não executam derivação nem reabrem tentativas. Cadastro Inactive/ausente invalida o desafio quando aplicável.

Na confirmação válida, preparar a assinatura e consumir o desafio na transação; só retornar o token após commit confirmado. Falha antes do commit mantém possibilidade de nova tentativa conforme estado; commit ambíguo ou perda de resposta podem consumir o código sem entregar token. Não implementar replay do JWT, reembolso ou entrega única. Duas confirmações simultâneas não podem concluir com dois tokens retornados por esse desafio.

200 permanece uma **string JSON contendo o JWT**, preservando o formato de sucesso atual:

```json
"<jwt-customer>"
```

Preservar issuer/audience por ambiente, HS256 e claims atuais de identidade/perfil compatíveis com o pacote: NameIdentifier é UUID do cliente persistido, Role é Customer e Name mantém o nome cadastrado já emitido hoje. Não acrescentar documento, e-mail, challengeId, order_id ou código às claims. Nenhuma claim vem diretamente da solicitação.

Validade efetiva de dez minutos; configurar ClockSkew=TimeSpan.Zero na validação JWT da API, documentando que a configuração compartilhada também torna estrita a expiração dos tokens internos. Não alterar seus perfis, duração de emissão ou contratos de login. Token expirado é 401 e exige novo fluxo; JWT válido é usado diretamente sem chamar verify-code outra vez.

## 6. Operações Customer na API

- GET /Orders/{id}/status mantém resposta atual, JWT Customer/Mechanic e verificação de titularidade.
- Cliente UUID inexistente ou OS ausente/de outro titular: 404 genérico Ordem não encontrada. Cliente existente Inactive: 403 fixo Acesso não autorizado, antes de consultar a OS. Não usar documento recebido para selecionar titular.
- PATCH /Orders/{id}/budget exige Customer e somente {"approved":true} ou {"approved":false}; campo obrigatório, não nulo, booleano, sem propriedades desconhecidas/duplicadas. CustomerDocument deixa o contrato e headers/query não o substituem.
- Retornar 200 {"decision":"Approved"} ou {"decision":"Refused"} somente depois de commit confirmado. Transição repetida/incompatível continua 422; preservar conflitos 409 quando já mapeados, transações/rollback, estoque e histórico.
- JWT ausente/inválido/expirado é 401; perfil não autorizado é 403. Mechanic não decide orçamento. Não emitir token na conclusão do diagnóstico nem tornar status anônimo.

## 7. Contrato interno EmailFunction v1

Invoke síncrono RequestResponse no alias live da função mecanica-<ambiente>-email. Entrada JSON até 128 KiB, contrato estrito, sem anexos, listas CC/BCC, remetente ou opções de conexão enviadas pelo chamador:

```json
{
  "version":1,
  "type":"customer_access_code",
  "toName":"Cliente de teste",
  "toEmail":"customer@example.invalid",
  "subject":"Código de acesso",
  "textBody":"Seu código de acesso é 01234567. Ele expira em cinco minutos.",
  "htmlBody":"<p>Seu código de acesso é <strong>01234567</strong>.</p>"
}
```

type admite customer_access_code, budget_available e status_changed. Não é autorização distinta por payload: API/auth são chamadores IAM confiáveis do mesmo alias. A origem monta o texto/HTML e faz encoding de valores de cadastro; a EmailFunction valida o envelope e monta o MIME, sem interpretar regras de OS/desafio.

Limites: toName até 100 caracteres; toEmail um único mailbox válido até 254, sem nome de exibição embutido; subject obrigatório até 200, sem CR/LF; textBody obrigatório até 32 KiB UTF-8; htmlBody opcional (ausente ou null) até 64 KiB UTF-8. Nome pode ser vazio quando indisponível; texto/assunto não podem ser vazios. Rejeitar duplicações/campos desconhecidos, controles de header e múltiplos destinatários. Sender fixo por configuração, padrão Mecânica <noreply@mecanica.local>. Nunca aceitar host/porta/credenciais/ARN a partir da mensagem.

Resposta de sucesso:

```json
{"version":1,"outcome":"captured"}
```

Negativa tratada:

```json
{"version":1,"outcome":"failure","error":"smtp-unavailable"}
```

error admite invalid-request, smtp-unavailable, timeout e technical-failure. Não retornar mensagem original, destinatário ou conteúdo. Validar version/outcome/schema da resposta; somente captured confirma aceitação SMTP pelo capturador, não entrega externa/persistência/retenção. Função sem execução válida, FunctionError, payload ausente/malformado ou status de Invoke diferente de 200 são falhas, ainda que o SDK não lance exceção.

O status do Invoke não comprova sucesso da função, conforme a [API AWS](https://docs.aws.amazon.com/lambda/latest/api/API_Invoke.html). Usar LogType=None e não solicitar LogResult. Não criar retries próprios; configurar MaxErrorRetry=0 no cliente Invoke para não repetir envio por política automática de transporte. Reexecução manual e timeout ainda podem duplicar mensagens; não prometer idempotência.

EmailFunction: timeout Lambda de seis segundos e orçamento total de três segundos incluindo parsing, configuração, MIME e SMTP, limitado pelo RemainingTime com um segundo reservado. Validar configuração ao inicializar o transporte na primeira mensagem válida, sem lookup AWS/RDS em cada mensagem. Consumidores: prazo total Invoke de cinco segundos, limitado pelo orçamento restante; auth preserva reserva atual de um segundo mais flush compartilhado de até 500 ms, sem estender deadline. Se não houver margem, não iniciar envio. Falha de envio da OS continua tratada, sem desfazer commit; falha do desafio segue a seção 3.

Logs JSON da EmailFunction registram uma conclusão com lambdaRequestId, type, outcome, durationMs e tipo da exceção quando houver. Mensagem fixa, sem stack trace, mensagens originais, MIME, destinatário, código ou configurações. Não registrar payloads de Invoke no chamador. A extensão da observabilidade da nova função não é pré-requisito deste contrato; preservar os sinais já existentes da API/auth e adequar suas categorias às duas rotas sem dados sensíveis.

Na AuthFunction, nomes de spans/rotas seguem os dois modelos fixos novos. Acrescentar as categorias code-requested (202) e rate-limited (429), preservando authenticated, invalid-request, invalid-document, not-eligible, timeout e technical-failure quando aplicáveis. A métrica de invocações passará a incluir solicitação e confirmação; não apresentar seu total como logins bem-sucedidos. Indicadores de autenticação efetiva continuam filtrando authenticated. Documentar a ampliação da população nos painéis existentes, sem novo contador ou dashboard nesta entrega; código/challengeId/documento/e-mail não viram dimensões.

## 8. Rede SMTP e descoberta

Reutilizar o NLB **interno** da base, sem renomear/substituir o recurso atual ou criar outro balanceador. Acrescentar listener TCP 2525 e target group próprio, com targets do mesmo ASG do EKS, Service NodePort 30025 → SMTP4dev:2525 e health check TCP separado. O Service SMTP mantém porta 25, direcionada à porta nomeada smtp/2525 do container; web usa Service 80 → container 8080. Portas internas acima de 1024 permitem execução sem root e sem capabilities. Preservar listener 80, target group/health checks da API e integração Gateway.

SG reservado da EmailFunction pertence à base; NLB permite entrada 2525 exclusivamente desse SG, além das regras atuais da API. Nós permitem 30025 proveniente somente do SG NLB. SAM email administra egress somente para NLB:2525; não há acesso HTTPS do código, pois não utiliza AWS SDK ou extensão. A função usa subnets privadas já existentes. Sem CIDR público, endpoint público SMTP, hostNetwork, IP fixo de pod/nó ou exposição web pelo NLB. Revisar regras atuais de SG antes de aplicar, sem abrir portas por conveniência.

**Limite explícito do ambiente demonstrativo:** SMTP interno usará TCP sem TLS/autenticação, como a captura local atual, restrito por SG e sem relay. Isso não cifra o código no trajeto dentro da VPC e não é solução de e-mail pessoal para produção real. Não desabilitar validação de certificados para simular TLS. HTTPS da invocação AWS e TLS VerifyFull do RDS permanecem. TLS SMTP exigiria uma entrega própria de nome/certificado/confiança; não será prometido implicitamente por se tratar de rede privada.

SMTP4dev rnwood/smtp4dev:3.15.0, uma réplica, sem PVC: ServerOptions:Database vazio (banco em memória), NumberOfMessagesToKeep=100 e NumberOfSessionsToKeep=100; RelayOptions:SmtpServer vazio e AutomaticEmails vazio, sem configurar credencial/destino de relay. UI somente ClusterIP/port-forward autenticado. Os [manifestos preparados na AE.4](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/AE4_SMTP4DEV_MANIFESTOS.md) limitam a mensagem MIME a 512 KiB, sem ampliar o contrato Invoke v1/corpos, e bloqueiam alterações pela interface. JSON somente leitura, diretórios temporários em memória e filtro de logs sem conteúdo; captura, retenção e perda após reinício conferidas no Docker, com capacidade/acesso AWS pendentes. A [configuração da versão fixada](https://github.com/rnwood/smtp4dev/blob/3.15.0/Rnwood.Smtp4dev/appsettings.json) documenta banco em memória e opções de relay. O Compose local foi alinhado na AE.5: configuração em deploy/smtp4dev/appsettings.json, caixa em memória, relay desabilitado, limite de 100 mensagens/sessões e tmpfs, sem montar smtp_data. Um volume antigo não é apagado automaticamente; o volume PostgreSQL permanece preservado. Captura e perda da caixa após restart foram conferidas com SAM e Compose.

Catálogo novo da base /mecanica/<ambiente>/base/v2/: smtp-host (DNS do NLB), smtp-port (2525) e email-security-group-id. Preservar chaves existentes. Provision email resolve esses metadados e os coloca como configuração não secreta da função; não consulta SSM para cada envio e não guarda configuração SMTP na API/auth. [Rede e recursos SSM preparados na AE.4](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/docs/AE4_REDE_SMTP.md), com validação Terraform local; aplicação e conectividade AWS permanecem pendentes; egress/catálogo/alias do SAM email foram preparados localmente na AE.5.

Catálogo separado /mecanica/<ambiente>/email/v1/: function-name, function-version numérica, invocation-arn do alias live, contract-version=1 e zip-key. Stack email publica via CloudFormation, seguindo proprietário/proteção dos catálogos atuais. API/auth carregam invocation-arn/contract-version na inicialização reutilizada; não buscam ARN em cada request. Conceder leitura somente dessas chaves e Invoke somente do alias exato do ambiente. Atualizar alias com contrato v1 compatível; versão incompatível exige contrato/prefixo e migração próprios.

API passa a invocar envio também localmente, retirando seu transporte MailKit e o adaptador de simulação quando AE.7 estiver concluída. Manter SMTP exclusivamente no projeto EmailFunction; sem referência Npgsql/JWT/domínio auth em seu publish. Nomes físicos auth, pacote compartilhado, serviço mecanica-auth e catálogos auth/v2 não mudam apenas por mover pastas/repositório.

## 9. Execução local e sequência

EmailFunction executa pelo SAM oficial, sem outro executável. Com Docker aberto, usar sam build --template-file templates/email.yaml e sam local start-lambda --template .aws-sam/build/template.yaml --host 127.0.0.1 --port 3001 --docker-network <rede-compose>. O template local/build não pode exigir leitura AWS ou layer remota para o transporte de e-mail. SMTP host é o nome smtp na rede Compose, porta 25. Configuração local não tem credenciais SMTP.

Cliente SDK da API usa endpoint local http://127.0.0.1:3001 quando executada no host; em container Docker Desktop usa http://host.docker.internal:3001, com listener SAM explicitamente acessível ao container somente na execução local. Essa exposição local deve ser declarada no comando, não ativada na AWS. FunctionName local é o identificador lógico EmailFunction; região us-east-1 e credenciais fictícias locais apenas para assinatura contra o emulador. Em hom/prd, proibir override de endpoint e exigir alias ARN do ambiente; não criar fallback automático quando AWS falhar.

Variáveis locais explícitas Email__InvocationEndpoint e Email__FunctionName são somente de execução sem Runtime__Environment; não versionar credenciais reais. O [procedimento AE.5](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE5_EMAIL_FUNCTION.md) fornece comandos SAM/Compose conferidos localmente. A integração do cliente Invoke na API e seu caminho container → SAM permanecem na AE.7. [SAM local start-lambda](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-cli-command-reference-sam-local-start-lambda.html) é o emulador oficial de Invoke; validação local de IAM não será alegada.

Auth continua exigindo Runtime__Environment=hom|prd e AWS_REGION na inicialização real; não criar configuração alternativa de banco/JWT só para esta entrega. Nos testes locais, bootstrap/configuração são injetados, PostgreSQL é descartável e o sender pode usar o handler real EmailFunction ou SAM local. O mesmo contrato v1 é usado por API/auth. Não criar modo que devolva o código publicamente ou teste que dependa de AWS real.

Ordem: AE.2 organiza projetos/identidade → AE.3 banco e AE.4 rede/permissões → AE.5 envio → AE.6 auth e AE.7 API → AE.8 publicação/Gateway → AE.9 comprovação local final → AE.10 AWS autorizada. Preparar contratos antes de publicar qualquer consumidor. O descarte continua Gateway → API/auth → banco/email → base.

## 10. Critérios e revisão de dependências

| Componente | Alteração contratual necessária |
|---|---|
| Bootstrap | Transição OIDC do nome do repo; roles runtime/pipeline email; API/auth Invoke alias e catálogo email; sem acesso SQL/JWT da função email |
| Base | SMTP4dev efêmero, listener/target group/SG/NodePort, ASG attachment e catálogo SMTP; base-destroy exige banco e email ausentes |
| Banco | Init.sql em banco vazio com duas tabelas/índices comuns; sem migrations/regras de negócio SQL; grants mínimos nelas e SELECT email de customers para mecanica_auth |
| Serverless auth | Dois parsers/casos de uso, limite durável, código/consumo/JWT; retirar validate/REQUEST; runtime/flush/sanitização preservados |
| Serverless email | Projeto, testes, template/stack/role/SG/alias/catálogo/ZIP próprios; SMTP e contrato v1 |
| API | Sender Invoke nos dois modos, Active/titularidade, ClockSkew zero, body approved estrito e decisão 200 |
| Gateway | Contrato auth com duas rotas e permissões Invoke específicas; consumidores de auth numérica preservados; sem Authorizer |
| CI/publicação | Reutilizáveis e entradas por função, VersionId/artefatos separados, required checks após rename; nenhuma chamada de envio público |
| Postman | request-code salva challengeId; operador lê SMTP4dev; verify-code salva string JWT; status/decisão usam Bearer; não imprimir/salvar token em evidências |

Comprovar posteriormente: requests estritos, cliente inexistente sem gravação/limpeza/envio e com log sanitizado, limites somente após elegibilidade, zeros iniciais, cinco tentativas, expiração, reenvio, falha/timeout SMTP, resposta capturada perdida, concorrência e consumo único; Active após login; OS de outro titular; mesma mensagem v1 via API/auth; erro FunctionError com HTTP 200; caixa perdida; sanitização e regressões existentes. São critérios de AE.3–AE.9, não testes executados pela consolidação documental.

As proteções de códigos temporários tomam como referência a [OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html). SG do NLB pode restringir origem/targets conforme a [documentação AWS](https://docs.aws.amazon.com/elasticloadbalancing/latest/network/load-balancer-security-groups.html). Configuração não equivale a comprovação: conectividade, comportamento SAM/Docker, recursos do nó, captura, publicação e uso real precisam das etapas seguintes. Renomeação remota, plano/apply e alterações de GitHub/AWS não foram executados na AE.1.


## Preparação transacional AE.3

As operações C# de criação/ativação/invalidação/consumo deste contrato foram preparadas e validadas localmente em 22/10/2026: [implementação e testes](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE3_OPERACOES_TRANSACIONAIS.md). Elas não substituem ainda os endpoints atuais nem invocam envio. [Grants restritos preparados e validados localmente](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/docs/AE3_PERMISSOES_AUTENTICACAO_2026-10-22.md); [Limpeza implementada e validada localmente](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE3_LIMPEZA_EXPIRADOS.md); composição HTTP permanece na AE.6. O resultado interno com código/e-mail não é o DTO de resposta pública e nunca deve ser serializado ou logado como tal.

## Implementação local AE.6 — 22/10/2026

[Handler, casos de uso, Invoke e evidências](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE6_AUTENTICACAO_POR_CODIGO.md). OpenAPI Serverless 2.0.0 substitui a rota antiga pelas duas rotas; publicar coordenadamente com Gateway na AE.8. Bootstrap preserva oito segundos, agora com lotes SSM de nove metadados base/database e dois email/v1, mais duas leituras de Secrets. Alias/ambiente/contrato 1 são validados; Invoke sem retry, logs de resposta ou override de endpoint AWS.

438 testes locais aprovados, com fluxo PostgreSQL/captura SMTP real, ativação, confirmação e JWT; AWS Invoke/bootstrap injetados nos testes, sem certificar IAM/VPC/runtime AWS. Métricas de invocações passam a incluir pedidos de código e confirmações; filtrar authenticated para logins efetivos. Próxima etapa AE.7 mantém esta entrega separada das regras da API, ainda sem alteração de seus contratos.

## Implementação local AE.7 — 22/10/2026

[Envio e acesso Customer na API](AE7_API_ENVIO_E_ACESSO_CUSTOMER.md) concluídos localmente: SMTP somente na EmailFunction, Invoke v1 com prazo/cancelamento, dois lotes SSM, cadastro Active, ClockSkew zero e orçamento explícito/200. 1.212 testes aprovados e caminho API container/SDK/SAM/SMTP4dev comprovado com dados fictícios, sem AWS. A métrica de notificações usa mode=smtp para a captura central em todos os ambientes; não significa entrega externa. Pontos históricos simulated preservados. Publicação/Gateway na AE.8, Postman/roteiro conjunto na AE.9.

## Preparação local AE.8 — 22/10/2026

[Workflows/SAM/Gateway preparados e validados localmente](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE8_WORKFLOWS_E_GATEWAY.md). Auth/email têm CI/publish/provision/destroy separados e reutilizáveis; VersionId fixado, checksum/artefatos imutáveis e prefixos distintos. Auth e deploy API exigem metadados email/v1 com alias live/contrato 1 antes de implantar. Email-destroy exige API/auth removidas; base-destroy já verifica banco/email. Gateway passa a exigir exatamente as duas rotas e OpenAPI auth 2.0.0, mantendo versão numérica auth e pacote Auth.Contracts 1.0.0. Sem Authorizer ou exposição pública email.

438 testes Serverless, SAM/publish/transformação hom/prd, lint, Terraform e Kustomize aprovados; proteções/identidade GitHub reconferidas em modo somente leitura. Não houve workflow remoto ou aplicação AWS. Cadastrar AWS_EMAIL_ROLE_ARN após bootstrap aprovado; publicar coordenadamente na AE.10. Postman e roteiro conjunto continuam na AE.9, sem misturar o plano extra com os aceites oficiais.

## AE.9 — consolidação e comprovação local, 23/10/2026

[Registro e roteiro de publicação](AE9_VALIDACAO_LOCAL_E_DOCUMENTACAO.md). Postman, contratos normativos, RFCs, ADRs e diagramas alinhados ao código AE. Confirmação real de código gerou JWT aceito pela API real para status antes do orçamento e decisão Approved/Refused; titularidade/Active, falha de SMTP e perda da caixa conferidos. Bootstrap auth injetado apenas na composição em memória; nenhum fallback produtivo ou mock de transporte Invoke, novo executável ou script operacional foi criado. 1.650 testes existentes e 47 conferências conjuntas aprovados. Implantação/comprovação AWS continua pendente na AE.10, com autorização própria.
