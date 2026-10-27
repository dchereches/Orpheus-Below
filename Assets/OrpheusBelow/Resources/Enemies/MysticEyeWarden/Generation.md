# Mystic Eye Warden painted production assets

Generated 2026-10-27 using the built-in image_gen tool. All image calls requested transparent_background=true. Final PNGs are unmodified copies of selected tool outputs; no alpha reconstruction, slicing, resampling, compositing, or pose duplication was performed.

Reference: D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (2).jpeg
Preserved design: heavy dark hood and stone eye helmet, long rust-orange eye-talisman veil, gold eye-crowned staff, broad oil brushwork.

## Layout

Body.png: 4 columns × 8 rows = 32 separately painted progressive body poses. Top-to-bottom: Idle, Move, Windup (staff aim), Attack (staff throw), Attack2 (staff recall), Recover, Hurt, Defeated. Left-to-right chronological frames. Quality-first reduction from the requested six columns avoids crowding the staff silhouette. Use proportional/rounded grid boundaries and alpha trim for the original dimensions. Foot anchors should use per-frame painted bottom, then normalize visible-height consistently.

Effects.png: 6 columns × 4 rows = 24 effect frames. Top-to-bottom: physical rotating gold eye staff outbound, physical reverse-rotating gold eye staff recall, eye impact burst, charging eye sigil. Left-to-right chronological frames. Empty regions contain RGB background values but alpha zero; rendering and trim MUST respect alpha.

## Selected output sources

Body source (4×8 generation): C:/Users/asus/.codex/generated_images/01a17ef4-08d8-7c51-8c2e-22b0f2ec88c1/exec-9be55c8d-5387-472d-904a-519b52dbf7d2.png
Final body spacing edit: C:/Users/asus/.codex/generated_images/01a17ef4-08d8-7c51-8c2e-22b0f2ec88c1/exec-4782eead-7c5b-4365-bda3-21bc305ac8dd.png
Effects source: C:/Users/asus/.codex/generated_images/01a17ef4-08d8-7c51-8c2e-22b0f2ec88c1/exec-3cfaf73b-eff0-486f-84c8-faab2a13b72a.png
Final effects alpha extraction edit: C:/Users/asus/.codex/generated_images/01a17ef4-08d8-7c51-8c2e-22b0f2ec88c1/exec-f83c51b7-2816-438b-bb8f-9d62591c1f8b.png

## Attack sequence

1. Wait 1.5 seconds.
2. Lock player position/direction and aim staff for 1.05 seconds. Charging effect row 3.
3. Release actual painted staff (effects row 0); shared projectile sweeps against solid cover and respects dash immunity. Speed 12, radius .32, damage 20.
4. Throw follow-through .28 seconds; outbound .85 seconds maximum (early recall on collision).
5. Recall .65 seconds using effects row 1 as a harmless return visual, followed by .9 seconds recovery.
6. Cancel removes staff, charge, recall and pending throw, then restores full wait. Parent combat owner cancels on stun/defeat/phase change.

## Exact final body generation prompt

```text
Use case: stylized-concept. Production transparent game animation sprite atlas.
Image 1 is the sole visual design/style reference for the Mystic Eye Warden. Preserve EXACTLY the ragged heavy charcoal-black hooded cloak, angular dark stone eye helmet with gold eye marks, long orange parchment veil painted with red eye talismans, slender gold staff with a large unmistakable EYE-shaped crown. Broad expressive oil-paint strokes, dark stony warm palette. Full body front three-quarter view, consistently facing slightly left.
CRITICAL ATLAS GEOMETRY: exactly FOUR COLUMNS and EIGHT ROWS, 32 individual progressive poses in a regular uniform grid. Tall portrait canvas with width:height ONE TO TWO, e.g. 1536 by 3072. All cells SQUARE. Every single cell must have a broad completely transparent empty moat on every side. Confine each whole sprite including all cloak tips, staff and arm extensions to the middle 68% width and middle 78% height of its cell. Top margin 10%; feet baseline 88%. All frames SAME SCALE. No pose touches a cell boundary. NO CONNECTING STROKES. Weapon throw uses compact torso poses so staff stays entirely inside each cell. If necessary make ALL sprites smaller together to satisfy gutters.
Rows TOP TO BOTTOM, each FOUR progressive frames LEFT TO RIGHT:
0 Idle, four successive breathing/robe/veil sway poses, staff upright.
1 Move, four consecutive walking/hovering gait frames, staff carried, veil trails.
2 Windup, four progressively deliberate AIMING poses, lift staff then draw arm back toward a compact overhead throw.
3 Attack, four actual STAFF THROW frames: first compact overhead staff pose, second release staff held close overhead; third and fourth empty-handed throw follow-through. Each entire staff fits within its OWN cell, never across cell edge.
4 Attack2, four recall frames: first empty hand beckons, second empty hand reaches, third catches staff close to body, fourth rests staff upright.
5 Recover, four successive robe settlement and upright staff recovery poses.
6 Hurt, four distinct short recoil then straighten poses.
7 Defeated, four successive sinking, kneeling and collapsed cloak poses, feet baseline remains 88%, staff lowered beside body within cell.
Genuinely transparent alpha background. No background paint, ground, shadow, scenery, paper, checkerboard, text, labels, lines, frame borders, numbers, logos, UI, watermark, blue bars, duplicates, clipped limbs/staff or overlaps. Clean individual complete painted sprites, ample invisible space between them.
```

## Exact body spacing edit prompt

```text
Edit this exact animation sprite atlas for production slicing. Preserve all 32 distinct painted character poses and their 4-column by 8-row motion order, artwork style, and alpha transparency. Re-layout ONLY: shrink every sprite uniformly to SIXTY-FIVE PERCENT of its present size and place each in the precise CENTER of its equal-sized grid cell. Canvas ratio exactly 1:2 with 4 columns and8rows of SQUARE cells. This creates a HUGE transparent moat around each isolated figure. Each sprite including extended weapon MUST occupy at most 60% cell width and 65% cell height. Anchor feet at 80% cell height, fixed scale throughout. Do NOT stretch the robe, staff, or head. Particularly keep throwing staff poses in row3 and collapsed pose bottom-right within their own square cell. No two sprites touch or overlap, no stray alpha from adjacent sprite crosses cell boundaries. True transparency around sprites, no background, no ground, no labels, no grid. Use the existing 32 poses, preserve their oil painting details, just smaller and centered with more spacing.
```

## Exact final effects generation prompt

```text
Transparent game sprite atlas. This image must have a TRUE ALPHA TRANSPARENT BACKGROUND, absolutely zero background paint. Do not render gold fog or brown background. Empty space is 0 alpha. No atmosphere, NO GLOW, no blurry ambient halo, no gradients, no floor, no shadows.
Use reference image 1 ONLY for the gold EYE-shaped staff design and chunky oil brushwork. Exactly 6 columns by 4 rows on a 3:2 landscape canvas, 24 isolated sprites in equally-sized square cells. Sprites must be SMALL, each confined to central 60% of cell, empty transparent 20% margins around all sides. Never touch cells. All shapes crisp enough to cut out cleanly. Antique gold physical paint, charcoal shaft.
Row0: six individual full-length physical gold EYE-crowned staffs spinning like a boomerang, one staff per cell, rotations 0,60,120,180,240,300 degrees. EYE crown has large round pupil and ornamental corners. Actual thin long weapon, no beam. Full staff fits each square cell.
Row1: same six individual full-length EYE-crowned staffs rotating back 300,240,180,120,60,0 degrees, with only three tiny golden paint flecks beside each. No smoke and no halo. Entire weapon fits in each square.
Row2: six successive physical golden-brushstroke impact bursts on transparency: tiny gold star, growing short gold star, gold eye burst, fading gold eye burst, scattered paint flecks, few tiny flecks. No background.
Row3: six successive small antique gold eye-shaped magic symbols made only of solid oil-paint strokes: dim gold thin eye, brighter thin eye, wider open eye, thick gold eye, eye with a few short stroke rays, open eye settled. No ambient glow at all. Only solid visible painted shapes, transparent between all strokes.
No characters, text, labels, borders, grid, numbers, scenery, watercolor washes, blue bars, laser, background. True transparent PNG.
```

## Exact effects background extraction edit prompt

```text
Use case: background-extraction. Edit the supplied image. REMOVE ALL of the brown/gold backdrop and ambient fog/lighting completely, replacing it by TRUE ZERO-ALPHA TRANSPARENCY. Preserve only the 24 physical gold staff and painted gold magic symbol sprites in exactly their current positions, sizes, grid, colors and shapes. Do not move or redraw or add any sprites. The entire background and space around and BETWEEN every separate painted stroke must be alpha 0, including centers of eye rings. No brown canvas, no black backdrop, no checkerboard illustration, no gold atmosphere, no floor. This is a production game sprite atlas. Output transparent PNG.
```

