using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Alternates a committed staff throw, a spreading eye volley, and three warned ground eruptions.</summary>
    public sealed class MysticEyeWardenAttack : PaintedAttackPattern
    {
        public const float WaitSeconds = .65f;
        public const float WindupSeconds = .65f;
        public const float ThrowSeconds = .18f;
        public const float OutboundSeconds = .6f;
        public const float RecallSeconds = .48f;
        public const float RecoverySeconds = .35f;
        public const float StaffSpeed = 14f;
        public const float StaffDamage = 30f;
        public const float StaffRadius = .32f;
        public const float FanAngle = 22f;
        public const float FanSpeed = 13f;
        public const float RuneRadius = .95f;
        public const float RuneInterval = .18f;
        public const int MaximumHazards = 3;

        public enum CastPhase { Wait, Windup, Throw, Outbound, Recall, Recover }
        public enum CastVariant { StaffThrow, EyeFan, RuneChain }
        public CastPhase Phase { get; private set; } = CastPhase.Wait;
        public CastVariant CurrentVariant { get; private set; } = CastVariant.StaffThrow;
        public float PhaseElapsed => _clock;
        public float PhaseRemaining => Mathf.Max(0f, CurrentDuration - _clock);
        public Vector3 LockedAimPoint { get; private set; }
        public Vector3 LockedDirection { get; private set; }
        public bool HasPendingThrow => Phase == CastPhase.Windup;
        public bool HasActiveStaff => _staff != null && !_staff.Removed;
        public int ThrowCount { get; private set; }
        public int CastCount { get; private set; }
        public int FanCount { get; private set; }
        public int RuneStrikeCount { get; private set; }
        public int ActiveWarningCount
        {
            get { int count = 0; foreach (var warning in _warnings) if (warning != null) count++; return count; }
        }
        public int ActiveProjectileCount
        {
            get
            {
                int count = HasActiveStaff ? 1 : 0;
                foreach (var bolt in _bolts) if (bolt != null && !bolt.Removed) count++;
                return count;
            }
        }

        private float _clock;
        private int _nextVariant, _runesStruckThisCast;
        private PaintedEnemyProjectile _staff;
        private GameObject _charge;
        private GameObject _recall;
        private Vector3 _lastStaffPoint, _recallStart;
        private readonly List<PaintedEnemyProjectile> _bolts = new List<PaintedEnemyProjectile>(MaximumHazards);
        private readonly List<GameObject> _impacts = new List<GameObject>(MaximumHazards);
        private readonly PaintedAttackShadow[] _warnings = new PaintedAttackShadow[MaximumHazards];
        private readonly Vector3[] _runePoints = new Vector3[MaximumHazards];
        private float CurrentDuration => CurrentVariant == CastVariant.RuneChain && Phase == CastPhase.Throw
            ? RuneInterval * 2f + .08f : Duration(Phase);

        public Vector3 RunePoint(int index) => _runePoints[Mathf.Clamp(index, 0, MaximumHazards - 1)];

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
            ThrowCount = CastCount = FanCount = RuneStrikeCount = _nextVariant = 0;
            CurrentVariant = CastVariant.StaffThrow;
            SetPhase(CastPhase.Wait);
        }

        public override void Tick(float dt)
        {
            if (Actor == null) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated ||
                    Actor.OwnerBoss.CurrentPhase != DuelPhase.ArenaCombat)))
            {
                Cancel();
                return;
            }
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt) || Time.timeScale <= 0f ||
                UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;

            // A hitch never consumes both the committed warning and its release in one frame.
            _clock += Mathf.Min(dt, .1f);
            if (HasActiveStaff) _lastStaffPoint = _staff.transform.position;
            if (Phase == CastPhase.Wait)
            {
                Actor.Face(player.transform.position);
                if (_clock >= WaitSeconds) LockAim(player.transform.position);
            }
            else if (Phase == CastPhase.Windup)
            {
                Actor.Face(Actor.GroundAnchor + LockedDirection);
                foreach (var warning in _warnings) if (warning != null) warning.Progress(_clock / WindupSeconds);
                if (_clock >= WindupSeconds) ReleaseCast();
            }
            else if (Phase == CastPhase.Throw)
            {
                if (CurrentVariant == CastVariant.RuneChain)
                {
                    // Only the first eruption fires at release; the others keep their visible warnings.
                    if (_runesStruckThisCast < MaximumHazards && _clock >= RuneInterval * _runesStruckThisCast)
                        EruptRune(_runesStruckThisCast);
                    if (_clock >= CurrentDuration) BeginRecovery();
                }
                else if (_clock >= ThrowSeconds) SetPhase(CastPhase.Outbound);
            }
            else if (Phase == CastPhase.Outbound)
            {
                if (_clock >= OutboundSeconds || (CurrentVariant == CastVariant.StaffThrow && !HasActiveStaff))
                {
                    if (CurrentVariant == CastVariant.StaffThrow) BeginRecall();
                    else BeginRecovery();
                }
            }
            else if (Phase == CastPhase.Recall)
            {
                // Recall is a harmless painted visual, never a returning damaging projectile.
                if (_recall != null)
                    _recall.transform.position = Vector3.Lerp(_recallStart, HandPoint(),
                        Mathf.SmoothStep(0f, 1f, _clock / RecallSeconds));
                if (_clock >= RecallSeconds)
                {
                    ClearVisual(ref _recall);
                    BeginRecovery();
                }
            }
            else if (_clock >= RecoverySeconds) SetPhase(CastPhase.Wait);
        }

        private void LockAim(Vector3 point)
        {
            ClearImpacts();
            CurrentVariant = (CastVariant)(_nextVariant++ % 3);
            LockedAimPoint = point;
            var direction = point - Actor.GroundAnchor;
            direction.y = 0f;
            LockedDirection = direction.sqrMagnitude > .001f ? direction.normalized : UnderworldCamera2D.IsoRightXZ;
            Actor.Face(Actor.GroundAnchor + LockedDirection);
            _runesStruckThisCast = 0;
            if (CurrentVariant == CastVariant.RuneChain)
            {
                Vector3 center = Actor.GroundAnchor + Vector3.ClampMagnitude(direction, 9f);
                Vector3 across = Actor.IsBossStage ? UnderworldCamera2D.IsoRightXZ : Vector3.Cross(Vector3.up, LockedDirection);
                for (int i = 0; i < MaximumHazards; i++)
                {
                    _runePoints[i] = center + across * ((i - 1) * 1.65f);
                    _warnings[i] = PaintedAttackShadow.Create(_runePoints[i], RuneRadius,
                        WindupSeconds + i * RuneInterval);
                }
            }
            SetPhase(CastPhase.Windup);
            _charge = EnemyPaintedEffects.Spawn(PaintedEnemyKind.MysticEyeWarden, 3,
                HandPoint(), CurrentVariant == CastVariant.EyeFan ? 1.5f : 1.2f, WindupSeconds);
        }

        private Vector3 HandPoint() => Actor.GroundAnchor + Vector3.up * .8f + LockedDirection * .55f;

        private void ReleaseCast()
        {
            ClearVisual(ref _charge);
            CastCount++;
            SetPhase(CastPhase.Throw);
            switch (CurrentVariant)
            {
                case CastVariant.StaffThrow: ThrowStaff(); break;
                case CastVariant.EyeFan: FireFan(); break;
                case CastVariant.RuneChain: EruptRune(0); break;
            }
        }

        private void ThrowStaff()
        {
            _lastStaffPoint = HandPoint();
            // The shared projectile sweeps cover and player colliders and respects dash immunity.
            _staff = PaintedEnemyProjectile.Spawn(PaintedEnemyKind.MysticEyeWarden, 0,
                _lastStaffPoint, LockedDirection, StaffSpeed, StaffDamage, StaffRadius,
                ThrowSeconds + OutboundSeconds + .2f);
            _staff.Owner = Actor;
            ThrowCount++;
        }

        private void FireFan()
        {
            for (int i = -1; i <= 1; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(i * FanAngle, Vector3.up) * LockedDirection;
                var bolt = PaintedEnemyProjectile.Spawn(PaintedEnemyKind.MysticEyeWarden, 2,
                    HandPoint(), direction, FanSpeed, 12f, .22f, ThrowSeconds + OutboundSeconds + .1f);
                bolt.Owner = Actor;
                // Reuse the painted eye impact as a compact magical bolt, never duplicate the held staff.
                var paint = bolt.GetComponentInChildren<EnemyPaintedEffects>();
                if (paint != null) paint.transform.localScale = Vector3.one * .8f;
                _bolts.Add(bolt);
            }
            FanCount++;
        }

        private void EruptRune(int index)
        {
            _warnings[index]?.Dispose();
            _warnings[index] = null;
            var impact = EnemyPaintedEffects.Spawn(PaintedEnemyKind.MysticEyeWarden, 2,
                _runePoints[index] + Vector3.up * .35f, RuneRadius * 2f, .3f);
            if (impact != null) _impacts.Add(impact);
            Actor.TryHitPlayer(_runePoints[index], RuneRadius, 15f);
            _runesStruckThisCast++;
            RuneStrikeCount++;
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

        private void BeginRecovery()
        {
            ClearBolts();
            ClearWarnings();
            SetPhase(CastPhase.Recover);
        }

        private void SetPhase(CastPhase phase)
        {
            Phase = phase;
            _clock = 0f;
            // The throw pose is empty handed until the real staff's harmless recall reaches the hand.
            if (Actor == null || phase == CastPhase.Outbound) return;
            EnemyMotion motion = phase == CastPhase.Windup ? EnemyMotion.Windup :
                phase == CastPhase.Throw ? (CurrentVariant == CastVariant.StaffThrow ? EnemyMotion.Attack : EnemyMotion.Windup) :
                phase == CastPhase.Recall ? EnemyMotion.Attack2 :
                phase == CastPhase.Recover ? EnemyMotion.Recover : EnemyMotion.Idle;
            Actor.Art?.Play(motion, CurrentDuration, phase == CastPhase.Wait);
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
            ClearBolts();
            ClearWarnings();
            ClearImpacts();
            ClearVisual(ref _charge);
            ClearVisual(ref _recall);
            LockedAimPoint = Vector3.zero;
            LockedDirection = Vector3.zero;
            _lastStaffPoint = _recallStart = Vector3.zero;
            _runesStruckThisCast = 0;
            SetPhase(CastPhase.Wait);
        }

        private void ClearBolts()
        {
            foreach (var bolt in _bolts) if (bolt != null) bolt.Remove();
            _bolts.Clear();
        }

        private void ClearWarnings()
        {
            for (int i = 0; i < _warnings.Length; i++)
            {
                if (_warnings[i] != null) _warnings[i].Dispose();
                _warnings[i] = null;
            }
        }

        private void ClearImpacts()
        {
            foreach (var impact in _impacts)
            {
                var visual = impact;
                ClearVisual(ref visual);
            }
            _impacts.Clear();
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
