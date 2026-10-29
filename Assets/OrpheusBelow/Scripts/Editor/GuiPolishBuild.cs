namespace OrpheusBelow.EditorTools
{
    /// <summary>Rebuild authored assets before checking the GUI and movement changes together.</summary>
    public static class GuiPolishBuild
    {
        public static void Build()
        {
            BuildOrpheus();
            PaintedChamberSceneBuilder.BuildScenes();
        }
        public static void BuildOrpheus()
        {
            OrpheusBroadOilAssetBuilder.Build();
            OrpheusAnimationVerification.Run();
        }
    }
}
