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
        private float _gaitPhase;
        private bool _singing;
        private bool _aulos;
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
            if (input.paused || input.deltaTime <= 0f) return;
            float distance = new Vector2(input.groundDelta.x, input.groundDelta.z).magnitude;
            bool moving = distance / input.deltaTime > .28f;
            bool movementRequested = input.movementRequested ?? moving;
            if ((input.dashing && _overridePriority < 60 && _overrideState != OrpheusAnimationState.DashStart) ||
                (movementRequested && (_overridePriority <= 45 || _overrideState == OrpheusAnimationState.SingSustain)
                    && _overrideState != OrpheusAnimationState.DashStart))
            {
                _overrideState = OrpheusAnimationState.None;
                _overridePriority = 0; _overrideRemaining = 0f; _overrideIndefinite = false;
                CancelQueuedBelow(46);
                if (_queued.Count > 0) StartQueued();
            }
            if (!movementRequested && !input.dashing && !input.defeated && _overrideState == OrpheusAnimationState.None && _singing)
                BeginOneShot(OrpheusAnimationState.SingSustain, 50, true);
            if (_overrideState != OrpheusAnimationState.None) input.action = _overrideState;
            _brain.Step(input);
            SetState(_brain.State);
            if (IsGait(_state))
            {
                _gaitPhase = Mathf.Repeat(_gaitPhase + distance / (_brain.GroundSpeed >= 4.5f ? 3.8f : 2.5f), 1f);
                OrpheusAnimationClip clip = _library?.FindClip(_state);
                if (clip != null) _frame = Mathf.Min((int)(_gaitPhase * clip.framesPerDirection), clip.framesPerDirection - 1);
            }
            else if (!moving && _overrideState == OrpheusAnimationState.None) _gaitPhase = 0f;
            ApplyFrame();
        }

        public void Advance(float deltaTime, bool paused = false)
        {
            if (paused || deltaTime <= 0f) return;
            _elapsed += deltaTime;
            OrpheusAnimationClip clip = _library?.FindClip(_state);
            if (clip != null && clip.IsComplete && !IsGait(_state))
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
                        if (_aulos) BeginOneShot(OrpheusAnimationState.AulosLoop, 55, true);
                        else if (_singing) BeginOneShot(OrpheusAnimationState.SingSustain, 50, true);
                        else SetState(OrpheusAnimationState.Idle);
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
            _singing = _aulos = false;
            _gaitPhase = 0f;
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
            // A two-frame draw on the opening chord; subsequent combo attacks cancel straight into their strike.
            if (comboIndex <= 1 && _overrideState == OrpheusAnimationState.None)
            {
                BeginOneShot(OrpheusAnimationState.LyreDraw, 60, false, .025f);
                QueueOneShot(strike, 60);
            }
            else BeginOneShot(strike, 60);
            QueueOneShot(OrpheusAnimationState.SoftPluck, 40, false, .07f);
            if (comboIndex >= 3) QueueOneShot(OrpheusAnimationState.SoftBolt, 60);
        }

        public void TriggerDashStrike() => BeginOneShot(OrpheusAnimationState.DashStrike, 65);
        public void TriggerDashStart()
        {
            _singing = false;
            if (_overridePriority < 90) ClearOverride();
            BeginOneShot(OrpheusAnimationState.DashStart, 45, false, .035f);
        }
        public void TriggerDashSettle() => BeginOneShot(OrpheusAnimationState.DashSettle, 45, false, .06f);
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
            _singing = false;
            BeginOneShot(OrpheusAnimationState.FullVoiceRelease, 75, false, .06f);
            QueueOneShot(state, 75);
        }
        public void EnterFlute()
        {
            _singing = false;
            _aulos = true;
            BeginOneShot(OrpheusAnimationState.AulosDraw, 55);
        }
        public void TriggerFluteNote(bool hard) => BeginOneShot(hard ? OrpheusAnimationState.AulosNoteHard : OrpheusAnimationState.AulosNoteSoft, 65);
        public void TriggerDiscord() => BeginOneShot(OrpheusAnimationState.Discord, 80);
        public void TriggerRequiem() => BeginOneShot(OrpheusAnimationState.Requiem, 85);
        public void TriggerHit(bool heavy)
        {
            _singing = _aulos = false;
            BeginOneShot(heavy ? OrpheusAnimationState.HeavyHit : OrpheusAnimationState.LightHit, 90);
            if (!heavy) return;
            BeginOneShot(OrpheusAnimationState.GroundedHurt, 89);
            BeginOneShot(OrpheusAnimationState.Rise, 89);
        }
        public void TriggerVictory()
        {
            _singing = _aulos = false;
            ClearOverride();
            BeginOneShot(OrpheusAnimationState.Victory, 100, true);
        }
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
            _singing = active;
            if (active)
            {
                if (_overrideState == OrpheusAnimationState.None) BeginOneShot(OrpheusAnimationState.SingSustain, 50, true);
            }
            else if (_overrideState == OrpheusAnimationState.SingSustain) BeginOneShot(OrpheusAnimationState.PhraseResolve, 50);
            if (!active) ReplaceQueued(OrpheusAnimationState.SingSustain, OrpheusAnimationState.PhraseResolve, 50);
        }

        public void SetAulosLoop(bool active)
        {
            _aulos = active;
            if (active)
            {
                if (_overrideState == OrpheusAnimationState.None) BeginOneShot(OrpheusAnimationState.AulosLoop, 55, true);
            }
            else if (_overrideState == OrpheusAnimationState.AulosLoop || _overrideState == OrpheusAnimationState.AulosDraw)
            {
                ClearOverride(); SetState(OrpheusAnimationState.Idle);
            }
            if (!active) RemoveQueued(OrpheusAnimationState.AulosLoop, OrpheusAnimationState.AulosDraw);
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
            // A repeated attack is a new action, even when the state name is unchanged.
            _elapsed = 0f; _frame = 0;
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
        }

        private void RemoveQueued(OrpheusAnimationState a, OrpheusAnimationState b)
        {
            var kept = new Queue<QueuedAnimation>();
            while (_queued.Count > 0)
            {
                QueuedAnimation item = _queued.Dequeue();
                if (item.state != a && item.state != b) kept.Enqueue(item);
            }
            while (kept.Count > 0) _queued.Enqueue(kept.Dequeue());
        }

        private static bool IsGait(OrpheusAnimationState state)
            => state == OrpheusAnimationState.Walk || state == OrpheusAnimationState.Run ||
               state == OrpheusAnimationState.Start || state == OrpheusAnimationState.Turn ||
               state == OrpheusAnimationState.StairAscent || state == OrpheusAnimationState.StairDescent;

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
            _body.flipX = _library.FindClip(_state).mirrorWest && (int)_brain.Facing >= (int)EurydiceFacing.NorthWest;
            ApplyEffectFrame();
            OrpheusOilSurface.Apply(_body, PaintedChamberMap.Active?.definition);
        }

        private void ApplyEffectFrame()
        {
            if (_effects == null) return;
            int row = EffectRow(_state);
            if (row < 0 || _library.FindClip(_state).embeddedEffects || _library.effects == null || _library.effects.Length < (row + 1) * 8)
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
                _effects.color = row == 6 ? new Color(1f, .9f, .72f, .32f) : Color.white;
                _effects.transform.localPosition = Vector3.up * 1.6f;
                _effects.transform.localScale = Vector3.one * .55f;
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
