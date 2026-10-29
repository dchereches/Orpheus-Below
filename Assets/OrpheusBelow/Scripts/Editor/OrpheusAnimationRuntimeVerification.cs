using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    /// <summary>Opt-in, rendered Play Mode verification against the real first chamber and gameplay components.</summary>
    [InitializeOnLoad]
    public static class OrpheusAnimationRuntimeVerification
    {
        private const string Key = "OrpheusBelow.VerifyAnimationRepair";
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/OrpheusRepair/Runtime"));
        private static OrpheusPlayerController _player;
        private static OrpheusSpriteAnimator _animator;
        private static OrpheusLyreCombat _combat;
        private static OrpheusFluteChannel _flute;
        private static int _segment, _step, _frame, _lastFrame = -1, _errors;
        private static bool _ready;
        private static Vector3 _start;
        private static readonly HashSet<string> Seen = new HashSet<string>();
        private sealed class Segment
        {
            public string name;
            public int frames;
            public Vector2 movement;
            public Action begin;
            public Segment(string name, int frames, Vector2 movement = default, Action begin = null)
            { this.name = name; this.frames = frames; this.movement = movement; this.begin = begin; }
        }
        private static Segment[] _segments;

        static OrpheusAnimationRuntimeVerification()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool(Key, false) && (type == LogType.Error || type == LogType.Exception)
                    && !stack.Contains("UnityEditor.Search.")) _errors++;
            };
        }

        public static void RunBatch()
        {
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers")));
            File.Delete(Path.Combine(Output, "result.json")); File.Delete(Path.Combine(Output, "failure.txt"));
            SessionState.SetInt(Key + "Viewed", PlayerPrefs.GetInt(IntroCinematicPlayer.ViewedKey, -1));
            SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 360f);
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(0));
            EditorApplication.isPlaying = true;
        }

        private static object Call(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

        private static void Prepare()
        {
            _player = OrpheusPlayerController.Instance;
            _animator = _player.GetComponent<OrpheusSpriteAnimator>();
            _combat = _player.GetComponent<OrpheusLyreCombat>(); _flute = _player.GetComponent<OrpheusFluteChannel>();
            _player.enabled = _animator.enabled = _combat.enabled = _flute.enabled = false;
            if (SensorPulseInput.Instance != null) SensorPulseInput.Instance.enabled = false;
            HadesChamberDirector.Instance.enabled = false;
            _player.maxHealth = 1000000; _player.RestoreTravelHealth(1000000);
            Object.FindFirstObjectByType<UnderworldCamera2D>().enabled = false;
            _start = _player.transform.position + UnderworldCamera2D.IsoRightXZ * 2f;
            _player.TeleportToChamberSpawn(_start);
            Camera camera = Camera.main;
            camera.orthographicSize = 8.5f;
            camera.transform.position = _start + Vector3.up * 1.7f - camera.transform.forward * 30f;
            Time.timeScale = 1f; Time.captureDeltaTime = 1f / 30f;
            _segment = _step = _frame = _errors = 0; Seen.Clear();
            _segments = new[]
            {
                new Segment("idle", 6),
                new Segment("run-east", 36, Vector2.right),
                new Segment("planted-stop", 4),
                new Segment("reverse-west", 36, Vector2.left),
                new Segment("stop-west", 4),
                new Segment("dash-east", 8, begin: () => Dash(UnderworldCamera2D.IsoRightXZ)),
                new Segment("dash-west", 8, begin: () => Dash(-UnderworldCamera2D.IsoRightXZ)),
                new Segment("walk-diagonal", 14, new Vector2(.22f, .22f)),
                new Segment("soft-one", 10, begin: () => { Reset(); Call(_combat, "ExecuteSoftComboStrike", .4f, false); }),
                new Segment("soft-two", 10, begin: () => Call(_combat, "ExecuteSoftComboStrike", .4f, false)),
                new Segment("soft-three", 12, begin: () => Call(_combat, "ExecuteSoftComboStrike", .4f, false)),
                new Segment("hard-boom", 14, begin: () => { Reset(); Call(_combat, "ExecuteHardBoom", .8f, false); }),
                new Segment("singing", 20, begin: () => { Reset(); _animator.SetSinging(true); }),
                new Segment("song-walk", 12, Vector2.left),
                new Segment("song-resume", 8),
                new Segment("phrase-resolve", 10, begin: () => _animator.SetSinging(false)),
                new Segment("hymn", 16, begin: () => { Reset(); _animator.TriggerCadencePerformance(OrpheusAnimationState.HymnBeam); }),
                new Segment("sanctuary", 16, begin: () => { Reset(); _animator.TriggerCadencePerformance(OrpheusAnimationState.SanctuaryCast); }),
                new Segment("convergence", 16, begin: () => { Reset(); _animator.TriggerCadencePerformance(OrpheusAnimationState.ConvergenceCast); }),
                new Segment("aulos", 20, begin: () => { Reset(); _flute.EnterFluteStance(); }),
                new Segment("aulos-soft", 12, begin: () => _animator.TriggerFluteNote(false)),
                new Segment("aulos-hard", 12, begin: () => _animator.TriggerFluteNote(true)),
                new Segment("requiem", 16, begin: () => _animator.TriggerRequiem()),
                new Segment("flute-release", 6, begin: () => _flute.ExitFluteStance()),
                new Segment("light-hit", 10, begin: () => { Reset(); _animator.TriggerHit(false); }),
                new Segment("heavy-hit-and-rise", 36, begin: () => { Reset(); _animator.TriggerHit(true); }),
                new Segment("inspect", 18, begin: () => { Reset(); _animator.TriggerInspect(); }),
                new Segment("look-back", 18, begin: () => { Reset(); _animator.TriggerLookBack(false); }),
                new Segment("clear", 16, begin: () => { Reset(); _animator.TriggerChamberClear(); }),
                new Segment("fatal-look-back", 30, begin: () => { Reset(); _animator.TriggerLookBack(true); }),
                new Segment("grief-hold", 35, begin: () => { Reset(); _animator.TriggerGrief(); }),
                new Segment("defeat-hold", 25, begin: () => Reset()),
                new Segment("victory", 45, begin: () => _player.TriggerRunVictory())
            };
            _ready = true;
            Debug.Log("[Orpheus animation runtime] Prepared first chamber.");
        }

        private static void Reset()
        {
            _animator.ResetMotion(); _player.TeleportToChamberSpawn(_start);
            Set(_player, "<AimDirectionXZ>k__BackingField", UnderworldCamera2D.IsoRightXZ);
        }
        private static void Dash(Vector3 direction)
        {
            _animator.ResetMotion(); Set(_player, "_dashDirXZ", direction); Call(_player, "BeginDashCommon");
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling) return;
            if (!EditorApplication.isPlaying)
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "Deadline", float.MaxValue))
                {
                    File.WriteAllText(Path.Combine(Output, "failure.txt"), "Play Mode did not start or was interrupted.");
                    Finish(1);
                }
                return;
            }
            try
            {
                Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + "Deadline", 0), "Runtime verification timed out.");
                if (!_ready)
                {
                    IntroCinematicPlayer intro = Object.FindFirstObjectByType<IntroCinematicPlayer>();
                    if (intro != null) { intro.Skip(); return; }
                    if (UnderworldFrontEnd.Instance == null || UnderworldFrontEnd.BlocksMenusOrTransitions || OrpheusPlayerController.Instance == null) return;
                    if (VoiceDirector.Instance != null) { Object.Destroy(VoiceDirector.Instance); return; }
                    OrpheusHud.Instance?.DismissTutorialModal(); Prepare();
                }
                if (_lastFrame == Time.frameCount) return;
                _lastFrame = Time.frameCount;
                if (_segment >= _segments.Length)
                {
                    Require(_errors == 0, "Runtime logged " + _errors + " errors.");
                    Require(Seen.Contains("Run") && Seen.Contains("DashTravel") && Seen.Contains("SingSustain") && Seen.Contains("AulosLoop") && Seen.Contains("Rise"), "Missing a required live animation. Seen: " + string.Join(", ", Seen));
                    File.WriteAllText(Path.Combine(Output, "result.json"), "{\"result\":\"pass\",\"segments\":" + _segments.Length + ",\"renderedFrames\":" + _frame + ",\"states\":[\"" + string.Join("\",\"", Seen) + "\"]}");
                    Finish(0); return;
                }
                Segment segment = _segments[_segment];
                if (_step == 0) segment.begin?.Invoke();
                Vector3 before = _player.transform.position;
                const float dt = 1f / 30f;
                Call(_player, "Update25DIsometricMovement", dt, segment.movement, null, null);
                if (segment.name.StartsWith("planted-stop")) Require(Vector3.Distance(before, _player.transform.position) < .001f, "Released movement drifted.");
                _animator.Advance(dt);
                bool ascending = false;
                var map = PaintedChamberMap.Active;
                bool onStairs = map != null && map.definition.StairMotion(_player.transform.position - map.transform.position,
                    _player.transform.position - before, out ascending);
                _animator.Drive(new OrpheusAnimationInput
                {
                    groundDelta = _player.transform.position - before, deltaTime = dt,
                    aimDirection = _player.AimDirectionXZ, dashing = _player.IsDashing,
                    movementRequested = segment.movement.sqrMagnitude > .0025f,
                    onStairs = onStairs, ascending = ascending,
                    defeated = segment.name == "defeat-hold"
                });
                _player.GetComponent<OrpheusGroundShadow>().ApplyState(_player.IsDashing,
                    OrpheusGroundShadow.IsAttackState(_animator.CurrentState), PaintedChamberMap.Active?.definition);
                Seen.Add(_animator.CurrentState.ToString());
                Require(GameObject.Find("HadesDashAfterimage") == null, "A dash cloned the character.");
                if (_flute.IsFluteActive) Require(!_player.transform.Find("AulosFluteVisual").gameObject.activeSelf, "Procedural flute placeholder is visible.");
                Capture("frame-" + _frame.ToString("D4"), 960, 540);
                _frame++; _step++;
                if (_step >= segment.frames)
                {
                    Capture(segment.name, 1920, 1080);
                    _step = 0; _segment++;
                }
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(Output, "failure.txt"), error.ToString());
                Debug.LogException(error); Finish(1);
            }
        }

        private static void Capture(string name, int width, int height)
        {
            typeof(PaintedChamberVerification).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { "orpheus-" + name, width, height, false, null });
            string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers/orpheus-" + name + ".png"));
            File.Copy(source, Path.Combine(Output, name + ".png"), true);
            File.Delete(source);
        }

        private static void Finish(int code)
        {
            int viewed = SessionState.GetInt(Key + "Viewed", -1);
            if (viewed < 0) PlayerPrefs.DeleteKey(IntroCinematicPlayer.ViewedKey); else PlayerPrefs.SetInt(IntroCinematicPlayer.ViewedKey, viewed);
            PlayerPrefs.Save(); Time.captureDeltaTime = 0f;
            SessionState.SetBool(Key, false); EditorApplication.Exit(code);
        }
    }
}
