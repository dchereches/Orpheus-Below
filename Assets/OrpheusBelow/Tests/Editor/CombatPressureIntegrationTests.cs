using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class CombatPressureIntegrationTests : CombatGameplayTestContext
    {
        [TestCase(0, false, 2)]
        [TestCase(2, false, 2)]
        [TestCase(4, false, 3)]
        [TestCase(7, false, 3)]
        [TestCase(0, true, 3)]
        [TestCase(5, true, 3)]
        public void FloorEncountersHaveMultipleIncreasingWaves(int depth, bool ascending, int count)
        {
            for (int wave = 1; wave <= count; wave++)
                Assert.That(ChamberEncounterPlan.Wave(depth, wave, ascending).Count,
                    Is.GreaterThanOrEqualTo(wave == 1 && depth == 0 && !ascending ? 3 : 4), "Wave " + wave);
            Assert.That(ChamberEncounterPlan.Wave(depth, count + 1, ascending), Is.Empty);
        }

        [Test]
        public void SpawnSelectionRejectsDisconnectedWalkableIslandAndStaysNearPlayer()
        {
            var layout = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
            layout.walkableBoundary = Rectangle(0, 0, 1536, 1024);
            layout.cover = new[] { new PaintedChamberDefinition.Cover { footprint = Rectangle(740, 0, 800, 1024) } };
            layout.well = new Vector2(100, 100);
            var go = new GameObject("SplitFloor"); var map = go.AddComponent<PaintedChamberMap>(); map.definition = layout;
            try
            {
                Vector3 anchor = map.ToWorld(new Vector2(600, 500));
                Vector3 island = map.ToWorld(new Vector2(1100, 500));
                Assert.That(map.IsWalkable(island, .65f), Is.True, "The rejected island is genuinely walkable.");
                var choose = typeof(PaintedEnemyNavigation).GetMethod("TryFindSpawn", BindingFlags.Static | BindingFlags.Public);
                Assert.That(choose, Is.Not.Null, "Spawns require connected-region selection, not only NearestWalkable.");
                var occupied = new List<Vector3>();
                for (int i = 0; i < 6; i++)
                {
                    object[] args = { map, anchor, island, occupied, Vector3.zero };
                    Assert.That((bool)choose.Invoke(null, args), Is.True);
                    Vector3 point = (Vector3)args[4];
                    Assert.That(map.ToImage(point).x, Is.LessThan(740));
                    Assert.That(map.IsWalkable(point, .65f), Is.True);
                    Assert.That(Vector3.Distance(anchor, point), Is.InRange(2.5f, 11f));
                    foreach (var previous in occupied) Assert.That(Vector3.Distance(previous, point), Is.GreaterThanOrEqualTo(1.5f));
                    occupied.Add(point);
                }
            }
            finally { PaintedEnemyNavigation.Invalidate(layout); Object.DestroyImmediate(go); Object.DestroyImmediate(layout); }
        }

        [TestCase(PaintedEnemyKind.Cerberus)]
        [TestCase(PaintedEnemyKind.Tripod)]
        [TestCase(PaintedEnemyKind.MysticEyeWarden)]
        [TestCase(PaintedEnemyKind.MaskedMonk)]
        [TestCase(PaintedEnemyKind.WingedSentinel)]
        [TestCase(PaintedEnemyKind.HoodedSkeleton)]
        [TestCase(PaintedEnemyKind.RoosterRider)]
        [TestCase(PaintedEnemyKind.Shepherd)]
        [TestCase(PaintedEnemyKind.Sheep)]
        public void EveryPaintedActorHasOneVisibleGroundShadow(PaintedEnemyKind kind)
        {
            var go = new GameObject("ShadowContract");
            try
            {
                var actor = go.AddComponent<PaintedEnemyActor>(); actor.Configure(kind, createPattern: false);
                actor.Configure(kind, createPattern: false);
                var shadow = go.transform.Find("EnemyGroundShadow");
                Assert.That(shadow, Is.Not.Null);
                Assert.That(shadow.GetComponent<SpriteRenderer>().enabled, Is.True);
                Assert.That(shadow.GetComponent<SpriteRenderer>().color.a, Is.GreaterThan(.2f));
                int shadows = 0; foreach (Transform child in go.transform) if (child.name == "EnemyGroundShadow") shadows++;
                Assert.That(shadows, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [TestCase(0, false)] [TestCase(2, false)] [TestCase(4, false)] [TestCase(5, false)] [TestCase(7, false)]
        [TestCase(0, true)] [TestCase(1, true)] [TestCase(2, true)] [TestCase(3, true)]
        [TestCase(4, true)] [TestCase(5, true)] [TestCase(6, true)]
        public void RealChamberEntryHasEnoughConnectedSpacedSlotsForItsLargestWave(int depth, bool returning)
        {
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + depth.ToString("D2"));
            var go = new GameObject("ConnectedSpawnSlots"); var map = go.AddComponent<PaintedChamberMap>(); map.definition = layout;
            try
            {
                Vector3 anchor = map.NearestWalkable(map.ToWorld(returning ? layout.returnSpawn : layout.entrance), .55f);
                var occupied = new List<Vector3>();
                int wave = ChamberEncounterPlan.WaveCount(depth, returning);
                int count = ChamberEncounterPlan.Wave(depth, wave, returning).Count;
                for (int i = 0; i < count; i++)
                {
                    Vector3 desired = map.ToWorld(layout.enemySpawns[i % layout.enemySpawns.Length]);
                    Assert.That(PaintedEnemyNavigation.TryFindSpawn(map, anchor, desired, occupied, out var point), Is.True,
                        "Missing safe slot " + i + " in chamber " + depth);
                    Assert.That(PaintedEnemyNavigation.CanReach(map, anchor, point), Is.True);
                    Assert.That(map.IsWalkable(point, .65f), Is.True);
                    Assert.That(Vector3.Distance(anchor, point), Is.LessThanOrEqualTo(10.1f));
                    foreach (var other in occupied) Assert.That(Vector3.Distance(other, point), Is.GreaterThanOrEqualTo(1.5f));
                    occupied.Add(point);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void RhythmPressureWarnsWithoutAnyMissedNoteAndCancellationRemovesIt()
        {
            var go = new GameObject("RhythmPressureContract");
            try
            {
                var actor = go.AddComponent<PaintedEnemyActor>(); actor.Configure(PaintedEnemyKind.Tripod, createPattern: false);
                Type type = typeof(PaintedEnemyActor).Assembly.GetType("OrpheusBelow.BossRhythmPressure");
                Assert.That(type, Is.Not.Null, "Correct note answers must still require dodging.");
                var pressure = go.AddComponent(type);
                type.GetMethod("Initialize").Invoke(pressure, new object[] { actor, Vector3.zero });
                type.GetMethod("Tick").Invoke(pressure, new object[] { .4f, Vector3.left * 2 });
                Assert.That((int)type.GetProperty("WarningsEmitted").GetValue(pressure), Is.GreaterThan(0));
                Assert.That((int)type.GetProperty("ActiveWarningCount").GetValue(pressure), Is.GreaterThan(0));
                type.GetMethod("Cancel").Invoke(pressure, null);
                Assert.That((int)type.GetProperty("ActiveWarningCount").GetValue(pressure), Is.Zero);
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static Vector2[] Rectangle(float left, float top, float right, float bottom)
            => new[] { new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom), new Vector2(left, bottom) };

        [Test]
        public void WarnedRhythmStrikeHurtsStationaryPlayerButRespectsDashAndLockedTarget()
        {
            float clock = Time.timeScale; Time.timeScale = 1;
            var hero = new GameObject("RhythmDodgePlayer"); var boss = new GameObject("RhythmStrikeOwner");
            var previousPlayer = OrpheusPlayerController.Instance;
            try
            {
                var player = hero.AddComponent<OrpheusPlayerController>(); player.RestoreTravelHealth(100);
                typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, player);
                var actor = boss.AddComponent<PaintedEnemyActor>(); actor.Configure(PaintedEnemyKind.Tripod, createPattern: false);
                var pressure = boss.AddComponent<BossRhythmPressure>(); pressure.Initialize(actor, Vector3.zero);
                player.transform.position = -UnderworldCamera2D.IsoRightXZ * 2;
                pressure.Tick(.4f, player.transform.position);
                float health = player.CurrentHealth;
                pressure.Tick(2f, player.transform.position);
                Assert.That(player.CurrentHealth, Is.EqualTo(health), "A hitch cannot swallow the dodge warning.");
                for (int i = 0; i < 7; i++) pressure.Tick(.1f, player.transform.position);
                Assert.That(player.CurrentHealth, Is.LessThan(health), "A perfect rhythm input cannot make standing still safe.");
                pressure.Initialize(actor, Vector3.zero);
                typeof(OrpheusPlayerController).GetField("_invulnTimer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, 0f);
                typeof(OrpheusPlayerController).GetField("_isDashing", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, true);
                pressure.Tick(.4f, player.transform.position); health = player.CurrentHealth;
                for (int i = 0; i < 8; i++) pressure.Tick(.1f, player.transform.position);
                Assert.That(player.CurrentHealth, Is.EqualTo(health));
                pressure.Initialize(actor, Vector3.zero);
                typeof(OrpheusPlayerController).GetField("_isDashing", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, false);
                pressure.Tick(.4f, player.transform.position);
                player.transform.position += UnderworldCamera2D.IsoRightXZ * 5;
                for (int i = 0; i < 8; i++) pressure.Tick(.1f, player.transform.position);
                Assert.That(player.CurrentHealth, Is.EqualTo(health), "Leaving the locked circle must evade it.");
            }
            finally
            {
                boss.GetComponent<BossRhythmPressure>()?.Cancel(); Object.DestroyImmediate(boss); Object.DestroyImmediate(hero);
                typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, previousPlayer); Time.timeScale = clock;
            }
        }
    }
}
