using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class WorldLightingSettingsTests : CombatGameplayTestContext
    {
        private readonly string[] _keys = { "Orpheus_LightingExposure", "Orpheus_LightingContrast", "Orpheus_LightingBloom", "Orpheus_Brightness" };
        private bool[] _had;
        private float[] _saved;

        [SetUp]
        public void SetUp()
        {
            _had = _keys.Select(PlayerPrefs.HasKey).ToArray();
            _saved = _keys.Select(k => PlayerPrefs.GetFloat(k)).ToArray();
            foreach (string key in _keys) PlayerPrefs.DeleteKey(key);
            Reload();
        }

        [TearDown]
        public void TearDown()
        {
            GameObject volume = GameObject.Find(WorldLightingSettings.VolumeName);
            if (volume != null) Object.DestroyImmediate(volume);
            for (int i = 0; i < _keys.Length; i++)
                if (_had[i]) PlayerPrefs.SetFloat(_keys[i], _saved[i]);
                else PlayerPrefs.DeleteKey(_keys[i]);
            PlayerPrefs.Save();
            Reload();
        }

        [Test]
        public void NewInstallationKeepsNeutralExposureAndExistingBloomDefault()
        {
            Assert.That(WorldLightingSettings.Exposure, Is.Zero);
            Assert.That(WorldLightingSettings.Contrast, Is.Zero);
            Assert.That(WorldLightingSettings.Bloom, Is.EqualTo(.2f));
        }

        [Test]
        public void SettingsClampPersistReloadAndReset()
        {
            WorldLightingSettings.SetExposure(9f);
            WorldLightingSettings.SetContrast(-90f);
            WorldLightingSettings.SetBloom(9f);
            Reload();
            Assert.That(WorldLightingSettings.Exposure, Is.EqualTo(1f));
            Assert.That(WorldLightingSettings.Contrast, Is.EqualTo(-40f));
            Assert.That(WorldLightingSettings.Bloom, Is.EqualTo(1f));
            WorldLightingSettings.ResetDefaults();
            Reload();
            Assert.That(WorldLightingSettings.Exposure, Is.Zero);
            Assert.That(WorldLightingSettings.Contrast, Is.Zero);
            Assert.That(WorldLightingSettings.Bloom, Is.EqualTo(.2f));
        }

        [Test]
        public void LegacyBrightnessMigratesOnceAndInvalidPreferencesStayNeutral()
        {
            PlayerPrefs.SetFloat("Orpheus_Brightness", 1.5f);
            Reload();
            Assert.That(WorldLightingSettings.Exposure, Is.EqualTo(Mathf.Log(1.5f, 2f)).Within(.0001f));
            WorldLightingSettings.SetExposure(-.4f);
            Reload();
            Assert.That(WorldLightingSettings.Exposure, Is.EqualTo(-.4f));
            PlayerPrefs.SetFloat("Orpheus_LightingExposure", float.NaN);
            PlayerPrefs.SetFloat("Orpheus_LightingContrast", float.PositiveInfinity);
            PlayerPrefs.SetFloat("Orpheus_LightingBloom", float.NegativeInfinity);
            Reload();
            Assert.That(WorldLightingSettings.Exposure, Is.Zero);
            Assert.That(WorldLightingSettings.Contrast, Is.Zero);
            Assert.That(WorldLightingSettings.Bloom, Is.EqualTo(.2f));
        }

        [Test]
        public void RuntimeVolumeOverridesActualHdrpParametersAndIsReused()
        {
            WorldLightingSettings.SetExposure(.65f);
            WorldLightingSettings.SetContrast(23f);
            WorldLightingSettings.SetBloom(.42f);
            WorldLightingSettings.EnsureApplied();
            Volume volume = GameObject.Find(WorldLightingSettings.VolumeName).GetComponent<Volume>();
            Assert.That(volume.isGlobal, Is.True);
            Assert.That(volume.priority, Is.EqualTo(510f));
            Assert.That(volume.weight, Is.EqualTo(1f));
            AssertParameter(volume, "Exposure", "fixedExposure", 0f);
            AssertParameter(volume, "ColorAdjustments", "postExposure", .65f);
            AssertParameter(volume, "ColorAdjustments", "contrast", 23f);
            AssertParameter(volume, "Bloom", "intensity", .42f);
            Assert.That(volume.sharedProfile.components.Any(c => c.GetType().Name == "LensDistortion"), Is.False);
            WorldLightingSettings.EnsureApplied();
            Assert.That(GameObject.Find(WorldLightingSettings.VolumeName).GetComponent<Volume>(), Is.SameAs(volume));
            WorldLightingSettings.SetExposure(-.5f);
            AssertParameter(volume, "ColorAdjustments", "postExposure", -.5f);
        }

        [Test]
        public void PauseSettingsLightingSlidersResetAndControllerNavigationStayUsable()
        {
            const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            GameObject root = new GameObject("LightingSettingsMenuContract");
            GameObject[] before = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            var menu = root.AddComponent<UnderworldFrontEnd>();
            try
            {
                typeof(UnderworldFrontEnd).GetField("_serif", hidden).SetValue(menu, GameTypography.Font);
                typeof(UnderworldFrontEnd).GetField("_sans", hidden).SetValue(menu, GameTypography.Font);
                typeof(UnderworldFrontEnd).GetMethod("EnsureEventSystem", hidden).Invoke(menu, null);
                typeof(UnderworldFrontEnd).GetMethod("BuildCanvas", hidden).Invoke(menu, null);
                typeof(UnderworldFrontEnd).GetField("_inRun", hidden).SetValue(menu, true);
                typeof(UnderworldFrontEnd).GetMethod("ShowSettings", hidden).Invoke(menu, null);
                Transform settings = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "SettingsSheet");
                Slider exposure = settings.Find("LightingExposureSlider").GetComponent<Slider>();
                Slider contrast = settings.Find("LightingContrastSlider").GetComponent<Slider>();
                Slider bloom = settings.Find("LightingBloomSlider").GetComponent<Slider>();
                Button reset = settings.Find("ResetLightingBtn").GetComponent<Button>();
                exposure.value = .5f;
                contrast.value = 12f;
                bloom.value = .7f;
                Assert.That(WorldLightingSettings.Exposure, Is.EqualTo(.5f));
                Assert.That(WorldLightingSettings.Contrast, Is.EqualTo(12f));
                Assert.That(WorldLightingSettings.Bloom, Is.EqualTo(.7f));
                Assert.That(exposure.navigation.selectOnDown, Is.SameAs(contrast));
                Assert.That(contrast.navigation.selectOnDown, Is.SameAs(bloom));
                Assert.That(bloom.navigation.selectOnDown, Is.SameAs(reset));
                reset.onClick.Invoke();
                Assert.That(exposure.value, Is.Zero);
                Assert.That(contrast.value, Is.Zero);
                Assert.That(bloom.value, Is.EqualTo(.2f));
                Assert.That(settings.GetComponentsInChildren<Text>(true).All(t => t.font == GameTypography.Font), Is.True);
                Assert.That(root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "BrightnessVeil"), Is.False);

                Selectable[] controls = settings.GetComponentsInChildren<Selectable>(true);
                for (int i = 0; i < controls.Length; i++)
                {
                    var rt = controls[i].GetComponent<RectTransform>();
                    var bounds = new Rect(rt.anchoredPosition.x, -rt.anchoredPosition.y, rt.sizeDelta.x, rt.sizeDelta.y);
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(720f), controls[i].name);
                    for (int j = i + 1; j < controls.Length; j++)
                    {
                        var other = controls[j].GetComponent<RectTransform>();
                        var otherBounds = new Rect(other.anchoredPosition.x, -other.anchoredPosition.y, other.sizeDelta.x, other.sizeDelta.y);
                        Assert.That(bounds.Overlaps(otherBounds), Is.False, controls[i].name + " overlaps " + controls[j].name);
                    }
                }
            }
            finally
            {
                var shade = (Texture2D)typeof(UnderworldFrontEnd).GetField("_shadeTexture", hidden).GetValue(menu);
                Object.DestroyImmediate(root);
                if (shade != null) Object.DestroyImmediate(shade);
                foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                    if (!before.Contains(go)) Object.DestroyImmediate(go);
            }
        }

        private static void AssertParameter(Volume volume, string componentName, string parameterName, float expected)
        {
            VolumeComponent component = volume.sharedProfile.components.Single(c => c.GetType().Name == componentName);
            Assert.That(component.active, Is.True);
            var parameter = (VolumeParameter)component.GetType().GetField(parameterName).GetValue(component);
            Assert.That(parameter.overrideState, Is.True);
            object actual = parameter.GetType().GetProperty("value").GetValue(parameter);
            Assert.That(Convert.ToSingle(actual), Is.EqualTo(expected).Within(.0001f));
        }

        private static void Reload() => typeof(WorldLightingSettings)
            .GetMethod("ResetSession", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    }
}
