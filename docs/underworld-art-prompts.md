# Underworld art generation — 26 October 2026

Generated with the built-in image generation tool. Runtime imports preserve the transparent creature atlas and uncompressed panorama. Orpheus remains the existing procedural diamond character.

## Creature atlas
Saved asset: `Assets/OrpheusBelow/Resources/Art/UnderworldCreatures.png`.

Initial prompt: production transparent four-column, two-row game atlas, painted Greek underworld creatures in ink, bronze, crimson and turquoise. Full-body wretch, violet caster, bronze hoplite warden, cyan abductor, fury priestess, ferryman with oar, ivory winged siren, three-headed Cerberus. Consistent scale, no text, no floor, no interface.

Correction prompt: preserve the eight creatures but reduce each figure's size by 30%, in an exact evenly divided four-column, two-row grid. At least 15% transparent padding on every side, including weapons, wings, glow, hair and cloth; no part crosses a cell boundary. Every figure fully visible, centered in its cell, feet at 85% of cell height. No captions, gridlines, fragments or overlapping cells.

Final output is 1536×1024 RGBA. The runtime uses eight explicitly measured padded regions rather than blind equal-cell slices. All eight region borders have alpha at most 4/255, checked from the raster and covered by an EditMode test. Regions are normalized to 2.4 world units tall.

## Panorama
Saved asset: `Assets/OrpheusBelow/Resources/Art/UnderworldPanorama.png`.

Prompt: wide painted Greek underworld panorama, cavern river, ruined arches and monumental Hellenic architecture, indigo shadow, turquoise waterfalls and a gold eclipse. Atmospheric recession and intricate stone detail. Keep the left area quiet and dark for a menu title. No characters, text or interface.

## Existing reference-derived Eurydice
The prior generated four-by-four crimson Eurydice sheet remains active. It was based on the user-provided dark-crimson reference sheet, with Idle, Glide, Teaching and Shatter rows. The new overhaul preserves this character design and changes the surrounding visual world.

## Stone paving
Saved asset: `Assets/OrpheusBelow/Resources/Art/UnderworldFloor.png`.

Prompt: square seamless top-down Greek underworld stone paving material, edge-to-edge dark blue slate and purple charcoal basalt flagstones with pitted texture, subtle cracks, worn seams, tiny gold inlay and occasional carved Greek meander. Restrained low contrast palette, fine painterly detail, flat overhead lighting; no perspective, buildings, objects, characters, text, cast shadows or central emblem. Repeatable walkable isometric dungeon material.
