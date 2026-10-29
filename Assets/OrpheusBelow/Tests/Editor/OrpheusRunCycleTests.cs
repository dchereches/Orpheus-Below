using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class OrpheusRunCycleTests
    {
        [TestCase(EurydiceFacing.South, 154f, 263f, 0f, -1f)]
        [TestCase(EurydiceFacing.SouthEast, 221f, 546f, 1f, -.45f)]
        [TestCase(EurydiceFacing.East, 234f, 848f, 1f, 0f)]
        [TestCase(EurydiceFacing.NorthEast, 90f, 1150f, 1f, .45f)]
        [TestCase(EurydiceFacing.North, 140f, 1470f, 0f, 1f)]
        public void RunningAlternatesBothPaintedBootsAcrossHalfStride(EurydiceFacing facing,
            float pixelX, float pixelYFromTop, float travelX, float travelY)
        {
            var library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            OrpheusAnimationClip run = library.FindClip(OrpheusAnimationState.Run);
            Sprite contact = library.Frame(OrpheusAnimationState.Run, facing, 0);
            Sprite opposite = library.Frame(OrpheusAnimationState.Run, facing, run.framesPerDirection / 2);
            Vector2 texel = new Vector2((pixelX + .37f) / 1024f, (1536f - pixelYFromTop - .41f) / 1536f);
            List<Vector2> first = PaintedPoints(contact, texel);
            List<Vector2> half = PaintedPoints(opposite, texel);
            Assert.That(first.Count, Is.EqualTo(2), "Both anatomical legs must contain painted boot geometry; a single repeated leading leg cannot pass.");
            Assert.That(half.Count, Is.EqualTo(2));
            Vector2 travel = new Vector2(travelX, travelY).normalized;
            Assert.That(Vector2.Dot(half[0] - first[0], travel), Is.GreaterThan(.3f), "The far painted boot must advance during the opposite half-stride.");
            Assert.That(Vector2.Dot(half[1] - first[1], travel), Is.LessThan(-.3f), "The near painted boot must sweep back as the far boot advances.");
            Assert.That(Vector2.Distance(first[0], first[1]), Is.GreaterThan(.4f));
            Assert.That(Vector2.Distance(half[0], half[1]), Is.GreaterThan(.4f));
        }

        [Test]
        public void RunningPlantKeepsTheContactBootInPlaceAsTheBodyAdvances()
        {
            var library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            Vector2 texel = new Vector2(234.37f / 1024f, (1536f - 848.41f) / 1536f);
            List<Vector2> contact = PaintedPoints(library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, 0), texel);
            List<Vector2> afterTravel = PaintedPoints(library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, 1), texel);
            Assert.That(contact.Count, Is.EqualTo(2));
            Assert.That(afterTravel.Count, Is.EqualTo(2));
            Vector2 worldFoot = afterTravel[1] + Vector2.right * .4f;
            Assert.That(Vector2.Distance(worldFoot, contact[1]), Is.LessThan(.075f),
                "During support, the boot must counter the body's real ground displacement.");
        }

        [Test]
        public void PassingPoseLiftsTheOppositeBootOnEachHalfStride()
        {
            var library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            OrpheusAnimationClip run = library.FindClip(OrpheusAnimationState.Run);
            Vector2 texel = new Vector2(234.37f / 1024f, (1536f - 848.41f) / 1536f);
            List<Vector2> firstPass = PaintedPoints(library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East,
                run.framesPerDirection / 4), texel);
            List<Vector2> secondPass = PaintedPoints(library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East,
                run.framesPerDirection * 3 / 4), texel);
            Assert.That(firstPass.Count, Is.EqualTo(2));
            Assert.That(secondPass.Count, Is.EqualTo(2));
            Assert.That(firstPass[0].y - firstPass[1].y, Is.GreaterThan(.3f));
            Assert.That(secondPass[1].y - secondPass[0].y, Is.GreaterThan(.3f));
        }

        [Test]
        public void RunningKneeMaterialHasNoOpenTJunctions()
        {
            var library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            OrpheusAnimationClip run = library.FindClip(OrpheusAnimationState.Run);
            for (int frame = 0; frame < run.framesPerDirection; frame++)
            {
                Sprite sprite = library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, frame);
                Vector2[] uv = sprite.uv, positions = sprite.vertices;
                ushort[] triangles = sprite.triangles;
                var rows = new Dictionary<int, Dictionary<int, List<Vector2>>>();
                for (int i = 0; i < uv.Length; i++)
                {
                    int x = Mathf.RoundToInt(uv[i].x * sprite.texture.width);
                    int y = Mathf.RoundToInt(uv[i].y * sprite.texture.height);
                    if (!rows.TryGetValue(y, out var row)) rows[y] = row = new Dictionary<int, List<Vector2>>();
                    if (!row.TryGetValue(x, out var points)) row[x] = points = new List<Vector2>();
                    points.Add(positions[i]);
                }
                for (int t = 0; t < triangles.Length; t += 3) for (int edge = 0; edge < 3; edge++)
                {
                    int a = triangles[t + edge], b = triangles[t + (edge + 1) % 3];
                    int ax = Mathf.RoundToInt(uv[a].x * sprite.texture.width), bx = Mathf.RoundToInt(uv[b].x * sprite.texture.width);
                    int ay = Mathf.RoundToInt(uv[a].y * sprite.texture.height), by = Mathf.RoundToInt(uv[b].y * sprite.texture.height);
                    int fromTop = sprite.texture.height - ay;
                    if (ay != by || fromTop < 775 || fromTop > 815 || ax == bx) continue;
                    foreach (var point in rows[ay])
                    {
                        if (point.Key <= Mathf.Min(ax, bx) || point.Key >= Mathf.Max(ax, bx) || point.Key < 180 || point.Key > 235) continue;
                        Vector2 onEdge = Vector2.Lerp(positions[a], positions[b], (point.Key - ax) / (float)(bx - ax));
                        Assert.That(point.Value.Min(p => Vector2.Distance(p, onEdge)), Is.LessThan(.00001f),
                            sprite.name + ": adjacent triangles must share the same rendered material edge, including intermediate UV vertices.");
                    }
                }
            }
        }

        [Test]
        public void RunningKeepsOpaqueWaistAndCapePaintJoinedToTheTorso()
        {
            var library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            Sprite source = library.sourceFrames.Single(s => s.name == "RunCycle_02_00");
            OrpheusAnimationClip run = library.FindClip(OrpheusAnimationState.Run);
            Vector2 marker = new Vector2(110.37f, 710.41f);
            Vector2 markerUv = new Vector2(marker.x / source.texture.width, 1f - marker.y / source.texture.height);
            for (int frame = 0; frame < run.framesPerDirection; frame++)
            {
                Sprite sprite = library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, frame);
                Vector2 shift = PaintedPoints(sprite, markerUv).Single() - SourcePosition(source, marker);
                foreach (float y in new[] { 738.41f, 746.41f }) foreach (float x in new[] { 125.37f, 135.37f, 145.37f })
                    Assert.That(OpaqueAt(sprite, SourcePosition(source, new Vector2(x, y)) + shift), Is.True,
                        sprite.name + ": the original solid waist/cape junction must not become a transparent cutout.");
            }
        }

        private static Vector2 SourcePosition(Sprite sprite, Vector2 topLeftPixel)
            => (new Vector2(topLeftPixel.x, sprite.texture.height - topLeftPixel.y) - sprite.rect.position - sprite.pivot) / sprite.pixelsPerUnit;

        private static bool OpaqueAt(Sprite sprite, Vector2 point)
        {
            Vector2[] positions = sprite.vertices, uv = sprite.uv;
            ushort[] indices = sprite.triangles;
            for (int t = 0; t < indices.Length; t += 3)
            {
                int a = indices[t], b = indices[t + 1], c = indices[t + 2];
                float determinant = Cross(positions[b] - positions[a], positions[c] - positions[a]);
                if (Mathf.Abs(determinant) < 1e-12f) continue;
                float u = Cross(point - positions[a], positions[c] - positions[a]) / determinant;
                float v = Cross(positions[b] - positions[a], point - positions[a]) / determinant;
                if (u < 0f || v < 0f || u + v > 1f) continue;
                Vector2 sample = uv[a] + (uv[b] - uv[a]) * u + (uv[c] - uv[a]) * v;
                if (sprite.texture.GetPixelBilinear(sample.x, sample.y).a > .8f) return true;
            }
            return false;
        }

        // Project the same real boot texel through the rendered triangles. Separate projections
        // identify the far and near painted limbs in draw order, rather than trusting pose labels.
        private static List<Vector2> PaintedPoints(Sprite sprite, Vector2 sample)
        {
            var result = new List<Vector2>();
            Vector2[] uv = sprite.uv, vertices = sprite.vertices;
            ushort[] indices = sprite.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                float determinant = Cross(uv[b] - uv[a], uv[c] - uv[a]);
                if (Mathf.Abs(determinant) < 1e-12f) continue;
                float u = Cross(sample - uv[a], uv[c] - uv[a]) / determinant;
                float v = Cross(uv[b] - uv[a], sample - uv[a]) / determinant;
                if (u < 0f || v < 0f || u + v > 1f) continue;
                Vector2 point = vertices[a] + (vertices[b] - vertices[a]) * u + (vertices[c] - vertices[a]) * v;
                if (result.Count == 0 || Vector2.Distance(result[result.Count - 1], point) > .001f) result.Add(point);
            }
            return result;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
