# GerenciamentoMecanica.Auth.Contracts

Pacote .NET 10, versão inicial **1.0.0**, com validação/normalização de documentos CPF/CNPJ e emissão JWT. Não referencia projetos da API, ASP.NET, banco, configuração ou serviços AWS. A chave é fornecida pelo consumidor no runtime.

`CpfRules.Normalize(string?)` retorna `XXX.XXX.XXX-XX`; `CnpjRules.Normalize(string?)` retorna `XX.XXX.XXX/XXXX-XX` para o CNPJ numérico já suportado pelo projeto. Ambos lançam `FormatException` em erro e usam o mesmo cálculo compartilhado de dígitos verificadores. A normalização com/sem máscara é idempotente para ambos.

`DocumentRules.Parse(string?)` identifica CPF ou CNPJ e retorna `NormalizedDocument`, com `Type` e `Value`. `DocumentRules.Normalize(string?)` retorna somente a máscara canônica. Use essa entrada genérica para validar o documento do cliente; use as regras específicas quando o tipo já for conhecido.

Os cálculos preservam a aceitação legada de pontuação arbitrária e sequências repetidas que passam nos dígitos verificadores. A entrada genérica rejeita letras e espaços, assim como as regras específicas, antes de identificar o tipo; o wrapper do domínio passa a delegar a ela sem eliminar esses caracteres previamente. O contrato HTTP da futura função exige uma verificação de formato estrito antes da normalização; essa adequação/testes pertencem à E3. Não usar normalização como evidência de existência ou elegibilidade do cliente.

`new JwtIssuer(key, issuer, audience).Generate(name, role, id?)` mantém HS256, expiração UTC + 10 minutos e os nomes serializados de Name/Role. O ID opcional acrescenta NameIdentifier para o contrato E0; o login interno continua usando a sobrecarga sem ID até a adequação E4. Roles e identidade são responsabilidade do consumidor, obtidas do cadastro autenticado. O pacote não autoriza rotas nem decide elegibilidade.

O `TimeProvider` opcional existe para testes. A API conserva seus parâmetros de validação, incluindo a tolerância de relógio, e sua exigência de chave com pelo menos 32 caracteres.

## Build independente

Na raiz do repositório da API:

```powershell
dotnet test Auth.Contracts.Tests -c Release
pwsh -File scripts/Pack-AuthContracts.ps1
```

O pacote e seu SHA-256 ficam em `artifacts/auth-contracts`. O workflow `auth-contracts` testa/empacota sem Docker, Aurora ou EKS; a publicação é uma ação manual explícita do workflow, com OIDC e Environment compatível com a branch.

## Consumo

A API usa ProjectReference; a função usa PackageReference com `Version="[1.0.0]"`. S3 é armazenamento do nupkg, não endpoint NuGet. Baixar o pacote e seu sidecar `.sha256`, conferir o hash e só então disponibilizá-lo no feed local. O repositório de autenticação contém o script de consumo e o teste independente do pacote.

Não usar versão flutuante, checkout da API no build da função ou valores secretos no pacote. A versão inicial permanece 1.0.0 enquanto não publicada. Após a primeira publicação, atualizações do conteúdo exigem nova versão; não sobrescrever uma versão existente. A publicação remota e o consumo com a role auth ainda precisam ser executados após integração do workflow.
