using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
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
            if (old<0) PlayerPrefs.DeleteKey(IntroCinematicPlayer.ViewedKey); else PlayerPrefs.SetInt(IntroCinematicPlayer.ViewedKey,old);
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
                    case 0: Call(menu,"BeginOrResume"); Call(menu,"BeginOrResume"); Step(1); break;
                    case 1:
                        if (intro==null || intro.PlaybackTime<.25f) return;
                        Require(Object.FindObjectsByType<IntroCinematicPlayer>(FindObjectsSortMode.None).Length==1,"Duplicate activation");
                        Require(UnderworldFrontEnd.BlocksGameplay && Time.timeScale==0,"Gameplay escaped intro");
                        Require(UnderworldSoundscape.Instance.CinematicActive,"Music not muted");
                        var titleGroup=(CanvasGroup)menu.GetType().GetField("_canvasGroup",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(menu);
                        Require(!titleGroup.interactable,"Hidden title remains selectable");
                        if (VoiceDirector.Instance!=null) Require(!VoiceDirector.Instance.GetComponent<AudioSource>().isPlaying,"Dialogue overlaps intro");
                        foreach (var button in intro.GetComponentsInChildren<Button>())
                        { Require(button.navigation.mode==Navigation.Mode.Explicit,"Automatic navigation"); Require(button.navigation.selectOnUp.transform.IsChildOf(intro.transform),"Navigation escaped"); }
                        intro.SetPaused(false); menu.TogglePause(); intro.TogglePaused();
                        Require(intro.IsPaused,"Duplicate input toggled pause twice");
                        _pausedClock=intro.PlaybackTime; Step(2,.35); break;
                    case 2:
                        Require(intro!=null && intro.IsPaused && Mathf.Abs(intro.PlaybackTime-_pausedClock)<.005f,"Pause clock moved");
                        intro.SetPaused(false); intro.Seek(112.62f); Step(3,.7); break;
                    case 3:
                        Require(intro!=null && intro.PlaybackTime>=112.62f && intro.PlaybackTime<116,"Seek/loading clock");
                        var incoming=intro.transform.Find("PaintingViewport/IncomingPainting").GetComponent<RawImage>();
                        Require(incoming.texture!=null && incoming.texture.name=="21","Wrong strike painting");
                        intro.SetPaused(true);
                        Capture(intro);
                        Step(4,.3); break;
                    case 4: intro.Skip(); intro.Skip(); Step(5,.8); break;
                    case 5:
                        Require(intro==null && Field(menu,"_inRun"),"Skip failed to start journey");
                        Require(!UnderworldSoundscape.Instance.CinematicActive,"Music mute leaked");
                        menu.TogglePause(); Call(menu,"BeginOrResume"); Step(6,.6); break;
                    case 6:
                        Require(intro==null && !Field(menu,"_open"),"Resume replayed intro");
                        Call(menu,"Leave"); Step(7,2); break;
                    case 7:
                        if (Field(menu,"_inRun")) return;
                        Call(menu,"ReplayIntro"); Step(8,.5); break;
                    case 8:
                        if (intro==null || intro.PlaybackTime<.25f) return;
                        PlayerPrefs.DeleteKey(IntroCinematicPlayer.ViewedKey);
                        intro.GetComponent<AudioSource>().Stop(); Step(9,.6); break;
                    case 9:
                        Require(intro==null && !Field(menu,"_inRun") && Field(menu,"_open"),"Audio failure started gameplay");
                        Require(!PlayerPrefs.HasKey(IntroCinematicPlayer.ViewedKey),"Failure marked viewed");
                        Call(menu,"ReplayIntro"); Step(10,.5); break;
                    case 10:
                        if (intro==null || intro.PlaybackTime<.25f) return;
                        intro.Seek(223.3f); Step(11,1); break;
                    case 11:
                        Require(intro==null && Field(menu,"_open") && !Field(menu,"_inRun"),"Replay completion did not return to title");
                        Require(PlayerPrefs.GetInt(IntroCinematicPlayer.ViewedKey)==1,"Natural completion not recorded");
                        Require(_errors==0,"Unexpected runtime errors: "+_errors);
                        File.WriteAllText(Path.Combine(Output,"result.json"),"{\"passed\":true,\"checks\":[\"new game\",\"duplicate activation\",\"pause deduplication\",\"pause clock\",\"bounded navigation\",\"seek and strike painting\",\"skip\",\"resume bypass\",\"replay\",\"audio failure\",\"natural ending\",\"cleanup\"]}");
                        Finish(0); break;
                }
            }
            catch(Exception error) { File.WriteAllText(Path.Combine(Output,"failure.txt"),error.ToString()); Debug.LogException(error); Finish(1); }
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
