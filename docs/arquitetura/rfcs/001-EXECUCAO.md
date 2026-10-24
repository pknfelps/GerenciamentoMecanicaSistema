# RFC 001 — Execução, identidade e integração

**Estado:** implementada; contratos de execução e limites descritos abaixo.

## Problema e solução

A REST API Regional encaminha `POST /customers/auth/request-code` e `POST /customers/auth/verify-code` à versão numérica da Lambda .NET 10. As rotas da API seguem HTTP proxy → VPC Link V2 → NLB interno TCP 80 → target de instâncias em 30080 → Service NodePort → API em 8080. Probes, Swagger e OpenAPI não são publicados pelo Gateway. Sua projeção também normaliza nomes de parâmetros de caminho para o importador REST, preservando o mapeamento para o backend.

API e Lambda compartilham o RDS PostgreSQL 16.15/db.t3.micro Single-AZ do ambiente, com credenciais diferentes, rede privada e TLS VerifyFull. A função usa SAM/ZIP; rede/Gateway/banco usam Terraform. Não há authorizer JWT no Gateway: a API autentica e autoriza cada operação protegida.

## Identidade e permissões

O [contrato HTTP/JWT](../ACESSO_E_AUTENTICACAO.md) é a fonte das rotas, claims e respostas. Admin cadastra/consulta/remove usuários internos; Admin e Mechanic alteram apenas a própria conta por UUID do JWT. Mechanic opera cadastros/OS. Customer é identidade de customers: consulta status e decide orçamento somente das próprias OSs. Admin não herda permissões operacionais.

API referencia o projeto Auth.Contracts; autenticação consome seu nupkg 1.0.0 exato com checksum. O pacote compartilha CPF/CNPJ numérico e assinatura JWT, sem entidades de OS ou acesso ao banco. Ambos emitem HS256, UUID/nome/perfil e prazo de dez minutos; a validação da API usa ClockSkew zero. Não há refresh token ou revogação centralizada. Customer Inactive é bloqueado por consulta de cadastro nas duas operações próprias, mesmo com JWT ainda válido. Operações próprias ainda exigem existência/vínculo dos cadastros; recriação de nome não transfere identidade.

## Bootstrap, consulta e falhas

API inicializa configuração SSM/Secrets e conexão antes de servir. Lambda inicializa autenticação na primeira chamada de corpo válido e reutiliza somente runtime saudável; parsing inválido retorna 400 antes desse bootstrap. Documento inválido não consulta o banco; pedido de cadastro ausente/Inactive retorna 202 genérico sem escrita/envio; confirmação não autorizada retorna 401; falha técnica/timeout tratado retorna 500, sem transformar infraestrutura em inelegibilidade.

Lambda consulta somente id/name/document/status/email de customers por documento parametrizado, usa mecanica_auth/Pooling=false e DML somente nas duas tabelas de desafios/limites. Persiste Pending antes do Invoke, ativa Ready após captura e só emite JWT após consumo/commit. Não consulta OS. Cada chamada fecha sua conexão inclusive em erro. API usa mecanica_api e suas transações existentes; suas credenciais não são compartilhadas com o Job administrativo.

Ver [validação de CPF/CNPJ](../diagramas/VALIDACAO_CPF.md) e [abertura de OS](../diagramas/ABERTURA_OS.md). Criação/estoque/itens/histórico têm transação única; depois do commit o serviço aguarda o handler de notificação. O filtro HTTP registra sucesso após o retorno do serviço/controller. Falha tratada de notificação não desfaz a persistência; timeout após commit não garante idempotência de nova criação.

API/auth montam suas mensagens e invocam EmailFunction privada por SDK/IAM, em todos os modos. EmailFunction concentra MIME/MailKit e SMTP4dev privado/efêmero; sem relay externo, fila, outbox ou retry próprio. Captura não é entrega externa. Falha no envio do código impede confirmação útil; falha tratada de notificação após commit preserva a OS. Telemetria tem filas/orçamentos limitados e não participa da transação de negócio.

## Segurança, tracing e comprovação

Secrets são resolvidos em runtime por identidades restritas, sem valores em manifestos/pipeline. Logs usam mensagem fixa/tipo, sem stack trace, SQL, payload, documento ou credenciais. API/Lambda aceitam somente pai W3C; PostgreSQL/logs seguem a árvore da aplicação. X-Ray nativo/polling é diagnóstico independente, sem promessa de união automática ao OTLP.

Fundamentação: [ADR 001](../adrs/001-IDENTIDADE.md), [ADR 002](../adrs/002-REDE-AWS.md), [ADR 006](../adrs/006-NOTIFICACOES.md), [ADR 009](../adrs/009-SAM-AUTENTICACAO.md) e [ADR 010](../adrs/010-GATEWAY-REST-V2.md).

Decisão de autenticação e envio: [ADR 011](../adrs/011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md). Configuração hom/prd não comprova disponibilidade atual ou união X-Ray/OTLP.
