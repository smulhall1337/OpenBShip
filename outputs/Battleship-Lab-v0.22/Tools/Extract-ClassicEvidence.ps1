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
$push=@(0x42393B,0x42399F,0x4239C2,0x4239E5,0x423A09)
$call=@(0x423988,0x4239AB,0x4239CE,0x4239F1,0x423A15)
$templates=New-Object 'Collections.Generic.List[byte]'
$names=New-Object 'Collections.Generic.List[byte]'
$ids=New-Object 'Collections.Generic.List[byte]'
$rows=for($i=0;$i -lt 5;$i++) {
    $op=Get-Bytes $push[$i] 2
    if($op[0] -ne 0x6a) { throw 'Expected push imm8' }
    $type=[int]$op[1]
    $instruction=Get-Bytes $call[$i] 5
    if($instruction[0] -ne 0xe8 -or ($call[$i]+5+[BitConverter]::ToInt32($instruction,1)) -ne 0x42CEF0) { throw 'Expected CreateShip call' }
    $templateVA=0x448D70+$type*268
    $nameVA=0x44C6C0+$type*26
    $template=Get-Bytes $templateVA 268
    $name=Get-Bytes $nameVA 26
    $templates.AddRange($template)
    $names.AddRange($name)
    $ids.Add([byte]$type)
    $x1=Get-Bytes (0x4483B0+$i*2) 2
    $y1=Get-Bytes (0x4483C0+$i*2) 2
    $x2=Get-Bytes (0x4483D0+$i*2) 2
    $y2=Get-Bytes (0x4483E0+$i*2) 2
    [pscustomobject]@{Slot=$i;Type=$type;Name=[Text.Encoding]::ASCII.GetString($name).Split([char]0)[0];Length=[BitConverter]::ToUInt16($template,14);Hits=[BitConverter]::ToUInt16($template,16);TemplateVA=('{0:X8}' -f $templateVA);NameVA=('{0:X8}' -f $nameVA);PushVA=('{0:X8}' -f $push[$i]);CallVA=('{0:X8}' -f $call[$i]);OwnX=[BitConverter]::ToInt16($x1,0);OwnY=[BitConverter]::ToInt16($y1,0);EnemyX=[BitConverter]::ToInt16($x2,0);EnemyY=[BitConverter]::ToInt16($y2,0)}
}
$rows | Export-Csv -LiteralPath (Join-Path $Destination 'Classic-Fleet.csv') -NoTypeInformation
[IO.File]::WriteAllBytes((Join-Path $Destination 'Classic-Templates.bin'),$templates.ToArray())
[IO.File]::WriteAllBytes((Join-Path $Destination 'Classic-Names.bin'),$names.ToArray())
[IO.File]::WriteAllBytes((Join-Path $Destination 'Classic-TypeIds.bin'),$ids.ToArray())
# Exact raw coordinate table spans, including their original padding.
[IO.File]::WriteAllBytes((Join-Path $Destination 'Classic-RosterXY.bin'),(Get-Bytes 0x4483B0 64))
$spans=@(
    @{Name='Classic-Initializer';Start=0x4238D0;End=0x423AD0},
    @{Name='Classic-ModeDispatch';Start=0x42B940;End=0x42B970},
    @{Name='Ship-TemplateCopy';Start=0x42CFE0;End=0x42D01A},
    @{Name='Classic-AutoPlacement';Start=0x41B820;End=0x41B9A0},
    @{Name='Placement-Validation';Start=0x431330;End=0x43149B},
    @{Name='Classic-Impact';Start=0x423FB0;End=0x424130},
    @{Name='Classic-Turns';Start=0x423540;End=0x4238A2},
    @{Name='Classic-RosterDraw';Start=0x40940C;End=0x4094E4}
)
$manifest=foreach($span in $spans) {
    $file=Join-Path $Destination ($span.Name+'.bin')
    [IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)))
    [pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}
}
$manifest | Export-Csv -LiteralPath (Join-Path $Destination 'Code-Spans.csv') -NoTypeInformation
$rows | Format-Table Slot,Type,Name,Length,Hits,OwnX,OwnY,EnemyX,EnemyY
