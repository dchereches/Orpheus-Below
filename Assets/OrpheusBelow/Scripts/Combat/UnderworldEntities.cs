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
        ArmoredWarden,
        MourningAbductor,
        SummonedSheep,
        MaskedMonk,
        WingedSentinel,
        Shepherd
    }

    public readonly struct ShadeBalance
    {
        public readonly float MaxHealth;
        public readonly float MoveSpeed;
        public readonly float ContactDamage;
        public readonly float AttackRecovery;
        public readonly float LungeTelegraph;
        public readonly float OrbDamage;
        public readonly float VolleyCooldown;

        private ShadeBalance(float maxHealth, float moveSpeed, float contactDamage,
            float attackRecovery, float lungeTelegraph, float orbDamage, float volleyCooldown)
        {
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            ContactDamage = contactDamage;
            AttackRecovery = attackRecovery;
            LungeTelegraph = lungeTelegraph;
            OrbDamage = orbDamage;
            VolleyCooldown = volleyCooldown;
        }

        public static ShadeBalance Get(ShadeArchetype archetype)
        {
            switch (archetype)
            {
                case ShadeArchetype.WretchLunger:
                    return new ShadeBalance(86f, 5.4f, 18f, 0.68f, 0.34f, 0f, 0f);
                case ShadeArchetype.BrimstoneCaster:
                    return new ShadeBalance(78f, 3.8f, 0f, 0f, 0.34f, 17f, 1.12f);
                case ShadeArchetype.ArmoredWarden:
                    return new ShadeBalance(150f, 4.3f, 24f, 0.68f, 0.34f, 0f, 0f);
                case ShadeArchetype.SummonedSheep:
                    return new ShadeBalance(36f, 7f, 14f, .9f, .7f, 0f, 0f);
                case ShadeArchetype.MaskedMonk:
                    return new ShadeBalance(300f,2.1f,0f,1.05f,1.35f,36f,0f);
                case ShadeArchetype.WingedSentinel:
                    return new ShadeBalance(380f,1.7f,38f,1.3f,1.8f,0f,0f);
                case ShadeArchetype.Shepherd:
                    return new ShadeBalance(340f,2f,0f,1.4f,1.2f,0f,0f);
                default:
                    return new ShadeBalance(82f, 4.6f, 0f, 0f, 0.34f, 0f, 0f);
            }
        }
    }

    /// <summary>
    /// Tormented Underworld Shades bound by Discord & Grief (Pacified by Orpheus's Lyre rather than slaughtered!):
    ///   - WretchLunger     : fast pursuit (5.4 m/s), short 0.34s crimson floor telegraph, 18.5 m/s lunge
    ///   - BrimstoneCaster  : fires twin/triple spread volleys of Stygian Orbs across the room
    ///   - ArmoredWarden    : heavy bronze Grief-Armor; resists soft chip until cracked by >=60% Hard Boom or S-B-S Vortex
    ///   - MourningAbductor : ignores Orpheus and glides straight for Blurred Eurydice to drag her Soul Tether into the dark!
    /// </summary>
    public class ShadeEnemy : MonoBehaviour, IOrphicTarget
    {
        public ShadeArchetype archetype = ShadeArchetype.WretchLunger;
        public bool hasBronzeShield;
        public float maxHealth = 65f;
        public float moveSpeed = 5.2f;

        public event Action<ShadeEnemy> OnShadeSlain;

        public bool IsVulnerableToFlute => !hasBronzeShield && (_hp <= maxHealth * 0.70f || _resonance.ExposedRemaining > 0f);
        public bool HasGriefShield => hasBronzeShield;
        public bool IsFleeing { get; private set; }
        public float CurrentHealth => _hp;
        public bool IsPacified => _pacified;
        public bool IsStunned => _stunTimer > 0f;
        public bool IsMiniBoss => ChamberEncounterPlan.IsMiniBoss(archetype);
        public PaintedEnemyActor PaintedActor => _paintedActor;
        public string DisplayName => archetype == ShadeArchetype.MaskedMonk ? "Masked Monk" : archetype == ShadeArchetype.WingedSentinel ? "Winged Sentinel" : archetype == ShadeArchetype.Shepherd ? "Shepherd of the Lost Flock" : ChamberEncounterPlan.ArtFor(archetype).ToString();

        private float _hp;
        private bool _pacified;
        private BlurredEurydice _eurydice;

        private Transform _billboardRoot;
        private SpriteRenderer _bodySr;
        private SpriteRenderer _armorAuraSr;
        private SpriteRenderer _eyesSr;
        private Transform _hpFillBar;
        private Transform _telegraphLine;
        private SpriteRenderer _telegraphSr;
        private Color _baseColor;
        private ShadeBalance _balance;
        private PaintedEnemyActor _paintedActor;
        private readonly ShadeResonance _resonance = new ShadeResonance();
        private float _threatMultiplier = 1f;

        public void SetChamberPressure(int chamberIndex)
        {
            _threatMultiplier = 0.90f + Mathf.Max(0, chamberIndex) * 0.04f;
            maxHealth *= 1.05f + Mathf.Max(0, chamberIndex) * 0.08f;
            _hp = maxHealth;
            UpdateHpBar();
        }

        public void ExposeToCadence()
        {
            _resonance.Expose();
            if (hasBronzeShield) BreakBronzeGriefShield();
        }

        private float _stunTimer;
        private float _flashTimer;
        private float _frenzyTimer;
        private Vector3 _knockbackVelXZ;
        private bool _flyingForWallSlam;

        private float _aiCooldown;
        private bool _windingUp;
        private float _windupTimer;
        private Vector3 _lungeDirXZ;
        private float _activeLungeTimer;
        private int _volleyCount;

        private void Awake()
        {
            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.52f;
            sc.center = new Vector3(0f, 0.65f, 0f);
            gameObject.AddComponent<PaintedActorDepth>();
        }

        private void Start()
        {
            _eurydice = UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>();
            if (_bodySr == null)
            {
                SetupArchetype(archetype);
            }
        }

        private void OnDestroy()
        {
            if (_eurydice != null)
            {
                _eurydice.UnregisterAbductor(this);
            }
        }

        public void SetupArchetype(ShadeArchetype type)
        {
            archetype = type;
            hasBronzeShield = (type == ShadeArchetype.ArmoredWarden);
            _balance = ShadeBalance.Get(type);
            maxHealth = _balance.MaxHealth;
            moveSpeed = _balance.MoveSpeed;

            switch (type)
            {
                case ShadeArchetype.WretchLunger:
                    _baseColor = new Color(1.45f, 0.22f, 0.36f, 1f);
                    break;
                case ShadeArchetype.BrimstoneCaster:
                    _baseColor = new Color(1.15f, 0.28f, 1.40f, 1f);
                    break;
                case ShadeArchetype.ArmoredWarden:
                    _baseColor = new Color(1.35f, 0.44f, 0.22f, 1f);
                    break;
                case ShadeArchetype.MourningAbductor:
                    _baseColor = new Color(0.32f, 1.35f, 1.55f, 0.94f);
                    break;
                default: _baseColor = new Color(.85f,.72f,.48f,1); break;
            }

            _hp = maxHealth;
            _aiCooldown = UnityEngine.Random.Range(0.25f, 0.75f);

            GameObject tel = new GameObject("AttackTelegraphLine");
            tel.transform.SetParent(transform, false);
            tel.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            GameObject telStrip = new GameObject("Strip");
            telStrip.transform.SetParent(tel.transform, false);
            telStrip.transform.localPosition = new Vector3(0f, 0f, 2.4f);
            telStrip.transform.localScale = new Vector3(0.32f, 4.8f, 1f);
            _telegraphSr = RenderPipeline2DSetup.AttachGroundDecal(telStrip, RenderPipeline2DSetup.SquareSprite, new Color(2.8f, 0.20f, 0.25f, 0.82f), 3);
            _telegraphLine = tel.transform;
            _telegraphLine.gameObject.SetActive(false);

            _billboardRoot = new GameObject("ShadeBillboard25D").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;

            GameObject bodyGo = new GameObject("ShadeBody");
            bodyGo.transform.SetParent(_billboardRoot, false);
            bodyGo.transform.localPosition = new Vector3(0f, 0.74f, 0f);
            bodyGo.transform.localScale = type == ShadeArchetype.ArmoredWarden
                ? new Vector3(1.08f, 1.52f, 1f)
                : (type == ShadeArchetype.MourningAbductor ? new Vector3(0.76f, 1.44f, 1f) : new Vector3(0.84f, 1.28f, 1f));
            _bodySr = RenderPipeline2DSetup.AttachSprite(bodyGo,
                type == ShadeArchetype.BrimstoneCaster ? RenderPipeline2DSetup.RingSprite : RenderPipeline2DSetup.DiamondSprite,
                _baseColor, 11);

            GameObject eyeGo = new GameObject("ShadeEyes");
            eyeGo.transform.SetParent(_billboardRoot, false);
            eyeGo.transform.localPosition = new Vector3(0f, 0.98f, -0.01f);
            eyeGo.transform.localScale = new Vector3(0.32f, 0.12f, 1f);
            Color eyeCol = type == ShadeArchetype.MourningAbductor
                ? new Color(2.9f, 0.22f, 0.45f, 1f)
                : new Color(2.6f, 2.0f, 0.4f, 1f);
            _eyesSr = RenderPipeline2DSetup.AttachSprite(eyeGo, RenderPipeline2DSetup.SquareSprite, eyeCol, 12);

            if (hasBronzeShield)
            {
                GameObject armorGo = new GameObject("GriefArmorOutline");
                armorGo.transform.SetParent(_billboardRoot, false);
                armorGo.transform.localPosition = new Vector3(0f, 0.74f, -0.02f);
                armorGo.transform.localScale = new Vector3(1.32f, 1.68f, 1f);
                _armorAuraSr = RenderPipeline2DSetup.AttachSprite(armorGo, RenderPipeline2DSetup.RingSprite, new Color(2.5f, 1.95f, 0.30f, 0.92f), 13);
            }

            GameObject barBg = new GameObject("DiscordBarBg");
            barBg.transform.SetParent(_billboardRoot, false);
            barBg.transform.localPosition = new Vector3(0f, 1.70f, -0.02f);
            barBg.transform.localScale = new Vector3(0.92f, 0.08f, 1f);
            RenderPipeline2DSetup.AttachSprite(barBg, RenderPipeline2DSetup.SquareSprite, new Color(0.06f, 0.02f, 0.07f, 0.80f), 14);

            GameObject barFill = new GameObject("DiscordBarFill");
            barFill.transform.SetParent(_billboardRoot, false);
            barFill.transform.localPosition = new Vector3(0f, 1.70f, -0.03f);
            barFill.transform.localScale = new Vector3(0.90f, 0.06f, 1f);
            RenderPipeline2DSetup.AttachSprite(barFill, RenderPipeline2DSetup.SquareSprite,
                hasBronzeShield ? new Color(2.4f, 1.95f, 0.30f, 1f) : new Color(2.2f, 0.25f, 0.35f, 1f), 15);
            _hpFillBar = barFill.transform;
            // Screen-space bars keep a consistent size above the painted silhouette.
            barBg.SetActive(false); barFill.SetActive(false);
            var kind = ChamberEncounterPlan.ArtFor(type);
            _paintedActor = gameObject.AddComponent<PaintedEnemyActor>();
            _paintedActor.Configure(kind, shade: this, createPattern: type != ShadeArchetype.WretchLunger && type != ShadeArchetype.ArmoredWarden);
            if (_paintedActor.Art.IsReady)
            {
                _bodySr.enabled = false; _eyesSr.enabled = false;
                if (_armorAuraSr != null) _armorAuraSr.enabled = false;
                float top = _paintedActor.Art.Library.visibleHeight + .2f;
                barBg.transform.localPosition = new Vector3(0, top, -.02f);
                barFill.transform.localPosition = new Vector3(0, top, -.03f);
            }
            EnemyCombatFeedback.Track(this);
        }

        /// <summary>
        /// Triggered when Orpheus plays a discordant note / gets hit while channeling the Flute:
        /// Resets flee progress and frenzies the Shade into faster lunges!
        /// </summary>
        public void TriggerDiscordFrenzy(float duration)
        {
            if (_pacified || IsFleeing || _hp <= 0f) return;

            _frenzyTimer = duration;
            _aiCooldown = 0f;
            _windupTimer = 0.12f;
            _windingUp = true;
            if (_eyesSr != null) _eyesSr.color = new Color(3.5f, 0.15f, 0.15f, 1f);

            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 2.2f, new Color(3f, 0.2f, 0.2f, 0.9f), 0.22f);
        }

        /// <summary>
        /// A4 Mechanic: Convinced Shades stop in their tracks, cover their ears/faces in sorrow,
        /// and sink into the floor mist in place over 1.25s.
        /// </summary>
        public void ConvinceToFlee()
        {
            if (_pacified || IsFleeing || _hp <= 0f) return;
            _paintedActor?.Cancel(true);
            IsFleeing = true;

            _eurydice?.UnregisterAbductor(this);
            _windingUp = false;
            _activeLungeTimer = 0f;
            _knockbackVelXZ = Vector3.zero;
            if (_telegraphLine != null) _telegraphLine.gameObject.SetActive(false);

            Collider c = GetComponent<Collider>();
            if (c != null) c.enabled = false;

            ProceduralLyreAudio.Instance?.PlayFluteFleeCatharsis();
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 2.8f, new Color(0.6f, 1.9f, 2.4f, 0.85f), 0.35f);

            WeepingShadeFleeSink.Attach(gameObject, _bodySr, _armorAuraSr, _eyesSr, _hpFillBar);

            OnShadeSlain?.Invoke(this);
            OnShadeSlain = null;
        }

        /// <summary>
        /// Used by the S - B - S Siren's Convergence Cadence to pull Shades into a tight harmonic cluster & shatter Grief-Armor!
        /// </summary>
        public void PullToward(Vector3 vortexCenterXZ, bool stripShield)
        {
            if (_pacified || _hp <= 0f) return;
            _paintedActor?.Cancel();

            _eurydice?.UnregisterAbductor(this);
            _windingUp = false;
            _activeLungeTimer = 0f;
            if (_telegraphLine != null) _telegraphLine.gameObject.SetActive(false);

            if (stripShield && hasBronzeShield)
            {
                BreakBronzeGriefShield();
            }

            Vector3 toCenter = vortexCenterXZ - transform.position;
            toCenter.y = 0f;
            _flyingForWallSlam = false;
            _knockbackVelXZ = toCenter * 5.8f;
            _stunTimer = Mathf.Max(_stunTimer, 0.58f);
        }

        public void ApplyStun(float duration, bool breakShield = false)
        {
            if (_pacified || _hp <= 0f) return;
            _paintedActor?.Cancel();
            _paintedActor?.Art.Play(EnemyMotion.Hurt, Mathf.Max(.2f, duration));
            if (breakShield && hasBronzeShield)
            {
                BreakBronzeGriefShield();
            }
            _stunTimer = Mathf.Max(_stunTimer, duration);
            _windingUp = false;
            _activeLungeTimer = 0f;
            _knockbackVelXZ = Vector3.zero;
            if (_telegraphLine != null) _telegraphLine.gameObject.SetActive(false);
        }

        private void BreakBronzeGriefShield()
        {
            UnderworldSoundscape.Instance?.PlayImpact(true);
            hasBronzeShield = false;
            if (_armorAuraSr != null) Destroy(_armorAuraSr.gameObject);
            if (_hpFillBar != null) _hpFillBar.GetComponent<SpriteRenderer>().color = new Color(2.2f, 0.25f, 0.35f, 1f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 3.2f, new Color(2.6f, 1.95f, 0.35f, 1f), 0.28f);
            UnderworldCamera2D.Instance?.AddShake(0.32f);
        }

        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector3 hitOrigin)
        {
            if (_pacified || _hp <= 0f) return;
            damage = _resonance.ResolveDamage(damage);
            UnderworldSoundscape.Instance?.PlayImpact();

            Vector3 pushDir = transform.position - hitOrigin;
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.001f) pushDir = UnderworldCamera2D.IsoUpXZ;
            pushDir.Normalize();

            if (hasBronzeShield)
            {
                if (!isHardBoom)
                {
                    float before = _hp;
                    _hp = Mathf.Max(0f, _hp - damage * 0.25f);
                    EnemyCombatFeedback.Damage(transform, before - _hp, shielded: true);
                    _flashTimer = 0.08f;
                    if (_armorAuraSr != null) _armorAuraSr.color = new Color(3f, 3f, 3f, 1f);
                    UpdateHpBar();
                    if (_hp <= 0f) Pacify();
                    return;
                }

                BreakBronzeGriefShield();
            }

            float healthBefore = _hp;
            _hp = Mathf.Max(0f, _hp - damage);
            EnemyCombatFeedback.Damage(transform, healthBefore - _hp, isHardBoom);
            _flashTimer = 0.10f;
            if (_bodySr != null) _bodySr.color = new Color(2.9f, 2.9f, 2.9f, 1f);
            UpdateHpBar();

            if (_hp <= 0f) { Pacify(); return; }
            // Damage still lands, but repeated notes cannot permanently interrupt attacks.
            if(IsMiniBoss && !isHardBoom) return;
            if (!_resonance.TryStagger()) return;
            _paintedActor?.Cancel();
            _paintedActor?.Art.Play(EnemyMotion.Hurt, .3f);
            _eurydice?.UnregisterAbductor(this);
            _windingUp = false;
            _activeLungeTimer = 0f;
            if (_telegraphLine != null) _telegraphLine.gameObject.SetActive(false);

            if (isHardBoom)
            {
                _stunTimer = 0.42f;
                _flyingForWallSlam = true;
                float kbSpeed = Mathf.Lerp(16.0f, 24.0f, Mathf.InverseLerp(0.60f, 1.0f, intensity));
                _knockbackVelXZ = pushDir * kbSpeed;
            }
            else if (_stunTimer < 0.45f) // Preserve longer vortex pull if active
            {
                _stunTimer = 0.16f;
                _knockbackVelXZ = pushDir * 4.8f;
            }

            if (_hp <= 0f) Pacify();
        }

        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            if (_pacified || IsFleeing) return;
            if (_billboardRoot != null) _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;

            float dt = Time.deltaTime;
            _resonance.Tick(dt);
            if (_frenzyTimer > 0f)
            {
                _frenzyTimer -= dt;
                if (_frenzyTimer <= 0f && _eyesSr != null)
                {
                    _eyesSr.color = (archetype == ShadeArchetype.MourningAbductor)
                        ? new Color(2.9f, 0.22f, 0.45f, 1f)
                        : new Color(2.6f, 2.0f, 0.4f, 1f);
                }
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                if (_flashTimer <= 0f)
                {
                    if (_bodySr != null) _bodySr.color = _baseColor;
                    if (_armorAuraSr != null) _armorAuraSr.color = new Color(2.5f, 1.95f, 0.30f, 0.92f);
                }
            }

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
            if (player == null || player.IsGameOver || player.IsVictory) { _paintedActor?.Cancel(); return; }

            if (_paintedActor != null && _paintedActor.Pattern != null)
            {
                if((archetype == ShadeArchetype.MaskedMonk || archetype == ShadeArchetype.Shepherd) &&
                    Vector3.Distance(transform.position,player.transform.position) > 10f &&
                    (_paintedActor.Art.Motion == EnemyMotion.Idle || _paintedActor.Art.Motion == EnemyMotion.Move || _paintedActor.Art.Motion == EnemyMotion.Recover))
                {
                    _paintedActor.Face(player.transform.position);
                    _paintedActor.MoveToward(player.transform.position,moveSpeed,dt,navigate:true);
                    _paintedActor.Art.Play(EnemyMotion.Move,.8f,true);
                    return;
                }
                _paintedActor.Tick(dt);
                return;
            }

            if (archetype == ShadeArchetype.MourningAbductor)
            {
                UpdateMourningAbductor(dt, player);
                return;
            }

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
                if (_paintedActor != null)
                {
                    EnemyMotion pose = _activeLungeTimer > 0 ? EnemyMotion.Attack : _windingUp ? EnemyMotion.Windup : dist > 1.5f ? EnemyMotion.Move : EnemyMotion.Idle;
                    _paintedActor.Face(player.transform.position);
                    if (_paintedActor.Art.Motion != pose) _paintedActor.Art.Play(pose, pose == EnemyMotion.Windup ? _balance.LungeTelegraph : .65f,
                        pose == EnemyMotion.Move || pose == EnemyMotion.Idle);
                }
            }
        }

        private void UpdateMourningAbductor(float dt, OrpheusPlayerController player)
        {
            if (_eurydice == null) _eurydice = UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>();

            if (_eurydice == null || _eurydice.IsVanished || _eurydice.HasSanctuaryShield)
            {
                _eurydice?.UnregisterAbductor(this);
                // Recoil away from Eurydice's Golden Sanctuary Aegis!
                if (_eurydice != null)
                {
                    Vector3 away = transform.position - _eurydice.transform.position;
                    away.y = 0f;
                    if (away.magnitude < 4.2f && away.sqrMagnitude > 0.001f)
                    {
                        MoveEnemyXZ(away.normalized * (moveSpeed * 0.85f * dt), checkWallSlam: false);
                    }
                }
                return;
            }

            Vector3 toEu = _eurydice.transform.position - transform.position;
            toEu.y = 0f;
            float distToEu = toEu.magnitude;

            if (distToEu <= 1.38f)
            {
                _eurydice.RegisterAbductor(this);
                // Drag slowly backward toward the Stygian abyss with Eurydice!
                MoveEnemyXZ(-UnderworldCamera2D.IsoUpXZ * (1.55f * dt), checkWallSlam: false);
            }
            else
            {
                _eurydice.UnregisterAbductor(this);
                Vector3 dirToEu = toEu / distToEu;
                MoveEnemyXZ(dirToEu * (moveSpeed * dt), checkWallSlam: false);
            }
        }

        private void UpdateMeleeLunger(float dt, OrpheusPlayerController player, Vector3 dirToPlayer, float dist)
        {
            if (_activeLungeTimer > 0f)
            {
                _activeLungeTimer -= dt;
                MoveEnemyXZ(_lungeDirXZ * (18.5f * dt), checkWallSlam: false);
                if (Vector3.Distance(transform.position, player.transform.position) < 0.98f)
                {
                    player.TakeDamage(_balance.ContactDamage * _threatMultiplier, transform.position);
                    _activeLungeTimer = 0f;
                }
                return;
            }

            if (_windingUp)
            {
                if (_telegraphSr != null)
                {
                    Color telegraphColor = _telegraphSr.color;
                    telegraphColor.a = 0.72f + Mathf.PingPong(Time.time * 5.5f, 0.24f);
                    _telegraphSr.color = telegraphColor;
                }
                _windupTimer -= dt;
                if (_windupTimer <= 0f)
                {
                    _windingUp = false;
                    if (_telegraphLine != null) _telegraphLine.gameObject.SetActive(false);
                    _activeLungeTimer = 0.24f;
                    _aiCooldown = _balance.AttackRecovery;
                }
                return;
            }

            if (_aiCooldown > 0f) _aiCooldown -= dt;

            float curSpeed = _frenzyTimer > 0f ? moveSpeed * 1.45f : moveSpeed;
            if (dist > 3.9f)
            {
                MoveEnemyXZ(dirToPlayer * (curSpeed * dt), checkWallSlam: false);
            }
            else if (_aiCooldown <= 0f)
            {
                _windingUp = true;
                _windupTimer = _balance.LungeTelegraph;
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

            float curSpeed = _frenzyTimer > 0f ? moveSpeed * 1.45f : moveSpeed;
            if (dist < 4.8f)
            {
                MoveEnemyXZ(-dirToPlayer * (curSpeed * 0.90f * dt), checkWallSlam: false);
            }
            else if (dist > 8.8f)
            {
                MoveEnemyXZ(dirToPlayer * (curSpeed * dt), checkWallSlam: false);
            }

            if (_aiCooldown <= 0f && dist < 15f)
            {
                _aiCooldown = _balance.VolleyCooldown;
                _volleyCount++;
                float[] spread = (_volleyCount % 2 == 0) ? new[] { -18f, 0f, 18f } : new[] { -11f, 11f };
                foreach (float ang in spread)
                {
                    Vector3 d = Quaternion.Euler(0f, ang, 0f) * dirToPlayer;
                    StygianOrbProjectile.Spawn(transform.position + new Vector3(0f, 0.6f, 0f) + d * 0.55f, d, 9.8f, _balance.OrbDamage * _threatMultiplier);
                }
            }
        }

        private void MoveEnemyXZ(Vector3 deltaXZ, bool checkWallSlam)
        {
            deltaXZ.y = 0f;
            float dist = deltaXZ.magnitude;
            if (dist < 0.0001f) return;

            Vector3 dir = deltaXZ / dist;
            if (ChamberCollision.SweepObstacle(transform.position + new Vector3(0f, 0.5f, 0f), deltaXZ, .45f, out RaycastHit hit))
            {
                IsoObstacle obs = hit.collider.GetComponent<IsoObstacle>();
                if (obs != null)
                {
                    if (checkWallSlam && obs.kind == IsoObstacleKind.SolidWall)
                    {
                        _flyingForWallSlam = false;
                        _knockbackVelXZ = Vector3.zero;
                        float before = _hp;
                        _hp = Mathf.Max(0f, _hp - 32f);
                        EnemyCombatFeedback.Damage(transform, before - _hp, true);
                        UpdateHpBar();
                        GroundShockwaveRing.Spawn(new Vector3(hit.point.x, 0.06f, hit.point.z), 2.6f, new Color(2.8f, 0.85f, 0.20f, 1f), 0.26f);
                        UnderworldCamera2D.Instance?.AddShake(0.28f);
                        OrpheusHud.Instance?.ShowSubtleCallout("Wall Resonance +32");
                        if (_hp <= 0f) Pacify();
                        return;
                    }

                    if (checkWallSlam && obs.kind == IsoObstacleKind.DashableChasm)
                    {
                        EnemyCombatFeedback.Damage(transform, Mathf.Max(0f, _hp), true);
                        _hp = 0f;
                        GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.05f, 0f), 2.4f, new Color(0.3f, 2.2f, 1.3f, 1f), 0.3f);
                        Pacify();
                        return;
                    }

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
            _hpFillBar.localScale = new Vector3(0.90f * pct, 0.06f, 1f);
            float height = _paintedActor != null && _paintedActor.Art.IsReady ? _paintedActor.Art.Library.visibleHeight + .2f : 1.7f;
            _hpFillBar.localPosition = new Vector3(-0.45f * (1f - pct), height, -0.03f);
        }

        /// <summary>
        /// #4 Pacifying Grief Instead of Slaughtering:
        /// Releases the Shade from Discord — transforming it into serene silver-gold light that ascends peacefully!
        /// </summary>
        private void Pacify()
        {
            if (_pacified) return;
            _pacified = true;
            _paintedActor?.Cancel(true);
            var collision = GetComponent<Collider>(); if (collision != null) collision.enabled = false;

            _eurydice?.UnregisterAbductor(this);
            ProceduralLyreAudio.Instance?.PlayShadePacified();

            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 2.5f, new Color(2.3f, 2.05f, 1.15f, 0.92f), 0.36f);
            PacifiedSpiritVessel.Spawn(transform.position, scaleMultiplier: 1.0f);

            OnShadeSlain?.Invoke(this);
            OnShadeSlain = null;
            if (Application.isPlaying) Destroy(gameObject, .85f);
            else DestroyImmediate(gameObject);
        }
    }

    /// <summary>
    /// Serene silver-gold spirit silhouette spawned when a Shade or Guardian is pacified by Orpheus's music.
    /// Bows gently and ascends into peaceful light motes instead of violent gore.
    /// </summary>
    public class PacifiedSpiritVessel : MonoBehaviour
    {
        private SpriteRenderer _soulSr;
        private SpriteRenderer _haloSr;
        private float _elapsed;
        private const float Duration = 1.15f;
        private Vector3 _baseScale;

        public static void Spawn(Vector3 worldPos, float scaleMultiplier = 1f)
        {
            GameObject go = new GameObject("PacifiedSpiritAscension");
            go.transform.position = worldPos + new Vector3(0f, 0.72f, 0f);
            go.transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;

            GameObject soul = new GameObject("SereneSpirit");
            soul.transform.SetParent(go.transform, false);
            soul.transform.localScale = new Vector3(0.76f * scaleMultiplier, 1.32f * scaleMultiplier, 1f);
            SpriteRenderer soulSr = RenderPipeline2DSetup.AttachSprite(soul, RenderPipeline2DSetup.DiamondSprite,
                new Color(2.3f, 2.15f, 1.45f, 0.90f), 16);

            GameObject halo = new GameObject("CatharsisHalo");
            halo.transform.SetParent(go.transform, false);
            halo.transform.localPosition = new Vector3(0f, 0.35f * scaleMultiplier, 0.01f);
            halo.transform.localScale = new Vector3(1.35f * scaleMultiplier, 1.35f * scaleMultiplier, 1f);
            SpriteRenderer haloSr = RenderPipeline2DSetup.AttachSprite(halo, RenderPipeline2DSetup.SoftGlowSprite,
                new Color(1.45f, 1.95f, 2.35f, 0.65f), 15);

            // Soft golden-moonlit motes rising gently
            for (int i = 0; i < 6; i++)
            {
                GameObject mote = new GameObject("CatharsisMote");
                mote.transform.position = worldPos + new Vector3(
                    UnityEngine.Random.Range(-0.45f, 0.45f) * scaleMultiplier,
                    UnityEngine.Random.Range(0.3f, 1.2f) * scaleMultiplier,
                    UnityEngine.Random.Range(-0.45f, 0.45f) * scaleMultiplier);
                float sz = UnityEngine.Random.Range(0.10f, 0.22f) * scaleMultiplier;
                mote.transform.localScale = new Vector3(sz, sz, 1f);
                SpriteRenderer msr = RenderPipeline2DSetup.AttachBillboardSprite(mote, RenderPipeline2DSetup.SoftGlowSprite,
                    new Color(2.4f, 2.1f, 0.95f, 0.85f), 17);
                mote.AddComponent<RisingAshParticle>().Init(msr, driftUpSpeed: 1.35f, duration: 1.05f);
            }

            PacifiedSpiritVessel v = go.AddComponent<PacifiedSpiritVessel>();
            v._soulSr = soulSr;
            v._haloSr = haloSr;
            v._baseScale = soul.transform.localScale;
        }

        private void Update()
        {
            transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            float u = Mathf.Clamp01(_elapsed / Duration);

            // Gentle bow first (0..0.28), then serene upward ascension
            float rise = u < 0.28f ? -Mathf.Sin((u / 0.28f) * Mathf.PI) * 0.08f : (u - 0.28f) * 1.45f;
            transform.position += new Vector3(0f, rise * dt * 2.2f, 0f);

            if (_soulSr != null)
            {
                _soulSr.transform.localScale = new Vector3(
                    _baseScale.x * Mathf.Lerp(1f, 0.35f, u * u),
                    _baseScale.y * Mathf.Lerp(1f, 1.35f, u),
                    1f);
                Color c = _soulSr.color;
                c.a = (1f - u) * 0.90f;
                _soulSr.color = c;
            }

            if (_haloSr != null)
            {
                Color c = _haloSr.color;
                c.a = (1f - u) * 0.65f;
                _haloSr.color = c;
            }

            if (u >= 1f) Destroy(gameObject);
        }
    }

    public class StygianOrbProjectile : MonoBehaviour
    {
        private Vector3 _dirXZ;
        private float _speed;
        private float _dmg;
        private float _life = 4.2f;
        private Transform _vis;
        private bool _shattered;

        public static void Spawn(Vector3 pos, Vector3 dirXZ, float speed, float damage)
        {
            GameObject go = new GameObject("StygianEnemyOrb");
            go.transform.position = pos;

            GameObject vis = new GameObject("OrbSprite");
            vis.transform.SetParent(go.transform, false);
            vis.transform.localScale = new Vector3(0.56f, 0.56f, 1f);
            RenderPipeline2DSetup.AttachBillboardSprite(vis, RenderPipeline2DSetup.CircleSprite, new Color(2.6f, 0.35f, 1.45f, 1f), 15);

            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.30f;

            StygianOrbProjectile p = go.AddComponent<StygianOrbProjectile>();
            p._dirXZ = dirXZ.normalized;
            p._speed = speed;
            p._dmg = damage;
            p._vis = vis.transform;
            go.AddComponent<PaintedActorDepth>();
        }

        public void ShatterOrb()
        {
            if (_shattered) return;
            _shattered = true;
            if (Application.isPlaying)
            {
                GroundShockwaveRing.Spawn(new Vector3(transform.position.x, 0.06f, transform.position.z), 1.2f, new Color(2.4f, 0.5f, 1.6f, 0.9f), 0.16f);
                Destroy(gameObject);
            }
            else DestroyImmediate(gameObject);
        }

        private void Update()
        {
            if (_vis != null) _vis.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (_shattered) return;
            Vector3 delta = _dirXZ * (_speed * deltaTime);
            Collider hit = ChamberCollision.ProjectileHit(transform.position, delta, .3f, true, out float distance);
            if (hit != null)
            {
                transform.position += _dirXZ * Mathf.Max(0f, distance - .01f);
                OnTriggerEnter(hit);
                return;
            }
            transform.position += delta;
            _life -= deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_shattered) return;
            IsoObstacle obs = other.GetComponent<IsoObstacle>();
            if (obs != null && obs.kind == IsoObstacleKind.SolidWall)
            {
                ShatterOrb();
                return;
            }

            OrpheusPlayerController player = other.GetComponent<OrpheusPlayerController>();
            if (player != null && !player.IsDashing)
            {
                player.TakeDamage(_dmg, transform.position);
                ShatterOrb();
            }
        }
    }

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

    public class TemptationWhisperZone : MonoBehaviour
    {
        [TextArea]
        public string whisperMessage = "Eurydice: \"The fog is so thick... Keep your eyes on the road ahead! Trust my voice!\"";

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

    public class GroundShockwaveRing : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _targetDiameter;
        private float _duration;
        private float _elapsed;
        private Color _startColor;
        private bool _uprightBillboard;

        public static void Spawn(Vector3 worldPos, float diameter, Color hdrColor, float duration, bool upright2D = false)
        {
            GameObject go = new GameObject("ShockwaveVFX");
            go.transform.position = worldPos;
            go.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

            GroundShockwaveRing fx = go.AddComponent<GroundShockwaveRing>();
            fx._uprightBillboard = upright2D;
            fx._sr = upright2D
                ? RenderPipeline2DSetup.AttachBillboardSprite(go, RenderPipeline2DSetup.RingSprite, hdrColor, 22)
                : RenderPipeline2DSetup.AttachGroundDecal(go, RenderPipeline2DSetup.RingSprite, hdrColor, 18);
            fx._targetDiameter = diameter;
            fx._duration = Mathf.Max(0.05f, duration);
            fx._startColor = hdrColor;
        }

        private void Update()
        {
            if (_uprightBillboard) transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;

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

    /// <summary>
    /// Animates a Shade sinking into the floor mist in sorrow when convinced by Orpheus's Flute (A4).
    /// </summary>
    public class WeepingShadeFleeSink : MonoBehaviour
    {
        private SpriteRenderer _bodySr;
        private SpriteRenderer _armorSr;
        private SpriteRenderer _eyesSr;
        private float _elapsed;
        private const float Duration = 1.25f;
        private Vector3 _startBodyScale;

        public static void Attach(GameObject go, SpriteRenderer body, SpriteRenderer armor, SpriteRenderer eyes, Transform hpBar)
        {
            if (hpBar != null) hpBar.gameObject.SetActive(false);
            WeepingShadeFleeSink sink = go.AddComponent<WeepingShadeFleeSink>();
            sink._bodySr = body;
            sink._armorSr = armor;
            sink._eyesSr = eyes;
            if (body != null) sink._startBodyScale = body.transform.localScale;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            float u = Mathf.Clamp01(_elapsed / Duration);

            transform.position += Vector3.down * (0.55f * dt);

            if (_bodySr != null)
            {
                _bodySr.transform.localScale = new Vector3(_startBodyScale.x * (1f + u * 0.15f), _startBodyScale.y * (1f - u * 0.85f), 1f);
                Color c = Color.Lerp(new Color(0.5f, 0.85f, 1.2f, 0.85f), new Color(0.2f, 0.5f, 0.8f, 0f), u);
                _bodySr.color = c;
            }

            if (_armorSr != null)
            {
                Color c = _armorSr.color;
                c.a = (1f - u) * 0.4f;
                _armorSr.color = c;
            }

            if (_eyesSr != null)
            {
                Color c = _eyesSr.color;
                c.a = 1f - u;
                _eyesSr.color = c;
            }

            if (UnityEngine.Random.value < 0.22f)
            {
                GameObject mote = new GameObject("WeepingMote");
                mote.transform.position = transform.position + new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f), 0.15f, UnityEngine.Random.Range(-0.35f, 0.35f));
                mote.transform.localScale = new Vector3(0.14f, 0.14f, 1f);
                SpriteRenderer sr = RenderPipeline2DSetup.AttachBillboardSprite(mote, RenderPipeline2DSetup.SoftGlowSprite, new Color(0.6f, 1.2f, 1.8f, 0.7f), 18);
                mote.AddComponent<RisingAshParticle>().Init(sr, driftUpSpeed: 0.8f, duration: 0.7f);
            }

            if (u >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
