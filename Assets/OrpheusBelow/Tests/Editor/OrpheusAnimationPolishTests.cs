using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public class OrpheusAnimationPolishTests
    {
        private GameObject _root;
        private OrpheusPlayerController _player;
        private OrpheusSpriteAnimator _animator;
        private GameObject[] _before;

        [SetUp]
        public void SetUp()
        {
            _before = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            _root = new GameObject("AnimationPolish");
            _player = _root.AddComponent<OrpheusPlayerController>();
            Call(_player, "Awake");
            _animator = _root.GetComponent<OrpheusSpriteAnimator>();
        }

        [TearDown]
        public void TearDown()
        {
            // These fixtures invoke Awake explicitly in EditMode, so pair the singleton lifecycle explicitly too.
            var flute = _root != null ? _root.GetComponent<OrpheusFluteChannel>() : null;
            if (flute != null) { flute.ExitFluteStance(); Call(flute, "OnDestroy"); }
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (!_before.Contains(go)) Object.DestroyImmediate(go);
        }

        [Test]
        public void DashNeverSpawnsASecondCharacterSprite()
        {
            Set(_player, "_dashDirXZ", UnderworldCamera2D.IsoRightXZ);
            Call(_player, "BeginDashCommon");
            var clones = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Where(r => r.sprite == _animator.BodyRenderer.sprite && r != _animator.BodyRenderer);
            Assert.That(clones, Is.Empty, "A dash should leave a brush streak, not another Orpheus.");
        }

        [Test]
        public void ReleasingMovementPlantsFeetImmediately()
        {
            Set(_player, "_velocityXZ", UnderworldCamera2D.IsoRightXZ * _player.moveSpeed);
            Vector3 before = _root.transform.position;
            Call(_player, "Update25DIsometricMovement", 1f / 60f, Vector2.zero, null, null);
            Assert.That(Vector3.Distance(before, _root.transform.position), Is.LessThan(.001f));
        }

        [Test]
        public void ReversingMovementDoesNotDriftInTheOldDirection()
        {
            Set(_player, "_velocityXZ", UnderworldCamera2D.IsoRightXZ * _player.moveSpeed);
            Call(_player, "Update25DIsometricMovement", 1f / 60f, Vector2.left, null, null);
            Assert.That(Vector3.Dot(_root.transform.position, UnderworldCamera2D.IsoRightXZ), Is.LessThan(0f));
        }

        [Test]
        public void BossRailHasNoSecondPositionSmoothingLayer()
        {
            _player.Enter2DBossStage(Vector3.zero);
            _root.transform.position = UnderworldCamera2D.IsoRightXZ * -4.2f;
            Call(_player, "Update2DBossStageMovement", 1f / 60f, 1f);
            float rail = (float)Get(_player, "_stageHorizPos");
            Assert.That(Vector3.Distance(_root.transform.position, UnderworldCamera2D.IsoRightXZ * rail), Is.LessThan(.001f));
        }

        [Test]
        public void MovementInterruptsPassiveCelebrationAndDashRecovery()
        {
            _animator.TriggerChamberClear();
            _animator.Drive(Motion(.2f, .016f));
            Assert.That(_animator.CurrentState, Is.Not.EqualTo(OrpheusAnimationState.ChamberClear));
            _animator.TriggerDashSettle();
            _animator.Drive(Motion(.2f, .016f));
            Assert.That(_animator.CurrentState, Is.Not.EqualTo(OrpheusAnimationState.DashSettle));
        }

        [Test]
        public void DashDirectionOverridesAnIdleSingingPose()
        {
            _animator.SetSinging(true);
            var input = Motion(.4f, .016f);
            input.dashing = true;
            input.aimDirection = UnderworldCamera2D.IsoUpXZ;
            _animator.Drive(input);
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.DashTravel));
            Assert.That(_animator.Facing, Is.EqualTo(EurydiceFacing.East));
        }

        [Test]
        public void RepeatingTheSameAttackRestartsItsAnticipation()
        {
            _animator.TriggerSoftAttack(1);
            _animator.Advance(.08f);
            Assert.That(_animator.CurrentFrame, Is.GreaterThan(0));
            _animator.TriggerSoftAttack(1);
            Assert.That(_animator.CurrentFrame, Is.Zero);
            Assert.That(_animator.Elapsed, Is.Zero);
        }

        [Test]
        public void ReleasingAulosCancelsItsQueuedSustain()
        {
            _animator.TriggerSoftAttack(1);
            _animator.EnterFlute();
            _animator.SetAulosLoop(true);
            _animator.SetAulosLoop(false);
            _animator.Advance(2f);
            _animator.Advance(2f);
            _animator.Drive(new OrpheusAnimationInput { deltaTime = .016f });
            Assert.That(_animator.CurrentState, Is.Not.EqualTo(OrpheusAnimationState.AulosLoop));
        }

        [Test]
        public void DashLandingClearsPreDashRunningMomentum()
        {
            Set(_player, "_velocityXZ", UnderworldCamera2D.IsoRightXZ * _player.moveSpeed);
            Set(_player, "_dashDirXZ", UnderworldCamera2D.IsoUpXZ);
            Call(_player, "BeginDashCommon");
            Assert.That(((Vector3)Get(_player, "_velocityXZ")).sqrMagnitude, Is.LessThan(.001f));
        }

        [Test]
        public void SustainedSingingUsesDistinctCleanVocalPoses()
        {
            var library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            var clip = library.FindClip(OrpheusAnimationState.SingSustain);
            Assert.That(clip.frames.Take(clip.framesPerDirection).Distinct().Count(), Is.GreaterThan(1));
            Assert.That(clip.frames.All(s => s.name.StartsWith("LyreSinging")), Is.True);
        }

        [Test]
        public void GaitAdvancesByTravelRatherThanTimeSpentSliding()
        {
            var otherRoot = new GameObject("SlowerGait");
            var other = otherRoot.AddComponent<OrpheusSpriteAnimator>();
            other.Initialize(otherRoot.AddComponent<SpriteRenderer>(), null, null,
                Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath));
            foreach (var animator in new[] { _animator, other })
            {
                animator.Drive(Motion(1.2f, .2f));
                animator.Drive(Motion(1.2f, .2f));
            }
            for (int i = 0; i < 10; i++)
            {
                _animator.Drive(Motion(.1f, .01f)); _animator.Advance(.01f);
                other.Drive(Motion(.1f, .02f)); other.Advance(.02f);
            }
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Run));
            Assert.That(other.CurrentState, Is.EqualTo(OrpheusAnimationState.Run));
            Assert.That(other.CurrentFrame, Is.EqualTo(_animator.CurrentFrame), "Equal ground distance must produce equal foot phase.");
        }

        [Test]
        public void RecoveryFramesDoNotDrawTheNeighborsSlash()
        {
            var library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            foreach (Sprite sprite in library.FindClip(OrpheusAnimationState.DashSettle).frames.Distinct())
            {
                int[] areas = VisibleComponents(sprite);
                Assert.That(areas.Skip(1).DefaultIfEmpty(0).Max(), Is.LessThan(areas[0] * .02f), sprite.name);
            }
        }

        [Test]
        public void ReleasedDashShowsItsLandingPose()
        {
            Set(_player, "_dashDirXZ", UnderworldCamera2D.IsoRightXZ);
            Call(_player, "BeginDashCommon");
            Set(_player, "_dashTimer", .001f);
            Call(_player, "Update25DIsometricMovement", .016f, Vector2.zero, null, null);
            Call(_player, "UpdateVisualPose");
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.DashSettle));
        }

        [Test]
        public void VictoryDuringDashStopsTravelAndKeepsItsTerminalPose()
        {
            Set(_player, "_dashDirXZ", UnderworldCamera2D.IsoRightXZ);
            Call(_player, "BeginDashCommon");
            _player.TriggerRunVictory();
            Call(_player, "UpdateVisualPose");
            Assert.That(_player.IsDashing, Is.False);
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Victory));
            _animator.Advance(3f);
            Call(_player, "UpdateVisualPose");
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Victory));
        }

        [Test]
        public void MovingOutOfComboRecoveryKeepsTheCommittedThirdBolt()
        {
            _animator.TriggerSoftPerformance(3);
            _animator.Advance(.3f);
            _animator.Drive(Motion(.2f, .016f));
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SoftBolt));
        }

        [Test]
        public void WalkingDuringSongUsesItsFeetAndResumesSingingWhenPlanted()
        {
            _animator.SetSinging(true);
            _animator.Drive(Motion(.2f, .016f));
            Assert.That(_animator.CurrentState, Is.Not.EqualTo(OrpheusAnimationState.SingSustain));
            _animator.Drive(Motion(0f, .016f));
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SingSustain));
        }

        [Test]
        public void EveryInstalledBodyFrameExcludesDetachedAtlasFragments()
        {
            var library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            foreach (Sprite sprite in library.clips.SelectMany(c => c.frames).Distinct())
            {
                int[] areas = VisibleComponents(sprite);
                Assert.That(areas, Is.Not.Empty, sprite.name);
                Assert.That(areas.Skip(1).DefaultIfEmpty(0).Max(), Is.LessThan(areas[0] * .02f), sprite.name);
            }
        }

        [Test]
        public void FluteGuideSitsAboveTheProductionSilhouette()
        {
            var flute = _root.AddComponent<OrpheusFluteChannel>();
            Call(flute, "Awake");
            flute.EnterFluteStance();
            Transform guide = _root.transform.Find("FluteStaveBillboard");
            Vector3 screenUp = UnderworldCamera2D.CurrentBillboardRotation * Vector3.up;
            Vector3 top = _animator.BodyRenderer.transform.TransformPoint(Vector3.up * _animator.BodyRenderer.sprite.bounds.max.y);
            Assert.That(Vector3.Dot(guide.position - top, screenUp), Is.GreaterThan(.25f));
            Assert.That(_root.transform.Find("AulosFluteVisual").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void GriefSettlesWithoutStandingUpAtTheLoopBoundary()
        {
            _animator.TriggerGrief();
            _animator.Advance(.7f);
            Sprite settled = _animator.BodyRenderer.sprite;
            _animator.Advance(.11f);
            Assert.That(_animator.BodyRenderer.sprite, Is.SameAs(settled));
            _animator.Advance(5f);
            Assert.That(_animator.BodyRenderer.sprite, Is.SameAs(settled));
        }

        [Test]
        public void VictoryRaisesTheLyreAndHoldsItsFinalPose()
        {
            _animator.TriggerVictory();
            _animator.Advance(.7f);
            Sprite settled = _animator.BodyRenderer.sprite;
            _animator.Advance(.5f);
            Assert.That(_animator.BodyRenderer.sprite, Is.SameAs(settled));
            _animator.Advance(5f);
            Assert.That(_animator.BodyRenderer.sprite, Is.SameAs(settled));
        }

        // Rasterize the actual sprite mesh, not just its rectangle: excluded atlas fragments must be invisible.
        private static int[] VisibleComponents(Sprite sprite)
        {
            var pixels = sprite.texture.GetPixels32();
            Vector2[] vertices = sprite.vertices.Select(v => v * sprite.pixelsPerUnit + sprite.pivot).ToArray();
            Vector2[] uv = sprite.uv;
            int left = Mathf.FloorToInt(vertices.Min(v => v.x)), bottom = Mathf.FloorToInt(vertices.Min(v => v.y));
            int w = Mathf.CeilToInt(vertices.Max(v => v.x)) - left + 1;
            int h = Mathf.CeilToInt(vertices.Max(v => v.y)) - bottom + 1;
            var mask = new bool[w * h];
            for (int i = 0; i < vertices.Length; i++) vertices[i] -= new Vector2(left, bottom);
            ushort[] triangles = sprite.triangles;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int ia = triangles[t], ib = triangles[t + 1], ic = triangles[t + 2];
                Vector2 a = vertices[ia], b = vertices[ib], c = vertices[ic];
                float determinant = Cross(b - a, c - a);
                if (Mathf.Abs(determinant) < .000001f) continue;
                int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
                int maxX = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
                int maxY = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + .5f, y + .5f) - a;
                    float u = Cross(p, c - a) / determinant, v = Cross(b - a, p) / determinant;
                    if (u < 0f || v < 0f || u + v > 1f) continue;
                    Vector2 sample = uv[ia] + (uv[ib] - uv[ia]) * u + (uv[ic] - uv[ia]) * v;
                    int sx = Mathf.Clamp(Mathf.FloorToInt(sample.x * sprite.texture.width), 0, sprite.texture.width - 1);
                    int sy = Mathf.Clamp(Mathf.FloorToInt(sample.y * sprite.texture.height), 0, sprite.texture.height - 1);
                    if (pixels[sy * sprite.texture.width + sx].a >= 40) mask[y * w + x] = true;
                }
            }
            var areas = new System.Collections.Generic.List<int>();
            var queue = new System.Collections.Generic.Queue<int>();
            for (int i = 0; i < mask.Length; i++)
            {
                if (!mask[i]) continue;
                mask[i] = false; queue.Enqueue(i); int area = 0;
                while (queue.Count > 0)
                {
                    int p = queue.Dequeue(); area++;
                    int x = p % w, y = p / w;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h || !mask[ny * w + nx]) continue;
                        mask[ny * w + nx] = false; queue.Enqueue(ny * w + nx);
                    }
                }
                areas.Add(area);
            }
            return areas.OrderByDescending(v => v).ToArray();
        }
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static OrpheusAnimationInput Motion(float distance, float dt) => new OrpheusAnimationInput
        {
            deltaTime = dt, groundDelta = UnderworldCamera2D.IsoRightXZ * distance,
            aimDirection = UnderworldCamera2D.IsoRightXZ
        };
        private static object Call(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static object Get(object target, string name)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
