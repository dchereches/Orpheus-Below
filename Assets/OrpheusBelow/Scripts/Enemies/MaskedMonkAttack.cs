using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Sky stones always land on positions fixed when their shadows appear.</summary>
    public sealed class MaskedMonkAttack : PaintedAttackPattern
    {
        public const float WarningSeconds = 1.35f;
        public const float FallSeconds = 0.45f;
        public const float ImpactRadius = 2.3f;
        public const float ImpactDamage = 24f;
        public const float RecoverySeconds = 1.05f;
        private const float StaggerSeconds = 0.35f;
        private const float FallHeight = 9f;
        private const float RestSeconds = 1.2f;

        private sealed class Stone
        {
            public Vector3 Target;
            public float DropAt;
            public PaintedAttackShadow Shadow;
            public GameObject PaintedShadow;
            public GameObject Rock;
            public bool Dropped;
            public bool Resolved;
        }

        private enum CastState { Rest, Summon, Recover }
        private readonly List<Stone> _stones = new List<Stone>(3);
        private readonly List<GameObject> _effects = new List<GameObject>(4);
        private CastState _state;
        private float _clock;
        private int _castIndex;
        private bool _grandSummon;
        private bool _summonPoseShown;
        public int CompletedImpacts { get; private set; }

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
            _castIndex = 0;
        }

        public override void Tick(float dt)
        {
            if (Actor == null || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated ||
                    Actor.OwnerBoss.CurrentPhase != DuelPhase.ArenaCombat)))
            {
                Cancel();
                return;
            }

            // A long frame may extend a warning, but must never skip straight through it.
            _clock += Mathf.Min(dt, 0.1f);
            if (_state == CastState.Rest)
            {
                if (_clock >= RestSeconds) BeginSummon(player.transform.position);
                return;
            }
            if (_state == CastState.Recover)
            {
                if (_clock >= RecoverySeconds)
                {
                    _state = CastState.Rest;
                    _clock = 0f;
                    Actor.Art?.Play(EnemyMotion.Idle, RestSeconds, true);
                }
                return;
            }

            if (!_summonPoseShown && _clock >= 0.55f)
            {
                _summonPoseShown = true;
                Actor.Art?.Play(_grandSummon ? EnemyMotion.Attack2 : EnemyMotion.Attack,
                    WarningSeconds + FallSeconds + StaggerSeconds * (_stones.Count - 1) - _clock);
            }
            bool finished = true;
            for (int stoneIndex = 0; stoneIndex < _stones.Count; stoneIndex++)
            {
                var stone = _stones[stoneIndex];
                if (stone.Resolved) continue;
                finished = false;
                stone.Shadow?.Progress(Mathf.Clamp01(_clock / (stone.DropAt + FallSeconds)));
                if (_clock < stone.DropAt) continue;
                if (!stone.Dropped)
                {
                    stone.Dropped = true;
                    stone.Rock = EnemyPaintedEffects.Spawn(PaintedEnemyKind.MaskedMonk, 0,
                        stone.Target + Vector3.up * FallHeight, 3.4f, FallSeconds + 0.2f);
                    Actor.Art?.Play(EnemyMotion.Attack, FallSeconds);
                }
                float fall = Mathf.Clamp01((_clock - stone.DropAt) / FallSeconds);
                if (stone.Rock != null)
                    stone.Rock.transform.position = stone.Target + Vector3.up * (FallHeight * (1f - fall * fall));
                if (fall < 1f) continue;

                // Resolve before damage: callbacks cannot cause a second hit from this stone.
                stone.Resolved = true;
                CompletedImpacts++;
                ClearStoneVisuals(stone);
                _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.MaskedMonk, 1,
                    stone.Target, ImpactRadius * 2f, 0.65f));
                Actor.TryHitPlayer(stone.Target, ImpactRadius, ImpactDamage);
                if (_state != CastState.Summon) return;
            }
            if (finished) BeginRecovery();
        }

        private void BeginSummon(Vector3 lastKnownPlayerPoint)
        {
            ClearOwnedVisuals();
            _state = CastState.Summon;
            _clock = 0f;
            bool grand = (++_castIndex % 2) == 0;
            _grandSummon = grand;
            _summonPoseShown = false;
            Actor.Face(lastKnownPlayerPoint);
            Actor.Art?.Play(EnemyMotion.Windup, 0.55f);
            _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.MaskedMonk, 2,
                Actor.transform.position + Vector3.up * 1.6f, 3.2f, WarningSeconds));

            // The three disks appear together; none follow the player after this point.
            int count = grand ? 3 : 1;
            Vector3 side = Actor.IsBossStage ? UnderworldCamera2D.IsoRightXZ : Vector3.right;
            for (int i = 0; i < count; i++)
            {
                float offset = grand ? (i - 1) * 3.1f : 0f;
                Vector3 target = lastKnownPlayerPoint + side * offset;
                target.y = Actor.transform.position.y;
                if (Actor.IsBossStage)
                {
                    float alongStage = Vector3.Dot(target - Actor.StageCenter, side);
                    target += side * (Mathf.Clamp(alongStage, -8.2f, 4.2f) - alongStage);
                }
                else if(PaintedChamberMap.Active != null) target = PaintedChamberMap.Active.NearestWalkable(target,.4f);
                float dropAt = WarningSeconds + StaggerSeconds * i;
                _stones.Add(new Stone
                {
                    Target = target,
                    DropAt = dropAt,
                    Shadow = PaintedAttackShadow.Create(target, ImpactRadius, dropAt + FallSeconds + 0.1f),
                    PaintedShadow = EnemyPaintedEffects.Spawn(PaintedEnemyKind.MaskedMonk, 3,
                        target, ImpactRadius * 2f, dropAt + FallSeconds + 0.1f, true)
                });
            }
        }

        private void BeginRecovery()
        {
            _stones.Clear();
            _state = CastState.Recover;
            _clock = 0f;
            Actor.Art?.Play(EnemyMotion.Recover, RecoverySeconds);
        }

        public override void Cancel()
        {
            ClearOwnedVisuals();
            _state = CastState.Rest;
            _clock = 0f;
            _summonPoseShown = false;
            base.Cancel();
        }

        private void ClearOwnedVisuals()
        {
            foreach (var stone in _stones) ClearStoneVisuals(stone);
            _stones.Clear();
            foreach (var effect in _effects) if (effect != null) RemoveVisual(effect);
            _effects.Clear();
        }

        private static void ClearStoneVisuals(Stone stone)
        {
            stone.Shadow?.Dispose();
            stone.Shadow = null;
            if (stone.PaintedShadow != null) RemoveVisual(stone.PaintedShadow);
            if (stone.Rock != null) RemoveVisual(stone.Rock);
            stone.PaintedShadow = null;
            stone.Rock = null;
        }

        protected override void OnDisable() { Cancel(); }
        private static void RemoveVisual(GameObject visual)
        { if(Application.isPlaying) Destroy(visual); else DestroyImmediate(visual); }
    }
}
