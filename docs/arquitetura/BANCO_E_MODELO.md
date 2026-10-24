# PostgreSQL, modelo relacional e histórico

[Arquitetura](README.md) · [ER completo](diagramas/DADOS.md) · [Decisão histórica](adrs/003-BANCO-E-HISTORICO.md) · [Escolha RDS](adrs/007-RDS-FREE-PLAN.md)

## Escolha implementada

PostgreSQL 16.15 no Amazon RDS, uma instância `db.t3.micro` Single-AZ com 20 GiB gp3 por ambiente, em rede privada, criptografia em repouso e TLS `VerifyFull`. Valores conferidos em `GerenciamentoMecanicaBancoDados/terraform/rds.tf`. RDS é provisionado; a parte serverless desta entrega é a Lambda de autenticação, não o banco.

O modelo já era relacional: clientes possuem veículos e OSs; OSs compõem serviços/materiais e alteram estoque. Confirmar OS, itens, reserva/consumo e histórico conjuntamente exige transações. PostgreSQL preserva SQL, chaves e código Npgsql/Dapper existentes; não foi necessário trocar a persistência para acrescentar entrada serverless e observabilidade.

| Alternativa | Decisão e limite |
|---|---|
| RDS PostgreSQL | Escolhido após a restrição de criação de Aurora na conta; mantém engine/código e banco gerenciado privado |
| Aurora Serverless v2 | Escolha original substituída em 02/10 após `FreeTierRestrictionError`; registro histórico desta conta, sem generalizar restrições de todas as contas atuais |
| PostgreSQL no EKS | Não adotado: adicionaria operação/persistência do banco ao cluster de nó único e ao ciclo de descarte da aplicação |
| Banco de documentos/chave-valor | Não adotado: exigiria remodelar relações/acessos transacionais existentes, sem necessidade demonstrada no escopo |

RDS provisionado não escala a zero. A economia escolhida é destruir/recriar o ambiente educacional nas janelas de uso, sem prometer auto-pause ou alta disponibilidade. Backup de um dia, `skip_final_snapshot=true`, sem proteção de exclusão e sem réplica refletem dados descartáveis; não são uma recomendação para produção com dados permanentes.

## Relações e responsabilidades

O [ER](diagramas/DADOS.md) inclui as doze tabelas do Init.sql revisado na AE.3, incluindo as duas tabelas de autenticação:

| Grupo | Relação e finalidade |
|---|---|
| `users` | Identidades internas Admin/Mechanic, UUID e hash de senha; sem FK com OS |
| `customers` → `vehicles` | Documento único do cliente referenciado pelo veículo |
| `customers`/`vehicles` → `orders` | OS referencia documento/placa; a API também valida que o veículo pertence ao cliente |
| `orders` → `order_services` → `catalog` | Itens com descrição, horas, preço e quantidade da OS |
| `orders` → `order_materials` → `stock` | Itens com nome, marca, preço e quantidade da OS |
| `order_status_history` | Linha temporal com UUID da OS, sequência, estados, UTC e motivo; sem FK para sobreviver ao DELETE |
| `schema_initialization` | Marcador operacional do Job; não é domínio nem mecanismo de migrações |
| `customer_auth_challenges`/`customer_auth_request_limits` | Desafios e limites por UUID do cliente existente; sem FK, cascata ou regras de negócio no SQL; operações transacionais e fluxo por código implementados em C# |

As referências por documento/placa foram preservadas. UUID identifica entidades nos contratos/JWT, mas nem todas as FKs físicas são UUID. Os itens não possuem PK/UNIQUE composto no SQL atual. O documento não acrescenta relações, índices, cascades ou constraints ausentes.

## Mudanças entregues

- `customers.status` obrigatório, default `Active`; domínio aceita Active/Inactive e PATCH cadastral preserva o estado. Não há endpoint de inativação.
- `users.role` separa Admin/Mechanic. Customer é emitido a partir de `customers`, sem usuário interno.
- Removidas datas/duração persistidas em `orders`; projeções usam o histórico, evitando fontes temporais divergentes.
- Histórico registra criação Received/sequence=1 e transições UTC; `ServiceCompleted` distingue conclusão de `BudgetRejected`.
- Itens conservam os valores usados pela OS; mudanças posteriores do cadastro não os reprecificam automaticamente.
- Esquema/seeds são aplicados em banco vazio pelo Job; sem reset no startup da API, migração incremental ou backfill.

## Transações, concorrência e retenção

Na criação, cliente/veículo são resolvidos e relacionados; dentro da transação, serviços são consultados, materiais bloqueados em ordem de UUID e estoque/OS/itens/evento inicial confirmados juntos. Nas mutações, a aplicação bloqueia a OS antes de ler/validar seu estado. Estado/evento são gravados na mesma transação; sequência desempata instantes iguais.

Sequências sem saltos, encadeamento, motivos e instantes não regressivos são validados pela aplicação. O SQL não implementa CHECK dessas regras, índice do histórico ou UNIQUE `(order_id, sequence)`, por decisão registrada. Essa proteção cobre os fluxos da API, não inserções SQL arbitrárias. Ausência de índice é um limite conhecido, não prova de desempenho em alto volume.

DELETE de OS remove itens e trata reservas/estoque conforme o estado, na transação; não remove eventos. Sem FK para a OS, o histórico continua correlacionável pelo UUID após a exclusão. A aplicação precisa impedir eventos inválidos nos seus fluxos. Destruir o banco inteiro encerra a disponibilidade desse histórico; não há backup lógico permanente garantido pela aplicação.

## Projeções e indicadores

`DateCreated` deriva do primeiro evento; `DateFinished` de Finished/ServiceCompleted; `Duration` é a diferença até essa conclusão. Recusa e intervalo aberto não viram zero/data mínima. Entrega não aumenta o tempo total do serviço. O repositório projeta datas/itens em SQL, sem uma consulta de histórico por OS na listagem.

Métricas HTTP de negócio são registradas após operação confirmada e podem ser perdidas. O leitor específico de durações consulta histórico, inclusive de OS excluída, sem depender do repositório que reconstrói o agregado atual. SQL é a referência por ocorrência/dia em `America/Sao_Paulo`; períodos OTLP/atrasos não oferecem reconciliação diária exata, replay ou entrega única. [RFC 003](rfcs/003-DADOS-E-OBSERVABILIDADE.md).

## Inicialização e credenciais

O Job grava 1.0.0/hash/horário na transação do esquema/seeds. Reexecução exige um único marcador compatível; banco não vazio sem marcador ou hash divergente falha. Grants são provisionados depois. Secrets temporários do Job são removidos ao final, inclusive em falha; Terraform usa ephemeral/write-only, sem senha em planos/estados.

`mecanica_api` possui SELECT/INSERT/UPDATE/DELETE nas nove tabelas de domínio, incluindo histórico; não é append-only. `mecanica_auth` possui SELECT somente em id/name/document/status/email de customers e DML nas duas tabelas de desafios/limites. Ambos ficam sem acesso ao marcador pelos grants. Scripts SQL são a fonte das permissões, sem garantias adicionais presumidas pelo desenho.

A configuração hom/prd não representa, por si, comprovação operacional de produção.

Referências: [RDS PostgreSQL](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_PostgreSQL.html), [SQL/esquema](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/docs/SCHEMA.md) e [grants da API](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados/blob/develop/sql/ProvisionApiUser.sql).
