# Orpheus Broad-Oil Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the geometric Orpheus placeholder with an enemy-scale broad-oil sprite character, extensive directional fighting/singing animations, painted effects, and screenshot-inspired chamber lighting and shadows.

**Architecture:** A saved `OrpheusAnimationLibrary` owns variable-count clips and direction metadata. A deterministic `OrpheusAnimationBrain` selects facing/state while `OrpheusSpriteAnimator` plays body/effect frames and exposes gameplay event entry points; `OrpheusGroundShadow` and `OrpheusOilSurface` handle grounding and chamber light. Existing player, lyre, flute, and journey code supply triggers without changing combat timing or balance.

**Tech Stack:** Unity 6000.3.10f1, C#, NUnit EditMode tests, HDRP SpriteRenderer shaders, transparent PNG atlases generated with the built-in image tool.

**Spec:** `docs/superpowers/specs/2026-10-28-orpheus-broad-oil-animation-design.md`

## Global Constraints

- Preserve the newest private character reference's slim young Orpheus, ragged black-brown cloak, off-white shirt, charcoal trousers, oxblood sash, wrapped boots, and warm-gold lyre.
- Do not copy either `D:\Serban\Test` JPEG into the repository.
- Standing visible height is 3.9 world units; collider, movement, reach, damage, and input remain unchanged.
- Player-controlled actions use eight real directions; passive/cinematic reactions may use four authored directional families with safe mirroring.
- Runtime art uses transparent backgrounds, stable foot pivots, simple masses, and long broad oil streaks.
- Neutral atlases receive chamber-aware rim/shadow color at runtime; no fixed orange rim is baked into every frame.
- Existing unrelated modified `.meta` files are preserved; no commit or push is performed unless requested.

## Review Focus

- Missing or partially imported atlases must fall back to the procedural Orpheus without null references or invisible gameplay.
- Rapid aim/movement disagreement must not flicker direction or restart the active attack clip.
- Damage, fatal look-back, and flute interruption must override singing/cosmetic states immediately.
- Paused menus must freeze frames and effects; chamber teleport must clear one-shots and motion history.
- Long shadows must remain beneath actors/cover and use the fallback direction when chamber metadata is absent.

---

### Task 1: Animation contracts and state selection

**Files:**
- Create: `Assets/OrpheusBelow/Scripts/Player/OrpheusAnimationLibrary.cs`
- Create: `Assets/OrpheusBelow/Scripts/Player/OrpheusAnimationBrain.cs`
- Create: `Assets/OrpheusBelow/Tests/Editor/OrpheusAnimationTests.cs`

**Interfaces:**
- Consumes: `OrpheusPlayerController.MoveInputXZ`, `AimDirectionXZ`, `IsDashing`, game-over/victory state, and existing `EurydiceFacing` eight-sector convention.
- Produces: `OrpheusAnimationState`, `OrpheusAnimationLayer`, `OrpheusAnimationLibrary.Frame(state, facing, frame)`, `FrameCount`, `Fps`, `Loops`, and `OrpheusAnimationBrain.Evaluate(OrpheusAnimationInputs, float)`.

- [ ] **Step 1: Write failing contract tests**

Add tests named `LibraryDeclaresEveryApprovedMotion`, `ControlledActionsRequireEightDirections`, `VisibleHeightMatchesHumanoidEnemies`, `PriorityDefeatHurtAttackMusicDashLocomotionIdle`, `FacingUsesAimDuringCombatAndMotionOtherwise`, `FacingHysteresisPreventsBoundaryFlicker`, and `TeleportResetReturnsIdleWithoutRestartNoise`.

- [ ] **Step 2: Run focused tests and verify failure**

Run Unity batch EditMode tests filtered to `OrpheusAnimationTests`; expect compile failures because the library and brain do not exist.

- [ ] **Step 3: Implement contracts and deterministic selection**

Define the complete named state enum from the approved spec, direction requirements, priority, clip metadata, one-shot completion, eight-sector conversion, and hysteresis. Keep this task independent of renderers and generated assets.

- [ ] **Step 4: Run focused tests and verify pass**

Expected: all pure animation contract/state tests pass.

### Task 2: Sprite playback, fallback, and gameplay event API

**Files:**
- Create: `Assets/OrpheusBelow/Scripts/Player/OrpheusSpriteAnimator.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Player/OrpheusPlayerController.cs`
- Modify: `Assets/OrpheusBelow/Tests/Editor/OrpheusAnimationTests.cs`

**Interfaces:**
- Consumes: Task 1 library/brain and a body/effect `SpriteRenderer` pair.
- Produces: `TriggerSoftAttack(int)`, `TriggerHardBoom()`, `TriggerCadence(OrpheusAnimationState)`, `EnterFlute()`, `TriggerFluteNote(bool)`, `TriggerDiscord()`, `TriggerRequiem()`, `TriggerHit(bool)`, `TriggerLookBack(bool)`, `TriggerVictory()`, `ResetMotion()`, `Advance(float, bool)`, and `UsesProductionArt`.

- [ ] **Step 1: Add failing playback/fallback tests**

Cover frame advancement, loop/non-loop endings, identical-state no restart, pause freeze, teleport reset, one-shot priority/interruption, missing-library procedural fallback, and 2D boss-stage side facing.

- [ ] **Step 2: Run focused tests and verify failure**

Expect missing animator/event API failures.

- [ ] **Step 3: Implement minimal animator and player visual replacement**

Create body/effect renderers under the existing billboard root. Hide the geometric cloak/head/lyre only when `Library.IsComplete`; otherwise preserve the exact current fallback. Keep the aim indicator separate.

- [ ] **Step 4: Run focused tests and verify pass**

Expected: playback, priority, pause, reset, and fallback tests pass.

### Task 3: Wire combat, singing cadence, aulos, damage, and narrative triggers

**Files:**
- Modify: `Assets/OrpheusBelow/Scripts/Player/OrpheusLyreCombat.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Player/OrpheusFluteChannel.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Player/OrpheusPlayerController.cs`
- Modify: `Assets/OrpheusBelow/Tests/Editor/OrpheusAnimationTests.cs`

**Interfaces:**
- Consumes: Task 2 event methods.
- Produces: event calls aligned with existing damage windows and cadence/flute state; no gameplay timing changes.

- [ ] **Step 1: Add failing integration tests**

Test combo indices 1-3, hard Boom, dash strike, three cadence finishers, cadence-window sung sustain/resolve, flute entry, correct note, wrong-note backlash, requiem, breath exhaustion/recovery, light/heavy damage, fatal look-back, abduction grief, chamber clear, and victory.

- [ ] **Step 2: Run focused tests and verify failure**

Expect missing trigger calls or incorrect states.

- [ ] **Step 3: Wire existing gameplay callbacks**

Call the animator at the same points that currently execute attacks/effects. Sustain singing only while the existing cadence buffer is active; resolve on completion, timeout, interruption, or unrelated hard release.

- [ ] **Step 4: Run focused tests and verify pass**

Expected: all integration tests pass without changing combat assertions elsewhere.

### Task 4: Layered ground shadow and chamber lighting

**Files:**
- Create: `Assets/OrpheusBelow/Scripts/Player/OrpheusGroundShadow.cs`
- Create: `Assets/OrpheusBelow/Scripts/Player/OrpheusOilSurface.cs`
- Create: `Assets/OrpheusBelow/Resources/Art/OrpheusOilSurface.shader`
- Modify: `Assets/OrpheusBelow/Scripts/World/PaintedChamberDefinition.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Editor/DescentChamberCatalog.cs`
- Modify: `Assets/OrpheusBelow/Resources/ChamberLayouts/Chamber_00.asset` through `Chamber_07.asset`
- Modify: `Assets/OrpheusBelow/Tests/Editor/OrpheusAnimationTests.cs`

**Interfaces:**
- Consumes: animator state, velocity/dash, `PaintedChamberDefinition.dominantLightDirection`, `dominantLightColor`, and `dominantLightIntensity`.
- Produces: contact, ellipse, and long cast-shadow layers plus material property block values for warm rim/cool shadow.

- [ ] **Step 1: Add failing lighting/shadow tests**

Test authored metadata serialization, absent-metadata fallback down-left, normalized cast direction, dash compression, attack widening, opacity clamping, sorting below actors, property-block isolation, and no transform/pivot mutation.

- [ ] **Step 2: Run focused tests and verify failure**

Expect missing metadata/components/shader contract.

- [ ] **Step 3: Implement metadata, shadow component, and surface shader helper**

Use three child ground decals. Keep source alpha and geometry unchanged; apply only RGB rim/shadow/bounce in the sprite rectangle.

- [ ] **Step 4: Run focused tests and verify pass**

Expected: lighting/shadow contract tests pass.

### Task 5: Generate and install broad-oil runtime atlases

**Files:**
- Create: `Assets/OrpheusBelow/Resources/Characters/Orpheus/BroadOil/*.png`
- Create: `Assets/OrpheusBelow/Resources/Characters/Orpheus/BroadOil/Generation.md`
- Create: `Assets/OrpheusBelow/Resources/Characters/Orpheus/BroadOil/GenerationPrompts.json`
- Create: `Assets/OrpheusBelow/Resources/Characters/Orpheus/BroadOil/AtlasLayout.json`
- Create: `Assets/OrpheusBelow/Scripts/Editor/OrpheusBroadOilAssetBuilder.cs`
- Modify: `Assets/OrpheusBelow/Tests/Editor/OrpheusAnimationTests.cs`

**Interfaces:**
- Consumes: both private reference images only through the built-in image tool; Task 1 state/direction contract.
- Produces: transparent body/effects atlases and `Resources/Characters/Orpheus/BroadOil/Library.asset` with 3.9-unit visible height.

- [ ] **Step 1: Generate separate transparent atlases**

Generate locomotion, dash/traversal, soft combat, heavy/cadence, lyre singing, aulos, hurt/recovery, narrative, and painted-effects sheets. Preserve the exact character anchor in every call and save every final under runtime Resources.

- [ ] **Step 2: Inspect every output**

Reject sheets with identity drift, armor/sword additions, incorrect lyre side, duplicated front views in rear rows, clipped feet/props, backgrounds, labels, or fine noisy paint.

- [ ] **Step 3: Write failing real-asset tests**

Assert all approved state/direction combinations resolve, alpha contains transparent and opaque pixels, safe gutters prevent neighboring contamination, foot pivots are stable, visible height is 3.9±0.08, and the total installed frame count is at least 600.

- [ ] **Step 4: Implement the editor baker**

Slice with alpha-aware measured cells, variable motion counts, bottom-support pivots, source hashes, and recorded layout metadata. Do not modify generated PNG bytes.

- [ ] **Step 5: Bake and run focused tests**

Run `OrpheusBroadOilAssetBuilder.Build` in Unity batch mode, then `OrpheusAnimationTests`; expect all real-asset checks pass.

### Task 6: Runtime verification, previews, and documentation

**Files:**
- Create: `Assets/OrpheusBelow/Scripts/Editor/OrpheusAnimationVerification.cs`
- Create: `docs/orpheus-broad-oil-animation-guide.md`
- Modify: `HANDOFF.md`
- Output: `Logs/OrpheusBroadOil/*`

**Interfaces:**
- Consumes: all prior tasks.
- Produces: deterministic in-engine captures, verification logs/results, GIF/MP4 preview when local encoders are available, and final implementation record.

- [ ] **Step 1: Build a deterministic verification harness**

Capture representative frames for every state family, all eight controlled directions, bright/dark chamber lighting, fallback light direction, pause, 2D boss stage, and procedural fallback.

- [ ] **Step 2: Run focused and full verification**

Run the focused suite, full EditMode suite, batch project import/compile, animation harness, and the existing journey verifier. Distinguish the four documented baseline failures from new regressions.

- [ ] **Step 3: Visually inspect captures**

Confirm enemy-scale readability, stable feet, correct rear views, broad oil streaks, unclipped props, attack anticipation/contact, visible singing, shadow grounding, cover depth, and no orange-light mismatch in dark rooms.

- [ ] **Step 4: Fix regressions and rerun affected verification**

No new compile, test, runtime, import, or visual regression remains.

- [ ] **Step 5: Document final assets and evidence**

Record atlas/frame counts, exact prompts, paths, trigger table, verification commands/results, known unrelated failures, and preview locations.
