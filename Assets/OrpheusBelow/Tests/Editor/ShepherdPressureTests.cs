using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class ShepherdPressureTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _owner, _playerObject;
        private PaintedEnemyActor _actor;
        private ShepherdAttack _attack;
        private OrpheusPlayerController _player, _previousPlayer;
        private PaintedChamberMap _previousMap;
        private float _timeScale;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale; Time.timeScale = 1;
            _previousMap = PaintedChamberMap.Active;
            typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, null);
            _previousPlayer = OrpheusPlayerController.Instance;
            _playerObject = new GameObject("ShepherdPressurePlayer");
            _player = _playerObject.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _player);
            _owner = new GameObject("ShepherdPressureOwner");
            _owner.transform.position = Vector3.right * 450000f;
            _playerObject.transform.position = _owner.transform.position + Vector3.right * 6;
            _actor = _owner.AddComponent<PaintedEnemyActor>();
            _actor.Configure(PaintedEnemyKind.Shepherd);
            _attack = (ShepherdAttack)_actor.Pattern;
        }

        [TearDown]
        public void TearDown()
        {
            _attack.Cancel();
            Object.DestroyImmediate(_owner); Object.DestroyImmediate(_playerObject);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, _previousMap);
            Time.timeScale = _timeScale;
        }

        [Test]
        public void RealCycleSummonsCommandsAndFiresThreeDifferentCallsWithinEightSeconds()
        {
            var calls = new List<ShepherdAttack.BellCall>();
            int previousCast = 0;
            for (int i = 0; i < 400; i++)
            {
                _actor.Tick(.02f);
                if (_attack.CastCount != previousCast)
                {
                    previousCast = _attack.CastCount; calls.Add(_attack.Variant);
                }
                if (_attack.VolleyCount > 0) break;
            }
            Assert.That(calls, Is.EqualTo(new[] { ShepherdAttack.BellCall.CallFlock,
                ShepherdAttack.BellCall.RallyCharge, ShepherdAttack.BellCall.BellVolley }));
            Assert.That(_attack.LivingSheep, Is.EqualTo(2));
            Assert.That(_attack.CommandCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(_attack.ActiveProjectileCount, Is.EqualTo(3));
            foreach (var owner in Flock())
            {
                var sheep = owner.GetComponent<SheepAttack>();
                owner.GetComponent<PaintedEnemyActor>().Tick(.02f);
                Assert.That(sheep.Phase, Is.EqualTo(SheepAttack.DashPhase.Warning));
                Assert.That(sheep.WarningCount, Is.GreaterThan(1));
            }
        }

        [Test]
        public void FullLivingFlockKeepsPressureWithoutExceedingThreeSheep()
        {
            Advance(18f);
            Assert.That(_attack.LivingSheep, Is.EqualTo(3));
            int volleys = _attack.VolleyCount;
            int commands = _attack.CommandCount;
            Advance(8f);
            Assert.That(_attack.LivingSheep, Is.EqualTo(3));
            Assert.That(_attack.VolleyCount, Is.GreaterThan(volleys));
            Assert.That(_attack.CommandCount, Is.GreaterThan(commands));
            Assert.That(Flock().Count, Is.EqualTo(3));
        }

        [Test]
        public void CausewaySummonsUseConnectedFloorAndMaterializeAtTheirResolvedWarnings()
        {
            var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_04");
            var map = new GameObject("ShepherdCausewayMap").AddComponent<PaintedChamberMap>();
            map.definition = layout;
            typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, map);
            try
            {
                _owner.transform.position = map.NearestWalkable(map.ToWorld(layout.entrance), .65f);
                _playerObject.transform.position = map.ToWorld(layout.exit);
                Advance(.55f);
                var positions = new List<Vector3>((List<Vector3>)typeof(ShepherdAttack).GetField("_positions", Hidden).GetValue(_attack));
                Assert.That(positions, Is.Not.Empty);
                foreach (var position in positions)
                {
                    Assert.That(map.IsWalkable(position, .6f), Is.True);
                    Assert.That(Vector3.Distance(position, _owner.transform.position), Is.GreaterThanOrEqualTo(3));
                    Assert.That(PaintedEnemyNavigation.CanReach(map, _owner.transform.position, position), Is.True);
                }
                Advance(ShepherdAttack.WarningSeconds + .03f);
                Assert.That(Flock().Count, Is.EqualTo(positions.Count));
                foreach (var sheep in Flock()) Assert.That(positions, Does.Contain(sheep.transform.position));
            }
            finally
            {
                _attack.Cancel();
                Object.DestroyImmediate(map.gameObject);
                typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, null);
            }
        }

        [Test]
        public void VolleyLocksOriginAndAimAndLeavesWideGapsBetweenItsThreeWarnedLanes()
        {
            BeginVolley();
            Vector3 direction = _attack.LockedDirection;
            Vector3 origin = _attack.VolleyOrigin;
            Assert.That(_attack.WarningCount, Is.GreaterThan(12));
            _owner.transform.position += Vector3.forward * 2;
            _playerObject.transform.position += Vector3.back * 6;
            Advance(ShepherdAttack.WarningSeconds + .03f);
            Assert.That(_attack.LockedDirection, Is.EqualTo(direction));
            var bells = Bells();
            Assert.That(bells.Count, Is.EqualTo(3));
            Vector3 previous = Vector3.zero;
            foreach (var bell in bells)
            {
                Assert.That(bell.Owner, Is.SameAs(_actor));
                Assert.That(Vector3.Distance(bell.transform.position, origin + Vector3.up * ShepherdAttack.BellHeight), Is.LessThan(.05f));
                Vector3 ray = (Vector3)typeof(PaintedEnemyProjectile).GetField("_direction", Hidden).GetValue(bell);
                if (previous != Vector3.zero)
                    Assert.That(Vector3.Distance(previous * 5, ray * 5), Is.GreaterThan(2 * ShepherdAttack.BellRadius + 1));
                previous = ray;
            }
            Assert.That(_attack.WarningCount, Is.Zero);
        }

        [Test]
        public void SolidCoverStopsTheActualBellProjectile()
        {
            BeginVolley(); Advance(ShepherdAttack.WarningSeconds + .03f);
            var bell = Bells()[1];
            var wall = new GameObject("ShepherdBellCover");
            try
            {
                wall.transform.position = bell.transform.position + Vector3.right * 2;
                wall.AddComponent<BoxCollider>().size = new Vector3(.2f, 4, 8);
                wall.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;
                Physics.SyncTransforms();
                bell.Advance(.5f);
                Assert.That(bell == null, Is.True);
            }
            finally { Object.DestroyImmediate(wall); }
        }

        [Test]
        public void PauseFreezesWarningAndTerminalStateStillCancelsItsFlockAndProjectiles()
        {
            Advance(2f); BeginVolley();
            Time.timeScale = 0;
            float elapsed = _attack.PhaseElapsed;
            _attack.Tick(10f);
            Assert.That(_attack.PhaseElapsed, Is.EqualTo(elapsed));
            Assert.That(_attack.WarningCount, Is.GreaterThan(0));
            Time.timeScale = 1;
            Advance(ShepherdAttack.WarningSeconds + .03f);
            Assert.That(_attack.ActiveProjectileCount, Is.EqualTo(3));
            Time.timeScale = 0;
            typeof(OrpheusPlayerController).GetProperty("IsVictory").SetValue(_player, true);
            _attack.Tick(.1f);
            AssertCancelled();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StunOrDisableCancelsSummonsWarningsAndActiveBells(bool disable)
        {
            ShadeEnemy shade = null;
            if (!disable)
            {
                shade = _owner.AddComponent<ShadeEnemy>();
                typeof(ShadeEnemy).GetField("_hp", Hidden).SetValue(shade, shade.maxHealth);
                typeof(ShadeEnemy).GetField("_paintedActor", Hidden).SetValue(shade, _actor);
                _actor.Configure(PaintedEnemyKind.Shepherd, shade);
            }
            Advance(2f); BeginVolley(); Advance(ShepherdAttack.WarningSeconds + .03f);
            Assert.That(_attack.LivingSheep, Is.GreaterThan(0));
            Assert.That(_attack.ActiveProjectileCount, Is.EqualTo(3));
            if (disable) { _owner.SetActive(false); _attack.Tick(.01f); } // EditMode has no player-loop OnDisable dispatch.
            else shade.ApplyStun(1);
            AssertCancelled();
        }

        [Test]
        public void DeathAlsoCancelsPendingSpawnPositionsBeforeTheyMaterialize()
        {
            var shade = _owner.AddComponent<ShadeEnemy>();
            typeof(ShadeEnemy).GetField("_hp", Hidden).SetValue(shade, shade.maxHealth);
            _actor.Configure(PaintedEnemyKind.Shepherd, shade);
            Advance(.55f);
            Assert.That(_attack.PendingSummons, Is.EqualTo(2));
            typeof(ShadeEnemy).GetField("_hp", Hidden).SetValue(shade, 0f);
            _actor.Tick(.1f);
            AssertCancelled();
            Advance(3f);
            Assert.That(_attack.LivingSheep, Is.Zero);
        }

        [Test]
        public void ALongFrameCannotSwallowTheCommittedWarning()
        {
            BeginVolley(); _attack.Tick(100);
            Assert.That(_attack.Phase, Is.EqualTo(ShepherdAttack.BellPhase.Warning));
            Assert.That(_attack.PhaseElapsed, Is.EqualTo(.1f).Within(.001f));
            Assert.That(_attack.ActiveProjectileCount, Is.Zero);
            Assert.That(_attack.WarningCount, Is.GreaterThan(0));
        }

        private void Advance(float seconds)
        {
            for (float left = seconds; left > .0001f; left -= .02f)
            {
                _actor.Tick(Mathf.Min(.02f, left));
                Assert.That(_attack.LivingSheep, Is.LessThanOrEqualTo(ShepherdAttack.MaximumLivingSheep));
            }
        }
        private void BeginVolley()
        {
            for (int i = 0; i < 3; i++)
            {
                typeof(ShepherdAttack).GetMethod("BeginBell", Hidden).Invoke(_attack, new object[] { _playerObject.transform.position });
                if (_attack.Variant == ShepherdAttack.BellCall.BellVolley) return;
            }
            Assert.Fail("The bell must rotate into its own projectile attack");
        }
        private List<PaintedEnemyProjectile> Bells() => (List<PaintedEnemyProjectile>)typeof(ShepherdAttack).GetField("_bells", Hidden).GetValue(_attack);
        private List<PaintedSummonOwnership> Flock()
        {
            var result = new List<PaintedSummonOwnership>();
            foreach (var owner in Object.FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None))
                if (owner.Owner == _actor) result.Add(owner);
            return result;
        }
        private void AssertCancelled()
        {
            Assert.That(_attack.PendingSummons, Is.Zero);
            Assert.That(_attack.WarningCount, Is.Zero);
            Assert.That(_attack.ActiveProjectileCount, Is.Zero);
            Assert.That(Flock(), Is.Empty);
            Assert.That(_attack.Phase, Is.EqualTo(ShepherdAttack.BellPhase.Rest));
        }
    }
}
