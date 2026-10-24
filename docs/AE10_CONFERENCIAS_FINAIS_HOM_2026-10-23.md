# AE.10 — Conferências finais de autenticação em hom

**23/10/2026 — concluídas.** Complementa a [validação integrada](AE10_VALIDACAO_HOM_2026-10-23.md) e fecha as três ressalvas restantes de hom: conferência SQL dos desafios, inativação e limite horário. Não certifica prd, entrega externa de e-mail ou cenários de carga/concorrência.

Gateway: `https://c00xbwprka.execute-api.us-east-1.amazonaws.com/hom`. Conferências HTTP aproximadamente entre **16:27 e 16:39 de Brasília (19:27–19:39 UTC)**; restauração SQL confirmada às 16:40. Foram registrados 25 resultados HTTP com os códigos esperados. Códigos/JWT permaneceram em memória.

## Acesso e leitura SQL

A sessão administrativa AWS estava válida. Foi utilizado o pod `database-access` já versionado no repositório BancoDados, no namespace `database-init`, com `psql`, CA RDS e `sslmode=verify-full`. O resolvedor seguro de credenciais não conseguiu fornecer a senha; a credencial fornecida pelo mantenedor foi usada somente no prompt oculto do psql. Não foi criada senha em argumentos, arquivos ou Kubernetes Secrets. Nenhum valor foi recuperado diretamente do Secrets Manager.

A sessão confirmou **TLS 1.3**. As consultas de conferência usaram transações READ ONLY, statement timeout de 10 s, lock timeout de 3 s e ROLLBACK ao final. Somente os ensaios de inativação/restauração e limite vencido usaram escrita, limitada ao cliente fictício adicional. As evidências SQL contêm IDs técnicos, estados, datas, contagens e comprimentos criptográficos; não contêm documento, nome, destinatário, código, salt, verifier ou senha.

| Conferência | Resultado SQL/HTTP |
|---|---|
| Pedido admitido e envio capturado | Desafio Ready, zero tentativas, validade de cinco minutos |
| Código incorreto | 401; desafio continuou Ready com failed_attempts=1 persistido |
| Código correto em seguida | 200; Consumed, consumed_at preenchido e failed_attempts=1 preservado |
| Reuso após consumo | 401, sem segundo consumo |
| Cinco códigos incorretos em novo desafio | Cinco respostas 401; SQL Invalidated, failed_attempts=5, invalidated_at preenchido e consumed_at ausente |
| Código correto após a quinta negativa | 401 |
| Proteção do código | Somente colunas code_salt/code_verifier, comprimentos observados de 16/32 bytes; valores não exportados |
| Grants mecanica_auth | DML nas duas tabelas auth e SELECT de customers.email; sem SELECT de phone/OS, sem UPDATE de customers e sem atributos administrativos |

## Cadastro inexistente e limpeza de desafios

O CNPJ fictício existente nos testes locais, usado para a negativa, foi confirmado ausente em customers. Dois pedidos retornaram 202 com challengeIds distintos e nenhuma mensagem adicional no SMTP4dev. Contagens e fingerprints dos campos de estado das duas tabelas auth permaneceram idênticos antes/depois; nenhuma linha persistida foi acrescentada ou modificada nessa janela.

Uma solicitação admitida posterior removeu automaticamente os dois desafios expirados identificados no baseline, inclusive um que já tinha sido consumido. A leitura seguinte confirmou ausência desses IDs, zero desafios vencidos naquele instante e o novo desafio Ready. Não houve exclusão SQL manual desses desafios nem retenção artificial de 24 horas.

## Cinco solicitações por hora

A conferência continuou a janela real já iniciada na validação anterior; não foram ajustados seus timestamps ou contagem para demonstrar o limite:

- SQL confirmou request_count=5, janela **18:32:11.717130–19:32:11.717130 UTC** e última admissão às 19:28:51.761981 UTC.
- A sexta tentativa ocorreu depois de 60 segundos desde a última admissão e retornou **429/Retry-After=100**, aguardando o fim da janela, em vez de somente outro cooldown.
- Após o término real dessa janela, um pedido retornou 202 e o SQL confirmou nova janela de uma hora com request_count=1.

A contagem representa solicitações admitidas, incluindo a admissão anterior cujo envio encontrou SMTP indisponível. Não equivale a cinco autenticações bem-sucedidas ou cinco e-mails entregues. O ensaio separado de limpeza abaixo ocorreu depois desta comprovação e não foi usado para demonstrar a passagem real de uma hora.

## Cliente inativo antes e depois do JWT

Foi usado somente o cliente adicional `0c492f0d-15cd-442c-939a-84dd5d0aa1cf`; o cadastro seed foi preservado.

1. Com Active, foi emitido JWT legítimo e preparado outro desafio Ready. O mesmo JWT recebeu 404 na OS do seed, conforme a proteção de titularidade.
2. O cliente adicional foi temporariamente marcado Inactive por SQL, com guarda de UUID/cadastro e exigência de exatamente uma linha alterada.
3. Novo request-code retornou **202 genérico**, sem mensagem e com contagens/fingerprints auth idênticos antes/depois.
4. O JWT já emitido recebeu **403** tanto na consulta de status quanto na decisão de orçamento.
5. Confirmar o desafio que estava Ready retornou **401**; o SQL confirmou sua invalidação, sem consumo.
6. Restaurado Active, o mesmo JWT voltou a receber **404** na OS alheia. O ensaio não concedeu acesso a essa OS nem alterou seu estado.

O resultado distingue a verificação de Active da titularidade: 403 durante a inativação, 404 para a OS de outro titular após restaurar o cadastro. O cliente adicional e o seed foram conferidos Active ao final.

## Limpeza de limites vencidos e restauração

Após autorização específica, o registro de limite do cliente adicional foi copiado para tabela temporária da sessão e recebeu timestamps vencidos consistentes com as regras C#. O relógio, as regras da aplicação e o registro do seed não foram alterados para preparar esse cenário.

Uma solicitação normal do seed retornou **202** e disparou a limpeza da aplicação. O SQL confirmou que o registro vencido do cliente adicional tinha sido removido. O registro original foi então restaurado, com **todos os campos iguais ao backup**: contagem 2, início 19:33:52.760267 UTC, fim 20:33:52.760267 UTC e última admissão 19:37:40.538943 UTC.

A primeira transação de restauração foi revertida por erro de sintaxe no bloco ad hoc de comparação. A restauração foi concluída usando o backup técnico registrado antes do ensaio, com conferência explícita de cada campo; limitRestoredExactly=true no resultado final. Isso foi um erro da consulta de validação, sem alteração de código da aplicação. A tabela temporária foi removida. O seed conservou somente a admissão normal realizada para disparar a limpeza.

## Encerramento e evidências

Logs de autenticação da rodada: **205 eventos inspecionados**, zero correspondências com códigos/JWT/documentos/e-mail fictícios usados no ensaio e zero correspondências com o padrão JWT. Conteúdo bruto não foi salvo.

API, Collector e SMTP4dev foram conferidos Running/Ready, com zero reinícios nos containers atuais. A sessão psql foi encerrada; pod de acesso e port-forward exclusivos da rodada foram removidos. Não foi criado arquivo pgpass. O ambiente permanece provisionado; não houve deploy, workflow remoto, alteração IAM, reinício de serviços ou destroy nesta rodada.

Evidências locais ignoradas pelo Git: `artifacts/ae10/remaining-validation-2026-10-23/`, com `http-results.json`, resumos `sql-*.json`, `auth-log-review.json` e `cleanup.json`. Nenhuma OS foi criada nesta rodada; permaneceram os quatro registros fictícios das conferências anteriores. Novos desafios/limites resultantes dos pedidos normais seguem o ciclo de vida e a limpeza da aplicação.

**AE.10 concluída no escopo de hom:** fluxo integrado, correções posteriores e as três conferências restantes comprovados. Concorrência/carga, prd, envio externo e robustez adicional permanecem fora deste aceite; não constituem novas etapas obrigatórias desta entrega.
