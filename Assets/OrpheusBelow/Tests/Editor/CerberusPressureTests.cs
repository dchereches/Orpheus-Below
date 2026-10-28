using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class CerberusPressureTests : CombatGameplayTestContext
    {
        private GameObject _playerObject, _enemyObject, _wall;
        private OrpheusPlayerController _player, _previousPlayer;
        private PaintedEnemyActor _actor;
        private CerberusAttack _attack;
        private float _previousTimeScale;
        private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            _previousPlayer = OrpheusPlayerController.Instance;
            _playerObject = new GameObject("CerberusPressurePlayer");
            _player = _playerObject.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _player);
            _player.RestoreTravelHealth(_player.maxHealth);
            _enemyObject = new GameObject("CerberusPressureEnemy");
            _enemyObject.transform.position = new Vector3(420000f, 0f, 0f);
            _playerObject.transform.position = _enemyObject.transform.position + Vector3.right * 3f;
            _actor = _enemyObject.AddComponent<PaintedEnemyActor>();
            _actor.Configure(PaintedEnemyKind.Cerberus);
            _attack = (CerberusAttack)_actor.Pattern;
        }

        [TearDown]
        public void TearDown()
        {
            if (_wall != null) Object.DestroyImmediate(_wall);
            Object.DestroyImmediate(_enemyObject);
            Object.DestroyImmediate(_playerObject);
            foreach (var effect in Object.FindObjectsByType<EnemyPaintedEffects>(FindObjectsSortMode.None))
                if (effect.transform.position.x > 419900f) Object.DestroyImmediate(effect.gameObject);
            foreach (var ring in Object.FindObjectsByType<GroundShockwaveRing>(FindObjectsSortMode.None))
                if (ring.transform.position.x > 419900f) Object.DestroyImmediate(ring.gameObject);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            Time.timeScale = _previousTimeScale;
        }

        [Test]
        public void ThreeAttacksCycleWithinSevenSecondsWithoutSkippingTheirWarnings()
        {
            float elapsed = 0f;
            while (_attack.PounceCount == 0 && elapsed < 7f)
            {
                // Keep the fight in range while letting the real state machine select attacks.
                _playerObject.transform.position = _enemyObject.transform.position + Vector3.right * 3f;
                _actor.Tick(.02f);
                elapsed += .02f;
            }
            Assert.That(_attack.BiteCount, Is.EqualTo(1));
            Assert.That(_attack.TailWhipCount, Is.EqualTo(1));
            Assert.That(_attack.PounceCount, Is.EqualTo(1));
            Assert.That(elapsed, Is.LessThan(7f));
        }

        [Test]
        public void PounceWarnsItsLaneAndCannotTrackASidestep()
        {
            BeginPounceWarning();
            float health = _player.CurrentHealth;
            Vector3 aim = _attack.CommittedTarget;
            Assert.That(LocalWarnings(), Is.GreaterThanOrEqualTo(4));
            _playerObject.transform.position += Vector3.forward * 4f;
            _actor.Tick(CerberusAttack.PounceWindupDuration + .01f);
            Assert.That(_player.CurrentHealth, Is.EqualTo(health));
            Assert.That(_attack.CommittedTarget, Is.EqualTo(aim));
            _actor.Tick(CerberusAttack.PounceDuration);
            Assert.That(_player.CurrentHealth, Is.EqualTo(health));
            Assert.That(_attack.PounceCount, Is.EqualTo(1));
        }

        [Test]
        public void PounceSweepsItsTravelAndHitsOnlyOnceAcrossALongFrame()
        {
            BeginPounceWarning();
            float health = _player.CurrentHealth;
            _actor.Tick(CerberusAttack.PounceWindupDuration + .01f);
            _actor.Tick(CerberusAttack.PounceDuration);
            Assert.That(_player.CurrentHealth, Is.LessThan(health), "A long frame must still test the travelled lane.");
            float afterHit = _player.CurrentHealth;
            typeof(OrpheusPlayerController).GetField("_invulnTimer", Hidden).SetValue(_player, 0f);
            _actor.Tick(.1f);
            Assert.That(_player.CurrentHealth, Is.EqualTo(afterHit));
        }

        [Test]
        public void PounceCannotCrossCoverOrDamageBeyondIt()
        {
            BeginPounceWarning();
            float health = _player.CurrentHealth;
            _wall = new GameObject("CerberusPounceCover");
            _wall.transform.position = _enemyObject.transform.position + Vector3.right * 1.5f + Vector3.up;
            _wall.AddComponent<BoxCollider>().size = new Vector3(.2f, 3f, 5f);
            _wall.AddComponent<IsoObstacle>();
            Physics.SyncTransforms();
            _actor.Tick(CerberusAttack.PounceWindupDuration + .01f);
            _actor.Tick(CerberusAttack.PounceDuration);
            Assert.That(_enemyObject.transform.position.x, Is.LessThan(_wall.transform.position.x));
            Assert.That(_player.CurrentHealth, Is.EqualTo(health));
        }

        [Test]
        public void DashingThroughPounceSpendsTheContactWithoutALaterHit()
        {
            BeginPounceWarning();
            float health = _player.CurrentHealth;
            typeof(OrpheusPlayerController).GetField("_isDashing", Hidden).SetValue(_player, true);
            _actor.Tick(CerberusAttack.PounceWindupDuration + .01f);
            _actor.Tick(CerberusAttack.PounceDuration * .6f);
            typeof(OrpheusPlayerController).GetField("_isDashing", Hidden).SetValue(_player, false);
            _actor.Tick(CerberusAttack.PounceDuration * .4f);
            Assert.That(_player.CurrentHealth, Is.EqualTo(health));
        }

        [Test]
        public void PauseAndInvalidDeltaCannotAdvanceWarningAndCancelDisposesIt()
        {
            BeginPounceWarning();
            float health = _player.CurrentHealth;
            Vector3 origin = _enemyObject.transform.position;
            Time.timeScale = 0f;
            _attack.Tick(10f);
            Time.timeScale = 1f;
            _attack.Tick(float.NaN);
            _attack.Tick(float.PositiveInfinity);
            Assert.That(_attack.Phase, Is.EqualTo(CerberusAttack.AttackPhase.Windup));
            Assert.That(_attack.AttackTimer, Is.Zero);
            Assert.That(_enemyObject.transform.position, Is.EqualTo(origin));
            _actor.Cancel();
            Assert.That(_attack.HasPendingDamage, Is.False);
            Assert.That(LocalWarnings(), Is.Zero);
            _actor.Tick(.01f);
            Assert.That(_player.CurrentHealth, Is.EqualTo(health));
        }

        [Test]
        public void CancelDuringPounceRemovesItsOwnedPaintedEffects()
        {
            BeginPounceWarning();
            _actor.Tick(CerberusAttack.PounceWindupDuration + .01f);
            Assert.That(_attack.HasPendingDamage, Is.True);
            _actor.Cancel();
            Assert.That(_attack.HasPendingDamage, Is.False);
            Assert.That(LocalWarnings(), Is.Zero);
            foreach (var effect in Object.FindObjectsByType<EnemyPaintedEffects>(FindObjectsSortMode.None))
                Assert.That(effect.transform.position.x, Is.LessThan(419900f));
        }

        [Test]
        public void DisabledActorCannotRestartACancelledPounce()
        {
            BeginPounceWarning();
            _enemyObject.SetActive(false);
            _attack.Tick(10f);
            Assert.That(_attack.Phase, Is.EqualTo(CerberusAttack.AttackPhase.Waiting));
            Assert.That(_attack.HasPendingDamage, Is.False);
            Assert.That(LocalWarnings(), Is.Zero);
        }

        private void BeginPounceWarning()
        {
            for (int i = 0; i < 2; i++)
            {
                _actor.Tick(CerberusAttack.OpeningWaitDuration + .01f);
                _actor.Tick(i == 0 ? CerberusAttack.BiteWindupDuration : CerberusAttack.TailWindupDuration);
                _actor.Tick(i == 0 ? CerberusAttack.BiteDuration : CerberusAttack.TailDuration);
                _actor.Tick(i == 0 ? CerberusAttack.BiteRecoveryDuration : CerberusAttack.TailRecoveryDuration);
            }
            _playerObject.transform.position = _enemyObject.transform.position + Vector3.right * 3f;
            _player.RestoreTravelHealth(_player.maxHealth);
            typeof(OrpheusPlayerController).GetField("_invulnTimer", Hidden).SetValue(_player, 0f);
            _actor.Tick(CerberusAttack.WaitDuration + .01f);
            Assert.That(_attack.SelectedAttack, Is.EqualTo(CerberusAttack.AttackKind.Pounce));
            Assert.That(_attack.Phase, Is.EqualTo(CerberusAttack.AttackPhase.Windup));
        }

        private int LocalWarnings()
        {
            int count = 0;
            foreach (var shadow in Object.FindObjectsByType<PaintedAttackShadow>(FindObjectsSortMode.None))
                if (shadow.transform.position.x > 419900f) count++;
            return count;
        }
    }
}
