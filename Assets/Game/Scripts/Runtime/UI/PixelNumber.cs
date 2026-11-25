using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 도트 숫자(3×5 픽셀 글자)로 수를 그린다. 숫자만 쓰므로 번역이 필요 없고 폰트 라이선스도 필요 없다.
    /// </summary>
    public sealed class PixelNumber : MonoBehaviour
    {
        public enum Align { Left, Right, Center }

        [SerializeField] private Align align = Align.Left;
        [Tooltip("글자 사이 간격(기준 픽셀). 3픽셀 글자 + 1픽셀 띄움")]
        [SerializeField] private float advancePx = 4f;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private int sortingOrder = SortOrder.Hud + 5;

        private readonly List<SpriteRenderer> digits = new List<SpriteRenderer>(8);
        private int shown = int.MinValue;

        public void Configure(Align alignment, Color textColor)
        {
            align = alignment;
            color = textColor;
            foreach (var d in digits) d.color = color;
        }

        /// <summary>그리는 순서(화면을 덮는 창 위에 숫자를 띄울 때).</summary>
        public void SetSortingOrder(int order)
        {
            sortingOrder = order;
            foreach (var d in digits) d.sortingOrder = order;
        }

        public void SetValue(int value)
        {
            if (value == shown) return;
            shown = value;
            string text = Mathf.Max(0, value).ToString();
            var assets = GameAssets.Instance;
            var config = PlayArea.Config;
            float advance = config.PixelsToUnits(advancePx);

            while (digits.Count < text.Length)
            {
                var sr = assets.CreateSprite("Digit", null, sortingOrder, transform);
                sr.color = color;
                digits.Add(sr);
            }

            float width = advance * text.Length - config.PixelsToUnits(1f);
            float start = align == Align.Left ? 0f : align == Align.Right ? -width : -width * 0.5f;
            float half = config.PixelsToUnits(1.5f); // 글자 중심(3픽셀의 절반)

            for (int i = 0; i < digits.Count; i++)
            {
                bool on = i < text.Length;
                digits[i].enabled = on;
                if (!on) continue;
                digits[i].sprite = assets.digitSprites[text[i] - '0'];
                digits[i].transform.localPosition = new Vector3(start + advance * i + half, 0f, 0f);
            }
        }
    }
}
