using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BouncyUFO
{
    /// <summary>
    /// 화면 글자(갈무리 도트 폰트). 글자는 번역 키로 넣는다(GameText.Create(... key ...)).
    /// 폰트는 실행할 때 필요한 글자만 도트 그대로(흐림 없이) 그려서 쓴다 — 한·일 글자가 많아도 가볍다.
    /// </summary>
    public static class GameText
    {
        public enum Style
        {
            /// <summary>Galmuri11, 12픽셀 글자 (제목·숫자·버튼)</summary>
            Title,
            /// <summary>Galmuri9, 10픽셀 글자 (설명·작은 글씨)</summary>
            Body,
        }

        private static readonly Dictionary<Style, TMP_FontAsset> cache = new Dictionary<Style, TMP_FontAsset>();

        private static int NativePixels(Style style) => style == Style.Title ? 12 : 10;

        private static TMP_FontAsset FontFor(Style style)
        {
            if (cache.TryGetValue(style, out var asset) && asset != null) return asset;
            var assets = GameAssets.Instance;
            Font font = style == Style.Title ? assets.titleFont : assets.bodyFont;
            if (font == null) return null;

            int size = NativePixels(style);
            asset = TMP_FontAsset.CreateFontAsset(font, size, 1, GlyphRenderMode.RASTER_HINTED, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null) return null;
            asset.name = font.name + " (runtime)";
            if (assets.textShader != null && asset.material != null) asset.material.shader = assets.textShader;
            foreach (var tex in asset.atlasTextures)
                if (tex != null) tex.filterMode = FilterMode.Point;
            cache[style] = asset;
            return asset;
        }

        /// <summary>
        /// 글자를 만든다. scale은 정수 배(1, 2…)로만 키워야 도트가 고르게 보인다.
        /// boxWidthPx는 글자 상자 폭(기준 픽셀). 왼쪽/오른쪽 정렬은 이 상자의 끝에 맞춘다.
        /// wrap이 true면 상자 폭에서 줄을 바꾼다.
        /// </summary>
        public static TextMeshPro Create(string name, Transform parent, Style style, int sortingOrder,
            TextAlignmentOptions align = TextAlignmentOptions.Center, float boxWidthPx = 0f, bool wrap = false, int scale = 1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * Mathf.Max(1, scale);
            var tmp = go.AddComponent<TextMeshPro>();
            var font = FontFor(style);
            if (font != null) tmp.font = font;

            // TextMeshPro: 글자 크기 10 = 1유닛. 도트 n픽셀 = n/PPU 유닛 → 크기 = n × 10 / PPU
            int ppu = PlayArea.Config != null ? PlayArea.Config.pixelsPerUnit : 16;
            tmp.fontSize = NativePixels(style) * 10f / ppu;
            tmp.alignment = align;
            tmp.textWrappingMode = wrap && boxWidthPx > 0f ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = false;
            tmp.extraPadding = false;
            var rt = tmp.rectTransform;
            float w = boxWidthPx > 0f ? boxWidthPx / ppu : 20f;
            rt.sizeDelta = new Vector2(w, 4f);
            tmp.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            return tmp;
        }

        /// <summary>번역 키로 글자를 넣는다.</summary>
        public static void SetKey(TextMeshPro text, string key) => text.text = Localization.Get(key);

        /// <summary>새 글자가 아틀라스에 추가되면 흐려지지 않게 다시 도트 필터를 건다.</summary>
        public static void KeepCrisp()
        {
            foreach (var asset in cache.Values)
            {
                if (asset == null) continue;
                foreach (var tex in asset.atlasTextures)
                    if (tex != null && tex.filterMode != FilterMode.Point) tex.filterMode = FilterMode.Point;
            }
        }
    }
}
