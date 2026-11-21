#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PackagePath,
    [Parameter(Mandatory)] [string] $BundleDirectory,
    [Parameter(Mandatory)] [string] $CecilPath,
    [string] $ReportPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $ReportPath) { $ReportPath = [IO.Path]::ChangeExtension([IO.Path]::GetFullPath($PackagePath), '.json') }
[IO.File]::WriteAllText([IO.Path]::GetFullPath($ReportPath), '{"packageVerified":false,"gameplayValidated":false}')
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -Path (Resolve-Path -LiteralPath $CecilPath).Path
function Hash([byte[]] $Bytes) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-', '') } finally { $sha.Dispose() }
}
$zip = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($PackagePath))
function Read-Entry([string] $Name) {
    $entry = $zip.GetEntry($Name)
    if ($null -eq $entry) { throw "Missing package entry: $Name" }
    $stream = $entry.Open(); $memory = [IO.MemoryStream]::new()
    try { $stream.CopyTo($memory); return ,$memory.ToArray() } finally { $stream.Dispose(); $memory.Dispose() }
}
try {
    $files = [ordered]@{
        'README.md' = Join-Path $repo 'README.md'
        'LICENSE' = Join-Path $repo 'LICENSE'
        'ASSET-CREDITS.txt' = Join-Path $BundleDirectory 'ASSET-CREDITS.txt'
        'BepInEx/config/FreakyFriday.GojoBracken.cfg' = Join-Path $repo 'packaging/release/FreakyFriday.GojoBracken.cfg'
        'BepInEx/plugins/GojoBracken/GojoBracken.dll' = Join-Path $repo 'src/GojoBracken/bin/Release/netstandard2.1/GojoBracken.dll'
        'BepInEx/plugins/GojoBracken/gojobracken' = Join-Path $BundleDirectory 'gojobracken'
    }
    $names = @($files.Keys) + @('manifest.json', 'icon.png')
    if ($zip.Entries.Count -ne $names.Count -or @($zip.Entries | Where-Object { $_.FullName -cnotin $names }).Count) { throw 'Unexpected or duplicate release package entries.' }
    foreach ($file in $files.GetEnumerator()) {
        if ((Hash (Read-Entry $file.Key)) -ne (Get-FileHash -LiteralPath $file.Value).Hash) { throw "Packaged file differs from the release input: $($file.Key)" }
    }
    $manifest = [Text.Encoding]::UTF8.GetString((Read-Entry 'manifest.json')) | ConvertFrom-Json
    $dll = Read-Entry 'BepInEx/plugins/GojoBracken/GojoBracken.dll'
    $memory = [IO.MemoryStream]::new($dll, $false)
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($memory)
    try {
        $version = $assembly.Name.Version.ToString(3)
        $pluginVersion = ($assembly.MainModule.GetType('GojoBracken.Plugin').Fields | Where-Object Name -eq 'PluginVersion').Constant
    } finally { $assembly.Dispose(); $memory.Dispose() }
    [xml] $project = Get-Content -LiteralPath (Join-Path $repo 'src/GojoBracken/GojoBracken.csproj') -Raw
    $sourceVersion = (Select-Xml -Xml $project -XPath '/Project/PropertyGroup/Version').Node.InnerText
    if ($version -ne $pluginVersion -or $version -ne $manifest.version_number -or $version -ne $sourceVersion -or
        [IO.Path]::GetFileName($PackagePath) -cne "GojoBracken-$version.zip") { throw 'Package filename, manifest, DLL, plugin and project versions must agree.' }
    if ($manifest.name -ne 'GojoBracken' -or @($manifest.dependencies).Count -ne 1 -or $manifest.dependencies[0] -ne 'BepInEx-BepInExPack-5.4.2305') { throw 'Unexpected package identity/dependencies.' }
    Add-Type -AssemblyName System.Drawing
    $memory = [IO.MemoryStream]::new((Read-Entry 'icon.png'), $false)
    $icon = [Drawing.Image]::FromStream($memory)
    try {
        if ($icon.Width -ne 256 -or $icon.Height -ne 256 -or $icon.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid) { throw 'Invalid release icon.' }
    } finally { $icon.Dispose(); $memory.Dispose() }
    $rules = Get-Content -LiteralPath (Join-Path $repo 'shared/GojoBracken.Presentation/Runtime/BrackenV81Presentation.cs') -Raw
    $mvid = [regex]::Match($rules, 'SupportedMvid\s*=\s*new Guid\("([^"]+)"\)').Groups[1].Value
    if (-not $mvid) { throw 'Supported game MVID was not found in the presentation contract.' }
    $record = [ordered]@{
        packageVerified = $true; gameplayValidated = $false
        pluginVersion = $version; gameMvid = $mvid
        packageSha256 = (Get-FileHash -LiteralPath $PackagePath).Hash
        dllSha256 = Hash $dll
        bundleSha256 = Hash (Read-Entry 'BepInEx/plugins/GojoBracken/gojobracken')
        configSha256 = Hash (Read-Entry 'BepInEx/config/FreakyFriday.GojoBracken.cfg')
        creditsSha256 = Hash (Read-Entry 'ASSET-CREDITS.txt')
    }
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($ReportPath), ($record | ConvertTo-Json))
} finally { $zip.Dispose() }
Write-Output "Release package verified: $PackagePath. Report: $ReportPath"
