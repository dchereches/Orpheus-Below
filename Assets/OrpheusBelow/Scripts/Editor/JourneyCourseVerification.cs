using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrpheusBelow.EditorTools
{
    [InitializeOnLoad]
    public static class JourneyCourseVerification
    {
        private const string Key = "OrpheusBelow.JourneyCourseVerification";
        private static readonly int[] Course = { 0,1,2,3,4,5,6,7,6,5,4,3,2,1,0 };
        private static int _phase, _visit, _errors;
        private static double _next;
        private static bool _checkedEscortWait;
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/JourneyCourse"));
        static JourneyCourseVerification()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool(Key, false) && EditorApplication.isPlaying && !stack.Contains("UnityEditor.Search.") &&
                    (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) _errors++;
            };
        }
        public static void RunBatch()
        {
            Directory.CreateDirectory(Output); EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(0));
            SessionState.SetBool(Key, true); SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 900);
            EditorApplication.isPlaying = true;
        }
        private static object Call(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static void Capture(string name, bool hud = true, int width = 1920, int height = 1080)
        {
            string temp = "journey-" + name;
            typeof(PaintedChamberVerification).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { temp, width, height, hud, null });
            File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers/" + temp + ".png")), Path.Combine(Output, name + ".png"), true);
            Debug.Log("[Journey course] Captured " + name);
        }
        private static void Frame(Vector2 point)
        {
            Camera.main.aspect = 16f / 9f; var map = PaintedChamberMap.Active;
            OrpheusPlayerController.Instance.TeleportToChamberSpawn(map.NearestWalkable(map.ToWorld(point), .55f));
            UnderworldCamera2D.Instance.SnapToTargetNow();
        }
        private static void StepAfter(double seconds, int phase) { _phase = phase; _next = EditorApplication.timeSinceStartup + seconds; }
        private static void StopEncounter()
        {
            var director = HadesChamberDirector.Instance; director.enabled = false; director.StopAllCoroutines();
            OrpheusHud.Instance.DismissTutorialModal(); OrpheusPlayerController.Instance.enabled = false;
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(enemy.gameObject);
            var companion = UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>(); if (companion != null) companion.enabled = false;
            Time.timeScale = 0;
        }
        private static void EnterPassage(bool parkCompanionFar = false)
        {
            var map = PaintedChamberMap.Active; var door = map.ActivePassage;
            Frame(UnderworldJourney.IsAscending ? map.definition.returnExit : map.definition.exit);
            OrpheusPlayerController.Instance.transform.position = door.transform.position;
            var companion = UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>();
            if (companion != null)
            {
                Vector3 destination = parkCompanionFar ? map.ToWorld(map.definition.returnSpawn) :
                    door.transform.position + (map.ToWorld(map.definition.encounterCenter) - door.transform.position).normalized * 2.8f;
                companion.SnapBehindOrpheus(map.NearestWalkable(destination, .5f));
            }
            Physics.SyncTransforms(); Time.timeScale = 1;
        }
        private static void Transfer()
        {
            OrpheusPlayerController.Instance.RestoreTravelHealth(73); OrpheusLyreCombat.Instance.UnlockedCadenceTier = 3; EnterPassage();
            if (_visit == Course.Length - 1) StepAfter(1, 7); else { _visit++; StepAfter(1.5, 2); }
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "Deadline", float.MaxValue)) throw new Exception("Journey verification timed out");
                if (EditorApplication.timeSinceStartup < _next || HadesChamberDirector.Instance == null || PaintedChamberMap.Active == null) return;
                var director = HadesChamberDirector.Instance; var map = PaintedChamberMap.Active; var player = OrpheusPlayerController.Instance;
                if (_phase < 7 && map.definition.index != Course[_visit]) return;
                if (_phase == 0)
                {
                    if (!UnderworldFrontEnd.BlocksGameplay || UnderworldJourney.HasEurydice || UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>() != null)
                        throw new Exception("Fresh title/solo start is wrong");
                    Capture("00-title-solo"); Call(UnderworldFrontEnd.Instance, "BeginOrResume"); StepAfter(1, 1); return;
                }
                if (_phase == 1)
                {
                    if (!OrpheusHud.IsTutorialModalActive || OrpheusHud.Instance.CurrentTutorialSpeaker != "Artemis") throw new Exception("Artemis did not teach the opening");
                    Capture("00-artemis-tutorial"); OrpheusHud.Instance.DismissTutorialModal(); player.enabled = false;
                    player.TeleportToChamberSpawn(player.transform.position + UnderworldCamera2D.IsoRightXZ * 2); Call(director, "Update");
                    if (OrpheusHud.Instance.CurrentTutorialSpeaker != "Artemis") throw new Exception("Movement lesson did not advance with Artemis");
                    OrpheusHud.Instance.DismissTutorialModal(); Call(OrpheusLyreCombat.Instance, "HandleAttackPulse", .3f, false); Call(director, "Update");
                    if (OrpheusHud.IsTutorialModalActive) OrpheusHud.Instance.DismissTutorialModal();
                    Call(OrpheusLyreCombat.Instance, "HandleAttackPulse", .8f, true); Call(director, "Update");
                    if (OrpheusHud.IsTutorialModalActive) OrpheusHud.Instance.DismissTutorialModal();
                    OrpheusFluteChannel.Instance?.EnterFluteStance(); Call(director, "Update");
                    if (OrpheusHud.IsTutorialModalActive) OrpheusHud.Instance.DismissTutorialModal();
                    Call(player, "BeginDashCommon"); Call(director, "Update");
                    if (OrpheusHud.IsTutorialModalActive) OrpheusHud.Instance.DismissTutorialModal();
                    Call(OrpheusLyreCombat.Instance, "HandleAttackPulse", .3f, false); Call(director, "Update");
                    if (!UnderworldJourney.Progress.TutorialComplete) throw new Exception("Opening tutorial did not complete");
                    StepAfter(.1, 2); return;
                }
                if (_phase == 2)
                {
                    if (SceneManager.GetActiveScene().path != PaintedChamberScene.ScenePath(Course[_visit])) throw new Exception("Incorrect scene path");
                    bool escort = _visit > 7; var companion = UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>();
                    if (UnderworldJourney.HasEurydice != escort || (companion != null) != escort) throw new Exception("Wrong solo/escort leg at visit " + _visit);
                    if (_visit > 0 && (Mathf.Abs(player.CurrentHealth - 93) > .1f || OrpheusLyreCombat.Instance.UnlockedCadenceTier != 3))
                        throw new Exception("Health/cadence continuity failed at visit " + _visit + " hp=" + player.CurrentHealth);
                    if (escort && director.chambers[Course[_visit]].guardianBoss != null) throw new Exception("A pacified guardian respawned on ascent");
                    if (escort && !UnderworldJourney.Progress.TutorialComplete) throw new Exception("Tutorial completion lost on return");
                    StopEncounter(); Frame(map.definition.encounterCenter); StepAfter(.3, 3); return;
                }
                if (_phase == 3)
                {
                    string leg = _visit < 8 ? "descent" : "ascent"; Capture(leg + "-" + map.definition.index.ToString("D2") + "-arena");
                    if (_visit < 8)
                    {
                        var camera = Camera.main; camera.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
                        camera.transform.position = map.transform.position - camera.transform.forward * 36; camera.orthographicSize = 24;
                        map.overlayRoot.gameObject.SetActive(true); Capture("map-" + map.definition.index.ToString("D2"), false, 1536, 1024); map.overlayRoot.gameObject.SetActive(false);
                    }
                    Call(director, "HandleChamberCleared"); OrpheusHud.Instance.DismissTutorialModal();
                    if (!map.ActivePassage.IsUnlocked || !UnderworldJourney.Progress.IsCleared(map.definition.index)) throw new Exception("Clear did not unlock correct direction");
                    Frame(UnderworldJourney.IsAscending ? map.definition.returnExit : map.definition.exit); Capture(leg + "-" + map.definition.index.ToString("D2") + "-passage");
                    StepAfter(.1, 4); return;
                }
                if (_phase == 4)
                {
                    if (_visit == 7) { EnterPassage(); StepAfter(.7, 5); return; }
                    if (_visit == 8 && !_checkedEscortWait) { EnterPassage(true); StepAfter(.4, 6); return; }
                    Transfer(); return;
                }
                if (_phase == 5)
                {
                    if (!UnderworldJourney.HasEurydice || !UnderworldJourney.IsAscending || UnderworldJourney.Depth != 7 || player.IsVictory) throw new Exception("Throne rescue did not turn the run around");
                    if (UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>() == null || !map.returnDoor.gameObject.activeSelf || map.exitDoor.gameObject.activeSelf) throw new Exception("Rescue did not create escort/swap passages");
                    Capture("07-reunion-artemis"); OrpheusHud.Instance.DismissTutorialModal(); UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>().enabled = false; Transfer(); return;
                }
                if (_phase == 6)
                {
                    if (map.definition.index != 6 || UnderworldJourney.Depth != 6) throw new Exception("Passage failed to wait for distant Eurydice");
                    _checkedEscortWait = true; Transfer(); return;
                }
                if (_phase == 7)
                {
                    if (!player.IsVictory || !UnderworldJourney.Progress.IsComplete || !_checkedEscortWait) throw new Exception("Surface escort victory failed");
                    Capture("00-together-in-the-light"); PaintedChamberScene.Restart(true); StepAfter(1.2, 8); return;
                }
                if (_phase == 8)
                {
                    if (map.definition.index != 0 || UnderworldJourney.HasEurydice || UnderworldJourney.Progress.TutorialComplete || player.CurrentHealth != player.maxHealth || OrpheusLyreCombat.Instance.UnlockedCadenceTier != 1 || UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>() != null)
                        throw new Exception("Restart did not reset a fresh solo descent");
                    PaintedChamberScene.Restart(false); StepAfter(1, 9); return;
                }
                if (_phase == 9)
                {
                    if (!UnderworldFrontEnd.BlocksGameplay || Time.timeScale != 0 || UnderworldJourney.HasEurydice) throw new Exception("Return to title failed");
                    if (_errors != 0) throw new Exception("Runtime errors: " + _errors);
                    Debug.Log("[Journey course] PASS: eight mapped scenes, 15 visits, actual two-way passages, solo descent, Artemis tutorial, deepest throne rescue, escort ascent, waiting at gates, pacified guardians, health/cadence continuity, surface victory, solo restart/title; zero runtime errors.");
                    SessionState.SetBool(Key, false); EditorApplication.Exit(0);
                }
            }
            catch (Exception e) { SessionState.SetBool(Key, false); Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
