using NUnit.Framework;

namespace OrpheusBelow.Tests
{
    public class OrphicPressureTests
    {
        [Test]
        public void HeldBoomCannotChainThroughRecoveryOrSustainWithoutRest()
        {
            var breath = new OrphicBreath();
            Assert.That(breath.TryPlay(true), Is.True);
            Assert.That(breath.TryPlay(true), Is.False);
            breath.Tick(0.5f);
            Assert.That(breath.TryPlay(true), Is.True);
            breath.Tick(0.5f);
            Assert.That(breath.TryPlay(true), Is.True);
            Assert.That(breath.DamageMultiplier, Is.LessThan(1f));
            breath.Tick(0.5f);
            Assert.That(breath.TryPlay(true), Is.False);
            breath.Tick(3f);
            Assert.That(breath.TryPlay(true), Is.True);
            Assert.That(breath.DamageMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void HymnSequenceIsAffordableAndRewardsBreathWithoutSpamPenalty()
        {
            var breath = new OrphicBreath();
            for (int phrase = 0; phrase < 4; phrase++)
            {
                Assert.That(breath.TryPlay(false), Is.True);
                breath.Tick(0.24f);
                Assert.That(breath.TryPlay(false), Is.True);
                breath.Tick(0.24f);
                Assert.That(breath.TryPlay(true), Is.True);
                Assert.That(breath.DamageMultiplier, Is.EqualTo(1f));
                float spent = breath.Breath;
                breath.CompleteCadence();
                Assert.That(breath.Breath, Is.GreaterThan(spent));
                breath.Tick(0.5f);
            }
        }

        [Test]
        public void RepeatedSoftNotesLoseDamageButChangingNoteRestoresIt()
        {
            var breath = new OrphicBreath();
            for (int i = 0; i < 4; i++) { Assert.That(breath.TryPlay(false), Is.True); breath.Tick(0.23f); }
            Assert.That(breath.DamageMultiplier, Is.EqualTo(0.3f));
            Assert.That(breath.TryPlay(true), Is.True);
            Assert.That(breath.DamageMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void RepeatedHitsCannotPermaStunAndCadencesOpenDefenses()
        {
            var shade = new ShadeResonance();
            Assert.That(shade.TryStagger(), Is.True);
            shade.Tick(0.25f);
            Assert.That(shade.TryStagger(), Is.False);
            float guarded = shade.ResolveDamage(40f);
            shade.Expose();
            Assert.That(shade.TryStagger(), Is.True);
            Assert.That(shade.ResolveDamage(40f), Is.GreaterThan(guarded * 1.8f));
            shade.Tick(3.3f);
            Assert.That(shade.ResolveDamage(40f), Is.EqualTo(guarded));
        }
    }
}
