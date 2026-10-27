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

Use **Orpheus Below/Bake Painted Bestiary** to rebuild `Library.asset` sprite subassets and update the saved guardian metadata. Bootstrap version is **14**. The eight saved chamber scenes read the updated layout definitions at runtime. The throne rescue and escorted ascent remain intact, and major/mini-bosses stay pacified on ascent.

`ChamberEncounterPlan` supplies the actual director rosters: three opening skeletons at depth 0; immediate Tripod duel at 1; Masked Monk plus two skeletons at 2; immediate Eye duel at 3; Sentinel plus two skeletons at 4; Shepherd plus two skeletons at 5; immediate Cerberus duel at 6; four skeletons at the throne at 7. Each ascent chamber has rooster abductors and two skeletons. There is one encounter per visit, without two generic waves hiding the unique designs.

The three mini-bosses have distinct shade identities and base health (Monk 200, Sentinel 250, Shepherd 230), increased further by chamber pressure. They retain the isometric painted floor and cover, show their own name/health bar, and receive Artemis entry guidance explaining their attack. Soft notes damage them without resetting every attack; Hard Booms, stun and other interruption mechanics still work. The major bosses use their existing side-view rhythm milestones. Legacy melee roles retain their behavior with Sentinel paintings rather than silently becoming skeletons. Serialized legacy enum values remain stable.

Floor pursuit uses a cached navigation grid over the authored walkable polygons, checks complete route segments against cover, and still sweeps actual movement against physical obstacles. The Monk and Shepherd approach when the player is distant; the Sentinel closes to sword range. Charges commit to their original heading and stop at obstacles.

`EnemyArtAnimator` selects successive frames using the duration of the actual combat phase; idle and movement loop, while windup, attack, recovery and reactions play their own rows. Pausing freezes the animation clock and attacks. `PaintedBestiaryAssetBuilder` honors explicit source rectangles and variable motion counts, checks transparency, and bakes a common body scale from the idle silhouettes. Original atlas pixels stay unchanged.

Animated bullets, staff and sonic waves sweep their entire movement against solid cover and respect dash invulnerability. Lyre projectile clears and the flute nova remove them. The side-view duel uses its existing open stage; exploration retains mapped cover collision. Circular warnings define the exact committed damage area and never follow a dodging player. The Monk's rocks drop at locked points after a 1.35-second warning; Sentinel sweeps warn for 1.8 seconds. Every attack has recovery time.

The rider registers with the existing abduction mechanic, retreats toward the current chamber's actual deeper passage, and moves Eurydice through the same collision-safe ground movement as her normal following. Sanctuary, damage stagger, stun and defeat release her. The Shepherd keeps at most three living sheep, creates at most two per summon and skips a planned spawn if the player has entered it. Sheep have a full 0.8-second dash warning and are disabled immediately when their owner is interrupted or defeated.

## Encounter integration verification — 28 October

New regression tests first reproduced three missing mini-boss rosters and three major bosses gated behind generic waves. The final EditMode run (`Logs/encounter-final.xml`) reports **117 tests: 113 passed, 4 existing failures**. The encounter wiring, imported attack/animation routing, all three immediate bosses, ascent rosters, eight painted navigation cases, independent flock movement, camera startup and warning disposal pass. The existing failures are the same three extended Asphodel tutorial expectations and `OffBeatOrdinaryDamageCannotDefeatGuardian` noted below.

The real route check exposed a physical basin omitted from the painted navigation polygons. Its footprint now participates in path clearance, and riders seek a clear approach before warning a dash. An immediate boss's requested camera view also survives camera startup. Scene teardown can safely cancel warnings that Unity has already destroyed. Each fix has a regression that failed before the change.

`EncounterGameplayVerification.RunBatch` **passed all 15 actual chamber visits with zero game runtime errors** (`Logs/encounter-gameplay.log`). It follows the actual director and physical passages, asserts the expected creature at every depth, observes warnings and completed attacks, then applies damage through each real enemy's hit handler and checks that death callbacks open the passage. Its observations include the Shepherd's charging flock, both Cerberus attacks, rooster abduction followed by a stun rescue, and surface victory with Eurydice. Captures are in `Logs/EncounterGameplay/`. This complements the earlier art showcase, which constructed actors directly and could not establish that players encountered them. The harness automates damage and passage travel; it does not assess human combat balance.

## Earlier asset verification

The initial red tests reproduced missing painted libraries and missing mini-boss encounters. The centralized bake succeeded for all nine actors and all six encounter placements.

The final EditMode run (`Logs/bestiary-complete-final-editmode.xml`) reports **94 tests: 90 passed, 4 failed**. All newly added bestiary checks passed: imported frame availability and transparency, variable motion lengths, pause clock, committed warning geometry, cancellation, no solo abduction, owner-specific flock cleanup, projectile cover, dash immunity, Cerberus anticipation, a moved skeleton firing from its current position, and the Sentinel approaching within sword range before committing its warning. The four failures match the earlier baseline: three extended Asphodel tutorial expectations and `OffBeatOrdinaryDamageCannotDefeatGuardian`.

`PaintedBestiaryVerification.RunBatch` reports **RENDER PASS with zero runtime errors** (`Logs/bestiary-complete-render.log`). It rendered all eight motion rows for each of the nine actors, exercised their live warning/primary attack sequences, and checked that cancellation removed projectiles, shadows and summons. Local captures are in `Logs/PaintedBestiary/`. This is an attack-integration smoke test, not an end-to-end combat balance or difficulty assessment.

A separate read-only integration review identified and verified fixes for live muzzle positions, retention of ordinary melee roles, cleanup on terminal player states and immediate disabling of cancelled sheep. Source/document whitespace checks passed. Unity-generated YAML retains the editor's normal formatting.

The fifteen-visit descent/rescue/ascent route also passed with zero runtime errors (`Logs/bestiary-journey-render.log`), including actual passages, companion creation, waiting at gates, pacified return guardians, travel health/cadences, surface victory and restart. That route harness bypasses combat; the bestiary harness separately exercises the attack patterns.
