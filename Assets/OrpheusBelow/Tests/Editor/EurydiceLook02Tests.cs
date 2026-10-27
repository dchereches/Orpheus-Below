using NUnit.Framework;
using UnityEngine;
using System.Reflection;

namespace OrpheusBelow.Tests
{
    public class EurydiceLook02Tests
    {
        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [Test]
        public void BodyFramesNeverIncludeAHeadOrFeetFromTheAdjacentAtlasRow()
        {
            var library = Resources.Load<EurydiceAnimationLibrary>(EurydiceAnimationLibrary.ResourcePath);
            // Last sanctuary column extends below the other columns: keep its robe out of Shatter/Reform.
            Assert.That(library.reactions[29].rect.yMin, Is.LessThanOrEqualTo(198f));
            Assert.That(library.reactions[35].rect.yMax, Is.LessThan(198f));
            foreach (var frames in new[] { library.idle, library.walk, library.reactions, library.stairs })
            {
                Color32[] pixels = frames[0].texture.GetPixels32(); int width = frames[0].texture.width;
                foreach (var sprite in frames)
                {
                    if (sprite.name.StartsWith("Reactions_05")) continue;
                    var r = sprite.rect; bool seen = false; int gap = 0;
                    for (int y = Mathf.CeilToInt(r.yMin); y < r.yMax; y++)
                    {
                        int count = 0;
                        for (int x = Mathf.CeilToInt(r.xMin); x < r.xMax; x++) if (pixels[y * width + x].a > 128) count++;
                        if (count > 2)
                        {
                            Assert.That(!seen || gap < 4, Is.True, sprite.name + " contains two vertically separated body pieces; atlas gutters need detecting.");
                            seen = true; gap = 0;
                        }
                        else if (seen) gap++;
                    }
                }
            }
        }

        [Test]
        public void TutorialCallbackSelectsTeachingImmediatelyEvenWhenGameTimeIsZero()
        {
            var root = new GameObject("PausedLesson");
            float timeScale = Time.timeScale;
            try
            {
                var companion = root.AddComponent<BlurredEurydice>();
                root.AddComponent<SpriteRenderer>(); var animator = root.AddComponent<EurydiceSpriteAnimator>();
                typeof(EurydiceSpriteAnimator).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animator, null);
                Set(companion, "_spriteAnimator", animator); Set(companion, "_reformTimer", .9f);
                Time.timeScale = 0;
                companion.SetTeachingPose(true);
                Assert.That(animator.CurrentState, Is.EqualTo(EurydiceAnimationState.Teaching));
                companion.SnapBehindOrpheus(Vector3.zero);
                typeof(BlurredEurydice).GetMethod("UpdateGeneratedSpriteAnimation", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(companion, new object[] { 0f });
                Assert.That(animator.CurrentState, Is.EqualTo(EurydiceAnimationState.Teaching), "An arrival reset during an already-open lesson must retain teaching.");
            }
            finally { Time.timeScale = timeScale; Object.DestroyImmediate(root); }
        }

        [Test]
        public void Look02FeetAnchorProjectsToTheActualGroundPosition()
        {
            var root = new GameObject("GroundAnchor");
            try
            {
                var companion = root.AddComponent<BlurredEurydice>();
                typeof(BlurredEurydice).GetMethod("BuildBlurredVisualStack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(companion, null);
                var animator = root.GetComponentInChildren<EurydiceSpriteAnimator>();
                Vector3 offset = animator.transform.position - root.transform.position;
                Vector3 screenUp = RenderPipeline2DSetup.IsoCameraRotation * Vector3.up;
                Assert.That(Vector3.Dot(offset, screenUp), Is.EqualTo(0f).Within(.01f), "Feet must align with the collision/depth-sort root.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void BreadcrumbFollowerKeepsUpDuringTenSecondsOfNormalRunning()
        {
            var root = new GameObject("Follower"); var leader = new GameObject("Leader");
            try
            {
                var companion = root.AddComponent<BlurredEurydice>(); companion.orpheus = leader.transform;
                root.transform.position = Vector3.left * companion.followLagDistance;
                Set(companion, "_desiredPos", root.transform.position);
                var follow = typeof(BlurredEurydice).GetMethod("FollowBreadcrumbs", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(follow, Is.Not.Null, "Following needs a time-step travel budget that can cross waypoint boundaries.");
                for (int i = 0; i < 600; i++)
                {
                    leader.transform.position += Vector3.right * (9.6f / 60f);
                    follow.Invoke(companion, new object[] { 1f / 60f, companion.followLagDistance });
                }
                Assert.That(Vector3.Distance(root.transform.position, leader.transform.position), Is.InRange(3.5f, 5f));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(leader); }
        }

        [Test]
        public void ShatterCleanupUsesTheSamePausedGameClockAsItsAnimation()
        {
            var root = new GameObject("ShatterClock");
            try
            {
                var companion = root.AddComponent<BlurredEurydice>();
                var routine = (System.Collections.IEnumerator)typeof(BlurredEurydice)
                    .GetMethod("ShatterRoutine", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(companion, null);
                Assert.That(routine.MoveNext(), Is.True);
                Assert.That(routine.Current, Is.TypeOf<WaitForSeconds>(), "Paused cleanup must wait for the scaled animation clock.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void BreadcrumbFollowerRetainsCornersAroundSolidCover()
        {
            var root = new GameObject("CornerFollower"); var leader = new GameObject("CornerLeader"); var wall = new GameObject("Cover");
            try
            {
                wall.transform.position = Vector3.up; wall.AddComponent<BoxCollider>().size = new Vector3(2, 2, 2); wall.AddComponent<IsoObstacle>();
                var companion = root.AddComponent<BlurredEurydice>(); companion.orpheus = leader.transform;
                root.transform.position = new Vector3(-2, 0, 0); leader.transform.position = new Vector3(2, 0, 0);
                Set(companion, "_desiredPos", root.transform.position); Set(companion, "_lastRecordedPos", leader.transform.position);
                var queue = (System.Collections.Generic.Queue<Vector3>)typeof(BlurredEurydice).GetField("_breadcrumbs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(companion);
                queue.Enqueue(new Vector3(-2, 0, 3)); queue.Enqueue(new Vector3(2, 0, 3)); queue.Enqueue(new Vector3(2, 0, 0));
                Physics.SyncTransforms();
                var follow = typeof(BlurredEurydice).GetMethod("FollowBreadcrumbs", BindingFlags.Instance | BindingFlags.NonPublic);
                for (int i = 0; i < 180; i++)
                {
                    follow.Invoke(companion, new object[] { 1f / 60f, 0f });
                    Vector3 pos = root.transform.position;
                    Assert.That(Mathf.Abs(pos.x) > 1.25f || Mathf.Abs(pos.z) > 1.25f, Is.True, "The recorded corner must keep the companion outside cover.");
                }
                Assert.That(Vector3.Distance(root.transform.position, leader.transform.position), Is.LessThan(.15f));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(leader); Object.DestroyImmediate(wall); }
        }

        [TestCase(0,-1,EurydiceFacing.South)]
        [TestCase(1,-1,EurydiceFacing.SouthEast)]
        [TestCase(1,0,EurydiceFacing.East)]
        [TestCase(1,1,EurydiceFacing.NorthEast)]
        [TestCase(0,1,EurydiceFacing.North)]
        [TestCase(-1,1,EurydiceFacing.NorthWest)]
        [TestCase(-1,0,EurydiceFacing.West)]
        [TestCase(-1,-1,EurydiceFacing.SouthWest)]
        public void ResolvesAllEightGroundDirections(int x, int y, EurydiceFacing facing)
        {
            Vector3 direction = UnderworldCamera2D.IsoRightXZ * x + UnderworldCamera2D.IsoUpXZ * y;
            Assert.That(EurydiceAnimationBrain.FacingFor(direction, EurydiceFacing.South), Is.EqualTo(facing));
        }

        [Test]
        public void StoppingPreservesBackFacingAndRejectsVerticalMotionAndSpeedJitter()
        {
            var brain = new EurydiceAnimationBrain();
            brain.Step(new EurydiceAnimationInput { groundDelta = UnderworldCamera2D.IsoUpXZ, deltaTime = .25f });
            for (int i = 0; i < 100; i++)
                brain.Step(new EurydiceAnimationInput { groundDelta = Vector3.up * Mathf.Sin(i) * .1f, deltaTime = .016f });
            Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Idle));
            Assert.That(brain.Facing, Is.EqualTo(EurydiceFacing.North));
            for (int i = 0; i < 20; i++) brain.Step(new EurydiceAnimationInput { groundDelta = Vector3.right * .002f, deltaTime = .016f });
            Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Idle));
        }

        [Test]
        public void DangerousEventsOverrideCosmeticGesturesAndLossOverridesEverything()
        {
            var brain = new EurydiceAnimationBrain();
            var input = new EurydiceAnimationInput { deltaTime = .1f, joy = true, teaching = true, replying = true, shield = true };
            brain.Step(input); Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Sanctuary));
            input.threatened = true; brain.Step(input); Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Fear));
            input.vanished = true; input.reforming = true; brain.Step(input); Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Shatter));
            input.paused = true; input.vanished = false; input.threatened = false;
            brain.Step(input); Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Shatter));
        }

        [Test]
        public void ArrivalJoyTeachingAndReplyHaveDistinctStates()
        {
            var brain = new EurydiceAnimationBrain();
            var input = new EurydiceAnimationInput { deltaTime = .1f, reforming = true, joy = true, teaching = true, replying = true };
            brain.Step(input); Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Reform));
            input.reforming = false; brain.Step(input); Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Joy));
            input.joy = false; brain.Step(input); Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Teaching));
            input.teaching = false; brain.Step(input); Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Listen));
        }

        [Test]
        public void StairMotionOverridesWalkingOnlyWhileMovingAndFollowsUpDownAxis()
        {
            var d = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_04");
            Assert.That(d.stairs.Length, Is.GreaterThan(0));
            var stair = d.stairs[0]; Vector2 center = Vector2.zero;
            foreach (var p in stair.polygon) center += p; center /= stair.polygon.Length;
            Vector3 up = d.ImageToGround(center + stair.uphill) - d.ImageToGround(center);
            Assert.That(d.StairMotion(d.ImageToGround(center), up, out bool ascending), Is.True);
            Assert.That(ascending, Is.True);
            Assert.That(d.StairMotion(d.ImageToGround(center), -up, out ascending), Is.True);
            Assert.That(ascending, Is.False);
            Assert.That(d.StairMotion(d.ImageToGround(center), Vector3.up, out ascending), Is.False);
            var brain = new EurydiceAnimationBrain();
            brain.Step(new EurydiceAnimationInput { groundDelta = up.normalized, deltaTime = .2f, onStairs = true, ascending = true });
            Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.StairAscent));
            brain.Step(new EurydiceAnimationInput { groundDelta = -up.normalized, deltaTime = .2f, onStairs = true });
            Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.StairDescent));
            brain.Step(new EurydiceAnimationInput { deltaTime = .3f, onStairs = true });
            Assert.That(brain.State, Is.EqualTo(EurydiceAnimationState.Idle));
        }

        [Test]
        public void Look02LibraryHasEveryFrameWithActualTransparencyAndLargerSilhouette()
        {
            var library = Resources.Load<EurydiceAnimationLibrary>(EurydiceAnimationLibrary.ResourcePath);
            Assert.That(library, Is.Not.Null); Assert.That(library.IsComplete, Is.True);
            Assert.That(library.idle[0].bounds.size.y, Is.InRange(2.45f, 2.65f));
            foreach (var frames in new[] { library.idle, library.walk, library.reactions, library.stairs, library.effects })
            {
                Assert.That(System.Array.TrueForAll(frames, s => s != null), Is.True);
                Texture2D texture = frames[0].texture;
                int clear = 0; foreach (Color32 p in texture.GetPixels32()) if (p.a < 10) clear++;
                Assert.That(clear, Is.GreaterThan(texture.width * texture.height / 5));
            }
            Assert.That(library.Frame(EurydiceAnimationState.Reform, EurydiceFacing.South, 5),
                Is.SameAs(library.Frame(EurydiceAnimationState.Shatter, EurydiceFacing.South, 0)));
        }

        [Test]
        public void PlaybackFreezesWhenPausedAndTerminalClipsHoldTheirLastFrame()
        {
            var go = new GameObject("Playback");
            try
            {
                go.AddComponent<SpriteRenderer>(); var animator = go.AddComponent<EurydiceSpriteAnimator>();
                typeof(EurydiceSpriteAnimator).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animator, null);
                Assert.That(animator.UsesLook02, Is.True);
                animator.Advance(.2f); Assert.That(animator.CurrentFrame, Is.Zero);
                animator.Advance(.6f); float elapsed = animator.Elapsed;
                animator.SetState(EurydiceAnimationState.Idle);
                Assert.That(animator.Elapsed, Is.EqualTo(elapsed));
                animator.Advance(10f, true); Assert.That(animator.Elapsed, Is.EqualTo(elapsed));
                animator.SetState(EurydiceAnimationState.Shatter); animator.Advance(10);
                Assert.That(animator.CurrentFrame, Is.EqualTo(5));
                animator.Advance(10); Assert.That(animator.CurrentFrame, Is.EqualTo(5));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void VerticalBobbingWhileStationaryDoesNotTriggerWalking()
        {
            var root = new GameObject("StationaryEurydice");
            try
            {
                var companion = root.AddComponent<BlurredEurydice>();
                var visual = new GameObject("Sprite"); visual.transform.SetParent(root.transform);
                visual.AddComponent<SpriteRenderer>();
                var animator = visual.AddComponent<EurydiceSpriteAnimator>();
                typeof(EurydiceSpriteAnimator).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animator, null);
                Set(companion, "_spriteAnimator", animator);
                Set(companion, "_lastAnimationPosition", new Vector3(0, -.08f, 0));
                root.transform.position = new Vector3(0, .08f, 0);
                typeof(BlurredEurydice).GetMethod("UpdateGeneratedSpriteAnimation", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(companion, new object[] { .016f });
                Assert.That(animator.CurrentState, Is.EqualTo(EurydiceAnimationState.Idle),
                    "A stationary companion's vertical breathing/bobbing must never select locomotion.");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
