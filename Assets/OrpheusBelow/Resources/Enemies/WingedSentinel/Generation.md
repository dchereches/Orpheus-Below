# Winged Sentinel generation record

Built-in `image_gen` mode; transparent background requested for every generation. No raster edits, repacking, duplicated stills, pixel cleanup or resizing were performed. The selected PNGs are unchanged copies of generated outputs.

Reference: `D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (3).jpeg`. Design invariants: purple/slate cracked skin, black hair and pointed ears, gold jewelry, ivory skirt, enormous ivory feathered wings and engraved greatsword, broad tactile oil strokes.

Selected body: `C:/Users/asus/.codex/generated_images/01a17ef5-7887-7a41-84dd-4bbbb2d6a0d1/exec-19bb2837-7fe0-4818-bc53-c989adf83329.png` (887×1774 RGBA).
Selected effects: `C:/Users/asus/.codex/generated_images/01a17ef5-7887-7a41-84dd-4bbbb2d6a0d1/exec-e405357e-0b56-4599-8442-0641a0dc82a0.png` (1536×1024 RGBA).
Discarded first body: `C:/Users/asus/.codex/generated_images/01a17ef5-7887-7a41-84dd-4bbbb2d6a0d1/exec-85257c2d-0b24-4690-84c3-a29d7fd9845c.png`; wing overlap prompted one targeted generation retry.

## Layout and inspection

Body: 4 columns × 8 rows = 32 distinct successive poses. Top-to-bottom rows: Idle, Move, Windup, Attack (horizontal sweep), Attack2 (overhead cleave), Recover, Hurt, Defeated. Four painted frames per row, left to right. Quality prioritized over the requested target of 48. Exactly 32 large disconnected opaque silhouettes were detected, one per frame. Equal grid slicing would clip overhead swords; `AtlasLayout.json` supplies explicit variable row/column rectangles and source-measured foot pivots. Retain a common pixels-per-unit value across every frame so these rectangles do not rescale the actor. Body x boundaries: 0,235,449,660,887. Body y boundaries: 0,236,441,674,866,1114,1338,1557,1774. Small low-alpha brush flecks exist near some gutters; opaque character silhouettes remain separate.

Effects: 6 columns × 4 rows = 24 distinct painted frames. Top-to-bottom rows: ivory sword arc, overhead cleave burst, impact, gold/purple circular warning buildup. Equal 256×256 cells. Alpha extrema 0–253; approximately 70% of source pixels have alpha <=10, with seams median alpha 0–1. Some warm color remains at very low alpha around effects. The painted warning disks are somewhat elliptical; the runtime geometric circular warning is the authoritative damage boundary.

`AtlasLayout.json` records exact source hashes, rectangles in top-left and Unity bottom-left conventions, pivots, alpha bounds, and unique frame counts. Do not pad or modify the source PNGs to force divisibility.

## Runtime

`WingedSentinelAttack` is ticked by its owner, with no Update method. Sweep locks a 6m circle at the sentinel; alternating overhead cleave locks a 3.4m circle up to 2.7m forward toward the player. Both give 1.8 seconds of windup before 0.8 seconds of attack animation; one 26-damage query occurs at 0.48 attack seconds, followed by 1.3 seconds of recovery and 1.65 seconds of cooldown. Movement only occurs while waiting, at 1.7m/s outside attack distance. The warning persists until the hit moment and does not track the player. Damage uses the shared dash/cover-aware actor gate. Cancellation clears the warning, owned painted effects and any pending hit. Phase, elapsed time, locked geometry and pending-hit state are public for checks.

Focused editor tests cover the locked six-metre warning, alternate forward cleave, cancellation disarming/destroying the warning, and minimum warning/recovery/cooldown windows. Root runs centralized Unity verification.

## Exact first body prompt

```
Use case: stylized-concept. Asset type: production transparent PNG sprite atlas for Unity isometric action game.
Reference image 1 is character design and painting style reference. Preserve this exact sentinel: tall lean muscular purple/slate cracked-skin male winged elf humanoid, swept-back black hair, long pointed ears, gold hoop earrings, gold arm bands/belt/bracelets, ivory cloth skirt, bare feet, huge ivory feathered wings and massive engraved ivory greatsword. Broad tactile oil brush strokes and rich painterly edges, warm highlights. No scenery.
Create EXACTLY 32 distinct sequential FULL BODY character poses on a perfectly regular FOUR COLUMN by EIGHT ROW atlas. Wide square cells with generous transparent gutters; each complete pose, wings, blade and feet strictly contained in its own cell with 12% empty margin. Stable scale, grounded foot anchor centered at 84% down each cell, same three-quarter camera, faces screen right throughout. Wings should be partially folded when necessary so no part ever crosses a cell boundary. No grids, labels, text or cast ground shadows. Real transparent alpha background.
Rows TOP TO BOTTOM, four successive animation frames left to right:
0 Idle: calm breathing, subtly lifting wings and sword.
1 Move: four distinct measured walking strides, advancing limbs, wing counterbalance.
2 Windup: begins bracing, draws huge sword back, unfolds wings, full tense sword-back anticipation.
3 Attack: four distinct stages of a huge horizontal greatsword sweep from back through front to extended follow-through. Painted blur may surround blade but still fully within cell.
4 Attack2: four distinct overhead great cleave stages, raises sword, apex, downward strike, deep low follow through.
5 Recover: four progressive reset poses, low blade to upright ready.
6 Hurt: impact recoil through pained contraction to recompose.
7 Defeated: progressive collapse to kneeling then fallen body with lowered folded wings and sword.
All 32 frames painted individually, no repeated stills. Prioritize complete clean readable silhouette and proper grid over tiny details. Portrait atlas with 4 square-wide cells and 8 square-high cells. Match reference silhouette and brushwork.
```

## Exact selected body prompt (one targeted retry)

```
Use case: stylized-concept. Asset type: transparent PNG game animation atlas.
Reference 1 is the design reference. Preserve its broad oil paint texture, purple/slate cracked-skinned lean male elf with black hair, gold hoop earrings and gold arm bands/belt, ivory skirt, large ivory feathered wings and ivory engraved greatsword. Three-quarter view facing screen right.
CRITICAL PRODUCTION LAYOUT: exactly FOUR columns and EIGHT rows, 32 separate full-body poses. Every cell includes a HUGE blank transparent moat: the character, wings, sword and painted motion MUST ALL fit inside only the central 66 percent width and central 72 percent height of each cell. Make the character small enough. NOTHING may enter the outer 17 percent left/right and 14 percent top/bottom. Fold wings to keep them inside. Keep sword diagonally inside, never horizontal across a neighboring cell. Complete feet, wing tips, sword tip always visible. Same character scale in every cell, foot anchor at 82 percent cell height. No background, no paper, true transparent alpha, no labels or grid.
Rows 0 through 7, TOP TO BOTTOM, each four genuinely successive poses left to right:
0 breathing idle with slight wing lift.
1 four walking stride positions.
2 four escalating slow sword windup positions.
3 four successive horizontal sword sweep positions, compact controlled wings.
4 four successive overhead great cleave positions.
5 four reset/recovery positions from low blade back to upright.
6 four hurt recoil positions.
7 four defeated positions: kneels, slumps, falls, fully fallen with wings folded.
Distinct painted animation, not duplicate stills. Perfectly regular 4×8 grid with unusually wide transparent gutters is the highest priority. Portrait canvas aspect ratio 1:2. No contact or overlap whatsoever between the 32 images.
```

## Exact effects prompt

```
Use case: stylized-concept. Asset type: transparent PNG combat effects sprite atlas for Unity.
Reference 1 is style and palette only: broad tactile oil paint, ivory sword/feathers, warm antique gold highlights, purple/slate shadows. Create effects only, no character.
EXACTLY SIX columns and FOUR rows, 24 genuinely distinct progressive painted animation frames. Perfectly regular cells. EVERY isolated effect must fit within central 70 percent of cell with huge transparent gaps on all four sides. Never overlap adjacent cells. No labels, text, borders, ground plane, paper or background. True transparent alpha.
Row 0 TOP: six successive giant ivory greatsword horizontal crescent sweep effects: small starting curved slash, widening arc, broad crescent, sweeping almost semicircle, fading broken crescent, tiny dissipating ivory/gold trails.
Row 1: six successive overhead great cleave effects: descending thin vertical ivory trail, thickening slash, explosive lower impact, outward ivory and gold brush burst, scattered flecks fading, sparse remnant.
Row 2: six successive sword impact bursts: compact star, growing angular ivory/gold painted burst, fullest burst with purple/slate chips, spreading fragments, fading fragments, sparse sparks.
Row 3 BOTTOM: six successive perfectly circular TOP DOWN magical warning shadow decals. Every disk is a true circle, same diameter and cell center, deep muted purple transparent fill with gold painted rim progressively brightening and becoming more urgent. Distinct progressive buildup, no ellipse or perspective; this is for ground projection.
Maintain six-by-four regular grid and wide gaps as highest priority; all 24 effects full and contained, no neighbor contamination. Landscape atlas 3:2 aspect ratio. Painterly oil brush marks, no vector slickness.
```

