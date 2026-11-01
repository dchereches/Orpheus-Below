using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class AlexaDifficultyAndXboxTests
    {
        private UnderworldDifficulty _savedDifficulty;
        private float _savedHp;
        private float _savedSpeed;

        [SetUp]
        public void SetUp()
        {
            _savedDifficulty = DifficultySettings.CurrentDifficulty;
            _savedHp = DifficultySettings.AlexaHp;
            _savedSpeed = DifficultySettings.AlexaGameSpeed;
        }

        [TearDown]
        public void TearDown()
        {
            DifficultySettings.SetDifficulty(_savedDifficulty);
            DifficultySettings.SetAlexaHp(_savedHp);
            DifficultySettings.SetAlexaGameSpeed(_savedSpeed);
        }

        [Test]
        public void AlexaModeIsNotDefaultDifficulty()
        {
            // Mortal is 0 and the intended story default
            Assert.That((int)UnderworldDifficulty.Mortal, Is.EqualTo(0));
            Assert.That((int)UnderworldDifficulty.Alexa, Is.EqualTo(3));
            Assert.That(UnderworldDifficulty.Alexa, Is.Not.EqualTo(UnderworldDifficulty.Mortal));
        }

        [Test]
        public void AlexaDifficultySlidersScaleAndClampProperly()
        {
            // HP slider tests (25 to 500)
            DifficultySettings.SetAlexaHp(200f);
            Assert.That(DifficultySettings.AlexaHp, Is.EqualTo(200f));

            DifficultySettings.SetAlexaHp(10f); // below min
            Assert.That(DifficultySettings.AlexaHp, Is.EqualTo(25f));

            DifficultySettings.SetAlexaHp(1000f); // above max
            Assert.That(DifficultySettings.AlexaHp, Is.EqualTo(500f));

            // Game Speed slider tests (0.30 to 2.00)
            DifficultySettings.SetAlexaGameSpeed(1.25f);
            Assert.That(DifficultySettings.AlexaGameSpeed, Is.EqualTo(1.25f).Within(0.001f));

            DifficultySettings.SetAlexaGameSpeed(0.10f); // below min
            Assert.That(DifficultySettings.AlexaGameSpeed, Is.EqualTo(0.30f).Within(0.001f));

            DifficultySettings.SetAlexaGameSpeed(3.50f); // above max
            Assert.That(DifficultySettings.AlexaGameSpeed, Is.EqualTo(2.00f).Within(0.001f));
        }

        [Test]
        public void EffectiveMaxHealthAppliesAlexaHpOnlyInAlexaMode()
        {
            DifficultySettings.SetAlexaHp(350f);

            DifficultySettings.SetDifficulty(UnderworldDifficulty.Mortal);
            Assert.That(DifficultySettings.EffectiveMaxHealth(100f), Is.EqualTo(100f));

            DifficultySettings.SetDifficulty(UnderworldDifficulty.Heroic);
            Assert.That(DifficultySettings.EffectiveMaxHealth(100f), Is.EqualTo(100f));

            DifficultySettings.SetDifficulty(UnderworldDifficulty.Tartarus);
            Assert.That(DifficultySettings.EffectiveMaxHealth(100f), Is.EqualTo(100f));

            DifficultySettings.SetDifficulty(UnderworldDifficulty.Alexa);
            Assert.That(DifficultySettings.EffectiveMaxHealth(100f), Is.EqualTo(350f));
        }

        [Test]
        public void EffectiveGameSpeedAppliesAlexaSpeedOnlyInAlexaMode()
        {
            DifficultySettings.SetAlexaGameSpeed(1.45f);

            DifficultySettings.SetDifficulty(UnderworldDifficulty.Mortal);
            Assert.That(DifficultySettings.EffectiveGameSpeed, Is.EqualTo(0.70f).Within(0.001f));

            DifficultySettings.SetDifficulty(UnderworldDifficulty.Heroic);
            Assert.That(DifficultySettings.EffectiveGameSpeed, Is.EqualTo(1.00f).Within(0.001f));

            DifficultySettings.SetDifficulty(UnderworldDifficulty.Tartarus);
            Assert.That(DifficultySettings.EffectiveGameSpeed, Is.EqualTo(1.00f).Within(0.001f));

            DifficultySettings.SetDifficulty(UnderworldDifficulty.Alexa);
            Assert.That(DifficultySettings.EffectiveGameSpeed, Is.EqualTo(1.45f).Within(0.001f));
            Assert.That(GameplayClock.NormalScale, Is.EqualTo(1.45f).Within(0.001f));
        }

        [Test]
        public void DifficultyCyclesAcrossAllFourPresets()
        {
            DifficultySettings.SetDifficulty(UnderworldDifficulty.Mortal);
            Assert.That(DifficultySettings.CurrentDifficulty, Is.EqualTo(UnderworldDifficulty.Mortal));

            DifficultySettings.CycleNext();
            Assert.That(DifficultySettings.CurrentDifficulty, Is.EqualTo(UnderworldDifficulty.Heroic));

            DifficultySettings.CycleNext();
            Assert.That(DifficultySettings.CurrentDifficulty, Is.EqualTo(UnderworldDifficulty.Tartarus));

            DifficultySettings.CycleNext();
            Assert.That(DifficultySettings.CurrentDifficulty, Is.EqualTo(UnderworldDifficulty.Alexa));

            DifficultySettings.CycleNext();
            Assert.That(DifficultySettings.CurrentDifficulty, Is.EqualTo(UnderworldDifficulty.Mortal));
        }

        [Test]
        public void Xbox360InputPropertiesSafeWithoutGamepad()
        {
            // Null safety checks when running headlessly or without physical controllers plugged in
            Assert.DoesNotThrow(() =>
            {
                var move = Xbox360Input.Move;
                var aim = Xbox360Input.Aim;
                bool dash = Xbox360Input.DashPressed;
                bool soft = Xbox360Input.SoftAttackPressed;
                bool softHeld = Xbox360Input.SoftAttackHeld;
                bool softRel = Xbox360Input.SoftAttackReleased;
                bool boom = Xbox360Input.HardBoomPressed;
                bool flute = Xbox360Input.FluteTogglePressed;
                bool look = Xbox360Input.LookBackHeld;
                bool pause = Xbox360Input.PausePressed;
                bool back = Xbox360Input.BackPressed;
                bool confirm = Xbox360Input.ConfirmPressed;
                bool help = Xbox360Input.HelpPressed;
                bool interact = Xbox360Input.InteractPressed;
            });
        }
    }
}
