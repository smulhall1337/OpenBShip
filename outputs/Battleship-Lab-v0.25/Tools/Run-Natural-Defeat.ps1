param([Parameter(Mandatory=$true)][string]$Package,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$packagePath=(Resolve-Path -LiteralPath $Package).Path
$target=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $target){throw 'Use a fresh owned diagnostic directory.'}
New-Item -ItemType Directory -Path $target | Out-Null
Copy-Item -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe') -Destination $target
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$packagePath\BattleshipLab.exe" "/out:$target\NaturalDefeatCheck.exe" (Join-Path $PSScriptRoot 'NaturalDefeatCheck.cs')
if($LASTEXITCODE -ne 0){throw 'Defeat diagnostic build failed'}
foreach($detail in @('low','high')){
 $audio=if($detail -eq 'high'){'silent-device'}else{'muted'}
 $report=Join-Path $target $detail
 $process=Start-Process -FilePath (Join-Path $target 'NaturalDefeatCheck.exe') -ArgumentList ('"'+(Join-Path $packagePath 'Assets')+'"'),('"'+$report+'"'),$detail,$audio -WindowStyle Hidden -Wait -PassThru
 Get-Content -LiteralPath (Join-Path $report 'Result.txt')
 if($process.ExitCode -ne 0){throw "Natural defeat failed: $detail"}
}
