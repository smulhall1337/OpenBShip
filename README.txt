Battleship reconstruction - playable v0.30 / evidence checkpoint 39

Run outputs/Launch-Reconstruction.cmd. Detailed scope, evidence and pending
hands-on checks: outputs/Current-Status.txt. Source and controls are packaged
under outputs/Battleship-Lab-v0.30. This is a runnable partial reconstruction.

Playable Classic route: Original menu > Solo > Classic > Start Game > Fleet
Configuration > Start Game > loading > battle. Direct Auto-place/Start works.
P/Escape pauses battle; F2 resets. Cinematics remain behind the board, with the
approved30%surface transparency and opaque ships/grid/markers/HUD.

Original menu > Replay intro plays the Hasbro/NMS/English startup sequence.
Preferences > Credits and the137movie archive expose the original media.
Original Quit shows its native artwork/default Back and closes after its fade.
Watch result after victory/defeat uses confirmed frame0/EOF/skip/fade behavior.
Source identities, formats, confidence and intentional modern differences are
documented in STARTUP-EVIDENCE.txt and ENDING-SEAMS-EVIDENCE.txt in the package.

Validation:136model/format +88real-form=224focused groups;48shutdown cases.
Master/high-detail full Classic -> victory -> Watch result -> title -> Quit
passes through production timers without movie skips:19shots/111battle stages,
112SILENT audio buffers,485seconds and maximum51GDI handles. Four additional
natural result runs cover both outcomes muted and with SILENT waveOut. These
checks establish API/behavior, not audible or physical-input acceptance.
Fresh source restoration matches1485Assets/27Programs/444Evidence files.

Build.cmd/Verify.cmd use the existing Windows .NET Framework compiler and x64
WinForms/GDI+/WinMM. The modern runtime needs no mount/registry/original EXE.
Source-only restore, with the exact supplied RIP:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Restore-Workspace.ps1 -OriginalRoot "<supplied RIP root>" -WithChecks -AudioDeviceCheck

Evidence checkpoint 39 adds a runnable bounded opponent firing/target probe:
720 numerical cases match the original chained selectors and all native draws.
Review outputs/Inspect-Native-Target-Order.cmd; reproduction and limits are in
outputs/Native-Target-Order-Findings.txt. The playable v0.30 streams are unchanged.

Source checkpoint 39 ZIP/hash record: outputs/Source-Checkpoint-39.txt.
Per user instruction, automatically make a local Git commit after each completed,
tested checkpoint. AGENTS.md records this preference; Complete-Git-Checkpoint.ps1
accepts -Message and writes commit identity to outputs/Git-Checkpoint.txt.
No remote, publication or upload is performed by this project workflow.

Ten recent tested builds are retained. Older generated packages and validation
copies retire after verified source/report/settings archival. Supplied material,
original reference, authoring/Git history and source checkpoints remain intact.
The ledger is outputs/Retired-Builds.csv; Build-Cleanup-Summary.txt is current.

Remaining: native live-gameplay reference, campaign/network/tutorials/radar,
controllers and full native lifecycle/RNG schedule. Source reference acceptance
is menu plus clean Quit only. Optional AI movie upscale remains SG-01backlog;
no models or processing run. outputs/Project-Roadmap.txt records next steps.

