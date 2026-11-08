<#
.SYNOPSIS
Reports recent Among Us App Control blocks and installed file signatures without changing files or settings.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$GameDir,
    [ValidateRange(1, 168)][int]$SinceHours = 24
)
$ErrorActionPreference = 'Stop'
$report = [ordered]@{
    GeneratedUtc = [DateTime]::UtcNow.ToString('o'); WindowsVersion = [Environment]::OSVersion.Version.ToString()
    Game = [ordered]@{ RequestedPath = $GameDir; ResolvedPath = $null; FileVersion = $null; ProductVersion = $null; Errors = @() }
    Logs = @(); CodeIntegrity = [ordered]@{ SinceUtc = (Get-Date).AddHours(-$SinceHours).ToUniversalTime().ToString('o'); Status = 'Unknown'; Error = $null; ParseErrors = @(); ScanScope = 'RecentCodeIntegrityRecords'; ScanLimit = 250; ScannedCount = 0; ScanLimitReached = $false; RelevantLimit = 100; Events = @() }
    Files = @()
}
$gameRoot = $null
Write-Verbose 'Reading the game version.'
try {
    $gameRoot = (Resolve-Path -LiteralPath $GameDir).ProviderPath
    if (!(Test-Path -LiteralPath $gameRoot -PathType Container)) { throw 'GameDir must be a directory.' }
    $report.Game.ResolvedPath = $gameRoot
    $gameExe = Get-Item -LiteralPath (Join-Path $gameRoot 'Among Us.exe')
    $report.Game.FileVersion = $gameExe.VersionInfo.FileVersion
    $report.Game.ProductVersion = $gameExe.VersionInfo.ProductVersion
    if (!$report.Game.FileVersion -and !$report.Game.ProductVersion) {
        $report.Game.Errors += 'Executable version metadata is unavailable; record the game version from the main menu.'
    }
} catch { $report.Game.Errors += $_.Exception.Message }

if ($gameRoot) {
    Write-Verbose 'Reading the BepInEx logs.'
    foreach ($name in @('LogOutput.log', 'LogOutput.txt')) {
        $logPath = Join-Path $gameRoot ('BepInEx/' + $name)
        $log = [ordered]@{ Path = $logPath; Status = 'Unknown'; LastWriteUtc = $null; VersionLines = @(); RuntimeErrors = @(); Error = $null }
        try {
            $logItem = Get-Item -LiteralPath $logPath
            $log.LastWriteUtc = $logItem.LastWriteTimeUtc.ToString('o')
            # Windows PowerShell attaches provider metadata to Get-Content strings.
            # Copy the text so JSON cannot recursively serialize provider objects.
            $lines = @(Get-Content -LiteralPath $logPath -Tail 1000 | ForEach-Object { [string]::new($_.ToCharArray()) })
            $log.VersionLines = @($lines | Where-Object { $_ -match 'BepInEx\s+\d|Loading \[.*Anti Cheat|Anti Cheat.*load|Process bitness|Running under Unity|Runtime version|Runtime information' } | Select-Object -First 12)
            $log.RuntimeErrors = @($lines | Where-Object { $_ -match '(?i)error|exception|0x800711c7|application control|blocked' } | Select-Object -Last 50)
            $log.Status = 'Read'
        } catch { $log.Status = 'Unavailable'; $log.Error = $_.Exception.Message }
        $report.Logs += $log
    }
}

$interopNames = @{}
$events = @()
Write-Verbose 'Reading recent Code Integrity records.'
try {
    $events = @(Get-WinEvent -FilterHashtable @{ LogName = 'Microsoft-Windows-CodeIntegrity/Operational'; StartTime = (Get-Date).AddHours(-$SinceHours) } -MaxEvents $report.CodeIntegrity.ScanLimit)
    $report.CodeIntegrity.ScannedCount = $events.Count
    $report.CodeIntegrity.ScanLimitReached = $events.Count -eq $report.CodeIntegrity.ScanLimit
    $report.CodeIntegrity.Status = 'Read'
} catch {
    if ($_.FullyQualifiedErrorId -like 'NoMatchingEventsFound*') { $report.CodeIntegrity.Status = 'Read' }
    else { $report.CodeIntegrity.Status = 'Unavailable'; $report.CodeIntegrity.Error = $_.Exception.Message }
}
foreach ($event in $events) {
    if ($event.Id -ne 3077) { continue }
    try {
        [xml]$xml = $event.ToXml()
        $data = [ordered]@{}
        $index = 0
        foreach ($field in $xml.Event.EventData.Data) {
            $key = [string]$field.Name
            if (!$key -or $data.Contains($key)) { $key = 'Field' + $index }
            $data[$key] = [string]$field.InnerText
            $index++
        }
        # Rendering Event.Message loads provider resources; filter raw paths first.
        $searchText = $data.Values -join ' '
        if ($searchText -notmatch '(?i)Among[ _]?Us|BepInEx') { continue }
        $report.CodeIntegrity.Events += [ordered]@{ RecordId = $event.RecordId; TimeCreatedUtc = $event.TimeCreated.ToUniversalTime().ToString('o'); ActivityId = [string]$xml.Event.System.Correlation.ActivityID; Fields = $data }
        foreach ($value in $data.Values) {
            if ($value -match '(?i)[\\/]BepInEx[\\/]interop[\\/].+\.dll$') { $interopNames[[IO.Path]::GetFileName($value)] = $true }
        }
        if ($report.CodeIntegrity.Events.Count -ge 100) { break }
    } catch {
        $report.CodeIntegrity.ParseErrors += [ordered]@{ RecordId = $event.RecordId; Error = $_.Exception.Message }
        $report.CodeIntegrity.Status = 'ReadWithErrors'
    }
}

if ($gameRoot) {
    Write-Verbose 'Checking installed file hashes and signatures.'
    $paths = @((Join-Path $gameRoot 'BepInEx/plugins/AntiCheat/AmongUs.AntiCheat.dll'), (Join-Path $gameRoot 'BepInEx/plugins/AntiCheat/AntiCheat.Core.dll'))
    foreach ($name in ($interopNames.Keys | Sort-Object)) { $paths += Join-Path $gameRoot ('BepInEx/interop/' + $name) }
    foreach ($path in $paths) {
        $file = [ordered]@{ Path = $path; LastWriteUtc = $null; SHA256 = $null; SignatureStatus = $null; SignatureMessage = $null; SignerSubject = $null; SignerThumbprint = $null; Errors = @() }
        try { $file.LastWriteUtc = (Get-Item -LiteralPath $path).LastWriteTimeUtc.ToString('o') } catch { $file.Errors += $_.Exception.Message }
        try { $file.SHA256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash } catch { $file.Errors += $_.Exception.Message }
        try {
            Write-Verbose "Checking signature: $path"
            $signature = Get-AuthenticodeSignature -LiteralPath $path
            $file.SignatureStatus = [string]$signature.Status; $file.SignatureMessage = $signature.StatusMessage
            if ($signature.SignerCertificate) { $file.SignerSubject = $signature.SignerCertificate.Subject; $file.SignerThumbprint = $signature.SignerCertificate.Thumbprint }
        } catch { $file.Errors += $_.Exception.Message }
        $report.Files += $file
    }
}
Write-Verbose 'Serializing the report.'
$report | ConvertTo-Json -Depth 9
