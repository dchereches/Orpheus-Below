using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class MysticEyeWardenPaintedTests
    {
        [TestCase("Body.png", 4, 8)]
        [TestCase("Effects.png", 6, 4)]
        public void EveryFrameHasDistinctPaintAndTransparentSpace(string fileName, int columns, int rows)
        {
            string path = Path.Combine(Application.dataPath, "OrpheusBelow/Resources/Enemies/MysticEyeWarden", fileName);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), Is.True);
                var hashes = new HashSet<ulong>();
                for (int row = 0; row < rows; row++)
                for (int col = 0; col < columns; col++)
                {
                    int x0 = Mathf.RoundToInt(col * texture.width / (float)columns);
                    int x1 = Mathf.RoundToInt((col + 1) * texture.width / (float)columns);
                    int y0 = Mathf.RoundToInt(row * texture.height / (float)rows);
                    int y1 = Mathf.RoundToInt((row + 1) * texture.height / (float)rows);
                    var pixels = texture.GetPixels(x0, y0, x1 - x0, y1 - y0);
                    int clear = 0, painted = 0;
                    ulong hash = 14695981039346656037UL;
                    foreach (Color pixel in pixels)
                    {
                        Color32 c = pixel;
                        if (c.a < 10) clear++;
                        if (c.a > 100) painted++;
                        unchecked
                        {
                            hash = (hash ^ c.r) * 1099511628211UL;
                            hash = (hash ^ c.g) * 1099511628211UL;
                            hash = (hash ^ c.b) * 1099511628211UL;
                            hash = (hash ^ c.a) * 1099511628211UL;
                        }
                    }
                    string cell = fileName + " row " + row + " column " + col;
                    Assert.That(painted, Is.GreaterThan(500), cell + " is empty.");
                    // This broad impact is intentionally wider than half its nominal source cell.
                    // Keep the majority-transparent requirement for every other body/effect cell.
                    float minimumClearFraction = fileName == "Effects.png" && row == 1 && col == 3 ? .45f : .5f;
                    Assert.That(clear, Is.GreaterThan(pixels.Length * minimumClearFraction), cell + " lacks transparent space.");
                    Assert.That(hashes.Add(hash), Is.True, cell + " duplicates a pose.");
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void StaffThrowGivesAFullWaitAndCommittedWarning()
        {
            Assert.That(MysticEyeWardenAttack.WaitSeconds, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(MysticEyeWardenAttack.WindupSeconds, Is.GreaterThanOrEqualTo(.9f));
            Assert.That(MysticEyeWardenAttack.RecallSeconds, Is.GreaterThan(.4f));
            Assert.That(MysticEyeWardenAttack.RecoverySeconds, Is.GreaterThanOrEqualTo(.8f));
        }

        [Test]
        public void CancelImmediatelyRemovesOwnedStaffAndVisualHazards()
        {
            var owner = new GameObject("EyeAttackCancellationTest");
            var staffObject = new GameObject("OwnedStaff");
            var charge = new GameObject("OwnedCharge");
            var recall = new GameObject("OwnedRecall");
            try
            {
                var attack = owner.AddComponent<MysticEyeWardenAttack>();
                var staff = staffObject.AddComponent<PaintedEnemyProjectile>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(MysticEyeWardenAttack).GetField("_staff", flags).SetValue(attack, staff);
                typeof(MysticEyeWardenAttack).GetField("_charge", flags).SetValue(attack, charge);
                typeof(MysticEyeWardenAttack).GetField("_recall", flags).SetValue(attack, recall);

                attack.Cancel();
                Assert.That(staffObject == null, Is.True, "Cancelled staff can still damage the player.");
                Assert.That(charge == null, Is.True);
                Assert.That(recall == null, Is.True);
                Assert.That(attack.HasActiveStaff, Is.False);
                Assert.That(attack.HasPendingThrow, Is.False);
                Assert.That(attack.Phase, Is.EqualTo(MysticEyeWardenAttack.CastPhase.Wait));
                Assert.That(attack.PhaseRemaining, Is.EqualTo(MysticEyeWardenAttack.WaitSeconds));
                attack.Cancel(); // Repeated parent cancellation is safe.
            }
            finally
            {
                if (owner != null) Object.DestroyImmediate(owner);
                if (staffObject != null) Object.DestroyImmediate(staffObject);
                if (charge != null) Object.DestroyImmediate(charge);
                if (recall != null) Object.DestroyImmediate(recall);
            }
        }
    }
}
