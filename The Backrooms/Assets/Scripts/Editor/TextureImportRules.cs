using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools
{
    public class TextureImportRules : AssetPostprocessor
    {
        static readonly string[] RootFolders = { "Assets/Player/", "Assets/Structure/", "Assets/Objects/" };

        static bool IsManaged(string path)
        {
            if (!path.Contains("/Textures/"))
                return false;
            foreach (var root in RootFolders)
                if (path.StartsWith(root))
                    return true;
            return false;
        }

        void OnPreprocessTexture()
        {
            if (!IsManaged(assetPath))
                return;

            var importer = (TextureImporter)assetImporter;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            importer.textureType = fileName.EndsWith("Normal")
                ? TextureImporterType.NormalMap
                : TextureImporterType.Default;
            importer.sRGBTexture = !(fileName.EndsWith("Normal") || fileName.EndsWith("MetallicSmoothness") || fileName.EndsWith("Roughness"));
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 1024;

            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 1024;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
