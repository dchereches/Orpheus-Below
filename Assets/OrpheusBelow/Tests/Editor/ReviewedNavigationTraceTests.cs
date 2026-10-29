using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class ReviewedNavigationTraceTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void SavedNavigationMatchesTheIndependentlyReviewedPainting(int index)
        {
            var trace = PaintedChamberNavigationTrace.Load(index);
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + index.ToString("D2"));
            Assert.That(layout.walkableBoundary, Is.EqualTo(trace.walkableBoundary), "Rebake the saved floor; runtime cannot use stale geometry.");
            Assert.That(layout.cover.Length, Is.EqualTo(trace.cover.Length));
            for (int i = 0; i < trace.cover.Length; i++)
            {
                Assert.That(layout.cover[i].name, Is.EqualTo(trace.cover[i].name));
                Assert.That(layout.cover[i].footprint, Is.EqualTo(trace.cover[i].footprint));
                Assert.That(layout.cover[i].silhouette, Is.EqualTo(trace.cover[i].silhouette));
            }
            Assert.That(layout.entrance, Is.EqualTo(trace.entrance));
            Assert.That(layout.exit, Is.EqualTo(trace.exit));
            Assert.That(layout.returnExit, Is.EqualTo(trace.returnExit));
            Assert.That(layout.returnSpawn, Is.EqualTo(trace.returnSpawn));
            Assert.That(layout.enemySpawns, Is.EqualTo(trace.enemySpawns));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void SourceSelectedFloorAndBlockedPointsRemainDistinctWithPlayerClearance(int index)
        {
            var trace = PaintedChamberNavigationTrace.Load(index);
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + index.ToString("D2"));
            Assert.That(trace.walkableProbes.Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(trace.blockedProbes.Length, Is.GreaterThanOrEqualTo(12));
            foreach (var p in trace.walkableProbes)
                Assert.That(layout.IsWalkable(layout.ImageToGround(p.Point), .45f), Is.True, index + "/" + p.name);
            foreach (var p in trace.blockedProbes)
                Assert.That(layout.IsWalkable(layout.ImageToGround(p.Point)), Is.False, index + "/" + p.name);
            foreach (var p in new[] { trace.entrance, trace.returnSpawn, trace.encounterCenter }.Concat(trace.enemySpawns))
                Assert.That(layout.IsWalkable(layout.ImageToGround(p), .45f), Is.True, "Actual spawn/fighting anchor " + p);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void OccludingFoliageAndArchitectureHaveWalkableFrontAndBehindWitnesses(int index)
        {
            var trace = PaintedChamberNavigationTrace.Load(index);
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + index.ToString("D2"));
            Assert.That(trace.occlusionProbes.Length, Is.GreaterThanOrEqualTo(3));
            foreach (var pair in trace.occlusionProbes)
            {
                var cover = layout.cover.Single(c => c.name == pair.coverName);
                Assert.That(layout.IsWalkable(layout.ImageToGround(pair.behind), .45f), Is.True, pair.name);
                Assert.That(layout.IsWalkable(layout.ImageToGround(pair.front), .45f), Is.True, pair.name);
                float depth = cover.footprint.Max(p => p.y);
                Assert.That(pair.behind.y, Is.LessThan(depth), "Behind witness must precede the base's front edge: " + pair.name);
                Assert.That(pair.front.y, Is.GreaterThan(depth), "Front witness must follow the base's front edge: " + pair.name);
            }
        }
    }
}
