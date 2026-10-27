using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Ivory head rings announce committed sonic pulses across the narrow duel stage.</summary>
    public sealed class TripodAttack : PaintedAttackPattern
    {
        public const float WaitSeconds = 1.5f;
        public const float WindupSeconds = 1.25f;
        public const float PulseGapSeconds = .8f;
        public const float FollowThroughSeconds = .3f;
        public const float RecoverySeconds = 1.1f;
        public const float WaveSpeed = 8f;
        public const float WaveDamage = 20f;
        public const float WaveRadius = .7f;
        public const float WaveHeight = .8f;
        public const float WaveLifetime = 3.5f;

        public enum CastPhase { Wait, Windup, PulseGap, FollowThrough, Recover }
        public CastPhase Phase { get; private set; } = CastPhase.Wait;
        public float PhaseElapsed => _clock;
        public float PhaseRemaining => Mathf.Max(0f, Duration(Phase) - _clock);
        public Vector3 LockedDirection { get; private set; }
        public int CastCount { get; private set; }
        public int PulseCount { get; private set; }
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
            if (Actor == null || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt) ||
                Time.timeScale <= 0f || UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated ||
                    Actor.OwnerBoss.CurrentPhase != DuelPhase.ArenaCombat)))
            {
                Cancel();
                return;
            }

            // A stall extends the telegraph instead of silently consuming the warning or pulse gap.
            _clock += Mathf.Min(dt, .1f);
            _waves.RemoveAll(wave => wave == null || wave.Removed);
            if (_charge != null) _charge.transform.position = HeadPoint();
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
                    EmitPulse(0);
                    SetPhase(IsDoublePulse ? CastPhase.PulseGap : CastPhase.FollowThrough);
                }
            }
            else if (Phase == CastPhase.PulseGap)
            {
                if (_clock >= PulseGapSeconds)
                {
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
            IsDoublePulse = (++CastCount % 2) == 0;
            Actor.Face(Actor.transform.position + LockedDirection);
            SetPhase(CastPhase.Windup);
            _charge = EnemyPaintedEffects.Spawn(PaintedEnemyKind.Tripod, 3,
                HeadPoint(), 1.65f, WindupSeconds + .1f);
        }

        private Vector3 HeadPoint() => Actor.transform.position + Vector3.up * 2.2f;

        private void EmitPulse(int effectRow)
        {
            // GroundAnchor is a fixed spawn point; all attacks originate from the boss's live position.
            Vector3 origin = Actor.transform.position + Vector3.up * WaveHeight + LockedDirection * .85f;
            var wave = PaintedEnemyProjectile.Spawn(PaintedEnemyKind.Tripod, effectRow,
                origin, LockedDirection, WaveSpeed, WaveDamage, WaveRadius, WaveLifetime);
            wave.Owner = Actor;
            _waves.Add(wave);
            PulseCount++;
            Actor.Art?.Play(IsDoublePulse ? EnemyMotion.Attack2 : EnemyMotion.Attack,
                IsDoublePulse && effectRow == 0 ? PulseGapSeconds : FollowThroughSeconds);
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
            LockedDirection = Vector3.zero;
            IsDoublePulse = false;
            SetPhase(CastPhase.Wait);
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
