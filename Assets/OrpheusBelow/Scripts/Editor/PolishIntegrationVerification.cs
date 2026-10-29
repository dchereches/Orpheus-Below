using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    // Opt-in rendered integration evidence, rather than a shipped gameplay probe.
    [InitializeOnLoad]
    public static class PolishIntegrationVerification
    {
        private const string Key = "OrpheusBelow.PolishVerification";
        private static readonly string[] Prefs = { WorldLightingSettings.ExposureKey, WorldLightingSettings.ContrastKey,
            WorldLightingSettings.BloomKey, "Orpheus_Brightness" };
        private static int _phase, _errors, _frame;
        private static double _next;
        private static Vector3 _cameraPosition, _actorPosition;
        private static Quaternion _cameraRotation;
        private static CompletionLensPulse _lens;
        private static GroundShockwaveRing _pauseEffect;
        private static float _effectElapsed;
        private static PortalLocationParticles _portal;
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PolishIntegration"));
        [Serializable] private sealed class Evidence
        {
            public string result, error;
            public int runtimeErrors, lensChangedPixels, portalParticles, portalChangedPixels;
            public float normalLuminance, brightLuminance;
            public bool pausePreservedWorld, pausePreservedEffect, effectResumed, chamberClearPulsed, comboPulsed;
        }
        private static readonly Evidence Proof = new Evidence();
        static PolishIntegrationVerification()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool(Key, false) && EditorApplication.isPlaying &&
                    !stack.Contains("UnityEditor.Search.") &&
                    (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) _errors++;
            };
        }
        public static void RunBatch()
        {
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers")));
            foreach (string key in Prefs)
            {
                SessionState.SetBool(Key + key + "Had", PlayerPrefs.HasKey(key));
                SessionState.SetFloat(Key + key, PlayerPrefs.GetFloat(key));
                PlayerPrefs.DeleteKey(key);
            }
            SessionState.SetInt(Key + "Mode", PlayerPrefs.GetInt("OrpheusBelow.SensorSourceMode", -1));
            SessionState.SetInt(Key + "Viewed", PlayerPrefs.GetInt(IntroCinematicPlayer.ViewedKey, -1));
            PlayerPrefs.SetInt("OrpheusBelow.SensorSourceMode", 0);
            _phase = _errors = 0; _next = 0; _frame = -1;
            EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(0));
            SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 180f);
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        private static object Call(object target, string method, params object[] args)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args);
        private static void Require(bool value, string reason) { if (!value) throw new Exception("Phase " + _phase + ": " + reason); }
        private static void Step(int phase, double delay = .2) { _phase = phase; _next = EditorApplication.timeSinceStartup + delay; }
        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
            try
            {
                Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + "Deadline", 0), "Timeout");
                if (EditorApplication.timeSinceStartup < _next || _frame == Time.frameCount) return;
                _frame = Time.frameCount;
                var menu = UnderworldFrontEnd.Instance;
                if (menu == null) return;
                if (_phase == 0)
                {
                    var intro = Object.FindFirstObjectByType<IntroCinematicPlayer>();
                    if (intro != null) { intro.Skip(); return; }
                    if (UnderworldFrontEnd.BlocksMenusOrTransitions || OrpheusPlayerController.Instance == null) return;
                    if (VoiceDirector.Instance != null) { Object.Destroy(VoiceDirector.Instance); return; }
                    HadesChamberDirector.Instance.enabled = false;
                    HadesChamberDirector.Instance.StopAllCoroutines();
                    OrpheusHud.Instance.DismissTutorialModal();
                    OrpheusHud.Instance.HideSubtitle();
                    foreach (var enemy in Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None)) Object.Destroy(enemy.gameObject);
                    OrpheusPlayerController.Instance.enabled = false;
                    SensorPulseInput.Instance.enabled = false;
                    WorldLightingSettings.ResetDefaults();
                    Time.timeScale = 1;
                    Step(1); return;
                }
                if (_phase == 1)
                {
                    _cameraPosition = Camera.main.transform.position; _cameraRotation = Camera.main.transform.rotation;
                    _actorPosition = OrpheusPlayerController.Instance.transform.position;
                    GroundShockwaveRing.Spawn(_actorPosition, 2f, new Color(1, .8f, .4f, .6f), 6f);
                    _pauseEffect = Object.FindFirstObjectByType<GroundShockwaveRing>();
                    _effectElapsed = (float)typeof(GroundShockwaveRing).GetField("_elapsed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_pauseEffect);
                    Call(menu, "OpenMenu"); Step(2, .45); return;
                }
                if (_phase == 2)
                {
                    Require(Time.timeScale == 0 && AudioListener.pause, "Pause does not freeze world/audio");
                    Require(Camera.main.transform.position == _cameraPosition && Camera.main.transform.rotation == _cameraRotation &&
                        OrpheusPlayerController.Instance.transform.position == _actorPosition, "Pause moved camera or actor");
                    var backdrop = (GameObject)typeof(UnderworldFrontEnd).GetField("_titleBackdrop", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(menu);
                    Require(!backdrop.activeSelf,
                        "Pause still shows title backdrop");
                    Proof.pausePreservedWorld = true;
                    Require(_pauseEffect != null && (float)typeof(GroundShockwaveRing).GetField("_elapsed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_pauseEffect) == _effectElapsed,
                        "World effect advanced behind pause overlay");
                    Proof.pausePreservedEffect = true;
                    Capture("paused-current-chamber", true);
                    Call(menu, "ShowSettings"); Step(3); return;
                }
                if (_phase == 3)
                {
                    Capture("lighting-settings", true);
                    Capture("lighting-neutral");
                    Proof.normalLuminance = Luminance("lighting-neutral");
                    WorldLightingSettings.SetExposure(.85f);
                    WorldLightingSettings.SetContrast(12f);
                    Step(4); return;
                }
                if (_phase == 4)
                {
                    Capture("lighting-brighter"); Proof.brightLuminance = Luminance("lighting-brighter");
                    Require(Proof.brightLuminance > Proof.normalLuminance + .012f, "Lighting slider did not brighten actual world pixels");
                    WorldLightingSettings.ResetDefaults(); Call(menu, "HideSettings"); Call(menu, "BeginOrResume");
                    Step(5, .6); return;
                }
                if (_phase == 5)
                {
                    if (UnderworldFrontEnd.BlocksMenusOrTransitions) return;
                    Require(_pauseEffect != null && (float)typeof(GroundShockwaveRing).GetField("_elapsed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_pauseEffect) > _effectElapsed,
                        "World effect failed to resume");
                    Proof.effectResumed = true;
                    UnderworldCamera2D.Instance.enabled = false;
                    _lens = CompletionLensPulse.EnsureInstance(); _lens.enabled = false; _lens.ResetPulse();
                    Call(OrpheusFluteChannel.Instance, "CompleteMelodyOfSorrow");
                    Require(_lens.PulseCount == 1, "Actual Aulos completion did not trigger lens");
                    _lens.Advance(CompletionLensPulse.ComboDuration * .18f);
                    Require(_lens.CurrentStrength < -.2f, "Completed combo has no inward distortion");
                    Proof.comboPulsed = true; Step(6, .03); return;
                }
                if (_phase == 6)
                {
                    Capture("lens-combo-peak");
                    // Same world frame, changing only the lens override: spawned combo VFX
                    // and player animation cannot masquerade as a working post-process.
                    float frozen = _lens.CurrentStrength;
                    Call(_lens, "ApplyStrength", 0f); Capture("lens-neutral");
                    Call(_lens, "ApplyStrength", frozen);
                    Proof.lensChangedPixels = ChangedPixels("lens-neutral", "lens-combo-peak");
                    Require(Proof.lensChangedPixels > 1000, "Lens component produced no visible rendered pulse");
                    Call(menu, "OpenMenu"); _lens.Advance(2);
                    Require(_lens.CurrentStrength == frozen, "Lens moved paused image");
                    Call(menu, "BeginOrResume"); Step(7, .6); return;
                }
                if (_phase == 7)
                {
                    if (UnderworldFrontEnd.BlocksMenusOrTransitions) return;
                    _lens.Advance(2); Require(_lens.CurrentStrength == 0, "Lens failed to settle");
                    Call(HadesChamberDirector.Instance, "HandleChamberCleared");
                    Require(_lens.PulseCount == 2, "Actual clear did not trigger lens once");
                    Call(HadesChamberDirector.Instance, "HandleChamberCleared");
                    Require(_lens.PulseCount == 2, "Repeated clear retriggered lens");
                    Proof.chamberClearPulsed = true; _lens.ResetPulse(); OrpheusHud.Instance.DismissTutorialModal();
                    var door = Object.FindFirstObjectByType<ChamberExitDoor>(); door.SetUnlocked(true);
                    _portal = (PortalLocationParticles)typeof(ChamberExitDoor).GetField("_exitParticles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(door);
                    for (int i = 0; i < 30; i++) Call(_portal, "Advance", 1f / 30f);
                    Proof.portalParticles = _portal.GetComponent<ParticleSystem>().particleCount;
                    Require(Proof.portalParticles > 5, "Door has no inward particles");
                    Camera.main.transform.position = _portal.AttractionPoint - Camera.main.transform.forward * 30f;
                    Camera.main.orthographicSize = 8f;
                    Step(8); return;
                }
                if (_phase == 8)
                {
                    Capture("doorway-suction");
                    var renderer = _portal.GetComponent<ParticleSystemRenderer>();
                    renderer.enabled = false; Capture("doorway-without-dust"); renderer.enabled = true;
                    Proof.portalChangedPixels = ChangedPixels("doorway-suction", "doorway-without-dust");

                    Require(Proof.portalChangedPixels > 30, "Inward dust exists but does not visibly render: " + Proof.portalChangedPixels);
                    Require(_errors == 0, "Runtime errors: " + _errors); Finish(0); return;
                }
            }
            catch (Exception error) { Proof.error = error.ToString(); Debug.LogException(error); Finish(1); }
        }
        private static void Capture(string name, bool hud = false)
        {
            typeof(PaintedChamberVerification).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { "polish-" + name, 1920, 1080, hud, null });
            File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers/polish-" + name + ".png")),
                Path.Combine(Output, name + ".png"), true);
        }
        private static Color32[] Pixels(string name)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(Path.Combine(Output, name + ".png")));
            var pixels = texture.GetPixels32(); Object.DestroyImmediate(texture); return pixels;
        }
        private static float Luminance(string name) => Pixels(name).Average(c => (c.r * .2126f + c.g * .7152f + c.b * .0722f) / 255f);
        private static int ChangedPixels(string a, string b)
        {
            var aa = Pixels(a); var bb = Pixels(b); int changed = 0;
            for (int i = 0; i < aa.Length; i++)
                if (Mathf.Max(Mathf.Abs(aa[i].r - bb[i].r), Mathf.Abs(aa[i].g - bb[i].g), Mathf.Abs(aa[i].b - bb[i].b)) > 8) changed++;
            return changed;
        }
        private static void Finish(int code)
        {
            Proof.result = code == 0 ? "pass" : "fail"; Proof.runtimeErrors = _errors;
            File.WriteAllText(Path.Combine(Output, "result.json"), JsonUtility.ToJson(Proof, true));
            foreach (string key in Prefs)
                if (SessionState.GetBool(Key + key + "Had", false)) PlayerPrefs.SetFloat(key, SessionState.GetFloat(Key + key, 0));
                else PlayerPrefs.DeleteKey(key);
            RestoreInt("OrpheusBelow.SensorSourceMode", "Mode"); RestoreInt(IntroCinematicPlayer.ViewedKey, "Viewed");
            PlayerPrefs.Save(); SessionState.SetBool(Key, false); EditorApplication.Exit(code);
        }
        private static void RestoreInt(string pref, string saved)
        { int old = SessionState.GetInt(Key + saved, -1); if (old < 0) PlayerPrefs.DeleteKey(pref); else PlayerPrefs.SetInt(pref, old); }
    }
}
