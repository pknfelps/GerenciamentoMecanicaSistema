# RFC 001 — Execução, identidade e integração

**Estado:** arquitetura aceita; implementação pendente. **Registro:** 2026-09-16. [Índice](../README.md)

## Problema e solução

A fase exige uma entrada gerenciada e validação serverless de CPF sem transferir o gerenciamento de OS para o cliente. A REST API Regional do API Gateway encaminha `/customers/validate` à Lambda .NET 10 em ZIP e as rotas da aplicação ao EKS por VPC Link/NLB interno. O banco compartilhado entre API e função é o Aurora PostgreSQL Serverless v2 do respectivo ambiente.

A função consulta diretamente clientes com credencial restrita à leitura necessária, conexão sem pooling e encerramento garantido. Ela normaliza o CPF para a representação atual do banco, verifica existência/estado e emite JWT. A API continua responsável por validar JWT, role e propriedade da OS; não haverá um segundo mecanismo de autorização no Gateway nesta arquitetura.

## Contratos e limites

O [contrato de acesso](../ACESSO_E_AUTENTICACAO.md) é a fonte dos formatos e códigos HTTP. Admin cadastra/remove usuários internos; Mechanic opera a oficina; ambos atualizam apenas o próprio registro. Customer não é usuário da tabela interna e só acessa status/decisão de orçamento das próprias OS. Abertura permanece exclusivamente na oficina.

As regras comuns de CPF/JWT serão extraídas para `GerenciamentoMecanica.Auth.Contracts`. O pacote não contém controllers, entidades de OS ou acesso ao banco. Não haverá refresh token, lista de revogação nem invalidação adicional de sessões por Update/Remove/inativação. Cadastro de cliente nasce Ativo; a fase não introduz um endpoint de inativação.

Segredos JWT e credenciais ficam no Secrets Manager por ambiente. API e função usam configurações JWT compatíveis dentro do mesmo ambiente. A integração REST será composta a partir dos contratos OpenAPI versionados dos dois produtores, com extensões AWS na infraestrutura.

## Fluxos e falhas

Ver [componentes](../diagramas/COMPONENTES.md), [validação de CPF](../diagramas/VALIDACAO_CPF.md) e [abertura de OS](../diagramas/ABERTURA_OS.md). Falha técnica de banco/secrets não equivale a CPF inelegível. Falha de telemetria não deve impedir uma operação de negócio. SMTP permanece na API, com falhas posteriores ao commit tratadas separadamente.

## Implementação e aceitação

A implementação abrange função/Gateway, permissões e ajustes da API, seguida da comprovação integrada. Demonstrar token da função aceito pela API, rejeição de acesso cruzado entre clientes/ambientes, permissões de usuários e criação exclusiva por Mechanic. Protocolos da integração, versões e exposição de documentação serão concretizados durante a implementação.

**Fundamentação:** [ADR 001](../adrs/001-IDENTIDADE.md), [ADR 002](../adrs/002-REDE-AWS.md) e [ADR 006](../adrs/006-NOTIFICACOES.md).
