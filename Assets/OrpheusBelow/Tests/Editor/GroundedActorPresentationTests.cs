using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class GroundedActorPresentationTests
    {
        private GameObject[] _before;
        private UnderworldCamera2D _cameraBefore;
        private Quaternion _billboardBefore;
        [SetUp] public void SetUp()
        {
            _before = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            _cameraBefore = UnderworldCamera2D.Instance;
            _billboardBefore = UnderworldCamera2D.CurrentBillboardRotation;
        }
        [TearDown] public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (!_before.Contains(go)) Object.DestroyImmediate(go);
            typeof(UnderworldCamera2D).GetProperty("Instance").SetValue(null, _cameraBefore);
            typeof(UnderworldCamera2D).GetProperty("CurrentBillboardRotation").SetValue(null, _billboardBefore);
        }

        [Test]
        public void PlayerShadowUsesVisibleSoftPaintAndItsCastExtendsAlongTheLightDirection()
        {
            var root = new GameObject("GroundedPlayer");
            var shadow = root.AddComponent<OrpheusGroundShadow>(); shadow.Initialize(null);
            AssertPaint(shadow.ContactRenderer);
            AssertPaint(shadow.AmbientRenderer);
            AssertPaint(shadow.CastRenderer);
            Vector3 offset = shadow.CastRenderer.transform.position - root.transform.position; offset.y = 0;
            Assert.That(Mathf.Abs(Vector3.Dot(shadow.CastRenderer.transform.right, offset.normalized)), Is.GreaterThan(.99f),
                "The cast's long axis must run away from the light, not across it.");
        }

        [Test]
        public void LowBossCameraRetainsAVisibleFlattenedFootprint()
        {
            var cameraRoot = new GameObject("LowBossCamera");
            var camera = cameraRoot.AddComponent<UnderworldCamera2D>(); Call(camera, "Awake");
            typeof(UnderworldCamera2D).GetProperty("SideViewBlend01").SetValue(camera, 1f);
            var root = new GameObject("GroundedStagePlayer");
            var shadow = root.AddComponent<OrpheusGroundShadow>(); shadow.Initialize(null);
            var contact = shadow.ContactRenderer;
            Assert.That(Vector3.Dot(contact.transform.forward, UnderworldCamera2D.CurrentBillboardRotation * Vector3.forward), Is.GreaterThan(.99f));
            Assert.That(contact.transform.localScale.y, Is.InRange(.07f, .2f));
            Assert.That(contact.transform.localScale.x, Is.GreaterThan(contact.transform.localScale.y * 3));
        }

        [Test]
        public void PlayerLosesAllShadowLayersWhenItsHealthReachesZero()
        {
            var root = new GameObject("DefeatedPlayer");
            var player = root.AddComponent<OrpheusPlayerController>();
            typeof(OrpheusPlayerController).GetProperty("CurrentHealth").SetValue(player, 100f);
            var shadow = root.AddComponent<OrpheusGroundShadow>(); shadow.Initialize(player);
            Assert.That(shadow.ContactRenderer.enabled, Is.True);
            typeof(OrpheusPlayerController).GetProperty("CurrentHealth").SetValue(player, 0f);
            shadow.ApplyState(false, false, null);
            Assert.That(shadow.ContactRenderer.enabled, Is.False);
            Assert.That(shadow.AmbientRenderer.enabled, Is.False);
            Assert.That(shadow.CastRenderer.enabled, Is.False);
        }

        [TestCase(PaintedEnemyKind.Cerberus)] [TestCase(PaintedEnemyKind.Tripod)]
        [TestCase(PaintedEnemyKind.MysticEyeWarden)] [TestCase(PaintedEnemyKind.WingedSentinel)]
        [TestCase(PaintedEnemyKind.MaskedMonk)] [TestCase(PaintedEnemyKind.Shepherd)]
        [TestCase(PaintedEnemyKind.HoodedSkeleton)] [TestCase(PaintedEnemyKind.RoosterRider)]
        [TestCase(PaintedEnemyKind.Sheep)]
        public void EveryEnemyHasContactAndDirectionalPaintThatFollowsItsGroundFoot(PaintedEnemyKind kind)
        {
            var root = new GameObject("GroundedEnemy");
            var actor = root.AddComponent<PaintedEnemyActor>(); actor.Configure(kind, createPattern: false);
            root.transform.position = new Vector3(4, 2, -3);
            Call(root.GetComponent<EnemyGroundShadow>(), "LateUpdate");
            var contact = root.transform.Find("EnemyGroundShadow").GetComponent<SpriteRenderer>();
            AssertPaint(contact);
            Assert.That(contact.transform.position.x, Is.EqualTo(4).Within(.001));
            Assert.That(contact.transform.position.z, Is.EqualTo(-3).Within(.001));
            Assert.That(contact.transform.position.y, Is.InRange(.01f, .08f));
            Assert.That(contact.transform.localScale.x, Is.InRange(.5f, 4f));
            var cast = root.transform.Find("EnemyCastShadow");
            Assert.That(cast, Is.Not.Null);
            AssertPaint(cast.GetComponent<SpriteRenderer>());
            Assert.That(contact.sortingOrder, Is.LessThan(actor.Art.Renderer.sortingOrder));
        }

        [Test]
        public void EurydiceHasGroundedContactAndLosesItsShadowWhenSheVanishes()
        {
            var root = new GameObject("GroundedCompanion");
            var companion = root.AddComponent<BlurredEurydice>(); Call(companion, "Start");
            var contact = root.GetComponentsInChildren<SpriteRenderer>().SingleOrDefault(r => r.name == "EurydiceContactShadow");
            Assert.That(contact, Is.Not.Null);
            AssertPaint(contact);
            companion.ShatterIntoAsh();
            Assert.That(contact.enabled, Is.False);
        }

        [Test]
        public void DefeatedEnemyStopsCastingAShadow()
        {
            var root = new GameObject("DefeatedEnemy");
            var shade = root.AddComponent<ShadeEnemy>(); shade.SetupArchetype(ShadeArchetype.MaskedMonk);
            var contact = root.transform.Find("EnemyGroundShadow").GetComponent<SpriteRenderer>();
            Assert.That(contact.enabled, Is.True);
            typeof(ShadeEnemy).GetField("_hp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(shade, 0f);
            Call(root.GetComponent<EnemyGroundShadow>(), "LateUpdate");
            Assert.That(contact.enabled, Is.False);
        }

        [Test]
        public void PaintedActorDepthKeepsShadowPaintTranslucentWhileOccludingTheBody()
        {
            var originalMap = PaintedChamberMap.Active;
            var mapRoot = new GameObject("ShadowDepthMap");
            var map = mapRoot.AddComponent<PaintedChamberMap>();
            map.definition = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_00");
            map.MakeActive();
            typeof(UnderworldCamera2D).GetProperty("Instance").SetValue(null, null);
            try
            {
                var root = new GameObject("ShadowDepthEnemy");
                var actor = root.AddComponent<PaintedEnemyActor>(); actor.Configure(PaintedEnemyKind.MaskedMonk, createPattern: false);
                actor.Art.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
                var depth = root.GetComponent<PaintedActorDepth>(); Call(depth, "Awake"); Call(depth, "LateUpdate");
                Assert.That(actor.Art.Renderer.sharedMaterial.shader.name, Is.EqualTo("OrpheusBelow/PaintedCover"));
                AssertPaint(root.transform.Find("EnemyGroundShadow").GetComponent<SpriteRenderer>());
                AssertPaint(root.transform.Find("EnemyCastShadow").GetComponent<SpriteRenderer>());
            }
            finally
            {
                Object.DestroyImmediate(mapRoot);
                if (originalMap != null) originalMap.MakeActive();
            }
        }

        [Test]
        public void EnemyVitalsKeepHealthFillWithoutNumericReadoutAndDamageNumbersRemain()
        {
            var root = new GameObject("EnemyVitals");
            var shade = root.AddComponent<ShadeEnemy>();
            EnemyCombatFeedback.Track(shade);
            var feedback = Object.FindFirstObjectByType<EnemyCombatFeedback>();
            Assert.That(feedback.GetComponentsInChildren<Image>(true).Any(r => r.name == "HealthFill"), Is.True);
            Assert.That(feedback.GetComponentsInChildren<Text>(true).Any(r => r.name == "EnemyHealthValue"), Is.False);
            EnemyCombatFeedback.Damage(root.transform, 12f);
            Assert.That(feedback.GetComponentsInChildren<Text>(true).Single(r => r.name == "EnemyDamageNumber").text, Is.EqualTo("12"));
        }

        private static void AssertPaint(SpriteRenderer renderer)
        {
            Assert.That(renderer.enabled, Is.True);
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("OrpheusBelow/GroundShadow"));
            Assert.That(renderer.color.a, Is.GreaterThan(.15f));
            Assert.That(renderer.sprite, Is.Not.Null);
        }
        private static void Call(object target, string method)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
