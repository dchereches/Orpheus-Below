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
        Complete
    }

    public readonly struct AsphodelTutorialSnapshot
    {
        public readonly int DashUseCount;
        public readonly int SoftNoteCount;
        public readonly int HardBoomCount;
        public readonly int StanceEntryCount;

        public AsphodelTutorialSnapshot(int dashUseCount, int softNoteCount, int hardBoomCount, int stanceEntryCount)
        {
            DashUseCount = dashUseCount;
            SoftNoteCount = softNoteCount;
            HardBoomCount = hardBoomCount;
            StanceEntryCount = stanceEntryCount;
        }
    }

    /// <summary>
    /// Pure progress model for Eurydice's first-run Asphodel lesson. It observes
    /// successful gameplay actions, so every supported input device behaves alike.
    /// </summary>
    public sealed class AsphodelCombatTutorial
    {
        public const float RequiredMovementDistance = 1.25f;

        private readonly AsphodelTutorialSnapshot _baseline;
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
            bool satisfied;
            switch (CurrentStep)
            {
                case AsphodelTutorialStep.Move:
                    Vector3 travelled = playerPosition - _startPosition;
                    travelled.y = 0f;
                    satisfied = travelled.magnitude >= RequiredMovementDistance;
                    break;
                case AsphodelTutorialStep.Dash:
                    satisfied = snapshot.DashUseCount > _baseline.DashUseCount;
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
                default:
                    return false;
            }

            if (!satisfied) return false;
            CurrentStep++;
            return true;
        }
    }
}
