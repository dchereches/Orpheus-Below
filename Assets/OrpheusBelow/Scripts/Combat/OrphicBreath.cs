using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Shared by every input source. Accepted notes spend breath; phrases restore it.</summary>
    public sealed class OrphicBreath
    {
        public float Breath { get; private set; } = 100f;
        public int RepeatedNotes { get; private set; }
        public float DamageMultiplier { get; private set; } = 1f;
        public float RecoveryRemaining { get; private set; }
        private float _rest;
        private bool _lastWasHard;
        private bool _hasNote;

        public void Tick(float dt)
        {
            dt = Mathf.Max(0f, dt);
            RecoveryRemaining = Mathf.Max(0f, RecoveryRemaining - dt);
            _rest += dt;
            if (_rest > 0.5f) Breath = Mathf.Min(100f, Breath + (24f * DifficultySettings.BreathRecoveryMultiplier) * dt);
            if (_rest > 2f) { RepeatedNotes = 0; _hasNote = false; DamageMultiplier = 1f; }
        }

        public bool TryPlay(bool hard)
        {
            if (RecoveryRemaining > 0f) return false;
            int repeats = _hasNote && hard == _lastWasHard ? RepeatedNotes + 1 : 1;
            float cost = (hard ? 24f : 7f) * (repeats >= 3 ? 1.25f : 1f);
            if (Breath + 0.001f < cost) return false;
            Breath -= cost;
            RepeatedNotes = repeats;
            DamageMultiplier = repeats <= 2 ? 1f : repeats == 3 ? 0.6f : 0.3f;
            _lastWasHard = hard;
            _hasNote = true;
            _rest = 0f;
            RecoveryRemaining = hard ? 0.48f : 0.22f;
            return true;
        }

        public void CompleteCadence()
        {
            Breath = Mathf.Min(100f, Breath + 30f);
            RepeatedNotes = 0;
            DamageMultiplier = 1f;
            _hasNote = false;
        }

        public void Refill()
        {
            Breath = 100f; RepeatedNotes = 0; RecoveryRemaining = 0f;
            DamageMultiplier = 1f; _hasNote = false; _rest = 0f;
        }
    }

    /// <summary>Ordinary hits cannot indefinitely cancel an enemy's telegraphed attack.</summary>
    public sealed class ShadeResonance
    {
        public float ExposedRemaining { get; private set; }
        private float _staggerGuard;
        public void Tick(float dt)
        {
            ExposedRemaining = Mathf.Max(0f, ExposedRemaining - dt);
            _staggerGuard = Mathf.Max(0f, _staggerGuard - dt);
        }
        public void Expose() { ExposedRemaining = 3.2f; _staggerGuard = 0f; }
        public float ResolveDamage(float damage) => damage * (ExposedRemaining > 0f ? 1.35f : 0.7f);
        public bool TryStagger()
        {
            if (_staggerGuard > 0f) return false;
            _staggerGuard = 1.25f;
            return true;
        }
    }
}
