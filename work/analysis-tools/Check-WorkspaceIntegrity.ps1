param([Parameter(Mandatory=$true)][string]$Workspace,[Parameter(Mandatory=$true)][string]$Package,[Parameter(Mandatory=$true)][string]$Report)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $Workspace).Path;$packagePath=(Resolve-Path -LiteralPath $Package).Path
$rows=New-Object 'Collections.Generic.List[string]'
$baseline=Get-Content -LiteralPath (Join-Path $root 'work\checkpoints\menu-baseline-001\baseline.json') -Raw | ConvertFrom-Json
foreach($file in $baseline.Files){if((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.SHA256){throw "Original reference changed: $($file.Path)"}}
$rows.Add('PASS original reference executable/wrapper/configuration/launcher match menu-baseline-001.')
$retiredPath=Join-Path $root 'outputs\Retired-Builds.csv';$retired=@();if(Test-Path -LiteralPath $retiredPath){$retired=@(Import-Csv -LiteralPath $retiredPath);foreach($group in $retired | Group-Object RecordsArchive){$record=$group.Group[0];$archive=[IO.Path]::GetFullPath($record.RecordsArchive);if(!$archive.StartsWith((Join-Path $root 'outputs')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Retirement archive escaped workspace'};if((Get-FileHash -LiteralPath $archive).Hash -ne $record.ArchiveSHA256){throw 'Retired source/report/settings archive changed'}};$rows.Add("PASS $($retired.Count) user-authorized retired builds have verified source/report/settings archives; compiled packages intentionally removed.")}
$current=Split-Path -Leaf $packagePath
foreach($directory in Get-ChildItem -LiteralPath (Join-Path $root 'outputs') -Directory | Where-Object {$_.Name -like 'Battleship-Lab-v*' -and $_.Name -ne $current}){
 if(Test-Path -LiteralPath (Join-Path $directory.FullName 'DRAFT-STATUS.txt')){$rows.Add("INFO $($directory.Name) remains a draft, excluded from frozen-release assertions.");continue}
 $manifest=@(Import-Csv -LiteralPath (Join-Path $directory.FullName 'Build-Manifest.csv'));foreach($record in $manifest){if((Get-FileHash -LiteralPath (Join-Path $directory.FullName $record.Path)).Hash -ne $record.SHA256){throw "Preserved build changed: $($directory.Name)/$($record.Path)"}}
 $matching=0;foreach($history in Get-ChildItem -Path (Join-Path $root 'work\reconstruction-v*\delivered-hashes.json') -File){$artifacts=Get-Content -LiteralPath $history.FullName -Raw | ConvertFrom-Json;foreach($artifact in $artifacts){if($artifact.Path -eq ($directory.FullName+'.zip') -or $artifact.Path -eq (Join-Path $directory.FullName 'BattleshipLab.exe')){if((Get-FileHash -LiteralPath $artifact.Path).Hash -ne $artifact.Hash){throw ("Preserved artifact changed: {0} from {1}; expected={2}; actual={3}" -f $artifact.Path,$history.FullName,$artifact.Hash,(Get-FileHash -LiteralPath $artifact.Path).Hash)};$matching++}}}
 if($matching -ne 2){throw "Missing frozen artifact identities: $($directory.Name)"};$rows.Add("PASS $($directory.Name): $($manifest.Count) manifest files and delivered exe/ZIP identities unchanged.")
}
$rows | Set-Content -LiteralPath $Report -Encoding UTF8
Get-Content -LiteralPath $Report
