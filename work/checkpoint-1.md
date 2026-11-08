# Battleship reconstruction checkpoint 1

## Source and provenance
- Original source: `C:\Users\Sean\Documents\Battleship-The-Classic-Naval-Warfare-Game_Win_EN_ISO-Version`
- Disc image: `Battleship.iso`, 654,481,408 bytes; SHA-256 `AAC95A184FE9CA7C922B15B1C3AA756EF3E5EA9F8C637A1250F2B7275E555F67`.
- The user extracted the image contents into the same folder. The folder contains the InstallShield 1997 installer, its `data1.cab` payload (55,284,377 bytes), DirectX 5 redistributable files, manuals, and the original game media. Original files have not been modified.
- `work/provenance-manifest.csv` records SHA-256, size, and source-relative paths for 1,942 files: the ISO, primary installer inputs, manuals/configuration, and extracted game artwork, palettes, sound, and control/media data. Hashing used SHA-256. Extracted contents are attributed to the user-supplied ISO; extraction tool/version has not been recorded.

## Confirmed observations
- `ReadMe.txt` is dated 1997-09-09 and describes **Classic Battleship** and **Ultimate Battleship**, says DirectX 5 is required, and documents hotseat, network/modem play, hotkeys, and movie playback differences.
- The disc root has the InstallShield launcher (`SETUP.EXE`, 59,392 bytes), not the installed game executable.
- Static ASCII strings recovered from `setup.ins` identify installed executable names `BShip.exe` and `Launcher\BShipL.exe`, a default target folder under `Hasbro Interactive\Battleship`, uninstall/DirectPlay registry entries, and an optional `DirectX\dxsetup.exe` step. This is metadata evidence; payload contents and actual behavior are not yet independently verified.
- Extracted content counts: GRAPHICS 381 files; HWEBS 290; lwebs 275; PINP 77; SFX 907. These include `.SPH`, `.PCX`, `.SCN`, `.PAL`, `.PAT`, `.WAV`, and `.SEQ` data. Their semantics remain unverified.
- `data1.cab` begins with `ISc(`, consistent with an InstallShield cabinet. Windows `expand.exe` did not list its contents, and no 7-Zip or Python CLI was available.

## Reference status
- No game executable has been extracted or launched. Windows refused to mount the ISO with `Access denied`; the extracted files now remove the need to mount it.
- `setup.ins` indicates installation can write registry entries and includes an optional DirectX 5 installer. A contained install under this workspace is the next step, with bundled DirectX installation declined. It is still necessary to inspect the installer UI/options and actual target behavior.
- Original Windows version and CPU architecture have not been confirmed. The README's DirectX 5 requirement and September 1997 date indicate a Windows 9x era title, but exact requirements remain to be validated.

## Next step
- Run the supplied `SETUP.EXE` only after confirmation because it is unrecognized software. Select a workspace-local destination if supported; avoid installing DirectX 5 or online components. Then hash and inspect installed binaries and attempt launch, recording startup, rendering, input, audio, and gameplay separately.
- Keep any reconstructed prototype separate from original files and clearly label provisional behavior. No engine or implementation language has been selected yet.

## Reference launch attempt (checkpoint 1 update)
- The user approved running the original installer into a workspace-contained location. The attempt used the supplied `SETUP.EXE` (SHA-256 `BF9C54E660C33313FE647B5C0647BA69F214A6DFA07DCDE97FBC88318C99F0A7`) with its source directory as working directory. Windows returned `0xc0000142`; no installer window appeared and no installation was performed.
- PE inspection found `SETUP.EXE` is an NE (New Executable) Windows binary, machine field `0x0149`, rather than PE. The environment reports `AMD64`. `AutoRun.exe` and `mplaynow.exe` are 32-bit PE i386 applications; they are the disc front-end and online helper, not the game identified by the setup script.
- The game executables named in installer metadata (`BShip.exe`, `Launcher\BShipL.exe`) are not loose files in the extracted tree; they are expected inside the InstallShield `data1.cab` (`ISc(` signature). Available command checks found no 7-Zip, `unshield`, `cabextract`, DOSBox, Wine, OTVDM, or .NET SDK. No new software was downloaded or installed.
- DirectX 5 setup, online helpers, and game binaries were not run. The source tree remains unchanged.

## Current dependency and handoff
- A runnable original reference now depends on either (a) an already-installed copy of Battleship that can be inspected/launched, or (b) a compatible way to extract this InstallShield cabinet and run its 16-bit installer/launcher in an isolated environment. Neither is available in the workspace at this checkpoint.
- Next useful work once that dependency is resolved: hash extracted game binaries; identify the 32-bit runtime/imports and asset lookup behavior; launch without installing DirectX 5 or connecting online; record an initial Classic Battleship scenario before choosing a substantial modernization implementation.

Superseded by work/checkpoint-2.txt: the user supplied an installed Full-RIP directory; a workspace reference copy and direct launcher now exist. Startup still exits immediately; registry configuration is absent.
