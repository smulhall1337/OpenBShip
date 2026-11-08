Read-only reconstruction tools

v0.5 display evidence:
Extract-ClassicDisplay.ps1 -Executable <matching Bs.exe> -Destination <new directory>
Records loader patches, palette API composition, fade arithmetic, Classic damage
resource/drawing and 40 ms frame-limiter context with source addresses/hashes.

ClassicPaletteOracle.exe <matching Bs.exe> <ISLAND.COL> <new output directory>
Build-Palette-Oracle.cmd rebuilds this optional x86 numerical tool. It executes
only copied hash-guarded patch/fade arithmetic, with validated relocations and a
local stub replacing the graphics API call. It never starts the game or changes
the desktop palette. It reproduces Patched-Island.rgb and 65 Fade-XX.rgb outputs
plus their manifest. DISPLAY-EVIDENCE.txt explains scope and reserved endpoints.

SystemPaletteProbe.cs records local GDI endpoint buffer observations. It does not
change the desktop palette. The captured run returned zero while populating the
20 endpoint RGB values; its count is not treated as API success. The shipped game
uses documented fixed endpoint values, not this optional probe at runtime.

v0.4 opponent evidence:
Extract-ClassicOpponent.ps1 -Executable <matching Bs.exe> -Destination <new directory>
Extracts target selector/caller, RNG, search budgets, default level and repeat
impact context, guarded by the exact original executable hash.

ClassicTargetOracle.exe <matching Bs.exe> <new output CSV>
Build-Opponent-Oracle.cmd rebuilds this optional x86 tool. It executes ONLY copied
original numerical target-selector/RNG spans in its own process, using controlled
observation grids and validated relocations. It never starts/attaches to a game,
modifies the source or uses its media/registry setup. Cases exclude the native
infinite-loop input described in OPPONENT-EVIDENCE.txt. It creates 113 numerical
rows with returned coordinates and final RNG states, which reproduce the shipped
Original-Target-Cases.csv. This is an independent subsystem oracle, not an original
gameplay/reference launch. The Windows x64 game does not load or invoke this tool.

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
