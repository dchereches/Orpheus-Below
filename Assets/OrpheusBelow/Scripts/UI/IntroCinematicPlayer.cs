using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace OrpheusBelow
{
    /// <summary>One narration clock drives the approved paintings; no independent shot timers.</summary>
    public sealed class IntroCinematicPlayer : MonoBehaviour
    {
        public const float SkipHoldSeconds = .9f;
        public float SkipHoldProgress => Mathf.Clamp01(_skipHoldElapsed / SkipHoldSeconds);
        public const string ViewedKey = "OrpheusBelow.IntroViewed";
        public bool IsPlaying { get; private set; }
        public bool Succeeded { get; private set; }
        public bool IsPaused => _paused;
        public float PlaybackTime => _clock;
        private IntroCinematicTimeline _timeline;
        private AudioSource _audio;
        private readonly VoiceAudioClipCache _loudClips = new VoiceAudioClipCache();
        private RawImage _back, _front;
        private Image _black, _skipProgress;
        private Text _skipCaption;
        private float _skipHoldElapsed;
        private bool _skipInputReady;
        private ButtonControl _skipHoldButton;
        private RectTransform _viewport;
        private Text _status;
        private readonly Dictionary<int,Texture2D> _textures = new Dictionary<int,Texture2D>();
        private readonly Dictionary<int,ResourceRequest> _requests = new Dictionary<int,ResourceRequest>();
        private bool _paused, _skipping, _waiting;
        private ResourceRequest _narrationRequest;
        private float _clock, _beganAt;
        private int _lastToggleFrame = -1;

        public void Begin()
        {
            if (IsPlaying) return;
            IsPlaying = true;
            _skipInputReady = HeldSkipButton() == null;
            _waiting = true;
            try { _timeline = IntroCinematicTimeline.Load(); }
            catch (Exception error) { Debug.LogError(error.Message); Finish(false); return; }
            BuildView();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.ignoreListenerPause = true;
            _audio.spatialBlend = 0;
            _audio.priority = 0;
            if (UnderworldSoundscape.Instance != null) UnderworldSoundscape.Instance.CinematicActive = true;
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            var narration = _narrationRequest = Resources.LoadAsync<AudioClip>("Cinematic/Intro/Narration");
            yield return narration;
            if (!IsPlaying) yield break;
            var narrationAsset = narration.asset as AudioClip;
            _audio.clip = _loudClips.Prepare(narrationAsset);
            if (narrationAsset != null && _audio.clip != narrationAsset) Resources.UnloadAsset(narrationAsset);
            _narrationRequest = null;
            if (_audio.clip == null) { Fail("Intro narration is missing."); yield break; }
            if (Mathf.Abs(_audio.clip.length-_timeline.duration)>.1f) { Fail("Intro narration length does not match the timeline."); yield break; }
            yield return Ensure(0);
            if (!IsPlaying) yield break;
            yield return Ensure(1);
            if (!IsPlaying) yield break;
            _waiting = false;
            _status.text = "";
            _beganAt = Time.unscaledTime;
            if (!_paused) _audio.Play();
            else { _audio.Play(); _audio.Pause(); }
            while (IsPlaying)
            {
                if (_skipping) { yield return null; continue; }
                if (!_paused && !_audio.isPlaying && Time.unscaledTime-_beganAt>.1f)
                {
                    if (IntroCinematicTimeline.ReachedEnd(_clock, Time.unscaledDeltaTime, _timeline.duration)) Finish(true);
                    else Fail("Intro narration stopped before its ending.");
                    yield break;
                }
                _clock = _audio.timeSamples/(float)_audio.clip.frequency;
                if (_clock>=_timeline.duration-.005f) { Finish(true); yield break; }
                int index = _timeline.FindShot(_clock);
                int previous = index>0 && _clock<_timeline.shots[index].start+_timeline.shots[index].fade ? index-1 : index;
                if (Get(index)==null || Get(previous)==null)
                {
                    _waiting=true;
                    _audio.Pause();
                    _status.text = "Loading painting…";
                    yield return Ensure(index);
                    yield return Ensure(previous);
                    if (!IsPlaying) yield break;
                    _waiting=false;
                    _status.text = _paused ? "Paused" : "";
                    if (!_paused) _audio.UnPause();
                }
                Render(_back, previous, 1);
                Render(_front, index, _timeline.Alpha(index,_clock));
                _black.color = new Color(0,0,0,_timeline.BlackAlpha(_clock));
                Request(index+1);
                Prune(index);
                yield return null;
            }
        }

        private void Request(int index)
        {
            if (index<0 || index>=_timeline.shots.Length || _textures.ContainsKey(index) || _requests.ContainsKey(index)) return;
            _requests[index] = Resources.LoadAsync<Texture2D>(_timeline.shots[index].resource);
        }
        private Texture2D Get(int index)
        {
            if (_textures.TryGetValue(index,out var texture)) return texture;
            if (_requests.TryGetValue(index,out var request) && request.isDone && request.asset is Texture2D ready)
            { _requests.Remove(index); _textures[index]=ready; return ready; }
            return null;
        }
        private IEnumerator Ensure(int index)
        {
            if (!IsPlaying || Get(index)!=null) yield break;
            Request(index);
            yield return _requests[index];
            if (IsPlaying && Get(index)==null) Fail("Intro painting is missing: " + _timeline.shots[index].resource);
        }
        private void Prune(int current)
        {
            var remove = new List<int>();
            foreach (var pair in _textures) if (pair.Key<current-1 || pair.Key>current+1) remove.Add(pair.Key);
            foreach (int id in remove) { Resources.UnloadAsset(_textures[id]); _textures.Remove(id); }
        }

        private void Render(RawImage image, int index, float alpha)
        {
            var texture = Get(index);
            image.texture = texture;
            image.color = new Color(1,1,1,alpha);
            float width = Mathf.Min(Screen.width,Screen.height*16f/9), height=width*9/16;
            _viewport.sizeDelta = new Vector2(width,height);
            var shot = _timeline.shots[index];
            float end = index+1<_timeline.shots.Length ? _timeline.shots[index+1].start+_timeline.shots[index+1].fade : _timeline.duration;
            float p = Mathf.Clamp01((_clock-shot.start)/(end-shot.start));
            string move = (shot.camera ?? "").ToLowerInvariant();
            if (move.Contains("then hold") || move.Contains("settling into stillness")) p=Mathf.Min(p/.7f,1);
            float zoom = move.Contains("pull") ? Mathf.Lerp(1.07f,1.03f,p) : Mathf.Lerp(1.02f,1.06f,p);
            if (move.Contains("locked composition")) zoom=1;
            else if (move.Contains("nearly locked")) zoom=Mathf.Lerp(1.02f,1.026f,p);
            float x=.5f,y=.5f;
            if (move.Contains("pan") || move.Contains("lateral") || move.Contains("horizontal") || move.Contains("track")) x=Mathf.Lerp(.25f,.75f,p);
            if (move.Contains("downward") || move.Contains("vertical") || move.Contains("tilt")) y=Mathf.Lerp(.25f,.75f,p);
            if (move.Contains("rise") || move.Contains("rising") || move.Contains("from the lyre to his face") || move.Contains("from the lyre to his lowered face")) y=Mathf.Lerp(.75f,.25f,p);
            var size = new Vector2(width,height);
            if (texture != null)
            {
                float aspect=texture.width/(float)texture.height;
                if (aspect>16f/9) size.x=height*aspect; else size.y=width/aspect;
            }
            size*=zoom;
            image.rectTransform.sizeDelta=size;
            image.rectTransform.anchoredPosition=new Vector2((size.x-width)*(.5f-x),(size.y-height)*(y-.5f));
        }

        public void TogglePaused()
        {
            if (_lastToggleFrame == Time.frameCount) return;
            _lastToggleFrame = Time.frameCount;
            SetPaused(!_paused);
        }
        public void SetPaused(bool paused)
        {
            if (!IsPlaying || _skipping) return;
            _paused=paused;
            ResetSkipHold();
            _skipInputReady=false;
            if (_audio!=null && _audio.clip!=null)
            {
                if (paused)
                {
                    _audio.Pause();
                    _clock=_audio.timeSamples/(float)_audio.clip.frequency;
                }
                else if (!_waiting) _audio.UnPause();
            }
            if (_status!=null) _status.text=paused ? "Paused" : "";
        }
        public void Seek(float seconds)
        {
            if (!IsPlaying || _audio==null || _audio.clip==null || _skipping) return;
            _audio.timeSamples=Mathf.Clamp(Mathf.RoundToInt(seconds*_audio.clip.frequency),0,_audio.clip.samples-1);
            _clock=_audio.timeSamples/(float)_audio.clip.frequency;
        }
        public void Skip()
        {
            if (!IsPlaying || _skipping) return;
            _skipping=true;
            ResetSkipHold();
            _audio?.Pause();
            StartCoroutine(FadeSkip());
        }
        private IEnumerator FadeSkip()
        {
            float from=_black.color.a;
            for (float t=0;t<.25f;t+=Time.unscaledDeltaTime)
            { _black.color=new Color(0,0,0,Mathf.Lerp(from,1,t/.25f)); yield return null; }
            Finish(true);
        }
        private void Update()
        {
            if (!IsPlaying) return;
            var keyboard=Keyboard.current; var pad=Gamepad.current;
            if ((keyboard!=null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) || (pad!=null && pad.startButton.wasPressedThisFrame)) TogglePaused();
            UpdateSkipHold();
        }
        private static ButtonControl HeldSkipButton()
        {
            var keyboard=Keyboard.current;
            if (keyboard!=null)
            {
                if (keyboard.enterKey.isPressed) return keyboard.enterKey;
                if (keyboard.numpadEnterKey.isPressed) return keyboard.numpadEnterKey;
            }
            var pad=Gamepad.current;
            return pad!=null && pad.buttonEast.isPressed ? pad.buttonEast : null;
        }
        private void ResetSkipHold()
        {
            _skipHoldElapsed=0;
            _skipHoldButton=null;
            if (_skipProgress!=null) _skipProgress.rectTransform.sizeDelta=new Vector2(0,2);
        }
        private void UpdateSkipHold()
        {
            var held=HeldSkipButton();
            if (_skipping || _paused || _waiting)
            {
                ResetSkipHold();
                _skipInputReady=false;
                return;
            }
            if (held==null)
            {
                ResetSkipHold();
                _skipInputReady=true;
                return;
            }
            if (!_skipInputReady) return;
            if (_skipHoldButton==null)
            {
                // A key already down on activation or resume must first be released.
                if (!held.wasPressedThisFrame) { _skipInputReady=false; return; }
                _skipHoldButton=held;
                _skipCaption.text=held==Gamepad.current?.buttonEast ? "Hold B to skip" : "Hold Enter to skip";
                return;
            }
            if (held!=_skipHoldButton)
            {
                ResetSkipHold();
                _skipInputReady=false;
                return;
            }
            _skipHoldElapsed+=Time.unscaledDeltaTime;
            _skipProgress.rectTransform.sizeDelta=new Vector2(150*SkipHoldProgress,2);
            if (_skipHoldElapsed>=SkipHoldSeconds) Skip();
        }
        private void OnApplicationPause(bool paused) { if (paused) SetPaused(true); }
        private void Fail(string message) { Debug.LogError(message); Finish(false); }
        private void Finish(bool success)
        {
            if (!IsPlaying) return;
            IsPlaying=false; Succeeded=success;
            ResetSkipHold();
            if (_black!=null) _black.color=Color.black;
            if (success) { PlayerPrefs.SetInt(ViewedKey,1); PlayerPrefs.Save(); }
            if (UnderworldSoundscape.Instance!=null) UnderworldSoundscape.Instance.CinematicActive=false;
            if (_back!=null) _back.texture=null;
            if (_front!=null) _front.texture=null;
            if (_audio!=null)
            {
                var clip=_audio.clip; _audio.Stop(); _audio.clip=null;
                if (clip!=null && (clip.hideFlags & HideFlags.DontSave)==0) Resources.UnloadAsset(clip);
            }
            _loudClips.Dispose();
            if (_narrationRequest!=null)
            {
                var pending=_narrationRequest;
                if (pending.isDone) { if (pending.asset!=null) Resources.UnloadAsset(pending.asset); }
                else pending.completed+=op=> { if (pending.asset!=null) Resources.UnloadAsset(pending.asset); };
                _narrationRequest=null;
            }
            foreach (var texture in _textures.Values) Resources.UnloadAsset(texture);
            _textures.Clear();
            foreach (var request in _requests.Values)
            {
                if (request.isDone) { if (request.asset!=null) Resources.UnloadAsset(request.asset); }
                else { var pending=request; pending.completed+=op=> { if (pending.asset!=null) Resources.UnloadAsset(pending.asset); }; }
            }
            _requests.Clear();
        }
        private void OnDestroy() { Finish(false); }

        public IEnumerator FadeAway(float duration)
        {
            var group=gameObject.AddComponent<CanvasGroup>();
            group.interactable=false; group.blocksRaycasts=true;
            for (float elapsed=0; elapsed<duration; elapsed+=Time.unscaledDeltaTime)
            { group.alpha=1-Mathf.Clamp01(elapsed/duration); yield return null; }
            group.alpha=0;
        }

        private static RectTransform Rect(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            return rect;
        }
        private void BuildView()
        {
            var canvas=gameObject.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=32760;
            var background=Rect("Letterbox",transform).gameObject.AddComponent<Image>(); background.color=Color.black;
            background.rectTransform.anchorMin=Vector2.zero; background.rectTransform.anchorMax=Vector2.one; background.rectTransform.sizeDelta=Vector2.zero;
            _viewport=Rect("PaintingViewport",transform); _viewport.gameObject.AddComponent<RectMask2D>();
            _back=Rect("OutgoingPainting",_viewport).gameObject.AddComponent<RawImage>(); _back.raycastTarget=false;
            _front=Rect("IncomingPainting",_viewport).gameObject.AddComponent<RawImage>(); _front.raycastTarget=false;
            _black=Rect("Fade",transform).gameObject.AddComponent<Image>(); _black.color=Color.black;
            _black.rectTransform.anchorMin=Vector2.zero; _black.rectTransform.anchorMax=Vector2.one; _black.rectTransform.sizeDelta=Vector2.zero; _black.raycastTarget=false;
            _status=Label("Loading introduction…",new Vector2(0,85),new Vector2(420,32));
            Text skip=_skipCaption=Label("Hold Enter to skip",new Vector2(-36,24),new Vector2(250,30));
            skip.name="IntroSkipCaption";
            skip.rectTransform.anchorMin=skip.rectTransform.anchorMax=skip.rectTransform.pivot=Vector2.right;
            skip.alignment=TextAnchor.MiddleRight; skip.fontSize=20;
            skip.color=new Color(.96f,.92f,.81f,.78f);
            _skipProgress=Rect("IntroSkipProgress",transform).gameObject.AddComponent<Image>();
            _skipProgress.raycastTarget=false;
            _skipProgress.color=new Color(.96f,.92f,.81f,.55f);
            var progressRect=_skipProgress.rectTransform;
            progressRect.anchorMin=progressRect.anchorMax=Vector2.right;
            progressRect.pivot=Vector2.zero;
            progressRect.anchoredPosition=new Vector2(-186,18);
            progressRect.sizeDelta=new Vector2(0,2);
        }
        private Text Label(string text,Vector2 position,Vector2 size)
        {
            var label=Rect(text,transform).gameObject.AddComponent<Text>();
            label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,0); label.rectTransform.anchoredPosition=position; label.rectTransform.sizeDelta=size;
            label.text=text; GameTypography.Apply(label,18); label.alignment=TextAnchor.MiddleCenter;
            GameTypography.Shadow(label);
            return label;
        }
    }
}
