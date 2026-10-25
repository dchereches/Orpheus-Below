using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    /// <summary>
    /// True 2.5D Isometric Hades Camera (Orthographic, Pitch 36°, Yaw 45°):
    ///   - Provides exact screen-to-isometric WASD axes (W = screen-up, D = screen-right)
    ///   - Raycasts mouse cursor cleanly onto the Y=0 chamber floor plane
    ///   - Smoothly follows Orpheus with subtle aim/velocity look-ahead, chamber framing, & trauma shake
    /// </summary>
    public class UnderworldCamera2D : MonoBehaviour
    {
        public static UnderworldCamera2D Instance { get; private set; }

        public static readonly Vector3 IsoRightXZ = new Vector3(0.70710678f, 0f, -0.70710678f);
        public static readonly Vector3 IsoUpXZ = new Vector3(0.70710678f, 0f, 0.70710678f);

        [Header("Targets")]
        public Transform target;
        public Transform eurydiceTarget;

        [Header("Isometric Framing")]
        public float baseOrthoSize = 7.8f;
        public float cameraDistance = 36f;
        public float smoothTime = 0.11f;
        public float aimLookAhead = 0.85f;

        [Header("Active Chamber Leash")]
        public Vector3 chamberCenter = Vector3.zero;
        public float chamberCameraLeashRadius = 11f;

        private Camera _cam;
        private Vector3 _focusPoint;
        private Vector3 _focusVel;
        private float _trauma;
        private float _lookBackBlend;
        private float _zoomOverride = -1f;

        public Camera Cam => _cam;

        private void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = gameObject.AddComponent<Camera>();

            RenderPipeline2DSetup.ConfigureCameraForIsometric25D(_cam, new Color(0.038f, 0.020f, 0.056f, 1f));
            _cam.orthographicSize = baseOrthoSize;
        }

        private void Start()
        {
            if (target != null) SnapToTargetNow();
        }

        public static Vector3 InputToIsoWorldDir(Vector2 input)
        {
            Vector3 world = IsoRightXZ * input.x + IsoUpXZ * input.y;
            if (world.sqrMagnitude > 1f) world.Normalize();
            return world;
        }

        public bool TryGetCursorGroundPoint(out Vector3 groundPoint)
        {
            groundPoint = target != null ? target.position : Vector3.zero;
            if (_cam == null || Mouse.current == null) return false;

            Vector2 mp = Mouse.current.position.ReadValue();
            Ray ray = _cam.ScreenPointToRay(new Vector3(mp.x, mp.y, 0f));
            Plane floorPlane = new Plane(Vector3.up, Vector3.zero);
            if (floorPlane.Raycast(ray, out float enter))
            {
                groundPoint = ray.GetPoint(enter);
                return true;
            }
            return false;
        }

        public void SetChamberBounds(Vector3 center, float leashRadius, float orthoOverride = -1f)
        {
            chamberCenter = center;
            chamberCameraLeashRadius = leashRadius;
            _zoomOverride = orthoOverride;
        }

        public void SnapToTargetNow()
        {
            if (target == null) return;
            _focusPoint = target.position;
            _focusVel = Vector3.zero;
            ApplyCameraTransform(Vector3.zero);
        }

        public void AddShake(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public void SetLookBackBlend(float blend01)
        {
            _lookBackBlend = Mathf.Clamp01(blend01);
        }

        private void LateUpdate()
        {
            if (target == null || _cam == null) return;

            float dt = Time.unscaledDeltaTime;

            // Subtle Hades cursor/aim look-ahead on the XZ floor
            Vector3 lead = Vector3.zero;
            if (OrpheusPlayerController.Instance != null)
            {
                lead = OrpheusPlayerController.Instance.AimDirectionXZ * aimLookAhead;
            }

            Vector3 normalGoal = target.position + lead;
            normalGoal.y = 0.55f;

            // Gently leash focus point inside the current chamber so void edges stay framed cleanly
            Vector3 offsetFromChamber = normalGoal - new Vector3(chamberCenter.x, 0.55f, chamberCenter.z);
            if (offsetFromChamber.magnitude > chamberCameraLeashRadius)
            {
                normalGoal = new Vector3(chamberCenter.x, 0.55f, chamberCenter.z)
                           + offsetFromChamber.normalized * chamberCameraLeashRadius;
            }

            Vector3 lookBackGoal = normalGoal;
            if (eurydiceTarget != null)
            {
                lookBackGoal = Vector3.Lerp(target.position, eurydiceTarget.position, 0.65f) + new Vector3(0f, 0.6f, 0f);
            }

            Vector3 desiredFocus = Vector3.Lerp(normalGoal, lookBackGoal, _lookBackBlend);
            _focusPoint = Vector3.SmoothDamp(_focusPoint, desiredFocus, ref _focusVel, smoothTime, Mathf.Infinity, dt);

            float targetZoom = _zoomOverride > 0f ? _zoomOverride : baseOrthoSize;
            targetZoom = Mathf.Lerp(targetZoom, 5.2f, _lookBackBlend);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetZoom, dt * 7f);

            // Screen-plane Perlin trauma shake
            Vector3 shakeWorld = Vector3.zero;
            if (_trauma > 0.001f)
            {
                float shake = _trauma * _trauma;
                float t = Time.unscaledTime * 44f;
                float sx = (Mathf.PerlinNoise(t, 1.3f) * 2f - 1f) * 0.58f * shake;
                float sy = (Mathf.PerlinNoise(2.7f, t) * 2f - 1f) * 0.58f * shake;
                shakeWorld = transform.right * sx + transform.up * sy;
                _trauma = Mathf.MoveTowards(_trauma, 0f, dt * 2.5f);
            }

            ApplyCameraTransform(shakeWorld);
        }

        private void ApplyCameraTransform(Vector3 shakeOffset)
        {
            transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
            transform.position = _focusPoint - (transform.forward * cameraDistance) + shakeOffset;
        }
    }
}
