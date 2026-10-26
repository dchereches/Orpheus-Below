using UnityEngine;

namespace OrpheusBelow
{
    public enum EurydiceAnimationState
    {
        Idle,
        Glide,
        Teaching,
        Shatter
    }

    /// <summary>Runtime slicer and compact four-state animator for Eurydice's 4x4 sheet.</summary>
    public sealed class EurydiceSpriteAnimator : MonoBehaviour
    {
        private const int FramesPerState = 4;
        private const float FrameDuration = 1f / 8f;
        private const string ResourcePath = "Characters/Eurydice/Eurydice_DarkCrimson_4x4";

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
            _frames = SliceSheet(texture);
            if (_renderer != null)
            {
                _renderer.enabled = IsReady;
                if (IsReady) _renderer.sprite = _frames[0];
            }
        }

        private void Update()
        {
            if (!IsReady || _renderer == null) return;

            _frameTimer += Time.unscaledDeltaTime;
            while (_frameTimer >= FrameDuration)
            {
                _frameTimer -= FrameDuration;
                if (_state == EurydiceAnimationState.Shatter)
                {
                    _frame = Mathf.Min(_frame + 1, FramesPerState - 1);
                }
                else
                {
                    _frame = (_frame + 1) % FramesPerState;
                }
                ApplyFrame();
            }
        }

        public void SetState(EurydiceAnimationState state)
        {
            if (_state == state) return;
            _state = state;
            _frame = 0;
            _frameTimer = 0f;
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
            return ((int)state * FramesPerState) + Mathf.Clamp(frame, 0, FramesPerState - 1);
        }

        public static Sprite[] SliceSheet(Texture2D texture, int columns = 4, int rows = 4, float pixelsPerUnit = 256f)
        {
            if (texture == null || columns <= 0 || rows <= 0 || pixelsPerUnit <= 0f ||
                texture.width % columns != 0 || texture.height % rows != 0)
            {
                return System.Array.Empty<Sprite>();
            }

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
