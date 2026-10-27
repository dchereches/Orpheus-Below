using UnityEngine;

namespace OrpheusBelow
{
    public sealed class PaintedEnemyProjectile : MonoBehaviour
    {
        public PaintedEnemyActor Owner;
        public bool Removed { get; private set; }
        public PaintedEnemyKind Kind { get; private set; }
        private Vector3 _direction;
        private float _speed, _damage, _radius, _life;
        public static PaintedEnemyProjectile Spawn(PaintedEnemyKind kind, int fxRow, Vector3 position, Vector3 direction, float speed, float damage, float radius = .25f, float lifetime = 4f)
        {
            var go = new GameObject(kind + "_PaintedProjectile"); go.transform.position = position;
            var projectile = go.AddComponent<PaintedEnemyProjectile>();
            projectile.Kind = kind; projectile.Owner = PaintedEnemyActor.CurrentAttackOwner;
            direction.y = 0; projectile._direction = direction.sqrMagnitude > .0001f ? direction.normalized : UnderworldCamera2D.IsoRightXZ;
            projectile._speed = speed; projectile._damage = damage; projectile._radius = radius; projectile._life = lifetime;
            var collider = go.AddComponent<SphereCollider>(); collider.isTrigger = true; collider.radius = radius;
            float size = kind == PaintedEnemyKind.MysticEyeWarden ? 2.5f : kind == PaintedEnemyKind.Tripod ? 2.6f : .7f;
            var art = EnemyPaintedEffects.Spawn(kind, fxRow, position, size, lifetime);
            if (art != null)
            {
                art.transform.SetParent(go.transform, true);
                art.GetComponent<EnemyPaintedEffects>().Loop = true;
            }
            return projectile;
        }
        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory) { Remove(); return; }
            Advance(Time.deltaTime);
        }
        public void Advance(float dt)
        {
            if (Removed || dt <= 0) return;
            var player = OrpheusPlayerController.Instance;
            Vector3 delta = _direction * (_speed * dt);
            float first = float.PositiveInfinity; Collider collider = null;
            foreach (var initial in Physics.OverlapSphere(transform.position, _radius, ~0, QueryTriggerInteraction.Ignore))
                if (initial.TryGetComponent<IsoObstacle>(out var solid) && solid.kind == IsoObstacleKind.SolidWall && !OutsideDuelFloor(initial)) { Remove(); return; }
            foreach (var hit in Physics.SphereCastAll(transform.position, _radius, _direction, delta.magnitude, ~0, QueryTriggerInteraction.Collide))
            {
                bool solid = hit.collider.TryGetComponent<IsoObstacle>(out var obstacle) && obstacle.kind == IsoObstacleKind.SolidWall && !OutsideDuelFloor(hit.collider);
                bool target = player != null && !player.IsDashing && hit.collider.GetComponent<OrpheusPlayerController>() == player;
                if ((solid || target) && hit.distance < first) { first = hit.distance; collider = hit.collider; }
            }
            if (collider != null)
            {
                transform.position += _direction * Mathf.Max(0, first - .01f);
                if (player != null && !player.IsDashing && collider.GetComponent<OrpheusPlayerController>() == player)
                    player.TakeDamage(_damage, transform.position);
                EnemyPaintedEffects.Spawn(Kind, 2, transform.position, .9f, .3f); Remove(); return;
            }
            transform.position += delta; _life -= dt; if (_life <= 0) Remove();
        }
        private bool OutsideDuelFloor(Collider obstacle) => Owner != null && Owner.IsBossStage && obstacle.GetComponentInParent<PaintedChamberMap>() != null;
        public void Remove()
        {
            if (Removed) return; Removed = true;
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
        public static void ClearAll()
        {
            foreach (var projectile in FindObjectsByType<PaintedEnemyProjectile>(FindObjectsSortMode.None)) projectile.Remove();
        }
    }
}
