# Orpheus Below — implementation handoff

## Current revision: visual rollback and gentler combat
The user asked to restore the appearance from before the full overhaul and lower difficulty slightly. Runtime bootstrap version is **8**. Restart Play mode to rebuild older generated scenes.

Unity 6000.3.10f1, HDRP 17.3, Input System 1.18. Paths below are relative to this repository.

## Appearance and flow
- Restored the pre-overhaul painted Asphodel viaduct, original bridge geometry, simple enemy/guardian shapes, original floor/pillar/chasm rendering and minimal HUD.
- Orpheus remains the crimson rhombus. The earlier generated crimson Eurydice animations and her speech-bubble tutorial remain.
- New paving, painted creature sprites, exploration scenery and water shader are no longer used by the generated world.
- The game starts directly in Asphodel again. The overhaul title/pause component is no longer instantiated.
- Generated overhaul artwork and helper code remain available in the repository for recovery, but are dormant.
- Kept fixes for backdrop scaling, scene restart, camera reset and sharp combat rendering.

## Rooms and audio
Twelve rooms remain. Asphodel uses its original 9.2 × 24.5 bridge footprint to match the restored painting. Later rooms retain the larger 34–44 × 38–46 dimensions. Only the current room is active. Guardians appear in rooms 1, 5, 9 and 12. Both Asphodel waves contain five enemies again; later encounters retain two waves.

Additional combat, footstep, portal SFX and wind/water/drone ambience remain in `UnderworldSoundscape`. Existing lyre, flute and cadence audio remains.

## Gentler combat
- Soft costs **7** breath; Boom costs **24**. Recovery begins after **0.5s** at **24 breath/sec**.
- Third repeated note deals **60%** damage, fourth and later **30%**, with a **25%** extra breath cost.
- Completing a cadence restores 30 breath; entering a room refills it.
- Ordinary shade damage multiplier is **70%**; cadence exposure remains **135%** for 3.2s.
- Shade health scales by **1.05 + 0.08 × room index**; incoming damage scales by **0.90 + 0.04 × room index**.
- Stagger protection remains, preventing indefinite interruption by spamming.
- Flute works on unarmored shades below **70%** health or exposed by a cadence.
- Guardian health multiplier drops from 1.45 to **1.20**, timing window widens from 0.18s to **0.23s**, and extra off-beat punishment is limited to once per **1.2s**.
- Guardians still require timed stave notes; ordinary attacks cannot bypass the rhythm fight.

The original minimal cadence line now includes a small breath readout. Eurydice teaches movement, dash, Soft, Boom, flute and S–S–B before wave one. Each step counts newly completed actions.

## Controls
| Input | Action |
|---|---|
| WASD / arrows / left stick | Move |
| Space / Shift / gamepad south | Dash |
| Tap J / LMB / gamepad west | Soft |
| K / RMB / gamepad north, or hold/release J / LMB | Boom |
| F / gamepad left trigger | Flute stance |
| J, J, K (Soft–Soft–Boom) | Hymn beam; available immediately |
| Boom–Soft | Sanctuary, unlocked after room 1 |
| Soft–Boom–Soft | Vortex, unlocked after room 2 |
| Tab | Cycle keyboard/microphone/ESP32 source |
| H | Controls card |
| Hold Q / gamepad left shoulder | Forbidden look-back |
| R | Restart descent |

## Verification
Use an isolated copy of Assets, Packages and ProjectSettings when the source project is already open in Unity. EditMode tests cover tutorial, animation, enemy balance, breath, cadence rewards, stagger protection, room encounters and restoration of procedural enemy/guardian sprites. The render harness `OrpheusBelow.EditorTools.UnderworldSmokeCapture.RunBatch` now verifies the restored Asphodel, direct game entry, original enemy sprites, room transitions, guardian stage and restart.

Rollback validation: 24/24 EditMode tests passed. The graphics batch smoke check passed for restored Asphodel, direct entry, original enemy art, 12 rooms, guardian stage and restart. Captures and logs are in the ignored `.superpowers/overhaul-verification/rollback-*` files. The clone logged an editor Search indexing exception; no game runtime errors were counted. Physical microphone/ESP32 inputs and subjective difficulty still need testing on the intended hardware.
