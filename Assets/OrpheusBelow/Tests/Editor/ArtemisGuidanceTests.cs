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
        public void NarratorReplacesArtemisWithoutPausingGameplay()
        {
            var show = typeof(OrpheusHud).GetMethod("ShowArtemisTutorial");
            Assert.That(show, Is.Not.Null, "Tutorial lessons must be accessible.");
            var root = new GameObject("NarratorLessonTest");
            float timeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1.0f;
                var companion = root.AddComponent<BlurredEurydice>();
                var hud = root.AddComponent<OrpheusHud>();
                EnsureAwake(hud);

                bool confirmed = false;
                show.Invoke(hud, new object[] { "Clear chambers to move further into the underworld! Move with W, A, S, D.", (Action)(() => confirmed = true) });

                // Narrator replaces Artemis
                Assert.That(typeof(OrpheusHud).GetProperty("CurrentTutorialSpeaker")?.GetValue(hud), Is.EqualTo("Narrator"));

                // Game no longer pauses on dialogue!
                Assert.That(Time.timeScale, Is.EqualTo(1.0f));
                Assert.That(OrpheusHud.IsTutorialModalActive, Is.False, "Dialogue boxes must be removed.");

                // Subtitle root exists and is updated
                var subRoot = typeof(OrpheusHud).GetField("_subtitleRoot", PrivateInstance)?.GetValue(hud) as GameObject;
                Assert.That(subRoot, Is.Not.Null, "Subtitle root must be instantiated.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                Time.timeScale = timeScale;
            }
        }

        [Test]
        public void CompanionSpeechAndHadesDialogueDisplaySubtitlesInRealTime()
        {
            var root = new GameObject("VoiceDialogueTest");
            float timeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1.0f;
                var companion = root.AddComponent<BlurredEurydice>();
                var hud = root.AddComponent<OrpheusHud>();
                EnsureAwake(hud);

                hud.ShowEurydiceTutorial("I am here, Orpheus. Right behind you.");
                Assert.That(typeof(OrpheusHud).GetProperty("CurrentTutorialSpeaker")?.GetValue(hud), Is.EqualTo("Eurydice"));
                Assert.That(Time.timeScale, Is.EqualTo(1.0f));

                hud.ShowHadesTutorial("A breath of living air stumbles down my halls.");
                Assert.That(typeof(OrpheusHud).GetProperty("CurrentTutorialSpeaker")?.GetValue(hud), Is.EqualTo("Hades"));
                Assert.That(Time.timeScale, Is.EqualTo(1.0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                Time.timeScale = timeScale;
            }
        }

        private static void EnsureAwake(OrpheusHud hud)
        {
            if (typeof(OrpheusHud).GetField("_serifFont", PrivateInstance).GetValue(hud) == null)
                typeof(OrpheusHud).GetMethod("Awake", PrivateInstance).Invoke(hud, null);
        }
    }
}
