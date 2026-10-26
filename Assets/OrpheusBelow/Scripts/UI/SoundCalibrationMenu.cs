using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OrpheusBelow
{
    /// <summary>
    /// Live Sound & Acoustic Calibration Menu:
    ///   - Real-time animated VU meter showing live breath/voice/mic intensity [0..100%].
    ///   - Live Soft Note (S) vs Hard Boom (B) detection feedback pill.
    ///   - Sliders and buttons for Mic Sensitivity, Soft Threshold, and Hard Boom Threshold.
    ///   - Microphone device selector cycling through connected inputs.
    ///   - Master Volume slider and audio previews.
    ///   - Preserves calibration in PlayerPrefs across game sessions.
    ///   - Press [C] anytime during gameplay or pause to toggle!
    /// </summary>
    public class SoundCalibrationMenu : MonoBehaviour
    {
        public static SoundCalibrationMenu Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        private static readonly Color Ivory = new Color(0.95f, 0.92f, 0.82f);
        private static readonly Color Gold = new Color(0.96f, 0.82f, 0.35f);
        private static readonly Color Crimson = new Color(0.98f, 0.32f, 0.25f);
        private static readonly Color Bronze = new Color(0.72f, 0.54f, 0.31f);
        private static readonly Color DimText = new Color(0.70f, 0.70f, 0.70f);
        private static readonly Color CardBg = new Color(0.04f, 0.03f, 0.06f, 0.96f);

        private Font _serifFont;
        private Font _sansFont;

        private GameObject _canvasRoot;
        private GameObject _panelRoot;

        // Mode buttons
        private Button _btnKbd;
        private Button _btnMic;
        private Button _btnEsp;
        private Text _lblKbd;
        private Text _lblMic;
        private Text _lblEsp;

        // Live VU Meter & Recognition
        private RectTransform _vuFill;
        private Image _vuFillImg;
        private RectTransform _softMarker;
        private RectTransform _boomMarker;
        private Text _detectionStatusText;
        private Text _liveLevelText;

        // Sliders & value labels
        private Text _micDeviceLabel;
        private Text _sensitivityValueLabel;
        private Text _softThresholdValueLabel;
        private Text _boomThresholdValueLabel;
        private Text _volumeValueLabel;
        private Text _muteButtonLabel;

        private const float VuMeterWidth = 500f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _serifFont = Font.CreateDynamicFontFromOSFont(new[] { "Palatino Linotype", "Georgia", "Times New Roman" }, 20);
            _sansFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 16);
            if (_serifFont == null) _serifFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_sansFont == null) _sansFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildUI();
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            bool toggle = pad != null && pad.selectButton.wasPressedThisFrame;

            if (toggle)
            {
                ToggleOpen();
                return;
            }

            if (IsOpen && kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
                return;
            }

            if (IsOpen)
            {
                UpdateLiveVuMeter();
            }
        }

        public void ToggleOpen()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (_canvasRoot != null) _canvasRoot.SetActive(open);

            if (open)
            {
                // Ensure audio monitoring is active while paused
                SensorPulseInput sensor = SensorPulseInput.Instance;
                if (sensor != null && sensor.sourceMode == SensorSourceMode.KeyboardPlusPCMicrophone)
                {
                    // Microphone runs live
                }
                SyncAllLabels();
            }
        }

        private void UpdateLiveVuMeter()
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor == null) return;

            float live = Mathf.Clamp01(sensor.CurrentLiveLevel);
            if (_vuFill != null)
            {
                _vuFill.sizeDelta = new Vector2(VuMeterWidth * live, 22f);
                if (live >= sensor.hardBoomThreshold)
                    _vuFillImg.color = Crimson;
                else if (live >= sensor.softMinThreshold)
                    _vuFillImg.color = Gold;
                else
                    _vuFillImg.color = new Color(0.40f, 0.45f, 0.50f, 0.65f);
            }

            if (_liveLevelText != null)
            {
                _liveLevelText.text = $"INPUT LEVEL: {Mathf.RoundToInt(live * 100f)}%";
            }

            if (_detectionStatusText != null)
            {
                if (live >= sensor.hardBoomThreshold)
                {
                    _detectionStatusText.text = "● TRIGGERING: HARD ORPHIC BOOM (B)!";
                    _detectionStatusText.color = Crimson;
                }
                else if (live >= sensor.softMinThreshold)
                {
                    _detectionStatusText.text = "● TRIGGERING: SOFT LYRE PLUCK (S)";
                    _detectionStatusText.color = Gold;
                }
                else
                {
                    _detectionStatusText.text = "● SILENCE / BACKGROUND NOISE (No Note)";
                    _detectionStatusText.color = DimText;
                }
            }

            // Update marker positions
            if (_softMarker != null)
            {
                _softMarker.anchoredPosition = new Vector2(VuMeterWidth * sensor.softMinThreshold, 0f);
            }
            if (_boomMarker != null)
            {
                _boomMarker.anchoredPosition = new Vector2(VuMeterWidth * sensor.hardBoomThreshold, 0f);
            }
        }

        private void SyncAllLabels()
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor != null)
            {
                HighlightSourceButton(sensor.sourceMode);
                if (_sensitivityValueLabel != null)
                    _sensitivityValueLabel.text = $"{sensor.micSensitivity:F1}x";
                if (_softThresholdValueLabel != null)
                    _softThresholdValueLabel.text = $"{sensor.softMinThreshold:F2}  ({Mathf.RoundToInt(sensor.softMinThreshold * 100f)}%)";
                if (_boomThresholdValueLabel != null)
                    _boomThresholdValueLabel.text = $"{sensor.hardBoomThreshold:F2}  ({Mathf.RoundToInt(sensor.hardBoomThreshold * 100f)}%)";

                if (_micDeviceLabel != null)
                {
                    string dev = sensor.CurrentMicDevice;
                    _micDeviceLabel.text = string.IsNullOrEmpty(dev) ? "Default Recording Device" : dev;
                }
            }

            UnderworldSoundscape sound = UnderworldSoundscape.Instance;
            if (sound != null)
            {
                if (_volumeValueLabel != null)
                    _volumeValueLabel.text = $"{Mathf.RoundToInt(sound.Volume * 100f)}%";
                if (_muteButtonLabel != null)
                    _muteButtonLabel.text = sound.IsMuted ? "Audio: Muted" : "Audio: On";
            }
        }

        private void HighlightSourceButton(SensorSourceMode mode)
        {
            if (_lblKbd != null) _lblKbd.color = (mode == SensorSourceMode.KeyboardAndMouseOnly) ? Gold : Ivory;
            if (_lblMic != null) _lblMic.color = (mode == SensorSourceMode.KeyboardPlusPCMicrophone) ? Gold : Ivory;
            if (_lblEsp != null) _lblEsp.color = (mode == SensorSourceMode.KeyboardPlusESP32Serial) ? Gold : Ivory;
        }

        // =========================================================
        // UI CONSTRUCTION (Programmatic Clean UGUI)
        // =========================================================
        private void BuildUI()
        {
            _canvasRoot = new GameObject("SoundCalibrationMenu_Canvas");
            _canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            CanvasScaler scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRoot.AddComponent<GraphicRaycaster>();

            // Dark semi-transparent full-screen backdrop
            GameObject backdrop = CreateRect("Backdrop", _canvasRoot.transform, 0, 0, 1920, 1080);
            Image bgImg = backdrop.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.65f);

            // Center Calibration Card (680 x 660)
            const float cardW = 700f;
            const float cardH = 680f;
            _panelRoot = CreateRect("CalibrationCard", _canvasRoot.transform, (1920 - cardW) * 0.5f, (1080 - cardH) * 0.5f, cardW, cardH);
            Image cardImg = _panelRoot.AddComponent<Image>();
            cardImg.color = CardBg;

            // Bronze border lines
            CreateLine(_panelRoot.transform, 0, 0, cardW, 2, Bronze);
            CreateLine(_panelRoot.transform, 0, cardH - 2, cardW, 2, Bronze);
            CreateLine(_panelRoot.transform, 0, 0, 2, cardH, Bronze);
            CreateLine(_panelRoot.transform, cardW - 2, 0, 2, cardH, Bronze);

            float curY = 22f;

            // --- HEADER ---
            Text title = CreateLabel(_panelRoot.transform, "ACOUSTIC & SOUND CALIBRATION", 26, curY, cardW - 52, 34, 24, Ivory, true);
            title.alignment = TextAnchor.MiddleCenter;
            curY += 34f;

            Text sub = CreateLabel(_panelRoot.transform, "Calibrate Microphone, Breath Intensity, and Underworld Soundscape", 26, curY, cardW - 52, 22, 14, Gold, false);
            sub.alignment = TextAnchor.MiddleCenter;
            curY += 28f;

            CreateLine(_panelRoot.transform, 30, curY, cardW - 60, 1, new Color(Bronze.r, Bronze.g, Bronze.b, 0.45f));
            curY += 16f;

            // --- 1. INPUT SOURCE SELECTION ---
            Text sec1 = CreateLabel(_panelRoot.transform, "1. INPUT SOURCE MODE", 34, curY, cardW - 68, 22, 15, Gold, true);
            curY += 24f;

            float btnW = 200f;
            float btnH = 36f;
            _btnKbd = CreateButton(_panelRoot.transform, "BtnKbd", "Keys & Mouse", 34, curY, btnW, btnH, () => {
                SensorPulseInput.Instance?.SetSourceMode(SensorSourceMode.KeyboardAndMouseOnly);
                SyncAllLabels();
            }, out _lblKbd);

            _btnMic = CreateButton(_panelRoot.transform, "BtnMic", "PC Microphone / Breath", 34 + btnW + 15, curY, btnW, btnH, () => {
                SensorPulseInput.Instance?.SetSourceMode(SensorSourceMode.KeyboardPlusPCMicrophone);
                SyncAllLabels();
            }, out _lblMic);

            _btnEsp = CreateButton(_panelRoot.transform, "BtnEsp", "ESP32 Sensor", 34 + (btnW + 15) * 2, curY, btnW, btnH, () => {
                SensorPulseInput.Instance?.SetSourceMode(SensorSourceMode.KeyboardPlusESP32Serial);
                SyncAllLabels();
            }, out _lblEsp);
            curY += 46f;

            // --- 2. LIVE VU METER ---
            Text sec2 = CreateLabel(_panelRoot.transform, "2. LIVE ACOUSTIC METER & NOTE RECOGNITION", 34, curY, 360, 22, 15, Gold, true);
            _liveLevelText = CreateLabel(_panelRoot.transform, "INPUT LEVEL: 0%", cardW - 220, curY, 185, 22, 14, Ivory, false);
            _liveLevelText.alignment = TextAnchor.MiddleRight;
            curY += 26f;

            // VU Meter Track (500 x 24)
            float vuX = (cardW - VuMeterWidth) * 0.5f;
            GameObject vuTrack = CreateRect("VuTrack", _panelRoot.transform, vuX, curY, VuMeterWidth, 24);
            Image trackImg = vuTrack.AddComponent<Image>();
            trackImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

            // Live Level Fill
            GameObject vuFillObj = CreateRect("VuFill", vuTrack.transform, 0, 0, 0, 24);
            _vuFill = vuFillObj.GetComponent<RectTransform>();
            _vuFillImg = vuFillObj.AddComponent<Image>();
            _vuFillImg.color = Gold;

            // Threshold Marker Lines
            GameObject softMarkerObj = CreateRect("SoftMarker", vuTrack.transform, 0, -2, 2, 28);
            _softMarker = softMarkerObj.GetComponent<RectTransform>();
            Image softMarkImg = softMarkerObj.AddComponent<Image>();
            softMarkImg.color = Gold;

            GameObject boomMarkerObj = CreateRect("BoomMarker", vuTrack.transform, 0, -2, 2, 28);
            _boomMarker = boomMarkerObj.GetComponent<RectTransform>();
            Image boomMarkImg = boomMarkerObj.AddComponent<Image>();
            boomMarkImg.color = Crimson;

            curY += 30f;

            // Legend / Detection status readout
            _detectionStatusText = CreateLabel(_panelRoot.transform, "● SILENCE / BACKGROUND NOISE", 34, curY, cardW - 68, 22, 14, DimText, false);
            _detectionStatusText.alignment = TextAnchor.MiddleCenter;
            curY += 28f;

            CreateLine(_panelRoot.transform, 30, curY, cardW - 60, 1, new Color(Bronze.r, Bronze.g, Bronze.b, 0.35f));
            curY += 14f;

            // --- 3. THRESHOLD TUNING CONTROLS ---
            Text sec3 = CreateLabel(_panelRoot.transform, "3. THRESHOLD & SENSITIVITY CALIBRATION", 34, curY, cardW - 68, 22, 15, Gold, true);
            curY += 24f;

            // Row A: Mic Sensitivity / Gain
            CreateLabel(_panelRoot.transform, "Mic Sensitivity (Gain):", 34, curY, 240, 28, 15, Ivory, false);
            CreateButton(_panelRoot.transform, "BtnSensDown", "-", 280, curY, 36, 28, () => AdjustSensitivity(-0.2f), out _);
            _sensitivityValueLabel = CreateLabel(_panelRoot.transform, "3.2x", 324, curY, 70, 28, 15, Gold, false);
            _sensitivityValueLabel.alignment = TextAnchor.MiddleCenter;
            CreateButton(_panelRoot.transform, "BtnSensUp", "+", 402, curY, 36, 28, () => AdjustSensitivity(+0.2f), out _);
            curY += 34f;

            // Row B: Soft Note Threshold
            CreateLabel(_panelRoot.transform, "Soft Note Threshold (Whisper/Puff):", 34, curY, 240, 28, 15, Ivory, false);
            CreateButton(_panelRoot.transform, "BtnSoftDown", "-", 280, curY, 36, 28, () => AdjustSoftThreshold(-0.02f), out _);
            _softThresholdValueLabel = CreateLabel(_panelRoot.transform, "0.14 (14%)", 324, curY, 70, 28, 15, Gold, false);
            _softThresholdValueLabel.alignment = TextAnchor.MiddleCenter;
            CreateButton(_panelRoot.transform, "BtnSoftUp", "+", 402, curY, 36, 28, () => AdjustSoftThreshold(+0.02f), out _);
            curY += 34f;

            // Row C: Hard Boom Threshold
            CreateLabel(_panelRoot.transform, "Hard Boom Threshold (Loud/Exhale):", 34, curY, 240, 28, 15, Ivory, false);
            CreateButton(_panelRoot.transform, "BtnBoomDown", "-", 280, curY, 36, 28, () => AdjustBoomThreshold(-0.02f), out _);
            _boomThresholdValueLabel = CreateLabel(_panelRoot.transform, "0.60 (60%)", 324, curY, 70, 28, 15, Crimson, false);
            _boomThresholdValueLabel.alignment = TextAnchor.MiddleCenter;
            CreateButton(_panelRoot.transform, "BtnBoomUp", "+", 402, curY, 36, 28, () => AdjustBoomThreshold(+0.02f), out _);
            curY += 34f;

            // Row D: Active Microphone Device
            CreateLabel(_panelRoot.transform, "Microphone Device:", 34, curY, 180, 28, 15, Ivory, false);
            CreateButton(_panelRoot.transform, "BtnCycleMic", "Change Device", 220, curY, 120, 28, CycleMicDevice, out _);
            _micDeviceLabel = CreateLabel(_panelRoot.transform, "Default", 355, curY, 310, 28, 13, DimText, false);
            _micDeviceLabel.alignment = TextAnchor.MiddleLeft;
            curY += 40f;

            CreateLine(_panelRoot.transform, 30, curY, cardW - 60, 1, new Color(Bronze.r, Bronze.g, Bronze.b, 0.35f));
            curY += 14f;

            // --- 4. MASTER AUDIO MIX ---
            Text sec4 = CreateLabel(_panelRoot.transform, "4. UNDERWORLD AUDIO MIX", 34, curY, cardW - 68, 22, 15, Gold, true);
            curY += 24f;

            CreateLabel(_panelRoot.transform, "Master Volume:", 34, curY, 140, 28, 15, Ivory, false);
            CreateButton(_panelRoot.transform, "BtnVolDown", "-", 180, curY, 36, 28, () => AdjustVolume(-0.05f), out _);
            _volumeValueLabel = CreateLabel(_panelRoot.transform, "75%", 224, curY, 60, 28, 15, Gold, false);
            _volumeValueLabel.alignment = TextAnchor.MiddleCenter;
            CreateButton(_panelRoot.transform, "BtnVolUp", "+", 292, curY, 36, 28, () => AdjustVolume(+0.05f), out _);

            CreateButton(_panelRoot.transform, "BtnMute", "Audio: On", 348, curY, 110, 28, ToggleMute, out _muteButtonLabel);
            curY += 40f;

            // --- 5. PREVIEW & FOOTER ---
            CreateButton(_panelRoot.transform, "BtnTestSoft", "♫ Test Soft Pluck", 34, curY, 150, 32, PlayTestSoft, out _);
            CreateButton(_panelRoot.transform, "BtnTestBoom", "💥 Test Boom", 194, curY, 140, 32, PlayTestBoom, out _);
            CreateButton(_panelRoot.transform, "BtnReset", "Reset Defaults", 344, curY, 130, 32, ResetDefaults, out _);
            CreateButton(_panelRoot.transform, "BtnClose", "Close [C]", cardW - 170, curY, 136, 32, () => SetOpen(false), out _, primary: true);
        }

        // =========================================================
        // CALIBRATION ADJUSTMENT HANDLERS
        // =========================================================
        private void AdjustSensitivity(float delta)
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor == null) return;
            sensor.micSensitivity = Mathf.Clamp(sensor.micSensitivity + delta, 1.0f, 8.0f);
            sensor.SaveCalibration();
            SyncAllLabels();
            UnderworldSoundscape.Instance?.PlayMenuMove();
        }

        private void AdjustSoftThreshold(float delta)
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor == null) return;
            sensor.softMinThreshold = Mathf.Clamp(sensor.softMinThreshold + delta, 0.05f, 0.40f);
            if (sensor.softMinThreshold >= sensor.hardBoomThreshold - 0.08f)
            {
                sensor.softMinThreshold = sensor.hardBoomThreshold - 0.08f;
            }
            sensor.SaveCalibration();
            SyncAllLabels();
            UnderworldSoundscape.Instance?.PlayMenuMove();
        }

        private void AdjustBoomThreshold(float delta)
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor == null) return;
            sensor.hardBoomThreshold = Mathf.Clamp(sensor.hardBoomThreshold + delta, 0.45f, 0.85f);
            if (sensor.hardBoomThreshold <= sensor.softMinThreshold + 0.08f)
            {
                sensor.hardBoomThreshold = sensor.softMinThreshold + 0.08f;
            }
            sensor.SaveCalibration();
            SyncAllLabels();
            UnderworldSoundscape.Instance?.PlayMenuMove();
        }

        private void CycleMicDevice()
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor == null) return;
            sensor.CycleMicDevice();
            SyncAllLabels();
            UnderworldSoundscape.Instance?.PlayMenuMove();
        }

        private void AdjustVolume(float delta)
        {
            UnderworldSoundscape sound = UnderworldSoundscape.Instance;
            if (sound == null) return;
            sound.SetVolume(sound.Volume + delta);
            SyncAllLabels();
            UnderworldSoundscape.Instance?.PlayMenuMove();
        }

        private void ToggleMute()
        {
            UnderworldSoundscape sound = UnderworldSoundscape.Instance;
            if (sound == null) return;
            sound.SetMuted(!sound.IsMuted);
            SyncAllLabels();
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
        }

        private void PlayTestSoft()
        {
            ProceduralLyreAudio.Instance?.PlayFluteNote(false);
        }

        private void PlayTestBoom()
        {
            ProceduralLyreAudio.Instance?.PlayHardBoom(1.0f);
        }

        private void ResetDefaults()
        {
            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor != null)
            {
                sensor.micSensitivity = 3.2f;
                sensor.softMinThreshold = 0.14f;
                sensor.hardBoomThreshold = 0.60f;
                sensor.SaveCalibration();
            }

            UnderworldSoundscape sound = UnderworldSoundscape.Instance;
            if (sound != null)
            {
                sound.SetVolume(0.75f);
                sound.SetMuted(false);
            }

            SyncAllLabels();
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
        }

        // =========================================================
        // UI HELPERS
        // =========================================================
        private GameObject CreateRect(string name, Transform parent, float x, float y, float w, float h)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return go;
        }

        private Text CreateLabel(Transform parent, string text, float x, float y, float w, float h, int size, Color col, bool serif)
        {
            GameObject go = CreateRect("Label_" + text, parent, x, y, w, h);
            Text t = go.AddComponent<Text>();
            t.font = serif ? _serifFont : _sansFont;
            t.text = text;
            t.fontSize = size;
            t.color = col;
            t.alignment = TextAnchor.MiddleLeft;
            t.raycastTarget = false;
            return t;
        }

        private Button CreateButton(Transform parent, string name, string label, float x, float y, float w, float h, UnityEngine.Events.UnityAction action, out Text labelText, bool primary = false)
        {
            GameObject go = CreateRect(name, parent, x, y, w, h);
            Image img = go.AddComponent<Image>();
            img.color = primary ? new Color(0.28f, 0.22f, 0.14f, 0.85f) : new Color(0.12f, 0.15f, 0.18f, 0.65f);

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = img.color;
            cb.highlightedColor = new Color(0.26f, 0.36f, 0.34f, 0.95f);
            cb.pressedColor = new Color(0.48f, 0.38f, 0.22f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(action);

            labelText = CreateLabel(go.transform, label, 0, 0, w, h, 14, primary ? Gold : Ivory, false);
            labelText.alignment = TextAnchor.MiddleCenter;

            // Subtle bronze border tick
            CreateLine(go.transform, 0, 0, 2, h, primary ? Gold : Bronze);

            return btn;
        }

        private void CreateLine(Transform parent, float x, float y, float w, float h, Color col)
        {
            GameObject go = CreateRect("Line", parent, x, y, w, h);
            Image img = go.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
        }
    }
}
