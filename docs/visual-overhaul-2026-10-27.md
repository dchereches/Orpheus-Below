# Quiet stone, beacon towers, and a cleaner interface

## Direction and implementation

Requested: overhaul UI, replace lighting props with lit dark towers, retain the oil painting aesthetic, quiet the floor tiles, and reduce each level to five chambers. The current game represents its level as a single descent, so the generated descent now has five rooms. This does not add a new level-selection system.

Palette: painted slate `#202A32`, charcoal violet `#322E3A`, old bronze `#857354`, lamplight `#D1B376`, bone `#F2EBD1`, muted stone `#B3B8B5`. Palatino/Georgia provides literary titles and speaker names; Segoe UI provides instructions, controls, HUD labels and settings labels. The memorable accent is amber light enclosed in dark stone architecture; floors and UI surfaces remain quiet.

Title and pause use one painted panel, three left-aligned actions and a spacious painting to the right. Settings use two columns with larger section titles, readable labels, thin slider tracks and explicit keyboard/controller navigation. Dialogue uses the same painted slate surface, a simple speaker name, left-aligned body text and a steady continue prompt. Vitality remains a five-pixel line with two dash diamonds. Controls are shortened and spaced, and a small persistent counter shows the five-room journey.

Room order and guardians:

| Chamber | Place | Guardian |
| --- | --- | --- |
| 1 | Asphodel Viaduct | Cantor of Fury |
| 2 | Vault of the Ferryman | Warden of the Oar |
| 3 | Phlegethon Foundry | Two shade waves |
| 4 | Cocytus Mirror Tarn | Siren of Acheron |
| 5 | Gates of Styx | Cerberus, then the sunlight exit |

Bootstrap version 10 rebuilds older generated scenes on entering Play. Healing wells, tutorial pauses, cadence unlocks, settings persistence and the two camera modes remain part of the descent. The opening viaduct uses a fixed camera to keep its painted floor registered to the gameplay footprint. Its railings, slab, towers and abyss proxies retain physics but no longer draw over the finished painting.

## Generated assets and exact prompts

All five assets were produced using the built-in image generation tool, copied into `Assets/OrpheusBelow/Resources/Art/`, and consumed by runtime code. No API-key or CLI fallback was used. Original artwork remains in the repository.

### QuietSlateFloor.png

Use case: stylized-concept. Asset type: production seamless square floor texture for an oil-painted mythic Greek underworld game. Generate a square orthographic top-down material swatch filling the entire image edge to edge: broad worn rectangular limestone and slate flagstones, muted charcoal blue gray, dusty mauve undertones, soft close-value joints, sparse broad brushwork, softly scumbled oil pigment. Extremely restrained low contrast so characters and attack telegraphs stand out. Approximately 4 by 4 broad irregular staggered slabs across the image; large quiet interiors. Flat diffuse illumination without directional lighting, shadows, perspective or vignette. Seamlessly repeating on all four edges. No gold inlay, no symbols, no decorative borders, no glowing cracks, no flecks or gritty photoreal detail, no objects or words. Actual game ground texture, not a presentation.

### QuietBasaltFloor.png

Use case: stylized-concept. Asset type: production seamless square floor texture for an oil-painted Greek underworld game. Edge-to-edge orthographic top-down material swatch: large old basalt pavers in broad irregular staggered rectangular courses, dusty dark umber, muted aubergine charcoal and slate gray. Restrained hand-brushed oils, soft scumbling and worn edges, almost uniform low contrast. About 4 by 4 broad slabs across square with very quiet matte interiors and shallow softly painted joints. Seamlessly repeating on every edge. Flat diffuse illumination, no directional lighting or cast shadows, no perspective, no vignette. No lava, no emissive cracks, no gold, no ornaments, no small noisy texture, no objects, no text. Ground should recede behind characters.

### DarkBeaconTower.png

Generated with a transparent background; its corner alpha was verified as zero.

Use case: stylized-concept. Asset type: isolated transparent-background game environment prop sprite. A single full-height slender dark Hellenic beacon tower, tall rectangular charcoal basalt masonry body on a small stepped square plinth, modest taper, a simple classical cornice, and a recessed open lantern chamber near the crown glowing warm muted amber through three narrow tall windows. Top is flat capped stone, no visible exposed flame. Three-quarter isometric view from 36 degrees above with two visible faces, vertical tower axis remains vertical. Handmade oil painting with broad brush marks, softly scumbled dark blue charcoal and dusty violet masonry, small antique bronze details, warm honey window light gently catching the nearby stone, dark tower silhouette is dominant. Centered with ample transparent padding, base centered at bottom, entire tower uncut. No environment, no ground plane, no text, no rune, no floating halo, no lightning bolts, no modern street lamp, no ornate fantasy spikes, no pedestal display.

### PaintedSlatePanel.png

Use case: stylized-concept. Asset type: finished production UI panel background texture for a restrained oil-painted Greek underworld game, square canvas filled edge to edge. A very dark blue charcoal painted slate surface, quiet broad oily scumbled brush marks with almost uniform tone; center 85 percent is empty and nearly flat for readable game text. A single extremely fine worn muted antique bronze hairline inset 4 percent from each edge, tiny unadorned corner joints. Elegant calm simplicity, low contrast, flat diffuse lighting. No letters, no words, no logos, no icons, no mockup, no separate panels, no gold filigree, no Greek meander, no glow, no vignette beyond subtle dark brushwork at edges. This image will be nine-sliced as a UI surface so the four corners must be consistent.

### AsphodelQuietVista.png

Edit target: `Assets/OrpheusBelow/Resources/ChamberBackgrounds/Chamber_00.png`. New output retained separately.

Use case: precise-object-edit. Image 1 is the edit target: existing oil-painted Asphodel viaduct background for an isometric Greek underworld game. Preserve the exact image framing, camera, bridge path, playable floor silhouette, archways, railings, pillars, abyss, background perspective and geometry. Refine ONLY these visual elements: make all walkway paving calmer and less busy, broad matte charcoal-slate slabs with subtle broad oil brush strokes, low contrast soft joints, remove luminous gold floor cracks and most tiny flecks. Reduce bright flowers around walkway to sparse muted asphodel clusters. Transform the tall foreground dark pier/tower ornaments into dark charcoal masonry beacon towers with small recessed amber-lit lantern windows near their crowns, simple stone caps, quiet silhouettes; no dangling bright gold lines. Preserve the existing dark violet/mauve abyss atmosphere and handmade oil painting aesthetic, slightly less saturated and more painterly, warm light limited to tower windows. No people, no UI, no words, no new structures or change to floor boundaries. Landscape 16:9.

## Verification

Unity EditMode suite and runtime screenshot capture are used to check route generation, final guardian/exit, floor bounds, generated resource imports, UI navigation and runtime lifecycle. Rendered views are retained under `Logs/VisualOverhaul/` for local inspection. The capture suppresses world bloom because it temporarily renders the normally crisp overlay UI through the world camera.

The original commit `40fc670` was independently extracted into an ignored baseline project and tested: 20 of 24 tests pass, with four failures in existing tutorial/combat expectations. The changed project adds two tests, and its final suite run passes 22 of 26 with exactly the same four failures. The failures are `FluteEntryDoesNotFinishUntilThePlayerPerformsACadence`, `PreplayedActionsCannotSkipLaterPrompts`, `RequiresMeaningfulMovementAndANewDashAfterThePrompt`, and `OffBeatOrdinaryDamageCannotDefeatGuardian`. These unrelated gameplay expectations were left intact.

Final validation on 27 October 2026:

- Runtime and editor assemblies compiled without C# warnings or errors in the final Unity runs.
- All changed/new route and art tests passed, including invisible collision proxies and the final Cerberus identity.
- Full EditMode result: `Logs/visual-overhaul-editmode-final.xml` (22 passed, four confirmed baseline failures).
- Runtime screenshot smoke: exit 0, zero runtime errors, verified title, settings at 1080p/720p/4:3, tutorial pause, exposure canvas during gameplay, controls, both floor textures, final guardian, restart, and return to title. Log: `Logs/visual-overhaul-smoke-final.log`.
- Final screenshots were inspected under `Logs/VisualOverhaul/`; source diff passed `git diff --check`.
