# Narrated introduction

Application boot goes directly into the42 approved oil paintings, without first showing the title menu. The intro fades in, closes to black, then fades into the first chamber. Gameplay stays paused until the ending or a deliberate skip. Returning to the title in the same application keeps the title available; **Begin descent** starts another journey, **Resume journey** bypasses the intro, and **Replay introduction** returns to the title afterward.

Esc, Space or controller Start pauses/resumes the narration and paintings together. Hold Enter or controller B for0.8s to skip, or use the on-screen Skip intro button. Pause and Skip have explicit controller navigation. Skip fades to black before continuing. The viewed preference is written only for natural completion or deliberate skip. Missing resources and early audio failure return to the title without starting gameplay.

`Assets/OrpheusBelow/Resources/Cinematic/Intro/` contains42 unchanged native PNGs, the supplied MP3 and `Timeline.json`. These media files use the repository's existing Git LFS rules. A fresh clone requires `git lfs pull`. No preview MP4 or private photographic references are required by the game.

`IntroCinematicPlayer` derives every dissolve and camera pose from `AudioSource.timeSamples / clip.frequency`. At most the outgoing, current and next paintings are resident during sequential playback. Missing paintings pause the audio until loading completes. The aspect-preserving overlay letterboxes other screen sizes. Narration respects the existing master volume/mute; ambient music is silenced during the cinematic and startup dialogue waits behind it. Ending, skip, destruction and load failure release owned resources and restore the soundscape.

Voice requests now queue rather than interrupt each other. A line or multi-speaker sequence holds player interaction, combat input, physics simulation and chamber travel until the complete queued speech finishes, including gaps between speakers. Pause menus suspend both voice audio and subtitle timing. Voices use full source gain with master gain applied once; ambient wind and drone duck to12% during speech and recover smoothly afterward. Repeat companion hints wait until gameplay is free.

Navigation debug colors and gizmos reset to hidden on chamber enable, including authored scenes saved with debug overlays active. F3 remains an explicit editor/development-build diagnostic. Portal particles stop before initialization changes duration, avoiding the first-entry particle warning.

`IntroCinematicImporter` disables texture mipmaps/CPU readability, uses clamp/bilinear filtering and high-quality compression, and preserves44.1kHz stereo narration as preloaded decompressed audio. The source paintings are around1672px wide, below the original preferred1920px; camera motion does not add native detail. Optional character/environment overlays are not installed.

`docs/cinematic/MasterTimeline.csv` retains the approved source authoring cues and master filenames. Runtime resource keys use the corresponding two-digit scene ID. The narration lasts223.553s, ends at Orpheus's first descent, and contains no later bargain or forbidden glance. Source cue alignment is automatic; listening verification remains an editorial task before subtitle/timing lock.

Verification: `IntroCinematicTests` checks cue/resource coverage, fades, new/resume policy, invalid data and early audio termination. `OrpheusBelow.EditorTools.IntroCinematicVerification.RunBatch` exercises real scene playback, pause/input deduplication, controller navigation, seek/loading, skip/resume/replay, intentional audio failure and natural completion. Results and a runtime capture go to `Logs/IntroCinematic/`.

Validated on2026-10-28: all9 intro tests and all12 real-scene checks passed. The full EditMode suite passed312/316; all4 failures reproduced unchanged on origin/main456d065 (LyreDraw animation expectations, removed rim lighting, and guardian damage). These existing combat/animation failures are outside this integration.

Updated2026-10-29: automatic boot, fade handoff, clean chamber entry, full voice locks and dialogue ducking verified. All15 focused tests and all26 real-scene checks pass. Full EditMode suite318/322, retaining only those same4 baseline failures.
