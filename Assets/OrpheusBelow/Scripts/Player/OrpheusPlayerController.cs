using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    /// <summary>
    /// Dual-Perspective HADES Controller for Orpheus:
    ///   - 2.5D Chamber-Crawler Mode : 360° Screen-Aligned Isometric Run (WASD) + 2-Charge Dash (vaults chasms!)
    ///   - 2D Boss Rhythm Stage Mode : Locks smoothly onto the horizontal 2D stage line (A/D + 2-Charge i-frame Dash)
    ///     facing the Rhythm Boss across the 2D Parallax Backdrop!
    /// </summary>
    public class OrpheusPlayerController : MonoBehaviour
    {
        public static OrpheusPlayerController Instance { get; private set; }

        [Header("Hades Run")]
        public float moveSpeed = 8.2f;
        public float acceleration = 220f;
        public float deceleration = 320f;
        public float bodyRadius = 0.38f;

        [Header("Hades Double-Dash ([Space] or [Left Shift])")]
        public int maxDashCharges = 2;
        public float dashDuration = 0.135f;
        public float dashStartSpeed = 30.5f;
        public float dashEndSpeed = 11.5f;
        public float dashRechargeTime = 0.48f;
        public float dashChainMinDelay = 0.055f;

        [Header("Health & Forbidden Look-Back ([Hold Q])")]
        public float maxHealth = 100f;
        public float lookBackHoldSeconds = 1.25f;

        public float CurrentHealth { get; private set; }
        public int CurrentDashCharges { get; private set; }
        public int DashUseCount { get; private set; }
        public bool IsDashing => _isDashing;
        public bool IsInDashStrikeWindow => _isDashing || _postDashStrikeTimer > 0f;
        public bool IsIn2DBossStage { get; private set; }
        public Vector3 MoveInputXZ { get; private set; }
        public Vector3 AimDirectionXZ { get; private set; } = new Vector3(0.7071f, 0f, 0.7071f);
        public float LookBackProgress01 { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsVictory { get; private set; }
        public string GameOverReason { get; private set; } = "";

        private Transform _billboardRoot;
        private Transform _spriteFlipRoot;
        private SpriteRenderer _cloakSr;
        private GameObject _proceduralVisualRoot;
        private OrpheusSpriteAnimator _spriteAnimator;
        private OrpheusGroundShadow _groundShadow;
        private Transform _aimRingRoot;
        private BlurredEurydice _eurydice;

        private Vector3 _velocityXZ;
        private bool _useMouseAim = true;

        // 2D Boss Stage Rail state
        private Vector3 _bossStageCenter;
        private float _bossStageMinX = -8.2f;
        private float _bossStageMaxX = 2.2f;
        private float _stageHorizPos;

        // Dash state
        private bool _isDashing;
        private float _dashTimer;
        private Vector3 _dashDirXZ;
        private float _dashChainCooldown;
        private float _dashRechargeTimer;
        private float _dashBufferTimer;
        private float _postDashStrikeTimer;
        private float _dashStreakTimer;

        // Micro-lunge & damage state
        private Vector3 _lungeVelXZ;
        private float _lungeTimer;
        private float _invulnTimer;
        private float _footstepDustTimer;
        private Vector3 _lastSafeGroundPos;
        private bool _lookedBackFatally;
        private Vector3 _lastAnimationPosition;

        private static readonly Collider[] OverlapBuf = new Collider[24];

        private void Awake()
        {
            Instance = this;
            CurrentHealth = maxHealth;
            CurrentDashCharges = maxDashCharges;

            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = bodyRadius;
            sc.center = new Vector3(0f, 0.65f, 0f);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            _lastSafeGroundPos = transform.position;
            _lastAnimationPosition = transform.position;
            BuildOrpheusVisuals();
            var depth = GetComponent<PaintedActorDepth>();
            if (depth == null) depth = gameObject.AddComponent<PaintedActorDepth>();
            depth.Exclude(_spriteAnimator.BodyRenderer);
            depth.Exclude(_spriteAnimator.EffectsRenderer);
        }

        private void Start()
        {
            _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();
        }

        private void BuildOrpheusVisuals()
        {
            _groundShadow = GetComponent<OrpheusGroundShadow>();
            if (_groundShadow == null) _groundShadow = gameObject.AddComponent<OrpheusGroundShadow>();
            _groundShadow.Initialize(this);

            _aimRingRoot = new GameObject("AimIndicatorRoot").transform;
            _aimRingRoot.SetParent(transform, false);
            _aimRingRoot.localPosition = new Vector3(0f, 0.03f, 0f);

            GameObject ringGo = new GameObject("FeetResonanceRing");
            ringGo.transform.SetParent(_aimRingRoot, false);
            ringGo.transform.localScale = new Vector3(1.10f, 1.10f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(ringGo, RenderPipeline2DSetup.RingSprite, new Color(1.8f, 1.45f, 0.35f, 0.38f), 2);

            GameObject arrowGo = new GameObject("AimChevron");
            arrowGo.transform.SetParent(_aimRingRoot, false);
            arrowGo.transform.localPosition = new Vector3(0f, 0f, 0.70f);
            arrowGo.transform.localScale = new Vector3(0.28f, 0.40f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(arrowGo, RenderPipeline2DSetup.DiamondSprite, new Color(2.4f, 1.95f, 0.45f, 0.85f), 3);

            _billboardRoot = new GameObject("OrpheusBillboard25D").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.localPosition = Vector3.zero;
            _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;

            _spriteFlipRoot = new GameObject("SpriteFlipRoot").transform;
            _spriteFlipRoot.SetParent(_billboardRoot, false);
            _spriteFlipRoot.localScale = Vector3.one * 1.2f;

            _proceduralVisualRoot = new GameObject("ProceduralOrpheusVisual");
            _proceduralVisualRoot.transform.SetParent(_spriteFlipRoot, false);

            GameObject cloakGo = new GameObject("OrpheusCloak");
            cloakGo.transform.SetParent(_proceduralVisualRoot.transform, false);
            cloakGo.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            cloakGo.transform.localScale = new Vector3(0.72f, 1.32f, 1f);
            _cloakSr = RenderPipeline2DSetup.AttachSprite(cloakGo, RenderPipeline2DSetup.DiamondSprite, new Color(1.15f, 0.16f, 0.28f, 1f), 14);

            GameObject headGo = new GameObject("OrpheusHead");
            headGo.transform.SetParent(_proceduralVisualRoot.transform, false);
            headGo.transform.localPosition = new Vector3(0.04f, 1.42f, -0.01f);
            headGo.transform.localScale = new Vector3(0.44f, 0.44f, 1f);
            RenderPipeline2DSetup.AttachSprite(headGo, RenderPipeline2DSetup.CircleSprite, new Color(0.97f, 0.89f, 0.79f, 1f), 15);

            GameObject lyreGo = new GameObject("GoldenLyre");
            lyreGo.transform.SetParent(_proceduralVisualRoot.transform, false);
            lyreGo.transform.localPosition = new Vector3(0.38f, 0.84f, -0.02f);
            lyreGo.transform.localScale = new Vector3(0.38f, 0.50f, 1f);
            RenderPipeline2DSetup.AttachSprite(lyreGo, RenderPipeline2DSetup.RingSprite, new Color(2.4f, 1.85f, 0.40f, 1f), 16);

            GameObject bodyGo = new GameObject("OrpheusBroadOilBody");
            bodyGo.transform.SetParent(_billboardRoot, false);
            SpriteRenderer body = bodyGo.AddComponent<SpriteRenderer>();
            body.sortingOrder = 15;

            GameObject effectsGo = new GameObject("OrpheusBroadOilEffects");
            effectsGo.transform.SetParent(_billboardRoot, false);
            SpriteRenderer effects = effectsGo.AddComponent<SpriteRenderer>();
            effects.sortingOrder = 16;

            _spriteAnimator = GetComponent<OrpheusSpriteAnimator>();
            if (_spriteAnimator == null) _spriteAnimator = gameObject.AddComponent<OrpheusSpriteAnimator>();
            _spriteAnimator.Initialize(body, effects, _proceduralVisualRoot,
                Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath));
        }

        public void Enter2DBossStage(Vector3 stageCenter, float minX = -8.2f, float maxX = 4.2f)
        {
            IsIn2DBossStage = true;
            _bossStageCenter = stageCenter;
            _bossStageMinX = minX;
            _bossStageMaxX = maxX;
            _stageHorizPos = -4.2f;
            _velocityXZ = Vector3.zero;
            _lungeTimer = 0f;
            _isDashing = false;
            CurrentDashCharges = maxDashCharges;

            if (_aimRingRoot != null) _aimRingRoot.gameObject.SetActive(false);
            if (_billboardRoot != null) _billboardRoot.localScale = Vector3.one * 0.70f;
        }

        public void Exit2DBossStage()
        {
            IsIn2DBossStage = false;
            if (_aimRingRoot != null) _aimRingRoot.gameObject.SetActive(true);
            if (_billboardRoot != null) _billboardRoot.localScale = Vector3.one;
            _lastSafeGroundPos = transform.position;
        }

        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            Mouse mouse = Mouse.current;

            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                OrpheusPrototypeBuilder.RestartDescent(true);
                return;
            }

            if (IsGameOver || IsVictory) { UpdateVisualPose(); return; }

            float dt = Time.deltaTime;
            if (_invulnTimer > 0f) _invulnTimer -= dt;
            if (_dashChainCooldown > 0f) _dashChainCooldown -= dt;
            if (_dashBufferTimer > 0f) _dashBufferTimer -= dt;
            if (_postDashStrikeTimer > 0f) _postDashStrikeTimer -= dt;

            // --- Optional Forbidden [Hold Q] Look-Back at Blurred Eurydice ---
            if (_eurydice == null) _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();
            bool holdingLookBack = (kb != null && kb.qKey.isPressed)
                                || (pad != null && pad.leftShoulder.isPressed);

            if (holdingLookBack && _eurydice != null && !_eurydice.IsVanished)
            {
                LookBackProgress01 = Mathf.MoveTowards(LookBackProgress01, 1f, Time.unscaledDeltaTime / Mathf.Max(0.25f, lookBackHoldSeconds));
                Time.timeScale = Mathf.Lerp(1f, 0.28f, LookBackProgress01);
                UnderworldCamera2D.Instance?.SetLookBackBlend(LookBackProgress01);
                _eurydice.SetLookBackFocus(LookBackProgress01);

                if (LookBackProgress01 >= 1f)
                {
                    TriggerLookBackLoss();
                    return;
                }
            }
            else if (LookBackProgress01 > 0f)
            {
                LookBackProgress01 = Mathf.MoveTowards(LookBackProgress01, 0f, Time.unscaledDeltaTime * 2.8f);
                Time.timeScale = Mathf.Lerp(1f, 0.28f, LookBackProgress01);
                UnderworldCamera2D.Instance?.SetLookBackBlend(LookBackProgress01);
                _eurydice?.SetLookBackFocus(LookBackProgress01);
            }

            // --- 1. Movement Input ---
            Vector2 rawMove = Vector2.zero;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) rawMove.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) rawMove.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) rawMove.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) rawMove.y += 1f;
            }
            if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > 0.04f)
            {
                rawMove = pad.leftStick.ReadValue();
                _useMouseAim = false;
            }

            // Dash Recharge & Input Buffer
            bool dashPressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame))
                            || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            if (dashPressed) _dashBufferTimer = 0.11f;

            if (!_isDashing && CurrentDashCharges < maxDashCharges)
            {
                _dashRechargeTimer -= dt;
                if (_dashRechargeTimer <= 0f) CurrentDashCharges = maxDashCharges;
            }

            if (IsIn2DBossStage)
            {
                Update2DBossStageMovement(dt, rawMove.x);
            }
            else
            {
                Update25DIsometricMovement(dt, rawMove, kb, mouse);
            }

            UpdateVisualPose();
        }

        private void Update2DBossStageMovement(float dt, float inputX)
        {
            MoveInputXZ = UnderworldCamera2D.IsoRightXZ * Mathf.Clamp(inputX, -1f, 1f);
            AimDirectionXZ = UnderworldCamera2D.IsoRightXZ; // Always face Right toward the Rhythm Boss!

            if (_dashBufferTimer > 0f && CurrentDashCharges > 0 && _dashChainCooldown <= 0f)
            {
                float dirSign = Mathf.Abs(inputX) > 0.05f ? Mathf.Sign(inputX) : 1f;
                _dashDirXZ = UnderworldCamera2D.IsoRightXZ * dirSign;
                BeginDashCommon();
            }

            if (_isDashing)
            {
                _dashTimer -= dt;
                float u = 1f - Mathf.Clamp01(_dashTimer / dashDuration);
                float curDashSpeed = Mathf.Lerp(dashStartSpeed, dashEndSpeed, u * u);
                float sign = Vector3.Dot(_dashDirXZ, UnderworldCamera2D.IsoRightXZ) >= 0f ? 1f : -1f;
                _stageHorizPos = Mathf.Clamp(_stageHorizPos + sign * curDashSpeed * dt, _bossStageMinX, _bossStageMaxX);

                _dashStreakTimer -= dt;
                if (_dashStreakTimer <= 0f)
                {
                    _dashStreakTimer = .07f;
                    SpawnDashStreak();
                }

                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                    _postDashStrikeTimer = 0.14f;
                    TriggerDashSettleAnimation();
                }
            }
            else
            {
                float curVx = Vector3.Dot(_velocityXZ, UnderworldCamera2D.IsoRightXZ);
                float desiredVx = Mathf.Clamp(inputX, -1f, 1f) * moveSpeed;
                float rate = Mathf.Abs(desiredVx) > 0.01f ? acceleration : deceleration;
                if (Mathf.Abs(inputX) < .05f) curVx = 0f;
                else
                {
                    if (curVx * desiredVx < 0f) curVx = 0f;
                    curVx = Mathf.MoveTowards(curVx, desiredVx, rate * dt);
                }
                _velocityXZ = UnderworldCamera2D.IsoRightXZ * curVx;
                _stageHorizPos = Mathf.Clamp(_stageHorizPos + curVx * dt, _bossStageMinX, _bossStageMaxX);
            }

            Vector3 desiredWorld = _bossStageCenter + UnderworldCamera2D.IsoRightXZ * _stageHorizPos;
            desiredWorld.y = 0f;
            transform.position = desiredWorld;
        }

        private void Update25DIsometricMovement(float dt, Vector2 rawMove, Keyboard kb, Mouse mouse)
        {
            MoveInputXZ = UnderworldCamera2D.InputToIsoWorldDir(rawMove);

            if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.5f || mouse.leftButton.isPressed || mouse.rightButton.isPressed))
            {
                _useMouseAim = true;
            }
            if (kb != null && (kb.jKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame) && MoveInputXZ.sqrMagnitude > 0.01f)
            {
                _useMouseAim = false;
            }

            if (_useMouseAim && UnderworldCamera2D.Instance != null && UnderworldCamera2D.Instance.TryGetCursorGroundPoint(out Vector3 cursorPt))
            {
                Vector3 toCursor = cursorPt - transform.position;
                toCursor.y = 0f;
                if (toCursor.sqrMagnitude > 0.02f) AimDirectionXZ = toCursor.normalized;
            }
            else if (MoveInputXZ.sqrMagnitude > 0.01f)
            {
                AimDirectionXZ = MoveInputXZ.normalized;
            }

            if (_aimRingRoot != null && AimDirectionXZ.sqrMagnitude > 0.001f)
            {
                _aimRingRoot.rotation = Quaternion.LookRotation(AimDirectionXZ, Vector3.up);
            }

            bool isFluting = OrpheusFluteChannel.Instance != null && OrpheusFluteChannel.Instance.IsFluteActive;
            if (isFluting)
            {
                _dashBufferTimer = 0f; // Dashing disabled while playing the Flute (A1)
            }

            if (_dashBufferTimer > 0f && CurrentDashCharges > 0 && _dashChainCooldown <= 0f)
            {
                _dashDirXZ = MoveInputXZ.sqrMagnitude > 0.02f ? MoveInputXZ.normalized : AimDirectionXZ;
                if (_dashDirXZ.sqrMagnitude < 0.001f) _dashDirXZ = UnderworldCamera2D.IsoUpXZ;
                BeginDashCommon();
            }

            if (_isDashing)
            {
                _dashTimer -= dt;
                float u = 1f - Mathf.Clamp01(_dashTimer / dashDuration);
                float curDashSpeed = Mathf.Lerp(dashStartSpeed, dashEndSpeed, u * u);
                MoveSweepXZ(_dashDirXZ * (curDashSpeed * dt), ignoreChasms: true);

                _dashStreakTimer -= dt;
                if (_dashStreakTimer <= 0f)
                {
                    _dashStreakTimer = .07f;
                    SpawnDashStreak();
                }

                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                    _postDashStrikeTimer = 0.14f;
                    TriggerDashSettleAnimation();
                    ResolvePostDashChasmLanding();
                }
            }
            else
            {
                if (_lungeTimer > 0f)
                {
                    _lungeTimer -= dt;
                    MoveSweepXZ(_lungeVelXZ * dt, ignoreChasms: false);
                    _lungeVelXZ = Vector3.Lerp(_lungeVelXZ, Vector3.zero, dt * 16f);
                }

                Vector3 desiredVel = MoveInputXZ * moveSpeed;
                if (isFluting) desiredVel *= 0.35f; // Slow movement while channeling the Flute (A1)
                float rate = desiredVel.sqrMagnitude > 0.01f ? acceleration : deceleration;
                if (MoveInputXZ.sqrMagnitude < .0025f) _velocityXZ = Vector3.zero;
                else
                {
                    // Releasing or reversing input plants the feet instead of spending frames coasting.
                    if (Vector3.Dot(_velocityXZ, desiredVel) < 0f) _velocityXZ = Vector3.zero;
                    _velocityXZ = Vector3.MoveTowards(_velocityXZ, desiredVel, rate * dt);
                }

                if (_velocityXZ.sqrMagnitude > 0.0005f)
                {
                    MoveSweepXZ(_velocityXZ * dt, ignoreChasms: false);
                    _footstepDustTimer -= dt;
                    if (_footstepDustTimer <= 0f)
                    {
                        _footstepDustTimer = 0.28f;
                        SpawnFootstepDust();
                        UnderworldSoundscape.Instance?.PlayFootstep(IsOverChasm(transform.position));
                    }
                }

                if (!IsOverChasm(transform.position))
                {
                    _lastSafeGroundPos = transform.position;
                }
            }
        }

        private void BeginDashCommon()
        {
            _dashBufferTimer = 0f;
            CurrentDashCharges--;
            DashUseCount++;
            _dashRechargeTimer = dashRechargeTime;
            _dashChainCooldown = dashChainMinDelay;
            _lungeTimer = 0f;
            _lungeVelXZ = Vector3.zero;
            _isDashing = true;
            _dashTimer = dashDuration;
            _dashStreakTimer = .07f;
            _velocityXZ = Vector3.zero;
            _invulnTimer = Mathf.Max(_invulnTimer, dashDuration + 0.05f);
            _spriteAnimator?.TriggerDashStart();

            ProceduralLyreAudio.Instance?.PlayDash();
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.04f, 0f), 1.55f, new Color(2.4f, 0.65f, 0.25f, 0.85f), 0.18f);
            SpawnDashStreak();
        }

        public void TriggerAttackLunge(Vector3 dirXZ, float speed, float duration)
        {
            if (IsIn2DBossStage) return; // Keep rhythm positioning razor-exact in 2D Boss Stage!

            if (_isDashing)
            {
                _isDashing = false;
                _postDashStrikeTimer = 0.12f;
                ResolvePostDashChasmLanding();
            }

            dirXZ.y = 0f;
            if (dirXZ.sqrMagnitude < 0.001f) dirXZ = AimDirectionXZ;
            _lungeVelXZ = dirXZ.normalized * speed;
            _lungeTimer = duration;
        }

        public void TeleportToChamberSpawn(Vector3 worldPos)
        {
            IsIn2DBossStage = false;
            if (_aimRingRoot != null) _aimRingRoot.gameObject.SetActive(true);
            _isDashing = false;
            _lungeTimer = 0f;
            _velocityXZ = Vector3.zero;
            worldPos.y = 0f;
            transform.position = worldPos;
            _lastSafeGroundPos = worldPos;
            _lastAnimationPosition = worldPos;
            _spriteAnimator?.ResetMotion();
            CurrentDashCharges = maxDashCharges;
        }

        public void RestoreTravelHealth(float health) { CurrentHealth = Mathf.Clamp(health, 1f, maxHealth); }

        private void MoveSweepXZ(Vector3 deltaXZ, bool ignoreChasms)
        {
            deltaXZ.y = 0f;
            var map = IsIn2DBossStage ? null : PaintedChamberMap.Active;
            Vector3 pos = SweepPositionXZ(transform.position, deltaXZ, ignoreChasms, map, true);
            Vector3 correction = DepenetrateXZ(pos, ignoreChasms) - pos;
            // Recovery must obey the same constraints, without a second independent sliding projection.
            pos = SweepPositionXZ(pos, correction, ignoreChasms, map, false);
            pos.y = 0f;
            transform.position = pos;
        }

        private Vector3 SweepPositionXZ(Vector3 pos, Vector3 deltaXZ, bool ignoreChasms, PaintedChamberMap map, bool slide)
        {
            for (int iter = 0; iter < 4; iter++)
            {
                float dist = deltaXZ.magnitude;
                if (dist < 0.0001f) break;

                Vector3 dir = deltaXZ / dist;
                Vector3 castOrigin = pos + new Vector3(0f, 0.5f, 0f);

                RaycastHit[] hits = Physics.SphereCastAll(castOrigin, bodyRadius, dir, dist + 0.02f, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                float bestDist = dist;
                Vector3 hitNormalXZ = Vector3.zero;

                for (int i = 0; i < hits.Length; i++)
                {
                    IsoObstacle obs = hits[i].collider.GetComponent<IsoObstacle>();
                    if (obs == null) continue;
                    if (ignoreChasms && obs.kind == IsoObstacleKind.DashableChasm) continue;
                    // SphereCastAll reports existing overlaps at distance zero with a synthetic opposing normal.
                    // Let this specific overlap decrease; every new obstacle still participates in the sweep.
                    if (hits[i].distance <= .0001f && ReducesInitialPenetration(hits[i].collider, pos, deltaXZ)) continue;

                    float moveAllowed = Mathf.Clamp(hits[i].distance - 0.015f, 0f, dist);
                    if (!blocked || moveAllowed < bestDist)
                    {
                        Vector3 n = hits[i].normal;
                        n.y = 0f;
                        if (n.sqrMagnitude > 0.001f)
                        {
                            blocked = true;
                            bestDist = moveAllowed;
                            hitNormalXZ = n.normalized;
                        }
                    }
                }

                // Physical gates and authored floor/cover participate in the same earliest-contact sweep.
                // Resolving either complete slide first can redirect the actor through the other constraint.
                if (map != null && map.SweepMovement(pos, deltaXZ, bodyRadius, out float fraction, out Vector3 paintedNormal))
                {
                    float moveAllowed = Mathf.Max(0f, fraction * dist - .01f);
                    if (!blocked || moveAllowed < bestDist)
                    {
                        blocked = true;
                        bestDist = moveAllowed;
                        hitNormalXZ = paintedNormal;
                    }
                }

                if (!blocked)
                {
                    pos += deltaXZ;
                    break;
                }

                pos += dir * bestDist;
                if (!slide) break;
                float remaining = dist - bestDist;
                deltaXZ = Vector3.ProjectOnPlane(dir, hitNormalXZ) * remaining;
            }
            return pos;
        }

        private bool ReducesInitialPenetration(Collider obstacle, Vector3 pos, Vector3 delta)
        {
            var body = GetComponent<SphereCollider>();
            if (body == null || !Physics.ComputePenetration(body, pos, Quaternion.identity,
                obstacle, obstacle.transform.position, obstacle.transform.rotation, out Vector3 escape, out float before))
                return false;
            // Reducing depth alone could otherwise permit crossing the entire obstacle to its opposite face.
            if (Vector3.Dot(delta, escape) <= .000001f) return false;
            return !Physics.ComputePenetration(body, pos + delta, Quaternion.identity,
                obstacle, obstacle.transform.position, obstacle.transform.rotation, out _, out float after)
                || after < before - .00001f;
        }

        private Vector3 DepenetrateXZ(Vector3 pos, bool ignoreChasms)
        {
            Vector3 sphereCenter = pos + new Vector3(0f, 0.5f, 0f);
            int count = Physics.OverlapSphereNonAlloc(sphereCenter, bodyRadius, OverlapBuf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider col = OverlapBuf[i];
                IsoObstacle obs = col.GetComponent<IsoObstacle>();
                if (obs == null) continue;
                if (ignoreChasms && obs.kind == IsoObstacleKind.DashableChasm) continue;

                if (Physics.ComputePenetration(
                    GetComponent<SphereCollider>(), pos, Quaternion.identity,
                    col, col.transform.position, col.transform.rotation,
                    out Vector3 pushDir, out float pushDist))
                {
                    pushDir.y = 0f;
                    if (pushDir.sqrMagnitude > 0.0001f)
                    {
                        pos += pushDir.normalized * (pushDist + 0.005f);
                    }
                }
            }
            return pos;
        }

        private bool IsOverChasm(Vector3 worldPos)
        {
            int count = Physics.OverlapSphereNonAlloc(worldPos + new Vector3(0f, 0.45f, 0f), bodyRadius * 0.72f, OverlapBuf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                IsoObstacle obs = OverlapBuf[i].GetComponent<IsoObstacle>();
                if (obs != null && obs.kind == IsoObstacleKind.DashableChasm) return true;
            }
            return false;
        }

        private bool IsBlockedOnLand(Vector3 worldPos)
        {
            int count = Physics.OverlapSphereNonAlloc(worldPos + new Vector3(0f, 0.5f, 0f), bodyRadius * 0.9f, OverlapBuf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (OverlapBuf[i].GetComponent<IsoObstacle>() != null) return true;
            }
            return false;
        }

        private void ResolvePostDashChasmLanding()
        {
            if (!IsOverChasm(transform.position))
            {
                _lastSafeGroundPos = transform.position;
                return;
            }

            for (float d = 0.25f; d <= 1.65f; d += 0.25f)
            {
                Vector3 forwardTest = transform.position + _dashDirXZ * d;
                if (!IsBlockedOnLand(forwardTest))
                {
                    transform.position = forwardTest;
                    _lastSafeGroundPos = forwardTest;
                    return;
                }
            }

            for (float r = 0.4f; r <= 1.5f; r += 0.35f)
            {
                for (int a = 0; a < 8; a++)
                {
                    float ang = a * Mathf.PI * 0.25f;
                    Vector3 test = transform.position + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                    if (!IsBlockedOnLand(test))
                    {
                        transform.position = test;
                        _lastSafeGroundPos = test;
                        return;
                    }
                }
            }

            OnHitStyxHazard(14f);
        }

        private void UpdateVisualPose()
        {
            if (_billboardRoot == null || _spriteFlipRoot == null) return;

            _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            float targetVisualScale = IsIn2DBossStage ? 0.70f : 1.0f;
            _billboardRoot.localScale = Vector3.one * targetVisualScale;

            if (_spriteAnimator != null)
            {
                Vector3 groundDelta = transform.position - _lastAnimationPosition;
                groundDelta.y = 0f;
                bool ascending = false;
                PaintedChamberMap map = PaintedChamberMap.Active;
                bool onStairs = map != null && map.definition != null &&
                    map.definition.StairMotion(transform.position - map.transform.position, groundDelta, out ascending);
                _spriteAnimator.Drive(new OrpheusAnimationInput
                {
                    groundDelta = groundDelta,
                    aimDirection = _isDashing ? _dashDirXZ : _lookedBackFatally ? -AimDirectionXZ : AimDirectionXZ,
                    deltaTime = Mathf.Max(Time.deltaTime, .0001f),
                    dashing = _isDashing,
                    onStairs = onStairs,
                    ascending = ascending,
                    defeated = IsGameOver,
                    bossStage = IsIn2DBossStage,
                    movementRequested = MoveInputXZ.sqrMagnitude > .0025f && !IsGameOver && !IsVictory
                });
                _lastAnimationPosition = transform.position;
                _groundShadow?.ApplyState(_isDashing, OrpheusGroundShadow.IsAttackState(_spriteAnimator.CurrentState),
                    PaintedChamberMap.Active?.definition);
            }

            float screenX = Vector3.Dot(_lookedBackFatally ? -AimDirectionXZ : AimDirectionXZ, UnderworldCamera2D.IsoRightXZ);
            if (Mathf.Abs(screenX) > 0.05f)
            {
                float sign = screenX >= 0f ? 1f : -1f;
                float runBob = (!_isDashing && _velocityXZ.sqrMagnitude > 0.5f)
                    ? Mathf.Abs(Mathf.Sin(Time.time * 17f)) * 0.11f
                    : 0f;
                _spriteFlipRoot.localScale = new Vector3(sign, 1f, 1f) * 1.2f;
                _spriteFlipRoot.localPosition = new Vector3(0f, runBob, 0f);
            }

            if (_cloakSr != null)
            {
                if (_isDashing)
                {
                    _cloakSr.color = new Color(2.6f, 0.85f, 0.30f, 0.92f);
                }
                else
                {
                    bool blink = _invulnTimer > 0f && Mathf.FloorToInt(Time.time * 24f) % 2 == 0;
                    _cloakSr.color = blink ? new Color(2.4f, 2.2f, 2.2f, 0.65f) : new Color(1.15f, 0.16f, 0.28f, 1f);
                }
            }
        }

        private void SpawnDashStreak()
        {
            var streak = new GameObject("OrpheusDashBrushStreak");
            streak.transform.position = transform.position - _dashDirXZ * .35f + Vector3.up * .045f;
            streak.transform.localScale = new Vector3(.35f, 1.5f, 1f);
            SpriteRenderer sr = RenderPipeline2DSetup.AttachGroundDecal(streak, RenderPipeline2DSetup.SoftGlowSprite,
                new Color(.85f, .30f, .12f, .24f), 3);
            streak.transform.rotation = Quaternion.LookRotation(_dashDirXZ.sqrMagnitude > .001f ? _dashDirXZ : Vector3.forward)
                * RenderPipeline2DSetup.GroundDecalRotation;
            streak.AddComponent<RisingAshParticle>().Init(sr, driftUpSpeed: 0f, duration: .12f);
        }

        private void SpawnFootstepDust()
        {
            GameObject dust = new GameObject("StepAsh");
            dust.transform.position = transform.position + new Vector3(Random.Range(-0.15f, 0.15f), 0.08f, Random.Range(-0.15f, 0.15f));
            dust.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
            SpriteRenderer sr = RenderPipeline2DSetup.AttachSprite(
                dust, RenderPipeline2DSetup.SoftGlowSprite, new Color(0.9f, 0.75f, 0.65f, 0.38f), 4);
            dust.transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            dust.AddComponent<RisingAshParticle>().Init(sr, driftUpSpeed: 0.65f, duration: 0.28f);
        }

        public void TakeDamage(float dmg, Vector3 sourcePos)
        {
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            if (IsGameOver || IsVictory || _invulnTimer > 0f || _isDashing) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - (dmg * DifficultySettings.DamageMultiplier));
            _spriteAnimator?.TriggerHit(dmg >= maxHealth * .25f);
            UnderworldSoundscape.Instance?.PlayHurt();
            _eurydice?.NotifyThreat(1.4f);
            _invulnTimer = 0.68f;

            OrpheusFluteChannel.Instance?.InterruptByDamage();

            if (!IsIn2DBossStage)
            {
                Vector3 push = transform.position - sourcePos;
                push.y = 0f;
                if (push.sqrMagnitude < 0.01f) push = -AimDirectionXZ;
                TriggerAttackLunge(push.normalized, 11f, 0.10f);
            }

            UnderworldCamera2D.Instance?.AddShake(0.38f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.05f, 0f), 2.1f, new Color(2.6f, 0.2f, 0.3f, 0.9f), 0.22f);

            if (CurrentHealth <= 0f)
            {
                IsGameOver = true;
                Time.timeScale = 1f;
                GameOverReason = "Overwhelmed by the Discord of Tartarus.";
            }
        }

        public void OnHitStyxHazard(float dmg)
        {
            if (IsGameOver || IsVictory) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - (dmg * DifficultySettings.DamageMultiplier));
            _spriteAnimator?.TriggerHit(dmg >= maxHealth * .25f);
            UnderworldCamera2D.Instance?.AddShake(0.42f);

            if (CurrentHealth <= 0f)
            {
                IsGameOver = true;
                Time.timeScale = 1f;
                GameOverReason = "Fell into the bottomless Stygian abyss.";
                return;
            }

            transform.position = _lastSafeGroundPos;
            _invulnTimer = 0.85f;
        }

        public void Heal(float amount)
        {
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        private void TriggerLookBackLoss()
        {
            _lookedBackFatally = true;
            _spriteAnimator?.TriggerLookBack(true);
            IsGameOver = true;
            Time.timeScale = 1f;
            ProceduralLyreAudio.Instance?.PlayLookBackTragedy();
            VoiceDirector.Instance?.Play(VoiceLineId.Hades_AMortals);
            _eurydice?.ShatterIntoAsh();
            GameOverReason = "Hades: \"A mortal's faith is brittle as dried reed. She returns to the dark, Orpheus. And you shall weep upon the earth alone.\"\n\nYou turned back — Eurydice scattered into Stygian ash.";
        }

        public void TriggerEurydiceAbductedLoss()
        {
            if (IsGameOver || IsVictory) return;
            IsGameOver = true;
            _spriteAnimator?.TriggerGrief();
            Time.timeScale = 1f;
            ProceduralLyreAudio.Instance?.PlayLookBackTragedy();
            GameOverReason = "Mourning Shades dragged Eurydice back into the Stygian dark while you pressed ahead.";
        }

        public void TriggerRunVictory()
        {
            if (IsGameOver || IsVictory) return;
            IsVictory = true;
            _isDashing = false;
            _velocityXZ = _lungeVelXZ = Vector3.zero;
            MoveInputXZ = Vector3.zero;
            _lungeTimer = _dashBufferTimer = 0f;
            _spriteAnimator?.TriggerVictory();
            _eurydice?.Celebrate(3f);
            Time.timeScale = 1f;

        }

        public void TriggerChamberClear() => _spriteAnimator?.TriggerChamberClear();
        public void TriggerDashSettleAnimation() => _spriteAnimator?.TriggerDashSettle();
        public void TriggerChamberEntranceAnimation() => _spriteAnimator?.TriggerChamberEntrance();
        public void TriggerAlertAnimation() => _spriteAnimator?.TriggerAlert();
        public void TriggerInspectAnimation() => _spriteAnimator?.TriggerInspect();
    }
}
