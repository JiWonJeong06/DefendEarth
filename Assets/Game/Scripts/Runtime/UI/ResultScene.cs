using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BouncyUFO
{
    /// <summary>
    /// 결과 씬: 점수(신기록), 최고 기록, 웨이브·버틴 시간, 골드(운석 + 단계 보상 = 이번 판, 가진 골드),
    /// 플레이 경험치와 계정 레벨(데모는 보여주기용). 다시 하기 / 로비.
    /// 값은 게임 씬이 정산해서 SceneFlow.LastResult로 넘겨준다.
    /// </summary>
    public sealed class ResultScene : MonoBehaviour
    {
        [SerializeField] private Vector2 panelSizePx = new Vector2(160f, 270f);

        private const int Order = SortOrder.Hud;

        private TextMeshPro newRecord;
        private TextMeshPro levelUp;
        private Transform xpFill;
        private float xpWidth;
        private float xpShown;
        private float xpTarget;
        private float shownAt;

        private GameConfig Config => PlayArea.Config;
        private float U(float px) => Config.PixelsToUnits(px);

        private void Start()
        {
            var a = GameAssets.Instance;
            float px = a.pixelSprite.bounds.size.x;
            StarField.Create(transform, 25);
            var r = SceneFlow.HasResult ? SceneFlow.LastResult : new RunResult();

            var border = a.CreateSprite("Border", a.pixelSprite, Order, transform);
            border.transform.localScale = new Vector3(U(panelSizePx.x) / px, U(panelSizePx.y) / px, 1f);
            border.color = new Color(0.45f, 0.6f, 1f);
            var back = a.CreateSprite("Back", a.pixelSprite, Order + 1, transform);
            back.transform.localScale = new Vector3(U(panelSizePx.x - 4f) / px, U(panelSizePx.y - 4f) / px, 1f);
            back.color = new Color(0.07f, 0.08f, 0.16f);

            float box = panelSizePx.x - 16f;
            Line("Title", r.quit ? "ui.result.quit" : "ui.result.title", GameText.Style.Title, 120f, r.quit ? new Color(0.75f, 0.85f, 1f) : new Color(1f, 0.55f, 0.5f));

            newRecord = Line("NewRecord", "ui.new_record", GameText.Style.Title, 102f, new Color(1f, 0.9f, 0.3f));
            newRecord.gameObject.SetActive(r.newBest);

            Line("ScoreLabel", "ui.score", GameText.Style.Body, 88f, new Color(0.75f, 0.8f, 1f));
            var score = GameText.Create("Score", transform, GameText.Style.Title, Order + 3, TextAlignmentOptions.Center, 0f, false, 2);
            GameText.SetPlain(score, r.score.ToString());
            score.transform.position = new Vector3(0f, U(70f), 0f);

            Row("Best", "ui.best", r.bestScore.ToString(), 48f, box, Color.white);
            Row("Wave", "ui.wave", r.wave.ToString(), 36f, box, Color.white);
            int sec = Mathf.FloorToInt(r.seconds);
            Row("Time", "ui.time", $"{sec / 60}:{sec % 60:00}", 24f, box, Color.white);

            var gold = new Color(1f, 0.86f, 0.4f);
            Row("Meteor", "ui.gold.meteor", "+" + r.meteorGold, 4f, box, gold);
            Row("Stage", "ui.gold.stage", "+" + r.stageGold, -8f, box, gold);
            var sep = a.CreateSprite("Sep", a.pixelSprite, Order + 3, transform);
            sep.transform.localScale = new Vector3(U(box) / px, U(1f) / px, 1f);
            sep.transform.position = new Vector3(0f, U(-15f), 0f);
            sep.color = new Color(1f, 0.86f, 0.4f, 0.5f);
            Row("Total", "ui.gold.total", r.totalGold.ToString(), -22f, box, gold);
            Row("Wallet", "ui.gold.wallet", r.walletGold.ToString(), -34f, box, new Color(0.8f, 0.8f, 0.85f));

            // 플레이 경험치 → 계정 레벨
            PilotView.Create(transform, new Vector3(U(-58f), U(-62f), 0f), 1, Order + 3);
            Row("Xp", "ui.result.xp", "+" + r.xpGained, -54f, box - 34f, new Color(0.6f, 1f, 0.7f), 17f);
            var lv = GameText.Create("Lv", transform, GameText.Style.Body, Order + 3, TextAlignmentOptions.MidlineLeft, box - 34f);
            GameText.SetPlain(lv, "Lv " + Mathf.Max(1, r.levelAfter));
            lv.transform.position = new Vector3(U(17f), U(-66f), 0f);
            xpWidth = U(box - 34f);
            var xpBack = a.CreateSprite("XpBack", a.pixelSprite, Order + 3, transform);
            xpBack.color = new Color(0.1f, 0.12f, 0.25f);
            xpBack.transform.localScale = new Vector3(xpWidth / px, U(3f) / px, 1f);
            xpBack.transform.position = new Vector3(U(17f), U(-76f), 0f);
            var xf = a.CreateSprite("XpFill", a.pixelSprite, Order + 4, transform);
            xf.color = new Color(0.6f, 1f, 0.7f);
            xpFill = xf.transform;
            xpTarget = r.xpNeedAfter > 0 ? Mathf.Clamp01((float)r.xpAfter / r.xpNeedAfter) : 0f;
            levelUp = Line("LevelUp", "ui.result.levelup", GameText.Style.Body, -88f, new Color(0.6f, 1f, 0.7f));
            levelUp.gameObject.SetActive(r.levelAfter > r.levelBefore && r.levelBefore > 0);

            var retry = PixelButton.Create("Retry", transform, new Vector2(0f, -108f), new Vector2(120f, 22f), "ui.retry", new Color(0.3f, 0.75f, 0.45f), Order + 5, 0);
            retry.Clicked += SceneFlow.StartGame;
            var lobby = PixelButton.Create("Lobby", transform, new Vector2(0f, -132f), new Vector2(120f, 18f), "ui.to_lobby", new Color(0.35f, 0.45f, 0.7f), Order + 5, 0, GameText.Style.Body);
            lobby.Clicked += SceneFlow.GoLobby;

            shownAt = Time.unscaledTime;
        }

        private TextMeshPro Line(string name, string key, GameText.Style style, float yPx, Color color)
        {
            var t = GameText.Create(name, transform, style, Order + 3);
            GameText.SetKey(t, key);
            t.color = color;
            t.transform.position = new Vector3(0f, U(yPx), 0f);
            return t;
        }

        private void Row(string name, string key, string value, float yPx, float boxPx, Color color, float xOffsetPx = 0f)
        {
            var label = GameText.Create(name + "Label", transform, GameText.Style.Body, Order + 3, TextAlignmentOptions.MidlineLeft, boxPx);
            GameText.SetKey(label, key);
            label.color = new Color(0.75f, 0.8f, 1f);
            label.transform.position = new Vector3(U(xOffsetPx), U(yPx), 0f);

            var v = GameText.Create(name + "Value", transform, GameText.Style.Body, Order + 3, TextAlignmentOptions.MidlineRight, boxPx);
            GameText.SetPlain(v, value);
            v.color = color;
            v.transform.position = new Vector3(U(xOffsetPx), U(yPx), 0f);
        }

        private void Update()
        {
            float t = Time.unscaledTime - shownAt;
            if (newRecord.gameObject.activeSelf) newRecord.alpha = 0.55f + 0.45f * Mathf.PingPong(t * 2.5f, 1f);
            if (levelUp.gameObject.activeSelf) levelUp.alpha = 0.55f + 0.45f * Mathf.PingPong(t * 2f, 1f);

            // 경험치 바가 차오르는 연출
            xpShown = Mathf.MoveTowards(xpShown, xpTarget, Time.unscaledDeltaTime * 0.8f);
            float px = GameAssets.Instance.pixelSprite.bounds.size.x;
            xpFill.localScale = new Vector3(xpWidth * xpShown / px, U(3f) / px, 1f);
            xpFill.position = new Vector3(U(17f) - xpWidth * 0.5f + xpWidth * xpShown * 0.5f, U(-76f), 0f);

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SceneFlow.GoLobby();
        }
    }
}
