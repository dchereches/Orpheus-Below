using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Attackable sheep commit to a charge, a two-beat hook, or a close stomp.</summary>
    public sealed class SheepAttack : PaintedAttackPattern
    {
        public const float WarningSeconds = .65f;
        public const float DashSpeed = 14f;
        public const float DashSeconds = .45f;
        public const float DashDamage = 14f;
        public const float HitRadius = .85f;
        private const float RestSeconds = .42f;
        private const float RecoverySeconds = .38f;
        private const float HookDistance = 2.8f;
        private const float HookWarningSeconds = .4f;
        private const float StompWarningSeconds = .5f;
        private const float StompRadius = 1.65f;
        private const float StompSeconds = .12f;
        private const float MovementSlice = .1f;
        public enum DashPhase { Rest, Warning, Dash, Recover }
        public enum SheepMode { Charge, HookCharge, Stomp }
        public DashPhase Phase { get; private set; }
        public SheepMode Variant { get; private set; }
        public Vector3 LockedDirection { get; private set; }
        public bool HasHit { get; private set; }
        public int WarningCount => _warnings.Count;
        public int LegIndex { get; private set; }
        private readonly List<PaintedAttackShadow> _warnings = new List<PaintedAttackShadow>(16);
        private readonly List<GameObject> _effects = new List<GameObject>(2);
        private readonly Vector3[] _legEnds = new Vector3[2];
        private float _elapsed;
        private int _attackIndex;
        private Vector3 _legOrigin;
        private bool _commandPending, _contactSpent, _stageAttack;
        private Vector3 _commandTarget;

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel(); base.Initialize(actor); _attackIndex = 0;
        }

        public override void Tick(float dt)
        {
            if (Actor == null) return;
            var player = OrpheusPlayerController.Instance;
            if (!CanAttack() || player == null ||
                (Phase != DashPhase.Rest && Actor.IsBossStage != _stageAttack))
            { Cancel(); return; }
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt) || Time.timeScale <= 0f ||
                UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;

            // One transition per tick retains every warning even after a stalled frame.
            float step = Mathf.Min(dt, .1f);
            _elapsed += step;
            if (_commandPending && (Phase == DashPhase.Rest || Phase == DashPhase.Recover))
            {
                Vector3 target = _commandTarget;
                _commandPending = false;
                CommandDash(target);
                return;
            }
            switch (Phase)
            {
                case DashPhase.Rest:
                    Actor.Art?.Play(EnemyMotion.Move, .8f, true);
                    Actor.Face(player.transform.position);
                    Vector3 toward = Flat(player.transform.position - Actor.transform.position);
                    var next = (SheepMode)(_attackIndex % 3);
                    if (toward.magnitude > (next == SheepMode.Stomp ? 2f : 3.5f))
                        Actor.MoveToward(player.transform.position, 4.8f, step, navigate: true);
                    if (_elapsed >= RestSeconds && (next != SheepMode.Stomp || toward.magnitude <= 2f))
                    {
                        _attackIndex++;
                        BeginAttack(next, player.transform.position);
                    }
                    break;
                case DashPhase.Warning:
                    // A displacement cannot drag a committed warning onto an unwarned lane.
                    if (Flat(Actor.transform.position - _legOrigin).sqrMagnitude > .04f)
                    { Cancel(); return; }
                    float warningTime = Variant == SheepMode.Stomp ? StompWarningSeconds :
                        LegIndex == 1 ? HookWarningSeconds : WarningSeconds;
                    foreach (var warning in _warnings) warning?.Progress(_elapsed / warningTime);
                    if (_elapsed >= warningTime)
                    {
                        ClearWarnings(); _elapsed = 0f; Phase = DashPhase.Dash;
                        Actor.Art?.Play(Variant == SheepMode.Charge ? EnemyMotion.Attack : EnemyMotion.Attack2,
                            Variant == SheepMode.Stomp ? StompSeconds : DashSeconds);
                        if (Variant == SheepMode.Stomp)
                        {
                            _contactSpent = true;
                            HasHit = Actor.TryHitPlayer(_legOrigin, StompRadius, DashDamage);
                            _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.Sheep, 2,
                                _legOrigin, StompRadius * 2f, .35f));
                        }
                    }
                    break;
                case DashPhase.Dash:
                    if (Variant == SheepMode.Stomp)
                    {
                        if (_elapsed >= StompSeconds) BeginRecovery();
                    }
                    else AdvanceDash(step);
                    break;
                case DashPhase.Recover:
                    if (_elapsed >= RecoverySeconds) { Phase = DashPhase.Rest; _elapsed = 0f; }
                    break;
            }
        }

        // Shepherd bells retain the original public straight-charge contract.
        public void CommandDash(Vector3 target)
        {
            if (Actor == null || !CanAttack()) { Cancel(); return; }
            if (Phase == DashPhase.Dash || Phase == DashPhase.Warning || !Finite(target)) return;
            _commandPending = false;
            BeginAttack(SheepMode.Charge, target);
        }

        public void RequestCommand(Vector3 target)
        {
            if (Actor == null || !CanAttack()) { Cancel(); return; }
            if (!Finite(target)) return;
            _commandTarget = target; _commandPending = true;
        }

        private bool CanAttack()
        {
            if (!isActiveAndEnabled || !Actor.isActiveAndEnabled) return false;
            var shade = Actor.OwnerShade;
            if (shade != null && (!shade.isActiveAndEnabled || shade.IsStunned || shade.IsPacified || shade.CurrentHealth <= 0f)) return false;
            var boss = Actor.OwnerBoss;
            if (boss != null && (boss.IsDefeated || boss.CurrentPhase != DuelPhase.ArenaCombat)) return false;
            var player = OrpheusPlayerController.Instance;
            return player == null || (!player.IsGameOver && !player.IsVictory && player.IsIn2DBossStage == Actor.IsBossStage);
        }

        private void BeginAttack(SheepMode variant, Vector3 target)
        {
            ClearWarnings(); ClearEffects();
            Variant = variant; LegIndex = 0; HasHit = false; _contactSpent = false;
            _stageAttack = Actor.IsBossStage;
            _legOrigin = Actor.transform.position;
            Vector3 aim = Flat(target - _legOrigin);
            if (_stageAttack) aim = UnderworldCamera2D.IsoRightXZ * Vector3.Dot(aim, UnderworldCamera2D.IsoRightXZ);
            Vector3 heading = aim.sqrMagnitude > .001f ? aim.normalized : UnderworldCamera2D.IsoRightXZ;
            if (variant == SheepMode.HookCharge)
            {
                // The chamber hook zigzags around its committed aim. On a narrow rail the
                // readable equivalent is an outward dash followed by a separately warned return.
                Vector3 first = _stageAttack ? heading : Quaternion.AngleAxis(32f, Vector3.up) * heading;
                Vector3 second = _stageAttack ? -heading : Quaternion.AngleAxis(-32f, Vector3.up) * heading;
                _legEnds[0] = ConstrainEnd(_legOrigin, first * HookDistance);
                _legEnds[1] = ConstrainEnd(_legEnds[0], second * HookDistance);
                LockedDirection = first;
            }
            else
            {
                LockedDirection = heading;
                _legEnds[0] = variant == SheepMode.Stomp ? _legOrigin : ConstrainEnd(_legOrigin, heading * (DashSpeed * DashSeconds));
                _legEnds[1] = _legEnds[0];
            }
            BeginWarning();
        }

        private void BeginWarning()
        {
            ClearWarnings(); _elapsed = 0f; Phase = DashPhase.Warning;
            float duration = Variant == SheepMode.Stomp ? StompWarningSeconds : LegIndex == 1 ? HookWarningSeconds : WarningSeconds;
            Actor.Face(_legOrigin + LockedDirection); Actor.Art?.Play(EnemyMotion.Windup, duration);
            if (Variant == SheepMode.Stomp)
            {
                _warnings.Add(PaintedAttackShadow.Create(_legOrigin, StompRadius, duration));
                return;
            }
            Vector3 delta = _legEnds[LegIndex] - _legOrigin;
            int disks = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .5f));
            // Slightly enlarged overlapping disks contain the whole swept hit capsule,
            // including its side edges between disk centers, not merely the center line.
            for (int i = 0; i <= disks; i++)
                _warnings.Add(PaintedAttackShadow.Create(_legOrigin + delta * (i / (float)disks), HitRadius + .12f, duration));
        }

        private Vector3 ConstrainEnd(Vector3 origin, Vector3 delta)
        {
            if (_stageAttack)
            {
                Vector3 end = origin + delta;
                float along = Vector3.Dot(end - Actor.StageCenter, UnderworldCamera2D.IsoRightXZ);
                return end + UnderworldCamera2D.IsoRightXZ * (Mathf.Clamp(along, -9.5f, 7.2f) - along);
            }
            Vector3 at = origin;
            int slices = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / MovementSlice));
            for (int i = 1; i <= slices; i++)
            {
                Vector3 next = origin + delta * (i / (float)slices);
                if (!ClearMovement(at, next)) break;
                at = next;
            }
            return at;
        }

        private bool ClearMovement(Vector3 from, Vector3 to)
        {
            if (_stageAttack) return true;
            if (ChamberCollision.SweepObstacle(from + Vector3.up * .5f, to - from, .4f, out _)) return false;
            var map = PaintedChamberMap.Active;
            return map == null || map.IsWalkable(to, .4f);
        }

        private void AdvanceDash(float step)
        {
            CheckContact();
            int slices = Mathf.Max(1, Mathf.CeilToInt(DashSpeed * step / MovementSlice));
            for (int i = 0; i < slices; i++)
            {
                Vector3 before = Actor.transform.position;
                Vector3 endpoint = _legEnds[LegIndex];
                Vector3 next = Vector3.MoveTowards(before, endpoint, DashSpeed * step / slices);
                if (!ClearMovement(before, next)) { BeginRecovery(); return; }
                Actor.MoveToward(next, DashSpeed, step / slices);
                CheckContact();
                if (Flat(Actor.transform.position - endpoint).sqrMagnitude < .004f)
                {
                    if (Variant == SheepMode.HookCharge && LegIndex == 0)
                    {
                        LegIndex = 1; _legOrigin = Actor.transform.position;
                        Vector3 nextHeading = Flat(_legEnds[1] - _legOrigin);
                        if (nextHeading.sqrMagnitude < .01f) { BeginRecovery(); return; }
                        LockedDirection = nextHeading.normalized;
                        BeginWarning();
                    }
                    else BeginRecovery();
                    return;
                }
                if (Flat(Actor.transform.position - before).sqrMagnitude < .00001f)
                { BeginRecovery(); return; }
            }
            // Movement stays finite if external collision or stage constraints change mid-dash.
            if (_elapsed >= DashSeconds + .1f) BeginRecovery();
        }

        private void CheckContact()
        {
            if (_contactSpent) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null) return;
            Vector3 at = Actor.transform.position, gap = Flat(player.transform.position - at);
            if (gap.sqrMagnitude > HitRadius * HitRadius) return;
            if (!_stageAttack && ChamberCollision.SweepObstacle(at + Vector3.up * .65f, gap, .08f, out _)) return;
            // A dash spends contact even during i-frames: ending a dodge cannot create
            // delayed damage, and the hook's return never damages twice in one attack.
            _contactSpent = true;
            HasHit = Actor.TryHitPlayer(at, HitRadius, DashDamage);
            if (HasHit) _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.Sheep, 2, at, 1.1f, .35f));
        }

        private void BeginRecovery()
        {
            ClearWarnings(); Phase = DashPhase.Recover; _elapsed = 0f;
            Actor.Art?.Play(EnemyMotion.Recover, RecoverySeconds);
        }

        public override void Cancel()
        {
            ClearWarnings(); ClearEffects();
            Phase = DashPhase.Rest; _elapsed = 0f; HasHit = false; LockedDirection = Vector3.zero;
            _commandPending = false; _contactSpent = false; _commandTarget = Vector3.zero;
            LegIndex = 0; _legOrigin = Vector3.zero; _legEnds[0] = _legEnds[1] = Vector3.zero;
        }

        private void ClearWarnings()
        {
            foreach (var warning in _warnings)
                if (warning != null) { warning.gameObject.SetActive(false); warning.Dispose(); }
            _warnings.Clear();
        }

        private void ClearEffects()
        {
            foreach (var effect in _effects)
                if (effect != null)
                {
                    effect.SetActive(false);
                    if (Application.isPlaying) Destroy(effect); else DestroyImmediate(effect);
                }
            _effects.Clear();
        }

        private static Vector3 Flat(Vector3 value) { value.y = 0f; return value; }
        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
