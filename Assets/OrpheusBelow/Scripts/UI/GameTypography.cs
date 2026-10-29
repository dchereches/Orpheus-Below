using UnityEngine;
using UnityEngine.UI;

namespace OrpheusBelow
{
    /// <summary>One face for HUD, dialogue and combat; the private preview face never enters a player build.</summary>
    public static class GameTypography
    {
        public const string PublicResource = "Typography/CrimsonText-Regular";
        public const string LocalPreviewPath = "Assets/OrpheusBelow/Scripts/Editor/LocalTypography/Mythology.ttf";
        private static Font _font;
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
#if UNITY_EDITOR
                _font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(LocalPreviewPath);
#endif
                if (_font == null) _font = Resources.Load<Font>(PublicResource);
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => _font = null;

        public static readonly Color Ivory = new Color(.97f, .93f, .82f, 1f);
        public static readonly Color Gold = new Color(.78f, .62f, .37f, 1f);
        public static readonly Color Ink = new Color(.045f, .036f, .033f, .92f);
        public static readonly Color Muted = new Color(.72f, .67f, .56f, 1f);

        public static void Apply(Text text, int size = 24)
        {
            text.font = Font; text.fontSize = size; text.fontStyle = FontStyle.Normal;
            text.color = Ivory; text.raycastTarget = false;
            text.supportRichText = false;
        }
        public static void Shadow(Text text, float opacity = .8f)
        {
            var shadow = text.GetComponent<Shadow>();
            if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, opacity);
            shadow.effectDistance = new Vector2(1f, -2f);
        }
    }
}
