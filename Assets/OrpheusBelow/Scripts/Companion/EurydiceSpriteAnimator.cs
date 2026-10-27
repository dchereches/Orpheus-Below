using UnityEngine;

namespace OrpheusBelow
{
    // Retain the old sheet's first four serialized indices.
    public enum EurydiceAnimationState { Idle, Glide, Teaching, Shatter, Joy, Fear, Listen, Sanctuary, StairAscent, StairDescent, Turn, Reform, Run, Walk = Glide }

    public sealed class EurydiceSpriteAnimator : MonoBehaviour
    {
        private SpriteRenderer _renderer, _effects;
        private EurydiceAnimationLibrary _library;
        private Sprite[] _legacy = System.Array.Empty<Sprite>();
        private bool _greekLegacy;
        private EurydiceAnimationState _state;
        private EurydiceFacing _facing, _turnFrom;
        private float _elapsed, _effectTime, _speed;
        private int _frame;
        private bool _shieldActive, _joyActive, _replyActive;
        public bool IsReady => _library != null ? _library.IsComplete : _legacy.Length == 16;
        public bool UsesLook02 => _library != null && _library.IsComplete;
        public EurydiceAnimationState CurrentState => _state;
        public EurydiceFacing Facing => _facing;
        public int CurrentFrame => _frame;
        public float Elapsed => _elapsed;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _library = Resources.Load<EurydiceAnimationLibrary>(EurydiceAnimationLibrary.ResourcePath);
            if (_library == null)
            {
                var texture = Resources.Load<Texture2D>("Characters/Eurydice/Eurydice_GreekOil_4x4");
                _greekLegacy = texture != null;
                _legacy = _greekLegacy ? SliceSheet(texture, allowSinglePixelRemainder: true)
                    : SliceSheet(Resources.Load<Texture2D>("Characters/Eurydice/Eurydice_DarkCrimson_4x4"), pixelsPerUnit: 256f);
            }
            if (_renderer != null) { _renderer.enabled = IsReady; ApplyFrame(); }
        }

        public void AttachEffects(SpriteRenderer renderer) { _effects = renderer; ApplyFrame(); }
        public void SetShieldActive(bool active, bool joyful = false, bool replying = false) { _shieldActive = active; _joyActive = joyful; _replyActive = replying; }
        private void Update()
        {
            // Modal lessons pause the world while Eurydice illustrates the lesson. The pause menu freezes both.
            bool lesson = OrpheusHud.IsTutorialModalActive && (_state == EurydiceAnimationState.Teaching ||
                _state == EurydiceAnimationState.Fear || _state == EurydiceAnimationState.Sanctuary || _state == EurydiceAnimationState.Joy);
            Advance(lesson ? Time.unscaledDeltaTime : Time.deltaTime, UnderworldFrontEnd.BlocksGameplay || (Time.timeScale <= 0f && !lesson));
        }

        public void Drive(EurydiceAnimationBrain brain)
        {
            _facing = brain.Facing; _turnFrom = brain.TurnFrom; _speed = brain.GroundSpeed;
            SetState(brain.State); ApplyFrame();
        }

        public void ResetMotion()
        {
            _speed = 0; _elapsed = 0; _frame = 0; SetState(EurydiceAnimationState.Idle); ApplyFrame();
        }

        public void SetState(EurydiceAnimationState state)
        {
            if (_state == state) return;
            _state = state; _elapsed = 0; _frame = 0;
            if (_renderer != null && state != EurydiceAnimationState.Reform && state != EurydiceAnimationState.Shatter)
            {
                Color color = _renderer.color; color.a = .94f; _renderer.color = color;
            }
            ApplyFrame();
        }

        public void Advance(float deltaTime, bool paused = false)
        {
            if (paused || deltaTime <= 0 || !IsReady) return;
            _elapsed += deltaTime; _effectTime += deltaTime;
            float fps = UsesLook02 ? EurydiceAnimationLibrary.Fps(_state) : 8f;
            if (_state == EurydiceAnimationState.Glide || _state == EurydiceAnimationState.Run || _state == EurydiceAnimationState.StairAscent || _state == EurydiceAnimationState.StairDescent)
                fps *= Mathf.Clamp(_speed / 3f, .65f, 1.5f);
            int count = UsesLook02 ? EurydiceAnimationLibrary.FrameCount(_state) : 4;
            bool loop = UsesLook02 ? EurydiceAnimationLibrary.Loops(_state) : _state != EurydiceAnimationState.Shatter;
            int frame = Mathf.FloorToInt(_elapsed * fps);
            _frame = loop ? frame % count : Mathf.Min(frame, count - 1);
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (!IsReady || _renderer == null) return;
            if (UsesLook02)
            {
                EurydiceFacing facing = _facing;
                if (_state == EurydiceAnimationState.Turn)
                {
                    float angle = Mathf.LerpAngle((int)_turnFrom * 45, (int)_facing * 45, Mathf.Clamp01(_elapsed / .18f));
                    facing = (EurydiceFacing)((Mathf.RoundToInt(angle / 45) + 8) % 8);
                }
                _renderer.sprite = _library.Frame(_state, facing, _frame);
                if (_state == EurydiceAnimationState.Shatter)
                {
                    Color color = _renderer.color; color.a = .94f * (1f - Mathf.Clamp01(_elapsed / 1f)); _renderer.color = color;
                }
                else if (_state == EurydiceAnimationState.Reform)
                {
                    Color color = _renderer.color; color.a = .94f * Mathf.Clamp01(_elapsed / .65f); _renderer.color = color;
                }
                bool reaction = _state == EurydiceAnimationState.Joy || _state == EurydiceAnimationState.Fear || _state == EurydiceAnimationState.Listen ||
                    _state == EurydiceAnimationState.Teaching || _state == EurydiceAnimationState.Sanctuary;
                _renderer.flipX = reaction && (int)_facing >= 5;
                if (_effects != null)
                {
                    int row = _state == EurydiceAnimationState.Shatter ? 2 : _shieldActive ? 3 : _state == EurydiceAnimationState.Fear ? 2 : _joyActive || _state == EurydiceAnimationState.Joy ? 1
                        : _state == EurydiceAnimationState.Sanctuary || _state == EurydiceAnimationState.Reform ? 3 : 0;
                    _effects.sprite = _library.effects[row * 6 + Mathf.FloorToInt(_effectTime * 7f) % 6];
                    float alpha = row > 0 ? .65f : _replyActive ? .35f : _state == EurydiceAnimationState.Idle ? .08f : .20f;
                    if (_state == EurydiceAnimationState.Shatter) alpha *= 1f - Mathf.Clamp01(_elapsed / 1.1f);
                    _effects.color = new Color(1, 1, 1, alpha);
                }
            }
            else _renderer.sprite = _legacy[_greekLegacy ? GetFrameIndex(_state, _frame) : Mathf.Min((int)_state, 3) * 4 + Mathf.Clamp(_frame, 0, 3)];
        }

        public static int GetFrameIndex(EurydiceAnimationState state, int frame)
        {
            int row = state == EurydiceAnimationState.Glide ? 1 : state == EurydiceAnimationState.Run ? 2
                : state == EurydiceAnimationState.Teaching || state == EurydiceAnimationState.Shatter ? 3 : 0;
            return row * 4 + Mathf.Clamp(frame, 0, 3);
        }
        public static Sprite[] SliceSheet(Texture2D texture, int columns = 4, int rows = 4, float pixelsPerUnit = 160f, bool allowSinglePixelRemainder = false)
        {
            if (texture == null || columns <= 0 || rows <= 0 || pixelsPerUnit <= 0 || texture.width < columns || texture.height < rows ||
                (!allowSinglePixelRemainder && (texture.width % columns != 0 || texture.height % rows != 0)) ||
                (allowSinglePixelRemainder && (texture.width % columns > 1 || texture.height % rows > 1)))
                return System.Array.Empty<Sprite>();
            int width = texture.width / columns, height = texture.height / rows;
            var sprites = new Sprite[columns * rows];
            for (int row = 0; row < rows; row++) for (int col = 0; col < columns; col++)
                sprites[row * columns + col] = Sprite.Create(texture, new Rect(col * width, texture.height - (row + 1) * height, width, height), new Vector2(.5f, .08f), pixelsPerUnit);
            return sprites;
        }

        private void OnDestroy()
        {
            foreach (var sprite in _legacy) if (sprite != null) { if (Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite); }
        }
    }
}
