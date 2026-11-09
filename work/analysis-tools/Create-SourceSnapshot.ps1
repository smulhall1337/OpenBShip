param(
 [string]$Workspace=(Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
 [ValidateRange(1,9999)][int]$Checkpoint=18
)
$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path -LiteralPath $Workspace).Path
Set-Location -LiteralPath $taskWorkspace
$archive=Join-Path $taskWorkspace "outputs\Battleship-Source-Checkpoint-$Checkpoint.zip"
$recordRelative="outputs/Source-Checkpoint-$Checkpoint.txt"
if(Test-Path -LiteralPath $archive){throw 'Source archive exists; retain it and use another checkpoint number.'}
$paths=@(git ls-files --cached --others --exclude-standard | Where-Object {$_ -ne $recordRelative -and (Test-Path -LiteralPath (Join-Path $taskWorkspace $_) -PathType Leaf)})
if($LASTEXITCODE -ne 0){throw 'Read-only source inventory failed.'}
$unexpected=@($paths | Where-Object {$_ -match '(^|/)(\.git|Assets|UserData|reference-[^/]+)/|\.(exe|dll|pdb|zip|iso|png|wav|pat|pal|idx|bin|dmp)$'})
if($unexpected.Count){throw ('Unexpected media/generated/metadata paths: '+($unexpected -join ', '))}
$prefix=$taskWorkspace+'\'
$rows=foreach($relative in $paths){
 $file=[IO.Path]::GetFullPath((Join-Path $taskWorkspace $relative))
 if(!$file.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Source path escaped workspace.'}
 $item=Get-Item -LiteralPath $file
 [pscustomobject]@{Path=$relative;Bytes=$item.Length;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash}
}
$scratch=Join-Path $taskWorkspace "work\source-snapshot-$Checkpoint"
New-Item -ItemType Directory -Path $scratch -Force | Out-Null
$manifest=Join-Path $scratch 'Source-Manifest.csv'
$rows | Export-Csv -LiteralPath $manifest -NoTypeInformation
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream=[IO.File]::Open($archive,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write)
$zip=New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Create)
$folder="Battleship-Source-Checkpoint-$Checkpoint/"
try{
 foreach($row in $rows){[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $taskWorkspace $row.Path),($folder+$row.Path.Replace('\','/')),[IO.Compression.CompressionLevel]::Optimal) | Out-Null}
 [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$manifest,($folder+'Source-Manifest.csv'),[IO.Compression.CompressionLevel]::Optimal) | Out-Null
}finally{$zip.Dispose();$stream.Dispose()}
$zip=[IO.Compression.ZipFile]::OpenRead($archive)
try{
 if($zip.Entries.Count -ne $rows.Count+1){throw 'Source ZIP entry count differs.'}
 $expected=@{};foreach($row in $rows){$expected[$folder+$row.Path.Replace('\','/')]=$row.SHA256};$expected[$folder+'Source-Manifest.csv']=(Get-FileHash -LiteralPath $manifest).Hash
 foreach($entry in $zip.Entries){if(!$expected.ContainsKey($entry.FullName)){throw 'Unexpected source ZIP entry.'};$inputStream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create();try{$hash=[BitConverter]::ToString($sha.ComputeHash($inputStream)).Replace('-','')}finally{$sha.Dispose();$inputStream.Dispose()};if($hash -ne $expected[$entry.FullName]){throw "Source ZIP mismatch: $($entry.FullName)"}}
}finally{$zip.Dispose()}
@(
 "Authored-source checkpoint $Checkpoint - $([DateTime]::UtcNow.ToString('o'))",
 "Archive: $archive",
 "Authored/source fixture files: $($rows.Count); ZIP entries: $($rows.Count+1).",
 "SHA256: $((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash)",
 'PASS every entry verified against source SHA256; Source-Manifest.csv included.',
 'Supplied original assets/executables, generated builds/media, settings, scratch and Git metadata are excluded. The sixty-byte recorded modern Windows palette fixture is intentionally retained.',
 'Restore latest with Restore-Workspace.ps1 and the supplied exact original RIP; README.txt provides flags. This source archive records no Git commit. The initialized local repository and prepared Complete-Git-Checkpoint script remain available.',
 'No remote, publication or upload performed.'
) | Set-Content -LiteralPath (Join-Path $taskWorkspace $recordRelative) -Encoding UTF8
Get-Content -LiteralPath (Join-Path $taskWorkspace $recordRelative)
