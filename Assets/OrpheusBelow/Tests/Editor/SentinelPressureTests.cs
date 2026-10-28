using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class SentinelPressureTests : CombatGameplayTestContext
    {
        private GameObject _guardian, _playerObject, _cover;
        private WingedSentinelAttack _attack;
        private OrpheusPlayerController _player, _previousPlayer;
        private bool _tutorial;
        private float _timeScale;
        private static readonly Vector3 Origin = new Vector3(43000, 0, 0);

        [SetUp]
        public void SetUp()
        {
            _previousPlayer = OrpheusPlayerController.Instance;
            _tutorial = OrpheusHud.IsTutorialModalActive;
            _timeScale = Time.timeScale;
            Time.timeScale = 1;
            typeof(OrpheusHud).GetProperty("IsTutorialModalActive").SetValue(null, false);
            _playerObject = new GameObject("SentinelPressurePlayer");
            _player = _playerObject.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _player);
            _guardian = new GameObject("SentinelPressureGuardian");
            _guardian.transform.position = Origin;
            var actor = _guardian.AddComponent<PaintedEnemyActor>();
            _attack = _guardian.AddComponent<WingedSentinelAttack>();
            _attack.Initialize(actor);
            ResetPlayer(new Vector3(5, 0, 0));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cover);
            Object.DestroyImmediate(_guardian);
            Object.DestroyImmediate(_playerObject);
            foreach (var ring in Object.FindObjectsByType<GroundShockwaveRing>(FindObjectsSortMode.None))
                if ((ring.transform.position - Origin).sqrMagnitude < 400) Object.DestroyImmediate(ring.gameObject);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            typeof(OrpheusHud).GetProperty("IsTutorialModalActive").SetValue(null, _tutorial);
            Time.timeScale = _timeScale;
        }

        [Test]
        public void SweepHitsSideButCommittedThrustLeavesTheSameSideEscapeOpen()
        {
            ResetPlayer(new Vector3(0, 0, 3));
            Begin(0); FinishStrike();
            Assert.That(_player.CurrentHealth, Is.LessThan(100), "The broad sword sweep must remain dangerous beside the sentinel.");
            ResetPlayer(new Vector3(0, 0, 3));
            Begin(1); FinishStrike();
            Assert.That(_player.CurrentHealth, Is.EqualTo(100), "A thrust must not secretly use the old circular cleave hitbox.");
            ResetPlayer(new Vector3(4.5f, 0, 0));
            Begin(1); FinishStrike();
            Assert.That(_player.CurrentHealth, Is.LessThan(100), "The committed thrust must reach along its warned lane.");
        }

        [Test]
        public void BladeFanReachesOutwardAfterWindupAndLeavesGapsBetweenBlades()
        {
            ResetPlayer(new Vector3(6, 0, 0));
            Begin(2);
            AdvanceToStrike();
            _attack.Tick(.1f);
            Assert.That(_player.CurrentHealth, Is.EqualTo(100), "The distant blade must travel outward instead of damaging the whole corridor immediately.");
            FinishStrike();
            Assert.That(_player.CurrentHealth, Is.LessThan(100));
            ResetPlayer(new Vector3(5.6f, 0, 1.35f));
            Begin(2); FinishStrike();
            Assert.That(_player.CurrentHealth, Is.EqualTo(100), "The three outward blades must retain a safe gap to step into.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EveryDamagingSampleWasInsideAnActualLockedWarning(int pattern)
        {
            var samples = new[] { new Vector3(0,0,3), new Vector3(3,0,0), new Vector3(5.4f,0,0),
                new Vector3(6,0,0), new Vector3(5,0,2.9f), new Vector3(5,0,-2.9f), new Vector3(7,0,0) };
            int hits = 0;
            foreach (var sample in samples)
            {
                ResetPlayer(sample); Begin(pattern);
                var shadows = new List<Vector3>();
                var radii = new List<float>();
                foreach (var shadow in Object.FindObjectsByType<PaintedAttackShadow>(FindObjectsSortMode.None))
                {
                    if ((shadow.transform.position - Origin).sqrMagnitude > 200) continue;
                    shadows.Add(shadow.transform.position); radii.Add(shadow.Radius);
                }
                // Move after locking: neither warning nor damage may follow the guardian.
                _guardian.transform.position += Vector3.back * 9;
                FinishStrike();
                if (_player.CurrentHealth < 100)
                {
                    hits++;
                    bool warned = false;
                    for (int i = 0; i < shadows.Count; i++)
                    {
                        Vector3 gap = Origin + sample - shadows[i]; gap.y = 0;
                        if (gap.magnitude <= radii[i] + .01f) warned = true;
                    }
                    Assert.That(warned, Is.True, "Damage escaped the drawn shadow at " + sample);
                }
                _guardian.transform.position = Origin;
            }
            Assert.That(hits, Is.GreaterThan(0), "The geometry test must exercise real damage, not only safe samples.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void WarningIsVisibleBeforeDamageAndPauseKeepsItLocked(int pattern)
        {
            ResetPlayer(new Vector3(3,0,0)); Begin(pattern);
            for (int i = 0; i < 10; i++) _attack.Tick(.05f);
            Assert.That(_attack.Phase, Is.EqualTo(WingedSentinelAttack.SwordPhase.Windup));
            Assert.That(_player.CurrentHealth, Is.EqualTo(100));
            float elapsed = _attack.PhaseElapsed;
            Vector3 center = _attack.LockedCenter;
            Time.timeScale = 0;
            _attack.Tick(4);
            Assert.That(_attack.PhaseElapsed, Is.EqualTo(elapsed));
            Assert.That(_attack.LockedCenter, Is.EqualTo(center));
            Assert.That(_attack.HasPendingHit, Is.True);
            Time.timeScale = 1;
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void DashAndSolidCoverPreventEachSwordPattern(int pattern)
        {
            ResetPlayer(new Vector3(4.5f,0,0)); Begin(pattern);
            SetField(_player, "_isDashing", true); FinishStrike();
            Assert.That(_player.CurrentHealth, Is.EqualTo(100));
            SetField(_player, "_isDashing", false);
            _cover = new GameObject("SentinelSolidCover");
            _cover.transform.position = Origin + new Vector3(2.5f,.7f,0);
            _cover.AddComponent<BoxCollider>().size = new Vector3(.4f, 2, 4);
            _cover.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;
            Physics.SyncTransforms();
            ResetPlayer(new Vector3(4.5f,0,0)); Begin(pattern); FinishStrike();
            Assert.That(_player.CurrentHealth, Is.EqualTo(100), "Sword damage must not originate on the far side of solid cover.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void CancelDisarmsAllWarnedHitsAndRemovesOwnedWarnings(int pattern)
        {
            ResetPlayer(new Vector3(4.5f,0,0)); Begin(pattern); AdvanceToStrike();
            int swings = _attack.CompletedSwings;
            _attack.Cancel();
            Assert.That(_attack.HasPendingHit, Is.False);
            foreach (var shadow in Object.FindObjectsByType<PaintedAttackShadow>(FindObjectsSortMode.None))
                Assert.That((shadow.transform.position - Origin).sqrMagnitude, Is.GreaterThan(200));
            // A cancelled attack cannot deliver a delayed blade during the next cooldown.
            for (int i = 0; i < 4; i++) _attack.Tick(.05f);
            Assert.That(_player.CurrentHealth, Is.EqualTo(100));
            Assert.That(_attack.CompletedSwings, Is.EqualTo(swings));
        }

        [Test]
        public void NaturalRecoveryReturnsQuicklyAndCyclesThroughThreeDifferentWarningShapes()
        {
            var expectedWarningCounts = new[] { 1, 6, 18, 1 };
            Begin(0);
            foreach (int count in expectedWarningCounts)
            {
                int warnings = 0;
                foreach (var shadow in Object.FindObjectsByType<PaintedAttackShadow>(FindObjectsSortMode.None))
                    if ((shadow.transform.position - Origin).sqrMagnitude < 200) warnings++;
                Assert.That(warnings, Is.EqualTo(count));
                FinishStrike();
                Assert.That(_attack.Phase, Is.EqualTo(WingedSentinelAttack.SwordPhase.Recover));
                for (int i = 0; i < 20 && _attack.Phase != WingedSentinelAttack.SwordPhase.Windup; i++) _attack.Tick(.05f);
                Assert.That(_attack.Phase, Is.EqualTo(WingedSentinelAttack.SwordPhase.Windup),
                    "Recovery plus cooldown must return pressure in one second, retaining the next full warning.");
            }
            Assert.That(_attack.CompletedSwings, Is.EqualTo(4), "Fan cells must count as one released sword attack.");
        }

        [Test]
        public void RealShadeStunAndDeadOwnerBothDisarmThePendingBladeFan()
        {
            var actor = _guardian.GetComponent<PaintedEnemyActor>();
            actor.Configure(PaintedEnemyKind.WingedSentinel, createPattern: false);
            typeof(PaintedEnemyActor).GetProperty("Pattern").SetValue(actor, _attack);
            var shade = _guardian.AddComponent<ShadeEnemy>();
            SetField(shade, "_hp", 100f);
            SetField(shade, "_paintedActor", actor);
            typeof(PaintedEnemyActor).GetProperty("OwnerShade").SetValue(actor, shade);
            Begin(2); AdvanceToStrike();
            shade.ApplyStun(1);
            Assert.That(_attack.HasPendingHit, Is.False);
            Assert.That(_attack.LockedRadius, Is.Zero);
            Begin(2);
            SetField(shade, "_hp", 0f);
            _attack.Tick(.1f);
            Assert.That(_attack.HasPendingHit, Is.False);
            Assert.That(_attack.LockedRadius, Is.Zero);
            Assert.That(_player.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase("IsVictory")]
        [TestCase("IsGameOver")]
        public void TerminalPlayerStateRemovesPendingWarningAndDisarmsDamage(string terminalProperty)
        {
            Begin(2); AdvanceToStrike();
            typeof(OrpheusPlayerController).GetProperty(terminalProperty).SetValue(_player, true);
            _attack.Tick(.1f);
            Assert.That(_attack.HasPendingHit, Is.False);
            Assert.That(_attack.LockedRadius, Is.Zero);
            Assert.That(_attack.CompletedSwings, Is.Zero);
        }

        private void Begin(int pattern)
        {
            _attack.Cancel();
            SetField(_attack, "_attackCount", pattern);
            typeof(WingedSentinelAttack).GetMethod("BeginWindup", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_attack, new object[] { Origin + Vector3.right * 5 });
        }

        private void ResetPlayer(Vector3 offset)
        {
            _player.transform.position = Origin + offset;
            typeof(OrpheusPlayerController).GetProperty("CurrentHealth").SetValue(_player, 100f);
            SetField(_player, "_invulnTimer", 0f);
        }

        private void AdvanceToStrike()
        {
            for (int i = 0; i < 60 && _attack.Phase == WingedSentinelAttack.SwordPhase.Windup; i++) _attack.Tick(.05f);
            Assert.That(_attack.Phase, Is.EqualTo(WingedSentinelAttack.SwordPhase.Strike));
        }

        private void FinishStrike()
        {
            if (_attack.Phase == WingedSentinelAttack.SwordPhase.Windup) AdvanceToStrike();
            for (int i = 0; i < 40 && _attack.Phase == WingedSentinelAttack.SwordPhase.Strike; i++) _attack.Tick(.05f);
        }

        private static void SetField(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
