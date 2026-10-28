using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Sky stones always land on positions fixed when their shadows appear.</summary>
    public sealed class MaskedMonkAttack : PaintedAttackPattern
    {
        public const float WarningSeconds = 0.65f;
        public const float FallSeconds = 0.3f;
        public const float ImpactRadius = 2.3f;
        public const float ImpactDamage = 24f;
        public const float RecoverySeconds = 0.4f;
        public const float RestSeconds = 0.45f;
        public const float StaggerSeconds = 0.16f;
        public const float RingRadius = 4.2f;
        public const int MaximumStones = 5;
        private const float FallHeight = 9f;
        public enum StonePattern { StaggeredCluster, SweepingLine, BrokenRing }
        public StonePattern Pattern { get; private set; }
        public int CastCount => _castIndex;
        public int PendingImpactCount
        {
            get
            {
                int count = 0;
                foreach (var stone in _stones) if (!stone.Resolved) count++;
                return count;
            }
        }
        public int ScheduledStoneCount => _stones.Count;
        public Vector3 LockedCenter { get; private set; }
        public Vector3 EscapeDirection { get; private set; }
        public bool IsSummoning => _state == CastState.Summon;
        public Vector3 StoneTarget(int index) => _stones[index].Target;
        public float StoneImpactTime(int index) => _stones[index].DropAt + FallSeconds;

        private sealed class Stone
        {
            public Vector3 Target;
            public float DropAt;
            public PaintedAttackShadow Shadow;
            public GameObject Rock;
            public bool Dropped;
            public bool Resolved;
        }

        private enum CastState { Rest, Summon, Recover }
        private readonly List<Stone> _stones = new List<Stone>(MaximumStones);
        private readonly List<GameObject> _effects = new List<GameObject>(4);
        private CastState _state;
        private float _clock;
        private int _castIndex;
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
            if (Actor == null) return;
            var player = OrpheusPlayerController.Instance;
            if (!isActiveAndEnabled || !Actor.isActiveAndEnabled ||
                player == null || player.IsGameOver || player.IsVictory ||
                player.IsIn2DBossStage != Actor.IsBossStage ||
                (Actor.OwnerShade != null && (Actor.OwnerShade.CurrentHealth <= 0f || Actor.OwnerShade.IsPacified)) ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated ||
                    Actor.OwnerBoss.CurrentPhase != DuelPhase.ArenaCombat)))
            {
                Cancel();
                return;
            }
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt) || Time.timeScale <= 0f ||
                UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;

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

            if (!_summonPoseShown && _clock >= WarningSeconds * .5f)
            {
                _summonPoseShown = true;
                Actor.Art?.Play(Pattern == StonePattern.StaggeredCluster ? EnemyMotion.Attack : EnemyMotion.Attack2,
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
                    KeepUntilCleared(stone.Rock);
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
            Pattern = (StonePattern)(_castIndex++ % 3);
            _summonPoseShown = false;
            Actor.Face(lastKnownPlayerPoint);
            Actor.Art?.Play(EnemyMotion.Windup, WarningSeconds * .5f);
            var charge = EnemyPaintedEffects.Spawn(PaintedEnemyKind.MaskedMonk, 2,
                Actor.transform.position + Vector3.up * 1.6f, 3.2f, WarningSeconds);
            _effects.Add(charge);

            // Every disk is committed together. Only the impact order changes after the warning.
            LockedCenter = lastKnownPlayerPoint;
            LockedCenter = new Vector3(LockedCenter.x, Actor.transform.position.y, LockedCenter.z);
            Vector3 aim = LockedCenter - Actor.transform.position;
            aim.y = 0f;
            if (Actor.IsBossStage)
                aim = UnderworldCamera2D.IsoRightXZ * Vector3.Dot(aim, UnderworldCamera2D.IsoRightXZ);
            EscapeDirection = aim.sqrMagnitude > .001f ? aim.normalized : UnderworldCamera2D.IsoRightXZ;
            Vector3 side = Actor.IsBossStage ? UnderworldCamera2D.IsoRightXZ : Vector3.Cross(Vector3.up, EscapeDirection).normalized;
            if (Pattern == StonePattern.StaggeredCluster)
            {
                AddStone(LockedCenter, 0);
                Vector3 forward = Actor.IsBossStage ? Vector3.zero : EscapeDirection * 1.4f;
                AddStone(LockedCenter + side * 3.2f + forward, 1);
                AddStone(LockedCenter - side * 3.2f + forward, 2);
            }
            else if (Pattern == StonePattern.SweepingLine)
            {
                for (int i = 0; i < MaximumStones; i++)
                    AddStone(LockedCenter + side * ((i - 2) * 3.1f), i);
            }
            else if (Actor.IsBossStage)
            {
                // The duel stage has one movement axis: flank it while leaving its center open.
                AddStone(LockedCenter - side * RingRadius, 0);
                AddStone(LockedCenter + side * RingRadius, 0);
            }
            else
            {
                // Five overlapping disks surround the center; the omitted sixth points away from
                // the caster. Its opening is wider than a player, and is never filled by projection.
                for (int i = 1; i <= MaximumStones; i++)
                {
                    float angle = i * Mathf.PI / 3f;
                    Vector3 target = LockedCenter + (EscapeDirection * Mathf.Cos(angle) + side * Mathf.Sin(angle)) * RingRadius;
                    AddStone(target, 0, preserveGeometry: true);
                }
            }
        }

        private void AddStone(Vector3 target, int order, bool preserveGeometry = false)
        {
            if (_stones.Count >= MaximumStones) return;
            if (Actor.IsBossStage)
            {
                Vector3 side = UnderworldCamera2D.IsoRightXZ;
                float along = Vector3.Dot(target - Actor.StageCenter, side);
                target += side * (Mathf.Clamp(along, -8.2f, 4.2f) - along);
                // A flanking ring must not collapse its warned escape against a stage boundary.
                if (Pattern == StonePattern.BrokenRing && Vector3.Distance(target, LockedCenter) < ImpactRadius + .7f) return;
            }
            else if (PaintedChamberMap.Active != null)
            {
                if (preserveGeometry)
                {
                    if (!PaintedChamberMap.Active.IsWalkable(target, .4f)) return;
                }
                else target = PaintedChamberMap.Active.NearestWalkable(target, .4f);
            }
            // Narrow passages may project several candidates onto one spot. Never stack impacts.
            foreach (var stone in _stones)
                if ((stone.Target - target).sqrMagnitude < 1f) return;
            float dropAt = WarningSeconds + StaggerSeconds * order;
            _stones.Add(new Stone
            {
                Target = target,
                DropAt = dropAt,
                Shadow = PaintedAttackShadow.Create(target, ImpactRadius, dropAt + FallSeconds + .1f)
            });
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
            LockedCenter = Vector3.zero;
            EscapeDirection = Vector3.zero;
            base.Cancel();
        }

        private void ClearOwnedVisuals()
        {
            foreach (var stone in _stones) ClearStoneVisuals(stone);
            _stones.Clear();
            ClearEffects();
        }

        private void ClearEffects()
        {
            foreach (var effect in _effects) if (effect != null) RemoveVisual(effect);
            _effects.Clear();
        }

        private static void KeepUntilCleared(GameObject visual)
        {
            if (visual != null) visual.GetComponent<EnemyPaintedEffects>().Loop = true;
        }

        private static void ClearStoneVisuals(Stone stone)
        {
            stone.Shadow?.Dispose();
            stone.Shadow = null;
            if (stone.Rock != null) RemoveVisual(stone.Rock);
            stone.Rock = null;
        }

        protected override void OnDisable() { Cancel(); }
        private static void RemoveVisual(GameObject visual)
        { visual.SetActive(false); if(Application.isPlaying) Destroy(visual); else DestroyImmediate(visual); }
    }
}
