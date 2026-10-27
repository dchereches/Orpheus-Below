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

