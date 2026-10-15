# Observabilidade local da API

**Estado atual — 14/10/2026:** coleta dirigida pelo ConfigMap, validada com **1.142 testes aprovados, zero falhas/ignorados**, incluindo 45 de observabilidade. Conteúdo do ConfigMap/publicação idêntico ao arquivo fonte; filtros/meters nativos e regras alteráveis sem recompilar comprovados. Build Release, PostgreSQL/Collector reais, OpenAPI repetido/idêntico, publish/certificado e Kustomize aprovados. Os resultados datados no fim do documento registram as entregas anteriores; este estado é o vigente. Sem implantação AWS/New Relic.

A instrumentação fica em `GerenciamentoMecanicaSistema/Observability`, na composição da API. Não há dependências OpenTelemetry em Domain/Application, spans manuais de negócio ou mudanças de persistência/transação. O console continua textual, com mensagens fixas e tipo do erro, sem stack trace.

## Configuração

A fonte única é [deploy/kubernetes/base/observability.json](../deploy/kubernetes/base/observability.json). O Kustomize usa esse arquivo para criar o ConfigMap `mecanica-api-observability`; o Deployment o monta somente para leitura em `/app/Observability/observability.json`. A API copia o mesmo arquivo no build/publish para uso local e o lê pelo provider JSON nativo do .NET. Não há montagem de YAML por scripts, parser YAML ou duplicação das regras no appsettings.

O arquivo entra como configuração de menor prioridade: appsettings, user-secrets, variáveis e argumentos existentes podem sobrescrever valores. Ele não é lido durante a geração oficial do OpenAPI. Sem arquivo e com telemetria desabilitada, não há instrumentação; habilitar sem regras de coleta válidas impede a inicialização com mensagem fixa. Não há recarga dos providers em execução.

| Seção do arquivo | Responsabilidade |
|---|---|
| `Telemetry:Collection:ExcludedPaths` | Caminhos literais excluídos dos traces HTTP e de seu SQL; comparação sem distinção de maiúsculas e tolerando barra final. |
| `Telemetry:Collection:HttpMethods` | Métodos reconhecidos para nomes seguros; demais usam marcador OTHER. |
| `Telemetry:Collection:HttpTraceAttributes` / `DatabaseTraceAttributes` | Listas de atributos permitidos antes da exportação. |
| `Telemetry:Collection:ExceptionAttributes` | Campos técnicos permitidos nos eventos de exceção já sanitizados. Valores nulos, inclusive stack trace, não são exportados. |
| `Telemetry:Collection:HttpMetricAttributes` | Dimensões permitidas antes da agregação HTTP. |
| `Telemetry:Collection:HttpErrorMessage` / `DatabaseErrorMessage` / `DatabaseSpanName` | Textos fixos dos spans/eventos; não contêm conteúdo recebido ou mensagem original da exceção. |
| `Logging:OpenTelemetry:LogLevel` | Filtros nativos por categoria, com Default None e somente categorias da aplicação habilitadas. |
| `Metrics:EnabledMetrics` | Seleção nativa dos meters; HTTP/runtime habilitados inicialmente. |
| `OTEL_*` | Identidade, lotes, filas, intervalos, temporalidade e timeouts. |

O arquivo é também uma política de proteção de dados, revisada junto ao manifesto: manter somente atributos técnicos, sem SQL, parâmetros, endereço da conexão, caminho/query recebidos ou identificadores pessoais nas métricas. Não habilitar categorias internas de frameworks/driver/exportador. As listas controlam o que é retido; não acrescentam acesso a dados de negócio.

O C# continua aplicando as regras dentro da API: registra SDK/instrumentação, filtra antes de exportar/agregar, obtém a rota confiável, descarta baggage/tracestate, intercepta exceções para usar apenas tipo/texto fixo e gerencia o ciclo de vida. `IncludeScopes=false` e `RecordException=false` continuam impostos em código por segurança, evitando escopos HTTP e captura automática de mensagens originais. O Collector recebe somente os sinais já tratados.

| Campo | Comportamento |
|---|---|
| `Telemetry__Enabled` | Ausente/false: não registra providers/listeners nem exporta. True: habilita os três sinais. Valor inválido impede a inicialização. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Obrigatório quando habilitado: endereço HTTP/HTTPS absoluto, sem credenciais, query ou fragmento. Exemplo local: `http://localhost:4318`. |
| `Telemetry__OtlpEndpoint` | Alternativa legada, usada somente se a variável padrão não estiver definida. |
| `OTEL_SERVICE_NAME` | Nome do serviço, padrão `mecanica-api` no arquivo declarativo. |
| `OTEL_RESOURCE_ATTRIBUTES` | Atributos técnicos separados por vírgula, interpretados pelo SDK; podem substituir versão/ambiente ou acrescentar metadados técnicos. |

Erros de configuração têm mensagem fixa, sem reproduzir os valores recebidos. Configuração desabilitada ignora o endpoint. A geração oficial do OpenAPI ignora toda a composição de telemetria, inclusive configurações inválidas.

Sem substituição explícita, `service.version` vem do commit nos metadados do assembly (fallback `local`) e `deployment.environment.name=local/hom/prd` vem do runtime existente. O detector nativo lê as variáveis padrão pela configuração da aplicação; não há parser próprio nem detecção automática de host/usuário. Use somente metadados técnicos: nunca coloque dados pessoais, credenciais ou tokens em atributos.

Os overlays hom/prd já definem `OTEL_SERVICE_NAME=mecanica-api` e `OTEL_RESOURCE_ATTRIBUTES=deployment.environment.name=hom|prd`. Não definem Enabled ou endpoint, portanto não ativam exportação. Para uma revisão com versão explícita, a configuração pode usar `OTEL_RESOURCE_ATTRIBUTES=deployment.environment.name=hom,service.version=<commit>`. Sem isso, prevalece o commit do assembly ou o fallback. Não foi alterado Dockerfile/workflow para injetar commit nesta entrega.

Os ajustes operacionais abaixo ficam no arquivo declarativo e são lidos diretamente pelo SDK. Variáveis de ambiente com os mesmos nomes substituem esses padrões, sem recompilar:

| Variáveis | Padrão |
|---|---|
| `OTEL_EXPORTER_OTLP_TIMEOUT` | 3000 ms |
| `OTEL_BSP_MAX_QUEUE_SIZE` / `OTEL_BLRP_MAX_QUEUE_SIZE` | 2048 registros |
| `OTEL_BSP_MAX_EXPORT_BATCH_SIZE` / `OTEL_BLRP_MAX_EXPORT_BATCH_SIZE` | 512 registros |
| `OTEL_BSP_SCHEDULE_DELAY` / `OTEL_BLRP_SCHEDULE_DELAY` | 1000 ms |
| `OTEL_BSP_EXPORT_TIMEOUT` / `OTEL_BLRP_EXPORT_TIMEOUT` | 3000 ms |
| `OTEL_METRIC_EXPORT_INTERVAL` | 30000 ms |
| `OTEL_METRIC_EXPORT_TIMEOUT` | 3000 ms |
| `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE` | delta |

`BSP` configura traces e `BLRP`, logs. O transporte continua HTTP/protobuf para um único endpoint, sem chave ou cabeçalhos de autenticação na API; overrides de endpoint/protocolo por sinal e headers OTLP são rejeitados com mensagem fixa para preservar esse contrato. O SDK acrescenta os caminhos de cada sinal, sem montar URLs por código próprio. Não há scripts novos.

## Atualizar o ConfigMap

Edite o arquivo declarativo e publique pelo fluxo normal do repositório. `kubectl apply -k` do workflow inclui o ConfigMap; o deploy de uma nova imagem reinicia os pods normalmente. O nome do ConfigMap é estável, sem sufixo de hash, para que atualizações não acumulem recursos e o `api-destroy` existente remova a configuração junto com o overlay.

Se alterar somente o ConfigMap de uma implantação existente, será necessário reiniciar os pods após aplicar a configuração, usando o comando padrão abaixo. O mount com subPath e a leitura na inicialização não fazem atualização automática:

```powershell
kubectl rollout restart deployment/deploy-gerenciamento-api --namespace default
kubectl rollout status deployment/deploy-gerenciamento-api --namespace default --timeout=600s
```

Esses comandos são um procedimento de operador, não foram executados nesta entrega. Não use a imagem provisória `pending` num apply manual: preserve o digest implantado conforme o procedimento de deploy. O ConfigMap contém somente configuração pública de telemetria, nunca JWT, banco, credenciais ou chave New Relic. Os overlays continuam sem ativação e sem endpoint até a entrega do Collector EKS.

Versões fixas: OpenTelemetry Hosting/OTLP `1.19.1`, instrumentação ASP.NET Core `1.19.0`, Npgsql.OpenTelemetry `10.0.2`. Métricas de runtime usam o meter nativo `System.Runtime` do .NET 10.

## Executar

Docker com engine Linux deve estar ativo; o procedimento não inicia Docker Desktop. O Compose de observabilidade é independente: não inicia API, banco ou SMTP e não prepara tabelas. Configure previamente a execução local da API, com conexão PostgreSQL e chave JWT válidas por variáveis/user-secrets; o esquema continua no repositório do banco. Para o processo executado no host, o banco publicado pelo Compose usa `localhost`, e o SMTP local usa `localhost:2525` quando necessário. Não copie credenciais para esta configuração do Collector.

Na raiz do repositório:

```powershell
docker compose -f compose.observability.yaml config --quiet
docker compose -f compose.observability.yaml up -d
$env:Telemetry__Enabled = "true"
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4318"
dotnet run --project GerenciamentoMecanicaSistema --launch-profile https
```

Deixe `Runtime__Environment` ausente para usar configuração local, sem leituras AWS. As variáveis acima não substituem as configurações JWT/banco já exigidas. O perfil HTTPS usa o certificado local de desenvolvimento; se necessário, prepare-o pelo procedimento padrão do .NET. Não é preciso alterar o Compose existente ou os manifestos de produção.

Em outro terminal:

```powershell
docker compose -f compose.observability.yaml logs -f collector
```

Execute chamadas locais da API pelo Swagger/Postman com tokens de teste locais. Uma chamada a `/Customers` produz span HTTP e comandos PostgreSQL relacionados; um erro tratado produz log correlacionado. As probes `/health/live`, `/health/ready` e `/health/startup` não produzem spans, mas mantêm métricas e respostas. Espere até 30 segundos para a exportação periódica das métricas. Os logs debug do Collector exibem recursos e os três sinais recebidos; o console da API permanece textual.

Para conferir indisponibilidade, pare apenas o Collector e repita chamadas/probes:

```powershell
docker compose -f compose.observability.yaml stop collector
docker compose -f compose.observability.yaml start collector
```

Finalize a API com Ctrl+C e encerre o Collector depois de permitir o envio dos lotes pendentes:

```powershell
docker compose -f compose.observability.yaml down
Remove-Item Env:Telemetry__Enabled, Env:OTEL_EXPORTER_OTLP_ENDPOINT -ErrorAction SilentlyContinue
```

O projeto Compose `mecanica-observability` não remove banco/SMTP do Compose principal. Não há volumes persistentes; os registros locais do Collector desaparecem com a remoção do container. Só `127.0.0.1:4318` é publicado. O exemplo executa a API no host; um container da API precisaria de configuração explícita de rede/endereço, fora deste Compose separado.

## Dados e limites

A [observabilidade de negócio](OBSERVABILIDADE_NEGOCIO.md) acrescenta o meter `Mecanica.Api.Business` à mesma configuração/exportação: criações, intervalos concluídos, falhas de ações de OS e notificações. As regras e consultas de conferência estão no procedimento específico, sem outro exporter ou dependência de New Relic para testar localmente. Referências anteriores neste documento a métricas de negócio pendentes descrevem a primeira entrega técnica.

- **Traces:** W3C, todas as requisições de aplicação mesmo com pai não amostrado; probes e seu SQL excluídos. Nomes HTTP usam verbo reconhecido/modelo de rota ou `OTHER`/`unmatched`; PostgreSQL usa nome fixo. Remover headers, URL/query, SQL, parâmetros, identificadores da conexão, baggage e tracestate antes da exportação. Manter somente método, rota, status, protocolo/esquema, sistema/nome/operação do banco e tipo/código técnico do erro.
- **Exceções:** `ActivityListener.ExceptionRecorder` substitui o conteúdo por tipo/mensagem fixa na origem. O processor remove o atributo de stack trace do evento e fixa a descrição de erro antes da exportação. Sem reflexão, mensagem original, exceção interna, Data ou mascaramento de strings.
- **Logs:** um provider OTLP, somente categorias da aplicação `GerenciamentoMecanicaSistema.`, `Infrastructure.` e `Service.`, nível Information ou superior. TraceId/SpanId automáticos, escopos desabilitados. Preservar filtros/console existentes; não reenviar stdout nem logs internos de frameworks, driver ou exportador. Mensagens/propriedades seguem a sanitização da E4.
- **Métricas técnicas:** duração/requisições HTTP ativas e runtime (memória, CPU do processo, GC, thread pool). Dimensões HTTP limitadas a método, rota, status, protocolo/esquema; sem usuário, documento, OS ou UUID. Não representam CPU/memória de nós/pods. Os indicadores de OS usam o meter separado de negócio, no mesmo provider/exporter.

OTLP HTTP/protobuf em `/v1/traces`, `/v1/logs` e `/v1/metrics`, sem autenticação. Os valores da tabela preservam os padrões anteriores: métricas 30 s/delta e batches 512/fila 2.048/intervalo 1 s/timeout 3 s. `UseOtlpExporter` registra a exportação dos três sinais; não registrar também exporters OTLP individuais. Providers/listener pertencem ao host e são descartados com ele. Collector usa `memory_limiter`, batch e exporter debug; memória limitada a 256 MiB e CPU a 0,5 no Compose.

As regras de sanitização, exclusão e dimensões estão no arquivo declarativo; o código as aplica antes de sair da aplicação e antes de criar séries desnecessárias. Interceptação de exceções, eliminação de contexto recebido e proteção contra escopos permanecem implementadas na API. O YAML do Collector continua responsável pelo processamento/transporte central; não substitui a proteção na origem.

Não há persistência, retentativas próprias ou garantia de entrega exatamente uma vez. Indisponibilidade, encerramento abrupto ou fila cheia podem perder sinais; o exportador não participa da readiness nem das transações. A validação funcional não é teste de carga.

## Validação e próxima entrega

Os testes existentes de HTTP/PostgreSQL foram ampliados com exporter InMemory somente no projeto de testes. Exercitam configuração, contexto recebido, vínculo HTTP→PostgreSQL, correlação concorrente, probes, descarte, sanitização e exportação real dos três sinais para Collector em container. Incluem exceção PostgreSQL real e continuidade com Collector parado, sem AWS/RDS externos. Docker indisponível é falha, sem testes ignorados.

```powershell
dotnet restore GerenciamentoMecanicaSistema.slnx
dotnet build GerenciamentoMecanicaSistema.slnx -c Release --no-restore
dotnet test GerenciamentoMecanicaSistema.slnx -c Release --no-build --no-restore
```

E5.1–E5.3 continuam parciais: falta Collector EKS/ingestão New Relic, conta/região e entrega segura da chave apenas ao exportador correspondente. Lambda, CloudWatch/X-Ray, métricas Kubernetes, dashboards e alertas ficam nas próximas entregas. A instrumentação local de negócio está no procedimento específico. Nenhum recurso AWS foi alterado. Avisos transitivos NU1902/NU1903 e avisos S6667 permanecem visíveis, fora deste escopo. Decisão em [ADR 005](arquitetura/adrs/005-OBSERVABILIDADE.md).

Validação em 14/10/2026: **1.128 testes aprovados, zero falhas/ignorados**, incluindo 31 específicos desta entrega. Restore/build Release, publish com certificado RDS e geração oficial do OpenAPI aprovados. Duas gerações do mesmo build com Runtime hom/telemetria inválida preservaram o contrato 3.0.4 byte a byte, SHA-256 `d784c3b554a65c4f8cfe732c0975b8bfedaf78d67f9c6ed69e2b07220c5f5d84`. Três sinais e sanitização da exceção PostgreSQL comprovados no Collector real; operações/readiness continuaram com ele parado. Isso não comprova ingestão New Relic ou implantação AWS.

Após a simplificação declarativa, em 14/10/2026: **1.136 testes aprovados, zero falhas/ignorados**, incluindo 39 de observabilidade. Configuração nativa de identidade/ajustes operacionais comprovada nos três sinais; exportação real, sanitização, probes e indisponibilidade preservadas. Build Release, OpenAPI repetido/idêntico, publish/certificado e renderizações Kustomize base/hom/prd aprovados. Overlays alterados somente para identidade, sem ativação ou deploy. Referência de configuração: [SDK OTLP .NET 1.19.1](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md#environment-variables).
