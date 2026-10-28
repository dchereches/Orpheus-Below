using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>A small flock alternates with committed charges and a bell fan with open escape lanes.</summary>
    public sealed class ShepherdAttack : PaintedAttackPattern
    {
        public const int MaximumLivingSheep = 3;
        public const float RestSeconds = .5f;
        public const float WarningSeconds = .85f;
        public const float RecoverySeconds = .6f;
        public const float BellSpeed = 7f;
        public const float BellDamage = 13f;
        public const float BellRadius = .4f;
        public const float BellHeight = .7f;
        public const float BellLifetime = 1.6f;
        public const float BellSpreadDegrees = 28f;
        private const float SummonSeconds = .3f;
        private const float CommandSeconds = .3f;
        public enum BellPhase { Rest, Warning, Summon, Command, Recover }
        public enum BellCall { CallFlock, RallyCharge, BellVolley }
        public BellPhase Phase { get; private set; }
        public BellCall Variant { get; private set; }
        public int CastCount { get; private set; }
        public int CommandCount { get; private set; }
        public int VolleyCount { get; private set; }
        public float PhaseElapsed => _elapsed;
        public int WarningCount => _warnings.Count;
        public Vector3 LockedDirection { get; private set; }
        public Vector3 VolleyOrigin { get; private set; }
        public int ActiveProjectileCount
        {
            get
            {
                int count = 0;
                foreach (var bell in _bells) if (bell != null && !bell.Removed) count++;
                return count;
            }
        }
        public int PendingSummons => _positions.Count;
        public int LivingSheep
        {
            get
            {
                int count = 0;
                foreach (var owner in FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None))
                    if (owner.Owner == Actor && IsLiving(owner.gameObject)) count++;
                return count;
            }
        }

        private float _elapsed;
        private readonly List<Vector3> _positions = new List<Vector3>(2);
        private readonly List<PaintedAttackShadow> _warnings = new List<PaintedAttackShadow>(2);
        private readonly List<GameObject> _effects = new List<GameObject>(8);
        private readonly List<PaintedEnemyProjectile> _bells = new List<PaintedEnemyProjectile>(3);
        private Vector3 _lockedTarget;

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
            CastCount = CommandCount = VolleyCount = 0;
        }

        public override void Tick(float dt)
        {
            if (Actor == null) return;
            var player = OrpheusPlayerController.Instance;
            if (!isActiveAndEnabled || !Actor.isActiveAndEnabled || player == null || player.IsGameOver || player.IsVictory ||
                (Actor.OwnerShade != null && (Actor.OwnerShade.IsStunned || Actor.OwnerShade.IsPacified ||
                    Actor.OwnerShade.IsFleeing || Actor.OwnerShade.CurrentHealth <= 0)) ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated || Actor.OwnerBoss.CurrentPhase != DuelPhase.ArenaCombat)))
            { Cancel(); return; }
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt) || Time.timeScale <= 0 ||
                UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            _bells.RemoveAll(bell => bell == null || bell.Removed);
            _elapsed += Mathf.Min(dt, .1f);
            switch (Phase)
            {
                case BellPhase.Rest:
                    Actor.Art?.Play(EnemyMotion.Idle, RestSeconds, true);
                    if (_elapsed >= RestSeconds) BeginBell(player.transform.position);
                    break;
                case BellPhase.Warning:
                    foreach (var warning in _warnings) warning?.Progress(_elapsed / WarningSeconds);
                    if (_elapsed >= WarningSeconds)
                    {
                        ClearWarnings();
                        if (Variant == BellCall.CallFlock) Summon();
                        else if (Variant == BellCall.RallyCharge) CommandFlock(_lockedTarget);
                        else FireBells();
                    }
                    break;
                case BellPhase.Summon:
                    if (_elapsed >= SummonSeconds) CommandFlock(_lockedTarget);
                    break;
                case BellPhase.Command:
                    if (_elapsed >= CommandSeconds)
                    {
                        Phase = BellPhase.Recover; _elapsed = 0;
                        Actor.Art?.Play(EnemyMotion.Recover, RecoverySeconds);
                    }
                    break;
                case BellPhase.Recover:
                    if (_elapsed >= RecoverySeconds) { Phase = BellPhase.Rest; _elapsed = 0; }
                    break;
            }
        }

        private void BeginBell(Vector3 target)
        {
            ClearWarnings(); ClearEffects(); _positions.Clear();
            Phase = BellPhase.Warning; _elapsed = 0;
            _lockedTarget = target;
            Vector3 aim = target - Actor.transform.position; aim.y = 0;
            if (Actor.IsBossStage) aim = UnderworldCamera2D.IsoRightXZ * Vector3.Dot(aim, UnderworldCamera2D.IsoRightXZ);
            LockedDirection = aim.sqrMagnitude > .001f ? aim.normalized : UnderworldCamera2D.IsoRightXZ;
            VolleyOrigin = Actor.transform.position;
            Variant = (BellCall)(CastCount++ % 3);
            // Neither a full flock nor a temporarily unavailable flock may leave an empty cast.
            if ((Variant == BellCall.CallFlock && LivingSheep >= MaximumLivingSheep) ||
                (Variant == BellCall.RallyCharge && LivingSheep == 0)) Variant = BellCall.BellVolley;
            Actor.Face(target);
            Actor.Art?.Play(EnemyMotion.Windup, WarningSeconds);
            _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.Shepherd, 0,
                Actor.transform.position + Vector3.up * 1.4f, 2.5f, WarningSeconds));
            if (Variant == BellCall.BellVolley) { WarnBells(); return; }
            if (Variant == BellCall.RallyCharge)
            {
                foreach (var owner in FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None))
                    if (owner.Owner == Actor && IsLiving(owner.gameObject))
                        _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.Shepherd, 3,
                            owner.transform.position, 1.7f, WarningSeconds, true));
                return;
            }
            int needed = Mathf.Min(2, MaximumLivingSheep - LivingSheep);
            for (int i = 0; i < needed; i++)
            {
                if (!TrySummonPosition(i, target, out var position)) continue;
                _positions.Add(position);
                _warnings.Add(PaintedAttackShadow.Create(position, .85f, WarningSeconds));
                _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.Shepherd, 3,
                    position, 1.7f, WarningSeconds, true));
            }
            if (_positions.Count == 0) { Variant = BellCall.BellVolley; WarnBells(); }
        }

        private Vector3 BellDirection(int index) => Quaternion.AngleAxis((index - 1) * BellSpreadDegrees,
            Vector3.up) * LockedDirection;

        private void WarnBells()
        {
            // Every projectile starts at the committed bell position. Warning disks cover its
            // complete swept capsule; the spaces between the three rays stay open for movement.
            float length = BellSpeed * BellLifetime;
            int intervals = Mathf.CeilToInt(length / (BellRadius * 1.8f));
            float warningRadius = Mathf.Sqrt(BellRadius * BellRadius + Mathf.Pow(length / intervals * .5f, 2));
            for (int lane = 0; lane < 3; lane++)
                for (int i = 0; i <= intervals; i++)
                    _warnings.Add(PaintedAttackShadow.Create(VolleyOrigin + BellDirection(lane) * (length * i / intervals),
                        warningRadius, WarningSeconds));
        }

        private void FireBells()
        {
            ClearWarnings();
            Phase = BellPhase.Command; _elapsed = 0;
            Actor.Art?.Play(EnemyMotion.Attack2, CommandSeconds);
            for (int i = 0; i < 3; i++)
            {
                var bell = PaintedEnemyProjectile.Spawn(PaintedEnemyKind.Shepherd, 1,
                    VolleyOrigin + Vector3.up * BellHeight, BellDirection(i), BellSpeed,
                    BellDamage, BellRadius, BellLifetime);
                bell.Owner = Actor;
                _bells.Add(bell);
            }
            VolleyCount++;
        }

        private bool TrySummonPosition(int ordinal, Vector3 player, out Vector3 point)
        {
            Vector3 home = Actor.transform.position;
            var occupiedPositions = new List<Vector3>(_positions);
            foreach (var owner in FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None))
                if (owner.Owner == Actor && IsLiving(owner.gameObject)) occupiedPositions.Add(owner.transform.position);
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3 candidate;
                if (Actor.IsBossStage)
                {
                    Vector3 axis = UnderworldCamera2D.IsoRightXZ;
                    float side = ((attempt + ordinal) & 1) == 0 ? -1 : 1;
                    candidate = home + axis * (side * (2.4f + .8f * (attempt / 2)));
                    float along = Vector3.Dot(candidate - Actor.StageCenter, axis);
                    candidate += axis * (Mathf.Clamp(along, -8.2f, 4.2f) - along);
                }
                else
                {
                    float angle = (ordinal * 180f + attempt * 47f) * Mathf.Deg2Rad;
                    candidate = home + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (3f + attempt * .18f);
                    var map = PaintedChamberMap.Active;
                    if (map != null && !PaintedEnemyNavigation.TryFindSpawn(map, home, candidate, occupiedPositions, out candidate)) continue;
                    Vector3 homeGap = candidate - home; homeGap.y = 0;
                    if (homeGap.sqrMagnitude < 9f) continue;
                }
                candidate.y = home.y;
                Vector3 playerGap = candidate - player; playerGap.y = 0;
                if (playerGap.sqrMagnitude < 6.25f) continue;
                if (PaintedChamberMap.Active != null && !Actor.IsBossStage && !PaintedChamberMap.Active.IsWalkable(candidate, .6f)) continue;
                Vector3 fromHome = candidate - home;
                if (!Actor.IsBossStage && PaintedChamberMap.Active == null &&
                    ChamberCollision.SweepObstacle(home + Vector3.up * .5f, fromHome, .5f, out _)) continue;
                bool occupied = false;
                foreach (var planned in occupiedPositions) if ((planned - candidate).sqrMagnitude < 2.25f) occupied = true;
                if (occupied) continue;
                point = candidate; return true;
            }
            point = home; return false;
        }

        private void Summon()
        {
            ClearWarnings();
            Phase = BellPhase.Summon; _elapsed = 0;
            Actor.Art?.Play(EnemyMotion.Attack, SummonSeconds);
            var player = OrpheusPlayerController.Instance;
            foreach (var position in _positions)
            {
                // The player may enter a warned spawn: skip it rather than materialize an animal inside them.
                if (LivingSheep >= MaximumLivingSheep) break;
                if (player != null && (player.transform.position - position).sqrMagnitude < 2.25f) continue;
                var go = new GameObject("Shepherd_SummonedSheep");
                go.transform.position = position;
                // Ownership handles cleanup; inheriting a pursuing parent's transform bypasses sheep collision.
                go.transform.SetParent(Actor.transform.parent, true);
                go.AddComponent<PaintedSummonOwnership>().Owner = Actor;
                var shade = go.AddComponent<ShadeEnemy>();
                shade.SetupArchetype(ShadeArchetype.SummonedSheep);
                _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.Shepherd, 1, position, 2.3f, SummonSeconds));
            }
            _positions.Clear();
        }

        private void CommandFlock(Vector3 target)
        {
            Phase = BellPhase.Command; _elapsed = 0;
            Actor.Face(target); Actor.Art?.Play(EnemyMotion.Attack2, CommandSeconds);
            CommandCount++;
            foreach (var owner in FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None))
                    if (owner.Owner == Actor && IsLiving(owner.gameObject)) owner.GetComponent<SheepAttack>()?.RequestCommand(target);
        }

        private static bool IsLiving(GameObject go)
        {
            var shade = go.GetComponent<ShadeEnemy>();
            return go.activeInHierarchy && shade != null && !shade.IsPacified && !shade.IsFleeing && shade.CurrentHealth > 0;
        }

        public override void Cancel()
        {
            ClearWarnings(); ClearEffects(); _positions.Clear();
            foreach (var bell in _bells) if (bell != null) bell.Remove();
            _bells.Clear();
            if (Actor != null)
                foreach (var summon in FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None))
                    if (summon.Owner == Actor) summon.Remove();
            Phase = BellPhase.Rest; _elapsed = 0;
            LockedDirection = VolleyOrigin = _lockedTarget = Vector3.zero;
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
