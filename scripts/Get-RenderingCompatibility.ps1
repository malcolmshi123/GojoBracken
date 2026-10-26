#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $GameDirectory,
    [string] $BepInExLogPath,
    [string] $PlayerLogPath,
    [string] $OutputPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $GameDirectory).ProviderPath
$managed = Join-Path $root 'Lethal Company_Data\Managed'
if (-not (Test-Path -LiteralPath (Join-Path $managed 'Assembly-CSharp.dll') -PathType Leaf)) {
    throw 'Pass the installed Lethal Company folder, not a mod profile.'
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) { $OutputPath = Join-Path (Get-Location) 'rendering-compatibility.json' }
if ([string]::IsNullOrWhiteSpace($BepInExLogPath)) { $BepInExLogPath = Join-Path $root 'BepInEx\LogOutput.log' }
if ([string]::IsNullOrWhiteSpace($PlayerLogPath)) { $PlayerLogPath = Join-Path $env:USERPROFILE 'AppData\LocalLow\ZeekerssRBLX\Lethal Company\Player.log' }
$files = @(foreach ($name in @('Assembly-CSharp.dll', 'Unity.RenderPipelines.Core.Runtime.dll',
    'Unity.RenderPipelines.HighDefinition.Runtime.dll', 'Unity.RenderPipelines.Universal.Runtime.dll')) {
    $path = Join-Path $managed $name
    $present = Test-Path -LiteralPath $path -PathType Leaf
    $version = if ($present) { [Diagnostics.FileVersionInfo]::GetVersionInfo($path) } else { $null }
    [ordered]@{
        name = $name; present = $present
        fileVersion = if ($present) { $version.FileVersion } else { $null }
        productVersion = if ($present) { $version.ProductVersion } else { $null }
        sha256 = if ($present) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash } else { $null }
    }
})
$logs = @(foreach ($entry in @(@{ label = 'BepInEx'; path = $BepInExLogPath }, @{ label = 'Player'; path = $PlayerLogPath })) {
    $present = Test-Path -LiteralPath $entry.path -PathType Leaf
    $lines = @()
    $modified = $null
    if ($present) {
        $modified = (Get-Item -LiteralPath $entry.path).LastWriteTimeUtc.ToString('o')
        # Export only rendering evidence, not complete logs or player/account information.
        $lines = @(Select-String -LiteralPath $entry.path -Pattern '\[Diagnostics\] Render pipeline=|\[RigReport\].*pipeline=|RenderPipelineAsset|Graphics API|GfxDevice:|Renderer:|Direct3D:|Color ?Space' |
            Select-Object -Last 30 | ForEach-Object { $_.Line })
    }
    [ordered]@{ source = $entry.label; present = $present; modifiedUtc = $modified; renderingLines = $lines }
})
$confirmed = @($logs | ForEach-Object { $_.renderingLines } | Where-Object {
    $_ -match '\[Diagnostics\] Render pipeline=(?!<null)' })
$report = [ordered]@{
    schemaVersion = 1; kind = 'offline-rendering-evidence'; generatedUtc = [DateTime]::UtcNow.ToString('o')
    powershellVersion = $PSVersionTable.PSVersion.ToString(); files = $files; logs = $logs
    hasHistoricalPipelineObservation = $confirmed.Count -gt 0
    limits = @('No game launch, assembly execution, game writes, or pipeline conversion.',
        'Assembly presence/version does not establish active pipeline or exact Unity package patch.',
        'Existing logs are historical evidence; check their timestamps and game assembly hash.',
        'No matching pipeline line means unknown, not built-in. Color space and shader compatibility may remain unknown.')
}
$fullOutput = [IO.Path]::GetFullPath($OutputPath)
if (Test-Path -LiteralPath $fullOutput) { throw 'Output already exists. Choose a fresh OutputPath to preserve prior evidence.' }
$parent = Split-Path -Parent $fullOutput
if (-not (Test-Path -LiteralPath $parent -PathType Container)) { throw 'The output directory must already exist.' }
[IO.File]::WriteAllText($fullOutput, ($report | ConvertTo-Json -Depth 8))
Write-Output "Rendering evidence saved: $fullOutput"
