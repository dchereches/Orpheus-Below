using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class PaintedNavigationPhysicsTests
    {
        private PaintedChamberDefinition _definition;
        private PaintedChamberMap _map;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
            _definition.index = 0;
            _definition.painting = Resources.Load<Texture2D>("PaintedChambers/Chamber_00");
            _definition.walkableBoundary = Polygon(400, 350, 900, 350, 900, 700, 400, 700);
            _definition.entrance = new Vector2(500, 600);
            _definition.returnSpawn = new Vector2(520, 600);
            _definition.exit = new Vector2(850, 600);
            _definition.returnExit = new Vector2(500, 600);
            _definition.enemySpawns = new[] { new Vector2(500, 600) };
            _definition.cover = new[] { new PaintedChamberDefinition.Cover {
                name = "ConcaveStoneBase", height = 3,
                footprint = Polygon(600, 400, 750, 400, 750, 450, 650, 450, 650, 550, 600, 550),
                silhouette = System.Array.Empty<Vector2>() } };
            _map = PaintedChamberMap.Build(null, _definition, Vector3.zero);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (_map != null) Object.DestroyImmediate(_map.gameObject);
            if (_definition != null) Object.DestroyImmediate(_definition);
        }

        [Test]
        public void ConcaveFootprintDoesNotInventSolidGroundInsideItsOpenNotch()
        {
            Vector3 point = _map.ToWorld(new Vector2(680, 475));
            Assert.That(_map.IsWalkable(point, .38f), Is.True);
            var overlaps = Physics.OverlapSphere(point + Vector3.up * .45f, .38f, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(overlaps.Any(c => c.transform.IsChildOf(_map.coverRoot)), Is.False,
                "A convex hull fills a notch which the actual painted footprint leaves open.");
            Vector3 target = _map.ToWorld(new Vector2(720, 475));
            var hits = Physics.SphereCastAll(point + Vector3.up * .45f, .38f, (target - point).normalized,
                Vector3.Distance(point, target), ~0, QueryTriggerInteraction.Ignore);
            Assert.That(hits.Any(h => h.collider.transform.IsChildOf(_map.coverRoot)), Is.False);
        }

        [Test]
        public void ActualBoundaryColliderStartsOutsideTheTracedFloorEdge()
        {
            Vector3 point = _map.ToWorld(new Vector2(400, 600)) + UnderworldCamera2D.IsoRightXZ * .42f;
            Assert.That(_map.IsWalkable(point, .38f), Is.True);
            var overlaps = Physics.OverlapSphere(point + Vector3.up * .45f, .38f, ~0, QueryTriggerInteraction.Ignore);
            Assert.That(overlaps.Any(c => c.transform.IsChildOf(_map.transform)), Is.False,
                "The physical wall must not consume another half wall thickness inside legal floor.");
        }

        [Test]
        public void SolidPartOfConcaveBaseStillStopsAFastPhysicalSweep()
        {
            Vector3 from = _map.ToWorld(new Vector2(700, 490)), to = _map.ToWorld(new Vector2(620, 490));
            Assert.That(_map.IsWalkable(from, .38f), Is.True);
            Assert.That(_map.IsWalkable(to, .38f), Is.False);
            var hits = Physics.SphereCastAll(from + Vector3.up * .45f, .38f, (to - from).normalized,
                Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore);
            Assert.That(hits.Any(h => h.collider.transform.IsChildOf(_map.coverRoot)), Is.True,
                "Accurate notches must not remove the solid object's real side faces.");
        }

        private static Vector2[] Polygon(params float[] points)
            => Enumerable.Range(0, points.Length / 2).Select(i => new Vector2(points[i * 2], points[i * 2 + 1])).ToArray();
    }
}
