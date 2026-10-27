using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class PaintedEnemyNavigationTests
    {
        // Returning the player directly, or joining clear nodes across a blocked corner,
        // would cross the lava bends in Broken Causeways or the fallen titan's cover.
        [TestCase(4, false)]
        [TestCase(4, true)]
        [TestCase(6, true)]
        public void PursuitReachesItsTargetThroughAuthoredFloorWithoutCuttingCorners(int chamber, bool detour)
        {
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + chamber.ToString("D2"));
            var map = MakeMap(layout);
            try
            {
                Vector3 start = map.ToWorld(layout.entrance);
                Vector3 goal = map.ToWorld(chamber == 4 && detour ? new Vector2(1000, 450) : layout.exit);
                if (detour) Assert.That(SegmentIsWalkable(map, start, goal, .45f), Is.False, "Fixture must require a detour.");
                int legs = FollowRoute(map, NewNavigation(), start, goal, .45f);
                if (detour) Assert.That(legs, Is.GreaterThan(1), "A blocked straight line needs intermediate waypoints.");
            }
            finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
        }

        [Test]
        public void ClearPursuitReturnsTheGoalAndPreservesActorHeightOnTranslatedMaps()
        {
            var layout = MakeOpenLayout();
            var map = MakeMap(layout);
            map.transform.position = new Vector3(300f, 7f, -120f);
            try
            {
                Vector3 start = map.ToWorld(new Vector2(300, 500));
                Vector3 goal = map.ToWorld(new Vector2(900, 500));
                goal.y += 3f;
                Vector3 waypoint = Next(NewNavigation(), map, start, goal, .45f);
                Assert.That(waypoint.x, Is.EqualTo(goal.x).Within(.001f));
                Assert.That(waypoint.z, Is.EqualTo(goal.z).Within(.001f));
                Assert.That(waypoint.y, Is.EqualTo(start.y).Within(.001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(map.gameObject); UnityEngine.Object.DestroyImmediate(layout); }
        }

        [Test]
        public void ImpassableCoverLeavesTheActorInItsReachableRegion()
        {
            var layout = MakeOpenLayout();
            layout.cover = new[] { new PaintedChamberDefinition.Cover { footprint = Rectangle(740, 0, 800, 1024) } };
            var map = MakeMap(layout);
            try
            {
                Vector3 start = map.ToWorld(new Vector2(300, 500));
                Vector3 goal = map.ToWorld(new Vector2(1100, 500));
                Assert.That(Next(NewNavigation(), map, start, goal, .45f), Is.EqualTo(start));
            }
            finally { UnityEngine.Object.DestroyImmediate(map.gameObject); UnityEngine.Object.DestroyImmediate(layout); }
        }

        [Test]
        public void EmptyFloorReturnsTheCurrentPosition()
        {
            var layout = MakeOpenLayout();
            layout.walkableBoundary = Array.Empty<Vector2>();
            var map = MakeMap(layout);
            try
            {
                Vector3 start = new Vector3(12, 0, 5);
                Assert.That(Next(NewNavigation(), map, start, Vector3.zero, .45f), Is.EqualTo(start));
            }
            finally { UnityEngine.Object.DestroyImmediate(map.gameObject); UnityEngine.Object.DestroyImmediate(layout); }
        }

        [Test]
        public void PursuitReplansWhenThePlayerMovesAcrossCover()
        {
            var layout = MakeOpenLayout();
            layout.cover = new[] { new PaintedChamberDefinition.Cover { footprint = Rectangle(650, 300, 850, 700) } };
            var map = MakeMap(layout);
            try
            {
                PaintedEnemyNavigation navigation = NewNavigation();
                Vector3 start = map.ToWorld(new Vector2(400, 500));
                Vector3 firstGoal = map.ToWorld(new Vector2(1100, 800));
                Vector3 firstWaypoint = Next(navigation, map, start, firstGoal, .45f);
                Assert.That(firstWaypoint, Is.Not.EqualTo(firstGoal));
                Assert.That(map.ToImage(firstWaypoint).y, Is.GreaterThan(500));
                Vector3 newGoal = map.ToWorld(new Vector2(1100, 200));
                Vector3 newWaypoint = Next(navigation, map, start, newGoal, .45f);
                Assert.That(newWaypoint, Is.Not.EqualTo(newGoal));
                Assert.That(map.ToImage(newWaypoint).y, Is.LessThan(500), "The old route goes around the wrong side of cover.");
                FollowRoute(map, navigation, start, newGoal, .45f);
            }
            finally { UnityEngine.Object.DestroyImmediate(map.gameObject); UnityEngine.Object.DestroyImmediate(layout); }
        }

        [Test]
        public void TitanRoomRiderRoutesAroundThePhysicalHealingWell()
        {
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_06");
            var map = MakeMap(layout);
            try
            {
                var well = AmbrosiaHealingWell.Spawn(map.transform, map.NearestWalkable(map.ToWorld(layout.well), .8f));
                if (well.GetComponent<BoxCollider>() == null)
                    typeof(AmbrosiaHealingWell).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(well, null);
                var solid = well.GetComponent<BoxCollider>();
                Assert.That(solid, Is.Not.Null, "The fixture must include the runtime well's physical collider.");
                Vector3 start = map.ToWorld(new Vector2(368, 485));
                Vector3 goal = map.ToWorld(new Vector2(460, 500));
                Physics.SyncTransforms();
                Assert.That(SegmentIsWalkable(map, start, goal, .45f), Is.True, "The authored polygons alone leave this approach clear.");
                Assert.That(SegmentIsWalkable(map, start, goal, .45f, solid), Is.False, "The actual well blocks the rider's approach.");
                int legs = FollowRoute(map, NewNavigation(), start, goal, .45f, solid);
                Assert.That(legs, Is.GreaterThan(1), "Pursuit must go around the physical well before returning to its target.");
            }
            finally { UnityEngine.Object.DestroyImmediate(map.gameObject); }
        }

        private static int FollowRoute(PaintedChamberMap map, PaintedEnemyNavigation navigation, Vector3 start, Vector3 goal, float clearance,
            Collider solid = null)
        {
            Vector3 current = start;
            int legs = 0;
            while (Vector3.Distance(current, goal) > .01f && legs < 256)
            {
                Vector3 next = Next(navigation, map, current, goal, clearance);
                Assert.That(Vector3.Distance(current, next), Is.GreaterThan(.001f), "Pursuit stalled before the reachable goal.");
                Assert.That(SegmentIsWalkable(map, current, next, clearance, solid), Is.True, "Waypoint crossed cover, a physical solid, or inaccessible floor.");
                // Exercise repeated frame-sized calls as well as waypoint arrivals.
                while (Vector3.Distance(current, next) > .01f)
                {
                    current = Vector3.MoveTowards(current, next, .15f);
                    Vector3 frameWaypoint = Next(navigation, map, current, goal, clearance);
                    Assert.That(SegmentIsWalkable(map, current, frameWaypoint, clearance, solid), Is.True);
                }
                current = next;
                legs++;
            }
            Assert.That(Vector3.Distance(current, goal), Is.LessThan(.01f), "Pursuit never reached the goal.");
            return legs;
        }

        private static bool SegmentIsWalkable(PaintedChamberMap map, Vector3 start, Vector3 end, float clearance, Collider solid = null)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / .04f));
            for (int i = 0; i <= steps; i++)
            {
                Vector3 point = Vector3.Lerp(start, end, i / (float)steps);
                if (!map.IsWalkable(point, clearance)) return false;
                point.y += .75f;
                if (solid != null && Vector3.Distance(point, solid.ClosestPoint(point)) < clearance) return false;
            }
            return true;
        }

        private static PaintedEnemyNavigation NewNavigation() => new PaintedEnemyNavigation();

        private static Vector3 Next(PaintedEnemyNavigation navigation, PaintedChamberMap map, Vector3 current, Vector3 goal, float clearance)
            => navigation.NextWaypoint(map, current, goal, clearance);

        private static PaintedChamberMap MakeMap(PaintedChamberDefinition layout)
        {
            var map = new GameObject("NavigationFixture").AddComponent<PaintedChamberMap>();
            map.definition = layout;
            return map;
        }

        private static PaintedChamberDefinition MakeOpenLayout()
        {
            var layout = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
            layout.walkableBoundary = Rectangle(100, 100, 1436, 924);
            return layout;
        }

        private static Vector2[] Rectangle(float left, float top, float right, float bottom)
            => new[] { new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom), new Vector2(left, bottom) };
    }
}
