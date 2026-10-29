using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    /// <summary>Opt-in native Play Mode evidence for the painted run, grounded actors and guardian attacks.</summary>
    [InitializeOnLoad]
    public static class PaintedCombatArtVerification
    {
        private const string Key = "OrpheusBelow.PaintedCombatArtVerification";
        private static readonly BossIdentity[] Bosses = { BossIdentity.Tripod, BossIdentity.MaskedMonk,
            BossIdentity.MysticEyeWarden, BossIdentity.WingedSentinel, BossIdentity.Shepherd, BossIdentity.CerberusHoundOfHades };
        private static readonly float[] BossBaseHealth = { 220, 240, 280, 330, 310, 460 };
        private static readonly string[] BossNames = { "Tripod", "MaskedMonk", "MysticEyeWarden", "WingedSentinel", "Shepherd", "Cerberus" };
        private static readonly Vector2[] RunDirections = { Vector2.down, new Vector2(1, -1), new Vector2(1, 1), Vector2.up };
        private static readonly EurydiceFacing[] RunFacings = { EurydiceFacing.South, EurydiceFacing.SouthEast, EurydiceFacing.NorthEast, EurydiceFacing.North };
        private static int _phase, _step, _bossIndex, _errors, _lastFrame = -1;
        private static int _directionIndex, _directionStep;
        private static double _next;
        private static OrpheusPlayerController _player;
        private static OrpheusSpriteAnimator _animator;
        private static BlurredEurydice _companion;
        private static ShadeEnemy _shade;
        private static CerberusRhythmBoss _boss;
        private static Renderer[] _runHiddenRenderers;
        private static Vector3 _stage, _movementStart;
        private static EurydiceFacing _facing;
        private static readonly HashSet<int> RunFrames = new HashSet<int>();
        private static readonly HashSet<int> DirectionRunFrames = new HashSet<int>();
        private static readonly HashSet<string> AttackPaths = new HashSet<string>();
        private static readonly List<string> Captures = new List<string>();
        private static readonly List<BossEvidence> BossProof = new List<BossEvidence>();
        private static readonly List<DirectionEvidence> DirectionProof = new List<DirectionEvidence>();
        private static DirectionEvidence _currentDirection;
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedCombatArt"));

        [Serializable] private sealed class BossEvidence
        {
            public string boss, projectileAsset, capture;
            public float maxHealth;
        }
        [Serializable] private sealed class Evidence
        {
            public string result, error, facing;
            public int runtimeErrors, uniqueRunFrames, shadowChangedPixels;
            public int orpheusShadowPixels, eurydiceShadowPixels, enemyShadowPixels;
            public float actualRunDistance;
            public string[] captures;
            public BossEvidence[] bosses;
            public DirectionEvidence[] additionalRunDirections;
        }
        [Serializable] private sealed class DirectionEvidence
        {
            public string facing;
            public int uniqueRunFrames;
            public float actualRunDistance;
            public string[] captures;
        }
        private static readonly Evidence Proof = new Evidence();

        static PaintedCombatArtVerification()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool(Key, false) && EditorApplication.isPlaying && !stack.Contains("UnityEditor.Search.") &&
                    (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) _errors++;
            };
        }

        public static void BakeAndRunBatch()
        {
            OrpheusBroadOilAssetBuilder.Build();
            RunBatch();
        }

        public static void RunBatch()
        {
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers")));
            File.Delete(Path.Combine(Output, "result.json")); File.Delete(Path.Combine(Output, "failure.txt"));
            SessionState.SetInt(Key + "Viewed", PlayerPrefs.GetInt(IntroCinematicPlayer.ViewedKey, -1));
            SessionState.SetInt(Key + "Mode", PlayerPrefs.GetInt("OrpheusBelow.SensorSourceMode", -1));
            SessionState.SetInt(Key + "InputBackground", (int)InputSystem.settings.backgroundBehavior);
            SessionState.SetInt(Key + "InputEditor", (int)InputSystem.settings.editorInputBehaviorInPlayMode);
            PlayerPrefs.SetInt("OrpheusBelow.SensorSourceMode", 0);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _phase = _step = _bossIndex = _errors = 0; _next = 0; _lastFrame = -1;
            RunFrames.Clear(); AttackPaths.Clear(); Captures.Clear(); BossProof.Clear(); DirectionProof.Clear();
            EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(0));
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 300f);
            EditorApplication.isPlaying = true;
        }

        private static object Call(object target, string method, params object[] args)
        {
            var member = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Require(member != null, "Missing runtime method " + target.GetType().Name + "." + method);
            return member.Invoke(target, args);
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Painted combat art phase " + _phase + ": " + message); }
        private static void Step(int phase, double delay = .1)
        { _phase = phase; _next = EditorApplication.timeSinceStartup + delay; }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling) return;
            try
            {
                Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + "Deadline", 0), "300 second deadline exceeded.");
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < _next) return;
                if (_lastFrame == Time.frameCount) return;
                _lastFrame = Time.frameCount;
                if (_phase <= 2) { Boot(); return; }
                if (_phase == 3)
                {
                    // Let the companion's ordinary Start build its production visual and shadow.
                    if (_companion.GetComponent<EurydiceGroundShadow>() == null) return;
                    PrepareActors();
                    _runHiddenRenderers = _companion.GetComponentsInChildren<Renderer>(true)
                        .Concat(_shade.GetComponentsInChildren<Renderer>(true)).ToArray();
                    foreach (var renderer in _runHiddenRenderers) renderer.forceRenderingOff = true;
                    Step(4); return;
                }
                if (_phase == 4)
                {
                    Drive(Vector2.right); _step++;
                    if (_step < 4) return;
                    _facing = _animator.Facing;
                    for (int i = 0; i < 4; i++) Drive(Vector2.zero);
                    Require(_animator.CurrentState == OrpheusAnimationState.Idle, "Orpheus did not settle into idle.");
                    Capture("orpheus-idle-same-facing");
                    _movementStart = _player.transform.position; _step = 0; Proof.actualRunDistance = 0;
                    Step(5); return;
                }
                if (_phase == 5)
                {
                    // Reuse a clear stretch of real walkable floor without restarting the gait cycle.
                    if (Vector3.Distance(_movementStart, _player.transform.position) > 3f)
                    {
                        _player.transform.position = _movementStart;
                        Physics.SyncTransforms();
                    }
                    Vector3 before = _player.transform.position;
                    Drive(Vector2.right); _step++;
                    Proof.actualRunDistance += Vector3.Distance(before, _player.transform.position);
                    if (_animator.CurrentState == OrpheusAnimationState.Run)
                    {
                        Require(_animator.Facing == _facing, "Running changed the selected comparison facing.");
                        if (RunFrames.Add(_animator.CurrentFrame)) Capture("orpheus-run-frame-" + _animator.CurrentFrame.ToString("D2"));
                    }
                    if (RunFrames.Count < 16 && _step < 128) return;
                    Require(Proof.actualRunDistance > .75f && RunFrames.Count == 16,
                        "Actual controller movement did not show all sixteen running poses; seen=" + RunFrames.Count);
                    Proof.facing = _facing.ToString(); Proof.uniqueRunFrames = RunFrames.Count;
                    _directionIndex = 0; BeginDirection(); Step(51); return;
                }
                if (_phase == 51)
                {
                    Vector2 movement = RunDirections[_directionIndex];
                    Vector3 ahead = _player.transform.position + UnderworldCamera2D.InputToIsoWorldDir(movement) * .5f;
                    if (Vector3.Distance(_movementStart, _player.transform.position) > 3f ||
                        !PaintedChamberMap.Active.IsWalkable(ahead, .6f))
                    {
                        _player.transform.position = _movementStart;
                        Physics.SyncTransforms();
                    }
                    Vector3 before = _player.transform.position;
                    Drive(movement); _directionStep++;
                    _currentDirection.actualRunDistance += Vector3.Distance(before, _player.transform.position);
                    if (_animator.CurrentState == OrpheusAnimationState.Run)
                    {
                        Require(_animator.Facing == RunFacings[_directionIndex], "Actual running direction differs from input: " + _currentDirection.facing);
                        if (DirectionRunFrames.Add(_animator.CurrentFrame) && (_animator.CurrentFrame == 4 || _animator.CurrentFrame == 12))
                            Capture("orpheus-run-" + _currentDirection.facing + "-frame-" + _animator.CurrentFrame.ToString("D2"));
                    }
                    if (DirectionRunFrames.Count < 16 && _directionStep < 128) return;
                    Require(DirectionRunFrames.Count == 16 && _currentDirection.actualRunDistance > .75f,
                        "Direction did not traverse all sixteen running poses: " + _currentDirection.facing + ", frames=" + DirectionRunFrames.Count);
                    _currentDirection.uniqueRunFrames = DirectionRunFrames.Count;
                    _currentDirection.captures = new[] { "orpheus-run-" + _currentDirection.facing + "-frame-04.png", "orpheus-run-" + _currentDirection.facing + "-frame-12.png" };
                    DirectionProof.Add(_currentDirection); _directionIndex++;
                    if (_directionIndex < RunDirections.Length) { BeginDirection(); return; }
                    foreach (var renderer in _runHiddenRenderers) if (renderer != null) renderer.forceRenderingOff = false;
                    PrepareActors();
                    for (int i = 0; i < 5; i++) Drive(Vector2.zero);
                    Step(6); return;
                }
                if (_phase == 6)
                {
                    RefreshShadows();
                    var shadows = ShadowRenderers();
                    Require(shadows.Length >= 7, "Missing persistent shadows for Orpheus, Eurydice or shade.");
                    foreach (var shadow in shadows)
                    {
                        Require(shadow.enabled && shadow.gameObject.activeInHierarchy && shadow.color.a > .05f,
                            "Invisible persistent shadow: " + shadow.name);
                        Require(shadow.sharedMaterial != null && shadow.sharedMaterial.shader.name == "OrpheusBelow/GroundShadow",
                            "Ground shadow does not use its transparent paint shader: " + shadow.name);
                    }
                    Capture("three-actors-grounded-shadows");
                    foreach (var shadow in shadows) shadow.enabled = false;
                    Capture("three-actors-shadow-reference");
                    foreach (var shadow in shadows) shadow.enabled = true;
                    Proof.shadowChangedPixels = ChangedPixels("three-actors-grounded-shadows", "three-actors-shadow-reference");
                    Require(Proof.shadowChangedPixels > 35, "The real camera did not render visible shadow paint; changed pixels=" + Proof.shadowChangedPixels);
                    Proof.orpheusShadowPixels = CheckActorShadow(_player.transform, "orpheus");
                    Proof.eurydiceShadowPixels = CheckActorShadow(_companion.transform, "eurydice");
                    Proof.enemyShadowPixels = CheckActorShadow(_shade.transform, "enemy");
                    _shade.OnOrphicHit(5, .25f, false, _player.transform.position);
                    Step(7); return;
                }
                if (_phase == 7)
                {
                    var feedback = Object.FindFirstObjectByType<EnemyCombatFeedback>();
                    Require(feedback != null, "Missing enemy combat feedback."); Call(feedback, "LateUpdate");
                    Require(!feedback.GetComponentsInChildren<Text>(true).Any(t => t.name == "EnemyHealthValue"), "Above-enemy health numbers remain.");
                    Require(feedback.GetComponentsInChildren<Image>(true).Any(t => t.name == "HealthFill"), "Enemy health bars were removed.");
                    Require(feedback.GetComponentsInChildren<Text>(true).Any(t => t.name == "EnemyDamageNumber" && !string.IsNullOrEmpty(t.text)), "Damage numbers were removed.");
                    Capture("enemy-bars-and-damage-without-health-numbers", true);
                    _shade.gameObject.SetActive(false); _companion.gameObject.SetActive(false);
                    Step(8); return;
                }
                if (_phase == 8) { PrepareBoss(); Step(9, .25); return; }
                if (_phase == 9)
                {
                    CaptureBoss();
                    foreach (var wave in Object.FindObjectsByType<StageSweepWave2D>(FindObjectsSortMode.None)) Object.DestroyImmediate(wave.gameObject);
                    if (_player.IsIn2DBossStage) _player.Exit2DBossStage();
                    Object.DestroyImmediate(_boss.gameObject); _boss = null;
                    _bossIndex++;
                    if (_bossIndex < Bosses.Length) { Step(8); return; }
                    Require(AttackPaths.Count == 6, "Guardian projectiles do not use six distinct generated assets.");
                    Require(_errors == 0, "Runtime logged " + _errors + " errors.");
                    Finish(0); return;
                }
            }
            catch (Exception error)
            {
                Proof.error = error.ToString(); File.WriteAllText(Path.Combine(Output, "failure.txt"), Proof.error);
                Debug.LogException(error); Finish(1);
            }
        }

        private static void Boot()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var intro = Object.FindFirstObjectByType<IntroCinematicPlayer>();
            if (_phase == 0)
            {
                if (UnderworldFrontEnd.Instance == null) return;
                if (intro != null)
                {
                    if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
                    InputSystem.EnableDevice(Keyboard.current);
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(UnityEngine.InputSystem.Key.Backspace));
                    Step(1, .25); return;
                }
                Call(UnderworldFrontEnd.Instance, "BeginOrResume"); Step(1, .25); return;
            }
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            if (intro != null) { intro.Skip(); return; }
            if (UnderworldFrontEnd.Instance == null || _phase == 2 && UnderworldFrontEnd.BlocksMenusOrTransitions) return;
            if (_phase == 1) { Call(UnderworldFrontEnd.Instance, "BeginOrResume"); Step(2, .25); return; }
            if (OrpheusPlayerController.Instance == null || PaintedChamberMap.Active == null || Camera.main == null) return;
            if (VoiceDirector.Instance != null)
            {
                for (int i = 0; i < 30 && VoiceDirector.HasPendingSpeech; i++) VoiceDirector.Instance.SkipCurrentLine();
                Object.Destroy(VoiceDirector.Instance); return;
            }
            if (HadesChamberDirector.Instance != null)
            { HadesChamberDirector.Instance.enabled = false; HadesChamberDirector.Instance.StopAllCoroutines(); }
            OrpheusHud.Instance?.DismissTutorialModal();
            _player = OrpheusPlayerController.Instance; _animator = _player.GetComponent<OrpheusSpriteAnimator>();
            Require(_animator != null && _animator.UsesProductionArt, "Production Orpheus animation is unavailable.");
            _player.enabled = _animator.enabled = false;
            if (OrpheusLyreCombat.Instance != null) OrpheusLyreCombat.Instance.enabled = false;
            if (OrpheusFluteChannel.Instance != null) OrpheusFluteChannel.Instance.enabled = false;
            if (SensorPulseInput.Instance != null) SensorPulseInput.Instance.enabled = false;
            UnderworldCamera2D.Instance.enabled = false;
            foreach (var shade in Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None)) Object.Destroy(shade.gameObject);
            _stage = PaintedChamberMap.Active.NearestWalkable(PaintedChamberMap.Active.ToWorld(PaintedChamberMap.Active.definition.encounterCenter), .6f);
            _companion = Object.FindFirstObjectByType<BlurredEurydice>() ?? OrpheusPrototypeBuilder.AddCompanion(null, _player.transform);
            Time.timeScale = 0; Step(3, .25);
        }

        private static void PrepareActors()
        {
            var map = PaintedChamberMap.Active;
            _player.TeleportToChamberSpawn(map.NearestWalkable(_stage - UnderworldCamera2D.IsoRightXZ * 1.8f, .6f));
            _animator.ResetMotion();
            _companion.SnapBehindOrpheus(map.NearestWalkable(_stage - UnderworldCamera2D.IsoRightXZ * 4f, .5f));
            _companion.enabled = false;
            if (_shade == null)
            {
                var go = new GameObject("PaintedCombatArt_Shade");
                go.transform.position = map.NearestWalkable(_stage + UnderworldCamera2D.IsoRightXZ * 2.8f, .5f);
                _shade = go.AddComponent<ShadeEnemy>(); _shade.SetupArchetype(ShadeArchetype.BrimstoneCaster); _shade.enabled = false;
            }
            _shade.PaintedActor.Art.enabled = false;
            SetView(false); RefreshShadows(); Physics.SyncTransforms();
        }

        private static void Drive(Vector2 movement)
        {
            const float dt = 1f / 30f;
            Vector3 before = _player.transform.position;
            Call(_player, "Update25DIsometricMovement", dt, movement, null, null);
            _animator.Advance(dt);
            _animator.Drive(new OrpheusAnimationInput { groundDelta = _player.transform.position - before,
                deltaTime = dt, aimDirection = _player.AimDirectionXZ, movementRequested = movement.sqrMagnitude > 0 });
            _player.GetComponent<OrpheusGroundShadow>().ApplyState(false, false, PaintedChamberMap.Active.definition);
        }

        private static void BeginDirection()
        {
            _directionStep = 0; DirectionRunFrames.Clear();
            _currentDirection = new DirectionEvidence { facing = RunFacings[_directionIndex].ToString() };
            _player.transform.position = _movementStart;
            Physics.SyncTransforms();
        }

        private static void SetView(bool side)
        {
            Camera camera = Camera.main;
            camera.transform.rotation = side ? UnderworldCamera2D.Side2DRotation : RenderPipeline2DSetup.IsoCameraRotation;
            typeof(UnderworldCamera2D).GetProperty("CurrentBillboardRotation").SetValue(null, camera.transform.rotation);
            typeof(UnderworldCamera2D).GetProperty("SideViewBlend01").SetValue(UnderworldCamera2D.Instance, side ? 1f : 0f);
            typeof(UnderworldCamera2D).GetProperty("CurrentSideParallaxOffset").SetValue(UnderworldCamera2D.Instance, 0f);
            camera.aspect = 16f / 9f; camera.orthographicSize = side ? 5.6f : 4.5f;
            camera.transform.position = _stage + Vector3.up * (side ? 1.8f : 1.2f) - camera.transform.forward * 36f;
            _animator.BodyRenderer.transform.rotation = camera.transform.rotation;
            foreach (var art in Object.FindObjectsByType<EnemyArtAnimator>(FindObjectsSortMode.None)) art.transform.rotation = camera.transform.rotation;
        }

        private static void RefreshShadows()
        {
            _player.GetComponent<OrpheusGroundShadow>().ApplyState(false, false, PaintedChamberMap.Active.definition);
            foreach (var shadow in Object.FindObjectsByType<EurydiceGroundShadow>(FindObjectsSortMode.None)) Call(shadow, "LateUpdate");
            foreach (var shadow in Object.FindObjectsByType<EnemyGroundShadow>(FindObjectsSortMode.None)) Call(shadow, "LateUpdate");
        }

        private static SpriteRenderer[] ShadowRenderers() => new[] { _player.transform, _companion.transform, _shade.transform }
            .SelectMany(t => t.GetComponentsInChildren<SpriteRenderer>())
            .Where(r => r.sharedMaterial != null && r.sharedMaterial.shader.name == "OrpheusBelow/GroundShadow").ToArray();

        private static void PrepareBoss()
        {
            var go = new GameObject("PaintedCombatArt_" + BossNames[_bossIndex]); _boss = go.AddComponent<CerberusRhythmBoss>();
            _boss.ConfigureBoss(Bosses[_bossIndex], _stage); _boss.ActivateGuardianDuel(); _boss.enabled = false;
            _boss.GetComponent<PaintedEnemyActor>().Art.enabled = false;
            Require(Mathf.Abs(_boss.maxHealth - BossBaseHealth[_bossIndex] * 7.2f) < .1f && Mathf.Abs(_boss.CurrentHealth - _boss.maxHealth) < .1f,
                "Boss health is not doubled from the prior 3.6 multiplier: " + BossNames[_bossIndex] + "=" + _boss.maxHealth);
            SetView(true); RefreshShadows();
            // Invoke the actual gameplay factory; reflection also makes a missing migration an explicit runtime failure.
            var spawn = typeof(StageSweepWave2D).GetMethod("Spawn", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(BossIdentity) }, null);
            Require(spawn != null, "StageSweepWave2D.Spawn has no boss-specific artwork parameter.");
            spawn.Invoke(null, new object[] { _stage + UnderworldCamera2D.IsoRightXZ * 1.4f + Vector3.up * .72f, _stage, 0f, Bosses[_bossIndex] });
            foreach (var wave in Object.FindObjectsByType<StageSweepWave2D>(FindObjectsSortMode.None))
            { wave.enabled = false; wave.transform.rotation = Camera.main.transform.rotation; }
            Physics.SyncTransforms();
        }

        private static void CaptureBoss()
        {
            RefreshShadows();
            var wave = Object.FindFirstObjectByType<StageSweepWave2D>();
            Require(wave != null, "Real boss sweep projectile was not created.");
            var renderers = wave.GetComponentsInChildren<SpriteRenderer>();
            var painted = renderers.FirstOrDefault(r => r.sprite != null && AssetDatabase.GetAssetPath(r.sprite).Replace('\\', '/').Contains("/Resources/BossAttacks/"));
            Require(painted != null && painted.enabled && painted.gameObject.activeInHierarchy, "Actual projectile does not display generated boss artwork.");
            Require(renderers.All(r => !r.enabled || r.sprite != RenderPipeline2DSetup.DiamondSprite), "Actual projectile still shows a diamond placeholder.");
            string path = AssetDatabase.GetAssetPath(painted.sprite).Replace('\\', '/');
            Require(AttackPaths.Add(path), "Two guardians share the same attack art: " + path);
            string name = "boss-projectile-" + BossNames[_bossIndex]; Capture(name);
            BossProof.Add(new BossEvidence { boss = BossNames[_bossIndex], projectileAsset = path, maxHealth = _boss.maxHealth, capture = name + ".png" });
        }

        private static void Capture(string name, bool hud = false)
        {
            string temporary = "combat-art-" + name;
            typeof(PaintedChamberVerification).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { temporary, 1920, 1080, hud, null });
            string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers/" + temporary + ".png"));
            File.Copy(source, Path.Combine(Output, name + ".png"), true); File.Delete(source); Captures.Add(name + ".png");
        }

        private static int CheckActorShadow(Transform actor, string name)
        {
            var shadows = actor.GetComponentsInChildren<SpriteRenderer>().Where(r => r.sharedMaterial != null &&
                r.sharedMaterial.shader.name == "OrpheusBelow/GroundShadow").ToArray();
            Require(shadows.Length >= 2, "Persistent contact and cast shadows are missing for " + name);
            foreach (var shadow in shadows) shadow.enabled = false;
            string reference = "shadow-reference-without-" + name;
            try
            {
                Capture(reference);
                int changed = ChangedPixels("three-actors-grounded-shadows", reference);
                Require(changed > 10, name + " shadow paint was not visible in the rendered camera (" + changed + " pixels).");
                return changed;
            }
            finally { foreach (var shadow in shadows) shadow.enabled = true; }
        }

        private static int ChangedPixels(string first, string second)
        {
            var a = new Texture2D(2, 2); var b = new Texture2D(2, 2);
            try
            {
                a.LoadImage(File.ReadAllBytes(Path.Combine(Output, first + ".png")));
                b.LoadImage(File.ReadAllBytes(Path.Combine(Output, second + ".png")));
                var aa = a.GetPixels32(); var bb = b.GetPixels32(); int changed = 0;
                Require(aa.Length == bb.Length, "Shadow reference capture size changed.");
                for (int i = 0; i < aa.Length; i++)
                    if (Mathf.Max(Mathf.Abs(aa[i].r - bb[i].r), Mathf.Abs(aa[i].g - bb[i].g), Mathf.Abs(aa[i].b - bb[i].b)) > 5) changed++;
                return changed;
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }

        private static void Finish(int code)
        {
            Proof.result = code == 0 ? "pass" : "fail"; Proof.runtimeErrors = _errors;
            Proof.captures = Captures.ToArray(); Proof.bosses = BossProof.ToArray(); Proof.additionalRunDirections = DirectionProof.ToArray();
            File.WriteAllText(Path.Combine(Output, "result.json"), JsonUtility.ToJson(Proof, true));
            RestorePref(IntroCinematicPlayer.ViewedKey, Key + "Viewed"); RestorePref("OrpheusBelow.SensorSourceMode", Key + "Mode");
            InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)SessionState.GetInt(Key + "InputBackground", 0);
            InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key + "InputEditor", 0);
            PlayerPrefs.Save(); Time.timeScale = 1; SessionState.SetBool(Key, false);
            Debug.Log("[Painted combat art] " + Proof.result.ToUpperInvariant() + ": " + Output);
            EditorApplication.Exit(code);
        }
        private static void RestorePref(string pref, string saved)
        { int old = SessionState.GetInt(saved, -1); if (old < 0) PlayerPrefs.DeleteKey(pref); else PlayerPrefs.SetInt(pref, old); }
    }
}
