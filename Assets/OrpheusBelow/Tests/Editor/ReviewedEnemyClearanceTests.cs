using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class ReviewedEnemyClearanceTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void CachedEnemyClearanceMatchesAuthoredFloorAtIndependentAndEdgeNearWitnesses(int chamber)
        {
            var trace = PaintedChamberNavigationTrace.Load(chamber);
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + chamber.ToString("D2"));
            var source = new List<Vector2> { trace.entrance, trace.returnSpawn, trace.returnExit, trace.exit, trace.encounterCenter };
            source.AddRange(trace.enemySpawns);
            foreach (var probe in trace.walkableProbes) source.Add(probe.Point);
            foreach (var probe in trace.blockedProbes) source.Add(probe.Point);
            foreach (var pair in trace.occlusionProbes) { source.Add(pair.behind); source.Add(pair.front); }
            var edges = new List<Vector3[]>();
            AddEdges(layout, layout.walkableBoundary, edges);
            foreach (var cover in layout.cover) AddEdges(layout, cover.footprint, edges);
            var getGrid = typeof(PaintedEnemyNavigation).GetMethod("GetGrid", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(getGrid, Is.Not.Null);
            foreach (float radius in new[] { 0f, .4f, .42f, .65f })
            {
                object grid = getGrid.Invoke(null, new object[] { layout, radius });
                var query = grid.GetType().GetMethod("IsWalkable", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(query, Is.Not.Null);
                long originalTicks = 0, cachedTicks = 0; int cases = 0;
                foreach (Vector2 pixel in source)
                    Compare(grid, query, layout, layout.ImageToGround(pixel), radius, ref originalTicks, ref cachedTicks, ref cases);
                // Bounded spread across floor and cover edges, including both sides of clearance.
                // A .01m offset avoids interpreting single-ULP threshold rounding as a changed floor.
                for (int i = 0; i < Mathf.Min(12, edges.Count); i++)
                {
                    Vector3[] edge = edges[i * edges.Count / Mathf.Min(12, edges.Count)];
                    Vector3 delta = edge[1] - edge[0], middle = (edge[0] + edge[1]) * .5f;
                    Vector3 normal = new Vector3(-delta.z, 0, delta.x).normalized;
                    foreach (float offset in new[] { 0f, radius + .01f, -(radius + .01f), Mathf.Max(0, radius - .01f), -Mathf.Max(0, radius - .01f) })
                        Compare(grid, query, layout, middle + normal * offset, radius, ref originalTicks, ref cachedTicks, ref cases);
                }
                TestContext.WriteLine("Chamber " + chamber + " radius " + radius + " equivalence cases=" + cases +
                    ", original ms=" + originalTicks * 1000d / Stopwatch.Frequency + ", cached ms=" + cachedTicks * 1000d / Stopwatch.Frequency);
            }
        }

        private static void AddEdges(PaintedChamberDefinition layout, Vector2[] polygon, List<Vector3[]> edges)
        {
            for (int i = 0; i < polygon.Length; i++)
                edges.Add(new[] { layout.ImageToGround(polygon[i]), layout.ImageToGround(polygon[(i + 1) % polygon.Length]) });
        }

        private static void Compare(object grid, MethodInfo query, PaintedChamberDefinition layout, Vector3 point, float radius,
            ref long originalTicks, ref long cachedTicks, ref int cases)
        {
            long before = Stopwatch.GetTimestamp(); bool expected = layout.IsWalkable(point, radius);
            originalTicks += Stopwatch.GetTimestamp() - before;
            before = Stopwatch.GetTimestamp(); bool actual = (bool)query.Invoke(grid, new object[] { point });
            cachedTicks += Stopwatch.GetTimestamp() - before; cases++;
            Assert.That(actual, Is.EqualTo(expected), "Clearance disagreement at image " + layout.GroundToImage(point) + ", radius=" + radius);
        }
    }
}
