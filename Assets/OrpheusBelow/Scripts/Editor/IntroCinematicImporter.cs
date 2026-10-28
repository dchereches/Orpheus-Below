using UnityEditor;
using UnityEngine;

namespace OrpheusBelow.EditorTools
{
    public sealed class IntroCinematicImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/OrpheusBelow/Resources/Cinematic/Intro/")) return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;
            importer.mipmapEnabled=false; importer.isReadable=false; importer.maxTextureSize=2048;
            importer.wrapMode=TextureWrapMode.Clamp; importer.filterMode=FilterMode.Bilinear;
            importer.alphaSource=TextureImporterAlphaSource.None;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
        }
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/OrpheusBelow/Resources/Cinematic/Intro/")) return;
            var importer=(AudioImporter)assetImporter;
            var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData=true;
            importer.defaultSampleSettings=settings; importer.forceToMono=false;
        }
    }
}
