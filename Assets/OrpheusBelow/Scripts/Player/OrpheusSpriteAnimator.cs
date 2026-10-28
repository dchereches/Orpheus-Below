using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    public sealed class OrpheusSpriteAnimator : MonoBehaviour
    {
        private readonly OrpheusAnimationBrain _brain = new OrpheusAnimationBrain();
        private SpriteRenderer _body;
        private SpriteRenderer _effects;
        private GameObject _fallback;
        private OrpheusAnimationLibrary _library;
        private OrpheusAnimationState _state = OrpheusAnimationState.Idle;
        private OrpheusAnimationState _overrideState = OrpheusAnimationState.None;
        private float _overrideRemaining;
        private int _overridePriority;
        private bool _overrideIndefinite;
        private float _elapsed;
        private int _frame;
        private int _stateVersion;
        private readonly Queue<QueuedAnimation> _queued = new Queue<QueuedAnimation>();

        private struct QueuedAnimation
        {
            public OrpheusAnimationState state;
            public int priority;
            public bool indefinite;
            public float duration;
        }

        public bool UsesProductionArt => _library != null && _library.IsComplete;
        public OrpheusAnimationState CurrentState => _state;
        public EurydiceFacing Facing => _brain.Facing;
        public int CurrentFrame => _frame;
        public float Elapsed => _elapsed;
        public int StateVersion => _stateVersion;
        public SpriteRenderer BodyRenderer => _body;
        public SpriteRenderer EffectsRenderer => _effects;

        private void Update()
        {
            Advance(Time.deltaTime, UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive || Time.timeScale <= 0f);
        }

        public void Initialize(SpriteRenderer body, SpriteRenderer effects, GameObject proceduralFallback,
            OrpheusAnimationLibrary library)
        {
            _body = body;
            _effects = effects;
            _fallback = proceduralFallback;
            _library = library;
            bool production = UsesProductionArt;
            if (_body != null) _body.enabled = production;
            if (_effects != null) _effects.enabled = production;
            if (_fallback != null) _fallback.SetActive(!production);
            ApplyFrame();
        }

        public void Drive(OrpheusAnimationInput input)
        {
            if (_overrideState != OrpheusAnimationState.None) input.action = _overrideState;
            _brain.Step(input);
            SetState(_brain.State);
            ApplyFrame();
        }

        public void Advance(float deltaTime, bool paused = false)
        {
            if (paused || deltaTime <= 0f) return;
            _elapsed += deltaTime;
            OrpheusAnimationClip clip = _library?.FindClip(_state);
            if (clip != null && clip.IsComplete)
            {
                int raw = Mathf.FloorToInt(_elapsed * clip.fps);
                _frame = clip.loop ? raw % clip.framesPerDirection : Mathf.Min(raw, clip.framesPerDirection - 1);
            }

            if (_overrideState != OrpheusAnimationState.None && !_overrideIndefinite)
            {
                _overrideRemaining -= deltaTime;
                if (_overrideRemaining <= 0f)
                {
                    _overrideState = OrpheusAnimationState.None;
                    _overridePriority = 0;
                    _overrideIndefinite = false;
                    if (_queued.Count > 0) StartQueued();
                    else
                    {
                        _brain.ResetMotion();
                        SetState(OrpheusAnimationState.Idle);
                    }
                }
            }
            ApplyFrame();
        }

        public void ResetMotion()
        {
            _overrideState = OrpheusAnimationState.None;
            _overrideRemaining = 0f;
            _overridePriority = 0;
            _overrideIndefinite = false;
            _queued.Clear();
            _brain.ResetMotion();
            SetState(OrpheusAnimationState.Idle);
            _elapsed = 0f;
            _frame = 0;
            ApplyFrame();
        }

        public void TriggerSoftAttack(int comboIndex)
        {
            OrpheusAnimationState state = comboIndex <= 1 ? OrpheusAnimationState.SoftStrike1
                : comboIndex == 2 ? OrpheusAnimationState.SoftStrike2 : OrpheusAnimationState.SoftStrike3;
            BeginOneShot(state, 60);
        }

        public void TriggerSoftPerformance(int comboIndex)
        {
            CancelQueuedBelow(61);
            OrpheusAnimationState strike = comboIndex <= 1 ? OrpheusAnimationState.SoftStrike1
                : comboIndex == 2 ? OrpheusAnimationState.SoftStrike2 : OrpheusAnimationState.SoftStrike3;
            // Immediate strike execution for snappy, responsive action-combat
            BeginOneShot(strike, 60);
            if (comboIndex >= 3) QueueOneShot(OrpheusAnimationState.SoftBolt, 60);
        }

        public void TriggerDashStrike() => BeginOneShot(OrpheusAnimationState.DashStrike, 65);
        public void TriggerDashStart() => BeginOneShot(OrpheusAnimationState.DashStart, 45, false, .045f);
        public void TriggerDashSettle() => BeginOneShot(OrpheusAnimationState.DashSettle, 45);
        public void TriggerChamberEntrance()
        {
            BeginOneShot(OrpheusAnimationState.ChamberEntrance, 20);
            QueueOneShot(OrpheusAnimationState.Inspect, 20);
        }
        public void TriggerAlert()
        {
            if (_overrideState == OrpheusAnimationState.ChamberEntrance)
                QueueOneShot(OrpheusAnimationState.Alert, 20);
            else BeginOneShot(OrpheusAnimationState.Alert, 20);
        }
        public void TriggerInspect() => BeginOneShot(OrpheusAnimationState.Inspect, 20);
        public void TriggerHardBoom() => BeginOneShot(OrpheusAnimationState.HardBoomRelease, 70);
        public void TriggerHardBoomPerformance()
        {
            BeginOneShot(OrpheusAnimationState.HardBoomWindup, 70, false, .06f);
            QueueOneShot(OrpheusAnimationState.HardBoomRelease, 70);
        }
        public void TriggerCadence(OrpheusAnimationState state) => BeginOneShot(state, 75);
        public void TriggerCadencePerformance(OrpheusAnimationState state)
        {
            BeginOneShot(OrpheusAnimationState.FullVoiceRelease, 75, false, .06f);
            QueueOneShot(state, 75);
        }
        public void EnterFlute() => BeginOneShot(OrpheusAnimationState.AulosDraw, 55);
        public void TriggerFluteNote(bool hard) => BeginOneShot(hard ? OrpheusAnimationState.AulosNoteHard : OrpheusAnimationState.AulosNoteSoft, 65);
        public void TriggerDiscord() => BeginOneShot(OrpheusAnimationState.Discord, 80);
        public void TriggerRequiem() => BeginOneShot(OrpheusAnimationState.Requiem, 85);
        public void TriggerHit(bool heavy)
        {
            BeginOneShot(heavy ? OrpheusAnimationState.HeavyHit : OrpheusAnimationState.LightHit, 90);
            if (!heavy) return;
            BeginOneShot(OrpheusAnimationState.GroundedHurt, 89);
            BeginOneShot(OrpheusAnimationState.Rise, 89);
        }
        public void TriggerVictory() => BeginOneShot(OrpheusAnimationState.Victory, 20);
        public void TriggerChamberClear() => BeginOneShot(OrpheusAnimationState.ChamberClear, 20);
        public void TriggerGrief() => BeginOneShot(OrpheusAnimationState.EurydiceGrief, 100, true);
        public void TriggerBreath(bool exhausted)
            => BeginOneShot(exhausted ? OrpheusAnimationState.BreathExhausted : OrpheusAnimationState.BreathRecover,
                exhausted ? 30 : 31);

        public void TriggerLookBack(bool fatal)
            => BeginOneShot(fatal ? OrpheusAnimationState.LookBackFatal : OrpheusAnimationState.LookBackTemptation,
                fatal ? 100 : 40, fatal);

        public void SetSinging(bool active)
        {
            if (active) BeginOneShot(OrpheusAnimationState.SingSustain, 50, false, float.PositiveInfinity);
            else if (_overrideState == OrpheusAnimationState.SingSustain) BeginOneShot(OrpheusAnimationState.PhraseResolve, 50);
            else ReplaceQueued(OrpheusAnimationState.SingSustain, OrpheusAnimationState.PhraseResolve, 50);
        }

        public void SetAulosLoop(bool active)
        {
            if (active) BeginOneShot(OrpheusAnimationState.AulosLoop, 55, false, float.PositiveInfinity);
            else if (_overrideState == OrpheusAnimationState.AulosLoop) ClearOverride();
        }

        private void BeginOneShot(OrpheusAnimationState state, int priority, bool indefinite = false, float duration = -1f)
        {
            if (_overrideState != OrpheusAnimationState.None && priority < _overridePriority)
            {
                QueueOneShot(state, priority, indefinite, duration);
                return;
            }
            if (_overrideState != OrpheusAnimationState.None && priority > _overridePriority)
                CancelQueuedBelow(priority);
            _overrideState = state;
            _overridePriority = priority;
            _overrideIndefinite = indefinite || float.IsPositiveInfinity(duration);
            if (duration < 0f)
            {
                OrpheusAnimationClip clip = _library?.FindClip(state);
                duration = clip != null && clip.fps > 0f ? clip.framesPerDirection / clip.fps : .55f;
            }
            _overrideRemaining = duration;
            SetState(state);
            ApplyFrame();
        }

        private void StartQueued()
        {
            QueuedAnimation next = _queued.Dequeue();
            BeginOneShot(next.state, next.priority, next.indefinite, next.duration);
        }

        private void QueueOneShot(OrpheusAnimationState state, int priority, bool indefinite = false, float duration = -1f)
            => _queued.Enqueue(new QueuedAnimation { state = state, priority = priority, indefinite = indefinite,
                duration = duration });

        private void CancelQueuedBelow(int priority)
        {
            if (_queued.Count == 0) return;
            var kept = new Queue<QueuedAnimation>();
            while (_queued.Count > 0)
            {
                QueuedAnimation item = _queued.Dequeue();
                if (item.priority >= priority) kept.Enqueue(item);
            }
            while (kept.Count > 0) _queued.Enqueue(kept.Dequeue());
        }

        private void ReplaceQueued(OrpheusAnimationState from, OrpheusAnimationState to, int priority)
        {
            if (_queued.Count == 0) return;
            var replacement = new Queue<QueuedAnimation>();
            while (_queued.Count > 0)
            {
                QueuedAnimation item = _queued.Dequeue();
                if (item.state == from)
                    item = new QueuedAnimation { state = to, priority = priority, duration = -1f };
                replacement.Enqueue(item);
            }
            while (replacement.Count > 0) _queued.Enqueue(replacement.Dequeue());
        }

        private void ClearOverride()
        {
            _overrideState = OrpheusAnimationState.None;
            _overridePriority = 0;
            _overrideRemaining = 0f;
            _overrideIndefinite = false;
            _queued.Clear();
            _brain.ResetMotion();
            SetState(OrpheusAnimationState.Idle);
        }

        private void SetState(OrpheusAnimationState state)
        {
            if (_state == state) return;
            _state = state;
            _elapsed = 0f;
            _frame = 0;
            _stateVersion++;
        }

        private void ApplyFrame()
        {
            if (!UsesProductionArt || _body == null) return;
            Sprite frame = _library.Frame(_state, _brain.Facing, _frame);
            if (frame != null) _body.sprite = frame;
            ApplyEffectFrame();
            OrpheusOilSurface.Apply(_body, PaintedChamberMap.Active?.definition);
        }

        private void ApplyEffectFrame()
        {
            if (_effects == null) return;
            int row = EffectRow(_state);
            if (row < 0 || _library.effects == null || _library.effects.Length < (row + 1) * 8)
            {
                _effects.sprite = null;
                _effects.enabled = false;
                return;
            }
            int effectFrame = Mathf.Clamp(Mathf.FloorToInt(_elapsed * 12f), 0, 7);
            _effects.sprite = _library.effects[row * 8 + effectFrame];
            _effects.enabled = _effects.sprite != null;
            if (_effects.enabled)
            {
                // Soft semi-transparent tint for dash effects (row 1)
                _effects.color = row == 1 ? new Color(1f, 0.55f, 0.40f, 0.42f) : Color.white;
            }
        }

        private static int EffectRow(OrpheusAnimationState state)
        {
            switch (state)
            {
                case OrpheusAnimationState.SoftStrike1:
                case OrpheusAnimationState.SoftStrike2:
                case OrpheusAnimationState.SoftStrike3:
                case OrpheusAnimationState.SoftBolt:
                case OrpheusAnimationState.SoftPluck: return 0;
                case OrpheusAnimationState.DashStart:
                case OrpheusAnimationState.DashTravel:
                case OrpheusAnimationState.DashSettle:
                case OrpheusAnimationState.DashStrike: return 1;
                case OrpheusAnimationState.HardBoomWindup:
                case OrpheusAnimationState.HardBoomRelease: return 2;
                case OrpheusAnimationState.HymnBeam: return 3;
                case OrpheusAnimationState.SanctuaryCast: return 4;
                case OrpheusAnimationState.ConvergenceCast: return 5;
                case OrpheusAnimationState.SingSustain:
                case OrpheusAnimationState.FullVoiceRelease:
                case OrpheusAnimationState.AulosNoteSoft:
                case OrpheusAnimationState.AulosNoteHard: return 6;
                case OrpheusAnimationState.Discord:
                case OrpheusAnimationState.Requiem: return 7;
                default: return -1;
            }
        }
    }
}
