using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// Orchestrates the 2.5D Isometric HADES Chamber-Crawler loop:
    ///   - Tracks active enemies per chamber, spawns multi-wave reinforcements via glowing Hades Summon Runes
    ///   - Drops Olympian Boon rewards & unlocks the NE Isometric Exit Archway on "CHAMBER CLEAR!"
    ///   - Smoothly fades & transitions Orpheus + Blurred Eurydice + Camera into the next diamond chamber
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
            public float cameraLeashRadius = 9.5f;
            public float cameraOrthoOverride = -1f;
            public ChamberExitDoor exitDoor;
            public bool dropApolloBoonOnClear;
            public CerberusRhythmBoss boss;
            public List<WaveSpawnSpec> wave1 = new List<WaveSpawnSpec>();
            public List<WaveSpawnSpec> wave2 = new List<WaveSpawnSpec>();
        }

        public List<ChamberRuntimeData> chambers = new List<ChamberRuntimeData>();
        public int CurrentChamberIndex { get; private set; }
        public int ActiveEnemyCount => _aliveShades.Count;
        public float ScreenFadeAlpha { get; private set; }

        private readonly List<ShadeEnemy> _aliveShades = new List<ShadeEnemy>();
        private int _currentWaveIndex;
        private bool _spawningWave;
        private bool _chamberCleared;
        private bool _transitioning;

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

        public string GetCurrentChamberTitle()
        {
            if (CurrentChamberIndex >= 0 && CurrentChamberIndex < chambers.Count)
            {
                return chambers[CurrentChamberIndex].chamberTitle;
            }
            return "TARTARUS";
        }

        public void TransitionToChamber(int nextIndex)
        {
            if (_transitioning || nextIndex < 0 || nextIndex >= chambers.Count) return;
            StartCoroutine(TransitionRoutine(nextIndex));
        }

        private IEnumerator TransitionRoutine(int nextIndex)
        {
            _transitioning = true;
            // Crisp Hades 0.22s dark wipe
            while (ScreenFadeAlpha < 1f)
            {
                ScreenFadeAlpha = Mathf.MoveTowards(ScreenFadeAlpha, 1f, Time.unscaledDeltaTime * 5.0f);
                yield return null;
            }

            EnterChamberImmediate(nextIndex);
            yield return new WaitForSecondsRealtime(0.08f);

            while (ScreenFadeAlpha > 0f)
            {
                ScreenFadeAlpha = Mathf.MoveTowards(ScreenFadeAlpha, 0f, Time.unscaledDeltaTime * 4.2f);
                yield return null;
            }
            _transitioning = false;
        }

        private void EnterChamberImmediate(int index)
        {
            CurrentChamberIndex = index;
            _chamberCleared = false;
            _currentWaveIndex = 0;
            _aliveShades.Clear();

            // Clear stray enemy orbs between rooms
            foreach (StygianOrbProjectile orb in Object.FindObjectsByType<StygianOrbProjectile>(FindObjectsSortMode.None))
            {
                Destroy(orb.gameObject);
            }

            ChamberRuntimeData ch = chambers[index];

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

            OrpheusHud.Instance?.ShowBanner(ch.chamberTitle, 3.0f);

            if (ch.boss != null)
            {
                ch.boss.OnCerberusDefeated += HandleChamberCleared;
                ch.boss.ActivateGuardianDuel();
            }
            else if (ch.wave1.Count > 0)
            {
                StartCoroutine(SpawnWaveWithRunesRoutine(ch, ch.wave1, 0.35f));
            }
            else
            {
                HandleChamberCleared();
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
            yield return new WaitForSeconds(0.55f);

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
            }

            _spawningWave = false;
        }

        private void OnShadeKilled(ShadeEnemy slain)
        {
            _aliveShades.Remove(slain);
            if (_spawningWave || _chamberCleared) return;

            if (_aliveShades.Count == 0)
            {
                ChamberRuntimeData ch = chambers[CurrentChamberIndex];
                if (_currentWaveIndex == 0 && ch.wave2 != null && ch.wave2.Count > 0)
                {
                    _currentWaveIndex = 1;
                    OrpheusHud.Instance?.ShowBanner("SHADE REINFORCEMENTS SUMMONED!", 2.2f);
                    StartCoroutine(SpawnWaveWithRunesRoutine(ch, ch.wave2, 0.30f));
                }
                else
                {
                    HandleChamberCleared();
                }
            }
        }

        private void HandleChamberCleared()
        {
            if (_chamberCleared) return;
            _chamberCleared = true;

            ChamberRuntimeData ch = chambers[CurrentChamberIndex];
            ProceduralLyreAudio.Instance?.PlaySoftAttack(0.85f);

            if (ch.dropApolloBoonOnClear)
            {
                HarmonicBoonShrine.SpawnAt(transform, ch.centerWorld + new Vector3(1.5f, 0f, 1.5f),
                    "BOON OF APOLLO CLAIMED: +22% Boom Radius, +15% Strike Damage & +35 Vitality!");
            }

            if (ch.exitDoor != null)
            {
                ch.exitDoor.SetUnlocked(true);
                GroundShockwaveRing.Spawn(ch.exitDoor.transform.position + new Vector3(0f, 0.06f, 0f), 4.2f, new Color(0.5f, 2.6f, 1.5f, 1f), 0.45f);
            }

            if (ch.boss == null)
            {
                OrpheusHud.Instance?.ShowBanner("CHAMBER CLEAR!  Exit Archway Unlocked (North-East)", 3.4f);
            }
        }
    }

    /// <summary>
    /// Builds the 4-Chamber 2.5D Isometric HADES Chamber-Crawler prototype.
    /// Automatically upgrades any empty or older 2D scene when Play is pressed!
    /// </summary>
    public static class OrpheusPrototypeBuilder
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapIfNeeded()
        {
            if (Object.FindFirstObjectByType<HadesChamberDirector>() != null)
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

            // --- 2. ORPHEUS & BLURRED EURYDICE (2.5D ISOMETRIC) ---
            GameObject orpheusGo = new GameObject("Orpheus_Player25D");
            orpheusGo.transform.SetParent(root.transform, false);
            orpheusGo.transform.position = new Vector3(-5.5f, 0f, -5.5f);
            OrpheusPlayerController player = orpheusGo.AddComponent<OrpheusPlayerController>();
            orpheusGo.AddComponent<OrpheusLyreCombat>();

            GameObject eurydiceGo = new GameObject("Eurydice_BlurredCompanion25D");
            eurydiceGo.transform.SetParent(root.transform, false);
            eurydiceGo.transform.position = new Vector3(-7.2f, 0f, -7.2f);
            BlurredEurydice eurydice = eurydiceGo.AddComponent<BlurredEurydice>();
            eurydice.orpheus = orpheusGo.transform;

            // --- 3. 2.5D ISOMETRIC CAMERA ---
            Camera mainCam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (mainCam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                mainCam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            // Remove any leftover 2D components if upgrading an older scene
            UnderworldCamera2D camIso = mainCam.GetComponent<UnderworldCamera2D>();
            if (camIso == null) camIso = mainCam.gameObject.AddComponent<UnderworldCamera2D>();
            camIso.target = player.transform;
            camIso.eurydiceTarget = eurydice.transform;

            Transform chambersRoot = new GameObject("Isometric_Chambers_25D").transform;
            chambersRoot.SetParent(root.transform, false);

            // =========================================================================
            // CHAMBER 01: COURTYARD OF ASPHODEL (Center: 0, 0, 0 | Size: 18 x 18)
            // Warm-up Hades diamond room: Pillars, Cracked Obelisk, & 3 Wretch Lungers
            // =========================================================================
            Vector3 c1 = new Vector3(0f, 0f, 0f);
            ChamberExitDoor door1 = BuildDiamondChamberShell(chambersRoot, "Chamber01_Asphodel", c1, 18f, 18f, targetChamber: 1, finalExit: false);
            CreateIsoPillar(chambersRoot, c1 + new Vector3(-3.8f, 0f,  1.2f));
            CreateIsoPillar(chambersRoot, c1 + new Vector3( 1.2f, 0f, -3.8f));
            CreateCrackedObelisk(chambersRoot, c1 + new Vector3(0f, 0f, 0f));

            director.chambers.Add(new HadesChamberDirector.ChamberRuntimeData
            {
                chamberTitle = "CHAMBER I — COURTYARD OF ASPHODEL",
                centerWorld = c1,
                playerSpawnWorld = c1 + new Vector3(-6.2f, 0f, -6.2f),
                cameraLeashRadius = 6.5f,
                exitDoor = door1,
                dropApolloBoonOnClear = false,
                wave1 = new List<HadesChamberDirector.WaveSpawnSpec>
                {
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.WretchLunger, localOffsetXZ = new Vector2( 2.8f,  1.5f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.WretchLunger, localOffsetXZ = new Vector2(-1.5f,  3.2f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.WretchLunger, localOffsetXZ = new Vector2( 3.8f, -1.2f) }
                }
            });

            // =========================================================================
            // CHAMBER 02: HALL OF PILLARS (Center: 80, 0, 80 | Size: 22 x 22)
            // 2-Wave Hades brawl: Wall-Slam pillars, Brimstone Casters, Armored Warden, & Boon Reward!
            // =========================================================================
            Vector3 c2 = new Vector3(80f, 0f, 80f);
            ChamberExitDoor door2 = BuildDiamondChamberShell(chambersRoot, "Chamber02_Pillars", c2, 22f, 22f, targetChamber: 2, finalExit: false);
            CreateIsoPillar(chambersRoot, c2 + new Vector3(-4.2f, 0f, -1.0f));
            CreateIsoPillar(chambersRoot, c2 + new Vector3(-1.0f, 0f, -4.2f));
            CreateIsoPillar(chambersRoot, c2 + new Vector3( 4.2f, 0f,  1.0f));
            CreateIsoPillar(chambersRoot, c2 + new Vector3( 1.0f, 0f,  4.2f));
            CreateCrackedObelisk(chambersRoot, c2 + new Vector3(-2.5f, 0f, 2.5f));
            CreateCrackedObelisk(chambersRoot, c2 + new Vector3( 2.5f, 0f, -2.5f));

            director.chambers.Add(new HadesChamberDirector.ChamberRuntimeData
            {
                chamberTitle = "CHAMBER II — HALL OF TARTARUS PILLARS",
                centerWorld = c2,
                playerSpawnWorld = c2 + new Vector3(-8.0f, 0f, -8.0f),
                cameraLeashRadius = 8.5f,
                exitDoor = door2,
                dropApolloBoonOnClear = true,
                wave1 = new List<HadesChamberDirector.WaveSpawnSpec>
                {
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.WretchLunger,    localOffsetXZ = new Vector2(-1.5f,  2.0f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.WretchLunger,    localOffsetXZ = new Vector2( 2.2f, -1.2f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.BrimstoneCaster, localOffsetXZ = new Vector2( 4.5f,  4.5f) }
                },
                wave2 = new List<HadesChamberDirector.WaveSpawnSpec>
                {
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.ArmoredWarden,   localOffsetXZ = new Vector2( 1.8f,  1.8f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.BrimstoneCaster, localOffsetXZ = new Vector2(-4.5f,  4.0f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.BrimstoneCaster, localOffsetXZ = new Vector2( 4.0f, -4.5f) }
                }
            });

            // =========================================================================
            // CHAMBER 03: CHASMS OF STYX (Center: 160, 0, 160 | Size: 24 x 24)
            // Dash across glowing Stygian Chasms ([Space] vaults right over!) + Eurydice Mist Whisper
            // =========================================================================
            Vector3 c3 = new Vector3(160f, 0f, 160f);
            ChamberExitDoor door3 = BuildDiamondChamberShell(chambersRoot, "Chamber03_StyxChasms", c3, 24f, 24f, targetChamber: 3, finalExit: false);

            // Two dashable Stygian Lava Chasm trenches cutting across the room (2.4m wide — crisp Spacebar Dash!)
            CreateDashableStyxChasm(chambersRoot, c3 + new Vector3(-2.6f, 0f,  2.2f), new Vector3(9.5f, 1.0f, 2.3f));
            CreateDashableStyxChasm(chambersRoot, c3 + new Vector3( 3.2f, 0f, -2.4f), new Vector3(2.3f, 1.0f, 9.5f));
            CreateIsoPillar(chambersRoot, c3 + new Vector3(0f, 0f, 0f));
            CreateTemptationMistZone(chambersRoot, c3 + new Vector3(5.5f, 0f, 5.5f), new Vector3(7f, 3f, 7f),
                "...Across the roaring Styx chasm, you cannot hear Eurydice's footsteps...   [Hold Q to Look Back]");

            director.chambers.Add(new HadesChamberDirector.ChamberRuntimeData
            {
                chamberTitle = "CHAMBER III — CHASMS OF THE STYX  ([Space] Dash Over Pits!)",
                centerWorld = c3,
                playerSpawnWorld = c3 + new Vector3(-8.8f, 0f, -8.8f),
                cameraLeashRadius = 9.5f,
                exitDoor = door3,
                dropApolloBoonOnClear = true,
                wave1 = new List<HadesChamberDirector.WaveSpawnSpec>
                {
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.BrimstoneCaster, localOffsetXZ = new Vector2(-4.5f,  5.8f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.BrimstoneCaster, localOffsetXZ = new Vector2( 6.0f, -4.2f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.WretchLunger,    localOffsetXZ = new Vector2( 1.8f,  2.2f) }
                },
                wave2 = new List<HadesChamberDirector.WaveSpawnSpec>
                {
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.ArmoredWarden,   localOffsetXZ = new Vector2( 4.2f,  4.2f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.WretchLunger,    localOffsetXZ = new Vector2(-2.2f, -1.8f) },
                    new HadesChamberDirector.WaveSpawnSpec { archetype = ShadeArchetype.WretchLunger,    localOffsetXZ = new Vector2( 5.5f,  1.5f) }
                }
            });

            // =========================================================================
            // CHAMBER 04: SANCTUARY OF CERBERUS (Center: 240, 0, 240 | Size: 26 x 26)
            // 2.5D Isometric Boss Sanctuary + Final Sunlight Threshold!
            // =========================================================================
            Vector3 c4 = new Vector3(240f, 0f, 240f);
            ChamberExitDoor door4 = BuildDiamondChamberShell(chambersRoot, "Chamber04_CerberusSanctuary", c4, 26f, 26f, targetChamber: 4, finalExit: true);
            CreateIsoPillar(chambersRoot, c4 + new Vector3(-6.0f, 0f,  0.5f));
            CreateIsoPillar(chambersRoot, c4 + new Vector3( 0.5f, 0f, -6.0f));

            GameObject bossGo = new GameObject("Cerberus_Guardian25D");
            bossGo.transform.SetParent(chambersRoot, false);
            bossGo.transform.position = c4 + new Vector3(5.2f, 0f, 5.2f);
            CerberusRhythmBoss cerberus = bossGo.AddComponent<CerberusRhythmBoss>();

            CreateTemptationMistZone(chambersRoot, c4 + new Vector3(8.8f, 0f, 8.8f), new Vector3(6f, 3f, 6f),
                "\"Orpheus... wait... one glance before the blinding upper sun...\"   [Hold Q to Look Back — or Walk into Sunlight]");

            director.chambers.Add(new HadesChamberDirector.ChamberRuntimeData
            {
                chamberTitle = "CHAMBER IV — GATES OF STYX: CERBERUS",
                centerWorld = c4,
                playerSpawnWorld = c4 + new Vector3(-9.2f, 0f, -9.2f),
                cameraLeashRadius = 9.0f,
                cameraOrthoOverride = 9.0f,
                exitDoor = door4,
                boss = cerberus
            });

            return root;
        }

        private static ChamberExitDoor BuildDiamondChamberShell(
            Transform parent, string name, Vector3 center, float sizeX, float sizeZ, int targetChamber, bool finalExit)
        {
            Transform room = new GameObject(name).transform;
            room.SetParent(parent, false);
            room.position = center;

            Color floorTop   = new Color(0.14f, 0.10f, 0.20f, 1f);
            Color floorLeft  = new Color(0.08f, 0.05f, 0.12f, 1f);
            Color floorRight = new Color(0.05f, 0.03f, 0.08f, 1f);

            // Main 3D Isometric Stone Slab (top surface at Y = 0)
            RenderPipeline2DSetup.CreateIsoBlock(room, "MainFloorSlab",
                new Vector3(0f, -0.75f, 0f), new Vector3(sizeX, 1.5f, sizeZ),
                floorTop, floorLeft, floorRight, addSolidWallCollider: false);

            // Subtle Hades mosaic floor tiles so 360° movement & dash speed read crystal-clearly!
            Color tileAccent = new Color(0.22f, 0.15f, 0.31f, 0.55f);
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

            // 4 Perimeter 2.5D Walls (Back-Left -X, Back-Right +Z, Front-Left -Z, Front-Right +X)
            // Back walls (+X, +Z) are tall grand Hades architecture; front walls (-X, -Z) are low parapets so camera never gets blocked!
            Color wallTop   = new Color(0.28f, 0.17f, 0.34f, 1f);
            Color wallLeft  = new Color(0.17f, 0.09f, 0.22f, 1f);
            Color wallRight = new Color(0.10f, 0.05f, 0.14f, 1f);

            float hx = sizeX * 0.5f;
            float hz = sizeZ * 0.5f;

            // Upper-Left Wall (+Z)
            RenderPipeline2DSetup.CreateIsoBlock(room, "Wall_NorthWest_PlusZ",
                new Vector3(-1.8f, 1.4f, hz + 0.6f), new Vector3(sizeX - 3.6f, 2.8f, 1.2f),
                wallTop, wallLeft, wallRight, addSolidWallCollider: true);

            // Upper-Right Wall (+X)
            RenderPipeline2DSetup.CreateIsoBlock(room, "Wall_NorthEast_PlusX",
                new Vector3(hx + 0.6f, 1.4f, -1.8f), new Vector3(1.2f, 2.8f, sizeZ - 3.6f),
                wallTop, wallLeft, wallRight, addSolidWallCollider: true);

            // Lower Parapet Walls (-Z and -X: short visual curb + tall invisible collider)
            CreateInvisibleRimWall(room, "Wall_SouthWest_MinusZ", new Vector3(0f, 0.35f, -hz - 0.5f), new Vector3(sizeX + 1.2f, 0.7f, 1.0f), wallTop, wallLeft, wallRight);
            CreateInvisibleRimWall(room, "Wall_SouthEast_MinusX", new Vector3(-hx - 0.5f, 0.35f, 0f), new Vector3(1.0f, 0.7f, sizeZ + 1.2f), wallTop, wallLeft, wallRight);

            // NE Corner Exit Archway (+hx - 1.2, +hz - 1.2) — right at the top apex of the diamond chamber!
            Vector3 doorPos = new Vector3(hx - 1.1f, 0f, hz - 1.1f);
            RenderPipeline2DSetup.CreateIsoBlock(room, "ExitGatePillarL",
                doorPos + new Vector3(-1.8f, 1.8f, 0.6f), new Vector3(1.1f, 3.6f, 1.1f),
                new Color(0.45f, 0.32f, 0.20f, 1f), wallLeft, wallRight, addSolidWallCollider: true);
            RenderPipeline2DSetup.CreateIsoBlock(room, "ExitGatePillarR",
                doorPos + new Vector3(0.6f, 1.8f, -1.8f), new Vector3(1.1f, 3.6f, 1.1f),
                new Color(0.45f, 0.32f, 0.20f, 1f), wallLeft, wallRight, addSolidWallCollider: true);
            // Solid backstop behind doorway so player can't walk into void when locked
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

            // Atmospheric Braziers at corners
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
            // Tall 2.5D Isometric Pillar (occludes sprites behind it via ZWrite On + triggers Hades WALL-SLAMS!)
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

            // Dark sunken pit decal + glowing emerald-turquoise Styx magma core on the floor!
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
