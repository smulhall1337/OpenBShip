param([Parameter(Mandatory=$true)][string]$Package,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$packagePath=(Resolve-Path -LiteralPath $Package).Path
$target=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $target){throw 'Use a fresh owned diagnostic directory.'}
New-Item -ItemType Directory -Path $target | Out-Null
Copy-Item -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe') -Destination $target
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$packagePath\BattleshipLab.exe" "/out:$target\NaturalResultCheck.exe" (Join-Path $PSScriptRoot 'NaturalResultCheck.cs')
if($LASTEXITCODE -ne 0){throw 'Result diagnostic build failed'}
foreach($audio in @('muted','silent-device')){
 $report=Join-Path $target $audio
 $process=Start-Process -FilePath (Join-Path $target 'NaturalResultCheck.exe') -ArgumentList ('"'+(Join-Path $packagePath 'Assets')+'"'),('"'+$report+'"'),$audio -WindowStyle Hidden -Wait -PassThru
 Get-Content -LiteralPath (Join-Path $report 'Result.txt')
 if($process.ExitCode -ne 0){throw "Natural result failed: $audio"}
}
