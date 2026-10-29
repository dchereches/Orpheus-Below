using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrpheusBelow
{
    /// <summary>Exploration scenery, deliberately outside the playable floor. Geometry is batched by depth.</summary>
    [DefaultExecutionOrder(80)]
    public sealed class UnderworldEnvironmentArt : MonoBehaviour
    {
        private sealed class Layer
        {
            public Transform root;
            public Vector3 origin;
            public float drift;
            public MeshRenderer renderer;
            public Color[] palette;
            public bool foreground;
            public float opacity = 1f;
        }

        private struct Atmosphere
        {
            public SpriteRenderer renderer;
            public Vector3 origin;
            public Color color;
            public float phase, drift;
            public bool mote;
        }

        private readonly List<Layer> _layers = new List<Layer>();
        private readonly List<Atmosphere> _atmosphere = new List<Atmosphere>();
        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private MaterialPropertyBlock _properties;
        private Sprite _panoramaSprite;
        private SpriteRenderer _panorama;
        private Vector3 _panoramaOrigin;
        private float _seed;
        private int _chamberIndex;

        private void Awake() { _properties = new MaterialPropertyBlock(); }

        public static void Build(Transform room, int chamberIndex, float width, float depth,
            ProceduralChamberGenerator.BiomeTheme theme)
        {
            if (room == null) return;
            GameObject go = new GameObject("Underworld • depth and atmosphere");
            go.transform.SetParent(room, false);
            UnderworldEnvironmentArt art = go.AddComponent<UnderworldEnvironmentArt>();
            art._seed = chamberIndex * 2.731f;
            art._chamberIndex = chamberIndex;
            art.BuildScenery(width, depth, theme);
        }

        private void BuildScenery(float width, float depth, ProceduralChamberGenerator.BiomeTheme theme)
        {
            Color stone = Color.Lerp(theme.wallTop, new Color(.38f, .39f, .43f), .36f);
            Color distant = Color.Lerp(theme.parallaxArch, new Color(.085f, .14f, .17f), .55f);
            Color waterDeep = Color.Lerp(new Color(.015f, .075f, .09f), theme.parallaxArch, .22f);
            Color waterFoam = Color.Lerp(new Color(.28f, .80f, .70f), theme.parallaxGlow, .24f);
            waterFoam.a = 1f;

            Texture2D panorama = _chamberIndex < 2
                ? Resources.Load<Texture2D>("Art/AsphodelQuietVista")
                : Resources.Load<Texture2D>("Art/UnderworldPanorama");
            if (panorama == null) panorama = Resources.Load<Texture2D>("Art/UnderworldPanorama");
            if (panorama != null)
            {
                _panoramaSprite = Sprite.Create(panorama, new Rect(0, 0, panorama.width, panorama.height),
                    new Vector2(.5f, .5f), 100f);
                GameObject plate = new GameObject("Distant painted world • behind all geometry");
                plate.transform.SetParent(transform, false);
                plate.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
                // Positive camera-forward is AWAY from the camera, well behind the floor and river.
                _panoramaOrigin = RenderPipeline2DSetup.IsoCameraRotation * Vector3.forward * 66f;
                plate.transform.localPosition = _panoramaOrigin;
                plate.transform.localScale = new Vector3(160f / _panoramaSprite.bounds.size.x,
                    90f / _panoramaSprite.bounds.size.y, 1f);
                _panorama = plate.AddComponent<SpriteRenderer>();
                _panorama.sprite = _panoramaSprite;
                _panorama.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
                _panorama.color = new Color(.56f, .66f, .72f, 1f);
                _panorama.sortingOrder = -100;
            }


            // Two receding aqueducts are real open arches: the river remains visible between their piers.
            BuildArcade("Far aqueduct", new Vector3(0f, -5.5f, depth * .5f + 18f),
                0f, width + 20f, 7, 10f, distant, .55f);
            BuildArcade("Distant western sanctuary", new Vector3(width * .5f + 17f, -5f, 0f),
                90f, depth + 16f, 6, 9f, distant * .78f, .36f);
            BuildArcade("Doric eastern promenade", new Vector3(0f, -.6f, depth * .5f + 4.8f),
                0f, width + 3f, 6, 5.4f, stone, .10f);
            BuildArcade("Doric northern promenade", new Vector3(width * .5f + 4.8f, -.6f, 0f),
                90f, depth + 3f, 6, 5.1f, stone * .87f, .08f);

            MeshBuilder shore = new MeshBuilder();
            for (int i = 0; i < 12; i++)
            {
                float x = Mathf.Lerp(-width * .5f, width * .5f, i / 11f);
                float z = Mathf.Lerp(-depth * .5f, depth * .5f, i / 11f);
                float h = .45f + Mathf.Abs(Mathf.Sin(i * 7.91f + _seed)) * .75f;
                shore.Box(new Vector3(x, -1.15f, -depth * .5f - .55f), new Vector3(2.2f, h, 1.1f));
                shore.Box(new Vector3(-width * .5f - .55f, -1.15f, z), new Vector3(1.1f, h, 2.2f));
            }
            AddSolidLayer("Eroded river foundations", shore, Vector3.zero, 0f, stone * .56f, 0f);


            // A few cypresses frame the near banks, with transparent foliage that yields to the player.
            for (int i = 0; i < 5; i++)
            {
                bool leftBank = i % 2 == 0;
                Vector3 origin = leftBank
                    ? new Vector3(-width * .5f - 2.5f - i * .4f, -.8f, -depth * .30f + i * 5f)
                    : new Vector3(-width * .32f + i * 5f, -.8f, -depth * .5f - 3f);
                BuildCypress(origin, 5.4f + Mathf.Sin(i + _seed) * 1.2f, .05f + i * .015f);
            }

            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                Vector3 origin = new Vector3(Mathf.Cos(angle) * (width * .5f + 4f),
                    -.3f + (i % 3) * .6f, Mathf.Sin(angle) * (depth * .5f + 4f));
                AddAtmosphere("Low drifting river mist", origin, new Vector2(12f, 2.7f),
                    new Color(.24f, .38f, .43f, .10f), false, i * .87f, .16f);
            }
            for (int i = 0; i < 22; i++)
            {
                float x = Mathf.Sin(i * 19.13f + _seed) * width * .54f;
                float z = Mathf.Cos(i * 13.77f + _seed) * depth * .54f;
                AddAtmosphere("Wandering soul mote", new Vector3(x, .5f + (i % 5) * .65f, z),
                    new Vector2(.10f + (i % 3) * .055f, .15f + (i % 3) * .07f),
                    new Color(.58f, .86f, .76f, .50f), true, i * 1.37f, 0f);
            }
        }

        private void BuildArcade(string name, Vector3 origin, float yaw, float span, int bays,
            float height, Color tint, float drift)
        {
            MeshBuilder mesh = new MeshBuilder();
            float spacing = span / bays;
            float radius = Mathf.Min(.58f, spacing * .10f);
            for (int i = 0; i <= bays; i++)
            {
                float x = -span * .5f + i * spacing;
                mesh.Column(new Vector3(x, 0f, 0f), height, radius);
                // Stepped plinth, echinus and abacus make each column read as Doric at game scale.
                mesh.Box(new Vector3(x, .12f, 0f), new Vector3(radius * 3.6f, .24f, radius * 3.6f));
                mesh.Box(new Vector3(x, .34f, 0f), new Vector3(radius * 2.9f, .20f, radius * 2.9f));
                mesh.Box(new Vector3(x, height - .20f, 0f), new Vector3(radius * 2.7f, .24f, radius * 2.7f));
                mesh.Box(new Vector3(x, height + .02f, 0f), new Vector3(radius * 3.5f, .22f, radius * 3.5f));
                if (i == bays) continue;
                float mid = x + spacing * .5f;
                mesh.Arch(new Vector3(mid, height * .56f, 0f), spacing * .5f - radius * .65f,
                    .33f, .68f);
                mesh.Box(new Vector3(mid, height + .35f, 0f), new Vector3(spacing + .12f, .38f, 1.12f));
                mesh.Box(new Vector3(mid, height + .64f, 0f), new Vector3(spacing + .16f, .14f, 1.30f));
                for (int j = 0; j < 5; j++)
                    mesh.Box(new Vector3(x + spacing * (j + .5f) / 5f, height + .10f, -.52f),
                        new Vector3(.16f, .19f, .24f));
            }
            AddSolidLayer(name, mesh, origin, yaw, tint, drift);
        }

        private void AddSolidLayer(string name, MeshBuilder builder, Vector3 origin, float yaw, Color color, float drift)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = origin;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Mesh mesh = builder.ToMesh(name);
            _ownedMeshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            Color[] colors = { Opaque(color * 1.13f), Opaque(color * .74f), Opaque(color * .49f) };
            renderer.sharedMaterials = new[] { RenderPipeline2DSetup.GetOpaque3DMaterial(colors[0]),
                RenderPipeline2DSetup.GetOpaque3DMaterial(colors[1]), RenderPipeline2DSetup.GetOpaque3DMaterial(colors[2]) };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _layers.Add(new Layer { root = go.transform, origin = origin, renderer = renderer, palette = colors, drift = drift });
        }

        private void BuildCypress(Vector3 origin, float height, float drift)
        {
            MeshBuilder builder = new MeshBuilder();
            builder.Column(Vector3.zero, height * .38f, .12f);
            for (int i = 0; i < 5; i++)
                builder.Cone(new Vector3(Mathf.Sin(i * 2.3f) * .10f, height * (.16f + i * .125f), 0f),
                    height * .40f, .83f * (1f - i * .13f), 10);
            Mesh mesh = builder.ToMesh("Cypress silhouette", true);
            Color[] colors = new Color[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = new Color(.018f, .045f, .048f, .94f);
            mesh.colors = colors;
            _ownedMeshes.Add(mesh);
            GameObject go = new GameObject("Foreground cypress • sightline fade");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = origin;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 20;
            _layers.Add(new Layer { root = go.transform, origin = origin, renderer = renderer, drift = drift, foreground = true });
        }

        private void AddAtmosphere(string name, Vector3 position, Vector2 size, Color color, bool mote, float phase, float drift)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
            renderer.sprite = RenderPipeline2DSetup.SoftGlowSprite;
            renderer.color = color;
            renderer.sortingOrder = mote ? 9 : 3;
            _atmosphere.Add(new Atmosphere { renderer = renderer, origin = position, color = color,
                phase = phase + _seed, drift = drift, mote = mote });
        }

        private void LateUpdate()
        {
            if (UnderworldFrontEnd.BlocksMenusOrTransitions) return;
            UnderworldCamera2D rig = UnderworldCamera2D.Instance;
            if (rig == null || rig.Cam == null) return;
            float visibility = 1f - Mathf.SmoothStep(0f, 1f, rig.SideViewBlend01);
            Vector3 focus = rig.transform.position + rig.transform.forward * rig.cameraDistance;
            Vector3 offset = transform.InverseTransformPoint(focus);
            offset.y = 0f;
            Vector3 playerScreen = rig.target != null ? rig.Cam.WorldToViewportPoint(rig.target.position + Vector3.up) : Vector3.one * -20f;
            if (_panorama != null)
            {
                _panorama.transform.localPosition = _panoramaOrigin + offset * .78f;
                Color tint = _panorama.color; tint.a = visibility; _panorama.color = tint;
                _panorama.enabled = visibility > .01f;
            }
            foreach (Layer layer in _layers)
            {
                layer.root.localPosition = layer.origin + offset * layer.drift;
                layer.renderer.enabled = visibility > .02f;
                if (layer.foreground)
                {
                    Vector3 nearScreen = rig.Cam.WorldToViewportPoint(layer.renderer.bounds.center);
                    Vector2 distance = new Vector2((nearScreen.x - playerScreen.x) * rig.Cam.aspect, nearScreen.y - playerScreen.y);
                    float target = Mathf.Lerp(.12f, 1f, Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(.06f, .24f, distance.magnitude)));
                    layer.opacity = Mathf.MoveTowards(layer.opacity, target, Time.unscaledDeltaTime * 4f);
                    _properties.Clear();
                    _properties.SetColor("_Color", new Color(1f, 1f, 1f, visibility * layer.opacity));
                    layer.renderer.SetPropertyBlock(_properties);
                }
                else
                {
                    // Opaque architecture retains depth writing; dissolve into atmospheric darkness and sink below stage.
                    layer.root.localPosition += Vector3.down * (1f - visibility) * 14f;
                    for (int material = 0; material < layer.palette.Length; material++)
                    {
                        Color color = Color.Lerp(new Color(.018f, .024f, .035f), layer.palette[material], visibility);
                        _properties.Clear();
                        _properties.SetColor("_UnlitColor", color);
                        _properties.SetColor("_BaseColor", color);
                        _properties.SetColor("_Color", color);
                        layer.renderer.SetPropertyBlock(_properties, material);
                    }
                }
            }
            float time = Time.time;
            for (int i = 0; i < _atmosphere.Count; i++)
            {
                Atmosphere item = _atmosphere[i];
                Vector3 motion = item.mote
                    ? new Vector3(Mathf.Sin(time * .27f + item.phase) * .7f, Mathf.Sin(time * .48f + item.phase) * .42f, 0f)
                    : new Vector3(Mathf.Sin(time * .09f + item.phase) * 2.7f, Mathf.Sin(time * .13f + item.phase) * .15f, 0f);
                item.renderer.transform.localPosition = item.origin + offset * item.drift + motion;
                item.renderer.transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;
                Color color = item.color;
                color.a *= visibility * (.72f + Mathf.Sin(time * .6f + item.phase) * .28f);
                item.renderer.color = color;
                item.renderer.enabled = visibility > .01f;
            }
        }

        private static Color Opaque(Color color) { color.a = 1f; return color; }

        private void OnDestroy()
        {
            for (int i = 0; i < _ownedMeshes.Count; i++) ReleaseOwned(_ownedMeshes[i]);
            ReleaseOwned(_panoramaSprite);
        }

        private static void ReleaseOwned(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset); else DestroyImmediate(asset);
        }

        // A small construction-time mesh batcher. No primitive colliders, per-column behaviours or owned materials.
        private sealed class MeshBuilder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<int>[] _triangles = { new List<int>(), new List<int>(), new List<int>() };

            private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int face)
            {
                int start = _vertices.Count;
                _vertices.Add(a); _vertices.Add(b); _vertices.Add(c); _vertices.Add(d);
                List<int> indices = _triangles[face];
                indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
                indices.Add(start); indices.Add(start + 2); indices.Add(start + 3);
            }

            public void Box(Vector3 center, Vector3 size)
            {
                Vector3 a = center - size * .5f, b = center + size * .5f;
                Quad(new Vector3(a.x,b.y,a.z), new Vector3(a.x,b.y,b.z), new Vector3(b.x,b.y,b.z), new Vector3(b.x,b.y,a.z), 0);
                Quad(new Vector3(a.x,a.y,b.z), new Vector3(a.x,b.y,b.z), new Vector3(a.x,b.y,a.z), new Vector3(a.x,a.y,a.z), 1);
                Quad(new Vector3(b.x,a.y,a.z), new Vector3(b.x,b.y,a.z), new Vector3(b.x,b.y,b.z), new Vector3(b.x,a.y,b.z), 1);
                Quad(new Vector3(a.x,a.y,a.z), new Vector3(a.x,b.y,a.z), new Vector3(b.x,b.y,a.z), new Vector3(b.x,a.y,a.z), 2);
                Quad(new Vector3(b.x,a.y,b.z), new Vector3(b.x,b.y,b.z), new Vector3(a.x,b.y,b.z), new Vector3(a.x,a.y,b.z), 2);
            }

            public void Column(Vector3 origin, float height, float radius)
            {
                const int segments = 20;
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments;
                    float ra = radius * (i % 2 == 0 ? 1f : .88f), rb = radius * (i % 2 == 0 ? .88f : 1f);
                    Vector3 p = new Vector3(Mathf.Cos(a) * ra, 0f, Mathf.Sin(a) * ra);
                    Vector3 q = new Vector3(Mathf.Cos(b) * rb, 0f, Mathf.Sin(b) * rb);
                    Quad(origin + p, origin + p * .82f + Vector3.up * height,
                        origin + q * .82f + Vector3.up * height, origin + q, i % 3);
                }
            }

            public void Cone(Vector3 origin, float height, float radius, int segments)
            {
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2f / segments, b = (i + 1) * Mathf.PI * 2f / segments;
                    Vector3 p = origin + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                    Vector3 q = origin + new Vector3(Mathf.Cos(b) * radius, 0f, Mathf.Sin(b) * radius);
                    Quad(p, origin + Vector3.up * height, origin + Vector3.up * height, q, i % 3);
                }
            }

            public void Arch(Vector3 origin, float radius, float thickness, float depth)
            {
                const int segments = 14;
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI / segments, b = (i + 1) * Mathf.PI / segments;
                    Vector3 p = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                    Vector3 q = new Vector3(Mathf.Cos(b) * radius, Mathf.Sin(b) * radius, 0f);
                    Vector3 po = p * ((radius + thickness) / radius), qo = q * ((radius + thickness) / radius);
                    Vector3 front = origin + Vector3.back * depth * .5f, back = origin + Vector3.forward * depth * .5f;
                    Quad(front + p, front + q, front + qo, front + po, 1);
                    Quad(back + po, back + qo, back + q, back + p, 2);
                    Quad(front + po, front + qo, back + qo, back + po, 0);
                    Quad(back + p, back + q, front + q, front + p, 2);
                }
            }

            public Mesh ToMesh(string name, bool singleMaterial = false)
            {
                Mesh mesh = new Mesh { name = name };
                if (_vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(_vertices);
                if (singleMaterial)
                {
                    List<int> all = new List<int>();
                    for (int i = 0; i < 3; i++) all.AddRange(_triangles[i]);
                    mesh.SetTriangles(all, 0);
                }
                else
                {
                    mesh.subMeshCount = 3;
                    for (int i = 0; i < 3; i++) mesh.SetTriangles(_triangles[i], i);
                }
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
