param([string]$Workspace=(Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $Workspace).Path
$sets=@(
 @{Version='0.11';Path='work\natural-cinematic-v011-level0';Reports=@('Report');High=$false},
 @{Version='0.11';Path='work\matrix-v011-low-muted';Reports=@('Level-1','Level-2','Level-3');High=$false},
 @{Version='0.12';Path='work\matrix-v012-high-muted';Reports=@('Level-0','Level-1','Level-2','Level-3');High=$true},
 @{Version='0.12';Path='work\matrix-v012-high-silent';Reports=@('Level-0','Level-1','Level-2','Level-3');High=$true},
 @{Version='0.13';Path='work\matrix-v013-low-muted';Reports=@('Level-0','Level-1','Level-2','Level-3');High=$false},
 @{Version='0.13';Path='work\matrix-v013-high-silent';Reports=@('Level-0','Level-1','Level-2','Level-3');High=$true},
 @{Version='0.13';Path='work\matrix-v013-metadata-fixed';Reports=@('Level-0');High=$true}
)
$lines=New-Object 'Collections.Generic.List[string]'
$lines.Add('Diagnostic frame-count audit - '+[DateTime]::UtcNow.ToString('o'))
$lines.Add('Original Stages.csv retained. Old diagnostic sampled Timeline before firing a new player shot, so some Player/Firing rows carry the preceding clip frame count. This affects that metadata column, not recorded clip identity, number of stages, final state, pause checks or actual buffer starts. Do not sum old Frames as movie duration. Stage-Frame-Audit.csv records original and source-derived counts separately.')
$total=0;$mismatches=0
foreach($set in $sets){
 $inventory=Join-Path $root ("outputs\Battleship-Lab-v$($set.Version)\Evidence\"+$(if($set.High){'HWEBS-Inventory.csv'}else{'Impact-Clip-Inventory.csv'}))
 $counts=@{};foreach($row in Import-Csv -LiteralPath $inventory){$counts[$row.Clip]=[int]$row.Frames}
 foreach($sub in $set.Reports){
  $folder=Join-Path $root ($set.Path+'\'+$sub);$result=Join-Path $folder 'Result.txt'
  if(!(Test-Path $result) -or !(Get-Content $result -Raw).StartsWith('PASS')){continue}
  $changed=0
  $records=foreach($row in Import-Csv -LiteralPath (Join-Path $folder 'Stages.csv')){
   if(!$counts.ContainsKey($row.Clip)){throw "Clip absent from native inventory: $($row.Clip)"}
   $different=[int]$row.Frames -ne $counts[$row.Clip]
   if($different){if($row.Side -ne 'Player' -or $row.Stage -ne 'Firing'){throw 'Unexpected diagnostic frame mismatch outside known sampler path'};$changed++}
   [pscustomobject]@{ElapsedMs=$row.ElapsedMs;Turn=$row.Turn;Side=$row.Side;Stage=$row.Stage;Clip=$row.Clip;OriginalDiagnosticFrames=$row.Frames;SourceFrames=$counts[$row.Clip];KnownStaleSampler=$different}
  }
  $records | Export-Csv -LiteralPath (Join-Path $folder 'Stage-Frame-Audit.csv') -NoTypeInformation
  $total+=$records.Count;$mismatches+=$changed
  $lines.Add("$($set.Path)\$sub : $($records.Count) stages, $changed stale Player/Firing frame-count entries.")
  if($set.Path -eq 'work\matrix-v013-metadata-fixed' -and $changed){throw 'Fixed sampler still records stale frames'}
 }
}
$lines.Add("Audited $total stage rows; $mismatches known stale metadata entries. Native inventories are independently original-derived. The fixed helper refreshes Timeline after action and asserts clip identity/count at every stage; production game is unchanged.")
$lines.Add('A failed initial audit attempt had a diagnostic-only CSV quote parser error before form startup. It is retained under work\matrix-v013-metadata-check with Failure-Note.txt and is excluded from played-duel totals.')
$lines | Set-Content -LiteralPath (Join-Path $root 'outputs\Natural-Frame-Metadata-Audit.txt') -Encoding UTF8
$lines | Select-Object -Last 3
