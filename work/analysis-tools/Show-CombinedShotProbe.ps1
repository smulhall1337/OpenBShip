param([Parameter(Mandatory=$true)][string]$RunDirectory,[ValidateRange(0,1279)][int]$Probe=8,[switch]$Events)
$ErrorActionPreference='Stop'
$run=(Resolve-Path -LiteralPath $RunDirectory).Path
$match=@(Import-Csv -LiteralPath (Join-Path $run 'Native-Combined-Shot-Cases.csv') | Where-Object {[int]$_.Probe -eq $Probe})
if($match.Count -ne 1){throw 'Expected one matching probe.'};$case=$match[0]
$x=[int]$case.X;$y=[int]$case.Y
"Combined original-code shot $Probe : $($case.Case), seed $($case.Seed), level $($case.Level), firing slot $($case.ShooterSlot)."
"Target $([char](65+$y))$($x+1) (source X=$x,Y=$y); weapon $($case.Weapon); movies $($case.FiringMovie) -> $($case.TravelMovie) -> $($case.FeedbackMovie)."
"Shared random draws: target boundary $($case.CallsAtTarget); final $($case.FinalCalls); state $($case.FinalState)."
"Outcome 0=miss/no new hit, 1=hit, 2=sink: $($case.Outcome); active player slot $($case.ActiveBefore) -> $($case.ActiveAfter)."
"Recorded terminal winner: $($case.Winner) (-1=none); final phase/side $($case.PhaseAfter)/$($case.SideAfter)."
$before=$case.HPBefore.Split(';');$after=$case.HPAfter.Split(';');$names=@('Super Carrier','Battleship','Frigate','Missile Submarine','Destroyer')
for($slot=0;$slot -lt 5;$slot++){'{0}: {1} HP {2} -> {3}' -f $slot,$names[$slot],$before[$slot],$after[$slot]}
'Before: . unknown water, S unhit ship, o miss, H hit. Brackets mark the native target.'
$cells=$case.GridBefore.Split(';');if($cells.Count -ne 100){throw 'Expected 10x10 source grid.'}
'    A  B  C  D  E  F  G  H  I  J'
for($row=0;$row -lt 10;$row++){
 $line='{0,2} ' -f ($row+1)
 for($col=0;$col -lt 10;$col++){$symbol=switch([int]$cells[$row*10+$col]){0 {'.'} 1 {'o'} 2 {'S'} 8 {'H'} default {'?'}};if($row -eq $x -and $col -eq $y){$line+="[$symbol]"}else{$line+=" $symbol "}}
 $line
}
if($Events){Import-Csv -LiteralPath (Join-Path $run 'Native-Combined-Shot-Events.csv') | Where-Object {[int]$_.Probe -eq $Probe} | Format-Table Order,Kind,CallerVA,Value,StateAfter,Phase,Side,HP,Cell,Outcome,Alive -AutoSize}
'Prepared fleet/seed and supplied completion calls. Media/view/message/terminal lifecycle are stubs.'
'This evidence viewer does not launch a game or claim original initialization/full gameplay.'
