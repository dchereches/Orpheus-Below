using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            public List<WaveSpawnSpec> wave1 = new List<WaveSpawnSpec>();
            public List<WaveSpawnSpec> wave2 = new List<WaveSpawnSpec>();
        }

        public const int PrototypeBuildVersion = 6;

        public int buildVersion = PrototypeBuildVersion;
        public List<ChamberRuntimeData> chambers = new List<ChamberRuntimeData>();
        public int CurrentChamberIndex { get; private set; }
        public int ActiveEnemyCount => _aliveShades.Count;
        public float ScreenFadeAlpha { get; private set; }

        private readonly List<ShadeEnemy> _aliveShades = new List<ShadeEnemy>();
        private int _currentWaveIndex;
        private bool _spawningWave;
        private bool _bossPhaseStarted;
        private bool _chamberCleared;
        private bool _transitioning;
        private readonly AsphodelTutorialSession _asphodelTutorial = new AsphodelTutorialSession();
        private AsphodelTutorialStep _shownTutorialStep = AsphodelTutorialStep.Complete;
        private bool _shownWardenHint;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (chambers.Count > 0)
            {
                EnterChamberImmediate(0);
            }
        }

        private void Update()
        {
            if (!_asphodelTutorial.IsActive) return;

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
                OrpheusHud.Instance?.HideEurydiceTutorial();
                ChamberRuntimeData chamber = chambers[CurrentChamberIndex];
                StartCoroutine(SpawnWaveWithRunesRoutine(chamber, chamber.wave1, 0.55f));
                return;
            }

            ShowCurrentTutorialStep();
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

        private IEnumerator TransitionRoutine(int nextIndex)
        {
            _transitioning = true;
            while (ScreenFadeAlpha < 1f)
            {
                ScreenFadeAlpha = Mathf.MoveTowards(ScreenFadeAlpha, 1f, Time.unscaledDeltaTime * 4.8f);
                yield return null;
            }

            EnterChamberImmediate(nextIndex);
            yield return new WaitForSecondsRealtime(0.08f);

            while (ScreenFadeAlpha > 0f)
            {
                ScreenFadeAlpha = Mathf.MoveTowards(ScreenFadeAlpha, 0f, Time.unscaledDeltaTime * 3.8f);
                yield return null;
            }
            _transitioning = false;
        }

        private void EnterChamberImmediate(int index)
        {
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
            if (ch.interiorObstaclesRoot != null) ch.interiorObstaclesRoot.gameObject.SetActive(true);

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player != null)
            {
                player.TeleportToChamberSpawn(ch.playerSpawnWorld);
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

            if (ch.wave1.Count > 0)
            {
                if (index == 0 && !_asphodelTutorial.HasCompleted)
                {
                    _asphodelTutorial.TryBegin(CaptureTutorialSnapshot(), ch.playerSpawnWorld);
                    _shownTutorialStep = AsphodelTutorialStep.Complete;
                    ShowCurrentTutorialStep();
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
            yield return new WaitForSeconds(delayBeforeRunes);

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
                shade.OnShadeSlain += OnShadeKilled;
                _aliveShades.Add(shade);

                if (s.archetype == ShadeArchetype.ArmoredWarden && !_shownWardenHint)
                {
                    _shownWardenHint = true;
                    OrpheusHud.Instance?.ShowEurydiceHint(
                        "Bronze grief-armor turns Soft Notes aside. Break it with a Hard Boom.", 4.0f);
                }
            }

            _spawningWave = false;
        }

        private void OnShadeKilled(ShadeEnemy slain)
        {
            _aliveShades.Remove(slain);
            if (_spawningWave || _chamberCleared || _bossPhaseStarted) return;

            if (_aliveShades.Count == 0)
            {
                ChamberRuntimeData ch = chambers[CurrentChamberIndex];
                if (_currentWaveIndex == 0 && ch.wave2 != null && ch.wave2.Count > 0)
                {
                    _currentWaveIndex = 1;
                    OrpheusHud.Instance?.ShowSubtleCallout("Second Wave");
                    StartCoroutine(SpawnWaveWithRunesRoutine(ch, ch.wave2, 0.28f));
                }
                else
                {
                    BeginChamberBossShowdown(ch);
                }
            }
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
                // Retract interior 3D obstacles so the 2D Side-View Rhythm Stage is 100% clean & unobstructed!
                if (ch.interiorObstaclesRoot != null) ch.interiorObstaclesRoot.gameObject.SetActive(false);

                ch.guardianBoss.OnCerberusDefeated += () =>
                {
                    if (ch.interiorObstaclesRoot != null) ch.interiorObstaclesRoot.gameObject.SetActive(true);
                    HandleChamberCleared();
                };
                ch.guardianBoss.ActivateGuardianDuel();
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
                flute != null ? flute.StanceEntryCount : 0);
        }

        private void ShowCurrentTutorialStep()
        {
            AsphodelTutorialStep step = _asphodelTutorial.CurrentStep;
            if (step == _shownTutorialStep) return;
            _shownTutorialStep = step;

            string text;
            switch (step)
            {
                case AsphodelTutorialStep.Move:
                    text = "Stay ahead of me. Move with W A S D or the left stick.";
                    break;
                case AsphodelTutorialStep.Dash:
                    text = "Space or Shift gives two invulnerable dashes. Cross danger cleanly.";
                    break;
                case AsphodelTutorialStep.SoftNote:
                    text = "Tap LMB or J — or play below 60% — for a Soft Note.";
                    break;
                case AsphodelTutorialStep.HardBoom:
                    text = "RMB, K, or a strong pulse makes a Hard Boom. It breaks bronze grief-armor.";
                    break;
                case AsphodelTutorialStep.Flute:
                    text = "Press F or the left trigger for the Aulos. Weaken Shades, then follow its four-note phrase.";
                    break;
                default:
                    OrpheusHud.Instance?.HideEurydiceTutorial();
                    return;
            }

            OrpheusHud.Instance?.ShowEurydiceTutorial(text);
        }

        private void HandleChamberCleared()
        {
            if (_chamberCleared) return;
            _chamberCleared = true;

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

            if (ch.exitDoor != null && ch.exitDoor.isFinalSunlightExit)
            {
                clearBanner = "Cerberus Charmed into Slumber  —  Ascend into the Sun";
            }
            else if (CurrentChamberIndex == 0 && combat != null && combat.UnlockedCadenceTier < 2)
            {
                combat.UnlockedCadenceTier = 2;
                combat.RefreshCadenceDisplay();
                clearBanner = "Guardian Pacified  —  Learned Cadence: Sanctuary of Eurydice (B - S)";
                OrpheusHud.Instance?.ShowCadenceUnlockCeremony(
                    "Sanctuary of Eurydice",
                    "[ B ]  —  [ S ]",
                    "Surround Eurydice in a Golden Aegis, repelling abductors and hearing her reassuring hum.");
            }
            else if (CurrentChamberIndex == 1 && combat != null && combat.UnlockedCadenceTier < 3)
            {
                combat.UnlockedCadenceTier = 3;
                combat.RefreshCadenceDisplay();
                clearBanner = "Guardian Pacified  —  Learned Cadence: Siren's Convergence (S - B - S)";
                OrpheusHud.Instance?.ShowCadenceUnlockCeremony(
                    "Siren's Convergence",
                    "[ S ]  —  [ B ]  —  [ S ]",
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapIfNeeded()
        {
            // Auto-upgrade if empty scene OR if an older prototype build version is open in the Editor!
            HadesChamberDirector existing = Object.FindFirstObjectByType<HadesChamberDirector>();
            if (existing != null && existing.buildVersion >= HadesChamberDirector.PrototypeBuildVersion)
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
            sysGo.AddComponent<OrpheusHud>();

            HadesChamberDirector director = root.AddComponent<HadesChamberDirector>();

            // --- 2. ORPHEUS & BLURRED EURYDICE ---
            GameObject orpheusGo = new GameObject("Orpheus_Player25D");
            orpheusGo.transform.SetParent(root.transform, false);
            orpheusGo.transform.position = new Vector3(-6.2f, 0f, -6.2f);
            OrpheusPlayerController player = orpheusGo.AddComponent<OrpheusPlayerController>();
            orpheusGo.AddComponent<OrpheusLyreCombat>();
            orpheusGo.AddComponent<OrpheusFluteChannel>();

            GameObject eurydiceGo = new GameObject("Eurydice_BlurredCompanion25D");
            eurydiceGo.transform.SetParent(root.transform, false);
            eurydiceGo.transform.position = new Vector3(-8.0f, 0f, -8.0f);
            BlurredEurydice eurydice = eurydiceGo.AddComponent<BlurredEurydice>();
            eurydice.orpheus = orpheusGo.transform;

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
            camIso.eurydiceTarget = eurydice.transform;

            Transform chambersRoot = new GameObject("Chambers_25D_And_Parallax2D").transform;
            chambersRoot.SetParent(root.transform, false);

            const int totalChambers = 8;
            for (int i = 0; i < totalChambers; i++)
            {
                Vector3 center = new Vector3(i * 90f, 0f, i * 90f);
                var chamberData = ProceduralChamberGenerator.GenerateChamber(chambersRoot, i, totalChambers, center);
                director.chambers.Add(chamberData);
            }

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

            Color tileAccent = new Color(0.20f, 0.14f, 0.29f, 0.50f);
            int gx = Mathf.FloorToInt(sizeX / 4f);
            int gz = Mathf.FloorToInt(sizeZ / 4f);
            for (int ix = -gx / 2; ix <= gx / 2; ix++)
            {
                for (int iz = -gz / 2; iz <= gz / 2; iz++)
                {
                    if ((ix + iz) % 2 != 0) continue;
                    GameObject tile = new GameObject("FloorMosaicTile");
                    tile.transform.SetParent(room, false);
                    tile.transform.localPosition = new Vector3(ix * 3.4f, 0.008f, iz * 3.4f);
                    tile.transform.localScale = new Vector3(3.15f, 3.15f, 1f);
                    RenderPipeline2DSetup.AttachGroundDecal(tile, RenderPipeline2DSetup.SquareSprite, tileAccent, 0);
                }
            }

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

            GameObject riverGlow = new GameObject("StyxLavaVein");
            riverGlow.transform.SetParent(chasm.transform, false);
            riverGlow.transform.localPosition = new Vector3(0f, 0.022f, 0f);
            riverGlow.transform.localScale = new Vector3(size.x * 0.78f, size.z * 0.78f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(riverGlow, RenderPipeline2DSetup.SquareSprite, new Color(0.22f, 1.95f, 1.25f, 0.85f), 2);

            BoxCollider col = chasm.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, 3.0f, size.z);
            col.center = new Vector3(0f, 1.0f, 0f);
            chasm.AddComponent<IsoObstacle>().kind = IsoObstacleKind.DashableChasm;
        }

        private static void CreateHadesBrazier(Transform parent, Vector3 localPos)
        {
            GameObject br = RenderPipeline2DSetup.CreateIsoBlock(parent, "StygianBrazier",
                localPos + new Vector3(0f, 0.55f, 0f), new Vector3(0.75f, 1.1f, 0.75f),
                new Color(0.45f, 0.34f, 0.18f, 1f),
                new Color(0.25f, 0.18f, 0.10f, 1f),
                new Color(0.15f, 0.10f, 0.06f, 1f),
                addSolidWallCollider: true);

            GameObject flame = new GameObject("BrazierFlame");
            flame.transform.SetParent(br.transform, false);
            flame.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            flame.transform.localScale = new Vector3(1.4f, 1.8f, 1f);
            RenderPipeline2DSetup.AttachBillboardSprite(flame, RenderPipeline2DSetup.SoftGlowSprite, new Color(2.8f, 0.85f, 0.25f, 0.90f), 12);
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
