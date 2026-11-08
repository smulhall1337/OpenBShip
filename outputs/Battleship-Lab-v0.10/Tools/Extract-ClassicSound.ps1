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
 @{Name='Sound-SequencePolling';Start=0x4029F0;End=0x402ACD},
 @{Name='Sound-MovieCleanup';Start=0x402980;End=0x4029F0},
 @{Name='Sound-CueRequest';Start=0x422320;End=0x4223EF},
 @{Name='Sound-VoiceAllocation';Start=0x41A990;End=0x41AAD4},
 @{Name='Sound-FinitePlay';Start=0x41A630;End=0x41A71C},
 @{Name='Sound-Stop';Start=0x41A720;End=0x41A78D},
 @{Name='Classic-TravelAndImpactPhase';Start=0x423540;End=0x423704},
 @{Name='Classic-OpponentPhaseTail';Start=0x423704;End=0x423892},
 @{Name='Classic-LateMovieNames';Start=0x44E711;End=0x44E762}
)
$records=foreach($span in $spans){
 $file=Join-Path $Destination ($span.Name+'.bin');[IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)))
 [pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}
}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'Sound-Code-Spans.csv') -NoTypeInformation
129..137 | ForEach-Object {[pscustomobject]@{MovieID=$_;Name=[Text.Encoding]::ASCII.GetString((Get-Bytes (0x44E288+9*$_) 9)).TrimEnd([char]0)}} | Export-Csv -LiteralPath (Join-Path $Destination 'Classic-LateMovieNames.csv') -NoTypeInformation
$records | Select-Object File,Bytes
