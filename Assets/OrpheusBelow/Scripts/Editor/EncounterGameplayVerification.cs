using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    /// <summary>Traverses the real director encounters, rather than instantiating an art showcase.</summary>
    [InitializeOnLoad]
    public static class EncounterGameplayVerification
    {
        private const string Key = "OrpheusBelow.EncounterGameplayVerification";
        private static readonly int[] Course = { 0, 1, 2, 3, 4, 5, 6, 7, 6, 5, 4, 3, 2, 1, 0 };
        private static readonly PaintedEnemyKind[] DescendingKinds = { PaintedEnemyKind.HoodedSkeleton, PaintedEnemyKind.Tripod,
            PaintedEnemyKind.MaskedMonk, PaintedEnemyKind.MysticEyeWarden, PaintedEnemyKind.WingedSentinel,
            PaintedEnemyKind.Shepherd, PaintedEnemyKind.Cerberus, PaintedEnemyKind.HoodedSkeleton };
        private static readonly HashSet<PaintedEnemyKind> Observed = new HashSet<PaintedEnemyKind>();
        private static int _phase, _visit, _errors;
        private static double _next, _encounterDeadline;
        private static PaintedEnemyActor _actor;
        private static bool _warned, _sheepDashed, _roosterGrabbed;
        private static bool _rhythmCaptured;
        private static int _answeredRhythmNotes, _verifiedWaves;
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/EncounterGameplay"));

        static EncounterGameplayVerification()
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
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers")));
            EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(0));
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + "Deadline", (float)EditorApplication.timeSinceStartup + 900);
            EditorApplication.isPlaying = true;
        }

        private static object Call(object target, string method, params object[] args)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

        private static void Capture(string name)
        {
            string temp = "encounter-" + name;
            typeof(PaintedChamberVerification).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { temp, 1920, 1080, true, null });
            File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PaintedChambers/" + temp + ".png")), Path.Combine(Output, name + ".png"), true);
        }

        private static void Step(int phase, double seconds = .1)
        { _phase = phase; _next = EditorApplication.timeSinceStartup + seconds; }

        private static void Require(bool condition, string message)
        { if (!condition) throw new Exception(message + " (visit " + _visit + ", phase " + _phase + ")"); }

        private static Vector3 ClearCompanionPosition(PaintedEnemyActor rider)
        {
            var map = PaintedChamberMap.Active;
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI / 12;
                Vector3 point = rider.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2.3f;
                if (!map.IsWalkable(point, .6f)) continue;
                if (ChamberCollision.SweepObstacle(rider.transform.position + Vector3.up * .5f,
                    point - rider.transform.position, .5f, out _)) continue;
                return point;
            }
            throw new Exception("No physics-clear companion placement near actual rider");
        }

        private static void EnterPassage()
        {
            var map = PaintedChamberMap.Active;
            var player = OrpheusPlayerController.Instance;
            player.enabled = false;
            player.TeleportToChamberSpawn(map.ActivePassage.transform.position);
            var companion = Object.FindFirstObjectByType<BlurredEurydice>();
            if (companion != null)
            {
                companion.enabled = false;
                companion.SnapBehindOrpheus(map.NearestWalkable(map.ActivePassage.transform.position +
                    (map.ToWorld(map.definition.encounterCenter) - map.ActivePassage.transform.position).normalized * 2, .5f));
            }
            Physics.SyncTransforms(); Time.timeScale = 1;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
            try
            {
                Require(EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + "Deadline", float.MaxValue), "Overall timeout");
                if (EditorApplication.timeSinceStartup < _next || HadesChamberDirector.Instance == null || PaintedChamberMap.Active == null) return;
                var director = HadesChamberDirector.Instance;
                var map = PaintedChamberMap.Active;
                var player = OrpheusPlayerController.Instance;
                player.RestoreTravelHealth(player.maxHealth);
                Camera.main.aspect = 16f / 9f;
                if (_phase >= 3 && OrpheusHud.IsTutorialModalActive)
                { OrpheusHud.Instance.DismissTutorialModal(); _next = EditorApplication.timeSinceStartup + 1; return; }
                if (_phase != 7 && map.definition.index != Course[_visit]) return;
                if (_phase == 0) { Step(1, 1); return; }
                if (_phase == 1)
                {
                    Call(UnderworldFrontEnd.Instance, "BeginOrResume"); Step(2, 1); return;
                }
                if (_phase == 2)
                {
                    Require(OrpheusHud.Instance.CurrentTutorialSpeaker == "Artemis", "Opening lesson missing Artemis");
                    OrpheusHud.Instance.DismissTutorialModal(); player.enabled = false;
                    player.TeleportToChamberSpawn(player.transform.position + UnderworldCamera2D.IsoRightXZ * 2);
                    Call(director, "Update"); OrpheusHud.Instance.DismissTutorialModal();
                    Call(OrpheusLyreCombat.Instance, "HandleAttackPulse", .3f, false); Call(director, "Update");
                    Require(UnderworldJourney.Progress.TutorialComplete, "Opening actions did not spawn encounter");
                    Step(3, 2); return;
                }
                if (_phase == 3)
                {
                    Require(SceneManager.GetActiveScene().path == PaintedChamberScene.ScenePath(Course[_visit]), "Wrong physical scene");
                    bool ascending = _visit > 7;
                    var chamber = director.chambers[map.definition.index];
                    var shades = Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None);
                    // The summon coroutine waits in scaled time while Artemis's entry hint is modal.
                    if (chamber.wave1.Count > 0 && director.ActiveEnemyCount < chamber.wave1.Count) return;
                    Require(UnderworldJourney.IsAscending == ascending, "Wrong journey leg");
                    OrpheusHud.Instance.DismissTutorialModal(); player.enabled = false;
                    _warned = false; _sheepDashed = false;
                    if (!ascending && ChamberEncounterPlan.IsMajorBoss(map.definition.index))
                    {
                        Require(shades.Length == 0 && chamber.guardianBoss != null && chamber.guardianBoss.IsBattleActive,
                            "Major boss was hidden behind generic waves");
                        Require(player.IsIn2DBossStage, "Major boss did not enter its duel");
                        Require(UnderworldCamera2D.Instance.SideViewBlend01 > .9f, "Immediate boss lost its camera transition");
                        _actor = chamber.guardianBoss.GetComponent<PaintedEnemyActor>();
                        player.transform.position = _actor.StageCenter - UnderworldCamera2D.IsoRightXZ * 4.2f;
                    }
                    else
                    {
                        Require(chamber.guardianBoss == null && director.ActiveEnemyCount == chamber.wave1.Count && shades.Length > 0,
                            "Normal director did not spawn authored roster");
                        VerifyFloorSpawns(map, player);
                        _verifiedWaves++;
                        _actor = null;
                        foreach (var shade in shades)
                            if(shade.PaintedActor.Kind == (ascending ? PaintedEnemyKind.RoosterRider : DescendingKinds[map.definition.index]))
                            { _actor = shade.PaintedActor; break; }
                        Require(_actor != null, "Authored creature missing from normal roster");
                        if (ascending)
                        {
                            foreach (var shade in shades)
                                if (shade.archetype == ShadeArchetype.MourningAbductor) { _actor = shade.PaintedActor; break; }
                            Require(_actor.Kind == PaintedEnemyKind.RoosterRider, "Return roster missing rooster");
                        }
                        if (director.ActiveMiniBoss != null)
                        {
                            Require(!player.IsIn2DBossStage && map.coverRoot.gameObject.activeSelf, "Mini-boss did not retain chamber obstacles");
                            Require(director.ActiveMiniBoss.CurrentHealth >= 200, "Mini-boss used ordinary skeleton health");
                        }
                        var bossBar = (GameObject)typeof(OrpheusHud).GetField("_bossBarRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(OrpheusHud.Instance);
                        Require(!bossBar.activeSelf, "Recurring floor enemy incorrectly showed a boss health bar");
                        Require(PaintedEnemyNavigation.TryFindSpawn(map, _actor.transform.position,
                            _actor.transform.position + UnderworldCamera2D.IsoRightXZ * (ascending ? -5 : 3), null, out var fightingPoint), "No connected test fighting point");
                        player.TeleportToChamberSpawn(fightingPoint);
                        var companion = Object.FindFirstObjectByType<BlurredEurydice>();
                        if (companion != null)
                        {
                            // The real rider deliberately ignores a disabled companion. Keep her alive
                            // and active, while a long follow lag holds the isolated sampling position.
                            companion.enabled = true;
                            companion.followLagDistance = 100f;
                            companion.SnapBehindOrpheus(ClearCompanionPosition(_actor));
                        }
                    }
                    Require(_actor.Pattern != null && _actor.Art.IsReady, "Normal spawn has no painted attack/animation");
                    Require(_actor.Kind == (ascending ? PaintedEnemyKind.RoosterRider : DescendingKinds[map.definition.index]),
                        "Director spawned the wrong creature: " + _actor.Kind);
                    if (!ascending && (map.definition.index == 2 || map.definition.index == 4 || map.definition.index == 5))
                        Require(director.ActiveMiniBoss != null, "Mini-boss missing from actual encounter");
                    if (!player.IsIn2DBossStage) UnderworldCamera2D.Instance.SnapToTargetNow();
                    Physics.SyncTransforms();
                    _encounterDeadline = EditorApplication.timeSinceStartup + 65;
                    Debug.Log("[Encounter gameplay] Normal spawn: " + _actor.Kind + " depth=" + map.definition.index + " ascending=" + ascending);
                    Step(4); return;
                }
                if (_phase == 4)
                {
                    OrpheusHud.Instance.DismissTutorialModal();
                    Require(EditorApplication.timeSinceStartup < _encounterDeadline, "Enemy never completed its actual attack: " + _actor.Kind + " motion=" + _actor.Art.Motion);
                    if (!_warned && _actor.Art.Motion == EnemyMotion.Windup)
                    { _warned = true; Capture(_visit.ToString("D2") + "-" + _actor.Kind + "-warning"); }
                    foreach (var sheep in Object.FindObjectsByType<SheepAttack>(FindObjectsSortMode.None))
                        if (sheep.Phase == SheepAttack.DashPhase.Dash) { _sheepDashed = true; Observed.Add(PaintedEnemyKind.Sheep); }
                    bool completed = false;
                    switch (_actor.Pattern)
                    {
                        case HoodedSkeletonAttack skeleton: completed = skeleton.VolleyCount > 0; break;
                        case TripodAttack tripod: completed = tripod.CastCount >= 3 && tripod.PulseCount >= 5; break;
                        case MaskedMonkAttack monk: completed = monk.CompletedImpacts >= 3; break;
                        case MysticEyeWardenAttack eye: completed = eye.CastCount >= 3 && eye.RuneStrikeCount >= 3; break;
                        case WingedSentinelAttack sentinel: completed = sentinel.CompletedSwings > 0; break;
                        case ShepherdAttack shepherd: completed = shepherd.LivingSheep > 0 && _sheepDashed; break;
                        case CerberusAttack cerberus: completed = cerberus.BiteCount > 0 && cerberus.TailWhipCount > 0 && cerberus.PounceCount > 0; break;
                        case RoosterRiderAttack rooster:
                            completed = rooster.IsDragging;
                            if (completed)
                            {
                                Require(Object.FindFirstObjectByType<BlurredEurydice>().IsBeingAbducted, "Rooster failed to register actual abduction");
                                _roosterGrabbed = true; Capture(_visit.ToString("D2") + "-rooster-drag");
                                _actor.OwnerShade.ApplyStun(1);
                                Require(!rooster.IsDragging, "Stun did not release the struck rooster's grip");
                                // Deeper return rosters contain two riders: each live grip must be broken.
                                foreach (var rider in Object.FindObjectsByType<RoosterRiderAttack>(FindObjectsSortMode.None))
                                {
                                    var actor = rider.GetComponent<PaintedEnemyActor>();
                                    if (actor != null && actor.OwnerShade != null) actor.OwnerShade.ApplyStun(1);
                                }
                                Require(!Object.FindFirstObjectByType<BlurredEurydice>().IsBeingAbducted, "Stun did not rescue Eurydice");
                            }
                            break;
                    }
                    if (!completed || !_warned) return;
                    Observed.Add(_actor.Kind);
                    Capture(_visit.ToString("D2") + "-" + _actor.Kind + "-attack");
                    Debug.Log("[Encounter gameplay] Attack completed: " + _actor.Kind);
                    var rhythmBoss = director.chambers[map.definition.index].guardianBoss;
                    if (rhythmBoss != null)
                    {
                        _answeredRhythmNotes = 0; _rhythmCaptured = false;
                        rhythmBoss.TriggerStaveClimax();
                        _encounterDeadline = EditorApplication.timeSinceStartup + 35;
                        Step(8, .02); return;
                    }
                    // Exercise the real damage and death callbacks; never call the director clear method.
                    foreach (var shade in Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None))
                        shade.OnOrphicHit(shade.CurrentHealth + 1000, 1, true, player.transform.position);
                    var boss = director.chambers[map.definition.index].guardianBoss;
                    if (boss != null) boss.OnOrphicHit(boss.CurrentHealth + 1000, 1, true, player.transform.position);
                    Step(5, 1); return;
                }
                if (_phase == 5)
                {
                    if (!map.ActivePassage.IsUnlocked)
                    {
                        if ((bool)typeof(HadesChamberDirector).GetField("_spawningWave", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director)) return;
                        if (director.ActiveEnemyCount > 0)
                        {
                            Require(!UnderworldJourney.Progress.IsCleared(map.definition.index), "Passage cleared before reinforcements died");
                            VerifyFloorSpawns(map, player); _verifiedWaves++;
                            foreach (var shade in Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None))
                                shade.OnOrphicHit(shade.CurrentHealth + 1000, 1, true, player.transform.position);
                            _next = EditorApplication.timeSinceStartup + 1;
                            return;
                        }
                    }
                    Require(map.ActivePassage.IsUnlocked && UnderworldJourney.Progress.IsCleared(map.definition.index), "Actual defeat callbacks failed to unlock passage");
                    Require(Object.FindFirstObjectByType<PaintedSummonOwnership>() == null, "Defeated Shepherd left an active flock");
                    OrpheusHud.Instance.DismissTutorialModal();
                    EnterPassage();
                    if (_visit == 7) Step(6, 1);
                    else if (_visit == Course.Length - 1) Step(7, 1);
                    else { _visit++; Step(3, 2); }
                    return;
                }
                if (_phase == 6)
                {
                    Require(UnderworldJourney.HasEurydice && Object.FindFirstObjectByType<BlurredEurydice>() != null, "Deepest throne rescue failed");
                    OrpheusHud.Instance.DismissTutorialModal(); EnterPassage(); _visit++; Step(3, 2); return;
                }
                if (_phase == 7)
                {
                    Require(player.IsVictory && UnderworldJourney.Progress.IsComplete && _roosterGrabbed, "Actual encounter course did not finish escort victory");
                    Require(Observed.Count == 9, "Not all eight designs and summoned sheep were observed");
                    Require(_errors == 0, "Runtime errors: " + _errors);
                    Capture("15-surface-victory");
                    Debug.Log("[Encounter gameplay] PASS: 15 visits, " + _verifiedWaves + " connected multi-waves, all nine designs, three boss attack kits, perfect-note rhythm with independent dodge hazards, sheep dash, rooster rescue, real defeat callbacks/two-way exits and surface victory; zero runtime errors.");
                    SessionState.SetBool(Key, false); EditorApplication.Exit(0);
                }
                if (_phase == 8)
                {
                    var boss = director.chambers[map.definition.index].guardianBoss;
                    Require(boss != null && EditorApplication.timeSinceStartup < _encounterDeadline, "Rhythm verification timed out");
                    if (boss.CurrentPhase == DuelPhase.StaveBarrage)
                    {
                        if (!_rhythmCaptured && boss.RhythmPressure.ActiveWarningCount > 0)
                        { Capture(_visit.ToString("D2") + "-rhythm-dodge"); _rhythmCaptured = true; }
                        var notes = (System.Collections.IList)typeof(CerberusRhythmBoss).GetField("_notes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boss);
                        foreach (var note in notes)
                        {
                            var type = note.GetType();
                            if ((bool)type.GetField("resolved").GetValue(note)) continue;
                            float remaining = (float)type.GetField("targetHitTime").GetValue(note) - Time.time;
                            if (remaining < -.05f || remaining > boss.hitWindowSeconds * .65f) continue;
                            bool hard = (bool)type.GetField("requiresHardBoom").GetValue(note);
                            boss.OnPlayerRhythmPulse(hard ? 1f : .3f, hard); _answeredRhythmNotes++;
                        }
                    }
                    if (boss.CurrentPhase != DuelPhase.CrashedStunned) return;
                    Require(_answeredRhythmNotes == 6 && boss.HarmonyStreak == 6, "Rhythm test must answer every note correctly");
                    Require(boss.RhythmPressure.AttacksReleased >= 2 && _rhythmCaptured, "Perfect music removed required dodge pressure");
                    Require(boss.RhythmPressure.ActiveWarningCount == 0, "Crash left a pending rhythm strike");
                    boss.OnOrphicHit(boss.CurrentHealth + 1000, 1, true, player.transform.position);
                    Step(5, 1);
                }
            }
            catch (Exception e) { SessionState.SetBool(Key, false); Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void VerifyFloorSpawns(PaintedChamberMap map, OrpheusPlayerController player)
        {
            foreach (var shade in Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None))
            {
                if (shade.CurrentHealth <= 0) continue;
                Require(map.IsWalkable(shade.transform.position, .45f), "Actor spawned outside solid-clear floor");
                Require(PaintedEnemyNavigation.CanReach(map, player.transform.position, shade.transform.position), "Actor spawned across an inaccessible gap");
                Require(shade.transform.Find("EnemyGroundShadow") != null, "Actor is missing its contact shadow");
            }
        }
    }
}
