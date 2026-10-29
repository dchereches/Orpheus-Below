using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrpheusBelow
{
    /// <summary>Screen-sized combat typography anchored to the painted enemy, independent of camera zoom.</summary>
    public sealed class EnemyCombatFeedback : MonoBehaviour
    {
        private static EnemyCombatFeedback _instance;
        private RectTransform _canvas;
        private readonly List<Vitals> _vitals = new List<Vitals>();
        private readonly List<Number> _numbers = new List<Number>();
        private sealed class Vitals
        {
            public ShadeEnemy enemy;
            public RectTransform root, fill;
            public Text label;
            public float width;
        }
        private sealed class Number
        {
            public Transform target;
            public Vector3 world;
            public Text text;
            public Color color;
            public float age, duration, side;
        }
        private static EnemyCombatFeedback Ensure()
        {
            if (_instance != null) return _instance;
            var root = new GameObject("EnemyCombatFeedbackCanvas", typeof(RectTransform));
            _instance = root.AddComponent<EnemyCombatFeedback>();
            Canvas canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            _instance._canvas = root.GetComponent<RectTransform>();
            return _instance;
        }
        public static void Track(ShadeEnemy enemy)
        {
            if (enemy == null) return;
            var owner = Ensure();
            foreach (Vitals existing in owner._vitals) if (existing.enemy == enemy) return;
            float width = enemy.IsMiniBoss ? 134f : 96f;
            RectTransform root = Rect("EnemyHealth", owner._canvas, new Vector2(width + 4, 34));
            Image frame = root.gameObject.AddComponent<Image>(); frame.color = new Color(.5f, .39f, .23f, .95f); frame.raycastTarget = false;
            // A small plate, with the bar separated from its numerical readout.
            root.sizeDelta = new Vector2(width + 4, 10);
            var bg = Rect("HealthTrack", root, new Vector2(width, 6)); bg.gameObject.AddComponent<Image>().color = new Color(.12f, .06f, .05f, 1);
            var fill = Rect("HealthFill", root, new Vector2(width, 6));
            fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0, .5f); fill.anchoredPosition = new Vector2(2, 0);
            fill.gameObject.AddComponent<Image>().color = enemy.hasBronzeShield ? GameTypography.Gold : new Color(.75f, .21f, .18f, 1);
            Text label = Text("EnemyHealthValue", root, 18, new Vector2(width + 40, 24));
            label.rectTransform.anchoredPosition = new Vector2(0, -20);
            label.text = Mathf.CeilToInt(enemy.CurrentHealth) + " / " + Mathf.CeilToInt(enemy.maxHealth);
            owner._vitals.Add(new Vitals { enemy = enemy, root = root, fill = fill, label = label, width = width });
        }
        public static void Damage(Transform target, float applied, bool heavy = false, bool shielded = false)
        {
            if (target == null || applied <= .001f || float.IsNaN(applied) || float.IsInfinity(applied)) return;
            var owner = Ensure();
            if (owner._numbers.Count >= 40)
            {
                Destroy(owner._numbers[0].text.gameObject); owner._numbers.RemoveAt(0);
            }
            int stack = 0;
            foreach (Number number in owner._numbers) if (number.target == target && number.age < .24f) stack++;
            Color color = shielded ? new Color(.78f, .72f, .59f) : heavy ? new Color(1f, .78f, .38f) : GameTypography.Ivory;
            Text label = Text("EnemyDamageNumber", owner._canvas, heavy ? 38 : 30, new Vector2(180, 60));
            label.text = applied < 1 ? applied.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) : Mathf.RoundToInt(applied).ToString();
            label.color = color;
            owner._numbers.Add(new Number { target = target, world = Top(target), text = label, color = color,
                duration = heavy ? 1.05f : .85f, side = (stack % 3 - 1) * 24f });
        }
        private static Vector3 Top(Transform target)
        {
            var art = target.GetComponent<PaintedEnemyActor>();
            if (art != null && art.Art != null && art.Art.IsReady)
                return art.Art.transform.TransformPoint(Vector3.up * art.Art.Library.visibleHeight);
            return target.position + UnderworldCamera2D.CurrentBillboardRotation * Vector3.up * 2f;
        }
        private bool Place(RectTransform rect, Camera camera, Vector3 world, Vector2 offset)
        {
            Vector3 point = camera.WorldToScreenPoint(world);
            bool visible = point.z > 0 && point.x > -100 && point.x < camera.pixelWidth + 100 && point.y > -100 && point.y < camera.pixelHeight + 100;
            rect.gameObject.SetActive(visible);
            if (!visible) return false;
            Canvas canvas = _canvas.GetComponent<Canvas>();
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, point, uiCamera, out Vector2 local);
            rect.anchoredPosition = local + offset;
            return true;
        }
        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            bool blocked = UnderworldFrontEnd.BlocksMenusOrTransitions;
            for (int i = _vitals.Count - 1; i >= 0; i--)
            {
                Vitals value = _vitals[i];
                if (value.enemy == null) { Destroy(value.root.gameObject); _vitals.RemoveAt(i); continue; }
                bool visible = !blocked && value.enemy.gameObject.activeInHierarchy && !value.enemy.IsPacified && !value.enemy.IsFleeing && value.enemy.CurrentHealth > 0;
                value.root.gameObject.SetActive(visible);
                if (!visible || !Place(value.root, camera, Top(value.enemy.transform), new Vector2(0, 22))) continue;
                float fraction = Mathf.Clamp01(value.enemy.CurrentHealth / Mathf.Max(1, value.enemy.maxHealth));
                value.fill.sizeDelta = new Vector2(value.width * fraction, 6);
                value.fill.GetComponent<Image>().color = value.enemy.hasBronzeShield ? GameTypography.Gold : new Color(.75f, .21f, .18f, 1);
                value.label.text = Mathf.CeilToInt(value.enemy.CurrentHealth) + " / " + Mathf.CeilToInt(value.enemy.maxHealth);
            }
            float dt = UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive ? 0f : Time.deltaTime;
            for (int i = _numbers.Count - 1; i >= 0; i--)
            {
                Number number = _numbers[i]; number.age += dt;
                if (number.age >= number.duration) { Destroy(number.text.gameObject); _numbers.RemoveAt(i); continue; }
                if (number.target != null) number.world = Top(number.target);
                number.text.gameObject.SetActive(!blocked);
                if (blocked || !Place(number.text.rectTransform, camera, number.world, new Vector2(number.side, 52 + number.age * 48))) continue;
                float fade = Mathf.Clamp01((number.duration - number.age) / .3f);
                Color color = number.color; color.a = fade; number.text.color = color;
                number.text.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, Mathf.Clamp01(number.age / .12f));
            }
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.sizeDelta = size;
            return rect;
        }
        private static Text Text(string name, Transform parent, int size, Vector2 bounds)
        {
            var text = Rect(name, parent, bounds).gameObject.AddComponent<Text>();
            GameTypography.Apply(text, size); text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            GameTypography.Shadow(text); return text;
        }
        private void OnDestroy() { if (_instance == this) _instance = null; }
    }
}
