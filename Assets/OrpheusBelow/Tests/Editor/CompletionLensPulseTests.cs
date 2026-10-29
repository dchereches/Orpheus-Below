using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class CompletionLensPulseTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private HashSet<int> _before;
        private float _scale;
        private Random.State _random;
        private JourneyProgress _journey;
        private object _director, _voice, _flute;

        [SetUp]
        public void SetUp()
        {
            _before = new HashSet<int>(Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Select(o => o.GetInstanceID()));
            _scale = Time.timeScale;
            _random = Random.state;
            Time.timeScale = 1f;
            _journey = UnderworldJourney.Progress;
            UnderworldJourney.Reset();
            _director = typeof(HadesChamberDirector).GetProperty("Instance").GetValue(null);
            _voice = typeof(VoiceDirector).GetProperty("Instance").GetValue(null);
            _flute = typeof(OrpheusFluteChannel).GetProperty("Instance").GetValue(null);
            typeof(VoiceDirector).GetProperty("Instance").SetValue(null, null);
        }

        [TearDown]
        public void TearDown()
        {
            // This runtime-only component receives no engine lifecycle callbacks in EditMode.
            // Release its native profile explicitly before destroying the isolated test objects.
            foreach (CompletionLensPulse pulse in Object.FindObjectsByType<CompletionLensPulse>(FindObjectsSortMode.None))
                if (!_before.Contains(pulse.gameObject.GetInstanceID())) Lifecycle(pulse, "OnDestroy");
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go != null && !_before.Contains(go.GetInstanceID())) Object.DestroyImmediate(go);
            typeof(UnderworldJourney).GetProperty("Progress").SetValue(null, _journey);
            typeof(HadesChamberDirector).GetProperty("Instance").SetValue(null, _director);
            typeof(VoiceDirector).GetProperty("Instance").SetValue(null, _voice);
            typeof(OrpheusFluteChannel).GetProperty("Instance").SetValue(null, _flute);
            Random.state = _random;
            Time.timeScale = _scale;
        }

        [Test]
        public void CurveInhalesReboundsAndReturnsExactlyNeutralWithinBound()
        {
            Assert.That(CompletionLensPulse.EvaluateStrength(0f), Is.Zero);
            Assert.That(CompletionLensPulse.EvaluateStrength(.18f), Is.EqualTo(-1f).Within(.0001f));
            Assert.That(CompletionLensPulse.EvaluateStrength(.5f), Is.Zero.Within(.0001f));
            Assert.That(CompletionLensPulse.EvaluateStrength(.68f), Is.EqualTo(.28f).Within(.0001f));
            Assert.That(CompletionLensPulse.EvaluateStrength(1f), Is.Zero);
            Assert.That(CompletionLensPulse.EvaluateStrength(float.NaN), Is.Zero);
            for (int i = 0; i <= 1000; i++)
                Assert.That(CompletionLensPulse.EvaluateStrength(i / 1000f), Is.InRange(-1f, .28f));
        }

        [Test]
        public void DedicatedHdrpVolumeStartsNeutralAndCleansItsOwnedObjects()
        {
            var pulse = CompletionLensPulse.EnsureInstance();
            Volume volume = pulse.GetComponent<Volume>();
            VolumeProfile profile = volume.sharedProfile;
            Assert.That(volume.isGlobal, Is.True);
            Assert.That(volume.priority, Is.EqualTo(520f));
            Assert.That(volume.weight, Is.EqualTo(1f));
            Assert.That(profile.components.Count, Is.EqualTo(1));
            VolumeComponent lens = profile.components.Single();
            Assert.That(lens.GetType().FullName, Is.EqualTo("UnityEngine.Rendering.HighDefinition.LensDistortion"));
            AssertParameter(lens, "intensity", 0f);
            AssertParameter(lens, "scale", 1f);
            Assert.That(pulse.IsPulsing, Is.False);
            Assert.That(CompletionLensPulse.EnsureInstance(), Is.SameAs(pulse));
            Lifecycle(pulse, "OnDestroy");
            Object.DestroyImmediate(pulse.gameObject);
            Assert.That(profile == null, Is.True);
            Assert.That(lens == null, Is.True);
            Assert.That(CompletionLensPulse.Instance == null, Is.True);
            Assert.That(CompletionLensPulse.EnsureInstance().CurrentStrength, Is.Zero);
        }

        [Test]
        public void PulseSettlesAndResetOrDisableCannotLeaveDistortionBehind()
        {
            CompletionLensPulse.PlayChamberClear(Vector3.zero);
            var pulse = CompletionLensPulse.Instance;
            pulse.Advance(CompletionLensPulse.ChamberDuration * .18f);
            Assert.That(pulse.CurrentStrength, Is.EqualTo(-CompletionLensPulse.MaximumStrength).Within(.0001f));
            VolumeComponent lens = pulse.GetComponent<Volume>().sharedProfile.components.Single();
            AssertParameter(lens, "intensity", pulse.CurrentStrength);
            Assert.That((float)Parameter(lens, "scale"), Is.GreaterThan(1f));
            pulse.Advance(CompletionLensPulse.ChamberDuration * .5f);
            Assert.That(pulse.CurrentStrength, Is.GreaterThan(0f));
            pulse.Advance(10f);
            Assert.That(pulse.IsPulsing, Is.False);
            Assert.That(pulse.CurrentStrength, Is.Zero);
            AssertParameter(lens, "intensity", 0f);
            AssertParameter(lens, "scale", 1f);
            CompletionLensPulse.PlayCombo(Vector3.zero);
            pulse.Advance(.12f);
            pulse.enabled = false;
            Lifecycle(pulse, "OnDisable");
            Assert.That(pulse.CurrentStrength, Is.Zero);
            Assert.That(pulse.IsPulsing, Is.False);
            AssertParameter(lens, "intensity", 0f);
            AssertParameter(lens, "scale", 1f);
            pulse.enabled = true;
            pulse.Advance(.2f);
            Assert.That(pulse.CurrentStrength, Is.Zero);
            CompletionLensPulse.PlayCombo(Vector3.zero);
            pulse.Advance(.1f);
            typeof(CompletionLensPulse).GetMethod("ResetSession", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            Assert.That(CompletionLensPulse.EnsureInstance(), Is.SameAs(pulse));
            Assert.That(pulse.CurrentStrength, Is.Zero, "A new play session must discard any previous pulse.");
            Assert.That(pulse.IsPulsing, Is.False);
        }

        [Test]
        public void RealPauseMenuTutorialAndVoiceFreezeClockAndRenderedStrength()
        {
            CompletionLensPulse.PlayCombo(Vector3.zero);
            var pulse = CompletionLensPulse.Instance;
            pulse.Advance(.1f);
            float elapsed = pulse.Elapsed, strength = pulse.CurrentStrength;
            Time.timeScale = 0f;
            AssertFrozen(pulse, elapsed, strength);
            Time.timeScale = 1f;
            var menuLock = typeof(UnderworldFrontEnd).GetField("_menuBlocksGameplay", BindingFlags.Static | BindingFlags.NonPublic);
            menuLock.SetValue(null, true);
            AssertFrozen(pulse, elapsed, strength);
            menuLock.SetValue(null, false);
            typeof(OrpheusHud).GetProperty("IsTutorialModalActive").SetValue(null, true);
            AssertFrozen(pulse, elapsed, strength);
            typeof(OrpheusHud).GetProperty("IsTutorialModalActive").SetValue(null, false);
            var voice = new GameObject("LensVoicePause").AddComponent<VoiceDirector>();
            typeof(VoiceDirector).GetProperty("Instance").SetValue(null, voice);
            var queue = (VoicePlaybackQueue)typeof(VoiceDirector).GetField("_queue", Hidden).GetValue(voice);
            queue.Enqueue(new[] { VoiceLineId.Hades_ABreath }, null);
            Assert.That(VoiceDirector.BlocksGameplay, Is.True);
            AssertFrozen(pulse, elapsed, strength);
            queue.Clear();
            pulse.Advance(.1f);
            Assert.That(pulse.Elapsed, Is.GreaterThan(elapsed));
            pulse.Advance(10f);
            Assert.That(pulse.CurrentStrength, Is.Zero);
        }

        [Test]
        public void OnlyCompletedFourNoteComboPulsesAndChamberClearGuardPulsesOnce()
        {
            var pulse = CompletionLensPulse.EnsureInstance();
            var flute = new GameObject("LensFluteCompletion").AddComponent<OrpheusFluteChannel>();
            if (OrpheusFluteChannel.Instance != flute) typeof(OrpheusFluteChannel).GetMethod("Awake", Hidden).Invoke(flute, null);
            flute.EnterFluteStance();
            for (int i = 0; i < 4; i++)
            {
                bool hard = flute.TargetSequence[flute.CurrentNoteIndex] == 'B';
                flute.ProcessFlutePulse(hard ? .8f : .3f, hard);
                Assert.That(pulse.PulseCount, Is.EqualTo(i == 3 ? 1 : 0));
            }
            Assert.That(pulse.IsPulsing, Is.True);
            flute.ExitFluteStance();
            flute.EnterFluteStance();
            bool wrongHard = flute.TargetSequence[0] != 'B';
            flute.ProcessFlutePulse(wrongHard ? .8f : .3f, wrongHard);
            Assert.That(pulse.PulseCount, Is.EqualTo(1), "Discord must not trigger the completion pulse.");
            var director = new GameObject("LensChamberClear").AddComponent<HadesChamberDirector>();
            director.chambers.Add(new HadesChamberDirector.ChamberRuntimeData());
            MethodInfo clear = typeof(HadesChamberDirector).GetMethod("HandleChamberCleared", Hidden);
            clear.Invoke(director, null);
            clear.Invoke(director, null);
            Assert.That(pulse.PulseCount, Is.EqualTo(2), "Repeated chamber-clear checks must emit exactly one pulse.");
        }

        private static void AssertFrozen(CompletionLensPulse pulse, float elapsed, float strength)
        {
            pulse.Advance(.5f);
            Assert.That(pulse.Elapsed, Is.EqualTo(elapsed));
            Assert.That(pulse.CurrentStrength, Is.EqualTo(strength));
            AssertParameter(pulse.GetComponent<Volume>().sharedProfile.components.Single(), "intensity", strength);
        }

        private static void Lifecycle(CompletionLensPulse pulse, string callback)
            => typeof(CompletionLensPulse).GetMethod(callback, Hidden).Invoke(pulse, null);

        private static object Parameter(VolumeComponent lens, string name)
        {
            var parameter = (VolumeParameter)lens.GetType().GetField(name).GetValue(lens);
            Assert.That(parameter.overrideState, Is.True);
            return parameter.GetType().GetProperty("value").GetValue(parameter);
        }

        private static void AssertParameter(VolumeComponent lens, string name, float expected)
            => Assert.That((float)Parameter(lens, name), Is.EqualTo(expected).Within(.0001f));
    }
}
