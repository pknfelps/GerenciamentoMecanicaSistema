# Login interno e abertura de OS

[Arquitetura](../README.md) · [Permissões e contratos](../ACESSO_E_AUTENTICACAO.md) · [Dados e métricas](../rfcs/003-DADOS-E-OBSERVABILIDADE.md)

Fluxo alvo. Somente Mechanic abre OS. Admin gerencia usuários; Customer consulta status e decide o próprio orçamento.

```mermaid
sequenceDiagram
    actor M as Mecânico
    participant G as API Gateway
    participant A as API via VPC Link e NLB
    participant D as Aurora
    participant O as OpenTelemetry
    participant N as Handler de notificação na API
    participant S as SMTP
    M->>G: POST /authentication (credenciais internas)
    G->>A: Encaminhar login
    A->>D: Consultar usuário interno
    D-->>A: Registro e hash
    A->>A: Validar credenciais conforme contrato existente
    A-->>G: 200 com JWT ou erro de login
    G-->>M: Resposta do login
    opt Login válido e solicitação de abertura
        M->>G: POST /orders + Bearer JWT + dados
        G->>A: Encaminhar criação
        A->>A: Validar JWT e role Mechanic
        alt Acesso inválido
            A-->>G: Erro de autenticação/autorização
        else Acesso permitido
            A->>D: Consultar cliente/veículo e validar vínculo
            D-->>A: Dados para validação
            A->>A: Validar requisição e regras de negócio
            alt Dados inválidos ou dependência ausente
                A-->>G: Erro conforme contrato da rota
            else Dados válidos
                A->>D: BEGIN; resolver itens e reservar estoque
                A->>D: Inserir OS Received, itens e histórico inicial UTC
                alt Falha ao persistir
                    A->>D: ROLLBACK
                    A-->>G: Erro conforme contrato da rota
                else Persistência bem-sucedida
                    A->>D: COMMIT
                    A->>O: Registrar criação confirmada (sem bloquear negócio)
                    A->>N: Publicar evento de status, após commit
                    N->>S: Tentar enviar notificação ao cliente
                    S-->>N: Resultado ou falha
                    N->>O: Registrar tentativa/erro sem dados pessoais
                    N-->>A: Concluir tratamento (falha SMTP não desfaz OS)
                    A-->>G: 201 com ID da OS
                end
            end
        end
        G-->>M: Resposta da aplicação
    end
```

A transação existente de criação/itens/estoque será ampliada para incluir o histórico inicial. Novas transições também atualizarão estado e histórico atomicamente. Publicar telemetria depois do commit evita contar um rollback como criação; entrega e reconciliação dos indicadores ainda precisam ser implementadas em E4/E5.

O envio SMTP permanece no processamento da API, após o commit, como em `OrdersService` e `OrderNotificationEventHandler`. Não há fila nem função de notificação nesta entrega. Falha de SMTP não pode recriar a OS ou transformar a persistência confirmada em falha de criação. Um timeout percebido pelo consumidor depois do commit não oferece garantia de idempotência de uma nova requisição.
