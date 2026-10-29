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
        private UnderworldDifficulty _previousDifficulty;
        private float _previousScale;
        private OrpheusPlayerController _previousPlayer;
        private OrpheusLyreCombat _previousCombat;
        private GameObject _root;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            _previousMode = SensorPulseInput.GlobalSourceMode;
            _previousDifficulty = DifficultySettings.CurrentDifficulty;
            SetDifficulty(UnderworldDifficulty.Mortal);
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
            SetDifficulty(_previousDifficulty);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            typeof(OrpheusLyreCombat).GetProperty("Instance").SetValue(null, _previousCombat);
            Time.timeScale = _previousScale;
        }

        [TestCase(UnderworldDifficulty.Mortal, .7f)]
        [TestCase(UnderworldDifficulty.Heroic, 1f)]
        [TestCase(UnderworldDifficulty.Tartarus, 1f)]
        public void DifficultySelectsGameplaySpeedForEveryInputMode(UnderworldDifficulty difficulty, float expected)
        {
            SetDifficulty(difficulty);
            foreach (SensorSourceMode mode in new[] { SensorSourceMode.KeyboardAndMouseOnly,
                SensorSourceMode.KeyboardPlusPCMicrophone, SensorSourceMode.KeyboardPlusESP32Serial })
            {
                SetMode(mode);
                GameplayClock.RefreshGameplayScale();
                Assert.That(GameplayClock.NormalScale, Is.EqualTo(expected));
                Assert.That(Time.timeScale, Is.EqualTo(expected));
            }
        }
        [Test]
        public void ChangingModeDuringPauseKeepsZeroAndResumesAtNewSpeed()
        {
            Time.timeScale = 0f;
            SetMode(SensorSourceMode.KeyboardPlusPCMicrophone);
            GameplayClock.RefreshGameplayScale();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameplayClock.ResumeScale, Is.EqualTo(.7f));
            SetMode(SensorSourceMode.KeyboardAndMouseOnly);
            GameplayClock.RefreshGameplayScale();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameplayClock.ResumeScale, Is.EqualTo(.7f));
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
                Assert.That(GameplayClock.ResumeScale, Is.EqualTo(.7f));
                Time.timeScale = 1f;
                SensorPulseInput.InitGlobalInput();
                Assert.That(Time.timeScale, Is.EqualTo(.7f));
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
            Assert.That(Time.timeScale, Is.EqualTo(.7f * .64f).Within(.0001f));

            var hitstop = (IEnumerator)typeof(OrpheusLyreCombat).GetMethod("HitstopRoutine", Hidden).Invoke(combat, new object[] { .048f });
            Assert.That(hitstop.MoveNext(), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(.05f));
            SetMode(SensorSourceMode.KeyboardAndMouseOnly);
            GameplayClock.RefreshGameplayScale();
            Assert.That(Time.timeScale, Is.EqualTo(.05f));
            Assert.That(hitstop.MoveNext(), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(.7f * .64f).Within(.0001f));
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
            Assert.That(GameplayClock.ResumeScale, Is.EqualTo(.7f));
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
            Assert.That(GameplayClock.ResumeScale, Is.EqualTo(.7f));
        }

        [Test]
        public void FreshDefaultAndSavedDifficultyKeepTheirSpeedThroughPausesAndLiveChanges()
        {
            const string key = "Orpheus_Difficulty";
            bool had = PlayerPrefs.HasKey(key);
            int saved = PlayerPrefs.GetInt(key);
            try
            {
                PlayerPrefs.DeleteKey(key);
                DifficultySettings.Initialize();
                Assert.That(DifficultySettings.CurrentDifficulty, Is.EqualTo(UnderworldDifficulty.Mortal));
                Assert.That(GameplayClock.NormalScale, Is.EqualTo(.7f));
                Time.timeScale = 0f;
                DifficultySettings.SetDifficulty(UnderworldDifficulty.Heroic);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(GameplayClock.ResumeScale, Is.EqualTo(1f));
                DifficultySettings.Initialize();
                Assert.That(DifficultySettings.CurrentDifficulty, Is.EqualTo(UnderworldDifficulty.Heroic));
                DifficultySettings.SetDifficulty(UnderworldDifficulty.Tartarus);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(GameplayClock.ResumeScale, Is.EqualTo(1f));
                Time.timeScale = 1f;
                DifficultySettings.SetDifficulty(UnderworldDifficulty.Mortal);
                Assert.That(Time.timeScale, Is.EqualTo(.7f));
                DifficultySettings.SetDifficulty(UnderworldDifficulty.Heroic);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(key, saved); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        private static void SetDifficulty(UnderworldDifficulty difficulty) =>
            typeof(DifficultySettings).GetProperty("CurrentDifficulty").SetValue(null, difficulty);

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
