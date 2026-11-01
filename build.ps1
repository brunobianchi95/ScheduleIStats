param([string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Schedule I')
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$deps = Join-Path $taskRoot '.deps'
New-Item -ItemType Directory -Force $deps | Out-Null
$archive = Join-Path $deps 'S1API-3.2.2.zip'
if (!(Test-Path -LiteralPath $archive)) {
    Invoke-WebRequest 'https://github.com/ifBars/S1API/releases/download/v3.2.2/S1API-Forked-3.2.2.zip' -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne 'EA81F1EAE46F6A13A5228F3CEC891E8E9529BDE22DB647AA33F3BB698A7C10B2') {
    throw 'El SHA-256 de S1API no coincide con la versión verificada.'
}
Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $deps 'S1API-release') -Force
dotnet build (Join-Path $taskRoot 'ScheduleIStats.csproj') -c Release "-p:GameDir=$GameDir" "-p:DepsDir=$deps"
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación.' }
