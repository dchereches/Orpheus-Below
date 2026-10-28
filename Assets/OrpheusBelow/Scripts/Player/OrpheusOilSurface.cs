using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Adds broad connected paint and authored rim light without replacing sprite geometry.</summary>
    public static class OrpheusOilSurface
    {
        private static readonly int FrameRect = Shader.PropertyToID("_FrameRect");
        private static readonly int RimDirection = Shader.PropertyToID("_RimDirection");
        private static readonly int RimColor = Shader.PropertyToID("_RimColor");
        private static readonly int RimIntensity = Shader.PropertyToID("_RimIntensity");
        private static readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
        private static Material _material;

        public static void Apply(SpriteRenderer renderer, Vector2 lightDirection, Color lightColor, float intensity)
        {
            if (renderer == null || renderer.sprite == null || renderer.sprite.texture == null) return;
            if (_material == null)
            {
                Shader shader = Resources.Load<Shader>("Art/OrpheusOilSurface");
                if (shader == null) shader = Shader.Find("OrpheusBelow/OrpheusOilSurface");
                if (shader == null) return;
                _material = new Material(shader)
                {
                    name = "Orpheus_BroadOil_AuthoredRim",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            Sprite sprite = renderer.sprite;
            Texture2D texture = sprite.texture;
            Rect rect = sprite.rect;
            Vector2 direction = lightDirection.sqrMagnitude < .0001f ? new Vector2(.7071068f, .7071068f) : lightDirection.normalized;
            renderer.sharedMaterial = _material;
            renderer.GetPropertyBlock(Properties);
            Properties.SetVector(FrameRect, new Vector4(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height));
            Properties.SetVector(RimDirection, new Vector4(direction.x, direction.y, 0f, 0f));
            Properties.SetColor(RimColor, lightColor);
            Properties.SetFloat(RimIntensity, 0f); // Rim highlights disabled
            renderer.SetPropertyBlock(Properties);
        }

        public static void Apply(SpriteRenderer renderer, PaintedChamberDefinition chamber)
        {
            Vector2 direction = chamber == null ? new Vector2(.7071068f, .7071068f) : chamber.ResolvedDominantLightDirection;
            Color color = chamber == null ? new Color(1f, .62f, .28f, 1f) : chamber.dominantLightColor;
            float intensity = chamber == null ? 1f : chamber.dominantLightIntensity;
            Apply(renderer, direction, color, intensity);
        }
    }
}
