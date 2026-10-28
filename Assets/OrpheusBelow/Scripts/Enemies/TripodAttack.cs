using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Ivory head rings announce committed sonic pulses across the narrow duel stage.</summary>
    public sealed class TripodAttack : PaintedAttackPattern
    {
        public const float WaitSeconds = .55f;
        public const float WindupSeconds = .85f;
        public const float PulseGapSeconds = .8f;
        public const float FollowThroughSeconds = .22f;
        public const float RecoverySeconds = .6f;
        public const float WaveSpeed = 8f;
        public const float WaveDamage = 30f;
        public const float WaveRadius = .7f;
        public const float WaveHeight = .8f;
        public const float WaveLifetime = 3.5f;
        public const float MaximumReturnDistance = 11f;
        private const float ReturnDashGapSeconds = .65f;

        public enum CastPhase { Wait, Windup, PulseGap, FollowThrough, Recover }
        public enum SonicCall { SinglePulse, DoubleEcho, ReturnChorus }
        public SonicCall Variant { get; private set; }
        public CastPhase Phase { get; private set; } = CastPhase.Wait;
        public float PhaseElapsed => _clock;
        public float PhaseRemaining => Mathf.Max(0f, CurrentPhaseDuration - _clock);
        private float CurrentPhaseDuration => Phase == CastPhase.PulseGap ? CurrentPulseGapSeconds : Duration(Phase);
        public Vector3 LockedDirection { get; private set; }
        public Vector3 ReturnOrigin { get; private set; }
        public int CastCount { get; private set; }
        public int PulseCount { get; private set; }
        public int PulsesInCast { get; private set; }
        public int PlannedPulseCount => Variant == SonicCall.SinglePulse ? 1 : 2;
        public int WarningCount => _warnings.Count;
        public float CurrentPulseGapSeconds { get; private set; } = PulseGapSeconds;
        public bool IsDoublePulse { get; private set; }
        public bool HasPendingPulse => Phase == CastPhase.Windup || Phase == CastPhase.PulseGap;
        public int ActiveWaveCount
        {
            get
            {
                int count = 0;
                foreach (var wave in _waves) if (wave != null && !wave.Removed) count++;
                return count;
            }
        }

        private readonly List<PaintedEnemyProjectile> _waves = new List<PaintedEnemyProjectile>(4);
        private readonly List<PaintedAttackShadow> _warnings = new List<PaintedAttackShadow>(26);
        private float _clock;
        private GameObject _charge;

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
            CastCount = 0;
            PulseCount = 0;
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
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt) ||
                Time.timeScale <= 0f || UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;

            // A stall extends the telegraph instead of silently consuming the warning or pulse gap.
            _clock += Mathf.Min(dt, .1f);
            _waves.RemoveAll(wave => wave == null || wave.Removed);
            if (_charge != null) _charge.transform.position = HeadPoint();
            foreach (var warning in _warnings) if (warning != null) warning.Progress(_clock / CurrentPhaseDuration);
            if (Phase == CastPhase.Wait)
            {
                Actor.Face(player.transform.position);
                if (_clock >= WaitSeconds) BeginWindup(player.transform.position);
            }
            else if (Phase == CastPhase.Windup)
            {
                Actor.Face(Actor.transform.position + LockedDirection);
                if (_clock >= WindupSeconds)
                {
                    ClearCharge();
                    ClearWarnings();
                    EmitPulse(0);
                    if (PlannedPulseCount > 1)
                    {
                        SetPhase(CastPhase.PulseGap);
                        PreparePulseWarning(1);
                    }
                    else SetPhase(CastPhase.FollowThrough);
                }
            }
            else if (Phase == CastPhase.PulseGap)
            {
                if (_clock >= CurrentPulseGapSeconds)
                {
                    ClearWarnings();
                    EmitPulse(1);
                    SetPhase(CastPhase.FollowThrough);
                }
            }
            else if (Phase == CastPhase.FollowThrough)
            {
                if (_clock >= FollowThroughSeconds) SetPhase(CastPhase.Recover);
            }
            else if (_clock >= RecoverySeconds) SetPhase(CastPhase.Wait);
        }

        private void BeginWindup(Vector3 playerPoint)
        {
            Vector3 delta = playerPoint - Actor.transform.position;
            delta.y = 0f;
            if (Actor.IsBossStage)
                LockedDirection = UnderworldCamera2D.IsoRightXZ *
                    (Vector3.Dot(delta, UnderworldCamera2D.IsoRightXZ) < 0f ? -1f : 1f);
            else
                LockedDirection = delta.sqrMagnitude > .001f ? delta.normalized : UnderworldCamera2D.IsoRightXZ;
            Variant = (SonicCall)(CastCount++ % 3);
            IsDoublePulse = Variant != SonicCall.SinglePulse;
            PulsesInCast = 0;
            ReturnOrigin = CalculateReturnOrigin(playerPoint);
            // The outward pulse must clear the remote source before the response is released.
            // This preserves a full dash recharge even for a player standing near either source.
            CurrentPulseGapSeconds = Variant == SonicCall.ReturnChorus
                ? Vector3.Distance(Actor.transform.position, ReturnOrigin) / WaveSpeed + ReturnDashGapSeconds
                : PulseGapSeconds;
            Actor.Face(Actor.transform.position + LockedDirection);
            SetPhase(CastPhase.Windup);
            ClearCharge();
            _charge = EnemyPaintedEffects.Spawn(PaintedEnemyKind.Tripod, 3,
                HeadPoint(), 1.65f, WindupSeconds + .1f);
            if (_charge != null) _charge.GetComponent<EnemyPaintedEffects>().Loop = true;
            PreparePulseWarning(0);
        }

        private Vector3 HeadPoint() => Actor.transform.position + Vector3.up * 2.2f;

        private Vector3 CalculateReturnOrigin(Vector3 playerPoint)
        {
            Vector3 home = Actor.transform.position;
            float along = Mathf.Clamp(Vector3.Dot(playerPoint - home, LockedDirection) + 2.8f, 3.5f, MaximumReturnDistance);
            Vector3 origin = home + LockedDirection * along;
            if (Actor.IsBossStage)
            {
                float stageAlong = Vector3.Dot(origin - Actor.StageCenter, UnderworldCamera2D.IsoRightXZ);
                origin += UnderworldCamera2D.IsoRightXZ * (Mathf.Clamp(stageAlong, -9.0f, 6.7f) - stageAlong);
            }
            else if (ChamberCollision.SweepObstacle(home + Vector3.up * WaveHeight,
                origin - home, WaveRadius, out var hit))
            {
                // A resonator cannot materialize on the far side of solid cover.
                origin = home + LockedDirection * Mathf.Max(0f, hit.distance - .1f);
            }
            origin.y = home.y;
            return origin;
        }

        private void PreparePulseWarning(int pulseIndex)
        {
            ClearWarnings();
            bool returning = Variant == SonicCall.ReturnChorus && pulseIndex == 1;
            Vector3 origin = returning ? ReturnOrigin : Actor.transform.position + LockedDirection * .85f;
            Vector3 direction = returning ? -LockedDirection : LockedDirection;
            float length = WaveSpeed * WaveLifetime;
            if (Actor.IsBossStage)
            {
                float stageAlong = Vector3.Dot(origin - Actor.StageCenter, UnderworldCamera2D.IsoRightXZ);
                float sign = Vector3.Dot(direction, UnderworldCamera2D.IsoRightXZ);
                length = Mathf.Min(length, Mathf.Max(0f, sign > 0f ? 7.2f - stageAlong : stageAlong + 9.5f));
            }
            int disks = Mathf.Max(1, Mathf.CeilToInt(length / (WaveRadius * 1.8f)));
            float warningRadius = Mathf.Sqrt(WaveRadius * WaveRadius + Mathf.Pow(length / disks * .5f, 2));
            for (int i = 0; i <= disks; i++)
                _warnings.Add(PaintedAttackShadow.Create(origin + direction * (length * i / disks),
                    warningRadius, pulseIndex == 0 ? WindupSeconds : CurrentPulseGapSeconds));
        }

        private void EmitPulse(int effectRow)
        {
            // Direct calls use the live boss; the return is committed to its warned resonator.
            bool returning = Variant == SonicCall.ReturnChorus && effectRow == 1;
            Vector3 direction = returning ? -LockedDirection : LockedDirection;
            Vector3 origin = (returning ? ReturnOrigin : Actor.transform.position + LockedDirection * .85f) + Vector3.up * WaveHeight;
            var wave = PaintedEnemyProjectile.Spawn(PaintedEnemyKind.Tripod, effectRow,
                origin, direction, WaveSpeed, WaveDamage, WaveRadius, WaveLifetime);
            wave.Owner = Actor;
            _waves.Add(wave);
            PulseCount++;
            PulsesInCast++;
            Actor.Art?.Play(IsDoublePulse ? EnemyMotion.Attack2 : EnemyMotion.Attack,
                IsDoublePulse && effectRow == 0 ? CurrentPulseGapSeconds : FollowThroughSeconds);
        }

        private void SetPhase(CastPhase phase)
        {
            Phase = phase;
            _clock = 0f;
            if (Actor == null || phase == CastPhase.PulseGap || phase == CastPhase.FollowThrough) return;
            EnemyMotion motion = phase == CastPhase.Windup ? EnemyMotion.Windup :
                phase == CastPhase.Recover ? EnemyMotion.Recover : EnemyMotion.Idle;
            Actor.Art?.Play(motion, Duration(phase), phase == CastPhase.Wait);
        }

        public static float Duration(CastPhase phase)
        {
            switch (phase)
            {
                case CastPhase.Windup: return WindupSeconds;
                case CastPhase.PulseGap: return PulseGapSeconds;
                case CastPhase.FollowThrough: return FollowThroughSeconds;
                case CastPhase.Recover: return RecoverySeconds;
                default: return WaitSeconds;
            }
        }

        public override void Cancel()
        {
            foreach (var wave in _waves) if (wave != null) wave.Remove();
            _waves.Clear();
            ClearCharge();
            ClearWarnings();
            LockedDirection = Vector3.zero;
            ReturnOrigin = Vector3.zero;
            Variant = SonicCall.SinglePulse;
            PulsesInCast = 0;
            CurrentPulseGapSeconds = PulseGapSeconds;
            IsDoublePulse = false;
            SetPhase(CastPhase.Wait);
        }

        private void ClearWarnings()
        {
            foreach (var warning in _warnings) if (warning != null) warning.Dispose();
            _warnings.Clear();
        }

        private void ClearCharge()
        {
            if (_charge != null)
            {
                if (Application.isPlaying) Destroy(_charge);
                else DestroyImmediate(_charge);
            }
            _charge = null;
        }
    }
}
