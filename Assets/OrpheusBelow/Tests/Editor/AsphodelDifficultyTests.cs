using NUnit.Framework;

namespace OrpheusBelow.Tests
{
    public class AsphodelDifficultyTests
    {
        [Test]
        public void WretchAndWardenApplyHarderMeleePressureWithoutShorterTelegraph()
        {
            ShadeBalance wretch = ShadeBalance.Get(ShadeArchetype.WretchLunger);
            ShadeBalance warden = ShadeBalance.Get(ShadeArchetype.ArmoredWarden);

            Assert.That(wretch.MaxHealth, Is.EqualTo(86f));
            Assert.That(wretch.ContactDamage, Is.EqualTo(18f));
            Assert.That(wretch.AttackRecovery, Is.EqualTo(0.68f));
            Assert.That(wretch.LungeTelegraph, Is.EqualTo(0.34f));
            Assert.That(warden.MaxHealth, Is.EqualTo(150f));
            Assert.That(warden.ContactDamage, Is.EqualTo(24f));
            Assert.That(warden.LungeTelegraph, Is.EqualTo(0.34f));
        }

        [Test]
        public void CasterFiresHarderMoreOften()
        {
            ShadeBalance caster = ShadeBalance.Get(ShadeArchetype.BrimstoneCaster);

            Assert.That(caster.MaxHealth, Is.EqualTo(78f));
            Assert.That(caster.OrbDamage, Is.EqualTo(17f));
            Assert.That(caster.VolleyCooldown, Is.EqualTo(1.12f));
        }

        [Test]
        public void AbductorSurvivesLonger()
        {
            Assert.That(ShadeBalance.Get(ShadeArchetype.MourningAbductor).MaxHealth, Is.EqualTo(82f));
        }

        [Test]
        public void RestoredAsphodelUsesFiveEnemiesPerWave()
        {
            Assert.That(ProceduralChamberGenerator.GetAsphodelWaveSpecs(1), Has.Count.EqualTo(5));
            Assert.That(ProceduralChamberGenerator.GetAsphodelWaveSpecs(2), Has.Count.EqualTo(5));
        }
    }
}
