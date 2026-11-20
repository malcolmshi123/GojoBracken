#requires -Version 5.1
[CmdletBinding()]
param(
    [string] $ApprovedPackage,
    [string] $ExpectedManifest,
    [string] $OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $ApprovedPackage) { $ApprovedPackage = Join-Path $repo 'artifacts/game-test/kit/GojoBracken-LocalTest.zip' }
if (-not $ExpectedManifest) { $ExpectedManifest = Join-Path $repo 'artifacts/game-test/kit/expected.json' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'artifacts/phase-preview' }
$expected = Get-Content -LiteralPath $ExpectedManifest -Raw | ConvertFrom-Json
if ($expected.pluginVersion -ne '0.2.7') { throw 'This private kit requires the approved 0.2.7 candidate.' }
if ((Get-FileHash -LiteralPath $ApprovedPackage).Hash -ne $expected.packageSha256) { throw 'Approved package hash mismatch.' }
if ((Get-FileHash -LiteralPath (Join-Path $repo 'artifacts/blender/Gojo.blend')).Hash -ne $expected.modelSha256) { throw 'Approved Blender model hash mismatch.' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
$kit = Join-Path $output 'kit'
$destination = Join-Path $output 'GojoBracken-PhasePreviewKit.zip'
if ((Test-Path -LiteralPath $kit) -or (Test-Path -LiteralPath $destination)) { throw 'Preview kit already exists; pass a fresh OutputDirectory.' }
New-Item -ItemType Directory -Path (Join-Path $kit 'payload') -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($ApprovedPackage))
try {
    $entries = @{
        'BepInEx/plugins/GojoBracken/GojoBracken.dll' = 'payload/GojoBracken.dll'
        'BepInEx/plugins/GojoBracken/gojobracken' = 'payload/gojobracken-hdrp-material-check'
        'ASSET-CREDITS.txt' = 'ASSET-CREDITS.txt'
        'LICENSE' = 'LICENSE'
    }
    foreach ($item in $entries.GetEnumerator()) {
        $entry = $archive.GetEntry($item.Key)
        if ($null -eq $entry) { throw "Approved package is missing $($item.Key)." }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $kit $item.Value))
    }
} finally { $archive.Dispose() }
$dll = Join-Path $kit 'payload/GojoBracken.dll'
$bundle = Join-Path $kit 'payload/gojobracken-hdrp-material-check'
if ((Get-FileHash -LiteralPath $dll).Hash -ne $expected.dllSha256 -or
    (Get-FileHash -LiteralPath $bundle).Hash -ne $expected.bundleSha256) { throw 'Preview payload differs from the approved candidate.' }
if ([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3) -ne '0.2.7') { throw 'DLL version mismatch.' }
foreach ($name in @('Request-HdrpPreview.ps1', 'Test-GameProfile.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $kit $name)
}
Copy-Item -LiteralPath (Join-Path $repo 'packaging/preview/FreakyFriday.GojoBracken.cfg') -Destination $kit
Copy-Item -LiteralPath (Join-Path $repo 'docs/phase-preview.md') -Destination (Join-Path $kit 'PREVIEW.md')
$expected | Add-Member -NotePropertyName sourcePackageSha256 -NotePropertyValue $expected.packageSha256 -Force
$expected.PSObject.Properties.Remove('packageSha256')
$expected | Add-Member -NotePropertyName mode -NotePropertyValue 'RenderPreview' -Force
$expected | Add-Member -NotePropertyName simulatedChecksOnly -NotePropertyValue $true -Force
$expected | Add-Member -NotePropertyName testCheckpoint -NotePropertyValue 'Simulated phase-cycle aura/audio preview; no live AI, cleanup or kill/body validation' -Force
$expected | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $kit 'expected.json')
$zip = [IO.Compression.ZipFile]::Open($destination, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in (Get-ChildItem -LiteralPath $kit -Recurse -File)) {
        $name = $file.FullName.Substring($kit.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $name) | Out-Null
    }
} finally { $zip.Dispose() }
Get-FileHash -LiteralPath $destination
Write-Output "Private simulated preview kit: $destination"
