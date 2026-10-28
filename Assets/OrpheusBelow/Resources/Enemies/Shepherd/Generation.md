# Shepherd broad oil-brush restyle — 2026-10-28

Built-in image_gen only. Candidate 3 selected after two smoother candidates were rejected in self-review. PNGs copied byte for byte from tool output; no production pixel filtering, repainting, recolouring, resizing, repacking or duplicate-frame inflation. Inspection composites under Logs/ArtReview are separate review-only files.

Original identity reference: D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (6).jpeg.
Prior animation reference: this folder's pre-restyle Body.png (source documented in the superseded record below).
Approved Cerberus style-only reference: C:/Users/asus/.codex/generated_images/01a17f97-6cdb-7291-b318-3bdb0835e4ca/exec-e2c9561e-9106-4d99-b43e-aeeb01c076ef.png.
Identity retained: tall conical olive cloak/hat, cream eye tabard, rust oval single-eye mask, candle crown, dark gloves/boots, bronze hand bell and sparse round talismans. Long connected olive sweeps replace the small interior leaves; cream/rust/charcoal regions use broad dragged strokes.

Selected Body.png: C:/Users/asus/.codex/generated_images/01a17fc7-b7a3-7f92-8c04-55be04e56860/exec-99d7a4d2-94a1-4e83-a0f5-7f66ca1f6129.png.
887 x 1774 RGBA. SHA256 1743cd0f0dfe95214841a741358a4842ac7cd4b45193344d5094457fd7fafd1e. Alpha 0–255; 862807 fully transparent pixels; 1026836 pixels below alpha 10 (65.2565%); 515246 pixels alpha >=80. 32 unique nominal cell images, 4 columns x 8 rows. The unique-cell count is a diagnostic, not evidence of valid regular cuts. Rows: idle, walk, bell windup, summon attack, flock command attack, recovery, hurt, defeat. Eight nonoverlapping opaque row bands at alpha >=80: [11,215), [223,419), [430,647), [662,860), [876,1082), [1098,1305), [1319,1524), [1552,1733). Full figures visually inspected on charcoal; clear gutters, full bell/candles/feet. Actual frames require source-aware segmentation and consistent foot pivots.

Selected Effects.png: C:/Users/asus/.codex/generated_images/01a17fc7-b7a3-7f92-8c04-55be04e56860/exec-7fa7f314-4f16-49cd-8005-d7f89fdf9277.png.
1536 x 1024 RGBA. SHA256 7ff6c336d9b410db573a7873ed8009869e4945cf20616f759e6301e0b1fb5424. Alpha 0–254; 394870 fully transparent pixels; 999299 pixels below alpha 10 (63.5337%); 488121 pixels alpha >=80. 24 unique nominal cell images, 6 columns x 4 rows. Rows: bell glow/sound wave, sheep summon cloud, command wave, warned spawn circle. Source ShepherdAttack uses FX0 for bell, FX1 for summon and FX3 for warning; row2 has been repainted from the former dust effect to a command wave without script changes. The warning row has complete elliptical eye disks and visible brightening/growth/fade. Four opaque row bands at alpha >=80: [22,270), [285,541), [573,773), [794,990). Warm RGB exists under low alpha; charcoal inspection confirms the delivered sheet does not have an opaque warm background. Do not use equal 256px row cuts.

AtlasLayout.json deliberately contains NO old body/effects frame rectangles or pivots: both sources have changed. autoSegmentationRequired=true. The opaque bands above are diagnostics, not authored cuts. Measure new source-aware frame rectangles before baking. No Unity run or gameplay claim is made here.

Inspection: Logs/ArtReview/Shepherd-broad-oil-source-validation.json; inspect-shepherd-broad-oil.py; Shepherd-broad-oil-Body-charcoal-inspection.png; Shepherd-broad-oil-Effects-charcoal-inspection.png; Shepherd-broad-oil-native-first-two-rows.png. Inspections read source pixels and create separate review composites only.

## Exact selected body prompt (candidate 3)

```text
Paint a transparent 2D game animation sheet in ROUGH BROAD-STROKE OIL PAINT, exactly 32 small complete figures arranged 4 columns x 8 rows. The attached image is ONLY the Shepherd design reference; simplify this design into a few great sweeping slabs of dragged oil paint.

MEDIUM IS THE MAIN REQUIREMENT: rough emphatic brushwork, loaded flat-brush sweeps, conspicuous LONG PARALLEL BRISTLE STREAKS and impasto ridges; scratchy streaks WITHIN connected paint masses. Apply the cloak with about FIVE LONG diagonal olive brush sweeps, each shoulder-to-hem, each with large clearly visible bristle channels. No individual little leaves inside the cloak. Long vertical cream brush drags make the tabard. Broad rust-red drag makes the mask, with one ochre concentric eye mark. Large charcoal brush drags make the sleeves and boots. Low detail; coarse, unfinished oil study; strokes remain obvious even at small resolution. NOT soft smooth cartoon painting. NO smooth gradients, NO airbrush, NO 3D, NO glossy render, NO tiny leaf chips, NO tiny facets, NO filigree, NO decorative tracery, NO tiny high frequency details, NO pixel art, NO noise. Preserve the sharp readable outer silhouette.

Identity: tall conical olive cloak/hat sweeping down almost to boots; crown of cream lit candles; oval rust red and ochre single-eye mask without visible human face; long ivory tabard with two large round gold eye emblems; dark gloves and boots; one hanging bronze hand bell; few large round bronze talismans. Palette muted olive, warm ivory, charcoal, rust red, antique ochre. Orthographic 3/4 view facing right. No sheep.
Layout: exactly FOUR COLUMNS, EIGHT ROWS. Stable figure size and feet anchor. Reduce each complete figure to 65 percent cell height; wide transparent gutters surround it, including candles and outstretched bell. Widest collapsed pose stays within its cell. No touching or overlap across cells. Real transparent background.
Four DIFFERENT sequential poses per row, top to bottom:
row1 idle swaying;
row2 walk alternating feet, cloak movement;
row3 slowly raise bell from waist to overhead;
row4 ring the bell outward to summon;
row5 command flock, extend free arm to point while holding bell opposite;
row6 return arms/bell to rest;
row7 hurt recoil and stagger;
row8 kneel then slump then fully collapse.
Clear change of pose between all four frames of each action, not repeated stills.
Canvas tall 1024x2048. No text, grid lines, floor, scenery, shadow or background colour. ONLY isolated painted Shepherds with actual alpha transparency.
```

## Exact selected effects prompt

```text
Use case: stylized-concept. Production transparent game effects animation atlas for SHEPHERD. Exactly SIX COLUMNS x FOUR ROWS =24 distinct effects frames. Landscape 1536x1024. Real RGBA alpha transparency, all gutters/corners alpha zero. No background colour, ground, characters, sheep bodies, scene, text, borders or drawn grid.

MEDIUM: Rough traditional oil painting with thick BROAD loaded brush sweeps, visible long parallel bristle grooves, very few large connected paint masses. Strokes span a large portion of each effect. Low-detail, NOT clouds made of many tiny puffs or particles; NOT fine luminous CGI; NOT smooth airbrush. Ivory, ochre gold, charcoal smoke, muted olive. Supporting image is Shepherd for palette/medium only.
Every frame completely isolated, generous empty transparent gutters, comfortably contained within its cell, no neighboring overlap, stable central anchor. Four different progressions row by row:
ROW1: SIX successive GOLDEN BELL GLOW / SOUND WAVE frames. Small thick ochre curved brush arc, growing wave, wider wave, expanding heavy crescent, breaking broad arc, fading two streaks. Few large brush marks. No individual tiny sparks.
ROW2: SIX successive SHEEP SUMMON CLOUD frames. Small cream/charcoal puff, three large broad swirling ivory brush masses swell, connected larger cloud, maximum broad cloud, breaking into two large wisps, fading streaks. Only a few bold curls and clearly visible bristle drag.
ROW3: SIX successive COMMAND WAVE frames. Low horizontal olive/gold sweeping gesture grows from short slash to longer outward moving broad crescent, peak thick rolling command sweep, fragments into two broad strokes, fades. Readable motion progression, no microscopic debris.
ROW4: SIX successive WARNED SPAWN CIRCLES on ground. Each is one readable gold elliptical DISK with a continuous clearly visible thick perimeter plus large central eye symbol, subtle translucent ochre interior paint fill. From faint small disk through larger brighter stronger disk to full bright warning disk; the last two show strong full disk then fade. Keep fill and perimeter legible on dark ground. Four eye accents maximum. Broad bristly elliptical paint strokes. All six full disks complete, no cutoff and no shared neighbor.
Do not paint an opaque gradient backdrop or coloured checkerboard. Only effect brushmarks have nonzero alpha. No labels.
```

## Rejected candidate 1
Source: C:/Users/asus/.codex/generated_images/01a17fc7-b7a3-7f92-8c04-55be04e56860/exec-55cc17d4-97ba-44aa-a3e2-cf6378c910f9.png.
Reason: fine cloak chips and overly smooth finish.

```text
Use case: style-transfer.
Asset: production transparent Unity SHEPHERD body animation atlas, exactly FOUR COLUMNS and EIGHT ROWS, 32 individual sequential full-body sprites. Tall 1024x2048 canvas preferred.
Inputs: image 1 is ORIGINAL SHEPHERD identity reference; image 2 is current SHEPHERD animation atlas and action/silhouette reference; image 3 is approved CERBERUS reference for BROAD OIL BRUSH MEDIUM ONLY, never borrow its animal anatomy or blue palette.
PRIMARY CHANGE: repaint Shepherd with DRAMATICALLY LESS DETAIL and obvious THICK BROAD OIL BRUSH STREAKS. Use a few large connected colour masses, long loaded-brush sweeps with conspicuous parallel bristle tracks visibly running along the cloak and tunic. Each stroke spans a substantial fraction of a body part. Coarse expressive traditional oil paint, opaque impasto strokes, no digital smooth shading. Simplify small leaves into 4-6 huge olive cloak sweeps, not dozens of separate leaf chips. Simplify decorative tracery away, retain just two or three LARGE gold eye emblems on cream tabard. Broad ochre/red strokes define the oval mask, no tiny carving. The brush streaks must be readily visible at game sprite size.
PRESERVE: the reference's distinctive tall conical heavy olive leafy cloak/hat silhouette, red-brown oval face mask with one large concentric gold eye, cream long eye-patterned tabard, crown of cream candles with ochre flames, dark sleeves/gloves/boots, bronze hand bell, sparse large dangling bronze talismans. Same muted moss olive, ivory, ochre gold, rust red, charcoal palette. Same 3/4 orthographic view facing slightly right. Shepherd alone, NO SHEEP.
LAYOUT: strict 4 columns x 8 rows. Each sprite COMPLETE, small enough to leave very generous empty transparent gutters, all candle flames, bell, fingertips and feet inside own cell. Character roughly 65-70 percent of cell height; widest gesture fits own cell. Consistent character scale, stable foot baseline within every row, no shadows or ground. NEVER overlap adjacent sprites. Bottom defeat poses remain centered inside their cells.
Rows top-to-bottom, four visibly distinct sequential poses left-to-right:
1 idle breathing, slight sways and bell movement;
2 walk, alternating actual steps and cloak swing;
3 windup raising bell from waist to overhead;
4 summon attack ringing bell outward through four clear motion phases;
5 command flock attack, extend free hand and point forward strongly, bell opposite hand, four clear phases;
6 recover lowering bell and relaxing arms through four phases;
7 hurt, progressive recoil then regain balance;
8 defeat, knees bend, kneel, slump, full low collapse.
AVOID: many tiny facets, chipped patchwork, pixel art, pixelated edges, tiny dabs, fur strands, fine folds, microtexture, detailed filigree, smooth 3D, airbrush gradients, photorealism, black outlines, scene backgrounds, labels, grid lines, duplicate stills.
Background MUST be actual transparent RGBA. Keep all non-character pixels alpha zero.
```

## Rejected candidate 2
Source: C:/Users/asus/.codex/generated_images/01a17fc7-b7a3-7f92-8c04-55be04e56860/exec-c3045d45-83f8-4f30-a4ad-3797382d6eed.png.
Reason: cleaner large masses but still smoother/less bristly than selected candidate 3.

```text
Use case: style-transfer. Repaint image 1, the SHEPHERD 32-frame transparent animation atlas. Image 2 is STYLE/MEDIUM reference only (Cerberus). Preserve Shepherd identity, original colours and all 32 poses, arranged FOUR COLUMNS by EIGHT ROWS.

Change ONLY rendering medium and reduce surface detail aggressively. This MUST look like a physical oil painting made with a very large flat brush. THICK, LONG, CONTINUOUS BRUSH STREAKS, visible parallel bristle grooves, broad dragged strokes. Stroke lengths 1/3 to 2/3 of each garment/body region. Cloak must read as ONLY FIVE OR SIX BIG CONNECTED OLIVE PAINT SWEEPS, each sweeping from shoulder down toward the cloak hem. Do not paint the individual leaves. No scalloped leaf chips or many tiny facets. Keep a leafy outside silhouette but make the inside connected. The cream tabard is 3 long broad ivory strokes with 2 or 3 large simple gold eye emblems; remove every fine decorative swirl. The mask is 3 large rust-red/ochre strokes and one simple gold eye. Bell, candles, gloved hands, boots and hanging round talismans remain clearly legible but with minimal broad strokes. Retain conical olive cloak/hat, crown of cream candles, rust oval mask, ivory tabard, little bronze hand bell, charcoal boots and gloves, sparse bronze discs.

The first image has too many small leaf chips and a smooth shaded finish. REPLACE these with conspicuous long bristle streaks; do not merely soften, blur or sharpen them. A few confident loaded brush sweeps, rough painterly edges, dark simple underpainting, compressed values, only a few highlights. MORE OBVIOUS HAND-PAINTED BRUSHWORK THAN IMAGE 2, fewer small details. No 3D rendering, airbrush, photoreal texture, mosaic patches, dot stipple, tiny chips, fine leaves, tiny folds, pixel art, outline ink.

Exactly 4 columns x 8 rows=32 distinct complete full-body sprites. Preserve sequential movement poses from image 1:
row1 idle; row2 walk; row3 raise bell windup; row4 ring bell summon attack; row5 point/command flock attack; row6 lower bell recovery; row7 recoil hurt; row8 bend knees, kneel, slump, full low collapse.
All sprites same scale. REDUCE figure height to about 65% of each cell, leaving clear generous gutters above candles and below boots. Widest bell/point gesture and collapsed figure must fully fit within own cell with transparent space on all sides. Every complete silhouette isolated. No sheep, no ground, no shadows, no text or labels, no drawn grid.
Actual transparent RGBA background, alpha zero outside paint. Tall 1024x2048 canvas.
```

---

# Superseded 2026-10-27 record (old source metrics and cuts below are historical)


Built-in image_gen mode, 2026-10-27. Transparent background requested. Source PNGs are copied byte for byte; no raster edits, resizing, repacking, duplicate stills or pixel cleanup.

Reference: D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (6).jpeg.
Design invariants: Olive leafy cloak, cream eye-patterned tunic, oval red/gold mask, candle crown, hand bell, dark gloves and hanging bronze talismans.

Selected body: C:/Users/asus/.codex/generated_images/01a17efb-9b98-7d02-809a-21bde3865a4c/exec-a593f041-8217-4b5f-935b-d2912544c0e6.png (887 x 1774 RGBA).
Selected effects: C:/Users/asus/.codex/generated_images/01a17efb-9b98-7d02-809a-21bde3865a4c/exec-f711e118-9229-483f-9860-59c9bb8f6de9.png (1536 x 1024 RGBA).


## Layout and verification

Body: exactly 32 distinct successive poses, four per motion. Top-to-bottom: Idle, Move, Windup, Attack, Attack2, Recover, Hurt, Defeated. Quality was prioritized over a larger nominal count. Source analysis found 32 disconnected main opaque silhouettes (alpha >100, component area >500 pixels), with complete silhouettes contained by the explicit rectangles. Canvas edges contain no opaque body pixels. A single layout retry reduced silhouette crowding. Equal grid cutting would still clip some crowns, tails or defeated poses; AtlasLayout.json supplies measured variable cuts and per-frame foot pivots. The PNG remains unchanged. The same pixels-per-unit must be used across every body row.

Effects: exactly 24 frames, six per row: bell sound wave, summon cloud, dust impact, golden eye warning. Real alpha range 0-253; 63.37% of source pixels have alpha below 10. The PNG stores warm RGB in transparent and very low-alpha pixels; a viewer that ignores alpha may expose a golden backdrop. It is not an opaque sheet. Explicit rectangles follow zero-opaque-pixel seams, with row cuts 0,265,525,768,1024 and measured column cuts per row. The first effect generation is retained because its separators were cleaner than the single retry. Do not use a regular 256-square cut. Geometric runtime warnings are the authoritative damage boundary.

AtlasLayout.json records exact SHA-256 hashes, source rectangles in top-left and Unity bottom-left coordinates, pivots, alpha bounds and frame counts. Focused source tests: Assets/OrpheusBelow/Tests/Editor/ShepherdPaintedTests.cs. Unity compilation and runtime/editor tests are centralized by the integration agent; this record does not assert they passed.

## Runtime

ShepherdAttack: rest 2.0 s, locked summon warning 1.2 s, summon animation 0.65 s, flock command 0.65 s, recovery 1.4 s. At most three living sheep; at most two summoned per cast. Spawn points stay at least 2.5 m from the player when planned, are rejected inside solid cover or another sheep, and are skipped if the player enters within 1.5 m before materialization. Summoned sheep use existing ShadeEnemy health/cadence/stun; they are not registered as ordinary wave enemies. Cancel removes all of this shepherd's summons, warnings and effects.

## Exact initial body prompt

```text
Use case: stylized-concept. Asset: production transparent Unity sprite animation atlas, EXACTLY 4 columns by 8 rows = 32 separate full-body poses. Reference image identifies only SHEPHERD character, not sheep. Repaint this masked broad oil-painted guardian: heavy olive leafy cloak, tapering cream tunic with huge gold eye motifs, oval red/gold mask, candle crown, dark gloves, little bronze hand bell and dangling talismans. Preserve reference costume and painterly brushwork. Orthographic three-quarter view facing slightly right, full feet, same size and foot anchor in every equal cell. Portrait atlas 1024x2048 preferred. Transparent RGBA background. Strict invisible equal 4x8 grid with generous empty transparent gutters; one distinct complete isolated shepherd per cell, no props outside their cell, no titles, no borders, no background, no drop shadows. Top-to-bottom rows: 1 Idle breathing four successive poses; 2 Move four walking steps; 3 Windup raising bell high across four poses; 4 Attack summon flock ceremonial bell shake four poses; 5 Attack2 commanding sheep with long arm pointing four poses; 6 Recover lowering bell four poses; 7 Hurt four recoiling poses; 8 Defeated four successive kneeling and collapsed poses. Full silhouette inside each cell including candles, hands, bell, cloak and feet. Stable dimensions, clear animation progression. Do not draw sheep in this atlas.
```

## Exact selected body prompt (single retry)

```text
Use case: stylized-concept. Asset: production transparent Unity sprite animation atlas, EXACTLY 4 columns by 8 rows = 32 separate full-body poses. Reference image identifies only SHEPHERD character, not sheep. Repaint this masked broad oil-painted guardian: heavy olive leafy cloak, tapering cream tunic with huge gold eye motifs, oval red/gold mask, candle crown, dark gloves, little bronze hand bell and dangling talismans. Preserve reference costume and painterly brushwork. Orthographic three-quarter view facing slightly right, full feet, same size and foot anchor in every equal cell. Portrait atlas 1024x2048 preferred. Transparent RGBA background. Strict invisible equal 4x8 grid with generous empty transparent gutters; one distinct complete isolated shepherd per cell, no props outside their cell, no titles, no borders, no background, no drop shadows. Top-to-bottom rows: 1 Idle breathing four successive poses; 2 Move four walking steps; 3 Windup raising bell high across four poses; 4 Attack summon flock ceremonial bell shake four poses; 5 Attack2 commanding sheep with long arm pointing four poses; 6 Recover lowering bell four poses; 7 Hurt four recoiling poses; 8 Defeated four successive kneeling and collapsed poses. Full silhouette inside each cell including candles, hands, bell, cloak and feet. Stable dimensions, clear animation progression. Do not draw sheep in this atlas. CRITICAL LAYOUT CORRECTION: reduce the shepherd sprite to just 70 percent of its equal cell height and width in EVERY cell. Reserve wide EMPTY transparent gutters 25px minimum around the COMPLETE silhouette. No candle flames touch the canvas top. Candle crown and full feet and bell all must fit comfortably inside one equal cell. Four columns eight rows 32 SMALLER sprites with generous whitespace. Prioritize complete unclipped silhouettes over large character size. Genuine transparent background, no red edge colors.
```

## Exact selected effects prompt

```text
Use case: stylized-concept. Transparent game visual effects animation atlas. EXACTLY SIX columns by FOUR rows = 24 separate effects frames. Equal invisible grid, generous transparent gutters, no text no borders no ground no characters. Broad oil-painted texture matches reference ivory, ochre gold, smoke gray and muted moss olive. Row1 six successive expanding curved GOLDEN BELL SOUND WAVES, bronze sparks, clean ring fragments; Row2 six successive SHEEP SUMMON CLOUD puffs, cream wool-like curling clouds with pale golden motes from tiny start through burst to dissipated; Row3 six successive DUST IMPACT puffs with tiny gold spark debris, hit splash spreading and fading; Row4 six successive circular GROUND WARNING sigil, thin golden ring with four eye-like accents swelling from faint to bright. Each frame isolated full effect inside own cell, no spill between cells. All 24 cells occupied, no duplicated frames. True RGBA transparency. Stable center in each equal cell. Match broad oil brushwork and hand-painted folk horror gold tones of the reference. Canvas landscape 1536x1024.
```

## Discarded effects retry prompt

```text
Use case: stylized-concept. Transparent game visual effects animation atlas. EXACTLY SIX columns by FOUR rows = 24 separate effects frames. Equal invisible grid, generous transparent gutters, no text no borders no ground no characters. Broad oil-painted texture matches reference ivory, ochre gold, smoke gray and muted moss olive. Row1 six successive expanding curved GOLDEN BELL SOUND WAVES, bronze sparks, clean ring fragments; Row2 six successive SHEEP SUMMON CLOUD puffs, cream wool-like curling clouds with pale golden motes from tiny start through burst to dissipated; Row3 six successive DUST IMPACT puffs with tiny gold spark debris, hit splash spreading and fading; Row4 six successive circular GROUND WARNING sigil, thin golden ring with four eye-like accents swelling from faint to bright. Each frame isolated full effect inside own cell, no spill between cells. All 24 cells occupied, no duplicated frames. True RGBA transparency. Stable center in each equal cell. Match broad oil brushwork and hand-painted folk horror gold tones of the reference. Canvas landscape 1536x1024. CRITICAL CORRECTION: completely REMOVE the brown/gold gradient backdrop. Output genuinely TRANSPARENT RGBA. Only paint marks of the individual 24 effects may have nonzero alpha. Empty gutters, corners and all background pixels must have alpha zero. Keep every effect comfortably 20 pixels away from its cell boundaries; reduce effect sizes to fit. Do not use a colored or checkered painted background.
```


## Integration source-region correction — 2026-10-28

Body playback now uses measured `AtlasLayout.json` body.frames rather than global grid cuts. PNG bytes remain unchanged. SHA256: `1743cd0f0dfe95214841a741358a4842ac7cd4b45193344d5094457fd7fafd1e`; source dimensions 887×1774.

Actual motion counts (Idle, Move, Windup, Attack, Attack2, Recover, Hurt, Defeated): `[4, 4, 4, 4, 4, 4, 4, 4]` (32 distinct poses). Each region retains every alpha>100 pixel of its own connected main silhouette; no meaningful detached component of 20–1999 pixels exists. Up to two source pixels of soft-edge padding are added when no extra foreign main pixels enter. Foot pivots use the median opaque X of the lowest 12% of the main figure and its measured lowest Y.

Omissions and source-column mapping are recorded in `body.omittedSourcePoses` and each retained frame's `sourceColumn`. 0 omission entries; no duplicate poses were created. All retained rectangles contain zero alpha>100 pixels from another main silhouette.
