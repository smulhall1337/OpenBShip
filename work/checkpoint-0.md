# Battleship reconstruction checkpoint 0

## Inputs and integrity
- Source: `C:\Users\Sean\Documents\Battleship-The-Classic-Naval-Warfare-Game_Win_EN_ISO-Version\Battleship.iso`
- Size: 654,481,408 bytes.
- SHA-256: `AAC95A184FE9CA7C922B15B1C3AA756EF3E5EA9F8C637A1250F2B7275E555F67`
- Last modified: 2017-05-24 14:58:46 UTC.
- The image begins with a zeroed sector. At byte offset 32,768, the volume descriptor begins `01 CD001 01` and its volume label is `Battleship`. This confirms an ISO9660 primary volume descriptor.

## Workspace
- Workspace root: `C:\Users\Sean\Documents\Codex\2026-11-06\yes`
- Existing folders: `work/` and `outputs/`; no pre-existing game project files or `AGENTS.md` were found in the workspace.
- The supplied ISO remains untouched.

## Current state and uncertainty
- `Mount-DiskImage` returned `Access denied`, so the original cannot yet be run from a mounted volume. No executable, install data, or game behavior has been inspected. The OS/tool check found PowerShell's `Mount-DiskImage`, but no `python` command or `7z` executable.
- Game edition, release year, executable architecture, runtime dependencies, and actual disc filesystem beyond the ISO9660 PVD are unconfirmed.
- No runnable reference checkpoint exists yet. Do not infer that the game launches or that the ISO contains a playable build based on the volume label.

## Next experiment
- Continue with a bounded ISO9660 directory-record reader (read-only) to inventory the image and identify installer/game binaries. Hash extracted original files and preserve their provenance. Then evaluate whether the executable can launch from a local extracted copy or whether a reversible compatibility/reference setup is required.
