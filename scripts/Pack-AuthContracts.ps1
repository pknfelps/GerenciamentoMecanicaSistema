#Requires -Version 7.0
[CmdletBinding()]
param([string]$OutputDirectory = (Join-Path $PSScriptRoot '../artifacts/auth-contracts'))
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../GerenciamentoMecanica.Auth.Contracts/GerenciamentoMecanica.Auth.Contracts.csproj'
[xml]$definition = Get-Content -LiteralPath $project -Raw
$version = [string]$definition.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use uma versão SemVer estável explícita no projeto.' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
& dotnet pack $project -c Release -o $OutputDirectory -p:ContinuousIntegrationBuild=true
if ($LASTEXITCODE -ne 0) { throw 'Falha no empacotamento de Auth.Contracts.' }
$package = Join-Path $OutputDirectory "GerenciamentoMecanica.Auth.Contracts.$version.nupkg"
# O pack do SDK 10 gera datas/IDs aleatórios no ZIP. Canonicaliza somente o envelope;
# DLL, nuspec (inclusive commit de origem) e README permanecem byte a byte.
$canonical = "$package.$([Guid]::NewGuid()).canonical"
$source = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $target = [IO.Compression.ZipFile]::Open($canonical, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in ($source.Entries | Sort-Object FullName)) {
            $name = $entry.FullName
            if ($name -like 'package/services/metadata/core-properties/*.psmdcp') {
                $name = 'package/services/metadata/core-properties/contracts.psmdcp'
            }
            $copy = $target.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal)
            $copy.LastWriteTime = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
            $copy.ExternalAttributes = 0
            $inputStream = $entry.Open()
            $outputStream = $copy.Open()
            try {
                if ($name -eq '_rels/.rels') {
                    $reader = [IO.StreamReader]::new($inputStream)
                    [xml]$relationships = $reader.ReadToEnd()
                    $number = 0
                    foreach ($relationship in $relationships.DocumentElement.ChildNodes) {
                        $relationship.SetAttribute('Id', "R$number")
                        $number++
                        if ($relationship.GetAttribute('Type').EndsWith('/core-properties')) {
                            $relationship.SetAttribute('Target', '/package/services/metadata/core-properties/contracts.psmdcp')
                        }
                    }
                    $bytes = [Text.Encoding]::UTF8.GetBytes($relationships.OuterXml)
                    $outputStream.Write($bytes, 0, $bytes.Length)
                }
                else { $inputStream.CopyTo($outputStream) }
            }
            finally { $inputStream.Dispose(); $outputStream.Dispose() }
        }
    }
    finally { $target.Dispose() }
}
finally { $source.Dispose() }
Move-Item -LiteralPath $canonical -Destination $package -Force
$hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$package.sha256", "$hash`n", [Text.UTF8Encoding]::new($false))
Write-Host "Pacote: $package"
Write-Host "SHA-256: $hash"
