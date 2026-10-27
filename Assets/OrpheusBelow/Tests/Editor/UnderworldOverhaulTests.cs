using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class UnderworldOverhaulTests
    {
        [Test]
        public void FiveChambersRetainLargerLaterRoomsAndTwoWaves()
        {
            Assert.That(ProceduralChamberGenerator.TotalChambers, Is.EqualTo(5));
            Assert.That(ProceduralChamberGenerator.ChamberNames.Length, Is.EqualTo(5));
            for (int i = 1; i < ProceduralChamberGenerator.TotalChambers; i++)
            {
                Vector2 size = ProceduralChamberGenerator.GetChamberSize(i);
                Assert.That(size.x, Is.GreaterThanOrEqualTo(34f));
                Assert.That(size.y, Is.GreaterThanOrEqualTo(38f));
                var first = ProceduralChamberGenerator.GenerateWaveSpecs(i, 1);
                var second = ProceduralChamberGenerator.GenerateWaveSpecs(i, 2);
                Assert.That(first.Count, Is.GreaterThanOrEqualTo(5));
                Assert.That(second.Count, Is.GreaterThanOrEqualTo(first.Count));
                foreach (var spawn in second)
                {
                    Assert.That(Mathf.Abs(spawn.localOffsetXZ.x), Is.LessThan(size.x * 0.5f - 2));
                    Assert.That(Mathf.Abs(spawn.localOffsetXZ.y), Is.LessThan(size.y * 0.5f - 2));
                }
            }
        }

        [Test]
        public void ShortenedRouteRetainsEveryGuardianAndEndsWithCerberus()
        {
            var root = new GameObject("FiveChamberRouteTest");
            BossIdentity?[] expected = { BossIdentity.CantorOfFury, BossIdentity.OarsmanOfStyx,
                null, BossIdentity.SirenOfAcheron, BossIdentity.CerberusHoundOfHades };
            try
            {
                for (int i = 0; i < ProceduralChamberGenerator.TotalChambers; i++)
                {
                    var room = ProceduralChamberGenerator.GenerateChamber(root.transform, i,
                        ProceduralChamberGenerator.TotalChambers, Vector3.right * i * 90f);
                    Assert.That(room.exitDoor.isFinalSunlightExit, Is.EqualTo(i == 4));
                    Assert.That(room.exitDoor.targetChamberIndex, Is.EqualTo(i + 1));
                    if (expected[i].HasValue)
                        Assert.That(room.guardianBoss.identity, Is.EqualTo(expected[i].Value));
                    else Assert.That(room.guardianBoss, Is.Null);
                    Assert.That(room.roomRoot.GetComponentInChildren<AmbrosiaHealingWell>(), Is.Not.Null);
                    Assert.That(room.roomRoot.transform.Find("StygianBrazier"), Is.Null);
                    if (i == 2) StringAssert.Contains("Burial", room.roomRoot.name);
                    if (i == 0)
                    {
                        var map = room.roomRoot.GetComponent<PaintedChamberMap>();
                        Assert.That(map, Is.Not.Null);
                        Assert.That(map.IsWalkable(room.playerSpawnWorld), Is.True);
                        Assert.That(map.transform.Find("NavigationBoundary_Solid_NoAccess").GetComponentsInChildren<BoxCollider>().Length,
                            Is.GreaterThan(4));
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PaintedFloorCoversRoomAndBeaconUsesGeneratedArtWithSolidFootprint()
        {
            var root = new GameObject("PaintedAssetsTest");
            try
            {
                var floor = UnderworldArtLibrary.CreatePaintedFloor(root.transform, 34f, 38f, false);
                Assert.That(floor.sprite.name, Is.EqualTo("QuietSlateFloor"));
                Assert.That(floor.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(floor.bounds.center.x, Is.EqualTo(0f).Within(.01f));
                Assert.That(floor.bounds.center.z, Is.EqualTo(0f).Within(.01f));
                Assert.That(floor.bounds.size.x, Is.EqualTo(34f).Within(.01f));
                Assert.That(floor.bounds.size.z, Is.EqualTo(38f).Within(.01f));
                var tower = UnderworldArtLibrary.CreateBeaconTower(root.transform, Vector3.zero);
                Assert.That(tower.transform.Find("PaintedBeacon").GetComponent<SpriteRenderer>().sprite.name,
                    Is.EqualTo("DarkBeaconTower"));
                Assert.That(tower.GetComponent<BoxCollider>().isTrigger, Is.False);
                Assert.That(tower.GetComponent<IsoObstacle>().kind, Is.EqualTo(IsoObstacleKind.SolidWall));
                var panel = UnderworldArtLibrary.PaintedSprite("PaintedSlatePanel", 8f, true);
                Assert.That(panel.border.x, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ShadesAndGuardiansUseOriginalProceduralSprites()
        {
            var shadeObject = new GameObject("RestoredShadeTest");
            var bossObject = new GameObject("RestoredBossTest");
            try
            {
                shadeObject.AddComponent<ShadeEnemy>().SetupArchetype(ShadeArchetype.WretchLunger);
                var body = shadeObject.transform.Find("ShadeBillboard25D/ShadeBody").GetComponent<SpriteRenderer>();
                Assert.That(body.sprite, Is.SameAs(RenderPipeline2DSetup.DiamondSprite));
                bossObject.AddComponent<CerberusRhythmBoss>().ConfigureBoss(BossIdentity.CantorOfFury, Vector3.zero);
                foreach (var sprite in bossObject.GetComponentsInChildren<SpriteRenderer>())
                    Assert.That(sprite.gameObject.name, Is.Not.EqualTo("PaintedGuardian"));
            }
            finally { Object.DestroyImmediate(shadeObject); Object.DestroyImmediate(bossObject); }
        }

        [Test]
        public void PaintedBackdropUsesWorldDimensionsRegardlessOfResolution()
        {
            var go = new GameObject("ParallaxTest");
            var texture = new Texture2D(384, 216);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 384, 216), Vector2.one * 0.5f, 10f);
            try
            {
                var rig = go.AddComponent<UnderworldParallaxRig>();
                rig.farAbyssSkySprite = sprite;
                rig.Initialize(Vector3.zero, Color.white, Color.white, Color.white);
                var sky = go.transform.Find("Parallax0_FarAbyss/CustomSky");
                Assert.That(sky, Is.Not.Null);
                Assert.That(sky.localScale.x * sprite.bounds.size.x, Is.EqualTo(36f).Within(0.001f));
                Assert.That(sky.localScale.y * sprite.bounds.size.y, Is.EqualTo(18f).Within(0.001f));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [Test]
        public void AllEightPaintedCreatureRegionsLoad()
        {
            for (int i = 0; i < 8; i++)
            {
                Sprite sprite = UnderworldArtLibrary.Creature(i);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.bounds.size.y, Is.EqualTo(2.4f).Within(0.01f));
            }
            Assert.That(Resources.Load<Texture2D>("Art/UnderworldPanorama"), Is.Not.Null);
        }

        [Test]
        public void EnvironmentCanBeBuiltAndRebuiltWithoutNativeInitializationOrCleanupErrors()
        {
            // Exercise Unity object lifecycle: pure combat tests cannot catch native resources
            // accidentally allocated by MonoBehaviour field initializers.
            for (int pass = 0; pass < 2; pass++)
            {
                var root = new GameObject("EnvironmentLifecycleTest");
                try
                {
                    UnderworldEnvironmentArt.Build(root.transform, 0, 34f, 38f, ProceduralChamberGenerator.Biomes[0]);
                    Assert.That(root.GetComponentsInChildren<MeshRenderer>().Length, Is.GreaterThan(0));
                }
                finally { Object.DestroyImmediate(root); }
            }
        }

        [Test]
        public void CreatureSpriteEdgesHaveTransparentPaddingWithoutNeighborFragments()
        {
            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Application.dataPath,
                    "OrpheusBelow/Resources/Art/UnderworldCreatures.png")));
                for (int i = 0; i < 8; i++)
                {
                    Rect r = UnderworldArtLibrary.Creature(i).rect;
                    int x0 = Mathf.RoundToInt(r.xMin), x1 = Mathf.RoundToInt(r.xMax) - 1;
                    int y0 = Mathf.RoundToInt(r.yMin), y1 = Mathf.RoundToInt(r.yMax) - 1;
                    for (int x = x0; x <= x1; x++)
                    {
                        Assert.That(texture.GetPixel(x, y0).a, Is.LessThan(.05f), "bottom of sprite " + i);
                        Assert.That(texture.GetPixel(x, y1).a, Is.LessThan(.05f), "top of sprite " + i);
                    }
                    for (int y = y0; y <= y1; y++)
                    {
                        Assert.That(texture.GetPixel(x0, y).a, Is.LessThan(.05f), "left of sprite " + i);
                        Assert.That(texture.GetPixel(x1, y).a, Is.LessThan(.05f), "right of sprite " + i);
                    }
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void OffBeatOrdinaryDamageCannotDefeatGuardian()
        {
            var go = new GameObject("GuardianTest");
            try
            {
                var boss = go.AddComponent<CerberusRhythmBoss>();
                boss.ConfigureBoss(BossIdentity.CantorOfFury, Vector3.zero);
                boss.ActivateGuardianDuel();
                float initial = boss.CurrentHealth;
                for (int i = 0; i < 100; i++) boss.OnOrphicHit(100f, 1f, true, Vector3.zero);
                Assert.That(boss.CurrentHealth, Is.EqualTo(initial));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
