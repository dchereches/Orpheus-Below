using NUnit.Framework;
namespace OrpheusBelow.Tests
{
    public class VoiceInteractionTests
    {
        [TestCase(VoiceLineId.Eurydice_IamHere)]
        [TestCase(VoiceLineId.Eurydice_HearTheNote)]
        [TestCase(VoiceLineId.Eurydice_RightHere)]
        [TestCase(VoiceLineId.Eurydice_AreYouStillThere)]
        [TestCase(VoiceLineId.Eurydice_LookAtMe)]
        public void RecurringRecognitionChatterIsSuppressed(VoiceLineId id)
        { Assert.That(VoiceLinePolicy.IsRecognitionChatter(id), Is.True); }

        [TestCase(VoiceLineId.Eurydice_Orpheus)]
        [TestCase(VoiceLineId.Eurydice_CannotFeel)]
        [TestCase(VoiceLineId.Narrator_FoundHer)]
        [TestCase(VoiceLineId.Hades_TakeHer)]
        public void ReunionStoryKeepsItsBlockingSpeech(VoiceLineId id)
        {
            Assert.That(VoiceLinePolicy.IsRecognitionChatter(id), Is.False);
            Assert.That(VoiceLinePolicy.IsInteractiveInstruction(id), Is.False);
        }

        [TestCase(AsphodelTutorialStep.Move, VoiceLineId.Narrator_ClearChambers)]
        [TestCase(AsphodelTutorialStep.SoftNote, VoiceLineId.Narrator_DefendYourself)]
        [TestCase(AsphodelTutorialStep.HardBoom, VoiceLineId.Narrator_RepellingMove)]
        [TestCase(AsphodelTutorialStep.Flute, VoiceLineId.Narrator_CombosFlee)]
        [TestCase(AsphodelTutorialStep.Dash, VoiceLineId.Narrator_PressF)]
        [TestCase(AsphodelTutorialStep.Cadence, VoiceLineId.Narrator_PluckCadences)]
        public void LessonsAcceptTheirCorrespondingCompletedAction(AsphodelTutorialStep step, VoiceLineId id)
        {
            Assert.That(VoiceLinePolicy.InstructionFor(step), Is.EqualTo(id));
            Assert.That(VoiceLinePolicy.IsInteractiveInstruction(id), Is.True);
        }
    }
}
