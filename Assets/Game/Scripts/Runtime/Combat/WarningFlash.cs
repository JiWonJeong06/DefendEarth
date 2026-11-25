using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 러시·보스 경고. 플레이 영역 양옆 가장자리가 깜빡인다(글자 없음).
    /// 러시는 빨강, 보스는 보라.
    /// </summary>
    public sealed class WarningFlash : MonoBehaviour
    {
        public enum Kind { Rush, Boss }

        [Tooltip("가장자리 띠 두께(기준 픽셀)")]
        [SerializeField] private float thicknessPx = 4f;

        private SpriteRenderer left;
        private SpriteRenderer right;
        private SpriteRenderer top;
        private float timer;
        private Color color;

        private void Start()
        {
            var assets = GameAssets.Instance;
            var config = PlayArea.Config;
            float px = assets.pixelSprite.bounds.size.x;
            float t = config.PixelsToUnits(thicknessPx);

            left = assets.CreateSprite("Left", assets.pixelSprite, SortOrder.Hud - 1, transform);
            left.transform.localScale = new Vector3(t / px, PlayArea.Size.y / px, 1f);
            left.transform.position = new Vector3(PlayArea.Left + t * 0.5f, 0f, 0f);

            right = assets.CreateSprite("Right", assets.pixelSprite, SortOrder.Hud - 1, transform);
            right.transform.localScale = left.transform.localScale;
            right.transform.position = new Vector3(PlayArea.Right - t * 0.5f, 0f, 0f);

            top = assets.CreateSprite("Top", assets.pixelSprite, SortOrder.Hud - 1, transform);
            top.transform.localScale = new Vector3(PlayArea.Size.x / px, t / px, 1f);
            top.transform.position = new Vector3(0f, PlayArea.Top - t * 0.5f, 0f);

            SetVisible(false);
        }

        public void Show(Kind kind, float duration)
        {
            color = kind == Kind.Rush ? new Color(1f, 0.25f, 0.2f) : new Color(0.75f, 0.35f, 1f);
            timer = duration;
            SetVisible(true);
        }

        private void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) { SetVisible(false); return; }
            var c = color;
            c.a = Mathf.PingPong(Time.time * 6f, 1f) * 0.8f + 0.1f; // 깜빡깜빡
            left.color = right.color = top.color = c;
        }

        private void SetVisible(bool on)
        {
            if (left == null) return;
            left.enabled = right.enabled = top.enabled = on;
        }
    }
}
