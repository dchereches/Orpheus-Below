using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace OrpheusBelow.Tests
{
    public class DescentAscentJourneyTests
    {
        [SetUp] public void ResetRun() => UnderworldJourney.Reset();

        [Test]
        public void RescueRequiresClearingTheDeepestThroneAndTurnsTheRunAround()
        {
            var run = new JourneyProgress();
            Assert.That(run.HasEurydice, Is.False); Assert.That(run.TryRescue(), Is.False);
            for (int i = 0; i < UnderworldJourney.DeepestChamber; i++)
            {
                Assert.That(run.TryDescend(), Is.False, "Clear the room before descending.");
                run.ClearCurrentChamber(); Assert.That(run.TryDescend(), Is.True);
                Assert.That(run.TryRescue(), Is.False);
            }
            run.ClearCurrentChamber();
            Assert.That(run.TryDescend(), Is.False);
            Assert.That(run.TryRescue(), Is.True);
            Assert.That(run.Depth, Is.EqualTo(7)); Assert.That(run.Leg, Is.EqualTo(JourneyLeg.Ascent));
            Assert.That(run.HasEurydice, Is.True); Assert.That(run.IsComplete, Is.False);
            Assert.That(run.TryRescue(), Is.False);
        }

        [Test]
        public void FullCourseIsFifteenVisitsAndVictoryRequiresEurydiceAtTheSurface()
        {
            var run = new JourneyProgress(); var visits = new System.Collections.Generic.List<int> { 0 };
            for (int i = 0; i < 7; i++) { run.ClearCurrentChamber(); run.TryDescend(); visits.Add(run.Depth); }
            run.ClearCurrentChamber(); run.TryRescue();
            for (int i = 7; i > 0; i--) { run.ClearCurrentChamber(); Assert.That(run.TryAscend(), Is.True); visits.Add(run.Depth); }
            Assert.That(visits, Is.EqualTo(new[] { 0,1,2,3,4,5,6,7,6,5,4,3,2,1,0 }));
            Assert.That(run.TryAscend(), Is.False, "The final return encounter still has to be cleared.");
            run.ClearCurrentChamber(); Assert.That(run.TryAscend(false), Is.False);
            Assert.That(run.TryAscend(true), Is.True); Assert.That(run.IsComplete, Is.True);
        }

        [Test]
        public void DescentContainsNoAbductorsAndAscentKeepsGuardiansPacified()
        {
            var root = new GameObject("EncounterRoute");
            try
            {
                var down = ProceduralChamberGenerator.GenerateChamber(root.transform, 6, 8, Vector3.zero);
                Assert.That(down.guardianBoss.identity, Is.EqualTo(BossIdentity.CerberusHoundOfHades));
                Assert.That(down.wave1.Exists(s => s.archetype == ShadeArchetype.MourningAbductor), Is.False);
                UnderworldJourney.Progress.Visit(7); UnderworldJourney.Progress.ClearCurrentChamber(); UnderworldJourney.Progress.TryRescue();
                UnderworldJourney.Progress.Visit(6);
                var up = ProceduralChamberGenerator.GenerateChamber(root.transform, 6, 8, Vector3.right * 100);
                Assert.That(up.guardianBoss, Is.Null);
                Assert.That(up.wave1.Exists(s => s.archetype == ShadeArchetype.MourningAbductor), Is.True);
                Assert.That(up.exitDoor.journeyRole, Is.EqualTo(JourneyDoorRole.Surfaceward));
            }
            finally { Object.DestroyImmediate(root); UnderworldJourney.Reset(); }
        }

        [Test]
        public void EveryLayoutHasTwoReachableSafeArrivalsAndTheThroneIsLast()
        {
            for (int i = 0; i < 8; i++)
            {
                var d = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + i.ToString("D2"));
                Assert.That(d.title, Is.EqualTo(UnderworldJourney.ChamberNames[i]));
                foreach (var point in new[] { d.entrance, d.exit, d.returnExit, d.returnSpawn, d.encounterCenter })
                    Assert.That(d.IsWalkable(d.ImageToGround(point), .55f), Is.True, d.title + " point " + point);
                Assert.That(Vector3.Distance(d.ImageToGround(d.entrance), d.ImageToGround(d.returnExit)), Is.GreaterThan(3.5f));
                Assert.That(Vector3.Distance(d.ImageToGround(d.returnSpawn), d.ImageToGround(d.exit)), Is.GreaterThan(3.5f));
            }
            var throne = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_07");
            Assert.That(throne.title, Does.Contain("Throne"));
            Assert.That(throne.cover, Has.Some.Matches<PaintedChamberDefinition.Cover>(c => c.name == "HadesThrone"));
        }
        [Test]
        public void RouteHasEightPhysicalChambersForTheDescentAndReturn()
        {
            Assert.That(ProceduralChamberGenerator.TotalChambers, Is.EqualTo(8));
        }

        [Test]
        public void EscortFollowsEveryReturnPathAroundTheAuthoredCover()
        {
            var follow = typeof(BlurredEurydice).GetMethod("FollowBreadcrumbs", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int chamber = 0; chamber < UnderworldJourney.ChamberCount; chamber++)
            {
                var d = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + chamber.ToString("D2"));
                var map = PaintedChamberMap.Build(null, d, Vector3.right * (5000 + chamber * 100));
                var player = new GameObject("ReturnPathLeader");
                var soul = new GameObject("ReturnPathEscort");
                try
                {
                    const float leaderClearance = .42f; // Actual .38 player radius plus .04 margin.
                    const int maximumLeaderSteps = 4096;
                    Vector3 start = map.ToWorld(d.returnSpawn), goal = map.ToWorld(d.returnExit);
                    Assert.That(map.IsWalkable(start, leaderClearance), Is.True, "Return arrival: " + d.title);
                    Assert.That(map.IsWalkable(goal, leaderClearance), Is.True, "Return passage: " + d.title);
                    player.transform.position = start;
                    var companion = soul.AddComponent<BlurredEurydice>(); companion.orpheus = player.transform;
                    companion.SnapBehindOrpheus(start); Physics.SyncTransforms();
                    var navigation = new PaintedEnemyNavigation();
                    // Drive real pursuit from the exact authored arrival to the exact passage;
                    // retain the actual escort rather than inventing another test-only pathfinder.
                    for (int step = 0; step < maximumLeaderSteps && Vector3.Distance(player.transform.position, goal) > .001f; step++)
                    {
                        Vector3 before = player.transform.position;
                        Vector3 waypoint = navigation.NextWaypoint(map, before, goal, leaderClearance);
                        Vector3 next = Vector3.MoveTowards(before, waypoint, .25f);
                        Assert.That(Vector3.Distance(before, next), Is.GreaterThan(.0001f), "Return leader stalled in " + d.title);
                        Assert.That(map.IsWalkable(next, leaderClearance), Is.True, "Return leader left clear floor in " + d.title);
                        // Cross-check each actual leader step with the independent gameplay sweep.
                        bool blocked = map.SweepMovement(before, next - before, leaderClearance, out float fraction, out _);
                        Assert.That(!blocked || fraction >= .99999f, Is.True, "Return leader crossed cover in " + d.title);
                        player.transform.position = next;
                        follow.Invoke(companion, new object[] { .06f, companion.followLagDistance });
                        Assert.That(map.IsWalkable(companion.transform.position, .3f), Is.True, "Escort entered solid cover in " + d.title);
                    }
                    Assert.That(Vector3.Distance(player.transform.position, goal), Is.LessThan(.001f), "Return leader did not reach the passage in " + d.title);
                    for (int i = 0; i < 100; i++)
                    {
                        follow.Invoke(companion, new object[] { .06f, companion.followLagDistance });
                        Assert.That(map.IsWalkable(companion.transform.position, .3f), Is.True, "Escort entered solid cover in " + d.title);
                    }
                    Vector3 gap = companion.transform.position - map.ToWorld(d.returnExit); gap.y = 0;
                    Assert.That(gap.magnitude, Is.LessThan(6f), "Escort could not reach the upward passage in " + d.title);
                }
                finally { Object.DestroyImmediate(soul); Object.DestroyImmediate(player); Object.DestroyImmediate(map.gameObject); }
            }
        }

        [Test]
        public void FreshRunStartsAloneInsteadOfCreatingTheEscortAtTheSurface()
        {
            var before = SceneManager.GetActiveScene().GetRootGameObjects();
            try
            {
                var root = OrpheusPrototypeBuilder.BuildPrototypeWorld();
                Assert.That(root.GetComponentInChildren<BlurredEurydice>(true), Is.Null,
                    "Eurydice joins only after the deepest throne-room rescue.");
            }
            finally
            {
                foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                    if (System.Array.IndexOf(before, go) < 0) Object.DestroyImmediate(go);
            }
        }
    }
}
