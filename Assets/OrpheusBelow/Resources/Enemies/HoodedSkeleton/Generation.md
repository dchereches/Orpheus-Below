# Hooded Skeleton painted assets

Generated with the built-in image_gen tool on 2026-10-27, using genuine transparency. Generated PNGs are copied unchanged; no programmatic duplicate, mirrored, shifted, or recolored animation frames.

Reference: D:\Serban\TEST\WhatsApp Image 2026-10-27 at 20.18.01.jpeg

## Body.png

48 individually painted frames, 6 columns by 8 rows, top-to-bottom: Idle, Move, Windup, Attack, Attack2, Recover, Hurt, Defeated. Frames read left-to-right. Original output: C:\Users\asus\.codex\generated_images\01a17ee2-b2be-7e53-bf02-5f753cb4e425\exec-cc59437f-9ea0-404c-a21e-00fa4377e06a.png

### Exact generation prompt

Use case: identity-preserve. Asset type: production Unity 2D game body animation sprite atlas.
Input image 1 is the CHARACTER AND OIL-PAINT STYLE REFERENCE. Preserve exactly this hooded skeleton gunslinger: ivory weathered skull with deep black eyes, exposed bony ribs and skeletal hands, rust-brown hood and ragged cloak, torn brown trousers, antique dark iron revolver. No redesign.
Create one transparent PNG atlas with EXACTLY SIX COLUMNS and EIGHT ROWS, 48 distinct individually painted full-body animation frames total. Use a canvas aspect ratio of 3:4, ideally 1536x2048, with equal 256x256 cells. Read rows from top to bottom and frames left to right. No headings, text, row labels, grid lines, backgrounds, floor, scenery, border, shadows outside character. Genuine transparent alpha in all empty space.
Every cell contains the same character in fixed game-view three-quarter orientation facing screen RIGHT. Preserve consistent full body size, skull identity and revolver design, full hood to feet, with the feet anchored at the same lower-center point of every cell, generous completely transparent gutters, no clipping. Keep all motion inside each cell. Refined rough oil-painted brushwork matching input, warm desaturated russet, dark umber and bone ivory. 6 distinct progressive poses in EVERY row, no repeats or frozen duplicated frames.
Row 0 Idle: six subtle sequential breathing / cloak sway / bony fingers tightening poses, revolver lowered.
Row 1 Move: six sequential locomotion poses making a complete walk cycle with clear changing legs and cloak follow-through.
Row 2 Windup: six sequential deliberate gun-raise poses, starting gun down, shoulder and elbow rising, skull leaning toward target, ending revolver fully aimed right. Strong readable anticipation.
Row 3 Attack: six sequential single-shot animation poses: steady aim, trigger/muzzle flash, sharp wrist recoil, upper-body recoil, wrist returns, aim steadies. Small oil-painted flash belongs only to the firing poses.
Row 4 Attack2: six sequential revolver fan-fire poses, free bony hand fanning hammer, alternating muzzle flash and recoils, visible progression.
Row 5 Recover: six sequential lowering and settling poses from gun up to lowered idle, cloth momentum.
Row 6 Hurt: six sequential hit reaction poses: torso snaps back, ribs hunch, cloak whips, regain footing, recover upright.
Row 7 Defeated: six sequential fall poses: knees buckle, skull drops, collapse descending, body falls, crumples, final skeletal heap and cloak on ground. Keep each full pose inside its cell. Final heap is lower in cell.
No other characters, no additional weapons, no painterly parchment backdrop, no opaque checkerboard.

## Effects.png

24 individually painted frames, 6 columns by 4 rows, top-to-bottom: bullet flight, muzzle flash, impact, gun smoke. Frames read left-to-right.

### Exact generation prompt

Use case: stylized-concept. Asset type: production Unity 2D oil-painted effects sprite atlas for a rust-hooded skeleton revolver gunslinger.
Input image 1 is a STYLE AND WEAPON REFERENCE only: match the rough impasto oil painting, warm dusty ochre light, charcoal gun metal and umber smoke of this character's antique revolver. Do not draw the character.
Create one genuinely transparent PNG with EXACTLY SIX equal COLUMNS and FOUR equal ROWS, 24 distinct sequential animation frames total. Landscape aspect 3:2, ideally 1536x1024 with equal square cells. Read rows top-to-bottom and frames left-to-right. Paint the actual effects; no circles, balls, flat geometric icons, no placeholders. No headings, no labels, no grid lines, no background, no terrain or ground. All unused pixels genuinely transparent. Each frame centered in its own cell with substantial transparent gutters and no overlap or clipped edges.
Row 0: six progressive frames of an antique revolver bullet flying RIGHT: small dark pointed iron / brass slug pointing right, warm ochre painted speed-streak trailing LEFT, each frame subtly changes the dusty spark tail's contour and length. Maintain slug center consistently in each cell. Long horizontal bullet silhouette, not a glowing orb.
Row 1: six sequential muzzle flash frames, center anchored at left-middle: ignition spark, narrow flare, expanding jagged golden shot blast, broad lopsided burst, shrinking hot flecks, fading smoke wisps. Six visibly different stages.
Row 2: six sequential impact frames: first chip spark, angular yellow sparks splitting, dust impact puff, radial rough debris, fading gray umber debris, remaining small smoke flecks. Six visibly different stages.
Row 3: six sequential soft umber gun smoke puff frames: small fresh curl, expanding curled cloud, larger dispersing cloud, broken translucent ribbons, thin drifting wisps, sparse fading remnants. Six visibly different stages.
Hand-painted natural brush edges in the exact rough oil medium of reference, ochre / pale gold muzzle blast with burnt umber smoke. No character, no gun, no opaque checkerboard texture.

### Exact transparency correction prompt

undefined

Final effects output copied unchanged from C:\Users\asus\.codex\generated_images\01a17ee2-b2be-7e53-bf02-5f753cb4e425\exec-7b164d1d-6046-48d6-824b-2d9ad0a325f9.png. Effects dimensions: 1536 x 1024 (256 x 256 cells). Body dimensions: 1086 x 1448 (181 x 181 cells). Both are 32-bit RGBA PNGs. Alpha checks confirmed fully transparent empty pixels, including canvas corners and inter-cell gutters. RGB in transparent areas may retain generated backdrop colors; alpha is preserved unchanged.

## Combat and playback contract

HoodedSkeletonAttack uses 0.68 seconds of committed gun-raise aim, alternating one shot and a three-shot fan at -14/0/+14 degrees. Firing recoil lasts 0.30 seconds, gun-lowering recovery lasts 0.36 seconds, and shot cooldown is 1.6 seconds from firing. Initial ready delay is 0.65 seconds. Idle cycles use 0.8 seconds; walking uses 0.65 seconds. The actor retreats below 4.8 units and pursues above 8, starting aim only within 14 units. Bullets use the six genuinely painted flight frames in effects row 0; shared projectile collision spawns row 2 impacts. Attack emits row 1 muzzle flash (0.18 seconds) and row 3 smoke (0.55 seconds).

Verification: all 48 body cells and all 24 effects cells contain visible alpha and have distinct SHA-256 hashes when independently read as PNG regions. No frame was synthesized by copying, mirroring, shifting, tinting, or scaling another. Unity compilation and shared combat integration belong to the parent task and were not run in this module task.

