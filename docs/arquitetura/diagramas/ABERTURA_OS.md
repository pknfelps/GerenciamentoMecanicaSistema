# Login interno e abertura de OS

[Arquitetura](../README.md) · [Permissões e contratos](../ACESSO_E_AUTENTICACAO.md) · [Dados e métricas](../rfcs/003-DADOS-E-OBSERVABILIDADE.md)

Fluxo alvo. Somente Mechanic abre OS. Admin gerencia usuários; Customer consulta status e decide o próprio orçamento.

```mermaid
sequenceDiagram
    actor M as Mecânico
    participant G as API Gateway
    participant A as API via VPC Link e NLB
    participant D as RDS PostgreSQL
    participant O as OpenTelemetry
    participant N as Handler de notificação na API
    participant S as SMTP (somente local)
    participant L as Log textual da API
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
                A->>D: BEGIN; resolver itens; bloquear materiais por UUID e reservar estoque
                A->>D: Inserir OS Received, itens e histórico inicial UTC
                alt Falha ao persistir
                    A->>D: ROLLBACK
                    A-->>G: Erro conforme contrato da rota
                else Persistência bem-sucedida
                    A->>D: COMMIT
                    A->>O: Registrar criação confirmada (sem bloquear negócio)
                    A->>N: Publicar evento de status, após commit
                    alt Runtime hom/prd
                        N->>L: Envio de e-mail simulado.
                    else Runtime ausente (mesmo fluxo local sem Gateway)
                        N->>S: Tentar enviar notificação ao cliente
                        S-->>N: Resultado ou falha
                    end
                    N-->>A: Concluir tratamento (falha de notificação não desfaz OS)
                    A-->>G: 201 com ID da OS
                end
            end
        end
        G-->>M: Resposta da aplicação
    end
```

A criação/itens/estoque e o histórico inicial são gravados na mesma transação. Nas alterações e exclusões, a API bloqueia a OS antes de carregar e validar o estado atual; materiais são bloqueados em ordem consistente de UUIDs. Assim, uma decisão concorrente não usa um estado anterior nem repete efeitos de estoque. Status/evento e exclusão dos itens/OS são atômicos, preservando o histórico após DELETE.

O [diagrama de dados](DADOS.md) registra as relações e projeções: conclusão/duração ausentes são null, não data mínima/zero artificial. As mudanças de consistência e contrato foram conferidas em hom em 14/10/2026; a sanitização dos logs e a simulação de e-mail foram validadas localmente e aguardam conferência da nova revisão na AWS. Os participantes OpenTelemetry continuam sendo o alvo da E5: instrumentação, exportação e reconciliação dos indicadores ainda precisam ser implementadas. Não considerar que todo o diagrama já está operacional.

O tratamento permanece no processamento da API, após o commit, como em `OrdersService` e `OrderNotificationEventHandler`. O handler continua consultando os dados necessários e EmailService monta a mensagem; IEmailSender define o destino do envio. Em hom/prd, LoggingEmailSender conclui e registra apenas `Envio de e-mail simulado.`, sem dados do envio ou entrega real. No modo local, MailKitEmailSender mantém SMTP, configurações e smtp4dev do Compose. A escolha usa o modo AWS já validado, não a máquina nem ASPNETCORE_ENVIRONMENT. Não há fila nem função de notificação nesta entrega. Falha de notificação não pode recriar a OS ou transformar a persistência confirmada em falha de criação. Um timeout percebido pelo consumidor depois do commit não oferece garantia de idempotência de uma nova requisição. A simulação está preparada para conferência após o deploy; SMTP funcional na AWS deixou de fazer parte da entrega por decisão do mantenedor.
