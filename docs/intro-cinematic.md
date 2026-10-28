# Narrated introduction

Choose **Begin descent** to play the42 approved oil paintings before a new journey. Gameplay stays paused until the ending or a deliberate skip. **Resume journey** bypasses the introduction. **Replay introduction** on the title returns to the title afterward.

Esc, Space or controller Start pauses/resumes the narration and paintings together. Hold Enter or controller B for0.8s to skip, or use the on-screen Skip intro button. Pause and Skip have explicit controller navigation. Skip fades to black before continuing. The viewed preference is written only for natural completion or deliberate skip. Missing resources and early audio failure return to the title without starting gameplay.

`Assets/OrpheusBelow/Resources/Cinematic/Intro/` contains42 unchanged native PNGs, the supplied MP3 and `Timeline.json`. These media files use the repository's existing Git LFS rules. A fresh clone requires `git lfs pull`. No preview MP4 or private photographic references are required by the game.

`IntroCinematicPlayer` derives every dissolve and camera pose from `AudioSource.timeSamples / clip.frequency`. At most the outgoing, current and next paintings are resident during sequential playback. Missing paintings pause the audio until loading completes. The aspect-preserving overlay letterboxes other screen sizes. Narration respects the existing master volume/mute; ambient music is silenced during the cinematic and startup dialogue waits behind it. Ending, skip, destruction and load failure release owned resources and restore the soundscape.

`IntroCinematicImporter` disables texture mipmaps/CPU readability, uses clamp/bilinear filtering and high-quality compression, and preserves44.1kHz stereo narration as preloaded decompressed audio. The source paintings are around1672px wide, below the original preferred1920px; camera motion does not add native detail. Optional character/environment overlays are not installed.

`docs/cinematic/MasterTimeline.csv` retains the approved source authoring cues and master filenames. Runtime resource keys use the corresponding two-digit scene ID. The narration lasts223.553s, ends at Orpheus's first descent, and contains no later bargain or forbidden glance. Source cue alignment is automatic; listening verification remains an editorial task before subtitle/timing lock.

Verification: `IntroCinematicTests` checks cue/resource coverage, fades, new/resume policy, invalid data and early audio termination. `OrpheusBelow.EditorTools.IntroCinematicVerification.RunBatch` exercises real scene playback, pause/input deduplication, controller navigation, seek/loading, skip/resume/replay, intentional audio failure and natural completion. Results and a runtime capture go to `Logs/IntroCinematic/`.
