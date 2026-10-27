using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>A deliberately small flock. Summon positions are locked before the bell rings.</summary>
    public sealed class ShepherdAttack : PaintedAttackPattern
    {
        public const int MaximumLivingSheep = 3;
        public const float RestSeconds = 2f;
        public const float WarningSeconds = 1.2f;
        public const float RecoverySeconds = 1.4f;
        private const float SummonSeconds = .65f;
        private const float CommandSeconds = .65f;
        public enum BellPhase { Rest, Warning, Summon, Command, Recover }
        public BellPhase Phase { get; private set; }
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

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
        }

        public override void Tick(float dt)
        {
            if (Actor == null || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory ||
                (Actor.OwnerBoss != null && (Actor.OwnerBoss.IsDefeated || Actor.OwnerBoss.CurrentPhase != DuelPhase.ArenaCombat)))
            { Cancel(); return; }
            _elapsed += Mathf.Min(dt, .1f);
            switch (Phase)
            {
                case BellPhase.Rest:
                    Actor.Art?.Play(EnemyMotion.Idle, RestSeconds, true);
                    if (_elapsed >= RestSeconds) BeginBell(player.transform.position);
                    break;
                case BellPhase.Warning:
                    foreach (var warning in _warnings) warning?.Progress(_elapsed / WarningSeconds);
                    if (_elapsed >= WarningSeconds) Summon();
                    break;
                case BellPhase.Summon:
                    if (_elapsed >= SummonSeconds) CommandFlock(player.transform.position);
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
            Actor.Face(target);
            Actor.Art?.Play(EnemyMotion.Windup, WarningSeconds);
            _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.Shepherd, 0,
                Actor.transform.position + Vector3.up * 1.4f, 2.5f, WarningSeconds));
            int needed = Mathf.Min(2, MaximumLivingSheep - LivingSheep);
            for (int i = 0; i < needed; i++)
            {
                if (!TrySummonPosition(i, target, out var position)) continue;
                _positions.Add(position);
                _warnings.Add(PaintedAttackShadow.Create(position, .85f, WarningSeconds));
                _effects.Add(EnemyPaintedEffects.Spawn(PaintedEnemyKind.Shepherd, 3,
                    position, 1.7f, WarningSeconds, true));
            }
        }

        private bool TrySummonPosition(int ordinal, Vector3 player, out Vector3 point)
        {
            Vector3 home = Actor.transform.position;
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
                    candidate = home + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (2.6f + attempt * .18f);
                }
                candidate.y = home.y;
                Vector3 playerGap = candidate - player; playerGap.y = 0;
                if (playerGap.sqrMagnitude < 6.25f) continue;
                if (PaintedChamberMap.Active != null && !Actor.IsBossStage && !PaintedChamberMap.Active.IsWalkable(candidate, .6f)) continue;
                Vector3 fromHome = candidate - home;
                if (!Actor.IsBossStage && ChamberCollision.SweepObstacle(home + Vector3.up * .5f, fromHome, .5f, out _)) continue;
                bool occupied = false;
                foreach (var planned in _positions) if ((planned - candidate).sqrMagnitude < 2.25f) occupied = true;
                foreach (var owner in FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None))
                    if (owner.Owner == Actor && IsLiving(owner.gameObject) && (owner.transform.position - candidate).sqrMagnitude < 2.25f) occupied = true;
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
            if (Actor != null)
                foreach (var summon in FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None))
                    if (summon.Owner == Actor) summon.Remove();
            Phase = BellPhase.Rest; _elapsed = 0;
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
