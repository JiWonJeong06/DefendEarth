using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 그림 3장(틀·검정 안 채움·빨강 채움)으로 만든 막대. 로딩바와 지구 체력 바가 같이 쓴다.
    /// 옛 DefendEarth의 슬라이더처럼 모두 Sliced(9칸 나누기)로 그려서, 가로로 길게 늘려도
    /// 모서리·테두리 픽셀은 그대로이고 가운데만 늘어난다.
    /// 그림은 옛날과 같은 PPU 64(게임 그림의 4배 촘촘함)라 테두리 20픽셀이 화면에선 5픽셀로 얇게 보인다.
    /// 크기·위치는 절대값(기준 픽셀)으로 정한다: 너비는 정해 준 값, 높이는 틀 그림 높이 그대로.
    /// </summary>
    public sealed class PixelBar : MonoBehaviour
    {
        /// <summary>틀 그림 가장자리에서 안쪽 구멍까지(그림 픽셀). 9칸 테두리(24)는 구멍의 둥근 모서리까지 포함해서 이보다 크다.</summary>
        private const float HoleInset = 20f;

        private SpriteRenderer fill;
        private SpriteRenderer fallbackFill;
        private float innerWidth;   // 유닛
        private float innerHeight;  // 유닛
        private float innerLeft;    // 유닛(막대 중심 기준)
        private float minFill;      // 유닛: 채움 그림의 좌우 모서리 합(이보다 좁으면 안 그림)
        private float pixel;        // 유닛: 그림 1픽셀
        private float value = 1f;

        /// <summary>막대를 만든다. widthPx는 틀 전체 너비(기준 픽셀).</summary>
        public static PixelBar Create(string name, Transform parent, int order, float widthPx)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var bar = go.AddComponent<PixelBar>();
            bar.Build(order, PlayArea.Config.PixelsToUnits(widthPx));
            return bar;
        }

        private void Build(int order, float width)
        {
            var a = GameAssets.Instance;
            var frameSprite = a.loadingBarFrame;
            if (frameSprite == null || a.loadingBarEmpty == null || a.loadingBarFill == null)
            {
                BuildFallback(order, width);
                return;
            }

            float ppu = frameSprite.pixelsPerUnit;
            pixel = 1f / ppu;
            Vector4 b = frameSprite.border; // x 왼, y 아래, z 오른, w 위 (그림 픽셀)
            float frameHeight = frameSprite.rect.height / ppu;
            innerLeft = -width * 0.5f + HoleInset / ppu;
            innerWidth = width - HoleInset * 2f / ppu;
            innerHeight = (frameSprite.rect.height - b.y - b.w) / ppu;
            float innerY = (b.y - b.w) * 0.5f / ppu;
            Vector4 fb = a.loadingBarFill.border;
            minFill = (fb.x + fb.z) / a.loadingBarFill.pixelsPerUnit;

            // 뒤에서부터: 검정 안 채움 → 빨강 채움 → 틀(구멍 가장자리를 덮음)
            var empty = Sliced("Empty", a.loadingBarEmpty, order);
            empty.size = new Vector2(innerWidth, innerHeight);
            empty.transform.localPosition = new Vector3(innerLeft + innerWidth * 0.5f, innerY, 0f);

            fill = Sliced("Fill", a.loadingBarFill, order + 1);
            fill.transform.localPosition = new Vector3(0f, innerY, 0f);

            var frame = Sliced("Frame", frameSprite, order + 2);
            frame.size = new Vector2(width, frameHeight);

            SetValue(value);
        }

        private SpriteRenderer Sliced(string name, Sprite sprite, int order)
        {
            var sr = GameAssets.Instance.CreateSprite(name, sprite, order, transform);
            sr.drawMode = SpriteDrawMode.Sliced;
            return sr;
        }

        /// <summary>그림이 연결 안 됐을 때(씬을 다시 만들기 전)만: 단색 막대.</summary>
        private void BuildFallback(int order, float width)
        {
            var a = GameAssets.Instance;
            pixel = PlayArea.Config.PixelsToUnits(1f);
            innerWidth = width;
            innerHeight = PlayArea.Config.PixelsToUnits(5f);
            innerLeft = -width * 0.5f;
            float px = a.pixelSprite.bounds.size.x;
            var back = a.CreateSprite("Back", a.pixelSprite, order, transform);
            back.color = new Color(0.13f, 0.13f, 0.2f);
            back.transform.localScale = new Vector3(innerWidth / px, innerHeight / px, 1f);
            fallbackFill = a.CreateSprite("Fill", a.pixelSprite, order + 1, transform);
            fallbackFill.color = new Color(0.81f, 0.05f, 0.13f);
            SetValue(value);
        }

        /// <summary>채운 정도 0~1. 너비는 그림 픽셀 단위로 맞춰서 반 칸씩 늘지 않는다. 왼쪽 끝 고정.</summary>
        public void SetValue(float k)
        {
            value = Mathf.Clamp01(k);
            float w = Mathf.Round(innerWidth * value / pixel) * pixel;
            if (fill != null)
            {
                bool show = w >= minFill;
                fill.gameObject.SetActive(show);
                if (!show) return;
                fill.size = new Vector2(w, innerHeight);
                var p = fill.transform.localPosition;
                fill.transform.localPosition = new Vector3(innerLeft + w * 0.5f, p.y, 0f);
            }
            else if (fallbackFill != null)
            {
                float px = GameAssets.Instance.pixelSprite.bounds.size.x;
                fallbackFill.transform.localScale = new Vector3(Mathf.Max(0.0001f, w) / px, innerHeight / px, 1f);
                fallbackFill.transform.localPosition = new Vector3(innerLeft + w * 0.5f, 0f, 0f);
            }
        }

        /// <summary>채움 색을 곱한다(흰색 = 그림 그대로). 맞았을 때 번쩍이게 할 때 쓴다.</summary>
        public void SetFillTint(Color c)
        {
            if (fill != null) fill.color = c;
        }
    }
}
