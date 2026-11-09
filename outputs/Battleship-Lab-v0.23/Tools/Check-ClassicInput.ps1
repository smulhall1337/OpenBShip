param([Parameter(Mandatory=$true)][string]$Evidence)
$ErrorActionPreference='Stop'
$rows=@(Import-Csv -LiteralPath (Join-Path $Evidence 'Classic-Key-Pulse-Oracle.csv'))
if($rows.Count -ne 12){throw 'Expected12key-state cases'}
foreach($row in $rows){$key=[int]$row.Key;$pulse=if($key){2}else{0};$held=if($key -and [int]$row.RepeatFlag -eq 0){1}else{0};if([int]$row.EventAfterDown -ne $pulse -or [int]$row.HeldAfterDown -ne $held -or [int]$row.FirstDecay -ne [int]($pulse -gt 0) -or [int]$row.SecondDecay -ne 0 -or [int]$row.HeldAfterUp -ne 0 -or [int]$row.EventAfterUp -ne 0){throw "Key writer differs: $($row.Key)/$($row.RepeatFlag)"}}
$rows=@(Import-Csv -LiteralPath (Join-Path $Evidence 'Classic-Pause-Key-Oracle.csv'))
if($rows.Count -ne 24){throw 'Expected24pause-dispatch cases'}
foreach($row in $rows){
 $paused=[int]$row.Paused;$network=[int]$row.Network;$menu=0;$calls=New-Object 'Collections.Generic.List[string]'
 $toggle={if($network -eq 1){$calls.Add('NetworkPause')}elseif($network -gt 1){$calls.Add('NetworkNotify')};$script:expectedPaused=1-$script:expectedPaused;$calls.Add($(if($script:expectedPaused){'AudioPause'}else{'AudioResume'}))}
 $script:expectedPaused=$paused
 if([int]$row.Escape){if($paused){$menu=1;if($network){$calls.Add('NetworkCommand:9:0:0');$calls.Add('NetworkFlush')}}else{& $toggle}}
 if([int]$row.P){& $toggle}
 if([int]$row.FinalPaused -ne $script:expectedPaused -or [int]$row.MenuState -ne 4*$menu -or [int]$row.ReturnState -ne 9*$menu -or [int]$row.ReturnMode -ne 2*$menu -or $row.MenuPointer -ne $(if($menu){'0044EF88'}else{'00000000'}) -or $row.Calls -ne ($calls -join '|')){throw "Pause dispatch differs: $($row | ConvertTo-Json -Compress)"}
}
'PASS12key-state and24Escape/P cases match independent expectations; grouped numerical fixtures, not physical key tests.'
