using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>A persistent contact shadow, distinct from temporary attack warnings.</summary>
    [DefaultExecutionOrder(190)]
    public sealed class EnemyGroundShadow : MonoBehaviour
    {
        private PaintedEnemyActor _actor;
        private SpriteRenderer _renderer;
        private SpriteRenderer _cast;
        private SpriteRenderer _floor;
        public static void Ensure(PaintedEnemyActor actor)
        {
            var shadow = actor.GetComponent<EnemyGroundShadow>();
            if (shadow == null) shadow = actor.gameObject.AddComponent<EnemyGroundShadow>();
            shadow._actor = actor;
            if (shadow._floor == null) shadow._floor = GroundShadowPaint.CreateStageFloor(actor.transform);
            if (shadow._renderer == null)
            {
                shadow._renderer = GroundShadowPaint.Create(actor.transform, "EnemyGroundShadow",
                    new Color(.025f, .015f, .035f, .66f), 0);
            }
            if (shadow._cast == null)
                shadow._cast = GroundShadowPaint.Create(actor.transform, "EnemyCastShadow",
                    new Color(.03f, .018f, .04f, .34f), -1);
            shadow.LateUpdate();
        }

        private void LateUpdate()
        {
            if (_actor == null || _renderer == null || _cast == null) return;
            bool alive = (_actor.OwnerShade == null || (_actor.OwnerShade.CurrentHealth > 0 && !_actor.OwnerShade.IsPacified))
                && (_actor.OwnerBoss == null || !_actor.OwnerBoss.IsDefeated);
            _renderer.enabled = _cast.enabled = alive;
            bool stage = _actor.IsBossStage;
            float width = Mathf.Clamp((_actor.Art?.Library?.visibleHeight ?? 3f) * .6f, 1f, 3.3f);
            GroundShadowPaint.PlaceStageFloor(_floor, transform, width, alive, stage);
            if (!alive) return;
            Vector2 castDirection = OrpheusGroundShadow.ResolveCastDirection(PaintedChamberMap.Active?.definition);
            GroundShadowPaint.Place(_renderer, transform, new Vector2(width, width * .7f), Vector2.right, 0, .035f, stage);
            GroundShadowPaint.Place(_cast, transform, new Vector2(width * 1.4f, width * .62f), castDirection, width * .32f, .025f, stage);
            float airborne = Mathf.Max(0, _actor.transform.position.y);
            float strength = Mathf.Lerp(1f, .45f, Mathf.Clamp01(airborne / 5f));
            _renderer.color = new Color(.025f, .015f, .035f, .72f * strength);
            _cast.color = new Color(.03f, .018f, .04f, .46f * strength);
        }
    }
}
