# Asphodel, Eurydice, and Combat Clarity Design

## Intent

Make the opening Asphodel section feel like a polished, high-resolution vertical slice. The newly added Asphodel artwork should define the entire chamber visually, Eurydice should appear as the supplied dark-crimson character rather than a procedural placeholder, and she should teach the unusual music-based combat through short in-world speech bubbles. The encounter should become harder without becoming opaque or unfair.

## Goals

- Preserve the composition and palette of the existing Asphodel painting while producing a sharp 4K background asset.
- Keep the painting visible and spatially consistent during exploration, both shade waves, camera transitions, and the Cantor of Fury duel.
- Replace Eurydice's procedural core shapes with an animated transparent sprite derived from the supplied reference image.
- Teach movement, dash, Soft Note, Hard Boom/armor breaking, and Flute pacification through staged Eurydice dialogue that waits for demonstrated actions.
- Increase enemy durability, pressure, and encounter composition while retaining readable telegraphs and viable non-lethal musical play.
- Preserve keyboard/mouse, controller, microphone, and ESP32 input paths.

## Non-goals

- Rebuilding the other seven chambers or generating their background plates.
- Replacing Orpheus, enemy, or guardian procedural visuals.
- Changing the deterministic cadence unlock order or adding random boons.
- Turning the tutorial into a permanent modal help screen.
- Reworking the core camera, hardware sensor protocol, or input architecture.

## Visual Direction

### Asphodel

The existing 1024x576 `Chamber_00.png` is the source of truth. Produce a 3840x2160 enhanced version that preserves the same isometric bridge, portals, pillars, flower growth, distant ruins, violet-black stone, and warm gold seams. Enhancement may add clean texture and edge detail but must not move landmarks, add text, or alter the playable composition. Unity should import it at up to 4096 pixels with high-quality settings. The same sprite is used as the exploration vista and the far plate of the rhythm-duel parallax rig. Procedural foreground effects may remain, but they must tint and frame the painting rather than cover it.

### Eurydice

Generate one transparent 4-by-4 sprite sheet with four equal-width animation rows and four frames per row:

1. Floating idle: subtle breathing, hair drift, and hem lift.
2. Following glide: forward motion with trailing hair and dress.
3. Teaching gesture: calm hand/arm movement suitable for speech bubbles and acoustic replies.
4. Dissolve/shatter: body separating into crimson-gold spectral motes.

The sheet should keep the reference's long dark-crimson hair, black sleeveless dress, youthful adult proportions, mournful expression, and refined high-resolution pixel-art finish. It must have true transparency, consistent scale and foot placement, no captions, borders, portrait panels, weapons, or floor tiles. The game remains camera-facing, so additional directional rows are unnecessary.

At runtime Eurydice uses idle while stationary, glide while following or being pulled, teaching during tutorial bubbles and hum replies, and shatter for the look-back/abduction loss. Her existing soul tether, blur layers, focus mechanic, sanctuary ring, and gameplay behavior remain.

## Tutorial Flow

The tutorial runs once per play session when Chamber 0 begins. It is implemented as a small state machine owned by the chamber director/HUD, with a world-anchored but screen-clamped bubble visually associated with Eurydice. It must not depend on a hard-coded screen resolution.

Enemy spawning is held until the basic movement step is complete. Each step contains one or two concise lines and advances after the player demonstrates the action rather than merely waiting:

1. Move: ask Orpheus to step forward; complete after meaningful movement.
2. Dash: explain the two dash charges and invulnerability; complete after a dash.
3. Soft Note: identify tap LMB/J or a sub-60% sensor pulse; complete after a Soft Note.
4. Hard Boom: identify RMB/K, held LMB, or a 60%+ sensor pulse and explain bronze armor; complete after a Hard Boom.
5. Flute: introduce F/controller trigger, weakened-shade requirement, and the displayed four-note phrase; complete after entering Flute stance.

After the introductory sequence, Wave 1 begins. Contextual bubbles can briefly reinforce armor breaking when the first Warden appears and Eurydice protection when the first Abductor latches on. Prompts auto-hide when complete, do not consume gameplay input, and disappear during the 2D guardian duel, game-over, or victory overlays.

## Difficulty Changes

Difficulty should come from longer engagements, slightly denser waves, and more frequent decisions—not invisible rules or shortened warning windows.

- Increase Wretch health from 62 to 86, Caster health from 54 to 78, Warden health from 105 to 150, and Abductor health from 58 to 82.
- Increase Wretch/Warden contact damage from 15/20 to 18/24 and caster orb damage from 14 to 17.
- Reduce post-lunge recovery from 0.82s to 0.68s and caster volley cooldown from 1.38s to 1.12s.
- Keep the 0.34s melee telegraph and make its visual pulse more legible rather than shortening it.
- Raise Asphodel Wave 1 from four to five shades and Wave 2 from three to five, with the Warden introduced only after the player has learned Hard Boom.
- Preserve hard counters: Hard Boom breaks armor, wall slams retain bonus damage, Flute remains effective after weakening, and Sanctuary remains the intended answer to abductors after it unlocks.

## Architecture

- Add a project-local Eurydice sheet under `Assets/OrpheusBelow/Resources/Characters/Eurydice/` with deterministic import metadata.
- Extend `BlurredEurydice` to load/slice the 4-by-4 sheet at runtime, animate the active row, and fall back to the existing procedural silhouette if the asset cannot be loaded.
- Add observable combat/tutorial events or lightweight counters to the existing player/combat classes instead of polling raw keys, so microphone and ESP32 actions also satisfy tutorial steps.
- Extend `OrpheusHud` with the Eurydice bubble and public methods for showing/updating/hiding it.
- Add the tutorial state machine to `HadesChamberDirector` and gate Chamber 0's first wave until the intro sequence completes.
- Increment `PrototypeBuildVersion` so an existing scene is rebuilt with the new runtime setup.
- Update the Asphodel texture importer for 4K use and ensure the same high-resolution sprite remains bound to the boss parallax rig.

## Verification

- Add focused EditMode tests where practical for tutorial progression, sheet slicing/row selection, and difficulty constants.
- Compile the Unity project in batch mode and run available EditMode tests.
- Play-test Chamber 0 enough to verify the background across both perspectives, all tutorial completion paths, first and second waves, Warden armor, Flute entry, Abductor warning, Eurydice animation switching, and loss shatter.
- Inspect the final background and sprite sheet directly for resolution, transparency, grid consistency, and preserved visual identity.

## Builder Calls

- Exact tutorial prose, animation frame rate, bubble easing, and minor color values may be tuned by the implementer as long as the flow and visual intent above remain intact.
- If automated Unity execution is unavailable, compile/test as far as the installed editor and command-line environment allow and report the limitation explicitly.
