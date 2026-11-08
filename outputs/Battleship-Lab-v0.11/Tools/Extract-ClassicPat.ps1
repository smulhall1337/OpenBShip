param(
 [Parameter(Mandatory=$true)][string]$Executable,
 [Parameter(Mandatory=$true)][string]$Destination
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
 @{Name='PAT-Entry';Start=0x433920;End=0x433930},
 @{Name='PAT-BlockLoop';Start=0x433AA5;End=0x433B7D},
 @{Name='PAT-HandlersAndQuartets';Start=0x433D41;End=0x434C90},
 @{Name='PAT-ExtraHandlers';Start=0x437EBD;End=0x437F9B},
 @{Name='PAT-OpcodeTable';Start=0x451875;End=0x4518A9},
 @{Name='PAT-QuartetTable';Start=0x4540E1;End=0x4544E1},
 @{Name='PAT-PaletteBranches';Start=0x4026E0;End=0x40279A},
 @{Name='PAT-FrameDriver';Start=0x402830;End=0x4028AE},
 @{Name='PAT-ScaleDispatch';Start=0x4028B0;End=0x402972},
 @{Name='PAT-ClassicCadence';Start=0x423210;End=0x4232CA},
 @{Name='PAT-SequenceRead';Start=0x402560;End=0x4026DA},
 @{Name='PAT-SequenceEvents';Start=0x4029F0;End=0x402ACD}
)
$records=foreach($span in $spans){
 $file=Join-Path $Destination ($span.Name+'.bin')
 [IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)))
 [pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}
}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'PAT-Code-Spans.csv') -NoTypeInformation
$records | Select-Object File,Bytes