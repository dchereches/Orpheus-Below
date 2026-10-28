# Broad oil cast and varied encounters

The character repaint keeps existing costumes, silhouettes, equipment and palettes, with fewer small details and long continuous strokes of thick paint. The independent character assignments and art director's acceptance evidence are recorded in [the art review](../Documentation/BroadOilCharacterReview.md). Each character's resource folder records generation prompts, source paths and selected-output hashes. Raster work uses built-in ImageGen; PNG pixels are retained unchanged.

## Gameplay

Monks, Sentinels and Shepherds now recur as regular floor enemies. They keep their dedicated warned attacks, health and musical interactions, with neither overhead nor boss health bars. Their small-group encounters replace most skeleton slots. The three immediate major duels and deepest throne rescue remain in place.

| Descent depth | Encounter |
| --- | --- |
| 0 | Skeleton and Monk |
| 1 | Tripod, 528 HP |
| 2 | Monk, Sentinel and skeleton |
| 3 | Mystic Eye Warden, 672 HP |
| 4 | Two Sentinels and a Monk |
| 5 | Shepherd, Sentinel and Monk |
| 6 | Cerberus, 1104 HP |
| 7 | Skeleton, Monk, Sentinel and Shepherd, then Eurydice's rescue |

Each return chamber includes rooster abductors and rotating Monk/Sentinel/Shepherd threats. Only return depths 0, 3 and 6 include a skeleton. There is at most one Shepherd per encounter and at most three living sheep per Shepherd. The full journey's skeleton slots drop from 27 to 6, while former mini-boss appearances rise from 3 to 25. Group sizes remain close to the previous course.

Boss health doubles relative to the previous configured values, including retained legacy guardian identities. World character silhouettes grow by 20% through sprite pixels-per-unit, preserving collision footprints and attack radii. Eurydice's surrounding non-damaging effects grow with her body. Guide portraits also grow by 20% within the existing dialogue panel. Separate authored spawn positions keep recurring enemies from stacking at the former single mini-boss anchor.

## Animation integration

Enemy attack modules and Eurydice's directional idle, movement, stairs, emotions and surrounding effects retain their semantic rows. The user explicitly deferred Orpheus's redesign, so his current artwork and animation behavior remain, with only the requested size increase. All regenerated Eurydice body concepts were rejected. Her five last-commit PNGs remain byte-identical; a separate oil-surface material simplifies their internal colours and adds brush texture without altering the original alpha outline, pose, face geometry or sprite mapping. The confirmed 20% height increase is uniform.

Use **Orpheus Below/Bake Broad Oil Cast** after replacing reviewed atlases. The baker imports full-resolution transparent PNGs with bilinear filtering and derives source-aware sprite boundaries/foot anchors. Bootstrap version is 15.

## Verification

The initial 17 balance/size regressions failed before implementation. The final EditMode run contains **146 tests: 142 passed and four pre-existing failures**. All new encounter diversity, boss-health, hidden-health-bar, 20% scale, frame slicing and Eurydice source/geometry checks pass. The unchanged failures are three `AsphodelCombatTutorialTests` cases (flute entry, preplayed actions and movement/dash prompts) and `UnderworldOverhaulTests.OffBeatOrdinaryDamageCannotDefeatGuardian`. Results: `Logs/broad-oil-verified.xml`.

**Bake passed:** all nine enemy libraries contain 295 playable body sprites and 208 FX entries; Eurydice retains her 148 original frames. The baker fits FX boundaries to actual painted content, and explicit measured body rectangles preserve heads, feet and weapons. Overlapping unusable poses are excluded rather than counted as playable animation.

**Eurydice graphics check passed:** actual Unity rendering verified quiet idle, eight walking directions, teaching, acoustic reply, joy, fear, sanctuary, shielded walking, rescue, chamber-clear joy, both stair directions, victory, reform/shatter and pause, with zero game runtime errors. The isolated sampler explicitly supplies the companion cache normally populated by the enabled player's Update. Log: `Logs/eurydice-original-surface-verified.log`. Focused original/finished comparison images: `Logs/EurydiceLook02/12-original-committed-sprite.png` and `13-painted-original-sprite.png`. The finish is deliberately confined to interior colours; silhouette, face geometry and every source PNG remain unchanged.

**Real journey passed:** `EncounterGameplayVerification.RunBatch` traversed all 15 chamber visits using the actual director spawns, attack modules, damage/defeat callbacks, passages, throne rescue and surface victory. It observed all eight enemy designs plus sheep, completed warned attacks, checked sheep dashes and rooster grabs/stun rescues, and asserted that regular floor encounters hide the boss bar. Deeper return rooms can have two simultaneous rooster grips; the sampler verifies each struck rider releases its own grip and interrupts all remaining riders before expecting a complete rescue. Exit code 0; zero game runtime errors. Log: `Logs/broad-oil-journey-verified.log`; captures: `Logs/EncounterGameplay/`.

The independent art director approved the stronger Eurydice surface comparison and representative back/profile walking, joy/fear and stair ascent/descent captures. Her finish remains more restrained than the generated NPC paintings to preserve her exact original face and anatomy. Unity's unrelated editor SearchDatabase startup exception is excluded from game runtime error counts. An earlier journey run timed out across an overnight system pause; the completed rerun above is the final result.
