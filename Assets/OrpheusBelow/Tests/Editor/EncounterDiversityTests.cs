using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class EncounterDiversityTests
    {
        [SetUp] public void ResetJourney() => UnderworldJourney.Reset();

        [Test]
        public void RecurringFormerMiniBossesOutnumberSkeletonsAcrossBothJourneyLegs()
        {
            foreach (bool ascending in new[] { false, true })
            {
                int skeletons = 0, formerMiniBosses = 0, variedRooms = 0;
                for (int depth = 0; depth < (ascending ? 7 : 8); depth++)
                {
                    var wave = ChamberEncounterPlan.Wave(depth, 1, ascending);
                    skeletons += wave.Count(s => s.archetype == ShadeArchetype.BrimstoneCaster);
                    int varied = wave.Count(s => ChamberEncounterPlan.IsMiniBoss(s.archetype));
                    formerMiniBosses += varied;
                    if (varied > 0) variedRooms++;
                    Assert.That(wave.Count(s => s.archetype == ShadeArchetype.Shepherd), Is.LessThanOrEqualTo(1), "Do not multiply bounded flocks into an unbounded encounter.");
                }
                Assert.That(formerMiniBosses, Is.GreaterThan(skeletons));
                Assert.That(variedRooms, Is.GreaterThanOrEqualTo(ascending ? 7 : 5));
                Assert.That(skeletons, Is.LessThanOrEqualTo(ascending ? 3 : 3));
            }
        }

        [TestCase(ShadeArchetype.MaskedMonk)]
        [TestCase(ShadeArchetype.WingedSentinel)]
        [TestCase(ShadeArchetype.Shepherd)]
        public void RecurringEnemiesHaveNoOverheadHealthBars(ShadeArchetype type)
        {
            var go = new GameObject("RecurringEnemyNoBar");
            try
            {
                go.AddComponent<ShadeEnemy>().SetupArchetype(type);
                Assert.That(go.GetComponentsInChildren<SpriteRenderer>(true).Any(r => r.name.StartsWith("DiscordBar") && r.enabled && r.gameObject.activeInHierarchy), Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [TestCase(BossIdentity.Tripod, 792f)]
        [TestCase(BossIdentity.MysticEyeWarden, 1008f)]
        [TestCase(BossIdentity.CerberusHoundOfHades, 1656f)]
        [TestCase(BossIdentity.WingedSentinel, 1188f)]
        [TestCase(BossIdentity.MaskedMonk, 864f)]
        [TestCase(BossIdentity.Shepherd, 1116f)]
        public void MajorBossHealthIsTwiceThePreviousConfiguredHealth(BossIdentity kind, float health)
        {
            var go = new GameObject("HealthierBoss");
            try
            {
                var boss = go.AddComponent<CerberusRhythmBoss>(); boss.ConfigureBoss(kind, Vector3.zero);
                Assert.That(boss.maxHealth, Is.EqualTo(health).Within(.01));
                Assert.That(boss.CurrentHealth, Is.EqualTo(health).Within(.01));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [TestCase(PaintedEnemyKind.Cerberus, 3.4f)]
        [TestCase(PaintedEnemyKind.HoodedSkeleton, 2.35f)]
        [TestCase(PaintedEnemyKind.MaskedMonk, 3.3f)]
        [TestCase(PaintedEnemyKind.MysticEyeWarden, 3.3f)]
        [TestCase(PaintedEnemyKind.WingedSentinel, 4.1f)]
        [TestCase(PaintedEnemyKind.RoosterRider, 2.8f)]
        [TestCase(PaintedEnemyKind.Tripod, 4.4f)]
        [TestCase(PaintedEnemyKind.Shepherd, 3.3f)]
        [TestCase(PaintedEnemyKind.Sheep, 1.35f)]
        public void EnemyArtworkIsTwentyPercentTaller(PaintedEnemyKind kind, float previousHeight)
        {
            var library = EnemyArtLibrary.Load(kind);
            Assert.That(library.visibleHeight, Is.EqualTo(previousHeight * 1.2f).Within(.001));
            var idle = Enumerable.Range(0,library.FrameCount(EnemyMotion.Idle)).Select(i=>library.Frame(EnemyMotion.Idle,i)).ToArray();
            Assert.That(MedianVisibleHeight(idle), Is.EqualTo(previousHeight * 1.2f).Within(.03));
        }

        [Test]
        public void EurydiceArtworkIsTwentyPercentTaller()
        {
            var library = Resources.Load<EurydiceAnimationLibrary>(EurydiceAnimationLibrary.ResourcePath);
            Assert.That(library.visibleHeight, Is.EqualTo(2.5274f * 1.2f).Within(.001));
            Assert.That(MedianVisibleHeight(library.idle.Take(4).ToArray()), Is.EqualTo(2.5274f * 1.2f).Within(.03));
        }

        private static float MedianVisibleHeight(Sprite[] frames)
        {
            var texture=frames[0].texture; var pixels=texture.GetPixels32(); var heights=new float[frames.Length];
            for(int i=0;i<frames.Length;i++)
            {
                var rect=frames[i].rect; int minY=(int)rect.yMax,maxY=(int)rect.yMin;
                for(int y=(int)rect.yMin;y<(int)rect.yMax;y++) for(int x=(int)rect.xMin;x<(int)rect.xMax;x++)
                    if(pixels[y*texture.width+x].a>100) { minY=Mathf.Min(minY,y); maxY=Mathf.Max(maxY,y); }
                heights[i]=(maxY-minY+1)/frames[i].pixelsPerUnit;
            }
            System.Array.Sort(heights); return heights[heights.Length/2];
        }

        [Test]
        public void RecurringEnemiesUseDistinctAuthoredSpawnPositions()
        {
            var go = new GameObject("VariedSpawnPositions");
            try
            {
                var wave = ProceduralChamberGenerator.GenerateChamber(go.transform,4,8,Vector3.zero).wave1;
                Assert.That(wave.Count, Is.GreaterThanOrEqualTo(3));
                for(int i=0;i<wave.Count;i++) for(int j=i+1;j<wave.Count;j++)
                    Assert.That(Vector2.Distance(wave[i].localOffsetXZ,wave[j].localOffsetXZ), Is.GreaterThan(1), "Recurring enemies must not stack at the old single mini-boss anchor.");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
