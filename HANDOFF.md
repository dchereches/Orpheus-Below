# Orpheus Below — Implementation Handoff & Master Asset Guide

## 1. Project Overview & Repository
- **Repository Root:** `C:\Users\chere\OneDrive\Desktop\Orpheus Below`
- **GitHub Remote:** `https://github.com/dchereches/Orpheus-Below.git` (`main` branch)
- **Engine & Version:** Unity 6 (`6000.3.10f1`), High Definition Render Pipeline (HDRP `17.3.0`), New Input System (`1.18.0`), `.NET Standard 2.1`.
- **Runtime Bootstrap Version:** `8` (auto-rebuilds and upgrades generated scenes on Play mode).
- **Core Premise:** A distinctive mythic Hellenic chamber-crawler based on **sound, breath, and grief**. Orpheus descends into the Underworld with his Lyre and Flute (Aulos) to lead Eurydice to the upper light. Features dual-perspective camera geometry, zero RNG stat boons, procedural 2.5D background-plate chamber generation, and deterministic musical progression.

---

## 2. Core Gameplay Systems (Current Revision)

### A. Dual-Perspective Gameplay
1. **2.5D Isometric Chamber-Crawler (`Pitch 36°, Yaw 45°`):**
   - 360° screen-aligned WASD / Left Stick movement with snappy physics acceleration (`155`) and deceleration (`185`).
   - 2-charge Double-Dash (`[Space]` / `[Shift]`) with `0.185s` invulnerability frames and chasm vaulting over `IsoObstacleKind.DashableChasm`.
   - Lyre Combat: Soft Notes (`S`, `< 60%`) fire crescent slashes and acoustic pings to Eurydice; Hard Booms (`B`, `>= 60%`) unleash radial shockwaves that crack bronze Grief-Armor and slam enemies into walls (`+32` damage).
2. **2D Parallax Guardian Duels (`Pitch 4.5°, Yaw 45°`):**
   - Camera smoothly glides down into a cinematic side-view stage locked along `UnderworldCamera2D.IsoRightXZ`.
   - Backed by a 5-layer procedural parallax backdrop (`UnderworldParallaxRig.cs`) and the painted Asphodel viaduct vista.

### B. The Flute (Aulos): Cataclysmic Requiem
- Hold/Toggle `[F]` (or Gamepad LT) to draw the Aulos Panpipes (slows movement by 65%, suppresses dash).
- **Note Pulses:** Each correct note input in the 4-note challenge pulses for **45 AOE damage** within 8.5m and micro-staggers surrounding enemies.
- **4th Note: Cataclysmic Requiem Nova:**
  - Deals **240 AOE Nova damage** across a 12.8m radius (vaporizes basic shades).
  - Instantly shatters all Bronze Grief-Armor in earshot.
  - Destroys all enemy projectiles and orbs across the chamber.
  - Stuns heavy survivors for 3.0s (vulnerable shades surrender and sink into mist).
  - Fully refills Orpheus's breath gauge.
  - Against Crashed Guardians, deals **480 colossal execution damage**!

### C. Phased 2D Guardian Duel
1. **Phase 1: Arena Combat:** Freeform 2D action! Player boundary widened (`maxX = 4.2m`). Boss actively attacks with telegraphed Spectral Wing Lunges, Ground Cleave shockwaves, and Stygian Geysers. All player slashes, bolts, and cadences deal full direct damage.
2. **Phase 2: Stave Climax (Milestones at 65% & 30% HP):** Boss leaps into the background ruins/vista. Stave illuminates overhead for a high-tempo 6-note rhythm barrage at 110–120 BPM. Deflecting notes fires radiant energy lances into the boss.
3. **Phase 3: Crashed & Stunned (The Flute Execution Window):** Boss crashes face-down, stunned for 5.0 seconds and taking **2.0x damage** from all attacks (`"GUARDIAN CRASHED! [F] UNLEASH THE FLUTE!"`).

### D. Sound & Acoustic Calibration Menu (`[C]`)
- Opened via `[C]` key or Gamepad Select anytime during gameplay or from pause.
- Features: Live animated VU Meter with Soft and Boom threshold markers; real-time note recognition badge (*Silence*, *Soft Pluck [S]*, *Hard Boom [B]*); sliders for Mic Sensitivity (Gain: 1.0x–8.0x), Soft Threshold (0.05–0.40), Hard Boom Threshold (0.45–0.85), and Master Volume (0%–100%); microphone device selector; audio preview buttons; auto-saves to `PlayerPrefs`.

---

## 3. Controls Reference
| Key / Input | Action |
|---|---|
| `W`, `A`, `S`, `D` / Left Stick | 360° Isometric Movement (A/D on 2D Boss Stage) |
| `Space` / `Left Shift` / Gamepad South | 2-Charge Dash (Vaults Chasms, i-frames) |
| `F` / Gamepad Left Trigger | Flute (Aulos) Stance & Cataclysmic Requiem |
| Tap `LMB` / `J` / Gamepad West | Soft Note ('S', < 60% intensity / Eurydice hums back) |
| `RMB` / `K` / Gamepad North, or Hold `LMB` | Hard Orphic Boom ('B', >= 60% intensity / Cracks armor) |
| `S - S - B` | Cadence: Hymn of Dawn (Piercing Resonance Beam) |
| `B - S` | Cadence: Sanctuary of Eurydice (Golden Aegis) |
| `S - B - S` | Cadence: Siren's Convergence (Harmonic Vortex) |
| `C` / Gamepad Select | Sound & Acoustic Calibration Menu |
| `Tab` | Cycle Lyre Input (Keyboard+Mouse / Microphone / ESP32) |
| `H` | Toggle Controls Card Overlay |
| Hold `Q` / Gamepad Left Shoulder | Forbidden Look-Back (Fatal temptation!) |
| `R` | Restart Descent |

---

## 4. Master Texture & Art Asset Checklist

Drop generated textures into the appropriate folder under `Assets/OrpheusBelow/Resources/`.

### Category A: 2.5D Isometric Background Plates (12 Chambers)
- **Folder:** `Assets/OrpheusBelow/Resources/ChamberBackgrounds/`
- **Naming:** `Chamber_00.png` through `Chamber_11.png`
- **Resolution:** `2048 x 2048` or `1920 x 1080` PNG / JPG (RGB, no transparency).
- **Camera Perspective:** Top-down angled isometric floor (`Pitch 36°`, `Yaw 45°`). No upright double-foreshortening.
- **Biomes & Chamber List:**
  1. `Chamber_00.png`: **Asphodel Viaduct** *(ACTIVE & INTEGRATED)* — Elevated stone bridge with asphodel blossoms, amber seams, abyss waterfall flanks.
  2. `Chamber_01.png`: **Cantor's Amphitheatre** — Ruined stone arena with ancient Hellenic musical staves etched into flagstones, broken fluted columns.
  3. `Chamber_02.png`: **Vault of the Ferryman** — Damp cavern with mossy wooden docks, stacks of green copper coins, Stygian riverbanks.
  4. `Chamber_03.png`: **Acheron Rapids** — Jagged obsidian stepping stones, roaring cyan whitewater, dashed chasm drops.
  5. `Chamber_04.png`: **Warden's Crossing** — Massive stone mooring posts, heavy iron chains crossing the abyss, shattered oars.
  6. `Chamber_05.png`: **Phlegethon Lava Vaults** — Floating basalt hexagonal tiles over glowing magma rivers, cracked magma veins.
  7. `Chamber_06.png`: **Crucible of Tisiphone** — Scorched crimson stone arena, boiling lava falls in background, obsidian altars.
  8. `Chamber_07.png`: **Elysian Sepulchre** — Pale marble mosaic floor overgrown with ghost-white reeds and silver weeping willows.
  9. `Chamber_08.png`: **Siren's Grove** — Ethereal moonlit reflecting pool, hollow resonant stone bells, mist-covered ruins.
  10. `Chamber_09.png`: **Cocytus Frozen Tarn** — Black translucent sheet ice with frozen shades trapped beneath, frost-cracked chasms.
  11. `Chamber_10.png`: **Lethe Oblivion Gate** — Deep purple/indigo stone floor fading into amnesiac violet mist, crumbling archways.
  12. `Chamber_11.png`: **The Gates of Styx (Cerberus Den)** — Colossal bronze threshold doors of Hades, blood-red volcanic stone, triple-chained pillars.

### Category B: 2D Parallax Layers for Boss Duels & Havens
- **Folder:** `Assets/OrpheusBelow/Resources/Art/Parallax/`
- **Resolution:** `1920 x 1080` PNG with transparent alpha background.
- **Perspective:** Side-scrolling horizontal profile (`Pitch 4.5°`, `Yaw 45°`).
- **Layers (Per Biome: Asphodel, Phlegethon, Elysium, Styx):**
  1. `Parallax_Layer0_FarSky.png`: Distant Tartarus skyline, smoky peaks, celestial abyss glow.
  2. `Parallax_Layer1_DistantRuins.png`: Colossal Greek temples, ruined aqueducts, distant colossal statues.
  3. `Parallax_Layer2_MidArchways.png`: Framing stone pillars and cracked arches (where boss perches during Stave Climax).
  4. `Parallax_Layer3_StagePlate.png`: Horizontal stone balustrade and cracked floor tiles of the duel arena.
  5. `Parallax_Layer4_ForegroundMist.png`: Drifting ghost mist, waterfall spray, or volcanic embers.

### Category C: Character Sprite Sheets & Animations
- **Folder:** `Assets/OrpheusBelow/Resources/Characters/`
- **Format:** Transparent PNG sprite sheets (e.g. `4x4` or `8x1` strips, `256 x 256` per frame).
- **Orpheus (`Characters/Orpheus/`):**
  - `Orpheus_Idle.png`: Holding golden Lyre, gentle cape flutter (4 frames).
  - `Orpheus_Run.png`: 8-directional run cycle (8 frames).
  - `Orpheus_Dash.png`: Forward elongated dash silhouette with after-image motion blur (3 frames).
  - `Orpheus_Lyre_SoftPluck.png`: Crescent slash windup, pluck, and golden chord release (3 frames).
  - `Orpheus_Lyre_HardBoom.png`: Overhead two-handed strike / sub-bass radial shockwave (4 frames).
  - `Orpheus_Flute_Stance.png`: Raising Aulos panpipes to lips, breathing stance (4 frames).
  - `Orpheus_Flute_Requiem.png`: Full-screen burst pose, arms outstretched, sonic nova release (3 frames).
  - `Orpheus_Hurt.png`: Recoil / stagger frame on taking damage (2 frames).
- **Eurydice (`Characters/Eurydice/`):**
  - `Eurydice_Veiled_Walk.png`: Ethereal floating walk cycle, translucent hem trailing mist (8 frames).
  - `Eurydice_CallReply_Hum.png`: Turning, glowing moonlit aura, hand over heart (4 frames).
  - `Eurydice_Abducted_Struggle.png`: Reaching forward toward Orpheus as wraiths drag her backward (4 frames).
  - `Eurydice_RestingDuet.png`: Seated by stone fountain, singing/listening pose (4 frames).

### Category D: Enemies & Bosses
- **Folder:** `Assets/OrpheusBelow/Resources/Creatures/`
- **Format:** Transparent PNGs, distinct silhouette and colors.
- **Shades:**
  - `Shade_WretchLunger.png`: Crimson agile ghoul; idle, leap windup, lunging strike, weeping sorrow sink.
  - `Shade_BrimstoneCaster.png`: Floating violet sorcerer skull; hover, orb cast, weeping sorrow sink.
  - `Shade_ArmoredWarden.png`: Heavy bronze hoplite ghost with round shield and spear; shield guard, spear thrust, shield shatter, sorrow sink.
  - `Shade_MourningAbductor.png`: Cyan-violet wraith with elongated reaching talons; glide, grabbing pose, sorrow sink.
- **Guardians / Bosses:**
  - `Boss_CantorOfFury.png`: Multi-winged fury priestess; floating hover, scythe sweep, ruin perch, crashed face-down stunned.
  - `Boss_WardenOfOar.png`: Giant spectral ferryman with massive stone anchor/oar; sweep, geyser slam, crashed stunned.
  - `Boss_SirenOfAcheron.png`: Four-winged harpy siren with stone harp; sonic cone, ruin perch, crashed stunned.
  - `Boss_Cerberus.png`: Three-headed brimstone hound; triple bite, fire wave slam, roar, crashed stunned.

### Category E: NPCs Along the Way
- **Folder:** `Assets/OrpheusBelow/Resources/NPCs/`
- **Format:** Transparent PNGs (`512 x 512` or `1024 x 1024`).
- **Characters:**
  1. `NPC_Charon.png`: The Ferryman leaning on his oar with glowing coin eyes, shrouded in Stygian cowl.
  2. `NPC_Hermes.png`: The Psychopomp guide with winged sandals and caduceus staff, leaning against an Underworld milestone.
  3. `NPC_PersephoneShade.png`: Queen of the Underworld veiled in silver pomegranates and nightshade blossoms.
  4. `NPC_Sisyphus.png`: Muscular shade resting by his boulder, offering breath endurance wisdom.
  5. `NPC_ThracianBard.png`: Ancient poet shade with a broken lyre, teaching forgotten chord cadences.

### Category F: Interactive Props, Hazards & Healing Items
- **Folder:** `Assets/OrpheusBelow/Resources/Props/`
- **Format:** Transparent PNGs.
- **Props & Objects:**
  - `Prop_TuningForkPillar.png`: Bronze tuning-fork pillar that vibrates with golden rings when struck.
  - `Prop_CrackedObelisk.png`: Destructible basalt obelisk with glowing amber runes (intact & shattered frames).
  - `Prop_StygianUrn.png`: Classical black-and-orange Greek vase that smashes into shards, dropping breath notes.
  - `Prop_DashableChasmEdge.png`: Broken stone bridge ledge with waterfalls tumbling into abyss.
  - `Prop_ChamberExitDoor.png`: Hellenic archway framed by glowing red/gold Underworld runes.
  - `Prop_ThresholdRuneRing.png`: Ground decal ring activating when all chamber enemies are cleared.
- **Healing & Shrines (`Props/Healing/`):**
  - `Shrine_AmbrosiaFountain.png`: Carved marble lion-head fountain pouring luminous golden water into a stone basin.
  - `Item_NectarFlask.png`: Golden Greek amphora containing healing honey-nectar (+40 HP).
  - `Shrine_HarmonicAltar.png`: Resonant stone altar that restores full breath and grants a temporary 4th dash charge.

### Category G: Fluids & Particle Textures
- **Folder:** `Assets/OrpheusBelow/Resources/Fluids/`
- **Format:** Seamless tileable textures (`1024 x 1024`).
- **Textures:**
  - `Fluid_StyxWater_Base.png`: Dark turquoise water with submerged dead petals.
  - `Fluid_StyxWater_Foam.png`: White-cyan foam and spray for rapids and waterfalls.
  - `Fluid_PhlegethonLava_Base.png`: Molten orange-red lava with dark cooling crust plates.
  - `Fluid_CocytusIce_Sheen.png`: Blue-black cracked sheet ice with trapped souls.
  - `Fluid_LetheMist_Tile.png`: Purple amnesiac fog and smoke particles.
  - `VFX_AulosShockwave_Ring.png`: Golden-cyan acoustic wave texture for the Cataclysmic Requiem.
  - `VFX_MusicalStave_Lines.png`: Radiant golden 3-line musical stave for 2D Boss Duels.

---

## 5. Next Steps for Incoming Developer / AI
1. Generated artwork dropped into `Assets/OrpheusBelow/Resources/` is instantly recognized by the multi-tier loading system in `ProceduralChamberGenerator.cs` and `UnderworldArtLibrary.cs`.
2. Hitting **Play** in Unity automatically refreshes older scenes via `HadesChamberDirector.AutoBootstrapIfNeeded()`.
3. Press `[C]` in-game or from pause to calibrate microphone, breath detection, and master audio volumes.
