param(
    [string]$DatabaseRepository,
    [string]$Commit,
    [switch]$Verify
)

$ErrorActionPreference = 'Stop'
$snapshotRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'deploy/local/database'
$sqlPath = Join-Path $snapshotRoot 'Init.sql'
$manifestPath = Join-Path $snapshotRoot 'source.json'
$repositoryUrl = 'https://github.com/pknfelps/GerenciamentoMecanicaBancoDados'
$utf8 = [Text.UTF8Encoding]::new($false)

if ($Verify) {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.repository -ne $repositoryUrl -or $manifest.path -ne 'sql/Init.sql' -or $manifest.commit -notmatch '^[a-f0-9]{40}$') {
        throw 'Referência de origem inválida no snapshot SQL.'
    }
    $actualHash = (Get-FileHash -LiteralPath $sqlPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($manifest.sha256 -ne $actualHash) { throw 'SQL local diverge do snapshot registrado. Atualize a partir de um commit do repositório de banco.' }
    Write-Output "Snapshot SQL válido: $($manifest.commit) / $actualHash"
    exit 0
}

if (!$DatabaseRepository -or $Commit -notmatch '^[a-f0-9]{40}$') {
    throw 'Informe -DatabaseRepository (clone local) e -Commit (SHA completo). Use -Verify para checar o snapshot sem outro checkout.'
}
$resolvedRepository = (Resolve-Path -LiteralPath $DatabaseRepository).Path
$remote = & git -C $resolvedRepository remote get-url origin
if ($LASTEXITCODE -ne 0 -or $remote -notin @($repositoryUrl, "$repositoryUrl.git", 'git@github.com:pknfelps/GerenciamentoMecanicaBancoDados.git')) {
    throw 'O origin informado não corresponde ao repositório de banco esperado.'
}
$resolvedCommit = & git -C $resolvedRepository rev-parse --verify "$Commit^{commit}"
if ($LASTEXITCODE -ne 0 -or $resolvedCommit -ne $Commit) { throw 'Commit não encontrado no clone do banco.' }
$sqlLines = & git -C $resolvedRepository show "${Commit}:sql/Init.sql"
if ($LASTEXITCODE -ne 0 -or !$sqlLines) { throw 'SQL ausente no commit informado.' }
$sql = ($sqlLines -join "`n") + "`n"
$sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($utf8.GetBytes($sql))).ToLowerInvariant()
$manifest = [ordered]@{ repository = $repositoryUrl; commit = $Commit; path = 'sql/Init.sql'; sha256 = $sha256 }
[IO.Directory]::CreateDirectory($snapshotRoot) | Out-Null
[IO.File]::WriteAllText($sqlPath, $sql, $utf8)
[IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json) + "`n", $utf8)
Write-Output "Snapshot atualizado para $Commit. Revise e versione Init.sql e source.json juntos."
