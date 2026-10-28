# Combat Pressure Implementation Plan

**Goal:** Keep enemies on reachable fighting ground, increase encounter pressure, give every painted enemy 2–3 attacks, add ground shadows, and require dodging during boss rhythm phrases.

**Architecture:** Dedicated workers own the nine attack modules and new behavior tests. Root owns reachable spawn selection, multi-wave director progression, shared actor shadows and independent rhythm hazards. Reuse the existing paintings, swept collision, dash immunity and scaled gameplay clock.

**Implementation choices:** Two waves in early floor encounters, three deeper down and on the return. Major bosses remain immediate duels. New spawn points must connect to the current player's navigation region, stay within a bounded path distance and keep space between actors. Faster attacks retain warnings and a way to evade; rhythm pressure occurs even when every musical note is answered correctly.

- [x] Add regression tests for unreachable spawns, multi-wave progression, all nine shadows and rhythm pressure.
- [x] Extend navigation graph traversal for reachable spawn selection; resolve positions again when each wave actually summons.
- [x] Add two/three-wave plans and director progression without early passage unlocks.
- [x] Integrate each dedicated attack worker: Cerberus, Tripod, Eye, Monk, Sentinel, Skeleton, Rooster, Shepherd, Sheep.
- [x] Add shared actor shadows and independent, warned boss-rhythm hazards with complete lifecycle cleanup.
- [x] Run EditMode checks (267/271, only four existing failures), verify 15 real visits/34 waves and perfect-note rhythm dodging, and complete independent source review. Deliver through the active D: Unity checkout.

**Verification:** Preserve four documented baseline failures. Intentional old cadence/one-wave expectations must be revised to assert the new behavior. Check pause, stun/death cleanup, connected floor/cover clearance, multiple riders, bounded flocks and note hits while moving.
