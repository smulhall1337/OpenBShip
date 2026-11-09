param(
 [Parameter(Mandatory=$true)][string]$Executable,
 [Parameter(Mandatory=$true)][string]$Report,
 [string]$TitlePrefix='Battleship Fleet Lab '
)
$ErrorActionPreference='Stop'
$executablePath=(Resolve-Path -LiteralPath $Executable).Path
if(!$TitlePrefix){throw 'A nonempty expected title prefix is required.'}
Add-Type -TypeDefinition (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'OwnedWindowCheck.cs') -Raw)
$process=Start-Process -FilePath $executablePath -WindowStyle Hidden -PassThru
$found='No matching main form'
for($attempt=0;$attempt -lt 20;$attempt++){Start-Sleep -Milliseconds 250;$found=[OwnedWindowCheck]::Close($process.Id,$TitlePrefix);if($found -match 'posted=True'){break};if($process.HasExited){break}}
if($found -notmatch 'posted=True'){throw "Owned startup did not reach expected form: PID $($process.Id). No unrelated process was terminated."}
$done=$process.WaitForExit(10000)
if(!$done){throw "Owned process did not exit after WM_CLOSE: PID $($process.Id). No forced termination was attempted."}
$exit=[OwnedWindowCheck]::ExitCode($process.Handle)
if($exit -ne 0){throw "Owned startup exited with code $exit"}
@('PASS owned packaged no-argument startup reached form/message loop.',$found,"Closed=$done, exit=$exit.",'Only this newly started hidden process was closed; no user game terminated. Physical input/audible playback separate.') | Set-Content -LiteralPath $Report -Encoding UTF8
Get-Content -LiteralPath $Report
