#requires -Version 5.1
[CmdletBinding()]
param(
    [string] $ArchivePath = (Join-Path $env:USERPROFILE 'Downloads\gojo-satoru-dance-jujutsu-kaisen.zip')
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# This checkpoint accepts only the archive whose structure was inspected.
$expectedHash = 'B0969E72C9BD3C3D0D48BFF1DB6A9033297D8CF128552F51C66C8B355985F8A9'
if ((Get-FileHash -LiteralPath $ArchivePath -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'Archive differs from the inspected candidate. Inspect it before updating the expected hash.'
}
$repository = Split-Path -Parent $PSScriptRoot
$destination = [IO.Path]::GetFullPath((Join-Path $repository 'UnityProject\Assets\LocalModelStudy'))
if (Test-Path -LiteralPath $destination) {
    throw "Local study already exists: $destination. Existing work will not be overwritten."
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ArchivePath).ProviderPath)
try {
    $expectedEntries = @(
        'source/gojo_menu.gltf',
        'textures/mat0_.png_3.png',
        'textures/mat0_001_.png_2.png',
        'textures/mat0_003_.png_0.png',
        'textures/mat0_002_.png_1.png'
    )
    if ($zip.Entries.Count -ne $expectedEntries.Count) { throw 'Unexpected archive entry count.' }
    foreach ($entry in $zip.Entries) {
        if ($expectedEntries -cnotcontains $entry.FullName) { throw "Unexpected entry: $($entry.FullName)" }
        $target = [IO.Path]::GetFullPath((Join-Path $destination $entry.FullName))
        if (-not $target.StartsWith(($destination + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Archive entry escapes the local study directory.'
        }
    }
    foreach ($entry in $zip.Entries) {
        $target = Join-Path $destination $entry.FullName
        New-Item -ItemType Directory -Force ([IO.Path]::GetDirectoryName($target)) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $false)
    }
} finally {
    $zip.Dispose()
}
Write-Output "Prepared ignored local candidate: $destination"
Write-Output 'In Unity, allow package resolution/import to finish, then choose GojoBracken > Create local Gojo model study.'
