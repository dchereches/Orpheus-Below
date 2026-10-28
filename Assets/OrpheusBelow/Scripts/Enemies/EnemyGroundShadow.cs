using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>A persistent contact shadow, distinct from temporary attack warnings.</summary>
    [DefaultExecutionOrder(190)]
    public sealed class EnemyGroundShadow : MonoBehaviour
    {
        private PaintedEnemyActor _actor;
        private SpriteRenderer _renderer;
        public static void Ensure(PaintedEnemyActor actor)
        {
            var shadow = actor.GetComponent<EnemyGroundShadow>();
            if (shadow == null) shadow = actor.gameObject.AddComponent<EnemyGroundShadow>();
            shadow._actor = actor;
            if (shadow._renderer == null)
            {
                var go = new GameObject("EnemyGroundShadow"); go.transform.SetParent(actor.transform, false);
                shadow._renderer = RenderPipeline2DSetup.AttachGroundDecal(go, RenderPipeline2DSetup.SoftGlowSprite,
                    new Color(.025f, .015f, .035f, .66f), 4);
                actor.GetComponent<PaintedActorDepth>()?.Exclude(shadow._renderer);
            }
            shadow.LateUpdate();
        }

        private void LateUpdate()
        {
            if (_actor == null || _renderer == null) return;
            bool stage = _actor.IsBossStage;
            float width = Mathf.Clamp((_actor.Art?.Library?.visibleHeight ?? 3f) * .6f, 1f, 3.3f);
            _renderer.transform.localScale = new Vector3(width, stage ? width * .24f : width * .7f, 1f);
            _renderer.transform.rotation = stage ? UnderworldCamera2D.CurrentBillboardRotation : RenderPipeline2DSetup.GroundDecalRotation;
            Vector3 position = _actor.transform.position; position.y = .035f;
            _renderer.transform.position = position;
            float airborne = Mathf.Max(0, _actor.transform.position.y);
            _renderer.color = new Color(.025f, .015f, .035f, Mathf.Lerp(.66f, .26f, Mathf.Clamp01(airborne / 5f)));
        }
    }
}
