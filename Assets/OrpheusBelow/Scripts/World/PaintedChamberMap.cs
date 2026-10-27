using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace OrpheusBelow
{
    public sealed class PaintedChamberMap : MonoBehaviour
    {
        public PaintedChamberDefinition definition;
        public Transform coverRoot;
        public Transform overlayRoot;
        public ChamberExitDoor exitDoor;
        public MeshRenderer background;
        public bool showNavigationGizmos = true;
        public static PaintedChamberMap Active { get; private set; }

        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private readonly List<Material> _ownedMaterials = new List<Material>();
        private readonly List<Sprite> _ownedSprites = new List<Sprite>();
        private readonly List<Texture2D> _ownedTextures = new List<Texture2D>();
        private HadesChamberDirector.ChamberRuntimeData _runtime;
        public Vector3 ToWorld(Vector2 pixel) => transform.position + definition.ImageToGround(pixel);
        public Vector2 ToImage(Vector3 world) => definition.GroundToImage(world - transform.position);
        public bool IsWalkable(Vector3 world, float radius = .4f) => definition.IsWalkable(world - transform.position, radius);
        public Vector3 NearestWalkable(Vector3 world, float radius = .6f) => transform.position + definition.NearestWalkable(world - transform.position, radius);
        public static int DepthOrder(float imageY) => Mathf.RoundToInt(imageY * 8f);

        private void OnEnable() { Active = this; }
        private void OnDisable() { if (Active == this) Active = null; }
        public void MakeActive() { Active = this; }

        public static PaintedChamberMap Build(Transform parent, PaintedChamberDefinition layout, Vector3 center)
        {
            var go = new GameObject("PaintedChamber_" + layout.index.ToString("D2") + "_" + layout.title);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var map = go.AddComponent<PaintedChamberMap>();
            map.definition = layout;
            map.BuildGeometry();
            return map;
        }

        private Material Material(Texture texture, Color tint)
        {
            var material = new Material(Shader.Find("OrpheusBelow/PaintedChamber"));
            material.name = definition.name + "_Paint";
            material.mainTexture = texture;
            material.color = tint;
            _ownedMaterials.Add(material);
            return material;
        }

        private void BuildGeometry()
        {
            var material = Material(definition.painting, Color.white);
            background = PaintedShape("Painting", transform,
                new[] { Vector2.zero, new Vector2(1536, 0), new Vector2(1536, 1024), new Vector2(0, 1024) }, material, -30000);
            background.transform.localPosition = RenderPipeline2DSetup.IsoCameraRotation * Vector3.forward * 48f;

            var boundary = new GameObject("NavigationBoundary_Solid_NoAccess").transform;
            boundary.SetParent(transform, false);
            for (int i = 0; i < definition.walkableBoundary.Length; i++)
            {
                Vector3 a = definition.ImageToGround(definition.walkableBoundary[i]);
                Vector3 b = definition.ImageToGround(definition.walkableBoundary[(i + 1) % definition.walkableBoundary.Length]);
                var wall = new GameObject("Boundary_" + i.ToString("D2"));
                wall.transform.SetParent(boundary, false);
                wall.transform.localPosition = (a + b) * .5f + Vector3.up * 2.5f;
                wall.transform.localRotation = Quaternion.LookRotation(b - a);
                wall.AddComponent<BoxCollider>().size = new Vector3(.3f, 5f, (b - a).magnitude + .2f);
                wall.AddComponent<IsoObstacle>();
            }

            coverRoot = new GameObject("AuthoredCover_Solid").transform;
            coverRoot.SetParent(transform, false);
            foreach (var obstacle in definition.cover)
            {
                var go = new GameObject(obstacle.name);
                go.transform.SetParent(coverRoot, false);
                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = Extrude(obstacle.footprint, obstacle.height);
                collider.convex = true;
                go.AddComponent<IsoObstacle>();
                if (obstacle.silhouette == null || obstacle.silhouette.Length < 3) continue;
                float front = 0f;
                foreach (Vector2 foot in obstacle.footprint) front = Mathf.Max(front, foot.y);
                var cutout = new GameObject("PaintedOcclusion");
                cutout.transform.SetParent(go.transform, false);
                cutout.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
                var sprite = CreateOcclusion(obstacle, out Vector2 center);
                float depth = Vector3.Dot(definition.ImageToGround(new Vector2(center.x, front)),
                    RenderPipeline2DSetup.IsoCameraRotation * Vector3.forward);
                cutout.transform.localPosition = RenderPipeline2DSetup.IsoCameraRotation * new Vector3(
                    (center.x / 1536f - .5f) * definition.imageWidth,
                    (.5f - center.y / 1024f) * definition.imageHeight, depth);
                sprite.name = obstacle.name + "_Occlusion";
                _ownedSprites.Add(sprite);
                var renderer = cutout.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                var coverMaterial = Material(sprite.texture, Color.white);
                coverMaterial.shader = Shader.Find("OrpheusBelow/PaintedCover");
                renderer.sharedMaterial = coverMaterial;
                renderer.sortingOrder = DepthOrder(front);
            }

            var exit = new GameObject("PaintedExit_NextChamber");
            exit.transform.SetParent(transform, false);
            exit.transform.position = ToWorld(definition.exit);
            exit.transform.rotation = Quaternion.LookRotation(UnderworldCamera2D.IsoUpXZ);
            var trigger = exit.AddComponent<BoxCollider>();
            trigger.isTrigger = true; trigger.center = Vector3.up; trigger.size = new Vector3(2.6f, 2.5f, 2.2f);
            exitDoor = exit.AddComponent<ChamberExitDoor>();
            exitDoor.targetChamberIndex = definition.index + 1;
            exitDoor.isFinalSunlightExit = definition.index == 4;
            var marker = new GameObject("ExitThreshold");
            marker.transform.SetParent(exit.transform, false);
            marker.transform.localPosition = Vector3.up * .035f;
            marker.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            var beacon = RenderPipeline2DSetup.AttachGroundDecal(marker, RenderPipeline2DSetup.RingSprite, Color.white, 20);
            exitDoor.SetupVisuals(null, beacon);
            BuildOverlay();
        }

        private Sprite CreateOcclusion(PaintedChamberDefinition.Cover obstacle, out Vector2 center)
        {
            // Bake cropped alpha masks: OverrideGeometry cannot modify sprites during editor scene authoring.
            Vector2 min = obstacle.silhouette[0], max = min;
            foreach (Vector2 p in obstacle.silhouette) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            int left = Mathf.FloorToInt(min.x), top = Mathf.FloorToInt(min.y);
            int width = Mathf.CeilToInt(max.x) - left, height = Mathf.CeilToInt(max.y) - top;
            var pixels = definition.painting.GetPixels(left, 1024 - top - height, width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (!PaintedPolygon.Contains(obstacle.silhouette, new Vector2(left + x + .5f, top + height - y - .5f)))
                        pixels[y * width + x].a = 0f;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = obstacle.name + "_OcclusionMask", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels); texture.Apply(false, true);
            _ownedTextures.Add(texture);
            center = new Vector2(left + width * .5f, top + height * .5f);
            return Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * .5f, 1536f / definition.imageWidth, 0, SpriteMeshType.FullRect);
        }

        private MeshRenderer PaintedShape(string name, Transform parent, Vector2[] polygon, Material material, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
            var mesh = new Mesh { name = name };
            var vertices = new Vector3[polygon.Length];
            var uv = new Vector2[polygon.Length];
            var colors = new Color[polygon.Length];
            for (int i = 0; i < polygon.Length; i++)
            {
                uv[i] = new Vector2(polygon[i].x / 1536f, 1f - polygon[i].y / 1024f);
                vertices[i] = new Vector3((uv[i].x - .5f) * definition.imageWidth, (uv[i].y - .5f) * definition.imageHeight, 0f);
                colors[i] = Color.white;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors;
            mesh.triangles = PaintedPolygon.Triangulate(polygon);
            mesh.RecalculateBounds();
            _ownedMeshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = order;
            return renderer;
        }

        private Mesh Extrude(Vector2[] polygon, float height)
        {
            int n = polygon.Length;
            var vertices = new Vector3[n * 2];
            var triangles = new List<int>();
            int[] face = PaintedPolygon.Triangulate(polygon);
            for (int i = 0; i < n; i++)
            {
                vertices[i] = definition.ImageToGround(polygon[i]) + Vector3.down * .15f;
                vertices[i + n] = vertices[i] + Vector3.up * (height + .15f);
                int j = (i + 1) % n;
                // Both windings make the static cover robust to either authored polygon winding.
                triangles.AddRange(new[] { i, j, j + n, i, j + n, i + n, j, i, i + n, j, i + n, j + n });
            }
            for (int i = 0; i < face.Length; i += 3)
                triangles.AddRange(new[] { face[i] + n, face[i + 1] + n, face[i + 2] + n,
                    face[i + 2] + n, face[i + 1] + n, face[i] + n });
            var mesh = new Mesh { name = "CoverFootprint" };
            mesh.vertices = vertices; mesh.triangles = triangles.ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            _ownedMeshes.Add(mesh);
            return mesh;
        }

        private void BuildOverlay()
        {
            overlayRoot = new GameObject("F3_NavigationOverlay_GreenWalk_RedCover_CyanExit").transform;
            overlayRoot.SetParent(transform, false);
            var green = Material(Texture2D.whiteTexture, new Color(.12f, .9f, .4f, .16f));
            var red = Material(Texture2D.whiteTexture, new Color(1f, .12f, .15f, .48f));
            var cyan = Material(Texture2D.whiteTexture, new Color(.1f, .85f, 1f, .65f));
            var outside = Material(Texture2D.whiteTexture, new Color(.65f, .05f, .1f, .16f));
            PaintedShape("InaccessibleExterior", overlayRoot,
                new[] { Vector2.zero, new Vector2(1536, 0), new Vector2(1536, 1024), new Vector2(0, 1024) }, outside, 30999);
            PaintedShape("AccessibleFloor", overlayRoot, definition.walkableBoundary, green, 31000);
            foreach (var obstacle in definition.cover)
                PaintedShape("Blocked_" + obstacle.name, overlayRoot, obstacle.footprint, red, 31001);
            Vector2 p = definition.exit;
            PaintedShape("Exit", overlayRoot, new[] { p + new Vector2(-28, -18), p + new Vector2(28, -18),
                p + new Vector2(28, 18), p + new Vector2(-28, 18) }, cyan, 31002);
            overlayRoot.gameObject.SetActive(false);
        }

        public HadesChamberDirector.ChamberRuntimeData RuntimeData()
        {
            if (_runtime != null) return _runtime;
            Vector3 arena = NearestWalkable(ToWorld(definition.encounterCenter), 1f);
            CerberusRhythmBoss guardian = null;
            if (definition.hasGuardian)
            {
                var go = new GameObject("Guardian_" + definition.guardian);
                go.transform.SetParent(transform, false);
                guardian = go.AddComponent<CerberusRhythmBoss>();
                guardian.ConfigureBoss(definition.guardian, arena);
            }
            var rigObject = new GameObject("PaintedGuardianParallax");
            rigObject.transform.SetParent(transform, false);
            var rig = rigObject.AddComponent<UnderworldParallaxRig>();
            rig.farAbyssSkySprite = Sprite.Create(definition.painting,
                new Rect(0, 0, definition.painting.width, definition.painting.height), Vector2.one * .5f, 100f);
            rig.Initialize(arena, new Color(.35f, .25f, .35f), new Color(.12f, .10f, .15f), new Color(.8f, .55f, .25f, .2f));
            AmbrosiaHealingWell.Spawn(transform, NearestWalkable(ToWorld(definition.well), .8f));
            _runtime = new HadesChamberDirector.ChamberRuntimeData
            {
                chamberTitle = (definition.index + 1) + "  ·  " + definition.title,
                roomRoot = gameObject, centerWorld = arena,
                playerSpawnWorld = NearestWalkable(ToWorld(definition.entrance), .65f),
                cameraLeashRadius = 200f, cameraOrthoOverride = definition.zoom,
                exitDoor = exitDoor, guardianBoss = guardian, interiorObstaclesRoot = coverRoot,
                wave1 = Wave(1, arena), wave2 = Wave(2, arena)
            };
            return _runtime;
        }

        private List<HadesChamberDirector.WaveSpawnSpec> Wave(int number, Vector3 center)
        {
            var wave = ProceduralChamberGenerator.GenerateWaveSpecs(definition.index, number);
            for (int i = 0; i < wave.Count; i++)
            {
                Vector3 point = NearestWalkable(ToWorld(definition.enemySpawns[(i + (number - 1) * 2) % definition.enemySpawns.Length]), .8f);
                wave[i].localOffsetXZ = new Vector2(point.x - center.x, point.z - center.z);
            }
            return wave;
        }

        public Vector3 ClampCameraFocus(Vector3 focus, float orthoSize, float aspect)
        {
            Vector3 local = focus - transform.position;
            Vector3 up = RenderPipeline2DSetup.IsoCameraRotation * Vector3.up;
            float halfWidth = Mathf.Max(0f, definition.imageWidth * .5f - orthoSize * aspect - .8f);
            float halfHeight = Mathf.Max(0f, definition.imageHeight * .5f - orthoSize - .8f);
            float x = Mathf.Clamp(Vector3.Dot(local, UnderworldCamera2D.IsoRightXZ), -halfWidth, halfWidth);
            float y = Mathf.Clamp(Vector3.Dot(local, up), -halfHeight, halfHeight);
            Vector3 forward = RenderPipeline2DSetup.IsoCameraRotation * Vector3.forward;
            float depthLimit = definition.imageHeight * .5f / Mathf.Tan(36f * Mathf.Deg2Rad);
            float depth = Mathf.Clamp(Vector3.Dot(local, forward), -depthLimit, depthLimit);
            return transform.position + UnderworldCamera2D.IsoRightXZ * x + up * y + forward * depth;
        }

        public float SafeZoom(float requested, float aspect) => Mathf.Min(requested,
            definition.imageHeight * .5f - 1f, (definition.imageWidth * .5f - 1f) / Mathf.Max(.1f, aspect));

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame && overlayRoot != null)
                overlayRoot.gameObject.SetActive(!overlayRoot.gameObject.activeSelf);
        }

        private void LateUpdate()
        {
            bool exploration = UnderworldCamera2D.Instance == null || UnderworldCamera2D.Instance.SideViewBlend01 < .1f;
            if (background != null) background.enabled = exploration;
        }

        private void OnDrawGizmos()
        {
            if (!showNavigationGizmos || definition == null) return;
            DrawBoundary(definition.walkableBoundary, new Color(.2f, 1f, .4f));
            foreach (var obstacle in definition.cover) DrawBoundary(obstacle.footprint, Color.red);
            if (definition.stairs != null) foreach (var stair in definition.stairs)
            {
                DrawBoundary(stair.polygon, new Color(.95f, .65f, .15f));
                Vector2 center = Vector2.zero; foreach (var p in stair.polygon) center += p;
                center /= stair.polygon.Length;
                Gizmos.color = new Color(.95f, .65f, .15f);
                Gizmos.DrawLine(ToWorld(center), ToWorld(center + stair.uphill * .3f));
            }
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(ToWorld(definition.exit), 1.2f);
        }

        private void DrawBoundary(Vector2[] polygon, Color color)
        {
            Gizmos.color = color;
            for (int i = 0; i < polygon.Length; i++) Gizmos.DrawLine(ToWorld(polygon[i]) + Vector3.up * .1f,
                ToWorld(polygon[(i + 1) % polygon.Length]) + Vector3.up * .1f);
        }

        // The editor persists generated meshes/materials as scene sub-assets before saving.
        public void MarkResourcesPersistent() { _ownedMeshes.Clear(); _ownedMaterials.Clear(); _ownedSprites.Clear(); _ownedTextures.Clear(); }
        private void OnDestroy()
        {
            foreach (var mesh in _ownedMeshes) if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            foreach (var material in _ownedMaterials) if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            foreach (var sprite in _ownedSprites) if (sprite != null) { if (Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite); }
            foreach (var texture in _ownedTextures) if (texture != null) { if (Application.isPlaying) Destroy(texture); else DestroyImmediate(texture); }
        }
    }

}
