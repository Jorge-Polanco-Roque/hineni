using UnityEngine;
using UnityEditor;

// Import rules for downloaded props and generated characters.
public sealed class AssetImportRules : AssetPostprocessor
{
    public override uint GetVersion() => 3;   // bump to reimport everything these rules touch
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Props/")) return;
        var t = (TextureImporter)assetImporter;
        // Through the ASCII pass only colour carries detail: colour maps at 512, the rest (normal, rough, metal) at 256, compressed.
        t.maxTextureSize = assetPath.Contains("_diff") || assetPath.Contains("_col") ? 512 : 256;
        t.textureCompression = TextureImporterCompression.Compressed;
        if (assetPath.Contains("_nor_")) t.textureType = TextureImporterType.NormalMap;
    }

    void OnPreprocessAudio()
    {
        if (assetPath.Contains("/Resources/Voices/"))
        {
            // Five languages of speech: mono, compressed, decoded on load (they are short).
            var v = (AudioImporter)assetImporter; v.forceToMono = true;
            var vs = v.defaultSampleSettings;
            vs.loadType = AudioClipLoadType.CompressedInMemory; vs.compressionFormat = AudioCompressionFormat.Vorbis; vs.quality = .3f;
            vs.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate; vs.sampleRateOverride = 16000;
            v.defaultSampleSettings = vs;
            return;
        }
        if (!assetPath.Contains("/Resources/Music/")) return;
        var a = (AudioImporter)assetImporter;
        var s = a.defaultSampleSettings;
        s.loadType = AudioClipLoadType.Streaming; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = .7f;
        a.defaultSampleSettings = s;
    }

    void OnPreprocessModel()
    {
        if (!assetPath.Contains("/Resources/Props/") && !assetPath.Contains("/Resources/Characters/")) return;
        var m = (ModelImporter)assetImporter;
        bool character = assetPath.Contains("/Resources/Characters/");
        m.animationType = character ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
        m.importBlendShapes = true;
        m.importAnimation = false;
        m.importCameras = false;
        m.importLights = false;
        m.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        m.materialSearch = ModelImporterMaterialSearch.Local;
    }
}
