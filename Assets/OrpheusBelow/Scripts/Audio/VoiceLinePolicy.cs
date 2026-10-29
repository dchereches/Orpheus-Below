namespace OrpheusBelow
{
    public static class VoiceLinePolicy
    {
        public static bool IsRecognitionChatter(VoiceLineId id) =>
            id == VoiceLineId.Eurydice_IamHere || id == VoiceLineId.Eurydice_HearTheNote ||
            id == VoiceLineId.Eurydice_RightHere || id == VoiceLineId.Eurydice_AreYouStillThere ||
            id == VoiceLineId.Eurydice_LookAtMe;

        public static bool IsInteractiveInstruction(VoiceLineId id) =>
            id >= VoiceLineId.Narrator_ClearChambers && id <= VoiceLineId.Narrator_PluckCadences;

        public static VoiceLineId InstructionFor(AsphodelTutorialStep step)
        {
            switch (step)
            {
                case AsphodelTutorialStep.Move: return VoiceLineId.Narrator_ClearChambers;
                case AsphodelTutorialStep.SoftNote: return VoiceLineId.Narrator_DefendYourself;
                case AsphodelTutorialStep.HardBoom: return VoiceLineId.Narrator_RepellingMove;
                case AsphodelTutorialStep.Flute: return VoiceLineId.Narrator_CombosFlee;
                case AsphodelTutorialStep.Dash: return VoiceLineId.Narrator_PressF;
                case AsphodelTutorialStep.Cadence: return VoiceLineId.Narrator_PluckCadences;
                default: return VoiceLineId.None;
            }
        }
    }
}
