using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    public static class PaintedBestiaryAssetBuilder
    {
        [Serializable] private class Frame { public int row, column; public float[] rectUnity, pivot; }
        [Serializable] private class Sheet { public Frame[] frames; }
        [Serializable] private class AtlasLayout { public int bodyColumns = 6, effectColumns = 6; public Sheet body, effects; }
        private const string Folder = "Assets/OrpheusBelow/Resources/Enemies/";

        [MenuItem("Orpheus Below/Bake Painted Bestiary")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            foreach (PaintedEnemyKind kind in Enum.GetValues(typeof(PaintedEnemyKind)))
            {
                string root = Folder + kind + "/";
                var layout = File.Exists(root + "AtlasLayout.json") ? JsonUtility.FromJson<AtlasLayout>(File.ReadAllText(root + "AtlasLayout.json")) : new AtlasLayout();
                if (!File.Exists(root + "AtlasLayout.json"))
                    layout.bodyColumns = kind == PaintedEnemyKind.HoodedSkeleton || kind == PaintedEnemyKind.MaskedMonk ? 6 : 4;
                string path = root + "Library.asset";
                var library = AssetDatabase.LoadAssetAtPath<EnemyArtLibrary>(path);
                if (library == null) { library = ScriptableObject.CreateInstance<EnemyArtLibrary>(); AssetDatabase.CreateAsset(library, path); }
                else foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite) Object.DestroyImmediate(asset, true);
                library.kind = kind; library.bodyColumns = layout.bodyColumns; library.effectColumns = layout.effectColumns;
                library.visibleHeight = kind == PaintedEnemyKind.HoodedSkeleton ? 2.35f : kind == PaintedEnemyKind.RoosterRider ? 2.8f
                    : kind == PaintedEnemyKind.Sheep ? 1.35f : kind == PaintedEnemyKind.Tripod ? 4.4f
                    : kind == PaintedEnemyKind.WingedSentinel ? 4.1f : kind == PaintedEnemyKind.Cerberus ? 3.4f : 3.3f;
                library.motionStarts = new int[8]; library.motionCounts = new int[8];
                library.body = Bake(root + "Body.png", layout.bodyColumns, 8, layout.body?.frames, library, false);
                library.effects = Bake(root + "Effects.png", layout.effectColumns, 4, layout.effects?.frames, library, true);
                if (!library.IsComplete) throw new InvalidOperationException("Incomplete " + kind);
                EditorUtility.SetDirty(library);
                Debug.Log("[Painted bestiary] Baked " + kind + ": " + library.body.Length + " body / " + library.effects.Length + " FX frames.");
            }
            // Only encounter metadata changes; existing saved collision meshes and scenes remain valid.
            for (int i = 0; i < UnderworldJourney.ChamberCount; i++)
            {
                var chamber = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + i.ToString("D2"));
                chamber.hasGuardian = i >= 1 && i <= 6;
                chamber.guardian = i == 1 ? BossIdentity.Tripod : i == 2 ? BossIdentity.MaskedMonk : i == 3 ? BossIdentity.MysticEyeWarden
                    : i == 4 ? BossIdentity.WingedSentinel : i == 5 ? BossIdentity.Shepherd : BossIdentity.CerberusHoundOfHades;
                EditorUtility.SetDirty(chamber);
            }
            AssetDatabase.SaveAssets(); Debug.Log("[Painted bestiary] BAKE PASS: all nine actors, saved sprites and six guardian encounters.");
        }

        private static Sprite[] Bake(string path, int columns, int rows, Frame[] authored, EnemyArtLibrary owner, bool effects)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException(path);
            importer.textureType = TextureImporterType.Default; importer.isReadable = true; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var pixels = texture.GetPixels32(); int clear = 0; foreach (var pixel in pixels) if (pixel.a < 10) clear++;
            if (clear < pixels.Length / 5) throw new InvalidOperationException(path + " does not have genuine alpha.");
            var frames = new List<Frame>();
            if (authored != null && authored.Length > 0) frames.AddRange(authored);
            else
            {
                var yCoverage = new int[texture.height];
                for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a > 100) yCoverage[texture.height - y - 1]++;
                int[] rowCuts = effects ? RegularCuts(texture.height, rows) : Cuts(yCoverage, rows);
                for (int row = 0; row < rows; row++)
                {
                    int bottom = texture.height - rowCuts[row + 1], top = texture.height - rowCuts[row];
                    var xCoverage = new int[texture.width];
                    for (int y = bottom; y < top; y++) for (int x = 0; x < texture.width; x++) if (pixels[y * texture.width + x].a > 100) xCoverage[x]++;
                    int[] colCuts = effects ? RegularCuts(texture.width, columns) : Cuts(xCoverage, columns);
                    for (int col = 0; col < columns; col++)
                        frames.Add(new Frame { row = row, column = col, rectUnity = new float[] { colCuts[col], bottom, colCuts[col + 1] - colCuts[col], top - bottom } });
                }
            }
            frames.Sort((a,b) => a.row == b.row ? a.column.CompareTo(b.column) : a.row.CompareTo(b.row));
            var bounds = new List<RectInt>(); var idleHeights = new List<int>();
            foreach (var frame in frames)
            {
                if (frame.row < 0 || frame.row >= rows || frame.rectUnity == null || frame.rectUnity.Length != 4) throw new InvalidDataException(path + " has invalid frame metadata.");
                var r = frame.rectUnity; int left = Mathf.RoundToInt(r[0]), bottom = Mathf.RoundToInt(r[1]);
                int right = Mathf.RoundToInt(r[0] + r[2]), top = Mathf.RoundToInt(r[1] + r[3]);
                if (left < 0 || bottom < 0 || right > texture.width || top > texture.height) throw new InvalidDataException(path + " frame outside source pixels.");
                int minX = right, minY = top, maxX = left, maxY = bottom;
                for (int y = bottom; y < top; y++) for (int x = left; x < right; x++) if (pixels[y * texture.width + x].a > 100)
                { minX = Mathf.Min(minX,x); minY = Mathf.Min(minY,y); maxX = Mathf.Max(maxX,x); maxY = Mathf.Max(maxY,y); }
                if (minX >= maxX || minY >= maxY) throw new InvalidDataException(path + " empty frame row " + frame.row + " col " + frame.column);
                var b = new RectInt(minX,minY,maxX-minX+1,maxY-minY+1); bounds.Add(b);
                if (frame.row == 0) idleHeights.Add(b.height);
            }
            idleHeights.Sort(); float ppu = effects ? texture.width / (float)columns : idleHeights[idleHeights.Count / 2] / owner.visibleHeight;
            var result = new List<Sprite>();
            for (int i = 0; i < frames.Count; i++)
            {
                var f = frames[i]; var b = bounds[i]; var r = f.rectUnity;
                Rect rect = new Rect(r[0],r[1],r[2],r[3]);
                Vector2 pivot = effects ? Vector2.one * .5f : new Vector2((b.center.x - rect.x) / rect.width, (b.yMin - rect.y) / rect.height);
                if (f.pivot?.Length == 2) pivot = new Vector2(f.pivot[0],f.pivot[1]);
                var sprite = Sprite.Create(texture, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
                sprite.name = owner.kind + "_" + (effects ? "FX" + f.row : ((EnemyMotion)f.row).ToString()) + "_" + f.column.ToString("D2");
                AssetDatabase.AddObjectToAsset(sprite, owner); result.Add(sprite);
                if (!effects) { if (owner.motionCounts[f.row] == 0) owner.motionStarts[f.row] = i; owner.motionCounts[f.row]++; }
            }
            if (!effects) foreach (int count in owner.motionCounts) if (count < 2) throw new InvalidDataException(path + " needs at least two clean frames for each motion.");
            return result.ToArray();
        }
        private static int[] RegularCuts(int length, int count)
        { var cuts = new int[count+1]; for (int i = 0; i <= count; i++) cuts[i] = Mathf.RoundToInt(length * i / (float)count); return cuts; }
        private static int[] Cuts(int[] coverage, int count)
        {
            var cuts = RegularCuts(coverage.Length,count); float spacing = coverage.Length / (float)count;
            for (int i = 1; i < count; i++)
            {
                int target = cuts[i], best = target; float score = float.MaxValue;
                for (int p = Mathf.Max(cuts[i-1]+2,Mathf.RoundToInt(target-spacing*.23f)); p < Mathf.Min(coverage.Length-1,target+spacing*.23f); p++)
                {
                    float value = coverage[p] + coverage[p-1] + Mathf.Abs(p-target)*.01f;
                    if (value < score) { best = p; score = value; }
                }
                cuts[i] = best;
            }
            return cuts;
        }
    }
}
