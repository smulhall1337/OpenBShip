param(
 [Parameter(Mandatory=$true)][string]$Executable,
 [Parameter(Mandatory=$true)][string]$Destination,
 [string]$Package
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$exe = (Resolve-Path -LiteralPath $Executable).Path
if ((Get-FileHash -LiteralPath $exe).Hash -ne 'B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569') { throw 'Original executable identity differs.' }
if (!$Package) { $Package = Join-Path $root 'outputs\Battleship-Lab-v0.30' }
$packagePath = (Resolve-Path -LiteralPath $Package).Path
$workspacePrefix = [IO.Path]::GetFullPath($root).TrimEnd('\') + '\'
$target = [IO.Path]::GetFullPath($Destination)
if (!$target.StartsWith($workspacePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Probe output must be inside this workspace.' }
if (Test-Path -LiteralPath $target) { throw 'Use a fresh bounded-probe directory.' }
New-Item -ItemType Directory -Path $target | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
Push-Location -LiteralPath $root
try {
 foreach ($name in @('ClassicTargetOrderOracle', 'TargetOrderTrace')) {
  & $compiler /nologo /platform:x86 "/out:$PSScriptRoot\$name.exe" (Join-Path $PSScriptRoot "$name.cs")
  if ($LASTEXITCODE -ne 0) { throw "Probe helper build failed: $name" }
 }
 & (Join-Path $PSScriptRoot 'TargetOrderTrace.exe') (Join-Path $PSScriptRoot 'ClassicTargetOrderOracle.exe') (Join-Path $target 'Trace') $exe $target
 if ($LASTEXITCODE -ne 0) { throw 'Owned native probe failed; see its trace. No existing game process is accepted.' }
 Copy-Item -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe') -Destination $target
 & $compiler /nologo /platform:x64 /r:System.Drawing.dll "/r:$packagePath\BattleshipLab.exe" "/out:$target\CheckTargetOrderTraces.exe" (Join-Path $PSScriptRoot 'Check-TargetOrderTraces.cs')
 if ($LASTEXITCODE -ne 0) { throw 'Delivered chained-selector comparison build failed.' }
 & (Join-Path $target 'CheckTargetOrderTraces.exe') (Join-Path $target 'Native-Target-Order-Cases.csv') (Join-Path $target 'Native-Target-Order-Draws.csv') (Join-Path $target 'Check.txt')
 if ($LASTEXITCODE -ne 0) { throw 'Native shared-state/target comparison failed.' }
 Get-FileHash -LiteralPath (Join-Path $target 'Native-Target-Order-Cases.csv'), (Join-Path $target 'Native-Target-Order-Draws.csv')
} finally { Pop-Location }
