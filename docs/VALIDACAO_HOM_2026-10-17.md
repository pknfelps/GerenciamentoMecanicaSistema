# Validação de hom — API, OpenTelemetry e X-Ray

Conferência realizada em 17/10/2026 à noite, horário de São Paulo; janela funcional de 18/10/2026 00:22:30–00:49:40 UTC. Este registro consolida a validação anterior, sem executar novamente workflows, chamadas ou alterações AWS na entrega de métricas Kubernetes.

## Revisões e resultados

- API `020e95d2ec5750d142cdc088b02620f678f82edf`, imagem `sha256:a819b88dcd367826e704687ca63432afa4bc6ac574eeb50dd94603cb701b91ef`. `service.version` recebido no New Relic corresponde ao SHA, substituindo a pendência antiga de versão `local`.
- Autenticação `9df4eadc9385f775441469de0968029d2bbc68ce`, Lambda versão numérica 7 consumida pelo Gateway, tracing Active. Ambos os serviços com ambiente hom.
- 268 chamadas HTTP e 59 conferências adicionais com resultados esperados, zero respostas 5xx. Matriz das 37 ações protegidas, CRUD, aprovação/recusa/entrega de OS, estoque, identidade e atualização/exclusão de usuários, incluindo negativas 400/401/403/404/409/422.
- Cliente fictício Inactive rejeitado em novo login; JWT anterior permanece válido até expirar. Atualização cadastral não reativa o cliente.
- API e Collector Ready sem reinícios. API com uma réplica, HPA 1–3. Amostra de consumo: API 2m CPU/116 MiB; Collector 1m/36 MiB. Isso antecede a coleta Kubernetes e não comprova seu consumo futuro.
- Probes internas live/ready/startup 200; não publicadas no Gateway. RDS PostgreSQL 16.15 privado/criptografado, NLB healthy em 30080, VPC Link disponível e Job database-init concluído, sem Secrets temporários remanescentes.

## Histórico SQL e métricas de negócio

Conexão SQL temporária com TLS 1.3, credencial restrita e sem exposição de senha. Treze eventos conferidos: sete da OS aprovada, cinco da recusada/entregue e um da OS excluída; sequência, encadeamento e preservação após exclusão confirmados.

| Indicador | Amostras | Soma em segundos, SQL e New Relic |
|---|---:|---:|
| Diagnóstico | 2 | 3.112321 |
| Execução | 1 | 1.087185 |
| Espera pela entrega | 2 | 2.964376 |
| Total do serviço | 1 | 9.681048 |

Três criações, cinco amostras de etapa, uma de serviço e onze notificações recebidas; zero falhas de medição/processamento na consulta. Não comprova entrega única ou reconciliação automática.

## Ingestão e proteção dos sinais

Na conta New Relic US 8626152, os três sinais OTLP foram recebidos para `mecanica-api` e `mecanica-auth`. Spans PostgreSQL relacionados às operações e logs correlacionados: API 29/29, auth 22/22 com trace.id. As onze mensagens fixas de envio de e-mail simulado também chegaram, sem falhas SMTP nas amostras inspecionadas.

Consulta em 577 spans OTLP, excluindo `xray.polling`, não encontrou db.statement, db.query.text, exception.stacktrace, url.full, url.query ou http.url. Amostra de logs da nova revisão sem valores fictícios sensíveis conhecidos, JWT ou stack trace. Nenhum erro de exportação nas amostras dos dois Collectors. A ausência desses valores na amostra não é uma auditoria irrestrita de todos os atributos/casos de falha.

Autenticação: 22 chamadas, 22 amostras de duração e 22 reports da Lambda; resultados authenticated=8, invalid-request=9, invalid-document=1 e not-eligible=4. Logs Gateway conferidos para 268/268 request IDs/códigos, todos com xrayTraceId.

## X-Ray nativo

Conexão `mecanica-xray` confirmada por API polling, somente X-Ray/us-east-1/cinco minutos. Após o polling, 786 spans com `newRelic.ingestPoint='xray.polling'` observados na janela 00:19:49–00:49:49 UTC. Três traces AWS inspecionados com segmentos Gateway/Lambda/Function relacionados.

**Os segmentos nativos incluem URL/IP e caminhos com IDs.** Não passam pela sanitização OTLP da aplicação. Receber os dois caminhos no New Relic não comprova sua união: a correlação W3C/X-Ray continua pendente por decisão do mantenedor.

## Limites e continuidade

Pod SQL e port-forward encerrados; segredos e tokens não integram as evidências. Não houve deploy, restart, alteração IAM ou destroy nessa validação. Foram utilizados dados fictícios; o cliente principal de validação permaneceu Inactive. Não foram induzidos 500, indisponibilidade, saturação, falhas IAM ou rollback em recursos reais.

Permanecem para entregas próprias: métricas/enriquecimento Kubernetes após implantação, CloudWatch, correlação W3C/X-Ray, dashboards, alertas, amostragem/congelamento/perdas sob falhas e validação de prd. E5 não está integralmente concluída.

Evidências detalhadas locais, não versionadas: `artifacts/validation-20261018/RESULTADO.md`, `http-results.json`, `authorization-matrix.json`, `sql-snapshot.json`, `duration-reference.json`, `gateway-correlation.json`, `newrelic-observations.json`, `log-safety.json`, `probes.json` e `xray-segments-sanitized.json`. Este resumo versionado preserva os resultados e limites, sem incluir credenciais ou dados pessoais.
