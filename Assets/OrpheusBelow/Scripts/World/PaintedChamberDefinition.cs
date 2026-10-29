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

        [Serializable]
        public class StairZone
        {
            public string name;
            public Vector2[] polygon;
            public Vector2 uphill;
        }

        public int index;
        public string title;
        public Texture2D painting;
        public string sourceFilename;
        public float imageWidth = 72f;
        public float imageHeight = 48f;
        public float zoom = 11f;
        [Header("Orpheus dominant light")]
        [Tooltip("Screen-space direction from Orpheus toward the chamber's dominant light.")]
        public Vector2 dominantLightDirection = new Vector2(.7071068f, .7071068f);
        public Color dominantLightColor = new Color(1f, .62f, .28f, 1f);
        [Range(.2f, 2f)] public float dominantLightIntensity = 1f;
        [Tooltip("Coordinates are pixels from the top-left of the original 1536 × 1024 painting.")]
        public Vector2[] walkableBoundary;
        public Cover[] cover = Array.Empty<Cover>();
        public StairZone[] stairs = Array.Empty<StairZone>();
        public Vector2 entrance;
        public Vector2 exit;
        public Vector2 returnExit;
        public Vector2 returnSpawn;
        public Vector2 reunionPoint;
        public Vector2 well;
        public Vector2 encounterCenter;
        public Vector2[] enemySpawns;
        public bool hasGuardian;
        public BossIdentity guardian;

        public Vector2 ResolvedDominantLightDirection
        {
            get
            {
                Vector2 direction = dominantLightDirection;
                if (direction.sqrMagnitude < .0001f) direction = new Vector2(.7071068f, .7071068f);
                return direction.normalized;
            }
        }

        public Vector3 ImageToGround(Vector2 pixel)
        {
            float x = (pixel.x / 1536f - .5f) * imageWidth;
            float y = (.5f - pixel.y / 1024f) * imageHeight;
            return UnderworldCamera2D.IsoRightXZ * x + UnderworldCamera2D.IsoUpXZ * (y / Mathf.Sin(36f * Mathf.Deg2Rad));
        }

        public bool StairMotion(Vector3 localPoint, Vector3 groundDelta, out bool ascending)
        {
            ascending = false;
            groundDelta.y = 0;
            if (groundDelta.sqrMagnitude < .000001f || stairs == null) return false;
            Vector2 pixel = GroundToImage(localPoint);
            foreach (var stair in stairs)
            {
                if (stair.polygon == null || !PaintedPolygon.Contains(stair.polygon, pixel)) continue;
                Vector3 axis = ImageToGround(stair.uphill) - ImageToGround(Vector2.zero);
                float alignment = Vector3.Dot(groundDelta.normalized, axis.normalized);
                if (Mathf.Abs(alignment) < .35f) continue;
                ascending = alignment > 0; return true;
            }
            return false;
        }

        public Vector2 GroundToImage(Vector3 point)
        {
            float x = Vector3.Dot(point, UnderworldCamera2D.IsoRightXZ);
            float y = Vector3.Dot(point, UnderworldCamera2D.IsoUpXZ) * Mathf.Sin(36f * Mathf.Deg2Rad);
            return new Vector2((x / imageWidth + .5f) * 1536f, (.5f - y / imageHeight) * 1024f);
        }

        public bool IsWalkable(Vector3 point, float radius = 0f)
        {
            point.y = 0f;
            Vector2 pixel = GroundToImage(point);
            if (!PaintedPolygon.Contains(walkableBoundary, pixel)) return false;
            if (DistanceToEdge(walkableBoundary, point) < radius) return false;
            foreach (Cover obstacle in cover)
                if (PaintedPolygon.Contains(obstacle.footprint, pixel) || DistanceToEdge(obstacle.footprint, point) < radius)
                    return false;
            return true;
        }

        // Sweep the complete actor footprint, including the rounded ends of every edge.
        // Checking only the destination misses thin cover and concave boundary corners during dashes.
        public bool SweepMovement(Vector3 origin, Vector3 delta, float radius, out float fraction, out Vector3 normal)
        {
            origin.y = delta.y = 0f;
            radius = Mathf.Max(0f, radius);
            fraction = 1f;
            normal = Vector3.zero;
            SweepPolygon(walkableBoundary, origin, delta, radius, ref fraction, ref normal);
            foreach (var obstacle in cover)
                SweepPolygon(obstacle.footprint, origin, delta, radius, ref fraction, ref normal);
            return normal != Vector3.zero;
        }

        private void SweepPolygon(Vector2[] polygon, Vector3 origin, Vector3 delta, float radius,
            ref float fraction, ref Vector3 normal)
        {
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector3 a = ImageToGround(polygon[i]), b = ImageToGround(polygon[(i + 1) % polygon.Length]);
                Vector3 edge = b - a;
                float length = edge.magnitude;
                if (length < .00001f) continue;
                edge /= length;
                Vector3 perpendicular = new Vector3(-edge.z, 0f, edge.x);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 inward = perpendicular * side;
                    float closingSpeed = Vector3.Dot(delta, inward);
                    float separation = Vector3.Dot(origin - a, inward);
                    if (closingSpeed >= -.000001f || separation < radius - .00001f) continue;
                    float t = (radius - separation) / closingSpeed;
                    if (t < 0f || t > fraction) continue;
                    float along = Vector3.Dot(origin + delta * t - a, edge);
                    if (along < 0f || along > length) continue;
                    fraction = t; normal = inward;
                }
                SweepEndpoint(origin, delta, a, radius, ref fraction, ref normal);
            }
        }

        private static void SweepEndpoint(Vector3 origin, Vector3 delta, Vector3 endpoint, float radius,
            ref float fraction, ref Vector3 normal)
        {
            Vector3 offset = origin - endpoint;
            float approach = Vector3.Dot(offset, delta);
            if (approach >= 0f) return;
            float speedSquared = delta.sqrMagnitude;
            float separation = offset.sqrMagnitude - radius * radius;
            float discriminant = approach * approach - speedSquared * separation;
            if (discriminant < 0f) return;
            float t = Mathf.Max(0f, (-approach - Mathf.Sqrt(discriminant)) / speedSquared);
            if (t > fraction) return;
            Vector3 contactNormal = (offset + delta * t).normalized;
            if (Vector3.Dot(delta, contactNormal) >= -.000001f) return;
            fraction = t; normal = contactNormal;
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
