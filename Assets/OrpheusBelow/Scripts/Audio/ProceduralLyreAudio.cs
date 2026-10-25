using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// Procedural Greek Lyre + Hades Sub-Bass Boom synthesizer so Soft vs Hard attacks,
    /// Cerberus rhythm beats, and the Look-Back tragedy sound punchy out-of-the-box.
    /// </summary>
    public class ProceduralLyreAudio : MonoBehaviour
    {
        public static ProceduralLyreAudio Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _drumSource;

        // D Phrygian Dominant (classic Greek / Hades underworld scale, in Hz)
        private static readonly float[] ScaleHz = { 146.83f, 155.56f, 185.00f, 196.00f, 220.00f, 233.08f, 261.63f, 293.66f, 370.00f };
        private int _noteIdx;

        private AudioClip[] _softPlucks;
        private AudioClip _hardBoomClip;
        private AudioClip _dashClip;
        private AudioClip _beatLowClip;
        private AudioClip _beatHighClip;
        private AudioClip _tragedyChordClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            _drumSource = gameObject.AddComponent<AudioSource>();
            _drumSource.playOnAwake = false;
            _drumSource.spatialBlend = 0f;

            BuildClips();
        }

        private void BuildClips()
        {
            _softPlucks = new AudioClip[ScaleHz.Length];
            for (int i = 0; i < ScaleHz.Length; i++)
            {
                _softPlucks[i] = CreateLyrePluckClip("Lyre_" + i, ScaleHz[i] * 2f, 0.38f);
            }

            _hardBoomClip = CreateBoomClip("OrphicBoom", 0.65f);
            _dashClip = CreateDashClip("HadesDash", 0.16f);
            _beatLowClip = CreateDrumClip("CerbBeatLow", 65f, 0.18f);
            _beatHighClip = CreateDrumClip("CerbBeatHigh", 130f, 0.14f);
            _tragedyChordClip = CreateTragedyClip("LookBackTragedy", 1.8f);
        }

        public void PlaySoftAttack(float intensity)
        {
            if (_sfxSource == null || _softPlucks == null) return;
            AudioClip clip = _softPlucks[_noteIdx % _softPlucks.Length];
            _noteIdx++;
            _sfxSource.pitch = Random.Range(0.98f, 1.03f);
            _sfxSource.PlayOneShot(clip, Mathf.Lerp(0.35f, 0.75f, intensity));
        }

        public void PlayHardBoom(float intensity)
        {
            if (_sfxSource == null || _hardBoomClip == null) return;
            _sfxSource.pitch = Mathf.Lerp(0.92f, 1.06f, intensity);
            _sfxSource.PlayOneShot(_hardBoomClip, Mathf.Lerp(0.8f, 1.0f, intensity));
        }

        public void PlayDash()
        {
            if (_sfxSource != null && _dashClip != null)
                _sfxSource.PlayOneShot(_dashClip, 0.45f);
        }

        public void PlayRhythmBeat(bool heavyBeat)
        {
            if (_drumSource == null) return;
            _drumSource.PlayOneShot(heavyBeat ? _beatLowClip : _beatHighClip, heavyBeat ? 0.7f : 0.45f);
        }

        public void PlayLookBackTragedy()
        {
            if (_sfxSource != null && _tragedyChordClip != null)
            {
                _sfxSource.pitch = 1f;
                _sfxSource.PlayOneShot(_tragedyChordClip, 0.95f);
            }
        }

        private static AudioClip CreateLyrePluckClip(string name, float freq, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 8.5f) * Mathf.Clamp01(t * 260f);
                float s = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.60f
                        + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.25f * Mathf.Exp(-t * 14f)
                        + Mathf.Sin(2f * Mathf.PI * freq * 3f * t) * 0.15f * Mathf.Exp(-t * 22f);
                data[i] = s * env * 0.55f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateBoomClip(string name, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            float phaseSub = 0f;
            System.Random rng = new System.Random(42);

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                // Pitch-dropping sub-bass slam (130 Hz -> 38 Hz) + brassy power fifth (D2 + A2 + F#3)
                float subFreq = Mathf.Lerp(135f, 38f, Mathf.Clamp01(t * 7f));
                phaseSub += 2f * Mathf.PI * subFreq / sr;

                float sub = Mathf.Sin(phaseSub) * Mathf.Exp(-t * 4.5f);
                float chord = (Mathf.Sin(2f * Mathf.PI * 73.42f * t)
                             + 0.7f * Mathf.Sin(2f * Mathf.PI * 110.0f * t)
                             + 0.5f * Mathf.Sin(2f * Mathf.PI * 185.0f * t)) * Mathf.Exp(-t * 5.5f);
                float transient = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 38f) * 0.55f;

                float raw = sub * 0.85f + chord * 0.45f + transient;
                // Warm soft-clip saturation for a big punchy boom
                data[i] = (float)System.Math.Tanh(raw * 1.6f) * 0.78f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateDashClip(string name, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            System.Random rng = new System.Random(7);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                float n = (float)rng.NextDouble() * 2f - 1f;
                lp += 0.18f * (n - lp);
                data[i] = lp * env * 0.65f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateDrumClip(string name, float baseHz, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            float ph = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float f = Mathf.Lerp(baseHz * 2.1f, baseHz, Mathf.Clamp01(t * 25f));
                ph += 2f * Mathf.PI * f / sr;
                data[i] = Mathf.Sin(ph) * Mathf.Exp(-t * 16f) * 0.7f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateTragedyClip(string name, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 1.6f) * Mathf.Clamp01(t * 25f);
                // Dissonant minor/diminished shiver
                float s = Mathf.Sin(2f * Mathf.PI * 293.66f * t)
                        + Mathf.Sin(2f * Mathf.PI * 311.13f * t) * 0.8f
                        + Mathf.Sin(2f * Mathf.PI * 440.00f * t) * 0.6f;
                data[i] = (s / 2.4f) * env * 0.65f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
