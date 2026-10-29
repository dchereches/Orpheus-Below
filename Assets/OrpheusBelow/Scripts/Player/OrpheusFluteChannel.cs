using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    /// <summary>
    /// Flute (Aulos) Stance & Cataclysmic Requiem Area Damage Mechanic:
    ///   - Hold/Toggle [F] to draw the Flute (slows movement by 65%, disables Dash).
    ///   - A 4-note sequence appears above Orpheus: input matching Soft ('S', <60%) or Hard Boom ('B', >=60%) notes.
    ///   - Each correct note pulses for 45 AOE damage & micro-staggers surrounding enemies.
    ///   - The 4th note detonates the CATACLYSMIC REQUIEM: a 240+ damage screen-shattering Nova that
    ///     obliterates basic shades, cracks all armor, destroys projectiles, and stuns heavy survivors for 3s.
    ///     Also chunks massive damage into Guardians (480 dmg if Crashed!).
    ///   - Discordant Backlash: Wrong note, timeout, or taking damage screeches, interrupts Flute,
    ///     and frenzies nearby Shades into faster aggressive lunges!
    /// </summary>
    public class OrpheusFluteChannel : MonoBehaviour
    {
        public static OrpheusFluteChannel Instance { get; private set; }

        public bool IsFluteActive { get; private set; }
        public int StanceEntryCount { get; private set; }
        public float earshotRadius = 8.5f;
        public float sequenceTimeoutSeconds = 4.2f;

        public IReadOnlyList<char> TargetSequence => _targetSequence;
        public int CurrentNoteIndex => _currentNoteIndex;
        public float SequenceTimeRemaining01 => Mathf.Clamp01(_sequenceTimer / Mathf.Max(0.1f, sequenceTimeoutSeconds));

        private Sprite _quaverSprite;
        private Sprite _fermataSprite;

        private OrpheusPlayerController _player;
        private OrpheusSpriteAnimator _spriteAnimator;
        private Transform _billboardRoot;
        private Transform _fluteVis;
        private Transform _noteDisplayRoot;
        private SpriteRenderer[] _noteRenderers;
        private Transform[] _noteTransforms;

        private readonly List<char> _targetSequence = new List<char>(4);
        private int _currentNoteIndex;
        private float _sequenceTimer;
        private float _lockoutCooldown;
        private float _staggerTimer;

        private static readonly char[][] PresetMelodies = new[]
        {
            new[] { 'S', 'S', 'B', 'S' },
            new[] { 'S', 'B', 'S', 'B' },
            new[] { 'B', 'S', 'S', 'B' },
            new[] { 'S', 'S', 'S', 'B' }
        };

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<OrpheusPlayerController>();
            _spriteAnimator = GetComponent<OrpheusSpriteAnimator>();
            BuildFluteVisuals();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildFluteVisuals()
        {
            _billboardRoot = new GameObject("FluteStaveBillboard").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.localPosition = new Vector3(0f, 2.15f, 0f);
            _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;

            GameObject fluteObj = new GameObject("AulosFluteVisual");
            fluteObj.transform.SetParent(transform, false);
            fluteObj.transform.localPosition = new Vector3(0.35f, 0.95f, -0.05f);
            fluteObj.transform.localScale = new Vector3(0.22f, 0.65f, 1f);
            RenderPipeline2DSetup.AttachSprite(fluteObj, RenderPipeline2DSetup.DiamondSprite, new Color(2.8f, 2.1f, 0.5f, 1f), 17);
            _fluteVis = fluteObj.transform;
            _fluteVis.gameObject.SetActive(false);

            _quaverSprite = Resources.Load<Sprite>("MusicalNotation/symbol_quaver_small");
            _fermataSprite = Resources.Load<Sprite>("MusicalNotation/symbol_fermata_boom");

            _noteDisplayRoot = new GameObject("FluteNoteStave").transform;
            _noteDisplayRoot.SetParent(_billboardRoot, false);

            // Subtle parchment backing ribbon behind the stave so black notes remain crisp against any background
            GameObject staveBacking = new GameObject("FluteMiniStaffBacking");
            staveBacking.transform.SetParent(_noteDisplayRoot, false);
            staveBacking.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            staveBacking.transform.localScale = new Vector3(1.82f, 0.34f, 1f);
            RenderPipeline2DSetup.AttachSprite(staveBacking, RenderPipeline2DSetup.SquareSprite, new Color(0.96f, 0.94f, 0.88f, 0.20f), 19);

            // Mini golden staff baseline behind the 4 notes (clean, subtle hairline)
            GameObject staveLine = new GameObject("FluteMiniStaffLine");
            staveLine.transform.SetParent(_noteDisplayRoot, false);
            staveLine.transform.localPosition = new Vector3(0f, -0.015f, 0.04f);
            staveLine.transform.localScale = new Vector3(1.72f, 0.012f, 1f);
            RenderPipeline2DSetup.AttachSprite(staveLine, RenderPipeline2DSetup.SquareSprite, new Color(0.95f, 0.82f, 0.45f, 0.45f), 20);

            _noteRenderers = new SpriteRenderer[4];
            _noteTransforms = new Transform[4];

            // 4 note runes spread apart above Orpheus for effortless readability
            for (int i = 0; i < 4; i++)
            {
                GameObject noteGo = new GameObject("NoteRune_" + i);
                noteGo.transform.SetParent(_noteDisplayRoot, false);
                noteGo.transform.localPosition = new Vector3((i - 1.5f) * 0.42f, 0f, 0f);
                noteGo.transform.localScale = new Vector3(0.22f, 0.22f, 1f);

                _noteRenderers[i] = RenderPipeline2DSetup.AttachSprite(
                    noteGo, _quaverSprite != null ? _quaverSprite : RenderPipeline2DSetup.DiamondSprite, new Color(0.8f, 0.8f, 0.8f, 0.3f), 23);
                _noteTransforms[i] = noteGo.transform;
            }

            _noteDisplayRoot.gameObject.SetActive(false);
            UpdateStavePose();
        }

        private void UpdateStavePose()
        {
            if (_billboardRoot == null) return;
            Quaternion rotation = UnderworldCamera2D.CurrentBillboardRotation;
            Vector3 up = rotation * Vector3.up;
            float height = 2.15f;
            SpriteRenderer body = _spriteAnimator != null ? _spriteAnimator.BodyRenderer : null;
            if (_spriteAnimator != null && _spriteAnimator.UsesProductionArt && body != null && body.sprite != null)
            {
                Vector3 top = body.transform.TransformPoint(Vector3.up * body.sprite.bounds.max.y);
                height = Vector3.Dot(top - transform.position, up) + .45f;
            }
            _billboardRoot.rotation = rotation;
            _billboardRoot.position = transform.position + up * height;
        }

        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
            UpdateStavePose();

            float dt = Time.deltaTime;
            if (_lockoutCooldown > 0f) _lockoutCooldown -= dt;
            if (_staggerTimer > 0f) _staggerTimer -= dt;

            if (_player == null || _player.IsGameOver || _player.IsVictory)
            {
                if (IsFluteActive) ExitFluteStance();
                return;
            }

            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            bool fluteToggle = (kb != null && kb.fKey.wasPressedThisFrame)
                            || (pad != null && pad.leftTrigger.wasPressedThisFrame);

            if (fluteToggle)
            {
                if (IsFluteActive)
                {
                    ExitFluteStance();
                }
                else if (_lockoutCooldown <= 0f && _staggerTimer <= 0f)
                {
                    EnterFluteStance();
                }
            }

            if (IsFluteActive)
            {
                if (_spriteAnimator != null && _spriteAnimator.CurrentState == OrpheusAnimationState.Idle)
                    _spriteAnimator.SetAulosLoop(true);
                _sequenceTimer -= dt;
                if (_sequenceTimer <= 0f)
                {
                    TriggerDiscordantBacklash("Melody Faded — Backlash!");
                    return;
                }

                UpdateNoteVisuals();
            }
        }

        public void EnterFluteStance()
        {
            if (IsFluteActive) return;
            IsFluteActive = true;
            _spriteAnimator?.EnterFlute();
            UpdateStavePose();
            StanceEntryCount++;
            GenerateNewMelody();
            if (_noteDisplayRoot != null) _noteDisplayRoot.gameObject.SetActive(true);
            if (_fluteVis != null) _fluteVis.gameObject.SetActive(_spriteAnimator == null || !_spriteAnimator.UsesProductionArt);
            OrpheusHud.Instance?.ShowSubtleCallout("Aulos Drawn  [Play Notes — Cataclysmic Requiem on 4th Note!]");
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.05f, 0f), 2.2f, new Color(0.6f, 1.8f, 2.4f, 0.7f), 0.25f);
        }

        public void ExitFluteStance()
        {
            IsFluteActive = false;
            _spriteAnimator?.SetAulosLoop(false);
            if (_noteDisplayRoot != null) _noteDisplayRoot.gameObject.SetActive(false);
            if (_fluteVis != null) _fluteVis.gameObject.SetActive(false);
        }

        private void GenerateNewMelody()
        {
            _targetSequence.Clear();
            char[] preset = PresetMelodies[Random.Range(0, PresetMelodies.Length)];
            _targetSequence.AddRange(preset);
            _currentNoteIndex = 0;
            _sequenceTimer = sequenceTimeoutSeconds;
            RefreshStaveNotes();
        }

        private void RefreshStaveNotes()
        {
            for (int i = 0; i < 4; i++)
            {
                if (i >= _targetSequence.Count) continue;
                bool isBoom = _targetSequence[i] == 'B';
                if (isBoom && _fermataSprite != null)
                {
                    _noteRenderers[i].sprite = _fermataSprite;
                    _noteTransforms[i].localScale = new Vector3(0.22f, 0.22f, 1f);
                }
                else if (!isBoom && _quaverSprite != null)
                {
                    _noteRenderers[i].sprite = _quaverSprite;
                    _noteTransforms[i].localScale = new Vector3(0.22f, 0.22f, 1f);
                }
                else
                {
                    _noteRenderers[i].sprite = isBoom ? RenderPipeline2DSetup.RingSprite : RenderPipeline2DSetup.DiamondSprite;
                    _noteTransforms[i].localScale = new Vector3(0.22f, 0.22f, 1f);
                }
            }
        }

        private void UpdateNoteVisuals()
        {
            float pulse = 1.0f + Mathf.Sin(Time.time * 12f) * 0.15f;

            for (int i = 0; i < 4; i++)
            {
                if (i >= _targetSequence.Count) continue;
                bool isBoom = _targetSequence[i] == 'B';
                Vector3 baseScale = new Vector3(0.22f, 0.22f, 1f);

                if (i < _currentNoteIndex)
                {
                    // Completed note - illuminates into radiant gold
                    _noteRenderers[i].color = new Color(1f, 0.88f, 0.25f, 0.95f);
                    _noteTransforms[i].localScale = baseScale;
                }
                else if (i == _currentNoteIndex)
                {
                    // Active target note needed now - simple black with 40% transparency (60% opacity), gently pulsing
                    _noteRenderers[i].color = new Color(0f, 0f, 0f, 0.60f);
                    _noteTransforms[i].localScale = baseScale * pulse;
                }
                else
                {
                    // Upcoming notes needed next - simple black with 40% transparency (60% opacity)
                    _noteRenderers[i].color = new Color(0f, 0f, 0f, 0.60f);
                    _noteTransforms[i].localScale = baseScale * 0.92f;
                }
            }
        }

        public bool ProcessFlutePulse(float intensity, bool isHardBoom)
        {
            if (!IsFluteActive || _targetSequence.Count == 0) return false;

            char playedNote = isHardBoom ? 'B' : 'S';
            char expectedNote = _targetSequence[_currentNoteIndex];

            if (playedNote == expectedNote)
            {
                _spriteAnimator?.TriggerFluteNote(isHardBoom);
                // Correct note played
                ProceduralLyreAudio.Instance?.PlayFluteNote(isHardBoom);
                GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 2.2f + _currentNoteIndex * 0.8f,
                    isHardBoom ? new Color(2.8f, 0.35f, 0.25f, 0.85f) : new Color(0.6f, 2.2f, 2.6f, 0.85f), 0.20f);

                // --- NOTE PULSE AOE DAMAGE: 45 DMG & Micro-Stagger ---
                const float pulseDmg = 45f;
                ShadeEnemy[] nearbyShades = Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None);
                for (int sIdx = 0; sIdx < nearbyShades.Length; sIdx++)
                {
                    ShadeEnemy s = nearbyShades[sIdx];
                    if (s == null || s.IsFleeing) continue;
                    if (Vector3.Distance(transform.position, s.transform.position) <= earshotRadius)
                    {
                        s.OnOrphicHit(pulseDmg, 0.8f, isHardBoom, transform.position);
                        s.ApplyStun(0.35f, breakShield: isHardBoom);
                    }
                }

                if (CerberusRhythmBoss.ActiveBoss != null && CerberusRhythmBoss.ActiveBoss.IsBattleActive)
                {
                    float bossDist = Vector3.Distance(transform.position, CerberusRhythmBoss.ActiveBoss.transform.position);
                    if (bossDist <= earshotRadius + 4.5f)
                    {
                        CerberusRhythmBoss.ActiveBoss.OnOrphicHit(pulseDmg, 0.8f, isHardBoom, transform.position);
                    }
                }

                _currentNoteIndex++;
                _sequenceTimer = sequenceTimeoutSeconds; // refresh timer on successful hit

                if (_currentNoteIndex >= 4)
                {
                    CompleteMelodyOfSorrow();
                }
                return true;
            }
            else
            {
                // Discordant note played (A2)
                TriggerDiscordantBacklash("Sour Note — Discord Frenzy!");
                return true;
            }
        }

        private void CompleteMelodyOfSorrow()
        {
            _spriteAnimator?.TriggerRequiem();
            ProceduralLyreAudio.Instance?.PlayFluteFleeCatharsis();
            UnderworldSoundscape.Instance?.PlayImpact(true);
            UnderworldCamera2D.Instance?.AddShake(0.85f);

            // Triple expanding screen-shattering shockwave rings
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), earshotRadius * 2.2f,
                new Color(0.6f, 2.4f, 3.0f, 1f), 0.55f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.08f, 0f), earshotRadius * 1.5f,
                new Color(2.9f, 2.4f, 0.6f, 1f), 0.38f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.10f, 0f), earshotRadius * 0.8f,
                new Color(3.5f, 3.5f, 3.5f, 1f), 0.25f);

            // Shatter all enemy projectiles across the chamber
            PaintedEnemyProjectile.ClearAll();
            StygianOrbProjectile[] orbs = Object.FindObjectsByType<StygianOrbProjectile>(FindObjectsSortMode.None);
            for (int i = 0; i < orbs.Length; i++)
            {
                if (orbs[i] != null) orbs[i].ShatterOrb();
            }

            const float novaDamage = 240f;
            int hitCount = 0;
            int convincedCount = 0;

            ShadeEnemy[] shades = Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None);
            for (int i = 0; i < shades.Length; i++)
            {
                ShadeEnemy s = shades[i];
                if (s == null || s.IsFleeing) continue;

                float dist = Vector3.Distance(transform.position, s.transform.position);
                if (dist <= earshotRadius * 1.5f)
                {
                    hitCount++;
                    s.ApplyStun(3.0f, breakShield: true);
                    s.OnOrphicHit(novaDamage, 1.0f, isHardBoom: true, transform.position);

                    if (s.CurrentHealth > 0f && s.IsVulnerableToFlute)
                    {
                        s.ConvinceToFlee();
                        convincedCount++;
                    }
                }
            }

            // Hit 2D Guardian Boss if present
            if (CerberusRhythmBoss.ActiveBoss != null && CerberusRhythmBoss.ActiveBoss.IsBattleActive)
            {
                float bossDist = Vector3.Distance(transform.position, CerberusRhythmBoss.ActiveBoss.transform.position);
                if (bossDist <= earshotRadius * 2.5f)
                {
                    CerberusRhythmBoss.ActiveBoss.OnOrphicHit(novaDamage, 1.0f, isHardBoom: true, transform.position);
                    hitCount++;
                }
            }

            // Full breath refill reward for executing the full 4-note Requiem!
            OrpheusLyreCombat.Instance?.Breath.Refill();

            OrpheusHud.Instance?.ShowSubtleCallout($"CATACLYSMIC REQUIEM! -{Mathf.RoundToInt(novaDamage)} AOE Nova ({hitCount} Hit)");

            GenerateNewMelody();
        }

        public void InterruptByDamage()
        {
            if (IsFluteActive)
            {
                TriggerDiscordantBacklash("Struck Mid-Melody — Discord Frenzy!");
            }
        }

        private void TriggerDiscordantBacklash(string reason)
        {
            _spriteAnimator?.TriggerDiscord();
            ProceduralLyreAudio.Instance?.PlayFluteBacklash();
            OrpheusHud.Instance?.ShowSubtleCallout(reason);

            UnderworldCamera2D.Instance?.AddShake(0.48f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 4.2f,
                new Color(3.2f, 0.15f, 0.18f, 1f), 0.35f);

            _lockoutCooldown = 2.0f;
            _staggerTimer = 0.42f;

            // Frenzy all nearby shades (A2)
            ShadeEnemy[] shades = Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None);
            for (int i = 0; i < shades.Length; i++)
            {
                ShadeEnemy s = shades[i];
                if (s != null && !s.IsFleeing && Vector3.Distance(transform.position, s.transform.position) <= earshotRadius)
                {
                    s.TriggerDiscordFrenzy(3.5f);
                }
            }

            ExitFluteStance();
        }
    }
}
