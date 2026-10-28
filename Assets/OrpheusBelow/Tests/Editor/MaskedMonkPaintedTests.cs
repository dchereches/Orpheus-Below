using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class MaskedMonkPaintedTests
    {
        [TestCase("Body.png", 8)]
        [TestCase("Effects.png", 4)]
        public void EveryMaskedMonkFrameContainsDistinctPaintAndTransparentSpace(string fileName, int rows)
        {
            string path = Path.Combine(Application.dataPath, "OrpheusBelow/Resources/Enemies/MaskedMonk", fileName);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), Is.True);
                Assert.That(texture.width % 6, Is.Zero, "Atlas columns must slice evenly.");
                Assert.That(texture.height % rows, Is.Zero, "Atlas rows must slice evenly.");
                int cellWidth = texture.width / 6;
                int cellHeight = texture.height / rows;
                var uniqueFrames = new HashSet<ulong>();
                for (int row = 0; row < rows; row++)
                for (int col = 0; col < 6; col++)
                {
                    var pixels = texture.GetPixels(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                    int transparent = 0;
                    int painted = 0;
                    ulong hash = 14695981039346656037UL;
                    foreach (var pixel in pixels)
                    {
                        Color32 color = pixel;
                        if (color.a < 10) transparent++;
                        if (color.a > 100) painted++;
                        unchecked
                        {
                            hash = (hash ^ color.r) * 1099511628211UL;
                            hash = (hash ^ color.g) * 1099511628211UL;
                            hash = (hash ^ color.b) * 1099511628211UL;
                            hash = (hash ^ color.a) * 1099511628211UL;
                        }
                    }
                    string frame = fileName + " row " + row + " col " + col;
                    Assert.That(painted, Is.GreaterThan(50), frame + " is empty.");
                    Assert.That(transparent, Is.GreaterThan(pixels.Length / 20), frame + " has no transparent spacing.");
                    Assert.That(uniqueFrames.Add(hash), Is.True, frame + " duplicates another frame.");
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void MaskedMonkProvidesFullWarningAndRecoveryWindows()
        {
            Assert.That(MaskedMonkAttack.WarningSeconds, Is.InRange(.55f, .85f));
            Assert.That(MaskedMonkAttack.FallSeconds, Is.GreaterThan(0f));
            Assert.That(MaskedMonkAttack.RecoverySeconds, Is.InRange(.3f, .6f));
        }
    }
}
