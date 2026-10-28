# Rooster Rider broad-streak overhaul — 2026-10-28

Generated with the built-in image_gen tool, transparent_background=true. Dedicated character worker: rooster_rider_redesign. Original identity and current runtime atlases were inspected before generation. The approved Cerberus image was referenced solely for long continuous loaded-brush medium; its character design was not borrowed.

## Current production assets

- Identity reference: D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (4).jpeg.
- Medium-only reference: C:/Users/asus/.codex/generated_images/01a17f97-6cdb-7291-b318-3bdb0835e4ca/exec-e2c9561e-9106-4d99-b43e-aeeb01c076ef.png.
- Body generated source: C:/Users/asus/.codex/generated_images/01a17fb2-6d02-75f3-845e-39411182ddc5/exec-30638bcd-d6d9-4a5d-ac20-98c16b1606bf.png.
- Effects generated source: C:/Users/asus/.codex/generated_images/01a17fb2-6d02-75f3-845e-39411182ddc5/exec-0ea19f41-0e39-4cf7-8a07-7a9c4da2a86f.png.
- Body.png: 887×1774 RGBA, 4 columns × 8 rows, 32 individually generated poses. Rows top-to-bottom: Idle, Move, Windup, Attack dash, Attack2 grab/pull, Recover, Hurt, Defeated. Every row now has four usable distinct poses; the former two-frame grab exclusion belongs to the archived previous source only.
- Effects.png: 1536×1024 RGBA, 6 columns × 4 rows, 24 individually generated frames. Rows: dash trail, soul tether, feather impact, ground dust.
- Body SHA-256: 6E8CE0534FB9B82048E8C463E595ED5C3149B05E7A8994416D6507C0441D9978.
- Effects SHA-256: 56A7CAD45568AE5D956391680C4B56AC5720936AFE76136324470301B1203A93.

Both selected generated PNGs were copied byte-for-byte unchanged into production. No painting, filtering, resizing, recolouring, repacking, alpha replacement, mirrored/shifted duplicates or synthetic frames were used.

## Visual and alpha review

The worker viewed the original identity reference, old body/FX atlas, approved medium reference and both generated outputs. The director independently viewed and approved body against the original identity source, then approved the FX charcoal composite. Body retains the balding bespectacled bare-chested rider/black shorts and red-combed ochre/ivory/black-teal rooster. Large connected ivory wing strokes and long teal tail swipes replace fine feather chatter. FX uses large continuous connected loaded-brush ribbons and dust curls.

Read-only analysis found 32 separate substantial body silhouettes at alpha >100 (each component >500 pixels), including all four grab poses. All 32 nominal body regions and all 24 nominal FX regions are nonempty and have distinct pixel hashes. Body alpha range is 0–255 with 64.072% of pixels below10; effects alpha is 0–254 with 70.105% below10. Transparent source RGB can appear coloured in an alpha-ignoring viewer; the proper charcoal composite is clean.

Inspection derivatives only:
- Logs/ArtReview/RoosterRider-body-native-inspection.png.
- Logs/ArtReview/RoosterRider-effects-composited-inspection.png.

## Slicing and integration

New AtlasLayout.json removes all previous manual frame rectangles, old exclusions and two-frame grab count. autoSegmentationRequired=true. Generated placement varies from nominal cells; alpha segmentation must preserve complete rider/rooster silhouettes. The dash-row first/second components have a narrow real gap (first bounds x12–240, second x241–458), so equal-grid cuts are unsafe. All other components remain separated. Use unchanged common pixels-per-unit across rows, with world 20% size handled by the main bake. Library.asset and shared code were intentionally not modified. Unity/runtime verification belongs to the main integration agent.

## Exact final body prompt

```text
Use case: identity-preserve.
Asset type: transparent production Unity 2D character animation atlas.
Input1 is the EXACT ROOSTER RIDER IDENTITY reference: balding middle-aged man with round spectacles, bare chest, lean body, black shorts and bare feet crouched atop the rooster. Rooster has a red comb and wattle, ochre-gold neck, large ivory wing mass, black belly and long curved black-teal tail. Preserve these identities and proportions.
Input2 is PAINT MEDIUM ONLY: match the long continuous broad loaded-brush strokes across Cerberus's torso/limbs/tail, not its animal design. VERY MUCH LESS DETAIL than input1. Treat rooster wing as 3–4 huge continuous ivory swipes, tail as 3 long curved teal-black strokes, neck as 2 ochre swipes. Skin is a few warm broad planes, no tiny muscle striations, no hair strand or feather-by-feather texture. Thick flat oil brush with visible directional bristle streaks, confident continuous swathes rather than polygon chips or speckled texture.
EXACT LAYOUT: FOUR COLUMNS and EIGHT ROWS, exactly32 distinct complete rider+rooster poses, portrait canvas 1:2. Invisible uniform square cells. Genuine transparent alpha in all background and huge clear gutters. All complete silhouettes including arms, tail, claws and rider head occupy central65% cell width and central70% cell height with at least15% transparent moat on all sides. No two characters touch. Same full-body scale across atlas, right-facing side-three-quarter game view, stable rooster foot baseline at85% of each cell. Make any grab arm compact/bent so it remains wholly inside its own cell. No effects outside body, no groundshadow.
Each row has FOUR sequential genuinely different poses left-to-right, no duplicated stills.
Row0 Idle: rider breathing/crouch shifts and rooster head/tail bob subtly.
Row1 Move: four clear running gait phases, alternating rooster legs and rider balancing.
Row2 Windup: four stages rider crouches lower and rooster coils for dash, wings compact.
Row3 Attack: forward dash four cycling stretched running strides, rider leans low and holds reins.
Row4 Attack2 Grab: rider reaches a SHORT bent arm to right, closes hand on invisible tether, draws elbow back while rooster retreats one step, finishes pulling near torso; keep all arms in own cell, do not draw tether or victim.
Row5 Recover: braking rooster legs, briefly raised wing, rider rises then resettles crouch over4poses.
Row6 Hurt: four clear recoils, rider and rooster flinch and regain balance.
Row7 Defeated: four successive stages rider slumps forward, rooster knees fold, both tumble low, final rider and rooster lying on side together, all limbs/head/tail complete inside cell.
No scenery, paper, solid backdrop, labels, numbers, text, borders, grid lines, watermarks, extra characters. Preserve full32 poses and LARGE EMPTY transparent gaps. Broad oil strokes should be obvious at small game scale.
```

## Exact final effects prompt

```text
Use case: stylized-concept.
Asset type: genuine transparent production Unity 2D VFX animation atlas for the Rooster Rider.
Input1 is CHARACTER PALETTE ONLY: ochre gold, ivory, black teal, red-comb accents. No person and no whole rooster in output. Input2 is PAINT MEDIUM ONLY: large connected continuous loaded-brush sweeps, visible broad directional bristle streaks. Much less detail, NO individual detailed feathers, speckle, tiny chips, pixelation or smooth CGI.
EXACT SIX COLUMNS and FOUR ROWS,24 genuinely distinct sequential effect frames, landscape3:2 canvas, equal invisible square cells. Every effect isolated within central65% cell width/height with at least15% genuinely transparent gutters; no adjacent effect touching. All unused background actual alpha0, no opaque backdrop, no ground plane, no labels/text/grid/border.
Each effect should use just3–6 HUGE continuous oil-paint swipes. Row0 SIX progressive dash trails: a long horizontal right-flowing black teal and ochre sweep curls at trailing left side, grows, extends, splits into2 thick ribbons, thins, fades. Broad feather-like SWATHES, not individual feathers.
Row1 SIX progressive grabbing soul tether: ivory-gold and muted crimson thick oil cord arcs tightens horizontally, coils, stretches, pulls taut, breaks into2 swipes, fades; no body/victim.
Row2 SIX progressive feather impact: few broad ivory/ochre/black wedge swipes expand in a burst, spread outward, fall, separate, fade. No miniature feather vanes.
Row3 SIX progressive ground dust: broad ochre and umber loaded-brush curl grows from seed, expands, swirls, breaks into3 large wisps, thins, fades; no pebbles or fine dust particles.
Production alpha transparency, clean broad edges matching the approved oil character medium. No artificial glowing gradients or geometric placeholder icons.
```

---

## Archived previous source and combat contract


# Rooster rider painted enemy

Generated with the built-in `image_gen.imagegen` tool on 2026-10-27; transparent-background mode, using the supplied reference. No CLI/API fallback. The PNG files are byte-for-byte copies of generated outputs; no resizing, recoloring, masking, compositing or alpha replacement was applied.

Reference: `D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (4).jpeg`.

Final body source: `C:/Users/asus/.codex/generated_images/01a17ef8-2c5a-7f03-aa76-00edc95d0595/exec-87cee98a-dbcb-48aa-820f-ce54aea6be1b.png`.
Final effects source: `C:/Users/asus/.codex/generated_images/01a17ef8-2c5a-7f03-aa76-00edc95d0595/exec-614c130d-627d-41c7-ba80-5192777f0d5b.png`.

## Usable frames and layout

Body is 887 x 1774, four columns by eight rows. Thirty distinct frames are usable: Idle 4, Move 4, Windup 4, Attack 4, Attack2 2, Recover 4, Hurt 4, Defeated 4. The two first source grab poses have crossing rectangular bounds after the single layout-repair attempt and are deliberately excluded. The two clean grab poses, source columns 2 and 3, map to output columns 0 and 1. The first clean grab rectangle starts at x=463 to exclude the preceding pose's extended arm. `AtlasLayout.json` supplies exact Unity rectangles and pivots, including the variable motion counts. No repeated stills were added to reach a target number.

Effects are 1536 x 1024, six columns by four rows, 24 successive frames: dash tail, soul tether, feather impact, ground dust. The generated alpha is preserved; 68.98% of effect pixels have alpha below 10. Some long trailing FX wisps meet their nominal cell edges; the centered primary effects remain readable. Body has true alpha 0–255; effects alpha 0–253.

Body SHA-256: `a23920981abba2ae859754e98d93ed30acee881d32c80e1e095722d22ec49772`.
Effects SHA-256: `63943369a4cb1a96a29d84d9677df57a31dbc3ac1ee2fd369a0f2c5b838c88ee`.

Asset verification passed: all 30 declared body cuts and 24 effects are distinct and nonempty, source hashes match, every declared cut contains transparent space. Minimum alpha<10 area is 60.7% for body and 47.2% for effects. Minimum alpha>100 paint area is 11,621 body pixels and 2,684 effect pixels. Runtime and C# tests await centralized Unity execution.

## Body initial prompt

Use case: stylized-concept. Asset type: production 2D game sprite atlas with real successive animation frames. Reference image is SUBJECT AND STYLE reference. Preserve the exact funny balding bespectacled lean muscular bare-chested middle-aged human crouching barefoot atop his rooster, black shorts, round thin glasses; rooster has red comb/wattle, golden neck, cream-white wings, black belly and long iridescent black teal tail. Broad expressive oil brush strokes, warm rich ochre highlights, same appearance as reference. Output ONLY a transparent sprite sheet in a STRICT UNIFORM 4 COLUMN BY 8 ROW grid, 32 isolated whole-character poses. Portrait canvas, each cell square. Every cell contains exactly one COMPLETE human PLUS rooster, all feet, tail and head within cell with transparent gutter at least 8 percent. All face RIGHT in side view; same subject size and stable foot baseline at 90 percent cell height, no camera changes. Four unique sequential frames per row. Row0 idle breathing and tail bob, row1 rooster running full stride cycle, row2 crouched anticipatory dash windup, row3 fast forward dash rider leaning low rooster stretched with cycling legs, row4 rider reaches right and grabs an INVISIBLE tether then pulls while rooster retreats, row5 braking then upright recovery, row6 hit recoil and ruffled wings, row7 defeat rider and rooster tumble low then settle lying on side, both remain complete visible. Real pose changes not duplicates. No extra persons, no scenery, no ground planes, no text, no lines, no numbers, no frames/borders. Genuine transparent background with clean alpha between every sprite. Prioritize preserving the reference silhouette and rendering over extra detail.

## Body single layout-repair prompt

Use case: precise-object-edit. Repair ONLY atlas layout of the first image. Preserve all 32 distinct rooster rider poses and the reference identity/style from image2 (balding glasses bare chest black shorts on gold white black red-comb rooster, broad oil paint). Keep EXACT STRICT4 columns8 rows uniform square cells. CRITICAL: make EVERY complete human+rooster pose MUCH SMALLER within its own cell: subject width must be no more than 70 percent of cell width and height no more than 72 percent cell height. All tails toes arms wings heads fully visible inside their own cell with generous at least 14 percent transparent gutter on all four edges. No sprite may touch another sprite or reach a cell boundary. Especially row4 extended grabbing arms: shorten reach within their own cell, huge transparent gaps. Row7 all collapsed bodies fully contained. Same size reference for every pose; feet baseline 85 percent cell height, center x50 percent. Exact row meaning remains0idle1run2windup3dash4grab5recover6hurt7defeated. True alpha transparency, no colored halo, no scenery, no text, no grid. Do not increase frame count; precisely32complete characters. Prioritize clean isolated poses.

## Effects prompt

Use case: stylized-concept. Asset type: isolated oil-painted 2D game effects sprite atlas. Reference gives brushwork and rooster feather palette only; do NOT include human or whole rooster. EXACT STRICT UNIFORM 6 COLUMNS BY 4 ROWS rectangular grid,24distinct effect frames,reading left to right. Every effect fully contained within central65percent of its own square cell; generous15percent true transparent gutters, no overlapping cells. Row0 six successive forward dash trails, curling black teal tail-feather wisps and golden brushstrokes flowing horizontally right. Row1 six successive magical grabbing soul tether curls, slender pale silver-gold and muted crimson cords tightening then fading, horizontal. Row2 six successive impact feathers puff expanding then dispersing,cream white golden black small loose feathers, no body. Row3 six successive ground dust steps, ochre soft paint dust puff expands then dissipates. Maintain reference expressive broad oil brush style, warm natural palette. Actual animated progression every frame not duplicate. No text no labels no grid lines no borders no scenery no ground background no shadows outside effect. Genuine transparent alpha background. Rows stay separate.

## Attack and companion integration

The pattern operates only on the escort ascent with a living, unshielded Eurydice and valid healthy owner shade. A 0.65-second stationary warning locks the 6.5-unit dash direction and warns its capture corridor, clipped against solid cover. The rooster then dashes at 13 units/second for up to 0.5 seconds. A single registration occurs only within 1.38 units and with no intervening cover. Misses recover for 0.9 seconds. A successful grab loops Attack2 while retreating at 1.55 units/second toward `PaintedChamberMap.Active.ToWorld(definition.exit)` through the actor's collision-safe movement.

`IsDragging`, `DragTarget` and `RetreatDirection` expose the companion coordination hook. Eurydice must use the rider's current `RetreatDirection` with her own swept/map-checked movement, replacing her former fixed `-IsoUpXZ` drag. Existing abduction loss timing, sanctuary clearing, and damage/stun interruption remain with their existing owners. Separation beyond 1.9 units releases the grab. Cancellation unregisters the companion and removes all warnings and owned effects, direction and grab-attempt flags. Unity execution is performed centrally by the root worker.

## Integration source-region correction — 2026-10-28

Body playback now uses measured `AtlasLayout.json` body.frames rather than global grid cuts. PNG bytes remain unchanged. SHA256: `6e8ce0534fb9b82048e8c463e595ed5c3149b05e7a8994416d6507c0441d9978`; source dimensions 887×1774.

Actual motion counts (Idle, Move, Windup, Attack, Attack2, Recover, Hurt, Defeated): `[4, 4, 4, 4, 4, 4, 4, 4]` (32 distinct poses). Each region retains every alpha>100 pixel of its own connected main silhouette; no meaningful detached component of 20–1999 pixels exists. Up to two source pixels of soft-edge padding are added when no extra foreign main pixels enter. Foot pivots use the median opaque X of the lowest 12% of the main figure and its measured lowest Y.

Omissions and source-column mapping are recorded in `body.omittedSourcePoses` and each retained frame's `sourceColumn`. 0 omission entries; no duplicate poses were created. All retained rectangles contain zero alpha>100 pixels from another main silhouette.
