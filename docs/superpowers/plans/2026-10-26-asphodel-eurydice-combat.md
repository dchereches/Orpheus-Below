# Asphodel, Eurydice, and Combat Clarity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a high-resolution Asphodel vertical slice with animated Eurydice guidance and tougher, more readable combat.

**Architecture:** Keep the runtime-built Unity prototype architecture. Add one pure tutorial-progress model, expose input-agnostic action counters from the existing gameplay components, add a focused Eurydice sprite animator, and let the chamber director coordinate the HUD bubble and wave gate. Preserve procedural visual fallbacks when generated assets cannot be loaded.

**Tech Stack:** Unity 6.0, C#/.NET Standard 2.1, UGUI, Unity Test Framework/NUnit, HDRP sprite materials, PNG resources.

**Spec:** `docs/superpowers/specs/2026-10-26-asphodel-eurydice-combat-design.md`

## Global Constraints

- Preserve keyboard/mouse, controller, microphone, and ESP32 input paths.
- Keep the deterministic cadence unlock order and current camera architecture.
- The Asphodel background is 3840x2160 and imported at up to 4096 pixels.
- Eurydice uses one transparent 4x4 sheet: idle, glide, teaching, shatter.
- The tutorial runs once per play session and advances from demonstrated gameplay actions.
- Enemy telegraphs remain at least as readable as the current 0.34-second lunge warning.
- Existing procedural Eurydice visuals remain as a missing-asset fallback.

## Review Focus

- Non-keyboard Soft/Hard notes must increment the same counters and complete tutorial steps.
- Re-entering Chamber 0 must not restart the tutorial or permanently suppress Wave 1.
- Missing/unreadable sprite resources must leave the procedural Eurydice visible without exceptions.
- Screen edges and non-1920x1080 resolutions must keep the tutorial bubble on screen.
- Game-over, victory, and guardian-duel transitions must hide tutorial UI and clear the teaching pose.

---

### Task 1: Input-Agnostic Tutorial Progress

**Files:**
- Create: `Assets/OrpheusBelow/Scripts/UI/AsphodelCombatTutorial.cs`
- Create: `Assets/OrpheusBelow/Tests/Editor/AsphodelCombatTutorialTests.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Player/OrpheusPlayerController.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Player/OrpheusLyreCombat.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Player/OrpheusFluteChannel.cs`

**Interfaces:**
- Produces: `AsphodelTutorialStep`, `AsphodelTutorialSnapshot`, and `AsphodelCombatTutorial.Observe(AsphodelTutorialSnapshot)`.
- Produces: `OrpheusPlayerController.DashUseCount`, `OrpheusLyreCombat.SoftNoteCount`, `OrpheusLyreCombat.HardBoomCount`, and `OrpheusFluteChannel.StanceEntryCount` read-only counters.
- Consumes: gameplay action paths already centralized in each component; no direct key polling.

- [ ] **Step 1: Write the failing progression tests**

Add NUnit tests named `RequiresMeaningfulMovementBeforeDash`, `AdvancesOnlyOnMatchingActionCounter`, `CompletesAfterFluteEntry`, and `IgnoresPreTutorialActionCounts`. Assert the exact order Move -> Dash -> SoftNote -> HardBoom -> Flute -> Complete and a movement threshold of 1.25 world units.

- [ ] **Step 2: Run the EditMode tests and verify failure**

Run Unity batch mode with `-runTests -testPlatform EditMode -testFilter OrpheusBelow.Tests.AsphodelCombatTutorialTests`. Expected: compile failure because the tutorial types do not exist.

- [ ] **Step 3: Implement the pure progress model**

Create `AsphodelCombatTutorial` with constructor `AsphodelCombatTutorial(AsphodelTutorialSnapshot baseline, Vector3 startPosition)`, property `AsphodelTutorialStep CurrentStep`, property `bool IsComplete`, and method `bool Observe(AsphodelTutorialSnapshot snapshot, Vector3 playerPosition)`. Counter comparisons are relative to the constructor baseline.

- [ ] **Step 4: Add action counters at centralized success points**

Increment `DashUseCount` only when a dash actually begins, note counters only when their corresponding combat action fires regardless of input source, and `StanceEntryCount` only on entering Flute stance.

- [ ] **Step 5: Run the focused tests and verify pass**

Run the same Unity EditMode test filter. Expected: all tutorial progression tests pass.

- [ ] **Step 6: Commit**

Commit message: `feat: track input-agnostic combat tutorial progress`.

### Task 2: Eurydice Tutorial Bubble and Wave Gate

**Files:**
- Modify: `Assets/OrpheusBelow/Scripts/UI/OrpheusHud.cs`
- Modify: `Assets/OrpheusBelow/Scripts/World/OrpheusPrototypeBuilder.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Companion/BlurredEurydice.cs`
- Modify: `Assets/OrpheusBelow/Tests/Editor/AsphodelCombatTutorialTests.cs`

**Interfaces:**
- Consumes: Task 1 tutorial model and action counters.
- Produces: `OrpheusHud.ShowEurydiceTutorial(string)`, `ShowEurydiceHint(string, float)`, and `HideEurydiceTutorial()`.
- Produces: `BlurredEurydice.SetTeachingPose(bool)` for Task 3's animator.

- [ ] **Step 1: Add failing lifecycle tests**

Add tests for completing only once, retaining completion on Chamber 0 re-entry, and producing the exact five step identifiers used by the director.

- [ ] **Step 2: Run tests and confirm the new lifecycle assertions fail**

Use the focused Unity test command from Task 1. Expected: failure on missing completion-latch behavior.

- [ ] **Step 3: Build the screen-clamped HUD bubble**

Add a non-raycast UGUI panel, tail, speaker label, and serif body text. Position it each frame from Eurydice's world position with `RectTransformUtility.ScreenPointToLocalPointInRectangle`; clamp to the canvas rectangle with 32-pixel margins.

- [ ] **Step 4: Gate Asphodel Wave 1 behind the tutorial**

Increment `PrototypeBuildVersion` to 6. On first Chamber 0 entry, snapshot counters/position and delay `SpawnWaveWithRunesRoutine` until the tutorial reaches Complete. Show concise copy for Move, Dash, Soft Note, Hard Boom, and Flute; call `SetTeachingPose(true)` only while a bubble is active.

- [ ] **Step 5: Add contextual Warden and Abductor hints**

Show one timed hint when each archetype first spawns or latches on. Do not restart the gated tutorial, overwrite boss UI, or consume input.

- [ ] **Step 6: Hide correctly across terminal states**

Hide the bubble during guardian activation, game-over, victory, and after tutorial completion; clear teaching pose in the same paths.

- [ ] **Step 7: Run focused tests and verify pass**

Expected: all tutorial tests pass.

- [ ] **Step 8: Commit**

Commit message: `feat: let Eurydice teach Asphodel combat`.

### Task 3: High-Resolution Eurydice Sprite Animation

**Files:**
- Create: `Assets/OrpheusBelow/Resources/Characters/Eurydice/Eurydice_DarkCrimson_4x4.png`
- Create: `Assets/OrpheusBelow/Resources/Characters/Eurydice/Eurydice_DarkCrimson_4x4.png.meta`
- Create: `Assets/OrpheusBelow/Scripts/Companion/EurydiceSpriteAnimator.cs`
- Create: `Assets/OrpheusBelow/Tests/Editor/EurydiceSpriteAnimatorTests.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Companion/BlurredEurydice.cs`

**Interfaces:**
- Produces: `EurydiceAnimationState { Idle, Glide, Teaching, Shatter }` and `EurydiceSpriteAnimator.SetState(EurydiceAnimationState)`.
- Produces: static `Sprite[] EurydiceSpriteAnimator.SliceSheet(Texture2D texture, int columns = 4, int rows = 4, float pixelsPerUnit = 256f)`.
- Consumes: `BlurredEurydice.SetTeachingPose(bool)` from Task 2.

- [ ] **Step 1: Generate and inspect the project-bound transparent sheet**

Use the supplied reference as a style/identity reference and the approved 4x4 row contract. Save the selected 2048x2048 transparent PNG in the target resource folder. Verify true alpha, four equal rows/columns, stable foot placement, no text/borders, and no cropped hair or dress.

- [ ] **Step 2: Write failing slicing and fallback tests**

Test 16 sprites, correct bottom-to-top Unity texture row mapping, equal 512x512 rectangles for a 2048 sheet, state row selection, and graceful empty output for null or non-divisible textures.

- [ ] **Step 3: Run the focused animator tests and confirm failure**

Run Unity EditMode tests filtered to `EurydiceSpriteAnimatorTests`. Expected: compile failure because the animator does not exist.

- [ ] **Step 4: Implement slicing and state animation**

Load `Characters/Eurydice/Eurydice_DarkCrimson_4x4`, slice deterministically, play at 8 fps using unscaled time, and expose the state enum. Shatter plays once and holds its last frame; other rows loop.

- [ ] **Step 5: Integrate with BlurredEurydice**

Create the generated-sprite renderer above the existing ghost layers. Select Idle/Glide from follow velocity, Teaching from tutorial/hum state, and Shatter on loss. Disable only the procedural sharp head/robe when loading succeeds; keep all procedural visuals when it fails.

- [ ] **Step 6: Run animator and tutorial tests**

Expected: all focused EditMode tests pass.

- [ ] **Step 7: Commit**

Commit message: `feat: animate Eurydice from dark crimson sprite sheet`.

### Task 4: 4K Asphodel and Harder Encounters

**Files:**
- Modify/replace: `Assets/OrpheusBelow/Resources/ChamberBackgrounds/Chamber_00.png`
- Modify: `Assets/OrpheusBelow/Resources/ChamberBackgrounds/Chamber_00.png.meta`
- Modify: `Assets/OrpheusBelow/Scripts/World/ProceduralChamberGenerator.cs`
- Modify: `Assets/OrpheusBelow/Scripts/World/UnderworldParallaxRig.cs`
- Modify: `Assets/OrpheusBelow/Scripts/Combat/UnderworldEntities.cs`
- Create: `Assets/OrpheusBelow/Tests/Editor/AsphodelDifficultyTests.cs`

**Interfaces:**
- Produces: `ShadeBalance.Get(ShadeArchetype)` returning exact health/speed/damage/cooldown values used by `ShadeEnemy` and tests.
- Consumes: the same `Chamber_00` texture for exploration and guardian parallax.

- [ ] **Step 1: Enhance and inspect the Asphodel source image**

Use the existing image as the edit target, preserve landmarks and framing, and generate a 3840x2160 detailed enhancement. Save non-destructively during review, then replace `Chamber_00.png` only after visual validation.

- [ ] **Step 2: Write failing balance and composition tests**

Assert Wretch 86 HP/18 damage/0.68s recovery, Caster 78 HP/17 orb damage/1.12s cooldown, Warden 150 HP/24 damage, Abductor 82 HP, lunge telegraph 0.34s, Wave 1 count 5, and Wave 2 count 5.

- [ ] **Step 3: Run focused difficulty tests and confirm failure**

Run Unity EditMode tests filtered to `AsphodelDifficultyTests`. Expected: failures showing current lower values and smaller waves.

- [ ] **Step 4: Centralize and apply difficulty values**

Add immutable `ShadeBalance` records/structs and use them in `SetupArchetype`, melee damage, caster projectiles, and cooldowns. Keep existing movement speeds unless the spec fixes a value.

- [ ] **Step 5: Expand Asphodel waves**

Add a third Wretch to Wave 1 and add one Caster plus one Abductor to Wave 2 while keeping spawn offsets on the visible bridge corridor and the Warden in Wave 2.

- [ ] **Step 6: Apply the 4K background across all Asphodel phases**

Set importer max size to 4096, disable destructive downscaling, and use high-quality compression/no compression as visually appropriate. Confirm the vista remains active during both waves and is reused as the rhythm duel's far background without an opaque procedural sky covering it.

- [ ] **Step 7: Run all EditMode tests**

Expected: tutorial, animator, and difficulty suites all pass.

- [ ] **Step 8: Commit**

Commit message: `feat: sharpen Asphodel and raise encounter pressure`.

### Task 5: Integration Verification and Handoff

**Files:**
- Modify: `HANDOFF.md`

**Interfaces:**
- Consumes: all prior tasks.
- Produces: verified build/test evidence and updated asset/runtime documentation.

- [ ] **Step 1: Run the full Unity EditMode suite**

Run Unity batch mode with `-runTests -testPlatform EditMode` and save the XML/log under a temporary verification directory outside `Assets`. Expected: zero failing tests and no compile errors.

- [ ] **Step 2: Run a Unity batch compile/import pass**

Open the project with `-batchmode -quit` and inspect the Editor log. Expected: clean import, no missing script or texture exceptions.

- [ ] **Step 3: Inspect final raster assets**

Verify background dimensions 3840x2160, sheet dimensions 2048x2048, PNG alpha on the sheet, and no LFS pointer text remains in either working file.

- [ ] **Step 4: Perform Chamber 0 smoke validation**

Verify first-entry tutorial gating, each action path, Wave 1/2 spawning, armor break, Flute stance, Abductor hint, boss transition, and Eurydice shatter. If headless execution cannot validate visuals, open the project in Unity and record the manual checks that remain.

- [ ] **Step 5: Update the handoff**

Document build version 6, generated asset paths, tutorial behavior, revised balance, and verification commands/results.

- [ ] **Step 6: Review repository diff and commit**

Confirm no unrelated changes or generated Unity cache folders are included. Commit message: `docs: update Asphodel vertical slice handoff`.
