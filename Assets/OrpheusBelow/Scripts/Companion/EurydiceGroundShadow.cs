using UnityEngine;

namespace OrpheusBelow
{
    [DefaultExecutionOrder(190)]
    public sealed class EurydiceGroundShadow : MonoBehaviour
    {
        private BlurredEurydice _companion;
        private SpriteRenderer _contact, _cast;

        public void Initialize(BlurredEurydice companion)
        {
            _companion = companion;
            if (_contact == null) _contact = GroundShadowPaint.Create(transform, "EurydiceContactShadow", new Color(.022f, .014f, .03f, .58f), 0);
            if (_cast == null) _cast = GroundShadowPaint.Create(transform, "EurydiceCastShadow", new Color(.03f, .018f, .04f, .32f), -1);
            LateUpdate();
        }

        public void Hide()
        {
            if (_contact != null) _contact.enabled = false;
            if (_cast != null) _cast.enabled = false;
        }

        private void LateUpdate()
        {
            if (_contact == null || _cast == null) return;
            if (_companion == null || _companion.IsVanished) { Hide(); return; }
            _contact.enabled = _cast.enabled = true;
            Vector2 cast = OrpheusGroundShadow.ResolveCastDirection(PaintedChamberMap.Active?.definition);
            bool stage = OrpheusPlayerController.Instance != null && OrpheusPlayerController.Instance.IsIn2DBossStage;
            GroundShadowPaint.Place(_contact, transform, new Vector2(.88f, .58f), Vector2.right, 0, .035f, stage);
            GroundShadowPaint.Place(_cast, transform, new Vector2(1.9f, .68f), cast, .55f, .025f, stage);
        }
    }
}
