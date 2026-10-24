# Login interno e abertura de OS

[Arquitetura](../README.md) · [Permissões](../ACESSO_E_AUTENTICACAO.md) · [Modelo de dados](../BANCO_E_MODELO.md)

```mermaid
sequenceDiagram
    actor M as Mecânico
    participant G as API Gateway
    participant A as Controller da API
    participant S as OrdersService
    participant D as PostgreSQL
    participant N as Handler de notificação
    participant E as Adaptador Invoke
    participant L as EmailFunction
    participant P as SMTP4dev privado
    participant F as Filtro de métricas HTTP
    M->>G: POST /Authentication com name, password e role
    G->>A: HTTP proxy
    A->>D: Consultar credenciais internas
    D-->>A: Cadastro e hash
    A->>A: Validar senha e emitir JWT com UUID
    A-->>G: 200 com token ou erro de login
    G-->>M: Resposta
    opt Login válido e abertura solicitada
        M->>G: POST /Orders com cliente, veículo, itens e Bearer
        G->>A: HTTP proxy
        A->>A: Validar JWT, role Mechanic e request
        A->>S: Criar OS
        S->>D: Resolver cliente/veículo e verificar vínculo
        S->>D: BEGIN e resolver serviços
        S->>D: Bloquear materiais por UUID e reservar estoque
        S->>D: Inserir OS Received, evento inicial UTC e itens
        alt Falha antes do commit
            S->>D: ROLLBACK
            S-->>A: Propagar falha
            A-->>G: Erro conforme contrato
        else Persistência confirmada
            S->>D: COMMIT
            S->>N: Publicar evento de status e aguardar handler
            N->>D: Consultar dados necessários à notificação
            N->>E: Montar e enviar mensagem
            E->>L: Invoke v1 sincrono no alias email do ambiente
            L->>P: Construir MIME e capturar por SMTP
            L-->>E: Captured ou falha sanitizada
            E-->>N: Resultado da tentativa
            N->>N: Registrar sucesso/falha da tentativa
            Note over N,S: Falha tratada da notificação não desfaz a OS
            N-->>S: Tratamento concluído
            S-->>A: UUID da OS
            A-->>F: Resultado 201 com UUID
            F->>F: Contar criação após retorno bem-sucedido
            F-->>G: Preservar resposta 201
        end
        G-->>M: Resposta
    end
```

Ordem conferida em `OrdersService.CreateServiceOrder`, `OrderNotificationEventHandler` e `OrderBusinessMetricsFilter`: **commit → handler aguardado → retorno do serviço/controller → métrica de criação**. O filtro não cria a transação e não registra sucesso antes de ela terminar. Falha da instrumentação não substitui o resultado de negócio. O desenho não representa fila nem execução independente da requisição.

Cliente/veículo são resolvidos antes da transação de criação. Dentro dela, serviços são consultados e materiais bloqueados em ordem de UUID; reserva, OS, histórico inicial e itens têm confirmação ou rollback conjunto. Alterações posteriores bloqueiam a OS antes de reconstruir/validar o estado; exclusão remove itens/OS atomicamente, preservando histórico.

API usa LambdaEmailSender em todos os modos; somente EmailFunction tem MailKit/SMTP. A tentativa inclui consultas/montagem e pode falhar antes do Invoke. O envio é síncrono e limitado; falhas após commit são tratadas e contadas separadamente, sem desfazer a OS. mode=smtp identifica captura central, sem entrega externa. Pontos históricos simulated mantêm seu significado.

Não há outbox, replay ou garantia de entrega única. Uma interrupção após commit pode perder notificação/telemetria; repetir a criação após timeout não recebe garantia de idempotência. O leitor de durações usa instantes persistidos e não é consultado para contar criação.

O transporte serverless já foi preparado; processamento independente/assíncrono com fila/outbox permanece fora do escopo. [ADR 006](../adrs/006-NOTIFICACOES.md) e [ADR 011](../adrs/011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md).
