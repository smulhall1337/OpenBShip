v0.8 firing/audio evidence:
Build-Firing-Oracles.cmd builds two new optional x86 fixture generators.
ClassicFiringPatOracle.exe <matching Bs.exe> <LWEBS directory> <new output directory>
Guards twenty original PAT hashes and writes Classic-Firing-Frames-Oracle.csv.
ClassicSequenceOracle.exe <matching Bs.exe> <package root> <new output directory>
Executes only copied polling/cleanup code with successful dummy sound-handle stubs;
the package root supplies original SEQs and Evidence/Firing-Clip-Inventory.csv.
It writes Classic-Sequence-Oracle.csv. No native sound-device/WAV loader runs.
Extract-ClassicSound.ps1 -Executable <matching Bs.exe> -Destination <new directory>
Records nine raw code/table spans, their manifest and late movie-name CSV.
Export-FiringInventory.ps1 -Game <original RIP root> -Destination <new directory>
Reproduces twenty clip and seventy-one WAV inventory records without execution.
If script policy blocks a tool, invoke powershell.exe -NoProfile -ExecutionPolicy
Bypass -File <script path> <arguments>; do not change system execution policy.
AUDIO-EVIDENCE.txt explains source identity, controlled fixtures, timing and limits.
The Windows x64 game never loads or invokes the optional analysis programs.
v0.7 PAT evidence:
Extract-ClassicPat.ps1 -Executable <matching Bs.exe> -Destination <new directory>
Records twelve decoder/table/driver/palette/sequence code spans and their manifest.
ClassicPatOracle.exe <matching Bs.exe> <original LWEBS/BATFIRE1.PAT> <new directory>
Build-Pat-Oracle.cmd rebuilds this optional x86 decoder fixture generator. Both
original executable and PAT hashes are mandatory. Only copied decoder arithmetic
runs in its own allocation, with validated relocations, exact consumed lengths
and output canaries. It writes 46 frame hashes, three full indexed frames, and
constructed quartet/all-opcode inputs with original-code expected outputs.
These are grouped decoder comparisons, not original gameplay observations. No
original process, graphics/audio API, registry or supplied file is modified.
The Windows x64 app never invokes these tools. See PAT-EVIDENCE.txt for limits.
v0.6 firing evidence:
Extract-ClassicWeapons.ps1 -Executable <matching Bs.exe> -Destination <new directory>
Reads exact-hash-guarded source spans, weapon lists, names and movie dispatch data.
ClassicWeaponsOracle.exe <matching Bs.exe> <new directory>
Build-Weapons-Oracle.cmd rebuilds the optional x86 tool. It executes only copied
numerical list/dispatch fragments with controlled RNG and record-only movie stubs.
It writes Weapons-Oracle.csv (500 rows) and Firing-Movies-Oracle.csv (220 rows).
These are two grouped numerical fixtures, not full gameplay observations. The
source validates relocation counts and never starts or modifies an original game.
WEAPONS-EVIDENCE.txt details scope, random scheduling and unresolved PAT/SEQ playback.
The Windows x64 game never loads or invokes these optional analysis tools.

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
