using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class TripodPressureTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _owner, _playerObject;
        private TripodAttack _attack;
        private OrpheusPlayerController _previousPlayer;
        private float _timeScale;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale;
            Time.timeScale = 1;
            _playerObject = new GameObject("TripodPressurePlayer");
            _previousPlayer = OrpheusPlayerController.Instance;
            var player = _playerObject.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, player);
            _playerObject.transform.position = Vector3.right * 6;
            _owner = new GameObject("TripodPressureOwner");
            var actor = _owner.AddComponent<PaintedEnemyActor>();
            _attack = _owner.AddComponent<TripodAttack>();
            _attack.Initialize(actor);
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
        public void CallsRotateThroughSingleEchoAndReturnWithoutTrackingDuringWarning()
        {
            var expected = new[] { TripodAttack.SonicCall.SinglePulse,
                TripodAttack.SonicCall.DoubleEcho, TripodAttack.SonicCall.ReturnChorus };
            foreach (var call in expected)
            {
                Begin();
                Assert.That(_attack.Variant, Is.EqualTo(call));
                Assert.That(_attack.PlannedPulseCount, Is.EqualTo(call == TripodAttack.SonicCall.SinglePulse ? 1 : 2));
                Vector3 aim = _attack.LockedDirection;
                _playerObject.transform.position = Vector3.forward * 9;
                _attack.Tick(.05f);
                Assert.That(_attack.LockedDirection, Is.EqualTo(aim));
                _attack.Cancel();
            }
            Assert.That(_attack.CastCount, Is.EqualTo(3));
        }

        [Test]
        public void ReturnChorusWarnsItsCommittedRemoteOriginThenEmitsBackTowardBoss()
        {
            SelectReturn();
            Vector3 returnOrigin = _attack.ReturnOrigin;
            Assert.That(returnOrigin.x, Is.GreaterThan(_playerObject.transform.position.x + 2));
            Assert.That(Vector3.Distance(returnOrigin, _owner.transform.position), Is.LessThanOrEqualTo(TripodAttack.MaximumReturnDistance));
            Emit(0);
            Invoke("PreparePulseWarning", 1);
            var warnings = (List<PaintedAttackShadow>)typeof(TripodAttack).GetField("_warnings", Hidden).GetValue(_attack);
            Assert.That(warnings.Count, Is.GreaterThan(1));
            Assert.That(Vector3.Distance(new Vector3(returnOrigin.x, .045f, returnOrigin.z), warnings[0].transform.position), Is.LessThan(.001f));
            _owner.transform.position += Vector3.forward * 3;
            Emit(1);
            var waves = Waves();
            Assert.That(waves.Count, Is.EqualTo(2));
            Assert.That(Vector3.Distance(waves[1].transform.position, returnOrigin + Vector3.up * TripodAttack.WaveHeight), Is.LessThan(.001f));
            Vector3 direction = (Vector3)typeof(PaintedEnemyProjectile).GetField("_direction", Hidden).GetValue(waves[1]);
            Assert.That(Vector3.Dot(direction, _attack.LockedDirection), Is.LessThan(-.999f));
            Assert.That(waves[1].Owner, Is.SameAs(_owner.GetComponent<PaintedEnemyActor>()));
            Assert.That(_attack.PulseCount, Is.EqualTo(2));
        }

        [Test]
        public void ReturnGapLetsOutboundWaveClearBeforeTheOpposingPulse()
        {
            SelectReturn();
            float travel = Vector3.Distance(_owner.transform.position, _attack.ReturnOrigin) / TripodAttack.WaveSpeed;
            Assert.That(_attack.CurrentPulseGapSeconds, Is.GreaterThanOrEqualTo(travel + .48f + .135f));
            Assert.That(TripodAttack.PulseGapSeconds, Is.GreaterThan(.48f + .135f));
        }

        [Test]
        public void WarningDisksCoverTheCompleteSonicSweepWithoutGaps()
        {
            Begin();
            var warnings = (List<PaintedAttackShadow>)typeof(TripodAttack).GetField("_warnings", Hidden).GetValue(_attack);
            Assert.That(warnings.Count, Is.LessThanOrEqualTo(26));
            for (int i = 1; i < warnings.Count; i++)
            {
                Vector3 delta = warnings[i].transform.position - warnings[i - 1].transform.position;
                Assert.That(delta.magnitude, Is.LessThanOrEqualTo(2 * TripodAttack.WaveRadius));
                Assert.That(Vector3.Dot(delta.normalized, _attack.LockedDirection), Is.GreaterThan(.999f));
            }
            float span = Vector3.Distance(warnings[0].transform.position, warnings[warnings.Count - 1].transform.position);
            Assert.That(span, Is.EqualTo(TripodAttack.WaveSpeed * TripodAttack.WaveLifetime).Within(.001f));
        }

        [Test]
        public void ReturnResonatorCannotMaterializeBehindSweptSolidCover()
        {
            var wall = new GameObject("TripodReturnCover");
            try
            {
                wall.transform.position = Vector3.right * 4 + Vector3.up;
                wall.AddComponent<BoxCollider>().size = new Vector3(.2f, 4, 4);
                wall.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;
                Physics.SyncTransforms();
                SelectReturn();
                Assert.That(_attack.ReturnOrigin.x, Is.GreaterThan(2.5f));
                Assert.That(_attack.ReturnOrigin.x, Is.LessThan(4 - TripodAttack.WaveRadius));
            }
            finally { Object.DestroyImmediate(wall); }
        }

        [Test]
        public void EchoSchedulesExactlyOneFollowupAfterItsWarnedDashRechargeGap()
        {
            Begin(); _attack.Cancel(); Begin();
            for (int i = 0; i < 20 && _attack.Phase == TripodAttack.CastPhase.Windup; i++) _attack.Tick(.05f);
            Assert.That(_attack.Phase, Is.EqualTo(TripodAttack.CastPhase.PulseGap));
            Assert.That(_attack.PulseCount, Is.EqualTo(1));
            for (int i = 0; i < 7; i++) _attack.Tick(.1f);
            Assert.That(_attack.PulseCount, Is.EqualTo(1));
            Assert.That(_attack.WarningCount, Is.GreaterThan(1));
            for (int i = 0; i < 2; i++) _attack.Tick(.1f);
            Assert.That(_attack.PulseCount, Is.EqualTo(2));
            Assert.That(_attack.PulsesInCast, Is.EqualTo(2));
            Assert.That(_attack.HasPendingPulse, Is.False);
            Assert.That(_attack.WarningCount, Is.Zero);
        }

        [Test]
        public void PauseFreezesWarningButVictoryStillClearsPendingHazards()
        {
            Begin(); Emit(0);
            Time.timeScale = 0;
            _attack.Tick(.1f);
            Assert.That(_attack.PhaseElapsed, Is.Zero);
            Assert.That(_attack.WarningCount, Is.GreaterThan(1));
            var player = _playerObject.GetComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetField("<IsVictory>k__BackingField", Hidden).SetValue(player, true);
            _attack.Tick(.1f);
            Assert.That(_attack.WarningCount, Is.Zero);
            Assert.That(_attack.ActiveWaveCount, Is.Zero);
            Assert.That(_attack.HasPendingPulse, Is.False);
        }

        [Test]
        public void FasterCycleStillCannotSkipItsWarningOnAStalledFrame()
        {
            Assert.That(TripodAttack.WaitSeconds + TripodAttack.RecoverySeconds, Is.LessThan(1.3f));
            _attack.Tick(10);
            Assert.That(_attack.Phase, Is.EqualTo(TripodAttack.CastPhase.Wait));
            Assert.That(_attack.PhaseElapsed, Is.EqualTo(.1f).Within(.001f));
            Begin();
            _attack.Tick(10);
            Assert.That(_attack.Phase, Is.EqualTo(TripodAttack.CastPhase.Windup));
            Assert.That(_attack.PulseCount, Is.Zero);
            Assert.That(_attack.WarningCount, Is.GreaterThan(1));
        }

        [Test]
        public void CancellationDisposesReturnWarningsAndBothOwnedPulses()
        {
            SelectReturn();
            Emit(0);
            Emit(1);
            var first = Waves()[0];
            var second = Waves()[1];
            _attack.Cancel();
            Assert.That(first == null && second == null, Is.True);
            Assert.That(_attack.WarningCount, Is.Zero);
            Assert.That(_attack.HasPendingPulse, Is.False);
            Assert.That(_attack.ActiveWaveCount, Is.Zero);
            Assert.That(_attack.ReturnOrigin, Is.EqualTo(Vector3.zero));
        }

        private void SelectReturn()
        {
            Begin(); _attack.Cancel(); Begin(); _attack.Cancel(); Begin();
        }
        private void Begin() => Invoke("BeginWindup", _playerObject.transform.position);
        private void Emit(int index) => Invoke("EmitPulse", index);
        private void Invoke(string method, object argument) => typeof(TripodAttack).GetMethod(method, Hidden).Invoke(_attack, new[] { argument });
        private List<PaintedEnemyProjectile> Waves() => (List<PaintedEnemyProjectile>)typeof(TripodAttack).GetField("_waves", Hidden).GetValue(_attack);
    }
}
