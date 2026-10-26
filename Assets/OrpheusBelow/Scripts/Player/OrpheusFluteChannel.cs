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

        private OrpheusPlayerController _player;
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

            _noteDisplayRoot = new GameObject("FluteNoteStave").transform;
            _noteDisplayRoot.SetParent(_billboardRoot, false);

            _noteRenderers = new SpriteRenderer[4];
            _noteTransforms = new Transform[4];

            for (int i = 0; i < 4; i++)
            {
                GameObject noteGo = new GameObject("NoteRune_" + i);
                noteGo.transform.SetParent(_noteDisplayRoot, false);
                noteGo.transform.localPosition = new Vector3((i - 1.5f) * 0.42f, 0f, 0f);
                noteGo.transform.localScale = new Vector3(0.38f, 0.38f, 1f);

                _noteRenderers[i] = RenderPipeline2DSetup.AttachSprite(
                    noteGo, RenderPipeline2DSetup.DiamondSprite, new Color(0.8f, 0.8f, 0.8f, 0.3f), 22);
                _noteTransforms[i] = noteGo.transform;
            }

            _noteDisplayRoot.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay) return;
            if (_billboardRoot != null)
                _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;

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
            StanceEntryCount++;
            GenerateNewMelody();
            if (_noteDisplayRoot != null) _noteDisplayRoot.gameObject.SetActive(true);
            if (_fluteVis != null) _fluteVis.gameObject.SetActive(true);
            OrpheusHud.Instance?.ShowSubtleCallout("Aulos Drawn  [Play Notes — Cataclysmic Requiem on 4th Note!]");
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.05f, 0f), 2.2f, new Color(0.6f, 1.8f, 2.4f, 0.7f), 0.25f);
        }

        public void ExitFluteStance()
        {
            IsFluteActive = false;
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
                _noteRenderers[i].sprite = isBoom ? RenderPipeline2DSetup.RingSprite : RenderPipeline2DSetup.DiamondSprite;
                _noteTransforms[i].localScale = isBoom ? new Vector3(0.48f, 0.48f, 1f) : new Vector3(0.38f, 0.38f, 1f);
            }
        }

        private void UpdateNoteVisuals()
        {
            float pulse = 1.0f + Mathf.Sin(Time.time * 12f) * 0.15f;

            for (int i = 0; i < 4; i++)
            {
                if (i >= _targetSequence.Count) continue;
                bool isBoom = _targetSequence[i] == 'B';

                if (i < _currentNoteIndex)
                {
                    // Completed note
                    _noteRenderers[i].color = new Color(2.6f, 2.2f, 0.5f, 0.95f);
                    _noteTransforms[i].localScale = (isBoom ? new Vector3(0.48f, 0.48f, 1f) : new Vector3(0.38f, 0.38f, 1f));
                }
                else if (i == _currentNoteIndex)
                {
                    // Active target note
                    Color activeCol = isBoom ? new Color(2.8f, 0.35f, 0.25f, 1f) : new Color(2.5f, 2.1f, 0.4f, 1f);
                    _noteRenderers[i].color = activeCol;
                    _noteTransforms[i].localScale = (isBoom ? new Vector3(0.55f, 0.55f, 1f) : new Vector3(0.44f, 0.44f, 1f)) * pulse;
                }
                else
                {
                    // Upcoming note
                    _noteRenderers[i].color = new Color(0.7f, 0.75f, 0.85f, 0.32f);
                    _noteTransforms[i].localScale = (isBoom ? new Vector3(0.44f, 0.44f, 1f) : new Vector3(0.34f, 0.34f, 1f));
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
