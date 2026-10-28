# Voice and boot fixes

User request: remove first-chamber debug colors, prevent interaction/travel until all speech finishes, make voice much louder relative to ambience, fade the intro and start directly in it at boot; push the result.

- [x] Reproduce with queued-voice, overlay, mix and fade tests; keep baseline4 combat/animation failures separate.
- [x] Implement queued atomic voice playback, a gameplay/physics lock that excludes its own menu wait condition, full voice gain and ambient ducking. Guard travel and clean up locks on destruction.
- [x] Reset authored navigation overlays on enable; stop particle systems before changing duration.
- [x] Auto-start the intro once per application boot, preserve travel/resume/title replay, and fade the black end frame into gameplay.
- [x] Verify focused tests and actual scene behavior, review and prepare for requested push/merge/pull; Git history records integration.

Verification:15/15 focused tests and26/26 live checks pass. Full EditMode suite318/322, with the same4 independently confirmed baseline animation/rim/guardian failures. Live captures confirm clean first-chamber art and visible subtitles. Review findings for subtitle updates behind the input gate and Escape during the ending handoff were fixed and verified. Voice test fixtures use short in-memory clips, without modifying the shipped audio.
