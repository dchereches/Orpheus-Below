# Broad Oil Cast Implementation Plan

> **For agentic workers:** Use the parallel art assignments and root integration workflow, with an independent art director review before baking.

**Goal:** Paint the complete cast with broad, simple oil strokes, enlarge world artwork by 20%, and replace skeleton-heavy floor encounters with recurring former mini-bosses.

**Architecture:** Keep the existing attack modules and animation-state mappings. Artists own atlas PNGs/manifests; root rebakes sprite libraries and changes encounter policy, health, HUD and sizing. Orpheus's artwork is explicitly deferred by the user.

**Tech Stack:** Unity 6000.3, C#, generated transparent PNG atlases, existing ImageGen pipeline.

**Spec:** `docs/superpowers/specs/2026-10-28-broad-oil-cast-design.md`

- [x] Add failing regression coverage for roster variety, fewer skeletons, hidden recurring-enemy health bars, doubled boss HP and 20% artwork scale.
- [x] Implement rosters in `ChamberEncounterPlan`, boss health in `CerberusRhythmBoss`, regular-enemy HUD rules and library sizing. Retain bounded sheep and actual exit callbacks. Twelve focused behavior checks pass; final artwork scale checks await the reviewed atlas bake.
- [x] Assign every character to a dedicated art worker; independently review broad strokes, simplified forms, identity, animation order and alpha. Defer Orpheus as explicitly requested.
- [x] Bake reviewed enemy libraries and the unchanged committed Eurydice atlases: 295 enemy body frames, 208 enemy FX entries and 148 Eurydice frames. The user's final Eurydice instruction supersedes regenerated concepts: preserve exact last-commit proportions and apply only a native RGB surface finish. Keep Orpheus's existing artwork and behavior.
- [x] Run EditMode regressions (142 passed, four unchanged baseline failures), independently reviewed Eurydice direction/emotion/stair graphics checks and the actual 15-visit journey (both render/journey harnesses exit 0, zero game runtime errors). Document prompts, source assets, review evidence and results.
