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
$target = [IO.Path]::GetFullPath($Destination)
if (!$target.StartsWith(([IO.Path]::GetFullPath($root).TrimEnd('\')+'\'), [StringComparison]::OrdinalIgnoreCase)) { throw 'Use a workspace output directory.' }
if (Test-Path -LiteralPath $target) { throw 'Use a fresh bounded-probe directory.' }
New-Item -ItemType Directory -Path $target | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
Push-Location -LiteralPath $root
try {
 & $compiler /nologo /platform:x86 /main:ClassicStageOrderOracle "/out:$PSScriptRoot\ClassicStageOrderOracle.exe" (Join-Path $PSScriptRoot 'ClassicTargetOrderOracle.cs') (Join-Path $PSScriptRoot 'ClassicStageOrderOracle.cs')
 if ($LASTEXITCODE -ne 0) { throw 'Native stage helper build failed.' }
 & $compiler /nologo /platform:x86 "/out:$PSScriptRoot\StageOrderTrace.exe" (Join-Path $PSScriptRoot 'StageOrderTrace.cs')
 if ($LASTEXITCODE -ne 0) { throw 'Owned stage debugger build failed.' }
 & (Join-Path $PSScriptRoot 'StageOrderTrace.exe') (Join-Path $PSScriptRoot 'ClassicStageOrderOracle.exe') (Join-Path $target 'Trace') $exe $target
 if ($LASTEXITCODE -ne 0) { throw 'Owned stage probe failed; see its trace.' }
 Copy-Item -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe') -Destination $target
 & $compiler /nologo /platform:x64 /r:System.Drawing.dll "/r:$packagePath\BattleshipLab.exe" "/out:$target\CheckStageOrderTraces.exe" (Join-Path $PSScriptRoot 'Check-StageOrderTraces.cs')
 if ($LASTEXITCODE -ne 0) { throw 'Delivered selector/stage comparison build failed.' }
 & (Join-Path $target 'CheckStageOrderTraces.exe') (Join-Path $target 'Native-Stage-Order-Cases.csv') (Join-Path $target 'Native-Stage-Order-Draws.csv') (Join-Path $target 'Check.txt')
 if ($LASTEXITCODE -ne 0) { throw 'Native shared-state/phase comparison failed.' }
 Get-FileHash -LiteralPath (Join-Path $target 'Native-Stage-Order-Cases.csv'), (Join-Path $target 'Native-Stage-Order-Draws.csv')
} finally { Pop-Location }
