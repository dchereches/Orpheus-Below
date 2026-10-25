using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    /// <summary>
    /// Authentic 2.5D Isometric HADES Controller for Orpheus:
    ///   - 360° Screen-Aligned Isometric Run (WASD / Stick) with zero ice-skating & smooth 45°-wall sliding
    ///   - 2-Charge HADES DASH ([Space] / [Left Shift]): front-loaded burst, i-frames, enemy phase-through,
    ///     vaults across DashableChasm pits with generous far-ledge snap, and cancels attack recovery on frame 1!
    ///   - Dash-Strike window + Attack Micro-Lunge + 360° Cursor/Stick Aiming
    ///   - Optional [Hold Q] Forbidden Look-Back at Blurred Eurydice
    /// </summary>
    public class OrpheusPlayerController : MonoBehaviour
    {
        public static OrpheusPlayerController Instance { get; private set; }

        [Header("Hades Isometric Run")]
        public float moveSpeed = 9.6f;
        public float acceleration = 155f;
        public float deceleration = 185f;
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

        // Live state read by Combat, Camera, & HUD
        public float CurrentHealth { get; private set; }
        public int CurrentDashCharges { get; private set; }
        public float DashRechargeProgress01 => _dashRechargeTimer <= 0f ? 1f : 1f - Mathf.Clamp01(_dashRechargeTimer / dashRechargeTime);
        public bool IsDashing => _isDashing;
        public bool IsInDashStrikeWindow => _isDashing || _postDashStrikeTimer > 0f;
        public Vector3 MoveInputXZ { get; private set; }
        public Vector3 AimDirectionXZ { get; private set; } = new Vector3(0.7071f, 0f, 0.7071f);
        public float LookBackProgress01 { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsVictory { get; private set; }
        public string GameOverReason { get; private set; } = "";

        private Transform _billboardRoot;
        private Transform _spriteFlipRoot;
        private SpriteRenderer _cloakSr;
        private Transform _aimRingRoot;
        private BlurredEurydice _eurydice;

        private Vector3 _velocityXZ;
        private bool _useMouseAim = true;

        // Dash state
        private bool _isDashing;
        private float _dashTimer;
        private Vector3 _dashDirXZ;
        private float _dashChainCooldown;
        private float _dashRechargeTimer;
        private float _dashBufferTimer;
        private float _postDashStrikeTimer;
        private float _ghostSpawnTimer;

        // Attack micro-lunge & knockback state
        private Vector3 _lungeVelXZ;
        private float _lungeTimer;
        private float _invulnTimer;
        private float _footstepDustTimer;
        private Vector3 _lastSafeGroundPos;
        private bool _lookedBackFatally;

        private static readonly Collider[] OverlapBuf = new Collider[24];

        private void Awake()
        {
            Instance = this;
            CurrentHealth = maxHealth;
            CurrentDashCharges = maxDashCharges;

            // Small trigger SphereCollider so enemy projectiles/triggers detect Orpheus cleanly in 3D
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
            BuildOrpheusVisuals();
        }

        private void Start()
        {
            _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();
        }

        private void BuildOrpheusVisuals()
        {
            // Ground Contact Shadow (flat on XZ)
            GameObject shadowGo = new GameObject("GroundShadow");
            shadowGo.transform.SetParent(transform, false);
            shadowGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadowGo.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(shadowGo, RenderPipeline2DSetup.CircleSprite, new Color(0f, 0f, 0f, 0.52f), 1);

            // Ground Golden Aim Ring + Directional Chevron (flat on XZ, rotates toward AimDirectionXZ!)
            _aimRingRoot = new GameObject("AimIndicatorRoot").transform;
            _aimRingRoot.SetParent(transform, false);
            _aimRingRoot.localPosition = new Vector3(0f, 0.03f, 0f);

            GameObject ringGo = new GameObject("FeetResonanceRing");
            ringGo.transform.SetParent(_aimRingRoot, false);
            ringGo.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(ringGo, RenderPipeline2DSetup.RingSprite, new Color(1.8f, 1.45f, 0.35f, 0.42f), 2);

            GameObject arrowGo = new GameObject("AimChevron");
            arrowGo.transform.SetParent(_aimRingRoot, false);
            arrowGo.transform.localPosition = new Vector3(0f, 0f, 0.72f);
            arrowGo.transform.localScale = new Vector3(0.32f, 0.44f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(arrowGo, RenderPipeline2DSetup.DiamondSprite, new Color(2.4f, 1.95f, 0.45f, 0.90f), 3);

            // 2.5D Isometric Billboard Character Root (ready for AI-generated PNG sprites!)
            _billboardRoot = new GameObject("OrpheusBillboard25D").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.localPosition = Vector3.zero;
            _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;

            _spriteFlipRoot = new GameObject("SpriteFlipRoot").transform;
            _spriteFlipRoot.SetParent(_billboardRoot, false);

            // Crimson & Obsidian Chiton/Cloak
            GameObject cloakGo = new GameObject("OrpheusCloak");
            cloakGo.transform.SetParent(_spriteFlipRoot, false);
            cloakGo.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            cloakGo.transform.localScale = new Vector3(0.72f, 1.32f, 1f);
            _cloakSr = RenderPipeline2DSetup.AttachSprite(cloakGo, RenderPipeline2DSetup.DiamondSprite, new Color(1.15f, 0.16f, 0.28f, 1f), 12);

            // Head + Golden Laurel
            GameObject headGo = new GameObject("OrpheusHead");
            headGo.transform.SetParent(_spriteFlipRoot, false);
            headGo.transform.localPosition = new Vector3(0.04f, 1.42f, -0.01f);
            headGo.transform.localScale = new Vector3(0.44f, 0.44f, 1f);
            RenderPipeline2DSetup.AttachSprite(headGo, RenderPipeline2DSetup.CircleSprite, new Color(0.97f, 0.89f, 0.79f, 1f), 13);

            // Glowing Golden Lyre
            GameObject lyreGo = new GameObject("GoldenLyre");
            lyreGo.transform.SetParent(_spriteFlipRoot, false);
            lyreGo.transform.localPosition = new Vector3(0.38f, 0.84f, -0.02f);
            lyreGo.transform.localScale = new Vector3(0.38f, 0.50f, 1f);
            RenderPipeline2DSetup.AttachSprite(lyreGo, RenderPipeline2DSetup.RingSprite, new Color(2.4f, 1.85f, 0.40f, 1f), 14);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            Mouse mouse = Mouse.current;

            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                return;
            }

            if (IsGameOver || IsVictory) return;

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

            // --- 1. Screen-Aligned 360° WASD / Stick Input ---
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

            MoveInputXZ = UnderworldCamera2D.InputToIsoWorldDir(rawMove);

            // --- 2. 360° Cursor / Movement Aiming ---
            if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.5f || mouse.leftButton.isPressed || mouse.rightButton.isPressed))
            {
                _useMouseAim = true;
            }
            if (kb != null && (kb.jKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame))
            {
                if (MoveInputXZ.sqrMagnitude > 0.01f) _useMouseAim = false;
            }

            if (_useMouseAim && UnderworldCamera2D.Instance != null && UnderworldCamera2D.Instance.TryGetCursorGroundPoint(out Vector3 cursorPt))
            {
                Vector3 toCursor = cursorPt - transform.position;
                toCursor.y = 0f;
                if (toCursor.sqrMagnitude > 0.02f)
                {
                    AimDirectionXZ = toCursor.normalized;
                }
            }
            else if (MoveInputXZ.sqrMagnitude > 0.01f)
            {
                AimDirectionXZ = MoveInputXZ.normalized;
            }

            if (_aimRingRoot != null && AimDirectionXZ.sqrMagnitude > 0.001f)
            {
                _aimRingRoot.rotation = Quaternion.LookRotation(AimDirectionXZ, Vector3.up);
            }

            // --- 3. HADES DOUBLE-DASH ([Space] or [Left Shift] or Gamepad A) ---
            bool dashPressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame))
                            || (pad != null && pad.buttonSouth.wasPressedThisFrame);

            if (dashPressed)
            {
                _dashBufferTimer = 0.11f;
            }

            // Recharge both charges rapidly once not actively dashing (exact Hades Greater Reflex cadence!)
            if (!_isDashing && CurrentDashCharges < maxDashCharges)
            {
                _dashRechargeTimer -= dt;
                if (_dashRechargeTimer <= 0f)
                {
                    CurrentDashCharges = maxDashCharges;
                }
            }

            if (_dashBufferTimer > 0f && CurrentDashCharges > 0 && _dashChainCooldown <= 0f)
            {
                StartHadesDash();
            }

            // --- 4. Execute Movement / Dash / Micro-Lunge on XZ Plane ---
            if (_isDashing)
            {
                _dashTimer -= dt;
                float u = 1f - Mathf.Clamp01(_dashTimer / dashDuration);
                float curDashSpeed = Mathf.Lerp(dashStartSpeed, dashEndSpeed, u * u);

                // Dash ignores Chasms and Enemies, sliding only along SolidWalls!
                MoveSweepXZ(_dashDirXZ * (curDashSpeed * dt), ignoreChasms: true);

                _ghostSpawnTimer -= dt;
                if (_ghostSpawnTimer <= 0f)
                {
                    _ghostSpawnTimer = 0.028f;
                    SpawnDashAfterimage();
                }

                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                    _postDashStrikeTimer = 0.14f;
                    ResolvePostDashChasmLanding();
                }
            }
            else
            {
                // Attack micro-lunge decay
                if (_lungeTimer > 0f)
                {
                    _lungeTimer -= dt;
                    MoveSweepXZ(_lungeVelXZ * dt, ignoreChasms: false);
                    _lungeVelXZ = Vector3.Lerp(_lungeVelXZ, Vector3.zero, dt * 16f);
                }

                // Instantaneous Hades Run acceleration & stop
                Vector3 desiredVel = MoveInputXZ * moveSpeed;
                float rate = desiredVel.sqrMagnitude > 0.01f ? acceleration : deceleration;
                _velocityXZ = Vector3.MoveTowards(_velocityXZ, desiredVel, rate * dt);

                if (_velocityXZ.sqrMagnitude > 0.0005f)
                {
                    MoveSweepXZ(_velocityXZ * dt, ignoreChasms: false);

                    _footstepDustTimer -= dt;
                    if (_footstepDustTimer <= 0f)
                    {
                        _footstepDustTimer = 0.14f;
                        SpawnFootstepDust();
                    }
                }

                // Record safe solid ground for chasm recovery
                if (!IsOverChasm(transform.position))
                {
                    _lastSafeGroundPos = transform.position;
                }
            }

            UpdateVisualPose();
        }

        private void StartHadesDash()
        {
            _dashBufferTimer = 0f;
            CurrentDashCharges--;
            _dashRechargeTimer = dashRechargeTime;
            _dashChainCooldown = dashChainMinDelay;

            // Cancel any attack micro-lunge immediately on frame 1!
            _lungeTimer = 0f;
            _lungeVelXZ = Vector3.zero;

            _isDashing = true;
            _dashTimer = dashDuration;
            _invulnTimer = Mathf.Max(_invulnTimer, dashDuration + 0.05f);

            // Hades rule: Dash moves in held WASD direction; if neutral, dashes toward facing/aim!
            _dashDirXZ = MoveInputXZ.sqrMagnitude > 0.02f ? MoveInputXZ.normalized : AimDirectionXZ;
            if (_dashDirXZ.sqrMagnitude < 0.001f) _dashDirXZ = UnderworldCamera2D.IsoUpXZ;

            ProceduralLyreAudio.Instance?.PlayDash();
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.04f, 0f), 1.55f, new Color(2.4f, 0.65f, 0.25f, 0.85f), 0.18f);
            SpawnDashAfterimage();
        }

        /// <summary>
        /// Called by OrpheusLyreCombat when attacking during a Dash (Hades Dash-Strike!) or stepping into a combo swing.
        /// </summary>
        public void TriggerAttackLunge(Vector3 dirXZ, float speed, float duration)
        {
            if (_isDashing)
            {
                // Convert tail of Dash into a forward-committed Dash-Strike lunge!
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
            _isDashing = false;
            _lungeTimer = 0f;
            _velocityXZ = Vector3.zero;
            worldPos.y = 0f;
            transform.position = worldPos;
            _lastSafeGroundPos = worldPos;
            CurrentDashCharges = maxDashCharges;
        }

        /// <summary>
        /// Custom 3-iteration Hades XZ Sweep-and-Slide solver:
        /// Glides smoothly along 45° isometric diamond walls & pillars with zero snagging!
        /// </summary>
        private void MoveSweepXZ(Vector3 deltaXZ, bool ignoreChasms)
        {
            deltaXZ.y = 0f;
            Vector3 pos = transform.position;

            for (int iter = 0; iter < 3; iter++)
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

                    float moveAllowed = Mathf.Max(0f, hits[i].distance - 0.015f);
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

                if (!blocked)
                {
                    pos += deltaXZ;
                    break;
                }

                pos += dir * bestDist;
                float remaining = dist - bestDist;
                Vector3 slideDir = Vector3.ProjectOnPlane(dir, hitNormalXZ);
                deltaXZ = slideDir * remaining;
            }

            // Depenetrate against SolidWalls (and Chasms when not dashing) so tight corners never trap the player
            pos = DepenetrateXZ(pos, ignoreChasms);
            pos.y = 0f;
            transform.position = pos;
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

        /// <summary>
        /// Exact Hades Ledge-Forgiveness: if a Dash ends just shy of the far chasm lip, snap smoothly onto solid bank!
        /// </summary>
        private void ResolvePostDashChasmLanding()
        {
            if (!IsOverChasm(transform.position))
            {
                _lastSafeGroundPos = transform.position;
                return;
            }

            // 1) Try nudging forward along _dashDirXZ first (up to 1.65m generous far-bank assist!)
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

            // 2) Radial search around landing point for nearest dry stone
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

            // 3) Dashed dead-center into wide Styx abyss -> minor Styx singe & return to safe bank
            OnHitStyxHazard(12f);
        }

        private void UpdateVisualPose()
        {
            if (_billboardRoot == null || _spriteFlipRoot == null) return;

            _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;

            // Flip sprite left/right based on screen-horizontal projection of AimDirectionXZ
            float screenX = Vector3.Dot(_lookedBackFatally ? -AimDirectionXZ : AimDirectionXZ, UnderworldCamera2D.IsoRightXZ);
            if (Mathf.Abs(screenX) > 0.05f)
            {
                float sign = screenX >= 0f ? 1f : -1f;
                float runBob = (!_isDashing && _velocityXZ.sqrMagnitude > 0.5f)
                    ? Mathf.Abs(Mathf.Sin(Time.time * 17f)) * 0.11f
                    : 0f;
                _spriteFlipRoot.localScale = new Vector3(sign, 1f, 1f);
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

        private void SpawnDashAfterimage()
        {
            GameObject ghost = new GameObject("HadesDashAfterimage");
            ghost.transform.position = transform.position + new Vector3(0f, 0.68f, 0f);
            ghost.transform.localScale = new Vector3(0.70f, 1.32f, 1f);
            SpriteRenderer sr = RenderPipeline2DSetup.AttachBillboardSprite(
                ghost, RenderPipeline2DSetup.DiamondSprite, new Color(2.4f, 0.50f, 0.22f, 0.62f), 9);
            ghost.AddComponent<RisingAshParticle>().Init(sr, driftUpSpeed: 0.5f, duration: 0.22f);
        }

        private void SpawnFootstepDust()
        {
            GameObject dust = new GameObject("StepAsh");
            dust.transform.position = transform.position + new Vector3(Random.Range(-0.15f, 0.15f), 0.08f, Random.Range(-0.15f, 0.15f));
            dust.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
            SpriteRenderer sr = RenderPipeline2DSetup.AttachBillboardSprite(
                dust, RenderPipeline2DSetup.SoftGlowSprite, new Color(0.9f, 0.75f, 0.65f, 0.38f), 4);
            dust.AddComponent<RisingAshParticle>().Init(sr, driftUpSpeed: 0.65f, duration: 0.28f);
        }

        public void TakeDamage(float dmg, Vector3 sourcePos)
        {
            if (IsGameOver || IsVictory || _invulnTimer > 0f || _isDashing) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);
            _invulnTimer = 0.72f;

            Vector3 push = transform.position - sourcePos;
            push.y = 0f;
            if (push.sqrMagnitude < 0.01f) push = -AimDirectionXZ;
            TriggerAttackLunge(push.normalized, 11f, 0.10f);

            UnderworldCamera2D.Instance?.AddShake(0.38f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.05f, 0f), 2.1f, new Color(2.6f, 0.2f, 0.3f, 0.9f), 0.22f);

            if (CurrentHealth <= 0f)
            {
                IsGameOver = true;
                Time.timeScale = 1f;
                GameOverReason = "TAKEN BY THE STYX —\"There is no escape.\"";
            }
        }

        public void OnHitStyxHazard(float dmg)
        {
            if (IsGameOver || IsVictory) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);
            UnderworldCamera2D.Instance?.AddShake(0.42f);

            if (CurrentHealth <= 0f)
            {
                IsGameOver = true;
                Time.timeScale = 1f;
                GameOverReason = "CLAIMED BY THE BOILING RIVER STYX.";
                return;
            }

            transform.position = _lastSafeGroundPos;
            _invulnTimer = 0.85f;
        }

        public void Heal(float amount)
        {
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        public void HealFull()
        {
            CurrentHealth = maxHealth;
        }

        private void TriggerLookBackLoss()
        {
            _lookedBackFatally = true;
            IsGameOver = true;
            Time.timeScale = 1f;
            ProceduralLyreAudio.Instance?.PlayLookBackTragedy();
            _eurydice?.ShatterIntoAsh();
            GameOverReason = "YOU LOOKED BACK.\nEurydice sharpened into clear focus for one heartbeat — then dissolved into Stygian ash.";
        }

        public void TriggerRunVictory()
        {
            if (IsGameOver || IsVictory) return;
            IsVictory = true;
            Time.timeScale = 1f;
        }
    }
}
