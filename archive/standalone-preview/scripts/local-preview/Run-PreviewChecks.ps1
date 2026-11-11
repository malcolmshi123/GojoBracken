#requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$reportDirectory = Join-Path $env:USERPROFILE 'AppData\LocalLow\GojoBrackenLocalTests\GojoBracken Local Preview'
$runStarted = [DateTime]::UtcNow
$process = Start-Process -FilePath (Join-Path $PSScriptRoot 'GojoBrackenPreview.exe') -ArgumentList '--smoke-test' -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'The preview timed out after two minutes. No game process was touched.' }
$path = Join-Path $reportDirectory 'preview-report.json'
if (-not (Test-Path -LiteralPath $path)) { throw "No report was generated. Check $reportDirectory\Player.log." }
$report = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
if ($process.ExitCode -ne 0 -or -not $report.smokePassed -or [DateTime]::Parse($report.generatedUtc).ToUniversalTime() -lt $runStarted) {
    throw "Preview smoke test failed or returned a stale report. Read $path and Player.log."
}
Copy-Item -LiteralPath $path -Destination (Join-Path $reportDirectory 'smoke-report.json') -Force
Copy-Item -LiteralPath (Join-Path $reportDirectory 'preview-render.png') -Destination (Join-Path $reportDirectory 'smoke-render.png') -Force
Write-Output "Smoke test passed. Report: $(Join-Path $reportDirectory 'smoke-report.json')"
Write-Output "Render: $(Join-Path $reportDirectory 'smoke-render.png')"
Write-Output 'This checks standalone prefabs and simulated cosmetics, not Lethal Company integration or the original voice.'
