# Eurydice — original committed sprite oil-surface finish

## Current implementation — original raster assets remain immutable

ALL generated raster redesigns, corrective sets and both strict paintover attempts were rejected and are not installed. The selected approach is a code-native RGB-only runtime finish on the five original HEAD PNGs, whose byte hashes remain unchanged. No raster API fallback or local pixel modification is used.

The five immutable HEAD/e0d1adc source hashes are:

| Original PNG | SHA-256 |
| --- | --- |
| Idle | 565f9e25dab6d31ae4dabf30dfffa5b4baaa46520353a21b5a2edde81b4a44d4 |
| Walk | 00e1bc26ba7ff686f7663dd2a85b9478d9768b1c6c2f7801018cb5d6990f58ee |
| Reactions | 70675a89aae5d757ff5e7b1bbec64531b63d1d3670f35a7f72b415ece3340491 |
| Stairs | f8c40ec440cb717b15008db97e72a60839cdd9b061983f745fc274604a05e361 |
| Effects | 4b59d168f51702678db68e41ea6ae35b950124c11b664400c210e770e3acd1be |

Rejected-only new metadata and its Unity .meta files were verified untracked and moved out of runtime Resources to ignored Logs/ArtReview/RejectedEurydice/: AtlasLayout.json, Validation.json, CorrectiveGeneration.md, CorrectiveValidation.json, Generation.md (plus each .meta). These are historical attempt records, never current source mappings. SurfaceGeneration.md is the sole authoritative new runtime art record; original PNG/Library baseline files and their metadata were not moved. Original private reference JPEGs were never copied into Resources.

Runtime files are Resources/Art/EurydiceOilSurface.shader and Scripts/Companion/EurydiceOilSurface.cs. The helper only selects a cached shared material and updates a preserved MaterialPropertyBlock with the exact normalized current sprite rectangle; it never replaces a sprite or changes texture, vertices, pivot, pixels-per-unit or Transform. UnitySprites.cginc/SpriteVert retain the standard SpriteRenderer-requested flips and material/renderer/vertex tints. There is no additional vertex deformation or UV warping.

The shader combines eight long directional bilateral RGB taps clamped to the current sprite rectangle with a half-texel inset, retaining strong colour edges between hair/skin/cloth. The top 25% of local sprite height uses source RGB exactly; warm skin strongly reduces the finish below that region. Long UV-anchored bristle swipes are deterministic and use no time noise. Alpha is the original unwarped centre texture sample multiplied only by normal renderer/material/vertex tint, never an averaged or generated alpha. The centre source sample intentionally preserves standard sprite sampling; all offset colour-filter taps are frame-clamped. Root owns Unity compilation, contract tests and actual graphics review; the worker has not run Unity or claimed graphical approval.

Root verification completed: all seven source/geometry/frame-UV contract tests passed. The stronger RGB finish uses strength 0.92, bilateral coefficient 12 and longer directional swipes; all alpha/geometry/face constraints remain unchanged. The final Unity direction/emotion/stair/shatter/pause graphics harness passed with zero game runtime errors (`Logs/eurydice-original-surface-verified.log`). The independent art director approved both the focused original/finished pair and representative animation captures. The actual 15-visit encounter journey also passed (`Logs/broad-oil-journey-verified.log`).

Historical attempts below document why pixel replacement was abandoned, not approved artwork.

Mandate: keep last-commit HEAD/e0d1adc geometry and identity exactly, simplifying only surface rendering into long broad oil marks. The user rejected both prior re-concept sets as too bulky. No rejected outputs are identity references or installed assets. Do not introduce proportions from photos or new concepts. HEAD original image geometry overrides verbal descriptions. Root confirmed original Idle is long slim draped gown, not a knee hem.

## Source and candidate 1

Sole edit target: `Assets/OrpheusBelow/Resources/Characters/Eurydice/Look02/Idle.png`, original restored HEAD, 887×1774, SHA-256 `565f9e25dab6d31ae4dabf30dfffa5b4baaa46520353a21b5a2edde81b4a44d4`.
Built-in `image_gen.imagegen`, `transparent_background=true`; unchanged generated output `C:/Users/asus/.codex/generated_images/01a17f97-58e7-7953-8a90-2c716197b68b/exec-fbc970e1-0a91-4b32-b1aa-617bb398b06b.png`, 887×1774 RGBA alpha, SHA-256 `70446e7d9611432c9326c98d391ed6f39061c9ab88ffff88e91aabd09416eaeb`.

Worker viewed source and output natively, prepared charcoal inspection previews `Logs/ArtReview/Eurydice-surface-Idle-HEAD-charcoal.png` and `Eurydice-surface-Idle-paintover1-charcoal.png`. Quantitative per-regular-cell alpha>=80 bounds comparison in `Logs/ArtReview/Eurydice-surface-Idle1-comparison.json`. Canvas/layout/poses/directions preserved visually, surface folds simplified. **No claim of exact geometry:** generated alpha bounds width/height changed +5.1% to +13.5% vs source (not installed). Main/root review pending; other four sheets not started.

Exact prompt:
```text
Use case: style-transfer, STRICT SURFACE-ONLY PAINTOVER of the supplied original PNG.
Treat the image as the fixed final drawing and animation layout. Keep its canvas, every pose, every direction, every face, every frame position and every transparent silhouette boundary EXACTLY where they are. Do not redesign, regenerate or reinterpret the character.
DO NOT WIDEN TORSO OR LIMBS. DO NOT CHANGE PROPORTIONS OR POSE AT ALL. Trace original shoulders/arms/waist/hips/legs/head/body-ratio/face/hair/garment hem 1:1. Do not add/remove fabric, anatomy, jewelry or hair. No physique/age/face change. Keep all 32 original drawings in the original4column8row arrangement, their exact scale and locations, original canvas887×1774. Preserve every existing direction as drawn; no new direction correction.
ONLY simplify the EXISTING SURFACE RENDERING: merge tiny cloth fold highlights into fewer LONG CONTINUOUS loaded flat oil-brush streaks that follow the existing folds, staying INSIDE the original silhouette. Merge small hair strands into long connected red paint strokes INSIDE the original hair outline. Reduce tiny texture/noise on fabric/hair/skin without smoothing into airbrush. Clearly visible wide oil-brush marks with directional bristle streaks, no small polygonchip mosaic. Retain original face likeness, colors, all costume details and narrow original anatomy exactly; face changes only remove excessive texture, not feature positions/shapes.
Original drawing and alpha outline are absolute constraints. This is like painting broader strokes ON TOP of an already finished drawing without moving a single contour. Keep genuine transparent background, no new halo, shadow, particles, floor, text or labels. ONLY MEDIUM/DETAIL CHANGES, ZERO GEOMETRY/IDENTITY/POSE CHANGES.
```

## Candidate 2 — strict interior-only edit, geometry threshold still exceeded

Main/root rejected candidate1 because face/shoulder/arm widening remained visible and requested one stricter attempt directly from HEAD. Candidate2 again uses the exact original HEAD Idle as SOLE edit target, no previous generation or photo/concept references. Generated unchanged source `C:/Users/asus/.codex/generated_images/01a17f97-58e7-7953-8a90-2c716197b68b/exec-4d4c0f2f-9140-44b5-8e33-0e3f7481593c.png`, 887×1774 RGBA, SHA-256 `3cd868c89cd4c10fd7f1471bfa147de3d3ac90ef89e82cb10f5ac22bbd03be08`.

Worker viewed raw and charcoal output. Source/output canvas remains887×1774 and grid/poses maintained visually. **Not installed:** regular-cell alpha>=80 bounds width/height ratios still changed +2.89% to +13.40%, exceeding root's ±3–5% target. Built-in edit did not honor an exact locked outline. Source-aware visual/landmark review remains with main/root; no exact geometry success claimed. Other four surface paintovers have not started. HEAD production images remain unchanged.

Inspection preview `Logs/ArtReview/Eurydice-surface-Idle-paintover2-charcoal.png`; comparison `Logs/ArtReview/Eurydice-surface-Idle2-comparison.json`.

Exact prompt:
```text
STRICT INTERIOR-ONLY OIL PAINT FINISH EDIT of this exact PNG. Canvas MUST remain887×1774. Keep all original pixels defining silhouettes and body/face landmarks locked in place1:1. Keep the entire outer contour and a thin band just inside it untouched. No outline movement or expansion anywhere, including hair and hem.
Do not redraw, enlarge or widen ANY bodypart or head. Keep original narrow face, thin shoulders/arms/waist/legs, original head/body ratio and all face features exactly. Keep every original pose, direction, shape, hair outline, garment outline, jewelry, all32figurepositions and original4column8rowlayout. No repositioning or scaling. No additional anatomy/design instructions: source image itself is the absolute geometry reference.
Change ONLY SURFACE DETAILS INSIDE the already-existing forms: soften and merge tiny cloth highlights/fold texture into fewer long continuous broad oil-brush marks FOLLOWING THE EXISTING folds, and merge tiny hair texture into longer oil streaks. Keep original colors and all shadow/feature locations. Let strokes stop well before silhouette edges. Keep face/skin rendering nearly untouched so likeness/width cannot change. No new polygons/noise, no airbrush. Paint INSIDE original forms rather than repainting original figure.
Output genuine transparentbackground and unchanged outside silhouettes. No shadow/text/grid/background/particles. ZERO GEOMETRY CHANGES. Exact original alpha outline, exact original proportions, only simpler oil paint surface finish.
```
