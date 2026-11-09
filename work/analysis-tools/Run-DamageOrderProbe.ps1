param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$Destination,[string]$Package,[switch]$Baseline)
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
 & $compiler /nologo /platform:x86 /main:ClassicDamageOrderOracle "/out:$PSScriptRoot\ClassicDamageOrderOracle.exe" (Join-Path $PSScriptRoot 'ClassicTargetOrderOracle.cs') (Join-Path $PSScriptRoot 'ClassicStageOrderOracle.cs') (Join-Path $PSScriptRoot 'ClassicDamageOrderOracle.cs')
 if($LASTEXITCODE -ne 0){throw 'Damage helper build failed.'}
 & $compiler /nologo /platform:x86 "/out:$PSScriptRoot\DamageOrderTrace.exe" (Join-Path $PSScriptRoot 'DamageOrderTrace.cs')
 if($LASTEXITCODE -ne 0){throw 'Owned damage debugger build failed.'}
 & (Join-Path $PSScriptRoot 'DamageOrderTrace.exe') (Join-Path $PSScriptRoot 'ClassicDamageOrderOracle.exe') (Join-Path $target 'Trace') $exe $target
 if($LASTEXITCODE -ne 0){throw 'Owned damage probe failed.'}
 Copy-Item -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe') -Destination $target
 & $compiler /nologo /platform:x64 /r:System.Drawing.dll "/r:$packagePath\BattleshipLab.exe" "/out:$target\CheckDamageOrderTraces.exe" (Join-Path $PSScriptRoot 'Check-DamageOrderTraces.cs')
 if($LASTEXITCODE -ne 0){throw 'Damage comparison build failed.'}
 $arguments=@((Join-Path $target 'Native-Damage-Order-Cases.csv'),(Join-Path $target 'Native-Damage-Order-Events.csv'),(Join-Path $target 'Check.txt'))
 if($Baseline){$arguments+='baseline'}
 & (Join-Path $target 'CheckDamageOrderTraces.exe') @arguments
 if($LASTEXITCODE -ne 0){throw 'Damage comparison failed.'}
 Get-FileHash -LiteralPath $arguments[0],$arguments[1]
}finally{Pop-Location}
