# Full-body athletic running

The previous repair alternated legs beneath a frozen, crouched torso. It had no flight interval and repeated the same arms. The user correctly identified the result as sneaking. This change replaces the underlying posture and full-body motion rather than speeding up that cycle.

- [x] Generate five upright painted canonical views with the built-in image tool, preserving Orpheus's clothes, curls and lyre.
- [x] Author anatomical source masks/landmarks and a sixteen-phase full-body run, with opposing arms, heel recovery, torso rise/pitch and cape follow-through.
- [x] Fit one constant skeleton to each source so the stride does not lower the upright pelvis into a crouch.
- [x] Use a5.8-unit stride, .34 support duty and alternating flight intervals; support feet counter actual root displacement.
- [x] Address review findings: compensate boss-stage visual scale, use exact posed renderer culling bounds and keep frame selection stable at floating-point boundaries.
- [x] Fix the actual fast-stair/start/turn path: three Unity regressions first showed `WalkCycle` being displayed, then passed with full-speed visual substitution while preserving logical stair states.
- [x] Verify all78 focused animation tests pass, including material-fragment checks over every installed pose and physical/source-painted motion checks.
- [x] Inspect native in-game movement and check every source for detached retained paint.
- [x] Run focused/full Unity tests and resolve review findings before the authorized push/merge/pull workflow.

The bitmap sources are imported at512px maximum for game use. Full-resolution generated PNGs remain editable source assets. All moving layers use a welded2px lattice; the retained body keeps only its largest connected island, preventing old limb remnants from becoming floating objects.

Initial Unity checks caught two actual integration defects: source Sprite bounds did not contain extended poses, and a .70-scaled boss billboard did not advance its gait fast enough to keep the support boot planted. Both had reproducing tests before their fixes. Two initial front-view assertions were corrected to account for vertical heel recovery projecting onto screen depth; they now check actual painted texel positions against the projected skeleton.

Native motion review also exposed an unnatural rear elbow position. Replaced the wrist-driven IK path with a constant90-degree elbow pump below the shoulder; dense angle/bone checks first failed on the prior model, then passed. Source anatomy overlays identified incorrectly placed shoulder landmarks and an incomplete far-upper-sleeve mask; the corrected landmarks/masks prevent retained sleeve fragments and detached hands. A rear-view shoulder had inadvertently been placed on transparent pixels and was moved back onto the painted shoulder root.

Independent review found no remaining blocker in the scale, culling or fast-locomotion visual-state integration. Attacks, dash, musical performances and ending states retain their own artwork.

Final Unity results:78/78 focused tests and384/385 full-suite tests passed. The only full-suite failure is the unchanged guardian baseline below. The native33-segment,559-frame capture passed, including actual authored stair ascent/descent, reversals, planted stops, dash, attacks, singing, flute, reactions and held ending poses. The previous leg-only baker was removed from the active project to avoid accidentally restoring that failed approach.

The final movement movie is saved locally in the original checkout at `output/athletic-running-2026-10-29/Running-Final.mp4`. Sources and exact generation prompts are under `Resources/Characters/Orpheus/BroadOil/AthleticRunGeneration.md`; source PNGs are unmodified built-in image-generator outputs, with anatomical posing handled in code.

Known baseline: `UnderworldOverhaulTests.OffBeatOrdinaryDamageCannotDefeatGuardian` already fails on main (expected792,actual0); no combat-balance change belongs in this animation repair.
