param(
 [Parameter(Mandatory=$true)][string]$Executable,
 [Parameter(Mandatory=$true)][string]$Destination,
 [Parameter(Mandatory=$true)][string]$ProgramsDirectory
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
 @{Name='Fleet-Flow-Script';Start=0x450CA8;End=0x450EC8},
 @{Name='Fleet-Classic-Buttons';Start=0x451418;End=0x4514F8},
 @{Name='Fleet-Classic-Initializer';Start=0x4238D0;End=0x423AE7},
 @{Name='Fleet-Camera';Start=0x431C10;End=0x431C67},
 @{Name='Fleet-Camera-Scale';Start=0x411540;End=0x4115A2},
 @{Name='Fleet-Placement-Validator';Start=0x431330;End=0x43149B},
 @{Name='Fleet-Classic-Edit-Guards';Start=0x4316CA;End=0x4317A2},
 @{Name='Fleet-Start-Guard';Start=0x431863;End=0x43198C}
)
$records=foreach($span in $spans){$file=Join-Path $Destination ($span.Name+'.bin');[IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)));[pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'Fleet-Code-Spans.csv') -NoTypeInformation
$records | Select-Object File,Bytes


New-Item -ItemType Directory -Path $ProgramsDirectory -Force | Out-Null
$programs=foreach($item in @(@{Name='FleetFlow';VA=0x450CA8;Length=184},@{Name='FleetButtons';VA=0x451418;Length=224})){
 $path=Join-Path $ProgramsDirectory ($item.Name+'.seqbin')
 [IO.File]::WriteAllBytes($path,(Get-Bytes $item.VA $item.Length))
 [pscustomobject]@{File=$item.Name+'.seqbin';SourceVA=('{0:X8}' -f $item.VA);Bytes=$item.Length;SHA256=(Get-FileHash -LiteralPath $path).Hash;ExecutableSHA256=$expected;Scope='Classic Fleet Configuration route to input wait37 and11button records; general fleet purchasing/VM not executed'}
}
$programs | Export-Csv -LiteralPath (Join-Path $ProgramsDirectory 'Fleet-Provenance.csv') -NoTypeInformation