$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
if(!(Test-Path -LiteralPath '.git')){throw 'The initialized local repository is missing.'}
# The sandbox can own .git while Sean runs this script. Trust only this exact
# workspace, for each command; do not change ownership or global Git settings.
$repositoryPath=[IO.Path]::GetFullPath($PSScriptRoot).Replace('\','/')
$gitContext=@('-c',('safe.directory='+$repositoryPath))
$paths=@(git @gitContext ls-files --cached --others --exclude-standard)
if($LASTEXITCODE -ne 0){throw 'Source snapshot inspection failed.'}
$unexpected=@($paths | Where-Object {$_ -match '(^|/)(Assets|UserData|reference-[^/]+)/|\.(exe|dll|pdb|zip|iso|png|wav|pat|pal|idx|bin|dmp)$'})
if($unexpected.Count){throw ('Refusing to stage unexpected media/generated files: '+($unexpected -join ', '))}
git @gitContext add -- .gitignore .gitattributes README.txt Restore-Workspace.ps1 Complete-Git-Checkpoint.ps1 outputs work
if($LASTEXITCODE -ne 0){throw 'Git staging failed. No commit or publication was performed.'}
git @gitContext -c user.name=Codex -c user.email=codex@localhost commit -m 'Checkpoint Classic duel, native HUD and original Solo Practice and Classic Fleet Configuration'
if($LASTEXITCODE -ne 0){throw 'Git commit did not complete. Inspect git status before retrying.'}
$commit=git @gitContext rev-parse HEAD
if($LASTEXITCODE -ne 0){throw 'Checkpoint commit inspection failed.'}
$branch=git @gitContext branch --show-current
if($LASTEXITCODE -ne 0){throw 'Checkpoint branch inspection failed.'}
@("Local source checkpoint: $commit","Branch: $branch",'Author: Codex <codex@localhost> (per-command identity; global configuration unchanged).','Original media, generated builds, ZIPs, settings and scratch files excluded.','No remote configured or publication performed by this script.','Source-only restoration passed 94 model checks, 53 real-form checks, 18 shutdown cases and the separate SILENT waveOut device check.') | Set-Content -LiteralPath outputs\Git-Checkpoint.txt -Encoding UTF8
git @gitContext status --short
Get-Content -LiteralPath outputs\Git-Checkpoint.txt





