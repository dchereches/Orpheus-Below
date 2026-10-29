using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    /// <summary>Opt-in native evidence against the eight saved scenes, never a rebuilt map clone.</summary>
    [InitializeOnLoad]
    public static class PaintedNavigationVerification
    {
        private const string Key = "OrpheusBelow.ReviewedNavigation";
        private const string ModePref = "OrpheusBelow.SensorSourceMode";
        private const float Radius = .38f, GridRadius = .42f;
        private const int InitialGridStep = 8;
        private static int _gridStep = InitialGridStep;
        private static int GridWidth => 1536 / _gridStep + 1;
        private static int GridHeight => 1024 / _gridStep + 1;
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly string[] CalibrationPrefs = { "OrpheusBelow.MicSensitivity", "OrpheusBelow.SoftThreshold", "OrpheusBelow.BoomThreshold" };
        private static readonly MethodInfo Move = typeof(OrpheusPlayerController).GetMethod("MoveSweepXZ", InstanceFlags);
        private static readonly MethodInfo Render = typeof(PaintedChamberVerification).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly List<MapEvidence> Maps = new List<MapEvidence>();
        private static int _index, _phase, _pairIndex, _errors, _lastFrame = -1;
        private static double _next;
        private static OrpheusPlayerController _player;
        private static OrpheusSpriteAnimator _animator;
        private static PaintedChamberMap _map;
        private static PaintedChamberNavigationTrace _trace;
        private static MapEvidence _proof;
        private static List<Anchor> _anchors;
        private static bool[] _clear;
        private static bool[] _mathClear;
        private static int[] _parents;
        private static Vector3[] _grid;
        private static readonly List<Vector3> PositiveSources = new List<Vector3>();
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/ReviewedNavigation"));

        [Serializable] private sealed class RunEvidence
        {
            public string result, error;
            public int runtimeErrors, savedScenesChecked;
            public MapEvidence[] maps;
        }
        [Serializable] private sealed class MapEvidence
        {
            public int chamberIndex, clearGridNodes, reachableGridNodes, savedCoverColliders, sourceRasterWidth, sourceRasterHeight;
            public int sourceCoordinateWidth = 1536, sourceCoordinateHeight = 1024;
            public string sourceRasterPolicy = "Full original raster preserved; trace coordinates use the painting's normalized 1536 x 1024 authoring reference";
            public string scene, result = "running", source = "Resources/ChamberNavigation", phase;
            public string occlusionMetric = "Raw RGB ON/OFF change >5 gated by OFF hero-minus-blank contrast >5, with actual occlusion requiring ON hero contrast <= half OFF contrast. Behind needs >15 attenuated pixels; front permits <=20. Raw changes remain diagnostic. Blank subtraction is used only outside hero bounds. AA disabled";
            public float playerRadius = Radius, graphRadius = GridRadius;
            public int graphStepPixels = InitialGridStep, graphDirections = 8;
            public string graphEdgePolicy = "Eight-direction grid, initially 8px with one 4px retry only if a required anchor is unreachable/missing/farther than1.05m. Every edge, including diagonals, requires an exact .42m mathematical sweep and an actual .38m production controller sweep. No corner cutting";
            public List<string> failures = new List<string>();
            public List<ProbeEvidence> positive = new List<ProbeEvidence>();
            public List<BlockedEvidence> blocked = new List<BlockedEvidence>();
            public List<ReachEvidence> reachability = new List<ReachEvidence>();
            public List<RouteEvidence> routes = new List<RouteEvidence>();
            public List<OcclusionEvidence> occlusion = new List<OcclusionEvidence>();
            public List<string> captures = new List<string>();
            public GraphDiagnostics graphDiagnostics = new GraphDiagnostics();
            public List<GraphAttemptEvidence> graphAttempts = new List<GraphAttemptEvidence>();
        }
        [Serializable] private sealed class GraphAttemptEvidence
        {
            public int stepPixels, directions = 8, clearNodes, reachableNodes, mathematicalClearNodes, mathematicalReachableNodes, nativePointRejectedNodes;
            public double elapsedSeconds;
            public string[] disconnectedRequiredAnchors;
        }
        [Serializable] private sealed class GraphDiagnostics
        {
            public string startFailure;
            public int mathematicalClearNodes, mathematicalReachableNodes, nativePointRejectedNodes;
            public int mathematicalRejectedEdgeAttempts, nativeRejectedEdgeAttempts;
            public int finalFrontierMathRejectedEdges, finalFrontierNativeRejectedEdges, finalFrontierPhysicsRejectedNodes;
            public Vector3 productionBodyCenter;
            public List<EdgeEvidence> firstMathRejectedEdges = new List<EdgeEvidence>();
            public List<EdgeEvidence> firstNativeRejectedEdges = new List<EdgeEvidence>();
            public List<EdgeEvidence> finalDisconnectedFrontier = new List<EdgeEvidence>();
            public List<PointCollisionEvidence> firstPhysicsRejectedNodes = new List<PointCollisionEvidence>();
        }
        [Serializable] private sealed class EdgeEvidence
        {
            public string reason;
            public int fromNode, toNode;
            public Vector2 fromPixel, toPixel;
            public Vector3 fromWorld, requestedWorld, actualWorld, delta, mathNormal42, mathNormal38;
            public float endpointError, mathFraction42, mathFraction38;
            public bool mathHit42, mathHit38;
            public CastHitEvidence[] sphereCastHits;
            public PointCollisionEvidence originPenetration, requestedPenetration, actualPenetration;
        }
        [Serializable] private sealed class PointCollisionEvidence
        {
            public Vector2 sourcePixel;
            public Vector3 world;
            public float radius;
            public PenetrationEvidence[] obstacles;
        }
        [Serializable] private sealed class PenetrationEvidence
        {
            public string colliderPath, colliderType, kind;
            public bool penetrating;
            public float depth;
            public Vector3 escape;
        }
        [Serializable] private sealed class CastHitEvidence
        {
            public string colliderPath, colliderType, kind, meshAsset;
            public bool convexMesh, reducesInitialPenetration, initialPenetration, requestedPenetration, landingPenetration;
            public int meshVertices, meshTriangles;
            public float distance, initialDepth, requestedDepth, landingDepth;
            public Vector3 point, normal, initialEscape, requestedEscape, landingEscape;
        }
        [Serializable] private sealed class ProbeEvidence
        {
            public string name;
            public Vector2 sourcePixel;
            public Vector3 world, afterZero, microTarget, microEnd;
            public bool sourceFloor, physicalClear, microFound, pass;
            public float zeroCorrection, microError;
        }
        [Serializable] private sealed class BlockedEvidence
        {
            public string name;
            public Vector2 sourcePixel;
            public Vector3 origin, requested, walkEnd, dashEnd;
            public bool sourceBlocked, walkOnFloor, dashOnFloor, pass;
            public float walkTargetDistance, dashTargetDistance;
        }
        [Serializable] private sealed class ReachEvidence
        {
            public string name;
            public Vector2 sourcePixel;
            public Vector3 nearestClear;
            public float nearestDistance;
            public bool pass;
        }
        [Serializable] private sealed class RouteEvidence
        {
            public string target;
            public int gridNodes, actualMicroSteps;
            public float requestedDistance, actualDistance, endpointError;
            public bool pass;
            public Vector3[] sampledWorldRoute;
        }
        [Serializable] private sealed class OcclusionEvidence
        {
            public string name, coverName, error;
            public Vector2 behindPixel, frontPixel;
            public Rect behindHeroViewport, frontHeroViewport;
            public float cameraOrthoSize;
            public Vector3 cameraWorldPosition;
            public int behindChangedPixels, frontChangedPixels, behindAttenuatedPixels, frontAttenuatedPixels;
            public int behindOutsideHeroPixels, frontOutsideHeroPixels, blankBackgroundChangedPixels;
            public bool pass;
            public string[] captures;
        }
        private sealed class Anchor
        {
            public string name;
            public Vector2 pixel;
            public Anchor(string label, Vector2 point) { name = label; pixel = point; }
        }

        static PaintedNavigationVerification()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool(Key, false) && EditorApplication.isPlaying &&
                    !(stack ?? "").Contains("UnityEditor.Search.") &&
                    (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) _errors++;
            };
        }

        public static void BakeAndRunBatch()
        {
            PaintedChamberSceneBuilder.BuildScenes();
            RunBatch();
        }

        public static void RunBatch()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Start reviewed navigation from Edit Mode.");
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers")));
            File.Delete(Path.Combine(Output, "result.json"));
            File.Delete(Path.Combine(Output, "failure.txt"));
            SavePreferences();
            PlayerPrefs.SetInt(ModePref, (int)SensorSourceMode.KeyboardAndMouseOnly);
            // Exercise the real modal/intro flow even if this user's intro was already viewed.
            PlayerPrefs.DeleteKey(IntroCinematicPlayer.ViewedKey);
            PlayerPrefs.Save();
            SetInputSettings();
            _index = _phase = _pairIndex = _errors = 0; _lastFrame = -1; _next = 0;
            Maps.Clear();
            SessionState.SetFloat(Key + ".Deadline", (float)EditorApplication.timeSinceStartup + 1200f);
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(0));
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling) return;
            try
            {
                Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + ".Deadline", 0), "1200 second native verification deadline exceeded.");
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < _next || _lastFrame == Time.frameCount) return;
                _lastFrame = Time.frameCount;
                if (_phase == 0) { BootstrapAndPrepare(); return; }
                if (_phase == 1)
                {
                    _proof.phase = "native source probes";
                    foreach (var anchor in _anchors) VerifyPositive(anchor);
                    foreach (var probe in _trace.blockedProbes) VerifyBlocked(probe);
                    SaveMap(); Step(2); return;
                }
                if (_phase == 2)
                {
                    _proof.phase = "8 pixel native-clear floor graph"; SaveMap();
                    BuildGraph();
                    foreach (var anchor in _anchors) VerifyReachable(anchor);
                    SaveMap(); Step(3); return;
                }
                if (_phase == 3)
                {
                    _proof.phase = "real controller routes";
                    WalkRoute("encounter", _trace.encounterCenter);
                    WalkRoute("returnSpawn", _trace.returnSpawn);
                    WalkRoute("exitApproach", _trace.exit);
                    WalkRoute("returnExitApproach", _trace.returnExit);
                    SaveMap(); Step(4); return;
                }
                if (_phase == 4)
                {
                    _proof.phase = "source painting and F3 overview";
                    Overview(); SaveMap(); _pairIndex = 0; Step(5); return;
                }
                if (_phase == 5)
                {
                    _proof.phase = "native occlusion " + _pairIndex;
                    if (_pairIndex < _trace.occlusionProbes.Length)
                    { VerifyOcclusion(_trace.occlusionProbes[_pairIndex++]); SaveMap(); Step(5); return; }
                    _proof.result = _proof.failures.Count == 0 ? "pass" : "fail"; _proof.phase = "complete";
                    SaveMap(); Debug.Log("[Reviewed navigation] Chamber " + _index + " " + _proof.result + ": " + _proof.failures.Count + " failures.");
                    _index++;
                    if (_index == 8) { Finish(null); return; }
                    Require(PaintedChamberScene.Travel(_index, false), "Could not load saved chamber scene " + _index);
                    Step(0, .4);
                }
            }
            catch (Exception error) { Finish(error.ToString()); }
        }

        private static void BootstrapAndPrepare()
        {
            SetInputSettings();
            var selection = Object.FindFirstObjectByType<InputSelectionModal>();
            if (selection != null) { if (!selection.IsConfirmed) selection.Confirm(); return; }
            var intro = Object.FindFirstObjectByType<IntroCinematicPlayer>();
            if (intro != null) { intro.Skip(); return; }
            var front = UnderworldFrontEnd.Instance;
            if (front == null) return;
            if (UnderworldFrontEnd.BlocksMenusOrTransitions)
            {
                // The boot intro may still be fading out. BeginOrResume guards its own active fade;
                // retry after it settles rather than treating an ignored early call as completion.
                Call(front, "BeginOrResume"); _next = EditorApplication.timeSinceStartup + .3;
                return;
            }
            _map = PaintedChamberMap.Active; _player = OrpheusPlayerController.Instance;
            if (_map == null || _map.definition == null || _map.definition.index != _index || _player == null || Camera.main == null) return;
            Require(SceneManager.GetActiveScene().path == PaintedChamberScene.ScenePath(_index) &&
                _map.gameObject.scene.path == PaintedChamberScene.ScenePath(_index), "Active map does not belong to the saved scene.");
            if (VoiceDirector.Instance != null) { Object.DestroyImmediate(VoiceDirector.Instance); }
            if (HadesChamberDirector.Instance != null)
            { HadesChamberDirector.Instance.enabled = false; HadesChamberDirector.Instance.StopAllCoroutines(); }
            OrpheusHud.Instance?.DismissTutorialModal(); OrpheusHud.Instance?.HideSubtitle();
            if (_player.IsIn2DBossStage) _player.Exit2DBossStage();
            _animator = _player.GetComponent<OrpheusSpriteAnimator>();
            Require(_animator != null && _animator.UsesProductionArt, "Production hero art unavailable.");
            _player.enabled = _animator.enabled = false;
            if (OrpheusLyreCombat.Instance != null) OrpheusLyreCombat.Instance.enabled = false;
            if (OrpheusFluteChannel.Instance != null) OrpheusFluteChannel.Instance.enabled = false;
            if (SensorPulseInput.Instance != null) SensorPulseInput.Instance.enabled = false;
            Require(UnderworldCamera2D.Instance != null, "Native camera driver missing.");
            UnderworldCamera2D.Instance.enabled = false;
            typeof(UnderworldCamera2D).GetProperty("CurrentBillboardRotation").SetValue(null, RenderPipeline2DSetup.IsoCameraRotation);
            typeof(UnderworldCamera2D).GetProperty("SideViewBlend01").SetValue(UnderworldCamera2D.Instance, 0f);
            typeof(UnderworldCamera2D).GetProperty("CurrentSideParallaxOffset").SetValue(UnderworldCamera2D.Instance, 0f);
            foreach (var boss in Object.FindObjectsByType<CerberusRhythmBoss>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(boss.gameObject);
            foreach (var shade in Object.FindObjectsByType<ShadeEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(shade.gameObject);
            foreach (var companion in Object.FindObjectsByType<BlurredEurydice>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(companion.gameObject);
            foreach (var actor in Object.FindObjectsByType<PaintedActorDepth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (actor != null && actor.gameObject != _player.gameObject && !actor.transform.IsChildOf(_player.transform)) Object.DestroyImmediate(actor.gameObject);
            foreach (var door in Object.FindObjectsByType<ChamberExitDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                door.SetUnlocked(true); door.enabled = false;
                // Unity may deliver trigger callbacks to disabled components. No trigger simulation is needed here.
                foreach (var col in door.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            }
            foreach (var particles in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var renderer in _player.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = renderer != _animator.BodyRenderer;
            Call(_player, "UpdateVisualPose"); _animator.ResetMotion(); _animator.BodyRenderer.enabled = true;
            DisableTemporalAA(); Time.timeScale = 0f; Physics.SyncTransforms();
            Require(Move != null && Render != null, "Required native sweep/capture method missing.");
            Require(Mathf.Abs(_player.bodyRadius - Radius) < .0001f && Mathf.Abs(_player.GetComponent<SphereCollider>().radius - Radius) < .0001f,
                "Verification requires the production .38 metre player body.");
            _trace = PaintedChamberNavigationTrace.Load(_index);
            _proof = new MapEvidence { chamberIndex = _index, scene = SceneManager.GetActiveScene().path, phase = "prepared" };
            Maps.Add(_proof); PositiveSources.Clear();
            Check(_trace.walkableProbes != null && _trace.walkableProbes.Length >= 12, "Trace needs at least twelve independent positive source probes.");
            Check(_trace.blockedProbes != null && _trace.blockedProbes.Length >= 12, "Trace needs at least twelve independent blocked source probes.");
            Require(_trace.walkableProbes != null && _trace.blockedProbes != null && _trace.occlusionProbes != null, "Incomplete trace arrays.");
            Check(_trace.occlusionProbes.Length >= 3, "Trace needs at least three independent behind/front pairs.");
            var coverColliders = _map.coverRoot.GetComponentsInChildren<MeshCollider>(true);
            _proof.savedCoverColliders = coverColliders.Length;
            Check(coverColliders.Length == _trace.cover.Length && coverColliders.All(c => !c.convex && c.sharedMesh != null), "Saved cover must contain every reviewed static, concave footprint mesh.");
            _anchors = new List<Anchor> { new Anchor("entrance", _trace.entrance), new Anchor("returnSpawn", _trace.returnSpawn),
                new Anchor("encounter", _trace.encounterCenter), new Anchor("exitApproach", _trace.exit), new Anchor("returnExitApproach", _trace.returnExit) };
            if (_index == 7) _anchors.Add(new Anchor("reunion", _trace.reunionPoint));
            _anchors.AddRange(_trace.walkableProbes.Select(p => new Anchor("positive:" + p.name, p.Point)));
            for (int i = 0; i < _trace.enemySpawns.Length; i++) _anchors.Add(new Anchor("enemy:" + i, _trace.enemySpawns[i]));
            foreach (var pair in _trace.occlusionProbes)
            { _anchors.Add(new Anchor("behind:" + pair.name, pair.behind)); _anchors.Add(new Anchor("front:" + pair.name, pair.front)); }
            SaveMap(); Debug.Log("[Reviewed navigation] Testing saved chamber " + _index); Step(1);
        }

        private static void VerifyPositive(Anchor anchor)
        {
            Vector3 point = _map.ToWorld(anchor.pixel);
            var proof = new ProbeEvidence { name = anchor.name, sourcePixel = anchor.pixel, world = point,
                sourceFloor = _map.IsWalkable(point, Radius), physicalClear = PhysicalClear(point, Radius) };
            Place(point); Sweep(Vector3.zero, false); proof.afterZero = _player.transform.position;
            proof.zeroCorrection = Vector3.Distance(point, proof.afterZero);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                Vector3 candidate = point + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * .14f;
                if (!_map.IsWalkable(candidate, Radius) || !PhysicalClear(candidate, Radius)) continue;
                proof.microFound = true; proof.microTarget = candidate; Place(point); Sweep(candidate - point, false);
                proof.microEnd = _player.transform.position; proof.microError = Vector3.Distance(candidate, proof.microEnd); break;
            }
            proof.pass = proof.sourceFloor && proof.physicalClear && proof.zeroCorrection <= .04f && proof.microFound && proof.microError <= .05f &&
                _map.IsWalkable(proof.microEnd, Radius - .002f);
            _proof.positive.Add(proof); Check(proof.pass, "Invalid native positive source case " + anchor.name);
            if (proof.pass) PositiveSources.Add(point);
        }

        private static void VerifyBlocked(PaintedChamberNavigationTrace.Probe probe)
        {
            Vector3 target = _map.ToWorld(probe.Point);
            var proof = new BlockedEvidence { name = probe.name, sourcePixel = probe.Point, requested = target,
                sourceBlocked = !_map.IsWalkable(target, 0f) };
            _proof.blocked.Add(proof);
            if (PositiveSources.Count == 0) { Check(false, "No valid source-positive origin for blocked case " + probe.name); return; }
            proof.origin = PositiveSources.OrderBy(p => (p - target).sqrMagnitude).First();
            Place(proof.origin); Sweep(target - proof.origin, false); proof.walkEnd = _player.transform.position;
            proof.walkTargetDistance = Vector3.Distance(target, proof.walkEnd);
            proof.walkOnFloor = _map.IsWalkable(proof.walkEnd, Radius - .002f) && PhysicalClear(proof.walkEnd, Radius - .02f);
            Place(proof.origin); Sweep(target - proof.origin, true); proof.dashEnd = _player.transform.position;
            proof.dashTargetDistance = Vector3.Distance(target, proof.dashEnd);
            proof.dashOnFloor = _map.IsWalkable(proof.dashEnd, Radius - .002f) && PhysicalClear(proof.dashEnd, Radius - .02f);
            proof.pass = proof.sourceBlocked && proof.walkOnFloor && proof.dashOnFloor && proof.walkTargetDistance > .05f && proof.dashTargetDistance > .05f;
            Check(proof.pass, "Native blocked walk/dash source case failed: " + probe.name);
        }

        private static bool PhysicalClear(Vector3 position, float radius)
        {
            var body = _player.GetComponent<SphereCollider>(); float original = body.radius; body.radius = radius;
            try
            {
                foreach (var col in Physics.OverlapSphere(position + Vector3.up * .5f, radius, ~0, QueryTriggerInteraction.Ignore))
                    if (col.GetComponent<IsoObstacle>() != null && Physics.ComputePenetration(body, position, Quaternion.identity,
                        col, col.transform.position, col.transform.rotation, out _, out float depth) && depth > .002f) return false;
                return true;
            }
            finally { body.radius = original; }
        }

        private static void BuildGraph()
        {
            _proof.graphAttempts.Clear();
            foreach (int step in new[] { InitialGridStep, 4 })
            {
                double started = EditorApplication.timeSinceStartup;
                BuildGraphAttempt(step);
                var disconnected = new List<string>();
                foreach (var anchor in _anchors)
                {
                    Vector3 point = _map.ToWorld(anchor.pixel); int node = NearestGrid(point);
                    if (node < 0 || _parents[node] == -2 || Vector3.Distance(_grid[node], point) > 1.05f) disconnected.Add(anchor.name);
                }
                var diagnostic = _proof.graphDiagnostics;
                _proof.graphAttempts.Add(new GraphAttemptEvidence { stepPixels = step, clearNodes = _proof.clearGridNodes,
                    reachableNodes = _proof.reachableGridNodes, mathematicalClearNodes = diagnostic.mathematicalClearNodes,
                    mathematicalReachableNodes = diagnostic.mathematicalReachableNodes, nativePointRejectedNodes = diagnostic.nativePointRejectedNodes,
                    elapsedSeconds = EditorApplication.timeSinceStartup - started, disconnectedRequiredAnchors = disconnected.ToArray() });
                SaveMap();
                if (disconnected.Count == 0 || step == 4) break;
                Debug.Log("[Reviewed navigation] Chamber " + _index + " retrying 4px native swept graph: " + disconnected.Count + " required anchors disconnected on 8px graph.");
            }
        }

        private static void BuildGraphAttempt(int step)
        {
            _gridStep = step; _proof.graphStepPixels = step;
            _proof.clearGridNodes = _proof.reachableGridNodes = 0;
            _proof.graphDiagnostics = new GraphDiagnostics();
            _proof.phase = "native swept graph attempt " + step + "px"; SaveMap();
            _clear = new bool[GridWidth * GridHeight]; _mathClear = new bool[_clear.Length];
            _parents = Enumerable.Repeat(-2, _clear.Length).ToArray(); _grid = new Vector3[_clear.Length];
            GraphDiagnostics diagnostic = _proof.graphDiagnostics;
            diagnostic.productionBodyCenter = _player.GetComponent<SphereCollider>().center;
            for (int y = 0; y < GridHeight; y++)
            {
                CheckDeadline();
                for (int x = 0; x < GridWidth; x++)
                {
                    int n = y * GridWidth + x; Vector3 point = _map.ToWorld(new Vector2(x * _gridStep, y * _gridStep)); _grid[n] = point;
                    _mathClear[n] = _map.IsWalkable(point, GridRadius);
                    _clear[n] = _mathClear[n] && PhysicalClear(point, GridRadius);
                    if (_mathClear[n]) diagnostic.mathematicalClearNodes++;
                    if (_mathClear[n] && !_clear[n])
                    {
                        diagnostic.nativePointRejectedNodes++;
                        if (diagnostic.firstPhysicsRejectedNodes.Count < 12) diagnostic.firstPhysicsRejectedNodes.Add(PointCollisions(point, GridRadius));
                    }
                    if (_clear[n]) _proof.clearGridNodes++;
                }
            }
            int start = NearestGrid(_map.ToWorld(_trace.entrance));
            if (start < 0 || Vector3.Distance(_grid[start], _map.ToWorld(_trace.entrance)) > 1.05f)
            {
                // Coarse sampling may miss a narrow approach. Only the final graph emits source-case failures.
                diagnostic.startFailure = "No native-clear graph start within 1.05m of exact reviewed entrance.";
                SaveMap(); return;
            }
            diagnostic.mathematicalReachableNodes = MathematicalReachableCount(start);
            var queue = new Queue<int>(); queue.Enqueue(start); _parents[start] = -1;
            while (queue.Count > 0)
            {
                int n = queue.Dequeue(); _proof.reachableGridNodes++;
                if (_proof.reachableGridNodes % 1000 == 0) { CheckDeadline(); SaveMap(); }
                foreach (int to in AdjacentGridNodes(n)) Visit(n, to, queue);
            }
            CaptureDisconnectedFrontier();
            Debug.Log("[Reviewed navigation] Chamber " + _index + " " + step + "px graph diagnostic: math=" + diagnostic.mathematicalReachableNodes + "/" + diagnostic.mathematicalClearNodes +
                ", native=" + _proof.reachableGridNodes + "/" + _proof.clearGridNodes + ", pointReject=" + diagnostic.nativePointRejectedNodes +
                ", frontier math/native/point=" + diagnostic.finalFrontierMathRejectedEdges + "/" + diagnostic.finalFrontierNativeRejectedEdges + "/" + diagnostic.finalFrontierPhysicsRejectedNodes);
            SaveMap();
        }

        private static void Visit(int from, int to, Queue<int> queue)
        {
            if (!_clear[to] || _parents[to] != -2) return;
            Vector3 delta = _grid[to] - _grid[from];
            if (_map.SweepMovement(_grid[from], delta, GridRadius, out float fraction, out _) && fraction < .999f)
            {
                _proof.graphDiagnostics.mathematicalRejectedEdgeAttempts++;
                if (_proof.graphDiagnostics.firstMathRejectedEdges.Count < 8)
                    _proof.graphDiagnostics.firstMathRejectedEdges.Add(DescribeEdge(from, to, "mathematical .42 radius sweep rejected", false));
                return;
            }
            // The graph edge itself is exercised by the real .38m native controller and saved physics.
            Place(_grid[from]); Sweep(delta, false);
            if (Vector3.Distance(_player.transform.position, _grid[to]) > .05f)
            {
                _proof.graphDiagnostics.nativeRejectedEdgeAttempts++;
                if (_proof.graphDiagnostics.firstNativeRejectedEdges.Count < 16)
                    _proof.graphDiagnostics.firstNativeRejectedEdges.Add(DescribeEdge(from, to, "production .38 radius MoveSweepXZ rejected", true));
                return;
            }
            _parents[to] = from; queue.Enqueue(to);
        }

        private static int MathematicalReachableCount(int start)
        {
            var seen = new bool[_clear.Length]; var queue = new Queue<int>(); queue.Enqueue(start); seen[start] = true; int count = 0;
            while (queue.Count > 0)
            {
                int from = queue.Dequeue(); count++;
                if (count % 1000 == 0) CheckDeadline();
                foreach (int to in AdjacentGridNodes(from))
                {
                    if (seen[to] || !_mathClear[to]) continue;
                    if (_map.SweepMovement(_grid[from], _grid[to] - _grid[from], GridRadius, out float fraction, out _) && fraction < .999f) continue;
                    seen[to] = true; queue.Enqueue(to);
                }
            }
            return count;
        }

        private static IEnumerable<int> AdjacentGridNodes(int n)
        {
            int x = n % GridWidth, y = n / GridWidth;
            if (x > 0) yield return n - 1; if (x < GridWidth - 1) yield return n + 1;
            if (y > 0) yield return n - GridWidth; if (y < GridHeight - 1) yield return n + GridWidth;
            if (x > 0 && y > 0) yield return n - GridWidth - 1;
            if (x < GridWidth - 1 && y > 0) yield return n - GridWidth + 1;
            if (x > 0 && y < GridHeight - 1) yield return n + GridWidth - 1;
            if (x < GridWidth - 1 && y < GridHeight - 1) yield return n + GridWidth + 1;
        }

        private static void CaptureDisconnectedFrontier()
        {
            var diagnostic = _proof.graphDiagnostics; int mathSamples = 0, nativeSamples = 0, pointSamples = 0;
            for (int from = 0; from < _parents.Length; from++)
            {
                if (_parents[from] == -2) continue;
                foreach (int to in AdjacentGridNodes(from))
                {
                    if (!_mathClear[to] || _parents[to] != -2) continue;
                    string reason;
                    if (!_clear[to])
                    {
                        diagnostic.finalFrontierPhysicsRejectedNodes++; if (pointSamples++ >= 8) continue;
                        reason = "final disconnected frontier: math-clear destination rejected by native penetration";
                    }
                    else if (_map.SweepMovement(_grid[from], _grid[to] - _grid[from], GridRadius, out float fraction, out _) && fraction < .999f)
                    {
                        diagnostic.finalFrontierMathRejectedEdges++; if (mathSamples++ >= 8) continue;
                        reason = "final disconnected frontier: mathematical .42 radius edge rejected";
                    }
                    else
                    {
                        diagnostic.finalFrontierNativeRejectedEdges++; if (nativeSamples++ >= 16) continue;
                        reason = "final disconnected frontier: production .38 radius MoveSweepXZ rejected";
                    }
                    var evidence = DescribeEdge(from, to, reason, false); diagnostic.finalDisconnectedFrontier.Add(evidence);
                    if (diagnostic.finalDisconnectedFrontier.Count <= 8)
                        Debug.Log("[Reviewed navigation] Chamber " + _index + " " + reason + " from " + evidence.fromPixel + " to " + evidence.toPixel +
                            "; native error=" + evidence.endpointError + "; first hit=" + (evidence.sphereCastHits.Length == 0 ? "none" : evidence.sphereCastHits[0].colliderPath));
                }
            }
        }

        private static EdgeEvidence DescribeEdge(int from, int to, string reason, bool useCurrentLanding)
        {
            var proof = new EdgeEvidence { reason = reason, fromNode = from, toNode = to, fromPixel = _map.ToImage(_grid[from]), toPixel = _map.ToImage(_grid[to]),
                fromWorld = _grid[from], requestedWorld = _grid[to], delta = _grid[to] - _grid[from] };
            if (!useCurrentLanding) { Place(proof.fromWorld); Sweep(proof.delta, false); }
            proof.actualWorld = _player.transform.position; proof.endpointError = Vector3.Distance(proof.actualWorld, proof.requestedWorld);
            proof.mathHit42 = _map.SweepMovement(proof.fromWorld, proof.delta, GridRadius, out proof.mathFraction42, out proof.mathNormal42);
            proof.mathHit38 = _map.SweepMovement(proof.fromWorld, proof.delta, Radius, out proof.mathFraction38, out proof.mathNormal38);
            proof.originPenetration = PointCollisions(proof.fromWorld, Radius);
            proof.requestedPenetration = PointCollisions(proof.requestedWorld, Radius);
            proof.actualPenetration = PointCollisions(proof.actualWorld, Radius);
            var body = _player.GetComponent<SphereCollider>();
            proof.sphereCastHits = Physics.SphereCastAll(proof.fromWorld + Vector3.up * .5f, Radius, proof.delta.normalized,
                    proof.delta.magnitude + .02f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.GetComponent<IsoObstacle>() != null).OrderBy(h => h.distance).Take(12).Select(hit =>
                {
                    var col = hit.collider; var obstacle = col.GetComponent<IsoObstacle>(); var mesh = col as MeshCollider;
                    var item = new CastHitEvidence { colliderPath = HierarchyPath(col.transform), colliderType = col.GetType().Name, kind = obstacle.kind.ToString(),
                        distance = hit.distance, point = hit.point, normal = hit.normal, convexMesh = mesh != null && mesh.convex,
                        meshAsset = mesh == null || mesh.sharedMesh == null ? "" : AssetDatabase.GetAssetPath(mesh.sharedMesh),
                        meshVertices = mesh == null || mesh.sharedMesh == null ? 0 : mesh.sharedMesh.vertexCount,
                        meshTriangles = mesh == null || mesh.sharedMesh == null ? 0 : mesh.sharedMesh.triangles.Length / 3 };
                    item.initialPenetration = Physics.ComputePenetration(body, proof.fromWorld, Quaternion.identity, col, col.transform.position, col.transform.rotation, out item.initialEscape, out item.initialDepth);
                    item.requestedPenetration = Physics.ComputePenetration(body, proof.requestedWorld, Quaternion.identity, col, col.transform.position, col.transform.rotation, out item.requestedEscape, out item.requestedDepth);
                    item.landingPenetration = Physics.ComputePenetration(body, proof.actualWorld, Quaternion.identity, col, col.transform.position, col.transform.rotation, out item.landingEscape, out item.landingDepth);
                    item.reducesInitialPenetration = (bool)Call(_player, "ReducesInitialPenetration", col, proof.fromWorld, proof.delta);
                    return item;
                }).ToArray();
            return proof;
        }

        private static PointCollisionEvidence PointCollisions(Vector3 position, float radius)
        {
            var body = _player.GetComponent<SphereCollider>(); float original = body.radius; body.radius = radius;
            try
            {
                return new PointCollisionEvidence { world = position, sourcePixel = _map.ToImage(position), radius = radius,
                    obstacles = Physics.OverlapSphere(position + Vector3.up * .5f, radius, ~0, QueryTriggerInteraction.Ignore)
                        .Where(c => c.GetComponent<IsoObstacle>() != null).Take(12).Select(col =>
                        {
                            var proof = new PenetrationEvidence { colliderPath = HierarchyPath(col.transform), colliderType = col.GetType().Name, kind = col.GetComponent<IsoObstacle>().kind.ToString() };
                            proof.penetrating = Physics.ComputePenetration(body, position, Quaternion.identity, col, col.transform.position, col.transform.rotation, out proof.escape, out proof.depth);
                            return proof;
                        }).ToArray() };
            }
            finally { body.radius = original; }
        }

        private static string HierarchyPath(Transform node)
        { string path = node.name; while (node.parent != null) { node = node.parent; path = node.name + "/" + path; } return path; }

        private static int NearestGrid(Vector3 target)
        {
            float best = float.PositiveInfinity; int found = -1;
            for (int i = 0; i < _clear.Length; i++)
            { if (!_clear[i]) continue; float distance = (_grid[i] - target).sqrMagnitude; if (distance >= best) continue; best = distance; found = i; }
            return found;
        }

        private static void VerifyReachable(Anchor anchor)
        {
            Vector3 point = _map.ToWorld(anchor.pixel); int n = NearestGrid(point);
            var proof = new ReachEvidence { name = anchor.name, sourcePixel = anchor.pixel, nearestDistance = n < 0 ? 9999f : Vector3.Distance(point, _grid[n]),
                nearestClear = n < 0 ? Vector3.zero : _grid[n], pass = n >= 0 && _parents[n] != -2 && Vector3.Distance(point, _grid[n]) <= 1.05f };
            _proof.reachability.Add(proof); Check(proof.pass, "Source target is not connected to the entrance graph: " + anchor.name);
        }

        private static void WalkRoute(string name, Vector2 targetPixel)
        {
            Vector3 entrance = _map.ToWorld(_trace.entrance), target = _map.ToWorld(targetPixel); int n = NearestGrid(target);
            var proof = new RouteEvidence { target = name, pass = false }; _proof.routes.Add(proof);
            if (n < 0 || _parents[n] == -2 || Vector3.Distance(_grid[n], target) > 1.05f)
            { Check(false, "No reviewed entrance route for exact target " + name); return; }
            var path = new List<Vector3>(); for (int p = n; p >= 0; p = _parents[p]) path.Add(_grid[p]);
            path.Reverse(); path.Insert(0, entrance); path.Add(target); proof.sampledWorldRoute = path.ToArray(); proof.gridNodes = path.Count - 2;
            Place(entrance); bool legal = true;
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 origin = _player.transform.position; Vector3 delta = path[i] - origin; proof.requestedDistance += delta.magnitude;
                int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .14f));
                for (int s = 1; s <= steps; s++)
                {
                    Vector3 requested = Vector3.Lerp(origin, path[i], s / (float)steps), before = _player.transform.position;
                    Sweep(requested - before, false); proof.actualMicroSteps++; proof.actualDistance += Vector3.Distance(before, _player.transform.position);
                    if (Vector3.Distance(requested, _player.transform.position) > .05f || !_map.IsWalkable(_player.transform.position, Radius - .002f) ||
                        !PhysicalClear(_player.transform.position, Radius - .02f)) { legal = false; break; }
                }
                if (!legal) break;
            }
            proof.endpointError = Vector3.Distance(target, _player.transform.position); proof.pass = legal && proof.endpointError <= .06f;
            Check(proof.pass, "Actual controller route failed for exact source target " + name + "; endpoint error=" + proof.endpointError);
        }

        private static void Overview()
        {
            string source = AssetDatabase.GetAssetPath(_map.definition.painting);
            if (!string.IsNullOrEmpty(source) && File.Exists(source))
            {
                string name = "chamber-" + _index.ToString("D2") + "-source.png";
                var fullPainting = new Texture2D(2, 2);
                try
                {
                    fullPainting.LoadImage(File.ReadAllBytes(source));
                    Require(fullPainting.width > 0 && fullPainting.height > 0, "Could not decode the full original painting.");
                    _proof.sourceRasterWidth = fullPainting.width; _proof.sourceRasterHeight = fullPainting.height;
                    File.WriteAllBytes(Path.Combine(Output, name), fullPainting.EncodeToPNG()); _proof.captures.Add(name);
                }
                finally { Object.DestroyImmediate(fullPainting); }
            }
            Frame(_map.transform.position, _map.SafeZoom(100f, 16f / 9f));
            _animator.BodyRenderer.forceRenderingOff = true;
            Capture("chamber-" + _index.ToString("D2") + "-painting-overview");
            if (_map.overlayRoot != null)
            {
                _map.overlayRoot.gameObject.SetActive(true); Capture("chamber-" + _index.ToString("D2") + "-F3-navigation-overlay");
                _map.overlayRoot.gameObject.SetActive(false);
            }
            else Check(false, "Saved scene has no F3 navigation overlay for visual review.");
            _animator.BodyRenderer.forceRenderingOff = false;
        }

        private static void VerifyOcclusion(PaintedChamberNavigationTrace.OcclusionProbe pair)
        {
            var proof = new OcclusionEvidence { name = pair.name, coverName = pair.coverName, behindPixel = pair.behind, frontPixel = pair.front };
            _proof.occlusion.Add(proof);
            var covers = _map.coverRoot.GetComponentsInChildren<SpriteRenderer>(true);
            var states = covers.Select(r => r.enabled).ToArray();
            try
            {
                var owner = _map.coverRoot.Find(pair.coverName);
                Require(owner != null, "Source occlusion case names a missing saved cover: " + pair.coverName);
                var named = owner.GetComponentsInChildren<SpriteRenderer>(true);
                Require(named.Length > 0, "Saved cover has no painted silhouette: " + pair.coverName);
                foreach (var renderer in covers) renderer.enabled = false;
                Vector3 behind = _map.ToWorld(pair.behind), front = _map.ToWorld(pair.front);
                Require(_map.IsWalkable(behind, Radius) && _map.IsWalkable(front, Radius), "Occlusion feet are not legal exact source points.");
                FrameOcclusionPair(behind, front);
                proof.cameraOrthoSize = Camera.main.orthographicSize; proof.cameraWorldPosition = Camera.main.transform.position;
                string stem = "chamber-" + _index.ToString("D2") + "-occlusion-" + (_pairIndex - 1).ToString("D2");
                var files = new List<string>();
                // A hero-hidden control supplies the hero mask and checks attenuation against actual background.
                // Camera, world and render frame are identical throughout all six captures.
                _animator.BodyRenderer.forceRenderingOff = true;
                foreach (var renderer in named) renderer.enabled = true;
                Capture(stem + "-blank-cover-on"); foreach (var renderer in named) renderer.enabled = false;
                Capture(stem + "-blank-cover-off"); _animator.BodyRenderer.forceRenderingOff = false;
                files.Add(stem + "-blank-cover-on.png"); files.Add(stem + "-blank-cover-off.png");
                proof.blankBackgroundChangedPixels = ChangedPixels(stem + "-blank-cover-on", stem + "-blank-cover-off", new RectInt(0, 0, 1920, 1080), out _);
                for (int side = 0; side < 2; side++)
                {
                    Place(side == 0 ? behind : front); Call(_player, "UpdateVisualPose"); _animator.ResetMotion(); _animator.BodyRenderer.transform.rotation = Camera.main.transform.rotation;
                    Call(_player.GetComponent<PaintedActorDepth>(), "LateUpdate");
                    var sprite = _animator.BodyRenderer.sprite; var rotation = _animator.BodyRenderer.transform.rotation; var position = _player.transform.position;
                    RectInt roi = HeroScreenBounds(out Rect viewportBounds); string prefix = stem + (side == 0 ? "-behind" : "-front");
                    if (side == 0) proof.behindHeroViewport = viewportBounds; else proof.frontHeroViewport = viewportBounds;
                    foreach (var renderer in named) renderer.enabled = true;
                    Capture(prefix + "-cover-on"); foreach (var renderer in named) renderer.enabled = false;
                    Capture(prefix + "-cover-off"); files.Add(prefix + "-cover-on.png"); files.Add(prefix + "-cover-off.png");
                    Require(_animator.BodyRenderer.sprite == sprite && _animator.BodyRenderer.transform.rotation == rotation && _player.transform.position == position,
                        "Hero pose or world frame changed within an occlusion comparison.");
                    int changed = HeroOcclusionPixels(prefix + "-cover-on", prefix + "-cover-off", stem + "-blank-cover-on", stem + "-blank-cover-off",
                        roi, out int attenuated, out int outside);
                    if (side == 0) { proof.behindChangedPixels = changed; proof.behindAttenuatedPixels = attenuated; proof.behindOutsideHeroPixels = outside; }
                    else { proof.frontChangedPixels = changed; proof.frontAttenuatedPixels = attenuated; proof.frontOutsideHeroPixels = outside; }
                }
                proof.captures = files.ToArray(); proof.pass = proof.behindChangedPixels > 15 && proof.behindAttenuatedPixels > 15 && proof.frontAttenuatedPixels <= 20 &&
                    proof.behindOutsideHeroPixels <= 20 && proof.frontOutsideHeroPixels <= 20;
                Check(proof.pass, "Native cover did not occlude behind and preserve front hero: " + pair.name + " (behind=" + proof.behindChangedPixels +
                    ", attenuated=" + proof.behindAttenuatedPixels + ", frontRaw=" + proof.frontChangedPixels + ", frontAttenuated=" + proof.frontAttenuatedPixels + ")");
            }
            catch (Exception error) { proof.error = error.ToString(); Check(false, "Occlusion source case failed: " + pair.name + ": " + error.Message); }
            finally
            {
                _animator.BodyRenderer.forceRenderingOff = false;
                for (int i = 0; i < covers.Length; i++) if (covers[i] != null) covers[i].enabled = states[i];
            }
        }

        private static Vector3[] HeroWorldVertices()
        {
            var body = _animator.BodyRenderer;
            Require(body.sprite != null && body.drawMode == SpriteDrawMode.Simple, "Expected intact production idle sprite geometry.");
            // Renderer.bounds can still describe the last rendered position after a manual teleport.
            // The authored vertices and current Transform give the exact current sprite geometry.
            Vector2[] local = body.sprite.vertices; var result = new Vector3[local.Length];
            Require(local.Length >= 3, "Production idle sprite has no renderable vertices.");
            for (int i = 0; i < local.Length; i++)
                result[i] = body.transform.TransformPoint(new Vector3(body.flipX ? -local[i].x : local[i].x, body.flipY ? -local[i].y : local[i].y, 0));
            return result;
        }

        private static void FrameOcclusionPair(Vector3 behind, Vector3 front)
        {
            Vector3 right = RenderPipeline2DSetup.IsoCameraRotation * Vector3.right, up = RenderPipeline2DSetup.IsoCameraRotation * Vector3.up;
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity, maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            foreach (Vector3 feet in new[] { behind, front })
            {
                Place(feet); Call(_player, "UpdateVisualPose"); _animator.ResetMotion();
                _animator.BodyRenderer.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
                foreach (Vector3 vertex in HeroWorldVertices())
                {
                    float x = Vector3.Dot(vertex, right), y = Vector3.Dot(vertex, up);
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
            }
            const float aspect = 16f / 9f, margin = .35f;
            float required = Mathf.Max(4.5f, (maxY - minY) * .5f + margin, ((maxX - minX) * .5f + margin) / aspect);
            float safe = _map.SafeZoom(required, aspect);
            Require(safe >= required - .0001f, "Both exact occlusion poses cannot fit within the saved painting's SafeZoom: required=" + required + ", safe=" + safe);
            Vector3 focus = (behind + front) * .5f;
            focus += right * ((minX + maxX) * .5f - Vector3.Dot(focus, right));
            focus += up * ((minY + maxY) * .5f - Vector3.Dot(focus, up));
            Frame(focus, safe);
        }

        private static RectInt HeroScreenBounds(out Rect viewportBounds)
        {
            Camera camera = Camera.main; Quaternion inverse = Quaternion.Inverse(camera.transform.rotation);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector3 point in HeroWorldVertices())
            {
                // Project into the explicit 1920x1080 orthographic target, independently of Editor/GameView size.
                Vector3 local = inverse * (point - camera.transform.position);
                Vector2 viewport = new Vector2(.5f + local.x / (2f * camera.orthographicSize * camera.aspect), .5f + local.y / (2f * camera.orthographicSize));
                min = Vector2.Min(min, viewport); max = Vector2.Max(max, viewport);
            }
            viewportBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            Require(min.x >= 0 && min.y >= 0 && max.x <= 1 && max.y <= 1,
                "Pair framing clips actual hero geometry: viewport=" + viewportBounds + ", ortho=" + camera.orthographicSize);
            min = Vector2.Scale(min, new Vector2(1920, 1080)); max = Vector2.Scale(max, new Vector2(1920, 1080));
            int x0 = Mathf.Clamp(Mathf.FloorToInt(min.x) - 4, 0, 1919), y0 = Mathf.Clamp(Mathf.FloorToInt(min.y) - 4, 0, 1079);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(max.x) + 4, 1, 1920), y1 = Mathf.Clamp(Mathf.CeilToInt(max.y) + 4, 1, 1080);
            Require(x1 > x0 && y1 > y0, "Actual hero geometry produced an empty capture region.");
            return new RectInt(x0, y0, x1 - x0, y1 - y0);
        }

        private static int ChangedPixels(string first, string second, RectInt roi, out int outside, string controlFirst = null, string controlSecond = null)
        {
            var a = new Texture2D(2, 2); var b = new Texture2D(2, 2);
            Texture2D c = null, d = null; outside = 0;
            try
            {
                a.LoadImage(File.ReadAllBytes(Path.Combine(Output, first + ".png"))); b.LoadImage(File.ReadAllBytes(Path.Combine(Output, second + ".png")));
                Require(a.width == 1920 && a.height == 1080 && b.width == a.width && b.height == a.height, "Native capture dimensions changed.");
                var aa = a.GetPixels32(); var bb = b.GetPixels32(); int changed = 0;
                Color32[] cc = null, dd = null;
                if (controlFirst != null)
                {
                    c = new Texture2D(2, 2); d = new Texture2D(2, 2);
                    c.LoadImage(File.ReadAllBytes(Path.Combine(Output, controlFirst + ".png"))); d.LoadImage(File.ReadAllBytes(Path.Combine(Output, controlSecond + ".png")));
                    Require(c.width == a.width && c.height == a.height && d.width == a.width && d.height == a.height, "Blank control dimensions changed.");
                    cc = c.GetPixels32(); dd = d.GetPixels32();
                }
                for (int i = 0; i < aa.Length; i++)
                {
                    int r = aa[i].r - bb[i].r, g = aa[i].g - bb[i].g, blue = aa[i].b - bb[i].b;
                    if (cc != null) { r -= cc[i].r - dd[i].r; g -= cc[i].g - dd[i].g; blue -= cc[i].b - dd[i].b; }
                    if (Mathf.Max(Mathf.Abs(r), Mathf.Abs(g), Mathf.Abs(blue)) <= 5) continue;
                    if (roi.Contains(new Vector2Int(i % a.width, i / a.width))) changed++; else outside++;
                }
                return changed;
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); if (c != null) Object.DestroyImmediate(c); if (d != null) Object.DestroyImmediate(d); }
        }

        private static int HeroOcclusionPixels(string on, string off, string blankOn, string blankOff, RectInt roi, out int attenuated, out int outside)
        {
            var textures = new Texture2D[4]; attenuated = outside = 0;
            try
            {
                var names = new[] { on, off, blankOn, blankOff }; var pixels = new Color32[4][];
                for (int n = 0; n < names.Length; n++)
                {
                    textures[n] = new Texture2D(2, 2); textures[n].LoadImage(File.ReadAllBytes(Path.Combine(Output, names[n] + ".png")));
                    Require(textures[n].width == 1920 && textures[n].height == 1080, "Native hero comparison dimensions changed.");
                    pixels[n] = textures[n].GetPixels32();
                }
                int changed = 0;
                for (int i = 0; i < pixels[0].Length; i++)
                {
                    Color32 a = pixels[0][i], b = pixels[1][i], c = pixels[2][i], d = pixels[3][i];
                    if (!roi.Contains(new Vector2Int(i % 1920, i / 1920)))
                    {
                        // Outside actual hero geometry, only differences beyond the blank cutout delta are noise.
                        if (Mathf.Max(Mathf.Abs((a.r - b.r) - (c.r - d.r)), Mathf.Abs((a.g - b.g) - (c.g - d.g)), Mathf.Abs((a.b - b.b) - (c.b - d.b))) > 5) outside++;
                        continue;
                    }
                    int rawChange = PixelContrast(a, b), offHeroContrast = PixelContrast(b, d);
                    // An identical front hero must remain unchanged even when the cutout resamples the background.
                    if (rawChange <= 5 || offHeroContrast <= 5) continue;
                    changed++;
                    if (PixelContrast(a, c) <= offHeroContrast * .5f) attenuated++;
                }
                return changed;
            }
            finally { foreach (var texture in textures) if (texture != null) Object.DestroyImmediate(texture); }
        }

        private static int PixelContrast(Color32 a, Color32 b)
        { return Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)); }

        private static void Frame(Vector3 focus, float zoom)
        {
            Camera camera = Camera.main; camera.transform.rotation = RenderPipeline2DSetup.IsoCameraRotation;
            camera.aspect = 16f / 9f; camera.orthographicSize = zoom; camera.transform.position = focus - camera.transform.forward * 36f;
            _animator.BodyRenderer.transform.rotation = camera.transform.rotation;
        }

        private static void DisableTemporalAA()
        {
            Type type = Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");
            Require(type != null, "HDRP camera metadata unavailable."); var component = Camera.main.GetComponent(type);
            Require(component != null, "HDRP camera component unavailable.");
            var field = type.GetField("antialiasing", InstanceFlags); var property = type.GetProperty("antialiasing", InstanceFlags);
            if (field != null) field.SetValue(component, Enum.Parse(field.FieldType, "None"));
            else if (property != null) property.SetValue(component, Enum.Parse(property.PropertyType, "None"));
            else throw new InvalidOperationException("Cannot disable temporal antialiasing for stable native cover comparisons.");
        }

        private static void Capture(string name)
        {
            string temporary = "reviewed-navigation-" + name;
            Render.Invoke(null, new object[] { temporary, 1920, 1080, false, null });
            string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers/" + temporary + ".png"));
            File.Copy(source, Path.Combine(Output, name + ".png"), true); File.Delete(source); _proof.captures.Add(name + ".png");
        }

        private static void Place(Vector3 point)
        { _player.TeleportToChamberSpawn(point); Physics.SyncTransforms(); }
        private static void Sweep(Vector3 delta, bool dash)
        { Physics.SyncTransforms(); Move.Invoke(_player, new object[] { delta, dash }); Physics.SyncTransforms(); }
        private static object Call(object target, string method, params object[] args)
        { Require(target != null, "Missing runtime target for " + method); var member = target.GetType().GetMethod(method, InstanceFlags); Require(member != null, "Missing " + method); return member.Invoke(target, args); }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static void CheckDeadline()
        { Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + ".Deadline", 0), "1200 second native verification deadline exceeded."); }
        private static void Check(bool value, string message)
        { if (value) return; _proof.failures.Add(message); Debug.LogWarning("[Reviewed navigation] Chamber " + _index + ": " + message); }
        private static void Step(int phase, double delay = .05) { _phase = phase; _next = EditorApplication.timeSinceStartup + delay; }
        private static void SaveMap()
        {
            if (_proof != null) File.WriteAllText(Path.Combine(Output, "chamber-" + _proof.chamberIndex.ToString("D2") + ".json"), JsonUtility.ToJson(_proof, true));
            File.WriteAllText(Path.Combine(Output, "progress.json"), JsonUtility.ToJson(new RunEvidence { result = "running", runtimeErrors = _errors, savedScenesChecked = Maps.Count, maps = Maps.ToArray() }, true));
        }

        private static void SavePreferences()
        {
            SaveIntPref(IntroCinematicPlayer.ViewedKey); SaveIntPref(ModePref);
            foreach (string pref in CalibrationPrefs)
            { SessionState.SetBool(Key + pref + ".Exists", PlayerPrefs.HasKey(pref)); SessionState.SetFloat(Key + pref, PlayerPrefs.GetFloat(pref)); }
            SessionState.SetBool(Key + ".MicDevice.Exists", PlayerPrefs.HasKey("OrpheusBelow.MicDevice"));
            SessionState.SetString(Key + ".MicDevice", PlayerPrefs.GetString("OrpheusBelow.MicDevice"));
            SessionState.SetInt(Key + ".InputBackground", (int)InputSystem.settings.backgroundBehavior);
            SessionState.SetInt(Key + ".InputEditor", (int)InputSystem.settings.editorInputBehaviorInPlayMode);
        }
        private static void SaveIntPref(string pref)
        { SessionState.SetBool(Key + pref + ".Exists", PlayerPrefs.HasKey(pref)); SessionState.SetInt(Key + pref, PlayerPrefs.GetInt(pref)); }
        private static void SetInputSettings()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }
        private static void RestorePreferences()
        {
            foreach (string pref in new[] { IntroCinematicPlayer.ViewedKey, ModePref })
                if (SessionState.GetBool(Key + pref + ".Exists", false)) PlayerPrefs.SetInt(pref, SessionState.GetInt(Key + pref, 0)); else PlayerPrefs.DeleteKey(pref);
            foreach (string pref in CalibrationPrefs)
                if (SessionState.GetBool(Key + pref + ".Exists", false)) PlayerPrefs.SetFloat(pref, SessionState.GetFloat(Key + pref, 0)); else PlayerPrefs.DeleteKey(pref);
            if (SessionState.GetBool(Key + ".MicDevice.Exists", false)) PlayerPrefs.SetString("OrpheusBelow.MicDevice", SessionState.GetString(Key + ".MicDevice", ""));
            else PlayerPrefs.DeleteKey("OrpheusBelow.MicDevice");
            InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)SessionState.GetInt(Key + ".InputBackground", 0);
            InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key + ".InputEditor", 0);
            PlayerPrefs.Save();
        }
        private static void Finish(string error)
        {
            bool pass = error == null && _errors == 0 && Maps.Count == 8 && Maps.All(m => m.result == "pass");
            if (_proof != null && _proof.result == "running") { _proof.result = "fail"; if (error != null) _proof.failures.Add(error); SaveMap(); }
            File.WriteAllText(Path.Combine(Output, "result.json"), JsonUtility.ToJson(new RunEvidence { result = pass ? "pass" : "fail", error = error,
                runtimeErrors = _errors, savedScenesChecked = Maps.Count, maps = Maps.ToArray() }, true));
            if (error != null) File.WriteAllText(Path.Combine(Output, "failure.txt"), error);
            RestorePreferences(); Time.timeScale = 1f; SessionState.SetBool(Key, false);
            Debug.Log("[Reviewed navigation] " + (pass ? "PASS" : "FAIL") + ": " + Output);
            EditorApplication.Exit(pass ? 0 : 1);
        }
    }
}
