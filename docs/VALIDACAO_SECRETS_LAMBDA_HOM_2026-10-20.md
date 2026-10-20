# E5 — Tentativa de falha Secrets Manager da Lambda — 20/10/2026

**Atualização posterior:** a [validação com role IAM temporária](VALIDACAO_SECRETS_LAMBDA_IAM_HOM_2026-10-20.md) comprovou a falha AccessDenied, a métrica, os sinais e a recuperação, mantendo Gateway/v10 e removendo a role de teste. Esse resultado fecha a lacuna específica; as duas tentativas de proxy descritas abaixo permanecem inconclusivas.

**Resultado: inconclusivo.** O método de isolamento atingiu a comunicação do runtime Lambda antes de uma resposta do handler; não comprovou a métrica de falha Secrets Manager. A configuração foi restaurada após cada tentativa, e ambas as chamadas de recuperação retornaram 400 para documento inválido, sem FunctionError. O Gateway manteve a versão numérica 10.

O mantenedor autorizou seguir a próxima validação. Foram preservados código, versões publicadas, alias, Gateway, IAM, SSM e valores de Secrets. Nenhuma credencial foi recuperada pelo agente, e não houve deploy ou workflow remoto. O ajuste do console da API continua adiado até a finalização das validações; não foram executados disparos CPU/memória ou carga na API.

## Preparação e isolamento tentado

Conta `121754142617`, região `us-east-1`, função `mecanica-hom-auth`. Leituras anteriores confirmaram runtime dotnet10, 512 MB, timeout 15 s, layer Collector 0.22.0 e configuração Active/Successful. `$LATEST` tinha código e ambiente idênticos à versão 10, sem overrides remanescentes. A integração POST /customers/validate do Gateway `v5qvpym6i7` referenciava explicitamente `:10/invocations`.

O override de endpoint Secrets Manager empregado em outros testes afetaria também a leitura da License Key pela extensão, por isso não foi usado. A alternativa investigada foi o proxy nativo do .NET: [HttpClient.DefaultProxy](https://learn.microsoft.com/dotnet/api/system.net.http.httpclient.defaultproxy) reconhece ALL_PROXY, enquanto o [cliente HTTP Go](https://github.com/golang/go/blob/go1.25.0/src/vendor/golang.org/x/net/http/httpproxy/proxy.go) utiliza HTTP_PROXY/HTTPS_PROXY/NO_PROXY. O [SDK AWS .NET](https://github.com/aws/aws-sdk-net/blob/main/sdk/src/Core/Amazon.Runtime/Pipeline/HttpHandler/_netstandard/HttpRequestMessageFactory.cs) preserva a seleção de proxy do handler quando não há proxy explícito.

Somente `$LATEST` recebeu temporariamente `ALL_PROXY=http://127.0.0.1:9`, apontando para porta local sem serviço de proxy. NO_PROXY incluía comunicação local, credenciais locais e os endpoints regionais SSM, para permitir parâmetros e envio OTLP ao Collector. O teste enviou somente um objeto com document=invalid: caso o bootstrap passasse, o resultado esperado continuaria sendo 400, sem emissão JWT ou leitura de cliente PostgreSQL.

Cada execução usou revisão atual de configuração e restauração em finally, aguardando LastUpdateStatus=Successful e comparando ambiente/código com o baseline.

## Tentativas e recuperação

Horários UTC do serviço; São Paulo corresponde a UTC−3.

| Etapa | UTC | Resultado |
|---|---|---|
| Primeira tentativa | 19:18:53–19:19:04 | Invoke aceito pelo serviço, FunctionError=Unhandled, sem status HTTP do handler; configuração restaurada |
| Recuperação após primeira tentativa | conferida até 19:20:34 | Status do handler 400, FunctionError ausente, `$LATEST` |
| Segunda tentativa | 19:22:53–19:23:02 | Mesmo erro de runtime, mesmo após incluir sandbox.localdomain no NO_PROXY; configuração restaurada |
| Recuperação final | conferida até 19:24:17 | Status do handler 400, FunctionError ausente, Active/Successful, ambiente/código iguais à versão 10 |

O HTTP 200 retornado pela operação AWS Invoke é a aceitação da invocação pelo serviço; **não representa HTTP 200 da autenticação**. Nas duas tentativas com fault, o payload não continha statusCode do handler, e FunctionError foi Unhandled. Portanto não foram apresentadas como HTTP 500 tratado nem como sucesso do experimento.

Os logs CloudWatch confirmaram conexão recusada na porta de proxy e frames de `Amazon.Lambda.RuntimeSupport.InternalRuntimeApiClient.NextAsync`, `RuntimeApiClient.GetNextInvocationAsync` e do loop de bootstrap. A primeira tentativa também identificou sandbox.localdomain na comunicação interna. Acrescentar esse host à exclusão não bastou na segunda tentativa; a razão específica de o bypass não preservar o runtime não foi estabelecida. **Não houve terceira tentativa ou ampliação de permissões/configuração para forçar o resultado.** Isso é uma limitação do método usado, não comprovação de novo defeito funcional do código de autenticação.

O método interceptou uma dependência anterior ao bootstrap de negócio. A inspeção não demonstrou GetSecretValue falhando dentro do componente instrumentado. Na primeira consulta New Relic, janela 19:18–19:25 UTC, somente os acessos recuperados apareceram: SSM get_parameters/success=1 e Secrets Manager get_secret_value/success=2, sem falha comprovada. Ausência de medição não foi convertida em sucesso do teste.

## Estado final e pendência

O último GetFunctionConfiguration confirmou Active/Successful, código e variáveis de ambiente idênticos aos de v10 e ausência de ALL_PROXY/NO_PROXY adicionados. As duas restaurações foram confirmadas, e as duas invocações posteriores retornaram 400/invalid-document. Não houve acesso PostgreSQL necessário para esses documentos inválidos, geração de token, alteração de cadastro ou impacto intencional no tráfego do Gateway/v10.

Os dois logs de recuperação também chegaram ao New Relic com status 400/category=invalid-document, associados aos respectivos marcadores. A primeira recuperação tem trace `6ad7befd646d534b5d33c66d3607bd30`/span `b6955fca6aed8876`; a final, trace `6ad7bfdc69cc02550d91f30924079756`/span `040b949803efe494`. Isso comprova a retomada do logging OTLP das invocações recuperadas, sem depender de interpretar um erro de runtime como falha Secrets.

Após a chegada da segunda recuperação, a consulta 19:18–19:30 UTC confirmou **SSM success=2 e Secrets Manager success=4**, sem série de falha observada. Esses seis acessos conferem com duas inicializações bem-sucedidas, posteriores às restaurações. A primeira consulta ainda mostrava somente uma delas; não foi necessário invocar novamente para preencher a exportação.

**Secrets Manager failure da Lambda permanece pendente.** Para comprová-lo, é necessário outro mecanismo que atinja somente esse cliente sem interferir na extensão nem na comunicação do runtime; esta rodada não criou tal mecanismo. Os testes locais existentes continuam sendo evidência local, sem substituir a comprovação implantada.

Artefato técnico ignorado pelo Git: `artifacts/e5-validacao-integrada/20261020-lambda-secrets-attempt/validation-summary.json`. Contém resultados/IDs técnicos, restaurações e diagnóstico resumido, sem valores de Secrets ou logs brutos. Os eventos CloudWatch foram consultados com paginação nativa; a consulta diagnóstica inicial limitada foi repetida sem limit para conferir os 104 eventos do intervalo.
