# Orpheus Below — Implementation Handoff & Master Asset Guide

## Latest revision — 27 October 2026

### Painted bestiary (current)

The newest TEST character sheets (filenames 20:18:00–20:18:01, saved 21:39:06) now supply **eight animated enemy designs plus summoned sheep**. One dedicated subagent worked on each requested creature. The imported libraries contain **318 body sprites and 208 FX entries**; 24 sheep FX entries intentionally reuse the Shepherd's paintings, for **502 distinct generated frames**. Exact prompts, source paths, grids and optional source rectangles are preserved in `Resources/Enemies/<Actor>/Generation.md` and `AtlasLayout.json`. All generated PNGs retain their original pixels.

The descending guardian roster is **Tripod (depth 1, first boss) → Masked Monk (2, mini-boss) → Mystic Eye Warden (3, second boss) → Winged Sentinel (4, mini-boss) → Shepherd (5, mini-boss) → Cerberus (6)**. The throne rescue remains at depth 7; these guardians stay pacified on ascent. Skeleton casters use animated pistol shots, bullets and muzzle effects. Rooster riders replace the ascent abductors, dash toward Eurydice and drag her toward the actual deeper exit. Existing ordinary melee and armored roles remain, with painted poses.

Cerberus waits and visibly winds up before biting or tail-whipping. The Monk marks locked circular shadows before boulders fall; the Sentinel warns its wide sword area before swinging. The Eye throws its painted staff instead of the old colored sweep. Tripod sonic waves respect dash invulnerability. The Shepherd summons at most three living, attackable sheep with warned dashes. Stun, defeat, guardian phase changes and terminal player states cancel pending attacks and owned hazards; pausing freezes frames and attacks. Musical progression and flute/lyre interactions remain integrated.

Bootstrap version is **13**. Use **Orpheus Below/Bake Painted Bestiary** to rebuild the saved sprite libraries and encounter metadata. See [bestiary assets, combat behavior and verification](docs/painted-bestiary-2026-10-27.md). The previous four-guardian roster below is superseded by this section.

### Eight-room descent, rescue and return (current)

Open `Assets/OrpheusBelow/Scenes/PaintedChamber_00.unity`. Bootstrap version is **12**. Eight supplied TEST paintings are now eight saved chamber scenes, registered first in Build Settings. The source filenames are 18:57:54–18:57:55 with filesystem saves at 19:09; this was the latest eight-image batch containing the requested throne room. Source bytes are unchanged.

The route is **Sanctuary of Artemis → Gardens of the Upper Light → Asphodel's Last Sun → Court of Withered Cypresses → Broken Causeways → Falls of Phlegethon → Titan's Rest → Throne of the Below**. Orpheus begins **alone**. Clear the deepest encounter and approach Eurydice beside the throne to rescue her, then escort her through the same chambers in reverse. Victory occurs only at the sanctuary's surface passage with Eurydice nearby. This makes **15 chamber visits**, with no repeated guardian duels on ascent. All four guardian identities remain on the descent; ascent encounters introduce abductor threats.

**Artemis** now speaks the tutorial and progression guidance, with a new broad-oil portrait. Escort warnings and tether UI remain hidden during the solo descent. Eurydice's approved Look 02 animation library is used after the rescue and for her waiting presence beside the throne. Tutorial completion, direction, encounter clears, health and learned cadences survive scene transitions; restart resets the solo descent.

Each new painting has traced walkable boundaries, solid obstacle footprints, original-art foreground cutouts, stairs, enemy positions and two safe arrival points. The active passage follows the painted exit in the current travel direction; upward passages wait for Eurydice. **F3** shows walkable, inaccessible, cover and passage zones. The existing zoom/crop clamp keeps painting edges out of view. Use **Orpheus Below/Build Eight Painted Chamber Scenes** to rebuild scenes and layouts from `DescentChamberCatalog`.

See [current route, image provenance and verification](docs/painted-chambers-descent-2026-10-27.md). The five-room route descriptions below are history and are superseded by this section.

### Eurydice Look 02 (current)

Eurydice now uses the approved **Look 02 / Broad Oil** ivory/gold/oxblood design and the supplied red-haired facial references. Five transparent atlases in `Resources/Characters/Eurydice/Look02/` supply **148 frames** for eight-direction idle/walking, stair motion, joy, fear, listening, teaching, sanctuary, shatter/reform and surrounding painted effects. The visible silhouette is approximately **30% larger**.

Ground-only movement and facing hysteresis keep idle quiet. Actual tutorial, acoustic reply, abduction, rescue, chamber-clear and victory events select reactions. Stair polygons in the painted layout assets distinguish ascent from descent. The pause menu freezes playback; tutorial gestures use the UI clock while gameplay is paused. `Orpheus Below/Bake Eurydice Look 02` rebuilds the saved sprite library. See [animation behavior and verification](docs/eurydice-look02-animation-guide.md) and [exact production prompts](docs/eurydice-look02-image-prompts.json). Older four-state/dark-crimson Eurydice notes below describe the retained fallback.

### Earlier five-room scene mapping

The active route now uses the **five newest** images from `D:/Serban/TEST`, copied unchanged into `Resources/PaintedChambers/`. Open `Assets/OrpheusBelow/Scenes/PaintedChamber_00.unity` to start. The five saved scene assets are registered first in Build Settings, and real scene transitions follow the painted exits. Bootstrap version is **11**.

Scene order: **Asphodel Viaduct → Court of the Unremembered → Burial Terraces → Vault of the Painted Vessels → Phlegethon Gate**. All four guardian identities remain; Cerberus guards the final gate. The opening chamber is remapped with the same system as all other rooms.

Each scene has a `PaintedChamberDefinition` asset with image-coordinate floor boundaries, solid cover footprints, foreground cutout outlines, entrance/spawn points and an exit. `PaintedChamberMap` inverse-projects the coordinates through the isometric camera, creates solid boundary/cover colliders, and provides a zoom/crop clamp. Actors depth-sort against original-art cutouts, and both enemy and friendly projectiles sweep their full frame movement to stop at cover. Solid obstacles also block dashes.

Press **F3** to inspect zones: green walkable, red inaccessible/solid cover, cyan exit. Scene-view gizmos show the same collision boundaries. `Orpheus Below/Build Five Painted Chamber Scenes` rebuilds the generated scene/layout assets from the authored catalog; ordinary mesh, material and collider data are persisted into assets so scenes reopen correctly. Health and learned cadences carry across chamber loads; restart returns to the first scene.

See [scene mapping notes and image provenance](docs/painted-chambers-2026-10-27.md). The UI/art revision below is retained as history; its five-room names, original opening vista and bootstrap version are superseded by this section.

### Earlier UI/art revision

The active visual revision uses **five chambers per descent**, bootstrap version **10**, subdued oil-painted slate/basalt floors, amber-lit dark stone beacon towers, and a cleaner painted-slate interface. The opening viaduct now loads `Art/AsphodelQuietVista`; the original background is retained. The five-room route is Asphodel → Ferryman → Phlegethon → Cocytus → Gates of Styx, with all four guardian identities retained and **Cerberus in the final chamber**.

The title/pause menu has three plain-language actions; settings retain microphone calibration, brightness, difficulty and audio with keyboard/controller focus. Dialogue, controls and menu panels share the generated `PaintedSlatePanel` surface. Vitality retains its thin bar and two dash diamonds. The exposure overlay lives on a separate canvas so brightness remains active during gameplay.

See [the visual revision notes and exact image prompts](docs/visual-overhaul-2026-10-27.md) for assets, integration and checks. The older detailed sections below describe the prior revision where they conflict with this summary; the twelve-background checklist is an art backlog, not the active room count.

## 1. Project Overview & Repository
- **Repository Root:** `C:\Users\chere\OneDrive\Desktop\Orpheus Below`
- **GitHub Remote:** `https://github.com/dchereches/Orpheus-Below.git` (`main` branch)
- **Engine & Version:** Unity 6 (`6000.3.10f1`), High Definition Render Pipeline (HDRP `17.3.0`), New Input System (`1.18.0`), `.NET Standard 2.1`.
- **Runtime Bootstrap Version:** `13` (auto-rebuilds and upgrades generated scenes on Play mode).
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

### D. Classical Greek Main Menu & Settings Submenu (`UnderworldFrontEnd.cs`)
- **Game Starts on Dedicated Main Menu:** On launch, the game boots directly into the 3-button Hellenic Main Menu. `Time.timeScale` is frozen at `0f`, audio is paused, and gameplay stays frozen behind the painted Underworld panorama until the player clicks `[ ✦ NEW GAME ✦ ]`.
- **Main Menu (3 Greek Mythology Styled Buttons):**
  - **`[ ✦  NEW GAME  ✦ ]`** (switches to **`[ ✦  RESUME  ✦ ]`** when paused in run).
  - **`[ ✦  SETTINGS  ✦ ]`** (opens the comprehensive Hellenic tuning sanctuary).
  - **`[ ✦  QUIT  ✦ ]`** (switches to **`[ ✦  RETURN TO TITLE  ✦ ]`** when paused in run).
- **Rock-Solid [Esc] Pause & Settings Toggle:**
  - Dedicated `TogglePause()` method with frame-deduplication (`_lastToggleFrame`) and a 150ms unscaled-time debounce to prevent button chatter and duplicate frame toggles.
  - Pressing `[Esc]` during gameplay pauses the world and displays the Greek menu.
  - Pressing `[Esc]` while in the Settings submenu returns to the Pause screen.
  - Pressing `[Esc]` while on the Pause screen resumes gameplay seamlessly.
- **Triple-Redundant Component Architecture:**
  1. `OrpheusPrototypeBuilder.BuildPrototypeWorld()` adds `UnderworldFrontEnd` to `Systems_Input_Audio_HUD`.
  2. `AutoBootstrapIfNeeded()` auto-rebuilds and spawns it if missing on scene load.
  3. `OrpheusHud.Start()` auto-attaches `UnderworldFrontEnd` if somehow absent, guaranteeing 100% presence.
  - Styled with chiseled bronze borders, gold leaf meander trims, deep obsidian backgrounds, and `Palatino Linotype` serif typography.
- **Settings Submenu (Sanctuary of Tuning):**
  1. **Acoustic & Microphone Calibration:**
     - Live 500px animated VU Meter (Gray -> Gold -> Crimson).
     - Live note detection feedback pill (`[ SILENCE ]`, `[ SOFT NOTE (S) ]`, `[ HARD BOOM (B) ]`).
     - **Mic Sensitivity / Gain Slider:** `1.0x` to `8.0x` (persisted in `PlayerPrefs` and updating `SensorPulseInput`).
     - **Soft Note Threshold Slider:** `0.05` to `0.40` (14% default).
     - **Hard Boom Threshold Slider:** `0.45` to `0.85` (60% default).
     - **Input Mode Selector:** Cycles Keyboard & Mouse, PC Microphone, ESP32 Serial.
  2. **Brightness & Exposure:**
     - Fullscreen brightness slider from `0.5x` (50%) to `1.5x` (150%).
     - Real-time full-screen luminance overlay (darkening veil for < 1.0, radiant warm glow for > 1.0).
     - Persists automatically via `PlayerPrefs`.
  3. **Underworld Difficulty:**
     - **Mortal:** Story descent. 0.70x incoming damage, +30% breath recovery.
     - **Heroic:** Standard balanced classical Underworld challenge (1.00x).
     - **Tartarus:** Wrath of Hades. 1.40x incoming damage, -15% breath recovery.
     - Persists via `DifficultySettings` / `PlayerPrefs`.
  4. **Audio Volumes:**
     - Master volume slider (0% to 100%) and Mute toggle.
  5. **`[ ✦  RETURN TO MENU  ✦ ]`** button.
- **Microphone Calibration Unbound from `[C]`:**
  - Removed `[C]` key gameplay trigger to prevent accidental toggling during combat. All calibration and microphone testing is accessible via Settings in the Main Menu and `[Esc]` Pause Menu.

### E. Restored Minimalist Vitality Bar & Dash Gems (`OrpheusHud.cs`)
- **Restored Exact Original Aesthetics:** Per player preference, reverted the 320x22px health box back to the subtle, literary 240px wide **5px hairline vitality bar** (`_hpFill`) and minimal 9x9 rotated 45° diamond dash gems.
- **Razor-Sharp HDRP Rendering:** Ultra-thin geometric precision renders cleanly across high-DPI displays without blocking the Underworld backdrop or boss action.
- **Dual Esc Handler:** `OrpheusHud.cs` includes a direct `[Esc]` / `[P]` / Gamepad Start listener that routes directly to `UnderworldFrontEnd.Instance.TogglePause()`, providing immediate pause response.

### F. Per-Chamber Healing & Restoration
1. **Interactive Ambrosia Healing Well (`AmbrosiaHealingWell.cs`):**
   - Stamped procedurally into every chamber (safe entrance alcove).
   - Features a glowing golden nectar ground decal and marble pedestal fountain.
   - Shows a floating 3D prompt when approaching within 2.4m: `[E] Drink Ambrosia (+35 HP)`.
   - Restores **+35 HP**, triggers a radiant golden shockwave ring and harp arpeggio fanfare, then depletes for the remainder of that chamber.
2. **Chamber Respite (+20 HP Transition):**
   - Stepping through an exit portal into a new chamber automatically restores **+20 HP**, providing steady momentum through deep Underworld crawls.

### G. Spread-Out Progressive Learning Tutorial ("Learn as You Go")
- Replaced the frontloaded 6-step checklist in Chamber 0 with contextual, progressive lessons:
  1. **Chamber 0 Entrance:** Move (`WASD`) and light strike (`LMB` / `J`) introduced in the peaceful viaduct walk. Completes upon moving and plucking, triggering Wave 1!
  2. **Chamber 0 Wave 1 (Fast Wretches):** Eurydice calls out to teach Double-Dash (`[Space]` / `[Shift]`) to evade lunging strikes.
  3. **Chamber 0 Wave 2 (Armored Wardens):** Eurydice teaches Hard Boom (`RMB` / `K`) to crack bronze Grief-Armor.
  4. **Chamber 1 Entrance (Shade Swarms):** Eurydice teaches Flute Stance (`[F]`) and Cataclysmic Requiem to pacify dense mobs.
  5. **Chamber 1 Exit / Boss:** Eurydice teaches Cadence [S - S - B] Hymn of Dawn piercing beam.
  6. **Chamber 2:** Eurydice whispers guidance for Cadence [B - S] Sanctuary of Eurydice.
  7. **Chamber 3:** Eurydice whispers guidance for Cadence [S - B - S] Siren's Convergence vortex.

### H. Bottom Tutorial Dialogue Box & Enter-to-Continue Flow (`OrpheusHud.cs`)
- **Fixed Bottom-Center Dialogue Anchor:** Replaced the world-anchored speech bubble above Eurydice's sprite with an elegant, fixed dialogue box anchored at the bottom of the screen (`anchorMin = (0.5, 0)`, `anchorMax = (0.5, 0)`, `pivot = (0.5, 0)`), sized `760x132px` at `pos = (0, 36px)`.
  - **Speaker Header:** `✦   E U R Y D I C E   ✦` in bold 15pt rose-gold serif.
  - **Dialogue Body:** Classical serif Palatino Linotype / Georgia ivory typography (17pt).
  - **Confirmation Prompt:** `[ Press ENTER to continue ↵ ]` (or controller Ⓐ), gently pulsating in warm antique gold (`new Color(0.96f, 0.84f, 0.46f)`).
  - **Frame Styling:** Obsidian background (`0.045, 0.018, 0.055, 0.94`) with antique gold hairline border and subtle rose outline.
- **Enter-to-Continue Level Gating:**
  - When any tutorial prompt or lesson is triggered, gameplay immediately halts (`Time.timeScale = 0f; OrpheusHud.IsTutorialModalActive = true;`).
  - Player movement, dashes, lyre strikes, flute stance, and enemy attacks/movement are completely frozen, giving the player full focus to read the guidance without pressure or danger.
  - The level pauses until the player explicitly presses **`[Enter]`** (or `[Numpad Enter]` / Gamepad South `[A]`).
  - Upon pressing Enter, a confirmation chime sounds, the modal dismisses smoothly, `Time.timeScale = 1f;`, and gameplay resumes immediately.
- **Complete Elimination of Water Systems:**
  - Removed all water assets, water shaders (`StyxWater.shader`), river meshes (`StyxWaterSurface.cs`), water audio sources ("StyxWater"), shallow water footstep audio ("ShallowWaterStep"), and glowing water/river ground decals across all chambers and scripts.
  - Chasms are now bottomless dark Stygian abysses (`AbyssPit`), fitting the bleak descent into Tartarus.

---

### I. Assembly Definition & Compilation Integrity
- **Assembly Boundaries:**
  - `OrpheusBelow.Runtime.asmdef`: Core gameplay, entities, combat, HUD, audio, camera, and world generation.
  - `OrpheusBelow.Editor.asmdef`: Editor tooling, screenshot capture, and menu commands.
  - `OrpheusBelow.EditModeTests.asmdef`: NUnit test suite covering tutorials, difficulty scaling, sprite animators, and environment lifecycle.
- **Pure Abyss Architecture:** All test files (e.g. `UnderworldOverhaulTests.cs`) and editor capture scripts (`UnderworldSmokeCapture.cs`) are verified and clean of legacy water references.
- **Zero-Warning Assembly Compiles:** All three projects compile with 0 errors via standard `dotnet build` and Unity Editor compilation.

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
| `Enter` / `Numpad Enter` / Gamepad South | Continue / Confirm Tutorial Dialogue Prompts |
| `E` / Gamepad South | Drink Ambrosia from Chamber Well (+35 HP) |
| `Esc` / Gamepad Start | Pause Menu & Greek Settings (Mic Calibration, Brightness, Difficulty) |
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
  1. `Chamber_00.png`: **Asphodel Viaduct** *(ACTIVE & INTEGRATED)* — Elevated stone bridge with asphodel blossoms, amber seams, abyss chasm flanks.
  2. `Chamber_01.png`: **Cantor's Amphitheatre** — Ruined stone arena with ancient Hellenic musical staves etched into flagstones, broken fluted columns.
  3. `Chamber_02.png`: **Vault of the Ferryman** — Damp cavern with mossy wooden docks, stacks of green copper coins, Stygian chasm docks.
  4. `Chamber_03.png`: **Acheron Rapids** — Jagged obsidian stepping stones, floating basalt slabs, dashable abyss chasm drops.
  5. `Chamber_04.png`: **Warden's Crossing** — Massive stone mooring posts, heavy iron chains crossing the abyss, shattered oars.
  6. `Chamber_05.png`: **Phlegethon Lava Vaults** — Floating basalt hexagonal tiles over glowing magma rivers, cracked magma veins.
  7. `Chamber_06.png`: **Crucible of Tisiphone** — Scorched crimson stone arena, boiling lava falls in background, obsidian altars.
  8. `Chamber_07.png`: **Elysian Sepulchre** — Pale marble mosaic floor overgrown with ghost-white reeds and silver weeping willows.
  9. `Chamber_08.png`: **Siren's Grove** — Ethereal moonlit obsidian mosaic floor, hollow resonant stone bells, mist-covered ruins.
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
  5. `Parallax_Layer4_ForegroundMist.png`: Drifting ghost mist, abyss vapors, or volcanic embers.

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
  - `Prop_DashableChasmEdge.png`: Broken stone bridge ledge overlooking the bottomless Stygian abyss.
  - `Prop_ChamberExitDoor.png`: Hellenic archway framed by glowing red/gold Underworld runes.
  - `Prop_ThresholdRuneRing.png`: Ground decal ring activating when all chamber enemies are cleared.
- **Healing & Shrines (`Props/Healing/`):**
  - `Shrine_AmbrosiaFountain.png`: Carved marble lion-head fountain pouring luminous golden water into a stone basin (`AmbrosiaHealingWell.cs`).
  - `Item_NectarFlask.png`: Golden Greek amphora containing healing honey-nectar (+40 HP).
  - `Shrine_HarmonicAltar.png`: Resonant stone altar that restores full breath and grants a temporary 4th dash charge.

### Category G: Environmental Surfaces & Atmospheric Textures
- **Folder:** `Assets/OrpheusBelow/Resources/Fluids/`
- **Format:** Seamless tileable textures (`1024 x 1024`).
- **Textures:**
  - `Surface_PhlegethonLava_Base.png`: Viscous molten orange-red magma with floating obsidian crust.
  - `Surface_CocytusIce_Sheet.png`: Frosted translucent ice sheet with sub-surface cracks and trapped shades.
  - `Surface_AbyssVapor_Noise.png`: Deep violet and ash particulate noise for bottomless Tartarus chasms.
