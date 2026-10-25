using UnityEngine;

namespace OrpheusBelow
{
    public interface IOrphicTarget
    {
        void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector2 hitOrigin);
    }

    /// <summary>
    /// Hades-inspired 2D Underworld Shade enemies:
    ///   - Darter Shade: fast lunger; takes bonus WALL-SLAM damage when launched into walls by a Hard Boom!
    ///   - Shielded Warden: blocks Soft Attacks until a HARD BOOM (>=0.60) shatters its bronze shield!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class ShadeEnemy : MonoBehaviour, IOrphicTarget
    {
        public bool hasBronzeShield;
        public float maxHealth = 60f;
        public float moveSpeed = 3.6f;
        public float aggroRange = 9.5f;
        public float contactDamage = 15f;

        private float _hp;
        private Rigidbody2D _rb;
        private SpriteRenderer _bodySr;
        private SpriteRenderer _shieldSr;
        private Color _baseColor;
        private float _stunTimer;
        private float _flashTimer;
        private float _attackCooldown;
        private bool _launchedForWallSlam;

        private void Awake()
        {
            _hp = maxHealth;
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 2.8f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void SetupVisuals(bool shielded)
        {
            hasBronzeShield = shielded;
            maxHealth = shielded ? 85f : 50f;
            _hp = maxHealth;
            _baseColor = shielded ? new Color(0.85f, 0.30f, 0.95f, 1f) : new Color(1.35f, 0.22f, 0.34f, 1f);

            _bodySr = RenderPipeline2DSetup.AttachSprite(gameObject, RenderPipeline2DSetup.DiamondSprite, _baseColor, 8);

            // Glowing eyes
            GameObject eyeGo = new GameObject("ShadeEyes");
            eyeGo.transform.SetParent(transform, false);
            eyeGo.transform.localPosition = new Vector3(-0.12f, 0.22f, 0f);
            eyeGo.transform.localScale = new Vector3(0.26f, 0.12f, 1f);
            RenderPipeline2DSetup.AttachSprite(eyeGo, RenderPipeline2DSetup.SquareSprite, new Color(2.2f, 1.8f, 0.4f, 1f), 9);

            if (shielded)
            {
                GameObject shGo = new GameObject("BronzeShield");
                shGo.transform.SetParent(transform, false);
                shGo.transform.localPosition = new Vector3(-0.48f, 0f, 0f);
                shGo.transform.localScale = new Vector3(0.22f, 1.15f, 1f);
                _shieldSr = RenderPipeline2DSetup.AttachSprite(shGo, RenderPipeline2DSetup.SquareSprite, new Color(1.8f, 1.35f, 0.35f, 1f), 10);
            }
        }

        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector2 hitOrigin)
        {
            if (_hp <= 0f) return;

            Vector2 dir = ((Vector2)transform.position - hitOrigin);
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
            dir.Normalize();

            if (hasBronzeShield)
            {
                if (!isHardBoom)
                {
                    // Soft note bounces off shield! Teaches player to punch/exhale HARD!
                    _flashTimer = 0.10f;
                    if (_shieldSr != null) _shieldSr.color = new Color(2.5f, 2.5f, 2.5f, 1f);
                    CombatVfxRing.Spawn(transform.position + new Vector3(-0.4f, 0f, 0f), 0.9f, new Color(1.4f, 1.2f, 0.5f, 0.8f), 0.18f);
                    return;
                }

                // HARD BOOM shatters the bronze shield!
                hasBronzeShield = false;
                if (_shieldSr != null) Destroy(_shieldSr.gameObject);
                CombatVfxRing.Spawn(transform.position, 2.8f, new Color(2.4f, 1.7f, 0.3f, 1f), 0.32f);
                UnderworldCamera2D.Instance?.AddShake(0.35f);
            }

            _hp -= damage;
            _flashTimer = 0.12f;
            if (_bodySr != null) _bodySr.color = new Color(2.8f, 2.8f, 2.8f, 1f);

            if (isHardBoom)
            {
                _stunTimer = 0.48f;
                _launchedForWallSlam = true;
                float kb = Mathf.Lerp(11f, 18f, intensity);
                _rb.linearVelocity = new Vector2(Mathf.Sign(dir.x) * kb, 5.2f);
            }
            else
            {
                _stunTimer = 0.16f;
                _rb.linearVelocity = new Vector2(Mathf.Sign(dir.x) * 3.8f, 2.2f);
            }

            if (_hp <= 0f)
            {
                Die();
            }
        }

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f)
                {
                    if (_bodySr != null) _bodySr.color = _baseColor;
                    if (_shieldSr != null) _shieldSr.color = new Color(1.8f, 1.35f, 0.35f, 1f);
                }
            }

            if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;
            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.deltaTime;
                if (_stunTimer <= 0f) _launchedForWallSlam = false;
                return;
            }

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver) return;

            float dx = player.transform.position.x - transform.position.x;
            float dist = Vector2.Distance(player.transform.position, transform.position);

            if (dist < aggroRange)
            {
                float dirX = Mathf.Sign(dx);
                if (_shieldSr != null)
                {
                    _shieldSr.transform.localPosition = new Vector3(dirX * 0.48f, 0f, 0f);
                }
                _rb.linearVelocity = new Vector2(dirX * moveSpeed, _rb.linearVelocity.y);
            }
            else
            {
                _rb.linearVelocity = new Vector2(Mathf.MoveTowards(_rb.linearVelocity.x, 0f, Time.deltaTime * 12f), _rb.linearVelocity.y);
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            // Hades WALL-SLAM bonus damage when launched into stone walls by a Hard Boom!
            if (_launchedForWallSlam && col.gameObject.GetComponent<OrpheusPlayerController>() == null)
            {
                foreach (ContactPoint2D cp in col.contacts)
                {
                    if (Mathf.Abs(cp.normal.x) > 0.65f)
                    {
                        _launchedForWallSlam = false;
                        _hp -= 25f;
                        CombatVfxRing.Spawn(cp.point, 2.0f, new Color(2.2f, 0.6f, 0.2f, 1f), 0.25f);
                        UnderworldCamera2D.Instance?.AddShake(0.25f);
                        if (_hp <= 0f) Die();
                        break;
                    }
                }
            }

            OrpheusPlayerController player = col.gameObject.GetComponent<OrpheusPlayerController>();
            if (player != null && _attackCooldown <= 0f)
            {
                _attackCooldown = 0.75f;
                player.TakeDamage(contactDamage, transform.position);
            }
        }

        private void Die()
        {
            CombatVfxRing.Spawn(transform.position, 2.4f, new Color(2.0f, 0.35f, 0.55f, 0.95f), 0.28f);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Cracked Underworld Pillar: immune to Soft Notes, shattered only by a HARD BOOM!
    /// </summary>
    public class CrackedStyxBarrier : MonoBehaviour, IOrphicTarget
    {
        private SpriteRenderer _sr;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector2 hitOrigin)
        {
            if (!isHardBoom)
            {
                if (_sr != null) _sr.color = new Color(0.75f, 0.65f, 0.85f, 1f);
                Invoke(nameof(RestoreTint), 0.1f);
                return;
            }

            CombatVfxRing.Spawn(transform.position, 3.8f, new Color(2.2f, 1.4f, 0.4f, 1f), 0.35f);
            UnderworldCamera2D.Instance?.AddShake(0.45f);
            Destroy(gameObject);
        }

        private void RestoreTint()
        {
            if (_sr != null) _sr.color = new Color(0.48f, 0.30f, 0.42f, 1f);
        }
    }

    /// <summary>
    /// Fragile Soul-Glass Bridge: safe under Soft Attacks, shatters if you fire a HARD BOOM right on it!
    /// Teaches precision breath/piezo control instead of mindlessly spamming max booms.
    /// </summary>
    public class FragileSoulBridge : MonoBehaviour, IOrphicTarget
    {
        private BoxCollider2D _col;
        private SpriteRenderer _sr;
        private Color _origColor;

        private void Awake()
        {
            _col = GetComponent<BoxCollider2D>();
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _origColor = _sr.color;
        }

        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector2 hitOrigin)
        {
            if (!isHardBoom || _col == null || !_col.enabled) return;

            StartCoroutine(ShatterAndRegenRoutine());
        }

        private System.Collections.IEnumerator ShatterAndRegenRoutine()
        {
            _col.enabled = false;
            if (_sr != null) _sr.color = new Color(_origColor.r, _origColor.g, _origColor.b, 0.12f);
            CombatVfxRing.Spawn(transform.position, 2.5f, new Color(0.5f, 1.8f, 2.2f, 0.9f), 0.3f);

            yield return new WaitForSeconds(3.5f);

            _col.enabled = true;
            if (_sr != null) _sr.color = _origColor;
        }
    }

    public class StyxHazardZone : MonoBehaviour
    {
        public float hazardDamage = 20f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            OrpheusPlayerController p = other.GetComponent<OrpheusPlayerController>();
            if (p != null)
            {
                p.OnHitStyxHazard(hazardDamage);
            }

            ShadeEnemy shade = other.GetComponent<ShadeEnemy>();
            if (shade != null)
            {
                shade.OnOrphicHit(999f, 1f, true, transform.position);
            }
        }
    }

    public class TemptationWhisperZone : MonoBehaviour
    {
        [TextArea]
        public string whisperMessage = "...Her footsteps stopped behind you. Is Eurydice still there?   [Hold Q to Look Back]";

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<OrpheusPlayerController>() != null)
            {
                BlurredEurydice eu = Object.FindFirstObjectByType<BlurredEurydice>();
                eu?.SetTemptationZone(true, whisperMessage);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<OrpheusPlayerController>() != null)
            {
                BlurredEurydice eu = Object.FindFirstObjectByType<BlurredEurydice>();
                eu?.SetTemptationZone(false, "");
            }
        }
    }

    public class HarmonicBoonShrine : MonoBehaviour
    {
        public string boonTitle = "Boon of Apollo: +25% Boom Radius & Full Heal!";
        private bool _claimed;
        private SpriteRenderer _sr;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (!_claimed)
            {
                transform.localScale = Vector3.one * (0.85f + Mathf.Sin(Time.time * 4f) * 0.08f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_claimed) return;
            OrpheusPlayerController p = other.GetComponent<OrpheusPlayerController>();
            if (p == null) return;

            _claimed = true;
            p.HealFull();
            OrpheusLyreCombat combat = p.GetComponent<OrpheusLyreCombat>();
            if (combat != null) combat.hardBoomMaxRadius *= 1.25f;

            ProceduralLyreAudio.Instance?.PlaySoftAttack(0.9f);
            CombatVfxRing.Spawn(transform.position, 4.2f, new Color(2.4f, 1.9f, 0.45f, 1f), 0.45f);
            OrpheusHud.Instance?.ShowBanner(boonTitle, 3.2f);
            if (_sr != null) _sr.color = new Color(0.3f, 0.3f, 0.35f, 0.35f);
        }
    }

    /// <summary>
    /// Expanding 2D shockwave ring VFX for Soft Notes, Hard Booms, Shield Breaks, and Rhythm Hits.
    /// </summary>
    public class CombatVfxRing : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _targetDiameter;
        private float _duration;
        private float _elapsed;
        private Color _startColor;

        public static void Spawn(Vector3 pos, float diameter, Color hdrColor, float duration)
        {
            GameObject go = new GameObject("OrphicShockwaveVFX");
            go.transform.position = new Vector3(pos.x, pos.y, -0.2f);
            go.transform.localScale = Vector3.one * 0.2f;

            CombatVfxRing fx = go.AddComponent<CombatVfxRing>();
            fx._sr = RenderPipeline2DSetup.AttachSprite(go, RenderPipeline2DSetup.RingSprite, hdrColor, 20);
            fx._targetDiameter = diameter;
            fx._duration = Mathf.Max(0.05f, duration);
            fx._startColor = hdrColor;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(_elapsed / _duration);
            float eased = 1f - Mathf.Pow(1f - u, 3f);
            float scale = Mathf.Lerp(0.25f, _targetDiameter, eased);
            transform.localScale = new Vector3(scale, scale, 1f);

            if (_sr != null)
            {
                Color c = _startColor;
                c.a = _startColor.a * (1f - u);
                _sr.color = c;
            }

            if (u >= 1f) Destroy(gameObject);
        }
    }
}
