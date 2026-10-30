using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Rendering;
using Unity.Collections;

namespace OrpheusBelow.EditorTools
{
    /// <summary>Isolates connected figures instead of treating irregular generated sheets as a perfect grid.</summary>
    public static class OrpheusAtlasSlicer
    {
        private sealed class Island
        {
            public readonly List<int> pixels = new List<int>();
            public RectInt bounds;
        }

        public static Sprite[] Bake(Texture2D texture, string name, OrpheusAnimationLibrary owner,
            int columns, int rows, bool effects, IReadOnlyDictionary<int, float> horizontalAnchors = null)
        {
            Color32[] pixels = texture.GetPixels32();
            int width = texture.width, height = texture.height;
            int[] coverage = new int[height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                if (pixels[y * width + x].a >= 96) coverage[height - 1 - y]++;
            int[] cuts = Cuts(coverage, rows);
            var figures = new Island[columns * rows];
            for (int row = 0; row < rows; row++)
            {
                var strip = new RectInt(0, height - cuts[row + 1], width, cuts[row + 1] - cuts[row]);
                List<Island> islands = Islands(pixels, width, strip);
                // Dust, floating lyre strings and disconnected slash tips are not character bodies.
                List<Island> bodies = islands.Where(i => i.pixels.Count >= strip.width * strip.height / (columns * 35))
                    .OrderByDescending(i => i.pixels.Count).Take(columns).OrderBy(i => i.bounds.center.x).ToList();
                for (int column = 0; column < columns; column++)
                {
                    Island figure;
                    if (bodies.Count == columns && !effects) figure = bodies[column];
                    else
                    {
                        int left = Mathf.RoundToInt(width * column / (float)columns);
                        int right = Mathf.RoundToInt(width * (column + 1) / (float)columns);
                        var cell = new RectInt(left, strip.y, right - left, strip.height);
                        List<Island> local = Islands(pixels, width, cell);
                        figure = local.OrderByDescending(i => i.pixels.Count).FirstOrDefault();
                    }
                    if (figure == null) throw new InvalidOperationException($"Empty {name} cell {row},{column}");
                    figures[row * columns + column] = figure;
                }
            }

            // Column zero is an upright pose, even on sheets whose other columns contain fallen bodies or large effects.
            int[] upright = Enumerable.Range(0, rows).Select(r => figures[r * columns].bounds.height).OrderBy(h => h).ToArray();
            float ppu = effects ? height / (rows * 4.6f) : upright[upright.Length / 2] / owner.visibleHeight;
            var sprites = new Sprite[figures.Length];
            for (int i = 0; i < figures.Length; i++)
            {
                Island figure = figures[i];
                RectInt b = figure.bounds;
                var rect = new RectInt(Mathf.Max(0, b.xMin - 2), Mathf.Max(0, b.yMin - 2), 0, 0);
                rect.xMax = Mathf.Min(width, b.xMax + 2); rect.yMax = Mathf.Min(height, b.yMax + 2);
                Vector2 pivot = effects ? Vector2.one * .5f : GroundPivot(figure, pixels, width, rect);
                if (!effects && horizontalAnchors != null && horizontalAnchors.TryGetValue(i, out float anchorX))
                {
                    if (float.IsNaN(anchorX) || float.IsInfinity(anchorX) || anchorX < rect.xMin || anchorX > rect.xMax)
                        throw new InvalidOperationException($"Reviewed body anchor lies outside {name} cell {i}.");
                    // Register the intact painting around its anatomical body root, not
                    // whichever support boot happens to occupy the bottom of this pose.
                    pivot.x = (anchorX - rect.x) / rect.width;
                }
                Sprite sprite = Sprite.Create(texture, new Rect(rect.x, rect.y, rect.width, rect.height), pivot, ppu, 0, SpriteMeshType.FullRect);
                sprite.name = name + "_" + (i / columns).ToString("D2") + "_" + (i % columns).ToString("D2");
                if (!effects) IsolateMesh(sprite, figure, rect, width);
                AssetDatabase.AddObjectToAsset(sprite, owner);
                sprites[i] = sprite;
            }
            return sprites;
        }

        private static Vector2 GroundPivot(Island figure, Color32[] pixels, int width, RectInt rect)
        {
            int bottom = figure.bounds.yMin;
            double totalX = 0; int count = 0;
            foreach (int index in figure.pixels)
            {
                Color32 pixel = pixels[index];
                if (index / width > bottom + figure.bounds.height * .16f || pixel.a < 96) continue;
                // Prefer boot paint to gold arcs when locating the ground anchor.
                if (pixel.r + pixel.g + pixel.b > 300) continue;
                totalX += index % width + .5; count++;
            }
            float anchor = count > 0 ? (float)(totalX / count) : figure.bounds.center.x;
            return new Vector2(Mathf.Clamp01((anchor - rect.x) / rect.width), (bottom - rect.y) / (float)rect.height);
        }

        private static List<Island> Islands(Color32[] pixels, int width, RectInt window)
        {
            var seen = new bool[window.width * window.height];
            var queue = new Queue<int>();
            var islands = new List<Island>();
            for (int y = window.yMin; y < window.yMax; y++) for (int x = window.xMin; x < window.xMax; x++)
            {
                int local = (y - window.y) * window.width + x - window.x;
                if (seen[local] || pixels[y * width + x].a < 40) continue;
                var island = new Island { bounds = new RectInt(x, y, 1, 1) };
                seen[local] = true; queue.Enqueue(y * width + x);
                int minX = x, maxX = x, minY = y, maxY = y;
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue(); island.pixels.Add(index);
                    int px = index % width, py = index / width;
                    minX = Mathf.Min(minX, px); maxX = Mathf.Max(maxX, px);
                    minY = Mathf.Min(minY, py); maxY = Mathf.Max(maxY, py);
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = px + dx, ny = py + dy;
                        if (nx < window.xMin || nx >= window.xMax || ny < window.yMin || ny >= window.yMax) continue;
                        int next = (ny - window.y) * window.width + nx - window.x;
                        if (seen[next] || pixels[ny * width + nx].a < 40) continue;
                        seen[next] = true; queue.Enqueue(ny * width + nx);
                    }
                }
                island.bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
                islands.Add(island);
            }
            return islands;
        }

        private static void IsolateMesh(Sprite sprite, Island figure, RectInt rect, int textureWidth)
        {
            // Small horizontal runs preserve the original alpha edge while excluding every other island.
            const int tile = 2;
            int columns = Mathf.CeilToInt(rect.width / (float)tile), rows = Mathf.CeilToInt(rect.height / (float)tile);
            var cells = new bool[columns * rows];
            foreach (int index in figure.pixels)
                cells[((index / textureWidth - rect.y) / tile) * columns + (index % textureWidth - rect.x) / tile] = true;
            var vertices = new List<Vector2>(); var triangles = new List<ushort>();
            for (int row = 0; row < rows; row++) for (int column = 0; column < columns; column++)
            {
                if (!cells[row * columns + column]) continue;
                int begin = column;
                while (column + 1 < columns && cells[row * columns + column + 1]) column++;
                float left = begin * tile, right = Mathf.Min(rect.width, (column + 1) * tile);
                float bottom = row * tile, top = Mathf.Min(rect.height, (row + 1) * tile);
                ushort v = (ushort)vertices.Count;
                vertices.Add((new Vector2(left, bottom) - sprite.pivot) / sprite.pixelsPerUnit);
                vertices.Add((new Vector2(right, bottom) - sprite.pivot) / sprite.pixelsPerUnit);
                vertices.Add((new Vector2(left, top) - sprite.pivot) / sprite.pixelsPerUnit);
                vertices.Add((new Vector2(right, top) - sprite.pivot) / sprite.pixelsPerUnit);
                triangles.Add(v); triangles.Add((ushort)(v + 2)); triangles.Add((ushort)(v + 1));
                triangles.Add((ushort)(v + 1)); triangles.Add((ushort)(v + 2)); triangles.Add((ushort)(v + 3));
            }
            var positionData = new Vector3[vertices.Count];
            var uvData = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                positionData[i] = vertices[i];
                Vector2 pixel = vertices[i] * sprite.pixelsPerUnit + sprite.pivot + sprite.rect.position;
                uvData[i] = new Vector2(pixel.x / sprite.texture.width, pixel.y / sprite.texture.height);
            }
            using (var positions = new NativeArray<Vector3>(positionData, Allocator.Temp))
            using (var uv = new NativeArray<Vector2>(uvData, Allocator.Temp))
            using (var indices = new NativeArray<ushort>(triangles.ToArray(), Allocator.Temp))
            {
                sprite.SetVertexCount(vertices.Count);
                sprite.SetVertexAttribute(VertexAttribute.Position, positions);
                sprite.SetVertexAttribute(VertexAttribute.TexCoord0, uv);
                sprite.SetIndices(indices);
            }
        }

        private static int[] Cuts(int[] coverage, int count)
        {
            var cuts = new int[count + 1]; cuts[count] = coverage.Length;
            float spacing = coverage.Length / (float)count;
            for (int i = 1; i < count; i++)
            {
                int target = Mathf.RoundToInt(spacing * i), radius = Mathf.RoundToInt(spacing * .24f);
                int best = target, score = int.MaxValue;
                for (int p = Mathf.Max(cuts[i - 1] + 8, target - radius); p <= Mathf.Min(coverage.Length - 2, target + radius); p++)
                {
                    int candidate = (coverage[p - 1] + coverage[p] + coverage[p + 1]) * 1000 + Mathf.Abs(p - target);
                    if (candidate < score) { score = candidate; best = p; }
                }
                cuts[i] = best;
            }
            return cuts;
        }
    }
}
