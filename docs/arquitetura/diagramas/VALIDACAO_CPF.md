# Validação de CPF e acesso à própria OS

[Arquitetura](../README.md) · [Contrato completo](../ACESSO_E_AUTENTICACAO.md)

Fluxo alvo: validação cadastral sem senha; a função emite um token Customer. Isso não cria uma OS nem um usuário interno.

```mermaid
sequenceDiagram
    actor C as Cliente
    participant G as API Gateway
    participant F as Lambda Validate
    participant S as Secrets Manager
    participant D as Aurora
    participant A as API via VPC Link e NLB
    C->>G: POST /customers/validate (CPF)
    G->>F: Encaminhar requisição
    F->>F: Validar formato/checksum e normalizar máscara
    alt CPF inválido
        F-->>G: 400
        G-->>C: 400
    else CPF válido
        F->>S: Obter configuração secreta quando necessária
        S-->>F: Credenciais de leitura e configuração JWT
        F->>D: Consulta parametrizada por CPF (Pooling=false)
        D-->>F: Cadastro/status ou erro técnico
        F->>F: Encerrar conexão em todos os caminhos
        alt Cadastro ausente ou Inativo
            F-->>G: 401
            G-->>C: 401
        else Cadastro Ativo
            F->>F: Assinar JWT HS256 com ID estável e role Customer
            F-->>G: 200 com token
            G-->>C: 200 com token
        else Falha de infraestrutura
            F-->>G: 5xx (não converter para 401)
            G-->>C: 5xx
        end
    end
    opt Cliente recebeu token e solicita status
        C->>G: GET de status da OS + Bearer JWT
        G->>A: Encaminhar para aplicação
        A->>A: Validar assinatura, issuer, audience, validade e role
        alt JWT/permissão inválidos
            A-->>G: Erro de autenticação/autorização
        else Identidade autorizada
            A->>D: Resolver cliente pelo ID e consultar vínculo da OS
            D-->>A: Vínculo/status ou ausência
            A->>A: Permitir somente OS do próprio cliente
            A-->>G: 200 com status ou erro conforme contrato
        end
        G-->>C: Resposta da aplicação
    end
```

Falha ao obter secrets também é técnica e interrompe o fluxo com 5xx. Não registrar CPF, token ou valores de secrets. A elegibilidade Ativo é verificada na emissão: inativação/remoção posterior não revoga automaticamente uma sessão existente. A consulta ainda depende da existência dos recursos e do vínculo com a OS.

O JWT segue o contrato atual, com expiração emitida em UTC + 10 minutos e ID estável; cada ambiente tem sua configuração. API e função compartilharão as regras de CPF/JWT pelo pacote de contratos, sem dependência do domínio de OS. A autorização de aprovação/recusa segue a mesma verificação de propriedade e é exclusiva de Customer.
