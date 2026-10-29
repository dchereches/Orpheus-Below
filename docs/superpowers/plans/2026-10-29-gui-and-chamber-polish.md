# GUI, chamber boundaries and bilateral running

**Goal:** Apply the requested typography and overhaul HUD/subtitles/combat feedback, simplify the introduction to a Backspace caption, remove healing wells, enforce painted floor/gate collision, and replace the one-leg run with a true two-leg stride.

**Architecture:** Shared `GameTypography`, screen-space UGUI for HUD/combat readability, health-loss feedback at the actual damage sites, continuous painted-floor collision, and an authored painted leg rig baked into the existing sprite library. Preserve voice locks and cinematic fades.

**Typography decision:** User chose to keep Mythology local. An ignored Editor-only copy supplies local preview; published builds bundle Crimson Text under OFL. No private font bytes or imported references may be committed or embedded in builds.

- [x] Reproduce GUI, boundary and running failures before production fixes: GUI5/5 RED, boundary6/6 RED, gait7/7 RED.
- [x] Build shared typography, new HUD/subtitles, nearby applied-damage numbers and readable health displays.
- [x] Remove intro buttons and use small bottom-right `Backspace to skip` caption with matching input.
- [x] Remove wells/reserved well footprint, enforce continuous floor clearance, add locked thresholds and correct chamber8 gate occlusion/arrival.
- [x] Bake bilateral near/far leg motion with alternating support and no stance-foot drift; inspect rendered cycle.
- [x] Rebuild assets/scenes, run focused/full tests, and visually inspect intro, HUD, combat feedback and gait in Play Mode; verify the gate masks/clearance and locked thresholds across all eight scenes.
- [x] Review and fix findings before the authorized push/merge/pull workflow.

## Known baseline

`UnderworldOverhaulTests.OffBeatOrdinaryDamageCannotDefeatGuardian` is the pre-existing failure (expected792, actual0). The old healing-well expectation must change with this request. Old intro button-navigation expectations must become keyboard/caption checks.

## Verification and review

- Rebuilt all eight painted scenes/layouts and the animation library:48 motions,1328 installed body slots,776 distinct referenced sprites including effects.
- Final Edit Mode run (`Logs/gui-polish-final.xml`):372 tests,371 passed; only the unchanged guardian baseline above failed. All30 newly added cases passed, including bilateral support/clearance, mesh continuity/hip coverage, no wells, chamber8 gate masks, and applied/overkill damage.
- Independent review found a Pulse meter mislabeled Breath: corrected the label, with Breath shown separately as its real resource value. Unsupported musical Unicode was replaced with readable Soft/Boom words; PNG note icons remain.
- Independent collision review exposed composition across a painted edge and diagonal runtime wall:2/2 regression cases failed before the fix. Both contact sources now compete in the same earliest-contact loop.
- Follow-up review exposed blocked initial-overlap recovery:4/4 regression cases failed before the fix. Existing overlap is ignored only for movement along its escape normal that actually reduces penetration; new walls still block. All six follow-up collision cases passed in the final suite. Reviewer found no remaining blocker.
- Rendered intro/GUI verification (`Logs/IntroCinematic/result.json`):27 checks passed, including actual queued Backspace input, no buttons, bottom-right caption, fades, voice/interaction locks, synchronized subtitles, full voice gain and ambient ducking. Verified armored80 input shows14 actual loss and136/150 health beside the enemy, with the shared font. Captured local and public typography.
- The batch input test explicitly routes input to the game while the Editor is unfocused; normal project input settings are restored afterwards.
- Native animation capture exposed two defects the small contact sheet missed: nonlinear strip deformation opened knee cracks and removed original waist/cape paint. Both new regression tests failed before correction. Moving limbs now use a conforming 2px grid with shared GPU vertices; original pelvis/cape paint is retained and limb roots stay pinned through the hip seam. Repaired meshes contain4196–5209 vertices, below the65535 limit. All65 focused animation tests passed; fresh1920px run capture shows no cracks/cutout.
- Final live animation verification (`Logs/OrpheusRepair/Runtime/result.json`):33 segments,523 rendered frames passed, including planted stops, reversal, dashing without sprite clones, singing, cadence casts, flute, reactions and held ending poses.
- Local review previews are saved in the original checkout's `output/gui-polish-2026-10-29/` folder, including both font variants, intro caption and a movement video/GIF; these previews are outside this worktree and are not committed.
- Integration uses the existing authorization to push, merge into main, and pull the original checkout. The unchanged guardian baseline is explicitly reported; no combat-balance change is bundled into this GUI/movement work.
