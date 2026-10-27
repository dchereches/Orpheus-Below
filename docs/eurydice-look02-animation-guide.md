# Eurydice — Look 02 animation guide

The user selected **Look 02 / Broad Oil** on 27 October 2026. The implementation uses new transparent PNG atlases, generated with the built-in imagegen tool from that approved concept, the three red-haired facial references saved at 16:21, and the broad-brush oil reference saved at 16:20. The private source photographs are not copied into the repository. Exact production prompts are preserved in `eurydice-look02-image-prompts.json`.

## Runtime artwork

`Assets/OrpheusBelow/Resources/Characters/Eurydice/Look02/` contains five atlases and a saved sprite library:

| Atlas | Frames | Purpose |
| --- | ---: | --- |
| Idle | 32 | Four quiet breathing/blinking frames for each of eight facings |
| Walk | 32 | Four walking poses for each of eight facings, including actual rear views |
| Reactions | 36 | Joy, fear, listen/answer, teaching, sanctuary and shatter; reverse shatter supplies reform |
| Stairs | 24 | Six frames each for northeast/northwest ascent and southeast/southwest descent |
| Effects | 24 | Ambient oil strokes, joyful gold flecks, fear fragments and sanctuary ribbons |

The 148 frames are sliced into `Library.asset` without modifying the generated PNGs. Non-divisible image dimensions and uneven atlas spacing are supported: alpha gutters are detected independently in each column to avoid clipping a pose or importing a neighboring head/foot. Visible bounds and foot-support pivots align the character with the ground. Standing direction rows are normalized to approximately **2.5274 units**, versus the previous 1.944-unit silhouette: a 30% increase. The original four-state sprite sheet remains available as a fallback.

Rebuild saved sprites and stair metadata using **Orpheus Below → Bake Eurydice Look 02**.

## Selection and triggers

| Animation | Gameplay trigger |
| --- | --- |
| Idle | No meaningful XZ travel; last facing is retained |
| Walking | Ground movement, with eight facing sectors and sector/speed hysteresis |
| Turn/settle | A substantial heading change; intermediate directional poses blend into the new walk |
| Stair ascent/descent | Movement along an authored stair polygon's uphill/downhill axis |
| Teaching | The existing tutorial dialog callback |
| Listen/answer | A nearby acoustic ping; the existing hum timing is retained |
| Joy | Chamber clear, release from the last abductor, or final victory |
| Fear | Abduction, a nearby live shade, player damage, or entering a temptation zone |
| Sanctuary | Casting the protective cadence; its painted protective effect persists for the shield duration |
| Reform | Arrival/reset into a chamber |
| Shatter | Existing fatal look-back and abduction-loss paths |

Loss and danger take precedence over cosmetic gestures. Body gestures yield to locomotion when Eurydice must catch up, while contextual effects/audio can continue. Follow movement spends a distance budget along recorded path corners, sweeps against cover, and preserves the escort lag. Decorative vertical motion never affects the walking decision.

The pause menu freezes playback and effect frames. Tutorial dialogs deliberately keep their contextual teaching/reaction animation running on the UI clock while the world remains paused. Paused callbacks select the teaching pose immediately, independently of script update order.

Stair zones are authored in the existing painted-image coordinates and persist in the chamber layout assets. Amber scene gizmos indicate stair boundaries and uphill axes. The initial viaduct stays ordinary walking terrain; later rooms contain the marked steps.

## Verification

EditMode tests cover stationary vertical bobbing, all eight directions, facing retention, priority, scaled/paused playback, actual alpha, sprite size, terminal clips, stair axes, paused tutorial callbacks, ground anchors and sustained following. The render harness `EurydiceLook02Verification.RunBatch` exercises the real companion callbacks and captures deterministic animation samples under `Logs/EurydiceLook02/`.

The project has four known pre-existing test failures: three tutorial-sequencing assertions and `OffBeatOrdinaryDamageCannotDefeatGuardian`. Keep these distinct from the Look 02 regression results.

Final merged results: **54 passed / 4 pre-existing failures**. All Look 02 tests pass, including gutter isolation, follow speed/corners, paused teaching initialization and shatter cleanup. The in-engine animation verifier passes with zero runtime errors. The five-chamber render regression also passes: cover occlusion in both directions, transitions, carried health/cadences, final victory, restart and return to title. A timed sequence of in-engine animation samples is saved as `Logs/EurydiceLook02/eurydice-look02-preview.mp4` and `.gif`.

The concurrent menu/input polish is retained. Look 02 remains the primary library, with legacy Walk/Run state names and Greek-oil sheet slicing retained for compatibility.
