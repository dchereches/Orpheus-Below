using UnityEngine;

namespace OrpheusBelow
{
    public sealed class EurydiceAnimationLibrary : ScriptableObject
    {
        public const string ResourcePath = "Characters/Eurydice/Look02/Library";
        public Sprite[] idle, walk, reactions, stairs, effects;
        public float visibleHeight = 2.5274f;
        public bool IsComplete => idle?.Length == 32 && walk?.Length == 32 && reactions?.Length == 36 &&
            stairs?.Length == 24 && effects?.Length == 24;

        public Sprite Frame(EurydiceAnimationState state, EurydiceFacing facing, int frame)
        {
            int direction = (int)facing;
            if (state == EurydiceAnimationState.Idle || state == EurydiceAnimationState.Turn) return idle[direction * 4 + frame % 4];
            if (state == EurydiceAnimationState.Glide) return walk[direction * 4 + frame % 4];
            bool west = facing == EurydiceFacing.West || facing == EurydiceFacing.NorthWest || facing == EurydiceFacing.SouthWest;
            if (state == EurydiceAnimationState.StairAscent) return stairs[(west ? 1 : 0) * 6 + frame % 6];
            if (state == EurydiceAnimationState.StairDescent) return stairs[(west ? 3 : 2) * 6 + frame % 6];
            int row = state == EurydiceAnimationState.Joy ? 0 : state == EurydiceAnimationState.Fear ? 1
                : state == EurydiceAnimationState.Listen ? 2 : state == EurydiceAnimationState.Teaching ? 3
                : state == EurydiceAnimationState.Sanctuary ? 4 : 5;
            return reactions[row * 6 + (state == EurydiceAnimationState.Reform ? 5 - frame % 6 : frame % 6)];
        }

        public static int FrameCount(EurydiceAnimationState state)
            => state == EurydiceAnimationState.Idle || state == EurydiceAnimationState.Glide || state == EurydiceAnimationState.Turn ? 4 : 6;
        public static bool Loops(EurydiceAnimationState state) => state == EurydiceAnimationState.Idle || state == EurydiceAnimationState.Glide ||
            state == EurydiceAnimationState.StairAscent || state == EurydiceAnimationState.StairDescent || state == EurydiceAnimationState.Teaching;
        public static float Fps(EurydiceAnimationState state) => state == EurydiceAnimationState.Idle ? 1.6f
            : state == EurydiceAnimationState.Turn ? 22f : state == EurydiceAnimationState.Teaching ? 3.5f
            : state == EurydiceAnimationState.Glide ? 8f : state == EurydiceAnimationState.StairAscent || state == EurydiceAnimationState.StairDescent ? 6f : 7f;
    }
}
