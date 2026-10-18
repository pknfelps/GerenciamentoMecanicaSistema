# Validação Kubernetes/New Relic — hom, 17/10/2026

Conferência após o `base-provision` executado pelo mantenedor, run `39086883099`, revisão de infraestrutura `92238eb`. Janela de observação de 18/10/2026 aproximadamente 01:34–01:48 UTC, noite de 17/10 em São Paulo. Conta AWS 121754142617, New Relic US 8626152. Apenas consultas e chamadas controladas de leitura/login; sem deploy, restart, mudança IAM, leitura da License Key ou alteração de cadastros.

## Collector e permissões

- Collector Contrib 0.161.0, uma réplica Ready, zero reinícios. ConfigMap implantado igual ao YAML local revisado.
- Pod com token Kubernetes habilitado; ServiceAccount mantém padrão desabilitado. RBAC explícito somente leitura, mais get nodes/stats, sem Secrets, escrita ou nodes/proxy. Identidade AWS preservada.
- Sem erros de autenticação, RBAC, certificados, coleta ou exportação nos logs da janela. Recebimento das métricas kubelet comprova acesso com o TLS verificado configurado, sem fallback inseguro.
- Apenas avisos não impeditivos de aliases legados (`otlphttp`, `k8sattributes`, `kubeletstats`) e normalização de prefixo OTTL. Nenhuma configuração foi modificada para tratar esses avisos.
- Consumo observado: cerca de 2m CPU e 55–56 MiB de memória, abaixo dos limites 500m/256Mi. Nó com aproximadamente 74–75% da memória alocável em uso, sem MemoryPressure. São amostras de baixa carga, não teste de capacidade ou garantia futura.

## Métricas no New Relic

As **23 métricas previstas** chegaram com `k8s.cluster.name=mecanica-hom-eks` e `deployment.environment.name=hom`. Cobertura observada em default, observability, kube-system e database-init. A coleta de fase inclui o Job já concluído: Succeeded=3; sua ausência de uso CPU/memória atual é esperada.

| Verificação | Resultado |
|---|---|
| CPU alocável do nó | 1,93 cores, equivalente a 1930m no Kubernetes |
| Memória alocável | 1.503.227.904 bytes, equivalente a 1.467.996Ki; não descrita como memória física |
| CPU do nó | Amostra New Relic 60m; kubectl top próximo de 62m na janela |
| Working set do nó | Cerca de 1075 MiB; coerente com kubectl top, admitindo diferença de janela |
| Condições | Ready=1, MemoryPressure/DiskPressure/PIDPressure=0 |
| Deployments | API 1/1, Collector 1/1, CoreDNS 2/2, Metrics Server 2/2 |
| API | Request 200m/256Mi, limit 750m/512Mi; ready=1, reinícios=0 |
| Collector | Request 100m/128Mi, limit 500m/256Mi; ready=1, reinícios=0 |
| Cadência | Duas amostras por minuto em cada um dos sete minutos completos entre 01:36 e 01:43 UTC, série k8s.node.cpu.usage |

Requests/limits, réplicas, reinícios e fases correspondem aos objetos consultados diretamente no cluster. Uptime foi consultado como valor cumulativo, aproximadamente 7252 segundos na amostra. Memória disponível e working set mantêm suas definições nativas; não somar esses valores à memória alocável como se fossem capacidade física.

Não apareceu atribuição a `mecanica-api` na amostra da métrica de CPU do nó. O inventário observado não contém métricas extras de rede/volumes. Uma réplica e a cadência observada não indicaram duplicação; isso não é garantia de entrega única em qualquer falha ou alteração de topologia.

## Continuidade e enriquecimento da API

Chamadas controladas produziram 200 no login, 400 para perfil inválido, 401 sem token e 404 para uma OS inexistente. Sem alteração de dados. Quatro cenários foram registrados em http-validation.json; duas chamadas preliminares de leitura/login também geraram traces. O token ficou apenas em memória e não integra as evidências.

Após o atraso de ingestão, dez spans de seis traces e o log tratado de 404 foram encontrados. Métricas técnicas, spans e log possuem a identidade Kubernetes esperada: namespace default, pod `deploy-gerenciamento-api-7f5fd85d54-6lfjq`, UID `15bf3de7-fe91-4efd-a902-9432785a81f9`, nó `ip-10-0-11-44.ec2.internal` e Deployment `deploy-gerenciamento-api`.

`service.name=mecanica-api` e `service.version=020e95d2ec5750d142cdc088b02620f678f82edf` preservados. O log e o trace do 404 compartilham `7193af9288124ad9a168033bbe5549a1`. A mensagem mantém operação fixa, tipo, status e correlação/modelo de rota; sem stack trace ou mensagem original de exceção.

Indicadores da janela anterior preservados: três OSs criadas, cinco amostras de etapa, uma de serviço e onze notificações. Esta conferência não refez o ciclo de negócio nem a inspeção SQL: [validação funcional anterior](VALIDACAO_HOM_2026-10-17.md).

## Conclusão e limites

Coleta e enriquecimento Kubernetes comprovados em **hom**, no escopo E5.4/E5.12. Prd, nós transitórios, carga/saturação e falhas de rede não foram exercitados. CloudWatch, correlação X-Ray/OTLP, dashboards e alertas continuam em entregas separadas; E5 permanece parcial.

Consultas realizadas pela sessão autenticada do New Relic, sem chave administrativa ou de ingestão no processo de validação. Evidências locais não versionadas em `artifacts/kubernetes-validation-20261018/`: validation.json, snapshots Kubernetes/RBAC/ConfigMap, métricas/top, logs do Collector/API e http-validation.json. Nenhum script operacional ou suíte de telemetria acrescentado.
