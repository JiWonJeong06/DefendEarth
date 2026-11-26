using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BouncyUFO
{
    /// <summary>
    /// 로딩: 다음 씬(보통 게임)을 뒤에서 불러오는 동안 UFO가 통통 튀고 진행 바가 찬다.
    /// 너무 빨리 지나가 깜빡이지 않게 최소 시간만큼은 보여 준다.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private float minimumSeconds = 0.8f;

        private AsyncOperation op;
        private float startedAt;
        private Transform fill;
        private float barWidth;
        private float pixelSize;
        private Transform ufo;
        private SquashStretch squash;
        private int lastBounce;
        private TextMeshPro tip;

        private float U(float px) => PlayArea.Config.PixelsToUnits(px);

        private void Start()
        {
            var a = GameAssets.Instance;
            StarField.Create(transform, 25);
            pixelSize = a.pixelSprite.bounds.size.x;

            var label = GameText.Create("Loading", transform, GameText.Style.Title, SortOrder.Hud + 5);
            GameText.SetKey(label, "ui.loading");
            label.transform.position = new Vector3(0f, U(-16f), 0f);

            barWidth = U(120f);
            var back = a.CreateSprite("BarBack", a.pixelSprite, SortOrder.Hud, transform);
            back.color = new Color(0.1f, 0.12f, 0.25f);
            back.transform.localScale = new Vector3(barWidth / pixelSize, U(4f) / pixelSize, 1f);
            back.transform.position = new Vector3(0f, U(-34f), 0f);
            var f = a.CreateSprite("BarFill", a.pixelSprite, SortOrder.Hud + 1, transform);
            f.color = new Color(0.45f, 0.9f, 1f);
            fill = f.transform;
            fill.position = back.transform.position;

            tip = GameText.Create("Tip", transform, GameText.Style.Body, SortOrder.Hud + 5, TextAlignmentOptions.Center, 160f, true);
            GameText.SetKey(tip, "tip." + Random.Range(1, 4));
            tip.color = new Color(0.75f, 0.85f, 1f);
            tip.transform.position = new Vector3(0f, U(-70f), 0f);

            var root = new GameObject("UFO").transform;
            root.SetParent(transform, false);
            var sr = a.CreateSprite("Visual", a.ufoSprite, SortOrder.Ufo, root);
            sr.transform.localScale = Vector3.one * 2f;
            squash = sr.gameObject.AddComponent<SquashStretch>();
            ufo = root;

            startedAt = Time.unscaledTime;
            op = SceneManager.LoadSceneAsync(SceneFlow.LoadingTarget);
            if (op != null) op.allowSceneActivation = false;
        }

        private void Update()
        {
            float elapsed = Time.unscaledTime - startedAt;
            float loadK = op != null ? Mathf.Clamp01(op.progress / 0.9f) : 1f;
            float k = Mathf.Min(loadK, Mathf.Clamp01(elapsed / minimumSeconds));
            fill.localScale = new Vector3(barWidth * k / pixelSize, U(4f) / pixelSize, 1f);
            fill.localPosition = new Vector3(-barWidth * 0.5f + barWidth * k * 0.5f, fill.localPosition.y, 0f);

            // UFO가 제자리에서 통통
            float period = 0.45f;
            int bounce = Mathf.FloorToInt(elapsed / period);
            float local = elapsed / period - bounce;
            ufo.position = new Vector3(0f, U(20f) + Mathf.Sin(local * Mathf.PI) * U(10f), 0f);
            if (bounce != lastBounce) { lastBounce = bounce; squash.Punch(0.3f); }

            if (op != null && k >= 1f && loadK >= 1f) op.allowSceneActivation = true;
        }
    }
}
