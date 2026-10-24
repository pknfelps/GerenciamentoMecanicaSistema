# Documento, código de acesso e operações Customer

```mermaid
sequenceDiagram
    actor C as Cliente / Postman
    participant G as Gateway
    participant F as AuthFunction
    participant D as PostgreSQL
    participant E as EmailFunction
    participant S as SMTP4dev privado
    participant A as API
    C->>G: POST request-code com document
    G->>F: AWS_PROXY para versao numerica
    F->>F: Validar JSON, limites UTF-8 e documento
    alt Entrada invalida
        F-->>C: 400 sem bootstrap SQL/AWS
    else Entrada valida
        F->>F: Obter runtime reutilizavel ou inicializar
        F->>D: Consultar cadastro e elegibilidade
        alt Ausente, Inactive ou email inutilizavel
            F-->>C: 202 generico sem escrita ou envio
        else Cliente elegivel
            F->>D: Transacao e lock por cliente, criar Pending, commit
            alt Cooldown ou limite atingido
                F-->>C: 429 com Retry-After
            else Solicitacao admitida
                F->>E: Invoke v1 sincrono
                E->>S: MIME e captura SMTP
                alt Captura confirmada
                    E-->>F: version 1, outcome captured
                    F->>D: Ativar Ready condicionalmente
                    F-->>C: 202 com challengeId, sem codigo
                else Falha conhecida ou timeout
                    F->>D: Tentar invalidar Pending
                    F-->>C: 500 sanitizado, sem reembolso do limite
                end
            end
        end
    end
    C->>S: Operador autorizado consulta a caixa privada
    C->>G: POST verify-code com challengeId, code e document
    G->>F: AWS_PROXY para a mesma versao numerica
    F->>D: Verificar cadastro, prazo, tentativas e consumir atomicamente
    alt Negativa
        F-->>C: 401 generico, tentativa persistida quando aplicavel
    else Consumo e commit confirmados
        F-->>C: 200 com JWT Customer por dez minutos
        C->>G: GET status ou PATCH budget com Bearer
        G->>A: HTTP_PROXY via VPC Link e NLB
        A->>A: Validar JWT, perfil e exp sem ClockSkew
        A->>D: Exigir Customer Active e OS do mesmo UUID
        A-->>C: Status ou 200 Approved / Refused apos commit
    end
```

Bootstrap real exige hom/prd/us-east-1. Auth carrega dois lotes SSM (nove base/banco, dois email/v1), duas credenciais Secrets e reutiliza o runtime saudável. Prazo de configuração: oito segundos, subordinado ao tempo restante menos um segundo; flush compartilhado existente permanece limitado. Documento/JSON inválidos não iniciam autenticação. Localmente configuração é injetada, sem fallback produtivo de banco/JWT.

Não manter transação durante Invoke/SMTP. Código tem oito dígitos, cinco minutos e cinco tentativas; cooldown de 60 s/cinco pedidos por hora para cliente existente/elegível. Pending não autentica; confirmação consome uma vez. Banco guarda salt/verificador, nunca o código em texto puro. Limpeza oportunista é limitada a 100 expirados por tabela após solicitação admitida, sem job/migration.

O mesmo JWT serve status e orçamento; a Lambda não consulta OS. CPF/CNPJ não aparece no JWT ou orçamento. Cliente inativado após emissão é bloqueado na API. Mensagem capturada pode ser perdida após reinício; pedir outra respeitando limites. Captura não é entrega externa, autenticação por caixa pessoal ou garantia única. Código/token/documento não entram em logs ou evidências.
