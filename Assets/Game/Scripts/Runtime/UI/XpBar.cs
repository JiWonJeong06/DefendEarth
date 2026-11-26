using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 경험치 바(얇은 하늘색 줄). 골드·점수·웨이브 줄 바로 아래. 레벨 숫자는 레벨업 화면에서 보여 준다.
    /// </summary>
    public sealed class XpBar : MonoBehaviour
    {
        [SerializeField] private LevelSystem levels;
        [Tooltip("플레이 영역 위쪽 끝에서 바 중심까지(기준 픽셀)")]
        [SerializeField] private float topOffsetPx = 17f;
        [Tooltip("좌우 끝에서 띄우는 거리(기준 픽셀)")]
        [SerializeField] private float sidePaddingPx = 6f;
        [SerializeField] private float heightPx = 2f;

        private Transform fill;
        private float width;
        private float pixelSize;
        private float shown;
        private float target;

        private void Start()
        {
            var assets = GameAssets.Instance;
            var config = PlayArea.Config;
            pixelSize = assets.pixelSprite.bounds.size.x;
            width = PlayArea.Size.x - config.PixelsToUnits(sidePaddingPx * 2f);
            float h = config.PixelsToUnits(heightPx);
            transform.position = new Vector3(0f, PlayArea.Top - config.PixelsToUnits(topOffsetPx), 0f);

            var back = assets.CreateSprite("Back", assets.pixelSprite, SortOrder.Hud, transform);
            back.color = new Color(0.05f, 0.1f, 0.2f, 0.9f);
            back.transform.localScale = new Vector3(width / pixelSize, h / pixelSize, 1f);
            var f = assets.CreateSprite("Fill", assets.pixelSprite, SortOrder.Hud + 1, transform);
            f.color = new Color(0.45f, 0.9f, 1f);
            f.transform.localScale = new Vector3(0f, h / pixelSize, 1f);
            fill = f.transform;

            if (levels != null)
            {
                levels.XpChanged += OnXpChanged;
                OnXpChanged(levels.Xp, levels.XpToNext);
            }
        }

        private void OnDestroy()
        {
            if (levels != null) levels.XpChanged -= OnXpChanged;
        }

        private void OnXpChanged(float xp, float need)
        {
            float value = need > 0f ? Mathf.Clamp01(xp / need) : 0f;
            if (value < target) shown = 0f; // 레벨업으로 넘어가면 처음부터 다시 차오름
            target = value;
        }

        private void Update()
        {
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 2f);
            var s = fill.localScale;
            s.x = width / pixelSize * shown;
            fill.localScale = s;
            fill.localPosition = new Vector3(-width * 0.5f + width * shown * 0.5f, 0f, 0f);
        }
    }
}
