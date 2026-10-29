using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class GameplayClockTests : CombatGameplayTestContext
    {
        private SensorSourceMode _previousMode;
        private float _previousScale;
        private OrpheusPlayerController _previousPlayer;
        private OrpheusLyreCombat _previousCombat;
        private GameObject _root;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            _previousMode = SensorPulseInput.GlobalSourceMode;
            _previousScale = Time.timeScale;
            _previousPlayer = OrpheusPlayerController.Instance;
            _previousCombat = OrpheusLyreCombat.Instance;
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, null);
            typeof(OrpheusLyreCombat).GetProperty("Instance").SetValue(null, null);
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            SetMode(_previousMode);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            typeof(OrpheusLyreCombat).GetProperty("Instance").SetValue(null, _previousCombat);
            Time.timeScale = _previousScale;
        }

        [TestCase(SensorSourceMode.KeyboardAndMouseOnly, 1f)]
        [TestCase(SensorSourceMode.KeyboardPlusPCMicrophone, .8f)]
        [TestCase(SensorSourceMode.KeyboardPlusESP32Serial, 1f)]
        public void ModeSelectsGameplaySpeed(SensorSourceMode mode, float expected)
        {
            SetMode(mode);
            GameplayClock.RefreshGameplayScale();
            Assert.That(GameplayClock.NormalScale, Is.EqualTo(expected));
            Assert.That(Time.timeScale, Is.EqualTo(expected));
        }

        [Test]
        public void ChangingModeDuringPauseKeepsZeroAndResumesAtNewSpeed()
        {
            Time.timeScale = 0f;
            SetMode(SensorSourceMode.KeyboardPlusPCMicrophone);
            GameplayClock.RefreshGameplayScale();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameplayClock.ResumeScale, Is.EqualTo(.8f));
            SetMode(SensorSourceMode.KeyboardAndMouseOnly);
            GameplayClock.RefreshGameplayScale();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameplayClock.ResumeScale, Is.EqualTo(1f));
        }

        [Test]
        public void SavedMicrophoneModeAppliesOnStartupWithoutReleasingPause()
        {
            const string key = "OrpheusBelow.SensorSourceMode";
            bool hadPreference = PlayerPrefs.HasKey(key);
            int previousPreference = PlayerPrefs.GetInt(key);
            try
            {
                PlayerPrefs.SetInt(key, (int)SensorSourceMode.KeyboardPlusPCMicrophone);
                Time.timeScale = 0f;
                SensorPulseInput.InitGlobalInput();
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(GameplayClock.ResumeScale, Is.EqualTo(.8f));
                Time.timeScale = 1f;
                SensorPulseInput.InitGlobalInput();
                Assert.That(Time.timeScale, Is.EqualTo(.8f));
            }
            finally
            {
                if (hadPreference) PlayerPrefs.SetInt(key, previousPreference);
                else PlayerPrefs.DeleteKey(key);
            }
        }

        [Test]
        public void LookBackAndHitstopSurviveModeChangesAndRestoreCurrentBase()
        {
            CreatePlayer();
            var player = _root.GetComponent<OrpheusPlayerController>();
            var combat = _root.GetComponent<OrpheusLyreCombat>();
            typeof(OrpheusPlayerController).GetProperty("LookBackProgress01").SetValue(player, .5f);
            SetMode(SensorSourceMode.KeyboardPlusPCMicrophone);
            GameplayClock.RefreshGameplayScale();
            Assert.That(Time.timeScale, Is.EqualTo(.8f * .64f).Within(.0001f));

            var hitstop = (IEnumerator)typeof(OrpheusLyreCombat).GetMethod("HitstopRoutine", Hidden).Invoke(combat, new object[] { .048f });
            Assert.That(hitstop.MoveNext(), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(.05f));
            SetMode(SensorSourceMode.KeyboardAndMouseOnly);
            GameplayClock.RefreshGameplayScale();
            Assert.That(Time.timeScale, Is.EqualTo(.05f));
            Assert.That(hitstop.MoveNext(), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(.64f).Within(.0001f));
        }

        [Test]
        public void HitstopEndingWhilePausedCannotReleasePauseOrLeaveStaleModifier()
        {
            CreatePlayer();
            SetMode(SensorSourceMode.KeyboardPlusPCMicrophone);
            var combat = _root.GetComponent<OrpheusLyreCombat>();
            var hitstop = (IEnumerator)typeof(OrpheusLyreCombat).GetMethod("HitstopRoutine", Hidden).Invoke(combat, new object[] { .048f });
            hitstop.MoveNext();
            Time.timeScale = 0f;
            Assert.That(hitstop.MoveNext(), Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameplayClock.ResumeScale, Is.EqualTo(.8f));
        }

        [Test]
        public void TerminalPlayerEventsCannotReleaseAnExistingPause()
        {
            CreatePlayer();
            SetMode(SensorSourceMode.KeyboardPlusPCMicrophone);
            Time.timeScale = 0f;
            var player = _root.GetComponent<OrpheusPlayerController>();
            player.OnHitStyxHazard(1000f);
            Assert.That(player.IsGameOver, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameplayClock.ResumeScale, Is.EqualTo(.8f));
        }

        private void CreatePlayer()
        {
            _root = new GameObject("GameplayClockPlayer");
            var player = _root.AddComponent<OrpheusPlayerController>();
            var combat = _root.AddComponent<OrpheusLyreCombat>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, player);
            typeof(OrpheusLyreCombat).GetProperty("Instance").SetValue(null, combat);
        }

        private static void SetMode(SensorSourceMode mode) =>
            typeof(SensorPulseInput).GetProperty("GlobalSourceMode").SetValue(null, mode);
    }
}
