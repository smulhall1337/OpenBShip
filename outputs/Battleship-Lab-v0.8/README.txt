Battleship Fleet Lab 0.8 — original firing clips and sound

Run BattleshipLab.exe or the parent outputs\Launch-Reconstruction.cmd.
Open Firing clips, choose a movie, set Sound:on and press Play. Twenty original
Classic firing clips now have their supplied sound cues. v0.7 is preserved.
This remains an independent study library; normal battle movies are not integrated.

Controls and behavior:
Play/Pause, Restart, Previous/Next frame and Skip to end work with finite playback.
Play after completion starts again. Switching clips, leaving the tab, Display and
focus loss pause/stop their owned audio appropriately. Frame stepping is silent;
resuming/unmuting seeks to the current position. The global Sound button applies
to the library, retaining the existing persistent sound preference. F2 resets the
study and game. Inspecting pauses a live duel; return and Resume/P explicitly.
Clip actions do not change game turns, damage, shots or random values.

New evidence:
All 1460 frames across twenty movies match isolated original-decoder hashes.
Original-code sequence fixtures confirm one queued sound onset per movie frame,
including tied thresholds, for all 104 cues. Seventy-one supplied WAV files decode
with their original sample counts. Finite eight-voice mixing is based on recovered
pool/Play behavior, with documented gain/device/timing boundaries. Audio-device
samples drive video while sound is enabled. Mute uses the previous bounded visual
clock. Errors report clearly and fall back to silent visual progress.
AUDIO-EVIDENCE.txt documents exact semantics, original quirks, tests, limits and
next steps. PAT-EVIDENCE.txt retains the earlier codec baseline. The current reader
supports only the observed 20,15,4,4 header; other variants fail explicitly.

Hands-on check:
Try BATFIRE1, TWOFIRE2 and SUBMISS1 with Sound:on. Confirm sound starts with the
animation, pauses/resumes, stops on skip/switch, and mutes without restarting video.
Actual audible output and perceived synchronization remain user checks. Automated
Windows device tests submit silent PCM; they do not prove that you hear anything.
The earlier user report "it plays" confirms v0.7 visible playback only.

Retained duel:
Auto-place -> Start battle. Select a live own ship, original fleet icon or list row,
then fire on the upper-right board. Recovered weapon names appear in the log.
Damage still affects one cell/one HP. A sunk selected ship switches to the first
survivor. Recovered Classic uses nineteen cells, centered fixed-axis placement,
original art/coordinates, damage effects and four targeting settings. The v0.1
preset retains its rotating prototype. Display offers original/default colors
and animated/still damage. Existing preferences/unknown keys remain compatible.
Mouse places/selects/fires; grid arrows + Enter place/fire. Click the board to
return keyboard focus from lists. The fleet list skips sunk ships; its confirmation
does not fire. P/Escape pauses/resumes the duel, R rotates in v0.1 only, F2 resets,
Sound toggles effects, Quit exits. Recovered maps remains a read-only inspector.

Build/validation:
Build.cmd uses the existing .NET Framework compiler/runtime for Windows x64
Forms/GDI+. New ClipSound.cs separates SEQ/PCM/mixing; WaveOutput.cs implements an
owned finite x64 audio buffer behind IClipOutput. No new engine, codec or SDK is
installed. The app needs bundled assets, not the ISO, original registry/wrapper,
Bs.exe or optional x86 analysis tools. The package includes source, build commands,
144 byte-identical original assets, raw evidence, provenance, optional tools,
reports and twelve previews. The previous v0.1..v0.7 builds remain unchanged.
Verify.cmd runs 55 focused checks and 19 real-form checks. Large fixture grids are
grouped comparisons, not thousands of independent gameplay scenarios. UI events
and failure injection are simulated offscreen. A relocated ZIP payload passes.
--audio-check Audio-Device-Check.txt separately exercises the actual x64 waveOut
API using SILENT PCM: position, pause/restart, completion, seek/reopen and cleanup.
--render-library-preview Library-Preview.png adds a SUBMISS1 diagnostic scene;
existing --render-* flags remain. Automated previews are muted and are not original
application captures. No-argument agent-desktop startup/clean close also passed.

Limits/next milestone:
This is a partial rebuild. The native Classic shot sequence also has travel and
impact stages; inserting only a firing clip would change its phase semantics.
Those stages, skip behavior and unsupported dependencies must be recovered before
coupling movies to battle turns. The accepted immediate-impact duel remains.
Audio gain/mixing, historical hardware latency/volume and shared voice occupancy
are not claimed identical to the original. Source end fields are retained with
the traced effective valid-handle behavior; finite cleanup is explicit and safe.
Native shared RNG scheduling, layout generation, first player, 600 ms response,
pause/reset, hit markers, banners, deterministic palette endpoints and terminal
reveal retain documented modern boundaries. Full original gameplay, fades,
missions, real-time simulation, saves, networking and controllers remain pending.
See AUDIO-EVIDENCE.txt and Current-Status.txt for exact next addresses/resources.