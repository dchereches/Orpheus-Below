using UnityEngine;

namespace OrpheusBelow
{
    public enum AsphodelTutorialStep
    {
        Move,
        SoftNote,
        HardBoom,
        Flute,
        Dash,
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
    /// Progressive learning model for Orpheus Below:
    ///   - Move / Clear chambers objective
    ///   - Defend with Soft Note [♪] (J / LMB / mic gentle sound)
    ///   - Cause big repelling move with Hard Boom [Fermata] (K / RMB / mic loud sound)
    ///   - Aulos flute combos force enemies to flee (F / LT)
    ///   - Dash to evade attacks (Space / Shift)
    ///   - Pluck cadences (Hymn of Dawn) to restore breath, crack shields, and open exits
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
                case AsphodelTutorialStep.HardBoom:
                    satisfied = snapshot.HardBoomCount > _baseline.HardBoomCount;
                    break;
                case AsphodelTutorialStep.Flute:
                    satisfied = snapshot.StanceEntryCount > _baseline.StanceEntryCount;
                    break;
                case AsphodelTutorialStep.Dash:
                    satisfied = snapshot.DashUseCount > _baseline.DashUseCount;
                    break;
                case AsphodelTutorialStep.Cadence:
                    satisfied = snapshot.CadenceCount > _baseline.CadenceCount || snapshot.SoftNoteCount > _baseline.SoftNoteCount;
                    break;
                default:
                    satisfied = true;
                    break;
            }

            if (!satisfied) return false;

            switch (CurrentStep)
            {
                case AsphodelTutorialStep.Move:
                    CurrentStep = AsphodelTutorialStep.SoftNote;
                    break;
                case AsphodelTutorialStep.SoftNote:
                    CurrentStep = AsphodelTutorialStep.HardBoom;
                    break;
                case AsphodelTutorialStep.HardBoom:
                    CurrentStep = AsphodelTutorialStep.Flute;
                    break;
                case AsphodelTutorialStep.Flute:
                    CurrentStep = AsphodelTutorialStep.Dash;
                    break;
                case AsphodelTutorialStep.Dash:
                    CurrentStep = AsphodelTutorialStep.Cadence;
                    break;
                case AsphodelTutorialStep.Cadence:
                    CurrentStep = AsphodelTutorialStep.Complete;
                    break;
                default:
                    CurrentStep = AsphodelTutorialStep.Complete;
                    break;
            }

            _baseline = snapshot;
            return true;
        }

        public static string GetStepId(AsphodelTutorialStep step)
        {
            switch (step)
            {
                case AsphodelTutorialStep.Move: return "move";
                case AsphodelTutorialStep.SoftNote: return "soft-note";
                case AsphodelTutorialStep.HardBoom: return "hard-boom";
                case AsphodelTutorialStep.Flute: return "flute";
                case AsphodelTutorialStep.Dash: return "dash";
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
