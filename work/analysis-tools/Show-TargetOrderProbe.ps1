param(
 [Parameter(Mandatory=$true)][string]$RunDirectory,
 [ValidateRange(0,1439)][int]$Probe = 0,
 [switch]$Draws,
 [switch]$Stages
)
$ErrorActionPreference = 'Stop'
$run = (Resolve-Path -LiteralPath $RunDirectory).Path
$dataset = if ($Stages) { 'Native-Stage-Order' } else { 'Native-Target-Order' }
$casesPath = Join-Path $run ($dataset+'-Cases.csv')
$case = @(Import-Csv -LiteralPath $casesPath | Where-Object { [int]$_.Probe -eq $Probe })
if ($case.Count -ne 1) { throw 'Expected one matching bounded probe.' }
$case = $case[0]
$cells = $case.CellsXMajor.Split(';')
if ($cells.Count -ne 100) { throw 'Expected a 10x10 X-major observation grid.' }
$x = [int]$case.X; $y = [int]$case.Y
if ($x -lt 0 -or $x -gt 9 -or $y -lt 0 -or $y -gt 9) { throw 'Target outside the board.' }
"Probe $Probe : $($case.Case), level $($case.Level), seed $($case.Seed), ship type $($case.ShipType)"
if ($Stages) {
 "Original result: weapon $($case.Weapon), firing movie $($case.FiringMovie), travel movie $($case.TravelMovie), target $([char](65+$x))$($y+1) (zero-based $x,$y)."
 "Travel unavailable: $($case.Unavailable); random calls through target: $($case.CallsAtTarget); final calls: $($case.FinalCalls); state: $($case.FinalState)."
 "Final phase/side: $($case.FinalPhase)/$($case.FinalSide); impact endpoints: $($case.Impacts); supplied completion calls: $($case.CompletionCalls)."
 'Order: native firing pair -> target -> phase 5 -> travel selection -> impact endpoint -> phase 0.'
 if ($case.Unavailable -eq '1') { 'Unavailable travel reaches the impact stub recursively before the firing-completion call returns.' }
 else { 'Admitted travel waits for a separate harness-supplied travel-completion call.' }
} else {
 "Original result: weapon $($case.Weapon), movie $($case.Movie), target $([char](65+$x))$($y+1) (zero-based $x,$y)."
 "Random calls: $($case.RandomCalls); final state: $($case.FinalState)."
 'Order: original firing pair -> movie request stub -> original target selector -> following-stage stub.'
}
'Grid: . unknown, o resolved miss, H unresolved hit; 2/4 are masked to unknown by original code.'
'Brackets identify the returned target; exhausted budgets can return an already resolved cell.'
'    A  B  C  D  E  F  G  H  I  J'
for ($row = 0; $row -lt 10; $row++) {
 $line = '{0,2} ' -f ($row+1)
 for ($col = 0; $col -lt 10; $col++) {
  $value = [int]$cells[$col*10+$row]
  $symbol = switch ($value) { 0 {'.'} 1 {'o'} 2 {'.'} 4 {'.'} 8 {'H'} default {'?'} }
  if ($col -eq $x -and $row -eq $y) { $line += "[$symbol]" } else { $line += " $symbol " }
 }
 $line
}
if ($Draws) {
 $columns = if ($Stages) { @('Order','Kind','CallerVA','Value','StateAfter','X','Y','Phase','Side') } else { @('Order','Kind','CallerVA','Value','StateAfter','X','Y') }
 Import-Csv -LiteralPath (Join-Path $run ($dataset+'-Draws.csv')) |
  Where-Object { [int]$_.Probe -eq $Probe } |
  Format-Table -Property $columns -AutoSize
}
'Numerical experiment only. This is not a captured native battle or a runtime RNG option.'
