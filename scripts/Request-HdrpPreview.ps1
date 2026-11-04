#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PluginDirectory,
    [Parameter(Mandatory)] [ValidateSet('calm','full','clear')] [string] $Command
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$directory = (Resolve-Path -LiteralPath $PluginDirectory).ProviderPath
foreach ($name in @('GojoBracken.dll', 'gojobracken-hdrp-material-check')) {
    if (-not (Test-Path -LiteralPath (Join-Path $directory $name) -PathType Leaf)) {
        throw "Preview install is missing $name in $directory."
    }
}
$request = Join-Path $directory 'gojobracken-preview.request'
if (Test-Path -LiteralPath $request) {
    throw 'A request is pending. Return to the game and check BepInEx LogOutput.log before retrying.'
}
$temporary = Join-Path $directory ('preview-' + [Guid]::NewGuid().ToString('N') + '.tmp')
try {
    [IO.File]::WriteAllText($temporary, $Command)
    [IO.File]::Move($temporary, $request)
} finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary }
}
Write-Output "Queued $Command. Return to the game and allow the preview to settle."
