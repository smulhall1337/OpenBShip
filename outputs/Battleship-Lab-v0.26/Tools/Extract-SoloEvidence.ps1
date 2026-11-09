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
 @{Name='Solo-Entry-Script';Start=0x44F3F8;End=0x44F440},
 @{Name='Solo-Practice-Script';Start=0x44F440;End=0x44F758},
 @{Name='Solo-Player-Limits';Start=0x451630;End=0x451684},
 @{Name='Solo-Player-Clamp';Start=0x4321D0;End=0x4321EF},
 @{Name='Solo-Practice-Sprite-Draw';Start=0x432490;End=0x432604},
 @{Name='Solo-Description-Call';Start=0x432604;End=0x432649},
 @{Name='Solo-Resources';Start=0x42EF0A;End=0x42EFA0}
)
$records=foreach($span in $spans){$file=Join-Path $Destination ($span.Name+'.bin');[IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)));[pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'Solo-Code-Spans.csv') -NoTypeInformation
$records | Select-Object File,Bytes


New-Item -ItemType Directory -Path $ProgramsDirectory -Force | Out-Null
$programs=foreach($item in @(@{Name='SoloEntry';VA=0x44F3F8;Length=72},@{Name='PracticeSetup';VA=0x44F440;Length=388},@{Name='PlayerLimits';VA=0x451630;Length=84})){
 $path=Join-Path $ProgramsDirectory ($item.Name+'.seqbin')
 [IO.File]::WriteAllBytes($path,(Get-Bytes $item.VA $item.Length))
 [pscustomobject]@{File=$item.Name+'.seqbin';SourceVA=('{0:X8}' -f $item.VA);Bytes=$item.Length;SHA256=(Get-FileHash -LiteralPath $path).Hash;ExecutableSHA256=$expected;Scope='Solo Practice setup through input wait39; gameplay uses an explicit modern Classic bridge'}
}
$programs | Export-Csv -LiteralPath (Join-Path $ProgramsDirectory 'Solo-Provenance.csv') -NoTypeInformation