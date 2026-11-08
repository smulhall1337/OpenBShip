Battleship Fleet Lab 0.7 — original firing clip study

Run BattleshipLab.exe or the parent outputs\Launch-Reconstruction.cmd.
Open Firing clip -> Play to view the original BATFIRE1 animation. This is a
silent study: soundtrack and battle-turn integration are not implemented yet.
The existing Classic duel remains playable, and v0.6 is preserved separately.

Clip controls:
Play/Pause, Restart, Previous frame, Next frame and Skip to end. Playback is finite
(46 frames, nominal 25 fps). Play after completion starts again. Leaving the tab,
opening Display or losing focus pauses it. F2 resets the study and the game.
Inspecting a clip pauses a live duel; return to Classic duel and Resume/P to play.
The clip consumes no game turns, damage, shots or random values. Its palette is
composed independently; changing the duel's Display colors does not recolor it.

Recovered evidence:
All 46 decoded frames match isolated original-decoder hashes, with exact input
consumption. Three complete indexed frame fixtures also match. Original-code
fixtures test all 13 block operations and 256 quartet patterns, including retained
pixels, repeated masks, literal escapes and an operation absent from this movie.
PAT-EVIDENCE.txt documents framing, addresses, palette branches, confidence and
limits. The reader explicitly supports only header 20,15,4,4 (160x120 scale four).
Other variants, malformed lengths/opcodes/markers and excessive counts fail clearly.
Only this one firing movie is exposed. A complete PAT/movie system is not claimed.

Retained duel:
Auto-place -> Start battle. Select a live own ship, its original fleet icon or its
right-side list row, then fire on the upper-right board. Original weapon choices
and names appear in the log. Carrier/submarine player lists differ; the opponent
always uses its generic list. Damage still affects one cell/one hit point. A sunk
selected ship falls back to the first surviving roster slot. The recovered fleet
has 19 cells, fixed-axis centered placement, original art/coordinates and four
original targeting settings. The rotating v0.1 preset retains its prototype model.
Persistent SMOKE damage, default/recovered color choices and animated/still damage
settings remain. Settings preserve legacy mute and unknown key/value entries.

Mouse places/selects/fires. Grid arrows + Enter place/fire; click the board to
return focus from a list. The firing list's arrows/Home/End skip sunk ships and
its confirmation does not fire. P/Escape pauses/resumes the duel; R rotates only
in the v0.1 preset; F2 resets; Sound mutes/unmutes existing battle effects; Quit
exits. Recovered maps is a read-only inspector that pauses the duel. Clip buttons
are keyboard accessible through normal Tab navigation and native confirmation.

Build/checks:
Build.cmd uses the existing Windows .NET Framework compiler/runtime for an x64
Forms/GDI+ application. Source separates readers, model, targeting, firing choices,
geometry, graphics, input/UI, audio, preferences and the explicit clip-study clock.
No new runtime, engine or codec is installed. The app needs only bundled assets,
not the ISO, original registry/wrapper, Bs.exe or optional analysis programs.
Verify.cmd runs 49 focused checks and 15 real-form checks. Numerical sweeps and
fixture rows are grouped checks, not independent gameplay scenarios. Form input
is simulated offscreen. Audio format checks do not establish audible output.
A relocated ZIP payload passes these checks. No-argument startup/clean close was
also checked on the agent desktop. Physical playback/input remains a user check.

The package includes maintainable source/build commands, fifteen byte-identical
original assets with provenance, raw address/hash evidence, optional source-built
analysis tools, integrity/portable reports and eleven application previews.
--render-clip-preview Clip-Preview.png and --render-clip-small-preview
Clip-Small-Preview.png render UI frame 21 for inspection. The other existing
--render-* previews remain; damage/firing scenes use explicitly labeled diagnostic
injection or scripted model actions, not original-gameplay captures.

Limits and next work:
This remains a partial reconstruction. Normal battle firing movies/impact phases,
SEQ soundtrack scheduling, automatic fades, missions, real-time simulation, saves,
networking and controllers remain absent/provisional. Native shared RNG scheduling,
layout RNG, first-player choice, 600 ms response, immediate impact, pause/reset,
selection outlines, hit markers, persistent banners, deterministic palette endpoints
and terminal reveal retain documented modern boundaries. Historical timing/palette
and x87 picking-boundary limitations remain. Original full-gameplay and audible
comparison are pending. Visible v0.5 damage effects were user-confirmed; v0.6/0.7
physical input/playback acceptance has not been inferred from continue requests.

Next: inventory/decode the other reachable firing clips, then recover .SEQ audio
lifecycle and completion/skip phase semantics before attaching movies to turns.
PAT-EVIDENCE.txt contains exact next addresses. Earlier builds and the accepted
original menu/clean-Quit setup are unchanged.