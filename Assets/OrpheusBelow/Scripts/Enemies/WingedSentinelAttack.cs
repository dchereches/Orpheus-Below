using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>A deliberate sword guardian: the circle locks before the blade moves.</summary>
    public sealed class WingedSentinelAttack : PaintedAttackPattern
    {
        public const float WarningSeconds = 1.8f;
        public const float SweepRadius = 6f;
        public const float CleaveRadius = 3.4f;
        public const float AttackSeconds = 0.8f;
        public const float HitAtSeconds = 0.48f;
        public const float RecoverySeconds = 1.3f;
        public const float CooldownSeconds = 1.65f;
        public const float SwordDamage = 26f;

        public enum SwordPhase { Cooldown, Windup, Strike, Recover }
        public SwordPhase Phase { get; private set; }
        public float PhaseElapsed { get; private set; }
        public Vector3 LockedCenter { get; private set; }
        public float LockedRadius { get; private set; }
        public bool HasPendingHit => Phase == SwordPhase.Windup || (Phase == SwordPhase.Strike && !_hitResolved);

        private readonly List<GameObject> _effects = new List<GameObject>(3);
        private PaintedAttackShadow _warning;
        private bool _cleave;
        private bool _hitResolved;
        private int _attackCount;

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
            _attackCount = 0;
            Actor?.Art?.Play(EnemyMotion.Idle, CooldownSeconds, true);
        }

        public override void Tick(float dt)
        {
            var player = OrpheusPlayerController.Instance;
            if (Actor == null || player == null || player.IsGameOver || player.IsVictory ||
                Time.timeScale <= 0f || UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated ||
                    Actor.OwnerBoss.CurrentPhase != DuelPhase.ArenaCombat)))
            {
                Cancel();
                return;
            }
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;

            // A stall can lengthen anticipation, but cannot consume an unseen warning.
            float step = Mathf.Min(dt, 0.1f);
            PhaseElapsed += step;
            switch (Phase)
            {
                case SwordPhase.Cooldown:
                    Actor.Face(player.transform.position);
                    Vector3 gap = player.transform.position - Actor.transform.position;
                    gap.y = 0f;
                    if (gap.sqrMagnitude > 5.4f * 5.4f)
                    {
                        Actor.MoveToward(player.transform.position, 1.7f, step);
                        Actor.Art?.Play(EnemyMotion.Move, 0.9f, true);
                        return;
                    }
                    if (PhaseElapsed >= CooldownSeconds) BeginWindup(player.transform.position);
                    break;
                case SwordPhase.Windup:
                    _warning?.Progress(PhaseElapsed / WarningSeconds);
                    if (PhaseElapsed >= WarningSeconds)
                    {
                        Phase = SwordPhase.Strike;
                        PhaseElapsed = 0f;
                        Actor.Art?.Play(_cleave ? EnemyMotion.Attack2 : EnemyMotion.Attack, AttackSeconds);
                        _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.WingedSentinel,
                            _cleave ? 1 : 0, LockedCenter + Vector3.up * 0.8f,
                            LockedRadius * 2f, AttackSeconds));
                    }
                    break;
                case SwordPhase.Strike:
                    if (!_hitResolved && PhaseElapsed >= HitAtSeconds)
                    {
                        // Set first: damage callbacks may cancel this pattern immediately.
                        _hitResolved = true;
                        ClearWarning();
                        _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.WingedSentinel, 2,
                            LockedCenter, LockedRadius * 2f, 0.55f));
                        Actor.TryHitPlayer(LockedCenter, LockedRadius, SwordDamage);
                        if (Phase != SwordPhase.Strike) return;
                    }
                    if (PhaseElapsed >= AttackSeconds)
                    {
                        ClearWarning();
                        Phase = SwordPhase.Recover;
                        PhaseElapsed = 0f;
                        Actor.Art?.Play(EnemyMotion.Recover, RecoverySeconds);
                    }
                    break;
                case SwordPhase.Recover:
                    if (PhaseElapsed >= RecoverySeconds)
                    {
                        ClearEffects();
                        Phase = SwordPhase.Cooldown;
                        PhaseElapsed = 0f;
                        Actor.Art?.Play(EnemyMotion.Idle, CooldownSeconds, true);
                    }
                    break;
            }
        }

        private void BeginWindup(Vector3 target)
        {
            ClearEffects();
            _cleave = (++_attackCount % 2) == 0;
            _hitResolved = false;
            Vector3 origin = Actor.transform.position;
            Vector3 direction = target - origin;
            direction.y = 0f;
            // The overhead strike reaches forward, while the sweep surrounds the sentinel.
            LockedCenter = _cleave ? origin + Vector3.ClampMagnitude(direction, 2.7f) : origin;
            LockedRadius = _cleave ? CleaveRadius : SweepRadius;
            Actor.Face(target);
            Phase = SwordPhase.Windup;
            PhaseElapsed = 0f;
            Actor.Art?.Play(EnemyMotion.Windup, WarningSeconds);
            _warning = PaintedAttackShadow.Create(LockedCenter, LockedRadius,
                WarningSeconds + HitAtSeconds + 0.1f);
        }

        public override void Cancel()
        {
            ClearWarning();
            ClearEffects();
            Phase = SwordPhase.Cooldown;
            PhaseElapsed = 0f;
            LockedCenter = Vector3.zero;
            LockedRadius = 0f;
            _hitResolved = true;
            base.Cancel();
        }

        private void ClearWarning()
        {
            _warning?.Dispose();
            _warning = null;
        }

        private void ClearEffects()
        {
            foreach (var effect in _effects)
            {
                if (effect == null) continue;
                if (Application.isPlaying) Destroy(effect); else DestroyImmediate(effect);
            }
            _effects.Clear();
        }

        protected override void OnDisable() { Cancel(); }
    }
}
