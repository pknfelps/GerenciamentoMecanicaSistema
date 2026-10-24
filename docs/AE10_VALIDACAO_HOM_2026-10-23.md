# AE.10 — Validação integrada em hom

**23/10/2026 — fluxo principal comprovado na AWS.** Complementa o [plano extra AE](PLANO_EXTRA_AUTENTICACAO_E_EMAIL.md) e a [validação local AE.9](AE9_VALIDACAO_LOCAL_E_DOCUMENTACAO.md). Não reabre os aceites oficiais E5/E6/E7 nem certifica prd.

Endpoint conferido: `https://c00xbwprka.execute-api.us-east-1.amazonaws.com/hom`. Janela HTTP registrada: aproximadamente **18:18–18:42 UTC**, em 23/10/2026. Foram registrados 68 resultados HTTP, incluindo negativas esperadas e tentativas de diagnóstico; esse número não representa 68 testes independentes aprovados.

O mantenedor realizou o provisionamento. Durante esta conferência não foram disparados workflows, planos/applies Terraform ou destroys. A única alteração de infraestrutura foi a interrupção/restauração do SMTP4dev de hom, autorizada explicitamente. Credenciais de teste, documentos fictícios, criação das OSs e cadastro adicional também foram autorizados. Códigos/JWT permaneceram em memória; nenhuma License Key, senha de banco ou chave de assinatura foi consultada.

## Implantação e acesso privado

| Componente | Evidência |
|---|---|
| Autenticação | Stack CREATE_COMPLETE; função Active/Successful, .NET 10, 512 MB/15 s; alias live e integrações do Gateway na versão **14** |
| Envio | Stack CREATE_COMPLETE; função Active/Successful, .NET 10, 512 MB/6 s; alias live na versão **1**; nenhuma Function URL |
| Revisões publicadas | Auth `c7628bcc4dc6e28268047d10788a656ffcd9b9c3`; email `e2c6606bf8738c9068501f3a875445e235d82487` |
| API | Imagem por digest `sha256:4c00fc0f954ca6b701f3983eea6af2bd8a771bcdeea0a341015487f11b208a87`; pod Ready, sem reinício |
| Gateway | Duas rotas auth AWS_PROXY para auth:14; caminho legado customers/validate ausente na conferência das rotas |
| SMTP4dev | Web ClusterIP acessada exclusivamente por port-forward em loopback; SMTP pelo NLB interno/2525 e target NodePort/30025 |
| Segurança de rede | SG email sem ingress; egress TCP/2525 para o SG do NLB; função de envio sem configuração de banco/JWT |
| Caixa | Configuração em memória, relay e retransmissão automática desabilitados, sem PVC; captura não significa entrega externa |

A simulação IAM da configuração implantada confirmou InvokeFunction de API/auth hom somente no alias email hom/live; o alias de prd retornou implicitDeny. A role email não pode invocar esses aliases. As capturas reais comprovam a invocação em hom; simulação IAM não equivale a executar chamadas em prd.

## Fluxo funcional e autorização

| Cenário | Resultado observado |
|---|---|
| Login Mechanic na rota canônica | 200 |
| JSON/documento inválido na solicitação | 400 |
| Pedido elegível | 202 com challengeId; resposta sem código/JWT; captura SMTP real com oito dígitos |
| Confirmação com documento divergente | 401 |
| Confirmação válida | 200 com JWT Customer aceito pela API |
| Reuso do código consumido | 401 |
| JWT | UUID do cliente e role Customer; sem documento, e-mail ou vínculo exclusivo com OS |
| Consulta de status antes do orçamento | 200 com o mesmo JWT Customer |
| Status sem JWT ou com JWT inválido | 401 |
| Diagnóstico pelo Mechanic | Início/conclusão 204 |
| Decisão de orçamento pelo Mechanic | 403 |
| Corpo de decisão com documento extra ou sem approved | 400 |
| Aprovação pelo titular | 200/Approved; status WaitingForExecution; execução e entrega 204; status final Delivered |
| Repetição da decisão | 422 |
| Recusa pelo titular, em outra OS | 200/Refused; status Finished; entrega 204; status final Delivered |
| Outro Customer autenticado | Status e decisão da OS do seed retornam 404 |
| Cadastro inexistente, antes do cadastro adicional | Dois pedidos 202 com identificadores distintos; nenhuma captura adicional; confirmação 401 |
| Cooldown de cliente existente | 429 com Retry-After |
| Cinco códigos incorretos | Cinco respostas 401; o código correto em seguida também retorna 401 |
| JWT comprovadamente expirado | Status 401 |

O cliente adicional foi cadastrado com nome **Validacao Secundario**: a proposta com algarismos no nome foi rejeitada por validação (400), antes do cadastro válido (201). O cliente seed não foi alterado.

Uma chamada inicialmente identificada como teste de JWT expirado retornou 404 porque a sessão do outro titular ainda era válida. A conferência foi corrigida selecionando em memória um JWT cujo exp já havia passado; esse retornou 401. Não foi identificado defeito na expiração a partir daquela tentativa prematura.

As duas primeiras criações persistiram, mas o cliente PowerShell usado inicialmente rejeitou o Location inseguro da resposta antes de registrar o resultado. As OSs foram recuperadas pela consulta operacional e seus fluxos concluídos, sem repetir a criação. A terceira criação registrou diretamente 201 usando HttpClient com redirecionamento desabilitado.

## Interrupção e recuperação do SMTP

O Deployment SMTP4dev foi escalado temporariamente para zero e restaurado para uma réplica em bloco de finalização. Os demais componentes permaneceram provisionados.

1. Durante a indisponibilidade, criar uma OS retornou **201**; a consulta posterior retornou **200/Received**. A falha da notificação não desfez a criação.
2. Solicitar código durante a mesma indisponibilidade retornou **500**, sem afirmar envio ou devolver código/JWT. Os logs de EmailFunction registraram duas falhas `smtp-unavailable`, correspondentes às duas origens.
3. Após recriar o pod, a caixa ficou **vazia**: as 13 mensagens anteriores foram perdidas, conforme o armazenamento efêmero escolhido.
4. Um novo pedido retornou **202**, capturou mensagem, confirmou **200** e emitiu sessão aceita no status da OS (**200**). Outro pedido para o cliente adicional também capturou mensagem após a recuperação.
5. Confirmar o desafio anterior à perda retornou **401**. Como já tinham passado mais de cinco minutos, este resultado não distingue substituição do desafio de expiração; a substituição sem essa ambiguidade foi comprovada no ensaio local AE.9.

Ao final, SMTP4dev, API e Collector estavam Running/Ready, sem reinícios de containers; o pod SMTP foi recriado intencionalmente. Target SMTP do NLB `healthy`, porta 30025. Consumo pontual observado: API 112 MiB/2m CPU, Collector 74 MiB/2m, SMTP4dev 137 MiB/4m. São amostras operacionais, sem ensaio de carga ou garantia de capacidade futura.

## Logs, duração e recursos

Conferidos logs CloudWatch da autenticação/envio, logs do pod da API e logs do novo pod SMTP. Buscas em memória pelos códigos, JWTs, documentos e e-mails usados no ensaio, além do padrão de JWT, retornaram **zero correspondências**. Foram preservados somente resumos técnicos, não os corpos dos logs/e-mails. Os logs do pod SMTP anterior à interrupção não foram recolhidos; não se afirma sua inspeção retrospectiva.

Na amostra recolhida dos eventos JSON `platform.report`:

| Função | Reports | Maior duração | Maior memória usada | Reports com Init Duration |
|---|---:|---:|---:|---:|
| Auth | 25 | 6.940,481 ms | 199 MB | 3; máximo 1.247,158 ms |
| Email | 17 | 905,173 ms | 88 MB | 1; 262,221 ms |

Os 17 eventos técnicos de envio incluíram 15 capturas e duas falhas SMTP esperadas. O número de reports é uma amostra dos logs disponíveis no momento da consulta, não uma reconciliação exaustiva das chamadas. As durações observadas ficaram abaixo dos timeouts configurados; isso não comprova todo cenário de congelamento, concorrência ou saturação.

## Ajustes encontrados e correção local posterior

1. **Postman e casing das rotas:** os arquivos da API usam `/authentication`, `/orders`, `/customers` etc. O Gateway publicado reconhece `/Authentication`, `/Orders`, `/Customers` etc.; as variantes minúsculas testadas retornaram 403. As duas rotas da Lambda `/customers/auth/...` são minúsculas e funcionaram. Alinhar a coleção ao contrato publicado; não mudar indiscriminadamente todas as rotas para maiúsculas.
2. **Location da criação de OS:** a resposta 201 usa `http` e um host diferente do Gateway público. A criação persiste corretamente, mas o endereço não representa o recurso pelo ponto de entrada externo. Revisar geração do Location/contexto do proxy sem aceitar hosts ou forwarded headers de origem não confiável. Não foram alterados código ou configuração para corrigir isso durante a validação.

**Correção local autorizada em seguida, em 23/10/2026:** as 42 requisições da API no Postman foram alinhadas ao casing do OpenAPI. As duas rotas Customer e as três probes permaneceram como publicadas. Os 47 YAMLs e corpos JSON foram lidos, as rotas/métodos da API comparados ao OpenAPI e a sintaxe dos 11 scripts conferida; comparação com HEAD confirmou que os demais campos das requisições não mudaram.

CreateOrder passou a retornar Location relativo para a consulta de status, em vez de CreatedAtAction da própria ação POST. Para criação em `/hom/Orders`, `Orders/<id>/status` resolve para o mesmo domínio/HTTPS e `/hom/Orders/<id>/status`; para `/hom/Orders/`, o valor é `<id>/status`. Não exige novo endpoint, URL pública configurada, conhecimento do stage pela API ou confiança em cabeçalhos de proxy. [Resolução de Location relativo no HTTP](https://www.rfc-editor.org/rfc/rfc9110.html#name-location).

O teste HTTP de criação foi ampliado para os dois formatos de barra final, resolução hom/prd, cabeçalhos de proxy não confiáveis e GET da localização retornada. Build Release/geração OpenAPI aprovados; o OpenAPI gerado permaneceu idêntico ao da AE.9 (SHA-256 `1DA42224DFAE8D03AE8AD2391F2EE89BE433A443FCCB5740F6558D503B4AA9C1`). Publicação da API e reconferência do cabeçalho no Gateway permanecem posteriores; a validação AWS anterior não usou esta correção.

Regressão Release completa com Docker ativo: **1.213 testes aprovados**, zero falhas/ignorados — Auth.Contracts 48, Domain 183, Service 195, Controller 597 e Infrastructure 190. Avisos já existentes de compilação preservados; nenhuma supressão acrescentada. Resultados em `artifacts/ae10/location-and-postman-tests/` e conferência da coleção/OpenAPI em `artifacts/ae10/postman-location-validation.json`. Não houve deploy, workflow remoto ou alteração AWS na correção local.

**Reconferência após deploy do mantenedor — 23/10/2026, 19:19 UTC (16:19 de Brasília):** login `/Authentication`, consulta `/Customers` e `/Vehicles` retornaram 200 pelos caminhos canônicos. Criar uma OS sem serviços/materiais retornou 201 e `Location: Orders/7d590888-5962-4513-9dc1-b9e542cbca75/status`. Resolvido contra a URL de criação, o endereço foi `https://c00xbwprka.execute-api.us-east-1.amazonaws.com/hom/Orders/7d590888-5962-4513-9dc1-b9e542cbca75/status`; GET autenticado retornou 200, o mesmo id e status Received. HTTPS, domínio público e stage preservados; não foi necessário configurar forwarded headers ou alterar o Gateway.

A conferência usou o seed fictício e a autorização já concedida; JWT somente em memória. Uma OS adicional permaneceu Received, sem itens ou reserva de estoque. A primeira tentativa local de login não solicitou JSON e recebeu o JWT como texto; o cliente de validação foi corrigido com Accept application/json antes de qualquer criação. Evidência sanitizada em `artifacts/ae10/location-gateway-post-deploy.json`, sem JWT/corpos de cadastro. Esta reconferência fecha os dois ajustes encontrados; não repete toda a AE.10 nem certifica os limites descritos abaixo.

## Limites e encerramento

**Complemento posterior em 23/10/2026:** as três ressalvas abaixo de SQL, inativação e limite horário foram conferidas na [rodada final de hom](AE10_CONFERENCIAS_FINAIS_HOM_2026-10-23.md), com restauração dos dados temporariamente alterados. As referências a ausência dessas verificações nos itens seguintes descrevem somente o ensaio inicial, preservado como histórico.

- Não houve consulta SQL direta nesta rodada. Ausência de gravações para cadastro inexistente, estados internos dos desafios, limpeza, grants específicos e persistência das tentativas não foram reconciliados diretamente no RDS. Os testes/SQL locais da AE.3/AE.9 permanecem a evidência desses detalhes; o comportamento HTTP/captura acima foi observado na AWS.
- Inativação antes/depois do JWT foi comprovada localmente, sem alterar o seed ou escrever diretamente no banco real nesta rodada. Não foi repetida na AWS.
- Não foram exercitados em AWS o limite de cinco solicitações por hora, confirmações concorrentes, destructive recovery, testes de carga ou prd. A expiração do JWT foi observada; validade/limpeza internas do desafio não foram inferidas apenas do HTTP.
- No ensaio inicial permaneceram três OSs fictícias: duas Delivered e uma Received (criada durante a indisponibilidade), além do cliente adicional autorizado. A reconferência do Location acrescentou uma quarta OS, Received e sem itens. O ambiente descartável permanece provisionado; não houve exclusão automática desses registros.
- Acesso temporário à caixa e sessão local de validação encerrados ao final. Nenhuma mensagem foi retransmitida para o destinatário externo no ensaio; o SMTP continua privado e sem relay.

Evidências locais ignoradas pelo Git em `artifacts/ae10/hom-validation-2026-10-23/`: `http-results.json`, `iam-invocation-review.json`, `log-sanitization-review.json`, `lambda-technical-review.json` e `lambda-resource-summary.json`. Não contêm códigos, JWTs ou corpo de e-mail.

**Conclusão:** autenticação por código, isolamento Customer, decisões Approved/Refused, envio privado centralizado e recuperação após perda da caixa comprovados em hom. Os dois ajustes encontrados foram corrigidos; publicação da API e reconferência do Location/rotas canônicas pelo Gateway concluídas em hom. A rodada final também comprovou SQL, inativação e limite horário; AE.10 concluída no escopo de hom. Não usar esta conferência como aceite de prd ou entrega real de e-mail.
