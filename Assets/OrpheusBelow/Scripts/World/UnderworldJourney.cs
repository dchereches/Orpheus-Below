using UnityEngine;

namespace OrpheusBelow
{
    public enum JourneyLeg { Descent, Ascent, Complete }

    /// <summary>The run survives scene loads; the throne is a turnaround, never the victory exit.</summary>
    public sealed class JourneyProgress
    {
        private readonly bool[] _descentCleared = new bool[UnderworldJourney.ChamberCount];
        private readonly bool[] _ascentCleared = new bool[UnderworldJourney.ChamberCount];
        public int Depth { get; private set; }
        public JourneyLeg Leg { get; private set; }
        public bool HasEurydice => Leg != JourneyLeg.Descent;
        public bool IsComplete => Leg == JourneyLeg.Complete;
        public bool TutorialComplete { get; set; }
        public bool IsCleared(int depth) => depth >= 0 && depth < UnderworldJourney.ChamberCount &&
            (Leg == JourneyLeg.Descent ? _descentCleared[depth] : _ascentCleared[depth]);
        public void Visit(int depth) { Depth = Mathf.Clamp(depth, 0, UnderworldJourney.DeepestChamber); }
        public void ClearCurrentChamber()
        {
            if (Leg == JourneyLeg.Descent) _descentCleared[Depth] = true;
            else _ascentCleared[Depth] = true;
        }
        public bool TryDescend()
        {
            if (Leg != JourneyLeg.Descent || Depth >= UnderworldJourney.DeepestChamber || !IsCleared(Depth)) return false;
            Depth++; return true;
        }
        public bool TryRescue()
        {
            if (Leg != JourneyLeg.Descent || Depth != UnderworldJourney.DeepestChamber || !IsCleared(Depth)) return false;
            Leg = JourneyLeg.Ascent; _ascentCleared[Depth] = true; return true;
        }
        public bool TryAscend(bool escortAtGate = true)
        {
            if (Leg != JourneyLeg.Ascent || !IsCleared(Depth) || !escortAtGate) return false;
            if (Depth == 0) Leg = JourneyLeg.Complete;
            else Depth--;
            return true;
        }
    }

    public static class UnderworldJourney
    {
        public const int ChamberCount = 8;
        public const int DeepestChamber = ChamberCount - 1;
        public static readonly string[] ChamberNames = {
            "Sanctuary of Artemis", "Gardens of the Upper Light", "Asphodel's Last Sun", "Court of Withered Cypresses",
            "Broken Causeways", "Falls of Phlegethon", "Titan's Rest", "Throne of the Below"
        };
        public static JourneyProgress Progress { get; private set; } = new JourneyProgress();
        public static bool HasEurydice => Progress.HasEurydice;
        public static bool IsAscending => Progress.Leg != JourneyLeg.Descent;
        public static int Depth => Progress.Depth;
        public static string ProgressLabel => Progress.IsComplete ? "TOGETHER IN THE LIGHT" :
            (IsAscending ? "ASCENT" : "DESCENT") + "  ·  " + (IsAscending ? ChamberCount - Depth : Depth + 1) + " / " + ChamberCount;
        public static string Objective => Progress.IsComplete ? "Eurydice has returned to the light."
            : HasEurydice ? "Lead Eurydice back to Artemis's sanctuary. Do not look back."
            : Depth == DeepestChamber ? "Find Eurydice beside the throne." : "Descend alone. Find Eurydice in the deepest chamber.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Progress = new JourneyProgress(); }
    }
}
