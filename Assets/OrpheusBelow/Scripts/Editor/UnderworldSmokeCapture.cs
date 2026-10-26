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
    /// <summary>Batch-only runtime/render verification. Run in a disposable project copy.</summary>
    [InitializeOnLoad]
    public static class UnderworldSmokeCapture
    {
        private const string Key = "OrpheusBelow.SmokeCapture";
        private static double _next;
        private static int _step;
        private static int _runtimeErrors;
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../overhaul-verification"));

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
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a disposable batch project for this check.");
            Directory.CreateDirectory(Output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + ".Deadline", (float)EditorApplication.timeSinceStartup + 300f);
            EditorApplication.isPlaying = true;
        }

        private static void Invoke(object target, string method, params object[] args)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        private static void Capture(string name)
        {
            // Batch editors have no Game view backbuffer. Render the camera and UI explicitly.
            var camera = Camera.main;
            var target = RenderTexture.GetTemporary(1920, 1080, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            var originalTarget = camera.targetTexture;
            var originalActive = RenderTexture.active;
            float originalAspect = camera.aspect;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var cameras = new Camera[canvases.Length];
            var distances = new float[canvases.Length];
            camera.targetTexture = target;
            camera.aspect = 16f / 9f;
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
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
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
                if (HadesChamberDirector.Instance == null || UnderworldFrontEnd.Instance == null) return;
                if (_step == 0) { _next = EditorApplication.timeSinceStartup + 12; _step++; return; }
                if (EditorApplication.timeSinceStartup < _next) return;
                var director = HadesChamberDirector.Instance;
                var player = OrpheusPlayerController.Instance;
                var menu = UnderworldFrontEnd.Instance;
                switch (_step)
                {
                    case 1:
                        if (!UnderworldFrontEnd.BlocksGameplay || director.chambers.Count != 12) throw new Exception("Title or chamber count invalid");
                        Capture("01-title");
                        break;
                    case 2:
                        Invoke(menu, "BeginOrResume");
                        break;
                    case 3:
                        Capture("02-asphodel-tutorial");
                        break;
                    case 4:
                        Time.timeScale = .05f;
                        Invoke(menu, "OpenMenu");
                        float hp = player.CurrentHealth;
                        player.TakeDamage(20f, player.transform.position);
                        if (player.CurrentHealth != hp) throw new Exception("Damage leaked through pause");
                        Invoke(menu, "BeginOrResume");
                        if (Time.timeScale != 1f) throw new Exception("Hitstop survived resume");
                        Invoke(director, "EnterChamberImmediate", 1);
                        player.TeleportToChamberSpawn(director.chambers[1].centerWorld + new Vector3(-2f, 0f, -4f));
                        break;
                    case 5:
                        Capture("03-garden-combat");
                        break;
                    case 6:
                        // Keep screenshot checks deterministic after the live combat sample.
                        foreach (var shade in UnityEngine.Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None))
                            UnityEngine.Object.Destroy(shade.gameObject);
                        director.StopAllCoroutines();
                        Invoke(director, "EnterChamberImmediate", 8);
                        director.StopAllCoroutines();
                        player.TeleportToChamberSpawn(director.chambers[8].centerWorld + new Vector3(4f, 0f, 14f));
                        break;
                    case 7:
                        Capture("04-cocytus-water");
                        Invoke(director, "BeginChamberBossShowdown", director.chambers[8]);
                        break;
                    case 8:
                        Capture("05-guardian-stage");
                        break;
                    case 9:
                        Debug.Log("[Overhaul smoke] Restarting scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " index=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                        OrpheusPrototypeBuilder.RestartDescent(true);
                        Debug.Log("[Overhaul smoke] Restart returned");
                        break;
                    case 10:
                        if (UnderworldFrontEnd.BlocksGameplay || player.CurrentHealth != player.maxHealth ||
                            UnderworldCamera2D.Instance.SideViewBlend01 > .01f)
                            throw new Exception("Unsaved scene restart did not reset the run");
                        if (_runtimeErrors > 0) throw new Exception("Runtime logged " + _runtimeErrors + " errors; inspect the batch log.");
                        SessionState.SetBool(Key, false);
                        Debug.Log("[Overhaul smoke] PASS: title, 12 rooms, pause damage gate, hitstop resume, biome transition, guardian stage, unsaved scene restart.");
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
