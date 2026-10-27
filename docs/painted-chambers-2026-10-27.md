# Authored painted chambers

## Requested behavior

Use only the latest images in `D:/Serban/TEST`. The current descent remains five chambers; the five newest files are copied without image edits into `Resources/PaintedChambers/Chamber_00.jpeg` through `Chamber_04.jpeg`.

| Index | Source filename | Environment |
| --- | --- | --- |
| 0 | WhatsApp Image 2026-10-27 at 12.23.29 (2).jpeg | Pale viaduct with upper-right arch |
| 1 | WhatsApp Image 2026-10-27 at 12.23.29 (1).jpeg | Ruined court with upper-right doorway |
| 2 | WhatsApp Image 2026-10-27 at 12.23.29.jpeg | Burial terraces, ascending toward the rear |
| 3 | WhatsApp Image 2026-10-27 at 12.23.28 (1).jpeg | Ceramic vault with glowing upper-right arch |
| 4 | WhatsApp Image 2026-10-27 at 12.23.28.jpeg | Lava arena with upper-left stairway and gate |

## Implementation contract

Each image has an authored layout asset in source image coordinates. Ground coordinates are obtained by inverse-projecting those coordinates through the existing 36-degree isometric camera. The same mapping places walkable boundary walls, solid cover footprints, foreground occlusion shapes, spawn points, healing wells and exit triggers. This replaces the original first chamber's approximate bridge hitboxes too.

Camera zoom and a rectangular image clamp keep the image edges outside the viewport while allowing the player to explore a large arena. Static collision footprints prevent walking or dashing through ruins and falling outside the marked floor. Projectiles sweep their entire frame displacement and stop at the first solid obstacle. Foreground cutouts use the original painting and depth-sort against actor feet so actors can disappear behind cover and reappear in front.

Five real Unity scene assets provide editable layouts and standalone entry points. Exit triggers at the painted doorway load the next authored scene while carrying health and cadence unlocks forward. The first scene retains the title/tutorial flow. Restart and return to title return to the first chamber.

An editor and in-game collision overlay marks walkable floor in green, solid cover in red, and the exit in cyan. Debug markings are inspection aids and do not recolor the source paintings.

Foreground silhouettes use cropped sprites with alpha masks baked from the authored polygons. Their colors are copied from the original painting, with transparent pixels outside each silhouette; the source JPEGs remain unchanged. The cutouts sit at their footprint's front depth. Cutouts and actors write masked depth during exploration, so HDRP renders actors behind and in front correctly; guardian duels restore the original actor materials. Saved collision classifiers, exit components and sprite binders have their own matching script filenames so Unity scene serialization retains them.

Verification covers spawn/exit reachability, boundary and cover collision, fast projectile interception, camera crop limits at multiple aspect ratios, foreground depth ordering, scene transitions and run-state continuity. Screenshots and diagnostics go into `Logs/PaintedChambers/`.

The final EditMode run has **32 passed / 4 failed**. All painted-chamber checks pass. The four failures match the pre-change baseline: three existing tutorial sequencing tests and `OffBeatOrdinaryDamageCannotDefeatGuardian`. A rendered Play-mode pass verifies all five scenes, both sides of foreground cover, four real chamber transitions, health/cadence continuity, final-gate victory, fresh restart and return to title with zero runtime errors.
