# Validação de CPF/CNPJ e acesso à própria OS

[Arquitetura](../README.md) · [Contrato HTTP/JWT](../ACESSO_E_AUTENTICACAO.md)

Fluxo implementado e conferido em hom. A Lambda valida documento/cadastro, sem senha, e emite JWT Customer; não cria cliente, usuário interno ou OS. O ambiente foi posteriormente removido. [Validação pública](../../VALIDACAO_TRACING_W3C_HOM_2026-10-21.md).

```mermaid
sequenceDiagram
    actor C as Cliente
    participant G as API Gateway
    participant F as Lambda Validate
    participant P as Runtime reutilizável
    participant S as SSM e Secrets Manager
    participant D as RDS PostgreSQL
    participant A as API via VPC Link e NLB
    C->>G: POST /customers/validate com document
    G->>F: Proxy para versão numérica publicada
    F->>F: Validar Content-Type, JSON, propriedades e Base64
    alt Corpo inválido
        F-->>G: 400 antes de inicializar autenticação AWS
    else Corpo válido
        F->>P: Obter runtime ou inicializar uma vez
        opt Primeira inicialização válida ou nova tentativa após falha
            P->>S: Parâmetros públicos e Secrets AWSCURRENT
            S-->>P: JWT e credencial mecanica_auth
        end
        F->>F: Validar formato e dígitos CPF/CNPJ
        alt Documento inválido
            F-->>G: 400 sem consulta SQL
        else Documento válido
            F->>D: Abrir conexão sem pooling e consultar documento parametrizado
            D-->>F: Cadastro e status
            F->>F: Descartar conexão inclusive em erro
            alt Cliente ausente ou Inactive
                F-->>G: 401 genérico
            else Cliente Active
                F->>F: JWT HS256 com UUID do cliente e role Customer
                F-->>G: 200 com token como string JSON
            end
        end
    end
    Note over F,D: Falha técnica ou timeout tratado retorna 500, nunca 401
    G-->>C: Resposta com Cache-Control no-store
    opt Cliente consulta a própria OS
        C->>G: GET /Orders/{id}/status com Bearer
        G->>A: HTTP proxy
        A->>A: Validar JWT, perfil e UUID obrigatório
        A->>D: Resolver cliente pelo UUID e consultar vínculo da OS
        D-->>A: Estado ou ausência
        A-->>G: 200 ou 404 genérico para ausência/terceiro
        G-->>C: Resultado
    end
    opt Cliente aprova ou recusa o próprio orçamento
        C->>G: PATCH /Orders/{id}/budget com approved, documento e Bearer
        G->>A: HTTP proxy
        A->>A: Exigir Customer e resolver UUID
        A->>D: Conferir titular e bloquear OS na transação
        alt Cadastro ou vínculo ausente
            A-->>G: 404 genérico
        else Documento divergente ou estado incompatível
            A-->>G: 422 sem alterar OS
        else Decisão válida
            A->>D: Atualizar OS e histórico e devolver reserva na recusa
            A->>D: COMMIT
            A->>A: Tratar notificação e métricas após commit
            A-->>G: 204
        end
        G-->>C: Resultado
    end
```

O runtime é inicializado antes da validação do documento pelo caso de uso: **corpo inválido** não inicializa autenticação; **corpo válido com documento inválido** pode inicializá-la, mas não consulta PostgreSQL. A observabilidade é composta antes desse bootstrap e pode existir mesmo numa chamada rejeitada.

O handler limita o processamento ao tempo restante menos um segundo; configuração AWS tem prazo de oito segundos e conexão/comando têm limites próprios, sujeitos ao prazo total. O runtime só guarda inicialização bem-sucedida e pode tentar novamente após falha. O data source é reutilizado como fábrica, com `Pooling=false`; cada consulta abre/fecha sua conexão. Não há leitura de Secret por requisição depois de inicializar com sucesso.

JWT usa HS256, UUID/nome do cadastro, role Customer e expiração UTC + dez minutos. A API mantém sua tolerância de relógio de cinco minutos. Remoção/inativação não revoga tokens emitidos; operações próprias continuam dependendo da existência e vínculo dos recursos. Não há endpoint de inativação na entrega.

400/401 são resultados esperados; falhas técnicas e timeout tratados retornam 500 sanitizado. Erros do próprio Gateway/runtime podem ter formato diferente. O contrato detalha também 401/403 e validação dos argumentos da API. Nenhum documento, token ou credencial é registrado nos logs.
