using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 지구 체력 바(글자 없음). 플레이 영역 맨 위에 그린다.
    /// 체력이 줄면 초록 → 노랑 → 빨강으로 바뀌고, 깎인 부분은 잠깐 하얗게 남았다가 줄어든다.
    /// </summary>
    public sealed class EarthHealthBar : MonoBehaviour
    {
        [SerializeField] private Earth earth;
        [Tooltip("바 너비·높이(기준 픽셀)")]
        [SerializeField] private Vector2 sizePx = new Vector2(120f, 5f);
        [Tooltip("플레이 영역 위쪽 끝에서 바 중심까지(기준 픽셀)")]
        [SerializeField] private float topOffsetPx = 7f;

        private Transform fill;
        private Transform trail;
        private SpriteRenderer fillRenderer;
        private float shown = 1f;
        private float trailValue = 1f;
        private float width;

        private void Start()
        {
            var assets = GameAssets.Instance;
            var config = PlayArea.Config;
            width = config.PixelsToUnits(sizePx.x);
            float height = config.PixelsToUnits(sizePx.y);
            transform.position = new Vector3(0f, PlayArea.Top - config.PixelsToUnits(topOffsetPx), 0f);

            float pixelSize = assets.pixelSprite.bounds.size.x;
            MakeBar("Border", new Color(0f, 0f, 0f, 0.8f), width + config.PixelsToUnits(2f), height + config.PixelsToUnits(2f), SortOrder.Hud, pixelSize);
            MakeBar("Back", new Color(0.25f, 0.05f, 0.08f, 1f), width, height, SortOrder.Hud + 1, pixelSize);
            trail = MakeBar("Trail", Color.white, width, height, SortOrder.Hud + 2, pixelSize).transform;
            fillRenderer = MakeBar("Fill", Color.green, width, height, SortOrder.Hud + 3, pixelSize);
            fill = fillRenderer.transform;

            if (earth != null)
            {
                earth.HpChanged += OnHpChanged;
                OnHpChanged(earth.Hp, earth.MaxHp);
            }
        }

        private void OnDestroy()
        {
            if (earth != null) earth.HpChanged -= OnHpChanged;
        }

        private SpriteRenderer MakeBar(string name, Color color, float w, float h, int order, float pixelSize)
        {
            var sr = GameAssets.Instance.CreateSprite(name, GameAssets.Instance.pixelSprite, order, transform);
            sr.color = color;
            sr.transform.localScale = new Vector3(w / pixelSize, h / pixelSize, 1f);
            return sr;
        }

        private void OnHpChanged(int hp, int max)
        {
            shown = max > 0 ? (float)hp / max : 0f;
            if (shown > trailValue) trailValue = shown;
            fillRenderer.color = shown > 0.5f ? new Color(0.35f, 0.9f, 0.4f)
                : shown > 0.25f ? new Color(1f, 0.8f, 0.25f)
                : new Color(1f, 0.3f, 0.3f);
            SetBar(fill, shown);
        }

        private void Update()
        {
            if (trailValue > shown) trailValue = Mathf.MoveTowards(trailValue, shown, Time.unscaledDeltaTime * 0.6f);
            SetBar(trail, trailValue);
        }

        private void SetBar(Transform bar, float value)
        {
            if (bar == null) return;
            var s = bar.localScale;
            float full = width / GameAssets.Instance.pixelSprite.bounds.size.x;
            s.x = full * Mathf.Clamp01(value);
            bar.localScale = s;
            // 왼쪽 끝을 고정
            bar.localPosition = new Vector3(-width * 0.5f + width * Mathf.Clamp01(value) * 0.5f, 0f, 0f);
        }
    }
}
