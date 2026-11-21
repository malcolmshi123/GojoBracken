#requires -Version 5.1
[CmdletBinding()]
param([Parameter(Mandatory)] [string] $CecilPath)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$run = Join-Path $repo ('artifacts/release-checks/' + [guid]::NewGuid().ToString('N'))
$bundle = Join-Path $repo 'artifacts/unity/mod-bundle'
& (Join-Path $repo 'scripts/Build-ThunderstorePackage.ps1') -BundleDirectory $bundle -IconPath (Join-Path $repo 'artifacts/unity/source/package/icon.png') -OutputDirectory $run | Out-Null
$package = (Get-ChildItem -LiteralPath $run -Filter '*.zip').FullName
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($package)
try {
    if (-not $zip.GetEntry('BepInEx/config/FreakyFriday.GojoBracken.cfg')) { throw 'Release package must enable normal attachment on a fresh install.' }
    if ($zip.GetEntry('CHANGELOG.md')) { throw 'Release package must not include CHANGELOG.md.' }
} finally { $zip.Dispose() }
$report = Join-Path $run 'report.json'
function Check([string] $Path, [bool] $Pass, [string] $Name) {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'RemoteSigned', '-File', ('"' + (Join-Path $repo 'scripts/Test-ReleasePackage.ps1') + '"'),
        '-PackagePath', ('"' + $Path + '"'), '-BundleDirectory', ('"' + $bundle + '"'),
        '-CecilPath', ('"' + (Resolve-Path -LiteralPath $CecilPath).Path + '"'), '-ReportPath', ('"' + $report + '"'))
    $process = Start-Process powershell.exe -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $run "$Name.log") -RedirectStandardError (Join-Path $run "$Name-error.log")
    if (($process.ExitCode -eq 0) -ne $Pass) { throw "Unexpected package verification result: $Name" }
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if ($result.packageVerified -ne $Pass -or $result.gameplayValidated) { throw "Stale or incorrect verification report: $Name" }
}
Check $package $true 'valid'
foreach ($change in @('version', 'bundle', 'config', 'extra-dll', 'extra-changelog', 'missing-entry', 'duplicate-entry')) {
    $folder = Join-Path $run $change
    New-Item -ItemType Directory -Path $folder | Out-Null
    $bad = Join-Path $folder ([IO.Path]::GetFileName($package))
    Copy-Item -LiteralPath $package -Destination $bad
    $zip = [IO.Compression.ZipFile]::Open($bad, [IO.Compression.ZipArchiveMode]::Update)
    try {
        $name = switch ($change) {
            version { 'manifest.json' }
            bundle { 'BepInEx/plugins/GojoBracken/gojobracken' }
            config { 'BepInEx/config/FreakyFriday.GojoBracken.cfg' }
            extra-dll { 'BepInEx/plugins/Duplicate/GojoBracken.dll' }
            extra-changelog { 'CHANGELOG.md' }
            missing-entry { 'ASSET-CREDITS.txt' }
            duplicate-entry { 'README.md' }
        }
        $old = $zip.GetEntry($name)
        $text = 'invalid release fixture'
        if ($change -eq 'version') {
            $reader = [IO.StreamReader]::new($old.Open())
            try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
            $manifest.version_number = '999.0.0'
            $text = $manifest | ConvertTo-Json
        }
        if ($old -and $change -ne 'duplicate-entry') { $old.Delete() }
        if ($change -ne 'missing-entry') {
            $entry = $zip.CreateEntry($name)
            $writer = [IO.StreamWriter]::new($entry.Open())
            try { $writer.Write($text) } finally { $writer.Dispose() }
        }
    } finally { $zip.Dispose() }
    Check $bad $false $change
}
Write-Output "RELEASE PACKAGE CHECKS PASS: fresh-install config, exact payload and metadata; version/bundle/config/extra-DLL tampering, unwanted changelog, missing/duplicate entries and stale reports rejected. $run"
