# Orpheus Below — implementation handoff

## Project
Unity 6000.3.10f1, HDRP 17.3, Input System 1.18, .NET Standard 2.1. Paths below are relative to this repository. Runtime code lives in `Assets/OrpheusBelow/Scripts`. The current bootstrap version is **7**; entering Play rebuilds older prototype scenes automatically. Orpheus intentionally retains his original crimson diamond silhouette.

## Run and controls
Open the project in Unity and press Play. The title screen offers Begin descent, How to play, volume, mute, and Quit. Escape / controller Start opens pause. R restarts a run; Return to title is available from pause. Unsaved editor scenes rebuild in place.

| Input | Action |
|---|---|
| WASD / arrows / left stick | Move; A/D on the guardian stage |
| Space / Shift / gamepad south | Two-charge dash |
| Tap J / LMB / gamepad west | Soft note |
| K / RMB / gamepad north; hold and release J / LMB | Boom |
| F / gamepad left trigger | Toggle flute; slower movement, no dash |
| Tab | Keyboard, microphone, ESP32 input mode |
| H | Controls card |
| Hold Q / gamepad left shoulder | Forbidden look-back; loses Eurydice |

S and B below mean Soft and Boom, not movement keys.

## Combat pressure and cadences
`OrphicBreath.cs` is the shared resource model for all exploration lyre inputs. Soft costs 8 breath and has 0.22 seconds recovery; Boom costs 28 and has 0.48 seconds recovery. Third repeated notes deal 45% damage, fourth and later 20%, and cost 50% extra breath. Resting starts recovery after 0.65 seconds; two seconds resets repetition. A completed cadence returns 30 breath. Room entry refills it.

Shades resist ordinary notes (55% damage). Cadences expose them for 3.2 seconds (135% damage), break bronze wards, and open the flute opportunity. Ordinary hits cannot repeatedly cancel a lunge: stagger protection lasts 1.25 seconds. Chamber pressure scales health by 1.15 + 0.10 × room index and damage by 1 + 0.055 × index. The original 0.34 second base melee tell remains.

| Phrase | Unlock | Effect |
|---|---|---|
| S–S–B | Start | Hymn of Dawn: 20m piercing corridor, 1.7m radius, 105 base damage, exposes wards |
| B–S | After chamber I | Sanctuary: protects Eurydice and repels abductors |
| S–B–S | After chamber II | Convergence: gathers enemies, breaks wards, exposes and damages |

The flute pacifies unarmored shades below 60% health or exposed by a cadence. Wrong flute notes cause backlash and nearby enemy frenzy. Four guardians use timed stave notes: ordinary attacks cannot chip their health. Off-beat input breaks harmony and can trigger a sweep; the hit window is 0.18 seconds.

## Twelve chambers
Every room is at least 34 × 38 world units, varying up to 44 × 46. Each has two escalating waves. Asphodel starts with five shades followed by eight. Only the current room remains active, keeping water/atmosphere work bounded.

1. Asphodel Viaduct — Cantor of Fury
2. Gardens of the Unremembered
3. Charon’s Tidal Court
4. Vault of the Ferryman
5. Acheron Spillway — Oarsman
6. Hall of Broken Oars
7. Phlegethon Foundry
8. Cinder Basilica
9. Cocytus Mirror Tarn — Siren
10. The Weeping Colonnade
11. Lethe’s Drowned Library
12. Gates of Styx — Cerberus

`ProceduralChamberGenerator` owns dimensions, waves, floor, collision, pillars, exits and guardian placement. `HadesChamberDirector` owns tutorial, waves, room activation and transitions. Camera follow has a larger leash while keeping a close combat view.

## Art, depth and water
- `UnderworldEnvironmentArt`: painted far vista, two distant aqueducts, Doric promenades, eroded foundations, near-bank cypresses, mist and embers. Layers move at different rates. Opaque geometry provides real front/back occlusion; near foliage fades around the player. Exploration scenery fades/sinks during guardian camera transitions.
- `StyxWaterSurface` / `Resources/Art/StyxWater.shader`: animated water and waterfalls with continuous per-pixel currents, caustics, fine foam, light bands and four bounded player ripples. A shared material and reused mesh arrays bound allocations. Water fills the surrounding river and dashable chasms.
- `Resources/Art/UnderworldFloor.png`: generated cracked basalt and Greek inlay paving, tiled across every chamber. Motion blur and depth-of-field are overridden off for readable combat.
- `UnderworldParallaxRig`: guardian-stage depth. Custom backgrounds are scaled to world dimensions independently of image resolution.
- Both Asphodel exploration rooms use the enhanced 3840×2160 `Resources/ChamberBackgrounds/Chamber_00.png`; its guardian stage uses that painting too. Other rooms and the menu use `Resources/Art/UnderworldPanorama.png`.
- `UnderworldArtLibrary` slices `Resources/Art/UnderworldCreatures.png` into four shade and four guardian sprites. Runtime sway, lunge, casting, hit flash and slumber effects animate them.
- Eurydice uses `Resources/Characters/Eurydice/Eurydice_DarkCrimson_4x4.png`: four-frame Idle, Glide, Teaching and Shatter rows. Her source reference and crimson appearance are preserved.
- `UnderworldArtImporter` retains full-resolution uncompressed art with bilinear filtering. Generation notes: [art prompts](docs/underworld-art-prompts.md).

## Tutorial and interface
Eurydice’s anchored speech bubbles teach movement, dash, Soft, Boom, flute and the first S–S–B cadence. Each step starts a fresh action-count baseline, so previously performed actions cannot skip teaching. Wave one starts after completing the cadence. Context hints explain wards and abductors.

The HUD shows route/chamber, remaining shades, health, breath, the current phrase and contextual recovery advice. `UnderworldFrontEnd` provides title/pause/help and pointer/controller navigation. Gameplay input, escort movement and damage stop while menus are open; closing consumes the close frame and next frame to prevent click-through. Pause clears queued sensor pulses and does not preserve transient hitstop.

## Audio
`UnderworldSoundscape` synthesizes and caches stone/wet footsteps, impacts, bronze break, hurt, dry breath, portals and menu cues, plus wind, water and drone ambience. It blends ambience with encounter tension and depth. Volume/mute persist in PlayerPrefs. Existing procedural lyre/flute/cadence audio is retained.

## Verification
On 26 October 2026, Unity batch EditMode verification passed **23/23 tests**, with zero failures and exit code 0, in an isolated project copy. The rendered runtime smoke check also passed: title, twelve rooms, pause damage gating, hitstop resume, biome transitions, guardian stage and unsaved-scene restart. Screenshots of title, Asphodel tutorial, combat, river architecture and guardian stage were inspected. The batch editor emitted an unrelated Unity Search index exception; the harness excludes that editor-only stack from gameplay error counting.

EditMode tests cover tutorial progression, Eurydice atlas/animation, shade balance, breath/repetition, stagger/exposure, twelve room dimensions/spawns, artwork loading and transparent sprite boundaries, scenery construction/cleanup, and guardian immunity to untimed damage.

Run Unity with `-batchmode -nographics -runTests -testPlatform EditMode -testResults <xml> -logFile <log>`. If the source project is open in Unity, use a separate project copy (Assets, Packages, ProjectSettings); never launch a second editor against its locked directory.

The editor-only `OrpheusBelow.EditorTools.UnderworldSmokeCapture.RunBatch` is a disposable-copy runtime/render check. Run with `-batchmode -executeMethod OrpheusBelow.EditorTools.UnderworldSmokeCapture.RunBatch` and graphics enabled. It checks title, room count, damage gating during pause, hitstop resume, later rooms and guardian transition, and captures screenshots beside the disposable project. It exits the batch editor. It refuses non-batch use.

Physical microphone/ESP32 calibration and subjective combat feel still need testing on the intended hardware.
