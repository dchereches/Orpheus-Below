using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// Procedural Chamber Generator based on Tilted 2.5D Painted Background Plates (A5):
    ///   - Supports arbitrary chamber count (8+ chambers across multiple Underworld Biomes).
    ///   - Drops in painted 2D background plate textures (or automatically loads from Resources/ChamberBackgrounds/).
    ///   - Procedurally stamps perimeter colliders, interior pillars, cracked obelisks, dashable chasms,
    ///     temptation mist zones, mirage decoys, and multi-wave shade encounters.
    /// </summary>
    public static class ProceduralChamberGenerator
    {
        public struct BiomeTheme
        {
            public string name;
            public Color floorTop;
            public Color floorLeft;
            public Color floorRight;
            public Color wallTop;
            public Color parallaxGlow;
            public Color parallaxArch;
            public Color parallaxFire;
        }

        public static readonly BiomeTheme[] Biomes = new[]
        {
            new BiomeTheme // Biome 0: Asphodel Threshold
            {
                name = "Asphodel Threshold",
                floorTop = new Color(0.13f, 0.09f, 0.19f, 1f),
                floorLeft = new Color(0.08f, 0.05f, 0.12f, 1f),
                floorRight = new Color(0.05f, 0.03f, 0.08f, 1f),
                wallTop = new Color(0.26f, 0.16f, 0.33f, 1f),
                parallaxGlow = new Color(0.55f, 0.12f, 0.28f, 0.85f),
                parallaxArch = new Color(0.16f, 0.08f, 0.22f, 0.94f),
                parallaxFire = new Color(2.0f, 0.45f, 0.22f, 0.42f)
            },
            new BiomeTheme // Biome 1: Vault of the Ferryman
            {
                name = "Vault of the Ferryman",
                floorTop = new Color(0.08f, 0.12f, 0.18f, 1f),
                floorLeft = new Color(0.05f, 0.08f, 0.13f, 1f),
                floorRight = new Color(0.03f, 0.05f, 0.09f, 1f),
                wallTop = new Color(0.16f, 0.26f, 0.35f, 1f),
                parallaxGlow = new Color(0.12f, 0.48f, 0.52f, 0.85f),
                parallaxArch = new Color(0.08f, 0.16f, 0.24f, 0.94f),
                parallaxFire = new Color(0.35f, 1.85f, 1.55f, 0.40f)
            },
            new BiomeTheme // Biome 2: Chasms of Acheron
            {
                name = "Chasms of Acheron",
                floorTop = new Color(0.14f, 0.07f, 0.18f, 1f),
                floorLeft = new Color(0.09f, 0.04f, 0.12f, 1f),
                floorRight = new Color(0.06f, 0.02f, 0.08f, 1f),
                wallTop = new Color(0.32f, 0.14f, 0.40f, 1f),
                parallaxGlow = new Color(0.45f, 0.14f, 0.65f, 0.86f),
                parallaxArch = new Color(0.14f, 0.08f, 0.26f, 0.94f),
                parallaxFire = new Color(1.65f, 0.45f, 2.0f, 0.42f)
            },
            new BiomeTheme // Biome 3: Phlegethon Lava Vaults
            {
                name = "Phlegethon Lava Vaults",
                floorTop = new Color(0.18f, 0.06f, 0.08f, 1f),
                floorLeft = new Color(0.12f, 0.04f, 0.05f, 1f),
                floorRight = new Color(0.08f, 0.02f, 0.03f, 1f),
                wallTop = new Color(0.42f, 0.12f, 0.14f, 1f),
                parallaxGlow = new Color(0.75f, 0.18f, 0.12f, 0.88f),
                parallaxArch = new Color(0.22f, 0.06f, 0.08f, 0.95f),
                parallaxFire = new Color(2.8f, 0.65f, 0.18f, 0.50f)
            },
            new BiomeTheme // Biome 4: Cocytus Frozen Tarn
            {
                name = "Cocytus Frozen Tarn",
                floorTop = new Color(0.06f, 0.12f, 0.18f, 1f),
                floorLeft = new Color(0.04f, 0.08f, 0.12f, 1f),
                floorRight = new Color(0.02f, 0.05f, 0.08f, 1f),
                wallTop = new Color(0.14f, 0.25f, 0.38f, 1f),
                parallaxGlow = new Color(0.18f, 0.38f, 0.65f, 0.85f),
                parallaxArch = new Color(0.06f, 0.14f, 0.24f, 0.95f),
                parallaxFire = new Color(0.45f, 1.45f, 2.4f, 0.40f)
            },
            new BiomeTheme // Biome 5: Gates of Styx
            {
                name = "Gates of Styx",
                floorTop = new Color(0.16f, 0.05f, 0.10f, 1f),
                floorLeft = new Color(0.10f, 0.03f, 0.06f, 1f),
                floorRight = new Color(0.06f, 0.01f, 0.04f, 1f),
                wallTop = new Color(0.38f, 0.10f, 0.18f, 1f),
                parallaxGlow = new Color(0.68f, 0.12f, 0.16f, 0.88f),
                parallaxArch = new Color(0.18f, 0.06f, 0.12f, 0.95f),
                parallaxFire = new Color(2.4f, 0.55f, 0.20f, 0.45f)
            }
        };

        private static readonly Dictionary<int, Texture2D> CustomPlates = new Dictionary<int, Texture2D>();

        public static void RegisterBackgroundPlate(int chamberIndex, Texture2D plateTexture)
        {
            if (plateTexture != null)
            {
                CustomPlates[chamberIndex] = plateTexture;
            }
        }

        public static HadesChamberDirector.ChamberRuntimeData GenerateChamber(
            Transform parent,
            int chamberIndex,
            int totalChambers,
            Vector3 centerWorld)
        {
            int biomeIdx = Mathf.Clamp(chamberIndex * Biomes.Length / Mathf.Max(1, totalChambers), 0, Biomes.Length - 1);
            BiomeTheme theme = Biomes[biomeIdx];

            float sizeX = 22f + (chamberIndex % 3) * 2.5f;
            float sizeZ = 22f + (chamberIndex % 3) * 2.5f;
            bool isFinal = (chamberIndex == totalChambers - 1);
            bool isBossChamber = (chamberIndex == 0 || chamberIndex == 2 || chamberIndex == 4 || isFinal);

            string titleRoman = ToRoman(chamberIndex + 1);
            string title = $"{titleRoman}  ·  {theme.name}";

            Transform chamberRoot = new GameObject($"ProceduralChamber_{chamberIndex:D2}_{theme.name}").transform;
            chamberRoot.SetParent(parent, false);
            chamberRoot.position = centerWorld;

            // 1. Tilted 2.5D Background Plate Quad (A5)
            BuildTiltedBackgroundPlate(chamberRoot, chamberIndex, sizeX, sizeZ, theme);

            // 2. Procedural Perimeter Walls & Invisible Rim Bounds
            ChamberExitDoor exitDoor = BuildPerimeterShell(chamberRoot, centerWorld, sizeX, sizeZ, chamberIndex + 1, isFinal, theme);

            // 3. Interior Obstacles Container
            Transform obsRoot = new GameObject("InteriorObstacles").transform;
            obsRoot.SetParent(chamberRoot, false);
            StampInteriorProps(obsRoot, chamberIndex, sizeX, sizeZ, theme);

            // 4. Temptation Mist Zones & Mirages (in mid-to-late chambers)
            if (chamberIndex >= 1 && !isFinal)
            {
                Vector3 mistPos = centerWorld + new Vector3(sizeX * 0.24f, 0f, sizeZ * 0.24f);
                CreateTemptationZoneWithMirage(chamberRoot, mistPos, "The Stygian mist swallows Eurydice's footsteps... [Play Soft Note to Hear Her — or Hold Q]");
            }

            // 5. Parallax Rig & Guardian Mini-Boss if a Boss Chamber
            CerberusRhythmBoss boss = null;
            CreateParallaxRig(chamberRoot, $"Parallax2DRig_Ch{chamberIndex}", centerWorld, theme);

            if (isBossChamber)
            {
                BossIdentity bossId = (chamberIndex == 0) ? BossIdentity.CantorOfFury
                                    : (chamberIndex == 2) ? BossIdentity.OarsmanOfStyx
                                    : (chamberIndex == 4) ? BossIdentity.SirenOfAcheron
                                    : BossIdentity.CerberusHoundOfHades;
                boss = CreateGuardian(chamberRoot, $"GuardianBoss_Ch{chamberIndex}", bossId, centerWorld);
            }

            // 6. Wave Enemy Spawns (Scales with chamber depth)
            List<HadesChamberDirector.WaveSpawnSpec> w1 = GenerateWaveSpecs(chamberIndex, waveNumber: 1);
            List<HadesChamberDirector.WaveSpawnSpec> w2 = isBossChamber ? null : GenerateWaveSpecs(chamberIndex, waveNumber: 2);

            return new HadesChamberDirector.ChamberRuntimeData
            {
                chamberTitle = title,
                centerWorld = centerWorld,
                playerSpawnWorld = centerWorld + new Vector3(-sizeX * 0.38f, 0f, -sizeZ * 0.38f),
                cameraLeashRadius = sizeX * 0.36f,
                cameraOrthoOverride = isFinal ? 8.8f : -1f,
                exitDoor = exitDoor,
                guardianBoss = boss,
                interiorObstaclesRoot = obsRoot,
                wave1 = w1,
                wave2 = w2
            };
        }

        private static void BuildTiltedBackgroundPlate(Transform root, int chamberIndex, float sizeX, float sizeZ, BiomeTheme theme)
        {
            // Try loading user background texture from Resources/ChamberBackgrounds/ or registered dictionary
            Texture2D plateTex = null;
            if (!CustomPlates.TryGetValue(chamberIndex, out plateTex))
            {
                plateTex = Resources.Load<Texture2D>($"ChamberBackgrounds/Chamber_{chamberIndex:D2}")
                        ?? Resources.Load<Texture2D>($"ChamberBackgrounds/Chamber_{chamberIndex}");
            }

            GameObject plateGo = new GameObject("TiltedBackgroundPlate25D");
            plateGo.transform.SetParent(root, false);
            plateGo.transform.localPosition = new Vector3(0f, 0.005f, 0f);

            if (plateTex != null)
            {
                // User-supplied painted background plate quad tilted flat on XZ ground
                Sprite sprite = Sprite.Create(plateTex, new Rect(0, 0, plateTex.width, plateTex.height), new Vector2(0.5f, 0.5f), 100f);
                SpriteRenderer sr = plateGo.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
                sr.sortingOrder = -1;
                plateGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                plateGo.transform.localScale = new Vector3(sizeX / (plateTex.width / 100f), sizeZ / (plateTex.height / 100f), 1f);
            }
            else
            {
                // Procedural high-res stylized mosaic slab with biome accent
                RenderPipeline2DSetup.CreateIsoBlock(root, "ProceduralBaseFloorSlab",
                    new Vector3(0f, -0.75f, 0f), new Vector3(sizeX, 1.5f, sizeZ),
                    theme.floorTop, theme.floorLeft, theme.floorRight, addSolidWallCollider: false);

                // Geometric mosaic floor patterns
                Color tileAccent = new Color(theme.floorTop.r * 1.5f, theme.floorTop.g * 1.5f, theme.floorTop.b * 1.5f, 0.45f);
                int gx = Mathf.FloorToInt(sizeX / 4f);
                int gz = Mathf.FloorToInt(sizeZ / 4f);
                for (int ix = -gx / 2; ix <= gx / 2; ix++)
                {
                    for (int iz = -gz / 2; iz <= gz / 2; iz++)
                    {
                        if ((ix + iz) % 2 != 0) continue;
                        GameObject tile = new GameObject("FloorMosaicTile");
                        tile.transform.SetParent(root, false);
                        tile.transform.localPosition = new Vector3(ix * 3.4f, 0.008f, iz * 3.4f);
                        tile.transform.localScale = new Vector3(3.15f, 3.15f, 1f);
                        RenderPipeline2DSetup.AttachGroundDecal(tile, RenderPipeline2DSetup.SquareSprite, tileAccent, 0);
                    }
                }
            }
        }

        private static ChamberExitDoor BuildPerimeterShell(
            Transform room, Vector3 center, float sizeX, float sizeZ, int targetChamber, bool finalExit, BiomeTheme theme)
        {
            float hx = sizeX * 0.5f;
            float hz = sizeZ * 0.5f;
            Color wallLeft = new Color(theme.wallTop.r * 0.6f, theme.wallTop.g * 0.6f, theme.wallTop.b * 0.6f, 1f);
            Color wallRight = new Color(theme.wallTop.r * 0.35f, theme.wallTop.g * 0.35f, theme.wallTop.b * 0.35f, 1f);

            // North-West and North-East Solid 2.5D Isometric Walls
            RenderPipeline2DSetup.CreateIsoBlock(room, "Wall_NorthWest",
                new Vector3(-1.8f, 1.4f, hz + 0.6f), new Vector3(sizeX - 3.6f, 2.8f, 1.2f),
                theme.wallTop, wallLeft, wallRight, addSolidWallCollider: true);

            RenderPipeline2DSetup.CreateIsoBlock(room, "Wall_NorthEast",
                new Vector3(hx + 0.6f, 1.4f, -1.8f), new Vector3(1.2f, 2.8f, sizeZ - 3.6f),
                theme.wallTop, wallLeft, wallRight, addSolidWallCollider: true);

            // Low rim walls on South sides (so camera sightline remains pristine)
            CreateInvisibleRimWall(room, "Wall_SouthWest", new Vector3(0f, 0.14f, -hz - 0.5f), new Vector3(sizeX + 1.2f, 0.28f, 1.0f), theme.wallTop, wallLeft, wallRight);
            CreateInvisibleRimWall(room, "Wall_SouthEast", new Vector3(-hx - 0.5f, 0.14f, 0f), new Vector3(1.0f, 0.28f, sizeZ + 1.2f), theme.wallTop, wallLeft, wallRight);

            // Exit Portal Doorway
            Vector3 doorPos = new Vector3(hx - 1.1f, 0f, hz - 1.1f);
            RenderPipeline2DSetup.CreateIsoBlock(room, "GatePillarL",
                doorPos + new Vector3(-1.8f, 1.8f, 0.6f), new Vector3(1.1f, 3.6f, 1.1f),
                new Color(0.45f, 0.32f, 0.20f, 1f), wallLeft, wallRight, addSolidWallCollider: true);
            RenderPipeline2DSetup.CreateIsoBlock(room, "GatePillarR",
                doorPos + new Vector3(0.6f, 1.8f, -1.8f), new Vector3(1.1f, 3.6f, 1.1f),
                new Color(0.45f, 0.32f, 0.20f, 1f), wallLeft, wallRight, addSolidWallCollider: true);
            RenderPipeline2DSetup.CreateIsoBlock(room, "ExitBackstop",
                doorPos + new Vector3(1.5f, 1.5f, 1.5f), new Vector3(4.5f, 3.0f, 4.5f),
                theme.wallTop, wallLeft, wallRight, addSolidWallCollider: true);

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

            CreateHadesBrazier(room, new Vector3(-hx + 1.8f, 0f, hz - 1.8f));
            CreateHadesBrazier(room, new Vector3(hx - 1.8f, 0f, -hz + 1.8f));

            return exitComp;
        }

        private static void StampInteriorProps(Transform obsRoot, int chamberIndex, float sizeX, float sizeZ, BiomeTheme theme)
        {
            // Procedurally place pillars and cracked obelisks based on room seed
            int seed = chamberIndex * 17 + 3;
            Random.InitState(seed);

            float spanX = sizeX * 0.32f;
            float spanZ = sizeZ * 0.32f;

            // Quad symmetry or flanking pillars
            CreateIsoPillar(obsRoot, new Vector3(-spanX, 0f, 1.2f), theme);
            CreateIsoPillar(obsRoot, new Vector3(1.2f, 0f, -spanZ), theme);

            if (chamberIndex % 2 == 1)
            {
                CreateIsoPillar(obsRoot, new Vector3(spanX, 0f, -1.2f), theme);
                CreateIsoPillar(obsRoot, new Vector3(-1.2f, 0f, spanZ), theme);
            }

            // Cracked Styx Obelisk (destructible with Hard Boom)
            CreateCrackedObelisk(obsRoot, new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)));

            // Chasms in deeper chambers
            if (chamberIndex >= 2 && chamberIndex != 7)
            {
                CreateDashableStyxChasm(obsRoot, new Vector3(-2.8f, 0f, 2.4f), new Vector3(8.5f, 1.0f, 2.2f));
            }
        }

        private static List<HadesChamberDirector.WaveSpawnSpec> GenerateWaveSpecs(int chamberIndex, int waveNumber)
        {
            List<HadesChamberDirector.WaveSpawnSpec> specs = new List<HadesChamberDirector.WaveSpawnSpec>();

            // Base wretches
            int wretches = Mathf.Clamp(2 + chamberIndex / 2, 2, 5);
            for (int i = 0; i < wretches; i++)
            {
                float angle = (i / (float)wretches) * Mathf.PI * 2f;
                specs.Add(new HadesChamberDirector.WaveSpawnSpec
                {
                    archetype = ShadeArchetype.WretchLunger,
                    localOffsetXZ = new Vector2(Mathf.Cos(angle) * 3.8f, Mathf.Sin(angle) * 3.8f)
                });
            }

            // Casters in chamber 1+
            if (chamberIndex >= 1)
            {
                specs.Add(new HadesChamberDirector.WaveSpawnSpec
                {
                    archetype = ShadeArchetype.BrimstoneCaster,
                    localOffsetXZ = new Vector2(5.2f, 4.8f)
                });
            }

            // Armored Wardens
            if (chamberIndex >= 1 && (waveNumber == 2 || chamberIndex >= 3))
            {
                specs.Add(new HadesChamberDirector.WaveSpawnSpec
                {
                    archetype = ShadeArchetype.ArmoredWarden,
                    localOffsetXZ = new Vector2(2.0f, 2.0f)
                });
            }

            // Mourning Abductors (target Eurydice's Soul Tether!)
            if (chamberIndex >= 1)
            {
                specs.Add(new HadesChamberDirector.WaveSpawnSpec
                {
                    archetype = ShadeArchetype.MourningAbductor,
                    localOffsetXZ = new Vector2(-4.5f, -3.5f)
                });
            }

            return specs;
        }

        private static void CreateTemptationZoneWithMirage(Transform parent, Vector3 worldPos, string whisper)
        {
            GameObject go = new GameObject("TemptationMistZone");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(7f, 3f, 7f);
            go.AddComponent<TemptationWhisperZone>().whisperMessage = whisper;

            EurydiceMirageDecoy.Spawn(parent, worldPos + new Vector3(1.2f, 0f, 1.2f));
        }

        private static void CreateParallaxRig(Transform parent, string name, Vector3 stageCenter, BiomeTheme theme)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            UnderworldParallaxRig rig = go.AddComponent<UnderworldParallaxRig>();
            rig.Initialize(stageCenter, theme.parallaxGlow, theme.parallaxArch, theme.parallaxFire);
        }

        private static CerberusRhythmBoss CreateGuardian(Transform parent, string name, BossIdentity id, Vector3 stageCenter)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            CerberusRhythmBoss b = go.AddComponent<CerberusRhythmBoss>();
            b.ConfigureBoss(id, stageCenter);
            return b;
        }

        private static void CreateInvisibleRimWall(Transform parent, string name, Vector3 pos, Vector3 visualSize, Color top, Color left, Color right)
        {
            GameObject curb = RenderPipeline2DSetup.CreateIsoBlock(parent, name, pos, visualSize, top, left, right, addSolidWallCollider: false);
            BoxCollider col = curb.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 5.0f, 1f);
            col.center = new Vector3(0f, 2.0f, 0f);
            curb.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;
        }

        private static void CreateIsoPillar(Transform parent, Vector3 localPos, BiomeTheme theme)
        {
            Color left = new Color(theme.wallTop.r * 0.55f, theme.wallTop.g * 0.55f, theme.wallTop.b * 0.55f, 1f);
            Color right = new Color(theme.wallTop.r * 0.32f, theme.wallTop.g * 0.32f, theme.wallTop.b * 0.32f, 1f);
            RenderPipeline2DSetup.CreateIsoBlock(parent, "TartarusPillar",
                localPos + new Vector3(0f, 1.55f, 0f), new Vector3(1.45f, 3.1f, 1.45f),
                theme.wallTop, left, right, addSolidWallCollider: true);
        }

        private static void CreateCrackedObelisk(Transform parent, Vector3 localPos)
        {
            GameObject ob = RenderPipeline2DSetup.CreateIsoBlock(parent, "CrackedStyxObelisk",
                localPos + new Vector3(0f, 1.05f, 0f), new Vector3(1.35f, 2.1f, 1.35f),
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

        private static void CreateDashableStyxChasm(Transform parent, Vector3 localPos, Vector3 size)
        {
            GameObject chasm = new GameObject("DashableStyxChasm");
            chasm.transform.SetParent(parent, false);
            chasm.transform.localPosition = localPos;

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

        private static string ToRoman(int number)
        {
            string[] romans = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            if (number >= 1 && number <= romans.Length) return romans[number - 1];
            return number.ToString();
        }
    }
}
