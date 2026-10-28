using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class ProjectileWarningCoverageTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [TestCase(PaintedEnemyKind.Shepherd)] [TestCase(PaintedEnemyKind.Tripod)]
        public void AdjacentWarningDisksCoverTheFullContinuousProjectileCapsule(PaintedEnemyKind kind)
        {
            var go = new GameObject("ContinuousWarningCoverage");
            try
            {
                var actor = go.AddComponent<PaintedEnemyActor>(); actor.Configure(kind);
                object pattern = actor.Pattern; var type = pattern.GetType();
                type.GetProperty("LockedDirection").SetValue(pattern, Vector3.right);
                float radius, length; Vector3 origin;
                if (kind == PaintedEnemyKind.Shepherd)
                {
                    origin = (Vector3)type.GetProperty("VolleyOrigin").GetValue(pattern);
                    radius = ShepherdAttack.BellRadius; length = ShepherdAttack.BellSpeed * ShepherdAttack.BellLifetime;
                    type.GetMethod("WarnBells", Hidden).Invoke(pattern, null);
                }
                else
                {
                    origin = go.transform.position + Vector3.right * .85f;
                    radius = TripodAttack.WaveRadius; length = TripodAttack.WaveSpeed * TripodAttack.WaveLifetime;
                    type.GetMethod("PreparePulseWarning", Hidden).Invoke(pattern, new object[] { 0 });
                }
                var warnings = (List<PaintedAttackShadow>)type.GetField("_warnings", Hidden).GetValue(pattern);
                AssertCapsule(warnings, origin, Vector3.right, length, radius);
            }
            finally { go.GetComponent<PaintedEnemyActor>()?.Cancel(); Object.DestroyImmediate(go); }
        }

        [Test]
        public void RhythmWaveWarningIncludesItsSweptProjectileWidthBetweenDisks()
        {
            var go = new GameObject("RhythmWarningCoverage");
            try
            {
                var actor = go.AddComponent<PaintedEnemyActor>(); actor.Configure(PaintedEnemyKind.Tripod, createPattern: false);
                var pressure = go.AddComponent<BossRhythmPressure>(); pressure.Initialize(actor, Vector3.zero);
                pressure.Tick(.3f, Vector3.zero); for (int i = 0; i < 8; i++) pressure.Tick(.1f, Vector3.zero);
                pressure.Tick(.6f, Vector3.zero);
                var warnings = (List<PaintedAttackShadow>)typeof(BossRhythmPressure).GetField("_warnings", Hidden).GetValue(pressure);
                AssertCapsule(warnings, UnderworldCamera2D.IsoRightXZ * 8f, -UnderworldCamera2D.IsoRightXZ, 16.2f, .58f);
            }
            finally { go.GetComponent<BossRhythmPressure>()?.Cancel(); Object.DestroyImmediate(go); }
        }

        private static void AssertCapsule(List<PaintedAttackShadow> warnings, Vector3 start, Vector3 direction, float length, float radius)
        {
            Vector3 side = Vector3.Cross(direction, Vector3.up);
            for (int i = 0; i <= 160; i++) foreach (float edge in new[] { -.99f, .99f })
            {
                Vector3 point = start + direction * (length * i / 160f) + side * radius * edge;
                bool covered = false;
                foreach (var warning in warnings)
                {
                    Vector3 gap = point - warning.transform.position; gap.y = 0;
                    if (gap.sqrMagnitude <= warning.Radius * warning.Radius + .0001f) { covered = true; break; }
                }
                Assert.That(covered, Is.True, "Unwarned damage at lane sample " + i + " edge " + edge);
            }
        }
    }
}
