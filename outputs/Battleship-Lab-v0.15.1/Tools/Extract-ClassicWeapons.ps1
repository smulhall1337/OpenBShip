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
 @{Name='Classic-InitialShooter';Start=0x4230DC;End=0x42312E},
 @{Name='Weapon-LabelLookup';Start=0x40DA43;End=0x40DA65},
 @{Name='Classic-PlayerShot';Start=0x423D70;End=0x423FB0},
 @{Name='Classic-OpponentShot';Start=0x41C8C0;End=0x41C93B},
 @{Name='Classic-SinkSelection';Start=0x424130;End=0x4241C2},
 @{Name='Weapon-MovieDispatch';Start=0x416640;End=0x4169F2},
 @{Name='Classic-WeaponLists';Start=0x44D730;End=0x44D778},
 @{Name='Weapon-Names';Start=0x44B800;End=0x44BC50},
 @{Name='Movie-Names';Start=0x44E288;End=0x44E711},
 @{Name='Movie-Load';Start=0x402460;End=0x402560},
 @{Name='Movie-PaletteAndHeader';Start=0x4026E0;End=0x40282F},
 @{Name='Movie-Frame';Start=0x402830;End=0x402972}
)
$manifest=foreach($span in $spans){
 $file=Join-Path $Destination ($span.Name+'.bin')
 [IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)))
 [pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}
}
$manifest | Export-Csv -LiteralPath (Join-Path $Destination 'Weapons-Code-Spans.csv') -NoTypeInformation
$lists=foreach($address in @(0x44D730,0x44D750,0x44D760)){
 $bytes=Get-Bytes $address 32
 for($i=0;$i -lt 8;$i++){
  $id=[BitConverter]::ToInt32($bytes,4*$i)
  if($id -eq 0){break}
  [pscustomobject]@{ListVA=('{0:X8}' -f $address);Index=$i;WeaponID=$id}
 }
}
$lists | Export-Csv -LiteralPath (Join-Path $Destination 'Classic-WeaponLists.csv') -NoTypeInformation
$remap=Get-Bytes 0x4169D0 34
$dispatch=Get-Bytes 0x41698C 68
0..33 | ForEach-Object {
 [pscustomobject]@{WeaponID=$_+1;HandlerIndex=$remap[$_];HandlerVA=('{0:X8}' -f [BitConverter]::ToUInt32($dispatch,4*$remap[$_]))}
} | Export-Csv -LiteralPath (Join-Path $Destination 'Weapon-Handlers.csv') -NoTypeInformation
0..128 | ForEach-Object {
 $bytes=Get-Bytes (0x44E288+9*$_) 9
 [pscustomobject]@{MovieID=$_;Name=[Text.Encoding]::ASCII.GetString($bytes).TrimEnd([char]0)}
} | Export-Csv -LiteralPath (Join-Path $Destination 'Movie-Names.csv') -NoTypeInformation
$lists
