using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>A warned, committed dash at the living companion followed by an interruptible retreat.</summary>
    public sealed class RoosterRiderAttack : PaintedAttackPattern
    {
        public const float WarningSeconds = .65f;
        public const float DashSeconds = .5f;
        public const float DashSpeed = 13f;
        public const float GrabRadius = 1.38f;
        public const float RetreatSpeed = 1.55f;
        public const float RecoverySeconds = .9f;
        private const float SeekSpeed = 3.8f;
        private const float DashRange = DashSpeed * DashSeconds;
        private enum RiderState { Seek, Windup, Dash, Drag, Recover }
        private RiderState _state;
        private float _clock, _dustClock, _targetScan;
        private Vector3 _dashDirection;
        private bool _registered, _grabAttempted, _moving;
        private BlurredEurydice _target;
        private readonly List<PaintedAttackShadow> _warnings = new List<PaintedAttackShadow>();
        private readonly List<GameObject> _effects = new List<GameObject>();

        public bool IsDragging => _registered && _state == RiderState.Drag;
        public BlurredEurydice DragTarget => IsDragging ? _target : null;
        public Vector3 RetreatDirection { get; private set; }

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
            Actor.Art?.Play(EnemyMotion.Idle, .8f, true);
        }

        public override void Tick(float dt)
        {
            if (Actor == null || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (!UnderworldJourney.HasEurydice || !UnderworldJourney.IsAscending || Actor.IsBossStage ||
                Actor.OwnerShade == null || Actor.OwnerShade.CurrentHealth <= 0 || Actor.OwnerShade.IsPacified)
            {
                Cancel();
                return;
            }
            _targetScan -= dt;
            if (_target == null && _targetScan <= 0)
            {
                _targetScan = .4f;
                _target = Object.FindFirstObjectByType<BlurredEurydice>();
            }
            if (_target == null || _target.IsVanished || _target.HasSanctuaryShield)
            {
                Cancel();
                Actor.Art?.Play(EnemyMotion.Idle, .8f, true);
                return;
            }

            // Long frames may lengthen the windup, never erase its readable warning.
            float step = Mathf.Min(dt, .1f);
            _clock += step;
            _effects.RemoveAll(effect => effect == null);
            Vector3 position = Actor.transform.position;
            Vector3 toTarget = Flat(_target.transform.position - position);
            switch (_state)
            {
                case RiderState.Seek:
                    if (toTarget.magnitude <= DashRange + .45f &&
                        !ChamberCollision.SweepObstacle(position + Vector3.up * .5f, toTarget, .4f, out _))
                        BeginWindup(toTarget);
                    else
                    {
                        SetMoving(true);
                        Actor.Face(_target.transform.position);
                        Actor.MoveToward(_target.transform.position, SeekSpeed, step,navigate:true);
                    }
                    break;
                case RiderState.Windup:
                    foreach (var warning in _warnings) warning?.Progress(_clock / WarningSeconds);
                    if (_clock >= WarningSeconds)
                    {
                        ClearWarnings();
                        _state = RiderState.Dash;
                        _clock = 0;
                        Actor.Art?.Play(EnemyMotion.Attack, DashSeconds);
                        TrackEffect(0, position, 2.6f, DashSeconds);
                    }
                    break;
                case RiderState.Dash:
                    Actor.MoveToward(position + _dashDirection * DashRange, DashSpeed, step);
                    if (TryGrab()) break;
                    // A collision ends the committed dash rather than allowing it to slide through cover.
                    if (_clock >= DashSeconds || Flat(Actor.transform.position - position).sqrMagnitude < .00001f)
                        BeginRecovery();
                    break;
                case RiderState.Drag:
                    if (!_target.IsBeingAbducted || toTarget.magnitude > 1.9f)
                    {
                        Release();
                        BeginRecovery();
                        break;
                    }
                    var map = PaintedChamberMap.Active;
                    if (map == null || map.definition == null)
                    {
                        Release();
                        BeginRecovery();
                        break;
                    }
                    Vector3 goal = map.ToWorld(map.definition.exit);
                    Actor.MoveToward(goal, RetreatSpeed, step,navigate:true);
                    RetreatDirection = Flat(Actor.transform.position-position).normalized;
                    Actor.Face(position + RetreatDirection);
                    _dustClock -= step;
                    if (_dustClock <= 0)
                    {
                        _dustClock = .4f;
                        TrackEffect(3, position, 1.5f, .45f, true);
                        TrackEffect(1, (position + _target.transform.position) * .5f + Vector3.up * .65f, 1.7f, .5f);
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

        private void BeginWindup(Vector3 toTarget)
        {
            _dashDirection = toTarget.sqrMagnitude > .001f ? toTarget.normalized : UnderworldCamera2D.IsoRightXZ;
            _grabAttempted = false;
            _state = RiderState.Windup;
            _clock = 0;
            _moving = false;
            Actor.Face(Actor.transform.position + _dashDirection);
            Actor.Art?.Play(EnemyMotion.Windup, WarningSeconds);
            _target.NotifyThreat(WarningSeconds + DashSeconds);
            // A chain of overlapping disks covers the exact committed corridor; it never follows Eurydice.
            Vector3 start = Actor.transform.position;
            float length = DashRange;
            if (ChamberCollision.SweepObstacle(start + Vector3.up * .5f, _dashDirection * length, .4f, out var hit))
                length = Mathf.Max(0, hit.distance - .025f);
            for (float distance = 0; distance <= length; distance += .65f)
                _warnings.Add(PaintedAttackShadow.Create(start + _dashDirection * distance, GrabRadius, WarningSeconds));
            _warnings.Add(PaintedAttackShadow.Create(start + _dashDirection * length, GrabRadius, WarningSeconds));
        }

        private bool TryGrab()
        {
            if (_grabAttempted) return false;
            Vector3 gap = Flat(_target.transform.position - Actor.transform.position);
            if (gap.magnitude > GrabRadius) return false;
            if (ChamberCollision.SweepObstacle(Actor.transform.position + Vector3.up * .65f, gap, .18f, out _)) return false;
            _grabAttempted = true;
            _target.RegisterAbductor(Actor.OwnerShade);
            if (!_target.IsBeingAbducted || _target.HasSanctuaryShield) return false;
            _registered = true;
            _state = RiderState.Drag;
            _clock = 0;
            _dustClock = 0;
            var map = PaintedChamberMap.Active;
            RetreatDirection = map != null && map.definition != null
                ? Flat(map.ToWorld(map.definition.exit) - Actor.transform.position).normalized : Vector3.zero;
            Actor.Art?.Play(EnemyMotion.Attack2, .7f, true);
            TrackEffect(2, Actor.transform.position, 1.8f, .4f);
            return true;
        }

        private void BeginRecovery()
        {
            ClearWarnings();
            _state = RiderState.Recover;
            _clock = 0;
            Actor.Art?.Play(EnemyMotion.Recover, RecoverySeconds);
        }

        private void SetMoving(bool moving)
        {
            if (_moving == moving) return;
            _moving = moving;
            Actor.Art?.Play(moving ? EnemyMotion.Move : EnemyMotion.Idle, .7f, true);
        }

        private void TrackEffect(int row, Vector3 at, float size, float duration, bool ground = false)
        {
            var effect = EnemyPaintedEffects.Spawn(PaintedEnemyKind.RoosterRider, row, at, size, duration, ground);
            if (effect != null) _effects.Add(effect);
        }

        private void Release()
        {
            if (_registered && _target != null && Actor != null) _target.UnregisterAbductor(Actor.OwnerShade);
            _registered = false;
            RetreatDirection = Vector3.zero;
        }

        private void ClearWarnings()
        {
            foreach (var warning in _warnings) warning?.Dispose();
            _warnings.Clear();
        }

        public override void Cancel()
        {
            Release();
            ClearWarnings();
            foreach (var effect in _effects)
                if (effect != null)
                {
                    if (Application.isPlaying) Destroy(effect); else DestroyImmediate(effect);
                }
            _effects.Clear();
            _state = RiderState.Seek;
            _clock = _dustClock = 0;
            _dashDirection = Vector3.zero;
            _grabAttempted = _moving = false;
        }

        private static Vector3 Flat(Vector3 vector) { vector.y = 0; return vector; }
    }
}
