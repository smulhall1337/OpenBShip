param(
 [Parameter(Mandatory=$true)][string]$Package,
 [Parameter(Mandatory=$true)][string]$Destination,
 [ValidateRange(0,3)][int]$Level=3,
 [ValidateSet('low','high')][string]$Detail='high',
 [ValidateSet('muted','silent-device')][string]$AudioMode='silent-device'
)
$ErrorActionPreference='Stop'
$packagePath=(Resolve-Path -LiteralPath $Package).Path
$target=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $target){throw 'Use a fresh owned diagnostic directory.'}
New-Item -ItemType Directory -Path $target | Out-Null
Copy-Item -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe') -Destination $target
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $compiler /nologo /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$packagePath\BattleshipLab.exe" "/out:$target\NaturalClassicRouteCheck.exe" (Join-Path $PSScriptRoot 'NaturalClassicRouteCheck.cs')
if($LASTEXITCODE -ne 0){throw 'Complete route checker build failed.'}
$report=Join-Path $target 'Report'
$arguments=@(('"'+(Join-Path $packagePath 'Assets')+'"'),('"'+$report+'"'),$Level,$Detail,$AudioMode)
$process=Start-Process -FilePath (Join-Path $target 'NaturalClassicRouteCheck.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
[pscustomobject]@{PID=$process.Id;Level=$Level;Detail=$Detail;Audio=$AudioMode;Report=$report;StartedUTC=[DateTime]::UtcNow.ToString('o');Scope='Owned offscreen perfect-target diagnostic; SILENT audio, no physical-input/native-game acceptance'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target 'Run.json') -Encoding UTF8
$process.WaitForExit()
Get-Content -LiteralPath (Join-Path $report 'Result.txt')
if($process.ExitCode -ne 0){throw 'Complete route failed; inspect its Result/Progress reports. No unrelated process was changed.'}
Get-Content -LiteralPath (Join-Path $report 'Complete-Route.txt')
