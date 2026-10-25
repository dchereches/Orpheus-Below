using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// 2.5D Isometric Blurred Eurydice companion:
    /// Drifts softly behind Orpheus along the XZ chamber floor as an 8-layer out-of-focus ghostly aura.
    /// Never blocks movement or combat, and ONLY sharpens into crystal focus if you hold [Q] to break your vow.
    /// </summary>
    public class BlurredEurydice : MonoBehaviour
    {
        public Transform orpheus;
        public float followLagDistance = 3.8f;
        public float smoothSpeed = 4.8f;

        public bool IsVanished { get; private set; }
        public bool InTemptationZone { get; private set; }
        public string ActiveWhisper { get; private set; } = "";

        private readonly Queue<Vector3> _breadcrumbs = new Queue<Vector3>();
        private Vector3 _lastRecordedPos;
        private Vector3 _desiredPos;

        private Transform _billboardRoot;
        private Transform[] _blurLayers;
        private SpriteRenderer[] _blurRenderers;
        private SpriteRenderer _sharpCoreHead;
        private SpriteRenderer _sharpCoreRobe;

        private float _focus01;

        private void Start()
        {
            BuildBlurredVisualStack();
            if (orpheus != null)
            {
                SnapBehindOrpheus(orpheus.position - UnderworldCamera2D.IsoUpXZ * followLagDistance);
            }
        }

        public void SnapBehindOrpheus(Vector3 worldPos)
        {
            _breadcrumbs.Clear();
            worldPos.y = 0f;
            transform.position = worldPos;
            _desiredPos = worldPos;
            if (orpheus != null) _lastRecordedPos = orpheus.position;
        }

        private void BuildBlurredVisualStack()
        {
            GameObject shadow = new GameObject("EurydiceMistAura");
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadow.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(shadow, RenderPipeline2DSetup.SoftGlowSprite, new Color(0.35f, 0.75f, 1.0f, 0.25f), 1);

            _billboardRoot = new GameObject("EurydiceBillboard25D").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.localPosition = new Vector3(0f, 0.85f, 0f);
            _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;

            const int layerCount = 8;
            _blurLayers = new Transform[layerCount];
            _blurRenderers = new SpriteRenderer[layerCount];

            for (int i = 0; i < layerCount; i++)
            {
                GameObject layerGo = new GameObject("BlurGhostLayer_" + i);
                layerGo.transform.SetParent(_billboardRoot, false);
                layerGo.transform.localScale = new Vector3(1.25f + i * 0.13f, 2.0f + i * 0.11f, 1f);

                Color c = Color.Lerp(
                    new Color(0.52f, 0.86f, 1.15f, 0.20f),
                    new Color(1.05f, 0.76f, 0.95f, 0.13f),
                    i / (float)(layerCount - 1));

                _blurRenderers[i] = RenderPipeline2DSetup.AttachSprite(layerGo, RenderPipeline2DSetup.SoftGlowSprite, c, 6);
                _blurLayers[i] = layerGo.transform;
            }

            // Sharp core figure — invisible during normal play, revealed only during fatal [Hold Q] look-back!
            GameObject robeGo = new GameObject("EurydiceSharpRobe");
            robeGo.transform.SetParent(_billboardRoot, false);
            robeGo.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            robeGo.transform.localScale = new Vector3(0.60f, 1.28f, 1f);
            _sharpCoreRobe = RenderPipeline2DSetup.AttachSprite(robeGo, RenderPipeline2DSetup.DiamondSprite, new Color(0.85f, 0.96f, 1.30f, 0f), 8);

            GameObject headGo = new GameObject("EurydiceSharpHead");
            headGo.transform.SetParent(_billboardRoot, false);
            headGo.transform.localPosition = new Vector3(0f, 0.68f, -0.01f);
            headGo.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            _sharpCoreHead = RenderPipeline2DSetup.AttachSprite(headGo, RenderPipeline2DSetup.CircleSprite, new Color(1.2f, 1.05f, 0.92f, 0f), 9);
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
            UpdateBlurVisuals();
            yield return new WaitForSecondsRealtime(1.0f);

            for (int i = 0; i < 42; i++)
            {
                GameObject ash = new GameObject("EurydiceAsh");
                ash.transform.position = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 1.5f), Random.Range(-0.5f, 0.5f));
                float sz = Random.Range(0.09f, 0.24f);
                ash.transform.localScale = new Vector3(sz, sz, 1f);
                Color col = Random.value > 0.4f ? new Color(1.7f, 1.15f, 0.45f, 0.9f) : new Color(0.7f, 0.95f, 1.45f, 0.85f);
                SpriteRenderer sr = RenderPipeline2DSetup.AttachBillboardSprite(ash, RenderPipeline2DSetup.SoftGlowSprite, col, 16);
                ash.AddComponent<RisingAshParticle>().Init(sr, driftUpSpeed: 1.8f, duration: 2.0f);
            }

            foreach (var r in _blurRenderers) if (r != null) r.enabled = false;
            if (_sharpCoreHead != null) _sharpCoreHead.enabled = false;
            if (_sharpCoreRobe != null) _sharpCoreRobe.enabled = false;
        }

        private void Update()
        {
            if (IsVanished) return;
            if (_billboardRoot != null) _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;

            if (orpheus != null)
            {
                if (Vector3.Distance(orpheus.position, _lastRecordedPos) > 0.35f)
                {
                    _breadcrumbs.Enqueue(orpheus.position);
                    _lastRecordedPos = orpheus.position;
                }

                float targetLag = InTemptationZone ? followLagDistance + 1.4f : followLagDistance;
                while (_breadcrumbs.Count > 1 && Vector3.Distance(_breadcrumbs.Peek(), orpheus.position) > targetLag)
                {
                    _desiredPos = _breadcrumbs.Dequeue();
                }

                float bob = Mathf.Sin(Time.time * 2.3f) * 0.08f;
                Vector3 goal = new Vector3(_desiredPos.x, bob, _desiredPos.z);
                transform.position = Vector3.Lerp(transform.position, goal, Time.unscaledDeltaTime * smoothSpeed);
            }

            UpdateBlurVisuals();
        }

        private void UpdateBlurVisuals()
        {
            if (_blurLayers == null) return;

            float blurSpread = Mathf.Lerp(0.45f, 0.02f, _focus01);
            float t = Time.unscaledTime;

            for (int i = 0; i < _blurLayers.Length; i++)
            {
                float angle = t * (1.1f + i * 0.23f) + i * 1.57f;
                float r = blurSpread * (0.35f + 0.65f * (i / (float)_blurLayers.Length));
                _blurLayers[i].localPosition = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle * 1.3f) * r * 0.8f, 0f);

                Color c = _blurRenderers[i].color;
                c.a = Mathf.Lerp(0.19f, 0.08f, _focus01);
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
        private float _maxLife;

        public void Init(SpriteRenderer sr, float driftUpSpeed = 1.4f, float duration = 1.2f)
        {
            _sr = sr;
            _vel = new Vector3(Random.Range(-0.45f, 0.45f), driftUpSpeed * Random.Range(0.7f, 1.3f), Random.Range(-0.45f, 0.45f));
            _maxLife = Mathf.Max(0.08f, duration);
            _life = _maxLife;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            transform.position += _vel * dt;
            _life -= dt;
            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = Mathf.Clamp01(_life / _maxLife) * 0.7f;
                _sr.color = c;
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
