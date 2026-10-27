using UnityEngine;

namespace OrpheusBelow
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyArtAnimator : MonoBehaviour
    {
        public EnemyArtLibrary Library { get; private set; }
        public EnemyMotion Motion { get; private set; }
        public float Elapsed { get; private set; }
        public SpriteRenderer Renderer { get; private set; }
        private float _duration = 1.4f;
        private bool _loop = true;
        public bool IsReady => Library != null && Library.IsComplete;
        public void Configure(EnemyArtLibrary library)
        {
            Library = library; Renderer = GetComponent<SpriteRenderer>();
            Renderer.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial(); Renderer.sortingOrder = 12;
            Play(EnemyMotion.Idle, 1.4f, true);
        }
        public void Play(EnemyMotion motion, float duration, bool loop = false)
        {
            if (Motion == motion && _loop && loop && Renderer != null && Renderer.sprite != null) return;
            Motion = motion; Elapsed = 0; _duration = Mathf.Max(.04f, duration); _loop = loop;
            Apply();
        }
        public void Advance(float dt)
        {
            if (dt <= 0 || !IsReady) return;
            Elapsed += dt; Apply();
        }
        private void Apply()
        {
            if (!IsReady || Renderer == null) return;
            float u = _loop ? Mathf.Repeat(Elapsed / _duration, 1f) : Mathf.Clamp01(Elapsed / _duration);
            int frames = Library.FrameCount(Motion);
            Renderer.sprite = Library.Frame(Motion, Mathf.Min(frames - 1, Mathf.FloorToInt(u * frames)));
        }
        private void LateUpdate()
        {
            transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            if (!UnderworldFrontEnd.BlocksGameplay && !OrpheusHud.IsTutorialModalActive) Advance(Time.deltaTime);
        }
        public GameObject PlayEffect(int row, Vector3 world, float size, float duration, bool ground = false)
            => Library == null ? null : EnemyPaintedEffects.Spawn(Library.kind, row, world, size, duration, ground);
    }
}
