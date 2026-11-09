param(
 [Parameter(Mandatory=$true)][string]$Package,
 [Parameter(Mandatory=$true)][string]$Destination,
 [ValidateSet('low','high')][string]$Detail='low',
 [ValidateSet('muted','silent-device')][string]$AudioMode='muted',
 [int]$FirstLevel=0,[int]$LastLevel=3
)
$ErrorActionPreference='Stop'
$packagePath=(Resolve-Path -LiteralPath $Package).Path
$target=[IO.Path]::GetFullPath($Destination)
if($FirstLevel -lt 0 -or $LastLevel -gt 3 -or $FirstLevel -gt $LastLevel){throw 'Invalid level range'}
New-Item -ItemType Directory -Path $target -Force | Out-Null
if(Test-Path -LiteralPath (Join-Path $target 'NaturalSoloDuelCheck.exe')){throw 'Use a fresh owned matrix directory'}
Copy-Item -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe') -Destination $target
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$packagePath\BattleshipLab.exe" "/out:$target\NaturalSoloDuelCheck.exe" (Join-Path $PSScriptRoot 'NaturalSoloDuelCheck.cs')
if($LASTEXITCODE -ne 0){throw 'Natural checker build failed'}
for($level=$FirstLevel;$level -le $LastLevel;$level++){
 $report=Join-Path $target "Level-$level"
 $arguments=@(('"'+(Join-Path $packagePath 'Assets')+'"'),('"'+$report+'"'),$level,$Detail,$AudioMode)
 $process=Start-Process -FilePath (Join-Path $target 'NaturalSoloDuelCheck.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
 [pscustomobject]@{Level=$level;PID=$process.Id;Detail=$Detail;Audio=$AudioMode;Status='running';StartedUTC=[DateTime]::UtcNow.ToString('o');Report=$report} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target 'Matrix-Progress.json') -Encoding UTF8
 $process.WaitForExit()
 if($process.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $report 'Result.txt');throw "Natural cinematic level $level failed"}
 Get-Content -LiteralPath (Join-Path $report 'Result.txt')
}
[pscustomobject]@{Detail=$Detail;Audio=$AudioMode;Status='complete';Levels=($FirstLevel..$LastLevel);FinishedUTC=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target 'Matrix-Progress.json') -Encoding UTF8
