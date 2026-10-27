using UnityEngine;

namespace OrpheusBelow
{
    public sealed class PaintedEnemyActor : MonoBehaviour
    {
        public PaintedEnemyKind Kind { get; private set; }
        public EnemyArtAnimator Art { get; private set; }
        public PaintedAttackPattern Pattern { get; private set; }
        public ShadeEnemy OwnerShade { get; private set; }
        public CerberusRhythmBoss OwnerBoss { get; private set; }
        public Vector3 GroundAnchor { get; private set; }
        public Vector3 StageCenter { get; private set; }
        public bool IsBossStage => (OwnerBoss != null || Kind == PaintedEnemyKind.Sheep) && OrpheusPlayerController.Instance != null && OrpheusPlayerController.Instance.IsIn2DBossStage;
        public static PaintedEnemyActor CurrentAttackOwner { get; private set; }
        private bool _cancelled;
        public void Configure(PaintedEnemyKind kind, ShadeEnemy shade = null, CerberusRhythmBoss boss = null, Vector3? stageCenter = null, bool createPattern = true)
        {
            Kind = kind; OwnerShade = shade; OwnerBoss = boss; GroundAnchor = transform.position; StageCenter = stageCenter ?? transform.position;
            if (kind == PaintedEnemyKind.Sheep && TryGetComponent<PaintedSummonOwnership>(out var summon) && summon.Owner != null)
                StageCenter = summon.Owner.StageCenter;
            var library = EnemyArtLibrary.Load(kind);
            if (Art == null)
            {
                var visual = new GameObject("PaintedEnemyBody"); visual.transform.SetParent(transform, false);
                Art = visual.AddComponent<EnemyArtAnimator>();
            }
            Art.Configure(library);
            if (GetComponent<PaintedActorDepth>() == null) gameObject.AddComponent<PaintedActorDepth>();
            var type = typeof(PaintedEnemyActor).Assembly.GetType("OrpheusBelow." + kind + "Attack");
            if (createPattern && type != null && Pattern == null) Pattern = (PaintedAttackPattern)gameObject.AddComponent(type);
            Pattern?.Initialize(this); _cancelled = false;
        }
        public void Tick(float dt)
        {
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory) { Cancel(); return; }
            if (dt <= 0 || UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            _cancelled = false; CurrentAttackOwner = this;
            try { Pattern?.Tick(dt); } finally { CurrentAttackOwner = null; }
        }
        public void Cancel(bool defeated = false)
        {
            if (!_cancelled) { Pattern?.Cancel(); _cancelled = true; }
            foreach (var projectile in FindObjectsByType<PaintedEnemyProjectile>(FindObjectsSortMode.None))
                if (projectile.Owner == this) projectile.Remove();
            foreach (var summon in FindObjectsByType<PaintedSummonOwnership>(FindObjectsSortMode.None))
                if (summon.Owner == this) summon.Remove();
            if (defeated) Art?.Play(EnemyMotion.Defeated, .85f);
        }
        private void OnDisable() { Cancel(); }
        public void Face(Vector3 target)
        {
            if (Art?.Renderer == null) return;
            float side = Vector3.Dot(target - transform.position, UnderworldCamera2D.IsoRightXZ);
            if (Mathf.Abs(side) > .1f) Art.Renderer.flipX = side < 0;
        }
        public void MoveToward(Vector3 goal, float speed, float dt)
        {
            if (dt <= 0) return;
            goal.y = transform.position.y;
            Vector3 delta = Vector3.ClampMagnitude(goal - transform.position, Mathf.Max(0, speed) * dt);
            if (IsBossStage)
            {
                Vector3 next = transform.position + UnderworldCamera2D.IsoRightXZ * Vector3.Dot(delta, UnderworldCamera2D.IsoRightXZ);
                float along = Vector3.Dot(next - StageCenter, UnderworldCamera2D.IsoRightXZ);
                transform.position = next + UnderworldCamera2D.IsoRightXZ * (Mathf.Clamp(along, -9.5f, 7.2f) - along);
                return;
            }
            Vector3 at = transform.position;
            if (ChamberCollision.SweepObstacle(at + Vector3.up * .5f, delta, .4f, out var hit))
                delta = delta.normalized * Mathf.Max(0, hit.distance - .025f);
            var map = PaintedChamberMap.Active;
            if (map == null || map.IsWalkable(at + delta, .4f)) transform.position = at + delta;
        }
        public bool TryHitPlayer(Vector3 center, float radius, float damage)
        {
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsDashing || player.IsGameOver || player.IsVictory || UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return false;
            Vector3 gap = player.transform.position - center; gap.y = 0;
            if (gap.magnitude > radius) return false;
            if (!IsBossStage && ChamberCollision.SweepObstacle(center + Vector3.up * .65f, gap, .08f, out _)) return false;
            player.TakeDamage(damage, center); return true;
        }
    }

    public sealed class PaintedSummonOwnership : MonoBehaviour
    {
        public PaintedEnemyActor Owner;
        public void Remove()
        {
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
    }
}
