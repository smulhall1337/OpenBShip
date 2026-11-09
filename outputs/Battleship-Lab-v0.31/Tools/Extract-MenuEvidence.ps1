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
 @{Name='Menu-Entry-Script';Start=0x44EF88;End=0x44EFC0},
 @{Name='Menu-Title-Script';Start=0x44EFC0;End=0x44F148},
 @{Name='Menu-Label-Scanner';Start=0x42F200;End=0x42F2F3},
 @{Name='Menu-Reset';Start=0x4321F0;End=0x432222},
 @{Name='Menu-Button-Loader';Start=0x430820;End=0x43087C},
 @{Name='Menu-Button-Dimensions';Start=0x433140;End=0x4331D5},
 @{Name='Menu-Title-Input';Start=0x4300B0;End=0x430359},
 @{Name='Menu-Title-Draw';Start=0x4323E0;End=0x432481},
 @{Name='Menu-Title-Resource';Start=0x42EF91;End=0x42EFA0},
 @{Name='Menu-Zero-Key-Copy';Start=0x4339F3;End=0x433A33},
 @{Name='Menu-Unscaled-Raster';Start=0x407950;End=0x407D6D}
)
$records=foreach($span in $spans){$file=Join-Path $Destination ($span.Name+'.bin');[IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)));[pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'Menu-Code-Spans.csv') -NoTypeInformation
$records | Select-Object File,Bytes


New-Item -ItemType Directory -Path $ProgramsDirectory -Force | Out-Null
$programs=foreach($item in @(@{Name='MenuEntry';VA=0x44EF88;Length=56},@{Name='MenuTitle';VA=0x44EFC0;Length=168})){
 $path=Join-Path $ProgramsDirectory ($item.Name+'.seqbin')
 [IO.File]::WriteAllBytes($path,(Get-Bytes $item.VA $item.Length))
 [pscustomobject]@{File=$item.Name+'.seqbin';SourceVA=('{0:X8}' -f $item.VA);Bytes=$item.Length;SHA256=(Get-FileHash -LiteralPath $path).Hash;ExecutableSHA256=$expected;Scope='Menu entry/title initialization through input wait39; original destinations not executed'}
}
$programs | Export-Csv -LiteralPath (Join-Path $ProgramsDirectory 'Menu-Provenance.csv') -NoTypeInformation