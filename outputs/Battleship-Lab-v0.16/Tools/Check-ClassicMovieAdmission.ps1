param([Parameter(Mandatory=$true)][string]$Fixture)
$ErrorActionPreference='Stop'
$rows=@(Import-Csv -LiteralPath $Fixture)
if($rows.Count -ne 182){throw 'Unexpected admission fixture count'}
foreach($row in $rows){
 $current=[int]$row.CurrentPriority;$priority=[int]$row.RequestedPriority
 $close=0;$palettes=0;$contexts=0;$sequences=0;$finalPriority=$current
 $active=[int]$row.Active;$finalMovie=77;$finalContext=88;$directory='';$name='';$result=1
 if([int]$row.Movie -ne 0 -and [int]$row.Enabled -ne 0 -and [int]$row.HasPath -ne 0){
  if($priority -gt $current -and ($priority -eq 4 -or [int]$row.DecodedFrames -gt 30)){$close=1;$active=0}
  if($active -eq 0){
   $palettes=1;$finalPriority=$priority;$directory=if([int]$row.Mode -eq 2){'H'}else{'L'}
   $name=if([int]$row.Movie -eq 12){'MIS_FLY2'}else{'CL_HIT'}
   if([int]$row.LoadFailure -eq 0){$result=0;$active=1;$contexts=1;$sequences=1;$finalMovie=[int]$row.Movie;$finalContext=0}
  }
 }
 $expected=@{Return=$result;CloseCalls=$close;PaletteCalls=$palettes;ContextCalls=$contexts;SequenceCalls=$sequences;FinalPriority=$finalPriority;FinalActive=$active;FinalMovie=$finalMovie;FinalContext=$finalContext;Directory=$directory;Name=$name}
 foreach($key in $expected.Keys){if([string]$row.$key -ne [string]$expected[$key]){throw "Admission differs in $key, $($row.Kind) priority $current->$priority, frames $($row.DecodedFrames), active $($row.Active)"}}
}
"PASS $($rows.Count) copied-code admission cases: guards, strict priority/31st-frame preemption, loading failure, directory choice and ignored sequence return."
'This is one grouped numerical fixture comparison, not played battles or complete native movie execution.'
