param(
 [Parameter(Mandatory=$true)][string]$RunDirectory,
 [Parameter(Mandatory=$true)][string]$Destination
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$run = (Resolve-Path -LiteralPath $RunDirectory).Path
$target = [IO.Path]::GetFullPath($Destination)
if (!$target.StartsWith(([IO.Path]::GetFullPath($root).TrimEnd('\')+'\'), [StringComparison]::OrdinalIgnoreCase)) { throw 'Use a workspace directory.' }
if (Test-Path -LiteralPath $target) { throw 'Use a fresh guard-check directory.' }
New-Item -ItemType Directory -Path $target | Out-Null
$cases = Join-Path $run 'Native-Target-Order-Cases.csv'
$draws = Join-Path $run 'Native-Target-Order-Draws.csv'
$check = Join-Path $run 'CheckTargetOrderTraces.exe'
$notes = New-Object 'System.Collections.Generic.List[string]'
function Invoke-Rejection([string]$Command, [string[]]$CommandArguments) {
 # Windows PowerShell 5 turns redirected native stderr into ErrorRecords.
 # Expected rejection must be inspected by exit status instead of becoming a
 # terminating harness exception before its assertions can run.
 $savedPreference = $ErrorActionPreference
 try {
  $ErrorActionPreference = 'Continue'
  $captured = & $Command @CommandArguments 2>&1
  $code = $LASTEXITCODE
 } finally { $ErrorActionPreference = $savedPreference }
 [pscustomobject]@{Code=$code; Output=$captured}
}
function Reject-Comparison([string]$Label, [string]$CasesFile, [string]$DrawsFile) {
 $report = Join-Path $target ($Label+'-unexpected-report.txt')
 $result = Invoke-Rejection $check @($CasesFile,$DrawsFile,$report)
 if ($result.Code -eq 0 -or (Test-Path -LiteralPath $report)) { throw "Comparison accepted $Label." }
 $result.Output | Out-File -LiteralPath (Join-Path $target ($Label+'.txt')) -Encoding UTF8
 $notes.Add("PASS comparison rejects $Label.")
}
$lines = [IO.File]::ReadAllLines($cases)
$columns = $lines[1].Split(','); $columns[7] = (([int]$columns[7]+1)%10).ToString(); $lines[1] = $columns -join ','
$badCases = Join-Path $target 'Altered-Target.csv'; [IO.File]::WriteAllLines($badCases,$lines)
Reject-Comparison 'altered-target' $badCases $draws
$lines = [IO.File]::ReadAllLines($draws)
$columns = $lines[2].Split(','); $columns[5] = '0'; $lines[2] = $columns -join ','
$badDraws = Join-Path $target 'Altered-State.csv'; [IO.File]::WriteAllLines($badDraws,$lines)
Reject-Comparison 'altered-state' $cases $badDraws
$lines = [IO.File]::ReadAllLines($draws)
$badDraws = Join-Path $target 'Missing-Boundary.csv'; [IO.File]::WriteAllLines($badDraws,$lines[0..($lines.Length-2)])
Reject-Comparison 'missing-final-boundary' $cases $badDraws
$wrongIdentityOutput = Join-Path $target 'wrong-identity'
$result = Invoke-Rejection 'powershell.exe' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'Run-TargetOrderProbe.ps1'),'-Executable',(Join-Path $run 'BattleshipLab.exe'),'-Destination',$wrongIdentityOutput)
if ($result.Code -eq 0 -or (Test-Path -LiteralPath $wrongIdentityOutput)) { throw 'Wrong identity reached allocation/output.' }
$result.Output | Out-File -LiteralPath (Join-Path $target 'wrong-identity.txt') -Encoding UTF8
$notes.Add('PASS wrong original identity rejects before output/helper allocation.')
Push-Location -LiteralPath $root
try {
 $wrongHelperOutput = Join-Path $target 'wrong-helper'
 $result = Invoke-Rejection (Join-Path $PSScriptRoot 'TargetOrderTrace.exe') @((Join-Path $run 'BattleshipLab.exe'),$wrongHelperOutput,(Join-Path $root 'work\reference-rip\Bs.exe'),$wrongHelperOutput)
 if ($result.Code -eq 0 -or (Test-Path -LiteralPath $wrongHelperOutput)) { throw 'Wrong helper reached launch/output.' }
 $result.Output | Out-File -LiteralPath (Join-Path $target 'wrong-helper.txt') -Encoding UTF8
 $notes.Add('PASS debugger rejects a non-owned helper path before launching or creating output.')
} finally { Pop-Location }
$notes.Add('Five failure-path checks; no existing game was started, attached to or terminated.')
$notes | Set-Content -LiteralPath (Join-Path $target 'Check.txt') -Encoding UTF8
$notes
