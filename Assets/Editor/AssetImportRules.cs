using UnityEngine;
using UnityEditor;

// Import rules for downloaded props and generated characters.
public sealed class AssetImportRules : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Props/")) return;
        var t = (TextureImporter)assetImporter;
        t.maxTextureSize = 1024;
        if (assetPath.Contains("_nor_")) t.textureType = TextureImporterType.NormalMap;
    }

    void OnPreprocessAudio()
    {
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
