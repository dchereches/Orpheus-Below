using UnityEngine;
using UnityEngine.Rendering;

namespace OrpheusBelow
{
    /// <summary>Bounded, allocation-free animated Styx water. Size is X/Z, or X/height for waterfalls.</summary>
    public sealed class StyxWaterSurface : MonoBehaviour
    {
        private Mesh _mesh;
        private Mesh _ribbonMesh;
        private Vector3[] _ribbonVertices;
        private Color[] _ribbonColors;
        private const int RibbonCount = 24;
        private const int RibbonSegments = 7;
        private Vector3[] _vertices;
        private Color[] _colors;
        private Vector2[] _coordinates;
        private Color _deep, _foam;
        private Vector2 _size;
        private bool _waterfall;
        private float _phase, _nextRipple;
        private Vector3 _previousPlayer;
        private bool _hasPlayer;
        private readonly Vector2[] _rippleCenters = new Vector2[4];
        private readonly float[] _rippleTimes = { -100f, -100f, -100f, -100f };
        private int _rippleCursor;
        private float _opacity = 1f;
        private static Material _waterMaterial;
        private static int _materialUsers;
        private MaterialPropertyBlock _waterProperties;
        private MeshRenderer _surfaceRenderer;
        private readonly Vector4[] _shaderRipples = new Vector4[4];
        private bool _usesWaterShader;

        public static StyxWaterSurface Create(Transform parent, Vector3 localCenter, Vector2 size,
            Color deep, Color foam, bool waterfall = false)
        {
            GameObject go = new GameObject(waterfall ? "Styx • falling veil" : "Styx • living current");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            StyxWaterSurface water = go.AddComponent<StyxWaterSurface>();
            water._size = new Vector2(Mathf.Max(.2f, size.x), Mathf.Max(.2f, size.y));
            water._deep = deep;
            water._foam = foam;
            water._waterfall = waterfall;
            water._phase = localCenter.x * .37f + localCenter.z * .21f;
            water._waterProperties = new MaterialPropertyBlock();
            water.BuildMesh();
            water.BuildRibbons();
            return water;
        }

        public void SetOpacity(float opacity)
        {
            _opacity = Mathf.Clamp01(opacity);
            ApplyWaterProperties(Time.time);
        }

        private void BuildMesh()
        {
            int columns = Mathf.Clamp(Mathf.CeilToInt(_size.x * 3f), 12, 80);
            int rows = Mathf.Clamp(Mathf.CeilToInt(_size.y * 3f), 10, 72);
            int count = (columns + 1) * (rows + 1);
            _vertices = new Vector3[count];
            _coordinates = new Vector2[count];
            _colors = new Color[count];
            Vector2[] uv = new Vector2[count];
            int[] indices = new int[columns * rows * 6];
            for (int z = 0; z <= rows; z++)
            for (int x = 0; x <= columns; x++)
            {
                int i = z * (columns + 1) + x;
                _coordinates[i] = new Vector2((x / (float)columns - .5f) * _size.x,
                    (z / (float)rows - .5f) * _size.y);
                uv[i] = new Vector2(x / (float)columns, z / (float)rows);
                if (x == columns || z == rows) continue;
                int t = (z * columns + x) * 6;
                indices[t] = i; indices[t + 1] = i + columns + 1; indices[t + 2] = i + 1;
                indices[t + 3] = i + 1; indices[t + 4] = i + columns + 1; indices[t + 5] = i + columns + 2;
            }
            _mesh = new Mesh { name = "Styx flowing tessellation" };
            _mesh.MarkDynamic();
            Animate(Time.time);
            _mesh.uv = uv;
            _mesh.triangles = indices;
            _mesh.bounds = _waterfall
                ? new Bounds(Vector3.zero, new Vector3(_size.x + .3f, _size.y + .3f, .5f))
                : new Bounds(Vector3.zero, new Vector3(_size.x + .3f, .5f, _size.y + .3f));
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            _surfaceRenderer = renderer;
            if (_waterMaterial == null)
            {
                Shader shader = Resources.Load<Shader>("Art/StyxWater");
                if (shader != null && shader.isSupported)
                    _waterMaterial = new Material(shader) { name = "Styx continuous surface • shared" };
            }
            _usesWaterShader = _waterMaterial != null;
            if (_usesWaterShader) _materialUsers++;
            renderer.sharedMaterial = _usesWaterShader ? _waterMaterial : RenderPipeline2DSetup.Get2DSpriteMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Same transparent queue and depth test as ground decals. Floor geometry hides the exterior river.
            renderer.sortingOrder = 1;
            ApplyWaterProperties(Time.time);
        }

        private void Update()
        {
            if (_mesh == null) return;
            float time = Time.time;
            if (!_waterfall) UpdatePlayerRipples(time);
            Animate(time);
            AnimateRibbons(time);
            ApplyWaterProperties(time);
        }

        private void ApplyWaterProperties(float time)
        {
            if (!_usesWaterShader || _surfaceRenderer == null || _waterProperties == null) return;
            for (int i = 0; i < 4; i++)
                _shaderRipples[i] = new Vector4(_rippleCenters[i].x, _rippleCenters[i].y, _rippleTimes[i], 0f);
            _waterProperties.SetColor("_DeepColor", _deep);
            _waterProperties.SetColor("_FoamColor", _foam);
            _waterProperties.SetVector("_Surface", new Vector4(_size.x, _size.y, _waterfall ? 1f : 0f, _opacity));
            _waterProperties.SetVector("_FlowTime", new Vector4(time + _phase, time, 0f, 0f));
            _waterProperties.SetVectorArray("_Ripples", _shaderRipples);
            _surfaceRenderer.SetPropertyBlock(_waterProperties);
        }

        private void BuildRibbons()
        {
            _ribbonVertices = new Vector3[RibbonCount * (RibbonSegments + 1) * 2];
            _ribbonColors = new Color[_ribbonVertices.Length];
            int[] triangles = new int[RibbonCount * RibbonSegments * 6];
            for (int band = 0; band < RibbonCount; band++)
            for (int j = 0; j < RibbonSegments; j++)
            {
                int vertex = band * (RibbonSegments + 1) * 2 + j * 2;
                int index = (band * RibbonSegments + j) * 6;
                triangles[index] = vertex; triangles[index + 1] = vertex + 2; triangles[index + 2] = vertex + 1;
                triangles[index + 3] = vertex + 1; triangles[index + 4] = vertex + 2; triangles[index + 5] = vertex + 3;
            }
            _ribbonMesh = new Mesh { name = "Styx • drifting silver ripples" };
            _ribbonMesh.MarkDynamic();
            AnimateRibbons(Time.time);
            _ribbonMesh.triangles = triangles;
            _ribbonMesh.bounds = _mesh.bounds;
            GameObject ribbons = new GameObject("Foam filaments and light bands");
            ribbons.transform.SetParent(transform, false);
            ribbons.AddComponent<MeshFilter>().sharedMesh = _ribbonMesh;
            MeshRenderer renderer = ribbons.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 2;
        }

        private void AnimateRibbons(float time)
        {
            if (_ribbonMesh == null) return;
            for (int band = 0; band < RibbonCount; band++)
            {
                float phase = band * 2.39996f + _phase;
                float cx = Mathf.Sin(phase * 13.17f) * _size.x * .43f;
                float cycle = Mathf.Repeat(time * (_waterfall ? .40f : .035f) + band * .173f, 1f);
                float cz = (cycle - .5f) * _size.y * .94f;
                float length = Mathf.Min(_size.x * .14f, .55f + (band % 4) * .44f);
                float fade = Mathf.Sin(cycle * Mathf.PI) * (.13f + .06f * Mathf.Sin(time + phase)) * _opacity;
                for (int j = 0; j <= RibbonSegments; j++)
                {
                    float fraction = j / (float)RibbonSegments;
                    float x = cx + (fraction - .5f) * length * 2f;
                    float z = cz + Mathf.Sin(fraction * Mathf.PI + phase + time * .6f) * .12f;
                    float width = .018f + .012f * Mathf.Sin(time * .8f + phase);
                    int index = band * (RibbonSegments + 1) * 2 + j * 2;
                    if (_waterfall)
                    {
                        x = cx + Mathf.Sin(fraction * 2f + phase) * .035f;
                        z = Mathf.Clamp(-cz + (fraction - .5f) * .95f, -_size.y * .5f, _size.y * .5f);
                        _ribbonVertices[index] = new Vector3(x - width, z, -.075f);
                        _ribbonVertices[index + 1] = new Vector3(x + width, z, -.075f);
                    }
                    else
                    {
                        x = Mathf.Clamp(x, -_size.x * .49f, _size.x * .49f);
                        z = Mathf.Clamp(z, -_size.y * .49f, _size.y * .49f);
                        _ribbonVertices[index] = new Vector3(x, .018f, z - width);
                        _ribbonVertices[index + 1] = new Vector3(x, .018f, z + width);
                    }
                    Color color = Color.Lerp(_foam, new Color(.36f, .51f, .46f), .55f);
                    color.a = Mathf.Sin(fraction * Mathf.PI) * fade;
                    _ribbonColors[index] = color;
                    _ribbonColors[index + 1] = color;
                }
            }
            _ribbonMesh.vertices = _ribbonVertices;
            _ribbonMesh.colors = _ribbonColors;
        }

        private void UpdatePlayerRipples(float time)
        {
            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player == null) { _hasPlayer = false; return; }
            Vector3 local = transform.InverseTransformPoint(player.transform.position);
            bool moved = _hasPlayer && (local - _previousPlayer).sqrMagnitude > .0004f;
            _previousPlayer = local;
            _hasPlayer = true;
            // Do not react through the room floor to the lower surrounding river.
            if (Mathf.Abs(local.y) > 1f || Mathf.Abs(local.x) > _size.x * .5f + .65f ||
                Mathf.Abs(local.z) > _size.y * .5f + .65f || !moved || time < _nextRipple) return;
            _nextRipple = time + .22f;
            if (player.IsDashing) UnderworldSoundscape.Instance?.PlayFootstep(true);
            _rippleCenters[_rippleCursor] = new Vector2(
                Mathf.Clamp(local.x, -_size.x * .5f, _size.x * .5f),
                Mathf.Clamp(local.z, -_size.y * .5f, _size.y * .5f));
            _rippleTimes[_rippleCursor] = time;
            _rippleCursor = (_rippleCursor + 1) % _rippleCenters.Length;
        }

        private void Animate(float time)
        {
            float t = time + _phase;
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector2 p = _coordinates[i];
                if (_usesWaterShader)
                {
                    // Only subtle broad geometry moves on the CPU. All visible fine detail is continuous fragment shading.
                    float swell = Mathf.Sin(p.x * .24f + p.y * .37f - t * .7f) * (_waterfall ? .035f : .009f);
                    _vertices[i] = _waterfall ? new Vector3(p.x, p.y, swell) : new Vector3(p.x, swell, p.y);
                    continue;
                }
                float flow = _waterfall ? p.y * 2.6f + t * 5.7f : p.y * 2.3f + p.x * .36f - t * 1.4f;
                float curl = Mathf.Sin(p.x * .83f + p.y * .43f + t * .62f);
                float wave = Mathf.Sin(flow + curl * 1.2f);
                float crossing = Mathf.Sin(p.x * 2.6f - p.y * 1.1f + t * .8f);
                float narrowBand = Mathf.Pow(Mathf.Max(0f, wave), 12f);
                float caustic = Mathf.Pow(Mathf.Max(0f, crossing * Mathf.Sin(flow * .73f - t * .4f)), 7f);
                float edge = Mathf.Min(_size.x * .5f - Mathf.Abs(p.x), _size.y * .5f - Mathf.Abs(p.y));
                float edgeFoam = Mathf.Clamp01(1f - edge / .30f) * (.55f + .25f * Mathf.Sin(flow + t));
                float ripple = 0f;
                if (!_waterfall)
                {
                    for (int r = 0; r < 4; r++)
                    {
                        float age = time - _rippleTimes[r];
                        if (age > 1.7f || age < 0f) continue;
                        float distance = Vector2.Distance(p, _rippleCenters[r]);
                        float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - age * 2.2f) / .19f);
                        ripple += ring * (1f - age / 1.7f) * .8f;
                    }
                }
                float light = .10f + .12f * (wave + 1f) + narrowBand * .26f + caustic * .18f;
                light = Mathf.Clamp01(light + edgeFoam * .62f + ripple);
                Color color = Color.Lerp(_deep, _foam, light);
                color.a = (_waterfall ? .82f : .98f) * _opacity;
                _colors[i] = color;
                // Chasm surfaces sit only 2 cm above stone: never dip below it or lift above a foot.
                float displacement = wave * (_waterfall ? .055f : .009f) + crossing * .004f;
                _vertices[i] = _waterfall ? new Vector3(p.x, p.y, displacement) : new Vector3(p.x, displacement, p.y);
            }
            _mesh.vertices = _vertices;
            if (!_usesWaterShader) _mesh.colors = _colors;
        }

        private void OnDestroy()
        {
            if (_usesWaterShader)
            {
                _materialUsers = Mathf.Max(0, _materialUsers - 1);
                if (_materialUsers == 0 && _waterMaterial != null)
                {
                    if (Application.isPlaying) Destroy(_waterMaterial);
                    else DestroyImmediate(_waterMaterial);
                    _waterMaterial = null;
                }
            }
            if (Application.isPlaying)
            {
                if (_mesh != null) Destroy(_mesh);
                if (_ribbonMesh != null) Destroy(_ribbonMesh);
            }
            else
            {
                if (_mesh != null) DestroyImmediate(_mesh);
                if (_ribbonMesh != null) DestroyImmediate(_ribbonMesh);
            }
        }
    }
}
