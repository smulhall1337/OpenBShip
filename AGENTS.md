# Project continuity

Read `README.md` before resuming. `src/` is the canonical current game source;
`scripts/` and the root setup/build/launch scripts reproduce its runnable build.
Keep generated files under ignored `build/`. Do not add original game assets,
executables, user settings, historical builds or research artifacts to Git.

If present locally, read `outputs/Current-Status.txt` and the latest
`work/checkpoint-*-handoff.txt` for reconstruction evidence and pending work.
These private comparison records are optional for building a fresh checkout.

User preference, accepted November 8, 2026: automatically make a local Git commit after each completed, tested checkpoint. Use a message describing the completed work. Include authorized pending project work; keep generated media, binaries, settings and scratch files excluded through the repository rules. Record the resulting commit in the ignored local `outputs/Git-Checkpoint.txt`. This preference does not authorize a push or publication.
