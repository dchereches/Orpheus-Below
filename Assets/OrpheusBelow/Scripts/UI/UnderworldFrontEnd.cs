using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OrpheusBelow
{
    /// <summary>Title, pause and controls screens. Created last by the runtime scene builder.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class UnderworldFrontEnd : MonoBehaviour
    {
        public static UnderworldFrontEnd Instance { get; private set; }
        public static bool SkipTitleOnce;
        public static bool BlocksGameplay => _menuBlocksGameplay || Time.frameCount <= _blockThroughFrame;

        private static bool _menuBlocksGameplay;
        private static int _blockThroughFrame = -1;
        private static readonly Color Ivory = new Color(0.94f, 0.90f, 0.79f);
        private static readonly Color Bronze = new Color(0.72f, 0.54f, 0.31f);
        private static readonly Color Teal = new Color(0.48f, 0.72f, 0.69f);
        private static readonly Color Muted = new Color(0.64f, 0.66f, 0.61f);
        private Font _serif, _sans;
        private GameObject _canvasObject, _main, _help;
        private RawImage _panorama;
        private Texture2D _shadeTexture;
        private Text _eyebrow, _premise, _primaryLabel, _leaveLabel, _muteLabel, _volumeLabel;
        private Button _primary, _how, _mute, _leave, _helpBack;
        private Slider _volume;
        private EventSystem _events;
        private bool _open, _inRun, _reloading, _ownsSerif, _ownsSans;
        private int _lastWidth, _lastHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Instance = null;
            SkipTitleOnce = false;
            _menuBlocksGameplay = false;
            _blockThroughFrame = -1;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            bool skip = SkipTitleOnce;
            SkipTitleOnce = false;
            // Set before any canvas work; input consumers can already safely query the gate.
            _menuBlocksGameplay = !skip;
            _blockThroughFrame = Time.frameCount + 1;
            _serif = Font.CreateDynamicFontFromOSFont(new[] { "Palatino Linotype", "Georgia", "Times New Roman" }, 32);
            _sans = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 20);
            _ownsSerif = _serif != null;
            _ownsSans = _sans != null;
            if (_serif == null) _serif = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_sans == null) _sans = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas();
            _inRun = skip;
            if (skip)
            {
                _canvasObject.SetActive(false);
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
            else OpenMenu();
        }

        private void EnsureEventSystem()
        {
            _events = EventSystem.current;
            if (_events == null) _events = Object.FindFirstObjectByType<EventSystem>();
            if (_events == null)
            {
                var go = new GameObject("UnderworldEventSystem");
                go.transform.SetParent(transform, false);
                _events = go.AddComponent<EventSystem>();
            }
            var input = _events.GetComponent<InputSystemUIInputModule>();
            if (input == null)
            {
                // Input System projects must not leave a legacy input module active alongside this one.
                foreach (var old in _events.GetComponents<BaseInputModule>()) old.enabled = false;
                input = _events.gameObject.AddComponent<InputSystemUIInputModule>();
                input.AssignDefaultActions();
            }
            input.enabled = true;
            input.deselectOnBackgroundClick = false;
            _events.enabled = true;
            _events.sendNavigationEvents = true;
        }

        private void BuildCanvas()
        {
            var root = new GameObject("UnderworldFrontEnd", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            _canvasObject = root;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var screen = root.GetComponent<RectTransform>();
            var background = Stretch("Panorama", screen);
            background.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.055f, 0.06f);
            var art = Stretch("PaintedUnderworld", screen);
            _panorama = art.gameObject.AddComponent<RawImage>();
            _panorama.texture = Resources.Load<Texture2D>("Art/UnderworldPanorama");
            _panorama.raycastTarget = false;
            if (_panorama.texture == null) _panorama.enabled = false;

            _shadeTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false) { name = "TitleLegibilityGradient", wrapMode = TextureWrapMode.Clamp };
            for (int i = 0; i < 256; i++)
            {
                float x = i / 255f;
                float alpha = Mathf.Lerp(0.96f, 0.16f, Mathf.SmoothStep(0f, 1f, x));
                _shadeTexture.SetPixel(i, 0, new Color(0.018f, 0.035f, 0.04f, alpha));
            }
            _shadeTexture.Apply();
            var veil = Stretch("ReadabilityVeil", screen).gameObject.AddComponent<RawImage>();
            veil.texture = _shadeTexture;
            veil.raycastTarget = false;

            var content = new GameObject("SafeComposition", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(screen, false);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(1280f, 720f);
            content.anchoredPosition = Vector2.zero;
            _main = Stretch("TitleAndNavigation", content).gameObject;
            RectTransform main = _main.GetComponent<RectTransform>();
            Line(main, 72, 57, 84, Bronze);
            Label(main, "A JOURNEY THROUGH THE UNDERWORLD", 174, 44, 520, 26, 12, Bronze, false);
            _eyebrow = Label(main, "THE DESCENT", 72, 101, 480, 24, 13, Teal, false);
            Label(main, "ORPHEUS", 66, 132, 560, 81, 72, Ivory);
            Label(main, "B E L O W", 72, 214, 480, 43, 33, Ivory);
            _premise = Label(main, "A lyre against the silence. A soul worth the descent.\nLead Eurydice out of Hades. Do not look back.", 74, 274, 510, 58, 18, Muted);

            _primary = MakeButton(main, "BeginDescent", "Begin descent", 72, 355, 400, 49, BeginOrResume, out _primaryLabel, true);
            _how = MakeButton(main, "HowToPlay", "How to play", 72, 411, 400, 43, ShowHelp, out _);
            _mute = MakeButton(main, "MuteAudio", "Audio  ·  On", 72, 461, 400, 43, ToggleMute, out _muteLabel);
            _volumeLabel = Label(main, "VOLUME", 89, 520, 367, 22, 12, Muted, false);
            _volume = MakeVolumeSlider(main, 88, 548, 367);
            _leave = MakeButton(main, "Leave", "Quit", 72, 586, 400, 43, Leave, out _leaveLabel);
            Label(main, "WASD / ARROWS / D-PAD   navigate     ·     ENTER / A   select", 73, 669, 670, 23, 11, Muted, false);
            Label(main, "A song can cross what a blade cannot.", 760, 630, 445, 30, 18, Ivory).alignment = TextAnchor.MiddleRight;
            Line(main, 1118, 678, 88, Bronze);
            LinkVertical(_primary, _how, _mute, _volume, _leave);
            BuildHelp(content);
            SyncAudioControls();
            FitPanorama();
        }

        private void BuildHelp(RectTransform parent)
        {
            _help = Stretch("HowToPlaySheet", parent).gameObject;
            var dim = _help.AddComponent<Image>();
            dim.color = new Color(0.018f, 0.034f, 0.039f, 0.98f);
            var body = _help.GetComponent<RectTransform>();
            Label(body, "THE ART OF THE DESCENT", 72, 53, 1000, 27, 14, Bronze, false);
            Label(body, "Make music. Break the silence.", 70, 93, 1100, 61, 42, Ivory);
            Line(body, 72, 175, 1136, Bronze);

            Label(body, "I   MOVE WITH INTENT", 72, 204, 500, 28, 15, Teal, false);
            Label(body, "W A S D  /  Arrow keys — move\nMouse — aim your lyre\nSpace / Shift — dash; two charges\nF — enter the flute stance\nEsc / controller Start — pause", 72, 250, 505, 154, 21, Ivory);
            Label(body, "Survive the exchange", 72, 433, 505, 35, 27, Ivory);
            Label(body, "Dodge the tell, then answer. Vary your notes:\nrepeated attacks lose their effectiveness.\nBooms spend breath; let it recover between strikes.\nUse the flute to pacify weakened shades.", 72, 480, 510, 133, 19, Muted);

            Label(body, "II   LEARN THE LANGUAGE", 665, 204, 530, 28, 15, Teal, false);
            Label(body, "S = Soft   ·   J / left mouse button\nB = Boom   ·   K / right mouse button", 665, 250, 530, 72, 23, Ivory);
            Label(body, "S – S – B", 665, 350, 175, 35, 27, Bronze);
            Label(body, "Hymn of Dawn\nA piercing beam. Available from the beginning.", 851, 346, 350, 72, 17, Ivory);
            Label(body, "B – S", 665, 427, 175, 35, 27, Bronze);
            Label(body, "Sanctuary of Eurydice\nUnlocked after chamber I.", 851, 424, 350, 72, 17, Ivory);
            Label(body, "S – B – S", 665, 504, 175, 35, 27, Bronze);
            Label(body, "Siren’s Convergence\nA gathering vortex. Unlocked after chamber II.", 851, 501, 350, 72, 17, Ivory);
            Label(body, "S and B name notes, not movement keys.", 665, 583, 530, 25, 15, Muted, false);
            Line(body, 72, 630, 1136, new Color(Bronze.r, Bronze.g, Bronze.b, 0.45f));
            _helpBack = MakeButton(body, "Back", "Return", 72, 650, 250, 43, HideHelp, out _, true);
            Label(body, "Hold Q to look back — and lose Eurydice.     R restarts a run.", 465, 655, 740, 30, 16, Muted).alignment = TextAnchor.MiddleRight;
            var nav = _helpBack.navigation;
            nav.mode = Navigation.Mode.None;
            _helpBack.navigation = nav;
            _help.SetActive(false);
        }

        private Slider MakeVolumeSlider(RectTransform parent, float x, float y, float width)
        {
            var rt = Rect("MasterVolume", parent, x, y, width, 26f);
            rt.gameObject.AddComponent<Image>().color = Color.clear;
            var slider = rt.gameObject.AddComponent<Slider>();
            var track = Rect("Track", rt, 0, 11, width, 3).gameObject.AddComponent<Image>();
            track.color = new Color(0.24f, 0.29f, 0.28f);
            var fillArea = Stretch("FillArea", rt);
            fillArea.offsetMin = new Vector2(0f, 11f);
            fillArea.offsetMax = new Vector2(0f, -12f);
            var fill = Stretch("Fill", fillArea).gameObject.AddComponent<Image>();
            fill.color = Bronze;
            slider.fillRect = fill.rectTransform;
            var handleArea = Stretch("HandleArea", rt);
            handleArea.offsetMin = new Vector2(6, 0);
            handleArea.offsetMax = new Vector2(-6, 0);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            handle.transform.SetParent(handleArea, false);
            handle.color = Ivory;
            handle.rectTransform.sizeDelta = new Vector2(12, -6);
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.direction = Slider.Direction.LeftToRight;
            var sliderColors = slider.colors;
            sliderColors.highlightedColor = Teal;
            sliderColors.selectedColor = Teal;
            slider.colors = sliderColors;
            slider.onValueChanged.AddListener(value =>
            {
                UnderworldSoundscape.Instance?.SetVolume(value / 100f);
                UpdateAudioLabels();
            });
            rt.gameObject.AddComponent<UnderworldMenuFeedback>();
            return slider;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            bool pause = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame);
            bool back = pad != null && pad.buttonEast.wasPressedThisFrame;
            if (_reloading) return;
            if (pause || (_open && back))
            {
                if (!_open) OpenMenu();
                else if (_help.activeSelf) HideHelp();
                else if (_inRun) BeginOrResume();
            }
            if (!_open) return;
            if (_lastWidth != Screen.width || _lastHeight != Screen.height) FitPanorama();
            if (_events.currentSelectedGameObject == null) Select(_help.activeSelf ? _helpBack : _primary);
        }

        private void FitPanorama()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            if (_panorama.texture == null) return;
            float displayAspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            float imageAspect = _panorama.texture.width / (float)_panorama.texture.height;
            if (imageAspect > displayAspect)
            {
                float w = displayAspect / imageAspect;
                _panorama.uvRect = new UnityEngine.Rect((1f - w) * 0.5f, 0, w, 1);
            }
            else
            {
                float h = imageAspect / displayAspect;
                _panorama.uvRect = new UnityEngine.Rect(0, (1f - h) * 0.5f, 1, h);
            }
        }

        private void OpenMenu()
        {
            _open = true;
            _menuBlocksGameplay = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            _canvasObject.SetActive(true);
            _main.SetActive(true);
            _help.SetActive(false);
            _eyebrow.text = _inRun ? "THE SONG IS HELD" : "THE DESCENT";
            _primaryLabel.text = _inRun ? "Resume" : "Begin descent";
            _leaveLabel.text = _inRun ? "Return to title" : "Quit";
            _premise.text = _inRun ? "The underworld waits. Take a breath.\nYour journey resumes exactly where you left it." : "A lyre against the silence. A soul worth the descent.\nLead Eurydice out of Hades. Do not look back.";
            SyncAudioControls();
            Select(_primary);
        }

        private void BeginOrResume()
        {
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
            _inRun = true;
            _open = false;
            _menuBlocksGameplay = false;
            // EventSystem submit/pointer callbacks run after player Update in some configurations.
            // Keep the close frame and the next complete frame out of gameplay input.
            _blockThroughFrame = Time.frameCount + 1;
            _events.SetSelectedGameObject(null);
            _canvasObject.SetActive(false);
            // Hitstop and look-back own transient slow motion; never persist it across a pause.
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void ShowHelp()
        {
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
            _main.SetActive(false);
            _help.SetActive(true);
            Select(_helpBack);
        }

        private void HideHelp()
        {
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
            _help.SetActive(false);
            _main.SetActive(true);
            Select(_how);
        }

        private void ToggleMute()
        {
            var sound = UnderworldSoundscape.Instance;
            if (sound == null) return;
            sound.SetMuted(!sound.IsMuted);
            sound.PlayMenuConfirm();
            UpdateAudioLabels();
        }

        private void SyncAudioControls()
        {
            var sound = UnderworldSoundscape.Instance;
            _volume.SetValueWithoutNotify(sound == null ? 75f : sound.Volume * 100f);
            UpdateAudioLabels();
        }

        private void UpdateAudioLabels()
        {
            var sound = UnderworldSoundscape.Instance;
            bool muted = sound != null && sound.IsMuted;
            _muteLabel.text = muted ? "Audio  ·  Muted" : "Audio  ·  On";
            int percent = Mathf.RoundToInt((sound == null ? 0.75f : sound.Volume) * 100f);
            _volumeLabel.text = "VOLUME   ·   " + percent + "%";
        }

        private void Leave()
        {
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
            if (_inRun)
            {
                _reloading = true;
                OrpheusPrototypeBuilder.RestartDescent(false);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private void Select(Selectable control)
        {
            _events.SetSelectedGameObject(null);
            _events.SetSelectedGameObject(control.gameObject);
        }

        private static void LinkVertical(params Selectable[] controls)
        {
            for (int i = 0; i < controls.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = controls[(i + controls.Length - 1) % controls.Length];
                nav.selectOnDown = controls[(i + 1) % controls.Length];
                controls[i].navigation = nav;
            }
        }

        private Button MakeButton(RectTransform parent, string name, string text, float x, float y, float w, float h, UnityEngine.Events.UnityAction action, out Text label, bool primary = false)
        {
            var rt = Rect(name, parent, x, y, w, h);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = primary ? new Color(0.27f, 0.22f, 0.14f, 0.65f) : new Color(0.1f, 0.16f, 0.16f, 0.25f);
            colors.highlightedColor = new Color(0.24f, 0.35f, 0.32f, 0.9f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.43f, 0.34f, 0.2f, 0.95f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            label = Label(rt, text, 17, 0, w - 34, h, primary ? 25 : 23, Ivory);
            label.alignment = TextAnchor.MiddleLeft;
            var line = Rect("Edge", rt, 0, 0, 2, h).gameObject.AddComponent<Image>();
            line.color = primary ? Bronze : new Color(Bronze.r, Bronze.g, Bronze.b, 0.45f);
            line.raycastTarget = false;
            button.onClick.AddListener(action);
            rt.gameObject.AddComponent<UnderworldMenuFeedback>();
            return button;
        }

        private Text Label(RectTransform parent, string value, float x, float y, float w, float h, int size, Color color, bool serif = true)
        {
            var text = Rect("Text", parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = serif ? _serif : _sans;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        private static RectTransform Stretch(string name, RectTransform parent)
        {
            var rt = Rect(name, parent, 0, 0, 0, 0);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void Line(RectTransform parent, float x, float y, float width, Color color)
        {
            var image = Rect("Rule", parent, x, y, width, 1).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            _menuBlocksGameplay = false;
            _blockThroughFrame = -1;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (_shadeTexture != null) Destroy(_shadeTexture);
            if (_ownsSerif && _serif != null) Destroy(_serif);
            if (_ownsSans && _sans != null) Destroy(_sans);
        }
    }

    /// <summary>Shared hover/focus behavior keeps controller and pointer navigation equivalent.</summary>
    public sealed class UnderworldMenuFeedback : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public void OnSelect(BaseEventData eventData) { UnderworldSoundscape.Instance?.PlayMenuMove(); }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
