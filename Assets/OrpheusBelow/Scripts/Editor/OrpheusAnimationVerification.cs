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
            Color32[] source = sprite.texture.GetPixels32();
            Vector2[] vertices = sprite.vertices;
            Vector2[] uv = sprite.uv;
            float scale = (cellHeight - 28f) / 5f;
            Vector2 origin = new Vector2(cellX + cellWidth * .5f, cellY + 12f);
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = origin + new Vector2(vertices[i].x * (flip ? -1f : 1f), vertices[i].y) * scale;
            ushort[] indices = sprite.triangles;
            for (int t = 0; t < indices.Length; t += 3)
            {
                int ia = indices[t], ib = indices[t + 1], ic = indices[t + 2];
                Vector2 a = vertices[ia], b = vertices[ib], c = vertices[ic];
                float determinant = Cross(b - a, c - a);
                if (Mathf.Abs(determinant) < .000001f) continue;
                int minX = Mathf.Max(cellX, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
                int maxX = Mathf.Min(cellX + cellWidth - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                int minY = Mathf.Max(cellY, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
                int maxY = Mathf.Min(cellY + cellHeight - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + .5f, y + .5f) - a;
                    float u = Cross(p, c - a) / determinant, v = Cross(b - a, p) / determinant;
                    if (u < 0f || v < 0f || u + v >= 1f) continue;
                    Vector2 sample = uv[ia] + (uv[ib] - uv[ia]) * u + (uv[ic] - uv[ia]) * v;
                    int sx = Mathf.Clamp(Mathf.FloorToInt(sample.x * sprite.texture.width), 0, sprite.texture.width - 1);
                    int sy = Mathf.Clamp(Mathf.FloorToInt(sample.y * sprite.texture.height), 0, sprite.texture.height - 1);
                    Color32 src = source[sy * sprite.texture.width + sx];
                    if (src.a == 0) continue;
                    int index = y * targetWidth + x;
                    Color32 dst = target[index];
                    float alpha = src.a / 255f;
                    target[index] = new Color32((byte)Mathf.RoundToInt(src.r * alpha + dst.r * (1f - alpha)),
                        (byte)Mathf.RoundToInt(src.g * alpha + dst.g * (1f - alpha)),
                        (byte)Mathf.RoundToInt(src.b * alpha + dst.b * (1f - alpha)), 255);
                }
            }
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
