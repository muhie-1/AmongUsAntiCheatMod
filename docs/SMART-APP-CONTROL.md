# Smart App Control compatibility

The current build scripts produce unsigned mod DLLs and an unsigned update notifier. These builds have **not** been verified to work with Smart App Control enabled. On the project's Microsoft Store test installation, Windows blocked `AmongUs.AntiCheat.dll` with `0x800711C7` before the plugin could execute. Earlier Code Integrity events also recorded blocks for generated BepInEx interop DLLs.

Keep Smart App Control enabled if that is your preference. The current blocked build cannot provide anti-cheat protection on that installation. Rebuilding the same unsigned source or changing the mod's configuration does not resolve the recorded trust block. Microsoft provides [no per-app Smart App Control exception](https://support.microsoft.com/en-gb/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions).

## Collect a report without changing security settings

From the source repository, run this in PowerShell, using your actual game folder:

```powershell
.\scripts\Get-AppControlReport.ps1 -GameDir 'C:\XboxGames\Among Us\Content'
```

The script prints JSON containing executable version metadata when available, BepInEx log details, file hashes and signature status, and matching blocks from up to 250 recent Code Integrity records. Include the game version from the main menu when sharing a report. The script does not execute game DLLs, sign files, change certificate stores, or change security policies. Missing files and inaccessible event logs are reported explicitly. An empty event list does not establish that Windows will allow a file.

Check the report for personal paths before sharing it. If the report script itself is blocked, use Event Viewer manually: **Applications and Services Logs > Microsoft > Windows > CodeIntegrity > Operational**. Event **3077** records an enforced block; correlated **3089** events provide signature details. See [Microsoft's event reference](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/operations/event-id-explanations).

## Publisher requirements

A trusted signed release is still pending. No code-signing certificate or signing-service account is currently configured for this project.

1. Obtain a code-signing certificate from a trusted provider, or configure a supported signing service. Microsoft documents both routes in [Signing for Smart App Control](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control). A self-signed certificate or a C# strong name does not supply the required publisher trust. Keep private keys and signing credentials outside this repository.
2. Build and finish publishing the binaries, then sign the final copies of **both** `AmongUs.AntiCheat.dll` and `AntiCheat.Core.dll`. Sign the optional notifier executable separately if distributing it. Rebuilding or changing a signed file requires signing the new file again.
3. Verify every signature and timestamp, then package those signed copies into the edition's release ZIP. The existing build scripts do not sign files; running them again creates a fresh unsigned package.
4. Resolve any remaining loader or generated-assembly blocks with the relevant upstream maintainers. Signing this project's files alone does not establish trust for BepInEx dependencies. Test loading with Smart App Control enabled, then test the mod in a private lobby before claiming compatibility.

BepInEx can [regenerate interop assemblies when its inputs change](https://github.com/BepInEx/BepInEx/blob/master/Runtimes/Unity/BepInEx.Unity.IL2CPP/Il2CppInteropManager.cs). A signature on one generated copy would not cover later regenerated files. This project does not automatically sign third-party files or redistribute generated game assemblies.

### SignTool example for a publisher with a certificate

This example requires the Windows SDK's `signtool.exe` on `PATH` and a provider-issued code-signing certificate with an accessible private key in **Current User > Personal**. A signing service may require a different workflow. See [Microsoft's SignTool documentation](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool).

First extract a freshly built mod ZIP into a **new staging folder**, such as `dist/signing-staging/MicrosoftStore`. Replace both placeholders below with your certificate's public thumbprint and your provider's RFC 3161 timestamp URL. Run this against the staging copies, with the game closed:

```powershell
$ErrorActionPreference = 'Stop'
$signTool = (Get-Command signtool.exe -ErrorAction Stop).Source
$certificateThumbprint = 'REPLACE_WITH_CERTIFICATE_THUMBPRINT'
$timestampUrl = 'https://REPLACE_WITH_PROVIDER_TIMESTAMP_ENDPOINT'
$stagedPluginDir = '.\dist\signing-staging\MicrosoftStore\BepInEx\plugins\AntiCheat'

foreach ($name in @('AmongUs.AntiCheat.dll', 'AntiCheat.Core.dll')) {
    $file = (Resolve-Path -LiteralPath (Join-Path $stagedPluginDir $name)).Path
    & $signTool sign /s My /sha1 $certificateThumbprint /fd SHA256 /tr $timestampUrl /td SHA256 $file
    if ($LASTEXITCODE -ne 0) { throw "Signing or timestamping failed: $name" }

    & $signTool verify /pa /all /tw $file
    if ($LASTEXITCODE -ne 0) { throw "Signature verification failed: $name" }
    if ((Get-AuthenticodeSignature -LiteralPath $file).Status -ne 'Valid') {
        throw "Authenticode validation failed: $name"
    }
}
```

Stop if any step fails; do not upload that staging folder as a signed release. After success, compress the entire staged package, preserving its `BepInEx/plugins/AntiCheat/` layout. A locally valid signature is useful evidence, but does not guarantee acceptance by Smart App Control on another device. Keep the compatibility claim pending until the complete loader, generated dependencies, and mod have been tested together.
