# Pacote Auth.Contracts

[Arquitetura](README.md) · [Contrato entre repositórios](CONTRATOS_ENTRE_REPOSITORIOS.md) · [API do pacote](../../GerenciamentoMecanica.Auth.Contracts/README.md)

Implementação inicial: **GerenciamentoMecanica.Auth.Contracts 1.0.0**, .NET 10. API referencia o projeto local; autenticação consome o nupkg com `Version="[1.0.0]"`. A extração abrange CPF e CNPJ por definição do usuário em 2026-09-25. A versão permanece 1.0.0, ainda não publicada. A extração não implementa Lambda, autorização de rotas, alteração de perfis ou acesso a banco.

## Conteúdo e compatibilidade

- Documentos: CpfRules e CnpjRules validam os cálculos numéricos existentes e produzem a máscara canônica. DocumentRules identifica o tipo e permite uso genérico. Corrigida a dupla máscara nas chamadas diretas de CPF e CNPJ; o formato persistido permanece igual.
- Erros continuam DomainValidationException na API. O pacote expõe FormatException e não referencia o domínio. Document/Cpf/Cnpj/DocumentWrapper delegam ao pacote, sem algoritmo duplicado. O wrapper deixa de eliminar letras/espaços antes da validação; os contratos HTTP existentes já rejeitam esses formatos.
- JWT: assinatura HS256, Name/Role com os nomes serializados anteriores, expiração de dez minutos. Validação da API, incluindo ClockSkew de cinco minutos, permanece intacta.
- NameIdentifier opcional está disponível para os futuros consumidores E3/E4. O gerador interno atual continua emitindo Name/Role; alterar o login e as permissões pertence à adequação da aplicação.
- O legado aceita pontuação arbitrária e sequências repetidas que passam no cálculo. A extração caracteriza esse comportamento em testes. A validação HTTP estrita exigida por customers/validate continua pendente na implementação da função; normalização não comprova cadastro ou elegibilidade.

O pacote depende apenas de System.IdentityModel.Tokens.Jwt; não carrega controllers, serviços de negócio, repositórios, configuração ou credenciais.

## Validar e empacotar

```powershell
dotnet test Auth.Contracts.Tests -c Release
dotnet test DomainTests -c Release
dotnet test ControllerTests -c Release --filter FullyQualifiedName~JwtContractCompatibilityTests
pwsh -File scripts/Pack-AuthContracts.ps1
pwsh -File scripts/tests/Test-AuthContractsDistribution.ps1
```

A saída fica em artifacts/auth-contracts, com nupkg e sidecar de 64 caracteres hexadecimais minúsculos mais LF. O script estabiliza datas e IDs do envelope ZIP gerado pelo SDK 10. DLL, nuspec e README permanecem intactos. Dois empacotamentos dos mesmos inputs/toolchain devem ter o mesmo hash. Mudança de SDK, dependências ou commit pode mudar os bytes; uma divergência nunca autoriza sobrescrever a versão.

O nuspec registra o commit do checkout. Um pacote local com alterações não commitadas serve para testes, não para evidência de release. Para a publicação definitiva, usar o artefato produzido pelo checkout da execução GitHub; o upload desse mesmo artefato pode ser feito pelo workflow ou pela CLI local.

## Publicar

O [workflow auth-contracts](../../.github/workflows/auth-contracts.yml) executa verificação/empacotamento nos PRs/pushes relevantes, sem autenticar AWS. Publicação é manual, com publish=true, usando develop/hom ou main/prd e as variáveis AWS_REGION, AWS_ROLE_ARN e ARTIFACTS_BUCKET existentes.

O GitHub exige que o workflow com workflow_dispatch esteja na branch padrão para disponibilizar a execução manual; integrar o arquivo conforme a governança do repositório antes de executar. Não alterar a branch padrão nem promover código à main automaticamente para contornar isso.

O job de publicação usa o nupkg já testado da mesma execução; não recompila após assumir a role. O script:

1. Confere nome/versão e sidecar.
2. Publica em packages/GerenciamentoMecanica.Auth.Contracts/1.0.0/ usando If-None-Match: *.
3. Em PreconditionFailed, baixa o objeto e compara seus bytes por SHA-256; conteúdo diferente encerra a operação.
4. Publica/verifica o sidecar por último. Rerun pode completar uma publicação parcial.
5. Não trata AccessDenied ou falhas de rede como existência do pacote.

O artefato é compartilhado pelos ambientes: publicar uma versão uma vez e consumi-la em hom/prd. Promover a aplicação não exige recompilar/republicar o pacote. Alterações do pacote exigem incremento explícito de versão e atualização do consumidor. O pacote não escreve release SSM da API nem exige banco/EKS disponíveis.

### Publicação pela CLI local

Os scripts podem ser executados nesta máquina com PowerShell 7 e AWS CLI autenticada com permissões no bucket. Eles não dependem de branch Git nem usam OIDC do GitHub. Uma execução local comprova o acesso da identidade da CLI; não comprova o acesso das roles das pipelines.

Para a versão definitiva, integrar os repositórios à develop e aguardar a verificação auth-contracts da API. Baixar o artefato auth-contracts dessa execução e extrair o nupkg e o sidecar juntos. Publicar esses mesmos bytes, sem recompilar, preserva o vínculo com o CI e evita diferenças de pacote entre máquinas/commits.

Na raiz da API, ajustando o caminho do artefato baixado:

```powershell
pwsh -File scripts/Publish-AuthContracts.ps1 -PackagePath C:/artefatos/GerenciamentoMecanica.Auth.Contracts.1.0.0.nupkg -Bucket mecanica-artifacts-121754142617-us-east-1
```

Depois, executar o comando de consumo S3 da seção seguinte na raiz da autenticação. Nenhum deploy de API/Lambda/banco é necessário para essa verificação. A integração à develop é a recomendação para rastreabilidade da versão; não é uma exigência técnica do script local.

Somente integrar à develop não disponibiliza necessariamente Run workflow: o workflow manual também precisa existir na branch padrão. Enquanto esse requisito não estiver atendido, é possível publicar o artefato do CI e validar seu consumo pela CLI sem antecipar a promoção da aplicação à main.

## Consumir sem checkout da API

No [repositório de autenticação](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao), scripts/Get-AuthContracts.ps1 lê a versão exata do projeto consumidor, baixa pacote/sidecar do S3, verifica checksum e identidade/versão do nuspec, e então disponibiliza o feed local. ExpectedSha256 permite conferir também uma referência externa, quando houver manifesto.

scripts/Test-AuthContracts.ps1 restaura com NuGet.Config e cache separado pelo hash verificado, e executa ContractTests. Package source mapping reserva o ID do contrato para o feed local. Não copiar fontes nem usar ProjectReference para outro repositório.

```powershell
# Na raiz do repositório de autenticação, com AWS CLI autenticada:
pwsh -File scripts/Test-AuthContracts.ps1 -Bucket mecanica-artifacts-121754142617-us-east-1

# Alternativa local: receber somente o nupkg e seu .sha256.
pwsh -File scripts/Test-AuthContracts.ps1 -PackagePath C:/artefatos/GerenciamentoMecanica.Auth.Contracts.1.0.0.nupkg
```

O workflow manual auth-contracts-check executa o consumo com a role auth. O sucesso local não comprova permissões remotas S3; publicação e download reais continuam como validação posterior à integração dos workflows.

## Referências

- [PackageReference e versões NuGet](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files).
- [PutObject e escrita condicional](https://docs.aws.amazon.com/cli/latest/reference/s3api/put-object.html).
