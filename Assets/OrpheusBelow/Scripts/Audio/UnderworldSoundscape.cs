using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Cached procedural foley and a continuous, chamber-reactive underworld score.</summary>
    public sealed class UnderworldSoundscape : MonoBehaviour
    {
        public static UnderworldSoundscape Instance { get; private set; }
        public float Volume { get; private set; }
        public bool IsMuted { get; private set; }

        private const int SampleRate = 22050;
        private const float Tau = Mathf.PI * 2f;
        private const string VolumeKey = "OrpheusBelow.MasterVolume";
        private const string MuteKey = "OrpheusBelow.Muted";
        private readonly List<AudioClip> _clips = new List<AudioClip>(16);
        private AudioSource _effects, _feet, _ui, _wind, _water, _drone;
        private AudioClip _impact, _armor, _hurt, _step, _splash, _portal, _dry, _move, _confirm;
        private float _previousVolume, _tension, _lastImpact = -1f, _lastMove = -1f;
        private bool _preferencesDirty;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _previousVolume = AudioListener.volume;
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.75f));
            IsMuted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
            ApplyVolume();
            _effects = Source("CombatFoley", false);
            _feet = Source("Footfalls", false);
            _ui = Source("MenuChimes", true);
            _wind = Source("CavernWind", true);
            _water = Source("StyxWater", true);
            _drone = Source("UnderworldDrone", true);
            _impact = CreateEffect("StoneAndFlesh", 0.27f, 0);
            _armor = CreateEffect("BronzeArmorCrack", 0.44f, 1);
            _hurt = CreateEffect("OrpheusWounded", 0.52f, 2);
            _step = CreateEffect("StoneFootfall", 0.16f, 3);
            _splash = CreateEffect("ShallowWaterStep", 0.25f, 4);
            _portal = CreateEffect("ThresholdResonance", 1.9f, 5);
            _dry = CreateEffect("SpentBreath", 0.22f, 6);
            _move = CreateEffect("MenuMovement", 0.16f, 7);
            _confirm = CreateEffect("MenuConfirmation", 0.55f, 8);
            StartLoop(_wind, CreateAtmosphere("WindLoop", 0), 0.25f);
            StartLoop(_water, CreateAtmosphere("WaterLoop", 1), 0.22f);
            StartLoop(_drone, CreateAtmosphere("DroneLoop", 2), 0.23f);
        }

        private AudioSource Source(string label, bool duringPause)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = duringPause;
            source.priority = duringPause ? 80 : 100;
            return source;
        }

        private static void StartLoop(AudioSource source, AudioClip clip, float volume)
        {
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.Play();
        }

        private void Update()
        {
            var director = HadesChamberDirector.Instance;
            bool menu = UnderworldFrontEnd.BlocksGameplay;
            float desiredTension = director != null && !menu ? Mathf.Clamp01(director.ActiveEnemyCount / 10f) : 0f;
            _tension = Mathf.MoveTowards(_tension, desiredTension, Time.unscaledDeltaTime * 0.15f);
            float depth = director == null ? 0f : Mathf.Clamp01(director.CurrentChamberIndex / (float)(ProceduralChamberGenerator.TotalChambers - 1));
            _wind.volume = Mathf.Lerp(0.23f, 0.34f, depth) + _tension * 0.06f;
            _water.volume = Mathf.Lerp(0.25f, 0.11f, depth) * (1f - _tension * 0.25f);
            _drone.volume = (menu ? 0.29f : 0.21f) + _tension * 0.17f;
        }

        public void SetVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Approximately(Volume, volume)) return;
            Volume = volume;
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            _preferencesDirty = true;
            ApplyVolume();
        }

        public void SetMuted(bool muted)
        {
            if (IsMuted == muted) return;
            IsMuted = muted;
            PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
            _preferencesDirty = true;
            ApplyVolume();
        }

        private void ApplyVolume() { AudioListener.volume = IsMuted ? 0f : Volume; }

        public void PlayImpact(bool armor = false)
        {
            if (UnderworldFrontEnd.BlocksGameplay || Time.unscaledTime - _lastImpact < 0.035f) return;
            _lastImpact = Time.unscaledTime;
            Play(_effects, armor ? _armor : _impact, armor ? 0.72f : 0.62f, true);
        }

        public void PlayHurt() { Play(_effects, _hurt, 0.78f); }
        public void PlayFootstep(bool wet = false) { Play(_feet, wet ? _splash : _step, wet ? 0.33f : 0.26f, true); }
        public void PlayPortal() { Play(_effects, _portal, 0.64f); }
        public void PlayDryNote() { Play(_effects, _dry, 0.38f); }
        public void PlayMenuMove()
        {
            if (Time.unscaledTime - _lastMove < 0.06f) return;
            _lastMove = Time.unscaledTime;
            Play(_ui, _move, 0.35f);
        }
        public void PlayMenuConfirm() { Play(_ui, _confirm, 0.5f); }

        private static void Play(AudioSource source, AudioClip clip, float volume, bool vary = false)
        {
            if (source == null || clip == null || (!source.ignoreListenerPause && UnderworldFrontEnd.BlocksGameplay)) return;
            source.pitch = vary ? Random.Range(0.92f, 1.08f) : 1f;
            source.PlayOneShot(clip, volume);
        }

        private AudioClip SaveClip(string name, float[] data, int channels = 1)
        {
            var clip = AudioClip.Create(name, data.Length / channels, channels, SampleRate, false);
            clip.SetData(data, 0);
            _clips.Add(clip);
            return clip;
        }

        private AudioClip CreateEffect(string name, float duration, int kind)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            var samples = new float[length];
            var rng = new System.Random(619 + kind * 41);
            float low = 0f, phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                low += 0.13f * (noise - low);
                float value;
                switch (kind)
                {
                    case 0:
                        phase += Tau * (53f + 170f * Mathf.Exp(-t * 38f)) / SampleRate;
                        value = Mathf.Sin(phase) * Mathf.Exp(-t * 18f) * 0.75f + noise * Mathf.Exp(-t * 65f) * 0.44f + low * Mathf.Exp(-t * 16f);
                        break;
                    case 1:
                        value = (Mathf.Sin(Tau * 837f * t) * 0.25f + Mathf.Sin(Tau * 1353f * t) * 0.16f + Mathf.Sin(Tau * 2231f * t) * 0.1f) * Mathf.Exp(-t * 12f)
                            + noise * Mathf.Exp(-t * 43f) * 0.7f + Mathf.Sin(Tau * 92f * t) * Mathf.Exp(-t * 20f) * 0.3f;
                        break;
                    case 2:
                        phase += Tau * Mathf.Lerp(165f, 57f, Mathf.Clamp01(t * 4f)) / SampleRate;
                        value = (Mathf.Sin(phase) * 0.55f + low * 1.1f) * Mathf.Exp(-t * 9f);
                        break;
                    case 3:
                        value = (low * 1.5f + Mathf.Sin(Tau * 96f * t) * 0.4f) * Mathf.Exp(-t * 36f) + noise * Mathf.Exp(-t * 105f) * 0.2f;
                        break;
                    case 4:
                        value = low * Mathf.Exp(-t * 16f) * 1.7f + Mathf.Sin(Tau * (610f * t - 450f * t * t)) * Mathf.Exp(-t * 30f) * 0.22f;
                        break;
                    case 5:
                        float swell = Mathf.Sin(Mathf.PI * t / duration);
                        value = (Mathf.Sin(Tau * 146.83f * t) + Mathf.Sin(Tau * 220f * t) * 0.5f + Mathf.Sin(Tau * 293.66f * t) * 0.25f) * swell * 0.25f
                            + low * swell * 0.33f;
                        break;
                    case 6:
                        value = (low * 1.3f + Mathf.Sin(Tau * 83f * t) * 0.16f) * Mathf.Sin(Mathf.PI * t / duration) * Mathf.Exp(-t * 9f);
                        break;
                    case 7:
                        value = (Mathf.Sin(Tau * 659.25f * t) + Mathf.Sin(Tau * 1318.5f * t) * 0.22f) * Mathf.Exp(-t * 32f) * 0.35f;
                        break;
                    default:
                        value = (Mathf.Sin(Tau * 440f * t) * Mathf.Exp(-t * 10f) + Mathf.Sin(Tau * 659.25f * t) * Mathf.Exp(-t * 7f)
                            + Mathf.Sin(Tau * 880f * t) * Mathf.Exp(-t * 13f) * 0.4f) * 0.27f;
                        break;
                }
                // Every transient has a short attack and a zero tail; no discontinuity clicks.
                float edge = Mathf.Clamp01(t * 700f) * Mathf.Clamp01((duration - t) * 80f);
                samples[i] = Mathf.Clamp(value * edge, -0.95f, 0.95f);
            }
            return SaveClip(name, samples);
        }

        private AudioClip CreateAtmosphere(string name, int kind)
        {
            const float duration = 12f;
            int frames = (int)(duration * SampleRate);
            var data = new float[frames * 2];
            var rng = new System.Random(7103 + kind);
            float left = 0f, right = 0f;
            for (int i = 0; i < frames; i++)
            {
                float t = i / (float)SampleRate;
                float cycle = Tau * t / duration;
                left += 0.035f * ((float)rng.NextDouble() * 2f - 1f - left);
                right += 0.035f * ((float)rng.NextDouble() * 2f - 1f - right);
                if (kind == 2)
                {
                    // Integral cycles per loop keep all harmonics and slow modulation phase-aligned.
                    float breath = 0.82f + 0.18f * Mathf.Sin(cycle * 2f);
                    float root = Mathf.Sin(cycle * 881f) * 0.30f;
                    float fifth = Mathf.Sin(cycle * 1320f) * 0.15f;
                    float upper = Mathf.Sin(cycle * 1762f) * 0.06f;
                    data[i * 2] = (root + fifth + upper + Mathf.Sin(cycle * 1867f) * 0.035f) * breath;
                    data[i * 2 + 1] = (root + fifth + upper + Mathf.Sin(cycle * 1868f) * 0.035f) * breath;
                }
                else
                {
                    float tide = kind == 0 ? 0.68f + 0.32f * Mathf.Sin(cycle + 0.7f) : 0.7f + 0.3f * Mathf.Sin(cycle * 3f);
                    float bubbles = kind == 1 ? Mathf.Sin(cycle * 3541f + Mathf.Sin(cycle * 13f) * 6f) * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(cycle * 17f), 12f) * 0.09f : 0f;
                    data[i * 2] = left * tide * 1.8f + bubbles;
                    data[i * 2 + 1] = right * tide * 1.8f + bubbles * 0.7f;
                }
            }
            if (kind != 2)
            {
                // Blend a shared seam into both ends while retaining a continuous noise floor.
                int seam = SampleRate / 2;
                for (int i = 0; i < seam; i++)
                {
                    float blend = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * i / (seam - 1));
                    for (int ch = 0; ch < 2; ch++)
                    {
                        int tail = (frames - seam + i) * 2 + ch;
                        int head = i * 2 + ch;
                        data[tail] = Mathf.Lerp(data[tail], data[head], blend);
                    }
                }
                // The crossfade ends at sample seam-1, so loop the retained section after the head.
                var loop = new float[(frames - seam) * 2];
                System.Array.Copy(data, seam * 2, loop, 0, loop.Length);
                data = loop;
            }
            return SaveClip(name, data, 2);
        }

        private void SavePreferences()
        {
            if (!_preferencesDirty) return;
            PlayerPrefs.Save();
            _preferencesDirty = false;
        }

        private void OnApplicationPause(bool paused) { if (paused) SavePreferences(); }
        private void OnApplicationQuit() { SavePreferences(); }
        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            AudioListener.volume = _previousVolume;
            SavePreferences();
            foreach (var clip in _clips) if (clip != null) Destroy(clip);
            _clips.Clear();
        }
    }
}
