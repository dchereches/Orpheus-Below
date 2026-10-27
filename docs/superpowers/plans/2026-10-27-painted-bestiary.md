# Painted bestiary implementation plan

> **For agentic workers:** Use subagent-driven-development for the eight independent creatures. Root owns shared code, integration and Unity verification.

**Goal:** Bring the supplied eight enemy character designs into actual combat with clean oil-painted animation and readable attacks.

**Architecture:** Retain the existing shade health/cadence/abduction systems and guardian duel phases. Replace their placeholder art and arena attacks through a shared sprite library/animator and cancellable per-creature attack patterns. Keep projectiles swept against solid cover; warnings must describe the actual damage area and disappear when interrupted.

**Tech stack:** Unity 6000.3.10f1, C#, built-in image generation, existing HDRP isometric/duel camera.

## User requirements and production choices

- Eight matching TEST sheets have filenames 20.18.00–20.18.01 and filesystem save time 21:39:06 (user referred to 21:38).
- Preserve each creature's silhouette, palette, costume and broad oil strokes. Generate real successive painted frames, never duplicate stills to inflate the count.
- Target 48 body frames and 24 effect frames per creature: idle, move, windup, primary, secondary, recover, hurt, defeated; six frames per row. Transparency, gutters and stable foot anchors are mandatory. Quality takes precedence over frame count.
- One separate subagent per creature, in waves of up to three active workers. Each owns only its own resource directory, attack-pattern source and focused test source. Root owns shared runtime, importer, existing combat files, route data and centralized Unity execution.
- Route choice: chamber 1 Tripod first boss; 2 Masked Monk mini-boss; 3 Mystic Eye Warden second boss; 4 Winged Sentinel mini-boss; 5 Shepherd mini-boss; 6 Cerberus; throne rescue remains chamber 7. Bosses remain pacified on ascent.
- Hooded Skeleton replaces caster visuals/AI; rooster rider replaces abductor visuals/AI on the escort ascent. Other ordinary shades may use these paintings with their existing appropriate combat roles.
- Cerberus: wait, deliberate bite windup/lunge and tail sweep. Monk: locked circular shadow, falling boulder, impact. Eye: actual painted thrown staff. Sentinel: slow sword sweep over warned area. Tripod: sonic waves that dash invulnerability can cross. Shepherd: warns/summons sheep that dash. Rooster: warns/dashes to Eurydice and drags her toward the deeper passage, interruptible through existing sanctuary/stun mechanics.

## Shared contracts

Namespace `OrpheusBelow`. `PaintedEnemyKind` names the eight creatures plus Sheep. `EnemyMotion` has Idle, Move, Windup, Attack, Attack2, Recover, Hurt, Defeated. `EnemyArtLibrary` supplies 48 body and 24 effect sprites; `EnemyArtAnimator` plays rows against attack duration and freezes on pause. `PaintedEnemyActor` owns facing, movement/collision, damage gating and the selected `PaintedAttackPattern`.

`PaintedAttackPattern.Initialize(actor)`, `Tick(dt)`, `Cancel()` form the module boundary. Patterns never update themselves; existing combat owners call them only when healthy, unstunned and in the appropriate phase. Actor exposes Art, Kind, GroundAnchor, StageCenter, IsBossStage, OwnerBoss, OwnerShade, Face, MoveToward and TryHitPlayer. Shared animated projectiles sweep their whole movement; shadow hazards are owned by the pattern and cancelled with it. Root integrates flute projectile clears and defeat cleanup.

## Tasks

- [x] Add failing asset/route tests and observe old placeholder art / missing minibosses.
- [x] Build shared art library, stable sprite importer, animator, actor, projectile and warning primitives.
- [x] Cerberus worker: reference-preserving animation, bite/tail pattern and focused checks.
- [x] Skeleton worker: gun animation, animated bullet/muzzle/impact and aiming/volley pattern.
- [x] Monk worker: summon animation, boulder/shadow/impact frames and cancellable hazard.
- [x] Eye worker: staff-throw/recovery animation and swept painted staff.
- [x] Sentinel worker: windup/sword-sweep animation and wide shadow.
- [x] Rooster worker: riding/dash/grab/retreat animation tied to existing abduction.
- [x] Tripod worker: wait/resonance/sonic attack animation and dash-through sonic waves.
- [x] Shepherd worker: bell/summon animation plus sheep dash frames and bounded summons.
- [x] Integrate regular enemies and six guardian encounters; preserve health, musical milestones and ascent behavior.
- [x] Bake/import all assets, rebuild affected saved layouts/scenes, run meaningful attack/cancellation/cover/pause/route tests and rendered attack checks.
- [x] Review, document provenance/prompts, frame counts and verification. Do not push unless requested.

## Review focus

Warnings must precede damage; damage may occur only once per hit; pause must freeze attacks and frames while stun/phase changes cancel pending attacks; summons/projectiles must stop on defeat; a player behind solid cover stays protected. Rooster abduction must target a real living companion, never spawn on the solo descent, and never move either actor through cover. Generated sprite cuts must contain exactly one complete pose and avoid neighboring cells.
