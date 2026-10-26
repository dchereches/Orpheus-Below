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
        CerberusHoundOfHades  // Chamber IV Final Boss (126 BPM)
    }

    /// <summary>
    /// 2D Side-View Parallax Rhythm Guardian System!
    /// Every Chamber culminates in a Guardian that smoothly drops the camera from 2.5D Isometric into a
    /// cinematic 2D Parallax Duelling Stage:
    ///   - A luminous 2D Musical Stave stretches between Guardian (Right) and Orpheus's Golden Lyre Ring (Left)
    ///   - GOLD DIAMOND (◇)  -> Soft Tap / Puff (< 60%)
    ///   - CRIMSON HALO (◎)  -> HARD PUNCH / EXHALE BOOM (>= 60%)!
    ///   - Meanwhile, dodge 2D Sweeping Shockwaves (Spacebar Dash i-frames!) & Floor Geysers (A/D step)!
    /// </summary>
    public class CerberusRhythmBoss : MonoBehaviour, IOrphicTarget
    {
        public static CerberusRhythmBoss ActiveBoss { get; private set; }

        [Header("Guardian Identity & Rhythm")]
        public BossIdentity identity = BossIdentity.CantorOfFury;
        public string bossDisplayName = "Cantor of Tisiphone";
        public float maxHealth = 260f;
        public float bpm = 100f;
        public float noteTravelSeconds = 1.55f;
        public float hitWindowSeconds = 0.23f;

        public float CurrentHealth { get; private set; }
        public bool IsBattleActive { get; private set; }
        public bool IsDefeated { get; private set; }
        public int HarmonyStreak { get; private set; }
        public string LastJudgement { get; private set; } = "";

        public event Action OnCerberusDefeated;

        private Vector3 _stageCenter;
        private Transform _billboardRoot;
        private Transform[] _heads;
        private SpriteRenderer[] _headRenderers;
        private Color _headBaseColor;
        private SpriteRenderer _paintedGuardian;
        private float _nextDiscordPenalty;

        // 2D Stave UI in world space (perfectly aligned to 2D Side-View!)
        private Transform _staveRoot;
        private Transform _targetHitRing;
        private SpriteRenderer _targetRingSr;
        private Vector3 _noteSpawnLocal;
        private Vector3 _noteTargetLocal;

        private float _beatInterval;
        private float _nextBeatTime;
        private int _beatCounter;
        private int[] _pattern;

        private class StaveBeatNote
        {
            public bool requiresHardBoom;
            public float targetHitTime;
            public Transform visual;
            public bool resolved;
        }

        private readonly List<StaveBeatNote> _notes = new List<StaveBeatNote>();

        // Distinct musical rhythm charts per Mini-Boss / Final Boss! (0 = Rest/Hazard, 1 = Soft Note, 2 = HARD BOOM)
        private static readonly int[] PatternCantor   = { 1, 0, 1, 0,  2, 0, 0, 0,  1, 1, 2, 0,  1, 0, 2, 0 };
        private static readonly int[] PatternOarsman  = { 1, 1, 2, 0,  1, 0, 2, 0,  2, 0, 1, 1,  2, 2, 0, 0 };
        private static readonly int[] PatternSiren    = { 1, 2, 1, 0,  1, 1, 2, 0,  2, 1, 2, 0,  1, 2, 2, 0 };
        private static readonly int[] PatternCerberus = { 1, 1, 2, 0,  2, 1, 2, 0,  1, 2, 1, 2,  2, 2, 0, 0 };

        public void ConfigureBoss(BossIdentity id, Vector3 chamberStageCenter)
        {
            identity = id;
            _stageCenter = chamberStageCenter;

            // Place Boss on the right side of the 2D duel stage (+5.6m along IsoRightXZ)
            transform.position = _stageCenter + UnderworldCamera2D.IsoRightXZ * 5.6f;

            switch (id)
            {
                case BossIdentity.CantorOfFury:
                    bossDisplayName = "Alecto's Cantor";
                    maxHealth = 220f;
                    bpm = 98f;
                    noteTravelSeconds = 1.65f;
                    _headBaseColor = new Color(1.55f, 0.22f, 0.36f, 1f);
                    _pattern = PatternCantor;
                    break;

                case BossIdentity.OarsmanOfStyx:
                    bossDisplayName = "Warden of the Oar";
                    maxHealth = 280f;
                    bpm = 110f;
                    noteTravelSeconds = 1.50f;
                    _headBaseColor = new Color(0.28f, 1.45f, 1.25f, 1f);
                    _pattern = PatternOarsman;
                    break;

                case BossIdentity.SirenOfAcheron:
                    bossDisplayName = "Siren of Acheron";
                    maxHealth = 330f;
                    bpm = 118f;
                    noteTravelSeconds = 1.40f;
                    _headBaseColor = new Color(1.25f, 0.38f, 1.65f, 1f);
                    _pattern = PatternSiren;
                    break;

                case BossIdentity.CerberusHoundOfHades:
                    bossDisplayName = "Cerberus, Hound of Hades";
                    maxHealth = 460f;
                    bpm = 126f;
                    noteTravelSeconds = 1.32f;
                    _headBaseColor = new Color(1.75f, 0.20f, 0.22f, 1f);
                    _pattern = PatternCerberus;
                    break;
            }

            maxHealth *= 1.45f;
            hitWindowSeconds = 0.18f;
            CurrentHealth = maxHealth;
            _beatInterval = 60f / Mathf.Max(50f, bpm);

            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.65f;
            sc.center = new Vector3(0f, 1.3f, 0f);

            BuildBossVisuals();
            gameObject.SetActive(false); // Hidden during 2.5D mob waves; awakens cleanly for the 2D Rhythm Showdown!
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

            Sprite portrait = UnderworldArtLibrary.Creature(4 + (int)identity);
            if (portrait != null)
            {
                foreach (SpriteRenderer old in _billboardRoot.GetComponentsInChildren<SpriteRenderer>()) old.enabled = false;
                GameObject art = new GameObject("PaintedGuardian");
                art.transform.SetParent(_billboardRoot, false);
                art.transform.localScale = Vector3.one * 1.65f;
                _paintedGuardian = RenderPipeline2DSetup.AttachSprite(art, portrait, Color.white, 14);
            }

            // --- CRISP 2D MUSICAL STAVE STRETCHING ACROSS UPPER-MID SCREEN ---
            _staveRoot = new GameObject("RhythmStave2D").transform;
            _staveRoot.SetParent(transform, false);
            _staveRoot.position = _stageCenter + new Vector3(0f, 3.65f, 0f);
            _staveRoot.rotation = UnderworldCamera2D.Side2DRotation;

            _noteSpawnLocal = new Vector3(4.9f, 0f, 0f);   // Above Boss (Right)
            _noteTargetLocal = new Vector3(-4.2f, 0f, 0f); // Above Orpheus (Left)

            // 3 delicate golden staff hairlines
            for (int s = -1; s <= 1; s++)
            {
                GameObject line = new GameObject("StaffLine_" + s);
                line.transform.SetParent(_staveRoot, false);
                line.transform.localPosition = new Vector3(0.35f, s * 0.24f, 0.05f);
                line.transform.localScale = new Vector3(9.8f, 0.03f, 1f);
                RenderPipeline2DSetup.AttachSprite(line, RenderPipeline2DSetup.SquareSprite, new Color(0.95f, 0.80f, 0.45f, 0.26f), 18);
            }

            // Pulsing Golden Lyre Target Ring on the Left
            GameObject ringGo = new GameObject("GoldenLyreHitRing");
            ringGo.transform.SetParent(_staveRoot, false);
            ringGo.transform.localPosition = _noteTargetLocal;
            ringGo.transform.localScale = new Vector3(1.38f, 1.38f, 1f);
            _targetRingSr = RenderPipeline2DSetup.AttachSprite(ringGo, RenderPipeline2DSetup.RingSprite, new Color(2.5f, 2.05f, 0.55f, 0.95f), 20);
            _targetHitRing = ringGo.transform;
        }

        public void ActivateGuardianDuel()
        {
            if (IsBattleActive || IsDefeated) return;

            gameObject.SetActive(true);
            ActiveBoss = this;
            IsBattleActive = true;
            HarmonyStreak = 0;
            LastJudgement = "Gold = Soft Tap   |   Crimson = Hard Boom";
            _nextBeatTime = Time.time + 1.05f; // Allow 1s for smooth 2.5D -> 2D camera sweep!

            UnderworldCamera2D.Instance?.EnterSide2DBossView(_stageCenter, identity == BossIdentity.CerberusHoundOfHades ? 6.3f : 5.8f);
            OrpheusPlayerController.Instance?.Enter2DBossStage(_stageCenter, minX: -8.2f, maxX: 2.2f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 1.2f, 0f), 5.2f, _headBaseColor, 0.45f, upright2D: true);

            OrpheusHud.Instance?.ShowBanner(bossDisplayName + "  —  Rhythm Duel", 3.2f);
        }

        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay) return;
            if (_billboardRoot != null) _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            if (_staveRoot != null)
            {
                _staveRoot.position = _stageCenter + new Vector3(0f, 3.65f, 0f);
                _staveRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            }

            if (IsDefeated || !IsBattleActive) return;

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory) return;

            // Animate heads & metronome target ring to the exact BPM!
            float beatPhase = (Time.time / _beatInterval) * Mathf.PI * 2f;
            if (_paintedGuardian != null)
            {
                _paintedGuardian.transform.localPosition = Vector3.up * (Mathf.Sin(beatPhase * 0.5f) * 0.08f);
                _paintedGuardian.color = Color.Lerp(_paintedGuardian.color, Color.white, Time.deltaTime * 8f);
            }
            float metronomePulse = 1.32f + Mathf.Pow(Mathf.Max(0f, Mathf.Cos(beatPhase * 0.5f)), 6f) * 0.22f;
            if (_targetHitRing != null) _targetHitRing.localScale = new Vector3(metronomePulse, metronomePulse, 1f);

            for (int i = 0; i < _heads.Length; i++)
            {
                Vector3 basePos = _heads.Length == 1
                    ? new Vector3(-0.15f, 2.65f, 0f)
                    : (i == 0 ? new Vector3(-1.1f, 2.25f, 0f) : i == 1 ? new Vector3(0f, 2.95f, -0.02f) : new Vector3(1.1f, 2.25f, 0f));
                _heads[i].localPosition = basePos + new Vector3(0f, Mathf.Sin(beatPhase + i * 1.1f) * 0.18f, 0f);
                _headRenderers[i].color = Color.Lerp(_headRenderers[i].color, _headBaseColor, Time.deltaTime * 8f);
            }

            if (Time.time >= _nextBeatTime)
            {
                if (_pattern == null || _pattern.Length == 0) _pattern = PatternCantor;
                int pat = _pattern[_beatCounter % _pattern.Length];
                _beatCounter++;
                _nextBeatTime += _beatInterval;

                ProceduralLyreAudio.Instance?.PlayRhythmBeat(pat == 2);
                if (pat != 0)
                {
                    SpawnStaveNote(pat == 2, _nextBeatTime + noteTravelSeconds - _beatInterval);
                }
                else if (_beatCounter % 4 == 0)
                {
                    // Physical 2D stage attack on rest measures so left-hand A/D + Space Dash stays engaged!
                    Trigger2DStageHazard(player);
                }
            }

            UpdateStaveNotes(player);
        }

        private void SpawnStaveNote(bool hardBoomRequired, float hitTime)
        {
            GameObject go = new GameObject(hardBoomRequired ? "Note2D_BOOM" : "Note2D_SOFT");
            go.transform.SetParent(_staveRoot, false);
            go.transform.localPosition = _noteSpawnLocal;

            if (hardBoomRequired)
            {
                go.transform.localScale = new Vector3(1.22f, 1.22f, 1f);
                RenderPipeline2DSetup.AttachSprite(go, RenderPipeline2DSetup.RingSprite, new Color(2.9f, 0.28f, 0.25f, 1f), 22);

                GameObject core = new GameObject("BoomCore");
                core.transform.SetParent(go.transform, false);
                core.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                RenderPipeline2DSetup.AttachSprite(core, RenderPipeline2DSetup.DiamondSprite, new Color(2.9f, 1.2f, 0.3f, 1f), 23);
            }
            else
            {
                go.transform.localScale = new Vector3(0.74f, 0.74f, 1f);
                RenderPipeline2DSetup.AttachSprite(go, RenderPipeline2DSetup.DiamondSprite, new Color(2.5f, 2.05f, 0.45f, 1f), 22);
            }

            _notes.Add(new StaveBeatNote
            {
                requiresHardBoom = hardBoomRequired,
                targetHitTime = hitTime,
                visual = go.transform,
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
                    n.visual.localPosition = Vector3.LerpUnclamped(_noteSpawnLocal, _noteTargetLocal, u);
                }

                if (rem < -hitWindowSeconds)
                {
                    n.resolved = true;
                    HarmonyStreak = 0;
                    LastJudgement = "Missed Beat — Dash the Shockwave!";
                    Spawn2DSweepWave();
                    if (n.visual != null) Destroy(n.visual.gameObject);
                    _notes.RemoveAt(i);
                }
            }
        }

        public void OnPlayerRhythmPulse(float intensity, bool isHardBoom)
        {
            if (UnderworldFrontEnd.BlocksGameplay || !IsBattleActive || IsDefeated) return;

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
                LastJudgement = "Off beat — wait for the note to reach the ring";
                if (Time.time >= _nextDiscordPenalty)
                {
                    _nextDiscordPenalty = Time.time + 0.8f;
                    Spawn2DSweepWave();
                    UnderworldSoundscape.Instance?.PlayDryNote();
                }
                return;
            }

            best.resolved = true;
            if (best.requiresHardBoom == isHardBoom)
            {
                HarmonyStreak++;
                float streakMult = 1f + Mathf.Min(HarmonyStreak, 6) * 0.08f;
                float dmg = (isHardBoom ? 54f : 30f) * streakMult;
                CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);

                LastJudgement = isHardBoom
                    ? $"Orphic Boom  x{HarmonyStreak}"
                    : $"Perfect Chord  x{HarmonyStreak}";

                if (_targetHitRing != null)
                {
                    GroundShockwaveRing.Spawn(_targetHitRing.position, isHardBoom ? 3.4f : 2.1f,
                        isHardBoom ? new Color(2.9f, 0.45f, 0.25f, 1f) : new Color(2.6f, 2.1f, 0.5f, 1f), 0.26f, upright2D: true);
                }
                GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 1.5f, 0f), isHardBoom ? 4.8f : 2.8f,
                    new Color(2.7f, 1.9f, 0.45f, 1f), 0.28f, upright2D: true);

                foreach (var sr in _headRenderers) sr.color = new Color(3f, 3f, 3f, 1f);
                if (_paintedGuardian != null) _paintedGuardian.color = new Color(1f, 0.79f, 0.48f);
                UnderworldCamera2D.Instance?.AddShake(isHardBoom ? 0.58f : 0.20f);

                if (CurrentHealth <= 0f) DefeatGuardian();
            }
            else
            {
                HarmonyStreak = 0;
                LastJudgement = best.requiresHardBoom ? "Too Soft — Needed Hard Boom" : "Too Loud — Needed Soft Note";
                Spawn2DSweepWave();
            }
        }

        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector3 hitOrigin)
        {
            // Only correctly timed stave notes damage guardians. Free projectile/boom chip
            // previously let the player bypass the rhythm encounter entirely.
        }

        private void Trigger2DStageHazard(OrpheusPlayerController player)
        {
            if (identity == BossIdentity.OarsmanOfStyx || (_beatCounter % 8 == 0 && identity == BossIdentity.CerberusHoundOfHades))
            {
                StageGeyserHazard2D.Spawn(player.transform.position);
            }
            else
            {
                Spawn2DSweepWave();
            }
        }

        private void Spawn2DSweepWave()
        {
            StageSweepWave2D.Spawn(transform.position + new Vector3(0f, 0.65f, 0f), _stageCenter,
                speed: identity == BossIdentity.CerberusHoundOfHades ? 12.8f : 10.6f);
        }

        private void DefeatGuardian()
        {
            if (IsDefeated) return;
            IsDefeated = true;
            IsBattleActive = false;
            if (ActiveBoss == this) ActiveBoss = null;

            foreach (StaveBeatNote n in _notes)
            {
                if (n.visual != null) Destroy(n.visual.gameObject);
            }
            _notes.Clear();

            // #4 Catharsis & Slumber: Guardian is charmed into peaceful golden rest rather than slaughtered
            ProceduralLyreAudio.Instance?.PlayCadenceComplete();
            ProceduralLyreAudio.Instance?.PlayShadePacified();
            if (_paintedGuardian != null) _paintedGuardian.color = new Color(1f, 0.87f, 0.58f, 0.65f);

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

            // Smoothly return camera & Orpheus from 2D Side-View back to 2.5D Isometric Chamber-Crawler!
            UnderworldCamera2D.Instance?.ExitSide2DBossView();
            if (OrpheusPlayerController.Instance != null)
            {
                OrpheusPlayerController.Instance.Exit2DBossStage();
                OrpheusPlayerController.Instance.Heal(18f);
            }

            OnCerberusDefeated?.Invoke();
            Destroy(gameObject, 0.35f);
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
