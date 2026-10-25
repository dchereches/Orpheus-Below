using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// 2D Hades-style dynamic camera with asymmetric "Don't Look Back" framing
    /// (keeps blurred Eurydice at the edge of your peripheral vision), chamber bounds,
    /// dramatic zoom on [Q] Look-Back temptation, and heavy trauma shake on Hard Booms.
    /// </summary>
    public class UnderworldCamera2D : MonoBehaviour
    {
        public static UnderworldCamera2D Instance { get; private set; }

        [Header("Targets")]
        public Transform target;
        public Transform eurydiceTarget;

        [Header("Framing & Follow")]
        public float baseOrthoSize = 7.4f;
        public Vector2 forwardOffset = new Vector2(2.3f, 1.15f);
        public float smoothTime = 0.14f;

        [Header("Chamber Bounds (Optional Lock)")]
        public bool useBounds = true;
        public Rect worldBounds = new Rect(-6f, -4f, 160f, 28f);

        private Camera _cam;
        private Vector3 _vel;
        private float _trauma;
        private float _lookBackBlend;
        private float _zoomOverride = -1f;
        private Vector2? _arenaCenterOverride;

        private void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = gameObject.AddComponent<Camera>();

            RenderPipeline2DSetup.ConfigureCameraFor2D(_cam, new Color(0.045f, 0.022f, 0.065f, 1f));
            _cam.orthographicSize = baseOrthoSize;
        }

        public void AddShake(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public void SetLookBackBlend(float blend01)
        {
            _lookBackBlend = Mathf.Clamp01(blend01);
        }

        public void LockArena(Vector2 center, float orthoSize)
        {
            _arenaCenterOverride = center;
            _zoomOverride = orthoSize;
        }

        public void UnlockArena()
        {
            _arenaCenterOverride = null;
            _zoomOverride = -1f;
        }

        private void LateUpdate()
        {
            if (target == null || _cam == null) return;

            float dt = Time.unscaledDeltaTime;

            // Desired focus point: forward-biased normally, panning back toward Eurydice if holding [Q]!
            Vector3 normalGoal = target.position + new Vector3(forwardOffset.x, forwardOffset.y, 0f);
            if (_arenaCenterOverride.HasValue)
            {
                Vector2 ac = _arenaCenterOverride.Value;
                normalGoal = Vector3.Lerp(normalGoal, new Vector3(ac.x, ac.y, 0f), 0.72f);
            }

            Vector3 lookBackGoal = normalGoal;
            if (eurydiceTarget != null)
            {
                Vector3 mid = Vector3.Lerp(target.position, eurydiceTarget.position, 0.65f) + new Vector3(0f, 0.6f, 0f);
                lookBackGoal = mid;
            }

            Vector3 desired = Vector3.Lerp(normalGoal, lookBackGoal, _lookBackBlend);
            desired.z = -10f;

            // Zoom: tighten dramatically during [Q] temptation, or widen for Cerberus boss arena
            float targetZoom = _zoomOverride > 0f ? _zoomOverride : baseOrthoSize;
            targetZoom = Mathf.Lerp(targetZoom, 4.9f, _lookBackBlend);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetZoom, dt * 6f);

            if (useBounds)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                if (worldBounds.width > halfW * 2f)
                    desired.x = Mathf.Clamp(desired.x, worldBounds.xMin + halfW, worldBounds.xMax - halfW);
                if (worldBounds.height > halfH * 2f)
                    desired.y = Mathf.Clamp(desired.y, worldBounds.yMin + halfH, worldBounds.yMax - halfH);
            }

            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref _vel, smoothTime, Mathf.Infinity, dt);

            // Trauma-squared screen shake for punchy Hard Booms!
            Vector3 shakeOffset = Vector3.zero;
            float rotZ = 0f;
            if (_trauma > 0.001f)
            {
                float shake = _trauma * _trauma;
                float t = Time.unscaledTime * 42f;
                shakeOffset.x = (Mathf.PerlinNoise(t, 1.3f) * 2f - 1f) * 0.65f * shake;
                shakeOffset.y = (Mathf.PerlinNoise(2.7f, t) * 2f - 1f) * 0.65f * shake;
                rotZ = (Mathf.PerlinNoise(t, t * 0.5f) * 2f - 1f) * 2.2f * shake;
                _trauma = Mathf.MoveTowards(_trauma, 0f, dt * 2.3f);
            }

            transform.position = smoothed + shakeOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        }
    }
}
