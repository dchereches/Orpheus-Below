using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class RoosterPressureTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _rider, _companion, _playerObject, _cover, _mapObject;
        private ShadeEnemy _shade;
        private RoosterRiderAttack _attack;
        private BlurredEurydice _target;
        private OrpheusPlayerController _player, _previousPlayer;
        private PaintedChamberMap _previousMap;
        private PaintedChamberDefinition _layout;
        private JourneyProgress _previousJourney;
        private float _previousScale;

        [SetUp]
        public void SetUp()
        {
            _previousScale = Time.timeScale; Time.timeScale = 1f;
            _previousJourney = UnderworldJourney.Progress;
            UnderworldJourney.Reset();
            UnderworldJourney.Progress.Visit(7); UnderworldJourney.Progress.ClearCurrentChamber(); UnderworldJourney.Progress.TryRescue();
            _previousMap = PaintedChamberMap.Active;
            typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, null);
            _previousPlayer = OrpheusPlayerController.Instance;
            _playerObject = new GameObject("RoosterPressurePlayer");
            _player = _playerObject.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _player);
            _player.RestoreTravelHealth(100f);
            _rider = new GameObject("RoosterPressureRider");
            _rider.transform.position = new Vector3(540000f, 0f, 0f);
            _shade = _rider.AddComponent<ShadeEnemy>(); _shade.SetupArchetype(ShadeArchetype.MourningAbductor);
            _attack = _rider.GetComponent<RoosterRiderAttack>();
            _companion = new GameObject("RoosterPressureEurydice");
            _target = _companion.AddComponent<BlurredEurydice>();
            // These tests drive combat directly. The first-abduction lesson has its own UI tests;
            // opening that modal here would correctly pause every subsequent combat tick.
            Set(_target, "_shownAbductorHint", true);
            _companion.transform.position = _rider.transform.position + Vector3.right * 4f;
            _playerObject.transform.position = _rider.transform.position + Vector3.back * 6f;
            Set(_attack, "_target", _target);
        }

        [TearDown]
        public void TearDown()
        {
            if (_attack != null) _attack.Cancel();
            Object.DestroyImmediate(_rider); Object.DestroyImmediate(_companion); Object.DestroyImmediate(_playerObject);
            if (_cover != null) Object.DestroyImmediate(_cover);
            if (_mapObject != null) Object.DestroyImmediate(_mapObject);
            if (_layout != null) { PaintedEnemyNavigation.Invalidate(_layout); Object.DestroyImmediate(_layout); }
            foreach (var ring in Object.FindObjectsByType<GroundShockwaveRing>(FindObjectsSortMode.None))
                if (ring.transform.position.x > 539900f && ring.transform.position.x < 540100f) Object.DestroyImmediate(ring.gameObject);
            typeof(PaintedChamberMap).GetProperty("Active").SetValue(null, _previousMap);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previousPlayer);
            typeof(UnderworldJourney).GetProperty("Progress").SetValue(null, _previousJourney);
            Time.timeScale = _previousScale;
            Physics.SyncTransforms();
        }

        [Test]
        public void MissedChargesCycleThroughTheFeintAndWarnedWingKick()
        {
            _attack.Tick(.02f);
            Assert.That(_attack.SelectedAttack, Is.EqualTo(RoosterRiderAttack.RiderAttack.GrabDash));
            _companion.transform.position += Vector3.forward * 12f;
            AdvanceUntil(() => _attack.IsRecovering);
            _companion.transform.position = _rider.transform.position + Vector3.right * 4f;
            AdvanceUntil(() => _attack.SelectedAttack == RoosterRiderAttack.RiderAttack.HookDash && _attack.IsWarning);
            Vector3 firstHeading = _attack.CommittedDirection;
            _companion.transform.position += Vector3.forward * 12f;
            AdvanceUntil(() => _attack.IsHookWarning);
            Assert.That(Vector3.Dot(firstHeading, _attack.CommittedDirection), Is.LessThan(.95f));
            Vector3 hookHeading = _attack.CommittedDirection;
            _companion.transform.position += Vector3.back * 20f;
            _attack.Tick(.02f);
            Assert.That(_attack.CommittedDirection, Is.EqualTo(hookHeading), "The second warning commits its heading too.");
            AdvanceUntil(() => _attack.IsRecovering);
            _playerObject.transform.position = _rider.transform.position + Vector3.right;
            AdvanceUntil(() => _attack.SelectedAttack == RoosterRiderAttack.RiderAttack.WingKick && _attack.IsWarning);
            float health = _player.CurrentHealth;
            Assert.That(health, Is.EqualTo(100f));
            AdvanceUntil(() => _attack.IsRecovering);
            Assert.That(_player.CurrentHealth, Is.LessThan(health));
        }

        [Test]
        public void CommittedDashSweepsPastEurydiceAndRegistersARealAbductor()
        {
            _companion.transform.position = _rider.transform.position + Vector3.right * 2f;
            _attack.Tick(.02f);
            AdvanceUntil(() => _attack.IsDragging);
            Assert.That(_target.IsBeingAbducted, Is.True);
            Assert.That(_attack.DragTarget, Is.SameAs(_target));
            _shade.ApplyStun(.8f);
            Assert.That(_target.IsBeingAbducted, Is.False, "A real stun must release the actual abductor registry immediately.");
            _attack.Tick(.1f);
            Assert.That(_attack.IsDragging, Is.False);
            Assert.That(_attack.IsWarning, Is.False, "Direct ticks may not start a new grab while stunned.");
        }

        [Test]
        public void DragRetreatUsesDeeperExitInsteadOfTheSurfacePassage()
        {
            _layout = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
            _layout.walkableBoundary = new[] { Vector2.zero, new Vector2(1536, 0), new Vector2(1536, 1024), new Vector2(0, 1024) };
            _layout.exit = new Vector2(1100, 512); _layout.returnExit = new Vector2(200, 512);
            _mapObject = new GameObject("RoosterRetreatMap"); _mapObject.transform.position = _rider.transform.position;
            var map = _mapObject.AddComponent<PaintedChamberMap>(); map.definition = _layout; map.MakeActive();
            _companion.transform.position = _rider.transform.position + Vector3.right;
            AdvanceUntil(() => _attack.IsDragging);
            Vector3 before = _rider.transform.position;
            _attack.Tick(.1f);
            Assert.That(Vector3.Dot(_rider.transform.position - before, map.ToWorld(_layout.exit) - before), Is.GreaterThan(0f));
            Assert.That(_attack.RetreatDirection.sqrMagnitude, Is.GreaterThan(.9f));
        }

        [Test]
        public void ApproachingRescuerMakesRiderReleaseForAWarnedKickAndRecoveryOpening()
        {
            _companion.transform.position = _rider.transform.position + Vector3.right;
            AdvanceUntil(() => _attack.IsDragging);
            _playerObject.transform.position = _rider.transform.position + Vector3.back;
            _attack.Tick(.02f);
            Assert.That(_target.IsBeingAbducted, Is.False);
            Assert.That(_attack.SelectedAttack, Is.EqualTo(RoosterRiderAttack.RiderAttack.WingKick));
            Assert.That(_attack.IsWarning, Is.True);
            Vector3 warned = _attack.CommittedDirection;
            _playerObject.transform.position = _rider.transform.position - warned * 4f;
            AdvanceUntil(() => _attack.IsRecovering);
            Assert.That(_player.CurrentHealth, Is.EqualTo(100f), "Moving out of the committed kick must work.");
            _attack.Tick(.1f);
            Assert.That(_attack.IsRecovering, Is.True);
            Assert.That(_target.IsBeingAbducted, Is.False);
        }

        [Test]
        public void CoverInsertedAfterWarningStopsChargeAndPreventsGrabAndDamage()
        {
            _playerObject.transform.position = _companion.transform.position;
            _attack.Tick(.02f);
            MakeCover(_rider.transform.position + Vector3.right * 1.5f);
            AdvanceUntil(() => _attack.IsRecovering);
            Assert.That(_rider.transform.position.x, Is.LessThan(_cover.transform.position.x));
            Assert.That(_target.IsBeingAbducted, Is.False);
            Assert.That(_player.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void DashImmunityConsumesContactInsteadOfDelayingDamage()
        {
            _playerObject.transform.position = _rider.transform.position + Vector3.right * 1.2f;
            Set(_player, "_isDashing", true);
            _attack.Tick(.02f);
            AdvanceUntil(() => _rider.transform.position.x > _playerObject.transform.position.x + .5f);
            Set(_player, "_isDashing", false);
            AdvanceUntil(() => _attack.IsDragging || _attack.IsRecovering);
            Assert.That(_player.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void PauseFreezesAWarningAndTerminalStateCancelsAllOwnedVisuals()
        {
            _companion.transform.position = _rider.transform.position + Vector3.right;
            AdvanceUntil(() => _attack.IsDragging);
            _playerObject.transform.position = _rider.transform.position + Vector3.back;
            _attack.Tick(.02f);
            Assert.That(_attack.SelectedAttack, Is.EqualTo(RoosterRiderAttack.RiderAttack.WingKick));
            Vector3 origin = _rider.transform.position;
            int warnings = LocalWarnings();
            Assert.That(warnings, Is.GreaterThan(0));
            var effects = (System.Collections.Generic.List<GameObject>)typeof(RoosterRiderAttack).GetField("_effects", Hidden).GetValue(_attack);
            var ownedEffects = effects.ToArray();
            Assert.That(ownedEffects.Length, Is.GreaterThan(0), "The real kick warning must own a charge effect to test its cancellation.");
            Time.timeScale = 0f;
            for (int i = 0; i < 30; i++) _attack.Tick(.1f);
            Assert.That(_attack.IsWarning, Is.True);
            Assert.That(_rider.transform.position, Is.EqualTo(origin));
            Assert.That(LocalWarnings(), Is.EqualTo(warnings));
            typeof(OrpheusPlayerController).GetProperty("IsVictory").SetValue(_player, true);
            _attack.Tick(.1f);
            Assert.That(LocalWarnings(), Is.Zero);
            Assert.That(_attack.IsWarning, Is.False);
            Assert.That(_target.IsBeingAbducted, Is.False);
            Assert.That(effects, Is.Empty);
            foreach (var effect in ownedEffects)
                Assert.That(effect == null, Is.True, "Terminal cancellation must destroy every effect this attack owned.");
        }

        [Test]
        public void DisableReleasesCompanionAndCannotRestartAGrab()
        {
            _companion.transform.position = _rider.transform.position + Vector3.right;
            AdvanceUntil(() => _attack.IsDragging);
            _rider.SetActive(false);
            _attack.Tick(10f);
            Assert.That(_target.IsBeingAbducted, Is.False);
            Assert.That(_attack.IsWarning, Is.False);
            Assert.That(LocalWarnings(), Is.Zero);
        }

        [Test]
        public void LethalHitReleasesTheActualAbductionRegistry()
        {
            _companion.transform.position = _rider.transform.position + Vector3.right;
            AdvanceUntil(() => _attack.IsDragging);
            _shade.OnOrphicHit(10000f, 1f, true, _rider.transform.position - Vector3.right);
            Assert.That(_shade.IsPacified, Is.True);
            Assert.That(_target.IsBeingAbducted, Is.False);
            Assert.That(_attack.DragTarget, Is.Null);
            _attack.Tick(.1f);
            Assert.That(_attack.IsWarning, Is.False);
        }

        [Test]
        public void CancellingTheFeintWarningRemovesTheScheduledHook()
        {
            _attack.Tick(.02f);
            _companion.transform.position += Vector3.forward * 12f;
            AdvanceUntil(() => _attack.IsRecovering);
            _companion.transform.position = _rider.transform.position + Vector3.right * 4f;
            AdvanceUntil(() => _attack.SelectedAttack == RoosterRiderAttack.RiderAttack.HookDash && _attack.IsWarning);
            _companion.transform.position += Vector3.forward * 12f;
            AdvanceUntil(() => _attack.IsHookWarning);
            Vector3 stoppedAt = _rider.transform.position;
            _shade.ApplyStun(1f);
            for (int i = 0; i < 50; i++) _attack.Tick(.1f);
            Assert.That(_rider.transform.position, Is.EqualTo(stoppedAt));
            Assert.That(LocalWarnings(), Is.Zero);
            Assert.That(_target.IsBeingAbducted, Is.False);
        }

        [Test]
        public void DashCannotTunnelAcrossAThinUnwalkableStripWithoutACollider()
        {
            _layout = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
            _layout.walkableBoundary = new[] { Vector2.zero, new Vector2(1536, 0), new Vector2(1536, 1024), new Vector2(0, 1024) };
            _mapObject = new GameObject("RoosterThinGapMap"); _mapObject.transform.position = _rider.transform.position;
            var map = _mapObject.AddComponent<PaintedChamberMap>(); map.definition = _layout; map.MakeActive();
            _companion.transform.position = _rider.transform.position + UnderworldCamera2D.IsoRightXZ * 4f;
            _attack.Tick(.02f);
            Assert.That(_attack.IsWarning, Is.True);
            _layout.cover = new[] { new PaintedChamberDefinition.Cover
            {
                footprint = new[] { new Vector2(817, 0), new Vector2(823, 0), new Vector2(823, 1024), new Vector2(817, 1024) }
            } };
            AdvanceUntil(() => _attack.IsRecovering);
            Assert.That(map.ToImage(_rider.transform.position).x, Is.LessThan(817f));
            Assert.That(_target.IsBeingAbducted, Is.False);
        }

        [Test]
        public void KickCannotHitThroughCoverAddedAfterItsWarning()
        {
            _companion.transform.position = _rider.transform.position + Vector3.right;
            AdvanceUntil(() => _attack.IsDragging);
            _playerObject.transform.position = _rider.transform.position + Vector3.forward * 1.4f;
            _attack.Tick(.02f);
            Assert.That(_attack.SelectedAttack, Is.EqualTo(RoosterRiderAttack.RiderAttack.WingKick));
            _cover = new GameObject("RoosterKickCover");
            _cover.transform.position = _rider.transform.position + Vector3.forward * .65f + Vector3.up;
            _cover.AddComponent<BoxCollider>().size = new Vector3(4f, 3f, .12f);
            _cover.AddComponent<IsoObstacle>(); Physics.SyncTransforms();
            AdvanceUntil(() => _attack.IsRecovering);
            Assert.That(_player.CurrentHealth, Is.EqualTo(100f));
        }

        private void MakeCover(Vector3 at)
        {
            _cover = new GameObject("RoosterCover"); _cover.transform.position = at + Vector3.up;
            _cover.AddComponent<BoxCollider>().size = new Vector3(.2f, 3f, 8f);
            _cover.AddComponent<IsoObstacle>(); Physics.SyncTransforms();
        }

        private void AdvanceUntil(Func<bool> reached)
        {
            for (int i = 0; i < 400 && !reached(); i++) _attack.Tick(.02f);
            Assert.That(reached(), Is.True, "The requested real combat phase was never reached.");
        }

        private int LocalWarnings()
        {
            int count = 0;
            foreach (var warning in Object.FindObjectsByType<PaintedAttackShadow>(FindObjectsSortMode.None))
                if (warning.transform.position.x > 539900f) count++;
            return count;
        }

        private static void Set(object instance, string name, object value)
            => instance.GetType().GetField(name, Hidden).SetValue(instance, value);
    }
}
