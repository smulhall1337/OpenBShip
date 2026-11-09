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
 @{Name='Ending-OutcomeRoute';Start=0x4196F0;End=0x41978F},
 @{Name='Ending-ClassicSinkRoute';Start=0x424130;End=0x4241C2},
 @{Name='Ending-VictoryScript';Start=0x4512D8;End=0x451318},
 @{Name='Ending-LossScript';Start=0x451318;End=0x451358},
 @{Name='Ending-MovieOpcode';Start=0x42F6C6;End=0x42F6F4},
 @{Name='Ending-MovieWait';Start=0x42F4C7;End=0x42F53E},
 @{Name='Ending-MovieNames';Start=(0x44E288+139*9);End=(0x44E288+141*9)}
)
$records=foreach($span in $spans){$file=Join-Path $Destination ($span.Name+'.bin');[IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)));[pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'Ending-Code-Spans.csv') -NoTypeInformation
$records | Select-Object File,Bytes


$catalog=foreach($item in @(@{Name='VICTORY';Id=139;Script=0x4512D8;Frames=661},@{Name='LOSE';Id=140;Script=0x451318;Frames=680})){
 $nameBytes=Get-Bytes (0x44E288+9*$item.Id) 9
 $name=[Text.Encoding]::ASCII.GetString($nameBytes).Trim([char]0)
 if($name -ne $item.Name){throw 'Ending name lookup differs'}
 $script=Get-Bytes $item.Script 64
 if([BitConverter]::ToUInt32($script,32) -ne 6 -or [BitConverter]::ToUInt32($script,36) -ne $item.Id){throw 'Ending script movie operand differs'}
 [pscustomobject]@{Clip=$name;MovieId=$item.Id;ScriptVA=('{0:X8}' -f $item.Script);Frames=$item.Frames;FrameMilliseconds=50;MovieOpcodeVA='0042F6C6';WaitLimiterVA='0042F4C7';ExecutableSHA256=$expected}
}
$catalog | Export-Csv -LiteralPath (Join-Path $Destination 'Ending-Catalog.csv') -NoTypeInformation
$words=foreach($row in $catalog){$address=[Convert]::ToUInt32($row.ScriptVA,16);$data=Get-Bytes $address 64;for($i=0;$i -lt 16;$i++){[pscustomobject]@{Clip=$row.Clip;Word=$i;VA=('{0:X8}' -f ($address+4*$i));RawValue=('{0:X8}' -f [BitConverter]::ToUInt32($data,4*$i))}}}
$words | Export-Csv -LiteralPath (Join-Path $Destination 'Ending-Script-Words.csv') -NoTypeInformation
