param(
 [Parameter(Mandatory=$true)][string]$Package,
 [Parameter(Mandatory=$true)][string]$Destination,
 [Parameter(Mandatory=$true)][string]$OriginalRoot,
 [Parameter(Mandatory=$true)][string]$Report
)
$ErrorActionPreference='Stop'
$source=(Resolve-Path -LiteralPath $Package).Path
$target=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $target){throw 'Use a fresh source-restore directory; existing files are retained.'}
$name=Split-Path -Leaf $source
$restored=Join-Path $target ('outputs\'+$name)
New-Item -ItemType Directory -Path $restored -Force | Out-Null
Copy-Item -LiteralPath (Join-Path (Split-Path -Parent (Split-Path -Parent $source)) 'Restore-Workspace.ps1') -Destination $target
Copy-Item -LiteralPath (Join-Path $source 'Source') -Destination $restored -Recurse
foreach($name in @('Build.cmd','Verify.cmd','Asset-Provenance.csv')){Copy-Item -LiteralPath (Join-Path $source $name) -Destination $restored}
$tools=Join-Path $restored 'Tools';New-Item -ItemType Directory -Path $tools -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $source 'Tools') -File | Where-Object {$_.Extension -in @('.cs','.ps1')} | Copy-Item -Destination $tools
# These two palette observations are modern input fixtures, not native outputs.
$palette=Join-Path $restored 'Evidence\Palette';New-Item -ItemType Directory -Path $palette -Force | Out-Null
foreach($name in @('Observed-Reserved-Endpoints.rgb','System-Palette-Observation.txt')){Copy-Item -LiteralPath (Join-Path $source ('Evidence\Palette\'+$name)) -Destination $palette}
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $target 'Restore-Workspace.ps1') -OriginalRoot $OriginalRoot -WithChecks -AudioDeviceCheck
if($LASTEXITCODE -ne 0){throw 'Fresh source restore/check failed.'}
$counts=@{}
foreach($area in @('Assets','Programs','Evidence')){
 $prefix=Join-Path $source $area;$count=0
 foreach($file in Get-ChildItem -LiteralPath $prefix -File -Recurse){
  $relative=$file.FullName.Substring($source.Length+1)
  $copy=Join-Path $restored $relative
  if(!(Test-Path -LiteralPath $copy) -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $copy).Hash){throw "Restored data differs: $relative"}
  $count++
 }
 $counts[$area]=$count
}
@("PASS fresh source-only restoration; Assets=$($counts.Assets), Programs=$($counts.Programs), Evidence=$($counts.Evidence) byte-match delivery.","Native-derived evidence regenerated from absence; two modern palette observations retained.",'Production self/form/shutdown and SILENT actual waveOut checks passed. No audible or physical input confirmation.',"Restored package: $restored") | Set-Content -LiteralPath $Report -Encoding UTF8
Get-Content -LiteralPath $Report
