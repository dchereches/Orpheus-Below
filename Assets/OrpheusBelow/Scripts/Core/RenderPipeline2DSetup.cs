using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrpheusBelow
{
    public enum IsoObstacleKind
    {
        SolidWall,      // Blocks walking, dashing, projectiles, and triggers Hades WALL-SLAMS!
        DashableChasm   // Blocks walking (can't accidentally walk off!), but DASH flies right across!
    }

    /// <summary>
    /// 2.5D Isometric Hades rendering & geometry toolkit (works across HDRP, URP, and Built-in RP):
    ///   - Configures Isometric Orthographic Camera (Pitch 36°, Yaw 45°) + HDRP Fixed Exposure = 0 EV
    ///   - Builds 3-submesh 3D Isometric Architectural Blocks with ZWrite On depth occlusion & facet shading
    ///   - Attaches Billboard 2D Sprites (facing camera, ready for AI PNGs!) & Ground-Plane XZ Decal Sprites
    /// </summary>
    public static class RenderPipeline2DSetup
    {
        public static readonly Quaternion IsoCameraRotation = Quaternion.Euler(36f, 45f, 0f);
        public static readonly Quaternion GroundDecalRotation = Quaternion.Euler(90f, 0f, 0f);

        private static Sprite _squareSprite;
        private static Sprite _circleSprite;
        private static Sprite _softGlowSprite;
        private static Sprite _ringSprite;
        private static Sprite _diamondSprite;
        private static Sprite _crescentArcSprite;

        private static Material _spriteMat;
        private static Mesh _facetedCubeMesh;
        private static readonly Dictionary<Color32, Material> _opaqueMatCache = new Dictionary<Color32, Material>();

        public static Material Get2DSpriteMaterial()
        {
            if (_spriteMat != null) return _spriteMat;

            // Sprites/Default has ZTest LEqual so 2D sprites occlude cleanly behind 3D ZWrite-On isometric pillars/walls!
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("UI/Default");
            _spriteMat = new Material(sh) { name = "Orpheus25D_SpriteMat" };
            return _spriteMat;
        }

        public static Material GetOpaque3DMaterial(Color color)
        {
            Color32 key = color;
            if (_opaqueMatCache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            Shader sh = Shader.Find("HDRP/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");

            Material mat = new Material(sh) { name = "OrpheusIso3D_Opaque" };
            mat.color = color;
            if (mat.HasProperty("_UnlitColor")) mat.SetColor("_UnlitColor", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            _opaqueMatCache[key] = mat;
            return mat;
        }

        public static void ConfigureCameraFor2D(Camera cam, Color bgColor)
        {
            ConfigureCameraForIsometric25D(cam, bgColor);
        }

        public static void ConfigureCameraForIsometric25D(Camera cam, Color bgColor)
        {
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = 8.0f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 140f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bgColor;
            cam.transform.rotation = IsoCameraRotation;

            // Disable default 3D template lights/sky that blow out HDRP auto-exposure
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                l.gameObject.SetActive(false);
            }

            Type hdCamType = Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");
            if (hdCamType != null)
            {
                Component hdCam = cam.GetComponent(hdCamType) ?? cam.gameObject.AddComponent(hdCamType);
                FieldInfo clearModeField = hdCamType.GetField("clearColorMode", BindingFlags.Public | BindingFlags.Instance);
                if (clearModeField != null)
                {
                    object colorEnumVal = Enum.ToObject(clearModeField.FieldType, 1); // ClearColorMode.Color
                    clearModeField.SetValue(hdCam, colorEnumVal);
                }

                FieldInfo bgHdrField = hdCamType.GetField("backgroundColorHDR", BindingFlags.Public | BindingFlags.Instance);
                if (bgHdrField != null)
                {
                    bgHdrField.SetValue(hdCam, bgColor);
                }

                EnsureHdrpZeroExposureVolume();
            }
        }

        private static void EnsureHdrpZeroExposureVolume()
        {
            GameObject existing = GameObject.Find("Orpheus_HDRP_2D_ExposureFix");
            if (existing != null)
            {
                Volume existingVolume = existing.GetComponent<Volume>();
                if (existingVolume != null && existingVolume.sharedProfile != null)
                {
                    AddClarityOverride(existingVolume.sharedProfile, "MotionBlur", "intensity", 0f);
                    AddClarityOverride(existingVolume.sharedProfile, "DepthOfField", "focusMode", 0);
                    return;
                }
            }

            Type exposureType = Type.GetType("UnityEngine.Rendering.HighDefinition.Exposure, Unity.RenderPipelines.HighDefinition.Runtime");
            Type visEnvType = Type.GetType("UnityEngine.Rendering.HighDefinition.VisualEnvironment, Unity.RenderPipelines.HighDefinition.Runtime");
            if (exposureType == null) return;

            GameObject volGo = new GameObject("Orpheus_HDRP_2D_ExposureFix");
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 500f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = profile;

            VolumeComponent expComp = (VolumeComponent)ScriptableObject.CreateInstance(exposureType);
            expComp.active = true;
            SetVolumeParamEnumOrValue(exposureType, expComp, "mode", 0); // ExposureMode.Fixed
            SetVolumeParamEnumOrValue(exposureType, expComp, "fixedExposure", 0f);
            profile.components.Add(expComp);

            // Crisp sprites, note tells and text must stay readable during camera moves and hitstop.
            AddClarityOverride(profile, "MotionBlur", "intensity", 0f);
            AddClarityOverride(profile, "DepthOfField", "focusMode", 0);

            if (visEnvType != null)
            {
                VolumeComponent envComp = (VolumeComponent)ScriptableObject.CreateInstance(visEnvType);
                envComp.active = true;
                SetVolumeParamEnumOrValue(visEnvType, envComp, "skyType", 0); // SkyType.None
                profile.components.Add(envComp);
            }
        }

        private static void SetVolumeParamEnumOrValue(Type compType, object comp, string fieldName, object rawVal)
        {
            FieldInfo fi = compType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (fi == null) return;
            object paramObj = fi.GetValue(comp);
            if (paramObj == null) return;

            PropertyInfo overrideProp = paramObj.GetType().GetProperty("overrideState", BindingFlags.Public | BindingFlags.Instance);
            overrideProp?.SetValue(paramObj, true);

            PropertyInfo valProp = paramObj.GetType().GetProperty("value", BindingFlags.Public | BindingFlags.Instance);
            if (valProp != null)
            {
                object converted = valProp.PropertyType.IsEnum
                    ? Enum.ToObject(valProp.PropertyType, rawVal)
                    : Convert.ChangeType(rawVal, valProp.PropertyType);
                valProp.SetValue(paramObj, converted);
            }
        }

        private static void AddClarityOverride(VolumeProfile profile, string componentName, string parameter, object value)
        {
            Type type = Type.GetType("UnityEngine.Rendering.HighDefinition." + componentName + ", Unity.RenderPipelines.HighDefinition.Runtime");
            if (type == null) return;
            VolumeComponent component = profile.components.Find(c => c != null && c.GetType() == type);
            if (component == null)
            {
                component = (VolumeComponent)ScriptableObject.CreateInstance(type);
                profile.components.Add(component);
            }
            component.active = true;
            SetVolumeParamEnumOrValue(type, component, parameter, value);
        }

        /// <summary>
        /// Builds a 3-submesh 3D Isometric Box (Submesh 0 = Top +Y/-Y, Submesh 1 = Left -X/+X, Submesh 2 = Right -Z/+Z)
        /// so every floor slab, wall, and pillar has crisp hand-painted Hades facet contrast + hardware ZWrite occlusion!
        /// </summary>
        public static Mesh GetFacetedCubeMesh()
        {
            if (_facetedCubeMesh != null) return _facetedCubeMesh;

            Mesh m = new Mesh { name = "Orpheus_IsoFacetedCube" };
            Vector3[] v =
            {
                // Top (+Y): 0..3
                new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f, -0.5f),
                // Bottom (-Y): 4..7
                new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
                // Left-Front (-X): 8..11
                new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
                // Back-Right (+X): 12..15
                new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
                // Right-Front (-Z): 16..19
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
                // Back-Left (+Z): 20..23
                new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(-0.5f, -0.5f,  0.5f)
            };

            m.vertices = v;
            m.subMeshCount = 3;
            m.SetTriangles(new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 }, 0);          // Top/Bottom
            m.SetTriangles(new[] { 8, 9, 10, 8, 10, 11, 12, 13, 14, 12, 14, 15 }, 1); // X faces (Camera-Left)
            m.SetTriangles(new[] { 16, 17, 18, 16, 18, 19, 20, 21, 22, 20, 22, 23 }, 2); // Z faces (Camera-Right)
            m.RecalculateNormals();
            m.RecalculateBounds();
            _facetedCubeMesh = m;
            return _facetedCubeMesh;
        }

        public static GameObject CreateIsoBlock(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 size,
            Color topColor,
            Color leftColor,
            Color rightColor,
            bool addSolidWallCollider = false)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetFacetedCubeMesh();

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[]
            {
                GetOpaque3DMaterial(topColor),
                GetOpaque3DMaterial(leftColor),
                GetOpaque3DMaterial(rightColor)
            };
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            IsoBlockBinder binder = go.AddComponent<IsoBlockBinder>();
            binder.topColor = topColor;
            binder.leftColor = leftColor;
            binder.rightColor = rightColor;

            if (addSolidWallCollider)
            {
                go.AddComponent<BoxCollider>();
                go.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;
            }

            return go;
        }

        public static SpriteRenderer AttachSprite(GameObject go, Sprite sprite, Color color, int sortingOrder = 0)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = Get2DSpriteMaterial();
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            ProceduralSpriteBinder binder = go.GetComponent<ProceduralSpriteBinder>();
            if (binder == null) binder = go.AddComponent<ProceduralSpriteBinder>();
            binder.shape = DetectShape(sprite);
            return sr;
        }

        public static SpriteRenderer AttachBillboardSprite(GameObject go, Sprite sprite, Color color, int sortingOrder = 10)
        {
            go.transform.rotation = IsoCameraRotation;
            return AttachSprite(go, sprite, color, sortingOrder);
        }

        public static SpriteRenderer AttachGroundDecal(GameObject go, Sprite sprite, Color color, int sortingOrder = 2)
        {
            go.transform.rotation = GroundDecalRotation;
            return AttachSprite(go, sprite, color, sortingOrder);
        }

        public static ProceduralSpriteShape DetectShape(Sprite s)
        {
            if (s != null)
            {
                if (s == _circleSprite) return ProceduralSpriteShape.Circle;
                if (s == _softGlowSprite) return ProceduralSpriteShape.SoftGlow;
                if (s == _ringSprite) return ProceduralSpriteShape.Ring;
                if (s == _diamondSprite) return ProceduralSpriteShape.Diamond;
                if (s == _crescentArcSprite) return ProceduralSpriteShape.CrescentArc;
            }
            return ProceduralSpriteShape.Square;
        }

        public static Sprite GetShapeSprite(ProceduralSpriteShape shape)
        {
            switch (shape)
            {
                case ProceduralSpriteShape.Circle: return CircleSprite;
                case ProceduralSpriteShape.SoftGlow: return SoftGlowSprite;
                case ProceduralSpriteShape.Ring: return RingSprite;
                case ProceduralSpriteShape.Diamond: return DiamondSprite;
                case ProceduralSpriteShape.CrescentArc: return CrescentArcSprite;
                default: return SquareSprite;
            }
        }

        public static Sprite SquareSprite
        {
            get
            {
                if (_squareSprite != null) return _squareSprite;
                Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                Color[] px = new Color[16 * 16];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                _squareSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
                return _squareSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite != null) return _circleSprite;
                const int s = 64;
                Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f - s * 0.5f) / (s * 0.5f);
                        float dy = (y + 0.5f - s * 0.5f) / (s * 0.5f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - d) * 14f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                _circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
                return _circleSprite;
            }
        }

        public static Sprite SoftGlowSprite
        {
            get
            {
                if (_softGlowSprite != null) return _softGlowSprite;
                const int s = 96;
                Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f - s * 0.5f) / (s * 0.5f);
                        float dy = (y + 0.5f - s * 0.5f) / (s * 0.5f);
                        float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                        float a = Mathf.Pow(1f - d, 2.4f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                _softGlowSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
                return _softGlowSprite;
            }
        }

        public static Sprite RingSprite
        {
            get
            {
                if (_ringSprite != null) return _ringSprite;
                const int s = 128;
                Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f - s * 0.5f) / (s * 0.5f);
                        float dy = (y + 0.5f - s * 0.5f) / (s * 0.5f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float ring = 1f - Mathf.Clamp01(Mathf.Abs(d - 0.82f) / 0.14f);
                        float fill = d < 0.82f ? 0.10f * (d / 0.82f) : 0f;
                        float a = Mathf.Clamp01(ring * ring + fill);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                _ringSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
                return _ringSprite;
            }
        }

        public static Sprite DiamondSprite
        {
            get
            {
                if (_diamondSprite != null) return _diamondSprite;
                const int s = 64;
                Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = Mathf.Abs(x + 0.5f - s * 0.5f) / (s * 0.5f);
                        float dy = Mathf.Abs(y + 0.5f - s * 0.5f) / (s * 0.5f);
                        float d = dx + dy;
                        float a = Mathf.Clamp01((1f - d) * 10f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                _diamondSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
                return _diamondSprite;
            }
        }

        /// <summary>
        /// Hades-style sweeping crescent slash arc pointing toward +X in sprite space!
        /// </summary>
        public static Sprite CrescentArcSprite
        {
            get
            {
                if (_crescentArcSprite != null) return _crescentArcSprite;
                const int s = 128;
                Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f - s * 0.5f) / (s * 0.5f);
                        float dy = (y + 0.5f - s * 0.5f) / (s * 0.5f);
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Atan2(dy, dx); // 0 = +X
                        float angMask = Mathf.Clamp01(1f - Mathf.Abs(ang) / 1.15f); // ~130 deg sweep
                        float radBand = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.72f) / 0.22f);
                        float a = Mathf.Pow(angMask, 0.65f) * radBand * radBand;
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a * 1.4f)));
                    }
                }
                tex.Apply();
                _crescentArcSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
                return _crescentArcSprite;
            }
        }
    }

    public enum ProceduralSpriteShape
    {
        Square,
        Circle,
        SoftGlow,
        Ring,
        Diamond,
        CrescentArc
    }

    [ExecuteAlways]
    public class IsoBlockBinder : MonoBehaviour
    {
        public Color topColor = new Color(0.18f, 0.13f, 0.24f, 1f);
        public Color leftColor = new Color(0.11f, 0.07f, 0.16f, 1f);
        public Color rightColor = new Color(0.06f, 0.04f, 0.10f, 1f);

        private void OnEnable()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh == null)
            {
                mf.sharedMesh = RenderPipeline2DSetup.GetFacetedCubeMesh();
            }

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null && (mr.sharedMaterials == null || mr.sharedMaterials.Length < 3 || mr.sharedMaterials[0] == null))
            {
                mr.sharedMaterials = new[]
                {
                    RenderPipeline2DSetup.GetOpaque3DMaterial(topColor),
                    RenderPipeline2DSetup.GetOpaque3DMaterial(leftColor),
                    RenderPipeline2DSetup.GetOpaque3DMaterial(rightColor)
                };
            }
        }
    }
}
