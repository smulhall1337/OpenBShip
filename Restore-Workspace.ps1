param(
 [Parameter(Mandatory=$true)][string]$OriginalRoot,
 [switch]$WithChecks,
 [switch]$AudioDeviceCheck
)
$ErrorActionPreference='Stop'
$package=Join-Path $PSScriptRoot 'outputs\Battleship-Lab-v0.30'
$original=(Resolve-Path -LiteralPath $OriginalRoot).Path
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if(!(Test-Path -LiteralPath $compiler)){throw 'The existing Windows .NET Framework compiler is required.'}
$running=@(Get-Process -Name BattleshipLab -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq (Join-Path $package 'BattleshipLab.exe')})
if($running.Count -gt 0){throw 'This version is running. Close it yourself before rebuilding; no process was terminated.'}
$assets=Join-Path $package 'Assets'
$prefix=[IO.Path]::GetFullPath($assets)+'\'
$sourcePrefix=[IO.Path]::GetFullPath($original)+'\'
foreach($record in Import-Csv -LiteralPath (Join-Path $package 'Asset-Provenance.csv')){
 $source=[IO.Path]::GetFullPath((Join-Path $original $record.Asset))
 $target=[IO.Path]::GetFullPath((Join-Path $assets $record.Asset))
 if(!$target.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or !$source.StartsWith($sourcePrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Asset path escaped its declared directory.'}
 if((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $record.SHA256){throw "Original asset identity differs: $($record.Asset)"}
 if(Test-Path -LiteralPath $target){if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $record.SHA256){throw "Existing modified asset retained: $target"};continue}
 New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
 Copy-Item -LiteralPath $source -Destination $target
}
$tools=Join-Path $package 'Tools';$evidence=Join-Path $package 'Evidence';$exe=Join-Path $original 'Bs.exe'
# Runtime ending/menu prefixes are needed even when fixture checks are omitted.
& (Join-Path $tools 'Extract-EndingFlow.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-MenuEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-SoloEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-FleetEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-EntryEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-MessageEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-PreferencesEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-CreditsEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-CreditsCatalogEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-QuitEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
& (Join-Path $tools 'Extract-StartupEvidence.ps1') -Executable $exe -Destination $evidence -ProgramsDirectory (Join-Path $package 'Programs') | Out-Null
if($WithChecks){
 $exe=Join-Path $original 'Bs.exe';$graphics=Join-Path $original 'GRAPHICS';$evidence=Join-Path $package 'Evidence';$tools=Join-Path $package 'Tools'
 foreach($name in @('Extract-ClassicEvidence.ps1','Extract-ClassicOpponent.ps1','Extract-ClassicDisplay.ps1','Extract-ClassicWeapons.ps1','Extract-ClassicPat.ps1','Extract-ClassicSound.ps1','Extract-ClassicShot.ps1','Extract-ClassicImpact.ps1','Extract-ClassicLifecycle.ps1','Extract-ClassicHud.ps1','Extract-ClassicInput.ps1','Extract-CreditsSeamEvidence.ps1','Extract-ResultSeamEvidence.ps1','Extract-PlacementEvidence.ps1','Extract-NavigationEvidence.ps1','Extract-EndingEvidence.ps1')){& (Join-Path $tools $name) -Executable $exe -Destination $evidence | Out-Null}
 & (Join-Path $tools 'Extract-ClassicPresentation.ps1') -Executable $exe -Graphics $graphics -Destination $evidence | Out-Null
 & (Join-Path $tools 'Export-FiringInventory.ps1') -Game $original -Destination $evidence
 & (Join-Path $tools 'Export-CombatInventory.ps1') -Game $original -Destination $evidence
 & (Join-Path $tools 'Export-ImpactInventory.ps1') -Game $original -Destination $evidence
 & (Join-Path $tools 'Export-HighInventory.ps1') -Game $original -Destination $evidence
 & (Join-Path $tools 'Export-EndingInventory.ps1') -Game $original -Destination $evidence
 foreach($name in @('ClassicTargetOracle','ClassicPaletteOracle','ClassicWeaponsOracle','ClassicPatOracle','ClassicFiringPatOracle','ClassicSequenceOracle','ClassicShotPatOracle','ClassicCombatSequenceOracle','ClassicPhaseOracle','ClassicImpactOracle','ClassicImpactPatOracle','ClassicImpactSequenceOracle','ClassicHighPatOracle','ClassicHighSequenceOracle','ClassicMovieAdmissionOracle','ClassicHudOracle','ClassicScaleOracle','ClassicInputOracle','EndingPatOracle','EndingSequenceOracle','EndingProgramOracle','MenuOracle','SoloOracle','FleetOracle','EntryOracle','MessageOracle','NavigationOracle','PreferencesOracle','CreditsOracle','PlacementOracle','CreditsCatalogPatOracle','CreditsCatalogRequestOracle','CreditsCatalogSequenceOracle','CreditsSeamOracle','QuitOracle','StartupPatOracle','StartupSequenceOracle','StartupProgramOracle','StartupPaletteOracle','ResultPaletteOracle','EndingSeamOracle')){
  & $compiler /nologo /target:exe /platform:x86 "/out:$tools\$name.exe" (Join-Path $tools "$name.cs")
  if($LASTEXITCODE -ne 0){throw "Fixture tool build failed: $name"}
 }
 & (Join-Path $tools 'ClassicTargetOracle.exe') $exe (Join-Path $evidence 'Original-Target-Cases.csv');if($LASTEXITCODE -ne 0){throw 'Target oracle failed'}
 & (Join-Path $tools 'ClassicPaletteOracle.exe') $exe (Join-Path $graphics 'ISLAND.COL') (Join-Path $evidence 'Palette');if($LASTEXITCODE -ne 0){throw 'Palette oracle failed'}
 & (Join-Path $tools 'ClassicWeaponsOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Weapon oracle failed'}
 & (Join-Path $tools 'ClassicPatOracle.exe') $exe (Join-Path $original 'LWEBS\BATFIRE1.PAT') $evidence;if($LASTEXITCODE -ne 0){throw 'PAT oracle failed'}
 foreach($name in @('ClassicFiringPatOracle','ClassicShotPatOracle','ClassicImpactPatOracle')){& (Join-Path $tools "$name.exe") $exe (Join-Path $original 'LWEBS') $evidence;if($LASTEXITCODE -ne 0){throw "Decoder oracle failed: $name"}}
 foreach($name in @('ClassicSequenceOracle','ClassicCombatSequenceOracle','ClassicImpactSequenceOracle')){& (Join-Path $tools "$name.exe") $exe $package $evidence;if($LASTEXITCODE -ne 0){throw "Sequence oracle failed: $name"}}
 & (Join-Path $tools 'ClassicPhaseOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Phase oracle failed'}
 & (Join-Path $tools 'ClassicHighPatOracle.exe') $exe (Join-Path $original 'HWEBS') $evidence;if($LASTEXITCODE -ne 0){throw 'High decoder oracle failed'}
 & (Join-Path $tools 'ClassicHighSequenceOracle.exe') $exe $package $evidence;if($LASTEXITCODE -ne 0){throw 'High sequence oracle failed'}
 & (Join-Path $tools 'EndingPatOracle.exe') $exe (Join-Path $original 'HWEBS') $evidence;if($LASTEXITCODE -ne 0){throw 'Ending decoder oracle failed'}
 & (Join-Path $tools 'EndingSequenceOracle.exe') $exe $package $evidence;if($LASTEXITCODE -ne 0){throw 'Ending sequence oracle failed'}
 & (Join-Path $tools 'EndingProgramOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Ending program oracle failed'}
 & (Join-Path $tools 'MenuOracle.exe') $exe $graphics $evidence;if($LASTEXITCODE -ne 0){throw 'Menu oracle failed'}
 & (Join-Path $tools 'SoloOracle.exe') $exe $graphics $evidence;if($LASTEXITCODE -ne 0){throw 'Solo oracle failed'}
 & (Join-Path $tools 'FleetOracle.exe') $exe $graphics $evidence;if($LASTEXITCODE -ne 0){throw 'Fleet oracle failed'}
 & (Join-Path $tools 'EntryOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Entry oracle failed'}
 & (Join-Path $tools 'MessageOracle.exe') $exe $graphics $evidence;if($LASTEXITCODE -ne 0){throw 'Message oracle failed'}
 & (Join-Path $tools 'NavigationOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Navigation oracle failed'}
 & (Join-Path $tools 'PreferencesOracle.exe') $exe $graphics $evidence;if($LASTEXITCODE -ne 0){throw 'Preferences oracle failed'}
 & (Join-Path $tools 'CreditsOracle.exe') $exe $graphics $evidence;if($LASTEXITCODE -ne 0){throw 'Credits oracle failed'}
 & (Join-Path $tools 'PlacementOracle.exe') $exe $graphics $evidence;if($LASTEXITCODE -ne 0){throw 'Placement oracle failed'}
 & (Join-Path $tools 'CreditsCatalogPatOracle.exe') $exe $original $evidence;if($LASTEXITCODE -ne 0){throw 'Credits catalog decoder failed'}
 & (Join-Path $tools 'CreditsCatalogRequestOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Credits catalog requests failed'}
 & (Join-Path $tools 'CreditsCatalogSequenceOracle.exe') $exe $original $evidence;if($LASTEXITCODE -ne 0){throw 'Credits catalog sequence failed'}
 & (Join-Path $tools 'CreditsSeamOracle.exe') $exe $graphics $evidence;if($LASTEXITCODE -ne 0){throw 'Credits seam oracle failed'}
 & (Join-Path $tools 'QuitOracle.exe') $exe $graphics $evidence;if($LASTEXITCODE -ne 0){throw 'Quit oracle failed'}
 & (Join-Path $tools 'StartupPatOracle.exe') $exe $original $evidence;if($LASTEXITCODE -ne 0){throw 'Startup decoder failed'}
 & (Join-Path $tools 'StartupSequenceOracle.exe') $exe $original $evidence;if($LASTEXITCODE -ne 0){throw 'Startup sequence failed'}
 & (Join-Path $tools 'StartupProgramOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Startup program failed'}
 & (Join-Path $tools 'StartupPaletteOracle.exe') $exe (Join-Path $package 'Assets') $evidence;if($LASTEXITCODE -ne 0){throw 'Startup palette failed'}
 & (Join-Path $tools 'ResultPaletteOracle.exe') $exe (Join-Path $package 'Assets') $evidence;if($LASTEXITCODE -ne 0){throw 'Result palette failed'}
 & (Join-Path $tools 'EndingSeamOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Ending seam failed'}
 & (Join-Path $tools 'ClassicMovieAdmissionOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Admission oracle failed'}
 foreach($name in @('ClassicHudOracle','ClassicScaleOracle','ClassicInputOracle')){& (Join-Path $tools "$name.exe") $exe $evidence;if($LASTEXITCODE -ne 0){throw "HUD/scale oracle failed: $name"}}
 & (Join-Path $tools 'Check-ClassicInput.ps1') -Evidence $evidence
 & (Join-Path $tools 'Check-ClassicMovieAdmission.ps1') -Fixture (Join-Path $evidence 'Classic-Movie-Admission-Oracle.csv')
 & (Join-Path $tools 'ClassicImpactOracle.exe') $exe $evidence;if($LASTEXITCODE -ne 0){throw 'Impact selector oracle failed'}
}
& (Join-Path $package 'Build.cmd');if($LASTEXITCODE -ne 0){throw 'Game build failed'}
if($WithChecks){& (Join-Path $package 'Verify.cmd');if($LASTEXITCODE -ne 0){throw 'Validation failed'}}
if($AudioDeviceCheck){$process=Start-Process -FilePath (Join-Path $package 'BattleshipLab.exe') -ArgumentList '--audio-check',('"'+(Join-Path $package 'Audio-Device-Check.txt')+'"') -WindowStyle Hidden -Wait -PassThru;if($process.ExitCode -ne 0){throw 'Silent device check failed'}}
"Ready: $package\BattleshipLab.exe"











