# Narrated intro implementation plan

**Goal:** Play the approved42-scene intro before a new journey; commit, push, merge and pull the result as requested.
**Architecture:** Native paintings and original MP3 in Resources, JSON timeline driven by AudioSource.timeSamples, a letterboxed overlay with two painting layers and bounded asynchronous preloading. Existing menu retains the gameplay/audio pause until completion or skip. Resume bypasses playback; title offers replay.
**Spec:** Approved cinematic package and UnityImplementation.md in the original workspace's output/intro-cinematic-2026-10-28. User explicitly authorized implementation and Git integration.
**Tech stack:** Existing Unity6000.3.10f1, UGUI, Input System, NUnit EditMode tests. Existing LFS rules cover PNG/MP3. No generated preview movie or private reference photos enter Git.

- [x] Add failing tests for42 cue coverage, exact strike cue, fade behavior, pause clock semantics, resume/new/replay policy and missing resources.
- [x] Implement IntroCinematicTimeline.cs and IntroCinematicPlayer.cs, import42 unchanged PNGs/audio/manifest with suitable texture/audio settings.
- [x] Connect UnderworldFrontEnd BeginOrResume and replay; keep gameplay blocked and music silent until done, restore audio/UI on completion, skip or errors.
- [x] Run focused tests, full EditMode suite, and runtime verification for beginning/pause/seek/skip/replay/end/cleanup.
- [x] Review and prepare the verified feature branch for the requested push/merge/pull; Git history records the integration.

Review focus: duplicate menu activation; pausing while a texture loads; missing audio/image; destroyed owner during playback; late seek/end callback duplication. Never mark the viewed preference on failure or interruption.

Verification:9 intro tests pass; full EditMode suite312/316 passes. The4 failures reproduce with identical assertions on unmodified origin/main456d065: two LyreDraw animation expectations, removed oil rim expectation, and off-beat guardian damage. Real-scene verification passes all12 checks, including intentional early-audio failure and natural completion. Independent code review has no remaining important findings. Native-created worktree initially contained LFS placeholders; git lfs checkout hydrated existing media before final verification. No chamber/world production changes were needed.
