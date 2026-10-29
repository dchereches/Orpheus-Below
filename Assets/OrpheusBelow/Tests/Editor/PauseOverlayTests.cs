using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class PauseOverlayTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private UnderworldFrontEnd _menu;
        private float _previousScale;
        private bool _previousAudioPause;
        private bool _hadBrightness;
        private float _previousBrightness;
        private UnderworldCamera2D _previousCamera;

        [SetUp]
        public void SetUp()
        {
            _previousScale = Time.timeScale;
            _previousAudioPause = AudioListener.pause;
            _hadBrightness = PlayerPrefs.HasKey("Orpheus_Brightness");
            _previousBrightness = PlayerPrefs.GetFloat("Orpheus_Brightness", 1f);
            _previousCamera = UnderworldCamera2D.Instance;
            _root = new GameObject("PauseOverlayContract");
            _menu = _root.AddComponent<UnderworldFrontEnd>();
            Set("_serif", GameTypography.Font);
            Set("_sans", GameTypography.Font);
            Call("EnsureEventSystem");
            Call("BuildCanvas");
            Set("_inRun", true);
        }

        [TearDown]
        public void TearDown()
        {
            var shade = Get<Texture2D>("_shadeTexture");
            Object.DestroyImmediate(_root);
            if (shade != null) Object.DestroyImmediate(shade);
            typeof(UnderworldCamera2D).GetProperty("Instance").SetValue(null, _previousCamera);
            Time.timeScale = _previousScale;
            AudioListener.pause = _previousAudioPause;
            if (_hadBrightness) PlayerPrefs.SetFloat("Orpheus_Brightness", _previousBrightness);
            else PlayerPrefs.DeleteKey("Orpheus_Brightness");
        }

        [Test]
        public void PausingKeepsCurrentSceneCameraAndActorPoseBehindCenteredControls()
        {
            var camera = new GameObject("CurrentChamberCamera").AddComponent<Camera>();
            var follow = camera.gameObject.AddComponent<UnderworldCamera2D>();
            typeof(UnderworldCamera2D).GetProperty("Instance").SetValue(null, follow);
            camera.transform.SetParent(_root.transform);
            camera.transform.SetPositionAndRotation(new Vector3(3, 5, -7), Quaternion.Euler(28, 12, 0));
            var actor = new GameObject("CurrentChamberActor");
            actor.transform.SetParent(_root.transform);
            actor.transform.position = new Vector3(-2, 1, 4);
            var scene = SceneManager.GetActiveScene();
            Vector3 cameraPosition = camera.transform.position;
            Quaternion cameraRotation = camera.transform.rotation;
            Vector3 actorPosition = actor.transform.position;

            _menu.TogglePause();

            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(AudioListener.pause, Is.True);
            Assert.That(UnderworldFrontEnd.BlocksMenusOrTransitions, Is.True);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(scene));
            Assert.That(camera.enabled && camera.gameObject.activeInHierarchy, Is.True);
            Assert.That(follow.enabled, Is.False, "Unscaled camera follow and shake must freeze while rendering the current chamber.");
            Assert.That(camera.transform.position, Is.EqualTo(cameraPosition));
            Assert.That(camera.transform.rotation, Is.EqualTo(cameraRotation));
            Assert.That(actor.activeInHierarchy, Is.True);
            Assert.That(actor.transform.position, Is.EqualTo(actorPosition));
            Assert.That(Get<GameObject>("_titleBackdrop").activeSelf, Is.False);
            Assert.That(Get<GameObject>("_main").activeSelf, Is.False);
            RectTransform panel = Get<GameObject>("_pause").GetComponent<RectTransform>();
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(panel.anchorMin, Is.EqualTo(Vector2.one * .5f));
            Assert.That(panel.anchorMax, Is.EqualTo(Vector2.one * .5f));
            Assert.That(panel.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(panel.GetComponent<Image>().color.a, Is.InRange(.75f, .96f));
            Assert.That(MenuEvents.currentSelectedGameObject, Is.SameAs(Get<Button>("_pauseResume").gameObject));
        }

        [Test]
        public void PauseSettingsReturnToSameOverlayWithControllerFocusAndRestartReachable()
        {
            Call("OpenMenu");
            Call("ShowSettings");
            Assert.That(Get<GameObject>("_settings").activeSelf, Is.True);
            Assert.That(Get<GameObject>("_pause").activeSelf, Is.False);
            Assert.That(Get<GameObject>("_titleBackdrop").activeSelf, Is.False);
            Call("HideSettings");
            Assert.That(Get<GameObject>("_pause").activeSelf, Is.True);
            Assert.That(Get<GameObject>("_main").activeSelf, Is.False);
            Assert.That(MenuEvents.currentSelectedGameObject, Is.SameAs(Get<Button>("_pauseSettings").gameObject));
            Button resume = Get<Button>("_pauseResume");
            Button settings = Get<Button>("_pauseSettings");
            Button restart = Get<Button>("_pauseRestart");
            Button leave = Get<Button>("_pauseLeave");
            Assert.That(resume.navigation.selectOnDown, Is.SameAs(settings));
            Assert.That(settings.navigation.selectOnDown, Is.SameAs(restart));
            Assert.That(restart.navigation.selectOnDown, Is.SameAs(leave));
            Assert.That(leave.navigation.selectOnDown, Is.SameAs(resume));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(AudioListener.pause, Is.True);
        }

        [Test]
        public void TitleSettingsReturnToTitlePresentation()
        {
            Set("_inRun", false);
            Call("OpenMenu");
            Call("ShowSettings");
            Call("HideSettings");
            Assert.That(Get<GameObject>("_titleBackdrop").activeSelf, Is.True);
            Assert.That(Get<GameObject>("_main").activeSelf, Is.True);
            Assert.That(Get<GameObject>("_pause").activeSelf, Is.False);
            Assert.That(MenuEvents.currentSelectedGameObject, Is.SameAs(Get<Button>("_settingsBtn").gameObject));
        }

        [TestCase(SensorSourceMode.KeyboardPlusPCMicrophone, .8f)]
        [TestCase(SensorSourceMode.KeyboardAndMouseOnly, 1f)]
        public void ResumingPreservesSelectedInputSpeedAndReleasesMenuPause(SensorSourceMode mode, float expected)
        {
            SensorSourceMode previous = SensorPulseInput.GlobalSourceMode;
            try
            {
                typeof(SensorPulseInput).GetProperty("GlobalSourceMode").SetValue(null, mode);
                var follow = new GameObject("ResumingChamberCamera").AddComponent<UnderworldCamera2D>();
                follow.transform.SetParent(_root.transform);
                typeof(UnderworldCamera2D).GetProperty("Instance").SetValue(null, follow);
                Call("OpenMenu");
                Assert.That(follow.enabled, Is.False);
                // Complete the resume coroutine synchronously; its visual fade is verified in Play Mode.
                IEnumerator routine = (IEnumerator)Call("FadeOutBeforeResume");
                while (routine.MoveNext()) { }
                Assert.That(Time.timeScale, Is.EqualTo(expected));
                Assert.That(AudioListener.pause, Is.False);
                Assert.That(Get<GameObject>("_canvasObject").activeSelf, Is.False);
                Assert.That(follow.enabled, Is.True);
            }
            finally { typeof(SensorPulseInput).GetProperty("GlobalSourceMode").SetValue(null, previous); }
        }

        // EditMode does not run EventSystem.OnEnable, which registers EventSystem.current.
        // Assert focus on the actual instance retained and selected by the front end.
        private EventSystem MenuEvents => Get<EventSystem>("_events");
        private object Call(string name) => typeof(UnderworldFrontEnd).GetMethod(name, Hidden).Invoke(_menu, null);
        private T Get<T>(string name) => (T)typeof(UnderworldFrontEnd).GetField(name, Hidden).GetValue(_menu);
        private void Set(string name, object value) => typeof(UnderworldFrontEnd).GetField(name, Hidden).SetValue(_menu, value);
    }
}
