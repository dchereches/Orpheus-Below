# Tripod generation and combat notes

- Built-in image_gen reference generation with transparent_background=true.
- Reference: D:\Serban\TEST\WhatsApp Image 2026-10-27 at 20.18.01 (5).jpeg, inspected before generation.
- Body.png: unchanged generated RGBA PNG, 887 x 1774 pixels, 4 columns x 8 rows, 32 distinct frames.
- Effects.png: unchanged generated RGBA PNG, 1536 x 1024 pixels, 6 columns x 4 rows, 24 distinct frames.
- One body spacing retry was used because the first square output compressed the eight rows. The selected portrait output preserves the complete silhouette. No manual pixel edits, resampling, alpha extraction, or frame duplication.
- AtlasLayout.json records explicit pixel rectangles and pivots. Cuts follow transparent separators where a stroke extends beyond the ideal grid; the first effect row ends at pixel y=258 rather than 256. Body pivots use the nominal column center and actual painted foot baseline, stabilizing feet without shifting pixels.
- Alpha verification: every selected body frame has at least 80.3% clear pixels (alpha below 10), every selected effect has at least 47.5%. All 32/24 frame hashes are distinct; no empty frame (minimum painted pixels at alpha above 100: body 4504, effects 2214). Fully transparent RGB may retain generated color; alpha is preserved unchanged.

Body rows from top: Idle, Move, Windup (resonating head), Attack (single woof), Attack2 (double pulse), Recover, Hurt, Defeated.
Effects rows from top: sonic ring, echo/second wave, impact, double-ring charge halo.

Combat: TripodAttack is driven only through PaintedEnemyActor.Tick, with no pattern Update. Wait 1.5 s; committed 1.25 s windup; alternate single and double casts; double pulse gap 0.8 s; follow-through 0.3 s; recovery 1.1 s. Speed 8, damage 20, radius 0.7, center height 0.8, lifetime 3.5 s. Boss-stage direction follows IsoRightXZ toward the player and is committed at windup; room mode aims in the ground plane. Every pulse starts from the actor's live transform. The shared swept projectile provides cover collision and dash immunity. Cancel clears all tracked waves and the charge halo, discarding the pending second pulse; phase, count, pending-pulse and active-wave diagnostics are public.

Tests added: warning/dash-gap invariants; cancellation with two waves and a pending pulse; live-position projectile origin and ownership. Unity test execution is reserved for the parent integration agent.

Generated sources:
- Body selected: C:\Users\asus\.codex\generated_images\01a17efb-094d-7022-891e-0cdf807ba22c\exec-a3dade3c-8da3-4aca-9908-9413fe64a04b.png
- Effects selected: C:\Users\asus\.codex\generated_images\01a17efb-094d-7022-891e-0cdf807ba22c\exec-e0250b25-8a9b-46cd-8323-ad6ba28796f3.png

SHA256:
- Body.png: 3e55236f825fec8edb1f43b8b66994cb7afc8fed87f33511f0fe98a1a61414b6
- Effects.png: 4051e4990940fa04376423a824f4ea95c57076d6269fab2b2bb7ec4ac483288b

## Final body prompt

Use case: stylized-concept. Production game sprite atlas. Input is character/style reference. Generate a PORTRAIT 1:2 aspect image, 1024 pixels wide and 2048 pixels high, EXACTLY FOUR columns and EIGHT rows of equal SQUARE cells (32 frames). TRUE transparent background. This is a spacing-critical retry: all creatures MUCH SMALLER within their own square cell, at most 65 percent cell height and 65 percent cell width, centered horizontally; feet aligned 82 percent down each cell, with at least 15 percent clear transparent margin above the tallest horn and below feet and to both sides. Whole creature visible in every frame, NEVER touch or cross a cell edge; no clipping at top of canvas. Entire subject identical size in every cell. Reference ivory three-stilt-legged creature, dark narrow angular torso, long needle legs, hollow DOUBLE-CIRCLE ring head with long side horns and ivory shoulder bone. Preserve empty transparent ring holes. Thick broad oil-painted ivory/ochre brush strokes and slate-brown shadows exactly like reference. All facing right 3/4 view. Top-to-bottom rows each four distinct frames: 0 Idle gentle head pulse, 1 Move three-stilt walking cycle, 2 Windup head rings resonate and lean back with braced legs, 3 Attack sonic woof body thrust and settle without drawn effects, 4 Attack2 double pulse body thrust twice without drawn effects, 5 Recover stance and head settle, 6 Hurt sharp recoils, 7 Defeated slowly crumpling through bent legs to compact wreck contained inside its cell. No background, ground, shadows, labels, text, grid lines, effects, extra characters, decorative frame. Prioritize full subject silhouette and wide transparent gaps.

## Effects prompt

Use case: stylized-concept. Asset type: Unity production game VFX sprite atlas with genuine transparency. Input image is reference for brushwork and ivory/ochre palette only. Create exactly 24 isolated painted effects in an EXACT regular SIX columns by FOUR rows grid. Landscape canvas, equal square cells. No creature and no environment. Every effect fully contained in inner 76 percent of cell, large transparent gutters on every side, centered consistently, never cross cell boundaries. Broad thick oil painted brush strokes, ivory/ochre resonance with muted slate-brown edges, handmade textured broken strokes, no neon, no vector gradient, no text or grid lines. Each row is a six-frame left-to-right animation: TOP ROW 0 one vertical sonic shockwave ring, coherent hollow oval viewed side-on 3/4 facing right, grows from compressed luminous seed to large ivory ring then thins, MAX diameter 75 percent cell, rings should be legible at small game scale. ROW 1 second sonic wave, same ivory vertical oval ring with a smaller echo behind it, pulses through six stages and fades; ROW 2 sonic impact burst, dense small ivory seed then broad radiating brushstrokes then breaking ivory dust and fade, contained in its cell; BOTTOM ROW 3 resonating charge halo, hollow double ivory rings like the reference creature's double-circle head, six progressive slight vibrations and warm intensification. Empty transparent alpha inside all hollow rings. No bodies, feet, shadows, scenery, cell borders, labels, decorative frame. True transparent canvas and wide transparent separation.

