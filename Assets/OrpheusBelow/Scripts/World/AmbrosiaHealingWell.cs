using UnityEngine;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    /// <summary>
    /// Interactive Ambrosia Fountain / Nectar Well stamped into each chamber:
    ///   - Allows Orpheus to drink once per chamber for +35 HP restoration.
    ///   - Beautiful Hellenic marble font with a radiant golden nectar pool.
    ///   - Floating classical Greek serif prompt: [E] Drink Ambrosia (+35 HP).
    ///   - Triggers radiant golden shockwave and lyre chime on consumption.
    /// </summary>
    public class AmbrosiaHealingWell : MonoBehaviour
    {
        public const float HealAmount = 35f;
        public const float InteractionRadius = 2.4f;

        public bool isConsumed { get; private set; }

        private GameObject _fontGo;
        private GameObject _promptRoot;
        private UnityEngine.UI.Text _promptText;
        private Canvas _promptCanvas;
        private SpriteRenderer _nectarDecal;
        private SpriteRenderer _wellSprite;
        private float _pulseTimer;

        public static AmbrosiaHealingWell Spawn(Transform parent, Vector3 worldPos)
        {
            GameObject go = new GameObject("AmbrosiaHealingWell");
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            return go.AddComponent<AmbrosiaHealingWell>();
        }

        private void Awake()
        {
            BuildWellVisuals();
            BuildFloatingPrompt();
        }

        private void BuildWellVisuals()
        {
            // 1. Ground Golden Nectar Pool Decal
            GameObject poolGo = new GameObject("NectarPoolDecal");
            poolGo.transform.SetParent(transform, false);
            poolGo.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            poolGo.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
            poolGo.transform.rotation = RenderPipeline2DSetup.GroundDecalRotation;

            _nectarDecal = poolGo.AddComponent<SpriteRenderer>();
            _nectarDecal.sprite = RenderPipeline2DSetup.SoftGlowSprite;
            _nectarDecal.material = RenderPipeline2DSetup.Get2DSpriteMaterial();
            _nectarDecal.color = new Color(2.6f, 2.0f, 0.45f, 0.85f);
            _nectarDecal.sortingOrder = 5;

            // 2. Ornate Marble Pedestal & Basin (Billboard Sprite)
            _fontGo = new GameObject("MarbleBasin");
            _fontGo.transform.SetParent(transform, false);
            _fontGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            _fontGo.transform.localScale = new Vector3(1.35f, 1.70f, 1f);

            _wellSprite = RenderPipeline2DSetup.AttachBillboardSprite(_fontGo, RenderPipeline2DSetup.DiamondSprite, new Color(0.92f, 0.88f, 0.78f, 0.95f), 15);

            // 3. Subtle physical collider so Orpheus slides smoothly around the basin
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.size = new Vector3(1.2f, 1.5f, 1.2f);
            gameObject.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;
        }

        private void BuildFloatingPrompt()
        {
            _promptRoot = new GameObject("PromptCanvas", typeof(RectTransform), typeof(Canvas));
            _promptRoot.transform.SetParent(transform, false);
            _promptRoot.transform.localPosition = new Vector3(0f, 2.1f, 0f);

            _promptCanvas = _promptRoot.GetComponent<Canvas>();
            _promptCanvas.renderMode = RenderMode.WorldSpace;
            _promptCanvas.sortingOrder = 200;

            RectTransform rt = _promptCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(3.6f, 0.85f);
            rt.localScale = Vector3.one * 0.012f;

            GameObject textGo = new GameObject("PromptText", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textGo.transform.SetParent(_promptRoot.transform, false);

            _promptText = textGo.GetComponent<UnityEngine.UI.Text>();
            _promptText.font = Font.CreateDynamicFontFromOSFont(new[] { "Palatino Linotype", "Georgia", "Times New Roman" }, 32);
            if (_promptText.font == null) _promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _promptText.fontSize = 28;
            _promptText.fontStyle = FontStyle.Normal;
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.color = new Color(2.8f, 2.4f, 1.2f, 1.0f);
            _promptText.text = "[E] Drink Ambrosia (+35 HP)";

            UnityEngine.UI.Shadow shadow = textGo.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);

            _promptRoot.SetActive(false);
        }

        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay) return;

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory)
            {
                if (_promptRoot.activeSelf) _promptRoot.SetActive(false);
                return;
            }

            // Gentle ambient pulsation of the golden nectar
            _pulseTimer += Time.deltaTime * 2.8f;
            if (!isConsumed && _nectarDecal != null)
            {
                float glow = 0.70f + Mathf.Sin(_pulseTimer) * 0.20f;
                _nectarDecal.color = new Color(2.6f * glow, 2.0f * glow, 0.45f * glow, 0.85f);
            }

            Vector3 diff = player.transform.position - transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;

            if (dist <= InteractionRadius && !isConsumed)
            {
                if (!_promptRoot.activeSelf) _promptRoot.SetActive(true);

                Keyboard kb = Keyboard.current;
                Gamepad pad = Gamepad.current;
                bool interact = (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                             || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame))
                             || Xbox360Input.InteractPressed;

                if (interact)
                {
                    ConsumeAmbrosia(player);
                }
            }
            else
            {
                if (_promptRoot.activeSelf) _promptRoot.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            Quaternion rot = UnderworldCamera2D.CurrentBillboardRotation;
            if (_fontGo != null) _fontGo.transform.rotation = rot;
            if (_promptRoot != null && _promptRoot.activeSelf) _promptRoot.transform.rotation = rot;
        }

        private void ConsumeAmbrosia(OrpheusPlayerController player)
        {
            isConsumed = true;
            player.Heal(HealAmount);

            // Audio & radiant VFX
            ProceduralLyreAudio.Instance?.PlayCadenceUnlockFanfare();
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.08f, 0f), 3.4f,
                new Color(2.8f, 2.2f, 0.55f, 1f), 0.45f);

            if (UnderworldJourney.HasEurydice)
            {
                OrpheusHud.Instance?.ShowSubtleCallout("Eurydice: \"A moment of rest... The nectar smells of sunlight and wild thyme.\"", 3.2f);
            }
            else
            {
                OrpheusHud.Instance?.ShowSubtleCallout("+35 HP  ·  Nectar of the Gods", 2.2f);
            }

            // Depleted well appearance
            if (_nectarDecal != null)
            {
                _nectarDecal.color = new Color(0.40f, 0.45f, 0.50f, 0.40f);
            }
            if (_wellSprite != null)
            {
                _wellSprite.color = new Color(0.65f, 0.65f, 0.68f, 0.85f);
            }

            if (_promptRoot != null) _promptRoot.SetActive(false);
        }
    }
}
