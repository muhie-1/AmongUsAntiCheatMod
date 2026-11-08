<#
.SYNOPSIS
Builds a plugin ZIP from shared Windows reference packages or a local game installation.
.EXAMPLE
.\scripts\Build-Release.ps1 -Store Steam -GameDir 'D:\SteamLibrary\steamapps\common\Among Us'
.EXAMPLE
.\scripts\Build-Release.ps1 -Store Steam
.EXAMPLE
.\scripts\Build-Release.ps1 -Store EpicGames
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Steam', 'MicrosoftStore', 'EpicGames')]
    [string]$Store,

    [string]$GameDir
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$packageWindows = $Store -in @('Steam', 'EpicGames') -and [string]::IsNullOrWhiteSpace($GameDir)
if ($packageWindows) {
    $projectPath = Join-Path $repoRoot 'src/AntiCheat.Steam/AntiCheat.Steam.csproj'
    $referenceInfo = 'Built against AmongUs.GameLibs.Steam 2026.9.29 using generated interop references.'
    if ($Store -eq 'EpicGames') {
        $targetInfo = 'Target: Epic Games Windows 2026.9.29 compatibility candidate. Steam references were used; Epic assembly compatibility and runtime behavior are UNVERIFIED. Match the loader to your executable architecture.'
    } else {
        $targetInfo = 'Target: Among Us Steam 2026.9.29 on Windows x64. Use the x64 Unity IL2CPP loader.'
    }
    $buildProperties = @()
} else {
    if ([string]::IsNullOrWhiteSpace($GameDir)) { throw 'GameDir is required for MicrosoftStore builds.' }
    $gameRoot = (Resolve-Path -LiteralPath $GameDir).Path
    foreach ($requiredFile in @(
        'Among Us.exe',
        'BepInEx/core/BepInEx.Unity.IL2CPP.dll',
        'BepInEx/interop/Assembly-CSharp.dll'
    )) {
        if (!(Test-Path -LiteralPath (Join-Path $gameRoot $requiredFile) -PathType Leaf)) {
            throw "Missing $requiredFile in '$gameRoot'. Install the matching BepInEx 6 IL2CPP loader and launch the game once."
        }
    }
    $projectPath = Join-Path $repoRoot 'src/AntiCheat.Plugin/AntiCheat.Plugin.csproj'
    $referenceInfo = "Built against the supplied $Store game installation's generated assemblies."
    $targetInfo = 'Target: the supplied installation; use a loader matching its version and architecture.'
    $buildProperties = @("-p:GameDir=$gameRoot")
}

$pluginSource = Get-Content -LiteralPath (Join-Path $repoRoot 'src/AntiCheat.Plugin/Plugin.cs') -Raw
$versionMatch = [regex]::Match($pluginSource, '\[BepInPlugin\("local\.amongus\.anticheat",\s*"[^"]+",\s*"([^"]+)"\)\]')
if (!$versionMatch.Success) { throw 'Could not read the plugin version.' }
$version = $versionMatch.Groups[1].Value
$workRoot = Join-Path $repoRoot ('dist/work/' + [guid]::NewGuid().ToString('N'))
$buildRoot = Join-Path $workRoot 'build'
$stageRoot = Join-Path $workRoot 'package'
$pluginRoot = Join-Path $stageRoot 'BepInEx/plugins/AntiCheat'
$outputRoot = Join-Path $repoRoot "dist/$Store"
New-Item -ItemType Directory -Path $pluginRoot, $outputRoot -Force | Out-Null

# For local builds, pass GameDir explicitly so another storefront's Game.props is not used.
# Rebuild to avoid reusing output compiled against a different game's assemblies.
& dotnet build $projectPath -c Release --no-incremental @buildProperties -o $buildRoot
if ($LASTEXITCODE -ne 0) { throw 'Build failed; no release ZIP was created.' }

foreach ($dll in @('AmongUs.AntiCheat.dll', 'AntiCheat.Core.dll')) {
    Copy-Item -LiteralPath (Join-Path $buildRoot $dll) -Destination $pluginRoot
}
foreach ($document in @('README.md', 'LICENSE')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $document) -Destination $stageRoot
}
$packageDocs = Join-Path $stageRoot 'docs'
New-Item -ItemType Directory -Path $packageDocs -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/SMART-APP-CONTROL.md') -Destination $packageDocs
$releaseNotes = Join-Path $repoRoot "dist/release-notes-v$version.md"
if (Test-Path -LiteralPath $releaseNotes) {
    Copy-Item -LiteralPath $releaseNotes -Destination (Join-Path $stageRoot 'RELEASE-NOTES.md')
}
$installText = @"
Among Us Anti Cheat v$version - $Store - Experimental prerelease

$referenceInfo
$targetInfo
Compilation does not establish in-game compatibility or detection accuracy.
This script produces unsigned DLLs. Smart App Control may block the plugin or
loader dependencies. See docs/SMART-APP-CONTROL.md before installing on such a PC.

1. Close Among Us.
2. Install BepInEx 6 Unity IL2CPP matching your game version and architecture.
3. Launch the game once to generate BepInEx/interop, then close it.
4. Extract this ZIP into the folder containing Among Us.exe.
   Both DLLs belong in BepInEx/plugins/AntiCheat/.
5. Launch via your storefront and test in a private lobby.
   Look for the Anti Cheat panel. Movement checks start during the match.

Meeting sabotage checks run during meetings/exile. Hosts can reject known
sabotage-start requests; guests only observe warnings. Repairs pass.
Secretly reading impostor roles cannot be reliably detected by this mod.

Keep automatic kicking off during testing. Guest mode only shows local warnings.
See README.md for Steam, Epic Games, and Microsoft Store setup and troubleshooting.
To uninstall, close the game and remove BepInEx/plugins/AntiCheat/.
Game assemblies and the BepInEx loader are not included.
"@
Set-Content -LiteralPath (Join-Path $stageRoot 'INSTALL.txt') -Value $installText -Encoding UTF8
$zipPath = Join-Path $outputRoot "AmongUsAntiCheatMod-v$version-$Store.zip"
Compress-Archive -Path (Join-Path $stageRoot '*') -DestinationPath $zipPath -Force
Write-Output "Release ZIP: $zipPath"
Write-Output 'Unsigned package: Smart App Control compatibility has not been established.'
Write-Output 'Run a private-lobby smoke test before advertising this build as verified.'
