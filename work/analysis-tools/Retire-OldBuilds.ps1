param([switch]$Apply,[string]$Batch="20261108")
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
Set-Location -LiteralPath $root
$all=@(Get-ChildItem -LiteralPath (Join-Path $root 'outputs') -Directory | Where-Object {$_.Name -match '^Battleship-Lab-v(\d+\.\d+(?:\.\d+)?)$'} | Sort-Object {[version]($_.Name -replace '^Battleship-Lab-v','')} -Descending)
$tested=@($all | Where-Object {!(Test-Path -LiteralPath (Join-Path $_.FullName 'DRAFT-STATUS.txt'))})
$keep=@($tested | Select-Object -First 10);$retire=@($tested | Select-Object -Skip 10);$retiredNames=@($retire.Name)
$registryPath=Join-Path $root 'outputs\Retired-Builds.csv'
$prior=@();if(Test-Path -LiteralPath $registryPath){$prior=@(Import-Csv -LiteralPath $registryPath)}
$retiredNames=@($retiredNames)+@($prior.Version) | Sort-Object -Unique
$running=@(Get-Process -Name BattleshipLab -ErrorAction SilentlyContinue)
foreach($process in $running){if(!$process.Path){throw 'A running game has an unreadable path; package cleanup is deferred.'};foreach($dir in $retire){if($process.Path.StartsWith($dir.FullName+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Retired candidate is running: $($process.Path)"}}}
$targets=New-Object 'Collections.Generic.List[string]'
foreach($dir in $retire){$targets.Add($dir.FullName)}
foreach($name in $retiredNames){
 $zipPath=Join-Path $root ('outputs\'+$name+'.zip')
 $launcherPath=Join-Path $root ('outputs\Launch-Reconstruction-v'+($name -replace '^Battleship-Lab-v','')+'.cmd')
 foreach($candidate in @($zipPath,$launcherPath)){if(Test-Path -LiteralPath $candidate){$targets.Add($candidate)}}
}
foreach($dir in Get-ChildItem -LiteralPath (Join-Path $root 'work') -Directory | Where-Object {$_.Name -match '^(portable|source-restore)-'}){
 $packages=@(Get-ChildItem -LiteralPath $dir.FullName -Directory | Where-Object {$_.Name -like 'Battleship-Lab-v*'})
 $out=Join-Path $dir.FullName 'outputs';if(Test-Path -LiteralPath $out){$packages+=@(Get-ChildItem -LiteralPath $out -Directory | Where-Object {$_.Name -like 'Battleship-Lab-v*'})}
 if($packages.Count -gt 0 -and @($packages | Where-Object {$_.Name -notin $retiredNames}).Count -eq 0){$targets.Add($dir.FullName)}
}
$outputsPrefix=(Join-Path $root 'outputs')+'\';$workPrefix=(Join-Path $root 'work')+'\'
$prefixes=@($outputsPrefix,$workPrefix)
foreach($target in $targets){$absolute=[IO.Path]::GetFullPath($target);if($absolute -ne $target -or !($absolute.StartsWith($prefixes[0],[StringComparison]::OrdinalIgnoreCase) -or $absolute.StartsWith($prefixes[1],[StringComparison]::OrdinalIgnoreCase))){throw "Unsafe cleanup target: $target"};if((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse point cleanup target rejected'}}
foreach($target in $targets){if((Get-Item -LiteralPath $target).PSIsContainer -and @(Get-ChildItem -LiteralPath $target -Recurse -Directory | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Nested reparse point rejected'}}
$files=@(foreach($target in $targets){if((Get-Item -LiteralPath $target).PSIsContainer){Get-ChildItem -LiteralPath $target -Recurse -File}else{Get-Item -LiteralPath $target}})
$bytes=($files | Measure-Object Length -Sum).Sum
[pscustomobject]@{RetiredVersions=($retiredNames -join ', ');Keep=($keep.Name -join ', ');Targets=$targets.Count;Files=$files.Count;Bytes=$bytes} | Format-List
if(!$Apply){return}
# Verify original frozen manifests before retiring; changed work is never discarded.
foreach($dir in $retire){foreach($r in Import-Csv -LiteralPath (Join-Path $dir.FullName 'Build-Manifest.csv')){$file=Join-Path $dir.FullName $r.Path;if((Get-FileHash -LiteralPath $file).Hash -ne $r.SHA256){throw "Changed old package retained: $file"}}}
$known=@{};foreach($history in Get-ChildItem -Path (Join-Path $root 'work\reconstruction-v*\delivered-hashes.json') -File){$artifacts=Get-Content -LiteralPath $history.FullName -Raw | ConvertFrom-Json;foreach($artifact in $artifacts){$known[$artifact.Path]=$artifact.Hash}}
foreach($target in $targets){if([IO.Path]::GetExtension($target) -eq '.zip'){if(!$known.ContainsKey($target) -or (Get-FileHash -LiteralPath $target).Hash -ne $known[$target]){throw "Unrecognized or changed old ZIP retained: $target"}}}
$archive=Join-Path $root ("outputs\Retired-Build-Records-"+$Batch+".zip")
if(Test-Path -LiteralPath $archive){throw 'Retirement archive already exists; retain it.'}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$records=@(foreach($file in $files){$relative=$file.FullName.Substring($root.Length+1);if($relative -match '\\Assets\\'){continue};if($file.Extension -in @('.cs','.ps1','.cmd','.txt','.csv','.rgb') -or $relative -match '\\UserData\\'){[pscustomobject]@{Path=$relative;Bytes=$file.Length;SHA256=(Get-FileHash -LiteralPath $file.FullName).Hash}}})
$stream=[IO.File]::Open($archive,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write);$zip=New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Create)
try{foreach($r in $records){[void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $root $r.Path),$r.Path.Replace('\','/'),[IO.Compression.CompressionLevel]::Optimal)}}finally{$zip.Dispose();$stream.Dispose()}
$zip=[IO.Compression.ZipFile]::OpenRead($archive)
try{if($zip.Entries.Count -ne $records.Count){throw 'Retirement archive entry count'};$map=@{};foreach($r in $records){$map[$r.Path.Replace('\','/')]=$r.SHA256};foreach($entry in $zip.Entries){$input=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create();try{$hash=[BitConverter]::ToString($sha.ComputeHash($input)).Replace('-','')}finally{$sha.Dispose();$input.Dispose()};if($map[$entry.FullName] -ne $hash){throw 'Retirement archive hash mismatch'}}}finally{$zip.Dispose()}
$records | Select-Object *,@{Name='Archive';Expression={$archive}} | Export-Csv -LiteralPath (Join-Path $root ("outputs\Retired-Build-Records-Manifest-"+$Batch+".csv")) -NoTypeInformation
$registry=@(foreach($dir in $retire){$manifest=Join-Path $dir.FullName 'Build-Manifest.csv';[pscustomobject]@{Version=$dir.Name;ManifestSHA256=(Get-FileHash -LiteralPath $manifest).Hash;FrozenFiles=@(Import-Csv -LiteralPath $manifest).Count;RetiredUtc=[DateTime]::UtcNow.ToString('o');RecordsArchive=$archive;ArchiveSHA256=(Get-FileHash -LiteralPath $archive).Hash}})
$combined=@($prior)+@($registry);$combined | Export-Csv -LiteralPath $registryPath -NoTypeInformation
# All checked absolute targets are named workspace package/validation paths.
foreach($target in $targets){$absolute=[IO.Path]::GetFullPath($target);if(!($absolute.StartsWith($prefixes[0],[StringComparison]::OrdinalIgnoreCase) -or $absolute.StartsWith($prefixes[1],[StringComparison]::OrdinalIgnoreCase))){throw 'Target escaped workspace'};Remove-Item -LiteralPath $absolute -Recurse -Force}
$reportPath=Join-Path $root ("outputs\Build-Cleanup-"+$Batch+".txt")
@("Removed $($retire.Count) additional old build versions and remaining retired ZIPs/launchers; $($targets.Count) targets.","Removed $($files.Count) files, $bytes bytes before preserving records.","Kept ten tested builds: $($keep.Name -join ', ').","Preserved $($records.Count) source/report/settings files in $archive",'Every archived entry verified by SHA256. Original material, reference baseline, authoring directories, Git history and source checkpoint archives retained.',"Records archive SHA256: $((Get-FileHash -LiteralPath $archive).Hash)") | Set-Content -LiteralPath $reportPath -Encoding UTF8
Get-Content -LiteralPath $reportPath
