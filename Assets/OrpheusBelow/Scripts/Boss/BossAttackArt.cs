using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>The six guardians retain their own painted attack silhouette throughout the duel.</summary>
    public static class BossAttackArt
    {
        public static string ResourcePath(BossIdentity identity)
        {
            switch (identity)
            {
                case BossIdentity.Tripod: return "BossAttacks/Tripod";
                case BossIdentity.MysticEyeWarden: return "BossAttacks/MysticEyeWarden";
                case BossIdentity.WingedSentinel: return "BossAttacks/WingedSentinel";
                case BossIdentity.MaskedMonk: return "BossAttacks/MaskedMonk";
                case BossIdentity.Shepherd: return "BossAttacks/Shepherd";
                default: return "BossAttacks/Cerberus";
            }
        }

        public static Sprite SpriteFor(BossIdentity identity) => Resources.Load<Sprite>(ResourcePath(identity));

        public static SpriteRenderer Attach(Transform parent, BossIdentity identity, float width = 2.4f, int order = 19)
        {
            var go = new GameObject(identity + "_PaintedAttack");
            go.transform.SetParent(parent, false);
            var sprite = SpriteFor(identity);
            if (sprite == null) throw new System.InvalidOperationException("Missing guardian attack art: " + ResourcePath(identity));
            var renderer = RenderPipeline2DSetup.AttachSprite(go, sprite, Color.white, order);
            renderer.flipX = identity == BossIdentity.Tripod;
            float scale = width / Mathf.Max(.01f, sprite.bounds.size.x);
            go.transform.localScale = Vector3.one * scale;
            return renderer;
        }

        public static Vector2 Motion(BossIdentity identity, float elapsed)
        {
            switch (identity)
            {
                case BossIdentity.Tripod: return new Vector2(.03f * Mathf.Sin(elapsed * 8f), 0);
                case BossIdentity.MysticEyeWarden: return new Vector2(.10f * Mathf.Sin(elapsed * 5f), 0);
                case BossIdentity.WingedSentinel: return new Vector2(.12f * Mathf.Sin(elapsed * 7f), 12f * Mathf.Sin(elapsed * 7f));
                case BossIdentity.MaskedMonk: return new Vector2(.025f * Mathf.Sin(elapsed * 15f), 4f * Mathf.Sin(elapsed * 6f));
                case BossIdentity.Shepherd: return new Vector2(.06f * Mathf.Sin(elapsed * 6f), 9f * Mathf.Sin(elapsed * 8f));
                default: return new Vector2(.035f * Mathf.Sin(elapsed * 18f), 3f * Mathf.Sin(elapsed * 12f));
            }
        }
    }
}
