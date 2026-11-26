using UnityEditor;
using UnityEngine;

namespace BouncyUFO.EditorTools
{
    /// <summary>
    /// 캐릭터 부품 그림을 폴더에 넣기만 하면 도트 설정이 자동으로 맞춰진다:
    /// 스프라이트, PPU 16, Point(흐림 없음), 압축 없음, 밉맵 없음, 기준점 가운데.
    /// (모든 부품이 같은 캔버스 크기라 가운데 기준이면 겹쳤을 때 위치가 맞는다)
    /// </summary>
    public sealed class PixelArtImporter : AssetPostprocessor
    {
        private static readonly string[] Folders =
        {
            "Assets/Art/Temp/Character/",
            "Assets/Art/Character/",
        };

        private void OnPreprocessTexture()
        {
            bool match = false;
            foreach (var f in Folders)
                if (assetPath.Replace('\\', '/').StartsWith(f)) { match = true; break; }
            if (!match) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
