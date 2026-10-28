using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class TripodPaintedTests
    {
        [Test]
        public void SonicPulsesHaveWarningAndARechargeableDoubleDashGap()
        {
            Assert.That(TripodAttack.WaitSeconds, Is.InRange(.4f, .8f));
            Assert.That(TripodAttack.WindupSeconds, Is.InRange(.7f, 1f));
            Assert.That(TripodAttack.PulseGapSeconds, Is.GreaterThan(.48f + .135f));
            Assert.That(TripodAttack.RecoverySeconds, Is.InRange(.4f, .8f));
            Assert.That(TripodAttack.WaveRadius, Is.GreaterThanOrEqualTo(.65f));
        }

        [Test]
        public void CancelRemovesEveryWaveAndChargeAndDiscardsPendingPulse()
        {
            var owner = new GameObject("TripodCancellationTest");
            var first = new GameObject("FirstSonicWave");
            var second = new GameObject("SecondSonicWave");
            var charge = new GameObject("ResonatingHead");
            try
            {
                var attack = owner.AddComponent<TripodAttack>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var waves = (List<PaintedEnemyProjectile>)typeof(TripodAttack).GetField("_waves", flags).GetValue(attack);
                waves.Add(first.AddComponent<PaintedEnemyProjectile>());
                waves.Add(second.AddComponent<PaintedEnemyProjectile>());
                typeof(TripodAttack).GetField("_charge", flags).SetValue(attack, charge);
                typeof(TripodAttack).GetMethod("SetPhase", flags).Invoke(attack,
                    new object[] { TripodAttack.CastPhase.PulseGap });
                Assert.That(attack.HasPendingPulse, Is.True);
                attack.Cancel();
                Assert.That(first == null, Is.True);
                Assert.That(second == null, Is.True);
                Assert.That(charge == null, Is.True);
                Assert.That(attack.ActiveWaveCount, Is.Zero);
                Assert.That(attack.HasPendingPulse, Is.False);
                Assert.That(attack.Phase, Is.EqualTo(TripodAttack.CastPhase.Wait));
                Assert.That(attack.PhaseRemaining, Is.EqualTo(TripodAttack.WaitSeconds));
                attack.Cancel();
            }
            finally
            {
                if (owner != null) Object.DestroyImmediate(owner);
                if (first != null) Object.DestroyImmediate(first);
                if (second != null) Object.DestroyImmediate(second);
                if (charge != null) Object.DestroyImmediate(charge);
            }
        }

        [Test]
        public void PulseStartsFromLiveBossPositionAndKeepsItsOwner()
        {
            var owner = new GameObject("TripodLiveOriginTest");
            TripodAttack attack = null;
            try
            {
                var actor = owner.AddComponent<PaintedEnemyActor>();
                attack = owner.AddComponent<TripodAttack>();
                attack.Initialize(actor);
                owner.transform.position = new Vector3(9f, 0f, 6f);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(TripodAttack).GetField("<LockedDirection>k__BackingField", flags).SetValue(attack, Vector3.right);
                typeof(TripodAttack).GetMethod("EmitPulse", flags).Invoke(attack, new object[] { 0 });
                var waves = (List<PaintedEnemyProjectile>)typeof(TripodAttack).GetField("_waves", flags).GetValue(attack);
                Assert.That(waves.Count, Is.EqualTo(1));
                Assert.That(waves[0].Owner, Is.SameAs(actor));
                Assert.That(Vector3.Distance(waves[0].transform.position,
                    owner.transform.position + Vector3.up * TripodAttack.WaveHeight + Vector3.right * .85f),
                    Is.LessThan(.001f));
                Assert.That(attack.PulseCount, Is.EqualTo(1));
            }
            finally
            {
                if (attack != null) attack.Cancel();
                if (owner != null) Object.DestroyImmediate(owner);
            }
        }
    }
}
