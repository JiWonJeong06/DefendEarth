using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BouncyUFO.EditorTools
{
    /// <summary>
    /// 앱 아이콘(게임 프로필): 도트 지구 그림 `Assets/Art/Icon/AppIcon.png`(1024×1024, 64픽셀 그림을 16배로 키움)를
    /// 모든 플랫폼의 기본 아이콘으로 정한다. 도트가 흐려지지 않게 가져오기 설정도 맞춘다(Point, 압축 없음).
    /// 메뉴 Bouncy UFO → Set App Icon, 그리고 Build All Scenes 때 같이 실행된다.
    /// 스토어 등록용 512×512는 `docs/store/icon_512.png`.
    /// </summary>
    public static class AppIconSetup
    {
        public const string IconPath = "Assets/Art/Icon/AppIcon.png";

        [MenuItem("Bouncy UFO/Set App Icon")]
        public static void SetFromMenu()
        {
            if (Apply()) Debug.Log("앱 아이콘을 정했습니다: " + IconPath);
        }

        public static bool Apply()
        {
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("앱 아이콘 그림이 없습니다: " + IconPath);
                return false;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
            if (importer.npotScale != TextureImporterNPOTScale.None) { importer.npotScale = TextureImporterNPOTScale.None; changed = true; }
            if (importer.maxTextureSize < 1024) { importer.maxTextureSize = 1024; changed = true; }
            if (changed) importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return false;
            // 기본 아이콘(플랫폼별 아이콘을 따로 안 정하면 안드로이드도 이걸 줄여서 쓴다)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
