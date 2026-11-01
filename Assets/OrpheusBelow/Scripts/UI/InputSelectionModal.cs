using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OrpheusBelow
{
    /// <summary>
    /// Classical painted oil modal dialog for selecting player input method:
    ///   - Keyboard & Mouse vs. Microphone (Breath/Acoustic).
    ///   - Enclosed in an ornate museum-grade classical gilded golden frame.
    ///   - Gilded golden card borders that bloom with radiant gold on hover and selection.
    ///   - Buttons smoothly size up on hover.
    ///   - Displays before the introductory cinematic on boot.
    /// </summary>
    public sealed class InputSelectionModal : MonoBehaviour
    {
        public bool IsConfirmed { get; private set; }
        public SensorSourceMode SelectedMode { get; private set; } = SensorSourceMode.KeyboardAndMouseOnly;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _modalBox;

        private HoverCard _keyboardCard;
        private HoverCard _micCard;
        private HoverCard _confirmButton;

        private Image _kbGlow;
        private Image _micGlow;
        private Image _confirmGlow;

        public void Begin()
        {
            // Initial selection based on persisted preference
            SelectedMode = SensorPulseInput.GlobalSourceMode;
            if (SelectedMode != SensorSourceMode.KeyboardPlusPCMicrophone)
            {
                SelectedMode = SensorSourceMode.KeyboardAndMouseOnly;
            }

            BuildUI();
            UpdateSelectionVisuals();
            StartCoroutine(FadeIn(0.40f));
        }

        private IEnumerator FadeIn(float duration)
        {
            _canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        public IEnumerator FadeAway(float duration)
        {
            float elapsed = 0f;
            float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        private void BuildUI()
        {
            var canvasGo = gameObject;
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 3000; // Above title and intro

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            // Fullscreen dark oil background veil
            var veilGo = new GameObject("BackdropVeil", typeof(RectTransform), typeof(Image));
            veilGo.transform.SetParent(transform, false);
            var veilRt = veilGo.GetComponent<RectTransform>();
            veilRt.anchorMin = Vector2.zero;
            veilRt.anchorMax = Vector2.one;
            veilRt.sizeDelta = Vector2.zero;
            var veilImg = veilGo.GetComponent<Image>();
            veilImg.color = new Color(0.02f, 0.015f, 0.025f, 0.94f);
            veilImg.raycastTarget = true;

            // Central modal box (1024 x 576 native artwork dimensions)
            var boxGo = new GameObject("ModalBox", typeof(RectTransform), typeof(Image));
            boxGo.transform.SetParent(transform, false);
            _modalBox = boxGo.GetComponent<RectTransform>();
            _modalBox.anchorMin = new Vector2(0.5f, 0.5f);
            _modalBox.anchorMax = new Vector2(0.5f, 0.5f);
            _modalBox.pivot = new Vector2(0.5f, 0.5f);
            _modalBox.sizeDelta = new Vector2(1024, 576);

            var bgImg = boxGo.GetComponent<Image>();
            bgImg.sprite = LoadSprite("UI/InputSelection/menu_backdrop", "menu_backdrop.png");
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;

            // Classical Museum Gilded Golden Frame encasing the modal dialog
            var frameGo = new GameObject("ModalGoldenFrame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(_modalBox, false);
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchoredPosition = Vector2.zero;
            frameRt.sizeDelta = new Vector2(1116f, 668f);
            var frameImg = frameGo.GetComponent<Image>();
            frameImg.sprite = LoadSprite("UI/InputSelection/frame_modal_golden", "frame_modal_golden.png");
            frameImg.color = Color.white;
            frameImg.raycastTarget = false;

            // 1. Keyboard Card
            // In 1024x576 coordinates (center is 512, 288):
            // Center is (-183, 3), Size: (314, 208)
            var kbGo = new GameObject("KeyboardCard", typeof(RectTransform), typeof(Image));
            kbGo.transform.SetParent(_modalBox, false);
            var kbRt = kbGo.GetComponent<RectTransform>();
            kbRt.anchoredPosition = new Vector2(-183f, 3f);
            kbRt.sizeDelta = new Vector2(314f, 208f);
            var kbImg = kbGo.GetComponent<Image>();
            kbImg.sprite = LoadSprite("UI/InputSelection/card_keyboard", "card_keyboard.png");
            kbImg.color = Color.white;
            kbImg.raycastTarget = true;

            _keyboardCard = kbGo.AddComponent<HoverCard>();
            _keyboardCard.normalScale = 1.0f;
            _keyboardCard.hoverScale = 1.07f;
            _keyboardCard.onClick = () => SelectMode(SensorSourceMode.KeyboardAndMouseOnly);

            // Gilded antique gold border for Keyboard card (size: 330 x 224)
            var kbGlowGo = new GameObject("KeyboardGlow", typeof(RectTransform), typeof(Image));
            kbGlowGo.transform.SetParent(kbGo.transform, false);
            var kbGlowRt = kbGlowGo.GetComponent<RectTransform>();
            kbGlowRt.anchoredPosition = Vector2.zero;
            kbGlowRt.sizeDelta = new Vector2(330f, 224f);
            _kbGlow = kbGlowGo.GetComponent<Image>();
            _kbGlow.sprite = LoadSprite("UI/InputSelection/glow_keyboard", "glow_keyboard.png");
            _kbGlow.color = new Color(1f, 1f, 1f, 1f);
            _kbGlow.raycastTarget = false;

            // 2. Microphone Card
            // Center is (+185.5, 3), Size: (319, 208)
            var micGo = new GameObject("MicrophoneCard", typeof(RectTransform), typeof(Image));
            micGo.transform.SetParent(_modalBox, false);
            var micRt = micGo.GetComponent<RectTransform>();
            micRt.anchoredPosition = new Vector2(185.5f, 3f);
            micRt.sizeDelta = new Vector2(319f, 208f);
            var micImg = micGo.GetComponent<Image>();
            micImg.sprite = LoadSprite("UI/InputSelection/card_mic", "card_mic.png");
            micImg.color = Color.white;
            micImg.raycastTarget = true;

            _micCard = micGo.AddComponent<HoverCard>();
            _micCard.normalScale = 1.0f;
            _micCard.hoverScale = 1.07f;
            _micCard.onClick = () => SelectMode(SensorSourceMode.KeyboardPlusPCMicrophone);

            // Gilded antique gold border for Microphone card (size: 335 x 224)
            var micGlowGo = new GameObject("MicrophoneGlow", typeof(RectTransform), typeof(Image));
            micGlowGo.transform.SetParent(micGo.transform, false);
            var micGlowRt = micGlowGo.GetComponent<RectTransform>();
            micGlowRt.anchoredPosition = Vector2.zero;
            micGlowRt.sizeDelta = new Vector2(335f, 224f);
            _micGlow = micGlowGo.GetComponent<Image>();
            _micGlow.sprite = LoadSprite("UI/InputSelection/glow_mic", "glow_mic.png");
            _micGlow.color = new Color(1f, 1f, 1f, 0.40f);
            _micGlow.raycastTarget = false;

            // 3. Confirm Button
            // Center is (0, -145.5), Size: (230, 45)
            var confirmGo = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image));
            confirmGo.transform.SetParent(_modalBox, false);
            var confirmRt = confirmGo.GetComponent<RectTransform>();
            confirmRt.anchoredPosition = new Vector2(0f, -145.5f);
            confirmRt.sizeDelta = new Vector2(230f, 45f);
            var confirmImg = confirmGo.GetComponent<Image>();
            confirmImg.sprite = LoadSprite("UI/InputSelection/btn_confirm", "btn_confirm.png");
            confirmImg.color = Color.white;
            confirmImg.raycastTarget = true;

            _confirmButton = confirmGo.AddComponent<HoverCard>();
            _confirmButton.normalScale = 1.0f;
            _confirmButton.hoverScale = 1.08f;
            _confirmButton.onClick = Confirm;

            // Gilded antique gold border for Confirm Button (size: 246 x 61)
            var confirmGlowGo = new GameObject("ConfirmGlow", typeof(RectTransform), typeof(Image));
            confirmGlowGo.transform.SetParent(confirmGo.transform, false);
            var confirmGlowRt = confirmGlowGo.GetComponent<RectTransform>();
            confirmGlowRt.anchoredPosition = Vector2.zero;
            confirmGlowRt.sizeDelta = new Vector2(246f, 61f);
            _confirmGlow = confirmGlowGo.GetComponent<Image>();
            _confirmGlow.sprite = LoadSprite("UI/InputSelection/glow_confirm", "glow_confirm.png");
            _confirmGlow.color = new Color(1f, 1f, 1f, 0.65f);
            _confirmGlow.raycastTarget = false;
        }

        private void SelectMode(SensorSourceMode mode)
        {
            if (SelectedMode == mode) return;
            SelectedMode = mode;
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            bool isKb = (SelectedMode == SensorSourceMode.KeyboardAndMouseOnly);
            bool isMic = (SelectedMode == SensorSourceMode.KeyboardPlusPCMicrophone);

            if (_keyboardCard != null) _keyboardCard.normalScale = isKb ? 1.025f : 1.0f;
            if (_micCard != null) _micCard.normalScale = isMic ? 1.025f : 1.0f;
        }

        public void Confirm()
        {
            if (IsConfirmed) return;
            IsConfirmed = true;
            UnderworldSoundscape.Instance?.PlayMenuConfirm();

            SensorPulseInput sensor = SensorPulseInput.Instance;
            if (sensor != null)
            {
                sensor.SetSourceMode(SelectedMode);
            }
            PlayerPrefs.SetInt("OrpheusBelow.SensorSourceMode", (int)SelectedMode);
            PlayerPrefs.Save();
        }

        private void Update()
        {
            if (IsConfirmed) return;

            // Keyboard / Gamepad navigation
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
                {
                    SelectMode(SensorSourceMode.KeyboardAndMouseOnly);
                }
                else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
                {
                    SelectMode(SensorSourceMode.KeyboardPlusPCMicrophone);
                }
                else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                {
                    Confirm();
                }
            }

            var activePad = Xbox360Input.GetActiveGamepad() ?? pad;
            if (activePad != null)
            {
                if (activePad.dpad.left.wasPressedThisFrame || activePad.leftStick.left.wasPressedThisFrame)
                {
                    SelectMode(SensorSourceMode.KeyboardAndMouseOnly);
                }
                else if (activePad.dpad.right.wasPressedThisFrame || activePad.leftStick.right.wasPressedThisFrame)
                {
                    SelectMode(SensorSourceMode.KeyboardPlusPCMicrophone);
                }
                else if (activePad.buttonSouth.wasPressedThisFrame || activePad.startButton.wasPressedThisFrame)
                {
                    Confirm();
                }
            }

            // Smoothly interpolate golden border alpha for Keyboard card
            if (_kbGlow != null && _keyboardCard != null)
            {
                bool isKb = (SelectedMode == SensorSourceMode.KeyboardAndMouseOnly);
                float targetAlpha = (isKb || _keyboardCard.isHovered) ? 1.0f : 0.40f;
                Color c = _kbGlow.color;
                c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.unscaledDeltaTime * 7f);
                _kbGlow.color = c;
            }

            // Smoothly interpolate golden border alpha for Microphone card
            if (_micGlow != null && _micCard != null)
            {
                bool isMic = (SelectedMode == SensorSourceMode.KeyboardPlusPCMicrophone);
                float targetAlpha = (isMic || _micCard.isHovered) ? 1.0f : 0.40f;
                Color c = _micGlow.color;
                c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.unscaledDeltaTime * 7f);
                _micGlow.color = c;
            }

            // Smoothly interpolate golden border alpha for Confirm button on hover
            if (_confirmGlow != null && _confirmButton != null)
            {
                float targetAlpha = _confirmButton.isHovered ? 1.0f : 0.65f;
                Color c = _confirmGlow.color;
                c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.unscaledDeltaTime * 7f);
                _confirmGlow.color = c;
            }
        }

        private static Sprite LoadSprite(string resPath, string fileName)
        {
            Sprite s = Resources.Load<Sprite>(resPath);
            if (s != null) return s;

            string directPath = Path.Combine(Application.dataPath, "OrpheusBelow/Resources", resPath + ".png");
            if (File.Exists(directPath))
            {
                byte[] data = File.ReadAllBytes(directPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(data))
                {
                    tex.name = fileName;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            return null;
        }

        public sealed class HoverCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
        {
            public float normalScale = 1.0f;
            public float hoverScale = 1.07f;
            public float pressScale = 0.97f;
            public bool isHovered { get; private set; }
            public bool isPressed { get; private set; }
            public System.Action onClick;

            public void OnPointerEnter(PointerEventData eventData)
            {
                isHovered = true;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                isHovered = false;
                isPressed = false;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                isPressed = true;
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (isPressed)
                {
                    isPressed = false;
                    onClick?.Invoke();
                }
            }

            private void Update()
            {
                float target = isPressed ? pressScale : (isHovered ? hoverScale : normalScale);
                float current = transform.localScale.x;
                float next = Mathf.Lerp(current, target, Time.unscaledDeltaTime * 14f);
                transform.localScale = new Vector3(next, next, 1f);
            }
        }
    }
}
