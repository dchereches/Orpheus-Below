using UnityEditor;
using UnityEngine;

namespace OrpheusBelow.EditorTools
{
    public sealed class BossAttackArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/OrpheusBelow/Resources/BossAttacks/") || !assetPath.EndsWith(".png")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 256f;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            // Anchor the solid leading core to the hit volume; its brush trails extend behind it.
            settings.spritePivot = assetPath.EndsWith("Tripod.png") ? new Vector2(.76f, .5f)
                : assetPath.EndsWith("WingedSentinel.png") ? new Vector2(.29f, .50f)
                : assetPath.EndsWith("MaskedMonk.png") ? new Vector2(.25f, .50f)
                : assetPath.EndsWith("Shepherd.png") ? new Vector2(.25f, .50f)
                : new Vector2(.29f, .50f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
