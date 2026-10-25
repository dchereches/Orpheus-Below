using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    /// <summary>
    /// 2.5D Isometric HADES HUD & Chamber Transition Overlay:
    ///   - Vitality Bar + Double-Dash Charge Diamonds (◆◆)
    ///   - Live Piezo / Breath / Mouse Charge Meter with 60% HARD BOOM marker
    ///   - Active Isometric Chamber Title + Remaining Shades Counter / Cerberus Boss Bar
    ///   - Smooth Hades Black-Fade between chambers + Game Over / Sunlight Victory screens
    /// </summary>
    public class OrpheusHud : MonoBehaviour
    {
        public static OrpheusHud Instance { get; private set; }

        public bool showControlsHelp = true;

        private Texture2D _flatTex;
        private string _bannerText = "";
        private float _bannerTimer;
        private BlurredEurydice _eurydice;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _centerBannerStyle;
        private GUIStyle _whisperStyle;
        private GUIStyle _bigOverlayTitle;
        private GUIStyle _bigOverlaySub;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _flatTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _flatTex.SetPixel(0, 0, Color.white);
            _flatTex.Apply();
        }

        private void Start()
        {
            _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();
        }

        private void Update()
        {
            if (_bannerTimer > 0f) _bannerTimer -= Time.unscaledDeltaTime;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame)
            {
                showControlsHelp = !showControlsHelp;
            }
        }

        public void ShowBanner(string text, float duration = 3.0f)
        {
            _bannerText = text;
            _bannerTimer = duration;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };
            _titleStyle.normal.textColor = new Color(0.98f, 0.88f, 0.62f, 1f);

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
            _labelStyle.normal.textColor = new Color(0.92f, 0.90f, 0.95f, 1f);

            _centerBannerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _centerBannerStyle.normal.textColor = new Color(1.0f, 0.88f, 0.48f, 1f);

            _whisperStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _whisperStyle.normal.textColor = new Color(0.76f, 0.93f, 1.0f, 1f);

            _bigOverlayTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            _bigOverlaySub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true
            };
            _bigOverlaySub.normal.textColor = new Color(0.92f, 0.89f, 0.94f, 1f);
        }

        private void DrawRect(Rect r, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(r, _flatTex);
            GUI.color = prev;
        }

        private void OnGUI()
        {
            EnsureStyles();

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            SensorPulseInput sensor = SensorPulseInput.Instance;
            HadesChamberDirector dir = HadesChamberDirector.Instance;
            if (_eurydice == null) _eurydice = Object.FindFirstObjectByType<BlurredEurydice>();

            float sw = Screen.width;
            float sh = Screen.height;

            // --- 1. TOP-LEFT: VITALITY, HADES DASH CHARGES, & PIEZO/BREATH METER ---
            DrawRect(new Rect(18, 16, 364, 158), new Color(0.05f, 0.02f, 0.08f, 0.86f));
            DrawRect(new Rect(18, 16, 364, 2), new Color(0.92f, 0.72f, 0.28f, 0.9f));

            float hpPct = player != null ? Mathf.Clamp01(player.CurrentHealth / Mathf.Max(1f, player.maxHealth)) : 1f;
            GUI.Label(new Rect(30, 21, 340, 22), $"ORPHEUS VITALITY   {(player != null ? Mathf.CeilToInt(player.CurrentHealth) : 100)} / {(player != null ? Mathf.CeilToInt(player.maxHealth) : 100)}", _titleStyle);
            DrawRect(new Rect(30, 43, 336, 13), new Color(0.18f, 0.06f, 0.10f, 1f));
            DrawRect(new Rect(30, 43, 336 * hpPct, 13), new Color(0.92f, 0.18f, 0.28f, 1f));

            // Hades Double-Dash Charges pip readout
            int charges = player != null ? player.CurrentDashCharges : 2;
            int maxCharges = player != null ? player.maxDashCharges : 2;
            string pips = "";
            for (int i = 0; i < maxCharges; i++)
            {
                pips += i < charges ? "<color=#55FFBB>◆</color> " : "<color=#554466>◇</color> ";
            }
            GUI.Label(new Rect(30, 61, 340, 22), $"HADES DASH <b>[Space]</b> :  {pips}", _labelStyle);

            // Piezo / Breath Live Meter
            float live = sensor != null ? Mathf.Clamp01(sensor.CurrentLiveLevel) : 0f;
            float boomThresh = sensor != null ? sensor.hardBoomThreshold : 0.60f;
            float pulseAge = sensor != null ? (Time.unscaledTime - sensor.LastPulseTime) : 99f;
            float displayBar = pulseAge < 0.22f ? Mathf.Max(live, sensor.LastPulseIntensity) : live;

            string lastBadge = "";
            if (pulseAge < 0.75f && sensor != null)
            {
                lastBadge = sensor.LastPulseWasBoom
                    ? $"  <color=#FF5844><b>[BOOM! {Mathf.RoundToInt(sensor.LastPulseIntensity * 100f)}%]</b></color>"
                    : $"  <color=#FFE066>[STRIKE {Mathf.RoundToInt(sensor.LastPulseIntensity * 100f)}%]</color>";
            }

            GUI.Label(new Rect(30, 86, 340, 22), $"PIEZO / BREATH PULSE{lastBadge}", _labelStyle);
            Rect meterBg = new Rect(30, 108, 336, 15);
            DrawRect(meterBg, new Color(0.12f, 0.10f, 0.18f, 1f));

            Color meterCol = displayBar >= boomThresh
                ? new Color(1.0f, 0.30f, 0.22f, 1f)
                : new Color(0.98f, 0.80f, 0.30f, 1f);
            DrawRect(new Rect(meterBg.x, meterBg.y, meterBg.width * displayBar, meterBg.height), meterCol);
            DrawRect(new Rect(meterBg.x + meterBg.width * boomThresh - 1f, meterBg.y - 3f, 3f, meterBg.height + 6f), Color.white);

            string modeStr = "Keyboard / Mouse";
            if (sensor != null)
            {
                if (sensor.sourceMode == SensorSourceMode.KeyboardPlusPCMicrophone)
                    modeStr = "KB/Mouse + <color=#7CFC9A>PC Mic (Tap/Blow!)</color>";
                else if (sensor.sourceMode == SensorSourceMode.KeyboardPlusESP32Serial)
                    modeStr = sensor.SerialConnected
                        ? $"KB/Mouse + <color=#7CFC9A>ESP32 {sensor.serialPortName} LIVE</color>"
                        : $"KB/Mouse + <color=#FF9966>ESP32 {sensor.serialPortName} (Waiting)</color>";
            }
            GUI.Label(new Rect(30, 130, 340, 22), $"Input <b>[Tab]</b>: {modeStr}", _labelStyle);

            // --- 2. TOP-CENTER: CHAMBER STATUS OR CERBERUS BOSS BAR ---
            CerberusRhythmBoss boss = CerberusRhythmBoss.ActiveBoss;
            if (boss != null && boss.IsBattleActive && !boss.IsDefeated)
            {
                float bw = 490f;
                float bx = (sw - bw) * 0.5f;
                DrawRect(new Rect(bx - 14f, 16f, bw + 28f, 84f), new Color(0.08f, 0.02f, 0.06f, 0.88f));
                GUI.Label(new Rect(bx, 20f, bw, 24f), "CERBERUS, GUARDIAN OF STYX", _centerBannerStyle);

                float bhp = Mathf.Clamp01(boss.CurrentHealth / Mathf.Max(1f, boss.maxHealth));
                DrawRect(new Rect(bx, 48f, bw, 16f), new Color(0.20f, 0.05f, 0.08f, 1f));
                DrawRect(new Rect(bx, 48f, bw * bhp, 16f), new Color(0.96f, 0.22f, 0.20f, 1f));

                if (!string.IsNullOrEmpty(boss.LastJudgement))
                {
                    GUI.Label(new Rect(bx - 40f, 68f, bw + 80f, 26f), boss.LastJudgement, _centerBannerStyle);
                }
            }
            else if (dir != null)
            {
                string enemiesTag = dir.ActiveEnemyCount > 0
                    ? $"   <color=#FF7788>[Shades: {dir.ActiveEnemyCount}]</color>"
                    : "   <color=#66FFAA>[EXIT OPEN ↗]</color>";
                DrawRect(new Rect((sw - 490f) * 0.5f, 16f, 490f, 34f), new Color(0.05f, 0.02f, 0.08f, 0.80f));
                GUI.Label(new Rect((sw - 490f) * 0.5f, 20f, 490f, 26f), dir.GetCurrentChamberTitle() + enemiesTag, _centerBannerStyle);
            }

            if (_bannerTimer > 0f && !string.IsNullOrEmpty(_bannerText))
            {
                float alpha = Mathf.Clamp01(_bannerTimer * 1.5f);
                DrawRect(new Rect((sw - 680f) * 0.5f, 108f, 680f, 36f), new Color(0.07f, 0.03f, 0.11f, 0.85f * alpha));
                Color prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect((sw - 680f) * 0.5f, 112f, 680f, 28f), _bannerText, _centerBannerStyle);
                GUI.color = prev;
            }

            // --- 3. TOP-RIGHT: HADES 2.5D CONTROLS LEGEND ([H] TOGGLE) ---
            if (showControlsHelp)
            {
                float hw = 315f;
                float hx = sw - hw - 18f;
                DrawRect(new Rect(hx, 16f, hw, 158f), new Color(0.05f, 0.02f, 0.08f, 0.82f));
                GUI.Label(new Rect(hx + 12f, 20f, hw - 20f, 22f), "HADES 2.5D CONTROLS  <color=#AAAAAA>[H] Hide</color>", _titleStyle);
                GUI.Label(new Rect(hx + 12f, 44f, hw - 20f, 126f),
                    "<b>W / A / S / D</b> : 360° Isometric Run\n" +
                    "<b>Space / Shift</b> : Double-Dash (Crosses Pits!)\n" +
                    "<b>Tap LMB / J</b> : <color=#FFE066>3-Hit Combo / Dash-Strike</color>\n" +
                    "<b>RMB / K / Hold LMB</b> : <color=#FF5844>ORPHIC BOOM!</color>\n" +
                    "<b>Tab</b> : Switch KB / PC Mic / ESP32\n" +
                    "<b>Hold Q</b> : <color=#FF77AA>Look Back</color>  |  <b>R</b> : Restart",
                    _labelStyle);
            }

            // --- 4. BOTTOM: EURYDICE WHISPERS & LOOK-BACK BAR ---
            if (_eurydice != null && _eurydice.InTemptationZone && player != null && !player.IsGameOver && !player.IsVictory)
            {
                DrawRect(new Rect((sw - 680f) * 0.5f, sh - 108f, 680f, 38f), new Color(0.04f, 0.08f, 0.14f, 0.82f));
                GUI.Label(new Rect((sw - 680f) * 0.5f, sh - 104f, 680f, 30f), _eurydice.ActiveWhisper, _whisperStyle);
            }

            if (player != null && player.LookBackProgress01 > 0.01f && !player.IsGameOver)
            {
                float p = player.LookBackProgress01;
                float ww = 420f;
                float wx = (sw - ww) * 0.5f;
                float wy = sh - 62f;
                DrawRect(new Rect(wx - 10f, wy - 26f, ww + 20f, 50f), new Color(0.18f, 0.02f, 0.08f, 0.90f));
                GUI.Label(new Rect(wx, wy - 24f, ww, 24f), "<color=#FF6688>DO NOT LOOK BACK! EURYDICE IS SHARPENING...</color>", _centerBannerStyle);
                DrawRect(new Rect(wx, wy + 2f, ww, 12f), new Color(0.12f, 0.04f, 0.08f, 1f));
                DrawRect(new Rect(wx, wy + 2f, ww * p, 12f), new Color(1.0f, 0.20f, 0.42f, 1f));
            }

            // --- 5. HADES CHAMBER WIPE FADE ---
            if (dir != null && dir.ScreenFadeAlpha > 0.005f)
            {
                DrawRect(new Rect(0, 0, sw, sh), new Color(0.02f, 0.01f, 0.04f, dir.ScreenFadeAlpha));
            }

            // --- 6. END-OF-RUN OVERLAYS ---
            if (player != null && player.IsGameOver)
            {
                bool lookedBack = player.GameOverReason.StartsWith("YOU LOOKED BACK");
                DrawRect(new Rect(0, 0, sw, sh), lookedBack ? new Color(0.04f, 0.02f, 0.07f, 0.74f) : new Color(0.12f, 0.01f, 0.03f, 0.78f));

                _bigOverlayTitle.normal.textColor = lookedBack ? new Color(0.85f, 0.92f, 1.0f, 1f) : new Color(1.0f, 0.28f, 0.32f, 1f);
                GUI.Label(new Rect(0, sh * 0.34f, sw, 56f), lookedBack ? "VOW BROKEN" : "THERE IS NO ESCAPE", _bigOverlayTitle);
                GUI.Label(new Rect((sw - 680f) * 0.5f, sh * 0.44f, 680f, 90f),
                    $"{player.GameOverReason}\n\n<color=#FFE066>Press <b>[R]</b> to fight out of Tartarus again.</color>",
                    _bigOverlaySub);
            }
            else if (player != null && player.IsVictory)
            {
                DrawRect(new Rect(0, 0, sw, sh), new Color(0.14f, 0.10f, 0.03f, 0.80f));
                _bigOverlayTitle.normal.textColor = new Color(1.0f, 0.88f, 0.42f, 1f);
                GUI.Label(new Rect(0, sh * 0.33f, sw, 56f), "INTO THE SUNLIGHT", _bigOverlayTitle);
                GUI.Label(new Rect((sw - 700f) * 0.5f, sh * 0.43f, 700f, 110f),
                    "You fought through the chambers of Tartarus, lulled Cerberus to sleep, and crossed into daylight without ever looking back.\n\n" +
                    "<color=#FFE066>Press <b>[R]</b> to run the chambers again.</color>",
                    _bigOverlaySub);
            }
        }
    }
}
