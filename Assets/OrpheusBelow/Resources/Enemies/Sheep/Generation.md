# Sheep broad oil-brush restyle — 2026-10-28

Built-in image_gen only. Selected material pass copied byte for byte from the native generated PNG. No production pixel filtering, repainting, resizing, cropping, repacking or duplicated frames. All image analysis was read-only alpha inspection.

Original identity reference: D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (6).jpeg.
Prior animation/identity input: pre-restyle Sheep/Body.png, source documented in the superseded record below.
Approved STYLE ONLY reference: C:/Users/asus/.codex/generated_images/01a17f97-6cdb-7291-b318-3bdb0835e4ca/exec-e2c9561e-9106-4d99-b43e-aeeb01c076ef.png. No wolf anatomy/palette imported.
Retained design: slender ivory sheep, long pointed mask face, gold ring eye, tall pointed ears, thin legs, long tail, large dark spherical bead collar and simple bronze pendants. Selected pass replaces short little cream facets with connected long loaded-brush sweeps and bristle streaks.

Selected Body.png: C:/Users/asus/.codex/generated_images/01a17fd6-bc4c-7fa2-9a00-fda8f743fa00/exec-0500f6d5-ea18-4115-8af7-8398744d6293.png.
Workspace destination: Assets/OrpheusBelow/Resources/Enemies/Sheep/Body.png.
887 x 1774 RGBA. SHA256 00e80e2c2ffdeaccf50988c2b0bd06cd1ceccdb34cdab5026de26be83dca2021.
Alpha 0–255; 843710 fully transparent pixels (53.6187%); 1053111 pixels alpha <10 (66.9263%); 494062 pixels alpha >=80.
Eight visual rows, counts 4,4,4,4,4,3,3,4 = 30 distinct visible poses. Recover and Hurt intentionally omit absent fourth poses rather than duplicate stills. Rows: Idle, Walk, WarnDash, Charge, SecondaryCharge/Buck, Recover, Hurt, Defeated.
Read-only row bands using alpha >=80 and minimum 10 pixels/scanline: [24,219), [240,418), [470,625), [663,856), [882,1109), [1135,1324), [1345,1533), [1579,1742). These are diagnostics, not production slicing rectangles. At alpha >100 all main body silhouettes remain off canvas edges. Tiny low-alpha paint fringes reach the image edges. The middle two Defeated poses touch at alpha >100; director/baker must inspect source-aware cuts and may omit a crowded pose. Never manufacture a missing pose. Raw RGB under low alpha includes yellow/red colors; preserve the native RGBA bytes.

Effects.png intentionally reuses the NEW approved Shepherd/Effects.png byte for byte; no Sheep FX generation was run.
Alias/source: Assets/OrpheusBelow/Resources/Enemies/Shepherd/Effects.png.
Original built-in output: C:/Users/asus/.codex/generated_images/01a17fc7-b7a3-7f92-8c04-55be04e56860/exec-7fa7f314-4f16-49cd-8005-d7f89fdf9277.png.
Source and destination SHA256 7ff6c336d9b410db573a7873ed8009869e4945cf20616f759e6301e0b1fb5424.
1536 x 1024 RGBA, alpha 0–254; 394870 alpha-zero pixels, 999299 alpha<10 pixels (63.5337%). Six columns/four rows: BellGlowSoundWave, SheepSummonCloud, CommandWave, WarnedSpawnCircle.

AtlasLayout.json preserves bodyColumns=4/bodyRows=8/effectColumns=6/effectRows=4. Its 2026-10-28 metadata follow-up now provides 28 explicit newly measured Body frames with rectTopLeft, rectUnity and foot pivots. Source30/Baked28: retain counts [4,4,4,4,4,3,3,2]; omit BOTH Defeated source cells row7/column1 and row7/column2 because their silhouettes touch. The clean retained defeated source columns remain 0 and 3. Recover/Hurt each use their three actual measured source regions, not four assumed equal-grid cells. body.autoSegmentationRequired=false; top-level autoSegmentationRequired=true remains for FX. No old frame geometry was reused.

Measurement: read-only alpha>100 four-connected components larger than 500 pixels, actual source row/column projections and complete main-figure bounds with up to 2px padding where gutters permit. All 28 regions are in bounds and nonempty; no retained alpha>100 main-figure pixel is clipped. Body PNG SHA256 remains 00e80e2c2ffdeaccf50988c2b0bd06cd1ceccdb34cdab5026de26be83dca2021. No sprite pixels were cropped or changed; these are source-region metadata only.

Known axis-isolation limitation, reported to root: 7 retained regions necessarily contain a small number of neighbor alpha>100 main pixels because source silhouettes overlap in horizontal projection. Zero-based row/column and foreign pixel counts: WarnDash (2,1)=4 and (2,2)=3; Charge (3,2)=8 and (3,3)=38; SecondaryCharge/Buck (4,0)=11, (4,1)=95 and (4,2)=117. Total 276 neighboring core pixels. No axis-aligned rectangle can preserve all main pixels while excluding every neighbor in those cases; AtlasLayout.body.segmentationWarnings and per-frame metrics disclose this rather than claiming clean gutters. Other 21 regions have zero foreign main pixels. Root must decide how to handle these small source overlaps when baking/reviewing. No C# or library assets edited; no Unity run, commit, push or runtime success claim.

## Exact selected material pass prompt

```text
Use case: style-transfer. Edit this transparent Sheep animation atlas IN PLACE, preserving EVERY existing sheep pose, silhouette, body proportions, color palette and cell location. Keep exactly its eight rows and four columns; row 7 has three poses, leave its fourth cell transparent. Preserve all complete ears, thin legs, gold ring eyes and black round bead necklaces. Genuine transparent alpha, no background or glow.
CHANGE ONLY THE PAINT MARK MAKING, DRAMATICALLY: repaint each sheep in VERY BROAD LONG THICK OIL BRUSHSTROKES. Current image has far too many small choppy paint dabs. Replace all the little cream facets on torso with THREE HUGE CONNECTED CREAM PAINT RIBBONS, each ribbon long enough to cover most of the animal's torso. Obvious LONG PARALLEL BRISTLE FURROWS run lengthwise across the whole shoulder and flank. Cream paint is physically heavy and dragged with a wide coarse flat brush. Shadow forms are only 2 large ochre/umber masses. Face is only a few clean planes plus the single gold ring eye. Limbs are a few long simple slate/cream strokes. Keep dark bead necklace and 2 bronze pendants simple. Overall roughly 15-20 broad strokes per whole sheep, NOT hundreds of dabs. Large connected shapes, loaded bristles, thick ridged impasto. Lower descriptive detail by 75 percent. No small chips, wool curls, speckles, hair strands, mosaic facets, dither, pixel art or tiny scratch detail. The character remains a cream sheep, never a wolf.
Do not change animation layout or anatomy, do not add frames or extras, no text or shadows. Preserve original alpha transparency around 31 sheep and empty bottom-right cell of row 7.
```

## Exact second candidate prompt (material-pass source)

Source output: C:/Users/asus/.codex/generated_images/01a17fd6-bc4c-7fa2-9a00-fda8f743fa00/exec-f0affcbe-0b51-430a-b5b0-35a28729c6cb.png.
SHA256 9d2c8870d5d6972828f03421e4ebd18e2cd4d6877dc64b5ba2fec4c6af3d3cf9.
Original reference and approved style-only reference were supplied. Too many small facets remained, so the selected material pass followed.

```text
Use case: stylized-concept. NEW production transparent sprite animation atlas. Portrait canvas, 4 columns x 8 rows, EXACTLY 32 isolated Sheep drawings. Each row is 1/8 height, each column is 1/4 width. This count is mandatory.
REFERENCE 1 original character sheet: draw ONLY the small IVORY SHEEP creatures, never the Shepherd. Preserve the reference sheep design: long pointed cream mask face, one large golden RING EYE, tall pointed upright ears, lean fleece-covered torso, thin long legs, long tail, large BLACK ROUND BEAD NECKLACE and 2 simple bronze pendants. No horns. Reference 2 is ONLY oil-paint material technique; do not copy its wolf character, colors, shapes or anatomy.
BROAD THICK OIL REDRAW: considerably less detail than reference. Only 3 to 5 LARGE CONNECTED cream brush-swipes make the entire fleece torso, each paint swipe extending most of torso length. Long clearly visible parallel dragged BRISTLE GROOVES in thick loaded oil paint. Simple broad ochre underpainting. Face and legs each blocked with 2-3 bold cream/slate/umber strokes. No little mosaic facets, individual hairs, curls, little spots, flecks or granular texture. Large smooth continuous paint masses with long bristle scoring, strong readable silhouette, not pixel art. Sheep remains cream with warm ochre shading, black bead collar, golden eye. Pure hand-painted oil, no 3D airbrushing.
LAYOUT: 4 by 8 regular grid with NO drawn grid. 32 equally sized complete sheep. Keep sheep small, occupying only 65% cell width and height, clear generous transparent gutters between every cell. All ears, hooves, tails complete. Fixed orthographic side-three-quarter camera facing RIGHT. Stable feet anchor, same scale. One sheep in every cell.
Eight rows top-to-bottom:
Row 1 (4 sheep): IDLE, four subtle breathing and ear positions.
Row 2 (4 sheep): WALK, four successive alternating walking-gait poses.
Row 3 (4 sheep): WARN DASH, four stages lowering head and compressing legs.
Row 4 (4 sheep): CHARGE, four successive fast running strides, legs extended then gathered.
Row 5 (4 sheep): SECONDARY CHARGE/BUCK, four successive bounding bucking poses, back hooves raised then landing.
Row 6 (4 sheep): RECOVER, four successive braking and rising poses.
Row 7 (4 sheep): HURT, four successive flinch/recoil poses, distinct from recover.
Row 8 (4 sheep): DEFEATED, four successive collapse stages from kneeling to fully lying on side.
Count all EIGHT rows separately. No duplicated frames. Do not omit hurt row.
TRUE TRANSPARENT RGBA with alpha zero in all empty space. No background paint, no shadows, no text, no borders, no labels, no glow, no effects, no colored fringes.
```

## Exact first candidate prompt (discarded)

Source output: C:/Users/asus/.codex/generated_images/01a17fd6-bc4c-7fa2-9a00-fda8f743fa00/exec-ce5f7d4b-b920-42bc-a06c-1213a3eb40c4.png.
SHA256 91aac1c536428401f55c6917b79ccbfaa708fc9a4078aebc874e32df7f6c149d.
Rejected in self-review: only seven rows/28 poses and insufficiently simplified paint.

```text
Use case: style-transfer.
Asset type: production game character sprite atlas, real transparent PNG.
Primary request: Redraw the SHEEP from image 1 and the small cream sheep in image 2 in a MUCH broader, less detailed thick oil-paint technique. Image 3 is STYLE ONLY: use its large connected painted masses and long visible bristle streaks, NEVER its wolf subject, color, heads or anatomy.
Input images: Image 1 is edit target and identity/pose reference, current Sheep atlas. Image 2 is original identity reference, use only cream sheep; ignore Shepherd. Image 3 is broad-oil material/style reference only.
Subject invariants: one slender fantastical IVORY SHEEP per frame, long pointed cream mask-like face, large GOLD CIRCULAR EYE, big upright pointed ears, lean body, thin umber/slate legs, long cream tail, oversized nearly black spherical bead necklace with 2-3 simple bronze pendant shapes. Preserve these unusual reference proportions and warm cream/ochre/umber palette. No horns. Do not turn into wolf, dog or Cerberus.
Style: simplify greatly into FEW BIG CONNECTED BRUSH MASSES: fleece torso painted as 3-5 long cream loaded-brush SWIPES, long directional parallel bristle ridges dragged through thick cream oil. Broad ivory highlights with ochre shadow swipes; face and legs made of few slate/umber/cream planes. Each visible stroke sweeps across a large fraction of the body. Rough palette-knife edges, obvious oil impasto. No individual wool curls, no little flecks, no fur hairs, no fine speckles, no pixel dither, no 3D render or airbrush. Match original silhouette.
Composition: EXACTLY FOUR COLUMNS and EIGHT ROWS = 32 distinct successive full-body Sheep poses on invisible equal 4x8 grid. Wide empty transparent gutters minimum 25 px. Animal occupies only 70% width and 70% height of cell. All ears, hooves, tail and pendants complete inside own cell. Camera fixed orthographic side-three-quarter view facing RIGHT. Same character body scale. Stable feet/ground anchor per row. No animal touches canvas edge.
Animation rows top to bottom, each FOUR poses left to right:
1 Idle: quiet standing, breathing, ear twitch, relaxed settle.
2 Walk: four genuine successive walking gait stages with alternating front/back limbs.
3 WarnDash: lower pointed head through four successive crouching anticipation stages.
4 Charge: four fast successive rightward charging stride stages, extended then gathered legs.
5 SecondaryCharge: four successive bucking bounding charge stages, rear legs lift and land.
6 Recover: four braking and rising stages, slowing to standing.
7 Hurt: four successive recoil stages, compress, pull back, stagger, steady.
8 Defeated: four successive collapse stages, kneeling, falling sideways, low lying, fully still.
No duplicated stills. Draw ONLY these 32 isolated sheep. No text, labels, borders, grid, environment, ground plane, drop shadows, glow or effects. Genuine alpha zero background, no painted checkerboard, no yellow/red fringe. Native output high resolution portrait atlas.
```

## Superseded generation record

# Sheep generation record

Built-in image_gen mode, 2026-10-27. Transparent background requested. Source PNGs are copied byte for byte; no raster edits, resizing, repacking, duplicate stills or pixel cleanup.

Reference: D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (6).jpeg.
Design invariants: Slender ivory sheep with pointed mask-like faces, circular golden eyes, upright ears, thin legs, rough cream fleece and oversized dark bead necklaces with bronze pendants.

Selected body: C:/Users/asus/.codex/generated_images/01a17efb-9b98-7d02-809a-21bde3865a4c/exec-5f98f043-7b84-4176-b0e3-7b7f4de1bf5f.png (887 x 1774 RGBA).
Selected effects: C:/Users/asus/.codex/generated_images/01a17efb-9b98-7d02-809a-21bde3865a4c/exec-f711e118-9229-483f-9860-59c9bb8f6de9.png (1536 x 1024 RGBA).
Effects.png is an intentional byte-identical alias of Shepherd/Effects.png. Summon cloud, gold bell magic, dust impact and gold warning language are shared by the flock; no unique sheep effects were requested or fabricated.

## Layout and verification

Body: exactly 32 distinct successive poses, four per motion. Top-to-bottom: Idle, Move, Windup, Attack, Attack2, Recover, Hurt, Defeated. Quality was prioritized over a larger nominal count. Source analysis found 32 disconnected main opaque silhouettes (alpha >100, component area >500 pixels), with complete silhouettes contained by the explicit rectangles. Canvas edges contain no opaque body pixels. A single layout retry reduced silhouette crowding. Equal grid cutting would still clip some crowns, tails or defeated poses; AtlasLayout.json supplies measured variable cuts and per-frame foot pivots. The PNG remains unchanged. The same pixels-per-unit must be used across every body row.

Effects: exactly 24 frames, six per row: bell sound wave, summon cloud, dust impact, golden eye warning. Real alpha range 0-253; 63.37% of source pixels have alpha below 10. The PNG stores warm RGB in transparent and very low-alpha pixels; a viewer that ignores alpha may expose a golden backdrop. It is not an opaque sheet. Explicit rectangles follow zero-opaque-pixel seams, with row cuts 0,265,525,768,1024 and measured column cuts per row. The first effect generation is retained because its separators were cleaner than the single retry. Do not use a regular 256-square cut. Geometric runtime warnings are the authoritative damage boundary.

AtlasLayout.json records exact SHA-256 hashes, source rectangles in top-left and Unity bottom-left coordinates, pivots, alpha bounds and frame counts. Focused source tests: Assets/OrpheusBelow/Tests/Editor/ShepherdPaintedTests.cs. Unity compilation and runtime/editor tests are centralized by the integration agent; this record does not assert they passed.

## Runtime

SheepAttack: 1.1 s rest, 0.8 s locked dash warning, 11 m/s dash for 0.6 s, 14 damage once per dash, 1.0 s recovery. Nine overlapping warning disks mark the swept hit lane. Aim is fixed before the warning; the sheep cannot steer its dash. Movement uses solid-safe actor movement in at most 0.25 m slices and checks dash invulnerability through TryHitPlayer. Boss stage aim projects onto the stage axis through Actor.IsBossStage. Bell commands queue until existing ShadeEnemy state permits the next AI Tick, so they cannot draw new warnings during a musical stun. Cancel removes every warning and owned transient effect.

## Exact initial body prompt

```text
Use case: stylized-concept. Production Unity transparent animation sprite atlas, EXACTLY FOUR COLUMNS and EIGHT ROWS, 32 separate successive full-body animal poses. Input image is character design reference; draw ONLY the small SHEEP creatures from it, never Shepherd. Preserve unusual slender ivory sheep with pointed long triangular mask-like face, large golden circular eyes, large upright ears, rough cream fleece, thin legs, dark oversized round bead necklace and little hanging bronze pendants. Broad hand-painted oil brushwork matching reference, warm cream and dull gold. Orthographic side-three-quarter view toward right. Genuine transparent RGBA backdrop. Equal 4x8 invisible grid, generously spaced complete body per cell with all ears, feet and tails within cell. Same animal identity and approximate body size throughout, stable ground baseline. Row1 Idle four subtle breathing/ear poses. Row2 Move four distinct walking gait poses. Row3 Windup crouch and lower head readying charge, four stages. Row4 Attack four successive leaping/charging stride poses. Row5 Attack2 alternate charge four bounding stride poses. Row6 Recover four stages slowing and lifting head. Row7 Hurt four distinct recoil poses. Row8 Defeated four stages falling to lying still. No writing, borders, environment, ground plane, drop shadows or accessories beyond reference. Do not pack extra animals or reference-sheet closeups. Entire silhouette must be isolated and have transparent gutters.
```

## Exact selected body prompt (single retry)

```text
Use case: stylized-concept. Production Unity transparent animation sprite atlas, EXACTLY FOUR COLUMNS and EIGHT ROWS, 32 separate successive full-body animal poses. Input image is character design reference; draw ONLY the small SHEEP creatures from it, never Shepherd. Preserve unusual slender ivory sheep with pointed long triangular mask-like face, large golden circular eyes, large upright ears, rough cream fleece, thin legs, dark oversized round bead necklace and little hanging bronze pendants. Broad hand-painted oil brushwork matching reference, warm cream and dull gold. Orthographic side-three-quarter view toward right. Genuine transparent RGBA backdrop. Equal 4x8 invisible grid, generously spaced complete body per cell with all ears, feet and tails within cell. Same animal identity and approximate body size throughout, stable ground baseline. Row1 Idle four subtle breathing/ear poses. Row2 Move four distinct walking gait poses. Row3 Windup crouch and lower head readying charge, four stages. Row4 Attack four successive leaping/charging stride poses. Row5 Attack2 alternate charge four bounding stride poses. Row6 Recover four stages slowing and lifting head. Row7 Hurt four distinct recoil poses. Row8 Defeated four stages falling to lying still. No writing, borders, environment, ground plane, drop shadows or accessories beyond reference. Do not pack extra animals or reference-sheet closeups. Entire silhouette must be isolated and have transparent gutters. CRITICAL LAYOUT CORRECTION: shrink every animal to only 70 percent of cell width and 70 percent of cell height. Ensure WIDE EMPTY TRANSPARENT gutters between all animals: minimum 25 pixels, including tail tips and all ears. All four paws and the complete tail must fit inside a SINGLE equal cell. No animal touches the canvas edge. DO NOT enlarge the character to fill the cell. 32 smaller clean complete animals is essential. Keep same body identity and all eight movement rows. Transparent pixels should not contain yellow or red fringe colors.
```

## Exact selected effects prompt

```text
Use case: stylized-concept. Transparent game visual effects animation atlas. EXACTLY SIX columns by FOUR rows = 24 separate effects frames. Equal invisible grid, generous transparent gutters, no text no borders no ground no characters. Broad oil-painted texture matches reference ivory, ochre gold, smoke gray and muted moss olive. Row1 six successive expanding curved GOLDEN BELL SOUND WAVES, bronze sparks, clean ring fragments; Row2 six successive SHEEP SUMMON CLOUD puffs, cream wool-like curling clouds with pale golden motes from tiny start through burst to dissipated; Row3 six successive DUST IMPACT puffs with tiny gold spark debris, hit splash spreading and fading; Row4 six successive circular GROUND WARNING sigil, thin golden ring with four eye-like accents swelling from faint to bright. Each frame isolated full effect inside own cell, no spill between cells. All 24 cells occupied, no duplicated frames. True RGBA transparency. Stable center in each equal cell. Match broad oil brushwork and hand-painted folk horror gold tones of the reference. Canvas landscape 1536x1024.
```

## Discarded effects retry prompt

```text
Use case: stylized-concept. Transparent game visual effects animation atlas. EXACTLY SIX columns by FOUR rows = 24 separate effects frames. Equal invisible grid, generous transparent gutters, no text no borders no ground no characters. Broad oil-painted texture matches reference ivory, ochre gold, smoke gray and muted moss olive. Row1 six successive expanding curved GOLDEN BELL SOUND WAVES, bronze sparks, clean ring fragments; Row2 six successive SHEEP SUMMON CLOUD puffs, cream wool-like curling clouds with pale golden motes from tiny start through burst to dissipated; Row3 six successive DUST IMPACT puffs with tiny gold spark debris, hit splash spreading and fading; Row4 six successive circular GROUND WARNING sigil, thin golden ring with four eye-like accents swelling from faint to bright. Each frame isolated full effect inside own cell, no spill between cells. All 24 cells occupied, no duplicated frames. True RGBA transparency. Stable center in each equal cell. Match broad oil brushwork and hand-painted folk horror gold tones of the reference. Canvas landscape 1536x1024. CRITICAL CORRECTION: completely REMOVE the brown/gold gradient backdrop. Output genuinely TRANSPARENT RGBA. Only paint marks of the individual 24 effects may have nonzero alpha. Empty gutters, corners and all background pixels must have alpha zero. Keep every effect comfortably 20 pixels away from its cell boundaries; reduce effect sizes to fit. Do not use a colored or checkered painted background.
```

