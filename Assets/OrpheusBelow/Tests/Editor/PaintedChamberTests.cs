using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace OrpheusBelow.Tests
{
    public class PaintedChamberTests
    {
        // Removing swept collision would let a fast orb jump across this thin cover in one step.
        [Test]
        public void FastEnemyProjectileStopsAtCoverInsteadOfTunnellingThroughIt()
        {
            var wall = new GameObject("ThinCover");
            wall.transform.position = new Vector3(10005f, 1f, 0f);
            wall.AddComponent<BoxCollider>().size = new Vector3(.2f, 2f, 3f);
            wall.AddComponent<IsoObstacle>();
            StygianOrbProjectile.Spawn(new Vector3(10000f, .6f, 0f), Vector3.right, 120f, 10f);
            var orb = Object.FindFirstObjectByType<StygianOrbProjectile>();
            try
            {
                var advance = typeof(StygianOrbProjectile).GetMethod("Advance", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(advance, Is.Not.Null, "Projectiles need a swept advance step to catch thin cover.");
                Physics.SyncTransforms();
                advance.Invoke(orb, new object[] { .1f });
                Assert.That(orb == null || orb.transform.position.x < 10005f, Is.True);
            }
            finally { Object.DestroyImmediate(wall); if (orb != null) Object.DestroyImmediate(orb.gameObject); }
        }

        // Missing authored layouts must never silently fall back to the previous rectangular floors.
        [Test]
        public void EveryChamberHasAnAuthoredPaintedLayout()
        {
            for (int i = 0; i < 5; i++)
                Assert.That(Resources.Load<ScriptableObject>("ChamberLayouts/Chamber_" + i.ToString("D2")),
                    Is.Not.Null, "Missing painted chamber " + i);
        }

        [Test]
        public void AuthoredScenesReopenWithLiveCollisionAndExitComponents()
        {
            for (int i = 0; i < 5; i++)
            {
                var scene = EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(i), OpenSceneMode.Additive);
                try
                {
                    PaintedChamberMap map = null;
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                            Assert.That(component, Is.Not.Null, "Missing serialized script in scene " + i);
                        if (root.TryGetComponent(out PaintedChamberMap found)) map = found;
                    }
                    Assert.That(map, Is.Not.Null);
                    Assert.That(map.exitDoor, Is.Not.Null, "The exit must survive scene serialization.");
                    foreach (var collider in map.GetComponentsInChildren<Collider>(true))
                        if (!collider.isTrigger) Assert.That(collider.GetComponent<IsoObstacle>(), Is.Not.Null,
                            "Solid collision classification missing on " + collider.name);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        [Test]
        public void FastFriendlyProjectileAlsoStopsAtSolidCover()
        {
            var wall = new GameObject("FriendlyProjectileCover");
            wall.transform.position = new Vector3(30005, 1, 0);
            wall.AddComponent<BoxCollider>().size = new Vector3(.2f, 3f, 4f);
            wall.AddComponent<IsoObstacle>();
            var noteObject = new GameObject("SweptNote");
            noteObject.transform.position = new Vector3(30000, .6f, 0);
            var note = noteObject.AddComponent<OrphicNoteProjectile>();
            note.Init(Vector3.right, 120f, 20f, .5f);
            try
            {
                Physics.SyncTransforms();
                note.Advance(.1f);
                Assert.That(note == null || note.transform.position.x < 30005f, Is.True);
            }
            finally { Object.DestroyImmediate(wall); if (noteObject != null) Object.DestroyImmediate(noteObject); }
        }

        [Test]
        public void AuthoredFloorExcludesPaintedCoverWhileLeavingTheAdjacentFightingSpaceOpen()
        {
            var d = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_01");
            Assert.That(d.IsWalkable(d.ImageToGround(new Vector2(806, 803))), Is.False, "Broken column's base is solid.");
            Assert.That(d.IsWalkable(d.ImageToGround(new Vector2(880, 803)), .4f), Is.True, "Floor immediately beside it remains usable.");
            Assert.That(d.IsWalkable(d.ImageToGround(new Vector2(50, 150))), Is.False, "The distant vista is inaccessible.");
        }

        [Test]
        public void OcclusionSpriteOccupiesOnlyItsAuthoredSilhouette()
        {
            var d = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_01");
            var map = PaintedChamberMap.Build(null, d, Vector3.zero);
            try
            {
                var sprite = map.coverRoot.Find("CenterBrokenColumn/PaintedOcclusion").GetComponent<SpriteRenderer>().sprite;
                // A 102-pixel-wide outline on a 1536-pixel-wide, 72-unit painting is 4.78125 units wide.
                Assert.That(sprite.bounds.size.x, Is.EqualTo(4.78125f).Within(.01f));
                Assert.That(sprite.bounds.size.y, Is.EqualTo(4.696875f).Within(.05f));
                Vector3 forward = RenderPipeline2DSetup.IsoCameraRotation * Vector3.forward;
                float depth = Vector3.Dot(map.coverRoot.Find("CenterBrokenColumn/PaintedOcclusion").position, forward);
                Assert.That(depth, Is.EqualTo(Vector3.Dot(d.ImageToGround(new Vector2(806, 836.2f)), forward)).Within(.01f),
                    "The cutout sits at the pillar's front edge so HDRP distance sorting also hides actors behind it.");
            }
            finally { Object.DestroyImmediate(map.gameObject); }
        }

        [Test]
        public void CameraProjectionMatchesSourceImageAndKeepsEveryViewportCornerInsideIt()
        {
            var d = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_01");
            Assert.That(d.ImageToGround(new Vector2(768, 512)).magnitude, Is.LessThan(.001f));
            Vector3 right = d.ImageToGround(new Vector2(1536, 512));
            Assert.That(right.x, Is.EqualTo(25.4558f).Within(.001f));
            Assert.That(right.z, Is.EqualTo(-25.4558f).Within(.001f));
            var map = PaintedChamberMap.Build(null, d, Vector3.zero);
            var cameraObject = new GameObject("CropCheck");
            var camera = cameraObject.AddComponent<Camera>();
            try
            {
                camera.orthographic = true; camera.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
                foreach (float aspect in new[] { 4f / 3f, 16f / 9f, 21f / 9f })
                {
                    camera.aspect = aspect;
                    camera.orthographicSize = map.SafeZoom(8.5f, aspect);
                    foreach (Vector3 desired in new[] { Vector3.one * 200f, Vector3.one * -200f, map.ToWorld(d.entrance), map.ToWorld(d.exit) })
                    {
                        camera.transform.position = map.ClampCameraFocus(desired, camera.orthographicSize, aspect) - camera.transform.forward * 36f;
                        var plane = new Plane(camera.transform.forward, camera.transform.forward * 48f);
                        foreach (Vector2 corner in new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one })
                        {
                            Ray ray = camera.ViewportPointToRay(corner);
                            Assert.That(plane.Raycast(ray, out float enter), Is.True);
                            Vector3 p = ray.GetPoint(enter);
                            Assert.That(Mathf.Abs(Vector3.Dot(p, camera.transform.right)), Is.LessThan(36f));
                            Assert.That(Mathf.Abs(Vector3.Dot(p, camera.transform.up)), Is.LessThan(24f));
                        }
                    }
                }
            }
            finally { Object.DestroyImmediate(map.gameObject); Object.DestroyImmediate(cameraObject); }
        }

        [Test]
        public void EveryEntranceAndExitIsReachableWithoutCrossingCover()
        {
            for (int index = 0; index < 5; index++)
            {
                var d = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + index.ToString("D2"));
                Assert.That(d.IsWalkable(d.ImageToGround(d.entrance), .55f), Is.True, "Entrance " + index);
                Assert.That(d.IsWalkable(d.ImageToGround(d.exit), .55f), Is.True, "Exit " + index);
                const int width = 96, height = 64;
                var open = new bool[width * height];
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    open[y * width + x] = d.IsWalkable(d.ImageToGround(new Vector2(x * 16 + 8, y * 16 + 8)), .5f);
                int start = Mathf.FloorToInt(d.entrance.y / 16) * width + Mathf.FloorToInt(d.entrance.x / 16);
                int end = Mathf.FloorToInt(d.exit.y / 16) * width + Mathf.FloorToInt(d.exit.x / 16);
                var queue = new Queue<int>(); var visited = new HashSet<int>();
                queue.Enqueue(start); visited.Add(start);
                while (queue.Count > 0)
                {
                    int at = queue.Dequeue();
                    foreach (var step in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) })
                    {
                        int x = at % width + step.x, y = at / width + step.y;
                        if (x < 0 || y < 0 || x >= width || y >= height) continue;
                        int next = y * width + x;
                        if (open[next] && visited.Add(next)) queue.Enqueue(next);
                    }
                }
                Assert.That(visited.Contains(end), Is.True, "No clear route through chamber " + index);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RunningAndDashingCannotCrossSolidCover(bool dash)
        {
            var wall = new GameObject("MovementCover");
            wall.transform.position = new Vector3(20005, 1, 0);
            wall.AddComponent<BoxCollider>().size = new Vector3(.2f, 3f, 6f);
            wall.AddComponent<IsoObstacle>();
            var playerObject = new GameObject("CoverPlayer");
            playerObject.transform.position = new Vector3(20000, 0, 0);
            var player = playerObject.AddComponent<OrpheusPlayerController>();
            try
            {
                Physics.SyncTransforms();
                typeof(OrpheusPlayerController).GetMethod("MoveSweepXZ", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(player, new object[] { Vector3.right * 12f, dash });
                Assert.That(player.transform.position.x, Is.LessThan(20004.8f));
            }
            finally { Object.DestroyImmediate(playerObject); Object.DestroyImmediate(wall); }
        }
    }
}
