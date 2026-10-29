using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Shared painted footprint placement for the exploration floor and the low boss camera.</summary>
    public static class GroundShadowPaint
    {
        private static Material _material;

        public static SpriteRenderer Create(Transform owner, string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(owner, false);
            var renderer = RenderPipeline2DSetup.AttachGroundDecal(go, RenderPipeline2DSetup.CircleSprite, color, order);
            if (_material == null)
                _material = new Material(Shader.Find("OrpheusBelow/GroundShadow")) { name = "PaintedGroundShadow" };
            renderer.sharedMaterial = _material;
            owner.GetComponent<PaintedActorDepth>()?.Exclude(renderer);
            return renderer;
        }

        public static void Place(SpriteRenderer renderer, Transform owner, Vector2 scale, Vector2 direction,
            float offset, float lift, bool stage = false)
        {
            float blend = UnderworldCamera2D.Instance != null ? UnderworldCamera2D.Instance.SideViewBlend01 : stage ? 1f : 0f;
            blend = Mathf.SmoothStep(0f, 1f, blend);
            Vector3 groundDirection = UnderworldCamera2D.IsoRightXZ * direction.x + UnderworldCamera2D.IsoUpXZ * direction.y;
            groundDirection.y = 0;
            groundDirection = groundDirection.sqrMagnitude > .0001f ? groundDirection.normalized : UnderworldCamera2D.IsoRightXZ;
            Quaternion floor = Quaternion.AngleAxis(Mathf.Atan2(-groundDirection.z, groundDirection.x) * Mathf.Rad2Deg, Vector3.up)
                * RenderPipeline2DSetup.GroundDecalRotation;
            // Keep a shallow ellipse visible as the camera approaches floor level.
            Quaternion billboard = UnderworldCamera2D.CurrentBillboardRotation;
            Quaternion side = billboard * Quaternion.Euler(0, 0, Mathf.Atan2(direction.y * .12f, direction.x) * Mathf.Rad2Deg);
            renderer.transform.rotation = Quaternion.Slerp(floor, side, blend);
            renderer.transform.localScale = new Vector3(scale.x, scale.y * Mathf.Lerp(1f, .32f, blend), 1f);
            Vector3 foot = owner.position; foot.y = lift;
            Vector3 sideDirection = billboard * new Vector3(direction.x, direction.y * .12f, 0f);
            renderer.transform.position = foot + Vector3.Lerp(groundDirection, sideDirection, blend) * offset;
            // Depth sorting must never turn translucent paint into a depth-writing body cutout.
            owner.GetComponent<PaintedActorDepth>()?.Exclude(renderer);
        }
    }
}
