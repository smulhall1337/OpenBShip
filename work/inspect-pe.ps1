param([Parameter(Mandatory=$true)][string]$Path)
$ErrorActionPreference='Stop'
$b=[IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path))
function U16([int]$p){[BitConverter]::ToUInt16($b,$p)}
function U32([int]$p){[BitConverter]::ToUInt32($b,$p)}
function Z([int]$p){$end=$p;while($end -lt $b.Length -and $b[$end] -ne 0){$end++};[Text.Encoding]::ASCII.GetString($b,$p,$end-$p)}
$pe=U32 60
if([Text.Encoding]::ASCII.GetString($b,$pe,4) -ne "PE`0`0"){throw 'Not a PE executable'}
$opt=$pe+24
$magic=U16 $opt
if($magic -ne 0x10b){throw 'This inspection script supports PE32 only'}
$count=U16 ($pe+6)
$sectionStart=$opt+(U16 ($pe+20))
$sections=for($i=0;$i -lt $count;$i++){$s=$sectionStart+$i*40;[pscustomobject]@{Name=[Text.Encoding]::ASCII.GetString($b,$s,8).Trim([char]0);Rva=U32 ($s+12);VirtualSize=U32 ($s+8);RawSize=U32 ($s+16);Raw=U32 ($s+20)}}
function Offset([uint32]$rva){foreach($s in $sections){if($rva -ge $s.Rva -and $rva -lt $s.Rva+[Math]::Max($s.VirtualSize,$s.RawSize)){return [int]($s.Raw+$rva-$s.Rva)}};throw "RVA outside sections: $rva"}
$importRva=U32 ($opt+104)
$imports=@()
if($importRva){$d=Offset $importRva;while((U32 ($d+12)) -ne 0){$dll=Z (Offset (U32 ($d+12)));$thunkRva=U32 $d;if(!$thunkRva){$thunkRva=U32 ($d+16)};$t=Offset $thunkRva;$symbols=@();while((U32 $t) -ne 0){$v=U32 $t;if($v -band 0x80000000){$symbols+='ordinal:'+($v -band 0xffff)}else{$symbols+=Z ((Offset $v)+2)};$t+=4};$imports+=[pscustomobject]@{Dll=$dll;Symbols=$symbols};$d+=20}}
[pscustomobject]@{Path=(Resolve-Path -LiteralPath $Path).Path;SHA256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash;Machine=('0x{0:X4}' -f (U16 ($pe+4)));ImageBase=('0x{0:X8}' -f (U32 ($opt+28)));EntryRva=('0x{0:X8}' -f (U32 ($opt+16)));Sections=$sections;Imports=$imports} | ConvertTo-Json -Depth 6
