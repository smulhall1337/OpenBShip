$ErrorActionPreference = 'Stop'
$gameDir = Join-Path $PSScriptRoot 'reference-rip'
$exe = Join-Path $gameDir 'Bs.exe'
$logDir = Join-Path $PSScriptRoot 'reference-logs'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$log = Join-Path $logDir ('launch-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '.txt')
"Reference: user-supplied Full-RIP; direct launch of Bs.exe" | Set-Content -LiteralPath $log
"Time: $([DateTimeOffset]::Now.ToString('o'))" | Add-Content -LiteralPath $log
"Executable: $exe" | Add-Content -LiteralPath $log
"SHA256: $((Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash)" | Add-Content -LiteralPath $log
try {
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $exe
    $startInfo.WorkingDirectory = $gameDir
    $startInfo.UseShellExecute = $false
    $process = [System.Diagnostics.Process]::Start($startInfo)
    "Started PID: $($process.Id). This does not establish visible rendering or gameplay." | Add-Content -LiteralPath $log
    Write-Host "Started Battleship (PID $($process.Id)). Launch log: $log"
    if ($process.WaitForExit(2000)) {
        $code = [BitConverter]::ToUInt32([BitConverter]::GetBytes([int]$process.ExitCode),0)
        $message = 'Exited during startup: 0x{0:X8}' -f $code
        $message | Add-Content -LiteralPath $log
        Write-Host $message
        if ($code -ne 0) { exit 1 }
    } else {
        'Still running after 2 seconds. Visual/audio/gameplay checks remain pending.' | Add-Content -LiteralPath $log
    }
} catch {
    "Launch failed: $($_.Exception.Message)" | Add-Content -LiteralPath $log
    Write-Host "Battleship launch failed: $($_.Exception.Message)"
    Write-Host "Launch log: $log"
    exit 1
}
