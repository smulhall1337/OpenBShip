param(
 [Parameter(Mandatory=$true)][string]$Archive,
 [Parameter(Mandatory=$true)][string]$Destination,
 [switch]$AudioDeviceCheck
)
$ErrorActionPreference='Stop'
$archivePath=(Resolve-Path -LiteralPath $Archive).Path
$target=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $target){throw 'Use a fresh portable-validation directory.'}
Expand-Archive -LiteralPath $archivePath -DestinationPath $target
$packages=@(Get-ChildItem -LiteralPath $target -Directory)
if($packages.Count -ne 1){throw 'Expected one versioned package directory.'}
$package=$packages[0].FullName
$records=@(Import-Csv -LiteralPath (Join-Path $package 'Build-Manifest.csv'))
$prefix=$package+'\'
foreach($record in $records){$file=[IO.Path]::GetFullPath((Join-Path $package $record.Path));if(!$file.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path escaped package.'};if((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $record.SHA256){throw "Manifest mismatch: $($record.Path)"}}
& (Join-Path $package 'Verify.cmd');if($LASTEXITCODE -ne 0){throw 'Relocated production checks failed.'}
if($AudioDeviceCheck){$process=Start-Process -FilePath (Join-Path $package 'BattleshipLab.exe') -ArgumentList '--audio-check',('"'+(Join-Path $package 'Audio-Device-Check.txt')+'"') -WindowStyle Hidden -Wait -PassThru;if($process.ExitCode -ne 0){throw 'Relocated silent device check failed.'}}
"PASS relocated $($records.Count) manifest records; production form/model checks. Audio device requested=$AudioDeviceCheck."
