# RFC 001 — Execução, identidade e integração

**Estado:** implementada; contratos de execução e limites descritos abaixo.

## Problema e solução

A REST API Regional encaminha `/Authentication`, `/Users` e as duas rotas de autenticação de cliente à versão numérica da Lambda .NET 10. As rotas da API seguem HTTP proxy → VPC Link V2 → NLB interno TCP 80 → target de instâncias em 30080 → Service NodePort → API em 8080. Probes, Swagger e OpenAPI não são publicados pelo Gateway. Sua projeção também normaliza nomes de parâmetros de caminho para o importador REST, preservando o mapeamento para o backend.

API e Lambda compartilham o RDS PostgreSQL 16.15/db.t3.micro Single-AZ do ambiente, com databases e credenciais próprios, rede privada e TLS VerifyFull. A função usa SAM/ZIP; rede/Gateway/banco usam Terraform. Cada backend valida JWT e autoriza suas operações protegidas.

## Identidade e permissões

O [contrato HTTP/JWT](../ACESSO_E_AUTENTICACAO.md) é a fonte das rotas, claims e respostas. Admin cadastra/consulta/remove usuários internos; Admin e Mechanic alteram apenas a própria conta por UUID do JWT. Mechanic opera cadastros/OS. Customer usa a cópia de acesso mantida pela AuthFunction: consulta status e decide orçamento somente das próprias OSs. Admin não herda permissões operacionais.

A API publica o Auth.Contracts. A AuthFunction consome o nupkg 1.0.0 com checksum e emite JWT HS256 com UUID, nome, perfil e prazo de dez minutos. A API valida com ClockSkew zero e verifica cadastro ativo e vínculo nas operações próprias da OS.

## Bootstrap, consulta e falhas

API inicializa configuração SSM/Secrets e conexão antes de servir. Lambda inicializa autenticação na primeira chamada de corpo válido e reutiliza somente runtime saudável; parsing inválido retorna 400 antes desse bootstrap. Documento inválido não consulta o banco; pedido de cadastro ausente/Inactive retorna 202 genérico sem escrita/envio; confirmação não autorizada retorna 401; falha técnica/timeout tratado retorna 500, sem transformar infraestrutura em inelegibilidade.

A AuthFunction usa mecanica_auth e consulta customer_access por documento. Persiste Pending antes do Invoke, ativa Ready após captura e só emite JWT após consumo e commit. O CRUD de contas internas usa users no mesmo database de identidade. A API preserva suas transações operacionais em mecanica e sincroniza o cadastro de acesso pela invocação privada descrita no contrato.

Ver [validação de CPF/CNPJ](../diagramas/VALIDACAO_CPF.md) e [abertura de OS](../diagramas/ABERTURA_OS.md). Criação/estoque/itens/histórico têm transação única; depois do commit o serviço aguarda o handler de notificação. O filtro HTTP registra sucesso após o retorno do serviço/controller. Falha tratada de notificação não desfaz a persistência; timeout após commit não garante idempotência de nova criação.

API/auth montam suas mensagens e invocam EmailFunction privada por SDK/IAM, em todos os modos. EmailFunction concentra MIME/MailKit e SMTP4dev privado/efêmero; sem relay externo, fila, outbox ou retry próprio. Captura não é entrega externa. Falha no envio do código impede confirmação útil; falha tratada de notificação após commit preserva a OS. Telemetria tem filas/orçamentos limitados e não participa da transação de negócio.

## Segurança, tracing e comprovação

Secrets são resolvidos em runtime por identidades restritas, sem valores em manifestos/pipeline. Logs usam mensagem fixa/tipo, sem stack trace, SQL, payload, documento ou credenciais. API/Lambda aceitam somente pai W3C; PostgreSQL/logs seguem a árvore da aplicação. X-Ray nativo/polling é diagnóstico independente, sem promessa de união automática ao OTLP.

Fundamentação: [ADR 001](../adrs/001-IDENTIDADE.md), [ADR 002](../adrs/002-REDE-AWS.md), [ADR 006](../adrs/006-NOTIFICACOES.md), [ADR 009](../adrs/009-SAM-AUTENTICACAO.md) e [ADR 010](../adrs/010-GATEWAY-REST-V2.md).

Decisão de autenticação e envio: [ADR 011](../adrs/011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md). Em 02/11/2026, foram concluídas 48 verificações HTTP por ambiente em hom e prd, sem divergência dos resultados esperados ou respostas 5xx: login/perfis, código de acesso, status, aprovação/recusa, execução e entrega. As probes privadas responderam Healthy e o SMTP4dev capturou as mensagens nos dois ambientes. [Alcance da observabilidade e limites](003-DADOS-E-OBSERVABILIDADE.md#validação-em-hom-e-prd). Configuração ou conferência pontual não garantem disponibilidade futura ou união X-Ray/OTLP.
