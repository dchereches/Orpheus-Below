using UnityEngine;

namespace OrpheusBelow
{
    public struct OrpheusAnimationInput
    {
        public Vector3 groundDelta;
        public Vector3 aimDirection;
        public float deltaTime;
        public bool paused;
        public bool dashing;
        public bool onStairs;
        public bool ascending;
        public bool lightHurt;
        public bool heavyHurt;
        public bool defeated;
        public bool bossStage;
        public OrpheusAnimationState action;
    }

    /// <summary>Renderer-independent animation choice using ground travel and explicit gameplay priorities.</summary>
    public sealed class OrpheusAnimationBrain
    {
        public OrpheusAnimationState State { get; private set; } = OrpheusAnimationState.Idle;
        public EurydiceFacing Facing { get; private set; } = EurydiceFacing.South;
        public float GroundSpeed { get; private set; }
        public float ActionElapsed { get; private set; }
        public int StateVersion { get; private set; }

        private bool _moving;
        private float _stillTime;

        public void ResetMotion()
        {
            GroundSpeed = 0f;
            ActionElapsed = 0f;
            _moving = false;
            _stillTime = 0f;
            SetState(OrpheusAnimationState.Idle);
        }

        public void Step(OrpheusAnimationInput input)
        {
            if (input.paused || input.deltaTime <= 0f) return;

            Vector3 ground = input.groundDelta;
            ground.y = 0f;
            bool wasMoving = _moving;
            EurydiceFacing previousFacing = Facing;
            GroundSpeed = ground.magnitude / input.deltaTime;
            if (GroundSpeed >= .28f)
            {
                _moving = true;
                _stillTime = 0f;
            }
            else if (GroundSpeed < .12f)
            {
                _stillTime += input.deltaTime;
                if (_stillTime >= .12f) _moving = false;
            }
            else _stillTime = 0f;

            bool directedAction = input.action != OrpheusAnimationState.None;
            bool terminalAction = input.action == OrpheusAnimationState.LookBackFatal ||
                input.action == OrpheusAnimationState.EurydiceGrief;
            Vector3 facingVector = directedAction ? input.aimDirection : ground;
            if (input.bossStage && Mathf.Abs(Vector3.Dot(input.aimDirection, UnderworldCamera2D.IsoRightXZ)) > .001f)
                facingVector = Mathf.Sign(Vector3.Dot(input.aimDirection, UnderworldCamera2D.IsoRightXZ)) * UnderworldCamera2D.IsoRightXZ;
            if (facingVector.sqrMagnitude > .000001f) Facing = FacingFor(facingVector, Facing);

            OrpheusAnimationState locomotion;
            if (State == OrpheusAnimationState.Start && _moving && ActionElapsed + input.deltaTime < .16f)
                locomotion = OrpheusAnimationState.Start;
            else if (State == OrpheusAnimationState.Stop && !_moving && ActionElapsed + input.deltaTime < .16f)
                locomotion = OrpheusAnimationState.Stop;
            else if (State == OrpheusAnimationState.Turn && _moving && ActionElapsed + input.deltaTime < .12f)
                locomotion = OrpheusAnimationState.Turn;
            else if (!wasMoving && _moving)
                locomotion = OrpheusAnimationState.Start;
            else if (wasMoving && !_moving)
                locomotion = OrpheusAnimationState.Stop;
            else if (_moving && previousFacing != Facing)
                locomotion = OrpheusAnimationState.Turn;
            else if (_moving && GroundSpeed >= 4.5f)
                locomotion = OrpheusAnimationState.Run;
            else if (_moving)
                locomotion = OrpheusAnimationState.Walk;
            else
                locomotion = State == OrpheusAnimationState.Idle && ActionElapsed >= 2.5f
                    ? OrpheusAnimationState.RestlessIdle : OrpheusAnimationState.Idle;

            OrpheusAnimationState next = terminalAction ? input.action
                : input.defeated ? OrpheusAnimationState.Defeat
                : input.heavyHurt ? OrpheusAnimationState.HeavyHit
                : input.lightHurt ? OrpheusAnimationState.LightHit
                : directedAction ? input.action
                : input.dashing ? OrpheusAnimationState.DashTravel
                : _moving && input.onStairs ? (input.ascending ? OrpheusAnimationState.StairAscent : OrpheusAnimationState.StairDescent)
                : locomotion;

            if (next == State) ActionElapsed += input.deltaTime;
            else SetState(next);
        }

        public static EurydiceFacing FacingFor(Vector3 worldDirection, EurydiceFacing previous)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < .000001f) return previous;
            float x = Vector3.Dot(worldDirection, UnderworldCamera2D.IsoRightXZ);
            float y = Vector3.Dot(worldDirection, UnderworldCamera2D.IsoUpXZ);
            float angle = Mathf.Atan2(x, -y) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle((int)previous * 45f, angle)) <= 27.5f) return previous;
            return (EurydiceFacing)((Mathf.RoundToInt(angle / 45f) + 8) % 8);
        }

        private void SetState(OrpheusAnimationState state)
        {
            if (State == state) return;
            State = state;
            ActionElapsed = 0f;
            StateVersion++;
        }
    }
}
