using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 판 끝 결과 화면. 점수·최고 기록(신기록 표시)·웨이브·버틴 시간,
    /// 골드는 "운석 골드 + 단계 보상 = 이번 판 골드"로 나눠 보여 주고 가진 골드 합계도 보여 준다.
    /// 잠시 뒤 아무 데나 누르면 다시 시작.
    /// </summary>
    public sealed class ResultScreen : MonoBehaviour
    {
        [SerializeField] private TapInput input;
        [Tooltip("결과가 뜨고 이 시간(초) 뒤부터 탭을 받는다(실수로 바로 넘어가지 않게)")]
        [SerializeField] private float tapDelay = 1f;
        [Tooltip("창 크기(기준 픽셀)")]
        [SerializeField] private Vector2 panelSizePx = new Vector2(152f, 206f);

        private const int Order = SortOrder.Overlay + 20;

        private Transform root;
        private TextMeshPro newRecord;
        private TextMeshPro tapHint;
        private float shownAt = -1f;

        private GameConfig Config => PlayArea.Config;
        private float U(float px) => Config.PixelsToUnits(px);

        private void Start()
        {
            root = new GameObject("Panel").transform;
            root.SetParent(transform, false);
            root.gameObject.SetActive(false);

            if (GameSession.Instance != null) GameSession.Instance.Finished += Show;
            if (input != null) input.Pressed += OnPressed;
        }

        private void OnDestroy()
        {
            if (GameSession.Instance != null) GameSession.Instance.Finished -= Show;
            if (input != null) input.Pressed -= OnPressed;
        }

        private void Show(GameSession.RunResult r)
        {
            var assets = GameAssets.Instance;
            float px = assets.pixelSprite.bounds.size.x;

            var dim = assets.CreateSprite("Dim", assets.pixelSprite, Order, root);
            dim.transform.localScale = new Vector3(200f / px, 200f / px, 1f);
            dim.color = new Color(0f, 0f, 0.05f, 0.7f);

            var border = assets.CreateSprite("Border", assets.pixelSprite, Order + 1, root);
            border.transform.localScale = new Vector3(U(panelSizePx.x) / px, U(panelSizePx.y) / px, 1f);
            border.color = new Color(0.45f, 0.6f, 1f);
            var back = assets.CreateSprite("Back", assets.pixelSprite, Order + 2, root);
            back.transform.localScale = new Vector3(U(panelSizePx.x - 4f) / px, U(panelSizePx.y - 4f) / px, 1f);
            back.color = new Color(0.07f, 0.08f, 0.16f);

            float half = panelSizePx.x * 0.5f - 8f;
            float box = half * 2f;

            Line("Title", "ui.result.title", GameText.Style.Title, 86f, new Color(1f, 0.55f, 0.5f));

            newRecord = Line("NewRecord", "ui.new_record", GameText.Style.Title, 66f, new Color(1f, 0.9f, 0.3f));
            newRecord.gameObject.SetActive(r.newBest);

            Line("ScoreLabel", "ui.score", GameText.Style.Body, 50f, new Color(0.75f, 0.8f, 1f));
            var score = GameText.Create("Score", root, GameText.Style.Title, Order + 3, TextAlignmentOptions.Center, 0f, false, 2);
            score.text = r.score.ToString();
            score.transform.localPosition = new Vector3(0f, U(32f), 0f);

            Row("Best", "ui.best", r.bestScore.ToString(), 10f, box, Color.white);
            Row("Wave", "ui.wave", r.wave.ToString(), -4f, box, Color.white);
            int sec = Mathf.FloorToInt(r.seconds);
            Row("Time", "ui.time", $"{sec / 60}:{sec % 60:00}", -18f, box, Color.white);

            var gold = new Color(1f, 0.86f, 0.4f);
            Row("Meteor", "ui.gold.meteor", "+" + r.meteorGold, -38f, box, gold);
            Row("Stage", "ui.gold.stage", "+" + r.stageGold, -52f, box, gold);
            var sep = assets.CreateSprite("Sep", assets.pixelSprite, Order + 3, root);
            sep.transform.localScale = new Vector3(U(box) / px, U(1f) / px, 1f);
            sep.transform.localPosition = new Vector3(0f, U(-60f), 0f);
            sep.color = new Color(1f, 0.86f, 0.4f, 0.5f);
            Row("Total", "ui.gold.total", r.totalGold.ToString(), -68f, box, gold);
            Row("Wallet", "ui.gold.wallet", r.walletGold.ToString(), -82f, box, new Color(0.8f, 0.8f, 0.85f));

            tapHint = Line("Tap", "ui.tap_retry", GameText.Style.Body, -96f, new Color(0.75f, 0.85f, 1f));
            tapHint.gameObject.SetActive(false);

            root.gameObject.SetActive(true);
            shownAt = Time.unscaledTime;
        }

        private TextMeshPro Line(string name, string key, GameText.Style style, float yPx, Color color)
        {
            var t = GameText.Create(name, root, style, Order + 3);
            GameText.SetKey(t, key);
            t.color = color;
            t.transform.localPosition = new Vector3(0f, U(yPx), 0f);
            return t;
        }

        private void Row(string name, string key, string value, float yPx, float boxPx, Color color)
        {
            var label = GameText.Create(name + "Label", root, GameText.Style.Body, Order + 3, TextAlignmentOptions.MidlineLeft, boxPx);
            GameText.SetKey(label, key);
            label.color = new Color(0.75f, 0.8f, 1f);
            label.transform.localPosition = new Vector3(0f, U(yPx), 0f);

            var v = GameText.Create(name + "Value", root, GameText.Style.Body, Order + 3, TextAlignmentOptions.MidlineRight, boxPx);
            v.text = value;
            v.color = color;
            v.transform.localPosition = new Vector3(0f, U(yPx), 0f);
        }

        private void Update()
        {
            if (shownAt < 0f) return;
            float t = Time.unscaledTime - shownAt;
            if (newRecord != null && newRecord.gameObject.activeSelf)
                newRecord.alpha = 0.55f + 0.45f * Mathf.PingPong(t * 2.5f, 1f);
            if (tapHint != null)
            {
                bool ready = t >= tapDelay;
                tapHint.gameObject.SetActive(ready);
                if (ready) tapHint.alpha = 0.4f + 0.6f * Mathf.PingPong(t * 1.5f, 1f);
            }
        }

        private void OnPressed(Vector2 _)
        {
            if (shownAt < 0f || Time.unscaledTime - shownAt < tapDelay) return;
            GameSession.Instance.Restart();
        }
    }
}
