using UnityEngine;

namespace OrpheusBelow
{
    public enum EurydiceAnimationState
    {
        Idle = 0,
        Walk = 1,
        Glide = Walk,
        Run = 2,
        Teaching = 3,
        Shatter = 4
    }

    /// <summary>Runtime slicer and animator for Eurydice's 4x4 idle/walk/run/gesture sheet.</summary>
    public sealed class EurydiceSpriteAnimator : MonoBehaviour
    {
        private const int FramesPerState = 4;
        private const float FrameDuration = 1f / 8f;
        private const string ResourcePath = "Characters/Eurydice/Eurydice_GreekOil_4x4";

        private SpriteRenderer _renderer;
        private Sprite[] _frames = System.Array.Empty<Sprite>();
        private EurydiceAnimationState _state;
        private int _frame;
        private float _frameTimer;

        public bool IsReady => _frames.Length == 16;
        public EurydiceAnimationState CurrentState => _state;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            Texture2D texture = Resources.Load<Texture2D>(ResourcePath);
            _frames = SliceSheet(texture, allowSinglePixelRemainder: true);
            if (_renderer != null)
            {
                _renderer.enabled = IsReady;
                if (IsReady) _renderer.sprite = _frames[0];
            }
        }

        private void Update()
        {
            if (!IsReady || _renderer == null) return;
            if (_state == EurydiceAnimationState.Shatter) return;

            _frameTimer += Time.unscaledDeltaTime;
            while (_frameTimer >= FrameDuration)
            {
                _frameTimer -= FrameDuration;
                _frame = (_frame + 1) % FramesPerState;
                ApplyFrame();
            }
        }

        public void SetState(EurydiceAnimationState state)
        {
            if (_state == state) return;
            _state = state;
            _frameTimer = 0f;
            if (state == EurydiceAnimationState.Shatter) return;
            _frame = 0;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (IsReady && _renderer != null)
            {
                _renderer.sprite = _frames[GetFrameIndex(_state, _frame)];
            }
        }

        public static int GetFrameIndex(EurydiceAnimationState state, int frame)
        {
            int row = state == EurydiceAnimationState.Shatter
                ? (int)EurydiceAnimationState.Teaching
                : Mathf.Clamp((int)state, 0, 3);
            return (row * FramesPerState) + Mathf.Clamp(frame, 0, FramesPerState - 1);
        }

        public static Sprite[] SliceSheet(
            Texture2D texture,
            int columns = 4,
            int rows = 4,
            float pixelsPerUnit = 160f,
            bool allowSinglePixelRemainder = false)
        {
            if (texture == null || columns <= 0 || rows <= 0 || pixelsPerUnit <= 0f ||
                texture.width < columns || texture.height < rows ||
                (!allowSinglePixelRemainder && (texture.width % columns != 0 || texture.height % rows != 0)) ||
                (allowSinglePixelRemainder &&
                 (texture.width % columns > 1 || texture.height % rows > 1)))
            {
                return System.Array.Empty<Sprite>();
            }

            // Use equal integer-sized cells and ignore any one-pixel remainder at the far edge.
            int frameWidth = texture.width / columns;
            int frameHeight = texture.height / rows;
            Sprite[] sprites = new Sprite[columns * rows];
            int index = 0;

            for (int row = 0; row < rows; row++)
            {
                float y = texture.height - ((row + 1) * frameHeight);
                for (int column = 0; column < columns; column++)
                {
                    Rect rect = new Rect(column * frameWidth, y, frameWidth, frameHeight);
                    sprites[index++] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.08f), pixelsPerUnit);
                }
            }

            return sprites;
        }
    }
}
