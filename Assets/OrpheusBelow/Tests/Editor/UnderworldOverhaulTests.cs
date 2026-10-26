using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class UnderworldOverhaulTests
    {
        [Test]
        public void AllTwelveChambersAreLargerAndHaveEscalatingTwoWaveEncounters()
        {
            Assert.That(ProceduralChamberGenerator.ChamberNames.Length, Is.EqualTo(12));
            for (int i = 0; i < ProceduralChamberGenerator.TotalChambers; i++)
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
                    Assert.That(root.GetComponentsInChildren<StyxWaterSurface>().Length, Is.GreaterThan(0));
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
