param(
 [Parameter(Mandatory=$true)][string]$RunDirectory,
 [ValidateRange(0,719)][int]$Probe = 0,
 [switch]$Draws
)
$ErrorActionPreference = 'Stop'
$run = (Resolve-Path -LiteralPath $RunDirectory).Path
$casesPath = Join-Path $run 'Native-Target-Order-Cases.csv'
$case = @(Import-Csv -LiteralPath $casesPath | Where-Object { [int]$_.Probe -eq $Probe })
if ($case.Count -ne 1) { throw 'Expected one matching bounded probe.' }
$case = $case[0]
$cells = $case.CellsXMajor.Split(';')
if ($cells.Count -ne 100) { throw 'Expected a 10x10 X-major observation grid.' }
$x = [int]$case.X; $y = [int]$case.Y
if ($x -lt 0 -or $x -gt 9 -or $y -lt 0 -or $y -gt 9) { throw 'Target outside the board.' }
"Probe $Probe : $($case.Case), level $($case.Level), seed $($case.Seed), ship type $($case.ShipType)"
"Original result: weapon $($case.Weapon), movie $($case.Movie), target $([char](65+$x))$($y+1) (zero-based $x,$y)."
"Random calls: $($case.RandomCalls); final state: $($case.FinalState)."
'Order: original firing pair -> movie request stub -> original target selector -> following-stage stub.'
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
 Import-Csv -LiteralPath (Join-Path $run 'Native-Target-Order-Draws.csv') |
  Where-Object { [int]$_.Probe -eq $Probe } |
  Format-Table Order,Kind,CallerVA,Value,StateAfter,X,Y -AutoSize
}
'Numerical experiment only. This is not a captured native battle or a runtime RNG option.'
