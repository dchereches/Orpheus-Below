# Artemis tutorial portrait

Generated on 2026-10-27 with the built-in `image_gen` tool using the `imagegen` skill, in its default built-in mode. This is a new fictional Artemis character. No identity photos were supplied.

Final asset: `Assets/OrpheusBelow/Resources/Characters/Artemis/GuidePortrait.png`. The full square portrait is used in the dialogue UI; the painting's pixels are retained unchanged. The UI displays it at a restrained 126 × 126 reference pixels beside the speaker's text.

Style reference only: `C:/Users/asus/.codex/generated_images/01a17c7b-1cbf-7c03-81ec-758a0f1d076d/exec-b158976d-84a6-4e5e-beff-8b58addf7724.png`, inspected before generation. This is the approved broad-oil Eurydice look sheet, used for brushwork only. The prompt explicitly excludes its character identity.

Original tool output: `C:/Users/asus/.codex/generated_images/01a17e62-0a2c-77e1-b710-5aa62cd85cc2/exec-cff79dc0-6876-4227-8be0-f81bc387228e.png`.

Exact prompt:

```text
Use case: stylized-concept
Asset type: small in-game dialogue portrait, square composition, no text.
Primary request: Artemis, the sober Greek huntress and lunar mentor of Orpheus. A new and distinct character, not Eurydice.
Input images: Image 1 is a painterly STYLE reference only. Borrow its broad confident oil strokes, impasto texture and restrained dramatic lighting. Do NOT borrow the red-haired woman's identity, face, hairstyle, outfit, gold laurel or pose. Do not produce a character sheet.
Subject: one composed adult Greek huntress with dark charcoal hair pulled back, clear attentive gray eyes, strong calm facial structure, an ivory chiton and muted forest-green mantle. A modest silver crescent at the brow and a partial wooden bow and quiver just behind the shoulder signal Artemis without busy detail.
Scene/backdrop: simple deep forest-green and charcoal painterly background.
Style/medium: broad confident oil-paint brush strokes and visible canvas, strong planar shapes, classical painterly game art.
Composition/framing: one head-and-shoulders bust filling a square canvas, face centered and large enough to read at 120 pixels, looking slightly to the viewer's right, whole crown and shoulders within frame. No border, vignette ornament or lettering.
Lighting/mood: restrained silver moonlight on ivory fabric and face, calm watchful mentor, dignified.
Color palette: silver, forest green, ivory and charcoal; subdued warm natural skin.
Constraints: distinct from the red-haired reference woman, no red hair, no contemporary clothes, no identity photos, no photorealistic skin, no extra characters, no text, no logo, no watermark.
```

Tool arguments: `transparent_background: false`; `referenced_image_paths` contains only the style reference above.

Visual inspection: one dark-haired huntress, forest mantle, ivory chiton, silver crescent, partial bow and quiver. The face is large and legible; the subject is distinct from red-haired Eurydice. No lettering or watermark is visible.


## Broad loaded-oil repaint — 2026-10-28

The guide portrait was repainted using the built-in `image_gen` tool and the `imagegen` skill in default built-in edit mode. Only this portrait and this provenance record were changed. Artemis retains her original female identity, anatomy, dark hair, silver crescent/laurel, ivory chiton, green mantle, bow, quiver, palette and square portrait framing.

The selected second candidate has larger connected hair, garment and background masses, long directional loaded brush streaks and less fine detail. The original and both candidates were inspected at native size and together at the current approximately 151 × 151 reference-pixel UI portrait scale. Face and props remain clear at that scale. Fine impasto ridges remain visible at native resolution. An inspection-only composite was created; the production PNG was copied from the generated output without resizing, filters, alpha modification or other pixel postprocessing.

- Final asset: `Assets/OrpheusBelow/Resources/Characters/Artemis/GuidePortrait.png`
- Original edit target backup: `C:/Users/asus/.codex/generated_images/01a17fd7-27a4-7c53-82f7-c2aef376b767/artemis-source-before-repaint.png`
- Original target SHA-256: `a9cd840dbcdcce165c6723a4986ebb485dbd8afa576c20c8827b17ea452179f6`
- Original and final dimensions: 1254 × 1254 pixels.
- Original and final mode: RGB, fully opaque; alpha inspected through RGBA conversion has extrema 255–255. The original opaque forest background was preserved.
- Style-only input for first pass: `C:/Users/asus/.codex/generated_images/01a17f97-6cdb-7291-b318-3bdb0835e4ca/exec-e2c9561e-9106-4d99-b43e-aeeb01c076ef.png`. This is a Cerberus sheet, used only for connected simplified paint masses; no subject or sheet format was borrowed.
- First candidate: `C:/Users/asus/.codex/generated_images/01a17fd7-27a4-7c53-82f7-c2aef376b767/exec-d39bc5b7-f1eb-4b5b-a65c-7f7f965cabb5.png`
- First candidate SHA-256: `1af2c7e4b775e157f1aff5e06fe0b8e4cdba2156e0d25735658cd5b667482606`
- Selected generated output: `C:/Users/asus/.codex/generated_images/01a17fd7-27a4-7c53-82f7-c2aef376b767/exec-0da13a83-7000-4063-b1a9-8dcbe354c04f.png`
- Selected output and final asset SHA-256: `d16e293959e51a5dd9d60085dec6ab0ad28b2414f9e33192e4e6a92b2aef220e`
- 151-pixel inspection composite: `C:/Users/asus/.codex/generated_images/01a17fd7-27a4-7c53-82f7-c2aef376b767/artemis-151px-inspection.png`

First-pass exact prompt (input 1: original `GuidePortrait.png`; input 2: style-only reference above; `transparent_background: false`):

```text
Use case: style-transfer
Asset type: existing square in-game Artemis dialogue portrait, readable at 151 pixels.
Input images: Image 1 is the EDIT TARGET and the sole authority for Artemis's identity, costume, face, anatomy, pose, palette, framing and objects. Image 2 is STYLE ONLY: use only the connected simplified painted masses; never borrow its animal subject or its sheet format.
Primary request: Repaint Image 1 as a boldly simplified THICK OIL PAINTING. The requested change is brushwork and level of detail only. Keep exactly the same dark-haired adult Greek huntress woman Artemis and the same head-and-shoulders portrait composition.
Strict invariants: keep her original feminine face identity and anatomy, calm watchful expression, gray eyes and gaze, original head angle, dark charcoal swept-back hair, small silver crescent circlet, silver laurel sprigs, ivory diagonal chiton, forest-green mantle with round shoulder clasp, left-shoulder quiver with pale arrow feathers, and partial wooden bow on the right. Same natural skin tones, silver/ivory/charcoal/deep green palette. Preserve the large centered face and the existing original opaque deep forest-green painted background. No new objects, no changed costume, no change of character.
Style/medium: large CONNECTED paint masses, very broad confident brush strokes, LONG VISIBLE loaded flat-brush streaks, buttery thick oil dragged in one direction, clear ridged impasto. Reduce at least 80 percent of tiny texture, fine hairs, skin detail and cloth details. Cheeks and forehead each built from only a few large planar strokes with clean anatomy; hair built from broad dark ribbons; mantle and chiton from wide sweeping paint bands. Keep eyes and mouth legible but simplified. Deliberately rough painterly stroke edges. The whole image should read as a hand-painted oil sketch made with a very large brush, not a detailed illustration covered in a texture filter.
Avoid: small flecked strokes, stippling, pixelated marks, tiny noisy impasto, cracked plaster/canvas all over skin, pore detail, delicate eyelashes, individual fine hairs, airbrushing, photographic rendering, smooth CGI, outlines, extra jewelry, red hair, Eurydice identity, animals, character sheet, collage, text, logos, border, watermark.
Return one square portrait only.
```

Targeted correction exact prompt (input 1: first candidate above; `transparent_background: false`):

```text
Use case: style-transfer
Edit ONLY Image 1, an existing Artemis portrait. This is a targeted correction to the painting style. Keep her identity, face proportions, female anatomy, head pose, gaze, crop, dark hair, crescent circlet, laurel, green mantle, ivory chiton, shoulder clasp, bow and quiver exactly the same.
The current image is MUCH TOO detailed and its skin paint is too small and crackled. Radically simplify it into a rough large-brush THICK OIL PAINT portrait with giant connected strokes. The face should be constructed from about twenty generous loaded brushstrokes TOTAL rather than hundreds of small marks. One uninterrupted long peach stroke across the entire forehead, one broad warm plane across each cheek, one cool broad plane under the jaw, one unified neck light, a few decisive strokes for eyes and nose and lips. Remove skin pores, hairline detail, fine eyelashes and all the tiny shattered dry flecks. Face anatomy and recognition MUST remain correct. The target is a bold loose portrait study by a master oil painter made with a 2-inch loaded flat brush in ten minutes, large blade-shaped strokes with obvious long bristle drag and deep raised oil ridges. Huge simple connected silhouette masses in hair and clothes. Each main cloth fold formed by a single stroke, only 4–6 folds. Maintain the same opaque forest background with just a few very broad diagonal green strokes.
Absolute style priority: LARGE CONNECTED COLOR MASSES and LONG VISIBLE BUTTERY LOADED BRUSH STREAKS over every part of the painting including skin. Reduce detail drastically. Rough wet painted edges. No mosaic, no tiny scratchy fleck marks, no fine crackle, no pebble texture, no speckling, no pixelation, no photorealism, no filter-like surface over detailed rendering. Same subject and composition. No text, frame, watermark or new objects. One square portrait.
```
