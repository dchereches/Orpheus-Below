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
            sheets["WalkCycle"] = BakeSheet("WalkCycle", library, false);
            sheets["RunCycle"] = BakeSheet("RunCycle", library, false);
            sheets["BilateralRun"] = OrpheusBilateralRunBaker.Bake(sheets["RunCycle"], library);
            var sourceFrames = new List<Sprite>();
            foreach (Sprite[] sheetFrames in sheets.Values) sourceFrames.AddRange(sheetFrames);
            library.sourceFrames = sourceFrames.ToArray();
            library.effects = BakeSheet("Effects", library, true);

            var clips = new List<OrpheusAnimationClip>();
            Add(clips, sheets, OrpheusAnimationState.Idle, "IdleTransitions", 0, true, 6f);
            Add(clips, sheets, OrpheusAnimationState.RestlessIdle, "IdleTransitions", 2, true, 6f);
            AddCycle(clips, sheets, OrpheusAnimationState.Start, "WalkCycle", false, 40f, 0, 1, 2, 3);
            AddCycle(clips, sheets, OrpheusAnimationState.Walk, "WalkCycle", true, 8f, 0, 1, 2, 3);
            AddRun(clips, sheets["BilateralRun"]);
            AddSequence(clips, sheets, OrpheusAnimationState.Stop, "IdleTransitions", false, 45f, 5, 6, 0, 0);
            AddCycle(clips, sheets, OrpheusAnimationState.Turn, "WalkCycle", false, 40f, 1, 3, 1, 3);

            AddCycle(clips, sheets, OrpheusAnimationState.DashStart, "RunCycle", false, 90f, 1, 0, 1, 0);
            AddCycle(clips, sheets, OrpheusAnimationState.DashTravel, "RunCycle", true, 30f, 1, 3, 1, 3);
            AddCycle(clips, sheets, OrpheusAnimationState.DashSettle, "WalkCycle", false, 60f, 1, 0, 1, 0);
            AddSequence(clips, sheets, OrpheusAnimationState.DashStrike, "SoftCombat", false, 24f, 5, 6, 7, 1);
            AddCycle(clips, sheets, OrpheusAnimationState.StairAscent, "WalkCycle", true, 8f, 0, 1, 2, 3);
            AddCycle(clips, sheets, OrpheusAnimationState.StairDescent, "WalkCycle", true, 8f, 0, 1, 2, 3);

            AddSequence(clips, sheets, OrpheusAnimationState.SoftStrike1, "SoftCombat", false, 24f, 1, 2, 3, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.SoftStrike2, "SoftCombat", false, 24f, 3, 4, 5, 3);
            AddSequence(clips, sheets, OrpheusAnimationState.SoftStrike3, "SoftCombat", false, 24f, 5, 6, 7, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.SoftBolt, "SoftCombat", false, 20f, 5, 6, 7, 7);
            AddSequence(clips, sheets, OrpheusAnimationState.HardBoomWindup, "HardCadence", false, 60f, 0, 1, 1, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.HardBoomRelease, "HardCadence", false, 20f, 2, 2, 7, 0);
            AddVocal(clips, sheets, OrpheusAnimationState.HymnBeam, false, 12f, 3, 6, 5, 7);
            AddVocal(clips, sheets, OrpheusAnimationState.SanctuaryCast, false, 12f, 3, 4, 6, 7);
            AddVocal(clips, sheets, OrpheusAnimationState.ConvergenceCast, false, 12f, 4, 6, 5, 7);

            AddVocal(clips, sheets, OrpheusAnimationState.LyreDraw, false, 16f, 0, 1, 2, 1);
            AddVocal(clips, sheets, OrpheusAnimationState.SoftPluck, false, 16f, 1, 2, 1, 7);
            AddVocal(clips, sheets, OrpheusAnimationState.SingSustain, true, 6f, 3, 4, 5, 4);
            AddVocal(clips, sheets, OrpheusAnimationState.FullVoiceRelease, false, 20f, 3, 4, 6, 5);
            AddVocal(clips, sheets, OrpheusAnimationState.PhraseResolve, false, 14f, 5, 4, 3, 7);

            AddSequence(clips, sheets, OrpheusAnimationState.AulosDraw, "Aulos", false, 8f, 0, 0, 1, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.AulosLoop, "Aulos", true, 7f, 2, 3, 2, 3);
            AddSequence(clips, sheets, OrpheusAnimationState.AulosNoteSoft, "Aulos", false, 16f, 2, 4, 3, 2);
            AddSequence(clips, sheets, OrpheusAnimationState.AulosNoteHard, "Aulos", false, 16f, 3, 5, 3, 2);
            AddSequence(clips, sheets, OrpheusAnimationState.Discord, "Aulos", false, 14f, 5, 6, 6, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.Requiem, "Aulos", false, 12f, 3, 5, 7, 2);

            AddSequence(clips, sheets, OrpheusAnimationState.BreathExhausted, "Reactions", true, 4f, 0, 1, 0, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.BreathRecover, "Reactions", false, 10f, 0, 1, 1, 7);
            AddSequence(clips, sheets, OrpheusAnimationState.LightHit, "Reactions", false, 24f, 2, 2, 1, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.HeavyHit, "Reactions", false, 16f, 2, 3, 4, 4);
            AddFixed(clips, sheets, OrpheusAnimationState.GroundedHurt, "Reactions", 4, true, 5f);
            AddSequence(clips, sheets, OrpheusAnimationState.Rise, "Reactions", false, 12f, 4, 5, 1, 7);
            AddSequence(clips, sheets, OrpheusAnimationState.Alert, "Reactions", false, 16f, 1, 7, 7, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.Defeat, "Reactions", false, 8f, 3, 4, 6, 6);

            AddSequence(clips, sheets, OrpheusAnimationState.ChamberEntrance, "Narrative", false, 12f, 0, 1, 0, 1);
            AddSequence(clips, sheets, OrpheusAnimationState.Inspect, "Narrative", false, 10f, 0, 2, 2, 0);
            AddSequence(clips, sheets, OrpheusAnimationState.LookBackTemptation, "Narrative", false, 12f, 0, 3, 3, 0);
            AddSequence(clips, sheets, OrpheusAnimationState.LookBackFatal, "Narrative", false, 10f, 3, 4, 4, 4);
            AddSequence(clips, sheets, OrpheusAnimationState.EurydiceGrief, "Narrative", false, 5f, 4, 5, 5, 5);
            AddSequence(clips, sheets, OrpheusAnimationState.ChamberClear, "Narrative", false, 12f, 0, 6, 7, 6);
            AddSequence(clips, sheets, OrpheusAnimationState.Victory, "Narrative", false, 8f, 0, 6, 7, 7);

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
                frames = frames, fps = fps, loop = loop, embeddedEffects = sheet == "SoftCombat" || sheet == "HardCadence" || sheet == "Aulos"
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
                frames = frames, fps = fps, loop = loop, embeddedEffects = sheet == "SoftCombat" || sheet == "HardCadence" || sheet == "Aulos"
            });
        }

        private static Sprite SourceFrame(Dictionary<string, Sprite[]> sheets, string sheet, int row, int column)
        {
            return sheets[sheet][row * Columns + column];
        }

        private static void AddCycle(List<OrpheusAnimationClip> clips, Dictionary<string, Sprite[]> sheets,
            OrpheusAnimationState state, string sheet, bool loop, float fps, params int[] columns)
        {
            int[] sourceRows = { 0, 1, 2, 3, 4, 3, 2, 1 };
            var frames = new Sprite[8 * columns.Length];
            for (int d = 0; d < 8; d++) for (int f = 0; f < columns.Length; f++)
                frames[d * columns.Length + f] = sheets[sheet][sourceRows[d] * 4 + columns[f]];
            clips.Add(new OrpheusAnimationClip { state = state, frames = frames, directions = 8,
                framesPerDirection = columns.Length, fps = fps, loop = loop, mirrorWest = true });
        }

        private static void AddRun(List<OrpheusAnimationClip> clips, Sprite[] source)
        {
            int[] sourceRows = { 0, 1, 2, 3, 4, 3, 2, 1 };
            var frames = new Sprite[64];
            for (int direction = 0; direction < 8; direction++)
                for (int frame = 0; frame < 8; frame++) frames[direction * 8 + frame] = source[sourceRows[direction] * 8 + frame];
            clips.Add(new OrpheusAnimationClip { state = OrpheusAnimationState.Run, frames = frames,
                directions = 8, framesPerDirection = 8, fps = 16f, loop = true, mirrorWest = true });
        }

        private static void AddVocal(List<OrpheusAnimationClip> clips, Dictionary<string, Sprite[]> sheets,
            OrpheusAnimationState state, bool loop, float fps, params int[] columns)
        {
            int[] sourceRows = { 0, 1, 2, 3, 4, 3, 2, 1 };
            var frames = new Sprite[8 * columns.Length];
            for (int d = 0; d < 8; d++) for (int f = 0; f < columns.Length; f++)
                frames[d * columns.Length + f] = sheets["LyreSinging"][sourceRows[d] * 8 + columns[f]];
            clips.Add(new OrpheusAnimationClip { state = state, frames = frames, directions = 8,
                framesPerDirection = columns.Length, fps = fps, loop = loop, mirrorWest = true });
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

            int columns = name.EndsWith("Cycle") ? 4 : Columns;
            int rows = name.EndsWith("Cycle") ? 5 : name == "LyreSinging" ? 7 : Rows;
            Sprite[] sprites = OrpheusAtlasSlicer.Bake(texture, name, owner, columns, rows, effects);
            Debug.Log($"[Orpheus Broad Oil] {name}: {columns}x{rows} isolated source poses.");
            return sprites;
        }
    }
}
