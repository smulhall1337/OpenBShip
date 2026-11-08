param([string]$Workspace=(Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $Workspace).Path
$sets=@(
 @{Version='0.11';Path='work\natural-cinematic-v011-level0';Reports=@('Report')},
 @{Version='0.11';Path='work\matrix-v011-low-muted';Reports=@('Level-1','Level-2','Level-3')},
 @{Version='0.12';Path='work\matrix-v012-high-muted';Reports=@('Level-0','Level-1','Level-2','Level-3')},
 @{Version='0.12';Path='work\matrix-v012-high-silent';Reports=@('Level-0','Level-1','Level-2','Level-3')},
 @{Version='0.13';Path='work\matrix-v013-low-muted';Reports=@('Level-0','Level-1','Level-2','Level-3')},
 @{Version='0.13';Path='work\matrix-v013-high-silent';Reports=@('Level-0','Level-1','Level-2','Level-3')},
 @{Version='0.13';Path='work\matrix-v013-metadata-fixed';Reports=@('Level-0')}
)
$rows=New-Object 'Collections.Generic.List[object]'
$modules=New-Object 'Collections.Generic.List[string]'
$pending=0
$coverage=@{}
foreach($set in $sets){
 $folder=Join-Path $root $set.Path
 $module=Join-Path $folder 'BattleshipLab.exe'
 $modules.Add("$($set.Path) : $((Get-FileHash -LiteralPath $module -Algorithm SHA256).Hash)")
 $frozen=Join-Path $root "outputs\Battleship-Lab-v$($set.Version)\BattleshipLab.exe"
 if((Get-FileHash -LiteralPath $module).Hash -ne (Get-FileHash -LiteralPath $frozen).Hash){throw "Diagnostic module differs from version $($set.Version)"}
 foreach($sub in $set.Reports){
  $scenario=Join-Path $folder $sub;$result=Join-Path $scenario 'Result.txt'
  if(!(Test-Path -LiteralPath $result)){$pending++;continue}
  $text=Get-Content -LiteralPath $result -Raw
  if(!$text.StartsWith('PASS')){throw "Scenario failed: $result"}
  $stats=[regex]::Match($text,'Player shots=(\d+), movie stages=(\d+), elapsed seconds=(\d+), max observed GDI handles=(\d+)')
  $level=[regex]::Match($text,'opponent level (\d+)').Groups[1].Value
  $detail=[regex]::Match($text,'Detail=(\d+x\d+)').Groups[1].Value;if(!$detail -and $set.Version -eq '0.11'){$detail='160x120'}
  $device=$text.Contains('actual waveOut')
  $starts=[regex]::Match($text,'buffer starts=(\d+), pause calls=(\d+), resumes=(\d+)')
  $stages=@(Import-Csv -LiteralPath (Join-Path $scenario 'Stages.csv'))
  if(!$stats.Success -or $stages.Count -ne [int]$stats.Groups[2].Value){throw "Stage count differs: $result"}
    $coverageKey=$set.Version+'/'+$detail
  if(!$coverage.ContainsKey($coverageKey)){$coverage[$coverageKey]=New-Object 'Collections.Generic.HashSet[string]'}
  foreach($stageRow in $stages){[void]$coverage[$coverageKey].Add($stageRow.Clip)}
  $memory=$stages | Measure-Object -Property PrivateBytes -Minimum -Maximum
  $rows.Add([pscustomobject]@{Version=$set.Version;Level=$level;Detail=$detail;Audio=$(if($device){'silent-device'}else{'muted'});Shots=[int]$stats.Groups[1].Value;Stages=$stages.Count;Seconds=[int]$stats.Groups[3].Value;MaxGDI=[int]$stats.Groups[4].Value;Starts=[int]$starts.Groups[1].Value;Pauses=[int]$starts.Groups[2].Value;Resumes=[int]$starts.Groups[3].Value;PeakPrivateMiB=[math]::Round($memory.Maximum/1MB,1);Report=$set.Path+'\'+$sub})
 }
}
$report=New-Object 'Collections.Generic.List[string]'
$report.Add('Naturally paced cinematic integration validation - '+[DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss')+' UTC')
$report.Add('')
$report.Add("Completed scripted duels=$($rows.Count), pending=$pending. Each completed duel reached victory with natural movie completion, no skips, and checked frozen frame/damage during pause.")
$report.Add("Totals: $((($rows | Measure-Object Shots -Sum).Sum)) player shots; $((($rows | Measure-Object Stages -Sum).Sum)) movie stages; $((($rows | Measure-Object Starts -Sum).Sum)) real-device SILENT buffer starts. These are integration events, not distinct gameplay scenarios or a fidelity score.")
$report.Add('')
$report.Add('Version Level Detail Audio Shots Stages Seconds MaxGDI Starts Pause Resume PeakPrivateMiB')
foreach($row in $rows){$report.Add(($row.Version,$row.Level,$row.Detail,$row.Audio,$row.Shots,$row.Stages,$row.Seconds,$row.MaxGDI,$row.Starts,$row.Pauses,$row.Resumes,$row.PeakPrivateMiB -join ' '))}
$report.Add('')
$report.Add('Method: owned offscreen production form and timers, original targeting levels0..3, fixed seed731, scripted perfect-target player knowledge for diagnostic brevity. Natural Firing->Travel->Feedback completion and final sink. Pause650ms during second travel; frame and both hit histories checked frozen. No stage skip calls. Real-device mode submits zero-valued PCM of the original durations to x64 waveOut and rejects fallback/errors; it is deliberately SILENT. Muted mode uses the visual clock. Windows scheduling can change elapsed time.')
$report.Add('GDI handles sampled at stage changes; this does not enumerate all GDI+ allocations or prove absence of memory leaks. Private-memory peaks are recorded to bound these particular runs. No physical focus/input, audible output/perceived sync, human difficulty assessment or full original-gameplay equivalence is established.')
$report.Add('')
$report.Add('Observed natural clip coverage (61supported clips per quality; decoding/library tests cover all61, these duels cover the following subset):')
foreach($key in ($coverage.Keys | Sort-Object)){$report.Add($key+' : '+$coverage[$key].Count+' distinct clips; '+(($coverage[$key] | Sort-Object) -join ', '))}
$report.Add('')
$report.Add('Copied diagnostic executable SHA256 values (each verified against its preserved version):')
foreach($entry in $modules){$report.Add($entry)}
$report.Add('')
$report.Add('Tools: work\analysis-tools\NaturalCinematicCheck.cs and Run-Natural-Matrix.ps1. Raw per-stage CSV, per-run Result.txt, First-Hit.png, First-Sink.png and Victory.png remain under the listed work directories. Original media, user games and preferences were not modified. Separate source/model/form/device/startup checks are not included in the duel totals.')
$report.Add('Frame metadata: see Natural-Frame-Metadata-Audit.txt. Earlier Player/Firing rows could capture the preceding Timeline count. Stage totals/terminal/pause/audio checks remain valid; original logs are retained. One additional level0duel verifies the corrected sampler against native clip identities/counts at all111stages. The quote-parser attempt before that started no game and is excluded. Repeated level0is a diagnostic regression, not a new difficulty scenario.')
$report.Add('Scenario records:')
foreach($row in $rows){$report.Add($row.Report+'\Result.txt')}
$report | Set-Content -LiteralPath (Join-Path $root 'outputs\Natural-Cinematic-Validation.txt') -Encoding UTF8
$report | Select-Object -First 7



