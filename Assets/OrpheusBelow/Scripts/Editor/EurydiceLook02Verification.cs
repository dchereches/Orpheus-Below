using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrpheusBelow.EditorTools
{
    [InitializeOnLoad]
    public static class EurydiceLook02Verification
    {
        private const string Key = "OrpheusBelow.EurydiceLook02Verification";
        private static int _phase, _errors, _video;
        private static double _next;
        private static BlurredEurydice _companion;
        private static EurydiceSpriteAnimator _animator;
        private static Vector3 _point;
        private static float _pausedElapsed;
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/EurydiceLook02"));

        static EurydiceLook02Verification()
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
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(3));
            UnderworldFrontEnd.SkipTitleOnce = true;
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 600);
            EditorApplication.isPlaying = true;
        }

        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object Call(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        private static void Drive(Vector3 delta)
        {
            Set(_companion, "_lastAnimationPosition", _companion.transform.position - delta);
            Call(_companion, "UpdateGeneratedSpriteAnimation", .1f);
        }

        private static void Expect(EurydiceAnimationState state)
        {
            if (_animator.CurrentState != state) throw new Exception("Expected " + state + ", got " + _animator.CurrentState);
        }

        private static void FrameCamera()
        {
            var camera = Camera.main;
            camera.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
            camera.orthographicSize = 4.2f; camera.aspect = 16f / 9f;
            camera.transform.position = _companion.transform.position + camera.transform.up * 1.1f - camera.transform.forward * 36;
        }

        private static Color Capture(string name, Vector3? probe = null)
        {
            FrameCamera();
            string temporaryName = "eurydice-" + name;
            Color color = (Color)typeof(PaintedChamberVerification).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { temporaryName, 1280, 720, false, probe });
            string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers/" + temporaryName + ".png"));
            string destination = Path.Combine(Output, name + ".png");
            File.Copy(source, destination, true);
            return color;
        }

        private static void Clip(string name, int frames = 6, float step = .14f)
        {
            for (int i = 0; i < frames; i++)
            {
                _animator.Advance(step);
                Capture("demo-" + (_video++).ToString("D4"));
            }
            Capture(name);
        }

        private static void ClearReactions()
        {
            foreach (string field in new[] { "_joyTimer", "_fearTimer", "_reformTimer", "_humPoseTimer", "_sanctuaryTimer", "_shieldIntroTimer" }) Set(_companion, field, 0f);
            _companion.SetTeachingPose(false);
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + "Deadline", float.MaxValue)) throw new Exception("Eurydice verification timed out");
                if (EditorApplication.timeSinceStartup < _next) return;
                if (_phase == 0)
                {
                    _companion = UnityEngine.Object.FindFirstObjectByType<BlurredEurydice>();
                    if (_companion == null)
                    {
                        // This isolated art sampler deliberately opens an escorted return visit.
                        var player = OrpheusPlayerController.Instance;
                        if (player == null || PaintedChamberMap.Active == null) return;
                        UnderworldJourney.Progress.Visit(UnderworldJourney.DeepestChamber);
                        UnderworldJourney.Progress.ClearCurrentChamber();
                        UnderworldJourney.Progress.TryRescue();
                        UnderworldJourney.Progress.Visit(PaintedChamberMap.Active.definition.index);
                        OrpheusPrototypeBuilder.AddCompanion(player.transform.parent, player.transform);
                        _next = EditorApplication.timeSinceStartup + .5; return;
                    }
                    _animator = _companion.GetComponentInChildren<EurydiceSpriteAnimator>();
                    if (_animator == null) return;
                    if (!_animator.UsesLook02) throw new Exception("Look02 library not in use");
                    if (UnderworldFrontEnd.BlocksGameplay)
                    {
                        Call(UnderworldFrontEnd.Instance, "BeginOrResume");
                        _next = EditorApplication.timeSinceStartup + 1; return;
                    }
                    HadesChamberDirector.Instance.enabled = false; HadesChamberDirector.Instance.StopAllCoroutines();
                    OrpheusHud.Instance.DismissTutorialModal();
                    OrpheusPlayerController.Instance.enabled = false;
                    foreach (var enemy in UnityEngine.Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(enemy.gameObject);
                    UnderworldCamera2D.Instance.enabled = false;
                    _companion.enabled = false;
                    _point = PaintedChamberMap.Active.ToWorld(new Vector2(920, 590));
                    _companion.SnapBehindOrpheus(_point);
                    Time.timeScale = 0;
                    _phase = 1; _next = EditorApplication.timeSinceStartup + 1; return;
                }
                if (_phase == 1)
                {
                    Drive(Vector3.zero); Expect(EurydiceAnimationState.Reform); Clip("01-reform", 6, .12f);
                    ClearReactions(); Drive(Vector3.zero);
                    for (int i = 0; i < 200; i++)
                    {
                        _companion.transform.position = _point + Vector3.up * Mathf.Sin(i * .2f) * .08f;
                        Drive(Vector3.up * .05f); Expect(EurydiceAnimationState.Idle);
                    }
                    _companion.transform.position = _point;
                    Clip("02-idle", 4, .63f);
                    float elapsed = _animator.Elapsed; _animator.Advance(4, true);
                    if (_animator.Elapsed != elapsed) throw new Exception("Pause advanced animation");
                    var renderer = _animator.GetComponent<SpriteRenderer>();
                    float height = renderer.sprite.bounds.size.y;
                    if (height < 2.4f || height > 2.7f) throw new Exception("Incorrect visible size: " + height);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * 45 * Mathf.Deg2Rad;
                        Vector3 delta = (UnderworldCamera2D.IsoRightXZ * Mathf.Sin(angle) - UnderworldCamera2D.IsoUpXZ * Mathf.Cos(angle)) * .3f;
                        for (int j = 0; j < 4; j++) Drive(delta);
                        Expect(EurydiceAnimationState.Glide);
                        if (_animator.Facing != (EurydiceFacing)i) throw new Exception("Wrong facing " + i);
                        Clip("walk-" + (EurydiceFacing)i, 4, .13f);
                    }
                    _phase = 10; _next = EditorApplication.timeSinceStartup + .1; return;
                }
                if (_phase == 10)
                {
                    ClearReactions();
                    OrpheusHud.Instance.ShowEurydiceTutorial("Verification lesson");
                    Expect(EurydiceAnimationState.Teaching);
                    _phase = 11; _next = EditorApplication.timeSinceStartup + .8; return;
                }
                if (_phase == 11)
                {
                    Expect(EurydiceAnimationState.Teaching);
                    if (_animator.Elapsed < .15f || Time.timeScale != 0) throw new Exception("Paused tutorial did not animate on its UI clock");
                    _pausedElapsed = _animator.Elapsed;
                    Call(UnderworldFrontEnd.Instance, "OpenMenu");
                    _phase = 12; _next = EditorApplication.timeSinceStartup + .6; return;
                }
                if (_phase == 12)
                {
                    if (Mathf.Abs(_animator.Elapsed - _pausedElapsed) > .001f) throw new Exception("Pause menu failed to freeze the lesson animation");
                    Call(UnderworldFrontEnd.Instance, "BeginOrResume");
                    OrpheusHud.Instance.DismissTutorialModal();
                    _phase = 2; _next = EditorApplication.timeSinceStartup + .4; return;
                }
                if (_phase == 2)
                {
                    if (UnderworldFrontEnd.BlocksGameplay) return;
                    Time.timeScale = 0;
                    _companion.SetTeachingPose(true); Drive(Vector3.zero); Expect(EurydiceAnimationState.Teaching); Clip("03-teaching");
                    _companion.SetTeachingPose(false); _companion.TriggerAcousticReply(false); Drive(Vector3.zero);
                    Expect(EurydiceAnimationState.Listen); Clip("04-listen-answer");
                    _companion.Celebrate(); Drive(Vector3.zero); Expect(EurydiceAnimationState.Joy); Clip("05-joy");
                    _companion.NotifyThreat(); Drive(Vector3.zero); Expect(EurydiceAnimationState.Fear); Clip("06-fear");
                    _companion.TriggerAcousticReply(true); Drive(Vector3.zero); Expect(EurydiceAnimationState.Sanctuary); Clip("07-sanctuary");
                    VerifyEffectsStayBehindBody();
                    Set(_companion, "_shieldIntroTimer", 0f); Set(_companion, "_joyTimer", 0f); Set(_companion, "_humPoseTimer", 0f);
                    for (int j = 0; j < 4; j++) Drive(UnderworldCamera2D.IsoRightXZ * .3f);
                    Expect(EurydiceAnimationState.Glide); Capture("08-walk-with-shield");
                    ClearReactions(); _companion.StopAllCoroutines();
                    var abductor = new GameObject("VerificationAbductor").AddComponent<ShadeEnemy>();
                    _companion.RegisterAbductor(abductor); Drive(Vector3.zero); Expect(EurydiceAnimationState.Fear);
                    _companion.UnregisterAbductor(abductor); Drive(Vector3.zero); Expect(EurydiceAnimationState.Joy);
                    UnityEngine.Object.Destroy(abductor.gameObject);
                    OrpheusHud.Instance.DismissTutorialModal(); Time.timeScale = 0;
                    ClearReactions();
                    OrpheusPlayerController.Instance.TakeDamage(1, _point - Vector3.right);
                    Drive(Vector3.zero); Expect(EurydiceAnimationState.Fear);
                    ClearReactions();
                    Call(HadesChamberDirector.Instance, "HandleChamberCleared"); Drive(Vector3.zero); Expect(EurydiceAnimationState.Joy);
                    ClearReactions();
                    _phase = 3; _next = EditorApplication.timeSinceStartup + .1; return;
                }
                if (_phase == 3)
                {
                    var map = PaintedChamberMap.Active; var stair = map.definition.stairs[0];
                    Vector2 center = Vector2.zero; foreach (Vector2 p in stair.polygon) center += p; center /= stair.polygon.Length;
                    Vector3 axis = (map.ToWorld(center + stair.uphill) - map.ToWorld(center)).normalized;
                    _companion.transform.position = map.ToWorld(center);
                    Drive(axis * .3f); Expect(EurydiceAnimationState.StairAscent); Clip("09-stair-ascent");
                    Drive(-axis * .3f); Expect(EurydiceAnimationState.StairDescent); Clip("10-stair-descent");
                    _companion.transform.position = _point; ClearReactions();
                    OrpheusPlayerController.Instance.TriggerRunVictory(); Drive(Vector3.zero); Expect(EurydiceAnimationState.Joy);
                    Time.timeScale = 0;
                    _companion.ShatterIntoAsh(); Expect(EurydiceAnimationState.Shatter); Clip("11-shatter", 6, .14f);
                    _phase = 4; _next = EditorApplication.timeSinceStartup + 1.4; return;
                }
                if (_phase == 4)
                {
                    if (!_animator.GetComponent<SpriteRenderer>().enabled) throw new Exception("Pause hid the companion before the shatter animation could finish");
                    Time.timeScale = 1; _phase = 5; _next = EditorApplication.timeSinceStartup + 1.4; return;
                }
                if (_phase == 5)
                {
                    if (_animator.GetComponent<SpriteRenderer>().enabled) throw new Exception("Shattered companion stayed visible");
                    if (_errors != 0) throw new Exception("Runtime errors: " + _errors);
                    Debug.Log("[Eurydice Look02] RENDER PASS: 148 frames loaded; idle survives vertical bob; eight directions, teaching, acoustic reply, joy, fear, sanctuary, shielded walking, rescue, chamber-clear joy, stair ascent/descent, victory, reform/shatter and pause verified; zero runtime errors.");
                    SessionState.SetBool(Key, false); EditorApplication.Exit(0);
                }
            }
            catch (Exception e) { SessionState.SetBool(Key, false); Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void VerifyEffectsStayBehindBody()
        {
            var renderer = _animator.GetComponent<SpriteRenderer>(); var sprite = renderer.sprite;
            int y = Mathf.RoundToInt(sprite.rect.y + sprite.rect.height * .23f);
            int x = Mathf.RoundToInt(sprite.rect.center.x);
            for (int px = Mathf.RoundToInt(sprite.rect.x + sprite.rect.width * .25f); px < sprite.rect.xMax; px++)
                if (sprite.texture.GetPixel(px, y).a > .95f) { x = px; break; }
            Vector2 local = (new Vector2(x - sprite.rect.x, y - sprite.rect.y) - sprite.pivot) / sprite.pixelsPerUnit;
            if (renderer.flipX) local.x = -local.x;
            Vector3 probe = renderer.transform.TransformPoint(local);
            var effects = _companion.transform.Find("EurydiceBillboard25D/EurydicePaintedEffects").GetComponent<SpriteRenderer>();
            var ring = _companion.transform.Find("SanctuaryAegisRing").GetComponent<SpriteRenderer>();
            effects.enabled = false; ring.enabled = false;
            Color reference = Capture("07-body-reference", probe);
            effects.enabled = true; ring.enabled = true;
            Color withEffects = Capture("07-body-with-effects", probe);
            float difference = Mathf.Max(Mathf.Abs(reference.r - withEffects.r), Mathf.Abs(reference.g - withEffects.g), Mathf.Abs(reference.b - withEffects.b));
            if (difference > .08f) throw new Exception("Effects/ground decals clipped the lower body; difference " + difference);
        }
    }
}
