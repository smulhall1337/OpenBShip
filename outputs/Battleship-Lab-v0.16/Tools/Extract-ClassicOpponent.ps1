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
 @{Name='Original-TargetSelector';Start=0x41C940;End=0x41CC5E},
 @{Name='Original-TargetEntry';Start=0x41C8C0;End=0x41C93B},
 @{Name='Original-TargetDispatch';Start=0x41CC60;End=0x41CC88},
 @{Name='Original-SoloDispatch';Start=0x42333F;End=0x42337B},
 @{Name='Original-PRNG';Start=0x439790;End=0x4397BA},
 @{Name='Original-DefaultLevel';Start=0x42A70B;End=0x42A722},
 @{Name='Original-RepeatImpact';Start=0x423FB0;End=0x42412E}
)
$manifest=foreach($span in $spans) {
 $file=Join-Path $Destination ($span.Name+'.bin')
 [IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)))
 [pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}
}
$manifest | Export-Csv -LiteralPath (Join-Path $Destination 'Opponent-Code-Spans.csv') -NoTypeInformation
$rows=for($level=0;$level -lt 4;$level++) {
 $entry=Get-Bytes (0x41CC60+$level*4) 4
 $va=[BitConverter]::ToUInt32($entry,0)
 $code=Get-Bytes $va 8
 if($code[0] -ne 0xc7 -or $code[1] -ne 0x44 -or $code[2] -ne 0x24 -or $code[3] -ne 0x14){throw 'Unexpected original search budget assignment'}
 [pscustomobject]@{NativeLevel=$level;SearchAttempts=[BitConverter]::ToInt32($code,4);AssignmentVA=('{0:X8}' -f $va)}
}
$rows | Export-Csv -LiteralPath (Join-Path $Destination 'Original-Search-Budgets.csv') -NoTypeInformation
$rows | Format-Table