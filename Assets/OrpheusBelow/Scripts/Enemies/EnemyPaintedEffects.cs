using UnityEngine;

namespace OrpheusBelow
{
    public sealed class EnemyPaintedEffects : MonoBehaviour
    {
        private EnemyArtLibrary _library;
        private SpriteRenderer _renderer;
        private float _elapsed, _duration;
        private int _row;
        private bool _ground;
        public bool Loop;
        public static GameObject Spawn(PaintedEnemyKind kind, int row, Vector3 point, float size, float duration, bool ground = false)
        {
            var library = EnemyArtLibrary.Load(kind); if (library == null || !library.IsComplete) return null;
            var go = new GameObject(kind + "_PaintedEffect_" + row); go.transform.position = point;
            go.transform.localScale = Vector3.one * size;
            var effect = go.AddComponent<EnemyPaintedEffects>();
            effect._library = library; effect._row = row; effect._duration = Mathf.Max(.05f, duration); effect._ground = ground;
            effect._renderer = ground ? RenderPipeline2DSetup.AttachGroundDecal(go, library.Effect(row, 0), Color.white, 8)
                : RenderPipeline2DSetup.AttachBillboardSprite(go, library.Effect(row, 0), Color.white, 18);
            return go;
        }
        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            _elapsed += Time.deltaTime;
            if (!_ground) transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            float u = Loop ? Mathf.Repeat(_elapsed * 2f, 1f) : Mathf.Clamp01(_elapsed / _duration);
            _renderer.sprite = _library.Effect(_row, Mathf.Min(_library.effectColumns - 1, Mathf.FloorToInt(u * _library.effectColumns)));
            if (!Loop && _elapsed >= _duration) Destroy(gameObject);
        }
    }

    public sealed class PaintedAttackShadow : MonoBehaviour
    {
        private SpriteRenderer _shadow;
        private SpriteRenderer _edge;
        private float _radius;
        public float Radius => _radius;
        public static PaintedAttackShadow Create(Vector3 center, float radius, float duration)
        {
            var go = new GameObject("EnemyAttack_CircularShadow"); go.transform.position = new Vector3(center.x, .045f, center.z);
            var shadow = go.AddComponent<PaintedAttackShadow>(); shadow._radius = radius;
            var fill = new GameObject("DarkWarning"); fill.transform.SetParent(go.transform, false);
            fill.transform.localScale = Vector3.one * radius * 2;
            shadow._shadow = RenderPipeline2DSetup.AttachGroundDecal(fill, RenderPipeline2DSetup.CircleSprite, new Color(.04f, .025f, .015f, .28f), 6);
            var rim = new GameObject("WarningEdge"); rim.transform.SetParent(go.transform, false); rim.transform.localScale = Vector3.one * radius * 2;
            shadow._edge = RenderPipeline2DSetup.AttachGroundDecal(rim, RenderPipeline2DSetup.RingSprite, new Color(.62f, .37f, .12f, .4f), 7);
            if (OrpheusPlayerController.Instance != null && OrpheusPlayerController.Instance.IsIn2DBossStage)
            {
                // A flat ground decal is edge-on in the duel camera; show its actual horizontal reach.
                fill.transform.rotation = rim.transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;
                fill.transform.localScale = rim.transform.localScale = new Vector3(radius * 2, radius * .45f, 1f);
            }
            var owner = PaintedEnemyActor.CurrentAttackOwner;
            if (owner != null && (owner.Kind == PaintedEnemyKind.MaskedMonk || owner.Kind == PaintedEnemyKind.WingedSentinel))
            {
                var paint = EnemyPaintedEffects.Spawn(owner.Kind, 3, go.transform.position, radius * 2, Mathf.Max(.1f, duration), true);
                if (paint != null) paint.transform.SetParent(go.transform, true);
            }
            return shadow;
        }
        public void Progress(float value)
        {
            float u = Mathf.Clamp01(value);
            if (_shadow != null) _shadow.color = new Color(.04f, .025f, .015f, Mathf.Lerp(.24f, .66f, u));
            if (_edge != null) _edge.color = new Color(.62f, .37f, .12f, Mathf.Lerp(.35f, .8f, u));
        }
        public void Dispose()
        {
            if (this == null) return; // Scene teardown may already have destroyed the warning.
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
    }
}
