using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class EncounterWiringTests
    {
        [Test]
        public void CameraStartupPreservesAnImmediateBossView()
        {
            var go = new GameObject("ImmediateBossCameraTest");
            var target = new GameObject("CameraTarget");
            try
            {
                go.AddComponent<Camera>();
                var camera = go.AddComponent<UnderworldCamera2D>();
                camera.target = target.transform;
                camera.EnterSide2DBossView(Vector3.zero);
                typeof(UnderworldCamera2D).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(camera, null);
                Assert.That((bool)typeof(UnderworldCamera2D).GetField("_inSide2DBossMode", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(camera), Is.True);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(target); }
        }

        [Test]
        public void SceneUnloadCanDisposeAnAlreadyDestroyedWarning()
        {
            var warning = PaintedAttackShadow.Create(Vector3.zero, 2, 1);
            Object.DestroyImmediate(warning.gameObject);
            Assert.DoesNotThrow(() => warning.Dispose());
        }

        [SetUp] public void ResetJourney() => UnderworldJourney.Reset();
        [TestCase(2,"MaskedMonk")]
        [TestCase(4,"WingedSentinel")]
        [TestCase(5,"Shepherd")]
        public void MiniBossIsPresentInTheFirstActualChamberWave(int depth,string creature)
        {
            var root = new GameObject("EncounterWiring");
            try
            {
                var room = ProceduralChamberGenerator.GenerateChamber(root.transform,depth,8,Vector3.zero);
                Assert.That(room.wave1.Exists(s=>s.archetype.ToString()==creature),Is.True,creature+" is missing from the normal spawn roster.");
                Assert.That(room.guardianBoss,Is.Null,"Mini-bosses fight on the painted chamber floor.");
                Assert.That(room.wave2,Is.Empty,"The unique encounter must not be buried under a second generic wave.");
            }
            finally { Object.DestroyImmediate(root); }
        }
        [TestCase(1,BossIdentity.Tripod)]
        [TestCase(3,BossIdentity.MysticEyeWarden)]
        [TestCase(6,BossIdentity.CerberusHoundOfHades)]
        public void MajorBossHasNoGenericWavesBlockingItsArrival(int depth,BossIdentity expected)
        {
            var root=new GameObject("BossEntryWiring");
            try
            {
                var room=ProceduralChamberGenerator.GenerateChamber(root.transform,depth,8,Vector3.zero);
                Assert.That(room.guardianBoss.identity,Is.EqualTo(expected));
                Assert.That(room.wave1,Is.Empty,"Major bosses must appear on entry.");
                Assert.That(room.wave2,Is.Empty);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(ShadeArchetype.MaskedMonk,PaintedEnemyKind.MaskedMonk)]
        [TestCase(ShadeArchetype.WingedSentinel,PaintedEnemyKind.WingedSentinel)]
        [TestCase(ShadeArchetype.Shepherd,PaintedEnemyKind.Shepherd)]
        public void NormalSpawnCreatesTheCorrectMiniBossArtAndAttack(ShadeArchetype archetype,PaintedEnemyKind kind)
        {
            var go=new GameObject("ActualMiniBossSpawn");
            try
            {
                var shade=go.AddComponent<ShadeEnemy>(); shade.SetupArchetype(archetype);
                Assert.That(shade.IsMiniBoss,Is.True);
                Assert.That(shade.PaintedActor.Kind,Is.EqualTo(kind));
                Assert.That(shade.PaintedActor.Pattern.GetType().Name,Is.EqualTo(kind+"Attack"));
                Assert.That(shade.PaintedActor.Art.IsReady,Is.True);
                Assert.That(shade.maxHealth,Is.GreaterThanOrEqualTo(200));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test]
        public void LegacyMeleeNoLongerSilentlyUsesSkeletonArt()
        {
            foreach(var type in new[]{ShadeArchetype.WretchLunger,ShadeArchetype.ArmoredWarden})
                Assert.That(ChamberEncounterPlan.ArtFor(type),Is.Not.EqualTo(PaintedEnemyKind.HoodedSkeleton));
        }
        [Test]
        public void ReturnRoostersAreSpawnedAndPacifiedMiniBossesStayAbsent()
        {
            for(int depth=0;depth<7;depth++)
            {
                var roster=ChamberEncounterPlan.Wave(depth,1,true);
                Assert.That(roster.Exists(s=>s.archetype==ShadeArchetype.MourningAbductor),Is.True);
                Assert.That(roster.Exists(s=>ChamberEncounterPlan.IsMiniBoss(s.archetype)),Is.False);
            }
        }
    }
}
