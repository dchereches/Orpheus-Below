# Eight-room descent painting provenance and authoring

This route supersedes the earlier five-image route described in `painted-chambers-2026-10-27.md`. All eight source files came from `D:/Serban/TEST`, with the 18:57:54–18:57:55 WhatsApp filenames saved on October 27, 2026. Each is copied directly to `Assets/OrpheusBelow/Resources/PaintedChambers/Chamber_00.jpeg` through `Chamber_07.jpeg`; no crop, recolor, resize, or recompression is applied to the source paintings. SHA-256 comparison confirmed identical source and destination bytes. Existing `Chamber_00`–`04` importer metadata and GUIDs are retained.

The request referred to an 18:09 save; the latest matching eight-file batch actually had filesystem write times at 19:09:54. This discrepancy was reported before implementation. This batch includes the throne, fallen titan, lava scenes and bright gardens.

## Journey and runtime

Orpheus descends alone through indices 0–7, clears the throne encounter, and approaches the waiting Eurydice on the carpet. That rescue turns the journey upward in the same scene. He then escorts her through 6–0; the sanctuary's upper temple passage ends the run only when she is nearby. There are eight physical scenes and fifteen visits. The throne is a turnaround, never a victory shortcut.

`UnderworldJourney` owns persistent direction, depth, per-leg encounter clears and tutorial completion. `PaintedChamberScene` carries health/cadences; both restart actions reset the journey. `ChamberExitDoor` has Deeper, Surfaceward and Reunion roles, and retries its trigger while a nearby player waits for the escort. `PaintedChamberMap.ConfigurePassages` enables the passage for the current leg. Four guardians occupy descent depths 1, 3, 5 and 6; they remain pacified on ascent, where encounters include abductor threats. No abductor is spawned on the solo descent.

Artemis speaks opening lessons, cadence guidance and rescue instructions. The HUD hides companion-specific warnings on descent and labels the current leg. Her portrait and exact generation prompt are in `Resources/Characters/Artemis/`; Eurydice retains her approved Look 02 art and animation behavior after rescue.

## Source order

| Index | Source filename | Chamber | Inward destination / upward destination |
| --- | --- | --- | --- |
| 0 | WhatsApp Image 2026-10-27 at 18.57.55 (6).jpeg | Sanctuary of Artemis | Lower-left golden arch / upper-right round temple |
| 1 | WhatsApp Image 2026-10-27 at 18.57.55 (5).jpeg | Gardens of the Upper Light | Left golden arch / upper-right colonnade path |
| 2 | WhatsApp Image 2026-10-27 at 18.57.55 (1).jpeg | Asphodel's Last Sun | Upper-right glowing arch / lower-left garden approach |
| 3 | WhatsApp Image 2026-10-27 at 18.57.54.jpeg | Court of Withered Cypresses | Left ruined arch / lower-right approach |
| 4 | WhatsApp Image 2026-10-27 at 18.57.55 (2).jpeg | Broken Causeways | Upper-left lava arch / lower-right bridge arch |
| 5 | WhatsApp Image 2026-10-27 at 18.57.55 (3).jpeg | Falls of Phlegethon | Left ruined arch / lower-right arch |
| 6 | WhatsApp Image 2026-10-27 at 18.57.55.jpeg | Titan's Rest | Lower-right arch / left arrival arch |
| 7 | WhatsApp Image 2026-10-27 at 18.57.55 (4).jpeg | Throne of the Below | Carpet before throne for rescue / lower-left golden arch |

| Resource | Source/destination SHA-256 |
| --- | --- |
| Chamber_00.jpeg | `85bb9401d5a1fdd857543a271863a02354d1885987a084eb4e1094d14a0aca88` |
| Chamber_01.jpeg | `2811d581f2c91a917a2fdb3ffa94b171dcbd9e5b2356388b39b9f1e9c10ace5c` |
| Chamber_02.jpeg | `fe217946f5c5df2a4adc3775be3c640b2eb4060c0ee96a1d45640ecd7dde4824` |
| Chamber_03.jpeg | `e534266e956b696b4c3ac06e256670be1c9dc416f6fa78e9995ffaf0faeb330f` |
| Chamber_04.jpeg | `de4cc1cd3702383d648c718d940f490aba0a0fa2dc69894e23991251a26f7abd` |
| Chamber_05.jpeg | `60e5ad88ed76ab2a16bfb915f532f211ca6744fe98baaffb929ed1ef386024be` |
| Chamber_06.jpeg | `e0405c44a071e4203b63d210e4aa0c6421ceeef735780da19ed32ce5eac82681` |
| Chamber_07.jpeg | `082e35d4db2dde77b7a45e6da138b7c54600d54d67f07969223eb1d31ae6a148` |

`DescentChamberCatalog.Author` is the source of authored floor, passage, spawn, encounter, well, cover, silhouette, and stair data. The scene builder imports all eight images, authors all eight layouts/scenes, and places the eight scenes first in build order. `UpdateStairZones` remains available to the character asset builder.

Coordinates use the original 1536 × 1024 painting. The 72 × 48 image dimensions, 36-degree inverse ground projection, zoom 11, and camera crop clamp remain in use. Each floor follows the painted terraces and paths. Cover uses solid convex footprints and original-painting alpha silhouettes; the fallen titan is split into arm, torso, leg, and rubble shapes to preserve the fighting floor in front. Visible stair runs have separate polygons and an uphill direction.

`entrance` is the safe descent arrival beside the upward passage (`returnExit`). `returnSpawn` is the safe ascent arrival beside the inward passage (`exit`). Authored arrival-to-passage separation is 4.45–9.88 ground units. In the throne room `exit` and `reunionPoint` sit on the carpet in front of the solid throne. The throne does not provide a ninth room.

Before scene generation, an independent numeric audit triangulated every floor, cover footprint, and silhouette; checked original-image bounds and convex collision footprints; checked 0.65 ground-unit clearance for both passages/arrivals and 0.8 for wells, encounter centers, and enemy positions; and connected every authored point through a four-neighbor, 16-pixel floor grid with 0.55 ground-unit clearance. All eight authored catalogs passed this audit. Source images and diagnostic collision overlays were visually inspected for all eight rooms.

## Verification

Unity 6000.3.10f1 built all eight saved scenes successfully. The final EditMode run (`Logs/journey-final-editmode.xml`) reports **67 tests, 63 passed, 4 failed**. New journey, Artemis, map serialization, collision, projectile-cover, camera and escort return-path checks passed. The escort check drives the existing breadcrumb follower along a clearance-tested path in every chamber, using the actual solid colliders, and confirms she stays on the floor and reaches passage range. Four failures match the earlier `eurydice-merge-editmode.xml` baseline: three extended Asphodel tutorial expectations and `OffBeatOrdinaryDamageCannotDefeatGuardian`. These existing failures are not changed by this revision.

`JourneyCourseVerification.RunBatch` exercised the real scene/trigger route through all fifteen visits and reported **PASS with zero runtime errors**. It checks the solo title/start, Artemis's opening, tutorial persistence, deepest rescue, companion creation, return passage swap, waiting for a distant escort, ascent guardian suppression, health/cadence continuity, surface victory and both restart actions. It deliberately clears encounters through the director and places actors at gates; it verifies route integration rather than a complete combat play-through. The separate EditMode escort test covers physical following around the new cover.

Rendered arena/passage captures for both legs, the opening Artemis dialogue, rescue, victory and all eight full-map overlays are in `Logs/JourneyCourse/` (local verification output, not source assets). A focused independent code review found no important route regressions. Source and documentation whitespace checks passed; Unity-generated YAML retains the editor's own formatting.
