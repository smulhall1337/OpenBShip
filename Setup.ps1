param([Parameter(Mandatory=$true)][string]$OriginalRoot)
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'scripts\Prepare-Assets.ps1') -OriginalRoot $OriginalRoot
& (Join-Path $PSScriptRoot 'Build.ps1')
'Ready. Run Launch.cmd to play.'
