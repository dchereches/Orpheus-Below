# Orpheus broad-oil runtime atlases

Generated on 2026-10-28 with the built-in image-generation tool in image-reference mode.

Private references (read in place and deliberately not copied into the repository):

- `D:/Serban/Test/WhatsApp Image 2026-10-28 at 16.12.58.jpeg` — character identity, costume, lyre, broad-oil handling.
- `D:/Serban/Test/WhatsApp Image 2026-10-28 at 16.14.51.jpeg` — combat readability, directional shadow, hot/cool contrast only.

All 11 PNG files are original generated RGBA sources. Hidden RGB may retain a brown matte under fully transparent pixels; alpha is authoritative and every atlas has well over 20% clear pixels. The editor baker never rewrites PNG bytes.

| Atlas | SHA-256 |
|---|---|
| `IdleTransitions.png` | `89AE79F9C14AFFF748FA44ECAE3BA01E34F5438A1ABE12F4D9E69FBF2CF9E4F5` |
| `Walk.png` | `4BA3C0E58DDC31B8F570AB106DE5618FE561209AACA3A1C6944181BC3028A531` |
| `Run.png` | `93728AA941E79060B0708AE28FE5BC0AAE0BDF18B16496DC177F160EEBEC280D` |
| `DashStairs.png` | `4F9C18538ED1FD4ED452E96033D308BF89B3F1A5C78D332F9F77AAE37815ACFA` |
| `SoftCombat.png` | `6AACFA4AF64C24D1EEF99646C9DDFD2BB7CBBC2073D918FB6073561A64CAFCB3` |
| `HardCadence.png` | `2BD4E8BFB11D3D3699EDCA42714C2589A12BF7742977B6214B1EDEC7965DD844` |
| `LyreSinging.png` | `34B7F2C0B9ED7B96D522895DD7F3D8263BF336541430BC31C0CF9EE5D49CA96A` |
| `Aulos.png` | `37D169FB94E7E3EB1B5347C6F2039FA21D92A855E5000AE9E565BC5E1D1CB342` |
| `Reactions.png` | `9A398C4C5D5D000E55199AF1919D65477C6599BA1237FB1FCEDAA612CA774E61` |
| `Narrative.png` | `7319770A9FCE2D1C33C95FF1B7DD1824E049B9782CD345C2551FA1FAB608A759` |
| `Effects.png` | `11C1428716C0F888B6A2D4796F0E2D20065AB3097F920908B5FBC0B7BE4D00A1` |

The original PNGs above remain unchanged. On 2026-10-29 two additional native RGBA sources were generated with the built-in image tool to replace the nearly stationary locomotion cycles:

| Atlas | SHA-256 |
|---|---|
| `RunCycle.png` | `2900F95275DE164A3970B6E6FF89229B79ECBDA70AD20026BBEA593B81CF9872` |
| `WalkCycle.png` | `D2654AEF623AA7BD7770B4B3C6FBC72C47671F09548A813795AC3B65C5F05F9B` |

`OrpheusBroadOilAssetBuilder` uses `OrpheusAtlasSlicer` to isolate connected figures, exclude neighboring alpha islands with sprite geometry, and anchor each figure on its boots. The singing source has seven rows, not eight; its five consistent directions supply vocal clips. The library preserves 48 named motions, eight controlled directions, explicit western mirroring, and the 3.9-world-unit standing height.

The generated `RunCycle.png` is **not a complete anatomical cycle**: columns zero/two repeat the same leading leg, and columns one/three lift the same knee. Playback speed and frame-count assertions cannot repair or detect this. The PNG is retained unchanged as a paint source.

`OrpheusBilateralRunBaker` extracts the intact trouser/boot paint from source column zero in each of the five facings. It removes both original leg silhouettes from the isolated torso/cape geometry, constructs separate far and near painted limbs, and skins each with a hip, knee, and ankle. Far leg, torso/cape, and near leg are drawn in that order. The original face, upper body, lyre, cloak, alpha edges, and oil texture remain the source artwork; no bitmap is repainted or resampled by the baker.

Deforming limbs use a conforming two-pixel grid with shared GPU vertices. The affine torso keeps horizontal runs. Original pelvis/cape paint is retained through the hip seam, and limb roots are pinned to the torso before blending into the bent leg. This prevents curved-strip T-junction cracks and transparent waist cutouts; native GPU screenshots are required in addition to contact sheets.

That eight-phase,3.2-unit leg-only repair is superseded by the full-body athletic cycle below. It solved anatomical leg alternation but left the original crouched torso and arms frozen, which the user correctly identified as sneaking.

`OrpheusRunCycleTests` projects actual visible boot texels through sprite triangles. It requires opposite near/far motion across half a stride in every source facing, alternate boot lift at passing poses, and a planted east-facing boot after accounting for the body's ground displacement. Contact sheets must interpolate sprite UVs through triangles; masking the original atlas rectangle does not display skinned geometry correctly.

Additional regressions compare rendered material edges at intermediate UV vertices around the knee and sample actual opaque coverage at the retained waist/cape junction throughout the cycle.

Exact prompts are recorded in `GenerationPrompts.json`; cell semantics are in `AtlasLayout.json`.

## Current full-body athletic run

Five new upright canonical paintings (`AthleticSouth`, `AthleticSouthEast`, `AthleticEast`, `AthleticNorthEast`, `AthleticNorth`) supply complete anatomical paint. `AthleticRunLayout.json` records their native-pixel masks/joints. `OrpheusAthleticRunBaker` poses far arm, far leg, retained torso/cape/head, near leg and near arm on a welded2px lattice. The retained body keeps its largest connected island to remove remnants of the source limbs.

`OrpheusRunMotion` defines a5.8-unit cycle with .34 support duty, two flight intervals, high heel recovery and knee drive. Constant-length legs fit each reference so the pelvis remains upright. Elbows counter the legs at90degrees with a waist-to-chest arm pump. Sixteen phases per direction create80 distinct poses and128 mirrored direction slots. The animator advances by actual ground distance and compensates visual scale, including the .70 boss billboard. Exact posed renderer bounds prevent extended feet/hands being culled outside the original painting's rectangle.

`OrpheusAthleticRunMotionTests` checks support, flight, heel recovery, elbow opposition/angle, bone lengths, upright pelvis and loop closure. `OrpheusRunCycleTests` projects actual painted boot/fist/hair texels to check the production art's motion, scaled support contact, torso rise and culling coverage. Native GPU motion review remains required; frame-count or contact-sheet checks alone do not establish animation quality. Source generation prompts and discarded-sheet rationale are recorded in `AthleticRunGeneration.md`.
