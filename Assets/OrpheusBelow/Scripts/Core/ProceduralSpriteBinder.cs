using UnityEngine;

namespace OrpheusBelow
{
    [ExecuteAlways]
    public sealed class ProceduralSpriteBinder : MonoBehaviour
    {
        public ProceduralSpriteShape shape = ProceduralSpriteShape.Square;
        private void OnEnable()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) return;
            if (renderer.sharedMaterial == null) renderer.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
            if (renderer.sprite == null) renderer.sprite = RenderPipeline2DSetup.GetShapeSprite(shape);
        }
    }
}
