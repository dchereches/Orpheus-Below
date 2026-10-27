using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    /// <summary>
    /// Dual-Mode Camera for Orpheus Below:
    ///   1) 2.5D Isometric Chamber-Crawler Mode (Pitch 36°, Yaw 45°) for multi-wave Tartarus brawls
    ///   2) 2D Side-View Parallax Boss Rhythm Mode (Pitch 4.5°, Yaw 45°) — smoothly glides down to eye-level
    ///      along the chamber's diagonal axis so Mini-Bosses & Cerberus play out in a cinematic 2D parallax view!
    /// </summary>
    public class UnderworldCamera2D : MonoBehaviour
    {
        public static UnderworldCamera2D Instance { get; private set; }

        public static readonly Vector3 IsoRightXZ = new Vector3(0.70710678f, 0f, -0.70710678f);
        public static readonly Vector3 IsoUpXZ = new Vector3(0.70710678f, 0f, 0.70710678f);
        public static readonly Quaternion Side2DRotation = Quaternion.Euler(4.5f, 45f, 0f);

        public static Quaternion CurrentBillboardRotation { get; private set; } = RenderPipeline2DSetup.IsoCameraRotation;

        [Header("Targets")]
        public Transform target;
        public Transform eurydiceTarget;

        [Header("2.5D Isometric Framing")]
        public float baseOrthoSize = 7.8f;
        public float cameraDistance = 36f;
        public float smoothTime = 0.11f;
        public float aimLookAhead = 0.80f;

        [Header("Active Chamber Leash")]
        public Vector3 chamberCenter = Vector3.zero;
        public float chamberCameraLeashRadius = 10f;

        public float SideViewBlend01 { get; private set; }
        public float CurrentSideParallaxOffset { get; private set; }
        public Camera Cam => _cam;

        private Camera _cam;
        private Vector3 _focusPoint;
        private Vector3 _focusVel;
        private float _trauma;
        private float _lookBackBlend;
        private float _zoomOverride = -1f;

        // 2D Boss Rhythm View state
        private bool _inSide2DBossMode;
        private Vector3 _sideStageCenter;
        private float _sideOrthoSize = 5.8f;

        private void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = gameObject.AddComponent<Camera>();

            RenderPipeline2DSetup.ConfigureCameraForIsometric25D(_cam, new Color(0.032f, 0.016f, 0.048f, 1f));
            _cam.orthographicSize = baseOrthoSize;
            CurrentBillboardRotation = RenderPipeline2DSetup.IsoCameraRotation;
        }

        private void Start()
        {
            // An immediate chamber boss can request its view before this component's first Start.
            if (target != null && !_inSide2DBossMode) SnapToTargetNow();
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

        /// <summary>
        /// Smoothly sweeps the camera from 2.5D Isometric down into the 2D Side-Profile Parallax Rhythm View!
        /// </summary>
        public void EnterSide2DBossView(Vector3 stageCenter, float sideOrtho = 5.9f)
        {
            _inSide2DBossMode = true;
            _sideStageCenter = stageCenter;
            _sideOrthoSize = sideOrtho;
        }

        /// <summary>
        /// Smoothly sweeps the camera back up from 2D Side-View to 2.5D Isometric Chamber-Crawler Mode!
        /// </summary>
        public void ExitSide2DBossView()
        {
            _inSide2DBossMode = false;
        }

        public void SnapToTargetNow()
        {
            if (target == null) return;
            _inSide2DBossMode = false;
            SideViewBlend01 = 0f;
            _lookBackBlend = 0f;
            _trauma = 0f;
            CurrentSideParallaxOffset = 0f;
            CurrentBillboardRotation = RenderPipeline2DSetup.IsoCameraRotation;
            _focusPoint = target.position;
            if (PaintedChamberMap.Active != null)
            {
                _cam.orthographicSize = PaintedChamberMap.Active.SafeZoom(_zoomOverride > 0 ? _zoomOverride : baseOrthoSize, _cam.aspect);
                _focusPoint = PaintedChamberMap.Active.ClampCameraFocus(_focusPoint, _cam.orthographicSize, _cam.aspect);
            }
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

            // Smoothly morph SideViewBlend01 (0 = 2.5D Isometric, 1 = 2D Side-View Boss Stage)
            float targetSide = _inSide2DBossMode ? 1f : 0f;
            SideViewBlend01 = Mathf.MoveTowards(SideViewBlend01, targetSide, dt * 1.65f);
            float smoothSide = Mathf.SmoothStep(0f, 1f, SideViewBlend01);

            CurrentBillboardRotation = Quaternion.Slerp(RenderPipeline2DSetup.IsoCameraRotation, Side2DRotation, smoothSide);

            // 1) Compute 2.5D Isometric Goal
            Vector3 lead = Vector3.zero;
            if (OrpheusPlayerController.Instance != null)
            {
                lead = OrpheusPlayerController.Instance.AimDirectionXZ * aimLookAhead;
            }
            Vector3 isoGoal = target.position + lead;
            isoGoal.y = 0.55f;

            Vector3 offsetFromChamber = isoGoal - new Vector3(chamberCenter.x, 0.55f, chamberCenter.z);
            if (offsetFromChamber.magnitude > chamberCameraLeashRadius)
            {
                isoGoal = new Vector3(chamberCenter.x, 0.55f, chamberCenter.z)
                        + offsetFromChamber.normalized * chamberCameraLeashRadius;
            }

            // 2) Compute 2D Side-View Boss Stage Goal (tracks horizontal player motion slightly for lush 2D parallax!)
            float playerHoriz = Vector3.Dot(target.position - _sideStageCenter, IsoRightXZ);
            CurrentSideParallaxOffset = Mathf.Lerp(CurrentSideParallaxOffset, playerHoriz, dt * 6f);
            Vector3 side2DGoal = _sideStageCenter
                               + IsoRightXZ * (playerHoriz * 0.32f + 0.6f)
                               + new Vector3(0f, 2.35f, 0f);

            Vector3 blendedBaseGoal = Vector3.Lerp(isoGoal, side2DGoal, smoothSide);

            // Optional [Hold Q] Look-Back blend
            Vector3 lookBackGoal = blendedBaseGoal;
            if (eurydiceTarget != null)
            {
                lookBackGoal = Vector3.Lerp(target.position, eurydiceTarget.position, 0.65f) + new Vector3(0f, 0.8f, 0f);
            }

            Vector3 desiredFocus = Vector3.Lerp(blendedBaseGoal, lookBackGoal, _lookBackBlend);
            _focusPoint = Vector3.SmoothDamp(_focusPoint, desiredFocus, ref _focusVel, smoothTime, Mathf.Infinity, dt);

            float isoZoom = _zoomOverride > 0f ? _zoomOverride : baseOrthoSize;
            float desiredZoom = Mathf.Lerp(isoZoom, _sideOrthoSize, smoothSide);
            desiredZoom = Mathf.Lerp(desiredZoom, 4.8f, _lookBackBlend);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, desiredZoom, dt * 6.5f);
            if (PaintedChamberMap.Active != null && SideViewBlend01 < .01f)
            {
                _cam.orthographicSize = PaintedChamberMap.Active.SafeZoom(_cam.orthographicSize, _cam.aspect);
                _focusPoint = PaintedChamberMap.Active.ClampCameraFocus(_focusPoint, _cam.orthographicSize, _cam.aspect);
            }

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
            transform.rotation = CurrentBillboardRotation;
            transform.position = _focusPoint - (transform.forward * cameraDistance) + shakeOffset;
        }
    }
}
