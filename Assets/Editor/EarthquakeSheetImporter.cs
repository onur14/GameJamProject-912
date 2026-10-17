using UnityEditor;

// Preserve the supplied pixel sheet at its original resolution in builds.
public sealed class EarthquakeSheetImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Disaster/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = UnityEngine.FilterMode.Point;
        importer.mipmapEnabled = false;
        if (assetPath.EndsWith("EarthquakeSheet.png")) importer.isReadable = true;
    }
}
