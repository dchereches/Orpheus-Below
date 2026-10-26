using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class AsphodelCombatTutorialTests
    {
        private static readonly AsphodelTutorialSnapshot Baseline = new AsphodelTutorialSnapshot(4, 7, 3, 2);

        [Test]
        public void RequiresMeaningfulMovementAndANewDashAfterThePrompt()
        {
            var tutorial = new AsphodelCombatTutorial(Baseline, Vector3.zero);
            var earlyDash = new AsphodelTutorialSnapshot(5, 7, 3, 2);
            Assert.That(tutorial.Observe(earlyDash, new Vector3(1.24f, 0f, 0f)), Is.False);
            Assert.That(tutorial.Observe(earlyDash, new Vector3(1.25f, 0f, 0f)), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Dash));
            Assert.That(tutorial.Observe(earlyDash, Vector3.zero), Is.False);
            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(6, 7, 3, 2), Vector3.zero), Is.True);
        }

        [Test]
        public void PreplayedActionsCannotSkipLaterPrompts()
        {
            var tutorial = new AsphodelCombatTutorial(Baseline, Vector3.zero);
            var allEarly = new AsphodelTutorialSnapshot(8, 12, 10, 5, 2);
            tutorial.Observe(allEarly, Vector3.right * 2f);
            Assert.That(tutorial.Observe(allEarly, Vector3.zero), Is.False);
            tutorial.Observe(new AsphodelTutorialSnapshot(9, 12, 10, 5, 2), Vector3.zero);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.SoftNote));
            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(9, 12, 11, 6, 3), Vector3.zero), Is.False);
            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(9, 13, 11, 6, 3), Vector3.zero), Is.True);
            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(9, 13, 11, 6, 3), Vector3.zero), Is.False);
        }

        [Test]
        public void FluteEntryDoesNotFinishUntilThePlayerPerformsACadence()
        {
            var session = new AsphodelTutorialSession();
            session.TryBegin(Baseline, Vector3.zero);
            session.Observe(Baseline, Vector3.right * 2f);
            session.Observe(new AsphodelTutorialSnapshot(5, 7, 3, 2), Vector3.zero);
            session.Observe(new AsphodelTutorialSnapshot(5, 8, 3, 2), Vector3.zero);
            session.Observe(new AsphodelTutorialSnapshot(5, 8, 4, 2), Vector3.zero);
            session.Observe(new AsphodelTutorialSnapshot(5, 8, 4, 3), Vector3.zero);
            Assert.That(session.HasCompleted, Is.False);
            Assert.That(session.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Cadence));
            session.Observe(new AsphodelTutorialSnapshot(5, 10, 5, 3, 1), Vector3.zero);
            Assert.That(session.HasCompleted, Is.True);
            Assert.That(session.CurrentStepId, Is.EqualTo("complete"));
            Assert.That(session.TryBegin(Baseline, Vector3.zero), Is.False);
        }
    }
}
