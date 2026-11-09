param(
 [Parameter(Mandatory=$true)][string]$Executable,
 [Parameter(Mandatory=$true)][string]$Destination,
 [string]$ProgramsDirectory
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
 @{Name='Credits-Catalog-Names';Start=0x44E288;End=(0x44E288+138*9)},
 @{Name='Credits-Catalog-Admission';Start=0x402460;End=0x402552},
 @{Name='Credits-Catalog-Start';Start=0x4027DA;End=0x40282D},
 @{Name='Credits-Catalog-Poll';Start=0x402830;End=0x4028AE},
 @{Name='Credits-Catalog-Skip';Start=0x42FE70;End=0x42FEE2}
)
$records=foreach($span in $spans){$file=Join-Path $Destination ($span.Name+'.bin');[IO.File]::WriteAllBytes($file,(Get-Bytes $span.Start ($span.End-$span.Start)));[pscustomobject]@{File=$span.Name+'.bin';StartVA=('{0:X8}' -f $span.Start);EndExclusiveVA=('{0:X8}' -f $span.End);Bytes=$span.End-$span.Start;SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash;ExecutableSHA256=$expected}}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'Credits-Catalog-Code-Spans.csv') -NoTypeInformation
$records | Select-Object File,Bytes
if($ProgramsDirectory){
 New-Item -ItemType Directory -Path $ProgramsDirectory -Force | Out-Null
 $path=Join-Path $ProgramsDirectory 'CreditsMovies.seqbin';[IO.File]::WriteAllBytes($path,(Get-Bytes 0x44E288 (138*9)))
 [pscustomobject]@{File='CreditsMovies.seqbin';SourceVA='0044E288';Bytes=138*9;SHA256=(Get-FileHash -LiteralPath $path).Hash;ExecutableSHA256=$expected;Scope='NOWEB sentinel and137native Credits movie name records, nine bytes each'} | Export-Csv -LiteralPath (Join-Path $ProgramsDirectory 'CreditsMovies-Provenance.csv') -NoTypeInformation
}