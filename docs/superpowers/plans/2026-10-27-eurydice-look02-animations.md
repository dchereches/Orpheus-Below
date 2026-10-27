# Eurydice Look 02 animation implementation plan

> **For agentic workers:** Use superpowers:executing-plans in the current checkout. The user selected Look 02 and explicitly requested implementation of all animations.

**Goal:** Ship the approved broad-oil Eurydice with directional locomotion, quiet idle, stair motion, contextual emotions and painted effects.

**Architecture:** A deterministic animation brain measures ground-plane motion and selects priority states. The sprite player uses new transparent Look 02 atlases; the existing companion and game events supply its inputs. Stair polygons live alongside the painted chamber definitions and use the same image-to-ground transform.

**Tech stack:** Unity 6000.3.10f1, C#, HDRP, generated transparent PNG atlases.

**Spec:** User-approved Look 02 concept and animation studies in this conversation, 27 October 2026.

## Constraints and decisions

- Preserve the referenced red-haired woman's likeness and the approved ivory/gold/oxblood costume with broad oil strokes.
- Prepare actual transparent assets; the labeled concept boards are not runtime atlases.
- Eight actual facing directions, including back views. Idle holds the last facing and ignores vertical bobbing.
- The pause menu freezes animation. Tutorial gestures use the UI clock while the world is paused. Teleports reset motion history. Movement uses enter/exit hysteresis.
- Priority: loss/shatter, entrance/reform, danger/fear, sanctuary, joy, teaching, acoustic reply, stair movement, turning, walking, idle.
- Joy: chamber clear, rescued from an abductor, victory. Fear: abduction, nearby threat/player hurt, temptation. Listen/answer: acoustic reply. Teaching: existing tutorial callback. Sanctuary: existing cadence shield. Shatter: existing look-back/abduction loss. Reform: chamber arrival.
- Author ascending axes and polygons for visible stairways in all applicable scenes. Bridge floor has no stair trigger.
- Size target is approximately 30% larger than the previous character's visible silhouette, preserving ground anchor and cover depth.
- Keep the current checkout and existing metadata changes. Do not push without a new request.

## Tasks

- [x] Reproduce the stationary-bob regression with an EditMode test and verify it fails.
- [x] Generate and inspect Look 02 transparent idle, walk, reaction, stairs and effects atlases; save assets and exact prompts in the project.
- [x] Implement state priorities, eight-direction selection, stable ground-speed detection, pause behavior and timed sprite playback; test priorities, direction retention, terminal clips and asset alpha.
- [x] Integrate companion movement/gesture events, larger visuals, restrained effects and stair-zone authoring; test stair ascent/descent and gameplay triggers.
- [x] Render real gameplay and deterministic animation samples; verify idle, front/back locomotion, stair directions, reactions, pause, shatter/reform and cover compatibility.
- [x] Review the change, update HANDOFF, and report actual verification results, including the known four pre-existing tutorial/combat failures.

## Review focus

Stationary vertical movement must never become walking. Identical facing input must not restart clips. Fear and loss must interrupt cosmetic reactions. Shield/joy/reply must not trigger continuously from a persistent unrelated condition. New sprites must retain transparent margins, stable feet, correct back directions and cover occlusion.

## Execution notes

- Stationary-bob regression failed with Glide instead of Idle before the implementation.
- Initial Look 02 suite: 47 passed, four known baseline failures. A fresh read-only review identified follow-speed and paused-teaching integration issues.
- Review regressions reproduced an incorrect -0.1623-unit screen-space ground offset and a paused callback remaining Idle. Following now uses a distance budget across corners; modal teaching selects synchronously and uses a UI-clock exception.
- Ruling: execute in the current checkout and preserve existing importer metadata changes; the user requested implementation here. Review is read-only and no commit/push is part of this request.
- Final results: 53 passed / four baseline failures. All Look 02 checks pass. In-engine animation and five-chamber regression harnesses pass with zero runtime errors. Review findings were fixed and the reviewer confirmed per-column gutter contamination is resolved. Captured frames were visually checked and encoded as timed MP4/GIF previews.
- At the user's push request, merged the concurrent menu/input polish from main, retained legacy Walk/Run compatibility, and reran verification. Merged suite: 54 passed / the same four baseline failures.
