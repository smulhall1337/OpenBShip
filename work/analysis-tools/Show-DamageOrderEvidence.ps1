param([Parameter(Mandatory=$true)][string]$Evidence)
$ErrorActionPreference='Stop'
$rows=Import-Csv -LiteralPath (Join-Path $Evidence 'Native-Damage-Order-Cases.csv')
'Original phase/impact comparison: source coordinate (6,3) is D7 in game labels.'
'The copied original executes damage, feedback selection and sink cleanup.'
'Media, message and final-result callbacks are recorded; no game is launched.'
$rows | Where-Object {$_.Level -eq '0' -and $_.Seed -eq '0' -and $_.ShipType -eq '7' -and $_.Weapon -eq '14'} |
 Select-Object Case,HPBefore,HPAfter,CellAfter,Movie,RandomCalls,Winner | Format-Table -AutoSize
'Repeat-hit: no new damage, no movie, no random draw.'
'Repeat-sunk: no new damage; native removed the ship record, so miss feedback applies.'
'v0.31 matches all 2,496 bounded cases. This does not establish a full native battle.'
