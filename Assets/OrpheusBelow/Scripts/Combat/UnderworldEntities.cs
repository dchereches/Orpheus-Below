using System;
using UnityEngine;

namespace OrpheusBelow
{
    public interface IOrphicTarget
    {
        void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector3 hitOrigin);
    }

    public enum ShadeArchetype
    {
        WretchLunger,
        BrimstoneCaster,
        ArmoredWarden
    }

    /// <summary>
    /// 2.5D Isometric HADES Underworld Shades:
    ///   - WretchLunger    : stalks in 360° XZ, telegraphs a crimson floor line, then lunges fast!
    ///   - BrimstoneCaster : keeps mid-range and fires destructible Stygian Orbs across the chamber!
    ///   - ArmoredWarden   : protected by Hades Yellow Armor (immune to hitstun until shattered by a >=60% HARD BOOM!)
    ///   - Full Hades WALL-SLAM physics when blasted into isometric walls or pillars by a Hard Boom!
    /// </summary>
    public class ShadeEnemy : MonoBehaviour, IOrphicTarget
    {
        public ShadeArchetype archetype = ShadeArchetype.WretchLunger;
        public bool hasBronzeShield;
        public float maxHealth = 60f;
        public float moveSpeed = 4.1f;

        public event Action<ShadeEnemy> OnShadeSlain;

        private float _hp;
        private Transform _billboardRoot;
        private SpriteRenderer _bodySr;
        private SpriteRenderer _armorAuraSr;
        private Transform _hpFillBar;
        private Transform _telegraphLine;
        private Color _baseColor;

        private float _stunTimer;
        private float _flashTimer;
        private Vector3 _knockbackVelXZ;
        private bool _flyingForWallSlam;

        // AI state
        private float _aiCooldown;
        private bool _windingUp;
        private float _windupTimer;
        private Vector3 _lungeDirXZ;
        private float _activeLungeTimer;

        private void Awake()
        {
            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.52f;
            sc.center = new Vector3(0f, 0.65f, 0f);
        }

        private void Start()
        {
            if (_bodySr == null)
            {
                SetupArchetype(archetype);
            }
        }

        public void SetupArchetype(ShadeArchetype type)
        {
            archetype = type;
            hasBronzeShield = (type == ShadeArchetype.ArmoredWarden);

            switch (type)
            {
                case ShadeArchetype.WretchLunger:
                    maxHealth = 52f;
                    moveSpeed = 4.4f;
                    _baseColor = new Color(1.45f, 0.22f, 0.36f, 1f);
                    break;
                case ShadeArchetype.BrimstoneCaster:
                    maxHealth = 44f;
                    moveSpeed = 3.2f;
                    _baseColor = new Color(1.15f, 0.28f, 1.35f, 1f);
                    break;
                case ShadeArchetype.ArmoredWarden:
                    maxHealth = 88f;
                    moveSpeed = 3.5f;
                    _baseColor = new Color(1.30f, 0.42f, 0.22f, 1f);
                    break;
            }

            _hp = maxHealth;
            _aiCooldown = UnityEngine.Random.Range(0.45f, 1.15f);

            // Ground shadow
            GameObject sh = new GameObject("ShadeShadow");
            sh.transform.SetParent(transform, false);
            sh.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            sh.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(sh, RenderPipeline2DSetup.CircleSprite, new Color(0f, 0f, 0f, 0.50f), 1);

            // Floor telegraph line (hidden until windup)
            GameObject tel = new GameObject("AttackTelegraphLine");
            tel.transform.SetParent(transform, false);
            tel.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            GameObject telStrip = new GameObject("Strip");
            telStrip.transform.SetParent(tel.transform, false);
            telStrip.transform.localPosition = new Vector3(0f, 0f, 2.2f);
            telStrip.transform.localScale = new Vector3(0.28f, 4.4f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(telStrip, RenderPipeline2DSetup.SquareSprite, new Color(2.8f, 0.22f, 0.25f, 0.65f), 3);
            _telegraphLine = tel.transform;
            _telegraphLine.gameObject.SetActive(false);

            // 2.5D Isometric Billboard Sprite Root
            _billboardRoot = new GameObject("ShadeBillboard25D").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;

            GameObject bodyGo = new GameObject("ShadeBody");
            bodyGo.transform.SetParent(_billboardRoot, false);
            bodyGo.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            bodyGo.transform.localScale = type == ShadeArchetype.ArmoredWarden
                ? new Vector3(1.05f, 1.48f, 1f)
                : new Vector3(0.84f, 1.28f, 1f);
            _bodySr = RenderPipeline2DSetup.AttachSprite(bodyGo,
                type == ShadeArchetype.BrimstoneCaster ? RenderPipeline2DSetup.RingSprite : RenderPipeline2DSetup.DiamondSprite,
                _baseColor, 11);

            GameObject eyeGo = new GameObject("ShadeEyes");
            eyeGo.transform.SetParent(_billboardRoot, false);
            eyeGo.transform.localPosition = new Vector3(0f, 0.96f, -0.01f);
            eyeGo.transform.localScale = new Vector3(0.32f, 0.12f, 1f);
            RenderPipeline2DSetup.AttachSprite(eyeGo, RenderPipeline2DSetup.SquareSprite, new Color(2.6f, 2.0f, 0.4f, 1f), 12);

            if (hasBronzeShield)
            {
                GameObject armorGo = new GameObject("HadesArmorOutline");
                armorGo.transform.SetParent(_billboardRoot, false);
                armorGo.transform.localPosition = new Vector3(0f, 0.72f, -0.02f);
                armorGo.transform.localScale = new Vector3(1.28f, 1.65f, 1f);
                _armorAuraSr = RenderPipeline2DSetup.AttachSprite(armorGo, RenderPipeline2DSetup.RingSprite, new Color(2.5f, 1.95f, 0.30f, 0.92f), 13);
            }

            // Overhead compact Hades HP bar
            GameObject barBg = new GameObject("HpBarBg");
            barBg.transform.SetParent(_billboardRoot, false);
            barBg.transform.localPosition = new Vector3(0f, 1.68f, -0.02f);
            barBg.transform.localScale = new Vector3(0.95f, 0.10f, 1f);
            RenderPipeline2DSetup.AttachSprite(barBg, RenderPipeline2DSetup.SquareSprite, new Color(0.08f, 0.03f, 0.08f, 0.85f), 14);

            GameObject barFill = new GameObject("HpBarFill");
            barFill.transform.SetParent(_billboardRoot, false);
            barFill.transform.localPosition = new Vector3(0f, 1.68f, -0.03f);
            barFill.transform.localScale = new Vector3(0.92f, 0.08f, 1f);
            RenderPipeline2DSetup.AttachSprite(barFill, RenderPipeline2DSetup.SquareSprite,
                hasBronzeShield ? new Color(2.4f, 1.95f, 0.30f, 1f) : new Color(2.2f, 0.25f, 0.35f, 1f), 15);
            _hpFillBar = barFill.transform;
        }

        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector3 hitOrigin)
        {
            if (_hp <= 0f) return;

            Vector3 pushDir = transform.position - hitOrigin;
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.001f) pushDir = UnderworldCamera2D.IsoUpXZ;
            pushDir.Normalize();

            if (hasBronzeShield)
            {
                if (!isHardBoom)
                {
                    // Hades Armor resists hitstun and takes reduced chip from Soft attacks — teaches Hard Boom!
                    _hp -= damage * 0.35f;
                    _flashTimer = 0.09f;
                    if (_armorAuraSr != null) _armorAuraSr.color = new Color(3f, 3f, 3f, 1f);
                    UpdateHpBar();
                    if (_hp <= 0f) Die();
                    return;
                }

                // HARD BOOM shatters Hades Armor immediately!
                hasBronzeShield = false;
                if (_armorAuraSr != null) Destroy(_armorAuraSr.gameObject);
                if (_hpFillBar != null) _hpFillBar.GetComponent<SpriteRenderer>().color = new Color(2.2f, 0.25f, 0.35f, 1f);
                GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 3.1f, new Color(2.6f, 1.95f, 0.35f, 1f), 0.28f);
                UnderworldCamera2D.Instance?.AddShake(0.32f);
            }

            // Interrupt enemy windup when unarmored (authentic Hades hitstun!)
            _windingUp = false;
            _activeLungeTimer = 0f;
            if (_telegraphLine != null) _telegraphLine.gameObject.SetActive(false);

            _hp -= damage;
            _flashTimer = 0.11f;
            if (_bodySr != null) _bodySr.color = new Color(2.9f, 2.9f, 2.9f, 1f);
            UpdateHpBar();

            if (isHardBoom)
            {
                _stunTimer = 0.46f;
                _flyingForWallSlam = true;
                float kbSpeed = Mathf.Lerp(15.5f, 23.5f, Mathf.InverseLerp(0.60f, 1.0f, intensity));
                _knockbackVelXZ = pushDir * kbSpeed;
            }
            else
            {
                _stunTimer = 0.20f;
                _knockbackVelXZ = pushDir * 5.2f;
            }

            if (_hp <= 0f) Die();
        }

        private void Update()
        {
            if (_billboardRoot != null) _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;

            float dt = Time.deltaTime;
            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f)
                {
                    if (_bodySr != null) _bodySr.color = _baseColor;
                    if (_armorAuraSr != null) _armorAuraSr.color = new Color(2.5f, 1.95f, 0.30f, 0.92f);
                }
            }

            // Knockback & Hades WALL-SLAM physics!
            if (_knockbackVelXZ.sqrMagnitude > 0.15f)
            {
                MoveEnemyXZ(_knockbackVelXZ * dt, checkWallSlam: _flyingForWallSlam);
                _knockbackVelXZ = Vector3.Lerp(_knockbackVelXZ, Vector3.zero, dt * 10f);
            }
            else
            {
                _flyingForWallSlam = false;
            }

            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                return;
            }

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            Vector3 dirToPlayer = dist > 0.01f ? toPlayer / dist : UnderworldCamera2D.IsoUpXZ;

            if (archetype == ShadeArchetype.BrimstoneCaster)
            {
                UpdateBrimstoneCaster(dt, dirToPlayer, dist);
            }
            else
            {
                UpdateMeleeLunger(dt, player, dirToPlayer, dist);
            }
        }

        private void UpdateMeleeLunger(float dt, OrpheusPlayerController player, Vector3 dirToPlayer, float dist)
        {
            if (_activeLungeTimer > 0f)
            {
                _activeLungeTimer -= dt;
                MoveEnemyXZ(_lungeDirXZ * (15.5f * dt), checkWallSlam: false);
                if (Vector3.Distance(transform.position, player.transform.position) < 0.95f)
                {
                    player.TakeDamage(archetype == ShadeArchetype.ArmoredWarden ? 18f : 12f, transform.position);
                    _activeLungeTimer = 0f;
                }
                return;
            }

            if (_windingUp)
            {
                _windupTimer -= dt;
                if (_windupTimer <= 0f)
                {
                    _windingUp = false;
                    if (_telegraphLine != null) _telegraphLine.gameObject.SetActive(false);
                    _activeLungeTimer = 0.24f;
                    _aiCooldown = 1.15f;
                }
                return;
            }

            if (_aiCooldown > 0f) _aiCooldown -= dt;

            if (dist > 3.4f)
            {
                MoveEnemyXZ(dirToPlayer * (moveSpeed * dt), checkWallSlam: false);
            }
            else if (_aiCooldown <= 0f)
            {
                // Start telegraphed Hades lunge!
                _windingUp = true;
                _windupTimer = 0.44f;
                _lungeDirXZ = dirToPlayer;
                if (_telegraphLine != null)
                {
                    _telegraphLine.gameObject.SetActive(true);
                    _telegraphLine.rotation = Quaternion.LookRotation(_lungeDirXZ, Vector3.up);
                }
            }
        }

        private void UpdateBrimstoneCaster(float dt, Vector3 dirToPlayer, float dist)
        {
            if (_aiCooldown > 0f) _aiCooldown -= dt;

            // Keep comfortable medium range
            if (dist < 4.5f)
            {
                MoveEnemyXZ(-dirToPlayer * (moveSpeed * 0.85f * dt), checkWallSlam: false);
            }
            else if (dist > 8.5f)
            {
                MoveEnemyXZ(dirToPlayer * (moveSpeed * dt), checkWallSlam: false);
            }

            if (_aiCooldown <= 0f && dist < 13f)
            {
                _aiCooldown = 1.85f;
                StygianOrbProjectile.Spawn(transform.position + new Vector3(0f, 0.6f, 0f) + dirToPlayer * 0.5f, dirToPlayer, 7.6f, 12f);
            }
        }

        private void MoveEnemyXZ(Vector3 deltaXZ, bool checkWallSlam)
        {
            deltaXZ.y = 0f;
            float dist = deltaXZ.magnitude;
            if (dist < 0.0001f) return;

            Vector3 dir = deltaXZ / dist;
            if (Physics.SphereCast(transform.position + new Vector3(0f, 0.5f, 0f), 0.45f, dir, out RaycastHit hit, dist + 0.04f, ~0, QueryTriggerInteraction.Ignore))
            {
                IsoObstacle obs = hit.collider.GetComponent<IsoObstacle>();
                if (obs != null)
                {
                    if (checkWallSlam && obs.kind == IsoObstacleKind.SolidWall)
                    {
                        // HADES WALL-SLAM!!
                        _flyingForWallSlam = false;
                        _knockbackVelXZ = Vector3.zero;
                        _hp -= 30f;
                        UpdateHpBar();
                        GroundShockwaveRing.Spawn(new Vector3(hit.point.x, 0.06f, hit.point.z), 2.6f, new Color(2.8f, 0.85f, 0.20f, 1f), 0.26f);
                        UnderworldCamera2D.Instance?.AddShake(0.28f);
                        OrpheusHud.Instance?.ShowBanner("WALL SLAM! (+30)", 1.1f);
                        if (_hp <= 0f) Die();
                        return;
                    }

                    if (checkWallSlam && obs.kind == IsoObstacleKind.DashableChasm)
                    {
                        // Knocked straight into the Stygian Chasm!
                        _hp = 0f;
                        GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.05f, 0f), 2.4f, new Color(0.3f, 2.2f, 1.3f, 1f), 0.3f);
                        Die();
                        return;
                    }

                    // Slide gently along walls during normal pursuit
                    Vector3 n = hit.normal;
                    n.y = 0f;
                    Vector3 slide = Vector3.ProjectOnPlane(deltaXZ, n.normalized);
                    transform.position += new Vector3(slide.x, 0f, slide.z);
                    return;
                }
            }

            transform.position += deltaXZ;
        }

        private void UpdateHpBar()
        {
            if (_hpFillBar == null) return;
            float pct = Mathf.Clamp01(_hp / Mathf.Max(1f, maxHealth));
            _hpFillBar.localScale = new Vector3(0.92f * pct, 0.08f, 1f);
            _hpFillBar.localPosition = new Vector3(-0.46f * (1f - pct), 1.68f, -0.03f);
        }

        private void Die()
        {
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 2.4f, new Color(2.4f, 0.30f, 0.55f, 0.95f), 0.26f);
            OnShadeSlain?.Invoke(this);
            OnShadeSlain = null;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Slow-moving glowing Hades Brimstone/Witch Orb — dodge with Dash i-frames or slice/boom out of the air!
    /// </summary>
    public class StygianOrbProjectile : MonoBehaviour
    {
        private Vector3 _dirXZ;
        private float _speed;
        private float _dmg;
        private float _life = 4.2f;

        public static void Spawn(Vector3 pos, Vector3 dirXZ, float speed, float damage)
        {
            GameObject go = new GameObject("StygianEnemyOrb");
            go.transform.position = pos;

            GameObject vis = new GameObject("OrbSprite");
            vis.transform.SetParent(go.transform, false);
            vis.transform.localScale = new Vector3(0.58f, 0.58f, 1f);
            RenderPipeline2DSetup.AttachBillboardSprite(vis, RenderPipeline2DSetup.CircleSprite, new Color(2.6f, 0.35f, 1.45f, 1f), 15);

            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.32f;

            StygianOrbProjectile p = go.AddComponent<StygianOrbProjectile>();
            p._dirXZ = dirXZ.normalized;
            p._speed = speed;
            p._dmg = damage;
        }

        public void ShatterOrb()
        {
            GroundShockwaveRing.Spawn(new Vector3(transform.position.x, 0.06f, transform.position.z), 1.2f, new Color(2.4f, 0.5f, 1.6f, 0.9f), 0.16f);
            Destroy(gameObject);
        }

        private void Update()
        {
            transform.position += _dirXZ * (_speed * Time.deltaTime);
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            IsoObstacle obs = other.GetComponent<IsoObstacle>();
            if (obs != null && obs.kind == IsoObstacleKind.SolidWall)
            {
                ShatterOrb();
                return;
            }

            OrpheusPlayerController player = other.GetComponent<OrpheusPlayerController>();
            if (player != null)
            {
                if (!player.IsDashing)
                {
                    player.TakeDamage(_dmg, transform.position);
                    ShatterOrb();
                }
            }
        }
    }

    /// <summary>
    /// Destructible 2.5D Cracked Stygian Pillar — blocks movement & orbs until shattered by a HARD BOOM!
    /// </summary>
    public class CrackedStyxBarrier : MonoBehaviour, IOrphicTarget
    {
        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector3 hitOrigin)
        {
            if (!isHardBoom)
            {
                GroundShockwaveRing.Spawn(new Vector3(transform.position.x, 0.06f, transform.position.z), 1.4f, new Color(1.2f, 0.9f, 1.4f, 0.7f), 0.16f);
                return;
            }

            GroundShockwaveRing.Spawn(new Vector3(transform.position.x, 0.06f, transform.position.z), 3.8f, new Color(2.6f, 1.5f, 0.35f, 1f), 0.32f);
            UnderworldCamera2D.Instance?.AddShake(0.42f);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Hades Chamber Reward Boon Orb — spawns when clearing an encounter!
    /// </summary>
    public class HarmonicBoonShrine : MonoBehaviour
    {
        public string boonTitle = "BOON OF APOLLO: +25% Orphic Boom Radius, +15% Attack & +35 Heal!";
        private bool _claimed;
        private Transform _orbVis;

        public static HarmonicBoonShrine SpawnAt(Transform parent, Vector3 groundPos, string title)
        {
            GameObject go = new GameObject("OlympianBoonOrb");
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(groundPos.x, 0f, groundPos.z);

            GameObject ring = new GameObject("FloorRune");
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ring.transform.localScale = new Vector3(1.9f, 1.9f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(ring, RenderPipeline2DSetup.RingSprite, new Color(2.5f, 1.95f, 0.45f, 0.85f), 4);

            GameObject orb = new GameObject("FloatingOrb");
            orb.transform.SetParent(go.transform, false);
            orb.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            orb.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            RenderPipeline2DSetup.AttachBillboardSprite(orb, RenderPipeline2DSetup.DiamondSprite, new Color(2.8f, 2.15f, 0.50f, 1f), 14);

            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.05f;

            HarmonicBoonShrine sh = go.AddComponent<HarmonicBoonShrine>();
            sh.boonTitle = title;
            sh._orbVis = orb.transform;
            return sh;
        }

        private void Update()
        {
            if (!_claimed && _orbVis != null)
            {
                _orbVis.rotation = RenderPipeline2DSetup.IsoCameraRotation;
                _orbVis.localPosition = new Vector3(0f, 0.95f + Mathf.Sin(Time.time * 3.8f) * 0.16f, 0f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_claimed) return;
            OrpheusPlayerController p = other.GetComponent<OrpheusPlayerController>();
            if (p == null) return;

            _claimed = true;
            p.Heal(35f);
            OrpheusLyreCombat combat = p.GetComponent<OrpheusLyreCombat>();
            if (combat != null)
            {
                combat.hardBoomMaxRadius *= 1.22f;
                combat.softBaseDamage *= 1.15f;
            }

            ProceduralLyreAudio.Instance?.PlaySoftAttack(0.95f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 4.4f, new Color(2.8f, 2.2f, 0.5f, 1f), 0.42f);
            OrpheusHud.Instance?.ShowBanner(boonTitle, 3.8f);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Hades Isometric Chamber Exit Archway — locked during combat, glows emerald-gold when cleared!
    /// </summary>
    public class ChamberExitDoor : MonoBehaviour
    {
        public int targetChamberIndex;
        public bool isFinalSunlightExit;
        public bool IsUnlocked { get; private set; }

        [SerializeField] private SpriteRenderer _portalSr;
        [SerializeField] private SpriteRenderer _floorBeaconSr;
        private bool _used;

        public void SetupVisuals(SpriteRenderer portalSr, SpriteRenderer floorBeaconSr)
        {
            _portalSr = portalSr;
            _floorBeaconSr = floorBeaconSr;
            SetUnlocked(false);
        }

        public void SetUnlocked(bool unlocked)
        {
            IsUnlocked = unlocked;
            if (_portalSr != null)
            {
                _portalSr.color = unlocked
                    ? (isFinalSunlightExit ? new Color(3.2f, 2.6f, 0.9f, 0.98f) : new Color(0.45f, 2.6f, 1.55f, 0.95f))
                    : new Color(1.5f, 0.15f, 0.25f, 0.55f);
            }
            if (_floorBeaconSr != null)
            {
                _floorBeaconSr.color = unlocked
                    ? new Color(0.6f, 2.4f, 1.5f, 0.85f)
                    : new Color(0.9f, 0.15f, 0.22f, 0.35f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsUnlocked || _used) return;
            OrpheusPlayerController p = other.GetComponent<OrpheusPlayerController>();
            if (p == null) return;

            _used = true;
            if (isFinalSunlightExit)
            {
                p.TriggerRunVictory();
            }
            else
            {
                HadesChamberDirector.Instance?.TransitionToChamber(targetChamberIndex);
            }
        }
    }

    public class TemptationWhisperZone : MonoBehaviour
    {
        [TextArea]
        public string whisperMessage = "...The Stygian mist swallows Eurydice's footsteps behind you.   [Hold Q to Look Back]";

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<OrpheusPlayerController>() != null)
            {
                UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>()?.SetTemptationZone(true, whisperMessage);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<OrpheusPlayerController>() != null)
            {
                UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>()?.SetTemptationZone(false, "");
            }
        }
    }

    /// <summary>
    /// Flat-on-the-ground (XZ) expanding shockwave ring for Hades Booms, Dashes, Wall-Slams, & Telegraphs!
    /// </summary>
    public class GroundShockwaveRing : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _targetDiameter;
        private float _duration;
        private float _elapsed;
        private Color _startColor;

        public static void Spawn(Vector3 worldPos, float diameter, Color hdrColor, float duration)
        {
            GameObject go = new GameObject("GroundShockwaveVFX");
            go.transform.position = worldPos;
            go.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

            GroundShockwaveRing fx = go.AddComponent<GroundShockwaveRing>();
            fx._sr = RenderPipeline2DSetup.AttachGroundDecal(go, RenderPipeline2DSetup.RingSprite, hdrColor, 18);
            fx._targetDiameter = diameter;
            fx._duration = Mathf.Max(0.05f, duration);
            fx._startColor = hdrColor;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(_elapsed / _duration);
            float eased = 1f - Mathf.Pow(1f - u, 3f);
            float sz = Mathf.Lerp(0.25f, _targetDiameter, eased);
            transform.localScale = new Vector3(sz, sz, 1f);

            if (_sr != null)
            {
                Color c = _startColor;
                c.a = _startColor.a * (1f - u);
                _sr.color = c;
            }

            if (u >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Flat-on-the-ground (XZ) sweeping Hades melee slash arc!
    /// </summary>
    public class GroundSlashArcVfx : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _life;
        private float _maxLife = 0.16f;
        private Color _col;

        public static void Spawn(Vector3 worldPos, Vector3 forwardXZ, float size, Color hdrColor, bool flipArc)
        {
            GameObject go = new GameObject("HadesSlashArcVFX");
            go.transform.position = worldPos;

            float yaw = Mathf.Atan2(forwardXZ.x, forwardXZ.z) * Mathf.Rad2Deg;
            // CrescentArcSprite points toward +X in sprite local space -> yaw - 90° aligns +X with forwardXZ!
            go.transform.rotation = Quaternion.Euler(90f, yaw - 90f, 0f);
            go.transform.localScale = new Vector3(size, flipArc ? -size : size, 1f);

            GroundSlashArcVfx fx = go.AddComponent<GroundSlashArcVfx>();
            fx._sr = RenderPipeline2DSetup.AttachSprite(go, RenderPipeline2DSetup.CrescentArcSprite, hdrColor, 17);
            fx._col = hdrColor;
            fx._life = fx._maxLife;
        }

        private void Update()
        {
            _life -= Time.unscaledDeltaTime;
            float u = 1f - Mathf.Clamp01(_life / _maxLife);
            if (_sr != null)
            {
                Color c = _col;
                c.a = _col.a * (1f - u * u);
                _sr.color = c;
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
