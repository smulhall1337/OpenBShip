param([Parameter(Mandatory=$true)][string]$RunDirectory,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$run=(Resolve-Path -LiteralPath $RunDirectory).Path;$target=[IO.Path]::GetFullPath($Destination)
if(!$target.StartsWith(([IO.Path]::GetFullPath($root).TrimEnd('\')+'\'),[StringComparison]::OrdinalIgnoreCase)){throw 'Use a workspace guard directory.'}
if(Test-Path -LiteralPath $target){throw 'Use a fresh guard directory.'}
New-Item -ItemType Directory -Path $target | Out-Null
$cases=Join-Path $run 'Native-Combined-Shot-Cases.csv';$events=Join-Path $run 'Native-Combined-Shot-Events.csv';$checker=Join-Path $run 'CheckCombinedShotTraces.exe'
$notes=New-Object 'Collections.Generic.List[string]'
function Invoke-Rejection([string]$Command,[string[]]$CommandArguments){
 $saved=$ErrorActionPreference
 try{$ErrorActionPreference='Continue';$output=& $Command @CommandArguments 2>&1;$code=$LASTEXITCODE}finally{$ErrorActionPreference=$saved}
 [pscustomobject]@{Code=$code;Output=$output}
}
function Reject([string]$Label,[string]$CasesFile,[string]$EventsFile){
 $report=Join-Path $target ($Label+'-unexpected-report.txt');$result=Invoke-Rejection $checker @($CasesFile,$EventsFile,$report)
 if($result.Code -eq 0 -or (Test-Path -LiteralPath $report)){throw "Checker accepted $Label."}
 $result.Output | Out-File -LiteralPath (Join-Path $target ($Label+'.txt')) -Encoding UTF8
 $notes.Add("PASS rejects $Label.")
}
foreach($kind in @('incoherent-initial-grid','altered-HP','altered-feedback')){
 $rows=[IO.File]::ReadAllLines($cases);$c=$rows[1].Split(',')
 if($kind -eq 'incoherent-initial-grid'){$v=$c[26].Split(';');$v[0]='8';$c[26]=$v -join ';'}
 elseif($kind -eq 'altered-HP'){$v=$c[25].Split(';');$v[0]=([int]$v[0]+1).ToString();$c[25]=$v -join ';'}
 else{$c[9]='0'}
 $rows[1]=$c -join ',';$bad=Join-Path $target ($kind+'.csv');[IO.File]::WriteAllLines($bad,$rows);Reject $kind $bad $events
}
foreach($kind in @('altered-state','wrong-target-call-site','premature-terminal','missing-final-boundary')){
 $rows=[IO.File]::ReadAllLines($events)
 if($kind -eq 'missing-final-boundary'){$rows=$rows[0..($rows.Length-2)]}
 else{
  $changed=$false
  for($i=1;$i -lt $rows.Length;$i++){$c=$rows[$i].Split(',')
   if($kind -eq 'altered-state' -and $c[2] -eq 'rand'){$c[5]='0';$changed=$true}
   elseif($kind -eq 'wrong-target-call-site' -and $c[3] -eq '0041CB4B'){$c[3]='0041CB5B';$changed=$true}
   elseif($kind -eq 'premature-terminal' -and $c[2] -eq 'FiringMovie'){$c[2]='TerminalStub';$changed=$true}
   if($changed){$rows[$i]=$c -join ',';break}
  }
  if(!$changed){throw 'Mutation did not find its intended event.'}
 }
 $bad=Join-Path $target ($kind+'.csv');[IO.File]::WriteAllLines($bad,$rows);Reject $kind $cases $bad
}
$wrong=Join-Path $target 'wrong-original'
$result=Invoke-Rejection 'powershell.exe' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'Run-CombinedShotProbe.ps1'),'-Executable',(Join-Path $run 'BattleshipLab.exe'),'-Destination',$wrong)
if($result.Code -eq 0 -or (Test-Path -LiteralPath $wrong)){throw 'Wrong original reached allocation.'}
$notes.Add('PASS wrong original identity rejects before output/helper allocation.')
Push-Location -LiteralPath $root
try{
 $wrong=Join-Path $target 'wrong-helper';$result=Invoke-Rejection (Join-Path $PSScriptRoot 'CombinedShotTrace.exe') @((Join-Path $run 'BattleshipLab.exe'),$wrong,(Join-Path $root 'work\reference-rip\Bs.exe'),$wrong)
 if($result.Code -eq 0 -or (Test-Path -LiteralPath $wrong)){throw 'Wrong helper reached launch.'}
 $notes.Add('PASS debugger rejects a non-owned helper path before output/launch.')
}finally{Pop-Location}
$notes.Add('Nine failure-path checks. No existing game process was started, attached to or terminated.')
$notes | Set-Content -LiteralPath (Join-Path $target 'Check.txt') -Encoding UTF8
$notes
