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
 @{Name='Preferences-Script';Start=0x450EC8;End=0x451250},
 @{Name='Preferences-Draw';Start=0x432E80;End=0x4330C4},
 @{Name='Preferences-Input';Start=0x4300B0;End=0x430359},
 @{Name='Preferences-ButtonMetrics';Start=0x433140;End=0x4331D5},
 @{Name='Preferences-VolumeCaller';Start=0x4223B6;End=0x4223D6},
 @{Name='Preferences-BufferVolume';Start=0x41B0E0;End=0x41B128},
 @{Name='Preferences-VolumeRoles';Start=0x42E8D0;End=0x42E9D0},
 @{Name='Preferences-RadarColourBranch';Start=0x40FBEF;End=0x40FC2F},
 @{Name='Preferences-Defaults';Start=0x42A6F0;End=0x42A780},
 @{Name='Preferences-PreviewSoundLoad';Start=0x42E6F0;End=0x42E72F},
 @{Name='Preferences-PreviewSoundPath';Start=0x44EDCC;End=0x44EDDC}
)
$records=foreach($span in $spans){$file=Join-Path $Destination ($span.Name+'.bin');[IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)));[pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'Preferences-Code-Spans.csv') -NoTypeInformation
$records | Select-Object File,Bytes
if($ProgramsDirectory){
 New-Item -ItemType Directory -Path $ProgramsDirectory -Force | Out-Null
 $path=Join-Path $ProgramsDirectory 'Preferences.seqbin';[IO.File]::WriteAllBytes($path,(Get-Bytes 0x450EC8 904))
 [pscustomobject]@{File='Preferences.seqbin';SourceVA='00450EC8';Bytes=904;SHA256=(Get-FileHash -LiteralPath $path).Hash;ExecutableSHA256=$expected;Scope='Bounded English Preferences initialization and setting actions; credits dependency declared separately'} | Export-Csv -LiteralPath (Join-Path $ProgramsDirectory 'Preferences-Provenance.csv') -NoTypeInformation
}