using UnityEngine;

namespace OrpheusBelow
{
    public enum AsphodelTutorialStep
    {
        Move,
        Dash,
        SoftNote,
        HardBoom,
        Flute,
        Cadence,
        Complete
    }

    public readonly struct AsphodelTutorialSnapshot
    {
        public readonly int DashUseCount;
        public readonly int SoftNoteCount;
        public readonly int HardBoomCount;
        public readonly int StanceEntryCount;
        public readonly int CadenceCount;

        public AsphodelTutorialSnapshot(int dashUseCount, int softNoteCount, int hardBoomCount, int stanceEntryCount, int cadenceCount = 0)
        {
            DashUseCount = dashUseCount;
            SoftNoteCount = softNoteCount;
            HardBoomCount = hardBoomCount;
            StanceEntryCount = stanceEntryCount;
            CadenceCount = cadenceCount;
        }
    }

    /// <summary>
    /// Spread-out progressive learning model for Orpheus Below:
    ///   - Chamber 0 Entrance introduces Movement and the primary Soft Note strike.
    ///   - Further mechanics (Dash, Hard Boom, Flute Stance, and Cadences) unlock organically
    ///     as encounters and chambers progress ("learn features as you go").
    /// </summary>
    public sealed class AsphodelCombatTutorial
    {
        public const float RequiredMovementDistance = 1.25f;

        private AsphodelTutorialSnapshot _baseline;
        private readonly Vector3 _startPosition;

        public AsphodelTutorialStep CurrentStep { get; private set; } = AsphodelTutorialStep.Move;
        public bool IsComplete => CurrentStep == AsphodelTutorialStep.Complete;

        public AsphodelCombatTutorial(AsphodelTutorialSnapshot baseline, Vector3 startPosition)
        {
            _baseline = baseline;
            _startPosition = startPosition;
        }

        public bool Observe(AsphodelTutorialSnapshot snapshot, Vector3 playerPosition)
        {
            bool satisfied = false;
            switch (CurrentStep)
            {
                case AsphodelTutorialStep.Move:
                    Vector3 travelled = playerPosition - _startPosition;
                    travelled.y = 0f;
                    satisfied = travelled.magnitude >= RequiredMovementDistance;
                    break;
                case AsphodelTutorialStep.SoftNote:
                    satisfied = snapshot.SoftNoteCount > _baseline.SoftNoteCount;
                    break;
                default:
                    satisfied = true;
                    break;
            }

            if (!satisfied) return false;

            if (CurrentStep == AsphodelTutorialStep.Move)
            {
                CurrentStep = AsphodelTutorialStep.SoftNote;
                _baseline = snapshot;
                return true;
            }
            else
            {
                CurrentStep = AsphodelTutorialStep.Complete;
                return true;
            }
        }

        public static string GetStepId(AsphodelTutorialStep step)
        {
            switch (step)
            {
                case AsphodelTutorialStep.Move: return "move";
                case AsphodelTutorialStep.Dash: return "dash";
                case AsphodelTutorialStep.SoftNote: return "soft-note";
                case AsphodelTutorialStep.HardBoom: return "hard-boom";
                case AsphodelTutorialStep.Flute: return "flute";
                case AsphodelTutorialStep.Cadence: return "cadence";
                default: return "complete";
            }
        }
    }

    /// <summary>Keeps the tutorial's once-per-play-session completion latch.</summary>
    public sealed class AsphodelTutorialSession
    {
        private AsphodelCombatTutorial _activeTutorial;

        public bool HasCompleted { get; private set; }
        public bool IsActive => _activeTutorial != null && !HasCompleted;
        public AsphodelTutorialStep CurrentStep => HasCompleted
            ? AsphodelTutorialStep.Complete
            : (_activeTutorial != null ? _activeTutorial.CurrentStep : AsphodelTutorialStep.Move);
        public string CurrentStepId => AsphodelCombatTutorial.GetStepId(CurrentStep);

        public bool TryBegin(AsphodelTutorialSnapshot baseline, Vector3 startPosition)
        {
            if (HasCompleted || _activeTutorial != null) return false;
            _activeTutorial = new AsphodelCombatTutorial(baseline, startPosition);
            return true;
        }

        public bool Observe(AsphodelTutorialSnapshot snapshot, Vector3 playerPosition)
        {
            if (!IsActive) return false;
            bool advanced = _activeTutorial.Observe(snapshot, playerPosition);
            if (_activeTutorial.IsComplete) HasCompleted = true;
            return advanced;
        }
    }
}
