using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OrpheusBelow
{
    /// <summary>
    /// Subtle, literary ScreenSpaceOverlay UGUI HUD for Orpheus Below.
    /// Uses classical Windows serif typography (Palatino Linotype / Georgia) + ultra-thin geometric bars
    /// rendering razor-sharp in HDRP without cluttering the artwork.
    /// </summary>
    public partial class OrpheusHud : MonoBehaviour
    {
        public static OrpheusHud Instance { get; private set; }
        public static bool IsTutorialModalActive { get; private set; }
        public string CurrentTutorialSpeaker { get; private set; } = string.Empty;

        public bool showControlsHelp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionStatic()
        {
            Instance = null;
            IsTutorialModalActive = false;
        }

        private Font _serifFont;
        private Font _bodyFont;
        private bool _ownsBodyFont;
        private BlurredEurydice _eurydice;

        // Bottom-left minimal bars + Cadence trail
        private RectTransform _hpFill;
        private Image[] _dashGems;
        private RectTransform _pulseFill;
        private Image _pulseFillImg;
        private Text _cadenceLabel;

        // Transparent PNG Musical Notation Sprites & Image Slots
        private Sprite _quaverSprite;
        private Sprite _fermataSprite;
        private Image[] _cadenceSlotImages;
        private GameObject _cadenceSlotContainer;
        private Image _fermataThresholdMarker;

        // Dialogue Musical Symbols Display
        private GameObject _dialogueSymbolsRoot;
        private Image[] _dialogueIcons;
        private Text _dialogueSymbolsCaption;

        private Text _sensorModeLabel;
        private Text _journeyLabel;
        private Text _journeyObjective;
        private Text _navigationLegend;
        private CanvasGroup _vitalsGroup;

        // Top-center subtle banners & Guardian Rhythm bar
        private CanvasGroup _bannerGroup;
        private Text _bannerTitleText;
        private float _bannerTimer;

        private Text _calloutText;
        private float _calloutTimer;

        private GameObject _bossBarRoot;
        private Text _bossNameText;
        private RectTransform _bossHpFill;
        private Text _bossJudgementText;

        // Bottom Eurydice Soul-Tether Abduction, Whisper & Look-back
        private GameObject _abductionRoot;
        private Text _abductionWarnText;
        private RectTransform _abductionFill;

        private Text _whisperText;
        private GameObject _lookBackRoot;
        private RectTransform _lookBackFill;

        // Literary non-blocking subtitles at screen bottom (real-time voice acting)
        private GameObject _subtitleRoot;
        private CanvasGroup _subtitleGroup;
        private Text _subtitleSpeakerText;
        private Text _subtitleBodyText;
        private float _subtitleTimer;

        // Bottom-anchored dialogue; Artemis teaches, Eurydice speaks after the rescue.
        private RectTransform _canvasRoot;
        private GameObject _tutorialBoxRoot;
        private RectTransform _tutorialBoxRect;
        private Text _tutorialSpeakerText;
        private Text _tutorialBodyText;
        private Text _tutorialContinuePrompt;
        private RawImage _tutorialPortrait;
        private bool _tutorialUsesCompanionPose;
        private System.Action _tutorialConfirmCallback;

        // Optional [H] controls card
        private GameObject _controlsCard;
        private Text _controlsHintCorner;

        // Cadence Unlock Ceremony Overlay
        private GameObject _cadenceUnlockRoot;
        private CanvasGroup _cadenceUnlockGroup;
        private Text _cadenceUnlockTitle;
        private Text _cadenceUnlockName;
        private Text _cadenceUnlockFormula;
        private Text _cadenceUnlockDesc;
        private float _cadenceUnlockTimer;
        private GameObject _cadenceUnlockIconsTray;
        private readonly Image[] _cadenceUnlockIcons = new Image[3];
        private readonly Text[] _cadenceUnlockDashes = new Text[2];

        // Screen wipe & End screen
        private Image _fadeOverlay;
        private GameObject _endScreenRoot;
        private Image _endScreenBg;
        private Text _endTitleText;
        private Text _endBodyText;

        private const float BarWidth = 300f;
        private const float BossBarWidth = 592f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (VoiceDirector.Instance == null)
            {
                gameObject.AddComponent<VoiceDirector>();
            }

            _serifFont = LoadClassicalFont();
            _bodyFont = _serifFont;
            _ownsBodyFont = false;

            // Load transparent PNG musical notation assets
            _quaverSprite = Resources.Load<Sprite>("MusicalNotation/symbol_quaver_small");
            _fermataSprite = Resources.Load<Sprite>("MusicalNotation/symbol_fermata_boom");

            BuildMinimalCanvas();
        }

        private void OnDestroy()
        {
            if (_ownsBodyFont && _bodyFont != null) Destroy(_bodyFont);
            if (Instance != this) return;
            Instance = null;
            ReleaseCompanionTeachingPose();
            if (IsTutorialModalActive)
            {
                IsTutorialModalActive = false;
                Time.timeScale = UnderworldFrontEnd.BlocksGameplay ? 0f : GameplayClock.ResumeScale;
            }
        }

        private void Start()
        {
            _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();
            // Guarantee UnderworldFrontEnd is always present in scene
            if (UnderworldFrontEnd.Instance == null && Object.FindFirstObjectByType<UnderworldFrontEnd>() == null)
            {
                gameObject.AddComponent<UnderworldFrontEnd>();
            }
        }

        private static Font LoadClassicalFont()
        {
            return GameTypography.Font;
        }

        public void ShowBanner(string text, float duration = 2.8f)
        {
            if (_bannerTitleText != null) _bannerTitleText.text = text;
            _bannerTimer = duration;
        }

        public void ShowSubtleCallout(string text, float duration = 1.35f)
        {
            if (_calloutText != null) _calloutText.text = text;
            _calloutTimer = duration;
        }

        public void ShowSubtitle(string speaker, Color speakerColor, string text, float duration)
        {
            if (_subtitleRoot == null) return;
            _subtitleRoot.SetActive(true);
            if (_subtitleSpeakerText != null)
            {
                _subtitleSpeakerText.text = speaker.ToUpperInvariant();
                _subtitleSpeakerText.color = speakerColor;
            }
            if (_subtitleBodyText != null)
            {
                _subtitleBodyText.text = text;
                float h = Mathf.Max(40f, _subtitleBodyText.preferredHeight + 8f);
                _subtitleBodyText.rectTransform.sizeDelta = new Vector2(1000f, h);
                RectTransform rt = _subtitleRoot.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(1040f, Mathf.Max(138f, h + 68f));
            }
            _subtitleTimer = duration;
        }

        public void HideSubtitle()
        {
            _subtitleTimer = 0f;
        }

        public void ShowArtemisTutorial(string text, System.Action onConfirm = null)
        {
            CurrentTutorialSpeaker = "Narrator";
            VoiceDirector.Instance?.PlayNarratorTutorial(text, onConfirm);
        }

        public void ShowArtemisHint(string text, float duration = 3.2f)
        {
            ShowSubtleCallout(text, duration);
        }

        public void ShowEurydiceTutorial(string text, System.Action onConfirm = null)
        {
            if (_eurydice == null) _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();
            CurrentTutorialSpeaker = "Eurydice";
            VoiceDirector.Instance?.PlayEurydiceTutorial(text, onConfirm);
        }

        public void ShowHadesTutorial(string text, System.Action onConfirm = null)
        {
            CurrentTutorialSpeaker = "Hades";
            VoiceDirector.Instance?.PlayHadesTutorial(text, onConfirm);
        }

        public void ShowBossTutorial(string bossName, string text, System.Action onConfirm = null)
        {
            // Unvoiced boss dialogues removed as requested; game remains real-time
            onConfirm?.Invoke();
        }

        private void ShowTutorial(string speaker, string text, System.Action onConfirm, bool companionSpeech)
        {
            ReleaseCompanionTeachingPose();
            CurrentTutorialSpeaker = speaker;
            if (speaker == "Hades")
                VoiceDirector.Instance?.PlayHadesTutorial(text, onConfirm);
            else if (speaker == "Eurydice")
                VoiceDirector.Instance?.PlayEurydiceTutorial(text, onConfirm);
            else
                VoiceDirector.Instance?.PlayNarratorTutorial(text, onConfirm);
        }

        private void ReleaseCompanionTeachingPose()
        {
            if (!_tutorialUsesCompanionPose) return;
            _eurydice?.SetTeachingPose(false);
            _tutorialUsesCompanionPose = false;
        }

        private void SetTutorialPortrait(string speaker, bool companionSpeech)
        {
            if (_tutorialPortrait == null) return;
            _tutorialPortrait.uvRect = new Rect(0f, 0f, 1f, 1f);

            Texture2D tex = null;
            if (speaker == "Eurydice" || companionSpeech)
            {
                tex = Resources.Load<Texture2D>("Characters/Eurydice/Portrait");
                if (tex == null)
                {
                    var library = Resources.Load<EurydiceAnimationLibrary>(EurydiceAnimationLibrary.ResourcePath);
                    if (library != null && library.idle != null && library.idle.Length > 0 && library.idle[0] != null)
                    {
                        Sprite frame = library.idle[0];
                        Rect rect = frame.rect;
                        float side = Mathf.Min(rect.width, rect.height * .48f);
                        _tutorialPortrait.texture = frame.texture;
                        _tutorialPortrait.uvRect = new Rect((rect.center.x - side * .5f) / frame.texture.width,
                            (rect.yMax - side) / frame.texture.height, side / frame.texture.width, side / frame.texture.height);
                        _tutorialPortrait.gameObject.SetActive(true);
                        return;
                    }
                }
            }
            else if (speaker == "Hades")
            {
                tex = Resources.Load<Texture2D>("Characters/Hades/Portrait");
            }
            else if (speaker.Contains("Tripod"))
            {
                tex = Resources.Load<Texture2D>("Characters/Tripod/Portrait");
            }
            else if (speaker.Contains("Eye Warden") || speaker.Contains("Mystic Eye"))
            {
                tex = Resources.Load<Texture2D>("Characters/MysticEyeWarden/Portrait");
            }
            else
            {
                tex = Resources.Load<Texture2D>("Characters/Artemis/GuidePortrait");
            }

            _tutorialPortrait.texture = tex;
            _tutorialPortrait.gameObject.SetActive(_tutorialPortrait.texture != null);
        }

        public void ShowEurydiceHint(string text, float duration = 3.2f)
        {
            // All tutorial lessons pause gameplay until player presses ENTER to continue
            ShowEurydiceTutorial(text);
        }

        public void DismissTutorialModal()
        {
            if (!IsTutorialModalActive) return;
            IsTutorialModalActive = false;
            CurrentTutorialSpeaker = string.Empty;
            if (_tutorialBoxRoot != null) _tutorialBoxRoot.SetActive(false);
            ReleaseCompanionTeachingPose();
            UnderworldSoundscape.Instance?.PlayMenuConfirm();
            Time.timeScale = UnderworldFrontEnd.BlocksGameplay ? 0f : GameplayClock.ResumeScale;

            System.Action cb = _tutorialConfirmCallback;
            _tutorialConfirmCallback = null;
            cb?.Invoke();
        }

        public void HideEurydiceTutorial()
        {
            DismissTutorialModal();
        }

        public void ShowCadenceUnlockCeremony(string cadenceName, string chordFormula, string description, float duration = 4.2f)
        {
            if (_cadenceUnlockRoot != null)
            {
                _cadenceUnlockRoot.SetActive(true);
                if (_cadenceUnlockName != null) _cadenceUnlockName.text = cadenceName;
                if (_cadenceUnlockDesc != null) _cadenceUnlockDesc.text = description;
                _cadenceUnlockTimer = duration;
                if (_cadenceUnlockGroup != null) _cadenceUnlockGroup.alpha = 0f;

                ConfigureCadenceUnlockIcons(cadenceName, chordFormula);
            }

            ProceduralLyreAudio.Instance?.PlayCadenceUnlockFanfare();
            if (OrpheusPlayerController.Instance != null)
            {
                GroundShockwaveRing.Spawn(OrpheusPlayerController.Instance.transform.position + new Vector3(0f, 0.08f, 0f), 7.5f,
                    new Color(2.8f, 2.2f, 0.55f, 1f), 0.55f);
            }
        }

        private void ConfigureCadenceUnlockIcons(string cadenceName, string chordFormula)
        {
            if (_cadenceUnlockIcons == null || _cadenceUnlockIcons.Length < 3) return;

            var notes = new System.Collections.Generic.List<char>();
            if (!string.IsNullOrEmpty(cadenceName) && cadenceName.Contains("Sanctuary"))
            {
                notes.Add('B');
                notes.Add('S');
            }
            else if (!string.IsNullOrEmpty(cadenceName) && cadenceName.Contains("Hymn"))
            {
                notes.Add('S');
                notes.Add('S');
                notes.Add('B');
            }
            else if (!string.IsNullOrEmpty(cadenceName) && cadenceName.Contains("Siren"))
            {
                notes.Add('S');
                notes.Add('B');
                notes.Add('S');
            }
            else
            {
                // Dynamic fallback from formula
                if (!string.IsNullOrEmpty(chordFormula) && (chordFormula.Contains("Boom") || chordFormula.Contains("Fermata") || chordFormula.Contains("B")))
                {
                    notes.Add('B');
                    notes.Add('S');
                }
                else
                {
                    notes.Add('S');
                    notes.Add('B');
                }
            }

            int count = UnityEngine.Mathf.Clamp(notes.Count, 1, 3);
            if (count == 2)
            {
                SetUnlockIcon(0, notes[0], new Vector2(-54f, 0f));
                SetUnlockDash(0, new Vector2(0f, 0f), true);
                SetUnlockIcon(1, notes[1], new Vector2(54f, 0f));
                SetUnlockDash(1, Vector2.zero, false);
                if (_cadenceUnlockIcons[2] != null) _cadenceUnlockIcons[2].gameObject.SetActive(false);
            }
            else
            {
                SetUnlockIcon(0, notes[0], new Vector2(-92f, 0f));
                SetUnlockDash(0, new Vector2(-46f, 0f), true);
                SetUnlockIcon(1, notes[1], new Vector2(0f, 0f));
                SetUnlockDash(1, new Vector2(46f, 0f), true);
                SetUnlockIcon(2, notes[2], new Vector2(92f, 0f));
            }

            if (_cadenceUnlockFormula != null)
            {
                string caption = count == 2
                    ? (notes[0] == 'B' ? "Fermata (Hard Boom)  —  Quaver (Soft Note)" : "Quaver (Soft Note)  —  Fermata (Hard Boom)")
                    : (notes[1] == 'B' ? "Quaver (Soft)  —  Fermata (Boom)  —  Quaver (Soft)" : "Quaver  —  Quaver  —  Fermata");
                _cadenceUnlockFormula.text = caption;
                _cadenceUnlockFormula.rectTransform.anchoredPosition = new Vector2(0f, -44f);
                _cadenceUnlockFormula.fontSize = 15;
                _cadenceUnlockFormula.color = new Color(0.85f, 0.78f, 0.58f, 0.85f);
            }
        }

        private void SetUnlockIcon(int idx, char note, Vector2 pos)
        {
            Image img = _cadenceUnlockIcons[idx];
            if (img == null) return;
            img.gameObject.SetActive(true);
            img.rectTransform.anchoredPosition = pos;
            bool isBoom = (note == 'B');
            img.sprite = isBoom ? _fermataSprite : _quaverSprite;
            img.color = isBoom
                ? new Color(1.0f, 0.88f, 0.18f, 1f)
                : new Color(1.0f, 0.92f, 0.52f, 1f);
        }

        private void SetUnlockDash(int idx, Vector2 pos, bool active)
        {
            if (_cadenceUnlockDashes[idx] != null)
            {
                _cadenceUnlockDashes[idx].gameObject.SetActive(active);
                _cadenceUnlockDashes[idx].rectTransform.anchoredPosition = pos;
            }
        }

        private void BuildMinimalCanvas()
        {
            GameObject canvasGo = new GameObject("OrpheusMinimalCanvas_UGUI");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = canvas.GetComponent<RectTransform>();
            _canvasRoot = root;

            BuildVitalsPanel(root);
            BuildJourneyPanel(root);

            // --- 2. TOP-CENTER: FADING LITERARY CHAMBER TITLE & GUARDIAN RHYTHM BAR ---
            RectTransform bannerRt = CreateRect("TopChamberBanner", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(1040f, 48f));
            _bannerGroup = bannerRt.gameObject.AddComponent<CanvasGroup>();
            _bannerGroup.alpha = 0f;
            _bannerTitleText = CreateSerifText("BannerText", bannerRt, Vector2.zero, new Vector2(1040f, 48f),
                29, FontStyle.Normal, TextAnchor.MiddleCenter, GameTypography.Ivory);

            _calloutText = CreateSerifText("SubtleCallout", root, new Vector2(0f, -96f), new Vector2(680f, 32f),
                23, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.95f, 0.84f, 0.58f, 0f),
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f));

            BuildGuardianPanel(root);

            // --- 3. BOTTOM-CENTER: SOUL TETHER ABDUCTION WARNING, WHISPER & [HOLD Q] ---
            RectTransform abRt = CreateRect("TetherAbductionCluster", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 102f), new Vector2(640f, 34f));
            _abductionRoot = abRt.gameObject;
            _abductionWarnText = CreateSerifText("AbductionWarnText", abRt, new Vector2(0f, 10f), new Vector2(640f, 24f),
                16, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(1.0f, 0.44f, 0.52f, 0.92f));
            _abductionWarnText.text = "Mourning Shades are dragging Eurydice back!   [Strike Them or Play B - S]";
            CreateBarBg(abRt, new Vector2(140f, 2f), new Vector2(360f, 3f), new Color(0.20f, 0.04f, 0.08f, 0.75f));
            _abductionFill = CreateBarFill(abRt, "AbductionFill", new Vector2(140f, 2f), new Vector2(0f, 3f), new Color(1.0f, 0.25f, 0.38f, 0.95f), out _);
            _abductionRoot.SetActive(false);

            _whisperText = CreateSerifText("EurydiceWhisper", root, new Vector2(0f, 64f), new Vector2(1040f, 34f),
                17, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.78f, 0.90f, 0.98f, 0f),
                anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(0.5f, 0f), pivot: new Vector2(0.5f, 0f));

            RectTransform lbRt = CreateRect("LookBackCluster", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(320f, 28f));
            _lookBackRoot = lbRt.gameObject;
            Text lbWarn = CreateSerifText("LookBackWarn", lbRt, new Vector2(0f, 10f), new Vector2(320f, 22f),
                15, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.98f, 0.55f, 0.68f, 0.90f));
            lbWarn.text = "Do not turn back...";
            CreateBarBg(lbRt, new Vector2(0f, 2f), new Vector2(320f, 3f), new Color(0.18f, 0.04f, 0.08f, 0.7f));
            _lookBackFill = CreateBarFill(lbRt, "LookBackFill", new Vector2(0f, 2f), new Vector2(0f, 3f), new Color(0.98f, 0.28f, 0.46f, 0.95f), out _);
            _lookBackRoot.SetActive(false);

            BuildSubtitlePanel(root);

            // --- DEPRECATED MODAL BOX (KEPT INACTIVE SO ZERO MODAL POPUPS APPEAR) ---
            _tutorialBoxRect = CreateRect("GuideDialogueBox", root,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 174f), new Vector2(820f, 196f));
            _tutorialBoxRoot = _tutorialBoxRect.gameObject;

            Image boxBg = _tutorialBoxRoot.AddComponent<Image>();
            UnderworldArtLibrary.StylePanel(boxBg);

            RectTransform portraitRect = CreateRect("SpeakerPortrait", _tutorialBoxRect,
                Vector2.up, Vector2.up, Vector2.up, new Vector2(12f, -22f), new Vector2(126f, 126f) * 1.2f);
            _tutorialPortrait = portraitRect.gameObject.AddComponent<RawImage>();
            _tutorialPortrait.raycastTarget = false;
            _tutorialPortrait.color = new Color(.94f, .94f, .90f);

            _tutorialSpeakerText = CreateSerifText("DialogueSpeaker", _tutorialBoxRect,
                new Vector2(178f, -24f), new Vector2(608f, 24f), 17, FontStyle.Normal,
                TextAnchor.MiddleLeft, new Color(.82f, .70f, .46f),
                anchorMin: Vector2.up, anchorMax: Vector2.up, pivot: Vector2.up);
            _tutorialSpeakerText.text = "Artemis";

            _tutorialBodyText = CreateSerifText("DialogueBody", _tutorialBoxRect,
                new Vector2(178f, -59f), new Vector2(608f, 90f), 22, FontStyle.Normal,
                TextAnchor.UpperLeft, new Color(.94f, .92f, .86f),
                anchorMin: Vector2.up, anchorMax: Vector2.up, pivot: Vector2.up);
            _tutorialBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            _tutorialBodyText.font = _bodyFont;
            _tutorialBodyText.lineSpacing = 1.12f;

            _tutorialContinuePrompt = CreateSerifText("TutorialContinuePrompt", _tutorialBoxRect,
                new Vector2(-34f, 22f), new Vector2(748f, 22f), 15, FontStyle.Normal,
                TextAnchor.MiddleRight, new Color(.82f, .78f, .67f),
                anchorMin: Vector2.right, anchorMax: Vector2.right, pivot: Vector2.right);
            _tutorialContinuePrompt.text = "Enter to continue";
            _tutorialContinuePrompt.font = _bodyFont;

            // Dialogue Musical Symbol Tray — displays transparent PNG images in dialogue (balanced optical scale)
            RectTransform diagSymRt = CreateRect("DialogueSymbolsTray", _tutorialBoxRect, Vector2.zero, Vector2.zero, new Vector2(0f, 0f), new Vector2(34f, 14f), new Vector2(500f, 36f));
            _dialogueSymbolsRoot = diagSymRt.gameObject;

            _dialogueIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform dIconRt = CreateRect("DiagIcon_" + i, diagSymRt, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(i * 34f + 16f, 18f), new Vector2(26f, 26f));
                _dialogueIcons[i] = dIconRt.gameObject.AddComponent<Image>();
                _dialogueIcons[i].preserveAspect = true;
            }

            _dialogueSymbolsCaption = CreateSerifText("DialogueSymbolsCaption", diagSymRt, new Vector2(124f, 6f), new Vector2(360f, 22f),
                14, FontStyle.Italic, TextAnchor.MiddleLeft, new Color(0.95f, 0.88f, 0.65f, 0.92f));
            _dialogueSymbolsCaption.font = _bodyFont;
            _dialogueSymbolsRoot.SetActive(false);

            _tutorialBoxRoot.SetActive(false);


            // --- 5. BOTTOM-RIGHT: DISCREET [H] CONTROLS TOGGLE & [Esc] SETTINGS HINT ---
            _controlsHintCorner = CreateSerifText("ControlsCornerHint", root, new Vector2(-38f, 30f), new Vector2(280f, 24f),
                15, FontStyle.Normal, TextAnchor.LowerRight, new Color(0.82f, 0.78f, 0.72f, 0.78f),
                anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f), pivot: new Vector2(1f, 0f));
            _controlsHintCorner.text = "Esc  Pause   H  Controls   F3  Zones";
            _controlsHintCorner.font = _bodyFont;

            RectTransform cardRt = CreateRect("ControlsCard", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-38f, 72f), new Vector2(470f, 330f));
            UnderworldArtLibrary.StylePanel(cardRt.gameObject.AddComponent<Image>());
            Text cardTxt = CreateSerifText("ControlsBody", cardRt, new Vector2(30f, 26f), new Vector2(410f, 278f),
                17, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(.90f, .88f, .82f));
            cardTxt.lineSpacing = 1.2f;
            cardTxt.font = _bodyFont;
            cardTxt.text =
                "WASD       Move · A / D during guardian duels\n" +
                "Space / Shift       Dash · two charges\n" +
                "LMB / J       Small Quaver (Soft)\n" +
                "RMB / K       Boom Fermata (Boom)\n" +
                "F       Draw the flute\n\n" +
                "Soft – Soft – Boom       Hymn of Dawn\n" +
                "Boom – Soft           Sanctuary of Eurydice\n" +
                "Soft – Boom – Soft       Siren's Convergence\n\n" +
                "R  Restart     Tab  Input     H  Close guide";
            _controlsCard = cardRt.gameObject;
            _controlsCard.SetActive(showControlsHelp);

            // --- 6. CADENCE UNLOCK CEREMONY OVERLAY ---
            RectTransform unlockRt = CreateRect("CadenceUnlockOverlay", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _cadenceUnlockRoot = unlockRt.gameObject;
            _cadenceUnlockGroup = unlockRt.gameObject.AddComponent<CanvasGroup>();
            _cadenceUnlockGroup.alpha = 0f;

            Image unlockBg = unlockRt.gameObject.AddComponent<Image>();
            unlockBg.color = new Color(0.04f, 0.02f, 0.08f, 0.78f);
            unlockBg.raycastTarget = false;

            _cadenceUnlockTitle = CreateSerifText("UnlockHeader", unlockRt, new Vector2(0f, 110f), new Vector2(800f, 32f),
                20, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.55f, 0.85f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));
            _cadenceUnlockTitle.text = "Cadence learned";

            _cadenceUnlockName = CreateSerifText("UnlockName", unlockRt, new Vector2(0f, 52f), new Vector2(900f, 58f),
                38, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(.95f, .91f, .79f, 1f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));

            // Visual Transparent PNG Notation Tray
            GameObject trayGo = new GameObject("UnlockNotationTray");
            RectTransform trayRt = trayGo.AddComponent<RectTransform>();
            trayRt.SetParent(unlockRt, false);
            trayRt.anchorMin = new Vector2(0.5f, 0.5f);
            trayRt.anchorMax = new Vector2(0.5f, 0.5f);
            trayRt.pivot = new Vector2(0.5f, 0.5f);
            trayRt.anchoredPosition = new Vector2(0f, -6f);
            trayRt.sizeDelta = new Vector2(400f, 60f);
            _cadenceUnlockIconsTray = trayGo;

            for (int i = 0; i < 3; i++)
            {
                GameObject iconGo = new GameObject("UnlockIcon_" + i);
                RectTransform iconRt = iconGo.AddComponent<RectTransform>();
                iconRt.SetParent(trayRt, false);
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = new Vector2(48f, 48f);
                Image img = iconGo.AddComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
                _cadenceUnlockIcons[i] = img;
            }

            for (int i = 0; i < 2; i++)
            {
                _cadenceUnlockDashes[i] = CreateSerifText("UnlockDash_" + i, trayRt, Vector2.zero, new Vector2(40f, 40f),
                    22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.55f, 0.90f),
                    anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));
            }

            _cadenceUnlockFormula = CreateSerifText("UnlockFormula", unlockRt, new Vector2(0f, -44f), new Vector2(800f, 30f),
                15, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(.85f, .78f, .58f, .85f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));

            _cadenceUnlockDesc = CreateSerifText("UnlockDesc", unlockRt, new Vector2(0f, -74f), new Vector2(760f, 65f),
                18, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.90f, 0.86f, 0.80f, 0.88f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));

            _cadenceUnlockRoot.SetActive(false);

            // --- 7. CHAMBER TRANSITION FADE & END OVERLAY ---
            RectTransform fadeRt = CreateRect("ChamberWipeFade", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _fadeOverlay = fadeRt.gameObject.AddComponent<Image>();
            _fadeOverlay.color = new Color(0.02f, 0.01f, 0.04f, 0f);
            _fadeOverlay.raycastTarget = false;

            RectTransform endRt = CreateRect("EndOverlay", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _endScreenBg = endRt.gameObject.AddComponent<Image>();
            _endScreenBg.color = new Color(0.03f, 0.01f, 0.05f, 0.82f);

            _endTitleText = CreateSerifText("EndTitle", endRt, new Vector2(0f, 64f), new Vector2(900f, 64f),
                42, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.95f, 0.86f, 0.68f, 0.95f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));

            _endBodyText = CreateSerifText("EndBody", endRt, new Vector2(0f, -36f), new Vector2(820f, 130f),
                19, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.86f, 0.82f, 0.78f, 0.85f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));

            _endScreenRoot = endRt.gameObject;
            _endScreenRoot.SetActive(false);
        }

        private void UpdateSpeechAndTransitionUi(float dt)
        {
            var map=PaintedChamberMap.Active;
            if (_navigationLegend!=null) _navigationLegend.gameObject.SetActive(map!=null && map.overlayRoot!=null && map.overlayRoot.gameObject.activeSelf);
            var director=HadesChamberDirector.Instance;
            if (_fadeOverlay!=null && director!=null) _fadeOverlay.color=new Color(.02f,.01f,.04f,director.ScreenFadeAlpha);
            if (_subtitleRoot==null || _subtitleGroup==null) return;
            if (_subtitleTimer>0)
            {
                if (!UnderworldFrontEnd.BlocksMenusOrTransitions) _subtitleTimer-=dt;
                _subtitleGroup.alpha=Mathf.MoveTowards(_subtitleGroup.alpha,1,dt*6);
            }
            else if (_subtitleGroup.alpha>0)
            {
                _subtitleGroup.alpha=Mathf.MoveTowards(_subtitleGroup.alpha,0,dt*3.5f);
                if (_subtitleGroup.alpha<=.01f) _subtitleRoot.SetActive(false);
            }
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            // Explicit Esc / Pause key handler directly in HUD so pause toggle is guaranteed
            if ((kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
                || (pad != null && pad.startButton.wasPressedThisFrame))
            {
                if (UnderworldFrontEnd.Instance != null)
                {
                    UnderworldFrontEnd.Instance.TogglePause();
                }
            }

            UpdateSpeechAndTransitionUi(Time.unscaledDeltaTime);
            if (UnderworldFrontEnd.BlocksGameplay) return;

            if (kb != null && kb.hKey.wasPressedThisFrame)
            {
                showControlsHelp = !showControlsHelp;
                if (_controlsCard != null) _controlsCard.SetActive(showControlsHelp);
            }

            float dt = Time.unscaledDeltaTime;
            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            OrpheusLyreCombat combat = OrpheusLyreCombat.Instance;
            SensorPulseInput sensor = SensorPulseInput.Instance;
            HadesChamberDirector dir = HadesChamberDirector.Instance;
            if (_eurydice == null) _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();
            UpdateTutorialModal(kb, pad, player);
            _vitalsGroup.alpha = IsTutorialModalActive ? .35f : 1f;
            _controlsCard.SetActive(showControlsHelp && !IsTutorialModalActive);
            _journeyLabel.text = UnderworldJourney.ProgressLabel;
            _journeyObjective.text = UnderworldJourney.Objective;

            // 1. Update Vitality & Dash Gems (Original hairline format)
            if (player != null && _hpFill != null)
            {
                float hpPct = Mathf.Clamp01(player.CurrentHealth / Mathf.Max(1f, player.maxHealth));
                _hpFill.sizeDelta = new Vector2(BarWidth * hpPct, 12f);
                if (_healthValue != null) _healthValue.text = Mathf.CeilToInt(player.CurrentHealth) + " / " + Mathf.CeilToInt(player.maxHealth);

                for (int i = 0; i < _dashGems.Length; i++)
                {
                    _dashGems[i].color = i < player.CurrentDashCharges
                        ? new Color(0.94f, 0.82f, 0.48f, 0.88f)
                        : new Color(0.28f, 0.22f, 0.34f, 0.35f);
                }
            }

            // 2. Update Sensor / Pulse Bar & Physical Cadence Readout
            if (combat != null && _cadenceLabel != null)
            {
                if (_breathValue != null) _breathValue.text = "Breath " + Mathf.CeilToInt(combat.Breath.Breath);
                bool activePhrase = combat.CurrentCadenceDisplay.StartsWith("Cadence:");
                _cadenceLabel.text = activePhrase ? combat.CurrentCadenceDisplay : "H for cadences";
                _cadenceLabel.color = activePhrase
                    ? new Color(0.98f, 0.90f, 0.58f, 0.94f)
                    : new Color(0.88f, 0.82f, 0.68f, 0.62f);

                // Update transparent PNG cadence slots
                if (_cadenceSlotImages != null)
                {
                    var buf = combat.CadenceBuffer;
                    for (int i = 0; i < _cadenceSlotImages.Length; i++)
                    {
                        if (i < buf.Count)
                        {
                            char c = buf[i];
                            bool isBoom = (c == 'B');
                            _cadenceSlotImages[i].sprite = isBoom ? _fermataSprite : _quaverSprite;
                            _cadenceSlotImages[i].color = isBoom
                                ? new Color(1.0f, 0.88f, 0.22f, 0.98f)
                                : new Color(1.0f, 0.94f, 0.45f, 0.98f);
                        }
                        else
                        {
                            _cadenceSlotImages[i].color = new Color(1f, 1f, 1f, 0f);
                        }
                    }
                }
            }

            if (sensor != null && _pulseFill != null)
            {
                float pulseAge = Time.unscaledTime - sensor.LastPulseTime;
                float val = pulseAge < 0.20f ? Mathf.Max(sensor.CurrentLiveLevel, sensor.LastPulseIntensity) : sensor.CurrentLiveLevel;
                val = Mathf.Clamp01(val);
                _pulseFill.sizeDelta = new Vector2(BarWidth * val, 6f);
                _pulseFillImg.color = val >= sensor.hardBoomThreshold
                    ? new Color(0.96f, 0.32f, 0.26f, 0.92f)
                    : new Color(0.95f, 0.80f, 0.42f, 0.85f);

                string mode = "Keyboard & mouse   ·   Tab to change";
                if (sensor.sourceMode == SensorSourceMode.KeyboardPlusPCMicrophone)
                    mode = "Microphone   ·   Tab to change";
                else if (sensor.sourceMode == SensorSourceMode.KeyboardPlusESP32Serial)
                    mode = sensor.SerialConnected
                        ? "Sensor connected   ·   Tab to change"
                        : "Sensor disconnected   ·   Tab to change";
                _sensorModeLabel.text = mode;
            }

            // 3. Guardian Rhythm Bar vs Subtle Chamber Banner
            CerberusRhythmBoss boss = CerberusRhythmBoss.ActiveBoss;
            bool bossActive = boss != null && boss.IsBattleActive && !boss.IsDefeated;
            if (_bossBarRoot != null) _bossBarRoot.SetActive(bossActive);

            if (bossActive)
            {
                _bossNameText.text = boss.bossDisplayName;
                float bpct = Mathf.Clamp01(boss.CurrentHealth / Mathf.Max(1f, boss.maxHealth));
                _bossHpFill.sizeDelta = new Vector2(BossBarWidth * bpct, 10f);
                if (_bossHealthValue != null) _bossHealthValue.text = Mathf.CeilToInt(boss.CurrentHealth) + " / " + Mathf.CeilToInt(boss.maxHealth);
                _bossJudgementText.text = boss.LastJudgement;
                if (_bannerGroup != null) _bannerGroup.alpha = 0f;
            }
            else if (_bannerGroup != null)
            {
                if (_bannerTimer > 0f)
                {
                    _bannerTimer -= dt;
                    _bannerGroup.alpha = Mathf.MoveTowards(_bannerGroup.alpha, Mathf.Clamp01(_bannerTimer * 1.4f), dt * 3f);
                }
                else
                {
                    _bannerGroup.alpha = Mathf.MoveTowards(_bannerGroup.alpha, 0f, dt * 2f);
                }
            }

            if (_calloutText != null)
            {
                if (_calloutTimer > 0f) _calloutTimer -= dt;
                Color cc = _calloutText.color;
                cc.a = Mathf.Clamp01(_calloutTimer * 1.6f) * 0.84f;
                _calloutText.color = cc;
            }


            // 4. Eurydice Soul-Tether Abduction Danger, Whisper & Look-Back
            bool escorting = UnderworldJourney.HasEurydice && _eurydice != null;
            if (_abductionRoot != null)
            {
                bool showAbduct = escorting && player != null
                    && (_eurydice.IsBeingAbducted || _eurydice.AbductionDanger01 > 0.02f) && !player.IsGameOver && !player.IsVictory;
                _abductionRoot.SetActive(showAbduct);
                if (showAbduct)
                {
                    var warningRect = (RectTransform)_abductionRoot.transform;
                    warningRect.anchoredPosition = new Vector2(0f, _subtitleRoot.activeSelf ? ((RectTransform)_subtitleRoot.transform).sizeDelta.y + 76f : 102f);
                    _abductionFill.sizeDelta = new Vector2(360f * Mathf.Clamp01(_eurydice.AbductionDanger01), 3f);
                    float pulse = 0.72f + Mathf.PingPong(Time.unscaledTime * 3.5f, 0.28f);
                    _abductionWarnText.color = new Color(1.0f, 0.42f, 0.50f, pulse);
                }
            }

            if (_whisperText != null)
            {
                bool showWhisper = escorting && _eurydice.InTemptationZone && player != null && !player.IsGameOver && !player.IsVictory && !_subtitleRoot.activeSelf;
                if (showWhisper) _whisperText.text = _eurydice.ActiveWhisper;
                Color wc = _whisperText.color;
                wc.a = escorting ? Mathf.MoveTowards(wc.a, showWhisper ? 0.78f : 0f, dt * 2.5f) : 0f;
                _whisperText.color = wc;
            }

            if (_lookBackRoot != null)
            {
                bool looking = escorting && player != null && player.LookBackProgress01 > 0.01f && !player.IsGameOver && !player.IsVictory;
                _lookBackRoot.SetActive(looking);
                if (looking) _lookBackFill.sizeDelta = new Vector2(320f * player.LookBackProgress01, 3f);
            }

            // 5. Cadence Unlock Ceremony & Screen Wipe & End Screen
            if (_cadenceUnlockRoot != null && _cadenceUnlockGroup != null)
            {
                if (_cadenceUnlockTimer > 0f)
                {
                    _cadenceUnlockTimer -= dt;
                    _cadenceUnlockGroup.alpha = Mathf.MoveTowards(_cadenceUnlockGroup.alpha, 1f, dt * 4f);
                }
                else if (_cadenceUnlockGroup.alpha > 0f)
                {
                    _cadenceUnlockGroup.alpha = Mathf.MoveTowards(_cadenceUnlockGroup.alpha, 0f, dt * 2.5f);
                    if (_cadenceUnlockGroup.alpha <= 0.01f)
                    {
                        _cadenceUnlockRoot.SetActive(false);
                    }
                }
            }

            if (_endScreenRoot != null && player != null)
            {
                if (player.IsGameOver)
                {
                    _endScreenRoot.SetActive(true);
                    bool looked = player.GameOverReason.Contains("turned back");
                    bool abducted = player.GameOverReason.Contains("Mourning Shades") || player.GameOverReason.Contains("dragged");

                    _endScreenBg.color = (looked || abducted)
                        ? new Color(0.03f, 0.02f, 0.06f, 0.82f)
                        : new Color(0.08f, 0.01f, 0.03f, 0.82f);

                    _endTitleText.text = looked ? "Vow Broken" : (abducted ? "Tether Severed" : "Silenced in Tartarus");
                    _endBodyText.text = player.GameOverReason + "\n\nPress [R] to descend anew.";
                }
                else if (player.IsVictory)
                {
                    _endScreenRoot.SetActive(true);
                    _endScreenBg.color = new Color(0.10f, 0.08f, 0.03f, 0.82f);
                    _endTitleText.text = "Into the Upper Light";
                    _endBodyText.text = "\"The dawn... Orpheus, look! The golden sun strikes the marble columns! We made it... we are alive!\"\n\nYou descended alone to the deepest throne and found Eurydice.\nTogether you climbed through the underworld, trusting her voice behind you.\nIn the warm light of day, she steps beside you — whole and alive.\n\nPress [R] to play again.";
                }
            }
        }

        private void UpdateTutorialModal(Keyboard kb, Gamepad pad, OrpheusPlayerController player)
        {
            if (!IsTutorialModalActive) return;

            if (player != null && (player.IsGameOver || player.IsVictory))
            {
                DismissTutorialModal();
                return;
            }

            if (_tutorialContinuePrompt != null)
            {
                _tutorialContinuePrompt.text = pad != null ? "A / Enter to continue" : "Enter to continue";
            }

            bool enterPressed = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                             || (pad != null && pad.buttonSouth.wasPressedThisFrame);

            if (enterPressed)
            {
                DismissTutorialModal();
            }
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        private static void CreateBarBg(RectTransform parent, Vector2 pos, Vector2 size, Color col)
        {
            RectTransform rt = CreateRect("BarBg", parent, Vector2.zero, Vector2.zero, Vector2.zero, pos, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
        }

        private static RectTransform CreateBarFill(RectTransform parent, string name, Vector2 pos, Vector2 size, Color col, out Image img)
        {
            RectTransform rt = CreateRect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, pos, size);
            img = rt.gameObject.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
            return rt;
        }

        private Text CreateSerifText(
            string name,
            RectTransform parent,
            Vector2 pos,
            Vector2 size,
            int fontSize,
            FontStyle style,
            TextAnchor align,
            Color col,
            Vector2? anchorMin = null,
            Vector2? anchorMax = null,
            Vector2? pivot = null)
        {
            RectTransform rt = CreateRect(
                name, parent,
                anchorMin ?? Vector2.zero,
                anchorMax ?? Vector2.zero,
                pivot ?? Vector2.zero,
                pos, size);

            Text txt = rt.gameObject.AddComponent<Text>();
            txt.font = _serifFont;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = col;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            Shadow sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.72f);
            sh.effectDistance = new Vector2(1.2f, -1.2f);

            return txt;
        }
    }
}
