param([Parameter(Mandatory=$true)][string]$Package,[Parameter(Mandatory=$true)][string]$Destination,[ValidateSet('muted','silent-device')][string]$AudioMode='silent-device')
$ErrorActionPreference='Stop'
$packagePath=(Resolve-Path -LiteralPath $Package).Path;$target=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $target){throw 'Use a fresh owned diagnostic directory.'}
New-Item -ItemType Directory -Path $target | Out-Null
Copy-Item -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe') -Destination $target
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $compiler /nologo /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$packagePath\BattleshipLab.exe" "/out:$target\NaturalRepeatShotCheck.exe" (Join-Path $PSScriptRoot 'NaturalRepeatShotCheck.cs')
if($LASTEXITCODE -ne 0){throw 'Controlled natural-shot checker build failed.'}
$process=Start-Process -FilePath (Join-Path $target 'NaturalRepeatShotCheck.exe') -ArgumentList @(('"'+(Join-Path $packagePath 'Assets')+'"'),('"'+(Join-Path $target 'Report')+'"'),$AudioMode) -WindowStyle Hidden -PassThru
$process.WaitForExit()
Get-Content -LiteralPath (Join-Path $target 'Report\Check.txt')
if($process.ExitCode -ne 0){throw 'Controlled production shot failed; see its owned report.'}
