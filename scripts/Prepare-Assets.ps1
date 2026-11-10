param([Parameter(Mandatory=$true)][string]$OriginalRoot)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$original=(Resolve-Path -LiteralPath $OriginalRoot).Path
$output=Join-Path $root 'build\current'
$sourcePrefix=$original.TrimEnd('\')+'\'
if($output.StartsWith($sourcePrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Build output must be outside the supplied original files.'}
$exe=Join-Path $original 'Bs.exe'
if(!(Test-Path -LiteralPath $exe) -or (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash -ne 'B9A8AEFD1E5CED839670DF69B47C596175F38A3C99E33E584FFDD32D2006A569'){
 throw 'Expected the supported English Battleship Bs.exe in OriginalRoot. See README.md.'
}
$assets=Join-Path $output 'Assets';$targetPrefix=[IO.Path]::GetFullPath($assets)+'\'
$manifest=@(Import-Csv -LiteralPath (Join-Path $PSScriptRoot 'assets.csv'))
if($manifest.Count -ne 1491){throw 'Unexpected asset manifest.'}
foreach($record in $manifest){
 $source=[IO.Path]::GetFullPath((Join-Path $original $record.Asset))
 $target=[IO.Path]::GetFullPath((Join-Path $assets $record.Asset))
 if(!$source.StartsWith($sourcePrefix,[StringComparison]::OrdinalIgnoreCase) -or !$target.StartsWith($targetPrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Asset path escaped its directory.'}
 $item=Get-Item -LiteralPath $source
 if($item.Length -ne [long]$record.SizeBytes -or (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $record.SHA256){throw "Original asset differs: $($record.Asset)"}
 if(Test-Path -LiteralPath $target){
  if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $record.SHA256){throw "Existing modified asset retained: $target"}
  continue
 }
 New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
 Copy-Item -LiteralPath $source -Destination $target
}
& (Join-Path $PSScriptRoot 'Extract-Runtime.ps1') -Executable $exe -Destination (Join-Path $output 'Programs')
'Imported and verified 1,491 original assets. Supplied files were preserved.'
