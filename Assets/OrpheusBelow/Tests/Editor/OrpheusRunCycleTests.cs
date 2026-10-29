using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class OrpheusRunCycleTests
    {
        [Serializable] private sealed class Mapping { public int framesPerDirection; public Source[] sources; public View[] views; public Observation[] observations; }
        [Serializable] private sealed class Source { public string file, sha256; public int width, height, columns, rows, maxTextureSize; }
        [Serializable] private sealed class View { public string facing; public Key[] frames; }
        [Serializable] private sealed class Key { public string source, role; public int cell; }
        [Serializable] private sealed class Observation { public string view, confidence, axis; public int phase, expectedSign; public Vector2 ankleA, ankleB; }
        private static OrpheusAnimationLibrary Library => Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
        private static Mapping Reviewed => JsonUtility.FromJson<Mapping>(Resources.Load<TextAsset>("Characters/Orpheus/BroadOil/NaturalRunV2/ManualSourceMapping").text);

        [Test]
        public void RunUsesOnlyTheFourReviewedContactAndPassingKeysInEachCanonicalView()
        {
            Mapping mapping = Reviewed;
            var run = Library.FindClip(OrpheusAnimationState.Run);
            Assert.That(run.framesPerDirection, Is.EqualTo(4));
            Assert.That(run.directions, Is.EqualTo(8));
            Assert.That(run.loop && run.mirrorWest, Is.True);
            int[] rows = { 0, 1, 2, 3, 4, 3, 2, 1 };
            for (int direction = 0; direction < 8; direction++) for (int phase = 0; phase < 4; phase++)
            {
                int row = rows[direction];
                Key key = mapping.views[row].frames[phase];
                Source source = mapping.sources.Single(s => s.file == key.source);
                Sprite paint = run.frames[direction * 4 + phase];
                Assert.That(paint, Is.SameAs(Library.sourceFrames.Single(s => s.name == $"NaturalRun_{row:D2}_{phase:D2}")));
                Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(paint.texture)), Is.EqualTo(key.source));
                float centreX = paint.rect.center.x / paint.texture.width;
                float centreY = 1f - paint.rect.center.y / paint.texture.height;
                Assert.That(Mathf.FloorToInt(centreX * source.columns), Is.EqualTo(key.cell % source.columns));
                Assert.That(Mathf.FloorToInt(centreY * source.rows), Is.EqualTo(key.cell / source.columns));
            }
            Assert.That(Library.sourceFrames.Count(s => s.name.StartsWith("NaturalRun_")), Is.EqualTo(20));
            Assert.That(Library.sourceFrames.Any(s => s.name.StartsWith("Athletic") || s.name.StartsWith("NaturalRunSource_")), Is.False,
                "Reconstructed limbs and unused draft poses must not remain in the library.");
        }

        [Test]
        public void ReviewedSourceBytesDimensionsAndOrdinaryImportSizesArePreserved()
        {
            foreach (Source source in Reviewed.sources)
            {
                string path = "Assets/OrpheusBelow/Resources/Characters/Orpheus/BroadOil/NaturalRunV2/" + source.file;
                using (var sha = SHA256.Create())
                {
                    string actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
                    Assert.That(actual, Is.EqualTo(source.sha256), "Review landmarks must refer to unedited generated PNG bytes: " + source.file);
                }
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                Assert.That(width, Is.EqualTo(source.width)); Assert.That(height, Is.EqualTo(source.height));
                Assert.That(importer.maxTextureSize, Is.EqualTo(source.maxTextureSize));
            }
        }

        [Test]
        public void HighConfidenceObservedBootLandmarksRemainPaintedAndKeepOppositeRoles()
        {
            // These are reviewed visible boot points, not generated pose labels or file-count
            // inference. Southeast/northeast hidden hip attachments are deliberately not asserted.
            Mapping mapping = Reviewed;
            foreach (Observation observation in mapping.observations.Where(o => o.confidence == "high"))
            {
                int row = Array.FindIndex(mapping.views, v => v.facing == observation.view);
                Key key = mapping.views[row].frames[observation.phase];
                Source source = mapping.sources.Single(s => s.file == key.source);
                Sprite paint = Library.Frame(OrpheusAnimationState.Run, (EurydiceFacing)row, observation.phase);
                Assert.That(PaintAlpha(paint, source, key.cell, observation.ankleA), Is.GreaterThan(.6f), observation.view + " ankle A");
                Assert.That(PaintAlpha(paint, source, key.cell, observation.ankleB), Is.GreaterThan(.6f), observation.view + " ankle B");
                float separation = observation.axis == "x" ? observation.ankleA.x - observation.ankleB.x : observation.ankleA.y - observation.ankleB.y;
                Assert.That(Mathf.Sign(separation), Is.EqualTo(observation.expectedSign));
            }
            Assert.That(mapping.observations.Single(o => o.view == "East" && o.phase == 1).expectedSign,
                Is.Not.EqualTo(mapping.observations.Single(o => o.view == "East" && o.phase == 3).expectedSign));
            Assert.That(mapping.observations.Single(o => o.view == "South" && o.phase == 0).expectedSign,
                Is.Not.EqualTo(mapping.observations.Single(o => o.view == "South" && o.phase == 2).expectedSign));
            Assert.That(mapping.observations.Single(o => o.view == "North" && o.phase == 0).expectedSign,
                Is.Not.EqualTo(mapping.observations.Single(o => o.view == "North" && o.phase == 2).expectedSign));
        }

        private static float PaintAlpha(Sprite sprite, Source source, int cell, Vector2 cellPixel)
        {
            Texture2D texture = sprite.texture;
            float nativeX = (cell % source.columns) * (source.width / (float)source.columns) + cellPixel.x;
            float nativeY = (cell / source.columns) * (source.height / (float)source.rows) + cellPixel.y;
            int x = Mathf.FloorToInt(nativeX / source.width * texture.width);
            int y = Mathf.FloorToInt((1f - nativeY / source.height) * texture.height);
            float maximum = 0f;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                int px = Mathf.Clamp(x + dx, 0, texture.width - 1), py = Mathf.Clamp(y + dy, 0, texture.height - 1);
                if (MeshContains(sprite, new Vector2((px + .5f) / texture.width, (py + .5f) / texture.height)))
                    maximum = Mathf.Max(maximum, texture.GetPixel(px, py).a);
            }
            return maximum;
        }

        private static bool MeshContains(Sprite sprite, Vector2 sample)
        {
            Vector2[] uv = sprite.uv; ushort[] triangles = sprite.triangles;
            for (int index = 0; index < triangles.Length; index += 3)
            {
                Vector2 a = uv[triangles[index]], b = uv[triangles[index + 1]], c = uv[triangles[index + 2]];
                float determinant = Cross(b - a, c - a);
                if (Mathf.Abs(determinant) < 1e-12f) continue;
                float u = Cross(sample - a, c - a) / determinant, v = Cross(b - a, sample - a) / determinant;
                if (u >= 0f && v >= 0f && u + v <= 1f) return true;
            }
            return false;
        }
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        [Test]
        public void EveryRunVertexKeepsItsOriginalPaintLocationWithoutLimbWarpingOrCloning()
        {
            foreach (Sprite sprite in Library.FindClip(OrpheusAnimationState.Run).frames)
            {
                Vector2[] positions = sprite.vertices, uv = sprite.uv;
                Assert.That(positions, Is.Not.Empty, sprite.name);
                for (int vertex = 0; vertex < positions.Length; vertex++)
                {
                    Vector2 sourcePixel = Vector2.Scale(uv[vertex], new Vector2(sprite.texture.width, sprite.texture.height));
                    Vector2 renderedPixel = positions[vertex] * sprite.pixelsPerUnit + sprite.pivot + sprite.rect.position;
                    Assert.That(Vector2.Distance(renderedPixel, sourcePixel), Is.LessThan(.002f),
                        sprite.name + " moved paint away from the original connected body.");
                }
                Assert.That(sprite.rect.height, Is.InRange(85f, 160f), "Keep the imported paint near the idle's physical detail scale.");
            }
        }

        [Test]
        public void OriginalRunningSilhouetteKeepsTheEstablishedActorHeight()
        {
            for (int facing = 0; facing < 8; facing++) for (int phase = 0; phase < 4; phase++)
            {
                float idleHeight = Library.Frame(OrpheusAnimationState.Idle, (EurydiceFacing)facing, 0).bounds.size.y;
                Sprite sprite = Library.Frame(OrpheusAnimationState.Run, (EurydiceFacing)facing, phase);
                Assert.That(sprite.bounds.size.y / idleHeight, Is.InRange(.8f, 1.2f), sprite.name);
            }
        }

        [Test]
        public void RunUsesTheSameTextureImportAndEdgePolicyAsIdle()
        {
            var idle = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Library.Frame(OrpheusAnimationState.Idle, EurydiceFacing.East, 0).texture));
            foreach (Sprite paint in Library.FindClip(OrpheusAnimationState.Run).frames.Distinct())
            {
                var run = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(paint.texture));
                Assert.That(run.alphaIsTransparency, Is.EqualTo(idle.alphaIsTransparency));
                Assert.That(run.filterMode, Is.EqualTo(idle.filterMode));
                Assert.That(run.textureCompression, Is.EqualTo(idle.textureCompression));
                Assert.That(run.mipmapEnabled, Is.EqualTo(idle.mipmapEnabled));
            }
        }

        [Test]
        public void AllActionsAndExistingTransitionStairAndDashSourcesRemainInstalled()
        {
            Assert.That(Library.IsComplete, Is.True); Assert.That(Library.clips.Length, Is.EqualTo(48));
            foreach (var state in new[] { OrpheusAnimationState.Start, OrpheusAnimationState.Walk, OrpheusAnimationState.Turn,
                OrpheusAnimationState.StairAscent, OrpheusAnimationState.StairDescent, OrpheusAnimationState.DashSettle })
                Assert.That(Library.FindClip(state).frames.All(s => s.name.StartsWith("WalkCycle_")), Is.True, state.ToString());
            foreach (var state in new[] { OrpheusAnimationState.DashStart, OrpheusAnimationState.DashTravel })
                Assert.That(Library.FindClip(state).frames.All(s => s.name.StartsWith("RunCycle_")), Is.True, state.ToString());
            Assert.That(Library.visibleHeight, Is.EqualTo(3.9f));
        }
    }
}
