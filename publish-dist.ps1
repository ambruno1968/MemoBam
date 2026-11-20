param(
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

[xml]$csproj = Get-Content 'MemoBam.csproj'
$version = $csproj.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Versione non trovata in MemoBam.csproj"
}

$distRoot = Join-Path $PSScriptRoot 'dist'
$packageName = "MemoBam-v$version-win-x64"
$outDir = Join-Path $distRoot $packageName
$zipPath = Join-Path $distRoot "$packageName.zip"

if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$scFlag = if ($SelfContained) { 'true' } else { 'false' }

Write-Host "Pubblicazione MemoBam v$version (SelfContained=$scFlag)..." -ForegroundColor Cyan
dotnet publish MemoBam.csproj `
    -c Release `
    -r win-x64 `
    --self-contained $scFlag `
    -p:PublishSingleFile=false `
    -o $outDir

Compress-Archive -Path "$outDir\*" -DestinationPath $zipPath -Force

Write-Host "Distribuzione pronta:" -ForegroundColor Green
Write-Host "  Cartella: $outDir"
Write-Host "  Archivio: $zipPath"
