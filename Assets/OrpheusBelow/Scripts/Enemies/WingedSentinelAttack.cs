using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Three committed sword shapes: a sweep, a thrust, and an outward blade fan.</summary>
    public sealed class WingedSentinelAttack : PaintedAttackPattern
    {
        public const float WarningSeconds = 1.05f;
        public const float SweepRadius = 6f;
        public const float CleaveRadius = 3.4f;
        public const float AttackSeconds = .7f;
        public const float HitAtSeconds = .16f;
        public const float RecoverySeconds = .42f;
        public const float CooldownSeconds = .48f;
        public const float SwordDamage = 26f;

        public enum SwordPhase { Cooldown, Windup, Strike, Recover }
        public enum SwordPattern { Sweep, Thrust, BladeFan }
        public SwordPhase Phase { get; private set; }
        public SwordPattern CurrentPattern { get; private set; }
        public float PhaseElapsed { get; private set; }
        public Vector3 LockedCenter { get; private set; }
        public float LockedRadius { get; private set; }
        public bool HasPendingHit => Phase == SwordPhase.Windup || (Phase == SwordPhase.Strike && !_hitResolved);
        public int CompletedSwings { get; private set; }

        private struct BladeCell
        {
            public Vector3 Center;
            public float Radius;
            public int Step, Lane;
            public PaintedAttackShadow Shadow;
        }

        // At most eighteen warned cells and three moving painted blades belong to this attack.
        private readonly List<BladeCell> _cells = new List<BladeCell>(18);
        private readonly List<GameObject> _effects = new List<GameObject>(3);
        private PaintedAttackShadow _warning;
        private bool _hitResolved, _damageDealt, _swingCounted;
        private int _attackCount, _resolvedWaveStep;
        private Vector3 _lockedOrigin;
        private float WindupDuration => CurrentPattern == SwordPattern.Sweep ? WarningSeconds :
            CurrentPattern == SwordPattern.Thrust ? .7f : .85f;

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
                (Actor.OwnerShade != null && (Actor.OwnerShade.CurrentHealth <= 0f || Actor.OwnerShade.IsPacified ||
                    Actor.OwnerShade.IsFleeing || Actor.OwnerShade.IsStunned)) ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated || Actor.OwnerBoss.CurrentPhase != DuelPhase.ArenaCombat)))
            {
                Cancel();
                return;
            }
            // Pausing preserves the locked warning and elapsed time, including direct Tick callers.
            if (Time.timeScale <= 0f || UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive ||
                dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;

            // A stall can lengthen anticipation, but cannot consume an unseen warning.
            float step = Mathf.Min(dt, .1f);
            PhaseElapsed += step;
            switch (Phase)
            {
                case SwordPhase.Cooldown:
                    Actor.Face(player.transform.position);
                    Vector3 gap = player.transform.position - Actor.transform.position;
                    gap.y = 0f;
                    if (gap.sqrMagnitude > 5.4f * 5.4f)
                    {
                        Actor.MoveToward(player.transform.position, 2.8f, step, navigate: true);
                        Actor.Art?.Play(EnemyMotion.Move, .7f, true);
                        return;
                    }
                    if (PhaseElapsed >= CooldownSeconds) BeginWindup(player.transform.position);
                    break;
                case SwordPhase.Windup:
                    foreach (var cell in _cells) cell.Shadow?.Progress(PhaseElapsed / WindupDuration);
                    if (PhaseElapsed >= WindupDuration)
                    {
                        Phase = SwordPhase.Strike;
                        PhaseElapsed = 0f;
                        Actor.Art?.Play(CurrentPattern == SwordPattern.Sweep ? EnemyMotion.Attack : EnemyMotion.Attack2, AttackSeconds);
                        if (CurrentPattern != SwordPattern.BladeFan)
                            _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.WingedSentinel,
                                CurrentPattern == SwordPattern.Sweep ? 0 : 1, LockedCenter + Vector3.up * .8f,
                                CurrentPattern == SwordPattern.Sweep ? SweepRadius * 2 : 3f, AttackSeconds));
                    }
                    break;
                case SwordPhase.Strike:
                    if (!_hitResolved && PhaseElapsed >= HitAtSeconds)
                    {
                        // Count a released sword attack once, regardless of how many blade cells it uses.
                        if (!_swingCounted) { _swingCounted = true; CompletedSwings++; }
                        int waveStep = CurrentPattern == SwordPattern.BladeFan
                            ? Mathf.Min(5, Mathf.FloorToInt((PhaseElapsed - HitAtSeconds) / .08f)) : 5;
                        ResolveCells(waveStep);
                        if (Phase != SwordPhase.Strike) return; // Damage callbacks may cancel the owner.
                        if (waveStep >= 5) { _hitResolved = true; ClearWarning(); }
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
            ClearWarning();
            ClearEffects();
            CurrentPattern = (SwordPattern)(_attackCount++ % 3);
            _hitResolved = _damageDealt = _swingCounted = false;
            _resolvedWaveStep = -1;
            _lockedOrigin = Actor.transform.position;
            Vector3 direction = target - _lockedOrigin;
            direction.y = 0;
            direction = direction.sqrMagnitude > .001f ? direction.normalized : Vector3.forward;
            LockedCenter = CurrentPattern == SwordPattern.Sweep ? _lockedOrigin : _lockedOrigin + direction * 2.7f;
            // Keep the public locked circle as a conservative bound around the complete committed shape.
            LockedRadius = CurrentPattern == SwordPattern.Sweep ? SweepRadius :
                CurrentPattern == SwordPattern.Thrust ? CleaveRadius : 4.7f;
            Actor.Face(target);
            Phase = SwordPhase.Windup;
            PhaseElapsed = 0f;
            Actor.Art?.Play(EnemyMotion.Windup, WindupDuration);
            if (CurrentPattern == SwordPattern.Sweep) AddCell(_lockedOrigin, SweepRadius, 0);
            else if (CurrentPattern == SwordPattern.Thrust)
            {
                for (int step = 0; step < 6; step++)
                {
                    Vector3 point = _lockedOrigin + direction * (.9f * (step + 1));
                    if (BlockedFromOrigin(point)) break;
                    AddCell(point, .7f, 0);
                }
            }
            else
            {
                // The entire three-lane fan is warned before release; gaps stay safe throughout.
                for (int lane = -1; lane <= 1; lane++)
                {
                    Vector3 ray = Quaternion.AngleAxis(lane * 32f, Vector3.up) * direction;
                    for (int step = 0; step < 6; step++)
                    {
                        Vector3 point = _lockedOrigin + ray * (step + 1);
                        if (BlockedFromOrigin(point)) break;
                        AddCell(point, .65f, step, lane + 1);
                    }
                }
            }
        }

        private void AddCell(Vector3 point, float radius, int step, int lane = 0)
        {
            var shadow = PaintedAttackShadow.Create(point, radius, WindupDuration + AttackSeconds + .1f);
            _cells.Add(new BladeCell { Center = point, Radius = radius, Step = step, Lane = lane, Shadow = shadow });
            if (_warning == null) _warning = shadow;
        }

        private void ResolveCells(int throughStep)
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell.Step <= _resolvedWaveStep || cell.Step > throughStep) continue;
                if (CurrentPattern == SwordPattern.BladeFan)
                {
                    // Reuse exactly three painted sword effects as the blades move out through warned cells.
                    int lane = cell.Lane;
                    while (_effects.Count <= lane) _effects.Add(null);
                    if (_effects[lane] == null)
                        _effects[lane] = EnemyPaintedEffects.Spawn(PaintedEnemyKind.WingedSentinel, 1,
                            cell.Center + Vector3.up * .65f, 1.8f, AttackSeconds);
                    else _effects[lane].transform.position = cell.Center + Vector3.up * .65f;
                }
                if (!_damageDealt && !BlockedFromOrigin(cell.Center))
                {
                    var player = OrpheusPlayerController.Instance;
                    if (player != null && !BlockedFromOrigin(player.transform.position) &&
                        Actor.TryHitPlayer(cell.Center, cell.Radius, SwordDamage)) _damageDealt = true;
                    if (Phase != SwordPhase.Strike) return;
                }
            }
            _resolvedWaveStep = throughStep;
        }

        private bool BlockedFromOrigin(Vector3 point)
        {
            Vector3 start = _lockedOrigin + Vector3.up * .65f;
            foreach (var collider in Physics.OverlapSphere(start, .08f, ~0, QueryTriggerInteraction.Ignore))
                if (collider.TryGetComponent<IsoObstacle>(out var obstacle) && obstacle.kind == IsoObstacleKind.SolidWall) return true;
            Vector3 delta = point - _lockedOrigin; delta.y = 0;
            return ChamberCollision.SweepObstacle(start, delta, .08f, out _);
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
            foreach (var cell in _cells) cell.Shadow?.Dispose();
            _cells.Clear();
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
