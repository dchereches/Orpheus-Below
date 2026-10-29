using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class ChamberBoundaryPolishTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void PlayerCannotLeavePaintedFloorWithoutPhysicalBoundaryColliders(bool dash)
        {
            WithFloor((layout, map, player) =>
            {
                Move(player, map.ToWorld(new Vector2(1700, 512)) - player.transform.position, dash);
                Assert.That(map.IsWalkable(player.transform.position, player.bodyRadius), Is.True);
                Assert.That(map.ToImage(player.transform.position).x, Is.LessThan(1528));
                Assert.That(map.ToImage(player.transform.position).x, Is.GreaterThan(1500), "Reach the edge instead of rejecting the whole movement.");
            });
        }

        [Test]
        public void DashCannotSkipThinAuthoredCoverWhenBothEndpointsAreWalkable()
        {
            WithFloor((layout, map, player) =>
            {
                layout.cover = new[] { new PaintedChamberDefinition.Cover {
                    footprint = new[] { new Vector2(800, 400), new Vector2(802, 400), new Vector2(802, 620), new Vector2(800, 620) }
                } };
                Move(player, map.ToWorld(new Vector2(900, 512)) - player.transform.position, true);
                Assert.That(map.ToImage(player.transform.position).x, Is.LessThan(792), "A thin obstacle must stop the entire swept radius.");
            });
        }

        [Test]
        public void NavigationClearanceUsesGroundDistanceAtAnyActorHeight()
        {
            WithFloor((layout, map, player) =>
            {
                var nearEdge = layout.ImageToGround(new Vector2(4, 512));
                Assert.That(layout.IsWalkable(nearEdge, .4f), Is.False);
                Assert.That(layout.IsWalkable(nearEdge + Vector3.up * 6f, .4f), Is.False,
                    "Visual height must not weaken horizontal clearance.");
            });
        }

        [Test]
        public void GlancingMovementSlidesAlongTheBoundary()
        {
            WithFloor((layout, map, player) =>
            {
                Move(player, map.ToWorld(new Vector2(1700, 400)) - player.transform.position, false);
                Assert.That(map.IsWalkable(player.transform.position, player.bodyRadius), Is.True);
                Assert.That(map.ToImage(player.transform.position).y, Is.EqualTo(400).Within(2f),
                    "A wall must preserve movement along its tangent.");
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PaintedBoundarySlidingCannotCrossADiagonalRuntimeObstacle(bool dash)
        {
            WithFloor((layout, map, player) =>
            {
                layout.walkableBoundary = new[] {
                    layout.GroundToImage(new Vector3(0, 0, -20)), layout.GroundToImage(new Vector3(20, 0, -20)),
                    layout.GroundToImage(new Vector3(20, 0, 20)), layout.GroundToImage(new Vector3(0, 0, 20))
                };
                player.transform.position = map.transform.position + new Vector3(2, 0, 5);
                var gate = new GameObject("DiagonalRuntimeGate");
                gate.transform.position = map.transform.position + Vector3.up;
                gate.transform.rotation = Quaternion.LookRotation(new Vector3(1, 0, 1));
                var solid = gate.AddComponent<BoxCollider>(); solid.size = new Vector3(.2f, 3f, 60f);
                gate.AddComponent<IsoObstacle>();
                try
                {
                    Physics.SyncTransforms();
                    Move(player, new Vector3(-4, 0, -10), dash);
                    Vector3 local = player.transform.position - map.transform.position;
                    Assert.That(local.x, Is.GreaterThanOrEqualTo(.375f), "Stay inside the painted floor's radius limit.");
                    Assert.That(local.z - local.x, Is.GreaterThan(.66f),
                        "Resolving the painted edge must not push the actor across the diagonal runtime gate.");
                    Vector3 center = player.transform.position + Vector3.up * .5f;
                    Assert.That(Vector3.Distance(center, solid.ClosestPoint(center)), Is.GreaterThanOrEqualTo(.375f));
                }
                finally { Object.DestroyImmediate(gate); }
            });
        }

        [TestCase(0f)]
        [TestCase(.1f)]
        [TestCase(-3f)]
        public void InitialBoxOverlapRecoversThroughTheNearestExit(float inputX)
        {
            WithFloor((layout, map, player) =>
            {
                var body = EnsureBodyCollider(player);
                player.transform.position = map.transform.position + Vector3.right * .9f;
                var gate = new GameObject("GateEnabledAroundPlayer");
                gate.transform.position = map.transform.position + Vector3.up;
                var solid = gate.AddComponent<BoxCollider>(); solid.size = new Vector3(2, 3, 6);
                gate.AddComponent<IsoObstacle>();
                try
                {
                    Physics.SyncTransforms();
                    Assert.That(Physics.ComputePenetration(body, player.transform.position, Quaternion.identity,
                        solid, gate.transform.position, gate.transform.rotation, out _, out _), Is.True,
                        "The player must initially overlap the newly enabled gate.");
                    Move(player, Vector3.right * inputX, false);
                    Assert.That(player.transform.position.x - map.transform.position.x, Is.GreaterThanOrEqualTo(1.375f),
                        "Recovery must escape the nearest face, even with zero input, without crossing the box.");
                    Assert.That(Physics.ComputePenetration(body, player.transform.position, Quaternion.identity,
                        solid, gate.transform.position, gate.transform.rotation, out _, out _), Is.False);
                }
                finally { Object.DestroyImmediate(gate); }
            });
        }

        [Test]
        public void EscapingAnInitialOverlapStillStopsAtANewSolid()
        {
            WithFloor((layout, map, player) =>
            {
                var body = EnsureBodyCollider(player);
                player.transform.position = map.transform.position + Vector3.right * .9f;
                var gate = new GameObject("InitialOverlapGate");
                gate.transform.position = map.transform.position + Vector3.up;
                gate.AddComponent<BoxCollider>().size = new Vector3(2, 3, 6);
                gate.AddComponent<IsoObstacle>();
                var wall = new GameObject("SeparateRecoveryPathWall");
                wall.transform.position = map.transform.position + new Vector3(1.8f, 1, 0);
                var solid = wall.AddComponent<BoxCollider>(); solid.size = new Vector3(.2f, 3, 6);
                wall.AddComponent<IsoObstacle>();
                try
                {
                    Physics.SyncTransforms();
                    Move(player, Vector3.zero, false);
                    float x = player.transform.position.x - map.transform.position.x;
                    Assert.That(x, Is.GreaterThan(1.29f), "Recovery should reduce the original overlap.");
                    Assert.That(x, Is.LessThan(1.33f), "A separate solid must still stop the recovery sweep.");
                    Assert.That(Physics.ComputePenetration(body, player.transform.position, Quaternion.identity,
                        solid, wall.transform.position, wall.transform.rotation, out _, out _), Is.False);
                }
                finally { Object.DestroyImmediate(gate); Object.DestroyImmediate(wall); }
            });
        }

        [Test]
        public void SweptRadiusStopsAtTheRoundedCornerOfCover()
        {
            WithFloor((layout, map, player) =>
            {
                layout.cover = new[] { new PaintedChamberDefinition.Cover { footprint = new[] {
                    layout.GroundToImage(new Vector3(2, 0, 2)), layout.GroundToImage(new Vector3(4, 0, 2)),
                    layout.GroundToImage(new Vector3(4, 0, 4)), layout.GroundToImage(new Vector3(2, 0, 4))
                } } };
                Move(player, new Vector3(6, 0, 6), true);
                Vector3 local = player.transform.position - map.transform.position;
                Assert.That(local.x, Is.LessThan(1.74f));
                Assert.That(local.z, Is.LessThan(1.74f));
                Assert.That(local.x, Is.GreaterThan(1.7f), "The actor should reach the radius contact at the corner.");
            });
        }

        [Test]
        public void ThroneForegroundGateOccludesItsBannerWithoutCoveringTheDoorOpening()
        {
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_07");
            PaintedChamberDefinition.Cover foreground = null;
            foreach (var cover in layout.cover)
                if (PaintedPolygon.Contains(cover.silhouette, new Vector2(510, 700))) foreground = cover;
            Assert.That(foreground, Is.Not.Null, "The foreground banner must hide characters walking behind it.");
            Assert.That(PaintedPolygon.Contains(foreground.silhouette, new Vector2(327, 700)), Is.False,
                "The opening through the arch must remain visible.");
            Assert.That(PaintedPolygon.Contains(foreground.silhouette, layout.entrance), Is.False,
                "The arrival must be visible on the floor beyond the foreground jamb.");
            float front = 0f;
            foreach (var point in foreground.footprint) front = Mathf.Max(front, point.y);
            Assert.That(front, Is.GreaterThan(950), "Depth ordering belongs at the foreground base, not the rear wall column.");
            Assert.That(layout.IsWalkable(layout.ImageToGround(layout.entrance), .55f), Is.True);
            Assert.That(layout.IsWalkable(layout.ImageToGround(layout.returnExit), .55f), Is.True);
        }

        [Test]
        public void ThroneArrivalIsVisibleOnTheCarpetAndNeverOnTheForegroundArchitecture()
        {
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_07");
            Assert.That(layout.entrance.x, Is.InRange(750f, 900f), "Enter on the open lower carpet beyond the foreground arch.");
            Assert.That(layout.entrance.y, Is.InRange(560f, 660f));
            Assert.That(layout.IsWalkable(layout.ImageToGround(layout.entrance), .8f), Is.True);
            foreach (var obstacle in layout.cover)
            {
                if (obstacle.silhouette == null) continue;
                foreach (var offset in new[] { Vector2.zero, new Vector2(0, -40), new Vector2(-16, -20), new Vector2(16, -20) })
                    Assert.That(PaintedPolygon.Contains(obstacle.silhouette, layout.entrance + offset), Is.False,
                        "Arrival body is hidden by " + obstacle.name);
            }
        }

        // These are architectural stone bases/wall tops in the supplied painting, not arbitrary
        // outside coordinates. The previous broad polygon allowed pursuit onto all of them.
        [TestCase(610f, 700f)]
        [TestCase(650f, 720f)]
        [TestCase(1010f, 810f)]
        [TestCase(1100f, 880f)]
        public void ThroneStoneArchitectureRejectsActorFeet(float x, float y)
        {
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_07");
            Assert.That(layout.IsWalkable(layout.ImageToGround(new Vector2(x, y)), .45f), Is.False,
                "Actor feet must not stand on painted architecture at " + new Vector2(x, y));
        }

        [Test]
        public void EveryPaintedPassageBlocksWhileLockedAndOpensItsThresholdOnUnlock()
        {
            UnderworldJourney.Reset();
            for (int i = 0; i < UnderworldJourney.ChamberCount; i++)
            {
                var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + i.ToString("D2"));
                var map = PaintedChamberMap.Build(null, layout, Vector3.right * 40000);
                try
                {
                    foreach (var door in new[] { map.exitDoor, map.returnDoor })
                    {
                        door.gameObject.SetActive(true);
                        door.SetUnlocked(false);
                        Physics.SyncTransforms();
                        Vector3 origin = door.transform.position - door.transform.forward * 4f + Vector3.up * .5f;
                        Assert.That(HitsDoor(origin, door), Is.True, layout.title + " " + door.journeyRole + " has no solid locked threshold.");
                        door.SetUnlocked(true);
                        Physics.SyncTransforms();
                        Assert.That(HitsDoor(origin, door), Is.False, "Unlocking must allow the player through the threshold.");
                    }
                }
                finally { Object.DestroyImmediate(map.gameObject); }
            }
        }

        [Test]
        public void GeneratedChambersContainNoHealingWellObjects()
        {
            UnderworldJourney.Reset();
            var root = new GameObject("NoHealingWellRoute");
            try
            {
                for (int i = 0; i < UnderworldJourney.ChamberCount; i++)
                {
                    var room = ProceduralChamberGenerator.GenerateChamber(root.transform, i, 8, Vector3.right * (45000 + i * 100));
                    Assert.That(room.roomRoot.GetComponentInChildren<AmbrosiaHealingWell>(true), Is.Null, "Healing well remains in chamber " + i);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static bool HitsDoor(Vector3 origin, ChamberExitDoor door)
        {
            foreach (var hit in Physics.SphereCastAll(origin, .38f, door.transform.forward, 8f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponent<IsoObstacle>() != null && hit.collider.GetComponentInParent<ChamberExitDoor>() == door)
                    return true;
            return false;
        }

        private static void Move(OrpheusPlayerController player, Vector3 delta, bool dash)
            => typeof(OrpheusPlayerController).GetMethod("MoveSweepXZ", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(player, new object[] { delta, dash });

        private static SphereCollider EnsureBodyCollider(OrpheusPlayerController player)
        {
            var body = player.GetComponent<SphereCollider>();
            if (body == null) body = player.gameObject.AddComponent<SphereCollider>();
            body.isTrigger = true; body.radius = player.bodyRadius; body.center = Vector3.up * .65f;
            return body;
        }

        private static void WithFloor(System.Action<PaintedChamberDefinition, PaintedChamberMap, OrpheusPlayerController> action)
        {
            var layout = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
            layout.walkableBoundary = new[] { Vector2.zero, new Vector2(1536, 0), new Vector2(1536, 1024), new Vector2(0, 1024) };
            layout.entrance = new Vector2(768, 512);
            var mapObject = new GameObject("NavigationOnlyFloor");
            mapObject.transform.position = Vector3.right * 35000;
            var map = mapObject.AddComponent<PaintedChamberMap>(); map.definition = layout; map.MakeActive();
            var playerObject = new GameObject("BoundaryPlayer"); playerObject.transform.position = mapObject.transform.position;
            var player = playerObject.AddComponent<OrpheusPlayerController>();
            try { action(layout, map, player); }
            finally { Object.DestroyImmediate(playerObject); Object.DestroyImmediate(mapObject); Object.DestroyImmediate(layout); }
        }
    }
}
