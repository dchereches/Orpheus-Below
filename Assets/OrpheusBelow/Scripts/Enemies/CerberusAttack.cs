using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Alternating, committed bite and tail attacks driven only by the owning actor.</summary>
    public sealed class CerberusAttack : PaintedAttackPattern
    {
        public enum AttackPhase { Waiting, Windup, Bite, TailWhip, Recovery }

        public const float WaitDuration = 1.45f;
        public const float BiteWindupDuration = 0.85f;
        public const float TailWindupDuration = 1.15f;
        public const float BiteDuration = 0.40f;
        public const float TailDuration = 0.50f;
        public const float BiteRecoveryDuration = 0.85f;
        public const float TailRecoveryDuration = 1.05f;
        public const float BiteRadius = 2.30f;
        public const float TailRadius = 4.20f;
        public const float BiteDamage = 18f;
        public const float TailDamage = 22f;

        public AttackPhase Phase { get; private set; } = AttackPhase.Waiting;
        public float AttackTimer { get; private set; }
        public bool IsTailAttack => _tailAttack;
        public bool HasPendingDamage => !_hitSpent &&
            (Phase == AttackPhase.Windup || Phase == AttackPhase.Bite || Phase == AttackPhase.TailWhip);

        private bool _nextTail;
        private bool _tailAttack;
        private bool _hitSpent;
        private Vector3 _attackCenter;
        private float _lungeSpeed;
        private PaintedAttackShadow _shadow;

        public override void Initialize(PaintedEnemyActor actor)
        {
            base.Initialize(actor);
            _nextTail = false;
            Cancel();
        }

        public override void Tick(float dt)
        {
            if (Time.timeScale <= 0f || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (Actor == null) return;

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory ||
                player.IsIn2DBossStage != Actor.IsBossStage)
            {
                Cancel();
                return;
            }

            Vector3 target = player.transform.position;
            target.y = Actor.transform.position.y;
            Vector3 delta = target - Actor.transform.position;
            // A detached or distant actor cannot progress a hidden attack against the player.
            if (delta.sqrMagnitude > 18f * 18f)
            {
                Cancel();
                return;
            }

            AttackTimer += dt;
            switch (Phase)
            {
                case AttackPhase.Waiting:
                    Actor.Face(target);
                    float stopDistance = _nextTail ? 3.5f : 4.2f;
                    if (delta.sqrMagnitude > stopDistance * stopDistance)
                    {
                        Actor.MoveToward(target - delta.normalized * stopDistance, 2.2f, dt);
                        Actor.Art?.Play(EnemyMotion.Move, 0.65f, true);
                    }
                    else Actor.Art?.Play(EnemyMotion.Idle, 1f, true);
                    if (AttackTimer >= WaitDuration) BeginWindup(target);
                    break;

                case AttackPhase.Windup:
                    float windup = _tailAttack ? TailWindupDuration : BiteWindupDuration;
                    _shadow?.Progress(Mathf.Clamp01(AttackTimer / windup));
                    if (AttackTimer >= windup) BeginStrike();
                    break;

                case AttackPhase.Bite:
                    Actor.MoveToward(_attackCenter, _lungeSpeed, dt);
                    if (!_hitSpent && AttackTimer >= BiteDuration * 0.55f)
                        Strike(BiteRadius, BiteDamage, 0, 4.6f);
                    if (AttackTimer >= BiteDuration) BeginRecovery();
                    break;

                case AttackPhase.TailWhip:
                    if (!_hitSpent && AttackTimer >= TailDuration * 0.50f)
                        Strike(TailRadius, TailDamage, 1, 8.4f);
                    if (AttackTimer >= TailDuration) BeginRecovery();
                    break;

                case AttackPhase.Recovery:
                    if (AttackTimer >= (_tailAttack ? TailRecoveryDuration : BiteRecoveryDuration))
                    {
                        Phase = AttackPhase.Waiting;
                        AttackTimer = 0f;
                        Actor.Art?.Play(EnemyMotion.Idle, 1f, true);
                    }
                    break;
            }
            // At most one phase transition per tick preserves a visible windup after a long frame.
        }

        private void BeginWindup(Vector3 target)
        {
            _tailAttack = _nextTail;
            _hitSpent = false;
            Phase = AttackPhase.Windup;
            AttackTimer = 0f;
            Vector3 origin = Actor.transform.position;
            Vector3 delta = target - origin;
            delta.y = 0f;
            // Commit both aim and damage center before telegraphing. Dodging cannot be tracked.
            _attackCenter = _tailAttack ? origin : origin +
                delta.normalized * Mathf.Min(2.6f, Mathf.Max(0f, delta.magnitude - 0.7f));
            _lungeSpeed = Vector3.Distance(origin, _attackCenter) / BiteDuration;
            Actor.Face(target);
            float windup = _tailAttack ? TailWindupDuration : BiteWindupDuration;
            _shadow = PaintedAttackShadow.Create(_attackCenter,
                _tailAttack ? TailRadius : BiteRadius, windup + (_tailAttack ? TailDuration : BiteDuration));
            Actor.Art?.Play(EnemyMotion.Windup, windup);
        }

        private void BeginStrike()
        {
            Phase = _tailAttack ? AttackPhase.TailWhip : AttackPhase.Bite;
            AttackTimer = 0f;
            Actor.Art?.Play(_tailAttack ? EnemyMotion.Attack2 : EnemyMotion.Attack,
                _tailAttack ? TailDuration : BiteDuration);
            _shadow?.Progress(1f);
            if (!_tailAttack) Actor.Art?.PlayEffect(3, Actor.transform.position, 2.2f, 0.5f, true);
        }

        private void Strike(float radius, float damage, int effectRow, float effectSize)
        {
            // Spend the hit even when cover or dash prevents damage; no repeated overlap damage.
            _hitSpent = true;
            Vector3 toCenter = _attackCenter - Actor.transform.position;
            toCenter.y = 0f;
            bool lungeBlocked = !Actor.IsBossStage && toCenter.sqrMagnitude > 0.001f &&
                ChamberCollision.SweepObstacle(Actor.transform.position + Vector3.up * 0.65f,
                    toCenter, 0.08f, out _);
            if (!lungeBlocked) Actor.TryHitPlayer(_attackCenter, radius, damage);
            Actor.Art?.PlayEffect(effectRow, _attackCenter + Vector3.up * 0.6f, effectSize, 0.4f);
            ClearShadow();
        }

        private void BeginRecovery()
        {
            _hitSpent = true;
            ClearShadow();
            _nextTail = !_tailAttack;
            Phase = AttackPhase.Recovery;
            AttackTimer = 0f;
            Actor.Art?.Play(EnemyMotion.Recover, _tailAttack ? TailRecoveryDuration : BiteRecoveryDuration);
        }

        public override void Cancel()
        {
            ClearShadow();
            _hitSpent = true;
            _attackCenter = Vector3.zero;
            _lungeSpeed = 0f;
            Phase = AttackPhase.Waiting;
            AttackTimer = 0f;
            if (Actor != null) Actor.Art?.Play(EnemyMotion.Idle, 1f, true);
        }

        private void ClearShadow()
        {
            _shadow?.Dispose();
            _shadow = null;
        }

        protected override void OnDisable() => Cancel();
        private void OnDestroy() => ClearShadow();
    }
}
