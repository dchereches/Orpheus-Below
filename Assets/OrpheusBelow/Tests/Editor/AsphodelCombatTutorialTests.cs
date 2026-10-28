using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class AsphodelCombatTutorialTests
    {
        private static readonly AsphodelTutorialSnapshot Baseline = new AsphodelTutorialSnapshot(4, 7, 3, 2, 0);

        [Test]
        public void RequiresMeaningfulMovementAndANewSoftNoteAfterThePrompt()
        {
            var tutorial = new AsphodelCombatTutorial(Baseline, Vector3.zero);
            var earlySoftNote = new AsphodelTutorialSnapshot(4, 8, 3, 2, 0);
            Assert.That(tutorial.Observe(earlySoftNote, new Vector3(1.24f, 0f, 0f)), Is.False);
            Assert.That(tutorial.Observe(earlySoftNote, new Vector3(1.25f, 0f, 0f)), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.SoftNote));
            Assert.That(tutorial.Observe(earlySoftNote, Vector3.zero), Is.False);
            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(4, 9, 3, 2, 0), Vector3.zero), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.HardBoom));
        }

        [Test]
        public void PreplayedActionsCannotSkipLaterPrompts()
        {
            var tutorial = new AsphodelCombatTutorial(Baseline, Vector3.zero);
            var allEarly = new AsphodelTutorialSnapshot(8, 12, 10, 5, 2);
            tutorial.Observe(allEarly, Vector3.right * 2f);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.SoftNote));
            Assert.That(tutorial.Observe(allEarly, Vector3.zero), Is.False);
            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(8, 12, 11, 6, 3), Vector3.zero), Is.False);
            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(8, 13, 11, 6, 3), Vector3.zero), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.HardBoom));
        }

        [Test]
        public void TutorialProgressionFlowsThroughAllStepsToCompletion()
        {
            var session = new AsphodelTutorialSession();
            session.TryBegin(Baseline, Vector3.zero);
            // 1. Move
            session.Observe(Baseline, Vector3.right * 2f);
            Assert.That(session.CurrentStep, Is.EqualTo(AsphodelTutorialStep.SoftNote));
            // 2. Soft Note
            session.Observe(new AsphodelTutorialSnapshot(4, 8, 3, 2, 0), Vector3.zero);
            Assert.That(session.CurrentStep, Is.EqualTo(AsphodelTutorialStep.HardBoom));
            // 3. Hard Boom
            session.Observe(new AsphodelTutorialSnapshot(4, 8, 4, 2, 0), Vector3.zero);
            Assert.That(session.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Flute));
            // 4. Flute Stance
            session.Observe(new AsphodelTutorialSnapshot(4, 8, 4, 3, 0), Vector3.zero);
            Assert.That(session.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Dash));
            // 5. Dash
            session.Observe(new AsphodelTutorialSnapshot(5, 8, 4, 3, 0), Vector3.zero);
            Assert.That(session.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Cadence));
            Assert.That(session.HasCompleted, Is.False);
            // 6. Cadence
            session.Observe(new AsphodelTutorialSnapshot(5, 8, 4, 3, 1), Vector3.zero);
            Assert.That(session.HasCompleted, Is.True);
            Assert.That(session.CurrentStepId, Is.EqualTo("complete"));
            Assert.That(session.TryBegin(Baseline, Vector3.zero), Is.False);
        }
    }
}
