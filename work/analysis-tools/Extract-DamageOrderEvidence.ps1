param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$exe=(Resolve-Path -LiteralPath $Executable).Path
if((Get-FileHash -LiteralPath $exe).Hash -ne 'B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569'){throw 'Original executable identity differs.'}
$destinationPath=[IO.Path]::GetFullPath($Destination).TrimEnd('\','/')
if($destinationPath -eq [IO.Path]::GetPathRoot($destinationPath).TrimEnd('\','/')){throw 'Use a scoped evidence directory.'}
New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$helper=Join-Path $PSScriptRoot 'ClassicDamageOrderOracle.exe'
& $compiler /nologo /platform:x86 /main:ClassicDamageOrderOracle "/out:$helper" (Join-Path $PSScriptRoot 'ClassicTargetOrderOracle.cs') (Join-Path $PSScriptRoot 'ClassicStageOrderOracle.cs') (Join-Path $PSScriptRoot 'ClassicDamageOrderOracle.cs')
if($LASTEXITCODE -ne 0){throw 'Damage fixture helper build failed.'}
$start=New-Object Diagnostics.ProcessStartInfo
$start.FileName=$helper;$start.Arguments='"'+$exe+'" "'+$destinationPath+'"'
$start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
$owned=New-Object Diagnostics.Process;$owned.StartInfo=$start
try{
 if(!$owned.Start()){throw 'Owned damage helper did not start.'}
 if(!$owned.WaitForExit(45000)){$owned.Kill();$owned.WaitForExit();throw 'Only the newly owned damage fixture helper was stopped after its 45-second timeout.'}
 $output=$owned.StandardOutput.ReadToEnd();$errors=$owned.StandardError.ReadToEnd()
 if($owned.ExitCode -ne 0){throw ('Owned damage helper failed: '+$errors)}
 $output.Trim()
}finally{$owned.Dispose()}
