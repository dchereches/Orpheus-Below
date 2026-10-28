using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    public enum BossIdentity
    {
        CantorOfFury,         // Chamber I Mini-Boss (98 BPM)
        OarsmanOfStyx,        // Chamber II Mini-Boss (110 BPM)
        SirenOfAcheron,       // Chamber III Mini-Boss (118 BPM)
        CerberusHoundOfHades,
        MaskedMonk,
        Shepherd,
        Tripod = CantorOfFury,
        MysticEyeWarden = OarsmanOfStyx,
        WingedSentinel = SirenOfAcheron
    }

    public enum DuelPhase
    {
        ArenaCombat,     // Active 2D action combat (slashes, lunges, floor waves, cadences)
        StaveLeap,       // Boss leaps into background ruins
        StaveBarrage,    // High-tempo rhythm barrage from the ruins
        CrashedStunned   // Boss crashed face-down, stunned 5s, 2x damage (Flute Requiem window!)
    }

    /// <summary>
    /// Phased 2D Parallax Guardian Duel System:
    ///   1. Arena Combat: Freeform 2D action! Orpheus slashes, dash-strikes, and casts cadences while dodging
    ///      boss lunges, ground cleaves, and geysers. Full damage to the boss!
    ///   2. Stave Climax: Boss leaps into the background vista/ruins to channel a high-tempo rhythm barrage.
    ///      Deflecting notes shoots radiant spears back into the boss!
    ///   3. Crashed Stunned: The boss plummets from the ruins and crashes face-down, stunned for 5 seconds.
    ///      Vulnerable to 2.0x damage — the ultimate window to unleash the FLUTE's Cataclysmic Requiem!
    /// </summary>
    public class CerberusRhythmBoss : MonoBehaviour, IOrphicTarget
    {
        public static CerberusRhythmBoss ActiveBoss { get; private set; }

        [Header("Guardian Identity & Rhythm")]
        public BossIdentity identity = BossIdentity.CantorOfFury;
        public string bossDisplayName = "Cantor of Tisiphone";
        public float maxHealth = 260f;
        public float bpm = 100f;
        public float noteTravelSeconds = 1.15f;
        public float hitWindowSeconds = 0.23f;

        public float CurrentHealth { get; private set; }
        public bool IsBattleActive { get; private set; }
        public bool IsDefeated { get; private set; }
        public int HarmonyStreak { get; private set; }
        public string LastJudgement { get; private set; } = "";

        public DuelPhase CurrentPhase { get; private set; } = DuelPhase.ArenaCombat;
        public bool IsStaveBarrageActive => CurrentPhase == DuelPhase.StaveBarrage || CurrentPhase == DuelPhase.StaveLeap;

        public event Action OnCerberusDefeated;

        private Vector3 _stageCenter;
        private Transform _billboardRoot;
        private Transform[] _heads;
        private SpriteRenderer[] _headRenderers;
        private Color _headBaseColor;
        private float _nextDiscordPenalty;

        // Position Anchors in 2D Space
        private Vector3 _bossGroundAnchor;
        private Vector3 _bossRuinsAnchor;
        private float _combatActionTimer = 1.8f;
        private int _combatActionIndex = 0;
        private bool _isLunging;
        private PaintedEnemyActor _paintedActor;
        private float _lungeTimer;
        private Vector3 _lungeStartPos;
        private Vector3 _lungeEndPos;

        // Climax Milestones
        private bool _milestone65Done;
        private bool _milestone30Done;
        private float _crashStunTimer;

        // 2D Stave UI in world space
        private Transform _staveRoot;
        private Transform _targetHitRing;
        private SpriteRenderer _targetRingSr;
        private Vector3 _noteSpawnLocal;
        private Vector3 _noteTargetLocal;

        // Abyssal Boss Blackout & Musical Notation Sprites
        private GameObject _blackoutBackdrop;
        private Color _originalCameraBgColor = Color.black;
        private bool _savedCameraBg;
        private Sprite _quaverSprite;
        private Sprite _fermataSprite;
        private Sprite _trebleClefSprite;

        private float _beatInterval;
        private float _nextBeatTime;
        private int _barrageNotesRemaining;

        private class StaveBeatNote
        {
            public bool requiresHardBoom;
            public float targetHitTime;
            public Transform visual;
            public bool resolved;
            public Vector3 spawnPos;
            public Vector3 targetPos;
        }

        private readonly List<StaveBeatNote> _notes = new List<StaveBeatNote>();

        public void ConfigureBoss(BossIdentity id, Vector3 chamberStageCenter)
        {
            identity = id;
            _stageCenter = chamberStageCenter;
            _bossGroundAnchor = _stageCenter + UnderworldCamera2D.IsoRightXZ * 4.8f;
            _bossRuinsAnchor = _stageCenter + UnderworldCamera2D.IsoRightXZ * 3.6f + new Vector3(0f, 3.8f, 0f)
                             - (UnderworldCamera2D.Side2DRotation * Vector3.forward * 2.2f);

            transform.position = _bossGroundAnchor;

            switch (id)
            {
                case BossIdentity.CantorOfFury:
                    bossDisplayName = "Tripod, Voice of the Deep";
                    maxHealth = 220f;
                    bpm = 110f;
                    noteTravelSeconds = 1.20f;
                    _headBaseColor = new Color(1.55f, 0.22f, 0.36f, 1f);
                    break;

                case BossIdentity.OarsmanOfStyx:
                    bossDisplayName = "Mystic Eye Warden";
                    maxHealth = 280f;
                    bpm = 116f;
                    noteTravelSeconds = 1.15f;
                    _headBaseColor = new Color(0.28f, 1.45f, 1.25f, 1f);
                    break;

                case BossIdentity.SirenOfAcheron:
                    bossDisplayName = "Winged Sentinel";
                    maxHealth = 330f;
                    bpm = 122f;
                    noteTravelSeconds = 1.10f;
                    _headBaseColor = new Color(1.25f, 0.38f, 1.65f, 1f);
                    break;

                case BossIdentity.CerberusHoundOfHades:
                    bossDisplayName = "Cerberus, Hound of Hades";
                    maxHealth = 460f;
                    bpm = 128f;
                    noteTravelSeconds = 1.05f;
                    _headBaseColor = new Color(1.75f, 0.20f, 0.22f, 1f);
                    break;
                case BossIdentity.MaskedMonk:
                    bossDisplayName = "Masked Monk"; maxHealth = 240f; bpm = 112f; noteTravelSeconds = 1.2f;
                    _headBaseColor = new Color(.6f, .65f, .4f); break;
                case BossIdentity.Shepherd:
                    bossDisplayName = "Shepherd of the Lost Flock"; maxHealth = 310f; bpm = 120f; noteTravelSeconds = 1.1f;
                    _headBaseColor = new Color(.9f, .7f, .3f); break;
            }

            maxHealth *= 2.40f;
            hitWindowSeconds = 0.23f;
            CurrentHealth = maxHealth;
            _beatInterval = 60f / Mathf.Max(50f, bpm);

            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.85f;
            sc.center = new Vector3(0f, 1.3f, 0f);

            BuildBossVisuals();
            _paintedActor = gameObject.AddComponent<PaintedEnemyActor>();
            var kind = id == BossIdentity.CantorOfFury ? PaintedEnemyKind.Tripod
                : id == BossIdentity.OarsmanOfStyx ? PaintedEnemyKind.MysticEyeWarden
                : id == BossIdentity.SirenOfAcheron ? PaintedEnemyKind.WingedSentinel
                : id == BossIdentity.MaskedMonk ? PaintedEnemyKind.MaskedMonk
                : id == BossIdentity.Shepherd ? PaintedEnemyKind.Shepherd : PaintedEnemyKind.Cerberus;
            _paintedActor.Configure(kind, boss: this, stageCenter: _stageCenter);
            if (_paintedActor.Art.IsReady) _billboardRoot.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void BuildBossVisuals()
        {
            _billboardRoot = new GameObject("GuardianBillboard2D").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;

            bool isThreeHeaded = (identity == BossIdentity.CerberusHoundOfHades);
            int headCount = isThreeHeaded ? 3 : 1;

            GameObject torso = new GameObject("GuardianBody");
            torso.transform.SetParent(_billboardRoot, false);
            torso.transform.localPosition = new Vector3(0f, 1.55f, 0.05f);
            torso.transform.localScale = isThreeHeaded ? new Vector3(3.1f, 3.0f, 1f) : new Vector3(1.85f, 2.75f, 1f);
            RenderPipeline2DSetup.AttachSprite(torso, RenderPipeline2DSetup.DiamondSprite, new Color(0.18f, 0.06f, 0.16f, 1f), 10);

            // Spectral halo behind mini-boss
            GameObject halo = new GameObject("GuardianHalo");
            halo.transform.SetParent(_billboardRoot, false);
            halo.transform.localPosition = new Vector3(0f, 1.85f, 0.08f);
            halo.transform.localScale = new Vector3(2.9f, 2.9f, 1f);
            RenderPipeline2DSetup.AttachSprite(halo, RenderPipeline2DSetup.RingSprite,
                new Color(_headBaseColor.r, _headBaseColor.g, _headBaseColor.b, 0.45f), 9);

            _heads = new Transform[headCount];
            _headRenderers = new SpriteRenderer[headCount];

            for (int i = 0; i < headCount; i++)
            {
                Vector3 hPos = headCount == 1
                    ? new Vector3(-0.15f, 2.65f, 0f)
                    : (i == 0 ? new Vector3(-1.1f, 2.25f, 0f) : i == 1 ? new Vector3(0f, 2.95f, -0.02f) : new Vector3(1.1f, 2.25f, 0f));

                GameObject h = new GameObject("GuardianHead_" + i);
                h.transform.SetParent(_billboardRoot, false);
                h.transform.localPosition = hPos;
                h.transform.localScale = isThreeHeaded ? new Vector3(1.30f, 1.22f, 1f) : new Vector3(1.15f, 1.15f, 1f);
                _headRenderers[i] = RenderPipeline2DSetup.AttachSprite(h, RenderPipeline2DSetup.DiamondSprite, _headBaseColor, 12 + i);

                GameObject eyes = new GameObject("Eyes");
                eyes.transform.SetParent(h.transform, false);
                eyes.transform.localPosition = new Vector3(-0.14f, 0.08f, -0.01f);
                eyes.transform.localScale = new Vector3(0.38f, 0.13f, 1f);
                RenderPipeline2DSetup.AttachSprite(eyes, RenderPipeline2DSetup.SquareSprite, new Color(2.9f, 2.2f, 0.45f, 1f), 16);

                _heads[i] = h.transform;
            }

            // Load transparent PNG musical notation assets
            _quaverSprite = Resources.Load<Sprite>("MusicalNotation/symbol_quaver_small");
            _fermataSprite = Resources.Load<Sprite>("MusicalNotation/symbol_fermata_boom");
            _trebleClefSprite = Resources.Load<Sprite>("MusicalNotation/symbol_treble_clef");

            // --- 2D MUSICAL STAVE FOR RHYTHM CLIMAX ---
            _staveRoot = new GameObject("RhythmStave2D").transform;
            _staveRoot.SetParent(transform, false);
            _staveRoot.position = _stageCenter + new Vector3(0f, 3.65f, 0f);
            _staveRoot.rotation = UnderworldCamera2D.Side2DRotation;

            _noteSpawnLocal = new Vector3(5.2f, 0f, 0f);   // Above Boss (Right)
            _noteTargetLocal = new Vector3(-4.4f, 0f, 0f); // Above Orpheus (Left)

            // 5 Classical Golden Staff Hairlines (E4, G4, B4, D5, F5)
            const float lineSpacing = 0.22f;
            for (int s = -2; s <= 2; s++)
            {
                GameObject line = new GameObject("StaffLine_" + s);
                line.transform.SetParent(_staveRoot, false);
                line.transform.localPosition = new Vector3(0.4f, s * lineSpacing, 0.05f);
                line.transform.localScale = new Vector3(10.8f, 0.024f, 1f);
                RenderPipeline2DSetup.AttachSprite(line, RenderPipeline2DSetup.SquareSprite, new Color(0.96f, 0.85f, 0.52f, 0.65f), 18);
            }

            // Subtle Vertical Measure Barlines across the 5 staff lines
            float[] barlineX = { -2.1f, 0.4f, 2.9f, 5.4f };
            for (int b = 0; b < barlineX.Length; b++)
            {
                GameObject bar = new GameObject("StaffBarline_" + b);
                bar.transform.SetParent(_staveRoot, false);
                bar.transform.localPosition = new Vector3(barlineX[b], 0f, 0.04f);
                bar.transform.localScale = new Vector3(0.025f, lineSpacing * 4.2f, 1f);
                RenderPipeline2DSetup.AttachSprite(bar, RenderPipeline2DSetup.SquareSprite, new Color(0.90f, 0.78f, 0.45f, 0.40f), 18);
            }

            // Classical Treble Clef (𝄞) Sprite at the Left Target Hit Register (delicate, refined scale)
            GameObject clefGo = new GameObject("TrebleClefRegister");
            clefGo.transform.SetParent(_staveRoot, false);
            clefGo.transform.localPosition = _noteTargetLocal + new Vector3(0f, 0.02f, 0.02f);
            clefGo.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            if (_trebleClefSprite != null)
            {
                RenderPipeline2DSetup.AttachSprite(clefGo, _trebleClefSprite, Color.white, 21);
            }

            // Pulsing Golden Lyre Target Ring around the Treble Clef
            GameObject ringGo = new GameObject("GoldenLyreHitRing");
            ringGo.transform.SetParent(_staveRoot, false);
            ringGo.transform.localPosition = _noteTargetLocal;
            ringGo.transform.localScale = new Vector3(0.58f, 0.58f, 1f);
            _targetRingSr = RenderPipeline2DSetup.AttachSprite(ringGo, RenderPipeline2DSetup.RingSprite, new Color(2.6f, 2.15f, 0.65f, 0.90f), 20);
            _targetHitRing = ringGo.transform;

            _staveRoot.gameObject.SetActive(false);
        }

        public void ActivateGuardianDuel()
        {
            if (IsBattleActive || IsDefeated) return;

            gameObject.SetActive(true);
            ActiveBoss = this;
            IsBattleActive = true;
            HarmonyStreak = 0;
            CurrentPhase = DuelPhase.ArenaCombat;
            LastJudgement = "Attack freely! Dodge lunges & floor waves!";

            // --- ABYSSAL BOSS BLACKOUT: Plunge entire stage background into pure inky void black ---
            if (Camera.main != null)
            {
                if (!_savedCameraBg)
                {
                    _originalCameraBgColor = Camera.main.backgroundColor;
                    _savedCameraBg = true;
                }
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.black;

                System.Type hdCamType = System.Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");
                if (hdCamType != null)
                {
                    Component hdCam = Camera.main.GetComponent(hdCamType);
                    if (hdCam != null)
                    {
                        var clearModeField = hdCamType.GetField("clearColorMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (clearModeField != null)
                        {
                            object colorEnumVal = System.Enum.ToObject(clearModeField.FieldType, 1);
                            clearModeField.SetValue(hdCam, colorEnumVal);
                        }
                        var bgHdrField = hdCamType.GetField("backgroundColorHDR", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (bgHdrField != null)
                        {
                            bgHdrField.SetValue(hdCam, Color.black);
                        }
                    }
                }
            }

            // Deactivate all UnderworldParallaxRig instances in the scene
            UnderworldParallaxRig[] rigs = UnityEngine.Object.FindObjectsByType<UnderworldParallaxRig>(FindObjectsSortMode.None);
            for (int r = 0; r < rigs.Length; r++)
            {
                if (rigs[r] != null) rigs[r].gameObject.SetActive(false);
            }

            if (PaintedChamberMap.Active != null)
            {
                if (PaintedChamberMap.Active.background != null) PaintedChamberMap.Active.background.enabled = false;
                if (PaintedChamberMap.Active.coverRoot != null) PaintedChamberMap.Active.coverRoot.gameObject.SetActive(false);
            }

            // Massive opaque Unlit black backdrop directly behind the stage plane (ZWrite On, zero ambient reflections)
            if (_blackoutBackdrop == null)
            {
                _blackoutBackdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _blackoutBackdrop.name = "BossAbyssalBlackoutBackdrop";
                Collider c = _blackoutBackdrop.GetComponent<Collider>();
                if (c != null) { if(Application.isPlaying) Destroy(c); else DestroyImmediate(c); }

                _blackoutBackdrop.transform.position = _stageCenter + (UnderworldCamera2D.Side2DRotation * Vector3.forward * 4.5f) + new Vector3(0f, 3.5f, 0f);
                _blackoutBackdrop.transform.rotation = UnderworldCamera2D.Side2DRotation;
                _blackoutBackdrop.transform.localScale = new Vector3(240f, 140f, 1f);

                MeshRenderer mr = _blackoutBackdrop.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.material = RenderPipeline2DSetup.GetOpaque3DMaterial(Color.black);
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
            }

            UnderworldCamera2D.Instance?.EnterSide2DBossView(_stageCenter, identity == BossIdentity.CerberusHoundOfHades ? 6.3f : 5.8f);
            OrpheusPlayerController.Instance?.Enter2DBossStage(_stageCenter, minX: -8.2f, maxX: 4.2f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 1.2f, 0f), 5.2f, _headBaseColor, 0.45f, upright2D: true);

            OrpheusHud.Instance?.ShowBanner(bossDisplayName + "  —  2D Guardian Duel", 3.2f);
        }

        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay) return;
            if (_billboardRoot != null) _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            if (_blackoutBackdrop != null) _blackoutBackdrop.transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            if (_staveRoot != null)
            {
                _staveRoot.position = _stageCenter + new Vector3(0f, 3.65f, 0f);
                _staveRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            }

            if (IsDefeated || !IsBattleActive) return;

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory) { _paintedActor?.Cancel(); return; }

            float dt = Time.deltaTime;

            // Animate heads to BPM
            float beatPhase = (Time.time / _beatInterval) * Mathf.PI * 2f;
            float metronomePulse = 0.58f + Mathf.Pow(Mathf.Max(0f, Mathf.Cos(beatPhase * 0.5f)), 6f) * 0.08f;
            if (_targetHitRing != null) _targetHitRing.localScale = new Vector3(metronomePulse, metronomePulse, 1f);

            for (int i = 0; i < _heads.Length; i++)
            {
                Vector3 basePos = _heads.Length == 1
                    ? new Vector3(-0.15f, 2.65f, 0f)
                    : (i == 0 ? new Vector3(-1.1f, 2.25f, 0f) : i == 1 ? new Vector3(0f, 2.95f, -0.02f) : new Vector3(1.1f, 2.25f, 0f));
                
                if (CurrentPhase == DuelPhase.CrashedStunned)
                {
                    // Head droops to the ground when crashed!
                    _heads[i].localPosition = basePos + new Vector3(0f, -0.85f, 0f);
                    _headRenderers[i].color = Color.Lerp(_headRenderers[i].color, new Color(0.6f, 0.4f, 0.4f, 0.8f), dt * 6f);
                }
                else
                {
                    _heads[i].localPosition = basePos + new Vector3(0f, Mathf.Sin(beatPhase + i * 1.1f) * 0.18f, 0f);
                    _headRenderers[i].color = Color.Lerp(_headRenderers[i].color, _headBaseColor, dt * 8f);
                }
            }

            switch (CurrentPhase)
            {
                case DuelPhase.ArenaCombat:
                    UpdateArenaCombat(dt, player);
                    break;

                case DuelPhase.StaveLeap:
                    UpdateStaveLeap(dt);
                    break;

                case DuelPhase.StaveBarrage:
                    UpdateStaveBarrage(dt, player);
                    break;

                case DuelPhase.CrashedStunned:
                    UpdateCrashedStunned(dt);
                    break;
            }
        }

        // =========================================================
        // PHASE 1: ACTIVE 2D ARENA COMBAT
        // =========================================================
        private void UpdateArenaCombat(float dt, OrpheusPlayerController player)
        {
            if (_paintedActor != null && _paintedActor.Pattern != null)
            {
                _paintedActor.Tick(dt);
                CheckMilestones();
                return;
            }
            if (_isLunging)
            {
                _lungeTimer += dt * 3.5f;
                if (_lungeTimer < 0.5f)
                {
                    // Charging forward along IsoRightXZ
                    float u = _lungeTimer * 2.0f;
                    transform.position = Vector3.Lerp(_lungeStartPos, _lungeEndPos, u);

                    // Contact damage with player if not dashing
                    if (!player.IsDashing && Vector3.Distance(transform.position, player.transform.position) < 1.65f)
                    {
                        player.TakeDamage(18f, transform.position);
                        GroundShockwaveRing.Spawn(player.transform.position, 2.2f, _headBaseColor, 0.2f, upright2D: true);
                    }
                }
                else
                {
                    // Dashing back to ground anchor
                    float u = (_lungeTimer - 0.5f) * 2.0f;
                    transform.position = Vector3.Lerp(_lungeEndPos, _bossGroundAnchor, u);

                    if (_lungeTimer >= 1.0f)
                    {
                        _isLunging = false;
                        transform.position = _bossGroundAnchor;
                    }
                }
                return;
            }

            // Maintain ground anchor
            transform.position = Vector3.Lerp(transform.position, _bossGroundAnchor, dt * 10f);

            _combatActionTimer -= dt;
            if (_combatActionTimer <= 0f)
            {
                _combatActionTimer = UnityEngine.Random.Range(2.0f, 2.8f);
                ExecuteRandomBossAction(player);
            }

            CheckMilestones();
        }

        private void CheckMilestones()
        {
            // Musical milestones interrupt and cancel any pending physical attack.
            if (!_milestone65Done && CurrentHealth <= maxHealth * 0.65f)
            {
                _milestone65Done = true;
                TriggerStaveClimax();
            }
            else if (!_milestone30Done && CurrentHealth <= maxHealth * 0.30f)
            {
                _milestone30Done = true;
                TriggerStaveClimax();
            }
        }

        private void ExecuteRandomBossAction(OrpheusPlayerController player)
        {
            _combatActionIndex = (_combatActionIndex + 1) % 3;

            switch (_combatActionIndex)
            {
                case 0: // Spectral Wing Lunge
                    _isLunging = true;
                    _lungeTimer = 0f;
                    _lungeStartPos = transform.position;
                    _lungeEndPos = player.transform.position + UnderworldCamera2D.IsoRightXZ * 0.5f;
                    GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 1.2f, 0f), 3.2f, Color.red, 0.25f, upright2D: true);
                    UnderworldSoundscape.Instance?.PlayImpact(true);
                    break;

                case 1: // Ground Cleave Wave
                    StageSweepWave2D.Spawn(transform.position + new Vector3(0f, 0.65f, 0f), _stageCenter,
                        speed: identity == BossIdentity.CerberusHoundOfHades ? 12.8f : 10.6f);
                    UnderworldCamera2D.Instance?.AddShake(0.25f);
                    break;

                case 2: // Stygian Geyser Hazard under player
                    StageGeyserHazard2D.Spawn(player.transform.position);
                    break;
            }
        }

        // =========================================================
        // PHASE 2: STAVE RHYTHM CLIMAX IN THE RUINS
        // =========================================================
        public void TriggerStaveClimax()
        {
            if (CurrentPhase == DuelPhase.StaveLeap || CurrentPhase == DuelPhase.StaveBarrage) return;

            CurrentPhase = DuelPhase.StaveLeap;
            _paintedActor?.Cancel();
            _paintedActor?.Art.Play(EnemyMotion.Windup, .75f);
            PaintedEnemyProjectile.ClearAll();
            _isLunging = false;
            _notes.Clear();

            // Clear any lingering 2D projectiles
            foreach (var wave in FindObjectsByType<StageSweepWave2D>(FindObjectsSortMode.None)) Destroy(wave.gameObject);

            UnderworldSoundscape.Instance?.PlayImpact(true);
            UnderworldCamera2D.Instance?.AddShake(0.55f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 1.2f, 0f), 6.5f, new Color(2.8f, 0.4f, 0.2f, 1f), 0.45f, upright2D: true);

            OrpheusHud.Instance?.ShowBanner(bossDisplayName + "  —  Stygian Hymn!", 2.8f);
            LastJudgement = "Deflect the Stave notes into the Treble Clef! Break the Guardian's hymn!";
        }

        private void UpdateStaveLeap(float dt)
        {
            // Smoothly leap up to the background ruins anchor
            transform.position = Vector3.Lerp(transform.position, _bossRuinsAnchor, dt * 5.5f);

            if (Vector3.Distance(transform.position, _bossRuinsAnchor) < 0.35f)
            {
                transform.position = _bossRuinsAnchor;
                CurrentPhase = DuelPhase.StaveBarrage;
                _staveRoot.gameObject.SetActive(true);
                _barrageNotesRemaining = 6;
                _nextBeatTime = Time.time + 0.35f;
            }
        }

        private void UpdateStaveBarrage(float dt, OrpheusPlayerController player)
        {
            if (_barrageNotesRemaining > 0 && Time.time >= _nextBeatTime)
            {
                bool isBoom = (_barrageNotesRemaining % 2 == 1);
                SpawnStaveNote(isBoom, Time.time + noteTravelSeconds);
                _barrageNotesRemaining--;
                _nextBeatTime = Time.time + _beatInterval;
                ProceduralLyreAudio.Instance?.PlayRhythmBeat(isBoom);
            }

            UpdateStaveNotes(player);

            // Once all notes are resolved, the boss crashes down!
            if (_barrageNotesRemaining <= 0 && _notes.Count == 0)
            {
                _staveRoot.gameObject.SetActive(false);
                EnterCrashedStunned();
            }
        }

        private void SpawnStaveNote(bool hardBoomRequired, float hitTime)
        {
            GameObject go = new GameObject(hardBoomRequired ? "Note2D_FERMATA" : "Note2D_QUAVER");
            go.transform.SetParent(_staveRoot, false);

            // Dynamic pitch variation across the 5 classical staff lines (from s = -2 to +2)
            int pitch = UnityEngine.Random.Range(-2, 3);
            float pitchY = pitch * 0.22f;
            Vector3 spawnPos = _noteSpawnLocal + new Vector3(0f, pitchY, 0f);
            Vector3 targetPos = _noteTargetLocal + new Vector3(0f, pitchY, 0f);
            go.transform.localPosition = spawnPos;

            if (hardBoomRequired)
            {
                // Hard Boom: Yellow Fermata (𝄐) transparent PNG sprite — delicate, refined note size (a lot smaller)
                go.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
                if (_fermataSprite != null)
                {
                    RenderPipeline2DSetup.AttachSprite(go, _fermataSprite, Color.white, 24);
                }
                else
                {
                    RenderPipeline2DSetup.AttachSprite(go, RenderPipeline2DSetup.RingSprite, new Color(2.8f, 2.2f, 0.4f, 1f), 22);
                }
            }
            else
            {
                // Small Strike: Golden Quaver (♪) transparent PNG sprite — delicate, refined note size (a lot smaller)
                go.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
                if (_quaverSprite != null)
                {
                    RenderPipeline2DSetup.AttachSprite(go, _quaverSprite, Color.white, 23);
                }
                else
                {
                    RenderPipeline2DSetup.AttachSprite(go, RenderPipeline2DSetup.DiamondSprite, new Color(2.5f, 2.05f, 0.45f, 1f), 22);
                }
            }

            _notes.Add(new StaveBeatNote
            {
                requiresHardBoom = hardBoomRequired,
                targetHitTime = hitTime,
                visual = go.transform,
                spawnPos = spawnPos,
                targetPos = targetPos,
                resolved = false
            });
        }

        private void UpdateStaveNotes(OrpheusPlayerController player)
        {
            for (int i = _notes.Count - 1; i >= 0; i--)
            {
                StaveBeatNote n = _notes[i];
                if (n.resolved)
                {
                    if (n.visual != null) Destroy(n.visual.gameObject);
                    _notes.RemoveAt(i);
                    continue;
                }

                float rem = n.targetHitTime - Time.time;
                float u = 1f - (rem / noteTravelSeconds);
                if (n.visual != null)
                {
                    n.visual.localPosition = Vector3.LerpUnclamped(n.spawnPos, n.targetPos, u);
                }

                if (rem < -hitWindowSeconds)
                {
                    n.resolved = true;
                    HarmonyStreak = 0;
                    LastJudgement = "Missed Beat — Dash the Shockwave!";
                    StageSweepWave2D.Spawn(transform.position + new Vector3(0f, 0.65f, 0f), _stageCenter, speed: 10.6f);
                    if (n.visual != null) Destroy(n.visual.gameObject);
                    _notes.RemoveAt(i);
                }
            }
        }

        public void OnPlayerRhythmPulse(float intensity, bool isHardBoom)
        {
            if (UnderworldFrontEnd.BlocksGameplay || !IsBattleActive || IsDefeated) return;
            if (CurrentPhase != DuelPhase.StaveBarrage) return;

            StaveBeatNote best = null;
            float bestAbs = hitWindowSeconds;

            foreach (StaveBeatNote n in _notes)
            {
                if (n.resolved) continue;
                float dt = Mathf.Abs(n.targetHitTime - Time.time);
                if (dt <= bestAbs)
                {
                    bestAbs = dt;
                    best = n;
                }
            }

            if (best == null)
            {
                HarmonyStreak = 0;
                LastJudgement = "Off beat — wait for note to reach the ring";
                if (Time.time >= _nextDiscordPenalty)
                {
                    _nextDiscordPenalty = Time.time + 1.2f;
                    StageSweepWave2D.Spawn(transform.position + new Vector3(0f, 0.65f, 0f), _stageCenter, speed: 10.6f);
                    UnderworldSoundscape.Instance?.PlayDryNote();
                }
                return;
            }

            best.resolved = true;
            if (best.requiresHardBoom == isHardBoom)
            {
                HarmonyStreak++;
                float dmg = isHardBoom ? 52f : 32f;
                CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);

                LastJudgement = isHardBoom ? $"Harmonic Fermata!  x{HarmonyStreak}" : $"Quaver Deflected!  x{HarmonyStreak}";

                // Fire radiant lance of energy from player ring into boss in the ruins!
                if (_targetHitRing != null)
                {
                    GroundShockwaveRing.Spawn(_targetHitRing.position, 2.8f, isHardBoom ? new Color(2.9f, 0.4f, 0.2f, 1f) : new Color(2.6f, 2.2f, 0.5f, 1f), 0.22f, upright2D: true);
                }
                GroundShockwaveRing.Spawn(transform.position, 3.8f, new Color(2.8f, 2.2f, 0.6f, 1f), 0.25f, upright2D: true);
                UnderworldCamera2D.Instance?.AddShake(0.35f);

                if (CurrentHealth <= 0f) DefeatGuardian();
            }
            else
            {
                HarmonyStreak = 0;
                LastJudgement = best.requiresHardBoom ? "Too Soft — Needed Fermata (Heavy Strike)" : "Too Loud — Needed Quaver (Small Strike)";
                StageSweepWave2D.Spawn(transform.position + new Vector3(0f, 0.65f, 0f), _stageCenter, speed: 10.6f);
            }
        }

        // =========================================================
        // PHASE 3: CRASHED & STUNNED (FLUTE EXECUTION WINDOW)
        // =========================================================
        private void EnterCrashedStunned()
        {
            CurrentPhase = DuelPhase.CrashedStunned;
            _paintedActor?.Cancel();
            _paintedActor?.Art.Play(EnemyMotion.Hurt, 5f);
            _crashStunTimer = 5.0f;
            transform.position = _bossGroundAnchor;

            UnderworldSoundscape.Instance?.PlayImpact(true);
            UnderworldCamera2D.Instance?.AddShake(0.75f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.8f, 0f), 7.2f, new Color(2.8f, 2.0f, 0.4f, 1f), 0.65f, upright2D: true);

            OrpheusHud.Instance?.ShowBanner("GUARDIAN CRASHED! [F] UNLEASH THE FLUTE / REQUIEM!", 4.5f);
            LastJudgement = "GUARDIAN VULNERABLE!  (2.0x Damage from Flute & Lyre!)";
        }

        private void UpdateCrashedStunned(float dt)
        {
            transform.position = _bossGroundAnchor;
            _crashStunTimer -= dt;

            if (_crashStunTimer <= 0f)
            {
                // Boss recovers and bursts back into active combat
                CurrentPhase = DuelPhase.ArenaCombat;
                _paintedActor?.Art.Play(EnemyMotion.Recover, .8f);
                _combatActionTimer = 1.2f;
                GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 1.2f, 0f), 4.8f, _headBaseColor, 0.35f, upright2D: true);
                UnderworldSoundscape.Instance?.PlayImpact(true);
                OrpheusHud.Instance?.ShowSubtleCallout(bossDisplayName + "  Recovered!");
            }
        }

        // =========================================================
        // DAMAGE RECEPTION (ACTIVE COMBAT & CRASHED PUNISH)
        // =========================================================
        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector3 hitOrigin)
        {
            if (!IsBattleActive || IsDefeated) return;

            // Invulnerable while up in the ruins channeling the rhythm barrage
            if (CurrentPhase == DuelPhase.StaveLeap || CurrentPhase == DuelPhase.StaveBarrage) return;

            float mult = (CurrentPhase == DuelPhase.CrashedStunned) ? 2.0f : 1.0f;
            float finalDmg = damage * mult;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDmg);
            UnderworldSoundscape.Instance?.PlayImpact(isHardBoom);
            UnderworldCamera2D.Instance?.AddShake(isHardBoom ? 0.38f : 0.14f);

            // Flash white on hit
            if (_headRenderers != null)
            {
                foreach (var sr in _headRenderers) if (sr != null) sr.color = Color.white;
            }

            if (CurrentPhase == DuelPhase.CrashedStunned)
            {
                OrpheusHud.Instance?.ShowSubtleCallout($"CRASHED HIT! -{Mathf.RoundToInt(finalDmg)} DMG");
            }

            if (CurrentHealth <= 0f)
            {
                DefeatGuardian();
            }
        }

        private void DefeatGuardian()
        {
            if (IsDefeated) return;
            IsDefeated = true;
            IsBattleActive = false;
            _paintedActor?.Cancel(true);
            if (ActiveBoss == this) ActiveBoss = null;

            if (_staveRoot != null) _staveRoot.gameObject.SetActive(false);
            foreach (StaveBeatNote n in _notes)
            {
                if (n.visual != null) Destroy(n.visual.gameObject);
            }
            _notes.Clear();

            ProceduralLyreAudio.Instance?.PlayCadenceComplete();
            ProceduralLyreAudio.Instance?.PlayShadePacified();

            if (_headRenderers != null)
            {
                foreach (var sr in _headRenderers)
                {
                    if (sr != null) sr.color = new Color(2.4f, 2.2f, 1.35f, 0.75f);
                }
            }

            UnderworldCamera2D.Instance?.AddShake(0.65f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 1.5f, 0f), 8.5f, new Color(2.7f, 2.25f, 1.15f, 0.95f), 0.65f, upright2D: true);
            PacifiedSpiritVessel.Spawn(transform.position, scaleMultiplier: 1.95f);

            // Restore camera & background from Abyssal Blackout
            if (_blackoutBackdrop != null)
            {
                if(Application.isPlaying) Destroy(_blackoutBackdrop); else DestroyImmediate(_blackoutBackdrop);
                _blackoutBackdrop = null;
            }
            if (_savedCameraBg && Camera.main != null)
            {
                Camera.main.backgroundColor = _originalCameraBgColor;
                _savedCameraBg = false;
            }
            if (PaintedChamberMap.Active != null)
            {
                if (PaintedChamberMap.Active.background != null) PaintedChamberMap.Active.background.enabled = true;
                if (PaintedChamberMap.Active.coverRoot != null) PaintedChamberMap.Active.coverRoot.gameObject.SetActive(true);
            }
            UnderworldParallaxRig[] rigs = UnityEngine.Object.FindObjectsByType<UnderworldParallaxRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int r = 0; r < rigs.Length; r++)
            {
                if (rigs[r] != null) rigs[r].gameObject.SetActive(true);
            }

            // Return camera & Orpheus back to 2.5D Isometric Chamber-Crawler!
            UnderworldCamera2D.Instance?.ExitSide2DBossView();
            if (OrpheusPlayerController.Instance != null)
            {
                OrpheusPlayerController.Instance.Exit2DBossStage();
                OrpheusPlayerController.Instance.Heal(25f);
            }

            OnCerberusDefeated?.Invoke();
            Destroy(gameObject, .95f);
        }

        private void OnDestroy()
        {
            if (ActiveBoss == this) ActiveBoss = null;
            if (_blackoutBackdrop != null)
            {
                if(Application.isPlaying) Destroy(_blackoutBackdrop); else DestroyImmediate(_blackoutBackdrop);
                _blackoutBackdrop = null;
            }
            if (_savedCameraBg && Camera.main != null)
            {
                Camera.main.backgroundColor = _originalCameraBgColor;
                _savedCameraBg = false;
            }
            if (PaintedChamberMap.Active != null)
            {
                if (PaintedChamberMap.Active.background != null) PaintedChamberMap.Active.background.enabled = true;
                if (PaintedChamberMap.Active.coverRoot != null) PaintedChamberMap.Active.coverRoot.gameObject.SetActive(true);
            }
            UnderworldParallaxRig[] rigs = UnityEngine.Object.FindObjectsByType<UnderworldParallaxRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int r = 0; r < rigs.Length; r++)
            {
                if (rigs[r] != null) rigs[r].gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Low 2D shockwave wall sweeping Left across the Boss Stage — Dash through with [Space] i-frames!
    /// </summary>
    public class StageSweepWave2D : MonoBehaviour
    {
        private Vector3 _stageCenter;
        private float _horizOffset;
        private float _speed;
        private float _life = 2.2f;

        public static void Spawn(Vector3 bossWorldPos, Vector3 stageCenter, float speed)
        {
            GameObject go = new GameObject("Boss2DSweepWave");
            go.transform.position = bossWorldPos;
            go.transform.localScale = new Vector3(0.85f, 1.55f, 1f);
            RenderPipeline2DSetup.AttachSprite(go, RenderPipeline2DSetup.DiamondSprite, new Color(2.8f, 0.38f, 0.20f, 0.95f), 19);

            StageSweepWave2D w = go.AddComponent<StageSweepWave2D>();
            w._stageCenter = stageCenter;
            w._horizOffset = Vector3.Dot(bossWorldPos - stageCenter, UnderworldCamera2D.IsoRightXZ) - 0.8f;
            w._speed = speed;
        }

        private void Update()
        {
            transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            _horizOffset -= _speed * Time.deltaTime;
            transform.position = _stageCenter + UnderworldCamera2D.IsoRightXZ * _horizOffset + new Vector3(0f, 0.72f, 0f);

            OrpheusPlayerController p = OrpheusPlayerController.Instance;
            if (p != null && p.IsIn2DBossStage && !p.IsDashing)
            {
                float pHoriz = Vector3.Dot(p.transform.position - _stageCenter, UnderworldCamera2D.IsoRightXZ);
                if (Mathf.Abs(pHoriz - _horizOffset) < 0.62f)
                {
                    p.TakeDamage(15f, transform.position);
                    Destroy(gameObject);
                    return;
                }
            }

            _life -= Time.deltaTime;
            if (_life <= 0f || _horizOffset < -10f) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Vertical 2D Stygian Geyser Telegraph under Orpheus's feet — step Left/Right with A/D before 0.72s eruption!
    /// </summary>
    public class StageGeyserHazard2D : MonoBehaviour
    {
        private Transform _columnVis;
        private float _timer;
        private const float TelegraphTime = 0.72f;

        public static void Spawn(Vector3 playerWorldPos)
        {
            GameObject go = new GameObject("Boss2DGeyserTelegraph");
            go.transform.position = new Vector3(playerWorldPos.x, 0.05f, playerWorldPos.z);

            GameObject col = new GameObject("GeyserBeam");
            col.transform.SetParent(go.transform, false);
            col.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            col.transform.localScale = new Vector3(0.28f, 4.4f, 1f);
            RenderPipeline2DSetup.AttachSprite(col, RenderPipeline2DSetup.SquareSprite, new Color(0.4f, 2.4f, 1.8f, 0.45f), 17);

            StageGeyserHazard2D g = go.AddComponent<StageGeyserHazard2D>();
            g._columnVis = col.transform;
        }

        private void Update()
        {
            transform.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            _timer += Time.deltaTime;

            if (_columnVis != null)
            {
                float w = Mathf.Lerp(0.22f, 1.15f, _timer / TelegraphTime);
                _columnVis.localScale = new Vector3(w, 4.4f, 1f);
            }

            if (_timer >= TelegraphTime)
            {
                GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 1.2f, 0f), 2.8f, new Color(0.4f, 2.7f, 1.9f, 1f), 0.25f, upright2D: true);
                UnderworldCamera2D.Instance?.AddShake(0.25f);

                OrpheusPlayerController p = OrpheusPlayerController.Instance;
                if (p != null && !p.IsDashing)
                {
                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0f;
                    if (diff.magnitude < 0.95f) p.TakeDamage(16f, transform.position);
                }
                Destroy(gameObject);
            }
        }
    }
}
