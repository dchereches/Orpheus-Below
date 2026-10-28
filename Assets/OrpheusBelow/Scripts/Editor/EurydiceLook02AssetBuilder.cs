using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    public static class EurydiceLook02AssetBuilder
    {
        private const string Folder = "Assets/OrpheusBelow/Resources/Characters/Eurydice/Look02/";
        [MenuItem("Orpheus Below/Bake Eurydice Look 02")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            string path = Folder + "Library.asset";
            var library = AssetDatabase.LoadAssetAtPath<EurydiceAnimationLibrary>(path);
            if (library == null) { library = ScriptableObject.CreateInstance<EurydiceAnimationLibrary>(); AssetDatabase.CreateAsset(library, path); }
            else foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite) Object.DestroyImmediate(asset, true);
            library.visibleHeight = 2.5274f * 1.2f;
            library.idle = Bake("Idle", 4, 8, library);
            library.walk = Bake("Walk", 4, 8, library);
            library.reactions = Bake("Reactions", 6, 6, library);
            library.stairs = Bake("Stairs", 6, 4, library);
            library.effects = Bake("Effects", 6, 4, library, true);
            EditorUtility.SetDirty(library);
            PaintedChamberSceneBuilder.UpdateStairZones();
            AssetDatabase.SaveAssets();
            Debug.Log("[Eurydice Look02] BAKE PASS: 148 transparent frames, stable ground anchors, 20% larger silhouette and authored stair zones.");
        }

        private static Sprite[] Bake(string name, int columns, int rows, EurydiceAnimationLibrary owner, bool effects = false)
        {
            string path = Folder + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Missing " + path);
            importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = true; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Color32[] pixels = texture.GetPixels32();
            var cells = new RectInt[columns * rows]; var bounds = new RectInt[cells.Length];
            int clear = 0; foreach (Color32 p in pixels) if (p.a < 10) clear++;
            if (clear < pixels.Length / 5) throw new InvalidOperationException(name + " needs a real transparent background.");
            var rowCoverage = new int[texture.height];
            for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
                if (pixels[y * texture.width + x].a > 128) rowCoverage[texture.height - y - 1]++;
            int[] rowCuts = effects ? RegularCuts(texture.height, rows) : GutterCuts(rowCoverage, rows, true);
            for (int row = 0; row < rows; row++)
            {
                int y = texture.height - rowCuts[row + 1], top = texture.height - rowCuts[row];
                var coverage = new int[texture.width];
                for (int py = y; py < top; py++) for (int x = 0; x < texture.width; x++) if (pixels[py * texture.width + x].a > 128) coverage[x]++;
                int[] colCuts = effects ? RegularCuts(texture.width, columns) : GutterCuts(coverage, columns, false);
                for (int col = 0; col < columns; col++)
                {
                    int x = colCuts[col], right = colCuts[col + 1], index = row * columns + col;
                    int cellBottom = y, cellTop = top;
                    if (!effects)
                    {
                        // AI rows can overlap vertically across different columns. Find each column's own gaps.
                        var columnCoverage = new int[texture.height];
                        for (int py = 0; py < texture.height; py++) for (int px = x; px < right; px++)
                            if (pixels[py * texture.width + px].a > 128) columnCoverage[texture.height - py - 1]++;
                        int[] localRows = GutterCuts(columnCoverage, rows, true);
                        cellBottom = texture.height - localRows[row + 1]; cellTop = texture.height - localRows[row];
                    }
                    cells[index] = new RectInt(x, cellBottom, right - x, cellTop - cellBottom);
                    int left = right, bottom = cellTop, maxX = x, maxY = cellBottom;
                    for (int py = cellBottom; py < cellTop; py++) for (int px = x; px < right; px++)
                        if (pixels[py * texture.width + px].a >= 80)
                        { left = Mathf.Min(left, px); bottom = Mathf.Min(bottom, py); maxX = Mathf.Max(maxX, px); maxY = Mathf.Max(maxY, py); }
                    bounds[index] = left < maxX ? new RectInt(left, bottom, maxX - left + 1, maxY - bottom + 1) : new RectInt(x, cellBottom, right - x, cellTop - cellBottom);
                }
            }
            var heights = new List<int>(); for (int i = 0; i < columns; i++) heights.Add(bounds[i].height); heights.Sort();
            float ppu = effects ? texture.height / (float)rows / (3.8f * 1.2f) : heights[heights.Count / 2] / owner.visibleHeight;
            var sprites = new Sprite[cells.Length];
            for (int row = 0; row < rows; row++)
            {
                float rowPpu = ppu;
                if (name == "Idle" || name == "Walk" || name == "Stairs")
                {
                    heights.Clear(); for (int c = 0; c < columns; c++) heights.Add(bounds[row * columns + c].height); heights.Sort();
                    rowPpu = heights[heights.Count / 2] / owner.visibleHeight;
                }
                Vector2 firstFoot = FootAnchor(pixels, texture.width, bounds[row * columns]);
                for (int col = 0; col < columns; col++)
                {
                    int i = row * columns + col; var cell = cells[i]; var b = bounds[i];
                    Vector2 foot = FootAnchor(pixels, texture.width, b);
                    if (name == "Reactions" && row == 5)
                        foot = new Vector2(cell.x + firstFoot.x - cells[row * columns].x, firstFoot.y);
                    int bottom = Mathf.Min(b.yMin, Mathf.FloorToInt(foot.y));
                    Rect rect = effects ? new Rect(cell.x, cell.y, cell.width, cell.height) : new Rect(b.x, bottom, b.width, b.yMax - bottom);
                    Vector2 pivot = effects ? Vector2.one * .5f : new Vector2(Mathf.Clamp01((foot.x - rect.x) / rect.width), (foot.y - rect.y) / rect.height);
                    Sprite sprite = Sprite.Create(texture, rect, pivot, rowPpu, 0, SpriteMeshType.FullRect);
                    sprite.name = name + "_" + row.ToString("D2") + "_" + col.ToString("D2");
                    sprites[i] = sprite; AssetDatabase.AddObjectToAsset(sprite, owner);
                }
            }
            Debug.Log("[Eurydice Look02] " + name + " " + texture.width + "x" + texture.height + " / " + sprites.Length + " frames / alpha clear " + clear);
            return sprites;
        }

        private static int[] RegularCuts(int size, int count)
        {
            var cuts = new int[count + 1];
            for (int i = 0; i <= count; i++) cuts[i] = Mathf.RoundToInt(i * size / (float)count);
            return cuts;
        }

        private static int[] GutterCuts(int[] coverage, int count, bool fitContent)
        {
            int first = 0, last = coverage.Length - 1;
            if (fitContent)
            {
                while (first < last && coverage[first] < count * 2) first++;
                while (last > first && coverage[last] < count * 2) last--;
            }
            float spacing = (last - first + 1f) / count;
            var cuts = new int[count + 1]; cuts[count] = coverage.Length;
            for (int i = 1; i < count; i++)
            {
                int target = Mathf.RoundToInt(first + spacing * i), best = target, bestScore = int.MaxValue;
                int start = Mathf.Max(cuts[i - 1] + Mathf.RoundToInt(spacing * .5f), target - Mathf.RoundToInt(spacing * .3f));
                int end = Mathf.Min(coverage.Length - 2, target + Mathf.RoundToInt(spacing * .3f));
                for (int p = start; p <= end; p++)
                {
                    int score = coverage[p] + coverage[p - 1];
                    if (score < bestScore || (score == bestScore && Mathf.Abs(p - target) < Mathf.Abs(best - target)))
                    { best = p; bestScore = score; }
                }
                cuts[i] = best;
            }
            return cuts;
        }

        private static Vector2 FootAnchor(Color32[] pixels, int width, RectInt bounds)
        {
            int firstSkin = -1, count = 0; float xSum = 0;
            for (int y = bounds.yMin; y < bounds.yMin + bounds.height * .28f; y++)
            {
                if (firstSkin >= 0 && y > firstSkin + 4) break;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Color32 c = pixels[y * width + x];
                    if (c.a > 128 && c.r > 85 && c.g > 50 && c.r > c.g * 1.12f && c.g > c.b * 1.05f)
                    { if (firstSkin < 0) firstSkin = y; count++; xSum += x; }
                }
            }
            return count > 0 ? new Vector2(xSum / count, firstSkin) : new Vector2(bounds.center.x, bounds.yMin);
        }
    }
}
