using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    public static class OrpheusBroadOilAssetBuilder
    {
        private const string Folder = "Assets/OrpheusBelow/Resources/Characters/Orpheus/BroadOil/";
        private const int Columns = 8;
        private const int Rows = 8;

        [MenuItem("Orpheus Below/Bake Orpheus Broad Oil Atlas")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            string libraryPath = Folder + "Library.asset";
            var library = AssetDatabase.LoadAssetAtPath<OrpheusAnimationLibrary>(libraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<OrpheusAnimationLibrary>();
                AssetDatabase.CreateAsset(library, libraryPath);
            }
            else
            {
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(libraryPath))
                    if (asset is Sprite) Object.DestroyImmediate(asset, true);
            }

            library.visibleHeight = 3.9f;
            var sheets = new Dictionary<string, Sprite[]>();
            foreach (string name in new[] { "IdleTransitions", "Walk", "Run", "DashStairs", "SoftCombat",
                "HardCadence", "LyreSinging", "Aulos", "Reactions", "Narrative" })
                sheets[name] = BakeSheet(name, library, false);
            var sourceFrames = new List<Sprite>();
            foreach (Sprite[] sheetFrames in sheets.Values) sourceFrames.AddRange(sheetFrames);
            library.sourceFrames = sourceFrames.ToArray();
            library.effects = BakeSheet("Effects", library, true);

            var clips = new List<OrpheusAnimationClip>();
            Add(clips, sheets, OrpheusAnimationState.Idle, "IdleTransitions", 0, true, 6f);
            Add(clips, sheets, OrpheusAnimationState.RestlessIdle, "IdleTransitions", 2, true, 6f);
            AddSequence(clips, sheets, OrpheusAnimationState.Start, "IdleTransitions", false, 9f, 4, 4, 5, 5);
            Add(clips, sheets, OrpheusAnimationState.Walk, "Walk", 0, true, 9f, 8);
            Add(clips, sheets, OrpheusAnimationState.Run, "Run", 0, true, 12f, 8);
            AddSequence(clips, sheets, OrpheusAnimationState.Stop, "IdleTransitions", false, 8f, 5, 6, 6, 6);
            AddFixed(clips, sheets, OrpheusAnimationState.Turn, "IdleTransitions", 7, false, 8f);

            AddSequence(clips, sheets, OrpheusAnimationState.DashStart, "DashStairs", false, 14f, 0, 0, 1, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.DashTravel, "DashStairs", true, 16f, 2, 3, 2, 3);
            AddSequence(clips, sheets, OrpheusAnimationState.DashSettle, "DashStairs", false, 12f, 5, 5, 5, 5);
            AddSequence(clips, sheets, OrpheusAnimationState.DashStrike, "DashStairs", false, 14f, 3, 4, 4, 5);
            AddSequence(clips, sheets, OrpheusAnimationState.StairAscent, "DashStairs", true, 8f, 6, 6, 6, 6);
            AddSequence(clips, sheets, OrpheusAnimationState.StairDescent, "DashStairs", true, 8f, 7, 7, 7, 7);

            Add(clips, sheets, OrpheusAnimationState.SoftStrike1, "SoftCombat", 0, false, 13f);
            Add(clips, sheets, OrpheusAnimationState.SoftStrike2, "SoftCombat", 2, false, 13f);
            Add(clips, sheets, OrpheusAnimationState.SoftStrike3, "SoftCombat", 4, false, 12f);
            AddSequence(clips, sheets, OrpheusAnimationState.SoftBolt, "SoftCombat", false, 12f, 5, 6, 7, 7);
            AddSequence(clips, sheets, OrpheusAnimationState.HardBoomWindup, "HardCadence", false, 8f, 0, 0, 1, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.HardBoomRelease, "HardCadence", false, 11f, 1, 2, 2, 2);
            AddSequence(clips, sheets, OrpheusAnimationState.HymnBeam, "HardCadence", false, 10f, 3, 3, 3, 3);
            AddSequence(clips, sheets, OrpheusAnimationState.SanctuaryCast, "HardCadence", false, 9f, 4, 4, 4, 4);
            AddSequence(clips, sheets, OrpheusAnimationState.ConvergenceCast, "HardCadence", false, 10f, 5, 5, 5, 5);

            AddSequence(clips, sheets, OrpheusAnimationState.LyreDraw, "SoftCombat", false, 8f, 0, 0, 1, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.SoftPluck, "SoftCombat", false, 10f, 0, 1, 2, 3);
            AddFixed(clips, sheets, OrpheusAnimationState.SingSustain, "HardCadence", 6, true, 7f);
            AddSequence(clips, sheets, OrpheusAnimationState.FullVoiceRelease, "HardCadence", false, 9f, 5, 6, 6, 6);
            AddSequence(clips, sheets, OrpheusAnimationState.PhraseResolve, "HardCadence", false, 7f, 6, 7, 7, 7);

            AddSequence(clips, sheets, OrpheusAnimationState.AulosDraw, "Aulos", false, 8f, 0, 0, 1, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.AulosLoop, "Aulos", true, 7f, 2, 3, 2, 3);
            AddSequence(clips, sheets, OrpheusAnimationState.AulosNoteSoft, "Aulos", false, 10f, 4, 4, 4, 4);
            AddSequence(clips, sheets, OrpheusAnimationState.AulosNoteHard, "Aulos", false, 11f, 5, 5, 5, 5);
            AddSequence(clips, sheets, OrpheusAnimationState.Discord, "Aulos", false, 11f, 6, 6, 6, 6);
            AddSequence(clips, sheets, OrpheusAnimationState.Requiem, "Aulos", false, 8f, 7, 7, 7, 7);

            AddFixed(clips, sheets, OrpheusAnimationState.BreathExhausted, "Reactions", 0, true, 5f);
            AddFixed(clips, sheets, OrpheusAnimationState.BreathRecover, "Reactions", 1, false, 7f);
            AddFixed(clips, sheets, OrpheusAnimationState.LightHit, "Reactions", 2, false, 11f);
            AddFixed(clips, sheets, OrpheusAnimationState.HeavyHit, "Reactions", 3, false, 10f);
            AddFixed(clips, sheets, OrpheusAnimationState.GroundedHurt, "Reactions", 4, true, 5f);
            AddFixed(clips, sheets, OrpheusAnimationState.Rise, "Reactions", 5, false, 7f);
            AddFixed(clips, sheets, OrpheusAnimationState.Alert, "Reactions", 7, false, 8f);
            AddFixed(clips, sheets, OrpheusAnimationState.Defeat, "Reactions", 6, false, 6f);

            AddFixed(clips, sheets, OrpheusAnimationState.ChamberEntrance, "Narrative", 0, false, 7f);
            AddFixed(clips, sheets, OrpheusAnimationState.Inspect, "Narrative", 2, false, 6f);
            AddFixed(clips, sheets, OrpheusAnimationState.LookBackTemptation, "Narrative", 3, false, 6f);
            AddFixed(clips, sheets, OrpheusAnimationState.LookBackFatal, "Narrative", 4, false, 7f);
            AddFixed(clips, sheets, OrpheusAnimationState.EurydiceGrief, "Narrative", 5, true, 5f);
            AddFixed(clips, sheets, OrpheusAnimationState.ChamberClear, "Narrative", 6, false, 7f);
            AddFixed(clips, sheets, OrpheusAnimationState.Victory, "Narrative", 7, true, 6f);

            library.clips = clips.ToArray();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int installed = 0;
            foreach (OrpheusAnimationClip clip in library.clips) installed += clip.frames.Length;
            Debug.Log($"[Orpheus Broad Oil] BAKE PASS: {library.clips.Length} motions, {installed} installed body slots, " +
                $"{library.effects.Length} effect frames, 3.9-unit enemy-scale silhouette.");
        }

        private static void Add(List<OrpheusAnimationClip> clips, Dictionary<string, Sprite[]> sheets,
            OrpheusAnimationState state, string sheet, int startColumn, bool loop, float fps, int frameCount = 4)
        {
            int directions = OrpheusAnimationLibrary.DirectionCount(state);
            var frames = new Sprite[directions * frameCount];
            for (int direction = 0; direction < directions; direction++)
            {
                int sourceRow = directions == 8 ? direction : direction * 2;
                for (int frame = 0; frame < frameCount; frame++)
                    frames[direction * frameCount + frame] = SourceFrame(sheets, sheet, sourceRow, (startColumn + frame) % Columns);
            }
            clips.Add(new OrpheusAnimationClip
            {
                state = state, directions = directions, framesPerDirection = frameCount,
                frames = frames, fps = fps, loop = loop
            });
        }

        private static void AddFixed(List<OrpheusAnimationClip> clips, Dictionary<string, Sprite[]> sheets,
            OrpheusAnimationState state, string sheet, int column, bool loop, float fps)
            => AddSequence(clips, sheets, state, sheet, loop, fps, column, column, column, column);

        private static void AddSequence(List<OrpheusAnimationClip> clips, Dictionary<string, Sprite[]> sheets,
            OrpheusAnimationState state, string sheet, bool loop, float fps, params int[] columns)
        {
            int directions = OrpheusAnimationLibrary.DirectionCount(state);
            var frames = new Sprite[directions * columns.Length];
            for (int direction = 0; direction < directions; direction++)
            {
                int sourceRow = directions == 8 ? direction : direction * 2;
                for (int frame = 0; frame < columns.Length; frame++)
                    frames[direction * columns.Length + frame] = SourceFrame(sheets, sheet, sourceRow, columns[frame]);
            }
            clips.Add(new OrpheusAnimationClip
            {
                state = state, directions = directions, framesPerDirection = columns.Length,
                frames = frames, fps = fps, loop = loop
            });
        }

        private static Sprite SourceFrame(Dictionary<string, Sprite[]> sheets, string sheet, int row, int column)
        {
            // The generated north-facing settle cell overlaps its neighbour despite a measured gutter.
            // Use the adjacent authored north settle/stair pose instead of shipping a clipped paint fragment.
            if (sheet == "DashStairs" && row == 4 && column == 5) column = 6;
            return sheets[sheet][row * Columns + column];
        }

        private static Sprite[] BakeSheet(string name, OrpheusAnimationLibrary owner, bool effects)
        {
            string path = Folder + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing " + path);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Color32[] pixels = texture.GetPixels32();
            int clear = 0;
            foreach (Color32 pixel in pixels) if (pixel.a < 10) clear++;
            if (clear < pixels.Length / 5) throw new InvalidOperationException(name + " needs a real transparent background.");

            var bounds = new RectInt[Columns * Rows];
            var uprightHeights = new List<int>();
            var rowCoverage = new int[texture.height];
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a >= 24) rowCoverage[texture.height - 1 - y]++;
            bool fixedGrid = name == "LyreSinging";
            int[] rowCuts = fixedGrid ? RegularCuts(texture.height, Rows) : GutterCuts(rowCoverage, Rows, false);
            for (int topRow = 0; topRow < Rows; topRow++)
            {
                int yMin = texture.height - rowCuts[topRow + 1];
                int yMax = texture.height - rowCuts[topRow];
                var columnCoverage = new int[texture.width];
                for (int y = yMin; y < yMax; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (pixels[y * texture.width + x].a >= 24) columnCoverage[x]++;
                int[] columnCuts = fixedGrid ? RegularCuts(texture.width, Columns) : GutterCuts(columnCoverage, Columns, false);
                for (int column = 0; column < Columns; column++)
                {
                    int xMin = columnCuts[column];
                    int xMax = columnCuts[column + 1];
                    int cellYMin = yMin;
                    int cellYMax = yMax;
                    if (!fixedGrid)
                    {
                        var localRows = new int[texture.height];
                        for (int y = 0; y < texture.height; y++)
                            for (int x = xMin; x < xMax; x++)
                                if (pixels[y * texture.width + x].a >= 24) localRows[texture.height - 1 - y]++;
                        int[] localCuts = GutterCuts(localRows, Rows, false);
                        cellYMin = texture.height - localCuts[topRow + 1];
                        cellYMax = texture.height - localCuts[topRow];
                    }
                    int left = xMax, right = xMin, bottom = cellYMax, top = cellYMin;
                    for (int y = cellYMin; y < cellYMax; y++)
                        for (int x = xMin; x < xMax; x++)
                            if (pixels[y * texture.width + x].a >= 24)
                            {
                                left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                                bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                            }
                    if (left > right)
                    {
                        left = xMin; right = xMax - 1; bottom = cellYMin; top = cellYMax - 1;
                    }
                    left = Mathf.Max(xMin, left - 2); right = Mathf.Min(xMax - 1, right + 2);
                    bottom = Mathf.Max(cellYMin, bottom - 2); top = Mathf.Min(cellYMax - 1, top + 2);
                    var bound = new RectInt(left, bottom, right - left + 1, top - bottom + 1);
                    bounds[topRow * Columns + column] = bound;
                    if (!effects && bound.height > (cellYMax - cellYMin) * .45f) uprightHeights.Add(bound.height);
                }
            }
            uprightHeights.Sort();
            float ppu = effects ? texture.height / (Rows * 4.6f) :
                uprightHeights[Mathf.Clamp(uprightHeights.Count / 2, 0, uprightHeights.Count - 1)] / owner.visibleHeight;
            var sprites = new Sprite[bounds.Length];
            for (int row = 0; row < Rows; row++)
                for (int column = 0; column < Columns; column++)
                {
                    int index = row * Columns + column;
                    RectInt bound = bounds[index];
                    Vector2 pivot = effects ? Vector2.one * .5f : new Vector2(.5f, .02f);
                    Sprite sprite = Sprite.Create(texture, new Rect(bound.x, bound.y, bound.width, bound.height),
                        pivot, ppu, 0, SpriteMeshType.FullRect);
                    sprite.name = name + "_" + row.ToString("D2") + "_" + column.ToString("D2");
                    sprites[index] = sprite;
                    AssetDatabase.AddObjectToAsset(sprite, owner);
                }
            Debug.Log($"[Orpheus Broad Oil] {name}: {texture.width}x{texture.height}, 64 alpha-cut cells, {clear} clear pixels.");
            return sprites;
        }

        private static int[] GutterCuts(int[] coverage, int count, bool fitContent)
        {
            int first = 0, last = coverage.Length - 1;
            if (fitContent)
            {
                while (first < last && coverage[first] < count) first++;
                while (last > first && coverage[last] < count) last--;
            }
            float spacing = (last - first + 1f) / count;
            var cuts = new int[count + 1];
            cuts[0] = 0; cuts[count] = coverage.Length;
            for (int i = 1; i < count; i++)
            {
                int target = Mathf.RoundToInt(first + spacing * i);
                int radius = Mathf.Max(3, Mathf.RoundToInt(spacing * .28f));
                int start = Mathf.Max(cuts[i - 1] + 2, target - radius);
                int end = Mathf.Min(coverage.Length - 2, target + radius);
                int best = Mathf.Clamp(target, start, end), bestScore = int.MaxValue;
                for (int p = start; p <= end; p++)
                {
                    int score = coverage[p - 1] + coverage[p] + coverage[p + 1];
                    if (score < bestScore || score == bestScore && Mathf.Abs(p - target) < Mathf.Abs(best - target))
                    { best = p; bestScore = score; }
                }
                cuts[i] = best;
            }
            return cuts;
        }

        private static int[] RegularCuts(int size, int count)
        {
            var cuts = new int[count + 1];
            for (int i = 0; i <= count; i++) cuts[i] = Mathf.RoundToInt(i * size / (float)count);
            return cuts;
        }
    }
}
