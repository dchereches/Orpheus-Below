using UnityEngine;

namespace OrpheusBelow
{
    public enum PaintedEnemyKind { Cerberus, HoodedSkeleton, MaskedMonk, MysticEyeWarden, WingedSentinel, RoosterRider, Tripod, Shepherd, Sheep }
    public enum EnemyMotion { Idle, Move, Windup, Attack, Attack2, Recover, Hurt, Defeated }

    public sealed class EnemyArtLibrary : ScriptableObject
    {
        public PaintedEnemyKind kind;
        public Sprite[] body, effects;
        public int bodyColumns = 6, effectColumns = 6;
        public int[] motionStarts, motionCounts;
        public float visibleHeight = 2.5f;
        public bool IsComplete => body != null && motionStarts?.Length == 8 && motionCounts?.Length == 8 &&
            body.Length >= 16 && effects != null && effects.Length == effectColumns * 4;
        public int FrameCount(EnemyMotion motion) => motionCounts != null && motionCounts.Length == 8 ? motionCounts[(int)motion] : bodyColumns;
        public Sprite Frame(EnemyMotion motion, int frame) => body[(motionStarts != null && motionStarts.Length == 8 ? motionStarts[(int)motion] : (int)motion * bodyColumns) + Mathf.Clamp(frame, 0, FrameCount(motion) - 1)];
        public Sprite Effect(int row, int frame) => effects[Mathf.Clamp(row, 0, 3) * effectColumns + Mathf.Clamp(frame, 0, effectColumns - 1)];
        public static EnemyArtLibrary Load(PaintedEnemyKind kind) => Resources.Load<EnemyArtLibrary>("Enemies/" + kind + "/Library");
    }
}
