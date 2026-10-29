# Orpheus broad-oil animation guide

Orpheus now uses the complete library at `Resources/Characters/Orpheus/BroadOil/Library`. The procedural diamond/head/lyre figure remains a safety fallback and is hidden only when every required clip is valid.

## Runtime structure

- `OrpheusAnimationBrain` selects motion and eight-sector facing without restarting identical states.
- `OrpheusSpriteAnimator` plays body and effects, freezes with blocked gameplay/tutorial/time scale, and preserves committed actions when movement cancels passive recovery. Walk/run/stair frames advance by ground distance (2.5-unit walking and 3.8-unit running strides), including across direction changes.
- `OrpheusGroundShadow` draws contact, ambient, and long directional cast layers below actor order 14.
- `OrpheusOilSurface` preserves sprite alpha/geometry and adds broad connected paint through a `MaterialPropertyBlock`. Colored rim outlines remain disabled by art direction.
- Every painted chamber stores its own dominant light direction, color, and intensity. Missing metadata resolves to an up-right light/down-left cast.

## Gameplay triggers

| Gameplay event | Motion |
|---|---|
| movement / dash / stairs | idle, start, walk, run, stop, turn, dash phases, stair ascent/descent |
| soft combo | first-note lyre draw, `SoftStrike1` → `SoftStrike2` → `SoftStrike3`, pluck follow-through, third-hit `SoftBolt`; dash converts to `DashStrike` |
| hard release | brief `HardBoomWindup` → `HardBoomRelease` |
| cadence finishers | `FullVoiceRelease` → `HymnBeam`, `SanctuaryCast`, or `ConvergenceCast` |
| open cadence window | `SingSustain`; timeout/completion resolves through `PhraseResolve` |
| aulos | draw, loop, soft/hard note, `Discord`, `Requiem` |
| damage / exhaustion | light hit, heavy hit, grounded hurt, rise, breath exhausted/recover |
| journey | chamber entrance → inspect → encounter alert, chamber clear, look-back temptation/fatal, Eurydice grief, victory, defeat |

Damage, fatal look-back, and grief outrank attacks and cosmetic states. Victory stops dash/movement and owns a terminal pose. Teleport clears queued one-shots and motion history. Movement stops on released input and reverses without coasting; boss rail position is applied directly. Default run speed is 8.2 units/second with acceleration 220. Dash charges, invulnerability, damage, reach and collision rules are preserved.

The body oil material writes alpha-clipped depth for cover occlusion. Clips with painted effects already embedded do not draw another overlay. Dashes use a short, low-opacity ground brush streak instead of full-character ghosts. The procedural flute is hidden with production art, and the note guide follows the top of the actual painted silhouette. Terminal grief and victory play into their final poses and hold without looping back to standing.

## Atlas repair (2026-10-29)

`OrpheusAtlasSlicer` finds connected figures inside measured row gutters, discards detached neighboring objects, and bakes a mesh that excludes other alpha islands. Ground pivots favor boot paint instead of the center of a wide cloak or slash. The original sources remain unchanged.

`RunCycle.png` and `WalkCycle.png` are new native generated sources, each four chronological poses by five directional rows (S, SE, E, NE, N). NW/W/SW mirror the matching eastern pose; `mirrorWest` is explicit clip metadata. The seven-row `LyreSinging.png` source is sliced as seven rows; its first five consistent directions supply vocal poses and their western mirrors. Named actions now use anticipation, contact and recovery poses instead of four references to one still wherever the source supports them.

## Rebuild and verify

In Unity use **Orpheus Below → Bake Orpheus Broad Oil Atlas**, then **Orpheus Below → Verify Orpheus Broad Oil Animation**. Batch equivalents:

```powershell
Unity.exe -batchmode -nographics -projectPath <project> -executeMethod OrpheusBelow.EditorTools.OrpheusBroadOilAssetBuilder.Build -quit
Unity.exe -batchmode -nographics -projectPath <project> -executeMethod OrpheusBelow.EditorTools.OrpheusAnimationVerification.Run -quit
```

The verifier writes `Logs/OrpheusBroadOil/Verification.json`, a 48-state contact sheet, an eight-direction action matrix, walk-cycle frames and `AllClips/` showing every installed frame at a consistent world scale. Previews rasterize the actual baked geometry. The library contains 48 motions, 1,296 body slots, 672 source poses and 64 effect sprites.

`OrpheusBelow.EditorTools.OrpheusAnimationRuntimeVerification.RunBatch` renders 33 controlled Play Mode segments in the actual first chamber to `Logs/OrpheusRepair/Runtime/`. It skips the intro, removes the voice component and disables live sensor input only in this disposable verification run, then drives the real movement/collision and combat components. It restores the intro-viewed preference before exiting. It never saves a scene.

`Generation.md`, `GenerationPrompts.json`, `RepairGenerationPrompts.md` and `AtlasLayout.json` beside the PNGs preserve provenance and slicing semantics.
