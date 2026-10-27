using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace OrpheusBelow.Tests
{
    public class ArtemisGuidanceTests
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void ArtemisLessonUsesHerPortraitAndNeverAnimatesEurydiceAsTheSpeaker()
        {
            var show = typeof(OrpheusHud).GetMethod("ShowArtemisTutorial");
            Assert.That(show, Is.Not.Null, "Control lessons must be spoken by Artemis.");
            var root = new GameObject("ArtemisLessonTest");
            float timeScale = Time.timeScale;
            try
            {
                var companion = root.AddComponent<BlurredEurydice>();
                var hud = root.AddComponent<OrpheusHud>();
                EnsureAwake(hud);
                bool confirmed = false;
                show.Invoke(hud, new object[] { "Descend alone. I will guide your song.", (Action)(() => confirmed = true) });
                Assert.That(typeof(OrpheusHud).GetProperty("CurrentTutorialSpeaker")?.GetValue(hud), Is.EqualTo("Artemis"));
                Assert.That(companion.IsTeachingPose, Is.False);
                Assert.That(OrpheusHud.IsTutorialModalActive, Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                var portrait = root.GetComponentInChildren<RawImage>(true);
                Assert.That(portrait, Is.Not.Null);
                Assert.That(portrait.texture, Is.Not.Null);
                Assert.That(portrait.texture, Is.SameAs(Resources.Load<Texture2D>("Characters/Artemis/GuidePortrait")));
                hud.DismissTutorialModal();
                Assert.That(confirmed, Is.True);
                Assert.That(OrpheusHud.IsTutorialModalActive, Is.False);
                Assert.That(companion.IsTeachingPose, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); Time.timeScale = timeScale; }
        }

        [Test]
        public void CompanionSpeechRetainsEurydiceAndReleasesHerPoseWhenArtemisSpeaks()
        {
            var show = typeof(OrpheusHud).GetMethod("ShowArtemisTutorial");
            Assert.That(show, Is.Not.Null);
            var root = new GameObject("CompanionLessonTest");
            float timeScale = Time.timeScale;
            try
            {
                var companion = root.AddComponent<BlurredEurydice>();
                var hud = root.AddComponent<OrpheusHud>();
                EnsureAwake(hud);
                hud.ShowEurydiceTutorial("I am here. Let us return to the light.");
                Assert.That(typeof(OrpheusHud).GetProperty("CurrentTutorialSpeaker")?.GetValue(hud), Is.EqualTo("Eurydice"));
                Assert.That(companion.IsTeachingPose, Is.True);
                var portrait = root.GetComponentInChildren<RawImage>(true);
                Assert.That(portrait.texture, Is.Not.Null);
                Assert.That(portrait.texture, Is.Not.SameAs(Resources.Load<Texture2D>("Characters/Artemis/GuidePortrait")));
                show.Invoke(hud, new object[] { "Guard her on the ascent.", null });
                Assert.That(companion.IsTeachingPose, Is.False);
                Assert.That(typeof(OrpheusHud).GetProperty("CurrentTutorialSpeaker")?.GetValue(hud), Is.EqualTo("Artemis"));
                hud.HideEurydiceTutorial();
                Assert.That(OrpheusHud.IsTutorialModalActive, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); Time.timeScale = timeScale; }
        }

        private static void EnsureAwake(OrpheusHud hud)
        {
            if (typeof(OrpheusHud).GetField("_tutorialBodyText", PrivateInstance).GetValue(hud) == null)
                typeof(OrpheusHud).GetMethod("Awake", PrivateInstance).Invoke(hud, null);
        }
    }
}
