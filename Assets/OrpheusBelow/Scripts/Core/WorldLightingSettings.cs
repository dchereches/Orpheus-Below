using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrpheusBelow
{
    /// <summary>Player display adjustments on the rendered world, independent of overlay UI.</summary>
    public sealed class WorldLightingSettings : MonoBehaviour
    {
        public const string VolumeName = "Orpheus_WorldLightingSettings";
        public const string ExposureKey = "Orpheus_LightingExposure";
        public const string ContrastKey = "Orpheus_LightingContrast";
        public const string BloomKey = "Orpheus_LightingBloom";
        public const float DefaultExposure = 0f;
        public const float DefaultContrast = 0f;
        // Match the existing HDRP default profile, so installing the controls preserves the look.
        public const float DefaultBloom = .2f;

        private const string HdrpAssembly = ", Unity.RenderPipelines.HighDefinition.Runtime";
        private static bool _loaded;
        private static float _exposure, _contrast, _bloom;
        private static Volume _volume;
        private VolumeProfile _ownedProfile;

        public static float Exposure { get { Load(); return _exposure; } }
        public static float Contrast { get { Load(); return _contrast; } }
        public static float Bloom { get { Load(); return _bloom; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            _loaded = false;
            _volume = null;
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            float exposure = PlayerPrefs.GetFloat(ExposureKey, DefaultExposure);
            // Preserve an older brightness choice as a gain in stops, without tinting or fading UI.
            if (!PlayerPrefs.HasKey(ExposureKey) && PlayerPrefs.HasKey("Orpheus_Brightness"))
            {
                float oldBrightness = Sanitize(PlayerPrefs.GetFloat("Orpheus_Brightness", 1f), .5f, 1.5f, 1f);
                exposure = Mathf.Log(oldBrightness, 2f);
                PlayerPrefs.SetFloat(ExposureKey, exposure);
                PlayerPrefs.Save();
            }
            _exposure = Sanitize(exposure, -1f, 1f, DefaultExposure);
            _contrast = Sanitize(PlayerPrefs.GetFloat(ContrastKey, DefaultContrast), -40f, 40f, DefaultContrast);
            _bloom = Sanitize(PlayerPrefs.GetFloat(BloomKey, DefaultBloom), 0f, 1f, DefaultBloom);
        }

        public static void SetExposure(float value)
        {
            Load();
            _exposure = Sanitize(value, -1f, 1f, DefaultExposure);
            Save(ExposureKey, _exposure);
        }

        public static void SetContrast(float value)
        {
            Load();
            _contrast = Sanitize(value, -40f, 40f, DefaultContrast);
            Save(ContrastKey, _contrast);
        }

        public static void SetBloom(float value)
        {
            Load();
            _bloom = Sanitize(value, 0f, 1f, DefaultBloom);
            Save(BloomKey, _bloom);
        }

        public static void ResetDefaults()
        {
            _loaded = true;
            _exposure = DefaultExposure;
            _contrast = DefaultContrast;
            _bloom = DefaultBloom;
            PlayerPrefs.SetFloat(ExposureKey, _exposure);
            PlayerPrefs.SetFloat(ContrastKey, _contrast);
            PlayerPrefs.SetFloat(BloomKey, _bloom);
            PlayerPrefs.Save();
            EnsureApplied();
        }

        private static void Save(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            EnsureApplied();
        }

        private static float Sanitize(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

        /// <summary>Called at camera setup and menu startup, before the player opens Settings.</summary>
        public static void EnsureApplied()
        {
            Load();
            if (HdrpType("ColorAdjustments") == null) return;
            if (_volume == null)
            {
                var existing = GameObject.Find(VolumeName);
                var owner = existing != null ? existing.GetComponent<WorldLightingSettings>() : null;
                if (owner == null)
                {
                    var go = new GameObject(VolumeName);
                    owner = go.AddComponent<WorldLightingSettings>();
                    _volume = go.AddComponent<Volume>();
                    owner._ownedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                    owner._ownedProfile.name = "WorldLightingSettings_Runtime";
                    owner._ownedProfile.hideFlags = HideFlags.DontSave;
                    _volume.sharedProfile = owner._ownedProfile;
                }
                else _volume = owner.GetComponent<Volume>();
            }
            if (_volume == null || _volume.sharedProfile == null) return;
            _volume.isGlobal = true;
            _volume.priority = 510f; // Above baseline exposure (500), below independent lens effects (520).
            _volume.weight = 1f;
            SetParameter("Exposure", "mode", 0); // Fixed; prevents scene auto-exposure from undoing the choice.
            SetParameter("Exposure", "fixedExposure", 0f);
            SetParameter("ColorAdjustments", "postExposure", _exposure);
            SetParameter("ColorAdjustments", "contrast", _contrast);
            SetParameter("Bloom", "intensity", _bloom);
        }

        private static Type HdrpType(string name)
            => Type.GetType("UnityEngine.Rendering.HighDefinition." + name + HdrpAssembly);

        private static void SetParameter(string componentName, string fieldName, object value)
        {
            Type type = HdrpType(componentName);
            if (type == null) return;
            VolumeProfile profile = _volume.sharedProfile;
            VolumeComponent component = profile.components.Find(c => c != null && c.GetType() == type);
            if (component == null)
            {
                component = (VolumeComponent)ScriptableObject.CreateInstance(type);
                component.hideFlags = HideFlags.DontSave;
                profile.components.Add(component);
            }
            component.active = true;
            var parameter = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(component) as VolumeParameter;
            if (parameter == null) return;
            parameter.overrideState = true;
            PropertyInfo property = parameter.GetType().GetProperty("value");
            if (property == null) return;
            property.SetValue(parameter, property.PropertyType.IsEnum
                ? Enum.ToObject(property.PropertyType, value)
                : Convert.ChangeType(value, property.PropertyType));
        }

        private void OnDestroy()
        {
            if (_volume != null && _volume.gameObject == gameObject) _volume = null;
            if (_ownedProfile == null) return;
            foreach (var component in _ownedProfile.components)
            {
                if (Application.isPlaying) Destroy(component);
                else DestroyImmediate(component);
            }
            if (Application.isPlaying) Destroy(_ownedProfile);
            else DestroyImmediate(_ownedProfile);
        }
    }
}
