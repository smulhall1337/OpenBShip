param([Parameter(Mandatory=$true)][string]$Game,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$names=@('BATFIRE1','BATFIRE2','BATFIRE3','BATFIRE4','F18FIRE1','F18FIRE2','SUBTORP1','SUBTORP2','SUBMISS1','SUBMISS2','SML_LNC1','SML_LNC2','TWOFIRE1','TWOFIRE2','FRG_FTRP','BATT_LT1','BATT_LT2','FRG_LT2','MEDFIRE1','MEDFIRE2','S_FLY1','S_FLY2','S_FLY3','TORPRUN','MIS_FLY3','CRS_FLY1','CL_MISS','CL_HIT','CAR_UMH','CAR_USH','BATT_UMH','BATT_UTH','BATT_USH','BATT_DMH','BATTSINK','SUB_USH','FRG_UMH','FRG_USH','TORPMISA','TORPMISB','TORPMISC','SHELMISA','SHELMISB','CRUISDIE','BATT_DTH','FRG_UTH','BATT_DSH','CAR_UTH','SHELMISC','SUB_UTH','FRIGSINK','SUB_UMH','SUB_DIE','CARRSINK','CAR_DSH','CAR_DTH','CAR_DMH','FRG_DTH','FRG_DMH','FRG_DSH','MIS_FLY2')
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$inventory=foreach($name in $names){
 $file=Get-Item -LiteralPath (Join-Path $Game "LWEBS/$name.PAT");$bytes=[IO.File]::ReadAllBytes($file.FullName);$p=0;$frames=0;$headers=[Collections.Generic.HashSet[string]]::new()
 while($p -le $bytes.Length-4){$n=[BitConverter]::ToUInt32($bytes,$p);if($n -eq [uint32]::MaxValue){break};if($n -lt 4 -or $n -gt $bytes.Length-$p-4){throw "Invalid $name frame at $p"};[void]$headers.Add([BitConverter]::ToString($bytes,$p+4,4));$p+=4+$n;$frames++}
 [pscustomobject]@{Clip=$name;Bytes=$file.Length;Frames=$frames;Headers=($headers -join ' ');Terminal=($n -eq [uint32]::MaxValue -and $p+4 -eq $bytes.Length);SHA256=(Get-FileHash -LiteralPath $file.FullName).Hash}
}
$inventory | Export-Csv -LiteralPath (Join-Path $Destination 'Impact-Clip-Inventory.csv') -NoTypeInformation
$waves=foreach($name in $names){
 $dir=Join-Path $Game "SFX/$name";$seq=Get-Content -LiteralPath (Join-Path $dir "$name.SEQ")
 foreach($stem in @($seq | Where-Object {!$_.StartsWith('*')} | ForEach-Object {($_ -split ',')[0]} | Sort-Object -Unique)){
  $file=Get-Item -LiteralPath (Join-Path $dir "$stem.WAV");$b=[IO.File]::ReadAllBytes($file.FullName);$p=12;$fmt=$null;$data=0
  while($p -le $b.Length-8){$id=[Text.Encoding]::ASCII.GetString($b,$p,4);$n=[BitConverter]::ToUInt32($b,$p+4);$p+=8;if($n -gt $b.Length-$p){throw 'Truncated WAV chunk'}
   if($id -eq 'fmt '){$fmt=[pscustomobject]@{Format=[BitConverter]::ToUInt16($b,$p);Channels=[BitConverter]::ToUInt16($b,$p+2);Rate=[BitConverter]::ToUInt32($b,$p+4);Bits=[BitConverter]::ToUInt16($b,$p+14)}}
   if($id -eq 'data'){$data=$n};$p+=$n+($n%2)
  }
  if($null -eq $fmt -or $fmt.Bits -eq 0 -or $fmt.Channels -eq 0){throw 'Missing WAV format'}
  [pscustomobject]@{Clip=$name;File=$file.Name;Bytes=$file.Length;Format=$fmt.Format;Channels=$fmt.Channels;Rate=$fmt.Rate;Bits=$fmt.Bits;Samples=($data/($fmt.Channels*$fmt.Bits/8));SHA256=(Get-FileHash -LiteralPath $file.FullName).Hash}
 }
}
$waves | Export-Csv -LiteralPath (Join-Path $Destination 'Impact-Wave-Inventory.csv') -NoTypeInformation
"Inventoried $($inventory.Count) clips and $($waves.Count) referenced PCM files."

