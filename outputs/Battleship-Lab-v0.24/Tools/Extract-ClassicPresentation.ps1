param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$Graphics,
    [Parameter(Mandatory=$true)][string]$Destination
)
$ErrorActionPreference='Stop'
$identity='B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569'
if((Get-FileHash -LiteralPath $Executable -Algorithm SHA256).Hash -ne $identity){throw 'Executable identity differs; presentation addresses are build-specific.'}
$bytes=[IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Executable).Path)
$pe=[BitConverter]::ToInt32($bytes,60)
$imageBase=[BitConverter]::ToUInt32($bytes,$pe+52)
$sectionCount=[BitConverter]::ToUInt16($bytes,$pe+6)
$table=$pe+24+[BitConverter]::ToUInt16($bytes,$pe+20)
function Offset([long]$va) {
    $rva=$va-$imageBase
    for($i=0;$i -lt $sectionCount;$i++) {
        $at=$table+$i*40; $start=[BitConverter]::ToUInt32($bytes,$at+12); $size=[BitConverter]::ToUInt32($bytes,$at+16)
        if($rva -ge $start -and $rva-$start -lt $size){return [int]([BitConverter]::ToUInt32($bytes,$at+20)+$rva-$start)}
    }
    throw 'Address outside PE section'
}
function Span([long]$va,[int]$count) {
    $offset=Offset $va
    if($offset -gt $bytes.Length-$count){throw 'Truncated source span'}
    $result=New-Object byte[] $count
    [Array]::Copy($bytes,$offset,$result,0,$count)
    return ,$result
}
function U32([long]$va){return [BitConverter]::ToUInt32($bytes,(Offset $va))}
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$scaleX=U32 0x408A4F; $scaleY=U32 0x408A4A; $camX=U32 0x408A36; $camY=U32 0x408A40
if($scaleX -ne 117 -or $scaleY -ne 78 -or $camX -ne 10 -or $camY -ne 5){throw 'Unexpected original camera parameters'}
$parameters=@(
    [pscustomobject]@{Parameter='ScaleX';Value=$scaleX;SourceVA='00408A4F'},
    [pscustomobject]@{Parameter='ScaleY';Value=$scaleY;SourceVA='00408A4A'},
    [pscustomobject]@{Parameter='CameraX';Value=$camX;SourceVA='00408A36'},
    [pscustomobject]@{Parameter='CameraY';Value=$camY;SourceVA='00408A40'},
    [pscustomobject]@{Parameter='InverseFactor';Value=[BitConverter]::ToSingle($bytes,(Offset 0x446180));SourceVA='00446180'},
    [pscustomobject]@{Parameter='OpponentOriginX';Value=U32 0x423907;SourceVA='00423907'}
)
$parameters | Export-Csv -LiteralPath (Join-Path $Destination 'Presentation-Parameters.csv') -NoTypeInformation
$banks=@{}
foreach($loader in @(0x42AA57,0x42AA6C,0x42AAAB,0x42AAEA)) {
    $offset=Offset $loader
    if($bytes[$offset] -ne 0x6a -or $bytes[$offset+2] -ne 0x68){throw 'Unexpected sprite load arguments'}
    $bank=[int]$bytes[$offset+1]; $nameVA=U32 ($loader+3); $nameOffset=Offset $nameVA; $end=$nameOffset
    while($bytes[$end] -ne 0){$end++}
    $banks[$bank]=[Text.Encoding]::ASCII.GetString($bytes,$nameOffset,$end-$nameOffset).ToUpperInvariant()
}
$percent=[int][Math]::Truncate(100*$scaleX/147)
$dispatch=foreach($type in @(7,1,2,12,4)) {
    $handler=U32 (0x40E068+($type-1)*4)
    $code=Span $handler 52
    if($code[0] -ne 0x8b -or $code[4] -ne 0x8b -or $code[5] -ne 0x15){throw 'Unexpected sprite handler'}
    $pointer=[BitConverter]::ToUInt32($code,6); $bank=[int](($pointer-0x563090)/4)
    $frame=0
    for($i=10;$i -lt 43;$i++){if($code[$i] -eq 0x83 -and $code[$i+1] -eq 0xc0){$frame=[int]$code[$i+2];break}}
    $resource=$banks[$bank]
    if(-not $resource){throw 'Unknown native sprite bank'}
    $file=Join-Path $Graphics $resource; $sph=[IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $file).Path)
    if($sph.Length -lt 2+4*($frame+1)){throw 'Truncated sprite frame table'}
    $at=[BitConverter]::ToUInt32($sph,2+4*$frame)
    if($at -gt $sph.Length-8){throw 'Invalid original frame offset'}
    $w=[BitConverter]::ToUInt16($sph,$at+4); $h=[BitConverter]::ToUInt16($sph,$at+6)
    [IO.File]::WriteAllBytes((Join-Path $Destination ('Ship-Handler-'+$type+'.bin')),$code)
    [pscustomobject]@{Type=$type;HandlerVA=('{0:X8}' -f $handler);BankPointerVA=('{0:X8}' -f $pointer);Bank=$bank;Resource=$resource;Frame=$frame;Width=$w;Height=$h;Percent=$percent;DrawWidth=[int][Math]::Truncate($w*$percent/100);DrawHeight=[int][Math]::Truncate($h*$percent/100);AssetSHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash}
}
$dispatch | Export-Csv -LiteralPath (Join-Path $Destination 'Native-Ship-Dispatch.csv') -NoTypeInformation
$spans=@(
    @{Name='Classic-Camera';Start=0x408A30;End=0x408A5D},
    @{Name='Classic-CameraTracking';Start=0x408D11;End=0x408D26},
    @{Name='Classic-ViewDispatch';Start=0x408C00;End=0x408C28},
    @{Name='Classic-CameraDispatch';Start=0x408D28;End=0x408D50},
    @{Name='Classic-BoardOrigins';Start=0x4238D0;End=0x42393D},
    @{Name='Sprite-LoadBindings';Start=0x42AA57;End=0x42AAF9},
    @{Name='Classic-ForwardAndShipAnchor';Start=0x40D640;End=0x40D957},
    @{Name='Classic-Inverse';Start=0x430D70;End=0x430F0A},
    @{Name='Classic-PlacementCommit';Start=0x431019;End=0x4310A6},
    @{Name='FloatToInteger';Start=0x439A8C;End=0x439AB3},
    @{Name='Scaled-Sprite';Start=0x407B59;End=0x407D65},
    @{Name='Sprite-DispatchTable';Start=0x40E068;End=0x40E0CC},
    @{Name='Classic-ResultBanners';Start=0x4094E4;End=0x40956F}
)
$manifest=foreach($span in $spans) {
    $file=Join-Path $Destination ($span.Name+'.bin')
    [IO.File]::WriteAllBytes($file,(Span $span.Start ($span.End-$span.Start)))
    [pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$identity}
}
$manifest | Export-Csv -LiteralPath (Join-Path $Destination 'Presentation-Code-Spans.csv') -NoTypeInformation
$dispatch | Format-Table Type,Resource,Frame,Width,Height,Percent,DrawWidth,DrawHeight
