#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $ManagedDirectory,
    [Parameter(Mandatory = $true)] [string] $CecilPath,
    [string] $OutputPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$assemblyPath = Join-Path $ManagedDirectory 'Assembly-CSharp.dll'
if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) { throw 'Assembly-CSharp.dll was not found.' }
if (-not (Test-Path -LiteralPath $CecilPath -PathType Leaf)) { throw 'Pass Mono.Cecil.dll from the existing BepInEx core directory.' }
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $repository = Split-Path -Parent $PSScriptRoot
    $OutputPath = Join-Path $repository ('artifacts\offline-game-metadata-' + [Guid]::NewGuid().ToString('N') + '.json')
}
# Only the metadata reader is loaded. The game assembly is never passed to Assembly.Load/Add-Type.
Add-Type -Path (Resolve-Path -LiteralPath $CecilPath).ProviderPath
$reader = New-Object Mono.Cecil.ReaderParameters
$reader.ReadSymbols = $false
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $assemblyPath).ProviderPath, $reader)
try {
    $selected = @($assembly.MainModule.Types | Where-Object { $_.Name -in @('FlowermanAI', 'EnemyAI') })
    if ($selected.Count -eq 0) { throw 'FlowermanAI and EnemyAI were not found in this assembly.' }
    $types = foreach ($type in $selected) {
        $fields = @($type.Fields | ForEach-Object {
            [ordered]@{
                name = $_.Name
                type = $_.FieldType.FullName
                isStatic = $_.IsStatic
                attributes = @($_.CustomAttributes | ForEach-Object { $_.AttributeType.FullName })
            }
        })
        $methods = @(foreach ($method in $type.Methods) {
            $calls = @()
            $writes = @()
            $strings = @()
            if ($method.HasBody) {
                foreach ($instruction in $method.Body.Instructions) {
                    $opcode = $instruction.OpCode.Code.ToString()
                    if ($opcode -in @('Call', 'Callvirt', 'Newobj')) { $calls += $instruction.Operand.FullName }
                    elseif ($opcode -in @('Stfld', 'Stsfld')) { $writes += $instruction.Operand.FullName }
                    elseif ($opcode -eq 'Ldstr') { $strings += [string]$instruction.Operand }
                }
            }
            [ordered]@{
                signature = $method.FullName
                calls = @($calls | Sort-Object -Unique)
                writtenFields = @($writes | Sort-Object -Unique)
                stringLiterals = @($strings | Sort-Object -Unique)
            }
        })
        [ordered]@{ name = $type.FullName; baseType = $type.BaseType.FullName; fields = $fields; methods = $methods }
    }
    $report = [ordered]@{
        schemaVersion = 1
        kind = 'offline-managed-metadata'
        generatedUtc = [DateTime]::UtcNow.ToString('O')
        file = 'Assembly-CSharp.dll'
        sha256 = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash
        assembly = $assembly.Name.FullName
        mvid = $assembly.MainModule.Mvid.ToString()
        limits = 'No game launch or game-assembly execution. No prefab bones, runtime render pipeline, state meanings, anger bounds, or replication are established. Calls/literals are metadata, not a control-flow analysis.'
        types = @($types)
    }
    $json = $report | ConvertTo-Json -Depth 12
} finally {
    $assembly.Dispose()
}
$fullOutput = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Force ([IO.Path]::GetDirectoryName($fullOutput)) | Out-Null
[IO.File]::WriteAllText($fullOutput, $json, (New-Object Text.UTF8Encoding $false))
Write-Output "Offline metadata report: $fullOutput"
