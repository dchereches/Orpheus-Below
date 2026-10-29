# Running art provenance

Updated 2026-10-29. The former sixteen-phase reconstructed athletic run is retired. The runtime Run clip uses four intact painted contact/passing keys per direction from `NaturalRunV2`, with western facings mirrored from their eastern counterparts.

`NaturalRunV2/ManualSourceMapping.json` records the selected cells, untouched PNG hashes, ordinary Unity import sizes and manually inspected ankle observations. `NaturalRunV2/GenerationProvenance.json` records the builtin image generation prompts and reference sources. The original idle and walking paintings supplied appearance references; the old Walk/WalkCycle sheets were unsuitable as the final run because their poses repeated the same leading leg.

The builder slices whole connected characters without reconstructing joints or warping sprite geometry. Run and idle share material, tint, alpha handling and the same painted surface. A distance-driven 3.2-unit loop advances four keys at 7.5 frames per second when travelling six units per second; stopping does not keep the legs moving.

The existing `Athletic*.png` files and `AthleticRunLayout.json` are historical source assets, not runtime run bindings. They are retained to preserve earlier source history. No current baker reads that layout or constructs its former mesh animation.

Verification checks source hashes and cell bindings, unchanged sprite geometry/UVs, distance cadence, start/stop, mirrored directions and shared surface state. Native captures exercise every running direction. Geometry and renderer checks complement visual review; distinct frames alone do not prove correct anatomy.
