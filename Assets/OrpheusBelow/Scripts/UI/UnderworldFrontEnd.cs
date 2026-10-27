using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OrpheusBelow
{
    /// <summary>
    /// Classical Greek Front-End for Orpheus Below:
    ///   - Main Menu with 3 Hellenic-styled buttons: [ NEW GAME ], [ SETTINGS ], [ QUIT ].
    ///   - Comprehensive Greek Settings Submenu:
    ///       1. Live Microphone Calibration (VU Meter, Note Recognition, Mic Sensitivity / Gain slider, Soft/Boom thresholds, Input Mode).
    ///       2. Brightness / Gamma adjustment slider (persisted in PlayerPrefs and applied via fullscreen luminance veil).
    ///       3. Underworld Difficulty selector (Mortal, Heroic, Tartarus with live mechanical descriptions).
    ///       4. Master Volume slider and Audio Mute toggle.
    ///       5. Back button to return to the Main Menu or Pause Screen.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class UnderworldFrontEnd : MonoBehaviour
    {
        public static UnderworldFrontEnd Instance { get; private set; }
        public static bool SkipTitleOnce;
        public static bool BlocksGameplay => _menuBlocksGameplay || Time.frameCount <= _blockThroughFrame;

        private static bool _menuBlocksGameplay;
        private static int _blockThroughFrame = -1;

        // Classical Hellenic Palette
        private static readonly Color Ivory = new Color(0.95f, 0.92f, 0.82f);
        private static readonly Color Gold = new Color(0.82f, 0.70f, 0.46f);
        private static readonly Color Bronze = new Color(0.52f, 0.45f, 0.33f);
        private static readonly Color Muted = new Color(0.70f, 0.72f, 0.71f);
        private static readonly Color Crimson = new Color(0.98f, 0.32f, 0.25f);

        private Font _serif, _sans;
        private GameObject _canvasObject, _main, _settings;
        private RawImage _panorama;
        private Texture2D _shadeTexture;
        private EventSystem _events;
        private bool _open, _inRun, _reloading, _ownsSerif, _ownsSans;
        private int _lastWidth, _lastHeight;

        // Main Menu controls (The 3 Greek Buttons)
        private Text _eyebrow, _premise, _primaryLabel, _settingsLabel, _leaveLabel;
        private Button _primary, _settingsBtn, _leave;

        // Settings Submenu controls
        // 1. Microphone & Breath Calibration
        private RectTransform _vuFill;
        private Image _vuFillImg;
        private Text _micStatusText;
        private Slider _sensitivitySlider, _softThresholdSlider, _boomThresholdSlider;
        private Text _sensitivityLabel, _softThresholdLabel, _boomThresholdLabel;
        private Button _inputModeBtn;
        private Text _inputModeLabel;

        // 2. Brightness
        private Slider _brightnessSlider;
        private Text _brightnessLabel;
        private Image _brightnessVeil;
        private float _currentBrightness = 1.0f;

        // 3. Difficulty
        private Button _diffBtn;
        private Text _diffLabel, _diffDesc;

        // 4. Audio
        private Slider _volume;
        private Text _volumeLabel, _muteLabel;
        private Button _mute;

        // 5. Back Button
        private Button _settingsBack;

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

            _menuBlocksGameplay = !skip;
            _blockThroughFrame = Time.frameCount + 1;

            _serif = Font.CreateDynamicFontFromOSFont(new[] { "Palatino Linotype", "Georgia", "Times New Roman" }, 32);
            _sans = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 20);
            _ownsSerif = _serif != null;
            _ownsSans = _sans != null;
            if (_serif == null) _serif = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_sans == null) _sans = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _currentBrightness = PlayerPrefs.GetFloat("Orpheus_Brightness", 1.0f);
            DifficultySettings.Initialize();

            EnsureEventSystem();
            BuildCanvas();

            _inRun = skip;
            if (skip)
            {
                _canvasObject.SetActive(false);
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
            else
            {
                OpenMenu();
            }
        }

        private void Start()
        {
            if (!_inRun && !_open)
            {
                OpenMenu();
            }
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

            // A sibling canvas keeps exposure active when the menu canvas is hidden.
            var exposure = new GameObject("ExposureCanvas", typeof(RectTransform), typeof(Canvas));
            exposure.transform.SetParent(transform, false);
            exposure.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            exposure.GetComponent<Canvas>().sortingOrder = 1200;
            var veilGo = new GameObject("BrightnessVeil", typeof(RectTransform), typeof(Image));
            veilGo.transform.SetParent(exposure.transform, false);
            var veilRt = veilGo.GetComponent<RectTransform>();
            veilRt.anchorMin = Vector2.zero;
            veilRt.anchorMax = Vector2.one;
            veilRt.offsetMin = veilRt.offsetMax = Vector2.zero;
            _brightnessVeil = veilGo.GetComponent<Image>();
            _brightnessVeil.raycastTarget = false;
            ApplyBrightness(_currentBrightness);

            // Panorama Underworld art
            var background = Stretch("Panorama", screen);
            background.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.055f, 0.06f);
            var art = Stretch("PaintedUnderworld", screen);
            _panorama = art.gameObject.AddComponent<RawImage>();
            _panorama.texture = Resources.Load<Texture2D>("Art/AsphodelQuietVista")
                ?? Resources.Load<Texture2D>("Art/UnderworldPanorama");
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

            // --- 1. MAIN MENU (Title & 3 Greek Buttons) ---
            _main = Stretch("TitleAndNavigation", content).gameObject;
            RectTransform main = _main.GetComponent<RectTransform>();
            var titleSurface = Rect("PaintedTitleSurface", main, 52, 80, 478, 552).gameObject.AddComponent<Image>();
            UnderworldArtLibrary.StylePanel(titleSurface, .88f);
            _eyebrow = Label(main, "The descent", 92, 118, 360, 24, 14, Gold, false);
            Label(main, "Orpheus", 86, 159, 410, 86, 70, Ivory);
            Label(main, "Below", 92, 242, 360, 46, 36, Ivory);
            _premise = Label(main, "Lead Eurydice out of Hades.\nTrust her voice. Do not look back.", 94, 306, 360, 62, 18, Muted, false);

            // 3 GREEK BUTTONS: [ NEW GAME ], [ SETTINGS ], [ QUIT ]
            _primary = MakeGreekButton(main, "NewGameBtn", "Begin descent", 92, 400, 352, 52, BeginOrResume, out _primaryLabel, true);
            _settingsBtn = MakeGreekButton(main, "SettingsBtn", "Settings", 92, 468, 352, 48, ShowSettings, out _settingsLabel);
            _leave = MakeGreekButton(main, "LeaveBtn", "Quit", 92, 530, 352, 48, Leave, out _leaveLabel);

            Label(main, "Arrow keys / D-pad to navigate     Enter / A to select", 73, 669, 670, 23, 13, Muted, false);
            Label(main, "A song can cross what a blade cannot.", 760, 630, 445, 30, 18, Ivory).alignment = TextAnchor.MiddleRight;
            LinkVertical(_primary, _settingsBtn, _leave);

            // --- 2. GREEK SETTINGS SUBMENU ---
            BuildSettingsSubmenu(content);

            FitPanorama();
        }

        private void BuildSettingsSubmenu(RectTransform parent)
        {
            _settings = Stretch("SettingsSheet", parent).gameObject;
            var dim = _settings.AddComponent<Image>();
            UnderworldArtLibrary.StylePanel(dim);
            dim.raycastTarget = true;

            var body = _settings.GetComponent<RectTransform>();
            Label(body, "Tune your descent", 72, 38, 1000, 26, 14, Gold, false);
            Label(body, "Settings", 70, 72, 1100, 50, 38, Ivory);
            Line(body, 72, 128, 1136, Bronze);

            // === LEFT COLUMN (x = 72): MICROPHONE & BREATH CALIBRATION ===
            Label(body, "Microphone & breath", 72, 148, 520, 28, 20, Ivory);

            // Live VU Meter Bar
            Label(body, "Live input · test your voice or breath", 72, 183, 500, 20, 13, Muted, false);
            var vuTrack = Rect("VuMeterTrack", body, 72, 206, 500, 22);
            vuTrack.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.18f, 0.90f);

            _vuFill = Rect("VuFill", vuTrack, 0, 0, 0, 22);
            _vuFill.anchorMin = _vuFill.anchorMax = _vuFill.pivot = new Vector2(0f, 1f);
            _vuFillImg = _vuFill.gameObject.AddComponent<Image>();
            _vuFillImg.color = Gold;

            // Live Recognition Pill
            _micStatusText = Label(body, "Listening", 72, 236, 500, 24, 14, Muted, false);
            _micStatusText.alignment = TextAnchor.MiddleLeft;

            // Sensitivity / Gain Slider (1.0x to 8.0x)
            _sensitivityLabel = Label(body, "Sensitivity   2.5×", 72, 274, 500, 22, 14, Ivory, false);
            _sensitivitySlider = MakeGenericSlider(body, 72, 298, 500, 1.0f, 8.0f, false, val =>
            {
                SensorPulseInput sensor = SensorPulseInput.Instance;
                if (sensor != null)
                {
                    sensor.micSensitivity = val;
                    sensor.SaveCalibration();
                }
                _sensitivityLabel.text = "Sensitivity   " + val.ToString("F1") + "×";
            });

            // Soft Threshold Slider (0.05 to 0.40)
            _softThresholdLabel = Label(body, "Soft note threshold   14%", 72, 338, 500, 22, 14, Ivory, false);
            _softThresholdSlider = MakeGenericSlider(body, 72, 362, 500, 0.05f, 0.40f, false, val =>
            {
                SensorPulseInput sensor = SensorPulseInput.Instance;
                if (sensor != null)
                {
                    sensor.softMinThreshold = val;
                    sensor.SaveCalibration();
                }
                _softThresholdLabel.text = "Soft note threshold   " + Mathf.RoundToInt(val * 100f) + "%";
            });

            // Hard Boom Threshold Slider (0.45 to 0.85)
            _boomThresholdLabel = Label(body, "Hard boom threshold   60%", 72, 402, 500, 22, 14, Ivory, false);
            _boomThresholdSlider = MakeGenericSlider(body, 72, 426, 500, 0.45f, 0.85f, false, val =>
            {
                SensorPulseInput sensor = SensorPulseInput.Instance;
                if (sensor != null)
                {
                    sensor.hardBoomThreshold = val;
                    sensor.SaveCalibration();
                }
                _boomThresholdLabel.text = "Hard boom threshold   " + Mathf.RoundToInt(val * 100f) + "%";
            });

            // Input Mode Toggle Button
            _inputModeBtn = MakeGreekButton(body, "InputModeBtn", "Input: Keyboard & Mouse", 72, 468, 500, 42, CycleInputMode, out _inputModeLabel);

            // === RIGHT COLUMN (x = 660): BRIGHTNESS, DIFFICULTY, AUDIO ===
            // 2. Brightness
            Label(body, "Display", 660, 148, 500, 28, 20, Ivory);
            _brightnessLabel = Label(body, "Brightness   100%", 660, 182, 500, 22, 14, Ivory, false);
            _brightnessSlider = MakeGenericSlider(body, 660, 206, 500, 0.5f, 1.5f, false, val =>
            {
                ApplyBrightness(val);
                _brightnessLabel.text = "Brightness   " + Mathf.RoundToInt(val * 100f) + "%";
            });

            // 3. Difficulty Setting
            Label(body, "Difficulty", 660, 252, 500, 28, 20, Ivory);
            _diffBtn = MakeGreekButton(body, "DiffBtn", "Heroic", 660, 290, 500, 44, CycleDifficulty, out _diffLabel);
            _diffDesc = Label(body, DifficultySettings.GetDescription(DifficultySettings.CurrentDifficulty), 660, 342, 500, 46, 14, Muted, false);

            // 4. Audio Controls
            Label(body, "Audio", 660, 396, 500, 28, 20, Ivory);
            _volumeLabel = Label(body, "Master volume   75%", 660, 430, 330, 22, 14, Ivory, false);
            _volume = MakeGenericSlider(body, 660, 452, 330, 0f, 100f, true, val =>
            {
                UnderworldSoundscape.Instance?.SetVolume(val / 100f);
                UpdateAudioLabels();
            });
            _mute = MakeGreekButton(body, "MuteBtn", "Audio: On", 1010, 442, 150, 40, ToggleMute, out _muteLabel);

            // Bottom Divider & Return Button
            Line(body, 72, 560, 1136, Bronze);
            _settingsBack = MakeGreekButton(body, "SettingsBack", "Back to menu", 72, 586, 260, 48, HideSettings, out _, true);
            Label(body, "Esc to go back", 960, 598, 200, 24, 13, Muted, false).alignment = TextAnchor.MiddleRight;
            LinkVertical(_sensitivitySlider, _softThresholdSlider, _boomThresholdSlider, _inputModeBtn,
                _brightnessSlider, _diffBtn, _volume, _mute, _settingsBack);

            _settings.SetActive(false);
        }

        private void CycleInputMode()
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor == null) return;

            int next = ((int)sensor.sourceMode + 1) % 3;
            sensor.SetSourceMode((SensorSourceMode)next);
            UpdateInputModeLabel();
        }

        private void UpdateInputModeLabel()
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor == null || _inputModeLabel == null) return;

            string text = sensor.sourceMode switch
            {
                SensorSourceMode.KeyboardAndMouseOnly => "Input: Keyboard & Mouse Only",
                SensorSourceMode.KeyboardPlusPCMicrophone => "Input: Keyboard + PC Microphone",
                SensorSourceMode.KeyboardPlusESP32Serial => "Input: Keyboard + ESP32 Serial",
                _ => "Input: Keyboard & Mouse"
            };
            _inputModeLabel.text = text;
        }

        private void CycleDifficulty()
        {
            DifficultySettings.CycleNext();
            UpdateDifficultyLabels();
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
        }

        private void UpdateDifficultyLabels()
        {
            if (_diffLabel != null)
                _diffLabel.text = DifficultySettings.GetName(DifficultySettings.CurrentDifficulty);
            if (_diffDesc != null)
                _diffDesc.text = DifficultySettings.GetDescription(DifficultySettings.CurrentDifficulty);
        }

        private void ApplyBrightness(float val)
        {
            _currentBrightness = Mathf.Clamp(val, 0.5f, 1.5f);
            PlayerPrefs.SetFloat("Orpheus_Brightness", _currentBrightness);
            PlayerPrefs.Save();

            if (_brightnessVeil != null)
            {
                if (_currentBrightness < 1.0f)
                {
                    // Darken
                    float darkness = (1.0f - _currentBrightness) * 0.72f;
                    _brightnessVeil.color = new Color(0f, 0f, 0f, darkness);
                }
                else
                {
                    // Lighten with warm amber exposure
                    float brilliance = (_currentBrightness - 1.0f) * 0.38f;
                    _brightnessVeil.color = new Color(1.0f, 0.95f, 0.85f, brilliance);
                }
            }
        }

        private Slider MakeGenericSlider(RectTransform parent, float x, float y, float width, float min, float max, bool whole, UnityEngine.Events.UnityAction<float> onVal)
        {
            var rt = Rect("Slider_" + x + "_" + y, parent, x, y, width, 24f);
            rt.gameObject.AddComponent<Image>().color = Color.clear;
            var slider = rt.gameObject.AddComponent<Slider>();

            var track = Rect("Track", rt, 0, 10, width, 4).gameObject.AddComponent<Image>();
            track.color = new Color(0.20f, 0.24f, 0.25f, 0.95f);

            var fillArea = Stretch("FillArea", rt);
            fillArea.offsetMin = new Vector2(0f, 10f);
            fillArea.offsetMax = new Vector2(0f, -10f);
            var fill = Stretch("Fill", fillArea).gameObject.AddComponent<Image>();
            fill.color = Gold;
            slider.fillRect = fill.rectTransform;

            var handleArea = Stretch("HandleArea", rt);
            handleArea.offsetMin = new Vector2(6, 0);
            handleArea.offsetMax = new Vector2(-6, 0);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            handle.transform.SetParent(handleArea, false);
            handle.color = Ivory;
            handle.rectTransform.sizeDelta = new Vector2(8, 18);

            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;
            slider.direction = Slider.Direction.LeftToRight;

            var sliderColors = slider.colors;
            sliderColors.highlightedColor = Gold;
            sliderColors.selectedColor = Gold;
            slider.colors = sliderColors;

            slider.onValueChanged.AddListener(onVal);
            rt.gameObject.AddComponent<UnderworldMenuFeedback>();
            return slider;
        }

        private int _lastToggleFrame = -1;
        private float _lastToggleRealtime = -1f;

        public void TogglePause()
        {
            if (_reloading) return;
            if (Time.frameCount == _lastToggleFrame) return;
            if (Time.unscaledTime - _lastToggleRealtime < 0.15f) return;
            _lastToggleFrame = Time.frameCount;
            _lastToggleRealtime = Time.unscaledTime;

            if (!_open)
            {
                OpenMenu();
            }
            else if (_settings != null && _settings.activeSelf)
            {
                HideSettings();
            }
            else if (_inRun)
            {
                BeginOrResume();
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            bool pause = (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
                      || (pad != null && pad.startButton.wasPressedThisFrame);
            bool back = pad != null && pad.buttonEast.wasPressedThisFrame;

            if (_reloading) return;

            // Calibration is accessed purely via Settings submenu now!
            if (pause || (_open && back))
            {
                TogglePause();
                return;
            }

            if (!_open) return;

            if (_settings.activeSelf)
            {
                UpdateLiveCalibrationTelemetry();
            }

            if (_lastWidth != Screen.width || _lastHeight != Screen.height) FitPanorama();
            if (_events != null && _events.currentSelectedGameObject == null)
            {
                Select(_settings.activeSelf ? _settingsBack : _primary);
            }
        }

        private void UpdateLiveCalibrationTelemetry()
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor == null) return;

            float live = Mathf.Clamp01(sensor.CurrentLiveLevel);
            if (_vuFill != null)
            {
                _vuFill.sizeDelta = new Vector2(500f * live, 22f);
                if (live >= sensor.hardBoomThreshold)
                    _vuFillImg.color = Crimson;
                else if (live >= sensor.softMinThreshold)
                    _vuFillImg.color = Gold;
                else
                    _vuFillImg.color = new Color(0.40f, 0.45f, 0.50f, 0.65f);
            }

            if (_micStatusText != null)
            {
                if (live >= sensor.hardBoomThreshold)
                {
                    _micStatusText.text = "Hard boom (B)   " + Mathf.RoundToInt(live * 100f) + "%";
                    _micStatusText.color = Crimson;
                }
                else if (live >= sensor.softMinThreshold)
                {
                    _micStatusText.text = "Soft note (S)   " + Mathf.RoundToInt(live * 100f) + "%";
                    _micStatusText.color = Gold;
                }
                else
                {
                    _micStatusText.text = "Listening   " + Mathf.RoundToInt(live * 100f) + "%";
                    _micStatusText.color = Muted;
                }
            }
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
            _settings.SetActive(false);

            _eyebrow.text = _inRun ? "The song is held" : "The descent";
            _primaryLabel.text = _inRun ? "Resume journey" : "Begin descent";
            _leaveLabel.text = _inRun ? "Return to title" : "Quit";
            _premise.text = _inRun
                ? "The underworld waits. Take a breath.\nYour journey resumes exactly where you left it."
                : "Lead Eurydice out of Hades.\nTrust her voice. Do not look back.";

            SyncAudioControls();
            Select(_primary);
        }

        private void BeginOrResume()
        {
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
            _inRun = true;
            _open = false;
            _menuBlocksGameplay = false;
            _blockThroughFrame = Time.frameCount + 1;
            _events.SetSelectedGameObject(null);
            _canvasObject.SetActive(false);
            Time.timeScale = OrpheusHud.IsTutorialModalActive ? 0f : 1f;
            AudioListener.pause = false;
        }

        private void ShowSettings()
        {
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
            _main.SetActive(false);
            _settings.SetActive(true);

            // Sync Settings Controls
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor != null)
            {
                _sensitivitySlider?.SetValueWithoutNotify(sensor.micSensitivity);
                if (_sensitivityLabel != null) _sensitivityLabel.text = "Sensitivity   " + sensor.micSensitivity.ToString("F1") + "×";

                _softThresholdSlider?.SetValueWithoutNotify(sensor.softMinThreshold);
                if (_softThresholdLabel != null) _softThresholdLabel.text = "Soft note threshold   " + Mathf.RoundToInt(sensor.softMinThreshold * 100f) + "%";

                _boomThresholdSlider?.SetValueWithoutNotify(sensor.hardBoomThreshold);
                if (_boomThresholdLabel != null) _boomThresholdLabel.text = "Hard boom threshold   " + Mathf.RoundToInt(sensor.hardBoomThreshold * 100f) + "%";

                UpdateInputModeLabel();
            }

            _brightnessSlider?.SetValueWithoutNotify(_currentBrightness);
            if (_brightnessLabel != null) _brightnessLabel.text = "Brightness   " + Mathf.RoundToInt(_currentBrightness * 100f) + "%";

            UpdateDifficultyLabels();
            SyncAudioControls();
            Select(_settingsBack);
        }

        private void HideSettings()
        {
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
            _settings.SetActive(false);
            _main.SetActive(true);
            Select(_settingsBtn);
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
            _volume?.SetValueWithoutNotify(sound == null ? 75f : sound.Volume * 100f);
            UpdateAudioLabels();
        }

        private void UpdateAudioLabels()
        {
            var sound = UnderworldSoundscape.Instance;
            bool muted = sound != null && sound.IsMuted;
            if (_muteLabel != null) _muteLabel.text = muted ? "Audio: Muted" : "Audio: On";
            int percent = Mathf.RoundToInt((sound == null ? 0.75f : sound.Volume) * 100f);
            if (_volumeLabel != null) _volumeLabel.text = "Master volume   " + percent + "%";
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
            if (control == null) return;
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

        private Button MakeGreekButton(RectTransform parent, string name, string text, float x, float y, float w, float h, UnityEngine.Events.UnityAction action, out Text label, bool primary = false)
        {
            var rt = Rect(name, parent, x, y, w, h);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = Color.white;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = primary ? new Color(.24f, .22f, .18f, .94f) : new Color(.12f, .15f, .17f, .9f);
            colors.highlightedColor = new Color(.32f, .30f, .24f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.39f, .34f, .25f, 1f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            // One quiet baseline and a warm focus fill give mouse and controller equal feedback.
            Line(rt, 0, h - 1, w, primary ? Gold : new Color(Bronze.r, Bronze.g, Bronze.b, .45f));

            label = Label(rt, text, 18, 0, w - 36, h, primary ? 22 : 19, Ivory);
            label.alignment = TextAnchor.MiddleLeft;

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
