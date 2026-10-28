using System;
using UnityEngine;

namespace OrpheusBelow
{
    public enum OrpheusAnimationState
    {
        None,
        Idle, RestlessIdle, Start, Walk, Run, Stop, Turn,
        DashStart, DashTravel, DashSettle, DashStrike,
        StairAscent, StairDescent,
        SoftStrike1, SoftStrike2, SoftStrike3, SoftBolt,
        HardBoomWindup, HardBoomRelease,
        HymnBeam, SanctuaryCast, ConvergenceCast,
        LyreDraw, SoftPluck, SingSustain, FullVoiceRelease, PhraseResolve,
        AulosDraw, AulosLoop, AulosNoteSoft, AulosNoteHard, Discord, Requiem,
        BreathExhausted, BreathRecover,
        LightHit, HeavyHit, GroundedHurt, Rise,
        ChamberEntrance, Alert, Inspect,
        LookBackTemptation, LookBackFatal, EurydiceGrief, ChamberClear, Victory, Defeat
    }

    [Serializable]
    public sealed class OrpheusAnimationClip
    {
        public OrpheusAnimationState state;
        public Sprite[] frames = Array.Empty<Sprite>();
        [Range(1, 8)] public int directions = 8;
        [Min(1)] public int framesPerDirection = 1;
        [Min(.1f)] public float fps = 8f;
        public bool loop;

        public bool IsComplete => frames != null && directions > 0 && framesPerDirection > 0 &&
            frames.Length == directions * framesPerDirection && Array.TrueForAll(frames, frame => frame != null);
    }

    public sealed class OrpheusAnimationLibrary : ScriptableObject
    {
        public const string ResourcePath = "Characters/Orpheus/BroadOil/Library";
        public float visibleHeight = 3.9f;
        public OrpheusAnimationClip[] clips = Array.Empty<OrpheusAnimationClip>();
        public Sprite[] sourceFrames = Array.Empty<Sprite>();
        public Sprite[] effects = Array.Empty<Sprite>();

        public static readonly OrpheusAnimationState[] RequiredStates = BuildRequiredStates();

        public bool IsComplete
        {
            get
            {
                if (clips == null || clips.Length < RequiredStates.Length) return false;
                for (int i = 0; i < RequiredStates.Length; i++)
                {
                    OrpheusAnimationClip clip = FindClip(RequiredStates[i]);
                    if (clip == null || !clip.IsComplete || clip.directions != DirectionCount(clip.state)) return false;
                }
                return true;
            }
        }

        public OrpheusAnimationClip FindClip(OrpheusAnimationState state)
            => clips == null ? null : Array.Find(clips, clip => clip != null && clip.state == state);

        public Sprite Frame(OrpheusAnimationState state, EurydiceFacing facing, int frame)
        {
            OrpheusAnimationClip clip = FindClip(state) ?? FindClip(OrpheusAnimationState.Idle);
            if (clip == null || !clip.IsComplete) return null;
            int direction = DirectionIndex(facing, clip.directions);
            int local = clip.loop ? PositiveModulo(frame, clip.framesPerDirection) : Mathf.Clamp(frame, 0, clip.framesPerDirection - 1);
            return clip.frames[direction * clip.framesPerDirection + local];
        }

        public static bool RequiresEightDirections(OrpheusAnimationState state)
        {
            switch (state)
            {
                case OrpheusAnimationState.LightHit:
                case OrpheusAnimationState.HeavyHit:
                case OrpheusAnimationState.GroundedHurt:
                case OrpheusAnimationState.Rise:
                case OrpheusAnimationState.ChamberEntrance:
                case OrpheusAnimationState.Alert:
                case OrpheusAnimationState.Inspect:
                case OrpheusAnimationState.LookBackTemptation:
                case OrpheusAnimationState.LookBackFatal:
                case OrpheusAnimationState.EurydiceGrief:
                case OrpheusAnimationState.ChamberClear:
                case OrpheusAnimationState.Victory:
                case OrpheusAnimationState.Defeat:
                case OrpheusAnimationState.BreathExhausted:
                case OrpheusAnimationState.BreathRecover:
                    return false;
                default:
                    return state != OrpheusAnimationState.None;
            }
        }

        public static int DirectionCount(OrpheusAnimationState state) => RequiresEightDirections(state) ? 8 : 4;

        public static int DirectionIndex(EurydiceFacing facing, int directions)
        {
            if (directions >= 8) return (int)facing;
            switch (facing)
            {
                case EurydiceFacing.South:
                case EurydiceFacing.SouthWest: return 0;
                case EurydiceFacing.SouthEast:
                case EurydiceFacing.East: return 1;
                case EurydiceFacing.NorthEast:
                case EurydiceFacing.North:
                case EurydiceFacing.NorthWest: return 2;
                default: return 3;
            }
        }

        private static OrpheusAnimationState[] BuildRequiredStates()
        {
            Array values = Enum.GetValues(typeof(OrpheusAnimationState));
            var result = new OrpheusAnimationState[values.Length - 1];
            int index = 0;
            foreach (OrpheusAnimationState state in values)
                if (state != OrpheusAnimationState.None) result[index++] = state;
            return result;
        }

        private static int PositiveModulo(int value, int modulus) => (value % modulus + modulus) % modulus;
    }
}
