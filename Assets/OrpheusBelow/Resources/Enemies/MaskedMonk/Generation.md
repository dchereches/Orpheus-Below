# Masked Monk broad oil overhaul — 2026-10-28

This current record supersedes the earlier generation notes below. Dedicated agent: `masked_monk_redesign`. Built-in `image_gen.imagegen` with `transparent_background=true`; selected PNG bytes are copied unchanged. No painting, filtering, recolouring, resizing, repacking or frame duplication was applied to the PNGs.

## Current approved assets

Identity reference: `D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (1).jpeg`, inspected with the existing Body/Effects before generation.
Approved medium-only reference: `C:/Users/asus/.codex/generated_images/01a17f9c-34b0-7b02-a22c-ade19a60d33d/exec-df35efbf-1680-47bd-a028-229a84ebb4fe.png` (Hooded Skeleton long connected cloak strokes).
The current Masked Monk atlas was inspected for motion semantics, but was deliberately not passed as a generation reference to avoid retaining fine surface detail.

- Body source: `C:/Users/asus/.codex/generated_images/01a17f9f-4502-7ba1-a96c-1057710404ae/exec-cd5c8d2f-4f05-4ff7-9f21-56805f95db6e.png`.
- Body PNG: 1086 × 1448 RGBA, 6 columns × 8 rows = 48 authored distinct poses.
- Body SHA-256: `edd6d1a48fb860847246ad126fc20e02be6eb1b06d0665cf296668b59073a351`.
- Effects source: `C:/Users/asus/.codex/generated_images/01a17f9f-4502-7ba1-a96c-1057710404ae/exec-4dfe1c6d-e8e0-4f68-b979-8f14cb1503da.png`.
- Effects PNG: 1536 × 1024 RGBA, 6 columns × 4 rows = 24 authored distinct frames.
- Effects SHA-256: `24baf900015830d715738d4a592b743b3728bcde05c5450c60b6f2643c39b860`.

Both outputs were self-viewed, then independently viewed and approved by the art director. Body approval: large connected long umber cloak strokes, round narrow-eyed mask, cheek ribbons, brass staff and gourd identity retained; readable raised summon and collapse progression. Effects approval: broad sage/ivory swirls with chunky stone/dust motion.

## Alpha and integration contract

Read-only source inspection measured body alpha 0–255, 566037 fully-clear pixels, 876611 pixels alpha <10 of 1572528 total. Effects alpha 0–254, 616782 fully-clear pixels, 1019153 pixels alpha <10 of 1572864 total. The viewer can expose coloured RGB stored beneath nearly transparent alpha; that RGB is not an opaque backdrop. PNG alpha is preserved unchanged.

`AtlasLayout.json` contains new source dimensions, hashes and motion counts, and requests `autoSegmentationRequired=true`. Visual intersprite gutters have variable spacing and low-alpha residual brush pixels. Do not reuse old Library sprite rectangles or assume nominal equal cuts are safe. The integration agent must alpha-segment and rebake the existing Library asset. Runtime scale (+20%) belongs to the main agent; no raster rescale was performed.

Body rows, top to bottom, six chronological poses each: Idle, Move, Windup, Attack (raised-arm summon), Attack2 (grand summon), Recover, Hurt, Defeated.
Effects rows, top to bottom, six chronological frames each: FallingBoulder, ImpactDebris, SummonEnergy, CircularShadow.
No duplicate stills were added to increase the counts. Runtime gameplay, timing and cancellation remain the integration agent's concern. No C#, Library.asset, Unity or Git operations were performed by this character agent.

## Exact current body prompt

```text
Use case: identity-preserve.
Asset type: transparent Unity 2D full-body character animation atlas.
Input image 1 is the Masked Monk IDENTITY reference only. Input image 2 is the approved MEDIUM reference only: copy its VERY LARGE continuous loaded-brush cloak sweeps and reduced detail, never copy its skeleton, gun or costume.
Redraw the same Masked Monk with MUCH LESS DETAIL and unmistakably THICK BROAD OIL PAINT BRUSH STREAKS. Preserve rounded gray-green mask, narrow dark horizontal eye slits, two pale sage cheek ribbons, ragged dark olive/umber layered robes, slim brass staff, little ochre belt gourd and small topknot. Mask only a few confident large planes and dark slits. Robe made of about 6–10 LONG continuous broad umber/olive paint swipes from shoulders to hem, bristle streaks following those long swipes. Each swipe wider than his hand. Large connected colour masses, chunky confident edges. Ribbons long simple pale brushstrokes. No tiny fold lines, polygon tiles, speckles, scratches, embroidery, fine hair, dense texture, pixel art, smooth airbrush or 3D render.
EXACT atlas layout: SIX columns by EIGHT rows, 48 distinct complete individually painted poses. Portrait canvas ideally1536x2048. Every frame same character scale, orthographic three-quarter front view slightly facing RIGHT. Feet anchored consistently. Fully transparent alpha background and transparent generous gutters; every full silhouette staff/ribbons/arms contained in inner75% of its equal cell. At least12% empty transparent margin each side; no character may touch neighbor. No labels, gridlines, scenery, ground plane, shadows outside figure, paper backdrop, borders or text.
Read row0 TOP to row7 BOTTOM and six poses LEFT to RIGHT:
0 Idle: subtle breathing, ribbons gently sway, staff grounded; six distinct phases.
1 Move: complete six-step shuffle cycle, alternating legs clearly different, gourd and ribbons follow.
2 Windup: gather inward then raise free hand and staff through six increasingly high anticipation poses.
3 Attack: progressively raise summoning arms overhead, ribbons lifting, staff held; six distinct stages.
4 Attack2: grand summon, powerful expansive two-arm upward sweep with robe follow-through; six distinct stages.
5 Recover: gradually lower staff and arms to idle, robe settles.
6 Hurt: sharp recoil and hunch then rebalance through six clear stages, mask retained.
7 Defeated: sequential buckle, kneel, sink, fall, crumple, final low slumped heap with staff resting alongside. Every pose fully inside its cell. No gore.
Genuine clean transparent empty pixels. Prioritize visible broad LONG paint streaks and exact 48 pose motion mapping.
```

## Exact current effects prompt

```text
Use case: stylized-concept.
Asset type: production Unity 2D transparent VFX sprite atlas for the Masked Monk.
Input image1 is IDENTITY/PALETTE only: muted olive stone, dark umber, pale sage magic and dull brass. Input image2 is the APPROVED BROAD OIL MEDIUM only: very large LONG continuous loaded-brush streaks and drastically reduced detail. Draw NO character or staff.
Create exactly SIX columns by FOUR rows =24 distinctly painted progressive effects on genuine transparent alpha. Landscape canvas1536x1024 preferred, equal square cells. Each whole effect contained within inner70% of its cell, with wide completely transparent gutters. All empty backdrop genuine alpha0. No text, gridlines, scenery, ground plane, checkerboard, border or labels.
THICK BROAD OIL BRUSH STREAKS: few huge connected colour masses, each boulder only3–5 broad olive/umber loaded-paint swipes. Each energy ribbon one long thick curved sage brushstroke. Dust and impact made from4–7 big confident paint swipes, NO dozens of tiny fragments, no speckles, no faceted polygon chips, no fine cracks or realistic stone texture, no glowing airbrush.
Rows top to bottom, each six poses left to right:
Row0 falling boulder: six different rotation/descent phases of large heavy olive/umber stone mass with a few pale sage curved brushribbons; full chunky boulder, no floor.
Row1 impact/debris: compact downward strike, expanding burst, broad chunky stone pieces in spread, peak dust swipes, breaking/fading dust swipes, last few remnants; exactly six distinct phases.
Row2 summon energy: small pale sage thick curl, curl unrolls, large double swirl, peak expanding swirl, broken two brushribbons, small fading curved swipe.
Row3 circular shadow: six distinct growing pulsing dark umber/olive horizontal oval ground-target marks with broad dull-brass circumference swipe, subtle change in ring thickness, no opaque background outside the painted target shape.
Isolated large readable painted effects only. Keep every effect cleanly away from neighboring cell.
```

## Previous generation record — superseded artwork

The notes below describe the prior 2026-10-27 fine-detail assets, retained only as provenance and the existing combat contract.

# Masked Monk painted animation

Reference: `D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (1).jpeg` (inspected before generation).

Generator: built-in `image_gen.imagegen`, referenced_image_paths set to reference above, transparent_background=true. Generated PNGs copied unchanged, no synthesized transforms or frame duplication.

## Body.png

6 columns x 8 rows = 48 painted body frames, row-major from top left. Rows: Idle, Move, Windup, Attack (raised-arm summon), Attack2 (grand summon), Recover, Hurt, Defeated. Generated dimensions: 1086 x 1448 (181 x 181 cells). Fixed foot-relative atlas framing, full body and staff. Preserve narrow-eyed gray-green rounded mask, pale ribbons, ragged robes, brass staff and gourd.

Generated source: `C:/Users/asus/.codex/generated_images/01a17ee3-0c14-7843-b517-d59ac7b998a7/exec-a9b182a1-6bfe-4e67-b614-72e7a2df95ca.png`.

Exact body prompt:

```text
Use case: stylized-concept
Asset type: production Unity game transparent sprite animation atlas.
Input image 1 is character and oil-painting style reference only. Create a NEW atlas of the reference Masked Monk: rounded weathered gray-green mask, narrow horizontal eye holes, two long pale sage ribbons flowing from cheek holes, dark ragged layered robes, brass staff, small ochre gourd at belt. Preserve this identity, chunky real oil brush texture and restrained gray/olive/brass palette.
EXACT LAYOUT: 6 equally sized columns by 8 equally sized rows, 48 full-body frames, row-major. Transparent background and transparent cell gutters, no labels, no grid lines, no scenery, no paper texture outside painted figure. Canvas ideally 1536 wide by 2048 high. Each cell 256 square. Fixed orthographic three-quarter front view facing slightly viewer-right, all feet anchored at same local point near cell bottom, same body scale, all limbs/staff/ribbons fully inside cell with 10% safe margins.
Animate 6 DISTINCT hand-painted progressive poses in EACH row, never six identical poses, never copies with mere translation/rotation/scaling:
Row 0 Idle: breathing and subtly floating ribbons, staff grounded.
Row 1 Move: six-step shuffling walk cycle, alternating feet, robes and gourd swing.
Row 2 Windup: gather torso inward then draw free hand and staff upward.
Row 3 Attack: summoning arms raised overhead in six progressive clear poses, ribbons lift with magic, staff held.
Row 4 Attack2: grand summon with pronounced two-arm upward sweep and expansive robe motion, six progressive poses.
Row 5 Recover: arms and staff settle down over six poses to idle.
Row 6 Hurt: clear recoil and bowed stance then partial rebound over six poses, retain mask.
Row 7 Defeated: progressively kneel and collapse, final low seated slumped figure, staff resting alongside, no gore.
Keep entire painted silhouette isolated with true alpha transparency. No ground shadows in body atlas. Equal regular frame cells are essential. No text, watermarks, panels, collage, concept sheet labels.
```

## Effects.png

6 columns x 4 rows = 24 painted effect frames. Rows: falling boulder, impact/debris, summon energy, circular shadow. Generated dimensions: 1536 x 1024 (256 x 256 cells).

Generated source: `C:/Users/asus/.codex/generated_images/01a17ee3-0c14-7843-b517-d59ac7b998a7/exec-d8a6aa4a-f40b-41e1-b1bc-03aeb6aa9db0.png`.

Validation: both PNGs are RGBA, body alpha spans 0..255 and effects alpha spans 0..254. Effects atlas corner pixels and sampled cell gutters have alpha 0; 948,719 of 1,572,864 pixels have alpha below 10. Both atlases inspected visually for distinct progressive poses/effects.

Independent frame validation found 48 unique body cells and 24 unique effects cells. Every body cell contains at least 41.7% transparent pixels; every effects cell contains at least 40.4% transparent pixels. No empty cells. Unity Editor regression tests are in `MaskedMonkPaintedTests.cs`; the equivalent direct PNG validation passed before handoff. PNG SHA-256 hashes match the built-in generator source files.

Exact effects prompt:

```text
undefined
```

## Attack timing and cancellation

The target position locks at cast start. Every impact has a circular warning for at least 1.35 seconds before its rock begins a 0.45 second descent. Alternating grand summons warn three targets together and stagger impacts by 0.35 seconds. Impact radius 2.3 units, damage 24, recovery 1.05 seconds. Every boulder resolves at most once. Cancellation disposes all warnings, rocks and owned effects and clears pending strikes, so stage transitions and stuns cannot leave delayed damage.


## Integration source-region correction — 2026-10-28

Body playback now uses measured `AtlasLayout.json` body.frames rather than global grid cuts. PNG bytes remain unchanged. SHA256: `edd6d1a48fb860847246ad126fc20e02be6eb1b06d0665cf296668b59073a351`; source dimensions 1086×1448.

Actual motion counts (Idle, Move, Windup, Attack, Attack2, Recover, Hurt, Defeated): `[6, 6, 6, 4, 6, 6, 6, 6]` (46 distinct poses). Each region retains every alpha>100 pixel of its own connected main silhouette; no meaningful detached component of 20–1999 pixels exists. Up to two source pixels of soft-edge padding are added when no extra foreign main pixels enter. Foot pivots use the median opaque X of the lowest 12% of the main figure and its measured lowest Y.

Omissions and source-column mapping are recorded in `body.omittedSourcePoses` and each retained frame's `sourceColumn`. 2 omission entries; no duplicate poses were created. All retained rectangles contain zero alpha>100 pixels from another main silhouette.
