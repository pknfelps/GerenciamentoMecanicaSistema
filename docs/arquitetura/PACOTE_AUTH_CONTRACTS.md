# Pacote Auth.Contracts

[Arquitetura](README.md) · [Contratos operacionais](CONTRATOS_ENTRE_REPOSITORIOS.md) · [API do pacote](../../GerenciamentoMecanica.Auth.Contracts/README.md)

Revisão de 21/10/2026. GerenciamentoMecanica.Auth.Contracts 1.0.0/.NET 10 compartilha CPF/CNPJ numérico e JWT. API referencia o projeto local; autenticação usa PackageReference exata [1.0.0], sem checkout ou ProjectReference entre repositórios. Pacote já publicado e consumo remoto comprovado; isso não constitui prova independente de todas as funções do sistema.

## Conteúdo e compatibilidade

DocumentRules/CpfRules/CnpjRules preservam cálculos/canonização do legado; pacote não inclui controllers, banco ou domínio de OS. A Lambda faz validação estrita de formato antes de delegar dígitos/máscara; não é suporte a CNPJ alfanumérico. A API adapta FormatException para suas regras de domínio.

JwtIssuer usa HS256, Name/Role, ID opcional como NameIdentifier e prazo UTC de dez minutos. Os emissores atuais de API/Lambda **já fornecem UUID persistido**; opcionalidade do pacote não significa que as operações próprias aceitam identidade inválida. Validação API mantém ClockSkew de cinco minutos. O pacote não conhece Secrets, issuer/audience de ambientes ou revogação de sessão.

## Produzir e publicar

No repositório API, os scripts existentes continuam sendo usados pelo workflow auth-contracts:

```powershell
dotnet test Auth.Contracts.Tests -c Release
dotnet test DomainTests -c Release
dotnet test ControllerTests -c Release --filter FullyQualifiedName~JwtContractCompatibilityTests
pwsh -File scripts/Pack-AuthContracts.ps1
pwsh -File scripts/tests/Test-AuthContractsDistribution.ps1
```

Esses comandos não acessam RDS. O empacotamento estabiliza o envelope ZIP e produz nupkg/sidecar em artifacts/auth-contracts. Mudança de SDK/dependências/commit pode mudar bytes; divergência exige versão nova, sem sobrescrever uma já publicada. Pacote com mudanças não commitadas é somente para teste.

Auth-contracts verifica PRs relevantes e permite dispatch. Publicação manual com publish=true usa develop/hom ou main/prd, OIDC/variáveis existentes e o artefato já testado da mesma execução. Não recompila após assumir a role. Os scripts produtor Pack/Publish permanecem; os antigos scripts consumidores do repositório auth foram retirados.

Nupkg e sidecar usam If-None-Match. PreconditionFailed permite baixar/comparar bytes idênticos; divergência, acesso negado ou rede falham, sem sobrescrita. Uma reexecução completa apenas os objetos faltantes. Versão é compartilhada entre ambientes, sem deploy de API/EKS/RDS ou release SSM para publicar o pacote.

## Consumir na autenticação

Auth-ci/auth-publish baixam via AWS CLI os dois objetos da versão exata, conferem SHA-256 e separam cache NuGet pelo hash. NuGet.Config reserva o ID ao feed local artifacts/auth-contracts; ContractTests valida o pacote independentemente dos projetos de produção.

Referência 1.0.0: b9080cea9d303722ac072e89421505d728126cdb52d57549da4328b1e18ab4e9. Na raiz da autenticação, com AWS CLI autenticada e leitura autorizada:

```powershell
New-Item -ItemType Directory -Force artifacts/auth-contracts
aws s3api get-object --bucket mecanica-artifacts-121754142617-us-east-1 --expected-bucket-owner 121754142617 --key packages/GerenciamentoMecanica.Auth.Contracts/1.0.0/GerenciamentoMecanica.Auth.Contracts.1.0.0.nupkg artifacts/auth-contracts/GerenciamentoMecanica.Auth.Contracts.1.0.0.nupkg
aws s3api get-object --bucket mecanica-artifacts-121754142617-us-east-1 --expected-bucket-owner 121754142617 --key packages/GerenciamentoMecanica.Auth.Contracts/1.0.0/GerenciamentoMecanica.Auth.Contracts.1.0.0.nupkg.sha256 artifacts/auth-contracts/GerenciamentoMecanica.Auth.Contracts.1.0.0.nupkg.sha256
Get-FileHash artifacts/auth-contracts/GerenciamentoMecanica.Auth.Contracts.1.0.0.nupkg -Algorithm SHA256
Get-Content artifacts/auth-contracts/GerenciamentoMecanica.Auth.Contracts.1.0.0.nupkg.sha256
```

Comparar ambos com a referência antes do restore; não executar pacote divergente. Usar cache separado conforme [README auth](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/README.md#consumo-do-authcontracts). Não há Get-AuthContracts.ps1/Test-AuthContracts.ps1 no consumidor atual. S3 não é feed NuGet HTTP: os arquivos são baixados antes do restore.

[Publicação inicial](https://github.com/pknfelps/GerenciamentoMecanicaSistema/actions/runs/36205335338), [pipeline produtor](../../.github/workflows/auth-contracts.yml), [consumo/deploy auth](https://github.com/pknfelps/GerenciamentoMecanicaServerless/blob/develop/docs/DEPLOY.md) e [cadeia de integração hom](../EVIDENCIAS_CICD_E_PROTECOES_2026-10-21.md). Não houve nova publicação, execução de scripts ou alteração de pacote nesta revisão.
