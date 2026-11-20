#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$shader = Join-Path $repo 'scripts/hdrp/Flame.shader'
$manifest = Join-Path $repo 'artifacts/unity/mod-bundle/build.json'
$shaderBytes = [IO.File]::ReadAllBytes($shader)
$manifestBytes = [IO.File]::ReadAllBytes($manifest)
$run = Join-Path $repo ('artifacts/bundle-provenance-checks/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
function Check-Rejection([string] $Name) {
    $manifestHash = (Get-FileHash -LiteralPath $manifest).Hash
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'RemoteSigned', '-File',
        ('"' + (Join-Path $repo 'scripts/Build-GojoBundle.ps1') + '"'), '-VerifyOnly')
    $output = Join-Path $run "$Name-stdout.log"
    $errors = Join-Path $run "$Name-stderr.log"
    $process = Start-Process powershell.exe -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $output -RedirectStandardError $errors
    if ($process.ExitCode -eq 0 -or -not (Select-String -LiteralPath $errors -SimpleMatch -Quiet 'Bundle build provenance differs')) {
        throw "Bundle provenance was not rejected: $Name. Read $run."
    }
    if ((Get-FileHash -LiteralPath $manifest).Hash -ne $manifestHash) {
        throw 'Verification rewrote the existing build provenance.'
    }
}
try {
    [IO.File]::AppendAllText($shader, "`n// Synthetic pending shader edit for provenance rejection.`n")
    Check-Rejection 'pending-shader'
    [IO.File]::WriteAllBytes($shader, $shaderBytes)
    $record = [Text.Encoding]::UTF8.GetString($manifestBytes) | ConvertFrom-Json
    $record.bundleSha256 = '0' * 64
    [IO.File]::WriteAllText($manifest, ($record | ConvertTo-Json))
    Check-Rejection 'stale-bundle-hash'
    $record = [Text.Encoding]::UTF8.GetString($manifestBytes) | ConvertFrom-Json
    $record.PSObject.Properties.Remove('flameShaderSha256')
    [IO.File]::WriteAllText($manifest, ($record | ConvertTo-Json))
    Check-Rejection 'missing-shader-provenance'
} finally {
    [IO.File]::WriteAllBytes($shader, $shaderBytes)
    [IO.File]::WriteAllBytes($manifest, $manifestBytes)
}
Write-Output "BUNDLE PROVENANCE CHECKS PASS: pending shaders, stale bundle hashes and missing shader provenance rejected without rewriting build metadata. Logs: $run"
