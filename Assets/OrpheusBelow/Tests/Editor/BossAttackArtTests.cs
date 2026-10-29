using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class BossAttackArtTests : CombatGameplayTestContext
    {
        private GameObject[] _before;
        private OrpheusPlayerController _previous;
        [SetUp] public void SetUp()
        {
            _before = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            _previous = OrpheusPlayerController.Instance;
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, null);
        }
        [TearDown] public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (!_before.Contains(go)) Object.DestroyImmediate(go);
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, _previous);
        }

        [TestCase(BossIdentity.Tripod)]
        [TestCase(BossIdentity.MysticEyeWarden)]
        [TestCase(BossIdentity.WingedSentinel)]
        [TestCase(BossIdentity.MaskedMonk)]
        [TestCase(BossIdentity.Shepherd)]
        [TestCase(BossIdentity.CerberusHoundOfHades)]
        public void GuardianAttacksUseTransparentPaintWithAnOpaqueLeadingCore(BossIdentity id)
        {
            var wave = StageSweepWave2D.Spawn(Vector3.right * 5, Vector3.zero, 10, id);
            var sprite = wave.ArtRenderer.sprite;
            Assert.That(wave.Identity, Is.EqualTo(id));
            Assert.That(sprite, Is.EqualTo(BossAttackArt.SpriteFor(id)));
            Assert.That(sprite, Is.Not.EqualTo(RenderPipeline2DSetup.DiamondSprite));
            var pixels = sprite.texture.GetPixels32();
            Assert.That(pixels.Count(p => p.a < 10), Is.GreaterThan(pixels.Length / 5), "A painted wash must not become an opaque rectangle.");
            int x = Mathf.Clamp(Mathf.RoundToInt(sprite.pivot.x), 0, sprite.texture.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(sprite.pivot.y), 0, sprite.texture.height - 1);
            Assert.That(pixels[y * sprite.texture.width + x].a, Is.GreaterThan(100), "The damaging anchor must sit in painted attack mass.");
            var eruption = StageGeyserHazard2D.Spawn(Vector3.zero, id);
            Assert.That(eruption.ArtRenderer.sprite, Is.EqualTo(sprite));
        }

        [Test] public void SixGuardiansHaveSixDifferentPaintedProjectiles()
        {
            var ids = new[] { BossIdentity.Tripod, BossIdentity.MysticEyeWarden, BossIdentity.WingedSentinel,
                BossIdentity.MaskedMonk, BossIdentity.Shepherd, BossIdentity.CerberusHoundOfHades };
            Assert.That(ids.Select(BossAttackArt.SpriteFor).Distinct().Count(), Is.EqualTo(6));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FastPaintedWaveSweepsThePlayerButRespectsDashImmunity(bool dashing)
        {
            var go = new GameObject("ProjectileSweepPlayer"); var player = go.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, player);
            typeof(OrpheusPlayerController).GetProperty("CurrentHealth").SetValue(player, 100f);
            typeof(OrpheusPlayerController).GetProperty("IsIn2DBossStage").SetValue(player, true);
            typeof(OrpheusPlayerController).GetField("_isDashing", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, dashing);
            var wave = StageSweepWave2D.Spawn(UnderworldCamera2D.IsoRightXZ * 5, Vector3.zero, 20, BossIdentity.Shepherd);
            wave.Advance(0);
            Assert.That(player.CurrentHealth, Is.EqualTo(100));
            wave.Advance(.5f);
            Assert.That(player.CurrentHealth, Is.EqualTo(dashing ? 100 : 76));
        }

        [Test] public void EruptionKeepsItsWarningAndDamageTiming()
        {
            var go = new GameObject("EruptionPlayer"); var player = go.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null, player);
            typeof(OrpheusPlayerController).GetProperty("CurrentHealth").SetValue(player, 100f);
            var eruption = StageGeyserHazard2D.Spawn(Vector3.zero, BossIdentity.MaskedMonk);
            for (int i = 0; i < 7; i++) eruption.Advance(.1f);
            Assert.That(player.CurrentHealth, Is.EqualTo(100));
            eruption.Advance(.1f);
            Assert.That(player.CurrentHealth, Is.EqualTo(75));
            Assert.That(Object.FindObjectsByType<PaintedAttackShadow>(FindObjectsSortMode.None), Is.Empty);
        }
    }
}
