[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $repoRoot 'dist/UpdateNotifier'
$workRoot = Join-Path $repoRoot ('dist/work/notifier-' + [guid]::NewGuid().ToString('N'))
$publishRoot = Join-Path $workRoot 'publish'
$packageRoot = Join-Path $workRoot 'package'
New-Item -ItemType Directory -Path $outputRoot, $packageRoot -Force | Out-Null
& dotnet publish (Join-Path $repoRoot 'src/AntiCheat.UpdateNotifier/AntiCheat.UpdateNotifier.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o $publishRoot
if ($LASTEXITCODE -ne 0) { throw 'Update notifier publish failed.' }
Copy-Item -LiteralPath (Join-Path $publishRoot 'AmongUs.AntiCheat.UpdateNotifier.exe') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/UPDATE-NOTIFICATIONS.md') -Destination (Join-Path $packageRoot 'README.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $packageRoot
$zipPath = Join-Path $outputRoot 'AmongUsAntiCheatMod-UpdateNotifier-win-x64.zip'
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath -Force
Write-Output "Optional notifier ZIP: $zipPath"
Write-Output "Executable: $(Join-Path $publishRoot 'AmongUs.AntiCheat.UpdateNotifier.exe')"
