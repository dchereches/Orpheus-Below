using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    [CreateAssetMenu(menuName = "Orpheus Below/Painted chamber layout")]
    public sealed class PaintedChamberDefinition : ScriptableObject
    {
        [Serializable]
        public class Cover
        {
            public string name;
            public Vector2[] footprint;
            public Vector2[] silhouette;
            public float height = 4f;
        }

        public int index;
        public string title;
        public Texture2D painting;
        public string sourceFilename;
        public float imageWidth = 72f;
        public float imageHeight = 48f;
        public float zoom = 11f;
        [Tooltip("Coordinates are pixels from the top-left of the original 1536 × 1024 painting.")]
        public Vector2[] walkableBoundary;
        public Cover[] cover = Array.Empty<Cover>();
        public Vector2 entrance;
        public Vector2 exit;
        public Vector2 well;
        public Vector2 encounterCenter;
        public Vector2[] enemySpawns;
        public bool hasGuardian;
        public BossIdentity guardian;

        public Vector3 ImageToGround(Vector2 pixel)
        {
            float x = (pixel.x / 1536f - .5f) * imageWidth;
            float y = (.5f - pixel.y / 1024f) * imageHeight;
            return UnderworldCamera2D.IsoRightXZ * x + UnderworldCamera2D.IsoUpXZ * (y / Mathf.Sin(36f * Mathf.Deg2Rad));
        }

        public Vector2 GroundToImage(Vector3 point)
        {
            float x = Vector3.Dot(point, UnderworldCamera2D.IsoRightXZ);
            float y = Vector3.Dot(point, UnderworldCamera2D.IsoUpXZ) * Mathf.Sin(36f * Mathf.Deg2Rad);
            return new Vector2((x / imageWidth + .5f) * 1536f, (.5f - y / imageHeight) * 1024f);
        }

        public bool IsWalkable(Vector3 point, float radius = 0f)
        {
            Vector2 pixel = GroundToImage(point);
            if (!PaintedPolygon.Contains(walkableBoundary, pixel)) return false;
            if (DistanceToEdge(walkableBoundary, point) < radius) return false;
            foreach (Cover obstacle in cover)
                if (PaintedPolygon.Contains(obstacle.footprint, pixel) || DistanceToEdge(obstacle.footprint, point) < radius)
                    return false;
            return true;
        }

        private float DistanceToEdge(Vector2[] polygon, Vector3 point)
        {
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector3 a = ImageToGround(polygon[i]), b = ImageToGround(polygon[(i + 1) % polygon.Length]);
                float t = Mathf.Clamp01(Vector3.Dot(point - a, b - a) / Mathf.Max(.0001f, (b - a).sqrMagnitude));
                nearest = Mathf.Min(nearest, Vector3.Distance(point, a + (b - a) * t));
            }
            return nearest;
        }

        public Vector3 NearestWalkable(Vector3 point, float radius = .6f)
        {
            if (IsWalkable(point, radius)) return point;
            Vector3 best = ImageToGround(entrance);
            float distance = float.PositiveInfinity;
            for (int y = 20; y < 1024; y += 16)
                for (int x = 20; x < 1536; x += 16)
                {
                    Vector3 candidate = ImageToGround(new Vector2(x, y));
                    float d = (candidate - point).sqrMagnitude;
                    if (d >= distance || !IsWalkable(candidate, radius)) continue;
                    best = candidate; distance = d;
                }
            return best;
        }
    }

    public static class PaintedPolygon
    {
        public static bool Contains(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) /
                    (polygon[j].y - polygon[i].y) + polygon[i].x) inside = !inside;
            return inside;
        }

        public static int[] Triangulate(Vector2[] polygon)
        {
            var remaining = new List<int>();
            float area = 0f;
            for (int i = 0; i < polygon.Length; i++)
            {
                remaining.Add(i);
                Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
                area += a.x * b.y - b.x * a.y;
            }
            if (area < 0) remaining.Reverse();
            var triangles = new List<int>();
            int guard = polygon.Length * polygon.Length;
            while (remaining.Count > 2 && guard-- > 0)
            {
                bool found = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i + remaining.Count - 1) % remaining.Count], b = remaining[i], c = remaining[(i + 1) % remaining.Count];
                    if (Cross(polygon[b] - polygon[a], polygon[c] - polygon[b]) <= .001f) continue;
                    bool occupied = false;
                    foreach (int p in remaining)
                        if (p != a && p != b && p != c && InTriangle(polygon[p], polygon[a], polygon[b], polygon[c])) { occupied = true; break; }
                    if (occupied) continue;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    remaining.RemoveAt(i); found = true; break;
                }
                if (!found) throw new InvalidOperationException("Painted polygon has intersecting or degenerate edges.");
            }
            return triangles.ToArray();
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            => Cross(b - a, p - a) >= 0f && Cross(c - b, p - b) >= 0f && Cross(a - c, p - c) >= 0f;
    }
}
