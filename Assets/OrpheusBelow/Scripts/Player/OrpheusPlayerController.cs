using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    /// <summary>
    /// Tight 2D Hades-meets-Celeste controller for Orpheus:
    ///   - Snappy ground/air run, variable jump + coyote time + jump buffer
    ///   - Hades i-frame Dash with crimson-gold afterimages
    ///   - Forward-locked gaze ("Never Look Back") + forbidden [Hold Q] Turn-Back instant-loss mechanic!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class OrpheusPlayerController : MonoBehaviour
    {
        public static OrpheusPlayerController Instance { get; private set; }

        [Header("Movement & Jump")]
        public float moveSpeed = 9.2f;
        public float acceleration = 90f;
        public float jumpForce = 15.8f;
        public float coyoteTime = 0.13f;
        public float jumpBufferTime = 0.13f;
        public LayerMask groundMask = ~0;

        [Header("Hades Dash ([Left Shift])")]
        public float dashSpeed = 22.5f;
        public float dashDuration = 0.16f;
        public float dashCooldown = 0.42f;

        [Header("Health & Forbidden Look-Back ([Hold Q])")]
        public float maxHealth = 100f;
        public float lookBackHoldSeconds = 1.25f;

        public float CurrentHealth { get; private set; }
        public float LookBackProgress01 { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsVictory { get; private set; }
        public string GameOverReason { get; private set; } = "";
        public int FacingDir => _lookedBackFatally ? -1 : 1; // Always faces forward (+X) unless you break the vow!

        private Rigidbody2D _rb;
        private BoxCollider2D _col;
        private Transform _visualRoot;
        private SpriteRenderer _cloakSr;
        private BlurredEurydice _eurydice;

        private float _moveInputX;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private float _dashDir = 1f;
        private float _invulnTimer;
        private float _ghostSpawnTimer;
        private Vector3 _lastSafeGroundPos;
        private bool _lookedBackFatally;

        private void Awake()
        {
            Instance = this;
            CurrentHealth = maxHealth;
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();

            _rb.gravityScale = 3.6f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _lastSafeGroundPos = transform.position;

            BuildOrpheusVisuals();
        }

        private void Start()
        {
            _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();
        }

        private void BuildOrpheusVisuals()
        {
            _visualRoot = new GameObject("OrpheusVisuals").transform;
            _visualRoot.SetParent(transform, false);

            // Obsidian & Crimson Chiton/Cloak
            GameObject bodyGo = new GameObject("Cloak");
            bodyGo.transform.SetParent(_visualRoot, false);
            bodyGo.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            bodyGo.transform.localScale = new Vector3(0.72f, 1.35f, 1f);
            _cloakSr = RenderPipeline2DSetup.AttachSprite(bodyGo, RenderPipeline2DSetup.SquareSprite, new Color(0.95f, 0.16f, 0.26f, 1f), 10);

            // Head + Golden Laurel Crown
            GameObject headGo = new GameObject("Head");
            headGo.transform.SetParent(_visualRoot, false);
            headGo.transform.localPosition = new Vector3(0.05f, 0.76f, 0f);
            headGo.transform.localScale = new Vector3(0.46f, 0.46f, 1f);
            RenderPipeline2DSetup.AttachSprite(headGo, RenderPipeline2DSetup.CircleSprite, new Color(0.96f, 0.88f, 0.78f, 1f), 11);

            // Glowing Golden Lyre held forward (+X)
            GameObject lyreGo = new GameObject("GoldenLyre");
            lyreGo.transform.SetParent(_visualRoot, false);
            lyreGo.transform.localPosition = new Vector3(0.46f, 0.16f, 0f);
            lyreGo.transform.localScale = new Vector3(0.36f, 0.52f, 1f);
            RenderPipeline2DSetup.AttachSprite(lyreGo, RenderPipeline2DSetup.RingSprite, new Color(2.2f, 1.65f, 0.35f, 1f), 12);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                return;
            }

            if (IsGameOver || IsVictory) return;

            if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;
            if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.deltaTime;

            // --- Forbidden [Hold Q] Look-Back Mechanic ---
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

            // --- Horizontal Input ---
            _moveInputX = 0f;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) _moveInputX -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) _moveInputX += 1f;
            }
            if (pad != null && Mathf.Abs(pad.leftStick.ReadValue().x) > 0.15f)
            {
                _moveInputX = pad.leftStick.ReadValue().x;
            }

            // --- Ground Check & Coyote Time ---
            bool grounded = CheckGrounded();
            if (grounded)
            {
                _coyoteTimer = coyoteTime;
                if (Mathf.Abs(_rb.linearVelocity.y) < 0.2f)
                {
                    _lastSafeGroundPos = transform.position;
                }
            }
            else
            {
                _coyoteTimer -= Time.deltaTime;
            }

            // --- Jump Input + Buffer + Variable Height ---
            bool jumpPressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
                            || (pad != null && pad.buttonSouth.wasPressedThisFrame);

            bool jumpReleased = (kb != null && (kb.spaceKey.wasReleasedThisFrame || kb.wKey.wasReleasedThisFrame || kb.upArrowKey.wasReleasedThisFrame))
                             || (pad != null && pad.buttonSouth.wasReleasedThisFrame);

            if (jumpPressed) _jumpBufferTimer = jumpBufferTime;
            else _jumpBufferTimer -= Time.deltaTime;

            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && !_isDashing)
            {
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
            }

            if (jumpReleased && _rb.linearVelocity.y > 0f && !_isDashing)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * 0.50f);
            }

            // --- Hades Dash Input ---
            bool dashPressed = (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.lKey.wasPressedThisFrame))
                            || (pad != null && pad.buttonEast.wasPressedThisFrame);

            if (dashPressed && _dashCooldownTimer <= 0f && !_isDashing)
            {
                _isDashing = true;
                _dashTimer = dashDuration;
                _dashCooldownTimer = dashCooldown;
                _invulnTimer = Mathf.Max(_invulnTimer, dashDuration + 0.08f);
                _dashDir = Mathf.Abs(_moveInputX) > 0.1f ? Mathf.Sign(_moveInputX) : 1f;
                ProceduralLyreAudio.Instance?.PlayDash();
            }

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                _ghostSpawnTimer -= Time.deltaTime;
                if (_ghostSpawnTimer <= 0f)
                {
                    _ghostSpawnTimer = 0.035f;
                    SpawnDashAfterimage();
                }
                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.45f, _rb.linearVelocity.y);
                }
            }

            // Subtle invulnerability blink
            if (_cloakSr != null)
            {
                bool blink = _invulnTimer > 0f && !_isDashing && Mathf.FloorToInt(Time.time * 24f) % 2 == 0;
                _cloakSr.color = blink ? new Color(2.2f, 2.0f, 2.0f, 0.65f) : new Color(0.95f, 0.16f, 0.26f, 1f);
            }
        }

        private void FixedUpdate()
        {
            if (IsGameOver || IsVictory)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }

            if (_isDashing)
            {
                _rb.linearVelocity = new Vector2(_dashDir * dashSpeed, 0f);
                return;
            }

            float targetVx = _moveInputX * moveSpeed;
            float newVx = Mathf.MoveTowards(_rb.linearVelocity.x, targetVx, acceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(newVx, Mathf.Max(_rb.linearVelocity.y, -24f));
        }

        private bool CheckGrounded()
        {
            Bounds b = _col.bounds;
            Vector2 boxCenter = new Vector2(b.center.x, b.min.y - 0.06f);
            Vector2 boxSize = new Vector2(b.size.x * 0.86f, 0.10f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, groundMask);
            foreach (Collider2D h in hits)
            {
                if (h != _col && !h.isTrigger) return true;
            }
            return false;
        }

        private void SpawnDashAfterimage()
        {
            GameObject ghost = new GameObject("DashGhost");
            ghost.transform.position = transform.position;
            ghost.transform.localScale = new Vector3(0.70f, 1.35f, 1f);
            SpriteRenderer sr = RenderPipeline2DSetup.AttachSprite(ghost, RenderPipeline2DSetup.SquareSprite, new Color(2.0f, 0.45f, 0.25f, 0.55f), 5);
            ghost.AddComponent<RisingAshParticle>().Init(sr);
        }

        public void TakeDamage(float dmg, Vector2 sourcePos)
        {
            if (IsGameOver || IsVictory || _invulnTimer > 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);
            _invulnTimer = 0.85f;
            float dirX = Mathf.Sign(transform.position.x - sourcePos.x);
            if (Mathf.Abs(dirX) < 0.01f) dirX = -1f;
            _rb.linearVelocity = new Vector2(dirX * 8f, 6.5f);
            UnderworldCamera2D.Instance?.AddShake(0.38f);

            if (CurrentHealth <= 0f)
            {
                IsGameOver = true;
                Time.timeScale = 1f;
                GameOverReason = "STRUCK DOWN BY SHADES OF TARTARUS.";
            }
        }

        public void OnHitStyxHazard(float dmg)
        {
            if (IsGameOver || IsVictory) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);
            UnderworldCamera2D.Instance?.AddShake(0.45f);

            if (CurrentHealth <= 0f)
            {
                IsGameOver = true;
                GameOverReason = "CLAIMED BY THE RIVER STYX.";
                return;
            }

            transform.position = _lastSafeGroundPos + new Vector3(0f, 0.6f, 0f);
            _rb.linearVelocity = Vector2.zero;
            _invulnTimer = 1.0f;
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
            if (_visualRoot != null) _visualRoot.localScale = new Vector3(-1f, 1f, 1f); // Orpheus turns left!

            ProceduralLyreAudio.Instance?.PlayLookBackTragedy();
            _eurydice?.ShatterIntoAsh();
            GameOverReason = "YOU LOOKED BACK.\nEurydice came into clear focus for one heartbeat — then dissolved into Stygian ash.";
        }

        public void TriggerRunVictory()
        {
            if (IsGameOver || IsVictory) return;
            IsVictory = true;
            Time.timeScale = 1f;
        }
    }
}
