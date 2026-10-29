using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace OrpheusBelow.EditorTools
{
    /// <summary>Builds sixteen full-body running poses from each upright painted canonical view.</summary>
    public static class OrpheusAthleticRunBaker
    {
        public const int Phases = 16;
        [Serializable] private sealed class Layout { public Rig[] rigs; }
        [Serializable] private sealed class Rig
        {
            // Native texture pixels, with origin at top left. Polygons isolate the visible source
            // limb; removed polygons erase the other source limb from the retained body layer.
            public Vector2 hip, knee, ankle, shoulder, elbow, wrist, otherShoulder, otherHip;
            public Vector2[] leg, removedLeg, arm, removedArm;
            public int sourceWidth, sourceHeight;
            public float legMargin, armMargin, removedMargin = 2f, removedArmMargin = 2f;
        }

        private struct Cell { public Vector2 min, max; public bool leg, arm, body; }

        public static Sprite[] Bake(Sprite[] canonicalFive, OrpheusAnimationLibrary owner, string layoutJsonPath)
        {
            if (canonicalFive == null || canonicalFive.Length != 5)
                throw new ArgumentException("Athletic running requires exactly South, SouthEast, East, NorthEast and North source sprites.");
            Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(layoutJsonPath));
            if (layout?.rigs == null || layout.rigs.Length != 5)
                throw new InvalidDataException("Run layout requires a rigs array with five canonical source rigs.");
            var frames = new Sprite[5 * Phases];
            for (int row = 0; row < 5; row++)
            {
                Sprite paint = canonicalFive[row];
                if (paint == null) throw new ArgumentException("Missing upright source sprite at row " + row);
                Rig rig = layout.rigs[row];
                Validate(rig, row);
                ScaleToImportedTexture(rig, paint.texture);
                var cells = Extract(paint, rig);
                for (int frame = 0; frame < Phases; frame++)
                {
                    Sprite sprite = Build(paint, rig, cells, row, frame / (float)Phases);
                    sprite.name = "AthleticRun_" + row.ToString("D2") + "_" + frame.ToString("D2");
                    AssetDatabase.AddObjectToAsset(sprite, owner);
                    frames[row * Phases + frame] = sprite;
                }
            }
            return frames;
        }

        private static void ScaleToImportedTexture(Rig rig, Texture2D texture)
        {
            Vector2 scale = new Vector2(rig.sourceWidth > 0 ? texture.width / (float)rig.sourceWidth : 1f,
                rig.sourceHeight > 0 ? texture.height / (float)rig.sourceHeight : 1f);
            rig.hip = Vector2.Scale(rig.hip, scale); rig.knee = Vector2.Scale(rig.knee, scale);
            rig.ankle = Vector2.Scale(rig.ankle, scale); rig.shoulder = Vector2.Scale(rig.shoulder, scale);
            rig.elbow = Vector2.Scale(rig.elbow, scale); rig.wrist = Vector2.Scale(rig.wrist, scale);
            rig.otherShoulder = Vector2.Scale(rig.otherShoulder, scale); rig.otherHip = Vector2.Scale(rig.otherHip, scale);
            foreach (Vector2[] polygon in new[] { rig.leg, rig.removedLeg, rig.arm, rig.removedArm })
                if (polygon != null) for (int i = 0; i < polygon.Length; i++) polygon[i] = Vector2.Scale(polygon[i], scale);
            float marginScale = Mathf.Min(scale.x, scale.y);
            rig.legMargin *= marginScale; rig.armMargin *= marginScale;
            rig.removedMargin *= marginScale; rig.removedArmMargin *= marginScale;
        }

        private static void Validate(Rig rig, int row)
        {
            if (rig == null || rig.leg == null || rig.arm == null || rig.leg.Length < 3 || rig.arm.Length < 3 ||
                (rig.hip - rig.knee).sqrMagnitude < 1f || (rig.knee - rig.ankle).sqrMagnitude < 1f ||
                (rig.shoulder - rig.elbow).sqrMagnitude < 1f || (rig.elbow - rig.wrist).sqrMagnitude < 1f)
                throw new InvalidDataException("Incomplete anatomical landmarks or limb masks in run rig " + row);
        }

        private static List<Cell> Extract(Sprite paint, Rig rig)
        {
            var cells = new List<Cell>();
            Color32[] pixels = paint.texture.GetPixels32();
            int left = Mathf.RoundToInt(paint.rect.xMin), bottom = Mathf.RoundToInt(paint.rect.yMin);
            int right = Mathf.RoundToInt(paint.rect.xMax), top = Mathf.RoundToInt(paint.rect.yMax);
            // All anatomical layers, including the pitching torso and cape, use the same 2px
            // lattice. Every cell is conforming; no merged strips can introduce T junctions.
            for (int y = bottom; y < top; y += 2) for (int x = left; x < right; x += 2)
            {
                int xmax = Mathf.Min(x + 2, right), ymax = Mathf.Min(y + 2, top);
                bool painted = false;
                for (int py = y; py < ymax; py++) for (int px = x; px < xmax; px++)
                    painted |= pixels[py * paint.texture.width + px].a >= 24;
                if (!painted) continue;
                Vector2 centre = new Vector2((x + xmax) * .5f, paint.texture.height - (y + ymax) * .5f);
                bool leg = Within(centre, rig.leg, rig.legMargin);
                bool arm = Within(centre, rig.arm, rig.armMargin);
                bool removedLeg = Within(centre, rig.removedLeg, rig.removedMargin);
                bool removedArm = Within(centre, rig.removedArm, rig.removedArmMargin);
                // Keep the solid coat/pelvis and the shoulder caps painted under the moving
                // limbs. Limbs blend from this same pinned root, preventing transparent seams.
                bool retainPelvis = centre.y <= rig.hip.y + 12f;
                bool retainShoulder = Vector2.Distance(centre, rig.shoulder) <= 7f ||
                    Vector2.Distance(centre, rig.otherShoulder) <= 7f;
                bool body = (!(leg || removedLeg) || retainPelvis) &&
                    (!(arm || removedArm) || retainShoulder);
                cells.Add(new Cell { min = new Vector2(x, y), max = new Vector2(xmax, ymax), leg = leg && !arm, arm = arm, body = body });
            }
            KeepLargestBodyIsland(cells);
            return cells;
        }

        private static void KeepLargestBodyIsland(List<Cell> cells)
        {
            // Removing both source limbs can leave an unowned boot or hand tip behind. Only
            // the retained torso/cape/head is filtered: moving anatomical paint stays intact.
            var bodyGrid = new Dictionary<Vector2, int>();
            for (int i = 0; i < cells.Count; i++)
                if (cells[i].body) bodyGrid.Add(cells[i].min, i);
            var visited = new bool[cells.Count];
            var largest = new List<int>();
            var queue = new Queue<int>();
            for (int seed = 0; seed < cells.Count; seed++)
            {
                if (!cells[seed].body || visited[seed]) continue;
                var component = new List<int>();
                visited[seed] = true; queue.Enqueue(seed);
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue(); component.Add(index);
                    Vector2 pixel = cells[index].min;
                    // Diagonal neighbors preserve thin, diagonally painted cape connections.
                    for (int dy = -2; dy <= 2; dy += 2) for (int dx = -2; dx <= 2; dx += 2)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (!bodyGrid.TryGetValue(pixel + new Vector2(dx, dy), out int next) || visited[next]) continue;
                        visited[next] = true; queue.Enqueue(next);
                    }
                }
                if (component.Count > largest.Count) largest = component;
            }
            var retained = new HashSet<int>(largest);
            for (int i = 0; i < cells.Count; i++)
            {
                if (!cells[i].body || retained.Contains(i)) continue;
                Cell cell = cells[i]; cell.body = false; cells[i] = cell;
            }
        }

        private static Sprite Build(Sprite paint, Rig rig, List<Cell> cells, int row, float phase)
        {
            Vector2 hip = Local(paint, rig.hip), knee = Local(paint, rig.knee), ankle = Local(paint, rig.ankle);
            Vector2 shoulder = Local(paint, rig.shoulder), elbow = Local(paint, rig.elbow), wrist = Local(paint, rig.wrist);
            Vector2 legs = OrpheusRunMotion.FitLegLengths(Vector2.Distance(hip, knee), Vector2.Distance(knee, ankle), hip.y - ankle.y);
            OrpheusRunMotion.Pose pose = OrpheusRunMotion.Evaluate(phase,
                legs.x, legs.y, Vector2.Distance(shoulder, elbow), Vector2.Distance(elbow, wrist));
            float angle = row * Mathf.PI * .25f;
            Vector2 travel = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle) * Mathf.Sin(36f * Mathf.Deg2Rad));
            Vector2 lateral = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle) * .2f);
            // The rendered boot anchor is independent of the source sprite's import pivot.
            Vector2 pelvis = new Vector2(0f, .30f + pose.hipHeight);
            Vector2 farHipOffset = rig.otherHip.sqrMagnitude > .01f
                ? Local(paint, rig.otherHip) - hip : lateral * -.26f;
            var positions = new List<Vector3>();
            var uvs = new List<Vector2>();
            var indices = new List<ushort>();
            // Far arm, far leg, retained body/cape/head, near leg, near arm.
            for (int layer = 0; layer < 5; layer++)
            {
                var welded = new Dictionary<Vector2, ushort>();
                bool arm = layer == 0 || layer == 4, leg = layer == 1 || layer == 3;
                bool near = layer >= 3;
                var limb = arm ? (near ? pose.nearArm : pose.farArm) : (near ? pose.nearLeg : pose.farLeg);
                Vector2 rootOffset = arm ? (near ? Vector2.zero : Local(paint, rig.otherShoulder) - shoulder)
                    : (near ? Vector2.zero : farHipOffset);
                Vector2 targetRoot = arm ? Body(shoulder, hip, pelvis, travel, lateral, phase, pose.pitch) + rootOffset : pelvis + rootOffset;
                Vector2 targetJoint = targetRoot + Project(limb.joint - limb.root, travel);
                Vector2 targetEnd = targetRoot + Project(limb.end - limb.root, travel);
                float q = Mathf.Repeat(phase + (near ? 0f : .5f), 1f);
                foreach (Cell cell in cells)
                {
                    if (arm ? !cell.arm : leg ? !cell.leg : !cell.body) continue;
                    var quad = new ushort[4];
                    Vector2[] corners = { cell.min, new Vector2(cell.max.x, cell.min.y), new Vector2(cell.min.x, cell.max.y), cell.max };
                    for (int c = 0; c < 4; c++)
                    {
                        Vector2 pixel = corners[c];
                        if (welded.TryGetValue(pixel, out ushort existing)) { quad[c] = existing; continue; }
                        if (positions.Count >= ushort.MaxValue)
                            throw new InvalidOperationException("Athletic run exceeds sprite vertex capacity; reduce the native canonical source resolution.");
                        ushort index = (ushort)positions.Count;
                        welded.Add(pixel, index); quad[c] = index;
                        Vector2 point = (pixel - paint.rect.position - paint.pivot) / paint.pixelsPerUnit;
                        Vector2 body = Body(point, hip, pelvis, travel, lateral, phase, pose.pitch);
                        Vector2 position = body;
                        if (arm || leg)
                            position = Skin(point, arm ? shoulder : hip, arm ? elbow : knee, arm ? wrist : ankle,
                                targetRoot, targetJoint, targetEnd, body + rootOffset, arm, q, paint.pixelsPerUnit);
                        positions.Add(position);
                        uvs.Add(new Vector2(pixel.x / paint.texture.width, pixel.y / paint.texture.height));
                    }
                    indices.Add(quad[0]); indices.Add(quad[2]); indices.Add(quad[1]);
                    indices.Add(quad[1]); indices.Add(quad[2]); indices.Add(quad[3]);
                }
            }
            Sprite sprite = Sprite.Create(paint.texture, paint.rect,
                new Vector2(paint.pivot.x / paint.rect.width, paint.pivot.y / paint.rect.height), paint.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            // The sprite data API accepts stride vertices outside the source rectangle;
            // OverrideGeometry rejects those vertices in the editor.
            using (var p = new NativeArray<Vector3>(positions.ToArray(), Allocator.Temp))
            using (var uv = new NativeArray<Vector2>(uvs.ToArray(), Allocator.Temp))
            using (var triangles = new NativeArray<ushort>(indices.ToArray(), Allocator.Temp))
            {
                sprite.SetVertexCount(positions.Count);
                sprite.SetVertexAttribute(VertexAttribute.Position, p);
                sprite.SetVertexAttribute(VertexAttribute.TexCoord0, uv);
                sprite.SetIndices(triangles);
            }
            return sprite;
        }

        private static Vector2 Body(Vector2 point, Vector2 hip, Vector2 pelvis, Vector2 travel, Vector2 lateral, float phase, float pitch)
        {
            Vector2 relative = point - hip;
            // Pitch and shoulder sway affect the whole painted torso. The same deterministic
            // transform is applied at shared corners; even cape lag remains a continuous field.
            Vector2 result = point + pelvis - hip + travel * (relative.y * pitch) +
                lateral * (Mathf.Sin(phase * Mathf.PI * 2f) * .028f * Mathf.Clamp01(relative.y));
            float trailing = Mathf.Clamp01(-Vector2.Dot(relative, travel.normalized) * 1.5f);
            float cape = trailing * Mathf.Clamp01((relative.y + .15f) * 1.2f);
            return result + travel * (cape * .035f * Mathf.Sin(phase * Mathf.PI * 2f - .6f));
        }

        private static Vector2 Skin(Vector2 point, Vector2 a, Vector2 b, Vector2 c, Vector2 ta, Vector2 tb, Vector2 tc,
            Vector2 rootPaint, bool arm, float phase, float ppu)
        {
            Vector2 upper = Bone(point, a, b, ta, tb);
            Vector2 lower = Bone(point, b, c, tb, tc);
            Vector2 direction = ((b - a).normalized + (c - b).normalized).normalized;
            float joint = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-.10f, .10f, Vector2.Dot(point - b, direction)));
            Vector2 result = Vector2.Lerp(upper, lower, joint);
            if (!arm)
            {
                // Boot paint stays rigid and level during plant instead of rotating with the
                // shin and scraping through the ground. Recovery permits a small toe dip.
                float tip = phase < OrpheusRunMotion.SupportDuty ? 0f : -.45f * Mathf.Sin((phase - OrpheusRunMotion.SupportDuty) / (1f - OrpheusRunMotion.SupportDuty) * Mathf.PI);
                Vector2 foot = tc + Rotate(point - c, tip);
                float footWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-.07f, .04f, Vector2.Dot(point - c, (c - b).normalized)));
                result = Vector2.Lerp(result, foot, footWeight);
            }
            float depth = arm ? Vector2.Distance(point, a) : a.y - point.y;
            float pinned = (arm ? 5f : 12f) / ppu;
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(pinned, pinned + (arm ? .16f : .24f), depth));
            return Vector2.Lerp(rootPaint, result, weight);
        }

        private static Vector2 Bone(Vector2 point, Vector2 a, Vector2 b, Vector2 ta, Vector2 tb)
        {
            Vector2 bone = b - a, posed = tb - ta, delta = point - a;
            float along = Vector2.Dot(delta, bone) / bone.sqrMagnitude;
            float across = (bone.x * delta.y - bone.y * delta.x) / bone.magnitude;
            return ta + posed * along + new Vector2(-posed.y, posed.x).normalized * across;
        }

        private static Vector2 Rotate(Vector2 p, float radians)
            => new Vector2(p.x * Mathf.Cos(radians) - p.y * Mathf.Sin(radians), p.x * Mathf.Sin(radians) + p.y * Mathf.Cos(radians));
        private static Vector2 Project(Vector2 point, Vector2 travel) => travel * point.x + Vector2.up * point.y;
        private static Vector2 Local(Sprite sprite, Vector2 pixel)
            => (new Vector2(pixel.x, sprite.texture.height - pixel.y) - sprite.rect.position - sprite.pivot) / sprite.pixelsPerUnit;

        private static bool Within(Vector2 point, Vector2[] polygon, float margin)
        {
            if (polygon == null || polygon.Length < 3) return false;
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[j], b = polygon[i];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                Vector2 edge = b - a;
                float t = edge.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude) : 0f;
                if (margin > 0f && (point - a - edge * t).sqrMagnitude <= margin * margin) return true;
            }
            return inside;
        }
    }
}
