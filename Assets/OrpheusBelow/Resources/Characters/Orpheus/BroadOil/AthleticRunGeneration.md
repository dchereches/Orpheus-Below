# Athletic run: established paint and full-body posing

Updated 2026-10-29. The five `Athletic*.png` files are now **lossless extracts of the existing `WalkCycle.png` atlas**, not newly generated illustrations. This keeps Orpheus's existing face, short tousled hair, long charcoal tattered coat, muted red lining, linen sleeves, simple gold lyre and broad brushwork unchanged between walking and running.

No image-generation prompt was used for this revision. The former generated short burgundy cape and dense curly hair paintings have been replaced. Their generation prompts are no longer instructions for the current assets.

## Reproducible extraction

`OrpheusBroadOilAssetBuilder.Build` first extracts the canonical paints directly from `WalkCycle.png`, using `sourceX`, `sourceY`, `sourceWidth` and `sourceHeight` in `AthleticRunLayout.json`. Coordinates use a top-left origin in the original 1024 x 1536 atlas. The alpha and RGB channels are copied without recoloring, repainting, keying or rescaling.

| Canonical paint | Atlas rectangle x, y, width, height |
|---|---|
| South | 0, 0, 256, 294 |
| SouthEast | 0, 294, 256, 288 |
| East | 0, 582, 256, 298 |
| NorthEast | 0, 880, 256, 293 |
| North | 0, 1173, 256, 363 |

The unequal row cuts follow the empty gutters between the actual figures. All five extracts were compared with the corresponding original atlas pixels: zero differing RGBA pixels. Native joint samples were also checked to land on opaque character paint.

## Athletic posing

The canonical paintings supply painted anatomy and costume; their walking pose does not supply the running motion. The existing sixteen-phase full-body run skeleton still drives both legs, both bent counter-swinging arms, an upright pelvis, torso pitch, flight and cape sway. Native anatomical landmarks and extraction masks have been reauthored for the established art in `AthleticRunLayout.json`. Hip-to-ankle height is fitted by `OrpheusRunMotion.FitLegLengths` before posing, preserving an upright runner even when the source leg is bent.

Western views mirror the matching eastern paint. The long coat is retained as the torso/cape layer while the two isolated leg and arm layers follow the athletic gait.

Retained cloth backing closes the coat where the original sleeves or hanging wrists covered it. The backing samples opaque coat paint from the same unchanged canonical PNG; it never retains original skin or hand paint. Explicit source-hand terminal cuts and a body-only island pass remove original fingertip remnants before the coat silhouette is measured. Backing replaces only actual arm/hand cuts, never unrelated leg cuts inside the panel bounds. Below the upper sleeves, it is clipped to cuts with retained coat paint on both sides. A final connectivity pass removes disconnected backing.

## Regression checks

`OrpheusRunCycleTests.RunningPaintIsAnExactExtractOfTheEstablishedWalkingCharacter` compares every canonical RGBA pixel against the established atlas. `EveryRunningFrameUsesOnlyTheMatchingEstablishedCharacterPaint` verifies every direction and phase binds its corresponding canonical texture. Existing rendered-boot, counter-arm, torso-flight, support, start/turn, stair and runtime-culling tests continue to exercise the posed result.

The backing tests check opaque donor paint and retained geometry in every phase, including ten specific native locations where rendered review exposed gaps behind elbows and beneath old wrists.

Twelve additional native terminal locations must have no original UV binding in any phase, preventing old fingertip paint from reconnecting as stationary appendages. A standalone native-alpha audit also checked all five views for retained source-hand body cells and unrelated leg cells used as backing; both counts were zero in every view.
