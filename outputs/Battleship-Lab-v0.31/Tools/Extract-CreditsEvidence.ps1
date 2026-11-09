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
 @{Name='Credits-Script';Start=0x4511FC;End=0x451220},
 @{Name='Credits-TextParser';Start=0x430520;End=0x4305EE},
 @{Name='Credits-FrameWait';Start=0x42F4C7;End=0x42F612},
 @{Name='Credits-Initialization';Start=0x42FD01;End=0x42FD62},
 @{Name='Credits-MoviePoll';Start=0x42FE70;End=0x430000},
 @{Name='Credits-DirectText';Start=0x427070;End=0x427230},
 @{Name='Credits-FontLoad';Start=0x42EFE0;End=0x42F030}
)
$records=foreach($span in $spans){$file=Join-Path $Destination ($span.Name+'.bin');[IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)));[pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'Credits-Code-Spans.csv') -NoTypeInformation
$records | Select-Object File,Bytes
if($ProgramsDirectory){
 New-Item -ItemType Directory -Path $ProgramsDirectory -Force | Out-Null
 $path=Join-Path $ProgramsDirectory 'Credits.seqbin';[IO.File]::WriteAllBytes($path,(Get-Bytes 0x4511FC 36))
 [pscustomobject]@{File='Credits.seqbin';SourceVA='004511FC';Bytes=36;SHA256=(Get-FileHash -LiteralPath $path).Hash;ExecutableSHA256=$expected;Scope='Bounded Preferences Credits action10: fade, clear, roll, fade, clear, title'} | Export-Csv -LiteralPath (Join-Path $ProgramsDirectory 'Credits-Provenance.csv') -NoTypeInformation
}