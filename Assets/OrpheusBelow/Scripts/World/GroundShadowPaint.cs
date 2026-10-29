using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Shared painted footprint placement for the exploration floor and the low boss camera.</summary>
    public static class GroundShadowPaint
    {
        private static Material _material;

        public static SpriteRenderer CreateStageFloor(Transform owner)
            => Create(owner, "StageFloorPool", new Color(.14f, .16f, .18f, .8f), -3);

        public static float ViewBlend(bool stage = false)
        {
            float blend = UnderworldCamera2D.Instance != null ? UnderworldCamera2D.Instance.SideViewBlend01 : stage ? 1f : 0f;
            return Mathf.SmoothStep(0f, 1f, blend);
        }

        /// <summary>A small lit patch of floor lets dark contact paint remain legible in the black guardian abyss.</summary>
        public static void PlaceStageFloor(SpriteRenderer renderer, Transform owner, float contactWidth, bool alive, bool stage)
        {
            float blend = ViewBlend(stage);
            renderer.enabled = alive && blend > .01f;
            if (!renderer.enabled) return;
            Place(renderer, owner, new Vector2(Mathf.Clamp(contactWidth * 2.5f, 2.3f, 5.4f), contactWidth * 1.1f),
                Vector2.right, 0, .008f, stage);
            renderer.color = new Color(.14f, .16f, .18f, .8f * blend);
        }

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
            float blend = ViewBlend(stage);
            Vector3 groundDirection = UnderworldCamera2D.IsoRightXZ * direction.x + UnderworldCamera2D.IsoUpXZ * direction.y;
            groundDirection.y = 0;
            groundDirection = groundDirection.sqrMagnitude > .0001f ? groundDirection.normalized : UnderworldCamera2D.IsoRightXZ;
            Quaternion floor = Quaternion.AngleAxis(Mathf.Atan2(-groundDirection.z, groundDirection.x) * Mathf.Rad2Deg, Vector3.up)
                * RenderPipeline2DSetup.GroundDecalRotation;
            // Keep a shallow ellipse visible as the camera approaches floor level.
            Quaternion billboard = UnderworldCamera2D.CurrentBillboardRotation;
            // A side-view floor has one visible horizontal axis. Projecting a screen-up light
            // with atan2 previously turned its cast into an upright stripe through the actor.
            float sideSign = Mathf.Abs(direction.x) > .05f ? Mathf.Sign(direction.x) : direction.y < 0 ? -1f : 1f;
            Quaternion side = billboard * Quaternion.Euler(0, 0, sideSign < 0 ? 180f : 0f);
            renderer.transform.rotation = Quaternion.Slerp(floor, side, blend);
            // Footprints are specified in world units, independently of an NPC's visual/root scale.
            renderer.transform.localScale = Vector3.one;
            Vector3 inheritedScale = renderer.transform.lossyScale;
            renderer.transform.localScale = new Vector3(scale.x / Mathf.Max(.0001f, Mathf.Abs(inheritedScale.x)),
                scale.y * Mathf.Lerp(1f, .32f, blend) / Mathf.Max(.0001f, Mathf.Abs(inheritedScale.y)),
                1f / Mathf.Max(.0001f, Mathf.Abs(inheritedScale.z)));
            Vector3 foot = owner.position; foot.y = lift;
            Vector3 sideDirection = billboard * Vector3.right * sideSign;
            renderer.transform.position = foot + Vector3.Lerp(groundDirection, sideDirection, blend) * offset;
            // Depth sorting must never turn translucent paint into a depth-writing body cutout.
            owner.GetComponent<PaintedActorDepth>()?.Exclude(renderer);
        }
    }
}
