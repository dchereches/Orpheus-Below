# Painted enemy bestiary

The eight character sheets in `D:/Serban/TEST` named `WhatsApp Image 2026-10-27 at 20.18.00.jpeg` and `20.18.01.jpeg` through `20.18.01 (6).jpeg` are the source designs. Their filesystem save time is **21:39:06**, matching the newest batch the user referred to as 21:38. One dedicated subagent implemented each requested creature; root integrated the shared art, combat and route code and ran Unity centrally.

## Actors and animation

| Actor | Source suffix | Body frames | FX frames | Role |
| --- | --- | ---: | ---: | --- |
| Cerberus | 20.18.00 | 32 | 16 | Guardian at depth 6; waits, warns, bites and tail-whips |
| Hooded Skeleton | 20.18.01 | 48 | 24 | Ordinary caster; locked pistol aim and single/fan volleys |
| Masked Monk | (1) | 48 | 24 | Mini-boss at depth 2; warned falling boulders |
| Mystic Eye Warden | (2) | 32 | 24 | Second boss, depth 3; physical eye-staff throw and harmless recall |
| Winged Sentinel | (3) | 32 | 24 | Mini-boss at depth 4; slow wide sword sweep and overhead cleave |
| Rooster Rider | (4) | 30 | 24 | Ordinary ascent abductor; warned dash, grab and retreat toward deeper exit |
| Tripod | (5) | 32 | 24 | First boss, depth 1; sonic waves to dash through |
| Shepherd | (6) | 32 | 24 | Mini-boss at depth 5; bell summons and commands a bounded flock |
| Summoned sheep | (6) | 32 | 24 shared | Attackable, stun/cadence-sensitive charging summons |

The nine libraries contain **318 body sprites and 208 effect entries**. The sheep intentionally reuse the Shepherd's 24 bell/cloud/dust/warning frames, giving **502 distinct generated frames** across **526 runtime sprite entries**. No still image was duplicated or transformed to inflate the animation count. Each body has eight motion rows: idle, move, windup, primary attack, secondary attack, recovery, hurt and defeat. The rider's grab row deliberately uses only two clean poses; neighboring clipped source poses are excluded.

All final generated PNGs are copied unchanged into `Assets/OrpheusBelow/Resources/Enemies/<Actor>/Body.png` and `Effects.png`. Each actor directory includes `Generation.md` with the exact built-in ImageGen prompts, reference and output paths; layouts and hashes are recorded in `AtlasLayout.json` where needed. Generation used the built-in tool, with no API/CLI fallback. The paintings retain the supplied designs, palette, clothing and broad oil strokes. Generous transparent gutters, selected source rectangles and stable foot pivots take precedence over a larger grid.

## Runtime and rebuilding

Use **Orpheus Below/Bake Painted Bestiary** to rebuild `Library.asset` sprite subassets and update the saved guardian metadata. Bootstrap version is **13**. Existing scene geometry is unchanged; the eight saved chamber scenes read the updated layout definitions at runtime. The throne rescue and escorted ascent remain intact, and guardians stay pacified on ascent.

`PaintedEnemyActor` connects existing shade health/stun/armor/cadence logic and guardian rhythm milestones to cancellable attack patterns. Ordinary caster and abductor behaviors use their own modules. Existing melee lungers and armored wardens retain their original combat roles with painted skeleton poses. Guardian enum aliases retain serialized legacy values: Tripod/Cantor, Eye/Oarsman and Sentinel/Siren. Monk and Shepherd add new values.

`EnemyArtAnimator` selects successive frames using the duration of the actual combat phase; idle and movement loop, while windup, attack, recovery and reactions play their own rows. Pausing freezes the animation clock and attacks. `PaintedBestiaryAssetBuilder` honors explicit source rectangles and variable motion counts, checks transparency, and bakes a common body scale from the idle silhouettes. Original atlas pixels stay unchanged.

Animated bullets, staff and sonic waves sweep their entire movement against solid cover and respect dash invulnerability. Lyre projectile clears and the flute nova remove them. The side-view duel uses its existing open stage; exploration retains mapped cover collision. Circular warnings define the exact committed damage area and never follow a dodging player. The Monk's rocks drop at locked points after a 1.35-second warning; Sentinel sweeps warn for 1.8 seconds. Every attack has recovery time.

The rider registers with the existing abduction mechanic, retreats toward the current chamber's actual deeper passage, and moves Eurydice through the same collision-safe ground movement as her normal following. Sanctuary, damage stagger, stun and defeat release her. The Shepherd keeps at most three living sheep, creates at most two per summon and skips a planned spawn if the player has entered it. Sheep have a full 0.8-second dash warning and are disabled immediately when their owner is interrupted or defeated.

## Verification

The initial red tests reproduced missing painted libraries and missing mini-boss encounters. The centralized bake succeeded for all nine actors and all six encounter placements.

The final EditMode run (`Logs/bestiary-complete-final-editmode.xml`) reports **94 tests: 90 passed, 4 failed**. All newly added bestiary checks passed: imported frame availability and transparency, variable motion lengths, pause clock, committed warning geometry, cancellation, no solo abduction, owner-specific flock cleanup, projectile cover, dash immunity, Cerberus anticipation, a moved skeleton firing from its current position, and the Sentinel approaching within sword range before committing its warning. The four failures match the earlier baseline: three extended Asphodel tutorial expectations and `OffBeatOrdinaryDamageCannotDefeatGuardian`.

`PaintedBestiaryVerification.RunBatch` reports **RENDER PASS with zero runtime errors** (`Logs/bestiary-complete-render.log`). It rendered all eight motion rows for each of the nine actors, exercised their live warning/primary attack sequences, and checked that cancellation removed projectiles, shadows and summons. Local captures are in `Logs/PaintedBestiary/`. This is an attack-integration smoke test, not an end-to-end combat balance or difficulty assessment.

A separate read-only integration review identified and verified fixes for live muzzle positions, retention of ordinary melee roles, cleanup on terminal player states and immediate disabling of cancelled sheep. Source/document whitespace checks passed. Unity-generated YAML retains the editor's normal formatting.

The fifteen-visit descent/rescue/ascent route also passed with zero runtime errors (`Logs/bestiary-journey-render.log`), including actual passages, companion creation, waiting at gates, pacified return guardians, travel health/cadences, surface victory and restart. That route harness bypasses combat; the bestiary harness separately exercises the attack patterns.
