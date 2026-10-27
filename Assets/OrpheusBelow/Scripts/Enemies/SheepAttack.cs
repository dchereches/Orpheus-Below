using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Attackable sheep lock their entire warned dash lane before moving.</summary>
    public sealed class SheepAttack : PaintedAttackPattern
    {
        public const float WarningSeconds = .8f;
        public const float DashSpeed = 11f;
        public const float DashSeconds = .6f;
        public const float DashDamage = 14f;
        public const float HitRadius = .85f;
        private const float RestSeconds = 1.1f;
        private const float RecoverySeconds = 1f;
        public enum DashPhase { Rest, Warning, Dash, Recover }
        public DashPhase Phase { get; private set; }
        public Vector3 LockedDirection { get; private set; }
        public bool HasHit { get; private set; }
        public int WarningCount => _warnings.Count;
        private readonly List<PaintedAttackShadow> _warnings = new List<PaintedAttackShadow>(9);
        private readonly List<GameObject> _effects = new List<GameObject>(3);
        private float _elapsed;
        private int _dashIndex;
        private Vector3 _dashOrigin;
        private bool _commandPending;
        private Vector3 _commandTarget;

        public override void Initialize(PaintedEnemyActor actor) { Cancel(); base.Initialize(actor); }

        public override void Tick(float dt)
        {
            if (Actor == null || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory) { Cancel(); return; }
            // Clamping attack time prevents a stalled frame from swallowing the visible warning.
            float step = Mathf.Min(dt, .1f);
            _elapsed += step;
            // A command is consumed only when the owning shade permits its AI tick, so a bell
            // cannot draw a fresh warning on a sheep that is still musically stunned.
            if (_commandPending && (Phase == DashPhase.Rest || Phase == DashPhase.Recover))
            {
                _commandPending = false;
                CommandDash(_commandTarget);
                return;
            }
            switch (Phase)
            {
                case DashPhase.Rest:
                    Actor.Art?.Play(EnemyMotion.Move, .8f, true);
                    Actor.Face(player.transform.position);
                    Vector3 toward = player.transform.position - Actor.transform.position; toward.y = 0;
                    if (toward.magnitude > 3.5f) Actor.MoveToward(player.transform.position, 3.5f, step);
                    if (_elapsed >= RestSeconds) CommandDash(player.transform.position);
                    break;
                case DashPhase.Warning:
                    foreach (var warning in _warnings) warning?.Progress(_elapsed / WarningSeconds);
                    if (_elapsed >= WarningSeconds)
                    {
                        ClearWarnings(); _elapsed = 0; Phase = DashPhase.Dash;
                        Actor.Art?.Play((++_dashIndex & 1) == 0 ? EnemyMotion.Attack2 : EnemyMotion.Attack, DashSeconds);
                    }
                    break;
                case DashPhase.Dash:
                    AdvanceDash(step);
                    if (_elapsed >= DashSeconds) BeginRecovery();
                    break;
                case DashPhase.Recover:
                    if (_elapsed >= RecoverySeconds) { Phase = DashPhase.Rest; _elapsed = 0; }
                    break;
            }
        }

        public void CommandDash(Vector3 target)
        {
            if (Actor == null || Phase == DashPhase.Dash || Phase == DashPhase.Warning) return;
            ClearWarnings(); ClearEffects();
            _dashOrigin = Actor.transform.position;
            Vector3 aim = target - _dashOrigin; aim.y = 0;
            if (Actor.IsBossStage) aim = UnderworldCamera2D.IsoRightXZ * Vector3.Dot(aim, UnderworldCamera2D.IsoRightXZ);
            LockedDirection = aim.sqrMagnitude > .001f ? aim.normalized : UnderworldCamera2D.IsoRightXZ;
            HasHit = false; _elapsed = 0; Phase = DashPhase.Warning;
            Actor.Face(_dashOrigin + LockedDirection); Actor.Art?.Play(EnemyMotion.Windup, WarningSeconds);
            // Overlapping disks cover the complete swept hit capsule, including its start and end.
            for (int i = 0; i <= 8; i++)
            {
                Vector3 center = _dashOrigin + LockedDirection * (DashSpeed * DashSeconds * i / 8f);
                _warnings.Add(PaintedAttackShadow.Create(center, HitRadius, WarningSeconds));
            }
        }

        public void RequestCommand(Vector3 target)
        {
            _commandTarget = target;
            _commandPending = true;
        }

        private void AdvanceDash(float step)
        {
            // Small movement slices prevent a quick animal from tunnelling through the player's hit area.
            int slices = Mathf.Max(1, Mathf.CeilToInt(DashSpeed * step / .25f));
            for (int i = 0; i < slices; i++)
            {
                Vector3 before = Actor.transform.position;
                Actor.MoveToward(before + LockedDirection, DashSpeed, step / slices);
                if (!HasHit && Actor.TryHitPlayer(Actor.transform.position, HitRadius, DashDamage))
                {
                    HasHit = true;
                    _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.Sheep, 2,
                        Actor.transform.position, 1.1f, .35f));
                }
                if ((Actor.transform.position - before).sqrMagnitude < .00001f) { BeginRecovery(); break; }
            }
        }

        private void BeginRecovery()
        {
            Phase = DashPhase.Recover; _elapsed = 0;
            Actor.Art?.Play(EnemyMotion.Recover, RecoverySeconds);
        }

        public override void Cancel()
        {
            ClearWarnings(); ClearEffects();
            Phase = DashPhase.Rest; _elapsed = 0; HasHit = false; LockedDirection = Vector3.zero; _commandPending = false;
        }
        private void ClearWarnings()
        {
            foreach (var warning in _warnings) warning?.Dispose();
            _warnings.Clear();
        }
        private void ClearEffects()
        {
            foreach (var effect in _effects)
                if (effect != null) { if (Application.isPlaying) Destroy(effect); else DestroyImmediate(effect); }
            _effects.Clear();
        }
    }
}
