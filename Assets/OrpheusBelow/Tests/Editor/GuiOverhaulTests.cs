using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public class GuiOverhaulTests
    {
        private GameObject[] _before;
        [SetUp] public void SetUp() => _before = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        [TearDown] public void TearDown()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (!_before.Contains(go)) Object.DestroyImmediate(go);
        }

        [Test]
        public void IntroductionHasOnlyASmallBottomRightSkipCaption()
        {
            var root = new GameObject("IntroGuiContract");
            var intro = root.AddComponent<IntroCinematicPlayer>();
            Call(intro, "BuildView");
            Assert.That(root.GetComponentsInChildren<Button>(true), Is.Empty);
            Text caption = root.GetComponentsInChildren<Text>(true).Single(t => t.name == "IntroSkipCaption");
            Assert.That(caption.text, Is.EqualTo("Hold Enter to skip"));
            Assert.That(caption.font, Is.SameAs(GameTypography.Font));
            Assert.That(caption.rectTransform.anchorMin, Is.EqualTo(Vector2.right));
            Assert.That(caption.rectTransform.anchorMax, Is.EqualTo(Vector2.right));
            Assert.That(caption.rectTransform.anchoredPosition.x, Is.LessThan(0));
            Assert.That(caption.rectTransform.anchoredPosition.y, Is.GreaterThan(0));
            Assert.That(caption.fontSize, Is.InRange(16, 23));
            Image progress = root.transform.Find("IntroSkipProgress").GetComponent<Image>();
            Assert.That(progress.raycastTarget, Is.False);
            Assert.That(progress.rectTransform.anchorMin, Is.EqualTo(Vector2.right));
            Assert.That(progress.rectTransform.anchorMax, Is.EqualTo(Vector2.right));
            Assert.That(progress.rectTransform.anchoredPosition.x, Is.LessThan(0));
            Assert.That(progress.rectTransform.anchoredPosition.y, Is.GreaterThan(0));
            Assert.That(progress.rectTransform.sizeDelta.x, Is.Zero, "Progress stays discreet until Enter is held.");
            Assert.That(progress.rectTransform.sizeDelta.y, Is.InRange(1, 3));
        }

        [Test]
        public void SubtitlesUseOneReadableTypefaceAndGenerousTextSize()
        {
            var root = new GameObject("HudTypographyContract");
            var hud = root.AddComponent<OrpheusHud>();
            Call(hud, "Awake");
            Text speaker = root.GetComponentsInChildren<Text>(true).Single(t => t.name == "SubSpeaker");
            Text body = root.GetComponentsInChildren<Text>(true).Single(t => t.name == "SubBody");
            Assert.That(body.font, Is.SameAs(speaker.font));
            Assert.That(body.fontSize, Is.GreaterThanOrEqualTo(28));
            Assert.That(body.rectTransform.rect.width, Is.GreaterThanOrEqualTo(1000));
        }

        [TestCase(false, 17f, "12")]
        [TestCase(true, 20f, "4")]
        public void EnemyDamageTextShowsHealthActuallyLost(bool armored, float hit, string expected)
        {
            var root = new GameObject("DamageNumberTarget");
            var enemy = root.AddComponent<ShadeEnemy>();
            enemy.SetupArchetype(armored ? ShadeArchetype.ArmoredWarden : ShadeArchetype.WretchLunger);
            float before = enemy.CurrentHealth;
            enemy.OnOrphicHit(hit, .3f, false, Vector3.back);
            Text number = Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .SingleOrDefault(t => t.name == "EnemyDamageNumber");
            Assert.That(number, Is.Not.Null, "Every damaging hit must display its applied damage beside the enemy.");
            Assert.That(number.text, Is.EqualTo(expected));
            Assert.That(number.text, Is.EqualTo(Mathf.RoundToInt(before - enemy.CurrentHealth).ToString()));
        }

        [Test]
        public void OverkillDisplaysOnlyRemainingHealth()
        {
            var enemy = new GameObject("OverkillFeedbackTarget").AddComponent<ShadeEnemy>();
            enemy.SetupArchetype(ShadeArchetype.WretchLunger);
            float remaining = enemy.CurrentHealth;
            enemy.OnOrphicHit(10000f, 1f, true, Vector3.back);
            Text number = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t => t.name == "EnemyDamageNumber");
            Assert.That(number.text, Is.EqualTo(Mathf.RoundToInt(remaining).ToString()));
        }

        [Test]
        public void PlayerHudShowsNumericHealthBesideItsBar()
        {
            var root = new GameObject("NumericVitalsContract");
            var hud = root.AddComponent<OrpheusHud>();
            Call(hud, "Awake");
            Text value = root.GetComponentsInChildren<Text>(true).SingleOrDefault(t => t.name == "PlayerHealthValue");
            Assert.That(value, Is.Not.Null);
            Assert.That(value.text, Does.Contain("/"));
        }

        private static object Call(object target, string method)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
