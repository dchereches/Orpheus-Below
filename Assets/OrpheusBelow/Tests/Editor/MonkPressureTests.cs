using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class MonkPressureTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _enemyObject, _playerObject;
        private PaintedEnemyActor _actor;
        private MaskedMonkAttack _attack;
        private OrpheusPlayerController _player, _previousPlayer;
        private PaintedChamberMap _previousMap;
        private float _previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            _previousPlayer = OrpheusPlayerController.Instance;
            _previousMap = PaintedChamberMap.Active;
            typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, null);
            _playerObject = new GameObject("MonkPressurePlayer");
            _player = _playerObject.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _player);
            _player.RestoreTravelHealth(_player.maxHealth);
            _enemyObject = new GameObject("MonkPressureEnemy");
            _enemyObject.transform.position = Vector3.right * 430000f;
            _playerObject.transform.position = _enemyObject.transform.position + Vector3.right * 5f;
            _actor = _enemyObject.AddComponent<PaintedEnemyActor>();
            _actor.Configure(PaintedEnemyKind.MaskedMonk);
            _attack = (MaskedMonkAttack)_actor.Pattern;
        }

        [TearDown]
        public void TearDown()
        {
            _attack.Cancel();
            Object.DestroyImmediate(_enemyObject);
            Object.DestroyImmediate(_playerObject);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, _previousMap);
            Time.timeScale = _previousTimeScale;
        }

        [Test]
        public void ThreeDifferentPatternsCompleteWithinSevenSecondsWithBoundedHazards()
        {
            var seen = new List<MaskedMonkAttack.StonePattern>();
            int previousCast = 0;
            float elapsed = 0f;
            while (_attack.CompletedImpacts < 13 && elapsed < 7f)
            {
                _actor.Tick(.02f);
                elapsed += .02f;
                if (_attack.CastCount == previousCast) continue;
                previousCast = _attack.CastCount;
                seen.Add(_attack.Pattern);
                Assert.That(_attack.ScheduledStoneCount, Is.LessThanOrEqualTo(MaskedMonkAttack.MaximumStones));
                // Keep the player alive while the real state machine completes all patterns.
                _playerObject.transform.position += Vector3.forward * 15f;
            }
            Assert.That(seen, Is.EqualTo(new[] { MaskedMonkAttack.StonePattern.StaggeredCluster,
                MaskedMonkAttack.StonePattern.SweepingLine, MaskedMonkAttack.StonePattern.BrokenRing }));
            Assert.That(_attack.CompletedImpacts, Is.EqualTo(13));
            Assert.That(elapsed, Is.LessThan(7f));
        }

        [Test]
        public void ClusterCommitsAllThreeTargetsAndResolvesEachScheduledImpactOnce()
        {
            Begin();
            Assert.That(_attack.Pattern, Is.EqualTo(MaskedMonkAttack.StonePattern.StaggeredCluster));
            var targets = Targets();
            Assert.That(targets.Length, Is.EqualTo(3));
            Assert.That(_attack.StoneImpactTime(0), Is.EqualTo(MaskedMonkAttack.WarningSeconds + MaskedMonkAttack.FallSeconds));
            Assert.That(_attack.StoneImpactTime(1) - _attack.StoneImpactTime(0), Is.EqualTo(MaskedMonkAttack.StaggerSeconds).Within(.001f));
            Assert.That(_attack.StoneImpactTime(2) - _attack.StoneImpactTime(1), Is.EqualTo(MaskedMonkAttack.StaggerSeconds).Within(.001f));
            _playerObject.transform.position += Vector3.forward * 15f;
            Advance(.9f);
            Assert.That(_attack.CompletedImpacts, Is.Zero);
            Assert.That(Targets(), Is.EqualTo(targets));
            Advance(.08f);
            Assert.That(_attack.CompletedImpacts, Is.EqualTo(1));
            Advance(.16f);
            Assert.That(_attack.CompletedImpacts, Is.EqualTo(2));
            Advance(.16f);
            Assert.That(_attack.CompletedImpacts, Is.EqualTo(3));
            Advance(.2f);
            Assert.That(_attack.CompletedImpacts, Is.EqualTo(3));
        }

        [Test]
        public void SweepingLineWarnsOverlappingCollinearDisksInImpactOrder()
        {
            Begin(); _attack.Cancel(); Begin();
            Assert.That(_attack.Pattern, Is.EqualTo(MaskedMonkAttack.StonePattern.SweepingLine));
            Assert.That(_attack.ScheduledStoneCount, Is.EqualTo(5));
            Vector3 direction = (_attack.StoneTarget(1) - _attack.StoneTarget(0)).normalized;
            for (int i = 1; i < 5; i++)
            {
                Vector3 gap = _attack.StoneTarget(i) - _attack.StoneTarget(i - 1);
                Assert.That(Vector3.Dot(gap.normalized, direction), Is.GreaterThan(.999f));
                Assert.That(gap.magnitude, Is.LessThan(MaskedMonkAttack.ImpactRadius * 2f));
                Assert.That(_attack.StoneImpactTime(i), Is.GreaterThan(_attack.StoneImpactTime(i - 1)));
            }
        }

        [Test]
        public void BrokenRingEnclosesCenterButLeavesAPlayerWideRadialEscape()
        {
            Begin(); _attack.Cancel(); Begin(); _attack.Cancel(); Begin();
            Assert.That(_attack.Pattern, Is.EqualTo(MaskedMonkAttack.StonePattern.BrokenRing));
            Assert.That(_attack.ScheduledStoneCount, Is.EqualTo(5));
            Vector3 center = _attack.LockedCenter;
            Vector3 escape = _attack.EscapeDirection;
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = _attack.StoneTarget(i) - center;
                Assert.That(offset.magnitude, Is.EqualTo(MaskedMonkAttack.RingRadius).Within(.05f));
                Assert.That(_attack.StoneImpactTime(i), Is.EqualTo(_attack.StoneImpactTime(0)));
                float projection = Mathf.Clamp(Vector3.Dot(offset, escape), 0f, MaskedMonkAttack.RingRadius * 2f);
                float escapeClearance = Vector3.Distance(_attack.StoneTarget(i), center + escape * projection);
                Assert.That(escapeClearance, Is.GreaterThan(MaskedMonkAttack.ImpactRadius + .6f));
                if (i > 0)
                    Assert.That(Vector3.Distance(_attack.StoneTarget(i), _attack.StoneTarget(i - 1)),
                        Is.LessThan(MaskedMonkAttack.ImpactRadius * 2f));
            }
            float health = _player.CurrentHealth;
            _playerObject.transform.position = center + escape * MaskedMonkAttack.RingRadius;
            Advance(1.1f);
            Assert.That(_player.CurrentHealth, Is.EqualTo(health));
        }

        [Test]
        public void LongFrameCannotSkipTheWarningAndPausePreservesItsLockedTargets()
        {
            Begin();
            var targets = Targets();
            _attack.Tick(100f);
            Assert.That(_attack.CompletedImpacts, Is.Zero);
            Assert.That(LocalWarnings(), Is.EqualTo(3));
            Time.timeScale = 0f;
            _attack.Tick(100f);
            _attack.Tick(float.NaN);
            _attack.Tick(float.PositiveInfinity);
            Assert.That(Targets(), Is.EqualTo(targets));
            Assert.That(_attack.PendingImpactCount, Is.EqualTo(3));
            Time.timeScale = 1f;
            Advance(.8f);
            Assert.That(_attack.CompletedImpacts, Is.Zero);
            Advance(.08f);
            Assert.That(_attack.CompletedImpacts, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TerminalOrDisabledActorCancelsFallingStonesAndOwnedVisuals(bool disabled)
        {
            Begin();
            Advance(.7f);
            Assert.That(LocalEffects(), Is.GreaterThan(0));
            if (disabled) _enemyObject.SetActive(false);
            else typeof(OrpheusPlayerController).GetProperty("IsVictory").SetValue(_player, true);
            _attack.Tick(.1f);
            AssertCancelled();
            _attack.Tick(100f);
            Assert.That(_attack.CompletedImpacts, Is.Zero);
        }

        [Test]
        public void MusicalStunCancelsEveryPendingImpactWithoutResettingCompletedCounter()
        {
            var shade = _enemyObject.AddComponent<ShadeEnemy>();
            typeof(ShadeEnemy).GetField("_hp", Hidden).SetValue(shade, shade.maxHealth);
            typeof(ShadeEnemy).GetField("_paintedActor", Hidden).SetValue(shade, _actor);
            _actor.Configure(PaintedEnemyKind.MaskedMonk, shade);
            Begin();
            Advance(.98f);
            Assert.That(_attack.CompletedImpacts, Is.EqualTo(1));
            Assert.That(_attack.PendingImpactCount, Is.EqualTo(2));
            shade.ApplyStun(1f);
            AssertCancelled();
            Assert.That(_attack.CompletedImpacts, Is.EqualTo(1));
            _attack.Tick(.02f);
            Assert.That(_attack.CompletedImpacts, Is.EqualTo(1));
        }

        private void AssertCancelled()
        {
            Assert.That(_attack.IsSummoning, Is.False);
            Assert.That(_attack.PendingImpactCount, Is.Zero);
            Assert.That(LocalWarnings(), Is.Zero);
            Assert.That(LocalEffects(), Is.Zero);
        }

        private void Begin()
        {
            for (int i = 0; i < 40 && !_attack.IsSummoning; i++) _actor.Tick(.02f);
            Assert.That(_attack.IsSummoning, Is.True);
        }

        private void Advance(float seconds)
        {
            for (float remaining = seconds; remaining > .0001f; remaining -= .02f)
                _actor.Tick(Mathf.Min(.02f, remaining));
        }

        private Vector3[] Targets()
        {
            var targets = new Vector3[_attack.ScheduledStoneCount];
            for (int i = 0; i < targets.Length; i++) targets[i] = _attack.StoneTarget(i);
            return targets;
        }

        private int LocalWarnings()
        {
            int count = 0;
            foreach (var shadow in Object.FindObjectsByType<PaintedAttackShadow>(FindObjectsSortMode.None))
                if (shadow.transform.position.x > 429900f) count++;
            return count;
        }

        private int LocalEffects()
        {
            int count = 0;
            foreach (var effect in Object.FindObjectsByType<EnemyPaintedEffects>(FindObjectsSortMode.None))
                if (effect.transform.position.x > 429900f) count++;
            return count;
        }
    }
}
