using System;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class VoiceLoudnessTests
    {
        [Test]
        public void QuietNarrationGetsSubstantialGainWithoutMovingAnySamples()
        {
            var samples = new float[48000];
            for (int i = 0; i < samples.Length; i++) samples[i] = (float)Math.Sin(i * .13) * .03f;
            double before = Rms(samples);
            VoiceLoudness.Normalize(samples);
            Assert.That(Rms(samples) / before, Is.GreaterThan(7), "At least 16dB of actual PCM gain");
            Assert.That(Rms(samples), Is.InRange(.20, .27));
            Assert.That(samples[0], Is.Zero);
            Assert.That(samples.Length, Is.EqualTo(48000));
        }

        [Test]
        public void LoudConsonantsCompressSmoothlyWithHeadroomAndUnchangedSilence()
        {
            var samples = new[] { 0f, .01f, -.01f, .03f, -.03f, .9f, -.9f, 0f };
            VoiceLoudness.Normalize(samples);
            Assert.That(samples[5], Is.InRange(.45f, .780001f));
            Assert.That(samples[5], Is.GreaterThan(samples[3]));
            Assert.That(samples[5] + samples[6], Is.EqualTo(0).Within(.00001));
            Assert.That(samples[0], Is.Zero);
            Assert.That(samples[7], Is.Zero);
            foreach (float sample in samples) Assert.That(Math.Abs(sample), Is.LessThanOrEqualTo(.780001f));
        }

        [Test]
        public void SilenceAndNearlySilentRecordingsDoNotGetUnboundedGain()
        {
            var silence = new float[100];
            VoiceLoudness.Normalize(silence);
            Assert.That(Rms(silence), Is.Zero);
            var noise = new[] { 1e-9f, -1e-9f };
            VoiceLoudness.Normalize(noise);
            Assert.That(Math.Abs(noise[0]), Is.LessThanOrEqualTo(1.60001e-8f));
        }

        [Test]
        public void PreparedSpeechRetainsItsClockAndStereoAndIsCachedWithoutChangingAsset()
        {
            var original = AudioClip.Create("SpeechTest", 4800, 2, 48000, false);
            float masterVolume = AudioListener.volume;
            bool masterPause = AudioListener.pause;
            using (var cache = new VoiceAudioClipCache())
            {
                try
                {
                    var input = new float[9600];
                    for (int i = 0; i < 4800; i++) { input[i * 2] = .01f; input[i * 2 + 1] = -.02f; }
                    original.SetData(input, 0);
                    var prepared = cache.Prepare(original);
                    Assert.That(prepared, Is.Not.SameAs(original));
                    Assert.That(cache.Prepare(original), Is.SameAs(prepared));
                    Assert.That(prepared.name, Is.EqualTo("SpeechTest"));
                    Assert.That(prepared.samples, Is.EqualTo(4800));
                    Assert.That(prepared.channels, Is.EqualTo(2));
                    Assert.That(prepared.frequency, Is.EqualTo(48000));
                    Assert.That(prepared.length, Is.EqualTo(.1f).Within(.00001));
                    Assert.That(AudioListener.volume, Is.EqualTo(masterVolume));
                    Assert.That(AudioListener.pause, Is.EqualTo(masterPause));
                    var output = new float[9600]; prepared.GetData(output, 0);
                    Assert.That(output[0], Is.GreaterThan(.1f));
                    Assert.That(output[1] / output[0], Is.EqualTo(-2f).Within(.0001));
                    original.GetData(output, 0);
                    Assert.That(output[0], Is.EqualTo(.01f).Within(.00001), "Imported asset remains untouched");
                }
                finally { UnityEngine.Object.DestroyImmediate(original); }
            }
        }

        [TestCase("Cinematic/Intro/Narration")]
        [TestCase("Voices/Narrator/todefendyourself")]
        [TestCase("Voices/Hades/abreath")]
        [TestCase("Voices/Eurydice/orpheus")]
        public void ImportedNarrationTutorialAndReunionReachAudibleLevelWithoutClipping(string resource)
        {
            var original = Resources.Load<AudioClip>(resource);
            Assert.That(original, Is.Not.Null, resource);
            var data = new float[original.samples * original.channels];
            Assert.That(original.GetData(data, 0), Is.True);
            using (var cache = new VoiceAudioClipCache())
            {
                var prepared = cache.Prepare(original);
                Assert.That(prepared.GetData(data, 0), Is.True);
                // Assert the resulting listening level even when imported media is already amplified.
                Assert.That(Rms(data), Is.InRange(.18, .27), resource);
                float peak = 0f;
                foreach (float sample in data) peak = Math.Max(peak, Math.Abs(sample));
                Assert.That(peak, Is.LessThanOrEqualTo(.780001f));
                Assert.That(prepared.samples, Is.EqualTo(original.samples));
                Assert.That(prepared.frequency, Is.EqualTo(original.frequency));
            }
        }

        [Test]
        public void SpeechDucksCompetingBusesAndRestoresThemWithoutAWorldPause()
        {
            var mix = new SpeechDucking();
            mix.Advance(true, .1f);
            Assert.That(mix.Gain, Is.LessThanOrEqualTo(.081f));
            mix.Advance(false, .1f);
            Assert.That(mix.Gain, Is.InRange(.1f, .4f), "Release avoids an abrupt jump after a line");
            mix.Advance(false, 2f);
            Assert.That(mix.Gain, Is.EqualTo(1f));
        }

        private static double Rms(float[] samples)
        {
            double energy = 0;
            foreach (float value in samples) energy += (double)value * value;
            return Math.Sqrt(energy / samples.Length);
        }
    }
}
