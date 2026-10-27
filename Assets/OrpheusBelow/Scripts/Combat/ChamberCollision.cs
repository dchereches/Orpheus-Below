using UnityEngine;

namespace OrpheusBelow
{
    public static class ChamberCollision
    {
        public static bool SweepObstacle(Vector3 origin, Vector3 displacement, float radius, out RaycastHit nearest)
        {
            nearest = default;
            float distance = displacement.magnitude;
            if (distance < .00001f) return false;
            float best = float.PositiveInfinity;
            foreach (var hit in Physics.SphereCastAll(origin, radius, displacement / distance,
                distance + .02f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponent<IsoObstacle>() == null || hit.distance >= best) continue;
                nearest = hit; best = hit.distance;
            }
            return best < float.PositiveInfinity;
        }

        public static Collider ProjectileHit(Vector3 origin, Vector3 displacement, float radius, bool enemy, out float distance)
        {
            distance = float.PositiveInfinity;
            Collider nearest = null;
            // Sphere casts do not report an initial overlap: an orb spawned in cover must stop immediately.
            foreach (var collider in Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var obstacle = collider.GetComponent<IsoObstacle>();
                if (obstacle != null && obstacle.kind == IsoObstacleKind.SolidWall) { distance = 0f; return collider; }
            }
            float travel = displacement.magnitude;
            if (travel < .00001f) return null;
            foreach (var hit in Physics.SphereCastAll(origin, radius, displacement / travel, travel, ~0, QueryTriggerInteraction.Collide))
            {
                var obstacle = hit.collider.GetComponent<IsoObstacle>();
                bool solid = obstacle != null && obstacle.kind == IsoObstacleKind.SolidWall;
                bool target = enemy ? hit.collider.GetComponent<OrpheusPlayerController>() != null
                    : hit.collider.GetComponentInParent<IOrphicTarget>() != null;
                if ((!solid && !target) || hit.distance >= distance) continue;
                nearest = hit.collider; distance = hit.distance;
            }
            return nearest;
        }
    }
}
