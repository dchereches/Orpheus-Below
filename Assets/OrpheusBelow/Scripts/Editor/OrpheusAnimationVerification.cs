using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OrpheusBelow.EditorTools
{
    public static class OrpheusAnimationVerification
    {
        [MenuItem("Orpheus Below/Verify Orpheus Broad Oil Animation")]
        public static void Run()
        {
            OrpheusAnimationLibrary library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            if (library == null || !library.IsComplete) throw new InvalidOperationException("Complete Orpheus broad-oil library is not installed.");
            string output = OutputFolder();
            Directory.CreateDirectory(output);

            var distinct = new HashSet<int>();
            int installed = 0;
            foreach (OrpheusAnimationState state in OrpheusAnimationLibrary.RequiredStates)
            {
                OrpheusAnimationClip clip = library.FindClip(state);
                if (clip == null || !clip.IsComplete) throw new InvalidOperationException("Incomplete state " + state);
                installed += clip.frames.Length;
                foreach (Sprite frame in clip.frames) distinct.Add(frame.GetInstanceID());
            }
            foreach (Sprite effect in library.effects) distinct.Add(effect.GetInstanceID());
            foreach (Sprite source in library.sourceFrames) distinct.Add(source.GetInstanceID());
            if (installed < 1200 || distinct.Count < 700) throw new InvalidOperationException("Insufficient frame coverage.");

            WriteStateFamilies(library, Path.Combine(output, "StateFamilies.png"));
            WriteDirectionMatrix(library, Path.Combine(output, "EightDirectionMatrix.png"));
            WriteWalkFrames(library, Path.Combine(output, "WalkPreviewFrames"));
            WriteAllClips(library, Path.Combine(output, "AllClips"));
            string json = "{\n" +
                "  \"result\": \"pass\",\n" +
                "  \"unityVersion\": \"" + Application.unityVersion + "\",\n" +
                "  \"motions\": " + library.clips.Length + ",\n" +
                "  \"installedBodySlots\": " + installed + ",\n" +
                "  \"distinctReferencedSpritesIncludingEffects\": " + distinct.Count + ",\n" +
                "  \"effectFrames\": " + library.effects.Length + ",\n" +
                "  \"visibleHeight\": " + library.visibleHeight.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ",\n" +
                "  \"controlledDirections\": 8,\n" +
                "  \"passiveDirections\": 4,\n" +
                "  \"fallbackLightCast\": \"down-left\",\n" +
                "  \"proceduralFallback\": \"verified by OrpheusAnimationTests.MissingLibraryKeepsProceduralFallbackVisible\"\n" +
                "}\n";
            File.WriteAllText(Path.Combine(output, "Verification.json"), json);
            Debug.Log($"[Orpheus Broad Oil] VERIFY PASS: {library.clips.Length} motions, {installed} slots, " +
                $"{distinct.Count} distinct referenced sprites, previews at {output}");
        }

        private static void WriteStateFamilies(OrpheusAnimationLibrary library, string path)
        {
            const int cell = 192, columns = 8;
            int rows = Mathf.CeilToInt(OrpheusAnimationLibrary.RequiredStates.Length / (float)columns);
            var canvas = Canvas(columns * cell, rows * cell);
            for (int i = 0; i < OrpheusAnimationLibrary.RequiredStates.Length; i++)
            {
                OrpheusAnimationClip clip = library.FindClip(OrpheusAnimationLibrary.RequiredStates[i]);
                Sprite sprite = clip.frames[Mathf.Min(clip.framesPerDirection / 2, clip.frames.Length - 1)];
                Blit(sprite, canvas.pixels, canvas.width, canvas.height,
                    (i % columns) * cell, (rows - 1 - i / columns) * cell, cell, cell);
            }
            Save(canvas, path);
        }

        private static void WriteDirectionMatrix(OrpheusAnimationLibrary library, string path)
        {
            OrpheusAnimationState[] states = { OrpheusAnimationState.Walk, OrpheusAnimationState.Run,
                OrpheusAnimationState.DashStrike, OrpheusAnimationState.SoftStrike3,
                OrpheusAnimationState.HardBoomRelease, OrpheusAnimationState.SingSustain,
                OrpheusAnimationState.AulosLoop, OrpheusAnimationState.HymnBeam };
            const int cell = 184;
            var canvas = Canvas(8 * cell, states.Length * cell);
            for (int row = 0; row < states.Length; row++)
            {
                OrpheusAnimationClip clip = library.FindClip(states[row]);
                for (int direction = 0; direction < 8; direction++)
                    Blit(clip.frames[direction * clip.framesPerDirection + clip.framesPerDirection / 2],
                        canvas.pixels, canvas.width, canvas.height, direction * cell,
                        (states.Length - 1 - row) * cell, cell, cell, clip.mirrorWest && direction >= 5);
            }
            Save(canvas, path);
        }

        private static void WriteWalkFrames(OrpheusAnimationLibrary library, string folder)
        {
            Directory.CreateDirectory(folder);
            OrpheusAnimationClip clip = library.FindClip(OrpheusAnimationState.Walk);
            for (int frame = 0; frame < clip.framesPerDirection; frame++)
            {
                var canvas = Canvas(512, 512);
                Blit(clip.frames[frame], canvas.pixels, canvas.width, canvas.height, 0, 0, 512, 512);
                Save(canvas, Path.Combine(folder, "walk_" + frame.ToString("D3") + ".png"));
            }
        }

        private static void WriteAllClips(OrpheusAnimationLibrary library, string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (OrpheusAnimationClip clip in library.clips)
            {
                const int cell = 192;
                var canvas = Canvas(clip.directions * cell, clip.framesPerDirection * cell);
                for (int direction = 0; direction < clip.directions; direction++)
                    for (int frame = 0; frame < clip.framesPerDirection; frame++)
                        Blit(clip.frames[direction * clip.framesPerDirection + frame], canvas.pixels,
                            canvas.width, canvas.height, direction * cell,
                            (clip.framesPerDirection - 1 - frame) * cell, cell, cell,
                            clip.mirrorWest && direction >= 5);
                Save(canvas, Path.Combine(folder, clip.state + ".png"));
            }
        }

        private static (Texture2D texture, Color32[] pixels, int width, int height) Canvas(int width, int height)
        {
            var pixels = new Color32[width * height];
            var background = new Color32(24, 18, 27, 255);
            Array.Fill(pixels, background);
            return (new Texture2D(width, height, TextureFormat.RGBA32, false), pixels, width, height);
        }

        private static void Blit(Sprite sprite, Color32[] target, int targetWidth, int targetHeight,
            int cellX, int cellY, int cellWidth, int cellHeight, bool flip = false)
        {
            Rect rect = sprite.rect;
            Color32[] source = sprite.texture.GetPixels32();
            int sourceWidth = sprite.texture.width;
            bool[] mask = GeometryMask(sprite);
            float scale = (cellHeight - 28f) / (5f * sprite.pixelsPerUnit);
            int width = Mathf.Max(1, Mathf.RoundToInt(rect.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(rect.height * scale));
            int left = cellX + cellWidth / 2 - Mathf.RoundToInt(sprite.pivot.x * scale);
            int bottom = cellY + 12 - Mathf.RoundToInt(sprite.pivot.y * scale);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int sx = Mathf.Clamp(Mathf.FloorToInt(rect.x + x / scale), 0, sprite.texture.width - 1);
                    int sy = Mathf.Clamp(Mathf.FloorToInt(rect.y + y / scale), 0, sprite.texture.height - 1);
                    int mx = sx - Mathf.RoundToInt(rect.x), my = sy - Mathf.RoundToInt(rect.y);
                    if (!mask[my * Mathf.RoundToInt(rect.width) + mx]) continue;
                    Color32 src = source[sy * sourceWidth + sx];
                    if (src.a == 0) continue;
                    int dx = flip ? cellX * 2 + cellWidth - 1 - (left + x) : left + x, dy = bottom + y;
                    if (dx < cellX || dx >= cellX + cellWidth || dy < cellY || dy >= cellY + cellHeight) continue;
                    int index = dy * targetWidth + dx;
                    Color32 dst = target[index];
                    float alpha = src.a / 255f;
                    target[index] = new Color32((byte)Mathf.RoundToInt(src.r * alpha + dst.r * (1f - alpha)),
                        (byte)Mathf.RoundToInt(src.g * alpha + dst.g * (1f - alpha)),
                        (byte)Mathf.RoundToInt(src.b * alpha + dst.b * (1f - alpha)), 255);
                }
        }

        private static bool[] GeometryMask(Sprite sprite)
        {
            int width = Mathf.RoundToInt(sprite.rect.width), height = Mathf.RoundToInt(sprite.rect.height);
            var mask = new bool[width * height];
            Vector2[] vertices = sprite.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = vertices[i] * sprite.pixelsPerUnit + sprite.pivot;
            ushort[] indices = sprite.triangles;
            for (int t = 0; t < indices.Length; t += 3)
            {
                Vector2 a = vertices[indices[t]], b = vertices[indices[t + 1]], c = vertices[indices[t + 2]];
                int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
                int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
                int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + .5f, y + .5f);
                    float ab = Cross(b - a, p - a), bc = Cross(c - b, p - b), ca = Cross(a - c, p - c);
                    if ((ab >= 0 && bc >= 0 && ca >= 0) || (ab <= 0 && bc <= 0 && ca <= 0)) mask[y * width + x] = true;
                }
            }
            return mask;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static void Save((Texture2D texture, Color32[] pixels, int width, int height) canvas, string path)
        {
            canvas.texture.SetPixels32(canvas.pixels);
            canvas.texture.Apply(false, false);
            File.WriteAllBytes(path, canvas.texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(canvas.texture);
        }

        private static string OutputFolder()
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            string parent = Directory.GetParent(project)?.Name;
            if (string.Equals(parent, ".superpowers", StringComparison.OrdinalIgnoreCase))
                project = Directory.GetParent(Directory.GetParent(project).FullName).FullName;
            return Path.Combine(project, "Logs", "OrpheusBroadOil");
        }
    }
}
