using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    [InitializeOnLoad]
    public static class PaintedBestiaryVerification
    {
        private const string Key = "OrpheusBelow.PaintedBestiaryVerification";
        private static readonly PaintedEnemyKind[] Kinds = { PaintedEnemyKind.Tripod, PaintedEnemyKind.MaskedMonk, PaintedEnemyKind.MysticEyeWarden,
            PaintedEnemyKind.WingedSentinel, PaintedEnemyKind.Shepherd, PaintedEnemyKind.Cerberus, PaintedEnemyKind.HoodedSkeleton, PaintedEnemyKind.RoosterRider, PaintedEnemyKind.Sheep };
        private static int _index, _phase, _step, _errors;
        private static PaintedEnemyActor _actor;
        private static BlurredEurydice _companion;
        private static Vector3 _stage;
        private static double _next;
        private static readonly HashSet<EnemyMotion> Seen = new HashSet<EnemyMotion>();
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PaintedBestiary"));
        static PaintedBestiaryVerification()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message,stack,type) =>
            {
                if (SessionState.GetBool(Key,false) && EditorApplication.isPlaying && !stack.Contains("UnityEditor.Search.") &&
                    (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) _errors++;
            };
        }
        public static void RunBatch()
        {
            Directory.CreateDirectory(Output); EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(0));
            SessionState.SetBool(Key,true); SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+900);
            UnderworldFrontEnd.SkipTitleOnce = true; EditorApplication.isPlaying = true;
        }
        private static void Capture(string name)
        {
            Camera.main.transform.rotation = _actor.IsBossStage ? UnderworldCamera2D.Side2DRotation : RenderPipeline2DSetup.IsoCameraRotation;
            typeof(UnderworldCamera2D).GetProperty("CurrentBillboardRotation").SetValue(null,Camera.main.transform.rotation);
            typeof(UnderworldCamera2D).GetProperty("SideViewBlend01").SetValue(UnderworldCamera2D.Instance,_actor.IsBossStage ? 1f : 0f);
            typeof(UnderworldCamera2D).GetProperty("CurrentSideParallaxOffset").SetValue(UnderworldCamera2D.Instance,0f);
            foreach (var rig in Object.FindObjectsByType<UnderworldParallaxRig>(FindObjectsSortMode.None))
                typeof(UnderworldParallaxRig).GetMethod("ApplyVisibility",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rig,new object[]{_actor.IsBossStage ? 1f : 0f,0f});
            _actor.Art.transform.rotation = Camera.main.transform.rotation;
            Camera.main.orthographicSize = _actor.IsBossStage ? 5.8f : 4.3f;
            Vector3 focus = _actor.IsBossStage ? _stage + Vector3.up * 1.5f : _actor.transform.position + Vector3.up * 1.2f;
            Camera.main.transform.position = focus - Camera.main.transform.forward * 36;
            string temp = "bestiary-"+name;
            typeof(PaintedChamberVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{temp,1280,720,false,null});
            File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PaintedChambers/"+temp+".png")),Path.Combine(Output,name+".png"),true);
        }
        private static void NewActor()
        {
            OrpheusHud.Instance.DismissTutorialModal();
            Seen.Clear(); _step = 0; var kind = Kinds[_index];
            var player = OrpheusPlayerController.Instance; var map = PaintedChamberMap.Active;
            _stage = map.ToWorld(map.definition.encounterCenter);
            player.TeleportToChamberSpawn(_stage - UnderworldCamera2D.IsoRightXZ * 3);
            if (_index < 6)
            {
                var go = new GameObject("Verify_"+kind);
                var boss = go.AddComponent<CerberusRhythmBoss>();
                BossIdentity id = kind == PaintedEnemyKind.Tripod ? BossIdentity.Tripod : kind == PaintedEnemyKind.MaskedMonk ? BossIdentity.MaskedMonk
                    : kind == PaintedEnemyKind.MysticEyeWarden ? BossIdentity.MysticEyeWarden : kind == PaintedEnemyKind.WingedSentinel ? BossIdentity.WingedSentinel
                    : kind == PaintedEnemyKind.Shepherd ? BossIdentity.Shepherd : BossIdentity.CerberusHoundOfHades;
                boss.ConfigureBoss(id,_stage); boss.ActivateGuardianDuel(); boss.enabled = false;
                map.coverRoot.gameObject.SetActive(false);
                _actor = go.GetComponent<PaintedEnemyActor>();
            }
            else
            {
                var go = new GameObject("Verify_"+kind); go.transform.position = map.NearestWalkable(_stage,.5f);
                var shade = go.AddComponent<ShadeEnemy>();
                shade.SetupArchetype(kind == PaintedEnemyKind.RoosterRider ? ShadeArchetype.MourningAbductor : kind == PaintedEnemyKind.Sheep ? ShadeArchetype.SummonedSheep : ShadeArchetype.BrimstoneCaster);
                shade.enabled = false; _actor = go.GetComponent<PaintedEnemyActor>();
                if (kind == PaintedEnemyKind.RoosterRider)
                {
                    UnderworldJourney.Progress.Visit(7); UnderworldJourney.Progress.ClearCurrentChamber(); UnderworldJourney.Progress.TryRescue();
                    UnderworldJourney.Progress.Visit(0);
                    _companion = OrpheusPrototypeBuilder.AddCompanion(null,player.transform);
                    _companion.SnapBehindOrpheus(map.NearestWalkable(_actor.transform.position + UnderworldCamera2D.IsoRightXZ * 3,.5f));
                    _companion.enabled = false;
                }
            }
            if (_actor.Pattern == null || !_actor.Art.IsReady) throw new Exception(kind+" missing painted animation/pattern");
            Physics.SyncTransforms();
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Key,false) || EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key+"Deadline",float.MaxValue)) throw new Exception("Bestiary verification timed out");
                if (EditorApplication.timeSinceStartup < _next || HadesChamberDirector.Instance == null || PaintedChamberMap.Active == null) return;
                if (_phase == 0) { _phase = -1; _next = EditorApplication.timeSinceStartup + 1; return; }
                if (_phase == -1)
                {
                    if (UnderworldFrontEnd.BlocksGameplay)
                        typeof(UnderworldFrontEnd).GetMethod("BeginOrResume",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(UnderworldFrontEnd.Instance,null);
                    HadesChamberDirector.Instance.enabled = false; HadesChamberDirector.Instance.StopAllCoroutines();
                    OrpheusHud.Instance.DismissTutorialModal(); OrpheusPlayerController.Instance.enabled = false;
                    UnderworldCamera2D.Instance.enabled = false;
                    foreach (var shade in Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None)) Object.Destroy(shade.gameObject);
                    _phase = 1; _next = EditorApplication.timeSinceStartup+.5; return;
                }
                if (_phase == 1) { NewActor(); _phase = 2; _next = EditorApplication.timeSinceStartup+.3; return; }
                if (_phase == 2)
                {
                    // Inspect every imported motion at gameplay scale, without changing the source pixels.
                    Time.timeScale = 0;
                    for (int row = 0; row < 8; row++)
                    {
                        _actor.Art.Play((EnemyMotion)row,1); _actor.Art.Advance(.5f);
                        Capture(Kinds[_index]+"-pose-"+(EnemyMotion)row);
                    }
                    _actor.Cancel(); _phase = 3; Time.timeScale = 1; return;
                }
                if (_phase == 3)
                {
                    if (OrpheusHud.IsTutorialModalActive) OrpheusHud.Instance.DismissTutorialModal();
                    var player = OrpheusPlayerController.Instance; player.RestoreTravelHealth(player.maxHealth);
                    _actor.Tick(.1f); _step++;
                    if (Seen.Add(_actor.Art.Motion)) Capture(Kinds[_index]+"-live-"+_actor.Art.Motion);
                    if (_step < 180) return;
                    if (!Seen.Contains(EnemyMotion.Windup) || !Seen.Contains(EnemyMotion.Attack))
                        throw new Exception(Kinds[_index]+" never completed a warned primary attack; blocked="+UnderworldFrontEnd.BlocksGameplay+" tutorial="+OrpheusHud.IsTutorialModalActive+" time="+Time.timeScale+" pattern="+_actor.Pattern.GetType().Name);
                    var shadow = Object.FindFirstObjectByType<PaintedAttackShadow>();
                    _actor.Cancel();
                    PaintedEnemyProjectile.ClearAll();
                    if (_companion != null) { Object.Destroy(_companion.gameObject); _companion = null; }
                    if (player.IsIn2DBossStage) player.Exit2DBossStage();
                    Object.Destroy(_actor.gameObject); _actor = null;
                    _index++; _phase = 4; _next = EditorApplication.timeSinceStartup+.3; return;
                }
                if (_phase == 4)
                {
                    if (Object.FindFirstObjectByType<PaintedAttackShadow>() != null || Object.FindFirstObjectByType<PaintedEnemyProjectile>() != null || Object.FindFirstObjectByType<PaintedSummonOwnership>() != null)
                        throw new Exception("An interrupted attack left a damaging hazard/summon alive");
                    if (_index < Kinds.Length) { _phase = 1; return; }
                    if (_errors != 0) throw new Exception("Runtime errors: "+_errors);
                    Debug.Log("[Painted bestiary] RENDER PASS: nine painted actors, all eight motion rows, warned primary attacks, cancelled projectiles/shadows/flocks, zero runtime errors.");
                    SessionState.SetBool(Key,false); EditorApplication.Exit(0);
                }
            }
            catch (Exception exception) { SessionState.SetBool(Key,false); Debug.LogException(exception); EditorApplication.Exit(1); }
        }
    }
}
