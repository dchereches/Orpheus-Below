using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class SkeletonPressureTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _owner, _playerObject;
        private PaintedEnemyActor _actor;
        private HoodedSkeletonAttack _attack;
        private OrpheusPlayerController _previousPlayer;
        private float _timeScale;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale;
            Time.timeScale = 1;
            _previousPlayer = OrpheusPlayerController.Instance;
            _playerObject = new GameObject("SkeletonPressurePlayer");
            var player = _playerObject.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, player);
            _owner = new GameObject("SkeletonPressureOwner");
            _owner.transform.position = Vector3.right * 52000;
            _playerObject.transform.position = _owner.transform.position + Vector3.right * 6;
            var shade = _owner.AddComponent<ShadeEnemy>();
            shade.SetupArchetype(ShadeArchetype.BrimstoneCaster);
            _actor = shade.PaintedActor;
            _attack = (HoodedSkeletonAttack)_actor.Pattern;
        }

        [TearDown]
        public void TearDown()
        {
            _attack.Cancel();
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_playerObject);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            Time.timeScale = _timeScale;
        }

        [Test]
        public void ThreeAttackCycleEmitsTwoFourAndSixActualOwnedProjectiles()
        {
            var variants = new[] { HoodedSkeletonAttack.GunAttack.Doubletap,
                HoodedSkeletonAttack.GunAttack.OpenFan, HoodedSkeletonAttack.GunAttack.ClosingVolley };
            var counts = new[] { 2, 4, 6 };
            for (int cast = 0; cast < variants.Length; cast++)
            {
                Begin();
                Assert.That(_attack.Variant, Is.EqualTo(variants[cast]));
                Assert.That(_attack.PendingProjectileCount, Is.EqualTo(counts[cast]));
                int before = _attack.ShotCount;
                FinishVolley();
                Assert.That(_attack.ShotCount - before, Is.EqualTo(counts[cast]));
                Assert.That(_attack.VolleyCount, Is.EqualTo(cast + 1));
                Assert.That(_actor.Art.Motion, Is.EqualTo(cast == 0 ? EnemyMotion.Attack : EnemyMotion.Attack2));
            }
            Assert.That(_attack.ActiveProjectileCount, Is.EqualTo(12));
            Assert.That(Shots().All(shot => shot.Owner == _actor), Is.True);
        }

        [Test]
        public void OpenFanHasFourSeparateLanesAndLeavesTheCommittedCenterOpen()
        {
            Begin(); FinishVolley();
            foreach (var shot in Shots().ToArray()) shot.Remove();
            Begin(); FinishVolley();
            var angles = Shots().Select(Angle).OrderBy(angle => angle).ToArray();
            Assert.That(angles, Is.EqualTo(new[] { -32f, -12f, 12f, 32f }).Within(.01f));
            Assert.That(angles.All(angle => Mathf.Abs(angle) >= 12f - .01f), Is.True);
        }

        [Test]
        public void ClosingVolleyNarrowsInThreeDistinctTimedPairsWithoutTracking()
        {
            Begin(); FinishVolley(); Begin(); FinishVolley();
            foreach (var shot in Shots().ToArray()) shot.Remove();
            Begin(); FireFirst();
            Assert.That(Shots().Count, Is.EqualTo(2));
            Assert.That(_attack.PendingProjectileCount, Is.EqualTo(4));
            AssertAngles(24f);
            _playerObject.transform.position = _owner.transform.position + Vector3.forward * 7;
            _attack.Tick(.1f);
            Assert.That(Shots().Count, Is.EqualTo(2));
            _attack.Tick(.1f);
            Assert.That(Shots().Count, Is.EqualTo(4));
            AssertAngles(24f, 16f);
            _attack.Tick(.1f); _attack.Tick(.1f);
            Assert.That(Shots().Count, Is.EqualTo(6));
            AssertAngles(24f, 16f, 8f);
            Assert.That(_attack.HasPendingShot, Is.False);
        }

        [Test]
        public void DoubletapLocksBothShotsButUsesTheLiveMuzzleForItsFollowup()
        {
            Begin();
            Vector3 aim = _attack.LockedDirection;
            _playerObject.transform.position = _owner.transform.position + Vector3.forward * 7;
            FireFirst();
            Assert.That(Shots().Count, Is.EqualTo(1));
            Assert.That(_attack.VolleyCount, Is.EqualTo(1));
            Assert.That(_attack.HasPendingShot, Is.True);
            _owner.transform.position += Vector3.forward * 3;
            _attack.Tick(.1f);
            Assert.That(Shots().Count, Is.EqualTo(1));
            _attack.Tick(.1f);
            Assert.That(Shots().Count, Is.EqualTo(2));
            Assert.That(Shots().All(shot => Vector3.Dot(Direction(shot), aim) > .999f), Is.True);
            Assert.That(Vector3.Distance(Shots()[1].transform.position,
                _owner.transform.position + Vector3.up * .78f + aim * .55f), Is.LessThan(.001f));
            float speed = (float)typeof(PaintedEnemyProjectile).GetField("_speed", Hidden).GetValue(Shots()[1]);
            Assert.That(speed, Is.GreaterThan(13f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StunAndDefeatRemoveActualShotsEffectsAndTheDelayedFollowup(bool defeated)
        {
            Begin(); FireFirst();
            var first = Shots()[0];
            Assert.That(_attack.OwnedEffectCount, Is.GreaterThan(0));
            if (defeated) _actor.Cancel(true);
            else _owner.GetComponent<ShadeEnemy>().ApplyStun(1f);
            Assert.That(first == null, Is.True);
            Assert.That(_attack.HasPendingShot, Is.False);
            Assert.That(_attack.PendingProjectileCount, Is.Zero);
            Assert.That(_attack.ActiveProjectileCount, Is.Zero);
            Assert.That(_attack.OwnedEffectCount, Is.Zero);
            Assert.That(_attack.VolleyCount, Is.Zero, "Cancellation preserves the original volley counter reset contract.");
            Assert.That(_actor.Art.Motion, Is.EqualTo(defeated ? EnemyMotion.Defeated : EnemyMotion.Hurt));
        }

        [Test]
        public void PauseFreezesWarningAndFollowupAndTerminalStateStillCancelsWhilePaused()
        {
            Begin();
            float remaining = _attack.PhaseRemaining;
            Time.timeScale = 0;
            _attack.Tick(10);
            Assert.That(_attack.PhaseRemaining, Is.EqualTo(remaining));
            Assert.That(_attack.ShotCount, Is.Zero);
            Time.timeScale = 1;
            FireFirst();
            remaining = _attack.PhaseRemaining;
            Time.timeScale = 0;
            _attack.Tick(10);
            Assert.That(_attack.ShotCount, Is.EqualTo(1));
            Assert.That(_attack.PhaseRemaining, Is.EqualTo(remaining));
            var player = _playerObject.GetComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetField("<IsVictory>k__BackingField", Hidden).SetValue(player, true);
            _attack.Tick(.1f);
            Assert.That(_attack.HasPendingShot, Is.False);
            Assert.That(_attack.ActiveProjectileCount, Is.Zero);
            Assert.That(_attack.OwnedEffectCount, Is.Zero);
        }

        [Test]
        public void RepeatedCastsRespectProjectileCapAndStalledFramesCannotSkipTheWarning()
        {
            Begin();
            _attack.Tick(10);
            Assert.That(_attack.Phase, Is.EqualTo(EnemyMotion.Windup));
            Assert.That(_attack.ShotCount, Is.Zero);
            for (int i = 0; i < 400; i++) _attack.Tick(.05f);
            Assert.That(_attack.VolleyCount, Is.GreaterThan(5));
            Assert.That(_attack.ActiveProjectileCount, Is.EqualTo(HoodedSkeletonAttack.MaximumActiveProjectiles));
            Assert.That(Shots().Count, Is.EqualTo(HoodedSkeletonAttack.MaximumActiveProjectiles));
            Assert.That(_attack.ShotCount, Is.EqualTo(HoodedSkeletonAttack.MaximumActiveProjectiles));
        }

        private void Begin()
        {
            for (int i = 0; i < 100 && _attack.Phase != EnemyMotion.Windup; i++) _attack.Tick(.05f);
            Assert.That(_attack.Phase, Is.EqualTo(EnemyMotion.Windup));
        }

        private void FireFirst()
        {
            for (int i = 0; i < 30 && _attack.Phase == EnemyMotion.Windup; i++) _attack.Tick(.05f);
            Assert.That(_attack.Phase == EnemyMotion.Attack || _attack.Phase == EnemyMotion.Attack2, Is.True);
        }

        private void FinishVolley()
        {
            FireFirst();
            for (int i = 0; i < 30 && _attack.HasPendingShot; i++) _attack.Tick(.05f);
            Assert.That(_attack.HasPendingShot, Is.False);
        }

        private List<PaintedEnemyProjectile> Shots() =>
            (List<PaintedEnemyProjectile>)typeof(HoodedSkeletonAttack).GetField("_shots", Hidden).GetValue(_attack);

        private static Vector3 Direction(PaintedEnemyProjectile shot) =>
            (Vector3)typeof(PaintedEnemyProjectile).GetField("_direction", Hidden).GetValue(shot);

        private float Angle(PaintedEnemyProjectile shot) => Vector3.SignedAngle(_attack.LockedDirection, Direction(shot), Vector3.up);

        private void AssertAngles(params float[] magnitudes)
        {
            float[] actual = Shots().Select(shot => Mathf.Abs(Angle(shot))).OrderBy(angle => angle).ToArray();
            float[] expected = magnitudes.SelectMany(angle => new[] { angle, angle }).OrderBy(angle => angle).ToArray();
            Assert.That(actual, Is.EqualTo(expected).Within(.01f));
        }
    }
}
