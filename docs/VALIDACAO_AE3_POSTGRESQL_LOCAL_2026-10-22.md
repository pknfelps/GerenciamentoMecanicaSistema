# AE.3 — Conferência PostgreSQL local

**22/10/2026 — validação concluída no escopo implementado, sem acesso AWS.** Complementa o [plano extra](PLANO_EXTRA_AUTENTICACAO_E_EMAIL.md). Não altera o plano oficial E5/E6/E7. Não existe migração: o esquema é criado pelo Init.sql em banco vazio, e regras de negócio permanecem no C#.

## Inicialização e permissões

PostgreSQL 16.15 descartável, sem portas publicadas, rede externa ou volume persistente. Executado o conteúdo atual do Job database-init, extraído do manifesto sem alteração de sua lógica, com os três SQLs reais do repositório de banco e credenciais exclusivamente fictícias. Usou conexão local; esta conferência não certifica TLS/RDS/EKS.

- Banco vazio: doze tabelas, seeds e marcador 1.0.0/hash criados com sucesso.
- Segunda execução: reconheceu o marcador, não reaplicou Init.sql e executou os provisionamentos de usuários/grants.
- Hash antigo: inicialização recusada; marcador, cadastro e tabelas de autenticação preservados.
- Banco não vazio sem marcador: inicialização recusada, preservando sua tabela e registro de teste.
- Dez verificações de estrutura/seeds/instantes/índices: somente PKs estruturais nas duas tabelas novas; três índices comuns; sem CHECK, FK, trigger, cascata ou unicidade condicional de negócio. SQL direto aceitou valores fora das regras para demonstrar que a responsabilidade permanece no C#; dados revertidos por rollback.
- Matriz dos grants atuais conferida após executar o Job: SELECT das cinco colunas de customers e DML nas duas tabelas novas somente para mecanica_auth; sem acesso a phone, OSs, usuários internos, marcador ou administração. API sem acesso às tabelas novas. [Conferência detalhada de permissões](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/docs/AE3_PERMISSOES_AUTENTICACAO_2026-10-22.md).
- Hash LF do Init.sql: `37ac7532b72288a21d9a52b03ab0113b85d343afd3856786f36ae0db3a2b0068`.

Container removido ao final. Evidências locais ignoradas em artifacts/ae3/validation; não foram versionados scripts operacionais ou criado mecanismo de migração.

## Componentes C# e regressão

Restore e build Release da solução Serverless aprovados, zero avisos/erros. Nova execução de todos os testes existentes: **309 aprovados, zero falhas ou ignorados**.

| Projeto | Aprovados |
|---|---:|
| InfrastructureTests | 194 |
| FunctionTests | 73 |
| ApplicationTests | 32 |
| ContractTests | 10 |

EmailFunctionTests continua sem casos; não representa cobertura de envio. A compilação usa o SDK 10.0.402 exigido. Os testes usam VSTest 18.6/runtime .NET 10.0.9 completos já instalados; o runtime temporário 10.0.13 do SDK está incompleto (System.Net.Mail ausente). Não houve alteração de dependências para contornar isso.

Entre os testes, 29 cenários PostgreSQL e 11 de regras C# cobrem cliente inexistente sem gravações, elegibilidade, cooldown/limite por cliente, Pending/Ready, substituição, expiração, cinco tentativas persistidas, falha de envio simulada na persistência, rollback por falha de INSERT/assinatura/cancelamento, preservação da exceção original, concorrência de solicitações/confirmações e fechamento das conexões. [Componentes e nomes dos testes](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE3_OPERACOES_TRANSACIONAIS.md).

As fixtures do store usam seu esquema local mínimo; a conferência dos arquivos SQL e grants reais foi executada separadamente, conforme acima. Não apresentar esses testes como validação do novo fluxo HTTP ou de SMTP. Relatório TRX em artifacts/ae3-validation/results/ae3-validation.trx no Serverless, fora do Git.

## Pendências preservadas

**Estado na conferência anterior:** limpeza oportunista ainda não implementada. Posteriormente, o mantenedor dispensou as 24 horas extras e a [limpeza foi implementada e validada](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/AE3_LIMPEZA_EXPIRADOS.md): até 100 desafios expirados e 100 limites com janela/cooldown encerrados por solicitação admitida. Nova regressão completa de 328 testes aprovada; AE.3 concluída no escopo local. Os resultados da rodada anterior acima permanecem históricos.

Ligação dos componentes ao novo HTTP/envio continua na AE.6. Nenhuma migração, regra SQL de negócio, código de produção, workflow, infraestrutura ou recurso AWS foi alterado nesta conferência.
