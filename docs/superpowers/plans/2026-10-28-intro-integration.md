# Narrated intro implementation plan

**Goal:** Play the approved42-scene intro before a new journey; commit, push, merge and pull the result as requested.
**Architecture:** Native paintings and original MP3 in Resources, JSON timeline driven by AudioSource.timeSamples, a letterboxed overlay with two painting layers and bounded asynchronous preloading. Existing menu retains the gameplay/audio pause until completion or skip. Resume bypasses playback; title offers replay.
**Spec:** Approved cinematic package and UnityImplementation.md in the original workspace's output/intro-cinematic-2026-10-28. User explicitly authorized implementation and Git integration.
**Tech stack:** Existing Unity6000.3.10f1, UGUI, Input System, NUnit EditMode tests. Existing LFS rules cover PNG/MP3. No generated preview movie or private reference photos enter Git.

- [ ] Add failing tests for42 cue coverage, exact strike cue, fade behavior, pause clock semantics, resume/new/replay policy and missing resources.
- [ ] Implement IntroCinematicTimeline.cs and IntroCinematicPlayer.cs, import42 unchanged PNGs/audio/manifest with suitable texture/audio settings.
- [ ] Connect UnderworldFrontEnd BeginOrResume and replay; keep gameplay blocked and music silent until done, restore audio/UI on completion, skip or errors.
- [ ] Run focused tests, full EditMode suite, and runtime verification for beginning/pause/seek/skip/replay/end/cleanup.
- [ ] Review diff, commit only task files, push feature branch, merge into main, push main, pull into original checkout while preserving unrelated local metadata.

Review focus: duplicate menu activation; pausing while a texture loads; missing audio/image; destroyed owner during playback; late seek/end callback duplication. Never mark the viewed preference on failure or interruption.
