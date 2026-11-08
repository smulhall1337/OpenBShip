param(
 [Parameter(Mandatory=$true)][string]$Archive,
 [Parameter(Mandatory=$true)][string]$Package,
 [Parameter(Mandatory=$true)][string]$Report,
 [Parameter(Mandatory=$true)][string]$ExpectedExecutableSHA256
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archivePath=(Resolve-Path -LiteralPath $Archive).Path
$packagePath=(Resolve-Path -LiteralPath $Package).Path
$prefix=(Split-Path -Leaf $packagePath)+'/'
$manifestPath=Join-Path $packagePath 'Build-Manifest.csv'
$records=@(Import-Csv -LiteralPath $manifestPath)
$expected=@{}
foreach($record in $records){
 $relative=$record.Path.Replace('\','/')
 if($relative -match '(^/|^[A-Za-z]:|(^|/)\.\.(/|$)|^UserData/)' -or $expected.ContainsKey($prefix+$relative)){throw "Invalid/duplicate manifest path: $relative"}
 $disk=[IO.Path]::GetFullPath((Join-Path $packagePath $record.Path))
 if(!$disk.StartsWith($packagePath+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Manifest path escaped package'}
 if((Get-FileHash -LiteralPath $disk).Hash -ne $record.SHA256 -or (Get-Item -LiteralPath $disk).Length -ne [long]$record.Bytes){throw "Working package differs: $relative"}
 $expected[$prefix+$relative]=[pscustomobject]@{Hash=$record.SHA256;Bytes=[long]$record.Bytes}
}
$expected[$prefix+'Build-Manifest.csv']=[pscustomobject]@{Hash=(Get-FileHash -LiteralPath $manifestPath).Hash;Bytes=(Get-Item -LiteralPath $manifestPath).Length}
$zip=[IO.Compression.ZipFile]::OpenRead($archivePath)
$entryCount=0
try {
 if($zip.Entries.Count -ne $expected.Count){throw 'Archive entry count differs from manifest plus manifest itself'}
 $seen=@{}
 foreach($entry in $zip.Entries){
  if(!$expected.ContainsKey($entry.FullName) -or $seen.ContainsKey($entry.FullName)){throw "Unexpected/duplicate archive entry: $($entry.FullName)"}
  $seen[$entry.FullName]=$true
  $wanted=$expected[$entry.FullName]
  if($entry.Length -ne $wanted.Bytes){throw "Archive size differs: $($entry.FullName)"}
  $stream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
  try{$hash=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','')}finally{$sha.Dispose();$stream.Dispose()}
  if($hash -ne $wanted.Hash){throw "Archive content differs: $($entry.FullName)"}
 }
 $entryCount=$zip.Entries.Count
}finally{$zip.Dispose()}
$exeHash=(Get-FileHash -LiteralPath (Join-Path $packagePath 'BattleshipLab.exe')).Hash
if($exeHash -ne $ExpectedExecutableSHA256){throw 'Executable changed after production validation'}
@(
 "PASS $($records.Count) working manifest files and $entryCount ZIP entries, including the manifest, match SHA256 and byte lengths.",
 'No unexpected/duplicate entries or user settings; frozen production executable matches validated hash.',
 "Archive: $archivePath",
 "ZIP SHA256: $((Get-FileHash -LiteralPath $archivePath).Hash)",
 "EXE SHA256: $exeHash"
) | Set-Content -LiteralPath $Report -Encoding UTF8
Get-Content -LiteralPath $Report
