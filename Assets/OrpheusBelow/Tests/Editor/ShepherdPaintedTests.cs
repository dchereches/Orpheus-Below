using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class ShepherdPaintedTests
    {
        [Test]
        public void SheepWarningLocksDirectionAndCancellationRemovesEveryDisk()
        {
            var go = new GameObject("SheepTest");
            try
            {
                var actor = go.AddComponent<PaintedEnemyActor>();
                var sheep = go.AddComponent<SheepAttack>();
                sheep.Initialize(actor);
                sheep.CommandDash(new Vector3(8, 0, 3));
                Vector3 direction = sheep.LockedDirection;
                Assert.That(sheep.Phase, Is.EqualTo(SheepAttack.DashPhase.Warning));
                Assert.That(sheep.WarningCount, Is.GreaterThan(1));
                go.transform.position += Vector3.forward * 4;
                Assert.That(sheep.LockedDirection, Is.EqualTo(direction));
                sheep.Cancel();
                Assert.That(sheep.WarningCount, Is.Zero);
                Assert.That(sheep.Phase, Is.EqualTo(SheepAttack.DashPhase.Rest));
                Assert.That(sheep.HasHit, Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void InterruptedShepherdRemovesOnlyItsOwnSheep()
        {
            var go = new GameObject("ShepherdTest");
            var owned = new GameObject("OwnedSheep");
            var unrelated = new GameObject("OtherSheep");
            try
            {
                var actor = go.AddComponent<PaintedEnemyActor>();
                var shepherd = go.AddComponent<ShepherdAttack>();
                shepherd.Initialize(actor);
                owned.AddComponent<PaintedSummonOwnership>().Owner = actor;
                unrelated.AddComponent<PaintedSummonOwnership>();
                typeof(ShepherdAttack).GetMethod("BeginBell", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(shepherd, new object[] { Vector3.right * 6 });
                shepherd.Cancel();
                Assert.That(owned == null, Is.True);
                Assert.That(unrelated != null, Is.True);
                Assert.That(shepherd.PendingSummons, Is.Zero);
                Assert.That(shepherd.Phase, Is.EqualTo(ShepherdAttack.BellPhase.Rest));
            }
            finally
            {
                Object.DestroyImmediate(go);
                if (owned != null) Object.DestroyImmediate(owned);
                Object.DestroyImmediate(unrelated);
            }
        }

        [Test]
        public void PursuingShepherdDoesNotTranslateItsSummonedFlock()
        {
            var go = new GameObject("MovingShepherdTest");
            var actor = go.AddComponent<PaintedEnemyActor>();
            try
            {
                actor.Configure(PaintedEnemyKind.Shepherd);
                var shepherd = (ShepherdAttack)actor.Pattern;
                typeof(ShepherdAttack).GetMethod("BeginBell", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(shepherd, new object[] { Vector3.right * 30 });
                typeof(ShepherdAttack).GetMethod("Summon", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(shepherd, null);
                var flock = Object.FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None);
                Assert.That(flock.Length, Is.GreaterThan(0));
                Vector3 before = flock[0].transform.position;
                go.transform.position += Vector3.forward * 3;
                Assert.That(flock[0].transform.position, Is.EqualTo(before), "A moving parent bypasses sheep collision and invalidates their locked warnings");
            }
            finally { actor.Cancel(); Object.DestroyImmediate(go); }
        }

        [Test]
        public void FlockAndAttackCadenceProvideReadableCounterplay()
        {
            Assert.That(ShepherdAttack.MaximumLivingSheep, Is.EqualTo(3));
            Assert.That(ShepherdAttack.WarningSeconds, Is.InRange(.7f, 1f));
            Assert.That(ShepherdAttack.RestSeconds, Is.InRange(.3f, .7f));
            Assert.That(ShepherdAttack.RecoverySeconds, Is.InRange(.4f, .8f));
            Assert.That(SheepAttack.WarningSeconds, Is.GreaterThanOrEqualTo(.65f));
            Assert.That(SheepAttack.DashSpeed, Is.EqualTo(14));
            Assert.That(SheepAttack.DashDamage, Is.EqualTo(14));
        }
    }
}
