using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 보스가 나와 있는 동안만 보이는 보스 체력 바(보라색). 지구 체력 바 아래 줄.
    /// </summary>
    public sealed class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private WaveDirector waves;
        [SerializeField] private Vector2 sizePx = new Vector2(100f, 3f);
        [Tooltip("플레이 영역 위쪽 끝에서 바 중심까지(기준 픽셀)")]
        [SerializeField] private float topOffsetPx = 29f;

        private Transform fill;
        private Transform root;
        private float width;
        private float pixelSize;

        private void Start()
        {
            var assets = GameAssets.Instance;
            var config = PlayArea.Config;
            pixelSize = assets.pixelSprite.bounds.size.x;
            width = config.PixelsToUnits(sizePx.x);
            float height = config.PixelsToUnits(sizePx.y);

            root = new GameObject("Bar").transform;
            root.SetParent(transform, false);
            root.position = new Vector3(0f, PlayArea.Top - config.PixelsToUnits(topOffsetPx), 0f);

            var border = assets.CreateSprite("Border", assets.pixelSprite, SortOrder.Hud, root);
            border.color = new Color(0f, 0f, 0f, 0.8f);
            border.transform.localScale = new Vector3((width + config.PixelsToUnits(2f)) / pixelSize, (height + config.PixelsToUnits(2f)) / pixelSize, 1f);
            var back = assets.CreateSprite("Back", assets.pixelSprite, SortOrder.Hud + 1, root);
            back.color = new Color(0.15f, 0.05f, 0.2f);
            back.transform.localScale = new Vector3(width / pixelSize, height / pixelSize, 1f);
            var f = assets.CreateSprite("Fill", assets.pixelSprite, SortOrder.Hud + 2, root);
            f.color = new Color(0.75f, 0.4f, 1f);
            f.transform.localScale = new Vector3(width / pixelSize, height / pixelSize, 1f);
            fill = f.transform;

            root.gameObject.SetActive(false);
            if (waves != null)
            {
                waves.BossAppeared += OnBossAppeared;
                waves.BossEnded += OnBossEnded;
            }
        }

        private void OnDestroy()
        {
            if (waves == null) return;
            waves.BossAppeared -= OnBossAppeared;
            waves.BossEnded -= OnBossEnded;
        }

        private void OnBossAppeared(Monster boss, int round) => root.gameObject.SetActive(true);
        private void OnBossEnded(bool killed) => root.gameObject.SetActive(false);

        private void Update()
        {
            if (root == null || !root.gameObject.activeSelf || waves == null) return;
            var boss = waves.CurrentBoss;
            float value = boss != null && boss.IsAlive ? Mathf.Clamp01((float)boss.Hp / boss.MaxHp) : 0f;
            var s = fill.localScale;
            s.x = width / pixelSize * value;
            fill.localScale = s;
            fill.localPosition = new Vector3(-width * 0.5f + width * value * 0.5f, 0f, 0f);
        }
    }
}
