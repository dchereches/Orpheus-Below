using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace OrpheusBelow.EditorTools
{
    [InitializeOnLoad]
    public static class IntroCinematicVerification
    {
        private const string Key="OrpheusBelow.VerifyIntro";
        private static int _phase, _errors;
        private static double _next;
        private static float _pausedClock;
        private static int _sequenceDone, _tailDone, _pausedSamples, _skipped;
        private static float _pausedSubtitle;
        private static readonly HashSet<string> SeenVoices=new HashSet<string>();
        private static AudioSource VoiceSource => (AudioSource)typeof(VoiceDirector).GetField("_voiceSource",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(VoiceDirector.Instance);
        private static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/IntroCinematic"));
        static IntroCinematicVerification()
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=(message,stack,type)=>
            {
                if (SessionState.GetBool(Key,false) && (type==LogType.Error || type==LogType.Exception)
                    && !stack.Contains("UnityEditor.Search.")
                    && !message.StartsWith("Intro narration stopped before its ending.")) _errors++;
            };
        }
        public static void RunBatch()
        {
            Directory.CreateDirectory(Output);
            File.Delete(Path.Combine(Output,"result.json"));
            File.Delete(Path.Combine(Output,"failure.txt"));
            _phase=_errors=_sequenceDone=_tailDone=0; _next=0; SeenVoices.Clear();
            SessionState.SetInt(Key+"InputBackground",(int)InputSystem.settings.backgroundBehavior);
            SessionState.SetInt(Key+"InputEditor",(int)InputSystem.settings.editorInputBehaviorInPlayMode);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PaintedChambers")));
            SessionState.SetInt(Key+"Mode",PlayerPrefs.GetInt("OrpheusBelow.SensorSourceMode",-1));
            PlayerPrefs.SetInt("OrpheusBelow.SensorSourceMode",0);
            SessionState.SetInt(Key+"Viewed",PlayerPrefs.GetInt(IntroCinematicPlayer.ViewedKey,-1));
            EditorSceneManager.OpenScene(PaintedChamberScene.ScenePath(0));
            SessionState.SetBool(Key,true);
            SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+180);
            EditorApplication.isPlaying=true;
        }
        private static void Call(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
        private static bool Field(object target,string name)=>(bool)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        private static void Require(bool condition,string message) { if (!condition) throw new Exception("Intro verification phase"+_phase+": "+message); }
        private static void Step(int phase,double delay=.2) { _phase=phase; _next=EditorApplication.timeSinceStartup+delay; }
        private static void Finish(int code)
        {
            int old=SessionState.GetInt(Key+"Viewed",-1);
            InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)SessionState.GetInt(Key+"InputBackground",0);
            InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Key+"InputEditor",0);
            if (old<0) PlayerPrefs.DeleteKey(IntroCinematicPlayer.ViewedKey); else PlayerPrefs.SetInt(IntroCinematicPlayer.ViewedKey,old);
            int oldMode=SessionState.GetInt(Key+"Mode",-1);
            if (oldMode<0) PlayerPrefs.DeleteKey("OrpheusBelow.SensorSourceMode"); else PlayerPrefs.SetInt("OrpheusBelow.SensorSourceMode",oldMode);
            PlayerPrefs.Save(); SessionState.SetBool(Key,false); EditorApplication.Exit(code);
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Key,false) || EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
            try
            {
                Require(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+"Deadline",0),"Timeout");
                if (EditorApplication.timeSinceStartup<_next) return;
                var menu=UnderworldFrontEnd.Instance;
                if (menu==null) return;
                var intro=Object.FindFirstObjectByType<IntroCinematicPlayer>();
                switch (_phase)
                {
                    case 0:
                        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                        Require(intro!=null && !Field(menu,"_open"),"Boot did not enter the introduction automatically");
                        Call(menu,"BeginOrResume"); Call(menu,"BeginOrResume"); Step(1); break;
                    case 1:
                        if (intro==null || intro.PlaybackTime<.25f) return;
                        Require(Object.FindObjectsByType<IntroCinematicPlayer>(FindObjectsSortMode.None).Length==1,"Duplicate activation");
                        Require(UnderworldFrontEnd.BlocksGameplay && Time.timeScale==0,"Gameplay escaped intro");
                        Require(UnderworldSoundscape.Instance.CinematicActive,"Music not muted");
                        Require(PaintedChamberMap.Active!=null && !PaintedChamberMap.Active.overlayRoot.gameObject.activeSelf,"Navigation colors enabled on first entry");
                        var titleGroup=(CanvasGroup)menu.GetType().GetField("_canvasGroup",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(menu);
                        Require(!titleGroup.interactable,"Hidden title remains selectable");
                        if (VoiceDirector.Instance!=null) Require(!VoiceSource.isPlaying,"Dialogue overlaps intro");
                        Require(intro.GetComponentsInChildren<Button>(true).Length==0,"Introduction still has buttons");
                        var skipCaption=intro.transform.Find("IntroSkipCaption").GetComponent<Text>();
                        Require(skipCaption.text=="Hold Enter to skip" && skipCaption.font==GameTypography.Font && skipCaption.rectTransform.anchorMin==Vector2.right,"Missing bottom-right skip caption");
                        intro.SetPaused(false); menu.TogglePause(); intro.TogglePaused();
                        Require(intro.IsPaused,"Duplicate input toggled pause twice");
                        _pausedClock=intro.PlaybackTime; Step(2,.35); break;
                    case 2:
                        Require(intro!=null && intro.IsPaused && Mathf.Abs(intro.PlaybackTime-_pausedClock)<.005f,"Pause clock moved: "+_pausedClock+" -> "+intro?.PlaybackTime);
                        intro.SetPaused(false); intro.Seek(112.62f); Step(3,.7); break;
                    case 3:
                        Require(intro!=null && intro.PlaybackTime>=112.62f && intro.PlaybackTime<116,"Seek/loading clock");
                        var incoming=intro.transform.Find("PaintingViewport/IncomingPainting").GetComponent<RawImage>();
                        Require(incoming.texture!=null && incoming.texture.name=="21","Wrong strike painting");
                        intro.SetPaused(true);
                        Capture(intro);
                        Step(4,.3); break;
                    case 4:
                        if (Keyboard.current==null) InputSystem.AddDevice<Keyboard>();
                        InputSystem.EnableDevice(Keyboard.current);
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Enter));
                        Step(41,1.1); break;
                    case 41:
                        Require(intro!=null && intro.IsPlaying && intro.SkipHoldProgress==0,"Paused hold advanced or skipped intro");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        intro.SetPaused(false); Step(42,.15); break;
                    case 42:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Enter));
                        Step(43,.25); break;
                    case 43:
                        Require(intro!=null && intro.IsPlaying && intro.SkipHoldProgress>0 && intro.SkipHoldProgress<1,"Short Enter hold did not show progress");
                        var progress=intro.transform.Find("IntroSkipProgress").GetComponent<Image>();
                        Require(progress.rectTransform.sizeDelta.x>0 && progress.rectTransform.sizeDelta.x<150 && !progress.raycastTarget,"Hold progress is missing or interactive");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        Step(44,.15); break;
                    case 44:
                        Require(intro!=null && intro.IsPlaying && intro.SkipHoldProgress==0,"Releasing Enter failed to reset hold");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Backspace));
                        Step(45,.2); break;
                    case 45:
                        Require(intro!=null && intro.IsPlaying && intro.SkipHoldProgress==0,"Backspace bypassed hold-to-skip");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        Step(46,.15); break;
                    case 46:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Enter));
                        Step(47,.3); break;
                    case 47:
                        Require(intro!=null && intro.SkipHoldProgress>0,"Fresh hold did not begin");
                        intro.SetPaused(true); Step(48,1.1); break;
                    case 48:
                        Require(intro!=null && intro.IsPlaying && intro.SkipHoldProgress==0,"Pause retained hold progress");
                        intro.SetPaused(false); Step(49,1.1); break;
                    case 49:
                        Require(intro!=null && intro.IsPlaying && intro.SkipHoldProgress==0,"Held Enter across resume skipped intro without fresh press");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        Step(491,.15); break;
                    case 491:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Enter));
                        Step(492,.35); break;
                    case 492:
                        Require(intro!=null && intro.IsPlaying && intro.SkipHoldProgress>0 && intro.SkipHoldProgress<1,"Final hold skipped early");
                        Step(493,.65); break;
                    case 493:
                        Require(intro!=null && Field(intro,"_skipping"),"Sustained Enter did not start skip fade");
                        Step(40,.3); break;
                    case 40:
                        Require(intro!=null && !intro.IsPlaying,"No end-frame handoff fade");
                        var fade=intro.GetComponent<CanvasGroup>();
                        Require(fade!=null && fade.alpha>0 && fade.alpha<1,"Handoff did not fade");
                        menu.TogglePause();
                        Require(Field(menu,"_startingIntro") && !Field(menu,"_open"),"Pause interrupted handoff");
                        Step(5,1); break;
                    case 5:
                        Require(intro==null && Field(menu,"_inRun"),"Skip failed to start journey");
                        Require(!UnderworldSoundscape.Instance.CinematicActive,"Music mute leaked");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        Step(12,.2); break;
                    case 12:
                        if (VoiceDirector.Instance.CurrentLine != VoiceLineId.Narrator_ClearChambers) return;
                        Require(!VoiceDirector.BlocksGameplay && Time.timeScale > 0, "Tutorial is not interactive");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.D));
                        Step(50,.65); break;
                    case 50:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        Require(VoiceDirector.Instance.CurrentLine == VoiceLineId.Narrator_DefendYourself, "Moving did not skip the movement instruction");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.J));
                        Step(51,.06); break;
                    case 51:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        Step(52,.45); break;
                    case 52:
                        Require(VoiceDirector.Instance.CurrentLine == VoiceLineId.Narrator_RepellingMove, "Soft note did not skip its instruction");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.K));
                        Step(53,.25); break;
                    case 53:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.F));
                        Require(VoiceDirector.Instance.CurrentLine == VoiceLineId.Narrator_CombosFlee, "Boom did not skip its instruction: "+VoiceDirector.Instance.CurrentLine+" count="+OrpheusLyreCombat.Instance.HardBoomCount);
                        Step(54,.25); break;
                    case 54:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        Require(VoiceDirector.Instance.CurrentLine == VoiceLineId.Narrator_PressF, "Flute did not skip its instruction");
                        Step(55,.1); break;
                    case 55:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.F));
                        Step(555,.2); break;
                    case 555:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Space));
                        Step(56,.3); break;
                    case 56:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.J));
                        Require(VoiceDirector.Instance.CurrentLine == VoiceLineId.Narrator_PluckCadences, "Dash did not skip its instruction");
                        Step(57,.06); break;
                    case 57:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        Step(58,.3); break;
                    case 58:
                        Require(UnderworldJourney.Progress.TutorialComplete && !VoiceDirector.HasPendingSpeech, "Final tutorial action did not finish narration");
                        var cache=(Dictionary<VoiceLineId,AudioClip>)typeof(VoiceDirector).GetField("_clipCache",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(VoiceDirector.Instance);
                        cache[VoiceLineId.Hades_ABreath]=AudioClip.Create("EnterSkipProbe",441000,1,44100,false);
                        _skipped=0;
                        VoiceDirector.Instance.Play(VoiceLineId.Hades_ABreath,()=>_skipped++);
                        Step(59,.2); break;
                    case 59:
                        Require(VoiceDirector.BlocksGameplay && Time.timeScale==0,"Story does not pause");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Enter));
                        Step(60,.2); break;
                    case 60:
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        Require(_skipped==1 && !VoiceDirector.HasPendingSpeech && !VoiceSource.isPlaying,"Enter did not stop speech/complete exactly once");
                        int silent=0;
                        VoiceDirector.Instance.Play(VoiceLineId.Eurydice_AreYouStillThere,()=>silent++);
                        VoiceDirector.Instance.PlayNextPingReply(); VoiceDirector.Instance.PlaySeparationCallout(); VoiceDirector.Instance.PlayLookBackTemptation();
                        Require(silent==1 && !VoiceDirector.HasPendingSpeech,"Recognition chatter remains queued");
                        SensorPulseInput.Instance.SetSourceMode(SensorSourceMode.KeyboardPlusPCMicrophone);
                        Require(Mathf.Abs(Time.timeScale-.8f)<.001f,"Mic mode is not .8 speed");
                        menu.TogglePause();
                        Require(Time.timeScale==0,"Mic mode defeated menu pause");
                        Call(menu,"BeginOrResume"); Step(61,.2); break;
                    case 61:
                        Require(Mathf.Abs(Time.timeScale-.8f)<.001f,"Mic resume lost .8 speed");
                        SensorPulseInput.Instance.SetSourceMode(SensorSourceMode.KeyboardAndMouseOnly);
                        Require(Mathf.Abs(Time.timeScale-1f)<.001f,"Keyboard mode failed to restore speed");
                        Step(62,.2); break;
                    case 62:
                        if (VoiceDirector.HasPendingSpeech) return;
                        Require(Time.timeScale>0,"Startup voice did not release simulation");
                        CaptureChamber("first-chamber-clean");
                        InstallShortVoices();
                        var player=OrpheusPlayerController.Instance;
                        var shadeRoot=new GameObject("GuiFeedbackPreview");
                        shadeRoot.transform.position=player.transform.position + UnderworldCamera2D.IsoRightXZ*3f;
                        var shade=shadeRoot.AddComponent<ShadeEnemy>(); shade.SetupArchetype(ShadeArchetype.ArmoredWarden); shade.enabled=false;
                        shade.OnOrphicHit(80f,.6f,false,player.transform.position);
                        VoiceDirector.Instance.PlaySequence(new[]{VoiceLineId.Hades_ABreath,VoiceLineId.Hades_YourStrings},()=>_sequenceDone++);
                        VoiceDirector.Instance.Play(VoiceLineId.Hades_YouDare,()=>_tailDone++);
                        Step(13,.2); break;
                    case 13:
                        Require(VoiceDirector.BlocksGameplay && UnderworldFrontEnd.BlocksGameplay && Time.timeScale==0,"Speech did not lock simulation/input");
                        var source=VoiceSource;
                        Require(source.volume==1 && source.clip!=null && source.clip.name=="VerifyFirst","Voice was interrupted or quiet");
                        SeenVoices.Add(source.clip.name);
                        var wind=(AudioSource)typeof(UnderworldSoundscape).GetField("_wind",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(UnderworldSoundscape.Instance);
                        Require(wind.volume<.05f,"Ambient not ducked under voice");
                        var subtitle=(CanvasGroup)typeof(OrpheusHud).GetField("_subtitleGroup",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(OrpheusHud.Instance);
                        Require(subtitle.alpha>.5f,"Subtitles invisible during locked speech");
                        var feedback=Object.FindFirstObjectByType<EnemyCombatFeedback>();
                        Call(feedback,"LateUpdate");
                        var damageText=feedback.GetComponentsInChildren<Text>().Single(t=>t.name=="EnemyDamageNumber");
                        Require(damageText.text=="14" && damageText.font==GameTypography.Font,"Applied armored damage/font mismatch");
                        Require(damageText.rectTransform.anchoredPosition.sqrMagnitude>100,"Damage number not anchored near enemy");
                        Require(!feedback.GetComponentsInChildren<Text>(true).Any(t=>t.name=="EnemyHealthValue"),"Enemy numeric health label remains");
                        var map=PaintedChamberMap.Active; var door=map.ActivePassage;
                        bool unlocked=door.IsUnlocked; door.SetUnlocked(true);
                        typeof(ChamberExitDoor).GetMethod("TryEnter",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(door,new object[]{OrpheusPlayerController.Instance.GetComponent<Collider>()});
                        Require(!Field(door,"_used") && !HadesChamberDirector.Instance.TryJourneyPassage(door) && !PaintedChamberScene.Travel(1,false),"Voice lock allowed chamber travel");
                        door.SetUnlocked(unlocked);
                        var animator=Object.FindFirstObjectByType<OrpheusSpriteAnimator>();
                        var state=animator.CurrentState;
                        typeof(OrpheusLyreCombat).GetMethod("HandleAttackPulse",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(OrpheusLyreCombat.Instance,new object[]{1f,true});
                        Require(animator.CurrentState==state,"Attack interacted during voice");
                        CaptureChamber("voice-lock-subtitle");
                        menu.TogglePause(); Step(14,.25); break;
                    case 14:
                        _pausedSamples=VoiceSource.timeSamples;
                        _pausedSubtitle=(float)typeof(OrpheusHud).GetField("_subtitleTimer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(OrpheusHud.Instance);
                        Step(15,.35); break;
                    case 15:
                        Require(VoiceSource.timeSamples==_pausedSamples,"Menu pause advanced voice");
                        float subtitleTime=(float)typeof(OrpheusHud).GetField("_subtitleTimer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(OrpheusHud.Instance);
                        Require(Mathf.Abs(subtitleTime-_pausedSubtitle)<.01f,"Menu pause advanced subtitles");
                        Call(menu,"BeginOrResume"); Step(16,.35); break;
                    case 16:
                        var playing=VoiceSource;
                        if (VoiceDirector.BlocksGameplay)
                        {
                            Require(Time.timeScale==0,"Simulation unlocked between voices");
                            if (playing.isPlaying) SeenVoices.Add(playing.clip.name);
                            if (playing.clip.name=="VerifySecond") Require(_sequenceDone==0,"Sequence completed early");
                            if (playing.clip.name=="VerifyTail") Require(_sequenceDone==1 && _tailDone==0,"Queued tail ordering");
                            return;
                        }
                        Require(_sequenceDone==1 && _tailDone==1 && SeenVoices.Count==3 && Time.timeScale>0,"Queue did not complete/release exactly once");
                        var publicFont=Resources.Load<Font>(GameTypography.PublicResource);
                        Require(publicFont!=null,"Public typeface missing");
                        foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) text.font=publicFont;
                        OrpheusHud.Instance.ShowSubtitle("Narrator",GameTypography.Gold,"Even in the deepest dark, a song can guide the way.",3f);
                        CaptureChamber("gui-public-font");
                        VoiceDirector.Instance.Play(VoiceLineId.Hades_ABreath);
                        VoiceDirector.Instance.enabled=false;
                        Require(!VoiceDirector.BlocksGameplay && Time.timeScale>0,"Disabled voice leaked lock");
                        VoiceDirector.Instance.enabled=true;
                        Require(PaintedChamberScene.Travel(7,false),"Could not enter final chamber for runtime verification");
                        Step(70,2); break;
                    case 70:
                        Require(PaintedChamberMap.Active.definition.index==7,"Final chamber did not load");
                        if (VoiceDirector.HasPendingSpeech) { VoiceDirector.Instance.SkipCurrentLine(); return; }
                        foreach(var enemy in Object.FindObjectsByType<ShadeEnemy>(FindObjectsSortMode.None)) enemy.enabled=false;
                        var finalMap=PaintedChamberMap.Active;
                        var finalPlayer=OrpheusPlayerController.Instance;
                        Require(finalMap.IsWalkable(finalPlayer.transform.position,finalPlayer.bodyRadius),"Final arrival outside real floor");
                        CaptureChamber("final-chamber-arrival");
                        finalPlayer.enabled=false;
                        foreach (var blocked in new[]{new Vector2(675,725),new Vector2(1080,840),new Vector2(1130,900)})
                        {
                            finalPlayer.transform.position=finalMap.ToWorld(finalMap.definition.entrance);
                            Physics.SyncTransforms();
                            typeof(OrpheusPlayerController).GetMethod("MoveSweepXZ",BindingFlags.Instance|BindingFlags.NonPublic)
                                .Invoke(finalPlayer,new object[]{finalMap.ToWorld(blocked)-finalPlayer.transform.position,true});
                            Require(finalMap.IsWalkable(finalPlayer.transform.position,finalPlayer.bodyRadius),"Dash escaped final chamber at "+blocked);
                            Require(Vector2.Distance(finalMap.ToImage(finalPlayer.transform.position),blocked)>12,"Dash stood on final chamber stone at "+blocked);
                        }
                        finalPlayer.transform.position=finalMap.ToWorld(finalMap.definition.entrance); finalPlayer.enabled=true;
                        menu.TogglePause(); Call(menu,"BeginOrResume"); Step(6,.6); break;
                    case 6:
                        Require(intro==null && !Field(menu,"_open"),"Resume replayed intro");
                        Call(menu,"Leave"); Step(7,2); break;
                    case 7:
                        if (Field(menu,"_inRun")) return;
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(UnityEngine.InputSystem.Key.Enter));
                        Step(701,.15); break;
                    case 701:
                        Call(menu,"ReplayIntro"); Step(8,1.3); break;
                    case 8:
                        if (intro==null || intro.PlaybackTime<.25f) return;
                        Require(intro.IsPlaying && intro.SkipHoldProgress==0,"Enter held before activation skipped replay");
                        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                        PlayerPrefs.DeleteKey(IntroCinematicPlayer.ViewedKey);
                        intro.GetComponent<AudioSource>().Stop(); Step(9,.6); break;
                    case 9:
                        Require(intro==null && !Field(menu,"_inRun") && Field(menu,"_open"),"Audio failure started gameplay");
                        Require(!PlayerPrefs.HasKey(IntroCinematicPlayer.ViewedKey),"Failure marked viewed");
                        Call(menu,"ReplayIntro"); Step(10,.5); break;
                    case 10:
                        if (intro==null || intro.PlaybackTime<.25f) return;
                        intro.Seek(223.3f); Step(11,1.8); break;
                    case 11:
                        Require(intro==null && Field(menu,"_open") && !Field(menu,"_inRun"),"Replay completion did not return to title");
                        Require(PlayerPrefs.GetInt(IntroCinematicPlayer.ViewedKey)==1,"Natural completion not recorded");
                        Require(_errors==0,"Unexpected runtime errors: "+_errors);
                        File.WriteAllText(Path.Combine(Output,"result.json"),"{\"passed\":true,\"checks\":[\"boot auto intro\",\"navigation overlay hidden\",\"duplicate activation\",\"pause deduplication\",\"pause clock\",\"no intro buttons\",\"bottom-right caption\",\"seek and strike painting\",\"Enter short hold and release reset\",\"pause resets hold and requires fresh press\",\"sustained Enter input skip\",\"held Enter handoff does not skip gameplay speech\",\"handoff fade\",\"pause during handoff\",\"voice simulation lock\",\"voice full gain\",\"ambient ducking\",\"visible synchronized subtitles\",\"locked trigger and direct travel\",\"locked attack\",\"voice and subtitle menu pause\",\"queued voice order\",\"sequence gaps locked\",\"callback exactly once\",\"disabled voice cleanup\",\"resume bypass\",\"replay\",\"pre-held Enter requires release on activation\",\"audio failure\",\"natural ending\",\"cleanup\",\"six tutorial actions skip speech\",\"Enter skips speech exactly once\",\"recognition chatter removed\",\"mic .8 speed and menu resume\",\"final chamber arrival and wall dash sweeps\"]}");
                        Finish(0); break;
                }
            }
            catch(Exception error) { File.WriteAllText(Path.Combine(Output,"failure.txt"),error.ToString()); Debug.LogException(error); Finish(1); }
        }

        private static void InstallShortVoices()
        {
            var cache=(Dictionary<VoiceLineId,AudioClip>)typeof(VoiceDirector).GetField("_clipCache",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(VoiceDirector.Instance);
            cache[VoiceLineId.Hades_ABreath]=AudioClip.Create("VerifyFirst",66150,1,44100,false);
            cache[VoiceLineId.Hades_YourStrings]=AudioClip.Create("VerifySecond",26460,1,44100,false);
            cache[VoiceLineId.Hades_YouDare]=AudioClip.Create("VerifyTail",26460,1,44100,false);
        }
        private static void CaptureChamber(string name)
        {
            typeof(PaintedChamberVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1920,1080,true,null});
            File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/PaintedChambers/"+name+".png")),Path.Combine(Output,name+".png"),true);
        }

        private static void Capture(IntroCinematicPlayer intro)
        {
            var camera=Camera.main;
            var canvas=intro.GetComponent<Canvas>();
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var enabled=new bool[canvases.Length];
            for (int i=0;i<canvases.Length;i++) { enabled[i]=canvases[i].enabled; canvases[i].enabled=canvases[i]==canvas; }
            var viewport=(RectTransform)intro.transform.Find("PaintingViewport");
            Vector2 oldSize=viewport.sizeDelta;
            float ratio=1920f/oldSize.x;
            foreach (var image in viewport.GetComponentsInChildren<RawImage>()) { image.rectTransform.sizeDelta*=ratio; image.rectTransform.anchoredPosition*=ratio; }
            viewport.sizeDelta=new Vector2(1920,1080);
            var target=RenderTexture.GetTemporary(1920,1080,24,UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
            camera.targetTexture=target; Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
            RenderTexture.active=target;
            var pixels=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Output,"bite.png"),pixels.EncodeToPNG()); Object.Destroy(pixels);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null; camera.targetTexture=oldTarget; RenderTexture.active=oldActive; RenderTexture.ReleaseTemporary(target);
            for (int i=0;i<canvases.Length;i++) canvases[i].enabled=enabled[i];
            viewport.sizeDelta=oldSize;
            foreach (var image in viewport.GetComponentsInChildren<RawImage>()) { image.rectTransform.sizeDelta/=ratio; image.rectTransform.anchoredPosition/=ratio; }
        }
    }
}
