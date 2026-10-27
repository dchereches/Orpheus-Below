using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Commits its aim before throwing its real painted staff, then recalls it without damage.</summary>
    public sealed class MysticEyeWardenAttack : PaintedAttackPattern
    {
        public const float WaitSeconds = 1.5f;
        public const float WindupSeconds = 1.05f;
        public const float ThrowSeconds = .28f;
        public const float OutboundSeconds = .85f;
        public const float RecallSeconds = .65f;
        public const float RecoverySeconds = .9f;
        public const float StaffSpeed = 12f;
        public const float StaffDamage = 20f;
        public const float StaffRadius = .32f;

        public enum CastPhase { Wait, Windup, Throw, Outbound, Recall, Recover }
        public CastPhase Phase { get; private set; } = CastPhase.Wait;
        public float PhaseElapsed => _clock;
        public float PhaseRemaining => Mathf.Max(0f, Duration(Phase) - _clock);
        public Vector3 LockedAimPoint { get; private set; }
        public Vector3 LockedDirection { get; private set; }
        public bool HasPendingThrow => Phase == CastPhase.Windup;
        public bool HasActiveStaff => _staff != null && !_staff.Removed;
        public int ThrowCount { get; private set; }

        private float _clock;
        private PaintedEnemyProjectile _staff;
        private GameObject _charge;
        private GameObject _recall;
        private Vector3 _lastStaffPoint;
        private Vector3 _recallStart;

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
            ThrowCount = 0;
            SetPhase(CastPhase.Wait);
        }

        public override void Tick(float dt)
        {
            if (Actor == null || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt) || Time.timeScale <= 0f) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated ||
                    Actor.OwnerBoss.CurrentPhase != DuelPhase.ArenaCombat)))
            {
                Cancel();
                return;
            }

            // Long frames extend the warning rather than consuming several attack phases at once.
            _clock += Mathf.Min(dt, .1f);
            if (_staff != null && !_staff.Removed) _lastStaffPoint = _staff.transform.position;
            if (Phase == CastPhase.Wait)
            {
                Actor.Face(player.transform.position);
                if (_clock >= WaitSeconds) LockAim(player.transform.position);
            }
            else if (Phase == CastPhase.Windup)
            {
                Actor.Face(Actor.GroundAnchor + LockedDirection);
                if (_clock >= WindupSeconds) ThrowStaff();
            }
            else if (Phase == CastPhase.Throw)
            {
                if (_clock >= ThrowSeconds) SetPhase(CastPhase.Outbound);
            }
            else if (Phase == CastPhase.Outbound)
            {
                if (_clock >= OutboundSeconds || !HasActiveStaff) BeginRecall();
            }
            else if (Phase == CastPhase.Recall)
            {
                // Recall is only a painted visual. It cannot create an untelegraphed return hit.
                if (_recall != null)
                    _recall.transform.position = Vector3.Lerp(_recallStart, HandPoint(),
                        Mathf.SmoothStep(0f, 1f, _clock / RecallSeconds));
                if (_clock >= RecallSeconds)
                {
                    ClearVisual(ref _recall);
                    SetPhase(CastPhase.Recover);
                }
            }
            else if (_clock >= RecoverySeconds) SetPhase(CastPhase.Wait);
        }

        private void LockAim(Vector3 point)
        {
            LockedAimPoint = point;
            var direction = point - Actor.GroundAnchor;
            direction.y = 0f;
            LockedDirection = direction.sqrMagnitude > .001f ? direction.normalized : UnderworldCamera2D.IsoRightXZ;
            Actor.Face(Actor.GroundAnchor + LockedDirection);
            SetPhase(CastPhase.Windup);
            _charge = EnemyPaintedEffects.Spawn(PaintedEnemyKind.MysticEyeWarden, 3,
                HandPoint(), 1.2f, WindupSeconds);
        }

        private Vector3 HandPoint() => Actor.GroundAnchor + Vector3.up * .8f + LockedDirection * .55f;

        private void ThrowStaff()
        {
            ClearVisual(ref _charge);
            _lastStaffPoint = HandPoint();
            // The shared projectile sweeps against cover and player colliders, respecting dash immunity.
            _staff = PaintedEnemyProjectile.Spawn(PaintedEnemyKind.MysticEyeWarden, 0,
                _lastStaffPoint, LockedDirection, StaffSpeed, StaffDamage, StaffRadius,
                ThrowSeconds + OutboundSeconds + .2f);
            _staff.Owner = Actor;
            ThrowCount++;
            SetPhase(CastPhase.Throw);
        }

        private void BeginRecall()
        {
            if (_staff != null)
            {
                if (!_staff.Removed) _lastStaffPoint = _staff.transform.position;
                _staff.Remove();
                _staff = null;
            }
            _recallStart = _lastStaffPoint;
            _recall = EnemyPaintedEffects.Spawn(PaintedEnemyKind.MysticEyeWarden, 1,
                _recallStart, 2.5f, RecallSeconds + .1f);
            SetPhase(CastPhase.Recall);
        }

        private void SetPhase(CastPhase phase)
        {
            Phase = phase;
            _clock = 0f;
            // Keep the empty-handed throw follow-through while the staff is in flight.
            if (Actor == null || phase == CastPhase.Outbound) return;
            EnemyMotion motion = phase == CastPhase.Windup ? EnemyMotion.Windup :
                phase == CastPhase.Throw ? EnemyMotion.Attack :
                phase == CastPhase.Recall ? EnemyMotion.Attack2 :
                phase == CastPhase.Recover ? EnemyMotion.Recover : EnemyMotion.Idle;
            Actor.Art?.Play(motion, Duration(phase), phase == CastPhase.Wait);
        }

        public static float Duration(CastPhase phase)
        {
            switch (phase)
            {
                case CastPhase.Windup: return WindupSeconds;
                case CastPhase.Throw: return ThrowSeconds;
                case CastPhase.Outbound: return OutboundSeconds;
                case CastPhase.Recall: return RecallSeconds;
                case CastPhase.Recover: return RecoverySeconds;
                default: return WaitSeconds;
            }
        }

        public override void Cancel()
        {
            if (_staff != null) { _staff.Remove(); _staff = null; }
            ClearVisual(ref _charge);
            ClearVisual(ref _recall);
            LockedAimPoint = Vector3.zero;
            LockedDirection = Vector3.zero;
            _lastStaffPoint = Vector3.zero;
            _recallStart = Vector3.zero;
            SetPhase(CastPhase.Wait);
        }

        private static void ClearVisual(ref GameObject visual)
        {
            if (visual != null)
            {
                if (Application.isPlaying) Destroy(visual);
                else DestroyImmediate(visual);
            }
            visual = null;
        }
    }
}
