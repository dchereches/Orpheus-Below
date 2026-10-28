using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class WingedSentinelPaintedTests
    {
        private GameObject _guardian;
        private WingedSentinelAttack _attack;

        [SetUp]
        public void SetUp()
        {
            _guardian = new GameObject("SentinelAttackTest");
            _guardian.transform.position = new Vector3(2f, 0f, 3f);
            var actor = _guardian.AddComponent<PaintedEnemyActor>();
            _attack = _guardian.AddComponent<WingedSentinelAttack>();
            _attack.Initialize(actor);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_guardian); }

        [Test]
        public void WideSweepWarningMatchesLockedSixMetreDamageCircle()
        {
            BeginWindup(new Vector3(7f, 0f, 3f));
            var warning = Warning;
            Assert.That(_attack.Phase, Is.EqualTo(WingedSentinelAttack.SwordPhase.Windup));
            Assert.That(_attack.LockedRadius, Is.EqualTo(6f));
            Assert.That(warning.Radius, Is.EqualTo(_attack.LockedRadius));
            Assert.That(warning.transform.position.x, Is.EqualTo(_attack.LockedCenter.x));
            Assert.That(warning.transform.position.z, Is.EqualTo(_attack.LockedCenter.z));
            Vector3 locked = _attack.LockedCenter;
            _guardian.transform.position += Vector3.forward * 4f;
            Assert.That(_attack.LockedCenter, Is.EqualTo(locked), "Warning must not track the guardian after locking.");
        }

        [Test]
        public void CancelDestroysWarningAndDisarmsPendingSwordHit()
        {
            BeginWindup(Vector3.right * 5f);
            var warning = Warning;
            Assert.That(_attack.HasPendingHit, Is.True);
            _attack.Cancel();
            Assert.That(warning == null, Is.True);
            Assert.That(_attack.HasPendingHit, Is.False);
            Assert.That(_attack.Phase, Is.EqualTo(WingedSentinelAttack.SwordPhase.Cooldown));
            Assert.That(_attack.PhaseElapsed, Is.Zero);
            Assert.That(_attack.LockedRadius, Is.Zero);
            Assert.DoesNotThrow(() => _attack.Cancel());
        }

        [Test]
        public void CommittedThrustUsesLinkedWarningCellsInsideItsLockedBound()
        {
            BeginWindup(new Vector3(8f, 0f, 3f));
            _attack.Cancel();
            BeginWindup(new Vector3(8f, 0f, 3f));
            Assert.That(_attack.LockedRadius, Is.EqualTo(WingedSentinelAttack.CleaveRadius));
            Assert.That(Vector3.Distance(_guardian.transform.position, _attack.LockedCenter), Is.EqualTo(2.7f).Within(0.001f));
            Assert.That(_attack.CurrentPattern, Is.EqualTo(WingedSentinelAttack.SwordPattern.Thrust));
            Assert.That(Warning.Radius, Is.InRange(.5f, .9f));
        }

        [Test]
        public void SwordLeavesLongVisibleWarningAndRecoveryWindows()
        {
            Assert.That(WingedSentinelAttack.WarningSeconds, Is.InRange(.85f, 1.2f));
            Assert.That(WingedSentinelAttack.RecoverySeconds, Is.InRange(.3f, .6f));
            Assert.That(WingedSentinelAttack.CooldownSeconds, Is.InRange(.3f, .7f));
            Assert.That(WingedSentinelAttack.HitAtSeconds, Is.GreaterThan(0f).And.LessThan(WingedSentinelAttack.AttackSeconds));
        }

        [Test]
        public void SentinelApproachesWithinSwordReachBeforeCommittingAWarning()
        {
            var playerGo = new GameObject("SentinelReachPlayer");
            var live = new GameObject("SentinelReachActor");
            var oldMap = PaintedChamberMap.Active;
            bool oldTutorial = OrpheusHud.IsTutorialModalActive;
            float oldTime = Time.timeScale;
            try
            {
                Time.timeScale = 1;
                typeof(OrpheusHud).GetProperty("IsTutorialModalActive").SetValue(null,false);
                typeof(PaintedChamberMap).GetProperty("Active").SetValue(null,null);
                var player = playerGo.AddComponent<OrpheusPlayerController>();
                typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null,player);
                live.transform.position = Vector3.right * 47000;
                playerGo.transform.position = live.transform.position + UnderworldCamera2D.IsoRightXZ * 7;
                var actor = live.AddComponent<PaintedEnemyActor>(); actor.Configure(PaintedEnemyKind.WingedSentinel);
                var attack = (WingedSentinelAttack)actor.Pattern;
                for (int i = 0; i < 80 && attack.Phase != WingedSentinelAttack.SwordPhase.Windup; i++) actor.Tick(.1f);
                Assert.That(attack.Phase, Is.EqualTo(WingedSentinelAttack.SwordPhase.Windup),
                    "gap="+Vector3.Distance(live.transform.position,playerGo.transform.position)+" elapsed="+attack.PhaseElapsed+" blocked="+UnderworldFrontEnd.BlocksGameplay+" tutorial="+OrpheusHud.IsTutorialModalActive+" pos="+live.transform.position);
                Assert.That(Vector3.Distance(live.transform.position,playerGo.transform.position), Is.LessThanOrEqualTo(WingedSentinelAttack.SweepRadius),
                    "The sentinel must approach rather than repeatedly swing outside its own range.");
            }
            finally
            {
                Object.DestroyImmediate(live); Object.DestroyImmediate(playerGo);
                typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null,null);
                typeof(PaintedChamberMap).GetProperty("Active").SetValue(null,oldMap); Time.timeScale = oldTime;
                typeof(OrpheusHud).GetProperty("IsTutorialModalActive").SetValue(null,oldTutorial);
            }
        }

        private void BeginWindup(Vector3 target)
        {
            typeof(WingedSentinelAttack).GetMethod("BeginWindup", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_attack, new object[] { target });
        }

        private PaintedAttackShadow Warning => (PaintedAttackShadow)typeof(WingedSentinelAttack)
            .GetField("_warning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_attack);
    }
}
