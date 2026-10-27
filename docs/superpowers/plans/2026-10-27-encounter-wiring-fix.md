# Make the painted bestiary part of normal gameplay

The previous implementation mapped every regular melee unit to skeleton art and placed all six unique guardians behind two generic waves. Its render harness created creatures directly, so it did not prove that players met them in the normal course.

Keep the approved paintings and animation libraries. Use explicit chamber encounter rosters: opening skeleton lesson; immediate Tripod boss at depth 1; isometric Masked Monk at 2; immediate Eye boss at 3; isometric Sentinel at 4; isometric Shepherd with attackable flock at 5; immediate Cerberus at 6; throne encounter/rescue at 7. The return uses skeletons and rooster abductors, with major and mini-bosses remaining pacified. Reduce repeated generic waves.

Give the three mini-bosses explicit shade identities, health, their existing dedicated attack patterns, engagement range, safe floor navigation and clear entry guidance. Preserve circle-before-boulder, slow warned sword sweep, bounded sheep dash summons, committed staff/sonic projectiles, Cerberus anticipation and escort abduction. No generic melee identity should silently choose skeleton art.

- [x] Reproduce missing mini-boss roster and gated major-boss arrival with failing tests.
- [x] Add explicit encounter policy, mini-boss identities, art/attack routing and encounter metadata.
- [x] Add obstacle-aware floor navigation and safe isometric mini-boss targeting/summons.
- [x] Verify the actual director spawns, activates and clears each encounter; do not instantiate showcase enemies in place of real gameplay.
- [x] Run regression/visual checks and preserve concurrent remote updates (117 tests: 113 pass, four unchanged baseline failures; actual 15-visit encounter course passes).
- [x] Commit and push the verified fix.

An isolated checkout is used because Unity is open on the primary checkout. Leave its pre-existing metadata edits untouched.
