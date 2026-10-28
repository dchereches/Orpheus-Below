# Orpheus broad-oil animation guide

Orpheus now uses the complete library at `Resources/Characters/Orpheus/BroadOil/Library`. The procedural diamond/head/lyre figure remains a safety fallback and is hidden only when every required clip is valid.

## Runtime structure

- `OrpheusAnimationBrain` selects motion and eight-sector facing without restarting identical states.
- `OrpheusSpriteAnimator` plays body and matching painted-effect rows, freezes with blocked gameplay/tutorial/time scale, and queues lower-priority follow-throughs behind committed attacks.
- `OrpheusGroundShadow` draws contact, ambient, and long directional cast layers below actor order 14.
- `OrpheusOilSurface` preserves sprite alpha/geometry while adding broad connected paint and chamber-authored rim light through a `MaterialPropertyBlock`.
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

Damage, fatal look-back, and grief outrank attacks and cosmetic states. Teleport clears queued one-shots and motion history. Combat timing, colliders, reach, damage, input, and balance are unchanged.

The body oil material writes the same alpha-clipped depth as painted actors, so the authored chamber rim survives `PaintedActorDepth` without losing cover occlusion. The separate effect renderer selects one of eight effect families for chord cuts, dash streaks, Boom, Hymn, Sanctuary, Convergence, musical/aulos pulses, Discord, and Requiem.

## Rebuild and verify

In Unity use **Orpheus Below → Bake Orpheus Broad Oil Atlas**, then **Orpheus Below → Verify Orpheus Broad Oil Animation**. Batch equivalents:

```powershell
Unity.exe -batchmode -nographics -projectPath <project> -executeMethod OrpheusBelow.EditorTools.OrpheusBroadOilAssetBuilder.Build -quit
Unity.exe -batchmode -nographics -projectPath <project> -executeMethod OrpheusBelow.EditorTools.OrpheusAnimationVerification.Run -quit
```

The verifier writes `Logs/OrpheusBroadOil/Verification.json`, a 48-state contact sheet, an eight-direction action matrix, and walk-cycle frames. `Generation.md`, `GenerationPrompts.json`, and `AtlasLayout.json` beside the PNGs preserve provenance and slicing semantics.
