Read-only reconstruction tools

v0.3 presentation evidence:
Extract-ClassicPresentation.ps1 -Executable <Bs.exe> -Graphics <GRAPHICS dir> -Destination <new directory>
Verifies the original executable identity, extracts camera/board/draw/scaling
context bytes, and derives the five native bank/frame bindings from original
dispatch/loader bytes. It records the supplied sprite hashes and dimensions.
It never executes original code or modifies input files. PRESENTATION-EVIDENCE.txt
describes the reconstructed arithmetic and its limits. Use the same one-process
PowerShell invocation below when local script policy requires it.

InspectAssets.exe <original GRAPHICS directory> <export directory>
Build-Tools.cmd rebuilds it using the existing Framework compiler.
Original-Format-Inventory.txt records the earlier full supplied-dataset scan.

Extract-ClassicEvidence.ps1 -Executable <Bs.exe> -Destination <new directory>
Run through powershell -NoProfile -ExecutionPolicy Bypass -File when necessary;
this affects that invocation only. It requires the exact SHA-256 documented in
CLASSIC-EVIDENCE.txt, reads the original without executing it, validates ship
push/call instructions and writes raw fixtures and CSV addresses. Use a new
destination for comparisons; the shipped Evidence folder is the build baseline.

PeDisasm.exe <Bs.exe> range <hex-start-VA> <hex-end-exclusive-VA>
PeDisasm.exe <Bs.exe> xref <hex-VA>
PeDisasm.exe <Bs.exe> strings
PeDisasm.exe <Bs.exe> dump <output-text-file>
Example: PeDisasm.exe <Bs.exe> range 4238D0 423AD0
Build-Analysis.cmd rebuilds the analysis exe against the bundled Iced.dll.
Range decodes from the specified start address. Supply a known instruction
boundary. Linear dump/xref decoding can include embedded data or miss code that
does not align with a linear pass; cross-check callers and raw bytes.

Analysis dependency: Iced 1.21.0, net45 assembly, MIT license included separately.
Official project: https://github.com/icedland/iced
Official package: https://www.nuget.org/packages/iced/1.21.0
Downloaded archive: https://api.nuget.org/v3-flatcontainer/iced/1.21.0/iced.1.21.0.nupkg
Archive SHA-256 D31613617E3DDF91239BBC94A8C587849B4236E8F8A2A2BA59EA2397A1487C7A
This optional tool is separate from the game; BattleshipLab.exe does not load it.
Dependency-Provenance.csv records local files and source URL. The package does
not install this library or register components on Windows.
