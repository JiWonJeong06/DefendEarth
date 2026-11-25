using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 판이 끝나면 화면을 어둡게 하고, 잠시 뒤 아무 데나 탭하면 다시 시작한다.
    /// (임시. 5번 묶음에서 점수·골드를 보여 주는 결과 화면으로 바뀐다)
    /// </summary>
    public sealed class GameOverOverlay : MonoBehaviour
    {
        [SerializeField] private TapInput input;
        [Tooltip("판이 끝나고 이 시간(초) 뒤부터 탭을 받는다(실수로 바로 넘어가지 않게)")]
        [SerializeField] private float tapDelay = 0.8f;

        private SpriteRenderer dim;
        private float shownAt = -1f;

        private void Start()
        {
            var assets = GameAssets.Instance;
            dim = assets.CreateSprite("Dim", assets.pixelSprite, SortOrder.Overlay, transform);
            float pixelSize = assets.pixelSprite.bounds.size.x;
            dim.transform.localScale = new Vector3(200f / pixelSize, 200f / pixelSize, 1f); // 화면 전체(여백 포함)
            dim.color = new Color(0f, 0f, 0.05f, 0.65f);
            dim.enabled = false;

            if (GameSession.Instance != null) GameSession.Instance.GameOver += OnGameOver;
            if (input != null) input.Tapped += OnTapped;
        }

        private void OnDestroy()
        {
            if (GameSession.Instance != null) GameSession.Instance.GameOver -= OnGameOver;
            if (input != null) input.Tapped -= OnTapped;
        }

        private void OnGameOver()
        {
            dim.enabled = true;
            shownAt = Time.unscaledTime;
        }

        private void OnTapped(Vector2 _)
        {
            if (shownAt < 0f || Time.unscaledTime - shownAt < tapDelay) return;
            GameSession.Instance.Restart();
        }
    }
}
