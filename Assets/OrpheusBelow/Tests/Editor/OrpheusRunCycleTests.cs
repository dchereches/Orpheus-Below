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
        [Serializable] private sealed class Rig { public Vector2 hip, knee, ankle, shoulder, elbow, wrist; public int sourceWidth, sourceHeight; }
        private static OrpheusAnimationLibrary Library => Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
        private static Rig Source(int row) => JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Characters/Orpheus/BroadOil/AthleticRunLayout").text).rigs[row];
        private static Vector2 Uv(Rig r, Vector2 pixel) => new Vector2(pixel.x / r.sourceWidth, 1f - pixel.y / r.sourceHeight);

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
        public void PaintedSupportBootCountersOneFrameOfRootTravel()
        {
            Rig r = Source(2);
            Vector2 a = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, 0), Uv(r, r.ankle))[1];
            Vector2 b = PaintedPoints(Library.Frame(OrpheusAnimationState.Run, EurydiceFacing.East, 1), Uv(r, r.ankle))[1];
            Assert.That(Vector2.Distance(b + Vector2.right * (OrpheusRunMotion.CycleDistance / 16f), a), Is.LessThan(.025f));
        }

        [Test]
        public void PaintedTorsoRisesDuringFlightInsteadOfRemainingFrozenInACrouch()
        {
            Rig r = Source(2); Vector2 hair = Uv(r, new Vector2(r.hip.x, 200));
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
