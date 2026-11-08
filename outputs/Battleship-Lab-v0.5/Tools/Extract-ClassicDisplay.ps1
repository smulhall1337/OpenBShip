param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$Destination
)
$ErrorActionPreference='Stop'
$expected='B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569'
if ((Get-FileHash -LiteralPath $Executable -Algorithm SHA256).Hash -ne $expected) { throw 'Executable identity differs; these addresses cannot be transferred to another build.' }
$image=[IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Executable).Path)
$pe=[BitConverter]::ToInt32($image,60)
if ([BitConverter]::ToUInt32($image,$pe) -ne 0x4550 -or [BitConverter]::ToUInt16($image,$pe+24) -ne 0x10b) { throw 'Expected PE32' }
$base=[BitConverter]::ToUInt32($image,$pe+52)
$sectionCount=[BitConverter]::ToUInt16($image,$pe+6)
$table=$pe+24+[BitConverter]::ToUInt16($image,$pe+20)
function Get-Offset([long]$address) {
    $rva=$address-$base
    for($i=0;$i -lt $sectionCount;$i++) {
        $s=$table+40*$i
        $start=[BitConverter]::ToUInt32($image,$s+12)
        $size=[BitConverter]::ToUInt32($image,$s+16)
        if($rva -ge $start -and $rva-$start -lt $size) { return [int]([BitConverter]::ToUInt32($image,$s+20)+$rva-$start) }
    }
    throw ('Address is outside raw PE sections: {0:X8}' -f $address)
}
function Get-Bytes([long]$address,[int]$length) {
    $offset=Get-Offset $address
    if($offset -gt $image.Length-$length) { throw 'Truncated source span' }
    $result=New-Object byte[] $length
    [Array]::Copy($image,$offset,$result,0,$length)
    return ,$result
}
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$spans=@(
 @{Name='Palette-LoadAndPatches';Start=0x42A780;End=0x42A841},
 @{Name='Palette-ApiComposition';Start=0x401D40;End=0x401ECC},
 @{Name='Classic-PaletteEntry';Start=0x42312E;End=0x42315D},
 @{Name='Palette-FadeInitialize';Start=0x422BA0;End=0x422BDE},
 @{Name='Palette-FadeOut';Start=0x422C10;End=0x422C6F},
 @{Name='Palette-FadeIn';Start=0x422C70;End=0x422CCE},
 @{Name='Classic-DamageDraw';Start=0x40DB39;End=0x40DCE0},
 @{Name='Classic-DamageResource';Start=0x42AB14;End=0x42AB23},
 @{Name='Classic-FrameLimiter';Start=0x423210;End=0x42325E},
 @{Name='Classic-ClockApi';Start=0x4222A0;End=0x4222A6}
)
$manifest=foreach($span in $spans) {
 $file=Join-Path $Destination ($span.Name+'.bin')
 [IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)))
 [pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}
}
$manifest | Export-Csv -LiteralPath (Join-Path $Destination 'Display-Code-Spans.csv') -NoTypeInformation
$manifest | Select-Object File,Bytes