using UnityEditor;

namespace OrpheusBelow.Editor
{
    public class UnderworldArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            bool floor = assetPath.Contains("QuietSlateFloor") || assetPath.Contains("QuietBasaltFloor");
            importer.mipmapEnabled = floor;
            importer.alphaIsTransparency = assetPath.Contains("Creatures") || assetPath.Contains("DarkBeaconTower");
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = floor ? UnityEngine.TextureWrapMode.Repeat : UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
        }
    }
}
