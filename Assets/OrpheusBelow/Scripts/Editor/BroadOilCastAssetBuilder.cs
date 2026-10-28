using UnityEditor;

namespace OrpheusBelow.EditorTools
{
    public static class BroadOilCastAssetBuilder
    {
        [MenuItem("Orpheus Below/Bake Broad Oil Cast")]
        public static void Build()
        {
            PaintedBestiaryAssetBuilder.Build(); EurydiceLook02AssetBuilder.Build();
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("[Broad oil cast] BAKE PASS: reviewed enemies and companion; Orpheus art deferred by user.");
        }
    }
}
