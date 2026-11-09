param([Parameter(Mandatory=$true)][string]$Game,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$low=@(Import-Csv -LiteralPath (Join-Path $Destination 'Impact-Clip-Inventory.csv'))
if($low.Count -ne 61){throw 'The explicit 61-clip low inventory is required first'}
$records=foreach($record in $low){
 $name=$record.Clip;if($name -notmatch '^[A-Z0-9_]{1,8}$'){throw 'Invalid clip name'}
 $file=Get-Item -LiteralPath (Join-Path $Game "HWEBS\$name.PAT");$bytes=[IO.File]::ReadAllBytes($file.FullName)
 if($bytes.Length -gt 16MB){throw 'High PAT exceeds reader bound'}
 $p=0;$frames=0;$max=0;$headers=[Collections.Generic.HashSet[string]]::new()
 while($p -le $bytes.Length-4){$n=[BitConverter]::ToUInt32($bytes,$p);if($n -eq [uint32]::MaxValue){break};if($n -lt 4 -or $n -gt 65532 -or $n -gt $bytes.Length-$p-4 -or $frames -ge 512){throw "Invalid high frame: $name"};$header=[BitConverter]::ToString($bytes,$p+4,4);if($header -ne '28-1E-02-02'){throw "Unsupported high variant: $name"};[void]$headers.Add($header);$max=[Math]::Max($max,$n);$p+=4+$n;$frames++}
 if($frames -eq 0 -or $p+4 -ne $bytes.Length -or $n -ne [uint32]::MaxValue){throw "Invalid high terminal: $name"}
 $highPalette=(Get-FileHash -LiteralPath (Join-Path $Game "HWEBS\$name.PAL")).Hash
 $lowPalette=(Get-FileHash -LiteralPath (Join-Path $Game "LWEBS\$name.PAL")).Hash
 if($highPalette -ne $lowPalette){throw "High palette differs; shared palette not safe: $name"}
 [pscustomobject]@{Clip=$name;Frames=$frames;Bytes=$file.Length;MaxPayload=$max;Headers=($headers -join ' ');Terminal=$true;PaletteMatches=$true;SHA256=(Get-FileHash -LiteralPath $file.FullName).Hash;PaletteSHA256=$highPalette;LowFrames=[int]$record.Frames;FrameDelta=($frames-[int]$record.Frames)}
}
$records | Export-Csv -LiteralPath (Join-Path $Destination 'HWEBS-Inventory.csv') -NoTypeInformation
"Inventoried $($records.Count) high-detail clips / $(($records | Measure-Object Frames -Sum).Sum) frames; identical palettes; per-clip durations retained."
