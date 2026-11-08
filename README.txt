Battleship reconstruction and modernization workspace — checkpoint24 / v0.18

Run: outputs/Launch-Reconstruction.cmd
Current scope/checks: outputs/Current-Status.txt
Priorities/stretch goal SG-01: outputs/Project-Roadmap.txt
Evidence/controls: outputs/Battleship-Lab-v0.18/FLEET-EVIDENCE.txt and README.txt
Feedback: outputs/User-Feedback.txt
Checkpoint history: work/checkpoint-*.txt and outputs/Reference-Checkpoint.txt

Original menu → Start New Solo Game → Classic → Start Game opens Fleet
Configuration. Move/review a complete fleet; its Start Game begins battle.
Existing direct Classic duel/manual deployment remains available. This is a
partial rebuild: recovered code/data evidence and modern choices are explicit.
Earlier runnable versions and supplied originals remain preserved.

Build and check the existing workspace:
outputs/Battleship-Lab-v0.18/Build.cmd
outputs/Battleship-Lab-v0.18/Verify.cmd

Restore in a clean Windows source checkout with the existing .NET Framework:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Restore-Workspace.ps1 -OriginalRoot "C:\path\to\Battleship_The_Classic_Naval_Warfare_Game"
Add -WithChecks to regenerate native-derived fixtures and run validation.
Add -AudioDeviceCheck for separate SILENT actual-device checks. The exact supplied
RIP must contain Bs.exe, GRAPHICS, LWEBS, HWEBS and SFX. Every copied asset hash
is checked; existing differing files are retained and running builds are not
replaced. No downloads, registry edits, original installation or disc mount.
Optional x86 fixture generators execute bounded hash-guarded copied code with
explicit stubs; read the evidence reports. Production never executes Bs.exe.
Optional PeDisasm/Iced dependency is licensed separately and not required to
restore, generate fixtures, build or play the game.

Latest authored-source backup: outputs/Battleship-Source-Checkpoint-24.zip
Integrity record: outputs/Source-Checkpoint-24.txt. Older snapshots retained.
The local repository is initialized, with first commit still pending because
this session protects existing .git metadata. No commit or remote is claimed.
To finish the prepared checkpoint from your own terminal, run:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Complete-Git-Checkpoint.ps1
Or double-click outputs\Complete-Git-Checkpoint.cmd. It audits exclusions and
uses a per-command Codex identity, without global Git settings or publication.

Git/source snapshots record authored source/build/analysis tools, provenance,
reports and numerical fixtures. Supplied original media/executables, binaries,
ZIPs, previews, user settings, scratch and Git metadata stay excluded. The
recorded sixty-byte modern Windows palette input is intentionally retained.
No external upload/publication/message performed. AI movie enhancement remains
optional stretch goal SG-01; no model or enhancement has been downloaded/run.

Git ownership compatibility: Complete-Git-Checkpoint.ps1 passes safe.directory for this exact workspace on each Git command. This handles sandbox-owned .git when running as Sean without adding a global trust exception or changing ownership. Rerun outputs\Complete-Git-Checkpoint.cmd after an ownership-check failure; the script includes the fix in the checkpoint.
