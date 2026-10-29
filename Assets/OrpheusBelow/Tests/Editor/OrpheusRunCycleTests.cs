using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public class OrpheusRunCycleTests
    {
        [Serializable] private sealed class Layout { public Rig[] rigs; }
        [Serializable] private sealed class Rig { public Vector2 hip, knee, ankle, shoulder, elbow, wrist; public int sourceWidth, sourceHeight, sourceX, sourceY; public Garment[] garments; }
        [Serializable] private sealed class Garment { public Vector2 min, max, sourceMin, sourceMax; }
        private static OrpheusAnimationLibrary Library => Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
        private static Rig Source(int row) => JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Characters/Orpheus/BroadOil/AthleticRunLayout").text).rigs[row];
        private static Vector2 Uv(Rig r, Vector2 pixel) => new Vector2(pixel.x / r.sourceWidth, 1f - pixel.y / r.sourceHeight);

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void RunningPaintIsAnExactExtractOfTheEstablishedWalkingCharacter(int row)
        {
            Rig r = Source(row);
            Texture2D established = Resources.Load<Texture2D>("Characters/Orpheus/BroadOil/WalkCycle");
            Sprite canonical = Library.sourceFrames.Single(x => x.name == "AthleticSource_" + row.ToString("D2"));
            Assert.That(canonical.texture.width, Is.EqualTo(r.sourceWidth));
            Assert.That(canonical.texture.height, Is.EqualTo(r.sourceHeight));
            var nativePaint = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                string path = System.IO.Path.Combine(Application.dataPath,
                    "OrpheusBelow/Resources/Characters/Orpheus/BroadOil/" + canonical.texture.name + ".png");
                Assert.That(ImageConversion.LoadImage(nativePaint, System.IO.File.ReadAllBytes(path)), Is.True);
                Assert.That(nativePaint.width, Is.EqualTo(r.sourceWidth));
                Assert.That(nativePaint.height, Is.EqualTo(r.sourceHeight));
                Color32[] expected = established.GetPixels32(), extracted = nativePaint.GetPixels32(), installed = canonical.texture.GetPixels32();
                int bottom = established.height - r.sourceY - r.sourceHeight;
                for (int y = 0; y < r.sourceHeight; y++) for (int x = 0; x < r.sourceWidth; x++)
                {
                    int index = y * r.sourceWidth + x;
                    Color32 paint = expected[(bottom + y) * established.width + r.sourceX + x];
                    // The saved PNG must copy every source RGBA byte, including hidden RGB.
                    Assert.That(extracted[index], Is.EqualTo(paint), "Canonical PNG extraction changed established character paint.");
                    Assert.That(installed[index].a, Is.EqualTo(paint.a), "Import changed the character silhouette alpha.");
                    // alphaIsTransparency bleeds edge colors into fully transparent texels on
                    // reimport. Those RGB values cannot contribute to rendered character paint.
                    if (paint.a > 0)
                        Assert.That(installed[index], Is.EqualTo(paint),
                            "The run must keep the existing face, hair, coat, palette and brushwork pixel for pixel.");
                }
            }
            finally { Object.DestroyImmediate(nativePaint); }
        }

        [Test]
        public void EveryRunningFrameUsesOnlyTheMatchingEstablishedCharacterPaint()
        {
            int[] rows = { 0, 1, 2, 3, 4, 3, 2, 1 };
            OrpheusAnimationClip run = Library.FindClip(OrpheusAnimationState.Run);
            for (int direction = 0; direction < 8; direction++)
            {
                Texture2D expected = Library.sourceFrames.Single(x => x.name == "AthleticSource_" + rows[direction].ToString("D2")).texture;
                for (int phase = 0; phase < run.framesPerDirection; phase++)
                    Assert.That(run.frames[direction * run.framesPerDirection + phase].texture, Is.SameAs(expected),
                        "No phase may bind a former red-cape generated source or the crouching RunCycle paint.");
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void RetainedCoatBackingUsesOpaqueEstablishedGarmentPaint(int row)
        {
            Rig rig = Source(row);
            Sprite canonical = Library.sourceFrames.Single(x => x.name == "AthleticSource_" + row.ToString("D2"));
            Color32[] paint = canonical.texture.GetPixels32();
            Assert.That(rig.garments, Has.Length.EqualTo(2));
            foreach (Garment panel in rig.garments)
            {
                // The complete coat overlap gets backing, including old wrists that covered
                // cloth. Backing samples garment UVs, so it cannot retain a frozen skin hand.
                for (int y = (int)panel.sourceMin.y; y <= (int)panel.sourceMax.y; y++)
                    for (int x = (int)panel.sourceMin.x; x <= (int)panel.sourceMax.x; x++)
                        Assert.That(paint[(rig.sourceHeight - 1 - y) * rig.sourceWidth + x].a, Is.GreaterThan(150),
                            "A transparent donor would reopen the coat when an arm swings away.");
                // Only excised arm regions need backing; an arbitrary panel midpoint may be
                // intact torso or a deliberately excluded leg. The rendered gap locations are
                // tested throughout the full cycle in OldArmCutsInsideTheCoatStayCoveredThroughoutTheRun.
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void OldArmCutsInsideTheCoatStayCoveredThroughoutTheRun(int row)
        {
            // Native pixels where rendered review found stair/background holes below the old
            // wrists or behind the old elbows. These locations must have retained cloth.
            Vector2[][] holes = {
                new[] { new Vector2(91, 175), new Vector2(194, 173) },
                new[] { new Vector2(112, 175), new Vector2(199, 145) },
                new[] { new Vector2(128, 181), new Vector2(181, 134) },
                new[] { new Vector2(205, 126), new Vector2(136, 147) },
                new[] { new Vector2(72, 145), new Vector2(214, 173) }
            };
            Rig rig = Source(row);
            for (int index = 0; index < 2; index++)
            {
                Garment panel = rig.garments[index]; Vector2 hole = holes[row][index];
                Assert.That(hole.x, Is.InRange(panel.min.x, panel.max.x));
                Assert.That(hole.y, Is.InRange(panel.min.y, panel.max.y));
                Vector2 t = new Vector2((hole.x - panel.min.x) / (panel.max.x - panel.min.x),
                    (hole.y - panel.min.y) / (panel.max.y - panel.min.y));
                Vector2 donor = Uv(rig, panel.sourceMin + Vector2.Scale(panel.sourceMax - panel.sourceMin, t));
                for (int phase = 0; phase < 16; phase++)
                    Assert.That(PaintedPoints(Library.Frame(OrpheusAnimationState.Run, (EurydiceFacing)row, phase), donor).Count,
                        Is.GreaterThanOrEqualTo(2), "Missing cloth beneath old arm cut at " + hole + " in phase " + phase);
            }
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void UnownedOriginalHandTipsNeverReturnAsStationaryTorsoPaint(int row)
        {
            Vector2[][] tips = {
                Array.Empty<Vector2>(),
                new[] { new Vector2(203, 155), new Vector2(201, 155), new Vector2(197, 151) },
                new[] { new Vector2(195, 157), new Vector2(213, 157), new Vector2(195, 159) },
                new[] { new Vector2(215, 152), new Vector2(217, 162), new Vector2(217, 164) },
                new[] { new Vector2(75, 174), new Vector2(73, 172), new Vector2(73, 174) }
            };
            Rig rig = Source(row);
            foreach (Vector2 originalTip in tips[row])
                for (int phase = 0; phase < 16; phase++)
                    Assert.That(PaintedPoints(Library.Frame(OrpheusAnimationState.Run, (EurydiceFacing)row, phase), Uv(rig, originalTip)),
                        Is.Empty, "Original terminal paint outside the moving limb must not reconnect to the torso at " + originalTip);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void FullBodyRunAlternatesPaintedBootsAndCounterSwingsBothFists(int row)
        {
            Rig r = Source(row); var facing = (EurydiceFacing)row;
            var clip = Library.FindClip(OrpheusAnimationState.Run);
            Assert.That(clip.framesPerDirection, Is.EqualTo(16));
            Sprite first = Library.Frame(OrpheusAnimationState.Run, facing, 0);
            Sprite half = Library.Frame(OrpheusAnimationState.Run, facing, 8);
            Assert.That(first.texture.name, Does.StartWith("Athletic"), "The former frozen crouching torso must not be reused.");
            Vector2 travel = new Vector2(Mathf.Sin(row * Mathf.PI * .25f), -Mathf.Cos(row * Mathf.PI * .25f) * Mathf.Sin(36f * Mathf.Deg2Rad)).normalized;
            List<Vector2> boots = PaintedPoints(first, Uv(r, r.ankle)), bootsHalf = PaintedPoints(half, Uv(r, r.ankle));
            List<Vector2> fists = PaintedPoints(first, Uv(r, r.wrist)), fistsHalf = PaintedPoints(half, Uv(r, r.wrist));
            Assert.That(boots.Count, Is.EqualTo(2), "Each anatomical leg needs its own painted boot.");
            Assert.That(bootsHalf.Count, Is.EqualTo(2)); Assert.That(fists.Count, Is.EqualTo(2)); Assert.That(fistsHalf.Count, Is.EqualTo(2));
            Sprite canonical = Library.sourceFrames.Single(x => x.name == "AthleticSource_" + row.ToString("D2"));
            Vector2 pixelScale = new Vector2(canonical.texture.width / (float)r.sourceWidth, canonical.texture.height / (float)r.sourceHeight) / canonical.pixelsPerUnit;
            Vector2 upper = Vector2.Scale(r.knee - r.hip, pixelScale), lower = Vector2.Scale(r.ankle - r.knee, pixelScale);
            Vector2 fitted = OrpheusRunMotion.FitLegLengths(upper.magnitude, lower.magnitude, (r.ankle.y - r.hip.y) * pixelScale.y);
            float upperArm = Vector2.Scale(r.elbow - r.shoulder, pixelScale).magnitude;
            float lowerArm = Vector2.Scale(r.wrist - r.elbow, pixelScale).magnitude;
            var startPose = OrpheusRunMotion.Evaluate(0f, fitted.x, fitted.y, upperArm, lowerArm);
            var halfPose = OrpheusRunMotion.Evaluate(.5f, fitted.x, fitted.y, upperArm, lowerArm);
            Vector2 footDelta = halfPose.nearLeg.end - startPose.nearLeg.end;
            Vector2 projected = new Vector2(Mathf.Sin(row * Mathf.PI * .25f), -Mathf.Cos(row * Mathf.PI * .25f) * Mathf.Sin(36f * Mathf.Deg2Rad)) * footDelta.x + Vector2.up * footDelta.y;
            Assert.That(Vector2.Distance(bootsHalf[1] - boots[1], projected), Is.LessThan(.04f));
            Assert.That(Vector2.Distance(bootsHalf[0] - boots[0], -projected), Is.LessThan(.04f));
            Vector2 armDelta = halfPose.nearArm.end - startPose.nearArm.end;
            Vector2 projectedArm = new Vector2(Mathf.Sin(row * Mathf.PI * .25f), -Mathf.Cos(row * Mathf.PI * .25f) * Mathf.Sin(36f * Mathf.Deg2Rad)) * armDelta.x + Vector2.up * armDelta.y;
            Assert.That(Vector2.Distance(fistsHalf[1] - fists[1], projectedArm), Is.LessThan(.04f), "The actual fist paint must follow the counter-swinging skeleton, including depth projection.");
            Assert.That(Vector2.Distance(fistsHalf[0] - fists[0], -projectedArm), Is.LessThan(.04f));
        }

        [Test]
        public void SideViewPlantsBothPaintedBootsAtTheSameForwardReach()
        {
            Rig r = Source(2);
            // Compare each leg at its own touchdown. A walking source's spread hips must
            // not leave one running step half a body-width behind the other.
            Vector2 near = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, 0), Uv(r, r.ankle))[1];
            Vector2 far = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, 8), Uv(r, r.ankle))[0];
            Assert.That(Mathf.Abs(near.x - far.x), Is.LessThan(.04f),
                "Side-on hips overlap horizontally; both legs need the same planted stride.");
        }

        [TestCase(0)] [TestCase(4)] [TestCase(8)] [TestCase(12)]
        public void EastMovingThighGathersTheWalkingFabricInsteadOfFormingARoundSack(int frame)
        {
            Rig rig = Source(2);
            Vector2 axis = (rig.knee - rig.hip).normalized;
            Vector2 across = new Vector2(-axis.y, axis.x);
            Vector2 center = Vector2.Lerp(rig.hip, rig.knee, .75f);
            Sprite canonical = Library.sourceFrames.Single(x => x.name == "AthleticSource_02");
            Sprite posed = Library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, frame);
            List<Vector2> left = PaintedPoints(posed, Uv(rig, center - across * 8f));
            List<Vector2> right = PaintedPoints(posed, Uv(rig, center + across * 8f));
            Assert.That(left.Count, Is.EqualTo(2));
            Assert.That(right.Count, Is.EqualTo(2));
            float walkingWidth = 16f * canonical.texture.width / rig.sourceWidth / canonical.pixelsPerUnit;
            for (int leg = 0; leg < 2; leg++)
                Assert.That(Vector2.Distance(left[leg], right[leg]), Is.LessThan(walkingWidth * .78f),
                    "The moving trouser thigh must gather across its bone while retaining the original paint and landmark positions.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void BothTrouserRootsStayOnTheRetainedCoatSeam(int row)
        {
            Rig rig = Source(row);
            Vector2 seam = Uv(rig, rig.hip + new Vector2(0f, 6f));
            foreach (int frame in new[] { 0, 4, 8, 12 })
            {
                List<Vector2> overlap = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, (EurydiceFacing)row, frame), seam);
                Assert.That(overlap.Count, Is.EqualTo(1),
                    "Pinned trouser paint must overlap the retained coat exactly; a shifted far-leg root opens a horizontal cut.");
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void ActualPaintedRigRetainsBothBoneLengthsThroughoutTheEntireRun(int row)
        {
            Rig rig = Source(row);
            Sprite canonical = Library.sourceFrames.Single(x => x.name == "AthleticSource_" + row.ToString("D2"));
            Vector2 scale = new Vector2(canonical.texture.width / (float)rig.sourceWidth,
                canonical.texture.height / (float)rig.sourceHeight) / canonical.pixelsPerUnit;
            Vector2 fitted = OrpheusRunMotion.FitLegLengths(Vector2.Scale(rig.knee - rig.hip, scale).magnitude,
                Vector2.Scale(rig.ankle - rig.knee, scale).magnitude, (rig.ankle.y - rig.hip.y) * scale.y);
            // Sample between baked frames too: the North painting's asymmetric bones
            // formerly put the recovering ankle inside its minimum reach near phase .625.
            for (int i = 0; i < 4096; i++)
            {
                var pose = OrpheusRunMotion.Evaluate(i / 4096f, fitted.x, fitted.y);
                foreach (var leg in new[] { pose.nearLeg, pose.farLeg })
                {
                    Assert.That(Vector2.Distance(leg.root, leg.end), Is.InRange(Mathf.Abs(fitted.x - fitted.y), fitted.x + fitted.y));
                    Assert.That(Vector2.Distance(leg.root, leg.joint), Is.EqualTo(fitted.x).Within(.00001f));
                    Assert.That(Vector2.Distance(leg.joint, leg.end), Is.EqualTo(fitted.y).Within(.00001f));
                }
            }
        }

        [TestCase(4, 0, 1)] [TestCase(12, 1, 0)]
        public void FrontViewClearlyLiftsTheRecoveringPaintedBootAboveThePlantedBoot(int frame, int recovering, int planted)
        {
            Rig r = Source(0);
            List<Vector2> boots = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, EurydiceFacing.South, frame), Uv(r, r.ankle));
            Assert.That(boots.Count, Is.EqualTo(2));
            Assert.That(boots[recovering].y - boots[planted].y, Is.GreaterThan(.25f),
                "At passing, recovery must visibly clear the opposite boot instead of projecting both feet to the same height.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void BothPaintedSupportBootsCounterRootTravelThroughoutTheirPlants(int row)
        {
            Rig r = Source(row);
            Vector2 travel = new Vector2(Mathf.Sin(row * Mathf.PI * .25f), -Mathf.Cos(row * Mathf.PI * .25f) * Mathf.Sin(36f * Mathf.Deg2Rad));
            for (int leg = 0; leg < 2; leg++)
            {
                int contact = leg == 1 ? 0 : 8;
                for (int step = 0; step < 5; step++)
                {
                    Vector2 a = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, (EurydiceFacing)row, contact + step), Uv(r, r.ankle))[leg];
                    Vector2 b = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, (EurydiceFacing)row, contact + step + 1), Uv(r, r.ankle))[leg];
                    Assert.That(Vector2.Distance(b + travel * (OrpheusRunMotion.CycleDistance / 16f), a), Is.LessThan(.025f),
                        "Both boots must stay planted in world space throughout support, including projected front/back travel.");
                }
            }
        }

        [Test]
        public void PaintedTorsoRisesDuringFlightInsteadOfRemainingFrozenInACrouch()
        {
            Rig r = Source(2); Vector2 hair = Uv(r, new Vector2(r.hip.x, r.sourceHeight * .09f));
            var contact = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, 0), hair);
            var flight = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, 6), hair);
            Assert.That(contact.Count, Is.EqualTo(1)); Assert.That(flight.Count, Is.EqualTo(1));
            Assert.That(flight[0].y - contact[0].y, Is.GreaterThan(.10f));
        }

        [Test]
        public void RuntimeRunningBoundsContainEveryRenderedVertexInEveryDirection()
        {
            var root = new GameObject("RunningCullingContract");
            try
            {
                SpriteRenderer body = root.AddComponent<SpriteRenderer>();
                var animator = root.AddComponent<OrpheusSpriteAnimator>(); animator.Initialize(body, null, null, Library);
                for (int direction = 0; direction < 8; direction++)
                {
                    float angle = direction * Mathf.PI * .25f;
                    Vector3 ground = (UnderworldCamera2D.IsoRightXZ * Mathf.Sin(angle) - UnderworldCamera2D.IsoUpXZ * Mathf.Cos(angle)) * .82f;
                    for (int i = 0; i < 4; i++) animator.Drive(new OrpheusAnimationInput { groundDelta = ground, deltaTime = .1f, movementRequested = true });
                    Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Run));
                    for (int frame = 0; frame < 16; frame++)
                    {
                        typeof(OrpheusSpriteAnimator).GetField("_frame", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(animator, frame);
                        typeof(OrpheusSpriteAnimator).GetMethod("ApplyFrame", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animator, null);
                        Bounds bounds = body.localBounds; bounds.Expand(.002f);
                        foreach (Vector2 vertex in body.sprite.vertices)
                            Assert.That(bounds.Contains(new Vector3(body.flipX ? -vertex.x : vertex.x, vertex.y, 0)), Is.True, body.sprite.name + " exceeds runtime culling bounds.");
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void FastStartsAndTurnsUseTheSameFullBodyRun()
        {
            WithAnimator((animator, body) =>
            {
                animator.Drive(new OrpheusAnimationInput { groundDelta = UnderworldCamera2D.IsoRightXZ * .82f, deltaTime = .1f, movementRequested = true });
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Start));
                Assert.That(body.sprite.texture.name, Does.StartWith("Athletic"));
                animator.Drive(new OrpheusAnimationInput { groundDelta = -UnderworldCamera2D.IsoRightXZ * .82f, deltaTime = .1f, movementRequested = true });
                Assert.That(body.sprite.texture.name, Does.StartWith("Athletic"));
            });
        }

        [TestCase(true)] [TestCase(false)]
        public void FastStairsKeepTheirStairStateAndShowTheFullBodyRun(bool ascending)
        {
            WithAnimator((animator, body) =>
            {
                animator.Drive(new OrpheusAnimationInput { groundDelta = UnderworldCamera2D.IsoRightXZ * .82f, deltaTime = .1f,
                    movementRequested = true, onStairs = true, ascending = ascending });
                Assert.That(animator.CurrentState, Is.EqualTo(ascending ? OrpheusAnimationState.StairAscent : OrpheusAnimationState.StairDescent));
                Assert.That(body.sprite.texture.name, Does.StartWith("Athletic"), "Full-speed stairs must not retain the former walking/crouching art.");
            });
        }

        private static void WithAnimator(Action<OrpheusSpriteAnimator, SpriteRenderer> action)
        {
            var root = new GameObject("FastLocomotionArtContract");
            try
            {
                var body = root.AddComponent<SpriteRenderer>(); var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(body, null, null, Library); action(animator, body);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void BossScaleKeepsTheRenderedSupportBootPlanted()
        {
            var root = new GameObject("ScaledRunningContract");
            try
            {
                var art = new GameObject("ScaledBillboard"); art.transform.SetParent(root.transform, false);
                art.transform.localScale = Vector3.one * .70f; art.transform.localRotation = UnderworldCamera2D.CurrentBillboardRotation;
                SpriteRenderer body = art.AddComponent<SpriteRenderer>();
                var animator = root.AddComponent<OrpheusSpriteAnimator>(); animator.Initialize(body, null, null, Library);
                for (int i = 0; i < 3; i++) animator.Drive(new OrpheusAnimationInput { groundDelta = UnderworldCamera2D.IsoRightXZ * .82f, deltaTime = .1f, movementRequested = true });
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Run));
                typeof(OrpheusSpriteAnimator).GetField("_gaitPhase", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(animator, 0f);
                typeof(OrpheusSpriteAnimator).GetField("_frame", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(animator, 0);
                typeof(OrpheusSpriteAnimator).GetMethod("ApplyFrame", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animator, null);
                Rig r = Source(2); Vector2 uv = Uv(r, r.ankle);
                Vector3 before = body.transform.TransformPoint(PaintedPoints(body.sprite, uv)[1]);
                float distance = OrpheusRunMotion.CycleDistance * .7f / 16f;
                root.transform.position += UnderworldCamera2D.IsoRightXZ * distance;
                animator.Drive(new OrpheusAnimationInput { groundDelta = UnderworldCamera2D.IsoRightXZ * distance, deltaTime = .02f, movementRequested = true });
                Vector3 after = body.transform.TransformPoint(PaintedPoints(body.sprite, uv)[1]);
                Assert.That(Vector3.Distance(before, after), Is.LessThan(.025f), "The 0.70 boss billboard must compensate its stride for scale.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static List<Vector2> PaintedPoints(Sprite sprite, Vector2 sample)
        {
            var result = new List<Vector2>();
            Vector2[] uv = sprite.uv, vertices = sprite.vertices;
            ushort[] indices = sprite.triangles;
            for (int i = 0; i < indices.Length; i += 3)
            {
                int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                float determinant = Cross(uv[b] - uv[a], uv[c] - uv[a]);
                if (Mathf.Abs(determinant) < 1e-12f) continue;
                float u = Cross(sample - uv[a], uv[c] - uv[a]) / determinant;
                float v = Cross(uv[b] - uv[a], sample - uv[a]) / determinant;
                if (u < 0f || v < 0f || u + v > 1f) continue;
                Vector2 point = vertices[a] + (vertices[b] - vertices[a]) * u + (vertices[c] - vertices[a]) * v;
                if (result.Count == 0 || Vector2.Distance(result[result.Count - 1], point) > .001f) result.Add(point);
            }
            return result;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
