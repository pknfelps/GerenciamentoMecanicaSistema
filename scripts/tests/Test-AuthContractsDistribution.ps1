#Requires -Version 7.0
# Testes offline do protocolo de publicação. Nenhuma chamada AWS real.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$publisher = Join-Path $PSScriptRoot '../Publish-AuthContracts.ps1'
$taskDirectory = Join-Path ([IO.Path]::GetTempPath()) ("auth-contracts-tests-" + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $taskDirectory | Out-Null
$package = Join-Path $taskDirectory 'GerenciamentoMecanica.Auth.Contracts.1.0.0.nupkg'
$global:AuthContractsTestStore = @{}
$global:AuthContractsTestGets = 0
$global:AuthContractsTestDeny = $false
function global:aws {
    $argumentsList = @($args)
    $key = $argumentsList[[Array]::IndexOf($argumentsList, '--key') + 1]
    if ($global:AuthContractsTestDeny) {
        $global:LASTEXITCODE = 1
        return 'An error occurred (AccessDenied)'
    }
    switch ($argumentsList[1]) {
        'put-object' {
            if ($argumentsList -notcontains '--if-none-match' -or
                $argumentsList[[Array]::IndexOf($argumentsList, '--if-none-match') + 1] -ne '*') {
                throw 'Publicação sem escrita condicional.'
            }
            if ($global:AuthContractsTestStore.ContainsKey($key)) {
                $global:LASTEXITCODE = 1
                return 'An error occurred (PreconditionFailed)'
            }
            $body = $argumentsList[[Array]::IndexOf($argumentsList, '--body') + 1]
            $global:AuthContractsTestStore[$key] = [IO.File]::ReadAllBytes($body)
        }
        'get-object' {
            $global:AuthContractsTestGets++
            [IO.File]::WriteAllBytes($argumentsList[-1], $global:AuthContractsTestStore[$key])
        }
        default { throw 'Operação AWS inesperada no teste.' }
    }
    $global:LASTEXITCODE = 0
}
function Set-TestPackage([string]$Content) {
    [IO.File]::WriteAllText($package, $Content)
    $hash = (Get-FileHash -LiteralPath $package).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$package.sha256", "$hash`n")
}
function Expect-Failure([scriptblock]$Action, [string]$Message) {
    try { & $Action } catch {
        if ($_.Exception.Message -notlike "*$Message*") { throw }
        return
    }
    throw "Falha esperada não ocorreu: $Message"
}
try {
    Set-TestPackage 'first'
    & $publisher -PackagePath $package -Bucket test-bucket
    if ($global:AuthContractsTestStore.Count -ne 2) { throw 'Pacote e sidecar não publicados.' }
    & $publisher -PackagePath $package -Bucket test-bucket
    if ($global:AuthContractsTestGets -ne 2) { throw 'Rerun não verificou ambos os objetos.' }

    $sidecarKey = @($global:AuthContractsTestStore.Keys | Where-Object { $_.EndsWith('.sha256') })[0]
    $global:AuthContractsTestStore.Remove($sidecarKey)
    & $publisher -PackagePath $package -Bucket test-bucket
    if ($global:AuthContractsTestStore.Count -ne 2) { throw 'Não recuperou publicação parcial.' }

    Set-TestPackage 'different'
    Expect-Failure { & $publisher -PackagePath $package -Bucket test-bucket } 'conteúdo diferente'
    Set-TestPackage 'first'
    [IO.File]::WriteAllText("$package.sha256", "invalid`n")
    Expect-Failure { & $publisher -PackagePath $package -Bucket test-bucket } 'Sidecar'

    Set-TestPackage 'first'
    $global:AuthContractsTestDeny = $true
    $before = $global:AuthContractsTestGets
    Expect-Failure { & $publisher -PackagePath $package -Bucket test-bucket } 'AccessDenied'
    if ($global:AuthContractsTestGets -ne $before) { throw 'AccessDenied foi tratado como objeto existente.' }
    Write-Host '6 cenários de publicação offline aprovados.'
}
finally {
    Remove-Item Function:/aws
    Remove-Variable AuthContractsTestStore, AuthContractsTestGets, AuthContractsTestDeny -Scope Global
    Remove-Item -LiteralPath $package, "$package.sha256" -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $taskDirectory -Force
}

# Os erros esperados do mock não são o resultado da suíte. O runner pwsh do GitHub
# usa LASTEXITCODE ao encerrar o step; só sinalizar sucesso após testes e cleanup.
$global:LASTEXITCODE = 0
