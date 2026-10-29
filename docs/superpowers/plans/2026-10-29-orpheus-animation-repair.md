# Orpheus animation repair implementation plan

> **For agentic workers:** Use superpowers:executing-plans to implement these tasks in this session.

**Goal:** Repair Orpheus's entire animation presentation and make movement stop and turn decisively.

**Architecture:** Keep the oil-painted character and the existing gameplay events. Isolate each body from neighboring atlas artwork with connected alpha regions and sprite geometry. Drive locomotion by actual distance, interrupt passive poses on movement, and replace full-body dash ghosts with a short ground streak.

**Tech Stack:** Unity 6000.3.10f1, C#, built-in image generation for replacement locomotion art.

**Spec:** User request, 2026-10-29: review all animations, remove dash clones and side objects, reduce sliding.

## Constraints and review focus

- Preserve combat timing, dash charges, invulnerability, pause/voice locks and chamber collision.
- Keep the current costume, broad oil finish, enemy-scale silhouette, and disabled colored rim outlines.
- Paused playback, blocked movement, rapid reversal, chained dashes and interrupted musical loops must remain coherent.
- Review every installed state/direction in contact sheets and exercise locomotion and combat in Play Mode.
- Original PNGs remain intact; new accepted locomotion sources are separate files.

## Tasks

- [x] Add regression tests for dash clones, release/reversal, movement overriding passive recovery, distance-based gait, repeated actions, loop cancellation and isolated atlas geometry. Run them red.
- [x] Repair atlas isolation and anchors; select distinct body poses for each action, correct the seven-row singing source, remove redundant effects and repeated still sequences. Bake and inspect all families.
- [x] Remove full-sprite dash afterimages; use a compact ground streak. Stop on released input, clear pre-dash momentum, remove boss-stage positional lag and shorten dash recovery.
- [x] Synchronize locomotion phase to ground distance, prioritize movement and dash direction correctly, restart repeated hits, cancel released musical loops even when queued.
- [x] Run focused and full EditMode suites, then a real rendered Play Mode capture of movement, dash, attacks, singing, flute and recovery. Review the final changes and fix findings. Integrate with the previously authorized commit/push/merge/pull workflow after verification.

## Validation

Prior full suite: 318/322. The repaired first-note draw/pluck flow resolves both musical reachability failures. The disabled-rim test now checks the existing art direction. The unrelated guardian damage failure is outside this request.

- Initial regression run: 9/10 failed, confirming dash clones, drift, stale momentum and pose problems. Expanded RED runs reproduced distance phase, detached slash, queued sustain, stationary landing, dash victory, moving combo bolt and moving song failures.
- First final full run: 337/339; all 17 new regression tests passed. One existing test still treated terminal Victory as cosmetic; its cosmetic-priority check now uses ChamberClear. The other failure is the known guardian damage case.
- Baked/inspected 48 state families with their directions; 1,296 installed body slots, 736 unique source/effect sprites. All installed body meshes pass connected-silhouette checks. Original sources are unchanged.
- Final review found three important issues. Fixed stationary dash landing using explicit movement intent; fixed terminal dash victory by stopping movement and owning the terminal pose; fixed moving combo-three recovery by preserving queued committed actions. All three were reproduced RED before the fix and pass in the full run. No deferred findings.
- Live capture exposed the flute guide across the larger sprite's waist; the guide test reproduced a -2.13-unit overlap RED, then passed after using the rendered silhouette height. Fixture cleanup explicitly pairs its EditMode singleton lifecycle. Full run after this fix: 339/340, with only the existing guardian failure.
- Final temporal review reproduced grief and victory snaps at loop wrap (2/2 RED). These terminal clips now play into held final poses. The rendered reel also exercises fatal look-back, grief and defeat holds.
- Ruling: five consistent directional source rows plus western mirroring replace the unreliable eight-row locomotion/singing arrangement. This keeps clear directional movement and avoids inconsistent generated western poses; the lyre's side is mirrored during these clips.
- Final full suite: **341/342 pass** (`Logs/orpheus-polish-verified.xml`). All **56 Orpheus animation tests pass**, including 20 new regression cases. The only failure is the unchanged `UnderworldOverhaulTests.OffBeatOrdinaryDamageCannotDefeatGuardian` (expected 792, actual 0).
- Final rendered verification: **33 segments, 523 frames, 38 visited animation states, zero runtime errors** (`Logs/OrpheusRepair/Runtime/result.json`). Live sensor input is disabled for deterministic capture. The remaining states/directions are covered by the complete contact sheets and frame checks. Final grief/victory images show settled held poses; the flute guide clears the head.
- Native art sources and exact built-in generation prompts are recorded beside the atlases. Four direction-family overview sheets and all 48 frame strips were inspected/generated from the actual baked sprite geometry.
