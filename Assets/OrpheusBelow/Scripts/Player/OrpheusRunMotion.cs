using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Running skeleton in a vertical sagittal plane: x forward, y height above ground.
    /// The baker projects this plane through each view, so forward knees shorten naturally in front views.</summary>
    public static class OrpheusRunMotion
    {
        public const float CycleDistance = 5.8f;
        public const float SupportDuty = .34f;
        public struct Limb { public Vector2 root, joint, end; public bool support; }
        public struct Pose
        {
            public float hipHeight, hipBob, pitch;
            public Limb nearLeg, farLeg, nearArm, farArm;
        }
        /// <summary>Fits one fixed skeleton to the upright painting before posing it. Long ground
        /// reach must bend a taller leg, rather than force the painted torso down into a crouch.</summary>
        public static Vector2 FitLegLengths(float sourceUpper, float sourceLower, float sourceHipToAnkleHeight)
        {
            if (sourceUpper <= 0f || sourceLower <= 0f || sourceHipToAnkleHeight <= .06f)
                throw new System.ArgumentOutOfRangeException(nameof(sourceUpper), "An upright source rig requires positive leg bones and hip height.");
            float desiredHeight = sourceHipToAnkleHeight - .06f;
            float span = CycleDistance * SupportDuty * .5f;
            float requiredTotal = Mathf.Sqrt((desiredHeight + .035f) * (desiredHeight + .035f) + span * span) / .94f;
            float scale = requiredTotal / (sourceUpper + sourceLower);
            return new Vector2(sourceUpper * scale, sourceLower * scale);
        }

        public static Pose Evaluate(float phase, float upperLeg = 1.08f, float lowerLeg = 1.02f,
            float upperArm = .52f, float lowerArm = .48f)
        {
            phase = Mathf.Repeat(phase, 1f);
            float half = Mathf.Repeat(phase, .5f);
            float span = CycleDistance * SupportDuty * .5f;
            float length = upperLeg + lowerLeg;
            if (upperLeg <= 0f || lowerLeg <= 0f || upperArm <= 0f || lowerArm <= 0f || length <= span * 1.1f)
                throw new System.ArgumentOutOfRangeException(nameof(upperLeg), "The painted rig must have positive bones and enough reach for the run stride.");
            // Leave room at contact for the long forward reach. Compression is in the knee,
            // never a scale change to the painted thigh or calf.
            float baseHeight = Mathf.Sqrt(length * length * .94f * .94f - span * span) - .035f;
            float bob = half < SupportDuty
                ? -.055f * Mathf.Sin(half / SupportDuty * Mathf.PI)
                : .22f * Mathf.Sin((half - SupportDuty) / (.5f - SupportDuty) * Mathf.PI);
            bob *= Mathf.Min(1f, length / 2.1f);
            float height = baseHeight + bob;
            return new Pose
            {
                hipHeight = height,
                hipBob = bob,
                pitch = .085f + .022f * Mathf.Cos(phase * Mathf.PI * 4f),
                nearLeg = Leg(phase, height, upperLeg, lowerLeg),
                farLeg = Leg(Mathf.Repeat(phase + .5f, 1f), height, upperLeg, lowerLeg),
                nearArm = Arm(phase, upperArm, lowerArm),
                farArm = Arm(Mathf.Repeat(phase + .5f, 1f), upperArm, lowerArm)
            };
        }

        private static Limb Leg(float phase, float hipHeight, float upper, float lower)
        {
            float span = CycleDistance * SupportDuty * .5f;
            Vector2 ankle;
            bool support = phase < SupportDuty;
            if (support) ankle = new Vector2(span - CycleDistance * phase, 0f);
            else if (phase < .50f)
                ankle = Curve(new Vector2(-span, 0f), new Vector2(-1.16f, .55f),
                    new Vector2(-.85f, 1.24f), (phase - SupportDuty) / (.50f - SupportDuty));
            else if (phase < .73f)
                ankle = Curve(new Vector2(-.85f, 1.24f), new Vector2(-.12f, 1.45f),
                    new Vector2(.52f, .64f), (phase - .50f) / .23f);
            else
                ankle = Curve(new Vector2(.52f, .64f), new Vector2(1.19f, .08f),
                    new Vector2(span, 0f), (phase - .73f) / .27f);
            // A shorter painted leg folds at a proportionally lower heel height; forcing the
            // same metre-high path can put its ankle inside the knee's minimum reach circle.
            ankle.y *= Mathf.Min(1f, (upper + lower) / 2.1f);
            Vector2 root = new Vector2(0f, hipHeight);
            return new Limb { root = root, joint = Joint(root, ankle, upper, lower, Vector2.right), end = ankle, support = support };
        }

        private static Limb Arm(float phase, float upper, float lower)
        {
            // Swing the upper arm below the shoulder and retain a 90-degree elbow. The
            // forearm lifts the advancing fist toward the chest and folds the rear fist
            // toward the waist, instead of pulling the elbow out into a horizontal wing.
            float alpha = -Mathf.Cos(phase * Mathf.PI * 2f) * .85f;
            Vector2 elbow = new Vector2(Mathf.Sin(alpha) * upper, -Mathf.Cos(alpha) * upper);
            Vector2 forearm = new Vector2(Mathf.Cos(alpha) * lower, Mathf.Sin(alpha) * lower);
            return new Limb { root = Vector2.zero, joint = elbow, end = elbow + forearm };
        }

        private static Vector2 Curve(Vector2 from, Vector2 control, Vector2 to, float t)
            => (1f - t) * (1f - t) * from + 2f * (1f - t) * t * control + t * t * to;

        private static Vector2 Joint(Vector2 root, Vector2 end, float upper, float lower, Vector2 bend)
        {
            Vector2 delta = end - root;
            float distance = delta.magnitude;
            if (distance >= upper + lower || distance <= Mathf.Abs(upper - lower))
                throw new System.ArgumentOutOfRangeException(nameof(end), "Run target lies outside the constant-length two-bone reach.");
            Vector2 direction = delta / distance;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            float along = (upper * upper - lower * lower + distance * distance) / (2f * distance);
            float across = Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));
            return root + direction * along + normal * across * (Vector2.Dot(normal, bend) >= 0f ? 1f : -1f);
        }
    }
}
