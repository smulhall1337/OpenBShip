param(
 [Parameter(Mandatory=$true)][string]$Executable,
 [Parameter(Mandatory=$true)][string]$Destination,
 [string]$ProgramsDirectory
)
$ErrorActionPreference='Stop'
$expected='B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569'
if((Get-FileHash -LiteralPath $Executable -Algorithm SHA256).Hash -ne $expected){throw 'Executable identity differs; do not transfer these addresses.'}
$image=[IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Executable).Path)
$pe=[BitConverter]::ToInt32($image,60)
$base=[BitConverter]::ToUInt32($image,$pe+52)
$sections=[BitConverter]::ToUInt16($image,$pe+6)
$table=$pe+24+[BitConverter]::ToUInt16($image,$pe+20)
function Get-Bytes([long]$address,[int]$length){
 $rva=$address-$base
 for($i=0;$i -lt $sections;$i++){
  $s=$table+40*$i;$start=[BitConverter]::ToUInt32($image,$s+12);$size=[BitConverter]::ToUInt32($image,$s+16)
  if($rva -ge $start -and $rva-$start -le $size-$length){
   $offset=[int]([BitConverter]::ToUInt32($image,$s+20)+$rva-$start)
   $bytes=New-Object byte[] $length;[Array]::Copy($image,$offset,$bytes,0,$length);return ,$bytes
  }
 }
 throw ('Span outside raw PE sections: {0:X8}+{1}' -f $address,$length)
}
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$spans=@(
 @{Name='Startup-EngineDispatch';Start=0x407570;End=0x407622},
 @{Name='Startup-ParentLoop';Start=0x42E9D0;End=0x42EA22},
 @{Name='Startup-PathInit';Start=0x42A6B8;End=0x42A6DF},
 @{Name='Startup-Script';Start=0x44EF48;End=0x44EF88},
 @{Name='Startup-MovieNames';Start=0x44E762;End=0x44E78F},
 @{Name='Startup-Opcode6';Start=0x42F6C6;End=0x42F6F5},
 @{Name='Startup-MovieWait';Start=0x42F35E;End=0x42F3B5},
 @{Name='Startup-SequenceAlias';Start=0x402560;End=0x4025E7},
 @{Name='Startup-SoundAlias';Start=0x422320;End=0x4223EF}
)
$records=foreach($span in $spans){$file=Join-Path $Destination ($span.Name+'.bin');[IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)));[pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'Startup-Code-Spans.csv') -NoTypeInformation
$records | Select-Object File,Bytes

if($ProgramsDirectory){
 New-Item -ItemType Directory -Path $ProgramsDirectory -Force | Out-Null
 $path=Join-Path $ProgramsDirectory 'Startup.seqbin'
 [IO.File]::WriteAllBytes($path,(Get-Bytes 0x44ef48 64))
 [pscustomobject]@{File='Startup.seqbin';SourceVA='0044EF48';Bytes=64;SHA256=(Get-FileHash -LiteralPath $path).Hash;ExecutableSHA256=$expected;Scope='Exact startup script; validated bounded reader, not general VM'} | Export-Csv -LiteralPath (Join-Path $ProgramsDirectory 'Startup-Provenance.csv') -NoTypeInformation
}


