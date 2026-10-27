using UnityEngine;

namespace OrpheusBelow
{
    public enum EurydiceFacing { South, SouthEast, East, NorthEast, North, NorthWest, West, SouthWest }

    public struct EurydiceAnimationInput
    {
        public Vector3 groundDelta;
        public float deltaTime;
        public bool paused, vanished, reforming, threatened, shield, joy, teaching, replying, onStairs, ascending;
    }

    /// <summary>Animation decisions use ground travel, never the companion's decorative vertical bob.</summary>
    public sealed class EurydiceAnimationBrain
    {
        public EurydiceAnimationState State { get; private set; } = EurydiceAnimationState.Idle;
        public EurydiceFacing Facing { get; private set; } = EurydiceFacing.South;
        public EurydiceFacing TurnFrom { get; private set; } = EurydiceFacing.South;
        public float GroundSpeed { get; private set; }
        private bool _moving;
        private float _stillTime, _turnTime;

        public void ResetMotion()
        {
            GroundSpeed = 0; _moving = false; _stillTime = 0; _turnTime = 0;
            State = EurydiceAnimationState.Idle;
        }

        public void Step(EurydiceAnimationInput input)
        {
            if (input.paused || input.deltaTime <= 0f) return;
            Vector3 delta = input.groundDelta; delta.y = 0;
            GroundSpeed = delta.magnitude / input.deltaTime;
            _turnTime = Mathf.Max(0, _turnTime - input.deltaTime);
            if (GroundSpeed >= .28f) { _moving = true; _stillTime = 0; }
            else if (GroundSpeed < .12f)
            {
                _stillTime += input.deltaTime;
                if (_stillTime >= .12f) _moving = false;
            }
            else _stillTime = 0;
            if (_moving && GroundSpeed >= .12f)
            {
                EurydiceFacing next = FacingFor(delta, Facing);
                if (next != Facing)
                {
                    int sectors = Mathf.Abs((int)next - (int)Facing);
                    if (Mathf.Min(sectors, 8 - sectors) >= 2) { TurnFrom = Facing; _turnTime = .18f; }
                    Facing = next;
                }
            }
            State = input.vanished ? EurydiceAnimationState.Shatter
                : input.reforming ? EurydiceAnimationState.Reform
                : input.threatened ? EurydiceAnimationState.Fear
                : input.shield ? EurydiceAnimationState.Sanctuary
                : input.joy ? EurydiceAnimationState.Joy
                : input.teaching ? EurydiceAnimationState.Teaching
                : input.replying ? EurydiceAnimationState.Listen
                : _moving && input.onStairs ? (input.ascending ? EurydiceAnimationState.StairAscent : EurydiceAnimationState.StairDescent)
                : _moving && _turnTime > 0 ? EurydiceAnimationState.Turn
                : _moving ? EurydiceAnimationState.Glide : EurydiceAnimationState.Idle;
        }

        public static EurydiceFacing FacingFor(Vector3 worldDirection, EurydiceFacing previous)
        {
            worldDirection.y = 0;
            if (worldDirection.sqrMagnitude < .000001f) return previous;
            float x = Vector3.Dot(worldDirection, UnderworldCamera2D.IsoRightXZ);
            float y = Vector3.Dot(worldDirection, UnderworldCamera2D.IsoUpXZ);
            float angle = Mathf.Atan2(x, -y) * Mathf.Rad2Deg;
            // Five degrees of hysteresis prevent sector-edge jitter from flipping the sprite.
            if (Mathf.Abs(Mathf.DeltaAngle((int)previous * 45f, angle)) <= 27.5f) return previous;
            return (EurydiceFacing)((Mathf.RoundToInt(angle / 45f) + 8) % 8);
        }
    }
}
