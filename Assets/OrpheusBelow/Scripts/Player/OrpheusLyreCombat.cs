using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// Orphic Lyre Combat & Physical S/B Chord Cadences (No Stat Menus, No RNG Boons!):
    ///   - SOFT ('S', < 0.60): 3-Hit Crescent Slash + Bolt + Dash-Strike + Acoustic Ping to Eurydice
    ///   - HARD BOOM ('B', >= 0.60): Radial Shockwave + Hitstop + Grief-Armor Shatter + Wall-Slam
    ///   - PHYSICAL CHORD CADENCES (Unlocked deterministically by pacifying Chamber Guardians):
    ///       Tier 1 [S - S - B] "Hymn of Dawn"          : Piercing 15.5m Golden Resonance Beam
    ///       Tier 2 [B - S]     "Sanctuary of Eurydice" : 4.5s Golden Aegis on Eurydice + Repels Abductors + Hum Reply
    ///       Tier 3 [S - B - S] "Siren's Convergence"   : Harmonic Vortex pulling Shades inward & cracking Grief-Armor
    /// </summary>
    public class OrpheusLyreCombat : MonoBehaviour
    {
        public static OrpheusLyreCombat Instance { get; private set; }

        [Header("Soft Hades Strike & Bolt (< 0.60)")]
        public float softBaseDamage = 20f;
        public float dashStrikeMultiplier = 1.45f;
        public float meleeSlashRadius = 2.85f;
        public float meleeSlashAngle = 128f;
        public float softBoltSpeed = 21f;

        [Header("Hard Orphic BOOM (>= 0.60)")]
        public float hardBoomMinDamage = 44f;
        public float hardBoomMaxDamage = 72f;
        public float hardBoomMinRadius = 4.3f;
        public float hardBoomMaxRadius = 6.4f;

        [Header("Physical S / B Chord Cadences")]
        public int UnlockedCadenceTier { get; set; } = 1;
        public float cadenceWindowSeconds = 1.35f;

        public string CurrentCadenceDisplay { get; private set; } = "";
        public int SoftNoteCount { get; private set; }
        public int HardBoomCount { get; private set; }
        public int CadenceCount { get; private set; }
        public int QuaverCount => SoftNoteCount;
        public int FermataCount => HardBoomCount;
        public IReadOnlyList<char> CadenceBuffer => _cadenceBuffer;
        public OrphicBreath Breath { get; } = new OrphicBreath();

        private OrpheusPlayerController _player;
        private BlurredEurydice _eurydice;
        private int _comboStep;
        private float _comboResetTimer;
        private bool _inHitstop;

        private readonly List<char> _cadenceBuffer = new List<char>(4);
        private float _cadenceTimer;
        private float _nextAmbientPingTime;
        private float _nextDryHint;

        private static readonly Collider[] HitBuf = new Collider[48];

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<OrpheusPlayerController>();
            RefreshCadenceDisplay();
        }

        private void Start()
        {
            _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();
            if (SensorPulseInput.Instance != null)
            {
                SensorPulseInput.Instance.OnAttackPulse += HandleAttackPulse;
            }
            RefreshCadenceDisplay();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (SensorPulseInput.Instance != null)
            {
                SensorPulseInput.Instance.OnAttackPulse -= HandleAttackPulse;
            }
        }

        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            Breath.Tick(Time.deltaTime);
            if (_comboResetTimer > 0f)
            {
                _comboResetTimer -= Time.deltaTime;
                if (_comboResetTimer <= 0f) _comboStep = 0;
            }

            if (_cadenceTimer > 0f)
            {
                _cadenceTimer -= Time.deltaTime;
                if (_cadenceTimer <= 0f && _cadenceBuffer.Count > 0)
                {
                    _cadenceBuffer.Clear();
                    RefreshCadenceDisplay();
                }
            }
        }

        private void HandleAttackPulse(float intensity, bool isHardBoom)
        {
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            if (_player == null || _player.IsGameOver || _player.IsVictory) return;
            if (_eurydice == null) _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();

            // Route pulses into Flute melody when Flute stance is active (A1)
            if (OrpheusFluteChannel.Instance != null && OrpheusFluteChannel.Instance.IsFluteActive)
            {
                OrpheusFluteChannel.Instance.ProcessFlutePulse(intensity, isHardBoom);
                return;
            }

            bool dashStrike = _player.IsInDashStrikeWindow;

            // The guardian's timed score has its own rules; exploration costs never block it.
            if (!_player.IsIn2DBossStage && !Breath.TryPlay(isHardBoom))
            {
                if (Breath.RecoveryRemaining <= 0f && Time.time >= _nextDryHint)
                {
                    _nextDryHint = Time.time + 1.5f;
                    UnderworldSoundscape.Instance?.PlayDryNote();
                    OrpheusHud.Instance?.ShowSubtleCallout("Out of breath — dodge and let it recover", 1.4f);
                }
                return;
            }

            if (CerberusRhythmBoss.ActiveBoss != null && CerberusRhythmBoss.ActiveBoss.IsBattleActive)
            {
                CerberusRhythmBoss.ActiveBoss.OnPlayerRhythmPulse(intensity, isHardBoom);
            }

            if (isHardBoom)
            {
                HardBoomCount++;
                ExecuteHardBoom(intensity, dashStrike);
            }
            else
            {
                SoftNoteCount++;
                ExecuteSoftComboStrike(intensity, dashStrike);
                TryPingEurydiceAcoustically();
            }

            // Physical S / B Chord Cadences operate freely in 2.5D exploration & combat!
            bool staveBarrage = CerberusRhythmBoss.ActiveBoss != null && CerberusRhythmBoss.ActiveBoss.IsStaveBarrageActive;
            if (!staveBarrage)
            {
                RecordCadenceNoteAndEvaluate(isHardBoom ? 'B' : 'S');
            }
        }

        private void TryPingEurydiceAcoustically()
        {
            if (_eurydice == null || _eurydice.IsVanished || _player.IsIn2DBossStage) return;

            float dist = Vector3.Distance(transform.position, _eurydice.transform.position);
            if (dist > 9.5f) return;

            // Always answer promptly inside deceptive Whisper/Mirage zones; gently throttle ambient replies elsewhere
            if (_eurydice.InTemptationZone || _eurydice.IsBeingAbducted || Time.time >= _nextAmbientPingTime)
            {
                _nextAmbientPingTime = Time.time + 2.8f;
                _eurydice.TriggerAcousticReply(grantSanctuaryShield: false);
            }
        }

        private void RecordCadenceNoteAndEvaluate(char note)
        {
            _cadenceBuffer.Add(note);
            if (_cadenceBuffer.Count > 3)
            {
                _cadenceBuffer.RemoveAt(0);
            }
            _cadenceTimer = cadenceWindowSeconds;

            int n = _cadenceBuffer.Count;

            // 1. Evaluate 3-note Cadences first
            if (n >= 3)
            {
                char a = _cadenceBuffer[n - 3];
                char b = _cadenceBuffer[n - 2];
                char c = _cadenceBuffer[n - 1];

                // [S - S - B] -> Hymn of Dawn (Tier 1+)
                if (UnlockedCadenceTier >= 1 && a == 'S' && b == 'S' && c == 'B')
                {
                    _cadenceBuffer.Clear();
                    _cadenceTimer = 0f;
                    ExecuteHymnOfDawnBeam();
                    RefreshCadenceDisplay();
                    return;
                }

                // [S - B - S] -> Siren's Convergence (Tier 3+)
                if (UnlockedCadenceTier >= 3 && a == 'S' && b == 'B' && c == 'S')
                {
                    _cadenceBuffer.Clear();
                    _cadenceTimer = 0f;
                    ExecuteSirensConvergenceVortex();
                    // Emergency grace: if Eurydice is actively being abducted while the player inputs B-S after a stray S, protect her too!
                    if (_eurydice != null && _eurydice.IsBeingAbducted && !_eurydice.HasSanctuaryShield)
                    {
                        _eurydice.TriggerAcousticReply(grantSanctuaryShield: true);
                    }
                    RefreshCadenceDisplay();
                    return;
                }
            }

            // 2. Evaluate 2-note Cadence: [B - S] -> Sanctuary of Eurydice (Tier 2+)
            if (n >= 2 && UnlockedCadenceTier >= 2)
            {
                char b = _cadenceBuffer[n - 2];
                char c = _cadenceBuffer[n - 1];
                if (b == 'B' && c == 'S')
                {
                    // Only hold off if Tier >= 3 AND buffer was [S, B, S], which already matched & returned above!
                    _cadenceBuffer.Clear();
                    _cadenceTimer = 0f;
                    ExecuteSanctuaryOfEurydice();
                    RefreshCadenceDisplay();
                    return;
                }
            }

            RefreshCadenceDisplay();
        }

        public void RefreshCadenceDisplay()
        {
            if (_cadenceBuffer.Count > 0)
            {
                StringBuilder sb = new StringBuilder("Cadence:  ");
                for (int i = 0; i < _cadenceBuffer.Count; i++)
                {
                    if (i > 0) sb.Append(" — ");
                    sb.Append(_cadenceBuffer[i] == 'B' ? "Boom" : "♪");
                }
                CurrentCadenceDisplay = sb.ToString();
            }
            else
            {
                if (UnlockedCadenceTier <= 1)
                    CurrentCadenceDisplay = "Cadences:  ♪-♪-Boom Beam";
                else if (UnlockedCadenceTier == 2)
                    CurrentCadenceDisplay = "Cadences:  ♪-♪-Boom Beam  ·  Boom-♪ Aegis";
                else
                    CurrentCadenceDisplay = "Cadences:  ♪-♪-Boom Beam  ·  Boom-♪ Aegis  ·  ♪-Boom-♪ Vortex";
            }
        }

        private void ExecuteHymnOfDawnBeam()
        {
            RewardCadence();
            ProceduralLyreAudio.Instance?.PlayCadenceComplete();
            OrpheusHud.Instance?.ShowSubtleCallout("Hymn of Dawn  (♪ — ♪ — Boom)");

            Vector3 aimDir = _player.AimDirectionXZ;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = UnderworldCamera2D.IsoUpXZ;
            aimDir.Normalize();

            const float beamLength = 20f;
            const float beamRadius = 1.7f;
            const float beamDamage = 105f;

            Vector3 startPos = transform.position + aimDir * 0.6f;
            GoldenResonanceBeamVfx.Spawn(startPos, aimDir, beamLength);
            UnderworldCamera2D.Instance?.AddShake(0.45f);

            // Sweep along the entire 15.5m golden corridor
            Vector3 p1 = transform.position + new Vector3(0f, 0.55f, 0f);
            Vector3 p2 = p1 + aimDir * beamLength;
            int count = Physics.OverlapCapsuleNonAlloc(p1, p2, beamRadius, HitBuf, ~0, QueryTriggerInteraction.Collide);
            HashSet<IOrphicTarget> visited = new HashSet<IOrphicTarget>();

            for (int i = 0; i < count; i++)
            {
                Collider c = HitBuf[i];
                StygianOrbProjectile orb = c.GetComponent<StygianOrbProjectile>();
                if (orb != null)
                {
                    orb.ShatterOrb();
                    continue;
                }

                IOrphicTarget tgt = c.GetComponentInParent<IOrphicTarget>();
                if (tgt != null && visited.Add(tgt))
                {
                    c.GetComponentInParent<ShadeEnemy>()?.ExposeToCadence();
                    tgt.OnOrphicHit(beamDamage, 0.92f, isHardBoom: true, transform.position);
                }
            }
        }

        private void ExecuteSanctuaryOfEurydice()
        {
            RewardCadence();
            ProceduralLyreAudio.Instance?.PlayCadenceComplete();
            OrpheusHud.Instance?.ShowSubtleCallout("Sanctuary of Eurydice  (Boom — ♪)");

            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 3.4f, new Color(2.5f, 2.05f, 0.55f, 0.90f), 0.30f);
            if (_eurydice != null && !_eurydice.IsVanished)
            {
                _eurydice.TriggerAcousticReply(grantSanctuaryShield: true);
            }
        }

        private void ExecuteSirensConvergenceVortex()
        {
            RewardCadence();
            ProceduralLyreAudio.Instance?.PlayCadenceComplete();
            OrpheusHud.Instance?.ShowSubtleCallout("Siren's Convergence  (♪ — Boom — ♪)");

            Vector3 aimDir = _player.AimDirectionXZ;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = UnderworldCamera2D.IsoUpXZ;
            aimDir.Normalize();

            Vector3 vortexCenter = transform.position + aimDir * 4.2f;
            vortexCenter.y = 0f;
            const float pullRadius = 5.8f;

            SirenConvergenceVortexVfx.Spawn(vortexCenter, pullRadius);
            UnderworldCamera2D.Instance?.AddShake(0.38f);

            int count = Physics.OverlapSphereNonAlloc(vortexCenter + new Vector3(0f, 0.55f, 0f), pullRadius, HitBuf, ~0, QueryTriggerInteraction.Collide);
            HashSet<IOrphicTarget> visited = new HashSet<IOrphicTarget>();

            for (int i = 0; i < count; i++)
            {
                Collider c = HitBuf[i];
                StygianOrbProjectile orb = c.GetComponent<StygianOrbProjectile>();
                if (orb != null)
                {
                    orb.ShatterOrb();
                    continue;
                }

                ShadeEnemy shade = c.GetComponentInParent<ShadeEnemy>();
                if (shade != null)
                {
                    shade.ExposeToCadence();
                    shade.PullToward(vortexCenter, stripShield: true);
                }

                IOrphicTarget tgt = c.GetComponentInParent<IOrphicTarget>();
                if (tgt != null && visited.Add(tgt))
                {
                    tgt.OnOrphicHit(38f, 0.78f, isHardBoom: false, vortexCenter);
                }
            }
        }

        private void RewardCadence()
        {
            CadenceCount++;
            Breath.CompleteCadence();
        }

        private void ExecuteSoftComboStrike(float intensity, bool isDashStrike)
        {
            ProceduralLyreAudio.Instance?.PlaySoftAttack(intensity);

            Vector3 aimDir = _player.AimDirectionXZ;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = UnderworldCamera2D.IsoUpXZ;
            aimDir.Normalize();

            if (isDashStrike)
            {
                _player.TriggerAttackLunge(aimDir, 18.5f, 0.13f);
            }
            else
            {
                _player.TriggerAttackLunge(aimDir, 7.8f, 0.075f);
            }

            float comboScale = _comboStep == 2 ? 1.25f : 1.0f;
            float dmg = softBaseDamage * comboScale * Mathf.Lerp(0.90f, 1.30f, intensity / 0.6f);
            if (isDashStrike) dmg *= dashStrikeMultiplier;
            bool staveBarrage = CerberusRhythmBoss.ActiveBoss != null && CerberusRhythmBoss.ActiveBoss.IsStaveBarrageActive;
            if (!staveBarrage) dmg *= Breath.DamageMultiplier;

            Vector3 slashOrigin = transform.position + new Vector3(0f, 0.06f, 0f);
            float reach = isDashStrike ? meleeSlashRadius * 1.22f : meleeSlashRadius;
            bool in2D = _player.IsIn2DBossStage;

            Color slashCol = isDashStrike
                ? new Color(2.8f, 1.35f, 0.35f, 0.95f)
                : (_comboStep == 2 ? new Color(2.6f, 2.05f, 0.50f, 0.92f) : new Color(2.2f, 1.75f, 0.42f, 0.88f));

            if (in2D)
            {
                GroundShockwaveRing.Spawn(transform.position + aimDir * 0.55f + new Vector3(0f, 0.95f, 0f), 1.9f, slashCol, 0.16f, upright2D: true);
            }
            else
            {
                GroundSlashArcVfx.Spawn(slashOrigin + aimDir * 0.45f, aimDir, reach * 1.65f, slashCol, _comboStep % 2 == 1);
            }
            UnderworldCamera2D.Instance?.AddShake(isDashStrike ? 0.16f : 0.07f);

            HitConeXZ(transform.position, aimDir, reach, meleeSlashAngle, dmg, intensity, isHardBoom: false);
            SpawnIsoLyreBolt(transform.position + aimDir * 0.65f + new Vector3(0f, in2D ? 0.95f : 0.55f, 0f), aimDir, dmg * 0.75f, intensity, isDashStrike, in2D);

            _comboStep = (_comboStep + 1) % 3;
            _comboResetTimer = 0.68f;
        }

        private void ExecuteHardBoom(float intensity, bool isDashStrike)
        {
            ProceduralLyreAudio.Instance?.PlayHardBoom(intensity);

            Vector3 aimDir = _player.AimDirectionXZ;
            if (isDashStrike)
            {
                _player.TriggerAttackLunge(aimDir, 15.0f, 0.11f);
            }

            float t = Mathf.InverseLerp(0.60f, 1.0f, intensity);
            float radius = Mathf.Lerp(hardBoomMinRadius, hardBoomMaxRadius, t);
            float dmg = Mathf.Lerp(hardBoomMinDamage, hardBoomMaxDamage, t) * (isDashStrike ? 1.2f : 1.0f);
            bool staveBarrage = CerberusRhythmBoss.ActiveBoss != null && CerberusRhythmBoss.ActiveBoss.IsStaveBarrageActive;
            if (!staveBarrage) dmg *= Breath.DamageMultiplier;
            bool in2D = _player.IsIn2DBossStage;

            Vector3 vfxCenter = in2D
                ? transform.position + new Vector3(0f, 1.0f, 0f)
                : transform.position + new Vector3(0f, 0.05f, 0f);

            GroundShockwaveRing.Spawn(vfxCenter, radius * 2.0f, new Color(2.9f, 0.42f, 0.22f, 1.0f), 0.34f, upright2D: in2D);
            GroundShockwaveRing.Spawn(vfxCenter, radius * 1.42f, new Color(2.7f, 1.95f, 0.45f, 0.95f), 0.24f, upright2D: in2D);
            GroundShockwaveRing.Spawn(vfxCenter, radius * 0.80f, new Color(2.6f, 2.6f, 2.6f, 0.90f), 0.15f, upright2D: in2D);

            UnderworldCamera2D.Instance?.AddShake(Mathf.Lerp(0.50f, 0.85f, t));

            int hits = HitRadiusXZ(transform.position, radius, dmg, intensity, isHardBoom: true);
            if (hits > 0 && !_inHitstop)
            {
                StartCoroutine(HitstopRoutine(0.048f));
            }
        }

        private int HitConeXZ(Vector3 origin, Vector3 forwardXZ, float radius, float fullAngleDeg, float dmg, float intensity, bool isHardBoom)
        {
            int n = Physics.OverlapSphereNonAlloc(origin + new Vector3(0f, 0.5f, 0f), radius, HitBuf, ~0, QueryTriggerInteraction.Collide);
            HashSet<IOrphicTarget> visited = new HashSet<IOrphicTarget>();
            float halfAngle = fullAngleDeg * 0.5f;
            int hits = 0;

            for (int i = 0; i < n; i++)
            {
                Collider c = HitBuf[i];

                StygianOrbProjectile orb = c.GetComponent<StygianOrbProjectile>();
                if (orb != null)
                {
                    orb.ShatterOrb();
                    continue;
                }

                IOrphicTarget tgt = c.GetComponentInParent<IOrphicTarget>();
                if (tgt == null || !visited.Add(tgt)) continue;

                Vector3 toTgt = c.transform.position - origin;
                toTgt.y = 0f;
                if (toTgt.magnitude <= 0.85f || Vector3.Angle(forwardXZ, toTgt) <= halfAngle)
                {
                    tgt.OnOrphicHit(dmg, intensity, isHardBoom, origin);
                    hits++;
                }
            }
            return hits;
        }

        private int HitRadiusXZ(Vector3 origin, float radius, float dmg, float intensity, bool isHardBoom)
        {
            int n = Physics.OverlapSphereNonAlloc(origin + new Vector3(0f, 0.5f, 0f), radius, HitBuf, ~0, QueryTriggerInteraction.Collide);
            HashSet<IOrphicTarget> visited = new HashSet<IOrphicTarget>();
            int hits = 0;

            for (int i = 0; i < n; i++)
            {
                Collider c = HitBuf[i];

                StygianOrbProjectile orb = c.GetComponent<StygianOrbProjectile>();
                if (orb != null)
                {
                    orb.ShatterOrb();
                    continue;
                }

                IOrphicTarget tgt = c.GetComponentInParent<IOrphicTarget>();
                if (tgt != null && visited.Add(tgt))
                {
                    tgt.OnOrphicHit(dmg, intensity, isHardBoom, origin);
                    hits++;
                }
            }
            return hits;
        }

        private void SpawnIsoLyreBolt(Vector3 pos, Vector3 dirXZ, float dmg, float intensity, bool empowered, bool upright2D)
        {
            GameObject bolt = new GameObject("IsoLyreBolt");
            bolt.transform.position = pos;

            GameObject vis = new GameObject("BoltVisual");
            vis.transform.SetParent(bolt.transform, false);
            if (upright2D)
            {
                vis.transform.localScale = empowered ? new Vector3(0.95f, 0.44f, 1f) : new Vector3(0.75f, 0.34f, 1f);
                RenderPipeline2DSetup.AttachBillboardSprite(vis, RenderPipeline2DSetup.DiamondSprite,
                    empowered ? new Color(2.8f, 1.6f, 0.4f, 1f) : new Color(2.4f, 1.95f, 0.45f, 1f), 18);
            }
            else
            {
                vis.transform.localScale = empowered ? new Vector3(0.48f, 0.92f, 1f) : new Vector3(0.36f, 0.72f, 1f);
                RenderPipeline2DSetup.AttachGroundDecal(vis, RenderPipeline2DSetup.DiamondSprite,
                    empowered ? new Color(2.8f, 1.6f, 0.4f, 1f) : new Color(2.4f, 1.95f, 0.45f, 1f), 14);
                bolt.transform.rotation = Quaternion.LookRotation(dirXZ, Vector3.up);
            }

            SphereCollider sc = bolt.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.34f;

            bolt.AddComponent<OrphicNoteProjectile>().Init(dirXZ, softBoltSpeed, dmg, intensity);
        }

        private IEnumerator HitstopRoutine(float realSeconds)
        {
            _inHitstop = true;
            if (Time.timeScale > 0.2f) Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(realSeconds);
            if (!UnderworldFrontEnd.BlocksGameplay && _player != null && !_player.IsGameOver && _player.LookBackProgress01 <= 0.01f)
            {
                Time.timeScale = 1f;
            }
            _inHitstop = false;
        }
    }

    public class GoldenResonanceBeamVfx : MonoBehaviour
    {
        private SpriteRenderer _outerSr;
        private SpriteRenderer _coreSr;
        private float _life;
        private const float MaxLife = 0.36f;
        private float _length;

        public static void Spawn(Vector3 startPos, Vector3 dirXZ, float length)
        {
            GameObject root = new GameObject("HymnOfDawn_BeamVFX");
            root.transform.position = new Vector3(startPos.x, 0.08f, startPos.z);
            root.transform.rotation = Quaternion.LookRotation(dirXZ, Vector3.up);

            GameObject outer = new GameObject("BeamOuter");
            outer.transform.SetParent(root.transform, false);
            outer.transform.localPosition = new Vector3(0f, 0f, length * 0.5f);
            outer.transform.localScale = new Vector3(1.85f, length, 1f);
            SpriteRenderer outerSr = RenderPipeline2DSetup.AttachGroundDecal(outer, RenderPipeline2DSetup.SquareSprite, new Color(2.8f, 2.05f, 0.45f, 0.92f), 19);

            GameObject core = new GameObject("BeamCore");
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.01f, length * 0.5f);
            core.transform.localScale = new Vector3(0.62f, length, 1f);
            SpriteRenderer coreSr = RenderPipeline2DSetup.AttachGroundDecal(core, RenderPipeline2DSetup.SquareSprite, new Color(3.2f, 3.0f, 2.2f, 0.98f), 20);

            GroundShockwaveRing.Spawn(new Vector3(startPos.x, 0.07f, startPos.z), 3.0f, new Color(2.9f, 2.2f, 0.5f, 1f), 0.28f);

            GoldenResonanceBeamVfx fx = root.AddComponent<GoldenResonanceBeamVfx>();
            fx._outerSr = outerSr;
            fx._coreSr = coreSr;
            fx._length = length;
            fx._life = MaxLife;
        }

        private void Update()
        {
            _life -= Time.unscaledDeltaTime;
            float u = 1f - Mathf.Clamp01(_life / MaxLife);
            float widthFactor = Mathf.Lerp(1.1f, 0.08f, u * u);

            if (_outerSr != null)
            {
                _outerSr.transform.localScale = new Vector3(1.85f * widthFactor, _length, 1f);
                Color c = _outerSr.color;
                c.a = (1f - u) * 0.9f;
                _outerSr.color = c;
            }
            if (_coreSr != null)
            {
                _coreSr.transform.localScale = new Vector3(0.62f * widthFactor, _length, 1f);
                Color c = _coreSr.color;
                c.a = 1f - u;
                _coreSr.color = c;
            }

            if (_life <= 0f) Destroy(gameObject);
        }
    }

    public class SirenConvergenceVortexVfx : MonoBehaviour
    {
        private SpriteRenderer _ringA;
        private SpriteRenderer _ringB;
        private float _maxDiameter;
        private float _elapsed;
        private const float Duration = 0.48f;

        public static void Spawn(Vector3 centerWorld, float radius)
        {
            GameObject go = new GameObject("SirenConvergence_VortexVFX");
            go.transform.position = centerWorld + new Vector3(0f, 0.06f, 0f);

            GameObject a = new GameObject("ImplodeRingOuter");
            a.transform.SetParent(go.transform, false);
            SpriteRenderer srA = RenderPipeline2DSetup.AttachGroundDecal(a, RenderPipeline2DSetup.RingSprite, new Color(1.9f, 0.75f, 2.7f, 0.95f), 19);

            GameObject b = new GameObject("ImplodeRingGold");
            b.transform.SetParent(go.transform, false);
            SpriteRenderer srB = RenderPipeline2DSetup.AttachGroundDecal(b, RenderPipeline2DSetup.RingSprite, new Color(2.7f, 2.1f, 0.55f, 0.95f), 20);

            SirenConvergenceVortexVfx fx = go.AddComponent<SirenConvergenceVortexVfx>();
            fx._ringA = srA;
            fx._ringB = srB;
            fx._maxDiameter = radius * 2.0f;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(_elapsed / Duration);
            // Contract inward from outer radius to tight harmonic focal point!
            float szA = Mathf.Lerp(_maxDiameter, 0.45f, Mathf.Pow(u, 0.65f));
            float szB = Mathf.Lerp(_maxDiameter * 0.68f, 0.25f, u);

            if (_ringA != null)
            {
                _ringA.transform.localScale = new Vector3(szA, szA, 1f);
                Color c = _ringA.color;
                c.a = (1f - u * 0.85f);
                _ringA.color = c;
            }
            if (_ringB != null)
            {
                _ringB.transform.localScale = new Vector3(szB, szB, 1f);
                Color c = _ringB.color;
                c.a = 1f - u;
                _ringB.color = c;
            }

            if (u >= 1f)
            {
                GroundShockwaveRing.Spawn(transform.position, 2.8f, new Color(2.7f, 2.15f, 0.65f, 0.95f), 0.22f);
                Destroy(gameObject);
            }
        }
    }

    public class OrphicNoteProjectile : MonoBehaviour
    {
        private Vector3 _dirXZ;
        private float _speed;
        private float _dmg;
        private float _intensity;
        private float _life = 0.72f;
        private readonly HashSet<IOrphicTarget> _hitSet = new HashSet<IOrphicTarget>();
        private bool _consumed;

        public void Init(Vector3 dirXZ, float speed, float dmg, float intensity)
        {
            _dirXZ = dirXZ.normalized;
            _speed = speed;
            _dmg = dmg;
            _intensity = intensity;
            if (GetComponent<PaintedActorDepth>() == null) gameObject.AddComponent<PaintedActorDepth>();
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (_consumed) return;
            Vector3 delta = _dirXZ * (_speed * deltaTime);
            Collider hit = ChamberCollision.ProjectileHit(transform.position, delta, .22f, false, out float distance);
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
            if (_consumed) return;
            if (other.GetComponent<OrpheusPlayerController>() != null) return;

            IsoObstacle obs = other.GetComponent<IsoObstacle>();
            if (obs != null && obs.kind == IsoObstacleKind.SolidWall)
            {
                _consumed = true;
                if (Application.isPlaying)
                {
                    GroundShockwaveRing.Spawn(new Vector3(transform.position.x, 0.06f, transform.position.z), 0.9f, new Color(2.0f, 1.6f, 0.4f, 0.8f), 0.14f);
                    Destroy(gameObject);
                }
                else DestroyImmediate(gameObject);
                return;
            }

            IOrphicTarget tgt = other.GetComponentInParent<IOrphicTarget>();
            if (tgt != null && _hitSet.Add(tgt))
            {
                _consumed = true;
                tgt.OnOrphicHit(_dmg, _intensity, false, transform.position - _dirXZ * 0.5f);
                GroundShockwaveRing.Spawn(new Vector3(transform.position.x, 0.06f, transform.position.z), 1.35f, new Color(2.3f, 1.85f, 0.5f, 0.9f), 0.16f);
                Destroy(gameObject);
            }
        }
    }
}
