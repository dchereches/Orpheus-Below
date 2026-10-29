using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public class OrpheusAuthoredRunMotionTests
    {
        private GameObject _root;
        private OrpheusSpriteAnimator _animator;
        private SpriteRenderer _body;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("AuthoredRunMotion");
            _animator = _root.AddComponent<OrpheusSpriteAnimator>();
            _body = _root.AddComponent<SpriteRenderer>();
            _animator.Initialize(_body, null, null, Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath));
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void ActualTravelVisitsEveryAuthoredPoseOncePerCycleAtANaturalCadence()
        {
            const int frames = 4;
            float distance = OrpheusSpriteAnimator.RunCycleDistance / frames;
            // At the normal six-unit speed the four contact/passing keys run between
            // seven and nine keys per second, instead of racing a high-knee 24fps rig.
            Assert.That(6f * frames / OrpheusSpriteAnimator.RunCycleDistance, Is.InRange(7f, 9f));
            for (int phase = 1; phase <= frames; phase++)
            {
                _animator.Drive(Motion(distance, distance / 6f));
                Assert.That(_animator.CurrentFrame, Is.EqualTo(phase % frames));
                Assert.That(_body.sprite.name, Does.StartWith("NaturalRun_"));
            }
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Run));
        }

        [Test]
        public void EqualTravelKeepsTheSameFootPhaseAtDifferentFrameRatesAndSpeeds()
        {
            int[] expected = new int[12];
            for (int i = 0; i < expected.Length; i++)
            {
                _animator.Drive(Motion(.21f, .035f)); expected[i] = _animator.CurrentFrame;
            }
            _animator.ResetMotion();
            for (int i = 0; i < expected.Length; i++)
            {
                _animator.Drive(Motion(.21f, .02333333f));
                Assert.That(_animator.CurrentFrame, Is.EqualTo(expected[i]));
            }
        }

        [Test]
        public void AdvanceWithoutGroundTravelCannotCycleTheFeetAndReleaseSettlesToIdle()
        {
            _animator.Drive(Motion(.6f, .1f)); _animator.Drive(Motion(.6f, .1f));
            int frame = _animator.CurrentFrame; Sprite paint = _body.sprite;
            _animator.Advance(.8f);
            Assert.That(_animator.CurrentFrame, Is.EqualTo(frame));
            Assert.That(_body.sprite, Is.SameAs(paint));
            _animator.Drive(Motion(0f, .016f));
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Stop));
            Assert.That(_body.sprite.texture.name, Is.EqualTo("IdleTransitions"));
            _animator.Drive(Motion(0f, .1f));
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Idle));
            Assert.That(_body.sprite.texture.name, Is.EqualTo("IdleTransitions"));
        }

        [Test]
        public void BossBillboardScaleCompensatesTravelWithoutStretchingThePaint()
        {
            _animator.Drive(Motion(.6f, .1f)); _animator.Drive(Motion(.6f, .1f));
            int normalPhase = _animator.CurrentFrame;
            _animator.ResetMotion(); _body.transform.localScale = Vector3.one * .7f;
            _animator.Drive(Motion(.42f, .07f)); _animator.Drive(Motion(.42f, .07f));
            Assert.That(_animator.CurrentFrame, Is.EqualTo(normalPhase));
            Assert.That(_body.transform.localScale, Is.EqualTo(Vector3.one * .7f));
        }

        [TestCase(true)] [TestCase(false)]
        public void FastStartTurnAndStairsKeepTheSameCoherentRunPaint(bool ascending)
        {
            _animator.Drive(Motion(.6f, .1f));
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Start));
            Assert.That(_body.sprite.name, Does.StartWith("NaturalRun_"));
            var turn = Motion(-.6f, .1f); _animator.Drive(turn);
            Assert.That(_body.sprite.name, Does.StartWith("NaturalRun_"));
            var stairs = Motion(-.6f, .1f); stairs.onStairs = true; stairs.ascending = ascending;
            _animator.Drive(stairs);
            Assert.That(_animator.CurrentState, Is.EqualTo(ascending ? OrpheusAnimationState.StairAscent : OrpheusAnimationState.StairDescent));
            Assert.That(_body.sprite.name, Does.StartWith("NaturalRun_"));
        }

        [Test]
        public void DashAndIdleReturnToTheirOwnAuthoredSources()
        {
            _animator.Drive(Motion(.6f, .1f));
            var dash = Motion(.6f, .1f); dash.dashing = true;
            _animator.Drive(dash);
            Assert.That(_animator.CurrentState, Is.EqualTo(OrpheusAnimationState.DashTravel));
            Assert.That(_body.sprite.texture.name, Is.EqualTo("RunCycle"));
            _animator.ResetMotion();
            Assert.That(_body.sprite.texture.name, Is.EqualTo("IdleTransitions"));
        }

        [Test]
        public void PauseFreezesPoseAndGroundPhase()
        {
            _animator.Drive(Motion(.6f, .1f));
            Sprite paint = _body.sprite; int frame = _animator.CurrentFrame;
            var paused = Motion(.6f, .1f); paused.paused = true;
            _animator.Drive(paused); _animator.Advance(.5f, true);
            Assert.That(_animator.CurrentFrame, Is.EqualTo(frame));
            Assert.That(_body.sprite, Is.SameAs(paint));
        }

        private static OrpheusAnimationInput Motion(float distance, float dt) => new OrpheusAnimationInput
        { groundDelta = UnderworldCamera2D.IsoRightXZ * distance, aimDirection = UnderworldCamera2D.IsoRightXZ * Mathf.Sign(distance),
            deltaTime = dt, movementRequested = distance != 0f };
    }
}
