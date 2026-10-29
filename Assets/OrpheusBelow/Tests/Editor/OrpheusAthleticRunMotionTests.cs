using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class OrpheusAthleticRunMotionTests
    {
        [Test]
        public void SupportFootCancelsActualRootTravel()
        {
            var a = OrpheusRunMotion.Evaluate(.02f);
            var b = OrpheusRunMotion.Evaluate(.30f);
            Assert.That(a.nearLeg.support && b.nearLeg.support, Is.True);
            Assert.That(b.nearLeg.end.x + 5.8f * .28f, Is.EqualTo(a.nearLeg.end.x).Within(.00001f));
            Assert.That(a.nearLeg.end.y, Is.EqualTo(0f));
            Assert.That(b.nearLeg.end.y, Is.EqualTo(0f));
        }

        [TestCase(.40f)]
        [TestCase(.90f)]
        public void FlightHasBothBootsClearOfGround(float phase)
        {
            var p = OrpheusRunMotion.Evaluate(phase);
            Assert.That(p.nearLeg.support || p.farLeg.support, Is.False);
            Assert.That(p.nearLeg.end.y, Is.GreaterThan(.02f));
            Assert.That(p.farLeg.end.y, Is.GreaterThan(.02f));
            Assert.That(p.hipBob, Is.GreaterThan(.15f));
        }

        [Test]
        public void RecoveryFoldsHeelThenDrivesFootForward()
        {
            var kick = OrpheusRunMotion.Evaluate(.50f);
            var drive = OrpheusRunMotion.Evaluate(.73f);
            Assert.That(kick.nearLeg.end.y, Is.GreaterThan(1f));
            Assert.That(kick.nearLeg.end.x, Is.LessThan(-.5f));
            Assert.That(drive.nearLeg.joint.x, Is.GreaterThan(.25f));
            Assert.That(drive.nearLeg.end.x, Is.GreaterThan(.25f));
        }

        [Test]
        public void ElbowsCounterTheIpsilateralLegAndDriveTheWristForward()
        {
            var contact = OrpheusRunMotion.Evaluate(0f);
            var opposite = OrpheusRunMotion.Evaluate(.5f);
            Assert.That(contact.nearLeg.end.x, Is.GreaterThan(.8f));
            Assert.That(contact.nearArm.joint.x, Is.LessThan(-.30f));
            Assert.That(contact.nearArm.joint.y, Is.LessThan(-.25f), "Back elbow stays below the shoulder instead of forming a horizontal wing.");
            Assert.That(contact.farArm.joint.x, Is.GreaterThan(.30f));
            Assert.That(opposite.nearArm.joint.x, Is.GreaterThan(.30f));
            Assert.That(opposite.nearArm.end.x - contact.nearArm.end.x, Is.GreaterThan(.70f));
            Assert.That(opposite.nearArm.end.y - contact.nearArm.end.y, Is.GreaterThan(.65f));
        }

        [Test]
        public void ElbowsStayAtRightAnglesThroughoutThePump()
        {
            for (int i = 0; i < 256; i++)
            {
                var p = OrpheusRunMotion.Evaluate(i / 256f);
                foreach (var arm in new[] { p.nearArm, p.farArm })
                {
                    Vector2 upper = (arm.joint - arm.root).normalized;
                    Vector2 lower = (arm.end - arm.joint).normalized;
                    Assert.That(Vector2.Dot(upper, lower), Is.EqualTo(0f).Within(.00001f));
                    Assert.That(arm.joint.y, Is.LessThan(-.25f));
                }
            }
        }

        [Test]
        public void EveryPosePreservesBothBonesOfEveryLimb()
        {
            for (int i = 0; i < 256; i++)
            {
                var p = OrpheusRunMotion.Evaluate(i / 256f, 1.08f, 1.02f, .52f, .48f);
                foreach (var leg in new[] { p.nearLeg, p.farLeg })
                {
                    Assert.That(Vector2.Distance(leg.root, leg.joint), Is.EqualTo(1.08f).Within(.00001f));
                    Assert.That(Vector2.Distance(leg.joint, leg.end), Is.EqualTo(1.02f).Within(.00001f));
                }
                foreach (var arm in new[] { p.nearArm, p.farArm })
                {
                    Assert.That(Vector2.Distance(arm.root, arm.joint), Is.EqualTo(.52f).Within(.00001f));
                    Assert.That(Vector2.Distance(arm.joint, arm.end), Is.EqualTo(.48f).Within(.00001f));
                }
            }
        }

        [Test]
        public void CanonicalUprightPelvisHeightSurvivesLongContactReach()
        {
            // East source: hip (670,675), knee (757,979), ankle (808,1218), ppu 310.
            float upper = new Vector2(87f, 304f).magnitude / 310f;
            float lower = new Vector2(51f, 239f).magnitude / 310f;
            Vector2 fitted = OrpheusRunMotion.FitLegLengths(upper, lower, 543f / 310f);
            var contact = OrpheusRunMotion.Evaluate(0f, fitted.x, fitted.y);
            Assert.That(contact.hipHeight, Is.EqualTo(1.691613f).Within(.00001f),
                "The contact pelvis may settle .06 units from the upright source, not drop into a crouch.");
            Assert.That(fitted.x / fitted.y, Is.EqualTo(upper / lower).Within(.00001f));
            for (int i = 0; i < 256; i++)
            {
                var pose = OrpheusRunMotion.Evaluate(i / 256f, fitted.x, fitted.y);
                Assert.That(Vector2.Distance(pose.nearLeg.root, pose.nearLeg.joint), Is.EqualTo(fitted.x).Within(.00001f));
                Assert.That(Vector2.Distance(pose.nearLeg.joint, pose.nearLeg.end), Is.EqualTo(fitted.y).Within(.00001f));
            }
        }

        [Test]
        public void PaintedLegProportionsRemainReachableThroughoutRecovery()
        {
            foreach (float length in new[] { 1.6f, 1.8f, 2.1f, 2.4f })
                for (int i = 0; i < 256; i++)
                {
                    var p = OrpheusRunMotion.Evaluate(i / 256f, length * .46f, length * .54f);
                    Assert.That(Vector2.Distance(p.nearLeg.root, p.nearLeg.joint), Is.EqualTo(length * .46f).Within(.00001f));
                    Assert.That(Vector2.Distance(p.nearLeg.joint, p.nearLeg.end), Is.EqualTo(length * .54f).Within(.00001f));
                }
        }

        [Test]
        public void CycleClosesWithoutPosePop()
        {
            var a = OrpheusRunMotion.Evaluate(0f);
            var b = OrpheusRunMotion.Evaluate(1f);
            var approaching = OrpheusRunMotion.Evaluate(.99999f);
            Assert.That(Vector2.Distance(a.nearLeg.end, b.nearLeg.end), Is.LessThan(.00001f));
            Assert.That(Vector2.Distance(a.nearLeg.end, approaching.nearLeg.end), Is.LessThan(.001f));
            Assert.That(Mathf.Abs(a.hipHeight - approaching.hipHeight), Is.LessThan(.001f));
        }
    }
}
