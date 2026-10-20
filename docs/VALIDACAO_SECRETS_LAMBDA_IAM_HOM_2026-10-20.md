# E5 — Falha Secrets Manager da Lambda comprovada por IAM — 20/10/2026

**Resultado: aprovado no cenário de acesso negado.** Após autorização explícita do mantenedor, a invocação direta de `$LATEST` registrou 500/technical-failure ao receber AccessDenied do Secrets Manager. O New Relic recebeu um incremento do acesso falho, o log correlacionado e o span com erro sanitizado. A role original foi restaurada, a recuperação registrou 400/invalid-document, e a role temporária foi removida com ausência confirmada. Gateway e versão 10 foram preservados.

Essa comprovação substitui a pendência de falta de um resultado real `secretsmanager/get_secret_value/bootstrap/failure` na Lambda. As [tentativas anteriores com proxy](VALIDACAO_SECRETS_LAMBDA_HOM_2026-10-20.md) continuam registradas como inconclusivas; não foram reinterpretadas como sucesso.

## Ambiente e autorização

- AWS `121754142617`, região `us-east-1`, hom; New Relic US `8626152`.
- Função `mecanica-hom-auth`, revisão `0446a19b4888921f1d9b4032d034de7d4009ab24`, mesmo ZIP/código de v10; dotnet10, 512 MB, timeout 15 s e layer Collector 0.22.0 preservados.
- Gateway `v5qvpym6i7`, integração POST /customers/validate referenciando explicitamente a versão numérica 10, conferida antes/depois.
- Janela de consulta das métricas: **19:50–19:55 UTC**, ou **16:50–16:55 em São Paulo**. A falha ocorreu às 19:51:50 UTC e a recuperação às 19:51:59 UTC.
- O mantenedor aprovou criar uma role temporária, associá-la somente a `$LATEST`, testar a negativa, restaurar e remover os recursos próprios. Não foram alterados valores de Secrets, SSM, código, alias, Gateway, Collectors ou a policy da role persistente.

## Permissões e isolamento

A role temporária `mecanica-hom-e5-secrets-7a3eae`, caminho `/mecanica/validation/`, usou confiança no serviço Lambda e as permissões necessárias já presentes no runtime: nove parâmetros SSM exatos, License Key específica, logs do grupo da função, X-Ray regional e gerenciamento de interfaces com as mesmas restrições existentes. Foi preservada a negação de operações de interface ao código da função.

A leitura dos Secrets da aplicação foi retirada da policy temporária e foi acrescentado **Deny de GetSecretValue somente no ARN exato do JWT**. A leitura da License Key pelo Collector continuou permitida. Nenhum privilégio foi acrescentado à role persistente ou ao usuário da aplicação.

IAM Access Analyzer: **zero findings**. As simulações individuais por recurso confirmaram JWT=explicitDeny, License Key=allowed e SSM=allowed. A primeira simulação em lote teve resultado agregado por ação; os testes individuais foram usados como evidência, sem inferir autorização de um recurso pelo resultado de outro.

Código e ambiente de `$LATEST` foram comparados à versão 10 antes da troca. A alteração de Role usou RevisionId atual; a restauração executou em finally, aguardando Active/Successful. A role publicada de v10 permaneceu `mecanica-hom-auth-runtime`. O request direto continha somente document=invalid: o bootstrap precisava ocorrer antes da validação de domínio, sem emissão de JWT ou consulta PostgreSQL para esse documento.

## Resultado funcional e sinais

| Conferência | Falha com role temporária | Recuperação com role original |
|---|---|---|
| Status registrado pelo handler | **500** | **400** |
| Categoria | technical-failure | invalid-document |
| Duração registrada, antes do flush | 1.598 ms | 1.882 ms |
| Request ID Lambda | `a21f7678-239d-4129-8571-67de84dc5418` | `660c7044-08bb-42e1-9068-a9d913c91591` |
| Marcador de validação | `e5-secrets-iam-08a8e65e-fc44-45c8-a461-de1b96049603` | `e5-secrets-iam-recovery-a2a843d2-f6c1-45c9-aa8f-1d754d5a8741` |
| Trace OTLP | `6ad7c6536d45e0d01e25df866eb42de9` | `6ad7c65b5eccbb704311dcb45b4ee6f4` |
| Span da invocação | `bbf54397d4d19e85` | `a036e345b67cdac4` |

Os dois registros foram encontrados no CloudWatch e no New Relic, com status/categoria e IDs correspondentes. A interpretação do 500 utiliza o statusCode da resposta registrado pelo handler, não o HTTP 200 de aceitação da API AWS Invoke.

No contador `mecanica.integrations.operations`, serviço `mecanica-auth`, hom, purpose=bootstrap, janela 19:50–19:55 UTC:

| Integração / operação | Resultado | Quantidade |
|---|---|---:|
| SSM / get_parameters | success | **2** |
| Secrets Manager / get_secret_value | failure | **1** |
| Secrets Manager / get_secret_value | success | **2** |

São cinco acessos técnicos: uma leitura SSM em cada inicialização; a primeira parou na leitura negada do JWT, e a recuperação conseguiu ler JWT e credenciais do banco. A leitura da License Key pela extensão não integra esse contador da aplicação. A primeira invocação de falha ocorreu num ambiente distinto do ambiente recuperado, como confirmado pelos streams CloudWatch diferentes; não foi necessário repetir o handler naquela instância para receber seus sinais no New Relic.

O span de falha tem nome fixo POST /customers/validate, status ERROR e descrição **“Falha durante a autenticação.”**. Nos dois spans consultados não apareceram os atributos inspecionados de mensagem original/stack trace, SQL, string de conexão ou URL completa. A conclusão limita-se aos atributos e registros consultados; não é auditoria de todos os logs nativos AWS nem de todas as variantes de erro.

## Confirmação nativa pelo CloudTrail

Dois eventos GetSecretValue da mesma role temporária demonstram o isolamento:

| UTC | Resultado | Evento |
|---|---|---|
| 19:51:48 | Leitura da License Key bem-sucedida | `f4173219-c705-4d5a-92f6-69a6a6b7b552` |
| 19:51:50 | **AccessDenied** no JWT | `99ab2762-bb5c-49b9-8d42-cef4f09391d6` |

O evento negado não disponibilizava requestParameters. A associação ao JWT foi confirmada pelo ARN em resources e na mensagem de erro, sem reproduzir a mensagem original no relatório. O evento permitido identificou o Secret de observabilidade. Não foi recuperado ou exposto valor de Secret pelo agente; os acessos ocorreram nos clientes nativos da função/extensão.

## Restauração e limpeza

Conferência independente às **19:52:48 UTC** confirmou:

- `$LATEST` Active/Successful, role original restaurada e código/ambiente idênticos a v10.
- Policy `auth-runtime-access` original exatamente igual ao baseline.
- DeleteRolePolicy e DeleteRole da identidade temporária concluídos; ListRoles em `/mecanica/validation/` sem a role e sem outras roles restantes nesse caminho.
- Integração do Gateway ainda em `:10/invocations`, reconferida posteriormente.

O script de operação teve um erro local de tratamento de exceção **depois de restaurar e remover a role**: o ambiente da ferramenta não disponibilizava ClientError na checagem esperada de NoSuchEntity. A sequência nativa de restauração/exclusão consta dos registros das chamadas e foi reconferida por outro script, usando ListRoles, GetFunctionConfiguration e GetRolePolicy. Isso não foi usado como comprovação de falha da aplicação; os resultados de negócio vieram dos logs e métricas reais. Nenhuma role ou alteração temporária permaneceu.

## Alcance e acompanhamento

**Comprovado:** falha por autorização do Secrets Manager na Lambda, contador único, 500 tratado, Collector com sua credencial acessível, sinais correlacionados, recuperação e restauração/limpeza. Não comprova timeout ou indisponibilidade de rede desse serviço, falha no segundo Secret da aplicação, cancelamento, acesso PostgreSQL nesse request ou operação em prd.

E5.8 continua parcial pelos cancelamentos e falhas de abertura/transações ainda não exercitados, mas **Secrets Manager failure da Lambda deixou de ser pendência de ausência de comprovação**. A correção do console do bootstrap da API permanece anotada para depois de finalizar as validações. Disparos CPU/memória e carga na API continuam dispensados pelo mantenedor.

Evidências técnicas locais, ignoradas pelo Git: `artifacts/e5-validacao-integrada/20261020-lambda-secrets-iam/`, com policy/trust/proposta revisadas e `validation-summary.json`. Sem senhas, JWTs, License Key ou conteúdo dos Secrets.
