# GerenciamentoMecanica.Auth.Contracts

Pacote .NET 10, versão inicial **1.0.0**, com validação/normalização de documentos CPF/CNPJ e emissão JWT. Não referencia projetos da API, ASP.NET, banco, configuração ou serviços AWS. A chave é fornecida pelo consumidor no runtime.

`CpfRules.Normalize(string?)` retorna `XXX.XXX.XXX-XX`; `CnpjRules.Normalize(string?)` retorna `XX.XXX.XXX/XXXX-XX` para o CNPJ numérico já suportado pelo projeto. Ambos lançam `FormatException` em erro e usam o mesmo cálculo compartilhado de dígitos verificadores. A normalização com/sem máscara é idempotente para ambos.

`DocumentRules.Parse(string?)` identifica CPF ou CNPJ e retorna `NormalizedDocument`, com `Type` e `Value`. `DocumentRules.Normalize(string?)` retorna somente a máscara canônica. Use essa entrada genérica para validar o documento do cliente; use as regras específicas quando o tipo já for conhecido.

Os cálculos preservam a aceitação legada de pontuação arbitrária e sequências repetidas que passam nos dígitos verificadores. A entrada genérica rejeita letras e espaços, assim como as regras específicas, antes de identificar o tipo; o wrapper do domínio passa a delegar a ela sem eliminar esses caracteres previamente. O parser HTTP da AuthFunction aplica uma verificação de formato estrito antes da normalização. Não usar normalização como evidência de existência ou elegibilidade do cliente.

`new JwtIssuer(key, issuer, audience).Generate(name, role, id?)` mantém HS256, expiração UTC + 10 minutos e os nomes serializados de Name/Role. O ID opcional acrescenta NameIdentifier; API e AuthFunction fornecem o UUID do cadastro autenticado. Roles e identidade são responsabilidade do consumidor, obtidas do cadastro autenticado. O pacote não autoriza rotas nem decide elegibilidade.

O `TimeProvider` opcional existe para testes. A API valida assinatura, emissor, audiência e expiração com ClockSkew zero e exige chave com pelo menos 32 caracteres.

## Build independente

Na raiz do repositório da API:

```powershell
dotnet test Auth.Contracts.Tests -c Release
pwsh -File scripts/Pack-AuthContracts.ps1
```

O pacote e seu SHA-256 ficam em `artifacts/auth-contracts`. O workflow `auth-contracts` testa/empacota sem Docker, banco ou EKS; a publicação é uma ação manual explícita do workflow, com OIDC e Environment compatível com a branch.

## Consumo

A API usa ProjectReference; a função usa PackageReference com `Version="[1.0.0]"`. S3 é armazenamento do nupkg, não endpoint NuGet. Baixar o pacote e seu sidecar `.sha256`, conferir o hash e só então disponibilizá-lo no feed local. O workflow Serverless baixa e confere o pacote antes do restore; ContractTests verifica sua compatibilidade, sem script consumidor próprio.

Não usar versão flutuante, checkout da API no build da função ou valores secretos no pacote. A versão 1.0.0 é consumida pelo Serverless. Mudanças de conteúdo exigem nova versão; não sobrescrever uma versão existente.
