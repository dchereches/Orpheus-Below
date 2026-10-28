using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Committed kidnapping lanes, a two-beat hook, and a kick that opens a rescue window.</summary>
    public sealed class RoosterRiderAttack : PaintedAttackPattern
    {
        public const float WarningSeconds = .46f;
        public const float DashSeconds = .4f;
        public const float DashSpeed = 15f;
        public const float GrabRadius = 1.38f;
        public const float RetreatSpeed = 1.55f;
        public const float RecoverySeconds = .55f;
        private const float SeekSpeed = 5.2f;
        private const float DashRange = DashSpeed * DashSeconds;
        private const float FeintSeconds = .18f;
        private const float HookWarningSeconds = .32f;
        private const float HookDashSeconds = .3f;
        private const float KickWarningSeconds = .44f;
        private const float KickRadius = 1.6f;
        private const float KickReach = .85f;
        private const float KickDamage = 18f;
        private const float DashDamage = 12f;
        private const float KickTriggerRange = 2.6f;
        private const int MaximumWarnings = 32;
        private const int MaximumEffects = 12;
        public enum RiderAttack { GrabDash, HookDash, WingKick }
        private enum RiderState { Seek, Windup, Dash, HookWindup, KickWindup, Drag, Recover }
        private RiderState _state;
        private float _clock, _dustClock, _targetScan;
        private Vector3 _dashDirection, _hookDirection, _kickOrigin;
        private bool _registered, _grabAttempted, _moving, _hookSecondLeg, _playerContactSpent;
        private int _attackIndex;
        private BlurredEurydice _target;
        private readonly List<PaintedAttackShadow> _warnings = new List<PaintedAttackShadow>();
        private readonly List<GameObject> _effects = new List<GameObject>();
        private readonly List<float> _effectRemaining = new List<float>();

        public bool IsDragging => _registered && _state == RiderState.Drag;
        public BlurredEurydice DragTarget => IsDragging ? _target : null;
        public Vector3 RetreatDirection { get; private set; }
        public RiderAttack SelectedAttack { get; private set; }
        public Vector3 CommittedDirection => _dashDirection;
        public bool IsWarning => _state == RiderState.Windup || _state == RiderState.HookWindup || _state == RiderState.KickWindup;
        public bool IsHookWarning => _state == RiderState.HookWindup;
        public bool IsRecovering => _state == RiderState.Recover;

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
            _attackIndex = 0;
            Actor.Art?.Play(EnemyMotion.Idle, .8f, true);
        }

        public override void Tick(float dt)
        {
            if (Actor == null) return;
            var player = OrpheusPlayerController.Instance;
            if (!isActiveAndEnabled || !Actor.isActiveAndEnabled ||
                !UnderworldJourney.HasEurydice || !UnderworldJourney.IsAscending || Actor.IsBossStage ||
                Actor.OwnerShade == null || !Actor.OwnerShade.isActiveAndEnabled || Actor.OwnerShade.CurrentHealth <= 0 ||
                Actor.OwnerShade.IsPacified || Actor.OwnerShade.IsStunned ||
                (player != null && (player.IsGameOver || player.IsVictory || player.IsIn2DBossStage)))
            {
                Cancel();
                return;
            }
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt) || Time.timeScale <= 0f ||
                UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            float step = Mathf.Min(dt, .1f);
            TickEffects(step);
            _targetScan -= step;
            if (_target == null && _targetScan <= 0)
            {
                _targetScan = .25f;
                _target = Object.FindFirstObjectByType<BlurredEurydice>();
            }
            if (_target == null || !_target.isActiveAndEnabled || _target.IsVanished || _target.HasSanctuaryShield)
            {
                Cancel();
                Actor.Art?.Play(EnemyMotion.Idle, .8f, true);
                return;
            }

            // One transition per tick preserves every warning, even on a very long frame.
            _clock += step;
            Vector3 position = Actor.transform.position;
            Vector3 toTarget = Flat(_target.transform.position - position);
            switch (_state)
            {
                case RiderState.Seek:
                    var next = (RiderAttack)(_attackIndex % 3);
                    if (next == RiderAttack.WingKick && player != null)
                    {
                        Vector3 gap = Flat(player.transform.position - position);
                        if (gap.magnitude <= KickTriggerRange && ClearLane(position, gap, .15f))
                        {
                            _attackIndex++;
                            BeginKick(gap);
                        }
                        else Seek(player.transform.position, step);
                    }
                    else if (toTarget.magnitude <= DashRange + .45f && ClearLane(position, toTarget, .4f))
                    {
                        _attackIndex++;
                        BeginWindup(toTarget, next == RiderAttack.HookDash);
                    }
                    else Seek(_target.transform.position, step);
                    break;
                case RiderState.Windup:
                case RiderState.HookWindup:
                    float warningSeconds = _state == RiderState.HookWindup ? HookWarningSeconds : WarningSeconds;
                    ProgressWarnings(_clock / warningSeconds);
                    if (_clock >= warningSeconds)
                    {
                        ClearWarnings();
                        _state = RiderState.Dash;
                        _clock = 0;
                        Actor.Art?.Play(EnemyMotion.Attack, CurrentDashSeconds);
                        TrackEffect(0, position, 2.6f, CurrentDashSeconds);
                    }
                    break;
                case RiderState.Dash:
                    float dashStep = Mathf.Min(step, Mathf.Max(0f, CurrentDashSeconds - (_clock - step)));
                    bool blocked = !MoveCommitted(_dashDirection, DashSpeed * dashStep);
                    Vector3 end = Actor.transform.position;
                    HitDashLane(position, end);
                    if (player != null && (player.IsGameOver || player.IsVictory)) { Cancel(); return; }
                    if ((SelectedAttack != RiderAttack.HookDash || _hookSecondLeg) && TryGrab(position, end)) break;
                    if (blocked) BeginRecovery();
                    else if (_clock >= CurrentDashSeconds)
                    {
                        if (SelectedAttack == RiderAttack.HookDash && !_hookSecondLeg) BeginHookWarning();
                        else BeginRecovery();
                    }
                    break;
                case RiderState.KickWindup:
                    ProgressWarnings(_clock / KickWarningSeconds);
                    if (_clock >= KickWarningSeconds)
                    {
                        ClearWarnings();
                        // Cover is checked from the rider, so a forward disk cannot hit through a wall.
                        if (player != null)
                        {
                            Vector3 gap = Flat(player.transform.position - _kickOrigin);
                            Vector3 center = _kickOrigin + _dashDirection * KickReach;
                            if (Vector3.Dot(gap, _dashDirection) >= 0f && ClearLane(_kickOrigin, gap, .08f))
                                Actor.TryHitPlayer(center, KickRadius, KickDamage);
                        }
                        TrackEffect(2, _kickOrigin + _dashDirection * KickReach, 2.7f, .3f);
                        if (player != null && (player.IsGameOver || player.IsVictory)) { Cancel(); return; }
                        BeginRecovery();
                    }
                    break;
                case RiderState.Drag:
                    if (!_target.IsBeingAbducted || toTarget.magnitude > 1.9f)
                    {
                        Release(); BeginRecovery(); break;
                    }
                    // Defending the catch deliberately drops it first. The rescuer can sidestep the
                    // warned kick and use the full recovery before another kidnapping can begin.
                    if (player != null)
                    {
                        Vector3 gap = Flat(player.transform.position - position);
                        if (gap.magnitude <= KickTriggerRange && ClearLane(position, gap, .15f))
                        {
                            BeginKick(gap); break;
                        }
                    }
                    var map = PaintedChamberMap.Active;
                    if (map == null || map.definition == null) { Release(); BeginRecovery(); break; }
                    Vector3 goal = map.ToWorld(map.definition.exit);
                    // Navigation chooses the route; small swept steps also prevent crossing thin
                    // unwalkable gaps or cover while escorting the registered companion.
                    for (float left = step; left > .0001f; left -= .04f)
                        Actor.MoveToward(goal, RetreatSpeed, Mathf.Min(.04f, left), navigate: true);
                    RetreatDirection = Flat(Actor.transform.position - position).normalized;
                    Actor.Face(position + RetreatDirection);
                    _dustClock -= step;
                    if (_dustClock <= 0)
                    {
                        _dustClock = .32f;
                        TrackEffect(3, position, 1.5f, .4f, true);
                        TrackEffect(1, (position + _target.transform.position) * .5f + Vector3.up * .65f, 1.7f, .4f);
                    }
                    break;
                case RiderState.Recover:
                    if (_clock >= RecoverySeconds)
                    {
                        _state = RiderState.Seek;
                        _clock = 0;
                        _moving = false;
                        Actor.Art?.Play(EnemyMotion.Idle, .8f, true);
                    }
                    break;
            }
        }

        private float CurrentDashSeconds => SelectedAttack == RiderAttack.HookDash
            ? (_hookSecondLeg ? HookDashSeconds : FeintSeconds) : DashSeconds;

        private void Seek(Vector3 goal, float step)
        {
            if (!_moving) { _moving = true; Actor.Art?.Play(EnemyMotion.Move, .55f, true); }
            Actor.Face(goal);
            for (float left = step; left > .0001f; left -= .04f)
                Actor.MoveToward(goal, SeekSpeed, Mathf.Min(.04f, left), navigate: true);
        }

        private void BeginWindup(Vector3 toTarget, bool hook)
        {
            ClearWarnings(); ClearEffects();
            SelectedAttack = hook ? RiderAttack.HookDash : RiderAttack.GrabDash;
            _dashDirection = Direction(toTarget);
            _hookSecondLeg = false;
            if (hook)
            {
                // Both legs are aimed now: the delayed hook never follows a dodging companion.
                _dashDirection = Quaternion.Euler(0, (_attackIndex % 2 == 0 ? 1 : -1) * 38f, 0) * _dashDirection;
                Vector3 predictedEnd = Actor.transform.position + _dashDirection * (DashSpeed * FeintSeconds);
                _hookDirection = Direction(_target.transform.position - predictedEnd);
            }
            _grabAttempted = _playerContactSpent = false;
            _state = RiderState.Windup;
            _clock = 0;
            _moving = false;
            Actor.Face(Actor.transform.position + _dashDirection);
            Actor.Art?.Play(EnemyMotion.Windup, WarningSeconds);
            _target.NotifyThreat(WarningSeconds + CurrentDashSeconds + (hook ? HookWarningSeconds + HookDashSeconds : 0f));
            WarnLane(Actor.transform.position, _dashDirection, DashSpeed * CurrentDashSeconds, WarningSeconds);
        }

        private void BeginHookWarning()
        {
            _hookSecondLeg = true;
            _dashDirection = _hookDirection;
            _state = RiderState.HookWindup;
            _clock = 0f;
            Actor.Face(Actor.transform.position + _dashDirection);
            Actor.Art?.Play(EnemyMotion.Windup, HookWarningSeconds);
            WarnLane(Actor.transform.position, _dashDirection, DashSpeed * HookDashSeconds, HookWarningSeconds);
        }

        private void BeginKick(Vector3 aim)
        {
            Release(); ClearWarnings(); ClearEffects();
            SelectedAttack = RiderAttack.WingKick;
            _dashDirection = Direction(aim);
            _kickOrigin = Actor.transform.position;
            _state = RiderState.KickWindup;
            _clock = 0f;
            _moving = false;
            Actor.Face(_kickOrigin + _dashDirection);
            Actor.Art?.Play(EnemyMotion.Windup, KickWarningSeconds);
            _warnings.Add(PaintedAttackShadow.Create(_kickOrigin + _dashDirection * KickReach, KickRadius, KickWarningSeconds));
            TrackEffect(1, _kickOrigin + Vector3.up * .65f, 1.5f, KickWarningSeconds);
        }

        private void WarnLane(Vector3 origin, Vector3 direction, float length, float duration)
        {
            float safeLength = 0f;
            for (float at = 0f; at <= length; at += .15f)
            {
                if (!ClearLane(origin, direction * at, .4f)) break;
                safeLength = at;
            }
            for (float distance = 0; distance <= safeLength && _warnings.Count < MaximumWarnings - 1; distance += .65f)
                _warnings.Add(PaintedAttackShadow.Create(origin + direction * distance, GrabRadius, duration));
            if (_warnings.Count < MaximumWarnings)
                _warnings.Add(PaintedAttackShadow.Create(origin + direction * safeLength, GrabRadius, duration));
        }

        private bool MoveCommitted(Vector3 direction, float distance)
        {
            // Endpoint-only walkability can tunnel over a narrow lava strip. Check the entire route.
            while (distance > .0001f)
            {
                float part = Mathf.Min(.15f, distance);
                Vector3 before = Actor.transform.position;
                Vector3 delta = direction * part;
                if (!ClearLane(before, delta, .4f)) return false;
                Actor.MoveToward(before + delta, DashSpeed, part / DashSpeed);
                if (Flat(Actor.transform.position - before).sqrMagnitude < part * part * .5f) return false;
                distance -= part;
            }
            return true;
        }

        private bool ClearLane(Vector3 origin, Vector3 displacement, float radius)
        {
            if (ChamberCollision.SweepObstacle(origin + Vector3.up * .5f, displacement, radius, out _)) return false;
            foreach (var collider in Physics.OverlapSphere(origin + Vector3.up * .5f, radius, ~0, QueryTriggerInteraction.Ignore))
                if (collider.GetComponent<IsoObstacle>() != null) return false;
            var map = PaintedChamberMap.Active;
            if (map == null || map.definition == null) return true;
            int samples = Mathf.Max(1, Mathf.CeilToInt(displacement.magnitude / .15f));
            for (int i = 0; i <= samples; i++)
                if (!map.IsWalkable(origin + displacement * (i / (float)samples), radius)) return false;
            return true;
        }

        private void HitDashLane(Vector3 start, Vector3 end)
        {
            var player = OrpheusPlayerController.Instance;
            if (_playerContactSpent || player == null) return;
            Vector3 contact = ClosestPoint(start, end, player.transform.position);
            if (Flat(player.transform.position - contact).magnitude > .9f) return;
            if (!ClearLane(contact, Flat(player.transform.position - contact), .08f)) return;
            // A dash through the rider consumes this contact even though its damage is immune.
            _playerContactSpent = true;
            Actor.TryHitPlayer(contact, .9f, DashDamage);
        }

        private bool TryGrab(Vector3 start, Vector3 end)
        {
            if (_grabAttempted || Actor.OwnerShade.IsStunned) return false;
            Vector3 closest = ClosestPoint(start, end, _target.transform.position);
            Vector3 gap = Flat(_target.transform.position - closest);
            if (gap.magnitude > GrabRadius || !ClearLane(closest, gap, .18f)) return false;
            // Keep the companion in its physical tether range even after a swept contact.
            if (Flat(_target.transform.position - Actor.transform.position).magnitude > 1.9f) return false;
            _grabAttempted = true;
            _target.RegisterAbductor(Actor.OwnerShade);
            if (!_target.IsBeingAbducted || _target.HasSanctuaryShield) return false;
            _registered = true;
            _state = RiderState.Drag;
            _clock = _dustClock = 0;
            var map = PaintedChamberMap.Active;
            RetreatDirection = map != null && map.definition != null
                ? Flat(map.ToWorld(map.definition.exit) - Actor.transform.position).normalized : Vector3.zero;
            Actor.Art?.Play(EnemyMotion.Attack2, .55f, true);
            TrackEffect(2, Actor.transform.position, 1.8f, .35f);
            return true;
        }

        private void BeginRecovery()
        {
            Release(); ClearWarnings();
            _state = RiderState.Recover;
            _clock = 0;
            _hookSecondLeg = false;
            Actor.Art?.Play(EnemyMotion.Recover, RecoverySeconds);
        }

        private void TrackEffect(int row, Vector3 at, float size, float duration, bool ground = false)
        {
            var effect = EnemyPaintedEffects.Spawn(PaintedEnemyKind.RoosterRider, row, at, size, duration, ground);
            if (effect == null) return;
            if (_effects.Count >= MaximumEffects) RemoveEffect(0);
            _effects.Add(effect); _effectRemaining.Add(duration);
        }

        private void TickEffects(float dt)
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                _effectRemaining[i] -= dt;
                if (_effects[i] == null || _effectRemaining[i] <= 0f) RemoveEffect(i);
            }
        }

        private void RemoveEffect(int index)
        {
            var effect = _effects[index];
            if (effect != null)
            {
                effect.SetActive(false);
                if (Application.isPlaying) Destroy(effect); else DestroyImmediate(effect);
            }
            _effects.RemoveAt(index); _effectRemaining.RemoveAt(index);
        }

        private void ClearEffects()
        {
            for (int i = _effects.Count - 1; i >= 0; i--) RemoveEffect(i);
        }

        private void Release()
        {
            if (_registered && _target != null && Actor != null) _target.UnregisterAbductor(Actor.OwnerShade);
            _registered = false;
            RetreatDirection = Vector3.zero;
        }

        private void ProgressWarnings(float progress)
        {
            foreach (var warning in _warnings) warning?.Progress(progress);
        }

        private void ClearWarnings()
        {
            foreach (var warning in _warnings)
                if (warning != null) { warning.gameObject.SetActive(false); warning.Dispose(); }
            _warnings.Clear();
        }

        public override void Cancel()
        {
            Release(); ClearWarnings(); ClearEffects();
            _state = RiderState.Seek;
            _clock = _dustClock = 0;
            _dashDirection = _hookDirection = _kickOrigin = Vector3.zero;
            _grabAttempted = _moving = _hookSecondLeg = _playerContactSpent = false;
        }

        private static Vector3 ClosestPoint(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector3 lane = Flat(end - start);
            float t = Mathf.Clamp01(Vector3.Dot(Flat(point - start), lane) / Mathf.Max(.00001f, lane.sqrMagnitude));
            return start + lane * t;
        }
        private static Vector3 Direction(Vector3 vector) => Flat(vector).sqrMagnitude > .001f ? Flat(vector).normalized : UnderworldCamera2D.IsoRightXZ;
        private static Vector3 Flat(Vector3 vector) { vector.y = 0; return vector; }
    }
}
