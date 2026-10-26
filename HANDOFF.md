# Orpheus Below — Project Architecture & AI Hand-off Guide

## 1. Project Overview & Repository
- **Repository Root:** `D:\Serban\software-projects\Orpheus-Below`
- **GitHub Remote:** `https://github.com/dchereches/Orpheus-Below.git` (`main` branch)
- **Engine & Version:** Unity 6 (`6000.3.10f1`), High Definition Render Pipeline (HDRP `17.3.0`), New Input System (`1.18.0`), `.NET Standard 2.1`.
- **Core Premise:** A distinctive mythic Hellenic chamber-crawler based on **sound, breath, and grief**. Orpheus descends into the Underworld with his Lyre and Flute (Aulos) to lead Eurydice to the upper light. Features a dual-perspective camera system, zero RNG stat boons, procedural 2.5D background-plate chamber generation, and deterministic musical progression.
- **Current Build Version:** `HadesChamberDirector.PrototypeBuildVersion = 6` (auto-upgrades scenes on Play mode).

---

## 2. Dual-Perspective Gameplay Systems

### A. 2.5D Isometric Chamber-Crawler (`Pitch 36°, Yaw 45°`)
- **Movement:** 360° screen-aligned isometric WASD / Left Stick movement with snappy acceleration (`acceleration = 155`, `deceleration = 185`).
- **Double-Dash (`[Space]` / `[Left Shift]`):** 2-charge dash with full invulnerability frames (`i-frames = 0.185s`), after-image ghost trails, and ledge-forgiveness chasm vaulting over `IsoObstacleKind.DashableChasm`.
- **Lyre Combat (`OrpheusLyreCombat.cs`):**
  - **Soft Note (`S`, `< 60% intensity`):** 3-hit forward micro-step crescent slash + projectile note + acoustic ping to Eurydice. Upgrades to high-speed Dash-Strike when triggered out of a dash.
  - **Hard Boom (`B`, `>= 60% intensity`):** Radial shockwave + hitstop + Wall-Slam knockback (`+32` wall damage) + cracks bronze Grief-Armor. Driven by Piezo / BMP280 Breath / PC Mic / Mouse RMB / Key K.

### B. 2D Parallax Rhythm Guardian Duels (`Pitch 4.5°, Yaw 45°`)
- Smoothly glides camera from 2.5D Isometric down to a 2D side-view stage locked along `UnderworldCamera2D.IsoRightXZ`.
- Backed by a 5-layer procedural parallax backdrop (`UnderworldParallaxRig.cs`).
- Features a 3-line musical stave between Orpheus (Left) and Guardian (Right):
  - **Gold Diamond (`◇`)** = Soft Note (`< 60%`).
  - **Crimson Halo (`◎`)** = Hard Orphic Boom (`>= 60%`).
- Physical 2D floor hazards: sweeping shockwaves (`[Space]` dash i-frames) and Stygian geysers (`A/D` stepping).
- Defeating Guardians charms them into cathartic golden slumber rather than violent slaughter (`DefeatGuardian()` -> `PacifiedSpiritVessel`).

---

## 3. The Flute (Aulos) Stance & Melodic Flee Mechanic (`OrpheusFluteChannel.cs`)
Differentiates the game sharply from *Hades* — sound and music persuade enemies rather than purely slashing them:
- **Flute Stance (`[F]` key / Left Trigger):** Orpheus draws the Aulos panpipes.
  - Movement speed is slowed by 65% (`desiredVel *= 0.35f`).
  - Dashing is disabled (`_dashBufferTimer = 0f`).
- **4-Note Melody Challenge:** A hovering musical rune stave appears above Orpheus's head with a 4-note challenge (combinations of Soft `'S'` and Hard Boom `'B'`).
- **Discordant Backlash (A2):** Playing the wrong note, letting the timer expire, or taking enemy damage causes a sour screech (`PlayFluteBacklash()`), red shockwave, 0.4s stagger, 2.0s flute lockout, and **frenzies all nearby Shades within 8.5m** (1.45x speed, blazing crimson eyes, immediate lunge reset).
- **Dual-Instrument Synergy (A3):** Full-HP armored enemies resist the Flute. Orpheus must first use the Lyre to crack bronze Grief-Armor (`hasBronzeShield`) or reduce enemies below 70% HP.
- **Sorrowful Fleeing (A4):** On completing the 4-note melody, vulnerable Shades in earshot stop, squish into sorrow pose, turn translucent weeping cyan-silver, and sink into the floor mist in place (`WeepingShadeFleeSink`), clearing them from the wave.

---

## 4. Deterministic Chord Cadences & Unlock Ceremonies
Sequences of Soft (`S`) and Hard Boom (`B`) notes woven within `1.35s` trigger mythic abilities (no stat boon menus!):
1. **`S - S - B` — *Hymn of Dawn* (Tier 1, unlocked from start):** Fires a 15.5m golden resonance beam piercing all enemies and shattering enemy orbs for 68 damage.
2. **`B - S` — *Sanctuary of Eurydice* (Tier 2, unlocked after Chamber I):** Wraps Eurydice in a 4.5s Golden Aegis, repels Mourning Abductors, and triggers her reassuring vocal hum.
3. **`S - B - S` — *Siren's Convergence* (Tier 3, unlocked after Chamber II):** Conjures an inward-pulling harmonic vortex 4.2m ahead that clusters Shades, breaks Grief-Armor, and deals 38 damage.
- **Unlock Ceremony (`OrpheusHud.ShowCadenceUnlockCeremony`):** Triggers slow-motion time dilation, golden expanding shockwaves, ceremonial harp/flute fanfare (`PlayCadenceUnlockFanfare()`), and a serif presentation card.

---

## 5. Procedural Chamber Generation & Asphodel Viaduct Integration (`ProceduralChamberGenerator.cs`)

### A. General Procedural Biome Pipeline
Implements **Approach A5**: 2D painted background plates aligned with the 2.5D isometric pitch (`36°`, `45°`) with procedurally stamped colliders, props, hazards, and encounters.
- **Automatic Upgrades:** `HadesChamberDirector.PrototypeBuildVersion = 6`. Pressing **Play** in Unity automatically rebuilds and upgrades any older scene in the hierarchy.
- **8 Underworld Biomes:**
  1. *Asphodel Viaduct* (Mini-Boss: Cantor of Fury) — **Active Painted Artwork!**
  2. *Vault of the Ferryman* (Heavy Abductor & Caster crossfire)
  3. *Chasms of Acheron* (Mini-Boss: Warden of the Oar + Dashable Chasms)
  4. *Phlegethon Lava Vaults* (Magma hazards + Obelisks)
  5. *Elysian Sepulchre* (Mini-Boss: Siren of Acheron + Dual Mirages)
  6. *Cocytus Frozen Tarn* (Frost chasms + Weeping Shades)
  7. *Lethe Oblivion Gate* (Elite Wardens + Abductor nests)
  8. *The Gates of Styx* (Final Boss: 3-Headed Cerberus)

### B. Chamber 0: Asphodel Viaduct Integration Details
The enhanced painted artwork (`Chamber_00.png`, 3840x2160) is fully integrated into the game world. The duplicate JPG was removed so the Resources lookup resolves one deterministic asset:
- **Camera-Facing 2.5D Vista Quad:**
  - Placed at `position = centerWorld + (0, 0.45, 0) - camForward * 5.0f` with exact rotation `Euler(36°, 45°, 0°)`.
  - Dimensions: `30.2f x 17.0f` (16:9 aspect ratio, orthographic size `8.5f`, `sortingOrder = -50`).
  - Because it matches the camera angle, upright painted columns, balustrades, and waterfalls render with 100% pixel fidelity without double-foreshortening distortion.
- **Walkable Corridor & Colliders:**
  - Bridge length runs along the `Z` axis (`-11.5m` to `+11.5m`), width along `X` (`-4.3m` to `+4.3m`).
  - Stone floor backing slab at `y = -0.6m`, size `(9.2m, 1.2m, 24.5m)` for physics raycasts and ground decals.
  - West balustrade collider at `x = -4.6m`, East balustrade collider at `x = +4.6m`.
  - South archway rim collider at `z = -12.5m`, North archway rim collider at `z = +12.5m`.
- **Combat Props & Hazards:**
  - Four stone pillars at `(±4.2m, 0, ±4.0m)` for Wall-Slam knockback combos (`+32` damage).
  - Destructible cracked obelisk at `(0, 0, 0.2m)` in bridge center.
  - Flanking abyss drop zones (`IsoObstacleKind.DashableChasm`) at `x = ±7.2m` where waterfalls plunge into Tartarus.
- **Spawn & Exit:**
  - Player Spawn: `(0, 0, -9.5m)` emerging through the bottom-right arched portal.
  - Chamber Exit Door: `(0, 0, +10.4m)` framed by the top-left distant arched portal.
- **Dual-Perspective Parallax Rig Binding:**
  - `rig.farAbyssSkySprite = vistaSprite`. When transitioning to the 2D Boss Duel (`Cantor of Fury`), the painted Asphodel Viaduct seamlessly shifts into the backdrop behind the musical stave.
  - When the full painted backdrop is assigned, procedural peaks, columns, and hanging chains are suppressed so they do not cover the artwork. The foreground frame remains for depth.
- **Multi-Tier Asset Loading Pipeline:**
  - `ProceduralChamberGenerator.LoadChamberTexture(int chamberIndex)`:
    1. Checks runtime registered dictionary (`CustomPlates[chamberIndex]`).
    2. Checks `Resources.Load<Texture2D>("ChamberBackgrounds/Chamber_XX")`.
    3. Direct disk byte-fallback (`Application.dataPath + "/OrpheusBelow/Resources/ChamberBackgrounds/Chamber_XX.png"` / `.jpg` via `Texture2D.LoadImage`). This guarantees instant zero-delay loading even before Unity generates `.meta` files.

### C. Asphodel Encounter Difficulty
- Both chamber waves now spawn **five Shades**.
- Wretch Lunger: `86 HP`, `18 contact damage`, `0.68s recovery`, `0.34s telegraph`.
- Brimstone Caster: `78 HP`, `17 orb damage`, `1.12s volley cooldown`.
- Armored Warden: `150 HP`, `24 contact damage`, `0.68s recovery`, `0.34s telegraph`.
- Mourning Abductor: `82 HP`.
- Melee telegraphs are wider, brighter, and pulse during wind-up. Increased durability and damage do not shorten the established reaction window.

---

## 6. Eurydice Psychological & Escort Mechanics (`BlurredEurydice.cs`)
- **Generated Runtime Sprite:** `Assets/OrpheusBelow/Resources/Characters/Eurydice/Eurydice_DarkCrimson_4x4.png` is a transparent 2048x2048 sheet sliced at runtime into four-frame Idle, Glide, Teaching, and Shatter rows by `EurydiceSpriteAnimator.cs` (8 FPS). The old procedural silhouette remains as a safe fallback if the resource cannot load.
- **Combat Tutorial:** On the first Asphodel entry in each runtime session, a screen-clamped speech bubble anchored to Eurydice teaches Move, Dash, Soft Note, Hard Boom, and Flute Stance in sequence. Progress comes from action counters rather than specific keyboard keys, so keyboard, gamepad, microphone, and hardware sensor inputs all work. Wave 1 is gated until the five actions are demonstrated.
- **Context Hints:** Eurydice explains bronze armor on the first Warden and warns about the first Mourning Abductor. Tutorial UI hides during game-over, victory, guardian, and 2D-stage transitions.
- **Soul Tether:** Glowing line renderer connecting Orpheus and Eurydice. Shimmers pale gold/silver normally; turns jagged crimson when grabbed.
- **Mourning Abductors:** Special cyan-crimson Shades that ignore Orpheus and drag Eurydice backward toward Tartarus. If `AbductionDanger01` hits 1.0, she is lost (`TriggerEurydiceAbductedLoss`).
- **Acoustic Call-and-Response:** Plucking a Soft note near Eurydice makes her hum back (`PlayEurydiceHumReply`) with a moonlit ripple.
- **Mirage Decoys (`EurydiceMirageDecoy`):** Deceptive Underworld silhouettes spawn in temptation zones to bait `[Hold Q]` (which is fatal). Plucking verifies the real Eurydice; hitting a mirage dispels the illusion without looking back.

---

## 7. Asset Checklist Needed from AI Art Generator / User

To replace procedural fallback blocks across other chambers with custom painted artwork:

### A. Tilted 2.5D Background Plates
- **Target Folder:** `Assets/OrpheusBelow/Resources/ChamberBackgrounds/`
- **Naming:** `Chamber_00.png` through `Chamber_07.png`
- **Current Status:**
  - `Chamber_00.png` (**Asphodel Viaduct**, 3840x2160) — **ACTIVE & INTEGRATED!**
  - `Chamber_01` to `Chamber_07` — Awaiting user paintings; currently use procedural stone mosaic fallbacks.
- **Resolution:** Prefer 4K 16:9 (`3840 x 2160`) PNG for full composite plates; the importer allows up to 4096px with no texture compression.
- **Perspective:** Top-down angled isometric floor (`36°` tilt, `45°` yaw).

### B. 2D Parallax Boss Duel Backdrops
- **Dimensions:** `1920 x 1080` PNG with transparency.
- **Layers per Biome:**
  1. `Far_Skyline.png` (distant Stygian peaks / smoky sky)
  2. `Mid_Archways.png` (colossal Greek pillars / crumbling ruins)
  3. `Near_Hazards.png` (glowing lava falls / ghostly wisps)

### C. Character & Enemy Sprites (Transparent PNGs)
- **Orpheus:** Run cycle, Dash silhouette, Lyre pluck pose, Flute playing pose, Hurt/Stagger pose.
- **Blurred Eurydice:** **ACTIVE** — dark-crimson 4x4 transparent sprite sheet with Idle, Glide, Teaching, and Shatter animations. Looking-back focus art can still be added as a future dedicated state.
- **Shades:**
  - *Wretch Lunger* (crimson agile ghoul, lunging pose)
  - *Brimstone Caster* (violet floating skull/sorcerer)
  - *Armored Warden* (heavy bronze-armored hoplite ghost)
  - *Mourning Abductor* (cyan-violet weeping wraith with reaching claws)
- **Guardians:**
  - *Alecto's Cantor* (fury priestess with spectral wings)
  - *Warden of the Oar* (spectral ferryman with massive oar)
  - *Siren of Acheron* (four-winged harpy siren)
  - *Cerberus* (three-headed brimstone hound)

---

## 8. Controls Reference
| Key / Input | Action |
|---|---|
| `W`, `A`, `S`, `D` | 360° Isometric Movement (A/D on 2D Boss Stage) |
| `Space` / `Left Shift` | 2-Charge Dash (Vaults Chasms, i-frames) |
| `F` / Gamepad LT | Toggle Flute (Aulos) Stance (Slows move, disables dash) |
| Tap `LMB` / `J` | Soft Note ('S', < 60% intensity / Eurydice hums back) |
| `RMB` / `K` / Hold `LMB` | Hard Orphic Boom ('B', >= 60% intensity / Cracks armor) |
| `S - S - B` | Cadence: Hymn of Dawn (Piercing Resonance Beam) |
| `B - S` | Cadence: Sanctuary of Eurydice (Golden Aegis) |
| `S - B - S` | Cadence: Siren's Convergence (Harmonic Vortex) |
| `Tab` | Cycle Lyre Input (Keyboard+Mouse / Microphone / ESP32) |
| Hold `Q` | Forbidden Look-Back (Fatal temptation!) |
| `H` | Toggle Controls Card Overlay |
| `R` | Restart Descent |

---

## 9. Verification & Next Steps

### Automated Verification (2026-10-26)
- Unity EditMode: **17/17 passed, 0 failed** on Unity `6000.3.10f1`.
- Unity batch import/compile: exited with code `0`; no C# compilation errors, missing-script errors, missing-reference exceptions, or texture-import exceptions were found in the log.
- Raster inspection: Asphodel background is a real `3840x2160` PNG; Eurydice sheet is a real `2048x2048` 32-bit PNG with alpha. Neither file is an unresolved Git LFS pointer.
- Automated coverage includes tutorial progression and session gating, animation slicing/frame selection, all four enemy balance profiles, and both five-enemy Asphodel waves.

### Manual Play-Mode Smoke Checklist
The headless checks cannot judge composition, bubble readability, or animation feel. In `SampleScene`, verify: the first-entry five-step tutorial; keyboard/gamepad and any connected mic/sensor action paths; Wave 1 and Wave 2 spawns; Warden armor break and hint; Flute stance; first-Abductor warning; boss transition; and Eurydice shatter. Confirm the painted plate stays unobscured at both 2.5D and boss-camera angles.

### Continuing Work
1. New `Resources/ChamberBackgrounds/Chamber_XX.png` plates are picked up automatically by `ProceduralChamberGenerator.LoadChamberTexture(int chamberIndex)`.
2. For a chamber-specific collider footprint, add a method patterned after `BuildAsphodelBridgeChamber`.
3. Press **Play** in Unity (`SampleScene`) to run bootstrap and complete the visual smoke checklist above.
4. If hardware sensor input is connected (Piezo / BMP280 via ESP32), select the active COM port in the `SensorPulseInput` inspector.
