param(
 [Parameter(Mandatory=$true)][string]$Executable,
 [Parameter(Mandatory=$true)][string]$Disassembler,
 [Parameter(Mandatory=$true)][string]$Evidence,
 [Parameter(Mandatory=$true)][string]$Destination
)
$ErrorActionPreference='Stop'
$expected='B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569'
if((Get-FileHash -LiteralPath $Executable).Hash -ne $expected){throw 'Executable identity differs; do not transfer addresses.'}
$bytes=[IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Executable).Path)
$pe=[BitConverter]::ToInt32($bytes,60);$table=$pe+24+[BitConverter]::ToUInt16($bytes,$pe+20)
function Offset([uint32]$va,[int]$length){
 $rva=$va-0x400000
 for($i=0;$i -lt [BitConverter]::ToUInt16($bytes,$pe+6);$i++){
  $at=$table+40*$i;$start=[BitConverter]::ToUInt32($bytes,$at+12);$size=[BitConverter]::ToUInt32($bytes,$at+16)
  if($rva -ge $start -and $rva-$start -le $size-$length){return [int]([BitConverter]::ToUInt32($bytes,$at+20)+$rva-$start)}
 }
 throw ('Source span outside raw image: {0:X8}+{1}' -f $va,$length)
}
$spans=foreach($file in Get-ChildItem -LiteralPath $Evidence -File -Filter '*Code-Spans.csv'){
 foreach($span in Import-Csv -LiteralPath $file.FullName){
  if($span.StartVA -and $span.EndExclusiveVA -and $span.ExecutableSHA256 -eq $expected){
   [pscustomobject]@{Name=$span.File;First=[Convert]::ToUInt32($span.StartVA,16);End=[Convert]::ToUInt32($span.EndExclusiveVA,16)}
  }
 }
}
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$sha=[Security.Cryptography.SHA256]::Create()
try {
 $records=foreach($target in @(0x439790,0x439780)){
  $lines=@(& $Disassembler $Executable xref ('{0:X8}' -f $target));if($LASTEXITCODE -ne 0){throw 'Read-only disassembly failed.'}
  foreach($line in $lines){
   if($line -notmatch '^([0-9A-F]{8})  call 004397(90|80)h$'){throw "Unexpected xref requires review: $line"}
   $va=[Convert]::ToUInt32($Matches[1],16);$raw=Offset $va 5
   $resolved=[int64]$va+5+[BitConverter]::ToInt32($bytes,$raw+1)
   if($bytes[$raw] -ne 0xe8 -or $resolved -ne $target){throw 'Candidate is not the asserted direct call.'}
   $context=New-Object byte[] 37;[Array]::Copy($bytes,(Offset ($va-16) 37),$context,0,37)
   $overlap=@($spans | Where-Object {$va -ge $_.First -and $va -lt $_.End} | ForEach-Object {$_.Name} | Sort-Object -Unique)
   [pscustomobject]@{CallVA=('{0:X8}' -f $va);TargetVA=('{0:X8}' -f $target);Kind=$(if($target -eq 0x439790){'rand'}else{'srand'});RecoveredSpanOverlap=($overlap -join ';');ContextStartVA=('{0:X8}' -f ($va-16));ContextSHA256=([BitConverter]::ToString($sha.ComputeHash($context)).Replace('-',''));ContextHex=[BitConverter]::ToString($context);ExecutableSHA256=$expected;Reachability='Candidate direct call; scene reachability and execution order unverified'}
  }
 }
 $records | Export-Csv -LiteralPath (Join-Path $Destination 'Native-Random-Call-Candidates.csv') -NoTypeInformation
 $state=[BitConverter]::ToUInt32($bytes,(Offset 0x454d50 4))
 $summary=@("Original identity: $expected",('Verified direct rand call candidates: '+@($records|Where-Object {$_.Kind -eq 'rand'}).Count),('Verified direct srand call candidates: '+@($records|Where-Object {$_.Kind -eq 'srand'}).Count),"CRT shared state VA00454D50, file initialization=$state.",'Each direct-call opcode/displacement and its37byte context are checked against the exact original bytes.','This is a static candidate census from linear disassembly. It does not establish scene reachability, dynamic call order, indirect consumers or whole-session behavior.','RecoveredSpanOverlap names source spans with independently recorded addresses; it does not assign a complete function or gameplay role.','Current independent modern RNG streams remain unchanged. No runtime or original asset was modified.')
 $summary | Set-Content -LiteralPath (Join-Path $Destination 'Survey.txt') -Encoding UTF8
 $summary
} finally {$sha.Dispose()}
