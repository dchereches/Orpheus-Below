# Orpheus Below — Project Architecture & AI Hand-off Guide

## 1. Project Overview & Repository
- **Repository Root:** `C:\Users\chere\OneDrive\Desktop\Orpheus Below`
- **GitHub Remote:** `https://github.com/dchereches/Orpheus-Below.git` (`main` branch)
- **Engine & Version:** Unity 6 (`6000.3.10f1`), High Definition Render Pipeline (HDRP `17.3.0`), New Input System (`1.18.0`), `.NET Standard 2.1`.
- **Core Premise:** A distinctive mythic Hellenic chamber-crawler based on **sound, breath, and grief**. Orpheus descends into the Underworld with his Lyre and Flute (Aulos) to lead Eurydice to the upper light. Features a dual-perspective camera system, zero RNG stat boons, procedural 2.5D background-plate chamber generation, and deterministic musical progression.

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

## 5. Procedural Chamber Generation (`ProceduralChamberGenerator.cs`)
Implements **Approach A5**: 2D painted background plates tilted at the 2.5D isometric pitch (`36°`) with procedurally stamped colliders, props, hazards, and encounters.
- **Background Plate Loading:** Checks `Resources/ChamberBackgrounds/Chamber_{index}` or custom registered textures. If absent, dynamically renders rich procedural multi-toned stone mosaic slabs with biome accents.
- **Underworld Biomes (8+ Chambers):**
  1. *Asphodel Threshold* (Mini-Boss: Cantor of Fury)
  2. *Vault of the Ferryman* (Heavy Abductor & Caster crossfire)
  3. *Chasms of Acheron* (Mini-Boss: Warden of the Oar + Dashable Chasms)
  4. *Phlegethon Lava Vaults* (Magma hazards + Obelisks)
  5. *Elysian Sepulchre* (Mini-Boss: Siren of Acheron + Dual Mirages)
  6. *Cocytus Frozen Tarn* (Frost chasms + Weeping Shades)
  7. *Lethe Oblivion Gate* (Elite Wardens + Abductor nests)
  8. *The Gates of Styx* (Final Boss: 3-Headed Cerberus)
- **Automatic Upgrades:** `HadesChamberDirector.PrototypeBuildVersion = 4`. Pressing **Play** in Unity automatically rebuilds and upgrades any older scene in the hierarchy.

---

## 6. Eurydice Psychological & Escort Mechanics (`BlurredEurydice.cs`)
- **Soul Tether:** Glowing line renderer connecting Orpheus and Eurydice. Shimmers pale gold/silver normally; turns jagged crimson when grabbed.
- **Mourning Abductors:** Special cyan-crimson Shades that ignore Orpheus and drag Eurydice backward toward Tartarus. If `AbductionDanger01` hits 1.0, she is lost (`TriggerEurydiceAbductedLoss`).
- **Acoustic Call-and-Response:** Plucking a Soft note near Eurydice makes her hum back (`PlayEurydiceHumReply`) with a moonlit ripple.
- **Mirage Decoys (`EurydiceMirageDecoy`):** Deceptive Underworld silhouettes spawn in temptation zones to bait `[Hold Q]` (which is fatal). Plucking verifies the real Eurydice; hitting a mirage dispels the illusion without looking back.

---

## 7. Asset Checklist Needed from AI Art Generator / User

To replace procedural fallback blocks with custom painted artwork, generate the following assets and drop them into the project:

### A. Tilted 2.5D Background Plates
- **Target Folder:** `Assets/OrpheusBelow/Resources/ChamberBackgrounds/`
- **Naming:** `Chamber_00.png` through `Chamber_07.png`
- **Resolution:** `2048 x 2048` or `1920 x 1080` PNG (no alpha for ground plates)
- **Perspective:** Top-down angled isometric floor (`36°` tilt). Dark Underworld tones, mosaic stone tiles, lava veins, or river edges.

### B. 2D Parallax Boss Duel Backdrops
- **Dimensions:** `1920 x 1080` PNG with transparency.
- **Layers per Biome:**
  1. `Far_Skyline.png` (distant Stygian peaks / smoky sky)
  2. `Mid_Archways.png` (colossal Greek pillars / crumbling ruins)
  3. `Near_Hazards.png` (glowing lava falls / ghostly wisps)

### C. Character & Enemy Sprites (Transparent PNGs)
- **Orpheus:** Run cycle, Dash silhouette, Lyre pluck pose, Flute playing pose, Hurt/Stagger pose.
- **Blurred Eurydice:** Veiled ghost silhouette with soft edge blur, looking-back focus sprite, and weeping mist pose.
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

## 9. Next Steps for Incoming Developer / AI
1. When custom PNGs are added to `Resources/ChamberBackgrounds/`, test them by hitting **Play** in Unity.
2. Consider adding sprite animator controllers to swap procedural Diamond/Circle geometry with the user's generated sprite sheets.
3. If hardware sensor input is connected (Piezo / BMP280 via ESP32), set COM port in `SensorPulseInput` inspector.
