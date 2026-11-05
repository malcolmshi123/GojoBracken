#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $BundleDirectory,
    [Parameter(Mandatory)] [string] $IconPath,
    [string] $OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$bundleRoot = (Resolve-Path -LiteralPath $BundleDirectory).ProviderPath
$bundle = Join-Path $bundleRoot 'gojobracken'
$credits = Join-Path $bundleRoot 'ASSET-CREDITS.txt'
$dll = Join-Path $repo 'src/GojoBracken/bin/Release/netstandard2.1/GojoBracken.dll'
foreach ($file in @($bundle, $credits, $dll, $IconPath)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or (Get-Item -LiteralPath $file).Length -eq 0) {
        throw "Missing or empty package input: $file"
    }
}
if ([string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $credits -Raw))) {
    throw 'ASSET-CREDITS.txt must record the sources and permissions for the packaged assets.'
}

Add-Type -AssemblyName System.Drawing
$icon = (Resolve-Path -LiteralPath $IconPath).ProviderPath
$image = [Drawing.Image]::FromFile($icon)
try {
    if ($image.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid -or
        $image.Width -ne 256 -or $image.Height -ne 256) {
        throw 'The package icon must be a 256 by 256 PNG.'
    }
} finally { $image.Dispose() }

$version = [Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3)
$manifest = [ordered]@{
    name = 'GojoBracken'
    version_number = $version
    website_url = 'https://github.com/malcolmshi123/GojoBracken'
    description = 'Replaces the Bracken visually with a standing Gojo, an anger-scaled aura, and a spatial aggro voice line.'
    dependencies = @('BepInEx-BepInExPack-5.4.2305')
}
$files = [ordered]@{
    'README.md' = (Join-Path $repo 'docs/packaging.md')
    'icon.png' = $icon
    'LICENSE' = (Join-Path $repo 'LICENSE')
    'ASSET-CREDITS.txt' = $credits
    'BepInEx/plugins/GojoBracken/GojoBracken.dll' = $dll
    'BepInEx/plugins/GojoBracken/gojobracken' = $bundle
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repo 'dist'
}
$output = (New-Item -ItemType Directory -Path $OutputDirectory -Force).FullName
$destination = Join-Path $output "GojoBracken-$version.zip"
if (Test-Path -LiteralPath $destination) {
    throw "Package already exists: $destination"
}
$pending = Join-Path $output ([Guid]::NewGuid().ToString('N') + '.tmp')
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
try {
    $archive = [IO.Compression.ZipFile]::Open($pending, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $entry = $archive.CreateEntry('manifest.json')
        $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
        try { $writer.WriteLine(($manifest | ConvertTo-Json -Depth 3)) } finally { $writer.Dispose() }
        foreach ($entry in $files.GetEnumerator()) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $entry.Value, $entry.Key) | Out-Null
        }
    } finally { $archive.Dispose() }
    [IO.File]::Move($pending, $destination)
} finally {
    if (Test-Path -LiteralPath $pending -PathType Leaf) { Remove-Item -LiteralPath $pending }
}
Get-FileHash -LiteralPath $destination -Algorithm SHA256

