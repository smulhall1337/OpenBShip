param()
$ErrorActionPreference='Stop'
$output=Join-Path $PSScriptRoot 'build\current'
$executable=Join-Path $output 'BattleshipLab.exe'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if(!(Test-Path -LiteralPath $compiler)){throw 'The Windows .NET Framework 4.x C# compiler is required.'}
foreach($process in @(Get-Process -Name BattleshipLab -ErrorAction SilentlyContinue)){
 if($process.Path -eq $executable){throw 'Close this build before rebuilding. No game was terminated.'}
}
$sources=@(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -File -Filter '*.cs' | Sort-Object Name | ForEach-Object {$_.FullName})
if(!$sources.Count){throw 'No C# source files found in src.'}
New-Item -ItemType Directory -Path $output -Force | Out-Null
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /debug:pdbonly /main:FleetLab.Program /r:System.Drawing.dll /r:System.Windows.Forms.dll "/out:$executable" @sources
if($LASTEXITCODE -ne 0){throw 'Build failed.'}
"Built $executable"
