using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow.EditorTools
{
    public static class PaintedNavigationAuthoring
    {
        public static void ApplyReviewedTrace(PaintedChamberDefinition definition)
        {
            var trace = PaintedChamberNavigationTrace.Load(definition.index);
            ValidatePolygon(trace.walkableBoundary, "walkable floor", definition.index);
            if (trace.cover == null || trace.cover.Length == 0 || trace.enemySpawns == null || trace.enemySpawns.Length == 0)
                throw new InvalidOperationException("Reviewed chamber must include cover and spawn positions: " + definition.index);
            var names = new HashSet<string>();
            foreach (var cover in trace.cover)
            {
                if (string.IsNullOrEmpty(cover.name) || !names.Add(cover.name) || cover.height <= 0)
                    throw new InvalidOperationException("Cover needs a unique name and positive height: " + definition.index);
                ValidatePolygon(cover.footprint, cover.name + " base", definition.index);
                ValidatePolygon(cover.silhouette, cover.name + " silhouette", definition.index);
            }
            trace.ApplyTo(definition);
            foreach (Vector2 arrival in new[] { trace.entrance, trace.returnSpawn, trace.encounterCenter })
                RequireFloor(definition, arrival, .45f, "arrival/encounter");
            foreach (Vector2 spawn in trace.enemySpawns) RequireFloor(definition, spawn, .45f, "enemy spawn");
            if (trace.walkableProbes == null || trace.walkableProbes.Length < 12 || trace.blockedProbes == null || trace.blockedProbes.Length < 12)
                throw new InvalidOperationException("Reviewed chamber needs independent positive and negative probes: " + definition.index);
            foreach (var probe in trace.walkableProbes) RequireFloor(definition, probe.Point, .45f, probe.name);
            foreach (var probe in trace.blockedProbes)
                if (definition.IsWalkable(definition.ImageToGround(probe.Point), 0f))
                    throw new InvalidOperationException("Blocked painting point became accessible: " + definition.index + "/" + probe.name);
            if (trace.occlusionProbes == null || trace.occlusionProbes.Length < 3)
                throw new InvalidOperationException("Reviewed chamber needs front/behind visual checks: " + definition.index);
            foreach (var probe in trace.occlusionProbes)
            {
                if (!names.Contains(probe.coverName)) throw new InvalidOperationException("Unknown occluding cover: " + probe.coverName);
                RequireFloor(definition, probe.behind, .45f, probe.name + " behind");
                RequireFloor(definition, probe.front, .45f, probe.name + " front");
            }
        }

        private static void RequireFloor(PaintedChamberDefinition definition, Vector2 pixel, float radius, string label)
        {
            if (!definition.IsWalkable(definition.ImageToGround(pixel), radius))
                throw new InvalidOperationException("Reviewed floor point is blocked: " + definition.index + "/" + label + " " + pixel);
        }

        public static void ValidatePolygon(Vector2[] polygon, string label, int chamber)
        {
            if (polygon == null || polygon.Length < 3) throw new InvalidOperationException("Missing polygon " + chamber + "/" + label);
            double twiceArea = 0;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
                if (!Finite(a.x) || !Finite(a.y) || a.x < 0 || a.x > 1536 || a.y < 0 || a.y > 1024 || (a - b).sqrMagnitude < .01f)
                    throw new InvalidOperationException("Out-of-image or duplicate vertex in " + chamber + "/" + label);
                twiceArea += a.x * b.y - b.x * a.y;
                for (int j = i + 2; j < polygon.Length; j++)
                {
                    if (i == 0 && j == polygon.Length - 1) continue;
                    Vector2 c = polygon[j], d = polygon[(j + 1) % polygon.Length];
                    if (SegmentsIntersect(a, b, c, d))
                        throw new InvalidOperationException("Intersecting edges in " + chamber + "/" + label + " " + i + "/" + j);
                }
            }
            if (Math.Abs(twiceArea) < 2) throw new InvalidOperationException("Degenerate polygon " + chamber + "/" + label);
            // Exercise the same ear clipping used by the saved collider/occlusion meshes.
            PaintedPolygon.Triangulate(polygon);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float p = Cross(b - a, c - a), q = Cross(b - a, d - a);
            float r = Cross(d - c, a - c), s = Cross(d - c, b - c);
            if (p * q < -.0001f && r * s < -.0001f) return true;
            return OnSegment(a, b, c, p) || OnSegment(a, b, d, q) || OnSegment(c, d, a, r) || OnSegment(c, d, b, s);
        }
        private static bool OnSegment(Vector2 a, Vector2 b, Vector2 p, float cross) => Math.Abs(cross) < .001f &&
            p.x >= Mathf.Min(a.x, b.x) - .001f && p.x <= Mathf.Max(a.x, b.x) + .001f &&
            p.y >= Mathf.Min(a.y, b.y) - .001f && p.y <= Mathf.Max(a.y, b.y) + .001f;
    }
}
