using UnityEngine;

namespace OrpheusBelow
{
    public readonly struct OrpheusShadowShape
    {
        public readonly Vector2 contactScale;
        public readonly Vector2 ambientScale;
        public readonly Vector2 castScale;
        public readonly float opacity;

        public OrpheusShadowShape(Vector2 contact, Vector2 ambient, Vector2 cast, float alpha)
        {
            contactScale = contact;
            ambientScale = ambient;
            castScale = cast;
            opacity = alpha;
        }
    }

    /// <summary>Three stable painted shadow layers: contact, ambient mass and directional cast.</summary>
    [DefaultExecutionOrder(185)]
    public sealed class OrpheusGroundShadow : MonoBehaviour
    {
        private OrpheusPlayerController _player;
        private SpriteRenderer _contact;
        private SpriteRenderer _ambient;
        private SpriteRenderer _cast;

        public SpriteRenderer ContactRenderer => _contact;
        public SpriteRenderer AmbientRenderer => _ambient;
        public SpriteRenderer CastRenderer => _cast;

        public void Initialize(OrpheusPlayerController player)
        {
            _player = player;
            if (_contact == null) _contact = GroundShadowPaint.Create(transform, "ContactShadow",
                new Color(.012f, .008f, .018f, .62f), 0);
            if (_ambient == null) _ambient = GroundShadowPaint.Create(transform, "AmbientPaintShadow",
                new Color(.025f, .012f, .035f, .34f), -1);
            if (_cast == null) _cast = GroundShadowPaint.Create(transform, "DominantLightCast",
                new Color(.035f, .012f, .025f, .28f), -2);
            ApplyState(false, false, PaintedChamberMap.Active?.definition);
        }

        public static Vector2 ResolveCastDirection(PaintedChamberDefinition chamber)
        {
            Vector2 towardLight = chamber == null ? new Vector2(.7071068f, .7071068f)
                : chamber.ResolvedDominantLightDirection;
            return -towardLight.normalized;
        }

        public static OrpheusShadowShape Evaluate(bool dashing, bool attacking, float lightIntensity)
        {
            float opacity = Mathf.Clamp(.28f + Mathf.Clamp(lightIntensity, 0f, 2f) * .15f, .18f, .62f);
            Vector2 contact = dashing ? new Vector2(.82f, .22f) : new Vector2(.78f, .34f);
            Vector2 ambient = attacking ? new Vector2(1.72f, .62f) : new Vector2(1.38f, .52f);
            Vector2 cast = dashing ? new Vector2(2.28f, .23f) : attacking
                ? new Vector2(2.42f, .48f) : new Vector2(1.82f, .42f);
            return new OrpheusShadowShape(contact, ambient, cast, opacity);
        }

        public static bool IsAttackState(OrpheusAnimationState state)
        {
            return state == OrpheusAnimationState.DashStrike || state == OrpheusAnimationState.SoftStrike1 ||
                state == OrpheusAnimationState.SoftStrike2 || state == OrpheusAnimationState.SoftStrike3 ||
                state == OrpheusAnimationState.SoftBolt || state == OrpheusAnimationState.HardBoomWindup ||
                state == OrpheusAnimationState.HardBoomRelease || state == OrpheusAnimationState.HymnBeam ||
                state == OrpheusAnimationState.SanctuaryCast || state == OrpheusAnimationState.ConvergenceCast ||
                state == OrpheusAnimationState.SoftPluck || state == OrpheusAnimationState.FullVoiceRelease ||
                state == OrpheusAnimationState.AulosNoteSoft || state == OrpheusAnimationState.AulosNoteHard ||
                state == OrpheusAnimationState.Discord || state == OrpheusAnimationState.Requiem;
        }

        public void ApplyState(bool dashing, bool attacking, PaintedChamberDefinition chamber)
        {
            if (_contact == null || _ambient == null || _cast == null) return;
            _contact.enabled = _ambient.enabled = _cast.enabled = _player == null || _player.CurrentHealth > 0;
            float intensity = chamber == null ? 1f : chamber.dominantLightIntensity;
            OrpheusShadowShape shape = Evaluate(dashing, attacking, intensity);
            Vector2 castDirection = ResolveCastDirection(chamber);
            bool stage = _player != null && _player.IsIn2DBossStage;
            GroundShadowPaint.Place(_contact, transform, shape.contactScale, Vector2.right, 0, .035f, stage);
            GroundShadowPaint.Place(_ambient, transform, shape.ambientScale, Vector2.right, 0, .025f, stage);
            GroundShadowPaint.Place(_cast, transform, shape.castScale, castDirection, dashing ? .82f : .62f, .015f, stage);
            _contact.color = new Color(.012f, .008f, .018f, Mathf.Clamp(shape.opacity + .1f, .18f, .68f));
            _ambient.color = new Color(.025f, .012f, .035f, shape.opacity * .72f);
            _cast.color = new Color(.035f, .012f, .025f, shape.opacity * .62f);
        }

        private void LateUpdate()
        {
            if (_player == null) return;
            OrpheusSpriteAnimator animator = _player.GetComponent<OrpheusSpriteAnimator>();
            OrpheusAnimationState state = animator == null ? OrpheusAnimationState.Idle : animator.CurrentState;
            ApplyState(_player.IsDashing, IsAttackState(state), PaintedChamberMap.Active?.definition);
        }
    }
}
