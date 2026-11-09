param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$Destination,[string]$Package)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$exe=(Resolve-Path -LiteralPath $Executable).Path
if((Get-FileHash -LiteralPath $exe).Hash -ne 'B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569'){throw 'Original executable identity differs.'}
if(!$Package){$Package=Join-Path $root 'outputs\Battleship-Lab-v0.31'}
$packagePath=(Resolve-Path -LiteralPath $Package).Path;$target=[IO.Path]::GetFullPath($Destination)
if(!$target.StartsWith(([IO.Path]::GetFullPath($root).TrimEnd('\')+'\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Use a workspace probe directory.'}
if(Test-Path -LiteralPath $target){throw 'Use a fresh probe directory.'}
New-Item -ItemType Directory -Path $target | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
Push-Location -LiteralPath $root
try{
 $sources=@('ClassicTargetOrderOracle.cs','ClassicStageOrderOracle.cs','ClassicDamageOrderOracle.cs','ClassicCombinedShotOracle.cs') | ForEach-Object {Join-Path $PSScriptRoot $_}
 & $compiler /nologo /platform:x86 /main:ClassicCombinedShotOracle "/out:$PSScriptRoot\ClassicCombinedShotOracle.exe" @sources
 if($LASTEXITCODE -ne 0){throw 'Combined helper build failed.'}
 & $compiler /nologo /platform:x86 "/out:$PSScriptRoot\CombinedShotTrace.exe" (Join-Path $PSScriptRoot 'CombinedShotTrace.cs')
 if($LASTEXITCODE -ne 0){throw 'Owned combined debugger build failed.'}
 & (Join-Path $PSScriptRoot 'CombinedShotTrace.exe') (Join-Path $PSScriptRoot 'ClassicCombinedShotOracle.exe') (Join-Path $target 'Trace') $exe $target
 if($LASTEXITCODE -ne 0){throw 'Owned combined probe failed.'}
 Copy-Item -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe') -Destination $target
 & $compiler /nologo /platform:x64 /r:System.Drawing.dll "/r:$packagePath\BattleshipLab.exe" "/out:$target\CheckCombinedShotTraces.exe" (Join-Path $PSScriptRoot 'Check-CombinedShotTraces.cs')
 if($LASTEXITCODE -ne 0){throw 'Combined comparison build failed.'}
 & (Join-Path $target 'CheckCombinedShotTraces.exe') (Join-Path $target 'Native-Combined-Shot-Cases.csv') (Join-Path $target 'Native-Combined-Shot-Events.csv') (Join-Path $target 'Check.txt')
 if($LASTEXITCODE -ne 0){throw 'Combined native/model comparison failed.'}
 Get-FileHash -LiteralPath (Join-Path $target 'Native-Combined-Shot-Cases.csv'),(Join-Path $target 'Native-Combined-Shot-Events.csv')
}finally{Pop-Location}
