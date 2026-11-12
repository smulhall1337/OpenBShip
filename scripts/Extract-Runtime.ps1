param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
if((Get-FileHash -LiteralPath $Executable -Algorithm SHA256).Hash -ne 'B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569'){throw 'Unsupported original executable identity.'}
$bytes=[IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Executable).Path)
if($bytes.Length -lt 64 -or [BitConverter]::ToUInt16($bytes,0) -ne 0x5A4D){throw 'Invalid DOS header.'}
$pe=[BitConverter]::ToInt32($bytes,60)
if($pe -lt 64 -or $pe -gt $bytes.Length-248 -or [BitConverter]::ToUInt32($bytes,$pe) -ne 0x4550 -or [BitConverter]::ToUInt16($bytes,$pe+4) -ne 0x14C -or [BitConverter]::ToUInt16($bytes,$pe+24) -ne 0x10B){throw 'Expected a bounded PE32/x86 image.'}
$basis=[BitConverter]::ToUInt32($bytes,$pe+52);$count=[BitConverter]::ToUInt16($bytes,$pe+6)
$table=$pe+24+[BitConverter]::ToUInt16($bytes,$pe+20)
if($basis -ne 0x400000 -or $count -lt 1 -or $count -gt 128 -or $table -gt $bytes.Length-$count*40){throw 'Invalid image base or section table.'}
$records=@(Import-Csv -LiteralPath (Join-Path $PSScriptRoot 'programs.csv'))
if($records.Count -ne 24){throw 'Unexpected runtime resource manifest.'}
$output=[IO.Path]::GetFullPath($Destination);$prefix=$output.TrimEnd('\')+'\';$seen=@{}
New-Item -ItemType Directory -Path $output -Force | Out-Null
foreach($record in $records){
 $path=[IO.Path]::GetFullPath((Join-Path $output $record.File))
 if(!$path.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or $seen.ContainsKey($path) -or $record.File -notmatch '^[A-Za-z][A-Za-z0-9]*\.seqbin$'){throw 'Invalid or duplicate program filename.'}
 $seen[$path]=$true;$address=[Convert]::ToUInt32($record.SourceVA,16);$length=[int]$record.Bytes
 if($address -lt $basis -or $length -le 0 -or $length -gt 16384){throw 'Program span bounds.'}
 $rva=[long]$address-$basis;$offset=-1L
 for($i=0;$i -lt $count;$i++){
  $at=$table+$i*40;$start=[long][BitConverter]::ToUInt32($bytes,$at+12);$size=[long][BitConverter]::ToUInt32($bytes,$at+16);$raw=[long][BitConverter]::ToUInt32($bytes,$at+20)
  if($raw -gt $bytes.Length-$size){throw 'Section exceeds source image.'}
  if($rva -ge $start -and $rva-$start -le $size-$length){$offset=$raw+$rva-$start;break}
 }
 if($offset -lt 0 -or $offset -gt $bytes.Length-$length){throw 'Program span outside raw sections.'}
 $data=New-Object byte[] $length;[Array]::Copy($bytes,$offset,$data,0,$length)
 $sha=[Security.Cryptography.SHA256]::Create();try{$hash=[BitConverter]::ToString($sha.ComputeHash($data)).Replace('-','')}finally{$sha.Dispose()}
 if($hash -ne $record.SHA256){throw "Extracted program identity differs: $($record.File)"}
 if(Test-Path -LiteralPath $path){if((Get-FileHash -LiteralPath $path).Hash -ne $hash){throw "Existing modified program retained: $path"};continue}
 [IO.File]::WriteAllBytes($path,$data)
}
'Extracted and verified 24 original runtime resources (17 programs, 2 cursors and 5 unit tables).'
