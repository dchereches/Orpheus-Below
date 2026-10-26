# Underworld overhaul

User-directed expansion of the Asphodel pass: overhaul every visual except Orpheus's rhombus, add parallax and foreground occlusion, water, SFX, main menu, more/larger rooms, and eliminate dominant single-attack spam.

## Implementation choices

- Keep Unity 6000.3.10f1 and existing input paths. Preserve the generated Eurydice identity.
- Twelve named chambers across six biome palettes. All arenas at least 34 by 38 world units, two waves in every arena and four guardian encounters. Activate only the current chamber.
- Painted enemy/guardian atlas and river panorama, detailed architectural meshes, exploration parallax, foreground fading, animated river/chasm/waterfall surfaces.
- Main title/pause flow with controls and audio settings. Menu freezes gameplay and gates input. Serif typography, indigo stone, tarnished gold, ivory, desaturated turquoise and carmine.
- Visible breath resource, attack recovery, repeat-note damage decay and enemy stagger immunity windows. Successful cadences restore breath and open enemy defenses. Guardian rhythm remains independent of exploration breath; missed/wrong/off-beat spam has a real penalty.
- Add synth ambience, footfalls, impacts, armor, hurt, portal and interface cues.

## Work / ownership

1. Main: combat rules, cadence defense-break integration, guardian art, room generator/director, art import, tests and documentation.
2. Menu/audio agent: new UnderworldFrontEnd and UnderworldSoundscape components only.
3. Environment agent: new UnderworldEnvironmentArt and StyxWaterSurface components only.

Integration interfaces: FrontEnd.BlocksGameplay gates player/input/flute/lyre; Soundscape exposes impact/hurt/footstep/portal/menu cues. EnvironmentArt.Build(room,index,width,depth,theme) and StyxWaterSurface.Create(parent,center,size,deep,foam,waterfall) build visuals without changing colliders. Art resources are Art/UnderworldPanorama and Art/UnderworldCreatures.

## Verification

Meaningful EditMode regression tests for sustained spam versus completed cadences, recovery and exhausted breath, enemy stagger protection, room sizing/encounter composition and imported art. Batch compilation; PlayMode smoke and screenshot inspection of title, gameplay, depth/water and boss transition. Fix material integration failures before handoff. No external publication requested.
