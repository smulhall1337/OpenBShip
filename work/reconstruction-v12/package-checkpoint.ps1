$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$package=Join-Path $root 'outputs\Battleship-Lab-v0.12'
$rip='C:\Users\Sean\Documents\Battleship-The-Classic-Naval-Warfare-Game_Win_EN_ISO-Version\Battleship-The-Classic-Naval-Warfare-Game_Win_EN_Full-RIP\Battleship_The_Classic_Naval_Warfare_Game'
$records=foreach($file in Get-ChildItem -LiteralPath (Join-Path $package 'Assets') -Recurse -File | Sort-Object FullName) {
    $relative=$file.FullName.Substring((Join-Path $package 'Assets').Length+1)
    $original=Join-Path $rip $relative
    $hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if($hash -ne (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash){throw "Asset differs from supplied original: $relative"}
    [pscustomobject]@{Asset=$relative;SizeBytes=$file.Length;SHA256=$hash;Source=$original;Altered=$false}
}
$records | Export-Csv -LiteralPath (Join-Path $package 'Asset-Provenance.csv') -NoTypeInformation
if(@($records).Count -ne 489){throw 'Unexpected asset count'}
$dependencyURL='https://api.nuget.org/v3-flatcontainer/iced/1.21.0/iced.1.21.0.nupkg'
$dependencies=foreach($name in @('Iced.dll','Iced-LICENSE.txt')) {
    $file=Get-Item -LiteralPath (Join-Path $package "Tools\$name")
    [pscustomobject]@{File="Tools\$name";Version='1.21.0';License='MIT';Bytes=$file.Length;SHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash;ArchiveSHA256='D31613617E3DDF91239BBC94A8C587849B4236E8F8A2A2BA59EA2397A1487C7A';Source=$dependencyURL}
}
$dependencies | Export-Csv -LiteralPath (Join-Path $package 'Dependency-Provenance.csv') -NoTypeInformation
$integrity=New-Object 'Collections.Generic.List[string]'
$baseline=Get-Content -LiteralPath (Join-Path $root 'work\checkpoints\menu-baseline-001\baseline.json') -Raw | ConvertFrom-Json
foreach($file in $baseline.Files) {
    if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.SHA256){throw "Accepted original reference changed: $($file.Path)"}
}
$integrity.Add('PASS accepted original reference executable, wrapper, configuration and launcher match menu-baseline-001.')
$v1=Join-Path $root 'outputs\Battleship-Lab-v0.1'
$v1files=Import-Csv -LiteralPath (Join-Path $v1 'Build-Manifest.csv')
foreach($file in $v1files) {
    if((Get-FileHash -LiteralPath (Join-Path $v1 $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256){throw "Accepted v0.1 file changed: $($file.Path)"}
}
$delivered=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v01\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $delivered) {
    if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash){throw "Accepted v0.1 delivery changed: $($file.Path)"}
}
$integrity.Add("PASS $(@($v1files).Count) accepted v0.1 manifest files and delivered exe/ZIP hashes are unchanged.")
$v2=Join-Path $root 'outputs\Battleship-Lab-v0.2'
$v2files=Import-Csv -LiteralPath (Join-Path $v2 'Build-Manifest.csv')
foreach($file in $v2files) {
    if((Get-FileHash -LiteralPath (Join-Path $v2 $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256){throw "Preserved v0.2 file changed: $($file.Path)"}
}
$previous=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v02\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous) {
    if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash){throw "Preserved v0.2 delivery changed: $($file.Path)"}
}
$integrity.Add("PASS $(@($v2files).Count) v0.2 manifest files and delivered exe/ZIP hashes are unchanged.")
$v3=Join-Path $root 'outputs\Battleship-Lab-v0.3'
$v3files=Import-Csv -LiteralPath (Join-Path $v3 'Build-Manifest.csv')
foreach($file in $v3files) {
    if((Get-FileHash -LiteralPath (Join-Path $v3 $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256){throw "Accepted v0.3 file changed: $($file.Path)"}
}
$previous3=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v03\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous3) {
    if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash){throw "Accepted v0.3 delivery changed: $($file.Path)"}
}
$integrity.Add("PASS $(@($v3files).Count) accepted v0.3 manifest files and delivered exe/ZIP hashes are unchanged.")
$v4=Join-Path $root 'outputs\Battleship-Lab-v0.4'
$v4files=Import-Csv -LiteralPath (Join-Path $v4 'Build-Manifest.csv')
foreach($file in $v4files) {
    if((Get-FileHash -LiteralPath (Join-Path $v4 $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256){throw "Preserved v0.4 file changed: $($file.Path)"}
}
$previous4=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v04\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous4) {
    if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash){throw "Preserved v0.4 delivery changed: $($file.Path)"}
}
$integrity.Add("PASS $(@($v4files).Count) v0.4 manifest files and delivered exe/ZIP hashes are unchanged.")
$v5=Join-Path $root 'outputs\Battleship-Lab-v0.5'
$v5files=Import-Csv -LiteralPath (Join-Path $v5 'Build-Manifest.csv')
foreach($file in $v5files) {
    if((Get-FileHash -LiteralPath (Join-Path $v5 $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256){throw "Preserved v0.5 file changed: $($file.Path)"}
}
$previous5=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v05\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous5) {
    if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash){throw "Preserved v0.5 delivery changed: $($file.Path)"}
}
$integrity.Add("PASS $(@($v5files).Count) v0.5 manifest files and delivered exe/ZIP hashes are unchanged.")
$v6=Join-Path $root 'outputs\Battleship-Lab-v0.6'
$v6files=Import-Csv -LiteralPath (Join-Path $v6 'Build-Manifest.csv')
foreach($file in $v6files) {
    if((Get-FileHash -LiteralPath (Join-Path $v6 $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256){throw "Preserved v0.6 file changed: $($file.Path)"}
}
$previous6=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v06\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous6) {
    if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash){throw "Preserved v0.6 delivery changed: $($file.Path)"}
}
$integrity.Add("PASS $(@($v6files).Count) v0.6 manifest files and delivered exe/ZIP hashes are unchanged.")
$v7=Join-Path $root 'outputs\Battleship-Lab-v0.7'
$v7files=Import-Csv -LiteralPath (Join-Path $v7 'Build-Manifest.csv')
foreach($file in $v7files) {
    if((Get-FileHash -LiteralPath (Join-Path $v7 $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256){throw "Preserved v0.7 file changed: $($file.Path)"}
}
$previous7=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v07\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous7) {
    if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash){throw "Preserved v0.7 delivery changed: $($file.Path)"}
}
$integrity.Add("PASS $(@($v7files).Count) v0.7 manifest files and delivered exe/ZIP hashes are unchanged.")
$v8=Join-Path $root 'outputs\Battleship-Lab-v0.8'
$v8files=Import-Csv -LiteralPath (Join-Path $v8 'Build-Manifest.csv')
foreach($file in $v8files){if((Get-FileHash -LiteralPath (Join-Path $v8 $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256){throw "Preserved v0.8 file changed: $($file.Path)"}}
$previous8=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v08\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous8){if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash){throw "Preserved v0.8 delivery changed: $($file.Path)"}}
$integrity.Add("PASS $(@($v8files).Count) v0.8 manifest files and delivered exe/ZIP hashes are unchanged.")
$v9=Join-Path $root 'outputs\Battleship-Lab-v0.9'
$v9files=Import-Csv -LiteralPath (Join-Path $v9 'Build-Manifest.csv')
foreach($file in $v9files){if((Get-FileHash -LiteralPath (Join-Path $v9 $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256){throw "Preserved v0.9 file changed: $($file.Path)"}}
$previous9=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v09\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous9){if((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash -ne $file.Hash){throw "Preserved v0.9 delivery changed: $($file.Path)"}}
$integrity.Add("PASS $(@($v9files).Count) v0.9 manifest files and delivered exe/ZIP hashes are unchanged.")
$v10=Join-Path $root 'outputs\Battleship-Lab-v0.10'
$v10files=Import-Csv -LiteralPath (Join-Path $v10 'Build-Manifest.csv')
foreach($file in $v10files){if((Get-FileHash -LiteralPath (Join-Path $v10 $file.Path)).Hash -ne $file.SHA256){throw "Preserved v0.10 file changed: $($file.Path)"}}
$previous10=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v10\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous10){if((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Hash){throw "Preserved v0.10 delivery changed: $($file.Path)"}}
$integrity.Add("PASS $(@($v10files).Count) v0.10 manifest files and delivered exe/ZIP hashes are unchanged.")
$v11=Join-Path $root 'outputs\Battleship-Lab-v0.11'
$v11files=Import-Csv -LiteralPath (Join-Path $v11 'Build-Manifest.csv')
foreach($file in $v11files){if((Get-FileHash -LiteralPath (Join-Path $v11 $file.Path)).Hash -ne $file.SHA256){throw "Preserved v0.11 file changed: $($file.Path)"}}
$previous11=Get-Content -LiteralPath (Join-Path $root 'work\reconstruction-v11\delivered-hashes.json') -Raw | ConvertFrom-Json
foreach($file in $previous11){if((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Hash){throw "Preserved v0.11 delivery changed: $($file.Path)"}}
$integrity.Add("PASS $(@($v11files).Count) v0.11 manifest files and delivered exe/ZIP hashes are unchanged.")
$exe=[IO.File]::ReadAllBytes((Join-Path $package 'BattleshipLab.exe'))
$pe=[BitConverter]::ToInt32($exe,60)
if([BitConverter]::ToUInt16($exe,$pe+4) -ne 0x8664 -or [BitConverter]::ToUInt16($exe,$pe+24) -ne 0x20b){throw 'Game is not PE32+ x64'}
$integrity.Add('PASS delivered v0.12 game executable is PE32+ machine 0x8664 (Windows x64).')
$integrity.Add('PASS 489 packaged original assets match the user-supplied original files byte for byte by SHA-256.')
$integrity | Set-Content -LiteralPath (Join-Path $package 'Integrity-Check.txt') -Encoding UTF8
$manifest=foreach($file in Get-ChildItem -LiteralPath $package -Recurse -File | Sort-Object FullName) {
    $relative=$file.FullName.Substring($package.Length+1)
    if($relative -eq 'Build-Manifest.csv' -or $relative.StartsWith('UserData\')){continue}
    [pscustomobject]@{Path=$relative;Bytes=$file.Length;SHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash}
}
$manifest | Export-Csv -LiteralPath (Join-Path $package 'Build-Manifest.csv') -NoTypeInformation
$zip=Join-Path $root 'outputs\Battleship-Lab-v0.12.zip'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archiveStream=[IO.File]::Open($zip,[IO.FileMode]::Create,[IO.FileAccess]::Write)
$archive=New-Object IO.Compression.ZipArchive($archiveStream,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach($file in Get-ChildItem -LiteralPath $package -Recurse -File | Sort-Object FullName) {
        $relative=$file.FullName.Substring($package.Length+1)
        if($relative.StartsWith('UserData\')){continue}
        $entryName='Battleship-Lab-v0.12/'+$relative.Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$entryName,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally {$archive.Dispose();$archiveStream.Dispose()}
@(
    [pscustomobject]@{Path=$zip;Hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash},
    [pscustomobject]@{Path=(Join-Path $package 'BattleshipLab.exe');Hash=(Get-FileHash -LiteralPath (Join-Path $package 'BattleshipLab.exe') -Algorithm SHA256).Hash}
) | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'delivered-hashes.json') -Encoding UTF8
$integrity
Get-Item -LiteralPath $zip | Select-Object FullName,Length






