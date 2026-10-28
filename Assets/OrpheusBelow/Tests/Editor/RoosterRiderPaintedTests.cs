using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class RoosterRiderPaintedTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void CancelReleasesTheCompanionAndDisposesPendingWarning()
        {
            var rider = new GameObject("RiderCancellationTest");
            var companion = new GameObject("CompanionCancellationTest");
            try
            {
                var shade = rider.AddComponent<ShadeEnemy>();
                var actor = rider.AddComponent<PaintedEnemyActor>();
                actor.Configure(PaintedEnemyKind.RoosterRider, shade);
                var pattern = rider.GetComponent<RoosterRiderAttack>();
                var target = companion.AddComponent<BlurredEurydice>();
                Set(target, "_shownAbductorHint", true);
                target.RegisterAbductor(shade);
                Set(pattern, "_target", target);
                Set(pattern, "_registered", true);
                var warnings = (List<PaintedAttackShadow>)typeof(RoosterRiderAttack).GetField("_warnings", Hidden).GetValue(pattern);
                var warning = PaintedAttackShadow.Create(Vector3.zero, 1f, 1f);
                warnings.Add(warning);

                pattern.Cancel();
                pattern.Cancel();

                Assert.That(target.IsBeingAbducted, Is.False, "Interruption must release Eurydice immediately.");
                Assert.That(pattern.IsDragging, Is.False);
                Assert.That(pattern.RetreatDirection, Is.EqualTo(Vector3.zero));
                Assert.That(warning == null, Is.True, "No warning may survive cancellation.");
            }
            finally
            {
                Object.DestroyImmediate(rider);
                Object.DestroyImmediate(companion);
            }
        }

        [Test]
        public void RiderSeeksAClearApproachBeforeCommittingItsDash()
        {
            JourneyProgress previous = UnderworldJourney.Progress;
            var previousPlayer = OrpheusPlayerController.Instance;
            var previousMap = PaintedChamberMap.Active;
            float previousScale = Time.timeScale;
            var rider = new GameObject("BlockedRiderTest");
            var companion = new GameObject("BlockedCompanionTest");
            var cover = new GameObject("DashBlockingCover");
            try
            {
                // This is a standalone clear-lane contract, independent of the open scene's map,
                // terminal player state, or paused menu clock.
                typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, null);
                typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, null);
                Time.timeScale = 1f;
                UnderworldJourney.Reset();
                UnderworldJourney.Progress.Visit(7); UnderworldJourney.Progress.ClearCurrentChamber(); UnderworldJourney.Progress.TryRescue();
                var shade = rider.AddComponent<ShadeEnemy>(); shade.SetupArchetype(ShadeArchetype.MourningAbductor);
                var target = companion.AddComponent<BlurredEurydice>(); companion.transform.position = Vector3.right * 4;
                var pattern = rider.GetComponent<RoosterRiderAttack>(); Set(pattern, "_target", target);
                cover.transform.position = new Vector3(2, .5f, 0);
                cover.AddComponent<BoxCollider>().size = Vector3.one;
                cover.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;
                Physics.SyncTransforms();
                pattern.Tick(.1f);
                Assert.That(shade.PaintedActor.Art.Motion, Is.EqualTo(EnemyMotion.Move), "A blocked nearby target needs pursuit, not repeated blocked dashes.");
                Object.DestroyImmediate(cover); Physics.SyncTransforms();
                pattern.Tick(.1f);
                Assert.That(shade.PaintedActor.Art.Motion, Is.EqualTo(EnemyMotion.Windup));
            }
            finally
            {
                typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, previousPlayer);
                typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, previousMap);
                Time.timeScale = previousScale;
                typeof(UnderworldJourney).GetProperty("Progress").SetValue(null, previous);
                Object.DestroyImmediate(rider); Object.DestroyImmediate(companion);
                if (cover != null) Object.DestroyImmediate(cover);
            }
        }

        [Test]
        public void SoloDescentDoesNotMoveOrGrabEvenWhenACompanionObjectExists()
        {
            JourneyProgress previous = UnderworldJourney.Progress;
            var rider = new GameObject("SoloRiderTest");
            var companion = new GameObject("SoloCompanionTest");
            try
            {
                UnderworldJourney.Reset();
                var shade = rider.AddComponent<ShadeEnemy>();
                typeof(ShadeEnemy).GetField("_hp", Hidden).SetValue(shade, 82f);
                var actor = rider.AddComponent<PaintedEnemyActor>();
                actor.Configure(PaintedEnemyKind.RoosterRider, shade);
                var target = companion.AddComponent<BlurredEurydice>();
                var pattern = rider.GetComponent<RoosterRiderAttack>();
                Set(pattern, "_target", target);
                Vector3 original = rider.transform.position;

                for (int i = 0; i < 30; i++) pattern.Tick(.1f);

                Assert.That(rider.transform.position, Is.EqualTo(original));
                Assert.That(target.IsBeingAbducted, Is.False);
                Assert.That(pattern.IsDragging, Is.False);
                Assert.That(actor.Art.Motion, Is.EqualTo(EnemyMotion.Idle));
            }
            finally
            {
                typeof(UnderworldJourney).GetProperty("Progress").SetValue(null, previous);
                Object.DestroyImmediate(rider);
                Object.DestroyImmediate(companion);
            }
        }

        private static void Set(object instance, string name, object value)
            => instance.GetType().GetField(name, Hidden).SetValue(instance, value);
    }
}
