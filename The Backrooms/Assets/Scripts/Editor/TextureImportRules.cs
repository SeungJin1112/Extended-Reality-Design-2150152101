using UnityEditor;
using UnityEngine;

namespace Backrooms.EditorTools
{
    /// <summary>
    /// Assets/Textures 아래 텍스처의 임포트 설정을 자동으로 맞춘다.
    /// - 파일명이 "Normal"로 끝나면 Normal Map으로 임포트
    /// - Quest(Android)용 ASTC 압축, 최대 1024px
    /// </summary>
    public class TextureImportRules : AssetPostprocessor
    {
        const string TextureFolder = "Assets/Textures/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(TextureFolder))
                return;

            var importer = (TextureImporter)assetImporter;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            importer.textureType = fileName.EndsWith("Normal")
                ? TextureImporterType.NormalMap
                : TextureImporterType.Default;
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
