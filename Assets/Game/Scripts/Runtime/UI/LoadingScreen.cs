using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BouncyUFO
{
    /// <summary>
    /// 로딩: 다음 씬(보통 게임)을 뒤에서 불러오는 동안 가운데에서 지구가 자전하고, 아래 로딩바(PixelBar)가 찬다.
    /// 너무 빨리 지나가 깜빡이지 않게 최소 시간만큼은 보여 준다.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        // 기준 픽셀(플레이 영역 가운데가 0)
        private const float EarthY = 36f;
        private const float EarthRadius = 44f;
        private const float LabelY = -24f;
        private const float BarY = -44f;
        private const float BarWidth = 130f;   // 옛 슬라이더처럼 화면 너비의 약 72%
        private const float TipY = -76f;

        [SerializeField] private float minimumSeconds = 0.8f;

        private AsyncOperation op;
        private float startedAt;
        private PixelBar bar;

        private static float U(float px) => PlayArea.Config.PixelsToUnits(px);

        private void Start()
        {
            StarField.Create(transform, 40);

            var earth = EarthGlobe.CreateWhole("Earth", transform, SortOrder.Earth, EarthRadius);
            earth.transform.position = new Vector3(0f, U(EarthY), 0f);

            var label = GameText.Create("Loading", transform, GameText.Style.Title, SortOrder.Hud + 5);
            GameText.SetKey(label, "ui.loading");
            label.transform.position = new Vector3(0f, U(LabelY), 0f);

            bar = PixelBar.Create("LoadingBar", transform, SortOrder.Hud, BarWidth);
            bar.transform.position = new Vector3(0f, U(BarY), 0f);
            bar.SetValue(0f);

            var tip = GameText.Create("Tip", transform, GameText.Style.Body, SortOrder.Hud + 5, TextAlignmentOptions.Center, 160f, true);
            GameText.SetKey(tip, "tip." + Random.Range(1, 6));
            tip.color = new Color(0.75f, 0.85f, 1f);
            tip.transform.position = new Vector3(0f, U(TipY), 0f);

            startedAt = Time.unscaledTime;
            op = SceneManager.LoadSceneAsync(SceneFlow.LoadingTarget);
            if (op != null) op.allowSceneActivation = false;
        }

        private void Update()
        {
            float elapsed = Time.unscaledTime - startedAt;
            float loadK = op != null ? Mathf.Clamp01(op.progress / 0.9f) : 1f;
            float k = Mathf.Min(loadK, Mathf.Clamp01(elapsed / minimumSeconds));
            bar.SetValue(k);

            if (op != null && k >= 1f && loadK >= 1f) op.allowSceneActivation = true;
        }
    }
}
