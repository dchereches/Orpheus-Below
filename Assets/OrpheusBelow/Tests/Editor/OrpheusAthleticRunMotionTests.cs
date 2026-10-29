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
        public void SwingMeetsPlantAndToeOffAtTheGroundRelativeFootSpeed()
        {
            const float sample = .0001f;
            var beforeContact = OrpheusRunMotion.Evaluate(1f - sample).nearLeg.end;
            var contact = OrpheusRunMotion.Evaluate(0f).nearLeg.end;
            var toeOff = OrpheusRunMotion.Evaluate(OrpheusRunMotion.SupportDuty).nearLeg.end;
            var afterToeOff = OrpheusRunMotion.Evaluate(OrpheusRunMotion.SupportDuty + sample).nearLeg.end;
            Assert.That((contact.x - beforeContact.x) / sample, Is.EqualTo(-5.8f).Within(.04f),
                "The descending foot must already travel backward at ground speed when it plants.");
            Assert.That((afterToeOff.x - toeOff.x) / sample, Is.EqualTo(-5.8f).Within(.04f),
                "The recovery must continue the planted foot's motion rather than snagging at toe-off.");
            Assert.That(Mathf.Abs((contact.y - beforeContact.y) / sample), Is.LessThan(.02f));
            Assert.That(Mathf.Abs((afterToeOff.y - toeOff.y) / sample), Is.LessThan(.02f));
        }

        [TestCase(.50f)]
        [TestCase(.73f)]
        public void RecoveryChangesDirectionWithoutVelocitySnaps(float phase)
        {
            const float sample = .0001f;
            Vector2 before = OrpheusRunMotion.Evaluate(phase - sample).nearLeg.end;
            Vector2 at = OrpheusRunMotion.Evaluate(phase).nearLeg.end;
            Vector2 after = OrpheusRunMotion.Evaluate(phase + sample).nearLeg.end;
            Assert.That(Vector2.Distance((at - before) / sample, (after - at) / sample), Is.LessThan(.08f),
                "The heel fold and forward extension must share velocity at their boundary.");
        }

        [Test]
        public void LegsExchangeIdenticalStridesWithAlternatingSingleSupport()
        {
            int nearPlants = 0, farPlants = 0, flights = 0;
            for (int i = 0; i < 256; i++)
            {
                float phase = i / 256f;
                var pose = OrpheusRunMotion.Evaluate(phase);
                var opposite = OrpheusRunMotion.Evaluate(phase + .5f);
                Assert.That(pose.nearLeg.support && pose.farLeg.support, Is.False,
                    "Running alternates single support with flight, rather than shuffling on both boots.");
                Assert.That(Vector2.Distance(pose.nearLeg.end, opposite.farLeg.end), Is.LessThan(.00001f));
                Assert.That(Vector2.Distance(pose.nearLeg.joint, opposite.farLeg.joint), Is.LessThan(.00001f));
                if (pose.nearLeg.support) nearPlants++;
                if (pose.farLeg.support) farPlants++;
                if (!pose.nearLeg.support && !pose.farLeg.support) flights++;
            }
            Assert.That(nearPlants, Is.EqualTo(farPlants));
            Assert.That(nearPlants, Is.InRange(80, 95));
            Assert.That(flights, Is.InRange(70, 90));
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
        public void AsymmetricNorthRigNeverFoldsInsideItsMinimumReach()
        {
            // Actual upright North painting after fitting. Its .199-unit minimum ankle
            // reach is much larger than the .06-unit minimum of the default skeleton.
            const float upper = .9240033f, lower = .7250422f;
            for (int i = 0; i < 4096; i++)
            {
                var pose = OrpheusRunMotion.Evaluate(i / 4096f, upper, lower);
                foreach (var leg in new[] { pose.nearLeg, pose.farLeg })
                {
                    Assert.That(Vector2.Distance(leg.root, leg.end), Is.GreaterThan(Mathf.Abs(upper - lower)));
                    Assert.That(Vector2.Distance(leg.root, leg.joint), Is.EqualTo(upper).Within(.00001f));
                    Assert.That(Vector2.Distance(leg.joint, leg.end), Is.EqualTo(lower).Within(.00001f));
                }
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
