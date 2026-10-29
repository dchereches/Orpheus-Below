using System;

namespace OrpheusBelow
{
    public sealed class SpeechDucking
    {
        public const float DuckedGain = .08f;
        public float Gain { get; private set; } = 1f;
        public void Advance(bool speaking, float unscaledDeltaTime)
        {
            float delta = Math.Max(0, unscaledDeltaTime);
            Gain = speaking ? Math.Max(DuckedGain, Gain - delta * 20f) : Math.Min(1f, Gain + delta * 2f);
        }
    }

    /// <summary>Speech-only PCM processing; never changes sample count or the playback clock.</summary>
    public static class VoiceLoudness
    {
        public const float PeakCeiling = .78f;
        private const double TargetRms = .25;
        private const double MaximumGain = 16;
        private const double Knee = .45;

        public static void Normalize(float[] samples)
        {
            if (samples == null || samples.Length == 0) return;
            double energy = 0;
            for (int i = 0; i < samples.Length; i++) energy += (double)samples[i] * samples[i];
            if (energy <= 0) return;
            // One linked gain for the whole recording preserves breaths, pauses and stereo balance.
            double gain = Math.Max(1, Math.Min(MaximumGain, TargetRms / Math.Sqrt(energy / samples.Length)));
            double headroom = PeakCeiling - Knee;
            for (int i = 0; i < samples.Length; i++)
            {
                double value = samples[i] * gain;
                double magnitude = Math.Abs(value);
                // Continuous value and slope at the knee; no hard-clipped consonants or shifted samples.
                if (magnitude > Knee)
                    magnitude = Knee + headroom * (1 - Math.Exp(-(magnitude - Knee) / headroom));
                samples[i] = (float)(value < 0 ? -magnitude : magnitude);
            }
        }
    }
}
