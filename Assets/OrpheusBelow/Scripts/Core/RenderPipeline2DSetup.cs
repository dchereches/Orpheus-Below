using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrpheusBelow
{
    /// <summary>
    /// Ensures 2D sprites and HDR glow look crisp across HDRP, URP, or Built-in RP,
    /// and generates clean procedural placeholder sprites (easy to replace with AI PNGs later!).
    /// </summary>
    public static class RenderPipeline2DSetup
    {
        private static Sprite _squareSprite;
        private static Sprite _circleSprite;
        private static Sprite _softGlowSprite;
        private static Sprite _ringSprite;
        private static Sprite _diamondSprite;
        private static Material _spriteMat;

        public static Material Get2DSpriteMaterial()
        {
            if (_spriteMat != null) return _spriteMat;

            // UI/Default works reliably across HDRP, URP, and Built-in RP with full vertex color + alpha + HDR bloom!
            Shader sh = Shader.Find("UI/Default");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            _spriteMat = new Material(sh) { name = "Orpheus2D_SpriteMat" };
            return _spriteMat;
        }

        public static void ConfigureCameraFor2D(Camera cam, Color bgColor)
        {
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.nearClipPlane = -20f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bgColor;

            // Disable leftover 3D template lights/sky volumes that blow out exposure in HDRP
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                l.gameObject.SetActive(false);
            }

            // Configure HDRP Camera & Fixed Exposure = 0 EV via reflection (zero assembly coupling!)
            Type hdCamType = Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");
            if (hdCamType != null)
            {
                Component hdCam = cam.GetComponent(hdCamType) ?? cam.gameObject.AddComponent(hdCamType);
                FieldInfo clearModeField = hdCamType.GetField("clearColorMode", BindingFlags.Public | BindingFlags.Instance);
                if (clearModeField != null)
                {
                    // ClearColorMode.Color == 1 in HDRP
                    object colorEnumVal = Enum.ToObject(clearModeField.FieldType, 1);
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
            if (GameObject.Find("Orpheus_HDRP_2D_ExposureFix") != null) return;

            Type exposureType = Type.GetType("UnityEngine.Rendering.HighDefinition.Exposure, Unity.RenderPipelines.HighDefinition.Runtime");
            Type visEnvType = Type.GetType("UnityEngine.Rendering.HighDefinition.VisualEnvironment, Unity.RenderPipelines.HighDefinition.Runtime");
            if (exposureType == null) return;

            GameObject volGo = new GameObject("Orpheus_HDRP_2D_ExposureFix");
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 500f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = profile;

            // Fixed Exposure = 0 EV -> 1.0x true sprite colors + natural HDRP Bloom on >1.0 HDR attack colors!
            VolumeComponent expComp = (VolumeComponent)ScriptableObject.CreateInstance(exposureType);
            expComp.active = true;
            SetVolumeParamEnumOrValue(exposureType, expComp, "mode", 0); // 0 = ExposureMode.Fixed
            SetVolumeParamEnumOrValue(exposureType, expComp, "fixedExposure", 0f);
            profile.components.Add(expComp);

            if (visEnvType != null)
            {
                VolumeComponent envComp = (VolumeComponent)ScriptableObject.CreateInstance(visEnvType);
                envComp.active = true;
                SetVolumeParamEnumOrValue(visEnvType, envComp, "skyType", 0); // 0 = None
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

        public static SpriteRenderer AttachSprite(GameObject go, Sprite sprite, Color color, int sortingOrder = 0)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = Get2DSpriteMaterial();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
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
                        // Gaussian-like dreamy falloff for blurred Eurydice & Hades glow auras
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
                        float fill = d < 0.82f ? 0.12f * (d / 0.82f) : 0f;
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
    }
}
