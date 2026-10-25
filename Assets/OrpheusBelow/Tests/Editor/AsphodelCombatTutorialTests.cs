using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class AsphodelCombatTutorialTests
    {
        private static readonly AsphodelTutorialSnapshot Baseline =
            new AsphodelTutorialSnapshot(dashUseCount: 4, softNoteCount: 7, hardBoomCount: 3, stanceEntryCount: 2);

        [Test]
        public void RequiresMeaningfulMovementBeforeDash()
        {
            AsphodelCombatTutorial tutorial = new AsphodelCombatTutorial(Baseline, Vector3.zero);
            AsphodelTutorialSnapshot dashAlreadyUsed =
                new AsphodelTutorialSnapshot(5, 7, 3, 2);

            Assert.That(tutorial.Observe(dashAlreadyUsed, new Vector3(1.24f, 0f, 0f)), Is.False);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Move));

            Assert.That(tutorial.Observe(dashAlreadyUsed, new Vector3(1.25f, 0f, 0f)), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Dash));

            Assert.That(tutorial.Observe(dashAlreadyUsed, new Vector3(1.25f, 0f, 0f)), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.SoftNote));
        }

        [Test]
        public void AdvancesOnlyOnMatchingActionCounter()
        {
            AsphodelCombatTutorial tutorial = new AsphodelCombatTutorial(Baseline, Vector3.zero);
            tutorial.Observe(Baseline, new Vector3(1.25f, 0f, 0f));

            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(4, 8, 4, 3), Vector3.zero), Is.False);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Dash));

            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(5, 8, 4, 3), Vector3.zero), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.SoftNote));

            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(5, 8, 4, 3), Vector3.zero), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.HardBoom));

            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(5, 8, 4, 3), Vector3.zero), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Flute));
        }

        [Test]
        public void CompletesAfterFluteEntry()
        {
            AsphodelCombatTutorial tutorial = new AsphodelCombatTutorial(Baseline, Vector3.zero);
            tutorial.Observe(Baseline, new Vector3(1.25f, 0f, 0f));
            tutorial.Observe(new AsphodelTutorialSnapshot(5, 7, 3, 2), Vector3.zero);
            tutorial.Observe(new AsphodelTutorialSnapshot(5, 8, 3, 2), Vector3.zero);
            tutorial.Observe(new AsphodelTutorialSnapshot(5, 8, 4, 2), Vector3.zero);

            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Flute));
            Assert.That(tutorial.Observe(new AsphodelTutorialSnapshot(5, 8, 4, 3), Vector3.zero), Is.True);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Complete));
            Assert.That(tutorial.IsComplete, Is.True);
        }

        [Test]
        public void IgnoresPreTutorialActionCounts()
        {
            AsphodelCombatTutorial tutorial = new AsphodelCombatTutorial(Baseline, Vector3.zero);

            tutorial.Observe(Baseline, new Vector3(1.25f, 0f, 0f));

            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Dash));
            Assert.That(tutorial.Observe(Baseline, Vector3.zero), Is.False);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(AsphodelTutorialStep.Dash));
        }
    }
}
