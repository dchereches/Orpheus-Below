using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Independent physical pressure while musical notes are being answered.</summary>
    public sealed class BossRhythmPressure : MonoBehaviour
    {
        public const float WarningSeconds = .72f;
        private PaintedEnemyActor _actor;
        private Vector3 _center, _lockedTarget;
        private float _wait = .25f, _warningElapsed;
        private bool _pending;
        private int _pattern;
        private readonly List<PaintedAttackShadow> _warnings = new List<PaintedAttackShadow>();
        private readonly List<PaintedEnemyProjectile> _shots = new List<PaintedEnemyProjectile>();
        private readonly List<GameObject> _effects = new List<GameObject>();
        public int WarningsEmitted { get; private set; }
        public int ActiveWarningCount => _warnings.Count;
        public int AttacksReleased { get; private set; }

        public void Initialize(PaintedEnemyActor actor, Vector3 center)
        { Cancel(); _actor = actor; _center = center; WarningsEmitted = AttacksReleased = 0; }

        public void Tick(float dt, Vector3 target)
        {
            var player = OrpheusPlayerController.Instance;
            if (_actor == null || (_actor.OwnerBoss != null && _actor.OwnerBoss.IsDefeated) ||
                (player != null && (player.IsGameOver || player.IsVictory))) { Cancel(); return; }
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt) || UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            _shots.RemoveAll(shot => shot == null || shot.Removed);
            _effects.RemoveAll(effect => effect == null);
            if (_pending)
            {
                // A stalled frame must not consume the player's entire visible dodge warning.
                _warningElapsed += Mathf.Min(dt, .1f);
                foreach (var warning in _warnings) if (warning != null) warning.Progress(_warningElapsed / WarningSeconds);
                if (_warningElapsed < WarningSeconds) return;
                ClearWarnings(); _pending = false;
                if ((_pattern & 1) == 0)
                {
                    _actor.TryHitPlayer(_lockedTarget, 1.2f, 17f);
                    var effect = _actor.Art.PlayEffect(2, _lockedTarget, 2.4f, .35f);
                    if (effect != null) _effects.Add(effect);
                }
                else
                {
                    Vector3 source = _center + UnderworldCamera2D.IsoRightXZ * 8f + Vector3.up * .65f;
                    var shot = PaintedEnemyProjectile.Spawn(_actor.Kind, 0, source,
                        -UnderworldCamera2D.IsoRightXZ, 10.5f, 16f, .58f, 1.8f);
                    shot.Owner = _actor; _shots.Add(shot);
                }
                AttacksReleased++; _pattern++; _wait = .58f;
                return;
            }
            _wait -= dt;
            if (_wait > 0) return;
            _lockedTarget = _center + UnderworldCamera2D.IsoRightXZ * Mathf.Clamp(
                Vector3.Dot(target - _center, UnderworldCamera2D.IsoRightXZ), -8f, 4.2f);
            _lockedTarget.y = .04f;
            if ((_pattern & 1) == 0) _warnings.Add(PaintedAttackShadow.Create(_lockedTarget, 1.3f, WarningSeconds));
            else for (int i = 0; i <= 12; i++)
                _warnings.Add(PaintedAttackShadow.Create(_center + UnderworldCamera2D.IsoRightXZ * (8f - i * 1.35f), .9f, WarningSeconds));
            WarningsEmitted++; _warningElapsed = 0; _pending = true;
        }

        private void ClearWarnings()
        { foreach (var warning in _warnings) if (warning != null) warning.Dispose(); _warnings.Clear(); }

        public void Cancel()
        {
            ClearWarnings();
            foreach (var shot in _shots) if (shot != null) shot.Remove(); _shots.Clear();
            foreach (var effect in _effects) if (effect != null)
            { if (Application.isPlaying) Destroy(effect); else DestroyImmediate(effect); }
            _effects.Clear(); _pending = false; _warningElapsed = 0; _wait = .25f; _pattern = 0;
        }
        private void OnDisable() => Cancel();
    }
}
