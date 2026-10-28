using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    /// <summary>
    /// Orchestrates the Dual-Perspective Chamber-Crawler Loop (No Boons!):
    ///   1) Harder Multi-Wave 2.5D Isometric Shade Brawls
    ///   2) Seamless Shift into 2D Multi-Layer Parallax Rhythm Duel against each Chamber's Guardian Mini-Boss!
    ///   3) Smooth Return to 2.5D Isometric View & Exit Archway Unlock upon Guardian Defeat
    /// </summary>
    public class HadesChamberDirector : MonoBehaviour
    {
        public static HadesChamberDirector Instance { get; private set; }
        public static bool IsDevMenuOpen { get; private set; }

        [System.Serializable]
        public class WaveSpawnSpec
        {
            public ShadeArchetype archetype;
            public Vector2 localOffsetXZ;
        }

        [System.Serializable]
        public class ChamberRuntimeData
        {
            public string chamberTitle;
            public Vector3 centerWorld;
            public Vector3 playerSpawnWorld;
            public float cameraLeashRadius = 9.0f;
            public float cameraOrthoOverride = -1f;
            public ChamberExitDoor exitDoor;
            public CerberusRhythmBoss guardianBoss;
            public Transform interiorObstaclesRoot;
            public GameObject roomRoot;
            public List<WaveSpawnSpec> wave1 = new List<WaveSpawnSpec>();
            public List<WaveSpawnSpec> wave2 = new List<WaveSpawnSpec>();
            public List<WaveSpawnSpec> wave3 = new List<WaveSpawnSpec>();
        }

        public const int PrototypeBuildVersion = 16;
        public int initialChamberIndex;

        public int buildVersion = PrototypeBuildVersion;
        public List<ChamberRuntimeData> chambers = new List<ChamberRuntimeData>();
        public int CurrentChamberIndex { get; private set; }
        public int ActiveEnemyCount => _aliveShades.Count;
        public ShadeEnemy ActiveMiniBoss => _aliveShades.Find(shade => shade != null && shade.IsMiniBoss && !shade.IsPacified && shade.CurrentHealth > 0);
        public float ScreenFadeAlpha { get; private set; }

        private readonly List<ShadeEnemy> _aliveShades = new List<ShadeEnemy>();
        private int _currentWaveIndex;
        private bool _spawningWave;
        private bool _bossPhaseStarted;
        private bool _chamberCleared;
        private bool _transitioning;
        private readonly AsphodelTutorialSession _asphodelTutorial = new AsphodelTutorialSession();
        private AsphodelTutorialStep _shownTutorialStep = AsphodelTutorialStep.Complete;
        private bool _firstTutorialTriggered;
        private bool _shownWardenHint;
        private bool _devChordWasHeld;
        private bool _spokenHadesChamber2;
        private bool _spokenHadesChamber4;
        private bool _spokenHadesChamber7Arrival;
        private bool _spokenHadesAscent5;
        private bool _spokenHadesAscent3;
        private bool _spokenHadesAscent1;
        private int _devSelection;
        private float _devPreviousTimeScale = 1f;
        private Vector2 _devScroll;
        private float _nextEscortWaitHint;

        private void Awake()
        {
            Instance = this;
            IsDevMenuOpen = false;
        }

        private void Start()
        {
            if (chambers.Count > 0)
            {
                EnterChamberImmediate(initialChamberIndex);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool chordHeld = keyboard != null && keyboard.zKey.isPressed && keyboard.xKey.isPressed;
            bool chordPressed = chordHeld && !_devChordWasHeld;
            _devChordWasHeld = chordHeld;

            if (IsDevMenuOpen)
            {
                if (keyboard != null)
                {
                    if (keyboard.escapeKey.wasPressedThisFrame || chordPressed) SetDevMenuOpen(false);
                    else if (keyboard.downArrowKey.wasPressedThisFrame) _devSelection = Mathf.Min(chambers.Count - 1, _devSelection + 1);
                    else if (keyboard.upArrowKey.wasPressedThisFrame) _devSelection = Mathf.Max(0, _devSelection - 1);
                    else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) DevTravelToChamber(_devSelection);
                }
                return;
            }

            if (chordPressed && chambers.Count > 0 && !UnderworldFrontEnd.BlocksGameplay && !OrpheusHud.IsTutorialModalActive)
            {
                _devSelection = Mathf.Clamp(CurrentChamberIndex, 0, chambers.Count - 1);
                _devPreviousTimeScale = Time.timeScale;
                IsDevMenuOpen = true;
                Time.timeScale = 0f;
                return;
            }

            if (UnderworldFrontEnd.BlocksGameplay) return;
            if (OrpheusHud.IsTutorialModalActive) return;

            if (!_asphodelTutorial.IsActive)
            {
                if (CurrentChamberIndex == 0 && !UnderworldJourney.HasEurydice && !UnderworldJourney.Progress.TutorialComplete && !_asphodelTutorial.HasCompleted && !_firstTutorialTriggered)
                {
                    _firstTutorialTriggered = true;
                    ChamberRuntimeData ch = chambers[0];
                    _asphodelTutorial.TryBegin(CaptureTutorialSnapshot(), ch.playerSpawnWorld);
                    _shownTutorialStep = AsphodelTutorialStep.Complete;
                    ShowCurrentTutorialStep();
                    return;
                }
                return;
            }

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory || player.IsIn2DBossStage)
            {
                OrpheusHud.Instance?.HideEurydiceTutorial();
                return;
            }

            bool advanced = _asphodelTutorial.Observe(CaptureTutorialSnapshot(), player.transform.position);
            if (!advanced) return;

            if (_asphodelTutorial.HasCompleted)
            {
                UnderworldJourney.Progress.TutorialComplete = true;
                OrpheusHud.Instance?.HideEurydiceTutorial();
                ChamberRuntimeData chamber = chambers[CurrentChamberIndex];
                StartCoroutine(SpawnWaveWithRunesRoutine(chamber, chamber.wave1, 0.55f));
                return;
            }

            ShowCurrentTutorialStep();
        }

        private void SetDevMenuOpen(bool open)
        {
            if (IsDevMenuOpen == open) return;
            IsDevMenuOpen = open;
            if (!open) Time.timeScale = _devPreviousTimeScale;
        }

        private void DevTravelToChamber(int index)
        {
            if (index < 0 || index >= chambers.Count) return;
            SetDevMenuOpen(false);
            TransitionToChamber(index);
        }

        private void OnGUI()
        {
            if (!IsDevMenuOpen || chambers.Count == 0) return;
            float width = Mathf.Min(620f, Screen.width - 48f);
            float height = Mathf.Min(720f, Screen.height - 48f);
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height), GUI.skin.box);
            GUILayout.Space(8f);
            GUILayout.Label("DEVELOPER CHAMBER SELECT", GUI.skin.label, GUILayout.Height(28f));
            GUILayout.Label($"Current: {CurrentChamberIndex + 1:00} — {ProceduralChamberGenerator.ChamberNames[Mathf.Clamp(CurrentChamberIndex, 0, ProceduralChamberGenerator.ChamberNames.Length - 1)]}");
            GUILayout.Label("Select a chamber and press Enter. Close with Esc or Z + X.");
            GUILayout.Space(8f);
            _devScroll = GUILayout.BeginScrollView(_devScroll, GUILayout.Height(height - 170f));
            for (int i = 0; i < chambers.Count; i++)
            {
                if (i == 0) GUILayout.Label("UPPER LIGHT");
                else if (i == 2) GUILayout.Label("ASPHODEL");
                else if (i == 4) GUILayout.Label("TARTARUS");
                else if (i == 7) GUILayout.Label("THE THRONE");
                string marker = i == CurrentChamberIndex ? "CURRENT  ·  " : i == _devSelection ? "SELECTED  ·  " : string.Empty;
                if (GUILayout.Button($"{marker}{i + 1:00}  —  {chambers[i].chamberTitle}", GUILayout.Height(30f))) _devSelection = i;
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            bool travel = GUILayout.Button($"TRAVEL TO {_devSelection + 1:00}", GUILayout.Height(38f));
            bool close = GUILayout.Button("CLOSE", GUILayout.Height(38f));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            if (travel) DevTravelToChamber(_devSelection);
            else if (close) SetDevMenuOpen(false);
        }

        public string GetCurrentChamberTitle()
        {
            if (CurrentChamberIndex >= 0 && CurrentChamberIndex < chambers.Count)
            {
                return chambers[CurrentChamberIndex].chamberTitle;
            }
            return "Tartarus";
        }

        public void TransitionToChamber(int nextIndex)
        {
            if (_transitioning || nextIndex < 0 || nextIndex >= chambers.Count) return;
            StartCoroutine(TransitionRoutine(nextIndex));
        }

        public void RestoreWardenHint(bool shown) { _shownWardenHint = shown; }

        public bool TryJourneyPassage(ChamberExitDoor door)
        {
            if (_transitioning || door == null || !door.IsUnlocked) return false;
            var map = PaintedChamberMap.Active;
            if (map == null || map.definition.index != CurrentChamberIndex || door.GetComponentInParent<PaintedChamberMap>() != map) return false;
            var progress = UnderworldJourney.Progress;
            if (door.journeyRole == JourneyDoorRole.Reunion)
            {
                if (!progress.TryRescue()) return false;
                map.HideWaitingSoul(); map.ConfigurePassages();
                chambers[CurrentChamberIndex].exitDoor = map.returnDoor;
                map.returnDoor.SetUnlocked(true);
                var companion = OrpheusPrototypeBuilder.AddCompanion(transform, OrpheusPlayerController.Instance.transform);
                UnderworldCamera2D.Instance.eurydiceTarget = companion.transform;
                OrpheusHud.Instance?.ShowBanner("Eurydice has joined you  ·  Begin the ascent", 4f);
                OrpheusHud.Instance?.ShowHadesTutorial(
                    "Take her, minstrel. But hear the law of Tartarus: Walk ahead. She shall follow in your shadow. If you turn your head to look upon her before the sun strikes your brow—she is mine forever.",
                    () =>
                    {
                        OrpheusHud.Instance?.ShowEurydiceTutorial(
                            "Orpheus...? Is that your lyre I hear through the frost? It is truly you... You came into the dark for me. Walk ahead—I will follow your music.",
                            () =>
                            {
                                OrpheusHud.Instance?.ShowArtemisTutorial(
                                    "You have found her, Orpheus. Lead her back through the chambers to my sanctuary. Trust the voice behind you; do not look back. Wait at each passage until she is near.");
                            });
                    });
                return true;
            }
            if (door.journeyRole == JourneyDoorRole.Deeper)
            {
                int next = CurrentChamberIndex + 1;
                if (next >= chambers.Count || !Application.CanStreamedLevelBeLoaded(PaintedChamberScene.ScenePath(next)) || !progress.TryDescend()) return false;
                TransitionToChamber(next); return true;
            }
            if (door.journeyRole == JourneyDoorRole.Surfaceward)
            {
                var companion = Object.FindFirstObjectByType<BlurredEurydice>();
                bool together = companion != null && !companion.IsVanished && Vector3.Distance(companion.transform.position, door.transform.position) <= 6f;
                if (!together)
                {
                    if (Time.unscaledTime > _nextEscortWaitHint)
                    { _nextEscortWaitHint = Time.unscaledTime + 1.5f; OrpheusHud.Instance?.ShowSubtleCallout("Wait for Eurydice at the passage.", 1.5f); }
                    return false;
                }
                int next = CurrentChamberIndex - 1;
                if (next >= 0 && !Application.CanStreamedLevelBeLoaded(PaintedChamberScene.ScenePath(next))) return false;
                if (!progress.TryAscend(together)) return false;
                if (progress.IsComplete) OrpheusPlayerController.Instance.TriggerRunVictory();
                else TransitionToChamber(next);
                return true;
            }
            return false;
        }

        private IEnumerator TransitionRoutine(int nextIndex)
        {
            _transitioning = true;
            while (ScreenFadeAlpha < 1f)
            {
                if (UnderworldFrontEnd.BlocksGameplay) { yield return null; continue; }
                ScreenFadeAlpha = Mathf.MoveTowards(ScreenFadeAlpha, 1f, Time.unscaledDeltaTime * 4.8f);
                yield return null;
            }

            while (UnderworldFrontEnd.BlocksGameplay) yield return null;
            if (PaintedChamberScene.Travel(nextIndex, _shownWardenHint)) yield break;
            EnterChamberImmediate(nextIndex);
            yield return new WaitForSecondsRealtime(0.08f);

            while (ScreenFadeAlpha > 0f)
            {
                if (UnderworldFrontEnd.BlocksGameplay) { yield return null; continue; }
                ScreenFadeAlpha = Mathf.MoveTowards(ScreenFadeAlpha, 0f, Time.unscaledDeltaTime * 3.8f);
                yield return null;
            }
            _transitioning = false;
        }

        private void EnterChamberImmediate(int index)
        {
            UnderworldJourney.Progress.Visit(index);
            CurrentChamberIndex = index;
            _chamberCleared = false;
            _bossPhaseStarted = false;
            _currentWaveIndex = 0;
            _aliveShades.Clear();

            foreach (StygianOrbProjectile orb in Object.FindObjectsByType<StygianOrbProjectile>(FindObjectsSortMode.None))
            {
                Destroy(orb.gameObject);
            }

            ChamberRuntimeData ch = chambers[index];
            for (int i = 0; i < chambers.Count; i++)
                if (chambers[i].roomRoot != null) chambers[i].roomRoot.SetActive(i == index);
            ch.roomRoot?.GetComponent<PaintedChamberMap>()?.MakeActive();
            OrpheusLyreCombat.Instance?.Breath.Refill();
            UnderworldSoundscape.Instance?.PlayPortal();
            if (ch.interiorObstaclesRoot != null) ch.interiorObstaclesRoot.gameObject.SetActive(true);

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player != null)
            {
                player.TeleportToChamberSpawn(ch.playerSpawnWorld);
                player.TriggerChamberEntranceAnimation();
                if (index > 0 || UnderworldJourney.IsAscending)
                {
                    player.Heal(10f);
                    OrpheusHud.Instance?.ShowSubtleCallout("+10 HP Restored  ·  Chamber Respite", 2.2f);
                }
            }

            BlurredEurydice eu = Object.FindFirstObjectByType<BlurredEurydice>();
            if (eu != null)
            {
                eu.SetTemptationZone(false, "");
                eu.SnapBehindOrpheus(ch.playerSpawnWorld - UnderworldCamera2D.IsoUpXZ * 2.2f);
            }

            if (UnderworldCamera2D.Instance != null)
            {
                UnderworldCamera2D.Instance.SetChamberBounds(ch.centerWorld, ch.cameraLeashRadius, ch.cameraOrthoOverride);
                UnderworldCamera2D.Instance.SnapToTargetNow();
            }

            if (ch.exitDoor != null) ch.exitDoor.SetUnlocked(false);

            OrpheusHud.Instance?.ShowBanner(ch.chamberTitle, 2.8f);

            if (index == 0) _firstTutorialTriggered = false;

            if (UnderworldJourney.IsAscending)
            {
                if (index == 5 && !_spokenHadesAscent5)
                {
                    _spokenHadesAscent5 = true;
                    OrpheusHud.Instance?.ShowHadesTutorial("Hear how quietly she walks behind you, Orpheus. Is she truly there? Or do you lead an empty shadow?");
                }
                else if (index == 3 && !_spokenHadesAscent3)
                {
                    _spokenHadesAscent3 = true;
                    OrpheusHud.Instance?.ShowHadesTutorial("A poet's greatest curse is curiosity. How easy it would be to glance over your shoulder... just once.");
                }
                else if (index == 1 && !_spokenHadesAscent1)
                {
                    _spokenHadesAscent1 = true;
                    OrpheusHud.Instance?.ShowHadesTutorial("The light is so close, musician. Can you trust what you cannot see?");
                }
            }
            else
            {
                if (index == 2 && !_spokenHadesChamber2)
                {
                    _spokenHadesChamber2 = true;
                    OrpheusHud.Instance?.ShowHadesTutorial("A breath of living air stumbles down my halls. Turn back, son of Apollo. The earth does not yield what it has swallowed.");
                }
                else if (index == 4 && !_spokenHadesChamber4)
                {
                    _spokenHadesChamber4 = true;
                    OrpheusHud.Instance?.ShowHadesTutorial("Your strings will fray before the weight of my kingdom softens. Mourn above like all the rest.");
                }
                else if (index == 7 && !_spokenHadesChamber7Arrival)
                {
                    _spokenHadesChamber7Arrival = true;
                    OrpheusHud.Instance?.ShowHadesTutorial("You dare stand before the throne of the Unseen? No living feet tread this carpet, Orpheus. Speak. Why should the King of Shadows release what fate has sealed?");
                }
            }

            if (UnderworldJourney.Progress.IsCleared(index))
            {
                HandleChamberCleared(); return;
            }

            if (ch.wave1.Count > 0)
            {
                if (index == 0 && !UnderworldJourney.HasEurydice && !UnderworldJourney.Progress.TutorialComplete && !_asphodelTutorial.HasCompleted)
                {
                    // Delay tutorial start until Update() when UnderworldFrontEnd.BlocksGameplay is false!
                }
                else
                {
                    StartCoroutine(SpawnWaveWithRunesRoutine(ch, ch.wave1, 0.35f));
                }
            }
            else
            {
                BeginChamberBossShowdown(ch);
            }
        }

        private IEnumerator SpawnWaveWithRunesRoutine(ChamberRuntimeData ch, List<WaveSpawnSpec> specs, float delayBeforeRunes)
        {
            _spawningWave = true;
            // No enemy attack dialogue hints
            yield return new WaitForSeconds(delayBeforeRunes);
            OrpheusPlayerController.Instance?.TriggerAlertAnimation();

            var map = PaintedChamberMap.Active;
            if (map != null && map.definition != null && map.definition.index == CurrentChamberIndex)
            {
                var safeSpecs = new List<WaveSpawnSpec>();
                var occupied = new List<Vector3>();
                Vector3 anchor = OrpheusPlayerController.Instance != null ? OrpheusPlayerController.Instance.transform.position : ch.playerSpawnWorld;
                foreach (var spec in specs)
                {
                    Vector3 desired = ch.centerWorld + new Vector3(spec.localOffsetXZ.x, 0, spec.localOffsetXZ.y);
                    if (!PaintedEnemyNavigation.TryFindSpawn(map, anchor, desired, occupied, out var point))
                    {
                        Debug.LogWarning("[Encounter] No safe connected summon slot for " + spec.archetype);
                        continue;
                    }
                    occupied.Add(point);
                    safeSpecs.Add(new WaveSpawnSpec { archetype = spec.archetype,
                        localOffsetXZ = new Vector2(point.x - ch.centerWorld.x, point.z - ch.centerWorld.z) });
                }
                specs = safeSpecs;
            }

            List<GameObject> runes = new List<GameObject>();
            foreach (WaveSpawnSpec s in specs)
            {
                Vector3 pos = ch.centerWorld + new Vector3(s.localOffsetXZ.x, 0.04f, s.localOffsetXZ.y);
                GameObject rune = new GameObject("HadesSummonRune");
                rune.transform.position = pos;
                rune.transform.localScale = new Vector3(1.85f, 1.85f, 1f);
                RenderPipeline2DSetup.AttachGroundDecal(rune, RenderPipeline2DSetup.RingSprite, new Color(2.8f, 0.22f, 0.35f, 0.90f), 6);
                runes.Add(rune);
            }

            ProceduralLyreAudio.Instance?.PlayRhythmBeat(false);
            yield return new WaitForSeconds(0.50f);

            for (int i = 0; i < specs.Count; i++)
            {
                if (runes[i] != null) Destroy(runes[i]);

                WaveSpawnSpec s = specs[i];
                Vector3 spawnPos = ch.centerWorld + new Vector3(s.localOffsetXZ.x, 0f, s.localOffsetXZ.y);
                GroundShockwaveRing.Spawn(spawnPos + new Vector3(0f, 0.05f, 0f), 2.1f, new Color(2.6f, 0.28f, 0.45f, 0.9f), 0.22f);

                GameObject go = new GameObject("Shade_" + s.archetype);
                go.transform.SetParent(transform, true);
                go.transform.position = spawnPos;
                ShadeEnemy shade = go.AddComponent<ShadeEnemy>();
                shade.SetupArchetype(s.archetype);
                shade.SetChamberPressure(CurrentChamberIndex);
                shade.OnShadeSlain += OnShadeKilled;
                _aliveShades.Add(shade);

                if (s.archetype == ShadeArchetype.ArmoredWarden && !_shownWardenHint)
                {
                    _shownWardenHint = true;
                    OrpheusHud.Instance?.ShowArtemisHint(
                        "Bronze grief-armor turns Soft Notes aside. Break it with a Hard Boom.", 4.0f);
                }
            }

            _spawningWave = false;
            if (specs.Count == 0) { AdvanceWaveOrBoss(ch); yield break; }
            if(_currentWaveIndex == 0 && !UnderworldJourney.IsAscending && ActiveMiniBoss != null)
            {
                string intro = ChamberEncounterPlan.Intro(CurrentChamberIndex);
                if(!string.IsNullOrEmpty(intro) && CurrentChamberIndex != 0) OrpheusHud.Instance?.ShowArtemisTutorial(intro);
            }
        }

        private void OnShadeKilled(ShadeEnemy slain)
        {
            _aliveShades.Remove(slain);
            if (_spawningWave || _chamberCleared || _bossPhaseStarted) return;

            if (_aliveShades.Count == 0)
            {
                ChamberRuntimeData ch = chambers[CurrentChamberIndex];
                AdvanceWaveOrBoss(ch);
            }
        }

        private void AdvanceWaveOrBoss(ChamberRuntimeData chamber)
        {
            var next = _currentWaveIndex == 0 ? chamber.wave2 : _currentWaveIndex == 1 ? chamber.wave3 : null;
            if (next != null && next.Count > 0)
            {
                _currentWaveIndex++;
                OrpheusHud.Instance?.ShowSubtleCallout(_currentWaveIndex == 1 ? "Second Wave" : "Final Wave");
                StartCoroutine(SpawnWaveWithRunesRoutine(chamber, next, .55f));
            }
            else BeginChamberBossShowdown(chamber);
        }

        private void BeginChamberBossShowdown(ChamberRuntimeData ch)
        {
            if (_bossPhaseStarted) return;
            _bossPhaseStarted = true;
            OrpheusHud.Instance?.HideEurydiceTutorial();

            foreach (StygianOrbProjectile orb in Object.FindObjectsByType<StygianOrbProjectile>(FindObjectsSortMode.None))
            {
                Destroy(orb.gameObject);
            }

            if (ch.guardianBoss != null)
            {
                PaintedEnemyProjectile.ClearAll();
                // Retract interior 3D obstacles so the 2D Side-View Rhythm Stage is 100% clean & unobstructed!
                if (ch.interiorObstaclesRoot != null) ch.interiorObstaclesRoot.gameObject.SetActive(false);

                ch.guardianBoss.OnCerberusDefeated += () =>
                {
                    if (ch.interiorObstaclesRoot != null) ch.interiorObstaclesRoot.gameObject.SetActive(true);
                    HandleChamberCleared();
                };
                ch.guardianBoss.ActivateGuardianDuel();
                string intro = ChamberEncounterPlan.Intro(CurrentChamberIndex);
                if(!string.IsNullOrEmpty(intro)) OrpheusHud.Instance?.ShowArtemisTutorial(intro);
            }
            else
            {
                HandleChamberCleared();
            }
        }

        private static AsphodelTutorialSnapshot CaptureTutorialSnapshot()
        {
            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            OrpheusLyreCombat combat = OrpheusLyreCombat.Instance;
            OrpheusFluteChannel flute = OrpheusFluteChannel.Instance;
            return new AsphodelTutorialSnapshot(
                player != null ? player.DashUseCount : 0,
                combat != null ? combat.SoftNoteCount : 0,
                combat != null ? combat.HardBoomCount : 0,
                flute != null ? flute.StanceEntryCount : 0,
                combat != null ? combat.CadenceCount : 0);
        }

        private void ShowCurrentTutorialStep()
        {
            AsphodelTutorialStep step = _asphodelTutorial.CurrentStep;
            if (step == _shownTutorialStep) return;
            _shownTutorialStep = step;

            bool usingMic = (SensorPulseInput.Instance != null && SensorPulseInput.Instance.sourceMode == SensorSourceMode.KeyboardPlusPCMicrophone)
                         || SensorPulseInput.GlobalSourceMode == SensorSourceMode.KeyboardPlusPCMicrophone;

            string text;
            switch (step)
            {
                case AsphodelTutorialStep.Move:
                    text = "Orpheus, I am Artemis. Clear chambers to move further into the underworld! Move with W, A, S, D or the left stick.";
                    break;
                case AsphodelTutorialStep.SoftNote:
                    text = usingMic
                        ? "To defend yourself with a Quaver [♪], press J or Left Click (or make a gentle sound if using the mic) to strike shades and hold the tempo."
                        : "To defend yourself with a Quaver [♪], press J or Left Click to strike shades and hold the tempo.";
                    break;
                case AsphodelTutorialStep.HardBoom:
                    text = usingMic
                        ? "To cause a big repelling move, press K or Right Click (or make a loud sound if using the mic) to unleash a Boom Fermata—blasting enemies away and shattering armor!"
                        : "To cause a big repelling move, press K or Right Click to unleash a Boom Fermata—blasting enemies away and shattering armor!";
                    break;
                case AsphodelTutorialStep.Flute:
                    text = "Combos can force enemies to flee! Press F to draw your Aulos and see the combo needed for the Cataclysmic Requiem.";
                    break;
                case AsphodelTutorialStep.Dash:
                    text = "Press F to holster your Aulos when ready. Space or Shift grants two invulnerable dashes to reposition swiftly.";
                    break;
                case AsphodelTutorialStep.Cadence:
                    text = "Pluck cadences like [ ♪ — ♪ — Fermata ] to restore breath and crack grief-armor. Clear the chamber to unseal the exit!";
                    break;
                default:
                    OrpheusHud.Instance?.HideEurydiceTutorial();
                    return;
            }

            OrpheusHud.Instance?.ShowArtemisTutorial(text);
        }

        private void HandleChamberCleared()
        {
            if (_chamberCleared) return;
            _chamberCleared = true;
            UnderworldJourney.Progress.ClearCurrentChamber();
            Object.FindFirstObjectByType<BlurredEurydice>()?.Celebrate(1.8f);
            OrpheusPlayerController.Instance?.TriggerChamberClear();

            ChamberRuntimeData ch = chambers[CurrentChamberIndex];
            ProceduralLyreAudio.Instance?.PlayCadenceComplete();

            if (ch.exitDoor != null)
            {
                ch.exitDoor.SetUnlocked(true);
                GroundShockwaveRing.Spawn(ch.exitDoor.transform.position + new Vector3(0f, 0.06f, 0f), 4.2f, new Color(0.5f, 2.6f, 1.5f, 1f), 0.45f);
            }

            // Deterministic Mythic Cadence Progression (Zero RNG Stat Boons!)
            OrpheusLyreCombat combat = OrpheusLyreCombat.Instance;
            string clearBanner;

            if (UnderworldJourney.IsAscending)
            {
                clearBanner = CurrentChamberIndex == 0 ? "The sanctuary is open  —  Bring Eurydice into the light" : "Ascent passage unlocked  —  Stay together";
            }
            else if (CurrentChamberIndex == UnderworldJourney.DeepestChamber)
            {
                clearBanner = "The throne is reached  —  Approach Eurydice";
            }
            else if (ch.exitDoor != null && ch.exitDoor.isFinalSunlightExit)
            {
                clearBanner = "Cerberus Charmed into Slumber  —  Ascend into the Sun";
            }
            else if (CurrentChamberIndex == 0 && combat != null && combat.UnlockedCadenceTier < 2)
            {
                combat.UnlockedCadenceTier = 2;
                combat.RefreshCadenceDisplay();
                clearBanner = "Guardian Pacified  —  Learned Cadence: Sanctuary of Eurydice (Fermata — Quaver)";
                OrpheusHud.Instance?.ShowCadenceUnlockCeremony(
                    "Sanctuary of Eurydice",
                    "Fermata — Quaver",
                    "Keep this cadence for your return. Its golden aegis will protect Eurydice from abductors.");
            }
            else if (CurrentChamberIndex == 1 && combat != null && combat.UnlockedCadenceTier < 3)
            {
                combat.UnlockedCadenceTier = 3;
                combat.RefreshCadenceDisplay();
                clearBanner = "Guardian Pacified  —  Learned Cadence: Siren's Convergence (Quaver — Fermata — Quaver)";
                OrpheusHud.Instance?.ShowCadenceUnlockCeremony(
                    "Siren's Convergence",
                    "Quaver — Fermata — Quaver",
                    "Conjure an inward-pulling harmonic vortex that cracks Grief-Armor and draws all Shades together.");
            }
            else
            {
                clearBanner = "Guardian Lulled to Slumber  —  Passage Unlocked";
            }

            OrpheusHud.Instance?.ShowBanner(clearBanner, 3.8f);
        }
    }

    public static class OrpheusPrototypeBuilder
    {
        public static BlurredEurydice AddCompanion(Transform parent, Transform player)
        {
            var existing = Object.FindFirstObjectByType<BlurredEurydice>();
            if (existing != null) return existing;
            var go = new GameObject("Eurydice_BlurredCompanion25D"); go.transform.SetParent(parent, false);
            go.transform.position = player.position - UnderworldCamera2D.IsoUpXZ * 2.2f;
            var companion = go.AddComponent<BlurredEurydice>(); companion.orpheus = player;
            return companion;
        }

        public static void RestartDescent(bool skipTitle)
        {
            UnderworldJourney.Reset();
            if (PaintedChamberScene.Restart(skipTitle)) return;
            UnderworldFrontEnd.SkipTitleOnce = skipTitle;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(scene.path) && scene.buildIndex >= 0)
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
            else
            {
                // An unsaved editor scene has no build index. Rebuild its generated world in place.
                foreach (StygianOrbProjectile orb in Object.FindObjectsByType<StygianOrbProjectile>(FindObjectsSortMode.None))
                    Object.Destroy(orb.gameObject);
                foreach (StageSweepWave2D wave in Object.FindObjectsByType<StageSweepWave2D>(FindObjectsSortMode.None))
                    Object.Destroy(wave.gameObject);
                foreach (StageGeyserHazard2D geyser in Object.FindObjectsByType<StageGeyserHazard2D>(FindObjectsSortMode.None))
                    Object.Destroy(geyser.gameObject);
                BuildPrototypeWorld();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapIfNeeded()
        {
            // Auto-upgrade if empty scene OR if an older prototype build version is open in the Editor!
            HadesChamberDirector existing = Object.FindFirstObjectByType<HadesChamberDirector>();
            if (existing != null && existing.buildVersion >= HadesChamberDirector.PrototypeBuildVersion && UnderworldFrontEnd.Instance != null)
            {
                return;
            }
            BuildPrototypeWorld();
        }

        public static GameObject BuildPrototypeWorld()
        {
            GameObject oldRoot = GameObject.Find("OrpheusBelow_PrototypeWorld");
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot);
            }

            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                l.enabled = false;
            }

            GameObject root = new GameObject("OrpheusBelow_PrototypeWorld");

            // --- 1. CORE SYSTEMS ---
            GameObject sysGo = new GameObject("Systems_Input_Audio_HUD");
            sysGo.transform.SetParent(root.transform, false);
            sysGo.AddComponent<SensorPulseInput>();
            sysGo.AddComponent<ProceduralLyreAudio>();
            sysGo.AddComponent<UnderworldSoundscape>();
            sysGo.AddComponent<OrpheusHud>();
            sysGo.AddComponent<UnderworldFrontEnd>();

            HadesChamberDirector director = root.AddComponent<HadesChamberDirector>();
            var authoredScene = Object.FindFirstObjectByType<PaintedChamberScene>();
            if (authoredScene != null) director.initialChamberIndex = authoredScene.chamberIndex;
            UnderworldJourney.Progress.Visit(director.initialChamberIndex);

            // --- 2. ORPHEUS & BLURRED EURYDICE ---
            GameObject orpheusGo = new GameObject("Orpheus_Player25D");
            orpheusGo.transform.SetParent(root.transform, false);
            orpheusGo.transform.position = new Vector3(-6.2f, 0f, -6.2f);
            OrpheusPlayerController player = orpheusGo.AddComponent<OrpheusPlayerController>();
            orpheusGo.AddComponent<OrpheusLyreCombat>();
            orpheusGo.AddComponent<OrpheusFluteChannel>();

            BlurredEurydice eurydice = UnderworldJourney.HasEurydice ? AddCompanion(root.transform, player.transform) : null;

            // --- 3. DUAL-PERSPECTIVE CAMERA (2.5D Isometric <-> 2D Parallax Boss Stage) ---
            Camera mainCam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (mainCam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                mainCam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            UnderworldCamera2D camIso = mainCam.GetComponent<UnderworldCamera2D>();
            if (camIso == null) camIso = mainCam.gameObject.AddComponent<UnderworldCamera2D>();
            camIso.target = player.transform;
            camIso.eurydiceTarget = eurydice != null ? eurydice.transform : null;

            Transform chambersRoot = new GameObject("Chambers_25D_And_Parallax2D").transform;
            chambersRoot.SetParent(root.transform, false);

            const int totalChambers = ProceduralChamberGenerator.TotalChambers;
            for (int i = 0; i < totalChambers; i++)
            {
                if (authoredScene != null)
                {
                    director.chambers.Add(i == authoredScene.chamberIndex ? authoredScene.map.RuntimeData()
                        : new HadesChamberDirector.ChamberRuntimeData { chamberTitle = UnderworldJourney.ChamberNames[i] });
                    continue;
                }
                Vector3 center = new Vector3(i * 90f, 0f, i * 90f);
                var chamberData = ProceduralChamberGenerator.GenerateChamber(chambersRoot, i, totalChambers, center);
                director.chambers.Add(chamberData);
            }

            PaintedChamberScene.RestoreTravel(director);

            return root;
        }

        private static Transform CreateObstaclesContainer(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void CreateParallaxRig(Transform parent, string name, Vector3 stageCenter, Color glow, Color arch, Color fire)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            UnderworldParallaxRig rig = go.AddComponent<UnderworldParallaxRig>();
            rig.Initialize(stageCenter, glow, arch, fire);
        }

        private static CerberusRhythmBoss CreateGuardian(Transform parent, string name, BossIdentity id, Vector3 stageCenter)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            CerberusRhythmBoss b = go.AddComponent<CerberusRhythmBoss>();
            b.ConfigureBoss(id, stageCenter);
            return b;
        }

        private static ChamberExitDoor BuildDiamondChamberShell(
            Transform parent, string name, Vector3 center, float sizeX, float sizeZ, int targetChamber, bool finalExit)
        {
            Transform room = new GameObject(name).transform;
            room.SetParent(parent, false);
            room.position = center;

            Color floorTop   = new Color(0.13f, 0.09f, 0.19f, 1f);
            Color floorLeft  = new Color(0.08f, 0.05f, 0.12f, 1f);
            Color floorRight = new Color(0.05f, 0.03f, 0.08f, 1f);

            RenderPipeline2DSetup.CreateIsoBlock(room, "MainFloorSlab",
                new Vector3(0f, -0.75f, 0f), new Vector3(sizeX, 1.5f, sizeZ),
                floorTop, floorLeft, floorRight, addSolidWallCollider: false);

            UnderworldArtLibrary.CreatePaintedFloor(room, sizeX, sizeZ, finalExit);

            Color wallTop   = new Color(0.26f, 0.16f, 0.33f, 1f);
            Color wallLeft  = new Color(0.16f, 0.09f, 0.21f, 1f);
            Color wallRight = new Color(0.10f, 0.05f, 0.14f, 1f);

            float hx = sizeX * 0.5f;
            float hz = sizeZ * 0.5f;

            RenderPipeline2DSetup.CreateIsoBlock(room, "Wall_NorthWest_PlusZ",
                new Vector3(-1.8f, 1.4f, hz + 0.6f), new Vector3(sizeX - 3.6f, 2.8f, 1.2f),
                wallTop, wallLeft, wallRight, addSolidWallCollider: true);

            RenderPipeline2DSetup.CreateIsoBlock(room, "Wall_NorthEast_PlusX",
                new Vector3(hx + 0.6f, 1.4f, -1.8f), new Vector3(1.2f, 2.8f, sizeZ - 3.6f),
                wallTop, wallLeft, wallRight, addSolidWallCollider: true);

            // Low 0.28m front trim so even at 4.5° Side-2D pitch the camera has an unobstructed sightline to Orpheus's feet!
            CreateInvisibleRimWall(room, "Wall_SouthWest_MinusZ", new Vector3(0f, 0.14f, -hz - 0.5f), new Vector3(sizeX + 1.2f, 0.28f, 1.0f), wallTop, wallLeft, wallRight);
            CreateInvisibleRimWall(room, "Wall_SouthEast_MinusX", new Vector3(-hx - 0.5f, 0.14f, 0f), new Vector3(1.0f, 0.28f, sizeZ + 1.2f), wallTop, wallLeft, wallRight);

            Vector3 doorPos = new Vector3(hx - 1.1f, 0f, hz - 1.1f);
            RenderPipeline2DSetup.CreateIsoBlock(room, "ExitGatePillarL",
                doorPos + new Vector3(-1.8f, 1.8f, 0.6f), new Vector3(1.1f, 3.6f, 1.1f),
                new Color(0.45f, 0.32f, 0.20f, 1f), wallLeft, wallRight, addSolidWallCollider: true);
            RenderPipeline2DSetup.CreateIsoBlock(room, "ExitGatePillarR",
                doorPos + new Vector3(0.6f, 1.8f, -1.8f), new Vector3(1.1f, 3.6f, 1.1f),
                new Color(0.45f, 0.32f, 0.20f, 1f), wallLeft, wallRight, addSolidWallCollider: true);
            RenderPipeline2DSetup.CreateIsoBlock(room, "ExitBackstop",
                doorPos + new Vector3(1.5f, 1.5f, 1.5f), new Vector3(4.5f, 3.0f, 4.5f),
                wallTop, wallLeft, wallRight, addSolidWallCollider: true);

            GameObject doorGo = new GameObject("Hades_ChamberExitDoor");
            doorGo.transform.SetParent(room, false);
            doorGo.transform.localPosition = doorPos;

            BoxCollider trig = doorGo.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.size = new Vector3(3.2f, 2.5f, 3.2f);
            trig.center = new Vector3(-0.4f, 1.0f, -0.4f);

            GameObject portalVis = new GameObject("ArchwayPortalGlow");
            portalVis.transform.SetParent(doorGo.transform, false);
            portalVis.transform.localPosition = new Vector3(-0.3f, 1.45f, -0.3f);
            portalVis.transform.localScale = new Vector3(2.2f, 2.9f, 1f);
            SpriteRenderer portalSr = RenderPipeline2DSetup.AttachBillboardSprite(portalVis, RenderPipeline2DSetup.SoftGlowSprite, Color.red, 10);
            portalVis.AddComponent<PortalLocationParticles>().SetAccent(new Color(0.9f, 0.58f, 0.30f));

            GameObject floorRing = new GameObject("DoorThresholdRune");
            floorRing.transform.SetParent(doorGo.transform, false);
            floorRing.transform.localPosition = new Vector3(-0.8f, 0.03f, -0.8f);
            floorRing.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
            SpriteRenderer beaconSr = RenderPipeline2DSetup.AttachGroundDecal(floorRing, RenderPipeline2DSetup.RingSprite, Color.red, 4);

            ChamberExitDoor exitComp = doorGo.AddComponent<ChamberExitDoor>();
            exitComp.targetChamberIndex = targetChamber;
            exitComp.isFinalSunlightExit = finalExit;
            exitComp.SetupVisuals(portalSr, beaconSr);

            CreateHadesBrazier(room, new Vector3(-hx + 1.8f, 0f,  hz - 1.8f));
            CreateHadesBrazier(room, new Vector3( hx - 1.8f, 0f, -hz + 1.8f));

            return exitComp;
        }

        private static void CreateInvisibleRimWall(Transform parent, string name, Vector3 pos, Vector3 visualSize, Color top, Color left, Color right)
        {
            GameObject curb = RenderPipeline2DSetup.CreateIsoBlock(parent, name, pos, visualSize, top, left, right, addSolidWallCollider: false);
            BoxCollider col = curb.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 5.0f, 1f);
            col.center = new Vector3(0f, 2.0f, 0f);
            curb.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;
        }

        private static void CreateIsoPillar(Transform parent, Vector3 worldPos)
        {
            RenderPipeline2DSetup.CreateIsoBlock(parent, "TartarusStonePillar",
                worldPos + new Vector3(0f, 1.55f, 0f), new Vector3(1.45f, 3.1f, 1.45f),
                new Color(0.34f, 0.24f, 0.42f, 1f),
                new Color(0.20f, 0.12f, 0.28f, 1f),
                new Color(0.11f, 0.06f, 0.16f, 1f),
                addSolidWallCollider: true);
        }

        private static void CreateCrackedObelisk(Transform parent, Vector3 worldPos)
        {
            GameObject ob = RenderPipeline2DSetup.CreateIsoBlock(parent, "CrackedStyxObelisk",
                worldPos + new Vector3(0f, 1.05f, 0f), new Vector3(1.35f, 2.1f, 1.35f),
                new Color(0.62f, 0.30f, 0.44f, 1f),
                new Color(0.42f, 0.18f, 0.30f, 1f),
                new Color(0.26f, 0.10f, 0.18f, 1f),
                addSolidWallCollider: true);
            ob.AddComponent<CrackedStyxBarrier>();

            GameObject fault = new GameObject("BoomCrackGlow");
            fault.transform.SetParent(ob.transform, false);
            fault.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            fault.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(fault, RenderPipeline2DSetup.RingSprite, new Color(2.5f, 0.5f, 0.25f, 0.8f), 3);
        }

        private static void CreateDashableStyxChasm(Transform parent, Vector3 worldPos, Vector3 size)
        {
            GameObject chasm = new GameObject("DashableStyxChasm");
            chasm.transform.SetParent(parent, false);
            chasm.transform.position = worldPos;

            GameObject darkPit = new GameObject("AbyssPit");
            darkPit.transform.SetParent(chasm.transform, false);
            darkPit.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            darkPit.transform.localScale = new Vector3(size.x, size.z, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(darkPit, RenderPipeline2DSetup.SquareSprite, new Color(0.02f, 0.05f, 0.06f, 0.98f), 1);



            BoxCollider col = chasm.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, 3.0f, size.z);
            col.center = new Vector3(0f, 1.0f, 0f);
            chasm.AddComponent<IsoObstacle>().kind = IsoObstacleKind.DashableChasm;
        }

        private static void CreateHadesBrazier(Transform parent, Vector3 localPos)
        {
            UnderworldArtLibrary.CreateBeaconTower(parent, localPos);
        }

        private static void CreateTemptationMistZone(Transform parent, Vector3 worldPos, Vector3 size, string whisper)
        {
            GameObject go = new GameObject("Eurydice_TemptationMistZone");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = size;
            go.AddComponent<TemptationWhisperZone>().whisperMessage = whisper;
        }
    }
}
