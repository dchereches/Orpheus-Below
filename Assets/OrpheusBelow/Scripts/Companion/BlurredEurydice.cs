using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// Blurred ghostly Eurydice who trails behind Orpheus throughout the ascent.
    /// Rendered as a multi-layer out-of-focus optical aura that ONLY sharpens into focus
    /// if the player yields to temptation and holds [Q] to turn back (losing the game).
    /// </summary>
    public class BlurredEurydice : MonoBehaviour
    {
        public Transform orpheus;
        public float followLagDistance = 2.9f;
        public float smoothSpeed = 6.5f;

        public bool IsVanished { get; private set; }
        public bool InTemptationZone { get; private set; }
        public string ActiveWhisper { get; private set; } = "";

        private readonly Queue<Vector3> _breadcrumbs = new Queue<Vector3>();
        private Vector3 _lastRecordedPos;
        private Vector3 _desiredPos;

        private Transform[] _blurLayers;
        private SpriteRenderer[] _blurRenderers;
        private SpriteRenderer _sharpCoreHead;
        private SpriteRenderer _sharpCoreRobe;

        private float _focus01; // 0 = heavily blurred (normal), 1 = crystal clear (fatal look-back!)

        private void Start()
        {
            BuildBlurredVisualStack();
            if (orpheus != null)
            {
                _lastRecordedPos = orpheus.position;
                _desiredPos = orpheus.position + new Vector3(-followLagDistance, 0f, 0f);
                transform.position = _desiredPos;
            }
        }

        private void BuildBlurredVisualStack()
        {
            const int layerCount = 8;
            _blurLayers = new Transform[layerCount];
            _blurRenderers = new SpriteRenderer[layerCount];

            for (int i = 0; i < layerCount; i++)
            {
                GameObject layerGo = new GameObject("BlurGhostLayer_" + i);
                layerGo.transform.SetParent(transform, false);
                layerGo.transform.localScale = new Vector3(1.35f + i * 0.14f, 2.15f + i * 0.12f, 1f);

                Color c = Color.Lerp(
                    new Color(0.55f, 0.88f, 1.15f, 0.22f),
                    new Color(1.10f, 0.78f, 0.95f, 0.14f),
                    i / (float)(layerCount - 1));

                _blurRenderers[i] = RenderPipeline2DSetup.AttachSprite(layerGo, RenderPipeline2DSetup.SoftGlowSprite, c, 4);
                _blurLayers[i] = layerGo.transform;
            }

            // Sharp core figure (completely hidden at focus=0, only revealed if you fatally look back!)
            GameObject robeGo = new GameObject("EurydiceSharpRobe");
            robeGo.transform.SetParent(transform, false);
            robeGo.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            robeGo.transform.localScale = new Vector3(0.62f, 1.32f, 1f);
            _sharpCoreRobe = RenderPipeline2DSetup.AttachSprite(robeGo, RenderPipeline2DSetup.DiamondSprite, new Color(0.85f, 0.96f, 1.25f, 0f), 6);

            GameObject headGo = new GameObject("EurydiceSharpHead");
            headGo.transform.SetParent(transform, false);
            headGo.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            headGo.transform.localScale = new Vector3(0.44f, 0.44f, 1f);
            _sharpCoreHead = RenderPipeline2DSetup.AttachSprite(headGo, RenderPipeline2DSetup.CircleSprite, new Color(1.2f, 1.05f, 0.9f, 0f), 7);
        }

        public void SetLookBackFocus(float focus01)
        {
            _focus01 = Mathf.Clamp01(focus01);
        }

        public void SetTemptationZone(bool active, string whisperText)
        {
            InTemptationZone = active;
            ActiveWhisper = active ? whisperText : "";
        }

        public void ShatterIntoAsh()
        {
            if (IsVanished) return;
            IsVanished = true;
            _focus01 = 1f;
            StartCoroutine(ShatterRoutine());
        }

        private System.Collections.IEnumerator ShatterRoutine()
        {
            // Hold crystal-clear unblurred gaze for 1.0 heartbreaking second before turning to ash
            UpdateBlurVisuals();
            yield return new WaitForSecondsRealtime(1.0f);

            // Spawn rising golden-violet underworld ash particles
            for (int i = 0; i < 42; i++)
            {
                GameObject ash = new GameObject("EurydiceAsh");
                ash.transform.position = transform.position + new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(-0.7f, 0.8f), 0f);
                float sz = Random.Range(0.08f, 0.24f);
                ash.transform.localScale = new Vector3(sz, sz, 1f);
                Color col = Random.value > 0.4f ? new Color(1.6f, 1.1f, 0.45f, 0.9f) : new Color(0.7f, 0.95f, 1.4f, 0.85f);
                SpriteRenderer sr = RenderPipeline2DSetup.AttachSprite(ash, RenderPipeline2DSetup.SoftGlowSprite, col, 15);
                ash.AddComponent<RisingAshParticle>().Init(sr);
            }

            foreach (var r in _blurRenderers) if (r != null) r.enabled = false;
            if (_sharpCoreHead != null) _sharpCoreHead.enabled = false;
            if (_sharpCoreRobe != null) _sharpCoreRobe.enabled = false;
        }

        private void Update()
        {
            if (IsVanished) return;

            if (orpheus != null)
            {
                if (Vector3.Distance(orpheus.position, _lastRecordedPos) > 0.22f)
                {
                    _breadcrumbs.Enqueue(orpheus.position);
                    _lastRecordedPos = orpheus.position;
                }

                float targetLag = InTemptationZone ? followLagDistance + 1.35f : followLagDistance;
                while (_breadcrumbs.Count > 1 && Vector3.Distance(_breadcrumbs.Peek(), orpheus.position) > targetLag)
                {
                    _desiredPos = _breadcrumbs.Dequeue();
                }

                // Always keep her to Orpheus's left/behind so she trails behind your ascent
                Vector3 fallbackBehind = orpheus.position + new Vector3(-targetLag, 0.1f, 0f);
                if (_desiredPos.x > orpheus.position.x - 1.1f)
                {
                    _desiredPos = fallbackBehind;
                }

                float bob = Mathf.Sin(Time.time * 2.4f) * 0.14f;
                Vector3 goal = new Vector3(_desiredPos.x, _desiredPos.y + bob, 0f);
                transform.position = Vector3.Lerp(transform.position, goal, Time.unscaledDeltaTime * smoothSpeed);
            }

            UpdateBlurVisuals();
        }

        private void UpdateBlurVisuals()
        {
            if (_blurLayers == null) return;

            // At _focus01 == 0: wide drifting optical blur. At _focus01 == 1: layers converge sharply!
            float blurSpread = Mathf.Lerp(0.46f, 0.02f, _focus01);
            float t = Time.unscaledTime;

            for (int i = 0; i < _blurLayers.Length; i++)
            {
                float angle = t * (1.1f + i * 0.23f) + i * 1.57f;
                float r = blurSpread * (0.35f + 0.65f * (i / (float)_blurLayers.Length));
                _blurLayers[i].localPosition = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle * 1.3f) * r * 0.8f, 0f);

                Color c = _blurRenderers[i].color;
                c.a = Mathf.Lerp(0.20f, 0.08f, _focus01);
                _blurRenderers[i].color = c;
            }

            float sharpAlpha = Mathf.SmoothStep(0f, 1f, (_focus01 - 0.25f) / 0.75f);
            if (_sharpCoreRobe != null)
            {
                Color rc = _sharpCoreRobe.color;
                rc.a = sharpAlpha;
                _sharpCoreRobe.color = rc;
            }
            if (_sharpCoreHead != null)
            {
                Color hc = _sharpCoreHead.color;
                hc.a = sharpAlpha;
                _sharpCoreHead.color = hc;
            }
        }
    }

    public class RisingAshParticle : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Vector3 _vel;
        private float _life;

        public void Init(SpriteRenderer sr)
        {
            _sr = sr;
            _vel = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.9f, 2.6f), 0f);
            _life = Random.Range(1.2f, 2.4f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _life -= dt;
            transform.position += (_vel + new Vector3(Mathf.Sin(Time.unscaledTime * 5f + transform.position.y) * 0.5f, 0f, 0f)) * dt;
            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = Mathf.Max(0f, c.a - dt * 0.55f);
                _sr.color = c;
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
