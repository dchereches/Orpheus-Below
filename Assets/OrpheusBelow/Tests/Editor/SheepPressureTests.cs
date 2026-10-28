using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class SheepPressureTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _owner, _playerObject, _cover, _mapObject;
        private SheepAttack _attack;
        private PaintedEnemyActor _actor;
        private OrpheusPlayerController _player, _previousPlayer;
        private PaintedChamberMap _previousMap;
        private PaintedChamberDefinition _layout;
        private float _previousScale;

        [SetUp]
        public void SetUp()
        {
            _previousScale = Time.timeScale; Time.timeScale = 1f;
            _previousPlayer = OrpheusPlayerController.Instance;
            _previousMap = PaintedChamberMap.Active;
            typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, null);
            _playerObject = new GameObject("SheepPressurePlayer");
            _player = _playerObject.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _player);
            _player.RestoreTravelHealth(100f);
            _owner = new GameObject("SheepPressureOwner");
            _owner.transform.position = Vector3.right * 620000f;
            _actor = _owner.AddComponent<PaintedEnemyActor>();
            _attack = _owner.AddComponent<SheepAttack>(); _attack.Initialize(_actor);
            PlacePlayer(Vector3.right * 2f);
        }

        [TearDown]
        public void TearDown()
        {
            _attack.Cancel();
            Object.DestroyImmediate(_owner); Object.DestroyImmediate(_playerObject);
            if (_cover != null) Object.DestroyImmediate(_cover);
            if (_mapObject != null) Object.DestroyImmediate(_mapObject);
            if (_layout != null) { PaintedEnemyNavigation.Invalidate(_layout); Object.DestroyImmediate(_layout); }
            foreach (var ring in Object.FindObjectsByType<GroundShockwaveRing>(FindObjectsSortMode.None))
                if (ring.transform.position.x > 619900f) Object.DestroyImmediate(ring.gameObject);
            typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, _previousMap);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            Time.timeScale = _previousScale; Physics.SyncTransforms();
        }

        [Test]
        public void AutonomousAttacksActuallyChargeHookAndStompWithARecoveryOpening()
        {
            foreach (var mode in new[] { SheepAttack.SheepMode.Charge, SheepAttack.SheepMode.HookCharge, SheepAttack.SheepMode.Stomp })
            {
                PlacePlayer(Vector3.right * 1.2f);
                AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Warning);
                Assert.That(_attack.Variant, Is.EqualTo(mode));
                Vector3 before = _owner.transform.position;
                if (mode != SheepAttack.SheepMode.Stomp) PlacePlayer(Vector3.forward * 20f);
                else Set(_player, "_invulnTimer", 0f);
                float health = _player.CurrentHealth;
                AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
                if (mode == SheepAttack.SheepMode.Stomp)
                {
                    Assert.That(_owner.transform.position, Is.EqualTo(before));
                    Assert.That(_player.CurrentHealth, Is.LessThan(health));
                }
                else Assert.That(Vector3.Distance(before, _owner.transform.position), Is.GreaterThan(1f));
                _attack.Tick(.1f);
                Assert.That(_attack.Phase, Is.EqualTo(SheepAttack.DashPhase.Recover));
            }
        }

        [Test]
        public void StraightChargeCrossesThePlayerButOnlyDamagesOnceAcrossManySlices()
        {
            PlacePlayer(Vector3.right * 2f);
            _attack.CommandDash(_playerObject.transform.position);
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Dash);
            float health = _player.CurrentHealth;
            for (int i = 0; i < 30 && _attack.Phase == SheepAttack.DashPhase.Dash; i++)
            {
                Set(_player, "_invulnTimer", 0f); _attack.Tick(.02f);
            }
            Assert.That(_attack.HasHit, Is.True);
            Assert.That(_player.CurrentHealth, Is.EqualTo(health - SheepAttack.DashDamage * DifficultySettings.DamageMultiplier).Within(.001f));
            Assert.That(_owner.transform.position.x, Is.GreaterThan(_playerObject.transform.position.x));
        }

        [Test]
        public void EveryPointOfTheSweptHitEnvelopeFitsInsideTheCommittedWarning()
        {
            _attack.CommandDash(_owner.transform.position + Vector3.right * 10f);
            var disks = Warnings();
            Assert.That(disks.Count, Is.InRange(2, 30));
            float length = SheepAttack.DashSpeed * SheepAttack.DashSeconds;
            for (float distance = 0; distance <= length; distance += .1f)
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 point = _owner.transform.position + Vector3.right * distance + Vector3.forward * (side * SheepAttack.HitRadius);
                    Assert.That(Covered(point, disks), Is.True, "A damaging edge must lie inside a warning disk.");
                }
            Vector3 locked = _attack.LockedDirection;
            PlacePlayer(Vector3.back * 8f); _attack.Tick(.1f);
            Assert.That(_attack.LockedDirection, Is.EqualTo(locked));
        }

        [Test]
        public void HookWarnsItsSecondLockedLegBeforeTurningAndNeverTracksThePlayer()
        {
            BeginAutonomousHook();
            Vector3 first = _attack.LockedDirection;
            PlacePlayer(Vector3.forward * 20f);
            AdvanceUntil(() => _attack.LegIndex == 1 && _attack.Phase == SheepAttack.DashPhase.Warning);
            Vector3 second = _attack.LockedDirection;
            Assert.That(Vector3.Dot(first, second), Is.LessThan(.8f));
            Assert.That(_attack.WarningCount, Is.GreaterThan(1));
            Vector3 before = _owner.transform.position;
            PlacePlayer(Vector3.back * 30f); _attack.Tick(.1f);
            Assert.That(_owner.transform.position, Is.EqualTo(before));
            Assert.That(_attack.LockedDirection, Is.EqualTo(second));
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
            Assert.That(Vector3.Dot(_owner.transform.position - before, second), Is.GreaterThan(1f));
        }

        [Test]
        public void HookCannotDamageAgainOnItsReturnEvenAfterPlayerInvulnerabilityExpires()
        {
            BeginAutonomousHook();
            PlacePlayer(_attack.LockedDirection * .3f);
            AdvanceUntil(() => _attack.LegIndex == 1 && _attack.Phase == SheepAttack.DashPhase.Warning);
            Assert.That(_attack.HasHit, Is.True);
            float health = _player.CurrentHealth;
            PlacePlayer(_attack.LockedDirection * .3f);
            Set(_player, "_invulnTimer", 0f);
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
            Assert.That(_player.CurrentHealth, Is.EqualTo(health));
        }

        [Test]
        public void BossStageHookUsesAWarnedReturnAlongTheStageRail()
        {
            typeof(OrpheusPlayerController).GetProperty("IsIn2DBossStage").SetValue(_player, true);
            typeof(PaintedEnemyActor).GetProperty("Kind").SetValue(_actor, PaintedEnemyKind.Sheep);
            typeof(PaintedEnemyActor).GetProperty("StageCenter").SetValue(_actor, _owner.transform.position);
            PlacePlayer(UnderworldCamera2D.IsoRightXZ * 2f);
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Warning);
            PlacePlayer(UnderworldCamera2D.IsoRightXZ * -8f);
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
            PlacePlayer(UnderworldCamera2D.IsoRightXZ * 2f);
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Warning);
            Vector3 origin = _owner.transform.position, first = _attack.LockedDirection;
            AdvanceUntil(() => _attack.LegIndex == 1 && _attack.Phase == SheepAttack.DashPhase.Warning);
            Assert.That(Vector3.Dot(first, _attack.LockedDirection), Is.LessThan(-.99f));
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
            Assert.That(Vector3.Cross(_owner.transform.position - origin, UnderworldCamera2D.IsoRightXZ).magnitude, Is.LessThan(.1f));
        }

        [Test]
        public void ShepherdCommandQueuesAStraightDashWithoutOverwritingAnActiveWarning()
        {
            _attack.RequestCommand(_owner.transform.position + Vector3.right * 6f);
            _attack.Tick(.02f);
            Assert.That(_attack.Variant, Is.EqualTo(SheepAttack.SheepMode.Charge));
            Assert.That(_attack.Phase, Is.EqualTo(SheepAttack.DashPhase.Warning));
            _attack.RequestCommand(_owner.transform.position + Vector3.back * 6f);
            _attack.Tick(.1f);
            Assert.That(Vector3.Dot(_attack.LockedDirection, Vector3.right), Is.GreaterThan(.99f));
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
            _attack.Tick(.02f);
            Assert.That(_attack.Phase, Is.EqualTo(SheepAttack.DashPhase.Warning));
            Assert.That(_attack.LockedDirection.z, Is.LessThan(0f));
        }

        [Test]
        public void DashingThroughContactCannotBecomeALateHitFromTheSameAttack()
        {
            PlacePlayer(Vector3.right * .3f); Set(_player, "_isDashing", true);
            _attack.CommandDash(_playerObject.transform.position);
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Dash);
            _attack.Tick(.02f); Set(_player, "_isDashing", false);
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
            Assert.That(_player.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void CoverInsertedAfterWarningStopsTheChargeAndShieldsThePlayer()
        {
            PlacePlayer(Vector3.right * 3f);
            _attack.CommandDash(_playerObject.transform.position);
            _cover = new GameObject("SheepPressureWall");
            _cover.transform.position = _owner.transform.position + Vector3.right * 1.5f + Vector3.up;
            _cover.AddComponent<BoxCollider>().size = new Vector3(.2f, 3f, 8f);
            _cover.AddComponent<IsoObstacle>(); Physics.SyncTransforms();
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
            Assert.That(_owner.transform.position.x, Is.LessThan(_cover.transform.position.x));
            Assert.That(_player.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void ChargeCannotCrossAThinUnwalkableStripWithoutACollider()
        {
            _layout = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
            _layout.walkableBoundary = new[] { Vector2.zero, new Vector2(1536, 0), new Vector2(1536, 1024), new Vector2(0, 1024) };
            _mapObject = new GameObject("SheepPressureMap"); _mapObject.transform.position = _owner.transform.position;
            var map = _mapObject.AddComponent<PaintedChamberMap>(); map.definition = _layout;
            map.MakeActive();
            Assert.That(PaintedChamberMap.Active, Is.SameAs(map));
            Assert.That(map.IsWalkable(_owner.transform.position, .4f), Is.True);
            PlacePlayer(UnderworldCamera2D.IsoRightXZ * 4f);
            _attack.CommandDash(_playerObject.transform.position);
            _layout.cover = new[] { new PaintedChamberDefinition.Cover { footprint = new[] {
                new Vector2(817, 0), new Vector2(823, 0), new Vector2(823, 1024), new Vector2(817, 1024) } } };
            Assert.That(map.IsWalkable(map.ToWorld(new Vector2(820, 512)), .4f), Is.False,
                "The active floor must contain the thin inaccessible strip before the charge moves.");
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
            Assert.That(map.ToImage(_owner.transform.position).x, Is.LessThan(817f));
        }

        [Test]
        public void PauseFreezesTheWarningButVictoryClearsItEvenWhilePaused()
        {
            _attack.CommandDash(_playerObject.transform.position);
            int count = _attack.WarningCount; Vector3 origin = _owner.transform.position;
            Time.timeScale = 0f;
            for (int i = 0; i < 30; i++) _attack.Tick(.1f);
            Assert.That(_attack.Phase, Is.EqualTo(SheepAttack.DashPhase.Warning));
            Assert.That(_attack.WarningCount, Is.EqualTo(count));
            Assert.That(_owner.transform.position, Is.EqualTo(origin));
            typeof(OrpheusPlayerController).GetProperty("IsVictory").SetValue(_player, true);
            _attack.Tick(.1f);
            Assert.That(_attack.WarningCount, Is.Zero);
            Assert.That(_attack.Phase, Is.EqualTo(SheepAttack.DashPhase.Rest));
        }

        [Test]
        public void StunCancelsAHookAndQueuedBellWithoutRestartingItOnDirectTicks()
        {
            var shade = _owner.AddComponent<ShadeEnemy>(); Set(shade, "_hp", 36f);
            typeof(PaintedEnemyActor).GetProperty("OwnerShade").SetValue(_actor, shade);
            BeginAutonomousHook();
            AdvanceUntil(() => _attack.LegIndex == 1 && _attack.Phase == SheepAttack.DashPhase.Warning);
            _attack.RequestCommand(_playerObject.transform.position);
            Set(shade, "_stunTimer", 1f); _attack.Tick(.02f);
            Vector3 origin = _owner.transform.position;
            for (int i = 0; i < 50; i++) _attack.Tick(.1f);
            Assert.That(_attack.WarningCount, Is.Zero);
            Assert.That(_owner.transform.position, Is.EqualTo(origin));
            Set(shade, "_stunTimer", 0f); _attack.Tick(.02f);
            Assert.That(_attack.Phase, Is.EqualTo(SheepAttack.DashPhase.Rest));
        }

        [Test]
        public void DisableCancelsTheQueuedCommandAndOwnedVisuals()
        {
            BeginAutonomousHook(); _attack.RequestCommand(_playerObject.transform.position);
            _owner.SetActive(false); _attack.Tick(.1f);
            Assert.That(_attack.WarningCount, Is.Zero);
            _owner.SetActive(true); _attack.Tick(.02f);
            Assert.That(_attack.Phase, Is.EqualTo(SheepAttack.DashPhase.Rest));
        }

        [Test]
        public void SummonedSheepRemainDamageableAndDeathCancelsTheirCommittedAttack()
        {
            var shade = _owner.AddComponent<ShadeEnemy>();
            Set(shade, "_hp", 36f);
            typeof(PaintedEnemyActor).GetProperty("OwnerShade").SetValue(_actor, shade);
            shade.OnOrphicHit(5f, 1f, true, _owner.transform.position - Vector3.right);
            Assert.That(shade.CurrentHealth, Is.LessThan(36f));
            Set(shade, "_stunTimer", 0f);
            _attack.CommandDash(_playerObject.transform.position);
            _attack.RequestCommand(_playerObject.transform.position);
            Set(shade, "_hp", 0f);
            _attack.Tick(.02f);
            Assert.That(_attack.WarningCount, Is.Zero);
            Assert.That(_attack.Phase, Is.EqualTo(SheepAttack.DashPhase.Rest));
            Assert.That(_attack.LockedDirection, Is.EqualTo(Vector3.zero));
        }

        private void BeginAutonomousHook()
        {
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Warning);
            PlacePlayer(Vector3.forward * 20f);
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Recover);
            PlacePlayer(Vector3.right * 2f);
            AdvanceUntil(() => _attack.Phase == SheepAttack.DashPhase.Warning);
            Assert.That(_attack.Variant, Is.EqualTo(SheepAttack.SheepMode.HookCharge));
        }
        private void AdvanceUntil(Func<bool> reached)
        {
            for (int i = 0; i < 400 && !reached(); i++) _attack.Tick(.02f);
            Assert.That(reached(), Is.True, "The requested real combat phase was never reached.");
        }
        private void PlacePlayer(Vector3 offset) => _playerObject.transform.position = _owner.transform.position + offset;
        private List<PaintedAttackShadow> Warnings() => (List<PaintedAttackShadow>)typeof(SheepAttack).GetField("_warnings", Hidden).GetValue(_attack);
        private static bool Covered(Vector3 point, List<PaintedAttackShadow> disks)
        {
            foreach (var disk in disks)
            {
                Vector3 gap = point - disk.transform.position; gap.y = 0;
                if (gap.magnitude <= disk.Radius + .04f) return true;
            }
            return false;
        }
        private static void Set(object instance, string name, object value) => instance.GetType().GetField(name, Hidden).SetValue(instance, value);
    }
}
