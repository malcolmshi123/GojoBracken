#requires -Version 5.1
[CmdletBinding()]
param(
    [string] $UnityEditorPath = 'C:\Program Files\Unity 2022.3.62f2\Editor\Unity.exe',
    [string] $UvPath = (Join-Path $env:USERPROFILE '.local\bin\uvx.exe'),
    [switch] $VerifyOnly
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repo 'artifacts\unity'
$project = Join-Path $output 'project'
$source = Join-Path $output 'source'
$package = Join-Path $source 'package'
$sourceZip = Join-Path $repo 'artifacts\blender\source\GojoBracken-0.2.7.zip'
if (-not (Test-Path -LiteralPath (Join-Path $project 'Assets\Gojo\Gojo.prefab'))) { throw 'Run Import-BlenderModel.ps1 first.' }
if (-not (Test-Path -LiteralPath $UnityEditorPath)) { throw 'Pass the Unity 2022.3.62f2 Editor path.' }
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($UnityEditorPath).ProductVersion
if ($version -notmatch '^2022\.3\.62f2(?=$|[\s_(])') { throw "Expected Unity 2022.3.62f2; found $version." }
if (-not (Test-Path -LiteralPath $UvPath)) { throw 'uvx is required to extract and verify encoded resource streams.' }
if ((Get-FileHash -LiteralPath $sourceZip -Algorithm SHA256).Hash -ne 'E9DABC5B5E7ECFD5073A64C895DE05182C69516A8D0D0E30E5C987B020F43A4A') { throw 'The published source package hash changed.' }
if (-not (Test-Path -LiteralPath $package)) {
    New-Item -ItemType Directory -Force $source | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($sourceZip, $package)
}
$releasedBundle = Join-Path $package 'BepInEx\plugins\GojoBracken\gojobracken'
if ((Get-FileHash -LiteralPath $releasedBundle -Algorithm SHA256).Hash -ne '1A4AC1AEF40C2188BB0F3BAFAD24492F79EE01A1128D2F97D06FBECC40476F1B') { throw 'The released source bundle hash changed.' }
$data = Get-Content -LiteralPath (Join-Path $output 'Gojo.json') -Raw
if ($data -notmatch '"sourceSha256":"([a-f0-9]{64})"') { throw 'Model source hash is missing.' }
if ($Matches[1] -ine (Get-FileHash -LiteralPath (Join-Path $repo 'artifacts\blender\Gojo.blend')).Hash) { throw 'The exported model no longer matches Gojo.blend.' }
$bundle = Join-Path $output 'mod-bundle\gojobracken'
$buildManifest = Join-Path $output 'mod-bundle\build.json'
if ($VerifyOnly) {
    if (-not (Test-Path -LiteralPath $buildManifest)) { throw 'Bundle build provenance differs or is missing. Rebuild the bundle first.' }
    $built = Get-Content -LiteralPath $buildManifest -Raw | ConvertFrom-Json
    if (-not $built.PSObject.Properties['flameShaderSha256'] -or -not $built.PSObject.Properties['bundleSha256'] -or
        $built.flameShaderSha256 -ne (Get-FileHash -LiteralPath (Join-Path $repo 'scripts/hdrp/Flame.shader')).Hash -or
        $built.bundleSha256 -ne (Get-FileHash -LiteralPath $bundle).Hash) {
        throw 'Bundle build provenance differs from the current shader or bundle. Rebuild the bundle first.'
    }
}
$editor = Join-Path $project 'Assets\Editor'
Copy-Item -Path (Join-Path $PSScriptRoot 'bundle\*.cs') -Destination $editor -Force
$runtime = Join-Path $project 'Assets\GojoRuntime'
New-Item -ItemType Directory -Force $runtime | Out-Null
foreach ($name in @('GojoAssetBundle.cs', 'BrackenCosmeticController.cs', 'BrackenModelAttachment.cs', 'BrackenVisibilityReader.cs', 'GojoAssetContract.cs', 'BrackenStateReader.cs', 'BrackenV81Presentation.cs')) {
    $pluginSource = Join-Path $repo "src\GojoBracken\$name"
    $original = if (Test-Path -LiteralPath $pluginSource) { $pluginSource } else { Join-Path $repo "shared\GojoBracken.Presentation\Runtime\$name" }
    Copy-Item -LiteralPath $original -Destination $runtime -Force
    $oldEditorCopy = Join-Path $editor $name
    if (Test-Path -LiteralPath $oldEditorCopy) { Remove-Item -LiteralPath $oldEditorCopy -Force }
    if (Test-Path -LiteralPath ($oldEditorCopy + '.meta')) { Remove-Item -LiteralPath ($oldEditorCopy + '.meta') -Force }
}
[IO.File]::WriteAllText((Join-Path $runtime 'EditorVisibility.cs'), '[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Assembly-CSharp-Editor")]')
function Invoke-Unity([string] $Method, [string] $LogName, [string] $Marker) {
    $log = Join-Path $output $LogName
    $arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'), '-buildTarget', 'Win64',
        '-force-d3d11', '-executeMethod', $Method, '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath $UnityEditorPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -SimpleMatch -Quiet -Pattern $Marker)) {
        throw "Unity failed during $Method. Read $log."
    }
}
if (-not $VerifyOnly) {
    & $UvPath --from UnityPy==1.25.4 python (Join-Path $PSScriptRoot 'bundle\bundle_resources.py') $releasedBundle --extract (Join-Path $source 'resources')
    if ($LASTEXITCODE -ne 0) { throw 'Released resource extraction failed.' }
    if (-not (Test-Path -LiteralPath (Join-Path $project 'Assets\GojoBundle'))) {
        Invoke-Unity 'BuildGojoBundle.Prepare' 'prepare-bundle.log' 'GOJO RELEASE ASSETS PREPARED:'
    }
    Invoke-Unity 'BuildGojoBundle.Build' 'build-bundle.log' 'GOJO COMPLETE BUNDLE BUILD PASS:'
} else {
    Invoke-Unity 'GojoBundleChecks.Verify' 'verify-bundle.log' 'GOJO COMPLETE BUNDLE CHECKS PASS'
}
& $UvPath --from UnityPy==1.25.4 python (Join-Path $PSScriptRoot 'bundle\bundle_resources.py') $releasedBundle --compare $bundle
if ($LASTEXITCODE -ne 0) { throw 'Encoded audio no longer matches the release.' }
if (-not $VerifyOnly) { Invoke-Unity 'GojoBracken.CaptureGojoBundle.Run' 'bundle-previews.log' 'GOJO COMPLETE BUNDLE PREVIEWS PASS:' }
if (-not $VerifyOnly) {
    $record = [ordered]@{
        modelSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'artifacts\blender\Gojo.blend')).Hash
        sourcePackageSha256 = (Get-FileHash -LiteralPath $sourceZip).Hash
        sourceBundleSha256 = (Get-FileHash -LiteralPath $releasedBundle).Hash
        bundleSha256 = (Get-FileHash -LiteralPath $bundle).Hash
        creditsSha256 = (Get-FileHash -LiteralPath (Join-Path $output 'mod-bundle\ASSET-CREDITS.txt')).Hash
        unityVersion = '2022.3.62f2'
        hdrpVersion = '14.0.12'
        flameShaderSha256 = (Get-FileHash -LiteralPath (Join-Path $repo 'scripts/hdrp/Flame.shader')).Hash
        resourceToolVersion = 'UnityPy 1.25.4'
        encodedAudioMatchesRelease = $true
    }
    [IO.File]::WriteAllText($buildManifest, ($record | ConvertTo-Json))
}
Get-FileHash -LiteralPath $bundle -Algorithm SHA256
Write-Output "Complete model/aura/audio bundle: $bundle"
