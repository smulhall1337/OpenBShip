$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$package=Join-Path $root 'outputs\Battleship-Lab-v0.26'
$rip='C:\Users\Sean\Documents\Battleship-The-Classic-Naval-Warfare-Game_Win_EN_ISO-Version\Battleship-The-Classic-Naval-Warfare-Game_Win_EN_Full-RIP\Battleship_The_Classic_Naval_Warfare_Game'
$records=foreach($file in Get-ChildItem -LiteralPath (Join-Path $package 'Assets') -Recurse -File | Sort-Object FullName) {
    $relative=$file.FullName.Substring((Join-Path $package 'Assets').Length+1)
    $original=Join-Path $rip $relative
    $hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if($hash -ne (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash){throw "Asset differs from supplied original: $relative"}
    [pscustomobject]@{Asset=$relative;SizeBytes=$file.Length;SHA256=$hash;Source=$original;Altered=$false}
}
$records | Export-Csv -LiteralPath (Join-Path $package 'Asset-Provenance.csv') -NoTypeInformation
if(@($records).Count -ne 1439){throw 'Unexpected asset count'}
$dependencyURL='https://api.nuget.org/v3-flatcontainer/iced/1.21.0/iced.1.21.0.nupkg'
$dependencies=foreach($name in @('Iced.dll','Iced-LICENSE.txt')) {
    $file=Get-Item -LiteralPath (Join-Path $package "Tools\$name")
    [pscustomobject]@{File="Tools\$name";Version='1.21.0';License='MIT';Bytes=$file.Length;SHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash;ArchiveSHA256='D31613617E3DDF91239BBC94A8C587849B4236E8F8A2A2BA59EA2397A1487C7A';Source=$dependencyURL}
}
$dependencies | Export-Csv -LiteralPath (Join-Path $package 'Dependency-Provenance.csv') -NoTypeInformation
$integrity=New-Object 'Collections.Generic.List[string]'
$historyReport=Join-Path $PSScriptRoot 'history-integrity.txt'
& (Join-Path $root 'work\analysis-tools\Check-WorkspaceIntegrity.ps1') -Workspace $root -Package $package -Report $historyReport | Out-Null
foreach($line in Get-Content -LiteralPath $historyReport){$integrity.Add($line)}
$exe=[IO.File]::ReadAllBytes((Join-Path $package 'BattleshipLab.exe'))
$pe=[BitConverter]::ToInt32($exe,60)
if([BitConverter]::ToUInt16($exe,$pe+4) -ne 0x8664 -or [BitConverter]::ToUInt16($exe,$pe+24) -ne 0x20b){throw 'Game is not PE32+ x64'}
$integrity.Add('PASS delivered v0.26 game executable is PE32+ machine 0x8664 (Windows x64).')
$integrity.Add('PASS 1439 packaged original assets match the user-supplied original files byte for byte by SHA-256.')
$integrity | Set-Content -LiteralPath (Join-Path $package 'Integrity-Check.txt') -Encoding UTF8
$manifest=foreach($file in Get-ChildItem -LiteralPath $package -Recurse -File | Sort-Object FullName) {
    $relative=$file.FullName.Substring($package.Length+1)
    if($relative -eq 'Build-Manifest.csv' -or $relative.StartsWith('UserData\')){continue}
    [pscustomobject]@{Path=$relative;Bytes=$file.Length;SHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash}
}
$manifest | Export-Csv -LiteralPath (Join-Path $package 'Build-Manifest.csv') -NoTypeInformation
$zip=Join-Path $root 'outputs\Battleship-Lab-v0.26.zip'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archiveStream=[IO.File]::Open($zip,[IO.FileMode]::Create,[IO.FileAccess]::Write)
$archive=New-Object IO.Compression.ZipArchive($archiveStream,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach($file in Get-ChildItem -LiteralPath $package -Recurse -File | Sort-Object FullName) {
        $relative=$file.FullName.Substring($package.Length+1)
        if($relative.StartsWith('UserData\')){continue}
        $entryName='Battleship-Lab-v0.26/'+$relative.Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$entryName,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally {$archive.Dispose();$archiveStream.Dispose()}
@(
    [pscustomobject]@{Path=$zip;Hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash},
    [pscustomobject]@{Path=(Join-Path $package 'BattleshipLab.exe');Hash=(Get-FileHash -LiteralPath (Join-Path $package 'BattleshipLab.exe') -Algorithm SHA256).Hash}
) | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'delivered-hashes.json') -Encoding UTF8
$integrity
Get-Item -LiteralPath $zip | Select-Object FullName,Length







