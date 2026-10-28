# Combat pressure, reachable summons and boss rhythm dodging

Bootstrap version **16** keeps the current artwork and increases the variety and pace of actual encounters.

## Spawning and waves

Wave summons use the player's connected navigation region, including solid cover and the healing basin, rather than choosing any nearby walkable point. Candidate positions have at least .65m floor clearance, 1.6m separation, 3m distance from the player and at most 10m of navigation travel. They are resolved before the summon runes appear. The Shepherd uses the same reachable-floor selection for summoned sheep.

Early floor encounters have two waves; deeper rooms and the return have three. Groups contain three to six enemies. Skeletons remain uncommon support, while Monks, Sentinels, Shepherds and return-path riders supply varied pressure. There is at most one Shepherd per wave and three living sheep per Shepherd. Reinforcements must be defeated before the passage unlocks. The three major boss rooms remain immediate duels.

## Nine dedicated enemy assignments

| Enemy | Dedicated worker | Attacks |
| --- | --- | --- |
| Cerberus | cerberus_attack_expansion | Bite, tail whip, committed lane pounce |
| Tripod | tripod_attack_expansion | Single sonic pulse, double echo, outbound/return chorus |
| Eye Warden | eye_attack_expansion | Staff throw/recall, eye fan, staggered ground sigils |
| Masked Monk | monk_attack_expansion | Staggered stone cluster, sweeping stone line, broken stone ring |
| Winged Sentinel | sentinel_attack_expansion | Wide sword sweep, narrow thrust, traveling blade fan |
| Hooded Skeleton | skeleton_attack_expansion | Doubletap, open fan, closing volley |
| Rooster Rider | rooster_attack_expansion | Grab dash, two-stage hook dash, warned wing kick |
| Shepherd | shepherd_attack_expansion | Call flock, command charge, bell volley |
| Sheep | sheep_attack_expansion | Straight charge, re-warned hook charge, close stomp |

Shorter cooldowns and recovery increase attack rate; warnings remain committed and readable. Dash immunity, cover collision, attack interruption, terminal-state cleanup and pause are preserved. Persistent soft contact shadows are created once for every actor, including bosses and summoned sheep. Temporary attack shadows remain separate.

Projectile-lane warnings enclose the entire swept capsule, including the spaces between adjacent warning disks. Stalled frames cannot swallow the visible warning. A separate reviewer checked attack geometry, lifecycle cleanup, spawning and rhythm hazards.

## Rhythm phases

The musical stave now runs alongside an independent sequence of locked ground strikes and painted sweeping projectiles. These hazards occur even if every note is answered correctly. Players can move or dash while answering notes; successful inputs continue to damage the boss. Crash, defeat and terminal states cancel pending rhythm strikes and owned projectiles. Duel warnings face the side-view camera so their horizontal reach remains visible.

## Verification

The first 17 shared regressions failed before implementation. The combined EditMode suite has **271 tests: 267 passed, with the same four previously documented failures** (three Asphodel tutorial cases and the ordinary off-beat guardian-damage case). New tests cover real attack execution, committed warning geometry, damage/evading, cover, connected spawning for every floor encounter, flock limits, multi-wave plans, all nine shadows and lifecycle cleanup. Results: `Logs/combat-pressure-verified.xml`.

EditMode fixtures explicitly isolate transient menu/frame locks and activate synthetic maps. This keeps tests independent of paused scene UI without changing runtime menu behavior. Source review has no remaining material blockers.

**Actual graphics/journey verification passed:** all 15 chamber visits, 34 connected waves, all nine enemy designs, all three boss attack kits, six correctly answered notes per tested boss hymn with independent dodge hazards, sheep dashes, rooster grab/stun rescue, actual defeat callbacks, both passage directions and surface victory. Zero game runtime errors; process exit 0. Log: `Logs/combat-pressure-journey-verified.log`; captures: `Logs/EncounterGameplay/`, including each major boss's `rhythm-dodge` image. The escort sampler keeps Eurydice active with a long follow lag because the new rider correctly ignores disabled companions.
