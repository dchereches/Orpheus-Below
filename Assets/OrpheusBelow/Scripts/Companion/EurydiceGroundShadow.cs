using UnityEngine;

namespace OrpheusBelow
{
    [DefaultExecutionOrder(190)]
    public sealed class EurydiceGroundShadow : MonoBehaviour
    {
        private BlurredEurydice _companion;
        private SpriteRenderer _contact, _cast, _floor;
        private bool _waitingSoul;

        public void InitializeWaitingSoul()
        {
            Initialize(null);
            _waitingSoul = true;
            LateUpdate();
        }

        public void Initialize(BlurredEurydice companion)
        {
            _waitingSoul = false;
            _companion = companion;
            if (_floor == null) _floor = GroundShadowPaint.CreateStageFloor(transform);
            if (_contact == null) _contact = GroundShadowPaint.Create(transform, "EurydiceContactShadow", new Color(.022f, .014f, .03f, .7f), 0);
            if (_cast == null) _cast = GroundShadowPaint.Create(transform, "EurydiceCastShadow", new Color(.03f, .018f, .04f, .45f), -1);
            LateUpdate();
        }

        public void Hide()
        {
            if (_contact != null) _contact.enabled = false;
            if (_cast != null) _cast.enabled = false;
            if (_floor != null) _floor.enabled = false;
        }

        private void LateUpdate()
        {
            if (_contact == null || _cast == null) return;
            if (!_waitingSoul && (_companion == null || _companion.IsVanished)) { Hide(); return; }
            _contact.enabled = _cast.enabled = true;
            Vector2 cast = OrpheusGroundShadow.ResolveCastDirection(PaintedChamberMap.Active?.definition);
            bool stage = OrpheusPlayerController.Instance != null && OrpheusPlayerController.Instance.IsIn2DBossStage;
            GroundShadowPaint.PlaceStageFloor(_floor, transform, 1.05f, true, stage);
            GroundShadowPaint.Place(_contact, transform, new Vector2(1.05f, .7f), Vector2.right, 0, .035f, stage);
            GroundShadowPaint.Place(_cast, transform, new Vector2(2.2f, .8f), cast, .65f, .025f, stage);
        }
    }
}
