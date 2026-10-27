using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace OrpheusBelow.EditorTools
{
    [InitializeOnLoad]
    public static class PaintedChamberVerification
    {
        private const string Key = "OrpheusBelow.PaintedVerification";
        private static int _index, _phase, _errors;
        private static double _next;
        private static bool _checkedTravel;
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers"));

        static PaintedChamberVerification()
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
            if (ProceduralChamberGenerator.TotalChambers == 8) { JourneyCourseVerification.RunBatch(); return; }
            if (!Application.isBatchMode) throw new InvalidOperationException("Use a batch editor for render verification.");
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(0));
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + ".Deadline", (float)EditorApplication.timeSinceStartup + 600f);
            EditorApplication.isPlaying = true;
        }

        private static void Invoke(object target, string method, params object[] args)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        private static Color Capture(string name, int width = 1920, int height = 1080, bool hud = true, Vector3? probe = null)
        {
            Camera camera = Camera.main;
            var target = RenderTexture.GetTemporary(width, height, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            var originalTarget = camera.targetTexture;
            var active = RenderTexture.active;
            float aspect = camera.aspect;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var cameras = new Camera[canvases.Length];
            var distances = new float[canvases.Length];
            var enabled = new bool[canvases.Length];
            var orders = new int[canvases.Length];
            var profile = GameObject.Find("Orpheus_HDRP_2D_ExposureFix").GetComponent<Volume>().sharedProfile;
            typeof(RenderPipeline2DSetup).GetMethod("AddClarityOverride", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { profile, "Bloom", "intensity", 0f });
            camera.targetTexture = target; camera.aspect = width / (float)height;
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera;
                orders[i] = canvases[i].sortingOrder;
                distances[i] = canvases[i].planeDistance; enabled[i] = canvases[i].enabled;
                if (!hud) { canvases[i].enabled = false; continue; }
                if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = 1f;
                // Preserve overlay priority while capturing UI through the world camera.
                canvases[i].sortingOrder = 30000 + orders[i];
            }
            foreach (var actor in UnityEngine.Object.FindObjectsByType<PaintedActorDepth>(FindObjectsSortMode.None))
                Invoke(actor, "LateUpdate");
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            Color probeColor = Color.clear;
            if (probe.HasValue)
            {
                Vector3 viewport = camera.WorldToViewportPoint(probe.Value);
                probeColor = pixels.GetPixel(Mathf.Clamp(Mathf.RoundToInt(viewport.x * width), 0, width - 1),
                    Mathf.Clamp(Mathf.RoundToInt(viewport.y * height), 0, height - 1));
            }
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), pixels.EncodeToPNG());
            UnityEngine.Object.Destroy(pixels);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i];
                canvases[i].planeDistance = distances[i]; canvases[i].enabled = enabled[i];
                canvases[i].sortingOrder = orders[i];
            }
            camera.targetTexture = originalTarget; camera.aspect = aspect;
            RenderTexture.active = active; RenderTexture.ReleaseTemporary(target);
            Debug.Log("[Painted verification] Captured " + name);
            return probeColor;
        }

        private static void PrepareRoom()
        {
            var director = HadesChamberDirector.Instance;
            director.enabled = false; director.StopAllCoroutines();
            OrpheusHud.Instance.DismissTutorialModal();
            OrpheusPlayerController.Instance.enabled = false;
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None))
                UnityEngine.Object.Destroy(enemy.gameObject);
            Time.timeScale = 0f;
            Physics.SyncTransforms();
        }

        private static void Frame(Vector2 point)
        {
            Camera.main.aspect = 16f / 9f;
            var map = PaintedChamberMap.Active;
            OrpheusPlayerController.Instance.TeleportToChamberSpawn(map.NearestWalkable(map.ToWorld(point), .55f));
            UnderworldCamera2D.Instance.SnapToTargetNow();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".Deadline", float.MaxValue))
                    throw new Exception("Painted scene verification timed out");
                if (!EditorApplication.isPlaying || HadesChamberDirector.Instance == null || PaintedChamberMap.Active == null) return;
                if (EditorApplication.timeSinceStartup < _next) return;
                var director = HadesChamberDirector.Instance;
                var map = PaintedChamberMap.Active;
                if (map.definition.index != _index) return;
                var player = OrpheusPlayerController.Instance;
                if (_phase == 5)
                {
                    if (!player.IsVictory) throw new Exception("The painted final gate did not trigger victory");
                    PaintedChamberScene.Restart(true);
                    _index = 0; _phase = 6; _next = EditorApplication.timeSinceStartup + 2; return;
                }
                if (_phase == 6)
                {
                    if (UnderworldFrontEnd.BlocksGameplay || player.CurrentHealth != player.maxHealth ||
                        OrpheusLyreCombat.Instance.UnlockedCadenceTier != 1)
                        throw new Exception("Restart did not start a fresh descent in the first scene");
                    PaintedChamberScene.Restart(false);
                    _phase = 7; _next = EditorApplication.timeSinceStartup + 2; return;
                }
                if (_phase == 7)
                {
                    if (!UnderworldFrontEnd.BlocksGameplay || Time.timeScale != 0f || !AudioListener.pause)
                        throw new Exception("Return to title did not pause the first scene");
                    if (!_checkedTravel || _errors != 0) throw new Exception("Transition unchecked or runtime errors: " + _errors);
                    Debug.Log("[Painted verification] PASS: five scenes, source images, collision maps, solid cover, zoom, painted exits, foreground occlusion in both directions, real scene transitions, health and cadence continuity, final victory, fresh restart and return to title; zero runtime errors.");
                    SessionState.SetBool(Key, false); EditorApplication.Exit(0); return;
                }
                if (_phase == 0)
                {
                    if (_index == 0)
                    {
                        if (!UnderworldFrontEnd.BlocksGameplay) throw new Exception("Initial scene skipped its title");
                        _next = EditorApplication.timeSinceStartup + 8;
                        _phase = 1; return;
                    }
                    _phase = 2;
                }
                if (_phase == 1)
                {
                    Capture("00-title"); Invoke(UnderworldFrontEnd.Instance, "BeginOrResume");
                    _phase = 2; _next = EditorApplication.timeSinceStartup + 2; return;
                }
                if (_phase == 2)
                {
                    if (_index == 1 && !_checkedTravel)
                    {
                        if (Mathf.Abs(player.CurrentHealth - 93f) > .1f || OrpheusLyreCombat.Instance.UnlockedCadenceTier != 3)
                            throw new Exception("Scene transition did not preserve health/cadences plus chamber respite");
                        _checkedTravel = true;
                    }
                    if (SceneManager.GetActiveScene().path != PaintedChamberScene.ScenePath(_index))
                        throw new Exception("Wrong authored scene is active");
                    PrepareRoom();
                    Frame(map.definition.entrance);
                    _phase = 3; _next = EditorApplication.timeSinceStartup + 1; return;
                }
                if (_phase == 3)
                {
                    Capture(_index.ToString("D2") + "-entrance");
                    Frame(map.definition.encounterCenter);
                    Capture(_index.ToString("D2") + "-arena");
                    var camera = Camera.main;
                    camera.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
                    camera.transform.position = map.transform.position - camera.transform.forward * 36f;
                    camera.orthographicSize = 24f;
                    map.overlayRoot.gameObject.SetActive(true);
                    Capture(_index.ToString("D2") + "-collision-map", 1536, 1024, false);
                    map.overlayRoot.gameObject.SetActive(false);
                    Frame(map.definition.exit);
                    Capture(_index.ToString("D2") + "-exit");
                    if (_index == 1)
                    {
                        Frame(new Vector2(806, 755));
                        Vector3 probe = player.transform.position + RenderPipeline2DSetup.IsoCameraRotation * Vector3.up * .5f;
                        Color hidden = Capture("01-behind-cover", probe: probe);
                        player.gameObject.SetActive(false);
                        Color reference = Capture("01-behind-cover-reference", probe: probe);
                        player.gameObject.SetActive(true);
                        float difference = Mathf.Max(Mathf.Abs(hidden.r - reference.r), Mathf.Abs(hidden.g - reference.g), Mathf.Abs(hidden.b - reference.b));
                        if (difference > .04f) throw new Exception("Foreground cover did not occlude the player's lower body; difference " + difference);
                        Frame(new Vector2(806, 850));
                        Debug.Log("[Painted verification] Front position " + map.ToImage(player.transform.position) +
                            "; pillar active " + map.coverRoot.gameObject.activeInHierarchy);
                        probe = player.transform.position + RenderPipeline2DSetup.IsoCameraRotation * Vector3.up * .9f;
                        Color visible = Capture("01-in-front-of-cover", probe: probe);
                        player.gameObject.SetActive(false);
                        reference = Capture("01-in-front-of-cover-reference", probe: probe);
                        player.gameObject.SetActive(true);
                        difference = Mathf.Max(Mathf.Abs(visible.r - reference.r), Mathf.Abs(visible.g - reference.g), Mathf.Abs(visible.b - reference.b));
                        if (difference < .08f) throw new Exception("Player remained hidden when standing in front of cover.");
                    }
                    foreach (var cover in map.coverRoot.GetComponentsInChildren<MeshCollider>())
                    {
                        var center = cover.bounds.center; center.y = .6f;
                        var ray = new Ray(center + Vector3.right * 20f, Vector3.left);
                        if (!cover.Raycast(ray, out var hit, 40f)) throw new Exception("Cover collider missing at " + cover.name);
                    }
                    _phase = 4;
                    _next = EditorApplication.timeSinceStartup + 1; return;
                }
                if (_phase == 4)
                {
                    if (_index == 4)
                    {
                        if (director.chambers[4].guardianBoss.identity != BossIdentity.CerberusHoundOfHades)
                            throw new Exception("Final guardian missing");
                        _phase = 5;
                    }
                    if (_index == 0) { player.RestoreTravelHealth(73f); OrpheusLyreCombat.Instance.UnlockedCadenceTier = 3; }
                    // Enter the real exit trigger and let physics invoke the actual transition.
                    map.exitDoor.SetUnlocked(true);
                    Frame(map.definition.exit);
                    player.transform.position = map.exitDoor.transform.position;
                    Physics.SyncTransforms();
                    Time.timeScale = 1f;
                    if (_index != 4) { _index++; _phase = 0; }
                    _next = EditorApplication.timeSinceStartup + 2;
                }
            }
            catch (Exception e)
            {
                SessionState.SetBool(Key, false); Debug.LogException(e); EditorApplication.Exit(1);
            }
        }
    }
}
