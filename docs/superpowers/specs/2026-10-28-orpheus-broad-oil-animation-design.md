# Orpheus broad-oil character and animation design

## Intent

Replace the current geometric placeholder Orpheus with a production-scale, fully painted and extensively animated protagonist. He should read instantly as the young, worn Orpheus in the newest character reference while matching the broad-oil cast, the physical size of the humanoid enemies, and the dramatic firelight and long shadows of the newest combat reference.

The two private source references are the newest files found in `D:\Serban\Test` on 28 October 2026:

- `WhatsApp Image 2026-10-28 at 16.12.58.jpeg`: identity, costume, physique, lyre, and broad-oil character reference.
- `WhatsApp Image 2026-10-28 at 16.14.51.jpeg`: isometric framing, orange fire rim-light, dense contact shadows, long cast shadows, and combat readability reference.

The private JPEGs will not be copied into the repository. Generated runtime art, generation prompts, slicing metadata, code, tests, and previews will live in the project.

## Visual target

Orpheus remains a slim, young mortal rather than a conventional armored hero. Preserve the character sheet's tousled dark hair, tired expressive face, loose off-white shirt, charcoal trousers, scuffed wrapped boots, ragged black-brown cloak, muted oxblood sash, leather straps, and old warm-gold lyre. The cloak and lyre are the primary silhouette cues. Do not add armor, a sword, a heroic cape, polished ornament, or a bodybuilder physique.

The paint treatment uses deliberately simple masses and long loaded oil streaks. Large strokes should describe the cloak, shirt, limbs, and hair; small noisy facets, photoreal pores, smooth airbrushing, and over-rendered fabric detail are anti-goals. Faces remain legible but are secondary to silhouette at gameplay scale. The lyre is a simple, unmistakable gold shape rather than intricate jewelry.

Orpheus targets **3.9 world units of visible standing height**, matching the project's standard large humanoid enemies (Masked Monk, Mystic Eye Warden, and Shepherd at 3.96) and clearly exceeding Euridice's 2.5274-unit silhouette. He remains smaller than the Winged Sentinel, Cerberus, and Tripod. This is a rendering-scale change only: movement, collider radius, reach, damage, and balance remain unchanged unless a visual test exposes an actual mismatch.

All frames use stable foot-support pivots, transparent backgrounds, consistent proportions, and real rear/three-quarter views. No frame may fake a rear direction by mirroring a front pose. Left/right mirroring is acceptable only for short reactions whose asymmetric lyre placement is not visible; locomotion and combat retain correct lyre/strap continuity.

## Animation set

The library is intentionally generous. The production target is roughly **600-800 usable frames** across compact transparent atlases, with variable frame counts per motion rather than forcing every clip into the same grid. This larger target is a deliberate builder call in response to “too many animations rather than too few.”

Full eight-direction artwork is required for every player-controlled motion: locomotion, dash, all active attacks, cadence finishers, lyre singing, and aulos performance. Passive narrative reactions, grounded hurt, defeat, and recovery may use four authored directional families with careful mirroring when the lyre/strap silhouette stays coherent. This keeps control-facing actions exact without multiplying purely cinematic poses that are never steered.

### Directional locomotion and traversal

Eight-direction art is required for the core movement set:

1. Quiet idle: breathing, cloth settling, occasional blink.
2. Restless idle: weight shift and hand near the lyre.
3. Walk.
4. Run.
5. Start/accelerate.
6. Stop/settle.
7. Directional turn.
8. Dash start, travel, and settle.
9. Dash-strike transition.
10. Stair ascent.
11. Stair descent.

The runtime may synthesize turn/settle selection from neighboring directional frames, but the source library preserves distinct start, stop, dash, and stair motion so movement never looks like a sped-up walk.

### Fighting and damage

Combat clips are driven by existing gameplay events:

1. Soft lyre strike 1: fast horizontal painted sound cut.
2. Soft lyre strike 2: returning diagonal cut.
3. Soft lyre strike 3: committed finishing sweep.
4. Soft note bolt release.
5. Hard Boom windup.
6. Hard Boom release and recoil.
7. Dash strike.
8. Hymn of Dawn beam cast.
9. Sanctuary of Euridice cast.
10. Siren's Convergence cast.
11. Light hit/stagger.
12. Heavy hit/knockdown.
13. Grounded hurt.
14. Recovery/rise.
15. Defeat/look-back collapse.

Attack anticipation and contact frames must be readable at game scale. Oil streak effects extend the action direction without hiding the body. Gameplay timing remains authoritative: animation follows attack windows and never delays damage.

### Singing and instruments

Singing is visible, not merely implied by generic attack poses. The face lifts, chest opens, and free hand shapes the phrase while the lyre or aulos remains physically coherent.

1. Lyre draw/ready.
2. Soft pluck with sung phrase.
3. Sustained singing loop.
4. Full-voice release.
5. Phrase resolve/lower lyre.
6. Aulos draw.
7. Aulos stance loop while moving slowly.
8. Correct aulos note pulse.
9. Wrong-note discord/backlash.
10. Cataclysmic Requiem finale.
11. Breath exhausted.
12. Breath recovery.

Existing soft/hard pulses, cadence completions, flute stance, correct/wrong note, requiem, and breath state provide the triggers. A separate unused “sing” button is not introduced; singing enriches the existing musical combat actions. After a soft note, Orpheus enters the short sung sustain while the cadence buffer remains active (up to the existing 1.35-second cadence window). The next note transitions directly into its pluck/chord. Cadence completion, buffer timeout, damage, dash, flute entry, or a hard release with no continuing cadence ends the sustain and plays phrase resolve. An isolated note therefore remains a short self-contained sung phrase, while an entered cadence reads as one connected performance.

### Narrative and contextual reactions

1. Chamber entrance/reform.
2. Alert/listen.
3. Inspect/interact.
4. Look-back temptation.
5. Fatal look-back realization.
6. Euridice abducted grief.
7. Chamber clear relief.
8. Final victory.

These clips may use fewer directions and controlled mirroring where the lyre remains consistent. Fatal, hurt, and active combat states override cosmetic reactions.

### Painted effects

Effects are kept in a separate transparent atlas so body art stays readable:

- ivory-gold pluck arcs and note bolts;
- thick orange-gold Boom crescents;
- a narrow gold-white cadence beam wake;
- sanctuary ribbons;
- inward violet-gold convergence strokes;
- cool cyan aulos breath rings;
- red-black discord fractures;
- dash after-streaks and restrained foot dust;
- defeat and reform fragments.

Effects use a few large brush ribbons, never confetti, glitter clouds, or dense particle noise.

## Lighting and shadows

The combat screenshot is a mood and readability reference, not a background to reproduce. Orpheus receives warm orange rim light on the fire-facing side, a cooler near-black shadow mass on the opposite side, and a restrained warm bounce on the white shirt and lyre. The source palette remains recognizable in quieter chambers.

Runtime atlases use neutral directional paint lighting rather than baking a fixed orange edge into every frame. A chamber-aware material adds the warm rim, cool shadow mass, and restrained bounce at runtime. This is a builder call so Orpheus remains correct when the dominant light moves or a chamber is not fire-lit.

The current circular blob shadow is replaced visually by a layered painted ground shadow:

- a dark compact contact patch directly beneath both feet;
- a broad soft ellipse grounding the full enemy-scale silhouette;
- a longer low-opacity directional cast shadow oriented away from the chamber's dominant light direction;
- dash compression/stretch and brief attack widening, without detaching the shadow from the feet.

The long shadow participates in the existing painted depth ordering and never draws above characters or cover. `PaintedChamberDefinition` gains explicit dominant-light metadata: a normalized image/screen-space direction, warm rim color, and intensity. This is authored once for each of the eight chamber layouts rather than inferred from decorative scene lights. Where metadata is absent, the fallback direction is down-left in screen space, matching the visual reference. Shadow opacity is strong enough to anchor Orpheus on bright floors but clamps in dark rooms.

## Runtime architecture

Follow Euridice's proven pattern while keeping player priorities separate:

- `OrpheusAnimationLibrary`: saved sprite arrays, measured pivots, per-state frame counts, looping/FPS metadata, and visible height.
- `OrpheusAnimationBrain`: deterministic priority and eight-sector facing selection using ground-plane motion and aim direction.
- `OrpheusSpriteAnimator`: timed playback, attack-event entry points, effect atlas playback, pause behavior, and stable frame changes.
- `OrpheusGroundShadow`: contact/ellipse/cast layers driven by facing, velocity, dash, and attack state.
- editor baker/import verification: alpha-aware per-cell slicing with recorded atlas layout and source hashes.

The existing procedural cloak/head/lyre remains as a fallback only when the production library cannot load. When the library is present, one body SpriteRenderer and one effect SpriteRenderer replace the visible placeholder stack. The aim indicator remains separate and unchanged.

Animation priority, highest first:

1. defeat/fatal look-back;
2. heavy hurt/grounded/recovery;
3. requiem and cadence finishers;
4. active attack and dash strike;
5. flute/singing channel;
6. dash;
7. chamber/narrative reaction;
8. stairs;
9. start/run/walk/stop/turn;
10. idle variants.

Pause menus freeze playback. Deliberate unscaled-time narrative sequences may continue only where the existing game already continues them. Teleports reset motion history. Identical state/facing input does not restart a clip. Movement and aim use hysteresis to prevent directional flicker.

## Integration triggers

- Player ground velocity selects start, walk, run, stop, and idle.
- `AimDirectionXZ` controls combat facing; movement direction controls non-combat facing.
- dash start/end and post-dash strike windows select dash clips.
- each soft/hard input, combo index, and cadence completion selects its matching clip.
- flute enter/exit, correct note, wrong note, requiem, and damage interruption select the musical clips.
- `TakeDamage`, Styx hazard, look-back progress/loss, Euridice abduction loss, chamber clear, teleport, and victory select reactions.
- the 2D boss stage uses the same library with side-facing selection rather than a separate character design.

## Generation and asset policy

Use the built-in image generation tool with the two newest local images loaded as references:

- Image 1, character reference: identity, costume, proportions, lyre, and broad-oil stroke language.
- Image 2, combat reference: lighting, shadow direction, isometric readability, and effect restraint.

Generate transparent runtime atlases rather than labeled concept boards. Keep exact prompts in a project JSON or Markdown record. Each final atlas is visually inspected for identity, directional correctness, stable scale, foot alignment, transparent alpha, neighboring-frame contamination, and prop continuity. Rejected variants remain outside runtime Resources.

## Verification and deliverables

Automated EditMode coverage will check:

- library completeness and every state's frame count;
- true alpha and safe frame gutters;
- visible height near 3.9 units and stable ground pivots;
- eight-direction selection and facing retention;
- state priority, non-restarting loops, pause behavior, and teleport reset;
- attack/flute/cadence/damage/look-back/victory trigger wiring;
- procedural fallback when assets are absent;
- shadow layering, direction fallback, and depth order;
- compatibility with both 2.5D chambers and the 2D boss stage.

An in-engine verifier will capture representative idle/walk/run/dash, all fighting clips, singing/aulos clips, hurt/recovery, look-back, victory, and the layered shadow under both bright firelight and dark chamber lighting. A GIF or MP4 preview and a compact animation guide will be saved under the project logs/docs. Existing unrelated dirty `.meta` files are preserved.

## Non-goals

- No combat rebalance, new input scheme, new weapon, or new ability.
- No redesign of Euridice, enemies, chamber geometry, or HUD.
- No copying of the private reference JPEGs into the repository.
- No photoreal character cutout, skeletal 3D rig, or pixel-art conversion.
- No tiny decorative animation that compromises silhouette clarity, timing, or memory without visible gameplay value.

## Builder calls left open

The builder may choose atlas dimensions, exact per-clip frame counts, texture packing, and whether closely related clips share a sheet, provided the full named motion set remains available and slicing stays deterministic. The builder may tune FPS and brief transition durations to match existing gameplay timing without changing that timing.
