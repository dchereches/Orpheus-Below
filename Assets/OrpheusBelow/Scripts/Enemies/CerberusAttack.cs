using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>A committed bite, tail sweep, and warned lane pounce driven by the owning actor.</summary>
    public sealed class CerberusAttack : PaintedAttackPattern
    {
        public enum AttackPhase { Waiting, Windup, Bite, TailWhip, Recovery, Pounce }
        public enum AttackKind { Bite, TailWhip, Pounce }

        public const float OpeningWaitDuration = 1.05f;
        public const float WaitDuration = 0.52f;
        public const float BiteWindupDuration = 0.48f;
        public const float TailWindupDuration = 0.65f;
        public const float PounceWindupDuration = 0.72f;
        public const float BiteDuration = 0.28f;
        public const float TailDuration = 0.34f;
        public const float PounceDuration = 0.36f;
        public const float BiteRecoveryDuration = 0.36f;
        public const float TailRecoveryDuration = 0.44f;
        public const float PounceRecoveryDuration = 0.48f;
        public const float BiteRadius = 2.30f;
        public const float TailRadius = 4.20f;
        public const float PounceRadius = 1.25f;
        public const float MaximumPounceDistance = 6.2f;
        public const float BiteDamage = 28f;
        public const float TailDamage = 34f;
        public const float PounceDamage = 30f;

        public AttackPhase Phase { get; private set; } = AttackPhase.Waiting;
        public float AttackTimer { get; private set; }
        public bool IsTailAttack => SelectedAttack == AttackKind.TailWhip;
        public AttackKind SelectedAttack { get; private set; }
        public AttackKind NextAttack { get; private set; }
        public Vector3 CommittedTarget => _attackCenter;
        public int BiteCount { get; private set; }
        public int TailWhipCount { get; private set; }
        public int PounceCount { get; private set; }
        public bool HasPendingDamage => !_hitSpent &&
            (Phase == AttackPhase.Windup || Phase == AttackPhase.Bite ||
             Phase == AttackPhase.TailWhip || Phase == AttackPhase.Pounce);

        private bool _openingWait = true;
        private bool _hitSpent;
        private Vector3 _attackCenter;
        private float _lungeSpeed;
        private readonly System.Collections.Generic.List<PaintedAttackShadow> _shadows =
            new System.Collections.Generic.List<PaintedAttackShadow>();
        private readonly System.Collections.Generic.List<GameObject> _effects =
            new System.Collections.Generic.List<GameObject>();

        private float WindupDuration => SelectedAttack == AttackKind.Pounce ? PounceWindupDuration :
            IsTailAttack ? TailWindupDuration : BiteWindupDuration;
        private float StrikeDuration => SelectedAttack == AttackKind.Pounce ? PounceDuration :
            IsTailAttack ? TailDuration : BiteDuration;
        private float RecoveryDuration => SelectedAttack == AttackKind.Pounce ? PounceRecoveryDuration :
            IsTailAttack ? TailRecoveryDuration : BiteRecoveryDuration;

        public override void Initialize(PaintedEnemyActor actor)
        {
            base.Initialize(actor);
            NextAttack = AttackKind.Bite;
            SelectedAttack = AttackKind.Bite;
            _openingWait = true;
            Cancel();
        }

        public override void Tick(float dt)
        {
            if (Time.timeScale <= 0f || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (Actor == null) return;
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (!Actor.isActiveAndEnabled || !isActiveAndEnabled ||
                player == null || player.IsGameOver || player.IsVictory ||
                (Actor.OwnerShade != null && (Actor.OwnerShade.CurrentHealth <= 0f || Actor.OwnerShade.IsPacified)) ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated ||
                    Actor.OwnerBoss.CurrentPhase == DuelPhase.CrashedStunned)) ||
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
                    float stopDistance = NextAttack == AttackKind.Pounce ? 6f :
                        NextAttack == AttackKind.TailWhip ? 3.5f : 4.2f;
                    if (delta.sqrMagnitude > stopDistance * stopDistance)
                    {
                        Actor.MoveToward(target - delta.normalized * stopDistance, 2.8f, dt);
                        Actor.Art?.Play(EnemyMotion.Move, 0.5f, true);
                    }
                    else Actor.Art?.Play(EnemyMotion.Idle, 1f, true);
                    if (AttackTimer >= (_openingWait ? OpeningWaitDuration : WaitDuration)) BeginWindup(target);
                    break;

                case AttackPhase.Windup:
                    ProgressShadows(Mathf.Clamp01(AttackTimer / WindupDuration));
                    if (AttackTimer >= WindupDuration) BeginStrike();
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

                case AttackPhase.Pounce:
                    AdvancePounce(player, dt);
                    if (AttackTimer >= PounceDuration) BeginRecovery();
                    break;

                case AttackPhase.Recovery:
                    if (AttackTimer >= RecoveryDuration)
                    {
                        ClearEffects();
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
            ClearShadows();
            ClearEffects();
            SelectedAttack = NextAttack;
            _openingWait = false;
            _hitSpent = false;
            Phase = AttackPhase.Windup;
            AttackTimer = 0f;
            Vector3 origin = Actor.transform.position;
            Vector3 delta = target - origin;
            delta.y = 0f;
            // Commit both aim and damage center before telegraphing. Dodging cannot be tracked.
            Vector3 direction = delta.sqrMagnitude > .0001f ? delta.normalized : UnderworldCamera2D.IsoRightXZ;
            if (Actor.IsBossStage)
                direction = Vector3.Dot(direction, UnderworldCamera2D.IsoRightXZ) < 0f ?
                    -UnderworldCamera2D.IsoRightXZ : UnderworldCamera2D.IsoRightXZ;
            float distance = SelectedAttack == AttackKind.Pounce ?
                Mathf.Clamp(delta.magnitude + 1.6f, 3f, MaximumPounceDistance) :
                Mathf.Min(2.6f, Mathf.Max(0f, delta.magnitude - 0.7f));
            _attackCenter = IsTailAttack ? origin : origin + direction * distance;
            // Match the duel floor's movement bound, so the warning does not promise unreachable travel.
            if (Actor.IsBossStage && SelectedAttack == AttackKind.Pounce)
            {
                float along = Vector3.Dot(_attackCenter - Actor.StageCenter, UnderworldCamera2D.IsoRightXZ);
                _attackCenter += UnderworldCamera2D.IsoRightXZ * (Mathf.Clamp(along, -9.5f, 7.2f) - along);
            }
            _lungeSpeed = Vector3.Distance(origin, _attackCenter) / StrikeDuration;
            Actor.Face(target);
            if (SelectedAttack == AttackKind.Pounce)
            {
                // Overlapping circles cover the entire swept contact lane before any movement begins.
                int segments = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(origin, _attackCenter) / PounceRadius));
                for (int i = 0; i <= segments; i++)
                    _shadows.Add(PaintedAttackShadow.Create(Vector3.Lerp(origin, _attackCenter, (float)i / segments),
                        PounceRadius + .2f, WindupDuration + StrikeDuration));
            }
            else _shadows.Add(PaintedAttackShadow.Create(_attackCenter,
                IsTailAttack ? TailRadius : BiteRadius, WindupDuration + StrikeDuration));
            Actor.Art?.Play(EnemyMotion.Windup, WindupDuration);
        }

        private void BeginStrike()
        {
            Phase = SelectedAttack == AttackKind.Pounce ? AttackPhase.Pounce :
                IsTailAttack ? AttackPhase.TailWhip : AttackPhase.Bite;
            AttackTimer = 0f;
            Actor.Art?.Play(IsTailAttack ? EnemyMotion.Attack2 : EnemyMotion.Attack, StrikeDuration);
            ProgressShadows(1f);
            if (SelectedAttack == AttackKind.Pounce) PounceCount++;
            if (!IsTailAttack) AddEffect(3, Actor.transform.position, 2.2f, StrikeDuration, true);
        }

        private void AdvancePounce(OrpheusPlayerController player, float dt)
        {
            Vector3 start = Actor.transform.position;
            Actor.MoveToward(_attackCenter, _lungeSpeed, Mathf.Min(dt, PounceDuration));
            if (_hitSpent) return;
            Vector3 travel = Actor.transform.position - start;
            travel.y = 0f;
            Vector3 toPlayer = player.transform.position - start;
            toPlayer.y = 0f;
            float u = travel.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector3.Dot(toPlayer, travel) / travel.sqrMagnitude) : 0f;
            Vector3 contact = start + travel * u;
            Vector3 gap = player.transform.position - contact;
            gap.y = 0f;
            if (gap.sqrMagnitude > PounceRadius * PounceRadius) return;
            // Spending a dashed/covered contact prevents a late hit after the player has escaped it.
            _hitSpent = true;
            Actor.TryHitPlayer(contact, PounceRadius, PounceDamage);
            AddEffect(0, contact + Vector3.up * .6f, 2.5f, .25f);
        }

        private void Strike(float radius, float damage, int effectRow, float effectSize)
        {
            // Spend the hit even when cover or dash prevents damage; no repeated overlap damage.
            _hitSpent = true;
            Vector3 toCenter = _attackCenter - Actor.transform.position;
            if (IsTailAttack) TailWhipCount++; else BiteCount++;
            toCenter.y = 0f;
            bool lungeBlocked = !Actor.IsBossStage && toCenter.sqrMagnitude > 0.001f &&
                ChamberCollision.SweepObstacle(Actor.transform.position + Vector3.up * 0.65f,
                    toCenter, 0.08f, out _);
            if (!lungeBlocked) Actor.TryHitPlayer(_attackCenter, radius, damage);
            AddEffect(effectRow, _attackCenter + Vector3.up * 0.6f, effectSize, .3f);
            ClearShadows();
        }

        private void BeginRecovery()
        {
            _hitSpent = true;
            ClearShadows();
            NextAttack = SelectedAttack == AttackKind.Bite ? AttackKind.TailWhip :
                SelectedAttack == AttackKind.TailWhip ? AttackKind.Pounce : AttackKind.Bite;
            Phase = AttackPhase.Recovery;
            AttackTimer = 0f;
            Actor.Art?.Play(EnemyMotion.Recover, RecoveryDuration);
        }

        public override void Cancel()
        {
            ClearShadows();
            ClearEffects();
            _hitSpent = true;
            _attackCenter = Vector3.zero;
            _lungeSpeed = 0f;
            Phase = AttackPhase.Waiting;
            AttackTimer = 0f;
            if (Actor != null) Actor.Art?.Play(EnemyMotion.Idle, 1f, true);
        }

        private void ProgressShadows(float progress)
        {
            foreach (var shadow in _shadows) if (shadow != null) shadow.Progress(progress);
        }

        private void ClearShadows()
        {
            foreach (var shadow in _shadows) if (shadow != null) shadow.Dispose();
            _shadows.Clear();
        }

        private void AddEffect(int row, Vector3 position, float size, float duration, bool ground = false)
        {
            GameObject effect = Actor.Art?.PlayEffect(row, position, size, duration, ground);
            if (effect != null) _effects.Add(effect);
        }

        private void ClearEffects()
        {
            foreach (var effect in _effects)
            {
                if (effect == null) continue;
                effect.SetActive(false);
                if (Application.isPlaying) Destroy(effect); else DestroyImmediate(effect);
            }
            _effects.Clear();
        }

        protected override void OnDisable() => Cancel();
        private void OnDestroy()
        {
            ClearShadows();
            ClearEffects();
        }
    }
}
