# E5 — Ajustes de logging da API — 20/10/2026

**Atualização de 21/10:** ajuste publicado e bootstrap reconferido no EKS: código 1, linha fixa/tipo, zero frames e métrica SSM failure=1 recebida. O caminho Information/499 de cancelamento continuou inconclusivo na AWS, preservando a comprovação local abaixo. E5 encerrada por aceite do mantenedor com essa limitação explícita. Ver [encerramento e reconferência](ENCERRAMENTO_E5_2026-10-21.md).

**Implementados e comprovados localmente:** saída segura da falha de inicialização e tratamento de cancelamento pelo cliente. Nenhum deploy, chamada AWS/New Relic, mudança de infraestrutura, Collector ou workflow foi realizado. Base a8ba2615d9fe918fc161d975fabd406b54fd0ff2 com alterações locais ainda não publicadas; o mesmo SHA de metadados no publish não significa que esse binário já esteja implantado.

## Alterações

### Inicialização

Program.Main passa a devolver código de saída: 0 para encerramento normal e 1 para falha. A execução existente foi mantida em RunAsync, incluindo o único provider antecipado de métricas e seu descarte nativo limitado.

Na falha da execução normal, o ponto de entrada escreve somente:

```text
Falha na inicialização ou execução da API. ExceptionType: System.InvalidOperationException
```

O tipo varia conforme a exceção capturada; mensagem original, inner exception, Data e stack trace não são passados ao console. O processo continua falhando quando a configuração obrigatória não pode ser carregada; não há fallback nem inicialização parcial. O descarte de métricas acontece antes de a falha chegar a essa borda.

A geração oficial do OpenAPI mantém seu caminho próprio e os diagnósticos da ferramenta de build. Não cria providers nem consulta AWS.

### Cancelamento pelo cliente

ExceptionHandlingMiddleware reconhece OperationCanceledException, incluindo TaskCanceledException, somente quando RequestAborted foi solicitado.

- Registra Information, com mensagem fixa de cancelamento e propriedades técnicas existentes, sem passar o objeto de exceção.
- Não escreve Problem Details nem corpo para uma conexão abandonada.
- Antes de a resposta começar, registra status técnico 499; se começou, preserva o status existente.
- O cliente já desconectado não recebe 499 como uma nova resposta/contrato.
- Cancelamento interno sem RequestAborted continua gerando tratamento 500; não é ignorado como desconexão.
- A proteção existente contra contagem de falha de OS em requisição abortada permanece preservada.

Nenhum filtro de log foi removido, nenhum objeto Exception passou a ser enviado ao logger e nenhuma supressão de analisador foi acrescentada.

## Testes automatizados

Seis casos novos nos projetos existentes, com nomes descritivos:

1. Configuração de runtime inválida retorna código 1 e apenas linha fixa/tipo.
2. Configuração de telemetria inválida tem o mesmo tratamento seguro.
3. OperationCanceledException por desconexão gera Information/499 e corpo vazio.
4. TaskCanceledException por desconexão preserva o mesmo comportamento.
5. Resposta já iniciada mantém seu status e não recebe Problem Details.
6. Cancelamento interno sem RequestAborted continua 500/Error no teste HTTP existente.

Os testes de cancelamento incluem mensagem, exceção interna e Data fictícios privados; a captura estruturada confere ausência desses valores e argumento Exception nulo. A renderização textual e os cenários anteriores de sanitização continuam cobertos. Não foram criadas suítes de telemetria, scripts permanentes, exporters fictícios ou dependências.

Regressão completa Release, sem filtros e com Docker ativo:

| Projeto | Aprovados | Falhas | Ignorados |
|---|---:|---:|---:|
| Auth.Contracts.Tests | 48 | 0 | 0 |
| DomainTests | 183 | 0 | 0 |
| ServiceTests | 195 | 0 | 0 |
| ControllerTests | 597 | 0 | 0 |
| InfrastructureTests | 174 | 0 | 0 |
| **Total** | **1.197** | **0** | **0** |

Os 17 testes direcionados iniciais também passaram. A regressão final inclui o uso de CancelAsync nos testes, evitando os avisos novos de cancelamento síncrono.

## Conferência operacional local do publish

Entre **23:35:49 e 23:36:12 UTC**, containers descartáveis executaram a revisão corrigida em rede Docker interna. API/Collector usaram filesystem somente leitura e usuário sem root. Nenhuma credencial real, dado de negócio ou destino externo foi utilizado.

### PostgreSQL pausado e cancelamento

PostgreSQL 16.15 real e descartável, pooling desabilitado:

| Fase | Cliente | Duração | Instrumento |
|---|---|---:|---|
| Baseline | 200 | 0,242607 s | success=1 |
| Banco pausado, aguarda prazo | 503 | 2,035315 s | timeout=1 |
| Cliente encerra em 300 ms | Sem resposta; curl 28 | 0,302262 s | canceled=1 |
| Banco retomado | 200 | 0,007051 s | success=1 |

Collector Contrib 0.161.0 recebeu OTLP real. O log de cancelamento foi **Information**, mensagem fixa, StatusCode=499, tipo TaskCanceledException e IDs técnicos. Não houve log de erro inesperado/500 para esse caso. Trace d4f50d78fa63afebc673a2df13f667f6; request 0HNPF0BH3QPK0:00000001. As quatro medições continuaram separadas por outcome/readiness. A API encerrou graciosamente com código 0.

### Falha do SDK no bootstrap

Outro container recebeu runtime hom e credenciais de teste inteiramente fictícias. A região foi mantida, mas o SDK SSM apontou exclusivamente para loopback fechado; a rede Docker também bloqueava saída externa. Limite externo de 55 s.

O cliente real falhou no acesso SSM. O processo encerrou com **código 1**, stdout/stderr combinado contendo somente a linha fixa/tipo, **zero frames** e sem mensagem interna, endpoint ou credenciais. O Collector recebeu **ssm/get_parameters/bootstrap/failure=1**, comprovando que o descarte/exportação final de métricas foi preservado.

Essa conferência operacional exercitou SSM. Os testes existentes do carregador continuam cobrindo Secrets Manager e falhas de configuração; não foi simulada uma resposta Secrets via novo servidor/mock. A correção da borda se aplica às falhas que atravessam esse mesmo ponto de entrada. A comprovação implantada dos caminhos SSM/Secrets continua dependente da publicação posterior.

Containers de validação, dados tmpfs e rede removidos; ausência confirmada separadamente. PostgreSQL local foi usado sem TLS nessa massa descartável; isso não altera nem certifica o TLS do RDS.

## Build, contrato e publicação

- Restore e build Release da solução concluídos.
- OpenAPI oficial gerado com Runtime__Environment e Telemetry__Enabled propositalmente inválidos: sucesso sem AWS/providers.
- Contrato byte a byte idêntico ao anterior: SHA-256 D784C3B554A65C4F8CFE732C0975B8BFEDAF78D67F9C6ED69E2B07220C5F5D84.
- Publish linux-x64 sem self-contained concluído; runtimeconfig, manifesto e certificado presentes.
- Certificado idêntico à origem: SHA-256 B1711D12BAE51838581281E23B6CB97B1074016873B4DAFC80ED14002462DD77.
- Renderizações Kustomize base/hom/prd concluídas, sem edição dos manifestos.
- git diff --check aprovado.

Avisos NU1902/NU1903 de System.Security.Cryptography.Pkcs e avisos existentes de código continuam separados. O novo log Information segue a mesma política de tipo/mensagem fixa e também mantém visível S6667; passar a exceção completa contrariaria a sanitização aprovada. Não foram atualizados pacotes, ocultados avisos ou corrigidos assuntos fora deste pedido.

Evidências locais ignoradas pelo Git em artifacts/e5-api-log-fixes: TRX completos, test-summary.json, publish, contrato anterior, renderizações e logs/resumo dos cenários Docker.

**Situação:** os dois ajustes estão concluídos localmente. Publicar a API e conferir somente logs da nova revisão antes de marcar a comprovação AWS de E5.2. A união X-Ray/OTLP e o limite de eventos tardios do Gateway permanecem separados e não foram alterados.

