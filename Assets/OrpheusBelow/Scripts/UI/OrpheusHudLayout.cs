using UnityEngine;
using UnityEngine.UI;

namespace OrpheusBelow
{
    public partial class OrpheusHud
    {
        private Text _healthValue, _breathValue, _bossHealthValue;

        private void Frame(RectTransform root, float opacity = .86f)
        {
            var panel = root.gameObject.AddComponent<Image>();
            panel.color = new Color(.045f, .036f, .033f, opacity); panel.raycastTarget = false;
            Edge(root, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 1), .65f);
            Edge(root, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 1), .65f);
            Edge(root, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(1, 0), .45f);
            Edge(root, new Vector2(1, 0), new Vector2(1, 1), Vector2.zero, new Vector2(1, 0), .45f);
        }
        private void Edge(RectTransform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size, float alpha)
        {
            var rt = CreateRect("BronzeRule", parent, min, max, new Vector2(.5f, .5f), position, size);
            var line = rt.gameObject.AddComponent<Image>();
            line.color = new Color(.78f, .62f, .37f, alpha); line.raycastTarget = false;
        }
        private Text PanelText(string name, RectTransform root, Vector2 position, Vector2 size, int fontSize, string text,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            Text label = CreateSerifText(name, root, position, size, fontSize, FontStyle.Normal, alignment, GameTypography.Ivory);
            label.text = text; return label;
        }

        private void BuildVitalsPanel(RectTransform root)
        {
            RectTransform bl = CreateRect("BottomLeftCluster", root, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(32, 34), new Vector2(390, 170));
            _vitalsGroup = bl.gameObject.AddComponent<CanvasGroup>(); Frame(bl);
            PanelText("VitalityLabel", bl, new Vector2(24, 132), new Vector2(140, 28), 24, "ORPHEUS");
            _healthValue = PanelText("PlayerHealthValue", bl, new Vector2(174, 132), new Vector2(150, 28), 23, "100 / 100", TextAnchor.MiddleRight);
            PanelText("DashLabel", bl, new Vector2(333, 133), new Vector2(46, 22), 13, "DASH", TextAnchor.MiddleCenter).color = GameTypography.Gold;
            CreateBarBg(bl, new Vector2(22, 110), new Vector2(304, 16), new Color(.54f, .42f, .25f, .9f));
            CreateBarBg(bl, new Vector2(24, 112), new Vector2(BarWidth, 12), new Color(.15f, .07f, .07f, 1f));
            _hpFill = CreateBarFill(bl, "HpFill", new Vector2(24, 112), new Vector2(BarWidth, 12), new Color(.72f, .19f, .21f, 1f), out _);
            _dashGems = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var gem = CreateRect("DashGem_" + i, bl, Vector2.zero, Vector2.zero, Vector2.one * .5f,
                    new Vector2(344 + i * 22, 118), new Vector2(11, 11));
                gem.localRotation = Quaternion.Euler(0, 0, 45);
                _dashGems[i] = gem.gameObject.AddComponent<Image>(); _dashGems[i].color = GameTypography.Gold;
            }
            PanelText("PulseLabel", bl, new Vector2(24, 82), new Vector2(150, 22), 18, "Pulse").color = GameTypography.Muted;
            _breathValue = PanelText("BreathValue", bl, new Vector2(174, 82), new Vector2(150, 22), 18, "Breath 100", TextAnchor.MiddleRight);
            CreateBarBg(bl, new Vector2(24, 70), new Vector2(BarWidth, 6), new Color(.20f, .17f, .12f, 1));
            _pulseFill = CreateBarFill(bl, "PulseFill", new Vector2(24, 70), new Vector2(0, 6), GameTypography.Gold, out _pulseFillImg);
            var threshold = CreateRect("FermataThresholdMarker", bl, Vector2.zero, Vector2.zero, Vector2.one * .5f,
                new Vector2(24 + BarWidth * .6f, 73), new Vector2(18, 18));
            _fermataThresholdMarker = threshold.gameObject.AddComponent<Image>();
            _fermataThresholdMarker.sprite = _fermataSprite;
            _fermataThresholdMarker.color = GameTypography.Ivory;
            _fermataThresholdMarker.enabled = _fermataSprite != null;

            var slots = CreateRect("CadenceImageSlots", bl, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(24, 32), new Vector2(86, 24));
            _cadenceSlotContainer = slots.gameObject; _cadenceSlotImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = CreateRect("CadenceSlot_" + i, slots, Vector2.zero, Vector2.zero, Vector2.zero,
                    new Vector2(i * 28, 0), new Vector2(24, 24));
                _cadenceSlotImages[i] = slot.gameObject.AddComponent<Image>(); _cadenceSlotImages[i].color = Color.clear;
            }
            _cadenceLabel = PanelText("CadenceText", bl, new Vector2(118, 32), new Vector2(254, 24), 17, "Cadence");
            _sensorModeLabel = PanelText("SensorModeText", bl, new Vector2(24, 8), new Vector2(348, 18), 14, "Keyboard & mouse   ·   Tab to change");
            _sensorModeLabel.color = GameTypography.Muted;
        }

        private void BuildJourneyPanel(RectTransform root)
        {
            var panel = CreateRect("JourneyPanel", root, Vector2.up, Vector2.up, Vector2.up,
                new Vector2(32, -32), new Vector2(440, 128));
            Frame(panel, .80f);
            _journeyLabel = CreateSerifText("JourneyProgress", panel, new Vector2(22, -12), new Vector2(396, 32),
                25, FontStyle.Normal, TextAnchor.MiddleLeft, GameTypography.Gold, Vector2.up, Vector2.up, Vector2.up);
            _journeyObjective = CreateSerifText("JourneyObjective", panel, new Vector2(22, -52), new Vector2(396, 64),
                20, FontStyle.Normal, TextAnchor.UpperLeft, GameTypography.Ivory, Vector2.up, Vector2.up, Vector2.up);
            _journeyLabel.text = UnderworldJourney.ProgressLabel; _journeyObjective.text = UnderworldJourney.Objective;
            _navigationLegend = CreateSerifText("NavigationLegend", root, new Vector2(34, -178), new Vector2(660, 28),
                16, FontStyle.Normal, TextAnchor.UpperLeft, GameTypography.Ivory, Vector2.up, Vector2.up, Vector2.up);
            _navigationLegend.text = "Green: walkable     Red: blocked     Cyan: exit"; _navigationLegend.gameObject.SetActive(false);
        }

        private void BuildGuardianPanel(RectTransform root)
        {
            var panel = CreateRect("GuardianBarCluster", root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1),
                new Vector2(0, -32), new Vector2(640, 112));
            Frame(panel, .92f); _bossBarRoot = panel.gameObject;
            _bossNameText = CreateSerifText("GuardianName", panel, new Vector2(0, -12), new Vector2(592, 32), 26,
                FontStyle.Normal, TextAnchor.MiddleCenter, GameTypography.Ivory,
                new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1));
            CreateBarBg(panel, new Vector2(22, 48), new Vector2(596, 14), new Color(.51f, .39f, .24f));
            CreateBarBg(panel, new Vector2(24, 50), new Vector2(BossBarWidth, 10), new Color(.19f, .09f, .08f));
            _bossHpFill = CreateBarFill(panel, "BossHpFill", new Vector2(24, 50), new Vector2(BossBarWidth, 10), GameTypography.Gold, out _);
            _bossHealthValue = PanelText("GuardianHealthValue", panel, new Vector2(24, 16), new Vector2(190, 22), 18, "");
            _bossJudgementText = PanelText("RhythmJudgement", panel, new Vector2(220, 16), new Vector2(396, 22), 18, "", TextAnchor.MiddleRight);
            _bossBarRoot.SetActive(false);
        }

        private void BuildSubtitlePanel(RectTransform root)
        {
            var panel = CreateRect("SubtitleCluster", root, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(0, 56), new Vector2(1040, 172));
            _subtitleRoot = panel.gameObject; Frame(panel, .90f);
            _subtitleGroup = panel.gameObject.AddComponent<CanvasGroup>(); _subtitleGroup.alpha = 0; _subtitleGroup.blocksRaycasts = false;
            _subtitleSpeakerText = CreateSerifText("SubSpeaker", panel, new Vector2(0, -14), new Vector2(1000, 26), 18,
                FontStyle.Normal, TextAnchor.MiddleCenter, GameTypography.Gold,
                new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1));
            _subtitleBodyText = CreateSerifText("SubBody", panel, new Vector2(0, -44), new Vector2(1000, 68), 28,
                FontStyle.Normal, TextAnchor.UpperCenter, GameTypography.Ivory,
                new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1));
            _subtitleBodyText.lineSpacing = 1.15f;

            BuildSubtitleSymbolsTray(panel);

            _subtitleRoot.SetActive(false);
        }

        private void BuildSubtitleSymbolsTray(RectTransform panel)
        {
            var trayGo = new GameObject("SubtitleSymbolsTray");
            var trayRt = trayGo.AddComponent<RectTransform>();
            trayRt.SetParent(panel, false);
            trayRt.anchorMin = new Vector2(0.5f, 0f);
            trayRt.anchorMax = new Vector2(0.5f, 0f);
            trayRt.pivot = new Vector2(0.5f, 0f);
            trayRt.anchoredPosition = new Vector2(0f, 12f);
            trayRt.sizeDelta = new Vector2(1000f, 40f);
            _subtitleSymbolsRoot = trayGo;

            var bgImg = trayGo.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.03f, 0.05f, 0.70f);
            bgImg.raycastTarget = false;

            // Soft note badge: Quaver icon + label
            var quaverIconGo = new GameObject("SubQuaverIcon");
            var qIconRt = quaverIconGo.AddComponent<RectTransform>();
            qIconRt.SetParent(trayRt, false);
            qIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            qIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            qIconRt.pivot = new Vector2(0.5f, 0.5f);
            qIconRt.anchoredPosition = new Vector2(-410f, 0f);
            qIconRt.sizeDelta = new Vector2(26f, 26f);
            _subQuaverIcon = quaverIconGo.AddComponent<Image>();
            _subQuaverIcon.sprite = _quaverSprite;
            _subQuaverIcon.color = new Color(1f, 0.94f, 0.52f, 1f);
            _subQuaverIcon.preserveAspect = true;
            _subQuaverIcon.raycastTarget = false;

            _subQuaverLabel = CreateSerifText("SubQuaverLabel", trayRt, new Vector2(-310f, 0f), new Vector2(170f, 26f),
                15, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.96f, 0.92f, 0.82f, 0.95f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));
            _subQuaverLabel.text = "Soft Quaver  [J / LMB]";

            // Boom note badge: Fermata icon + label
            var fermataIconGo = new GameObject("SubFermataIcon");
            var fIconRt = fermataIconGo.AddComponent<RectTransform>();
            fIconRt.SetParent(trayRt, false);
            fIconRt.anchorMin = new Vector2(0.5f, 0.5f);
            fIconRt.anchorMax = new Vector2(0.5f, 0.5f);
            fIconRt.pivot = new Vector2(0.5f, 0.5f);
            fIconRt.anchoredPosition = new Vector2(-190f, 0f);
            fIconRt.sizeDelta = new Vector2(30f, 20f);
            _subFermataIcon = fermataIconGo.AddComponent<Image>();
            _subFermataIcon.sprite = _fermataSprite;
            _subFermataIcon.color = new Color(1f, 0.84f, 0.22f, 1f);
            _subFermataIcon.preserveAspect = true;
            _subFermataIcon.raycastTarget = false;

            _subFermataLabel = CreateSerifText("SubFermataLabel", trayRt, new Vector2(-85f, 0f), new Vector2(180f, 26f),
                15, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.96f, 0.92f, 0.82f, 0.95f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));
            _subFermataLabel.text = "Boom Fermata  [K / RMB]";

            // Aulos Stave container
            var staveGo = new GameObject("SubStaveContainer");
            var staveRt = staveGo.AddComponent<RectTransform>();
            staveRt.SetParent(trayRt, false);
            staveRt.anchorMin = new Vector2(0.5f, 0.5f);
            staveRt.anchorMax = new Vector2(0.5f, 0.5f);
            staveRt.pivot = new Vector2(0.5f, 0.5f);
            staveRt.anchoredPosition = new Vector2(230f, 0f);
            staveRt.sizeDelta = new Vector2(480f, 36f);
            _subStaveRow = staveGo;

            _subStavePrefix = CreateSerifText("SubStavePrefix", staveRt, new Vector2(-160f, 0f), new Vector2(130f, 26f),
                15, FontStyle.Italic, TextAnchor.MiddleRight, new Color(0.85f, 0.78f, 0.60f, 0.85f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));
            _subStavePrefix.text = "·  Aulos Stave:";

            _subStaveSlotImages = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var slotGo = new GameObject("SubStaveSlot_" + i);
                var slotRt = slotGo.AddComponent<RectTransform>();
                slotRt.SetParent(staveRt, false);
                slotRt.anchorMin = new Vector2(0.5f, 0.5f);
                slotRt.anchorMax = new Vector2(0.5f, 0.5f);
                slotRt.pivot = new Vector2(0.5f, 0.5f);
                slotRt.anchoredPosition = new Vector2(-75f + i * 28f, 0f);
                slotRt.sizeDelta = new Vector2(24f, 24f);

                _subStaveSlotImages[i] = slotGo.AddComponent<Image>();
                _subStaveSlotImages[i].sprite = _quaverSprite;
                _subStaveSlotImages[i].color = new Color(1f, 0.88f, 0.25f, 0.95f);
                _subStaveSlotImages[i].preserveAspect = true;
                _subStaveSlotImages[i].raycastTarget = false;
            }

            _subStaveSuffix = CreateSerifText("SubStaveSuffix", staveRt, new Vector2(110f, 0f), new Vector2(190f, 26f),
                15, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.95f, 0.85f, 0.55f, 0.95f),
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f));
            _subStaveSuffix.text = "→  Cataclysmic Requiem!";

            _subtitleSymbolsRoot.SetActive(false);
        }
    }
}
