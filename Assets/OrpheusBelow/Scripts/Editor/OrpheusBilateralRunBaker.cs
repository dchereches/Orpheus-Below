using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace OrpheusBelow.EditorTools
{
    /// <summary>Authors a full stride from existing oil paint, rather than trusting generated pose labels.</summary>
    public static class OrpheusBilateralRunBaker
    {
        private const int Phases = 8;
        private const float Clearance = .46f;
        private const float RetainedHipDepthPixels = 22f;

        private sealed class Rig
        {
            // Source coordinates are in native RunCycle.png pixels, measured from its top-left.
            public Vector2 hip, knee, ankle;
            public Vector2[] leg, removedLeg;
            public Vector2 travel, lateral;
            public float legMargin = 8f, removedMargin = 12f;
        }

        private struct Patch
        {
            public Vector2 min, max;
            public bool leg;
        }

        private static readonly Rig[] Rigs =
        {
            new Rig { hip = new Vector2(160,151), knee = new Vector2(154,209), ankle = new Vector2(154,263),
                leg = Points(142,140, 179,143, 183,164, 176,191, 172,221, 164,252, 163,275, 156,296,
                    135,300, 130,284, 133,253, 137,223, 133,198, 136,170),
                removedLeg = Points(108,150, 147,146, 150,167, 139,195, 129,216, 108,217, 103,199, 111,177) },
            new Rig { hip = new Vector2(168,438), knee = new Vector2(195,488), ankle = new Vector2(221,546),
                leg = Points(148,420, 179,421, 191,441, 199,466, 215,492, 230,524, 236,543, 257,551,
                    262,574, 241,589, 216,587, 209,568, 202,546, 186,523, 171,504, 159,480, 148,460),
                removedLeg = Points(103,432, 155,430, 158,465, 148,489, 132,505, 134,526, 119,542,
                    100,536, 97,516, 101,497, 96,478) },
            new Rig { hip = new Vector2(174,749), knee = new Vector2(204,790), ankle = new Vector2(234,848),
                leg = Points(151,731, 184,736, 204,757, 219,781, 235,808, 247,831, 286,832, 294,850,
                    279,867, 251,880, 229,872, 220,853, 198,834, 177,814, 160,786, 146,758),
                removedLeg = Points(104,745, 155,746, 165,765, 150,785, 121,805, 96,821, 80,842,
                    85,860, 75,881, 48,881, 38,862, 40,836, 47,815, 68,793, 91,771) },
            new Rig { hip = new Vector2(147,1049), knee = new Vector2(123,1101), ankle = new Vector2(90,1150),
                removedMargin = 18f,
                leg = Points(133,1023, 164,1033, 166,1058, 156,1083, 146,1106, 129,1129, 114,1150,
                    114,1169, 101,1190, 81,1194, 67,1176, 65,1155, 78,1131, 93,1110, 105,1086, 117,1055),
                removedLeg = Points(169,1031, 198,1031, 211,1056, 220,1079, 241,1084, 240,1107,
                    223,1122, 201,1112, 185,1089, 172,1069) },
            new Rig { hip = new Vector2(143,1360), knee = new Vector2(142,1413), ankle = new Vector2(140,1470),
                legMargin = 14f,
                leg = Points(122,1341, 157,1338, 166,1361, 166,1392, 162,1420, 157,1452, 156,1480,
                    147,1507, 129,1513, 119,1498, 120,1475, 123,1448, 124,1422, 118,1395, 115,1367),
                removedLeg = Points(97,1340, 127,1340, 140,1364, 133,1388, 122,1416, 114,1447,
                    97,1450, 90,1431, 93,1410, 96,1387, 90,1364) }
        };

        public static Sprite[] Bake(Sprite[] source, OrpheusAnimationLibrary owner)
        {
            var frames = new Sprite[Rigs.Length * Phases];
            for (int row = 0; row < Rigs.Length; row++)
            {
                Sprite paint = source[row * 4];
                Rig rig = Rigs[row];
                float angle = row * Mathf.PI * .25f;
                // The 36-degree isometric camera projects ground depth into screen height.
                rig.travel = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle) * Mathf.Sin(36f * Mathf.Deg2Rad));
                rig.lateral = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle) * .2f) * .13f;
                List<Patch> patches = ExtractPatches(paint, rig);
                for (int phase = 0; phase < Phases; phase++)
                {
                    Sprite frame = BuildFrame(paint, patches, rig, phase / (float)Phases);
                    frame.name = "BilateralRun_" + row.ToString("D2") + "_" + phase.ToString("D2");
                    AssetDatabase.AddObjectToAsset(frame, owner);
                    frames[row * Phases + phase] = frame;
                }
            }
            return frames;
        }

        private static List<Patch> ExtractPatches(Sprite source, Rig rig)
        {
            // Curved skinning needs a conforming grid: merging neighboring rows into differently
            // sized strips creates T-junction cracks once their edges bend. Keep moving cells on
            // the slicer's shared two-pixel lattice; only the affine torso may retain long strips.
            var patches = new List<Patch>();
            Vector2[] vertices = source.vertices;
            for (int i = 0; i < vertices.Length; i += 4)
            {
                Vector2 bottom = vertices[i] * source.pixelsPerUnit + source.pivot + source.rect.position;
                Vector2 top = vertices[i + 3] * source.pixelsPerUnit + source.pivot + source.rect.position;
                bottom = new Vector2(Mathf.Round(bottom.x), Mathf.Round(bottom.y));
                top = new Vector2(Mathf.Round(top.x), Mathf.Round(top.y));
                bool keepingTorso = false;
                float start = bottom.x;
                for (float x = bottom.x; x < top.x; x += 2f)
                {
                    float end = Mathf.Min(top.x, x + 2f);
                    Vector2 centre = new Vector2((x + end) * .5f, source.texture.height - (bottom.y + top.y) * .5f);
                    bool leg = Within(centre, rig.leg, rig.legMargin);
                    if (leg) patches.Add(new Patch { min = new Vector2(x, bottom.y), max = new Vector2(end, top.y), leg = true });
                    // The original coat/pelvis root remains solid above the common pinned seam.
                    bool torso = centre.y <= rig.hip.y + RetainedHipDepthPixels ||
                        (!leg && !Within(centre, rig.removedLeg, rig.removedMargin));
                    if (torso == keepingTorso) continue;
                    if (keepingTorso)
                        patches.Add(new Patch { min = new Vector2(start, bottom.y), max = new Vector2(x, top.y) });
                    start = x; keepingTorso = torso;
                }
                if (keepingTorso) patches.Add(new Patch { min = new Vector2(start, bottom.y), max = top });
            }
            return patches;
        }

        private static Sprite BuildFrame(Sprite paint, List<Patch> patches, Rig rig, float phase)
        {
            var positions = new List<Vector3>();
            var uv = new List<Vector2>();
            var indices = new List<ushort>();
            Vector2 hip = Local(paint, rig.hip), knee = Local(paint, rig.knee), ankle = Local(paint, rig.ankle);
            float bob = .035f * Mathf.Sin(phase * Mathf.PI * 4f);
            float span = OrpheusSpriteAnimator.RunCycleDistance * .25f;
            float footHeight = ankle.y + Mathf.Abs(rig.travel.y) * span;
            float furthest = new Vector2(rig.travel.x * span, hip.y - ankle.y).magnitude;
            float length = Mathf.Max(Vector2.Distance(hip, knee) + Vector2.Distance(knee, ankle), furthest * 1.035f);
            float upperLength = length * Vector2.Distance(hip, knee) /
                (Vector2.Distance(hip, knee) + Vector2.Distance(knee, ankle));
            // Far limb, retained torso/cape, near limb. All three retain original source UVs.
            for (int layer = 0; layer < 3; layer++)
            {
                // Shared source grid corners are shared GPU vertices within each anatomical layer.
                var layerVertices = new Dictionary<Vector2, ushort>();
                bool leg = layer != 1, near = layer == 2;
                float q = Mathf.Repeat(phase + (near ? 0f : .5f), 1f);
                float sweep = q < .5f ? 1f - q * 4f : q * 4f - 3f;
                float lift = q < .5f ? 0f : Mathf.Sin((q - .5f) * Mathf.PI * 2f) * Clearance;
                Vector2 lateral = rig.lateral * (near ? 1f : -1f);
                Vector2 targetHip = hip + lateral + Vector2.up * bob;
                Vector2 targetAnkle = new Vector2(hip.x, footHeight) + lateral + rig.travel * (span * sweep) + Vector2.up * lift;
                Vector2 bend = Mathf.Abs(rig.travel.x) > .1f ? Vector2.right
                    : Vector2.right * Mathf.Sign(rig.lateral.x) * (near ? 1f : -1f);
                Vector2 targetKnee = Knee(targetHip, targetAnkle, upperLength, length - upperLength, bend);
                foreach (Patch patch in patches)
                {
                    if (patch.leg != leg) continue;
                    var quad = new ushort[4];
                    int corner = 0;
                    foreach (Vector2 pixel in new[] { patch.min, new Vector2(patch.max.x, patch.min.y),
                        new Vector2(patch.min.x, patch.max.y), patch.max })
                    {
                        if (layerVertices.TryGetValue(pixel, out ushort existing))
                        {
                            quad[corner++] = existing;
                            continue;
                        }
                        if (positions.Count >= ushort.MaxValue) throw new InvalidOperationException("Bilateral run mesh exceeds sprite vertex capacity.");
                        ushort index = (ushort)positions.Count;
                        layerVertices.Add(pixel, index);
                        quad[corner++] = index;
                        Vector2 point = (pixel - paint.rect.position - paint.pivot) / paint.pixelsPerUnit;
                        Vector2 position = leg ? Skin(point, hip, knee, ankle, targetHip, targetKnee, targetAnkle,
                                Vector2.up * bob, RetainedHipDepthPixels / paint.pixelsPerUnit)
                            : point + Vector2.up * bob;
                        positions.Add(position);
                        uv.Add(new Vector2(pixel.x / paint.texture.width, pixel.y / paint.texture.height));
                    }
                    indices.Add(quad[0]); indices.Add(quad[2]); indices.Add(quad[1]);
                    indices.Add(quad[1]); indices.Add(quad[2]); indices.Add(quad[3]);
                }
            }
            Sprite frame = Sprite.Create(paint.texture, paint.rect,
                new Vector2(paint.pivot.x / paint.rect.width, paint.pivot.y / paint.rect.height), paint.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            using (var p = new NativeArray<Vector3>(positions.ToArray(), Allocator.Temp))
            using (var t = new NativeArray<Vector2>(uv.ToArray(), Allocator.Temp))
            using (var triangles = new NativeArray<ushort>(indices.ToArray(), Allocator.Temp))
            {
                frame.SetVertexCount(positions.Count);
                frame.SetVertexAttribute(VertexAttribute.Position, p);
                frame.SetVertexAttribute(VertexAttribute.TexCoord0, t);
                frame.SetIndices(triangles);
            }
            return frame;
        }

        private static Vector2 Skin(Vector2 point, Vector2 hip, Vector2 knee, Vector2 ankle,
            Vector2 targetHip, Vector2 targetKnee, Vector2 targetAnkle, Vector2 torsoShift, float pinnedDepth)
        {
            Vector2 upper = TransformBone(point, hip, knee, targetHip, targetKnee);
            Vector2 lower = TransformBone(point, knee, ankle, targetKnee, targetAnkle);
            Vector2 along = ((knee - hip).normalized + (ankle - knee).normalized).normalized;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-.14f, .14f, Vector2.Dot(point - knee, along)));
            Vector2 posed = Vector2.Lerp(upper, lower, blend);
            float rootWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(pinnedDepth, pinnedDepth + .42f, hip.y - point.y));
            return Vector2.Lerp(point + torsoShift, posed, rootWeight);
        }

        private static Vector2 TransformBone(Vector2 point, Vector2 sourceA, Vector2 sourceB, Vector2 targetA, Vector2 targetB)
        {
            Vector2 source = sourceB - sourceA, target = targetB - targetA;
            Vector2 offset = point - sourceA;
            float along = Vector2.Dot(offset, source) / source.sqrMagnitude;
            float across = (source.x * offset.y - source.y * offset.x) / source.magnitude;
            return targetA + target * along + new Vector2(-target.y, target.x).normalized * across;
        }

        private static Vector2 Knee(Vector2 hip, Vector2 ankle, float upper, float lower, Vector2 bend)
        {
            Vector2 delta = ankle - hip;
            float distance = Mathf.Clamp(delta.magnitude, .001f, upper + lower - .001f);
            Vector2 direction = delta.normalized, perpendicular = new Vector2(-direction.y, direction.x);
            float along = (upper * upper - lower * lower + distance * distance) / (2f * distance);
            float across = Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));
            return hip + direction * along + perpendicular * across * (Vector2.Dot(perpendicular, bend) >= 0f ? 1f : -1f);
        }

        private static Vector2 Local(Sprite sprite, Vector2 topLeftPixel)
            => (new Vector2(topLeftPixel.x, sprite.texture.height - topLeftPixel.y) - sprite.rect.position - sprite.pivot) / sprite.pixelsPerUnit;

        private static Vector2[] Points(params float[] coordinates)
        {
            var result = new Vector2[coordinates.Length / 2];
            for (int i = 0; i < result.Length; i++) result[i] = new Vector2(coordinates[i * 2], coordinates[i * 2 + 1]);
            return result;
        }

        private static bool Inside(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) /
                    (polygon[j].y - polygon[i].y) + polygon[i].x) inside = !inside;
            return inside;
        }

        private static bool Within(Vector2 point, Vector2[] polygon, float margin)
        {
            if (Inside(point, polygon)) return true;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 edge = polygon[i] - polygon[j];
                float along = Mathf.Clamp01(Vector2.Dot(point - polygon[j], edge) / edge.sqrMagnitude);
                if ((point - polygon[j] - edge * along).sqrMagnitude <= margin * margin) return true;
            }
            return false;
        }
    }
}
