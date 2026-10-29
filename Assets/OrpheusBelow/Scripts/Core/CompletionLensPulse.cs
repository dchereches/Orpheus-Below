using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrpheusBelow
{
    /// <summary>A brief optical inhale and release for completed musical/world events.</summary>
    [DisallowMultipleComponent]
    public sealed class CompletionLensPulse : MonoBehaviour
    {
        public const string VolumeName = "Orpheus_CompletionLensPulse";
        public const float MaximumStrength = .32f;
        public const float ComboDuration = .68f;
        public const float ChamberDuration = .84f;
        public static CompletionLensPulse Instance { get; private set; }
        public float CurrentStrength { get; private set; }
        public float Elapsed { get; private set; }
        public int PulseCount { get; private set; }
        public bool IsPulsing => _duration > 0f && Elapsed < _duration;

        private VolumeProfile _ownedProfile;
        private VolumeComponent _lens;
        private VolumeParameter _intensity, _scale, _center;
        private float _duration, _peak;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            // Also covers Enter Play Mode with scene/domain reload disabled.
            if (Instance != null) Instance.ResetPulse();
            Instance = null;
        }

        public static CompletionLensPulse EnsureInstance()
        {
            if (Instance == null)
            {
                Instance = FindFirstObjectByType<CompletionLensPulse>();
                if (Instance == null) Instance = new GameObject(VolumeName).AddComponent<CompletionLensPulse>();
            }
            Instance.EnsureVolume();
            return Instance;
        }

        public static void PlayCombo(Vector3 origin) => EnsureInstance().BeginPulse(origin, ComboDuration, .28f);
        public static void PlayChamberClear(Vector3 origin) => EnsureInstance().BeginPulse(origin, ChamberDuration, MaximumStrength);

        private void Awake()
        {
            Instance = this;
            EnsureVolume();
            ResetPulse();
        }

        private void EnsureVolume()
        {
            if (_ownedProfile != null) return;
            // Runtime assembly deliberately avoids a hard dependency on HDRP.
            Type type = Type.GetType("UnityEngine.Rendering.HighDefinition.LensDistortion, Unity.RenderPipelines.HighDefinition.Runtime");
            if (type == null) return;
            var volume = GetComponent<Volume>();
            if (volume == null) volume = gameObject.AddComponent<Volume>();
            _ownedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            _ownedProfile.name = "CompletionLensPulse_Runtime";
            _ownedProfile.hideFlags = HideFlags.DontSave;
            _lens = (VolumeComponent)ScriptableObject.CreateInstance(type);
            _lens.hideFlags = HideFlags.DontSave;
            _lens.active = true;
            _ownedProfile.components.Add(_lens);
            volume.sharedProfile = _ownedProfile;
            volume.isGlobal = true;
            volume.priority = 520f;
            volume.weight = 1f;
            _intensity = Parameter("intensity");
            _scale = Parameter("scale");
            _center = Parameter("center");
            Set(Parameter("xMultiplier"), 1f);
            Set(Parameter("yMultiplier"), 1f);
            Set(_center, new Vector2(.5f, .5f));
            ApplyStrength(0f);
        }

        private VolumeParameter Parameter(string name)
            => _lens.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(_lens) as VolumeParameter;

        private static void Set(VolumeParameter parameter, object value)
        {
            if (parameter == null) return;
            parameter.overrideState = true;
            parameter.GetType().GetProperty("value")?.SetValue(parameter, value);
        }

        private void BeginPulse(Vector3 origin, float duration, float peak)
        {
            EnsureVolume();
            _duration = duration;
            _peak = Mathf.Clamp(peak, 0f, MaximumStrength);
            Elapsed = 0f;
            PulseCount++;
            Vector2 center = new Vector2(.5f, .5f);
            Camera camera = Camera.main;
            if (camera != null)
            {
                Vector3 viewport = camera.WorldToViewportPoint(origin);
                if (viewport.z > 0f)
                    center = new Vector2(Mathf.Clamp(viewport.x, .35f, .65f), Mathf.Clamp(viewport.y, .35f, .65f));
            }
            Set(_center, center);
            ApplyStrength(0f);
        }

        // LateUpdate observes pause locks raised by gameplay/UI earlier in the same frame.
        private void LateUpdate() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (Time.timeScale <= 0f || UnderworldFrontEnd.BlocksGameplay ||
                OrpheusHud.IsTutorialModalActive || VoiceDirector.BlocksGameplay)
            {
                // Freeze with the world; do not move the paused image or consume the pulse.
                return;
            }
            if (!IsPulsing)
            {
                if (CurrentStrength != 0f) ApplyStrength(0f);
                return;
            }
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            Elapsed = Mathf.Min(_duration, Elapsed + deltaTime);
            ApplyStrength(EvaluateStrength(Elapsed / _duration) * _peak);
        }

        /// <summary>Normalized, bounded curve: inhale, cross neutral, gentle outward release.</summary>
        public static float EvaluateStrength(float progress)
        {
            if (float.IsNaN(progress) || progress <= 0f || progress >= 1f) return 0f;
            if (progress < .18f) return -Mathf.SmoothStep(0f, 1f, progress / .18f);
            if (progress < .50f) return -1f + Mathf.SmoothStep(0f, 1f, (progress - .18f) / .32f);
            if (progress < .68f) return .28f * Mathf.SmoothStep(0f, 1f, (progress - .50f) / .18f);
            return .28f * (1f - Mathf.SmoothStep(0f, 1f, (progress - .68f) / .32f));
        }

        private void ApplyStrength(float strength)
        {
            CurrentStrength = Mathf.Clamp(strength, -MaximumStrength, MaximumStrength);
            Set(_intensity, CurrentStrength);
            // A small zoom keeps the distorted image inside the sampled screen edges.
            Set(_scale, 1f + Mathf.Abs(CurrentStrength) * .3f);
        }

        public void ResetPulse()
        {
            _duration = 0f;
            Elapsed = 0f;
            ApplyStrength(0f);
            Set(_center, new Vector2(.5f, .5f));
        }

        private void OnDisable() => ResetPulse();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_ownedProfile == null) return;
            foreach (VolumeComponent component in _ownedProfile.components)
                if (Application.isPlaying) Destroy(component); else DestroyImmediate(component);
            if (Application.isPlaying) Destroy(_ownedProfile); else DestroyImmediate(_ownedProfile);
        }
    }
}
