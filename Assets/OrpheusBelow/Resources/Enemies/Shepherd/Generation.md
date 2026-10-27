# Shepherd generation record

Built-in image_gen mode, 2026-10-27. Transparent background requested. Source PNGs are copied byte for byte; no raster edits, resizing, repacking, duplicate stills or pixel cleanup.

Reference: D:/Serban/TEST/WhatsApp Image 2026-10-27 at 20.18.01 (6).jpeg.
Design invariants: Olive leafy cloak, cream eye-patterned tunic, oval red/gold mask, candle crown, hand bell, dark gloves and hanging bronze talismans.

Selected body: C:/Users/asus/.codex/generated_images/01a17efb-9b98-7d02-809a-21bde3865a4c/exec-a593f041-8217-4b5f-935b-d2912544c0e6.png (887 x 1774 RGBA).
Selected effects: C:/Users/asus/.codex/generated_images/01a17efb-9b98-7d02-809a-21bde3865a4c/exec-f711e118-9229-483f-9860-59c9bb8f6de9.png (1536 x 1024 RGBA).


## Layout and verification

Body: exactly 32 distinct successive poses, four per motion. Top-to-bottom: Idle, Move, Windup, Attack, Attack2, Recover, Hurt, Defeated. Quality was prioritized over a larger nominal count. Source analysis found 32 disconnected main opaque silhouettes (alpha >100, component area >500 pixels), with complete silhouettes contained by the explicit rectangles. Canvas edges contain no opaque body pixels. A single layout retry reduced silhouette crowding. Equal grid cutting would still clip some crowns, tails or defeated poses; AtlasLayout.json supplies measured variable cuts and per-frame foot pivots. The PNG remains unchanged. The same pixels-per-unit must be used across every body row.

Effects: exactly 24 frames, six per row: bell sound wave, summon cloud, dust impact, golden eye warning. Real alpha range 0-253; 63.37% of source pixels have alpha below 10. The PNG stores warm RGB in transparent and very low-alpha pixels; a viewer that ignores alpha may expose a golden backdrop. It is not an opaque sheet. Explicit rectangles follow zero-opaque-pixel seams, with row cuts 0,265,525,768,1024 and measured column cuts per row. The first effect generation is retained because its separators were cleaner than the single retry. Do not use a regular 256-square cut. Geometric runtime warnings are the authoritative damage boundary.

AtlasLayout.json records exact SHA-256 hashes, source rectangles in top-left and Unity bottom-left coordinates, pivots, alpha bounds and frame counts. Focused source tests: Assets/OrpheusBelow/Tests/Editor/ShepherdPaintedTests.cs. Unity compilation and runtime/editor tests are centralized by the integration agent; this record does not assert they passed.

## Runtime

ShepherdAttack: rest 2.0 s, locked summon warning 1.2 s, summon animation 0.65 s, flock command 0.65 s, recovery 1.4 s. At most three living sheep; at most two summoned per cast. Spawn points stay at least 2.5 m from the player when planned, are rejected inside solid cover or another sheep, and are skipped if the player enters within 1.5 m before materialization. Summoned sheep use existing ShadeEnemy health/cadence/stun; they are not registered as ordinary wave enemies. Cancel removes all of this shepherd's summons, warnings and effects.

## Exact initial body prompt

```text
Use case: stylized-concept. Asset: production transparent Unity sprite animation atlas, EXACTLY 4 columns by 8 rows = 32 separate full-body poses. Reference image identifies only SHEPHERD character, not sheep. Repaint this masked broad oil-painted guardian: heavy olive leafy cloak, tapering cream tunic with huge gold eye motifs, oval red/gold mask, candle crown, dark gloves, little bronze hand bell and dangling talismans. Preserve reference costume and painterly brushwork. Orthographic three-quarter view facing slightly right, full feet, same size and foot anchor in every equal cell. Portrait atlas 1024x2048 preferred. Transparent RGBA background. Strict invisible equal 4x8 grid with generous empty transparent gutters; one distinct complete isolated shepherd per cell, no props outside their cell, no titles, no borders, no background, no drop shadows. Top-to-bottom rows: 1 Idle breathing four successive poses; 2 Move four walking steps; 3 Windup raising bell high across four poses; 4 Attack summon flock ceremonial bell shake four poses; 5 Attack2 commanding sheep with long arm pointing four poses; 6 Recover lowering bell four poses; 7 Hurt four recoiling poses; 8 Defeated four successive kneeling and collapsed poses. Full silhouette inside each cell including candles, hands, bell, cloak and feet. Stable dimensions, clear animation progression. Do not draw sheep in this atlas.
```

## Exact selected body prompt (single retry)

```text
Use case: stylized-concept. Asset: production transparent Unity sprite animation atlas, EXACTLY 4 columns by 8 rows = 32 separate full-body poses. Reference image identifies only SHEPHERD character, not sheep. Repaint this masked broad oil-painted guardian: heavy olive leafy cloak, tapering cream tunic with huge gold eye motifs, oval red/gold mask, candle crown, dark gloves, little bronze hand bell and dangling talismans. Preserve reference costume and painterly brushwork. Orthographic three-quarter view facing slightly right, full feet, same size and foot anchor in every equal cell. Portrait atlas 1024x2048 preferred. Transparent RGBA background. Strict invisible equal 4x8 grid with generous empty transparent gutters; one distinct complete isolated shepherd per cell, no props outside their cell, no titles, no borders, no background, no drop shadows. Top-to-bottom rows: 1 Idle breathing four successive poses; 2 Move four walking steps; 3 Windup raising bell high across four poses; 4 Attack summon flock ceremonial bell shake four poses; 5 Attack2 commanding sheep with long arm pointing four poses; 6 Recover lowering bell four poses; 7 Hurt four recoiling poses; 8 Defeated four successive kneeling and collapsed poses. Full silhouette inside each cell including candles, hands, bell, cloak and feet. Stable dimensions, clear animation progression. Do not draw sheep in this atlas. CRITICAL LAYOUT CORRECTION: reduce the shepherd sprite to just 70 percent of its equal cell height and width in EVERY cell. Reserve wide EMPTY transparent gutters 25px minimum around the COMPLETE silhouette. No candle flames touch the canvas top. Candle crown and full feet and bell all must fit comfortably inside one equal cell. Four columns eight rows 32 SMALLER sprites with generous whitespace. Prioritize complete unclipped silhouettes over large character size. Genuine transparent background, no red edge colors.
```

## Exact selected effects prompt

```text
Use case: stylized-concept. Transparent game visual effects animation atlas. EXACTLY SIX columns by FOUR rows = 24 separate effects frames. Equal invisible grid, generous transparent gutters, no text no borders no ground no characters. Broad oil-painted texture matches reference ivory, ochre gold, smoke gray and muted moss olive. Row1 six successive expanding curved GOLDEN BELL SOUND WAVES, bronze sparks, clean ring fragments; Row2 six successive SHEEP SUMMON CLOUD puffs, cream wool-like curling clouds with pale golden motes from tiny start through burst to dissipated; Row3 six successive DUST IMPACT puffs with tiny gold spark debris, hit splash spreading and fading; Row4 six successive circular GROUND WARNING sigil, thin golden ring with four eye-like accents swelling from faint to bright. Each frame isolated full effect inside own cell, no spill between cells. All 24 cells occupied, no duplicated frames. True RGBA transparency. Stable center in each equal cell. Match broad oil brushwork and hand-painted folk horror gold tones of the reference. Canvas landscape 1536x1024.
```

## Discarded effects retry prompt

```text
Use case: stylized-concept. Transparent game visual effects animation atlas. EXACTLY SIX columns by FOUR rows = 24 separate effects frames. Equal invisible grid, generous transparent gutters, no text no borders no ground no characters. Broad oil-painted texture matches reference ivory, ochre gold, smoke gray and muted moss olive. Row1 six successive expanding curved GOLDEN BELL SOUND WAVES, bronze sparks, clean ring fragments; Row2 six successive SHEEP SUMMON CLOUD puffs, cream wool-like curling clouds with pale golden motes from tiny start through burst to dissipated; Row3 six successive DUST IMPACT puffs with tiny gold spark debris, hit splash spreading and fading; Row4 six successive circular GROUND WARNING sigil, thin golden ring with four eye-like accents swelling from faint to bright. Each frame isolated full effect inside own cell, no spill between cells. All 24 cells occupied, no duplicated frames. True RGBA transparency. Stable center in each equal cell. Match broad oil brushwork and hand-painted folk horror gold tones of the reference. Canvas landscape 1536x1024. CRITICAL CORRECTION: completely REMOVE the brown/gold gradient backdrop. Output genuinely TRANSPARENT RGBA. Only paint marks of the individual 24 effects may have nonzero alpha. Empty gutters, corners and all background pixels must have alpha zero. Keep every effect comfortably 20 pixels away from its cell boundaries; reduce effect sizes to fit. Do not use a colored or checkered painted background.
```

