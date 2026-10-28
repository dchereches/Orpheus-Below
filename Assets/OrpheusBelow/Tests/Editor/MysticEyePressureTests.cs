using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class MysticEyePressureTests : CombatGameplayTestContext
    {
        private GameObject _enemy, _playerObject;
        private MysticEyeWardenAttack _attack;
        private PaintedEnemyActor _actor;
        private OrpheusPlayerController _player, _previousPlayer;
        private float _previousScale;

        [SetUp]
        public void SetUp()
        {
            _previousScale = Time.timeScale;
            Time.timeScale = 1f;
            _previousPlayer = OrpheusPlayerController.Instance;
            _playerObject = new GameObject("EyePressurePlayer");
            _player = _playerObject.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _player);
            _player.RestoreTravelHealth(_player.maxHealth);
            _enemy = new GameObject("EyePressureOwner");
            _enemy.transform.position = Vector3.right * 85000f;
            _playerObject.transform.position = _enemy.transform.position + Vector3.right * 5f;
            _actor = _enemy.AddComponent<PaintedEnemyActor>();
            _actor.Configure(PaintedEnemyKind.MysticEyeWarden);
            _attack = (MysticEyeWardenAttack)_actor.Pattern;
        }

        [TearDown]
        public void TearDown()
        {
            _attack?.Cancel(); // EditMode destruction does not run the normal player-loop lifecycle.
            if (_enemy != null) Object.DestroyImmediate(_enemy);
            if (_playerObject != null) Object.DestroyImmediate(_playerObject);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            Time.timeScale = _previousScale;
        }

        private void Until(System.Func<bool> condition)
        {
            for (int i = 0; i < 300 && !condition(); i++) _attack.Tick(.05f);
            Assert.That(condition(), Is.True, "Eye attack failed to reach its next bounded phase.");
        }

        [Test]
        public void EyeCyclesThroughStaffFanAndGroundChainAtTheNewCadence()
        {
            Until(() => _attack.CastCount == 1);
            Assert.That(_attack.CurrentVariant, Is.EqualTo(MysticEyeWardenAttack.CastVariant.StaffThrow));
            Assert.That(_attack.HasActiveStaff, Is.True);
            Assert.That(_attack.ThrowCount, Is.EqualTo(1));
            Assert.That(_actor.Art.Motion, Is.EqualTo(EnemyMotion.Attack));
            Until(() => _attack.Phase == MysticEyeWardenAttack.CastPhase.Recall);
            Assert.That(_attack.HasActiveStaff, Is.False, "Recall must never retain a damaging staff.");
            Until(() => _attack.CastCount == 2);
            Assert.That(_attack.CurrentVariant, Is.EqualTo(MysticEyeWardenAttack.CastVariant.EyeFan));
            Assert.That(_attack.ActiveProjectileCount, Is.EqualTo(3));
            Assert.That(_attack.HasActiveStaff, Is.False);
            Assert.That(_actor.Art.Motion, Is.EqualTo(EnemyMotion.Windup), "Non-throw magic keeps the staff in the hand.");
            Until(() => _attack.CastCount == 3);
            Assert.That(_attack.CurrentVariant, Is.EqualTo(MysticEyeWardenAttack.CastVariant.RuneChain));
            Until(() => _attack.RuneStrikeCount == 3);
            Assert.That(_attack.ActiveWarningCount, Is.Zero);
            Assert.That(_attack.ThrowCount, Is.EqualTo(1));
            Assert.That(MysticEyeWardenAttack.WaitSeconds + MysticEyeWardenAttack.RecoverySeconds, Is.LessThan(1.1f));
            Assert.That(MysticEyeWardenAttack.WindupSeconds, Is.GreaterThanOrEqualTo(.6f));
        }

        [Test]
        public void FanKeepsCommittedAimAndUsesThreeDifferentSweptDirections()
        {
            Until(() => _attack.CastCount == 1);
            Until(() => _attack.CurrentVariant == MysticEyeWardenAttack.CastVariant.EyeFan && _attack.HasPendingThrow);
            Vector3 committed = _attack.LockedDirection;
            _playerObject.transform.position += Vector3.forward * 8f;
            Until(() => _attack.CastCount == 2);
            Assert.That(_attack.LockedDirection, Is.EqualTo(committed));
            var shots = Object.FindObjectsByType<PaintedEnemyProjectile>(FindObjectsSortMode.None)
                .Where(p => p.Owner == _actor).ToArray();
            Assert.That(shots.Length, Is.EqualTo(3));
            var directions = shots.Select(p =>
            {
                Vector3 before = p.transform.position;
                p.Advance(.05f);
                return (p.transform.position - before).normalized;
            }).ToArray();
            Assert.That(directions.Count(d => Vector3.Dot(d, committed) > .999f), Is.EqualTo(1));
            Assert.That(Vector3.Dot(directions[0], directions[1]), Is.LessThan(.99f));
            Assert.That(Vector3.Dot(directions[1], directions[2]), Is.LessThan(.99f));
        }

        [Test]
        public void CancelDisarmsEveryRuneAndPauseDoesNotAdvanceWarning()
        {
            Until(() => _attack.CurrentVariant == MysticEyeWardenAttack.CastVariant.RuneChain && _attack.HasPendingThrow);
            Assert.That(_attack.ActiveWarningCount, Is.EqualTo(3));
            float elapsed = _attack.PhaseElapsed;
            Time.timeScale = 0f;
            _attack.Tick(10f);
            Assert.That(_attack.PhaseElapsed, Is.EqualTo(elapsed));
            Time.timeScale = 1f;
            _attack.Cancel();
            Assert.That(_attack.ActiveWarningCount, Is.Zero);
            Assert.That(_attack.ActiveProjectileCount, Is.Zero);
            Assert.That(_attack.HasPendingThrow, Is.False);
            Assert.That(_attack.RuneStrikeCount, Is.Zero);
            _attack.Cancel();
        }

        [Test]
        public void GroundChainActuallyHitsItsWarnedPointOnceAndFanCancellationRemovesEveryBolt()
        {
            Until(() => _attack.CastCount == 2);
            Assert.That(_attack.ActiveProjectileCount, Is.EqualTo(3));
            _attack.Cancel();
            Assert.That(_attack.ActiveProjectileCount, Is.Zero);
            Assert.That(Object.FindObjectsByType<PaintedEnemyProjectile>(FindObjectsSortMode.None)
                .Any(p => p.Owner == _actor), Is.False);
            Until(() => _attack.CurrentVariant == MysticEyeWardenAttack.CastVariant.RuneChain && _attack.HasPendingThrow);
            _playerObject.transform.position = _attack.RunePoint(0);
            float hp = _player.CurrentHealth;
            Until(() => _attack.RuneStrikeCount == 1);
            Assert.That(_player.CurrentHealth, Is.LessThan(hp), "A warned eruption must deal real damage at its committed point.");
            float afterHit = _player.CurrentHealth;
            Until(() => _attack.Phase == MysticEyeWardenAttack.CastPhase.Recover);
            Assert.That(_player.CurrentHealth, Is.EqualTo(afterHit), "An erupted sigil cannot linger as a repeated damage field.");
            Assert.That(_attack.ActiveWarningCount, Is.Zero);
        }

        [Test]
        public void RuneDamageHonorsDashAndCoverAndGameOverClearsTheKit()
        {
            Until(() => _attack.CurrentVariant == MysticEyeWardenAttack.CastVariant.RuneChain && _attack.HasPendingThrow);
            _playerObject.transform.position = _attack.RunePoint(0);
            float hp = _player.CurrentHealth;
            typeof(OrpheusPlayerController).GetField("_isDashing", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_player, true);
            Until(() => _attack.RuneStrikeCount == 1);
            Assert.That(_player.CurrentHealth, Is.EqualTo(hp));
            typeof(OrpheusPlayerController).GetField("_isDashing", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_player, false);
            _playerObject.transform.position = _attack.RunePoint(1) + Vector3.right * .7f;
            var wall = new GameObject("RuneCover");
            try
            {
                wall.transform.position = _attack.RunePoint(1) + Vector3.right * .35f + Vector3.up * .65f;
                wall.AddComponent<BoxCollider>().size = new Vector3(.1f, 2f, 2f);
                wall.AddComponent<IsoObstacle>();
                Physics.SyncTransforms();
                Until(() => _attack.RuneStrikeCount == 2);
                Assert.That(_player.CurrentHealth, Is.EqualTo(hp));
                typeof(OrpheusPlayerController).GetProperty("IsGameOver").SetValue(_player, true);
                _attack.Tick(.05f);
                Assert.That(_attack.ActiveWarningCount, Is.Zero);
                Assert.That(_attack.ActiveProjectileCount, Is.Zero);
                Assert.That(_attack.HasPendingThrow, Is.False);
                Assert.That(_attack.RuneStrikeCount, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(wall); }
        }
    }
}
