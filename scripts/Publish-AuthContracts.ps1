#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagePath,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9][a-z0-9.-]+$')][string]$Bucket,
    [string]$Region = 'us-east-1',
    [string]$ExpectedAccount = '121754142617'
)
$ErrorActionPreference = 'Stop'
$PackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$fileName = [IO.Path]::GetFileName($PackagePath)
if ($fileName -notmatch '^GerenciamentoMecanica\.Auth\.Contracts\.(\d+\.\d+\.\d+)\.nupkg$') {
    throw 'Nome/versão do pacote inválido.'
}
$version = $Matches[1]
$hash = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ([IO.File]::ReadAllText("$PackagePath.sha256") -cne "$hash`n") {
    throw 'Sidecar ausente, inválido ou divergente do pacote.'
}
$prefix = "packages/GerenciamentoMecanica.Auth.Contracts/$version"
function Publish-ImmutableObject([string]$Path, [string]$Key) {
    $result = & aws s3api put-object --bucket $Bucket --key $Key --body $Path --if-none-match '*' --expected-bucket-owner $ExpectedAccount --region $Region --no-cli-pager 2>&1
    if ($LASTEXITCODE -eq 0) { return }
    if (($result -join "`n") -notmatch 'PreconditionFailed|\(412\)') {
        throw "Falha ao publicar $Key. $($result -join ' ')"
    }
    $existing = [IO.Path]::GetTempFileName()
    try {
        & aws s3api get-object --bucket $Bucket --key $Key --expected-bucket-owner $ExpectedAccount --region $Region --no-cli-pager $existing | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Não foi possível conferir o objeto existente: $Key" }
        if ((Get-FileHash -LiteralPath $existing).Hash -ne (Get-FileHash -LiteralPath $Path).Hash) {
            throw "Versão imutável com conteúdo diferente: $Key. Incremente a versão; não sobrescreva."
        }
        Write-Host "Reutilizado objeto idêntico: $Key"
    }
    finally { Remove-Item -LiteralPath $existing -Force }
}
# Publicar o pacote primeiro permite concluir o sidecar em um rerun após falha parcial.
Publish-ImmutableObject $PackagePath "$prefix/$fileName"
Publish-ImmutableObject "$PackagePath.sha256" "$prefix/$fileName.sha256"
Write-Host "Publicado/verificado: s3://$Bucket/$prefix/$fileName (SHA-256 $hash)"
