# Chamber mapping and natural running implementation plan

> **For agentic workers:** Use superpowers:subagent-driven-development, one agent for each chamber and a separate animation agent. Root integrates and verifies.

**Goal:** Match Orpheus's original idle/walking art and natural locomotion, and make all eight chambers' accessible ground and visual occlusion follow the supplied paintings.

**Architecture:** Replace the rejected procedural leg mesh with untouched authored locomotion sprites. Each chamber has an independently reviewed original-pixel JSON trace; scene authoring persists its floor, base colliders and visual silhouettes. Validate the saved runtime scenes and actual player movement, not only JSON geometry.

**Tech stack:** Unity 6000.3.10f1/HDRP, C#, existing authored PNG/JPEG assets, Python/Pillow for annotated mapping evidence.

**Spec:** Approved user request in this chat: original idle/walk style and opacity, natural run rather than raised-knee exercise; one agent per chamber to precisely map accessible ground, solid bases, and places the player can go behind objects.

## Constraints and review focus

- Preserve newly pulled input-selection UI and unrelated changes; base afac844.
- Root owns Unity launches, shared authoring/collision code, test integration and Git. Three worker slots run concurrently.
- Do not infer solid ground from tree canopies or shadows. Trace base contact and full visible silhouette separately.
- No fabricated bridges across lava/water; inaccessible distant islands remain outside the reachable floor.
- Match original Orpheus idle/walk identity and paint, with identical shader/tint/opacity across idle and run; remove procedural high-knee deformation and surface compensation. Existing locomotion sheets fail support-leg alternation, so new reference-based full-body poses require visual review before integration.
- Keep convex-hull physics from filling concave footprint notches; validate radius and sweep against saved scene colliders.
- Validate entrance, return arrival, encounter and enemy positions, graph connectivity, gates, narrow routes, behind/front occlusion and dashes.
- Record ambiguity in partly hidden roots and foreground architecture rather than implying pixel-exact certainty.

## Tasks

- [x] Animation agent: compare source sheets; install four reviewed contact/passing keys from five canonical painted views, mirroring west; remove rejected deformation/filter; update motion and visual regressions; root rebakes and captures.
- [x] Chamber agents 00–07: independently inspect full painting and coordinate crops, trace floor/base/silhouette, author positive/negative and behind/front probes, check .45-radius connectivity, save annotated overlays and ambiguity notes.
- [x] Root: integrate reviewed trace schema and strict authoring checks; rebake all eight layouts/scenes.
- [x] Root: test concave solid-footprint physics, actual controller movement/wall sliding/dashes and source-pixel occlusion in every saved chamber.
- [x] Review agent: assess geometry consistency, source overlays and run visual continuity; resolve material findings.
- [x] Root: run EditMode suite and native gameplay captures and inspect visual results.
- [ ] Root: publish the verified commit, merge to main, and pull the result into the original checkout as authorized in the ongoing request.

## Verification evidence

Each map supplies at least 12 actual-floor and 12 blocked-painting probes, three behind/front pairs, all journey/enemy anchors and an annotated source overlay. Native verification must reopen each saved scene, use real controller movement and physics, and show cover enabled/disabled pairs at walkable positions. Run verification compares original reference sprite geometry and renderer state, includes all directions, start/stop/stairs and clips of movement. Known pre-existing guardian-damage test failure must be reported separately, never called a clean suite.

## Animation source review

The original Walk sheet has eight drawings per direction, but its side-view poses keep the same leading leg. WalkCycle and RunCycle repeat contact/recovery on that same leg. Distinct sprite references therefore do not establish an alternating gait, and playback of those sources alone is not an acceptable completion.

NaturalRunV2 sources are provisionally approved for native comparison as four keys: contact A, passing A, contact B, passing B. East selects source cells 0, 2, 4 and 6 from its 4 by 2 atlas; foreground passing overlap reverses visibly between source cells 2 and 6. South, SouthEast, NorthEast and North select the first three cells of their Four 2 by 2 atlases, then use a separately generated single passing-B sprite. South uses SouthPassBFinal, not the rejected SouthPassB. Old North/NorthEast eight-pose sheets are also excluded. A 3.2-unit distance cycle gives 7.5 keys per second at speed six.

The coat partly hides thigh junctions at the southeast/northeast contacts, so those contact-leg identities remain documented ambiguities. Passing-B singles can also change upper-body identity or proportions. Native playback acceptance is required. Source mapping, actual image dimensions, generated-source SHA256 values and only high-confidence painted ankle observations are saved in Assets/OrpheusBelow/Resources/Characters/Orpheus/BroadOil/NaturalRunV2/ManualSourceMapping.json; all recorded observation points were sampled on actual nontransparent paint. Low-confidence metadata is retained in generation provenance and is not promoted into anatomical assertions.

Keep source PNG bytes unchanged. At alpha >= 96 the original idle East bodies measure 120–124 pixels high. Use ordinary Unity maxTextureSize 256 for Four atlases, 128 for single passing-B images and 512 for East. These target about 110 pixels for the former sources and 142–143 for East; East at 256 would be only about 71. Use uncompressed, no mipmaps, NPOT unchanged and the same bilinear/alpha handling as idle. This is normal texture import resampling; the shared surface has no run-only blur, tint, desaturation or opacity adjustment. Root must inspect the imported native result before final acceptance.

## Reviewed corrections and evidence

- The original convex solid collider filled concave cover notches. Three native physics regressions were first RED (two failures), then GREEN after preserving the static concave mesh and moving boundary-wall thickness outside the traced floor.
- Native saved-scene verification passed all eight chambers, 331 valid placements, 181 negative painting probes, 32 actual controller routes and 28 front/behind cover pairs, with zero runtime errors. A final repeat follows the three source-position corrections below.
- The checker uses swept eight-direction connectivity. It refines from eight to four source pixels only when a required anchor cannot connect; Chamber 05 needs this finer grid. Mathematical and actual production controller sweeps remain required on every graph edge.
- Independent review found a separate production enemy-navigation defect: its coarse cardinal graph disconnected legal passages in Chambers 05 and 06. Two native Ash Garden pursuit cases failed before the bounded cached 16→8→4 pixel fix. Diagonal edges still require exact circle clearance, and the connection-mask cache now distinguishes a fully connected node from its uninitialized sentinel.
- Repeated coordinate transforms were removed from enemy-clearance queries by reusing the grid's existing world-edge cache. Eight equivalence cases compare independent source witnesses and edge-near positions with the original floor query at four radii.
- Native art verification passed four intact keys in all canonical directions, mirrored western bindings, shared idle/run surface state and real movement. The first South passing-B source had a visible head/body proportion change; a targeted builtin imagegen edit replaced it. Both root and an independent reviewer accepted the fresh native South captures. No final PNG was repainted, warped or filtered outside normal Unity texture import.
- Existing test-only cardinal lattices and two obsolete source coordinates were replaced with swept production routes while retaining actual escort following, floor clearance and destination assertions. The former-well check now crosses its original stored position without entering the painted Titan arm.
- Source audit corrected Chamber 04 returnSpawn (413,482)→(438,490), raising its exit distance from 3.199 to 4.533 units. Enemy source 3 moved (696,619)→(730,654) in Chamber 04 and (601,652)→(635,675) in Chamber 05 to separate the actual generated wave slots from their encounter-center mini-boss. All actual wave pairs across both journey directions now exceed 1.6 units. Floor/base/silhouette polygons and independent probes were unchanged by these corrections.
- The first complete suite ran 527 tests: 523 passed, three new issues above were identified and corrected, and the pre-existing guardian off-beat damage failure remained (expected 1584, actual 0). Final suite and Git integration are recorded after the final native repeat.

QA evidence is kept outside the tracked game at D:/Serban/software-projects/Orpheus-Below/output/chamber-mapping-natural-run-2026-10-29/index.html, including source overlays, final coordinates, native scene reports and cyclic capture previews. PNG source selection and exact generation prompts are tracked in NaturalRunV2/ManualSourceMapping.json and GenerationProvenance.json.


Final verification: the rebuilt saved scenes passed all eight chambers again after the position corrections, with zero runtime errors; the final native art run also passed with zero runtime errors. The final EditMode run completed 536 tests: 535 passed, and only the previously established OffBeatOrdinaryDamageCannotDefeatGuardian failure remained (expected 1584, actual 0). New clearance-equivalence, escort, arrival-spacing and both encounter-wave regressions all passed. Git publication follows this verification commit.
