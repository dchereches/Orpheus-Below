using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace OrpheusBelow.EditorTools
{
    /// <summary>Batch-only runtime/render verification in a new, unsaved scene.</summary>
    [InitializeOnLoad]
    public static class UnderworldSmokeCapture
    {
        private const string Key = "OrpheusBelow.SmokeCapture";
        private static double _next;
        private static int _step;
        private static int _runtimeErrors;
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/VisualOverhaul"));

        static UnderworldSmokeCapture()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool(Key, false) && EditorApplication.isPlaying &&
                    !stack.Contains("UnityEditor.Search.") &&
                    (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) _runtimeErrors++;
            };
        }

        public static void RunBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a batch editor for this check.");
            Directory.CreateDirectory(Output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + ".Deadline", (float)EditorApplication.timeSinceStartup + 300f);
            EditorApplication.isPlaying = true;
        }

        private static void Invoke(object target, string method, params object[] args)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        private static void Capture(string name, int width = 1920, int height = 1080)
        {
            // Batch editors have no Game view backbuffer. Render the camera and UI explicitly.
            var camera = Camera.main;
            // Overlay UI normally bypasses world bloom. Suppress bloom for this camera-rendered capture too.
            var profile = GameObject.Find("Orpheus_HDRP_2D_ExposureFix").GetComponent<Volume>().sharedProfile;
            typeof(RenderPipeline2DSetup).GetMethod("AddClarityOverride", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { profile, "Bloom", "intensity", 0f });
            var target = RenderTexture.GetTemporary(width, height, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            var originalTarget = camera.targetTexture;
            var originalActive = RenderTexture.active;
            float originalAspect = camera.aspect;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var cameras = new Camera[canvases.Length];
            var distances = new float[canvases.Length];
            camera.targetTexture = target;
            camera.aspect = width / (float)height;
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera; distances[i] = canvases[i].planeDistance;
                if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), pixels.EncodeToPNG());
            UnityEngine.Object.Destroy(pixels);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i];
            }
            camera.targetTexture = originalTarget;
            camera.aspect = originalAspect;
            RenderTexture.active = originalActive;
            RenderTexture.ReleaseTemporary(target);
            Debug.Log("[Overhaul smoke] Captured " + name);
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".Deadline", float.MaxValue))
                    throw new Exception("Runtime smoke capture timed out");
                if (!EditorApplication.isPlaying) return;
                if (HadesChamberDirector.Instance == null) return;
                if (_step == 0) { _next = EditorApplication.timeSinceStartup + 12; _step++; return; }
                if (EditorApplication.timeSinceStartup < _next) return;
                var director = HadesChamberDirector.Instance;
                var player = OrpheusPlayerController.Instance;
                switch (_step)
                {
                    case 1:
                        if (UnderworldFrontEnd.Instance == null || !UnderworldFrontEnd.BlocksGameplay || director.chambers.Count != 5)
                            throw new Exception("Title flow or five-room route invalid");
                        var vista = GameObject.Find("AsphodelPaintedVista_25D").GetComponent<SpriteRenderer>();
                        if (vista.sprite.texture.name != "AsphodelQuietVista") throw new Exception("Refined Asphodel art missing");
                        Capture("01-title");
                        Invoke(UnderworldFrontEnd.Instance, "ShowSettings");
                        break;
                    case 2:
                        Capture("02-settings");
                        Capture("02-settings-720p", 1280, 720);
                        Capture("02-settings-4x3", 1024, 768);
                        Invoke(UnderworldFrontEnd.Instance, "HideSettings");
                        Invoke(UnderworldFrontEnd.Instance, "BeginOrResume");
                        break;
                    case 3:
                        if (!OrpheusHud.IsTutorialModalActive) throw new Exception("Opening tutorial did not pause");
                        if (GameObject.Find("ExposureCanvas") == null) throw new Exception("Exposure canvas hidden during gameplay");
                        Capture("03-dialogue");
                        OrpheusHud.Instance.DismissTutorialModal();
                        // This is a visual tour, not an input-driven completion of the opening tutorial.
                        director.enabled = false;
                        director.StopAllCoroutines();
                        Invoke(director, "EnterChamberImmediate", 1);
                        OrpheusHud.Instance.DismissTutorialModal();
                        director.StopAllCoroutines();
                        player.TeleportToChamberSpawn(director.chambers[1].centerWorld + new Vector3(-10, 0, 10));
                        UnderworldCamera2D.Instance.SnapToTargetNow();
                        break;
                    case 4:
                        OrpheusHud.Instance.DismissTutorialModal();
                        Capture("04-slate-floor-and-beacons");
                        OrpheusHud.Instance.showControlsHelp = true;
                        break;
                    case 5:
                        Capture("05-controls");
                        OrpheusHud.Instance.showControlsHelp = false;
                        foreach (var shade in UnityEngine.Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None))
                            UnityEngine.Object.Destroy(shade.gameObject);
                        director.StopAllCoroutines();
                        Invoke(director, "EnterChamberImmediate", 2);
                        OrpheusHud.Instance.DismissTutorialModal();
                        director.StopAllCoroutines();
                        player.TeleportToChamberSpawn(director.chambers[2].centerWorld + new Vector3(-9, 0, 10));
                        UnderworldCamera2D.Instance.SnapToTargetNow();
                        break;
                    case 6:
                        Capture("06-basalt-floor");
                        foreach (var shade in UnityEngine.Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None))
                            UnityEngine.Object.Destroy(shade.gameObject);
                        director.StopAllCoroutines();
                        Invoke(director, "EnterChamberImmediate", 4);
                        director.StopAllCoroutines();
                        if (director.chambers[4].guardianBoss.identity != BossIdentity.CerberusHoundOfHades ||
                            !director.chambers[4].exitDoor.isFinalSunlightExit) throw new Exception("Final guardian/exit invalid");
                        Invoke(director, "BeginChamberBossShowdown", director.chambers[4]);
                        break;
                    case 7:
                        Capture("07-final-guardian");
                        OrpheusPrototypeBuilder.RestartDescent(true);
                        break;
                    case 8:
                        if (UnderworldFrontEnd.BlocksGameplay || player.CurrentHealth != player.maxHealth ||
                            UnderworldCamera2D.Instance.SideViewBlend01 > .01f)
                            throw new Exception("Restart did not reset the run");
                        Capture("08-restart");
                        OrpheusPrototypeBuilder.RestartDescent(false);
                        break;
                    case 9:
                        if (!UnderworldFrontEnd.BlocksGameplay || Time.timeScale != 0f || !AudioListener.pause)
                            throw new Exception("Return to title did not pause the run");
                        if (_runtimeErrors > 0) throw new Exception("Runtime logged " + _runtimeErrors + " errors; inspect the batch log.");
                        SessionState.SetBool(Key, false);
                        Debug.Log("[Visual overhaul smoke] PASS: title, settings, dialogue, painted floors, beacon towers, five chambers, Cerberus, restart, return to title; zero runtime errors.");
                        EditorApplication.Exit(0);
                        return;
                }
                _step++;
                _next = EditorApplication.timeSinceStartup + 3;
            }
            catch (Exception e)
            {
                SessionState.SetBool(Key, false);
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
