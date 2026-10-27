# Descent, rescue and ascent implementation plan

**Goal:** Eight supplied chamber paintings form a solo descent to the deepest throne room and an escorted return to the surface. Artemis speaks the tutorial.

**Architecture:** A persistent journey state records depth, direction, cleared encounters, health/cadences and rescue status across scene loads. Each physical chamber has inward and outward passages with separate safe arrival points. Existing collision, foreground cover, zoom clamps, musical combat and Look 02 animations remain in use.

**Assets:** Latest eight TEST images: WhatsApp filenames 18.57.54–18.57.55, filesystem save time 19:09 (the only new eight-room batch matching the user's throne-room request). Retain pixels unchanged.

## Route

| Depth | Image suffix | Chamber |
| --- | --- | --- |
| 0 | 18.57.55 (6) | Sanctuary of Artemis — bright temple and bridge |
| 1 | 18.57.55 (5) | Gardens of the Upper Light — floral court |
| 2 | 18.57.55 (1) | Asphodel's Last Sun — fading golden garden |
| 3 | 18.57.54 | Court of Withered Cypresses — gray ruins |
| 4 | 18.57.55 (2) | Broken Causeways — divided lava terraces |
| 5 | 18.57.55 (3) | Falls of Phlegethon — lava cataract |
| 6 | 18.57.55 | Titan's Rest — fallen colossus |
| 7 | 18.57.55 (4) | Throne of the Below — deepest rescue |

Visit 0→7 alone, meet Eurydice after clearing the throne encounter, then visit 6→0 with her. The final surface gate waits for the escort. Pacified guardians remain asleep on return; return encounters emphasize escort threats. No descent dialogue or mechanic assumes Eurydice is already following.

## Interfaces and ownership

- Root: `UnderworldJourney` static service with `const int ChamberCount=8`, `DeepestChamber=7`, `ChamberNames[]`, `HasEurydice`, `IsAscending`, `Depth`, `ProgressLabel`, `Objective`; persistent `JourneyProgress` plain state model. Own scene travel, passage roles, encounter routing, companion spawn/rescue, map/runtime changes and integration tests.
- Layouts: existing definition adds `returnExit`, `returnSpawn`, `reunionPoint` in source pixels; existing `entrance` stays descent spawn, `exit` stays deeper passage/reunion point. Layout authoring owns scene builder/catalog and copies the eight source images. Author both endpoints, floor, solid cover/silhouettes, stairs, encounter center and safe enemy spawns.
- UI/Artemis: owns HUD and front end. Add `ShowArtemisTutorial(string, Action=null)` and `ShowArtemisHint(string,float)`, `CurrentTutorialSpeaker`; keep Eurydice-specific dialogue separate. Use journey label/objective and suppress escort UI while alone. New Artemis portrait uses existing broad-oil aesthetic.

## Execution

- [x] Reproduce old five-room/always-following behavior with failing tests.
- [x] Implement journey state, two-way doors, safe spawn direction, rescue, ascent-only companion and surface victory/restart.
- [x] Author eight collision/cover/stair maps and save eight real scenes; preserve crop constraints and exit alignment.
- [x] Integrate Artemis tutorial speaker/portrait, solo/escort HUD and rewritten premise.
- [x] Update outdated route tests; verify route transitions in both directions, rescue gating, state carry, once-only tutorial, solo start/restart, throne depth, collision reachability, cover and camera crops.
- [x] Run full EditMode suite and rendered gameplay route verification; review and document actual results.

Final checks: eight-scene build succeeded; EditMode 67 total / 63 passed / 4 unchanged baseline failures; rendered fifteen-visit route integration passed with zero runtime errors. Actual breadcrumb following around cover passed in all eight rooms. A focused independent review found no important route regressions. See `docs/painted-chambers-descent-2026-10-27.md` for the verification scope and known failures.

Use the parallel-agents skill for independent layout authoring and Artemis UI, with non-overlapping file ownership. Root integrates both with core journey logic and runs Unity centrally. Do not push unless requested.
