using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// Procedural Greek Lyre, Sub-Bass Orphic Boom, Eurydice Acoustic Hum Reply,
    /// Cadence Chime, and Pacification Bell synthesizer (zero external audio dependencies).
    /// </summary>
    public class ProceduralLyreAudio : MonoBehaviour
    {
        public static ProceduralLyreAudio Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _drumSource;

        // D Phrygian Dominant (Greek mythic scale, in Hz)
        private static readonly float[] ScaleHz = { 146.83f, 155.56f, 185.00f, 196.00f, 220.00f, 233.08f, 261.63f, 293.66f, 370.00f };
        private int _noteIdx;

        private AudioClip[] _softPlucks;
        private AudioClip _hardBoomClip;
        private AudioClip _dashClip;
        private AudioClip _beatLowClip;
        private AudioClip _beatHighClip;
        private AudioClip _tragedyChordClip;
        private AudioClip _eurydiceHumClip;
        private AudioClip _cadenceArpClip;
        private AudioClip _pacifyBellClip;
        private AudioClip _fluteNoteClipLow;
        private AudioClip _fluteNoteClipHigh;
        private AudioClip _fluteBacklashClip;
        private AudioClip _fluteWeepClip;
        private AudioClip _cadenceUnlockFanfareClip;

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
            _eurydiceHumClip = CreateEurydiceHumClip("EurydiceHum", 0.95f);
            _cadenceArpClip = CreateCadenceArpClip("CadenceArp", 0.55f);
            _pacifyBellClip = CreatePacifyBellClip("PacifyBell", 0.52f);
            _fluteNoteClipLow = CreateFluteToneClip("FluteLow", 440f, 0.45f);
            _fluteNoteClipHigh = CreateFluteToneClip("FluteHigh", 659.25f, 0.45f);
            _fluteBacklashClip = CreateFluteBacklashClip("FluteBacklash", 0.55f);
            _fluteWeepClip = CreateFluteWeepClip("FluteWeep", 1.2f);
            _cadenceUnlockFanfareClip = CreateUnlockFanfareClip("UnlockFanfare", 1.8f);
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

        public void PlayEurydiceHumReply()
        {
            if (_drumSource != null && _eurydiceHumClip != null)
            {
                _drumSource.PlayOneShot(_eurydiceHumClip, 0.72f);
            }
        }

        public void PlayCadenceComplete()
        {
            if (_drumSource != null && _cadenceArpClip != null)
            {
                _drumSource.PlayOneShot(_cadenceArpClip, 0.80f);
            }
        }

        public void PlayShadePacified()
        {
            if (_drumSource != null && _pacifyBellClip != null)
            {
                _drumSource.PlayOneShot(_pacifyBellClip, 0.42f);
            }
        }

        public void PlayFluteNote(bool highPitch)
        {
            if (_sfxSource != null)
            {
                AudioClip clip = highPitch ? _fluteNoteClipHigh : _fluteNoteClipLow;
                if (clip != null)
                {
                    _sfxSource.pitch = Random.Range(0.97f, 1.03f);
                    _sfxSource.PlayOneShot(clip, 0.75f);
                }
            }
        }

        public void PlayFluteBacklash()
        {
            if (_sfxSource != null && _fluteBacklashClip != null)
            {
                _sfxSource.pitch = 1.0f;
                _sfxSource.PlayOneShot(_fluteBacklashClip, 0.90f);
            }
        }

        public void PlayFluteFleeCatharsis()
        {
            if (_drumSource != null && _fluteWeepClip != null)
            {
                _drumSource.PlayOneShot(_fluteWeepClip, 0.85f);
            }
        }

        public void PlayCadenceUnlockFanfare()
        {
            if (_sfxSource != null && _cadenceUnlockFanfareClip != null)
            {
                _sfxSource.pitch = 1.0f;
                _sfxSource.PlayOneShot(_cadenceUnlockFanfareClip, 0.95f);
            }
        }

        private static AudioClip CreateFluteToneClip(string name, float freq, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            float phase = 0f;
            System.Random rng = new System.Random(name.GetHashCode());

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float breath = ((float)rng.NextDouble() * 2f - 1f) * 0.08f;
                float vib = Mathf.Sin(t * 32f) * 4.5f;
                phase += 2f * Mathf.PI * (freq + vib) / sr;
                float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                float tone = Mathf.Sin(phase) * 0.70f + Mathf.Sin(phase * 2f) * 0.22f + Mathf.Sin(phase * 3f) * 0.08f;
                data[i] = (tone + breath) * env * 0.60f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateFluteBacklashClip(string name, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            System.Random rng = new System.Random(999);

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float f1 = Mathf.Lerp(1200f, 220f, t / duration);
                float f2 = Mathf.Lerp(1250f, 290f, t / duration);
                float env = Mathf.Exp(-t * 5.2f) * Mathf.Clamp01(t * 40f);
                float screech = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.5f;
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.35f;
                data[i] = (screech + noise) * env * 0.75f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateFluteWeepClip(string name, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float f = Mathf.Lerp(520f, 240f, Mathf.Pow(t / duration, 1.4f));
                float env = Mathf.Exp(-t * 2.2f) * Mathf.Clamp01(t * 18f);
                float s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f + Mathf.Sin(4f * Mathf.PI * f * t) * 0.25f;
                data[i] = s * env * 0.55f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateUnlockFanfareClip(string name, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            float[] arpeggio = { 293.66f, 369.99f, 440.00f, 587.33f, 739.99f, 880.00f };

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                int step = Mathf.Min(arpeggio.Length - 1, Mathf.FloorToInt((t / duration) * arpeggio.Length * 1.2f));
                float localT = t - step * (duration / (arpeggio.Length * 1.2f));
                float env = Mathf.Exp(-Mathf.Max(0f, localT) * 4.5f);
                float f = arpeggio[step];
                float s = Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 2f * t);
                data[i] = s * env * 0.45f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
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
                float subFreq = Mathf.Lerp(135f, 38f, Mathf.Clamp01(t * 7f));
                phaseSub += 2f * Mathf.PI * subFreq / sr;

                float sub = Mathf.Sin(phaseSub) * Mathf.Exp(-t * 4.5f);
                float chord = (Mathf.Sin(2f * Mathf.PI * 73.42f * t)
                             + 0.7f * Mathf.Sin(2f * Mathf.PI * 110.0f * t)
                             + 0.5f * Mathf.Sin(2f * Mathf.PI * 185.0f * t)) * Mathf.Exp(-t * 5.5f);
                float transient = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 38f) * 0.55f;

                float raw = sub * 0.85f + chord * 0.45f + transient;
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
                float s = Mathf.Sin(2f * Mathf.PI * 293.66f * t)
                        + Mathf.Sin(2f * Mathf.PI * 311.13f * t) * 0.8f
                        + Mathf.Sin(2f * Mathf.PI * 440.00f * t) * 0.6f;
                data[i] = (s / 2.4f) * env * 0.65f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateEurydiceHumClip(string name, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            float phase = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                // Ethereal female vocal/flute 2-note rising fifth (D5 587Hz -> A5 880Hz) with soft vibrato
                float baseF = t < 0.34f ? 587.33f : Mathf.Lerp(587.33f, 880.0f, Mathf.Clamp01((t - 0.34f) * 9f));
                float vib = Mathf.Sin(t * 34f) * 3.5f;
                phase += 2f * Mathf.PI * (baseF + vib) / sr;
                float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI) * Mathf.Clamp01(t * 12f);
                data[i] = (Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.18f) * env * 0.42f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateCadenceArpClip(string name, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            float[] notes = { 293.66f, 369.99f, 440.00f, 587.33f };
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                int step = Mathf.Min(3, Mathf.FloorToInt((t / duration) * 4.5f));
                float localT = t - step * (duration / 4.5f);
                float env = Mathf.Exp(-Mathf.Max(0f, localT) * 9f);
                float f = notes[step];
                data[i] = (Mathf.Sin(2f * Mathf.PI * f * t) + 0.35f * Mathf.Sin(4f * Mathf.PI * f * t)) * env * 0.40f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreatePacifyBellClip(string name, float duration)
        {
            const int sr = 44100;
            int len = Mathf.CeilToInt(sr * duration);
            float[] data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 7.5f) * Mathf.Clamp01(t * 120f);
                float s = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.55f
                        + Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.35f;
                data[i] = s * env * 0.36f;
            }
            AudioClip clip = AudioClip.Create(name, len, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
