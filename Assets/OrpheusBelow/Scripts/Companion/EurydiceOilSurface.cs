using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Paints the committed sprite colours without replacing or reshaping the sprite.</summary>
    public static class EurydiceOilSurface
    {
        private static readonly int FrameRect = Shader.PropertyToID("_FrameRect");
        private static readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
        private static Material _material;

        public static void Apply(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return;
            Sprite sprite = renderer.sprite;
            Texture2D texture = sprite.texture;
            if (texture == null) return;

            if (_material == null)
            {
                Shader shader = Resources.Load<Shader>("Art/EurydiceOilSurface");
                if (shader == null) shader = Shader.Find("OrpheusBelow/EurydiceOilSurface");
                if (shader == null) return;
                _material = new Material(shader)
                {
                    name = "Eurydice_OriginalSprite_OilSurface",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            Rect rect = sprite.rect;
            renderer.sharedMaterial = _material;
            renderer.GetPropertyBlock(Properties);
            Properties.SetVector(FrameRect, new Vector4(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height));
            renderer.SetPropertyBlock(Properties);
        }
    }
}
