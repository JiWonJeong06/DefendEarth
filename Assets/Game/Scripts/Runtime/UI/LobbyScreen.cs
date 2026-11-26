using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BouncyUFO
{
    /// <summary>
    /// 로비: 프로필(캐릭터·닉네임·계정 레벨), 골드, 최고 기록, 게임 시작, 강화·꾸미기·순위표·설정.
    /// 뒤로 가기(안드로이드 뒤로 버튼)는 열린 창을 닫는다.
    /// </summary>
    public sealed class LobbyScreen : MonoBehaviour
    {
        private const int PanelOrder = SortOrder.Overlay;

        private PilotView pilot;
        private TextMeshPro level;
        private TextMeshPro nickname;
        private TextMeshPro gold;
        private TextMeshPro best;
        private Transform xpFill;
        private float xpWidth;
        private float xpLeft;
        private Transform ufo;
        private SquashStretch ufoSquash;
        private int lastBounce;

        private UpgradePanel upgrade;
        private CustomizePanel customize;
        private LeaderboardPanel ranking;
        private SettingsPanel settings;
        private ProfilePanel profile;
        private UfoPanel ufoPanel;
        private SpriteRenderer ufoRenderer;
        private ConfirmPanel quitConfirm;

        private GameConfig Config => PlayArea.Config;
        private float U(float px) => Config.PixelsToUnits(px);

        private void Start()
        {
            var a = GameAssets.Instance;
            float px = a.pixelSprite.bounds.size.x;
            StarField.Create(transform, 30);

            // 위: 프로필(캐릭터 + 닉네임 / 계정 레벨 + 경험치 바). 누르면 프로필 창
            var card = PixelButton.Create("ProfileCard", transform,
                new Vector2(-Config.playAreaPixels.x * 0.5f + 55f, Config.playAreaPixels.y * 0.5f - 22f), new Vector2(106f, 40f),
                null, new Color(0.16f, 0.2f, 0.42f, 0.55f), SortOrder.Hud - 5, 0);
            pilot = PilotView.Create(transform, new Vector3(PlayArea.Left + U(16f), PlayArea.Top - U(22f), 0f), 1, SortOrder.Hud);
            // 1줄: 닉네임 (한글 8자 = 80픽셀까지, 골드 숫자와 겹치지 않게)
            nickname = GameText.Create("Nickname", transform, GameText.Style.Body, SortOrder.Hud + 2, TextAlignmentOptions.MidlineLeft, 84f);
            nickname.transform.position = new Vector3(PlayArea.Left + U(30f + 42f), PlayArea.Top - U(11f), 0f);
            // 2줄: "Lv 3" + 경험치 바
            level = GameText.Create("Level", transform, GameText.Style.Body, SortOrder.Hud + 2, TextAlignmentOptions.MidlineLeft, 30f);
            level.color = new Color(1f, 0.85f, 0.35f);
            level.transform.position = new Vector3(PlayArea.Left + U(30f + 15f), PlayArea.Top - U(23f), 0f);
            xpWidth = U(44f);
            xpLeft = PlayArea.Left + U(62f);
            var xpBack = a.CreateSprite("XpBack", a.pixelSprite, SortOrder.Hud, transform);
            xpBack.color = new Color(0.1f, 0.12f, 0.25f);
            xpBack.transform.localScale = new Vector3(xpWidth / px, U(3f) / px, 1f);
            xpBack.transform.position = new Vector3(xpLeft + xpWidth * 0.5f, PlayArea.Top - U(23f), 0f);
            var xf = a.CreateSprite("XpFill", a.pixelSprite, SortOrder.Hud + 1, xpBack.transform.parent);
            xf.color = new Color(0.6f, 1f, 0.7f);
            xpFill = xf.transform;
            xpFill.position = xpBack.transform.position;

            // 위 오른쪽: 골드
            var coin = a.CreateSprite("Coin", a.coinSprite, SortOrder.Hud, transform);
            coin.transform.position = new Vector3(PlayArea.Right - U(66f), PlayArea.Top - U(14f), 0f);
            gold = GameText.Create("Gold", transform, GameText.Style.Title, SortOrder.Hud + 2, TextAlignmentOptions.MidlineRight, 80f);
            gold.color = new Color(1f, 0.86f, 0.4f);
            gold.transform.position = new Vector3(PlayArea.Right - U(6f + 40f), PlayArea.Top - U(14f), 0f);

            // 제목 + 최고 기록
            var title = GameText.Create("Title", transform, GameText.Style.Title, SortOrder.Hud + 2);
            GameText.SetKey(title, "game.title");
            title.color = new Color(0.65f, 0.95f, 1f);
            title.transform.position = new Vector3(0f, U(104f), 0f);
            best = GameText.Create("Best", transform, GameText.Style.Body, SortOrder.Hud + 2, TextAlignmentOptions.Center, 170f);
            best.transform.position = new Vector3(0f, U(88f), 0f);
            best.color = new Color(0.8f, 0.85f, 1f);

            // 가운데: 지구 위에서 통통 튀는 UFO
            var earth = EarthGlobe.CreateWhole("Earth", transform, SortOrder.Earth, 44f);
            earth.transform.position = new Vector3(0f, U(-4f), 0f);
            var root = new GameObject("UFO").transform;
            root.SetParent(transform, false);
            var sr = a.CreateSprite("Visual", a.ufoSprite, SortOrder.Ufo, root);
            UfoTypes.Apply(sr, UfoTypes.SelectedIndex); // 고른 UFO
            ufoRenderer = sr;
            sr.transform.localScale = Vector3.one * 2f;
            ufoSquash = sr.gameObject.AddComponent<SquashStretch>();
            ufo = root;

            // 버튼
            var start = PixelButton.Create("Start", transform, new Vector2(0f, -66f), new Vector2(124f, 28f), "ui.start", new Color(0.3f, 0.75f, 0.45f), SortOrder.Hud, 0);
            start.Clicked += SceneFlow.StartGame;

            int panelBase = PanelOrder;
            upgrade = UpgradePanel.Create(transform, panelBase);
            customize = CustomizePanel.Create(transform, panelBase);
            ranking = LeaderboardPanel.Create(transform, panelBase);
            settings = SettingsPanel.Create(transform, panelBase, true);
            profile = ProfilePanel.Create(transform, panelBase);
            card.Clicked += profile.Open;
            profile.Closed += () => { pilot.Refresh(); Refresh(); };
            profile.CustomizeRequested += customize.Open;
            quitConfirm = ConfirmPanel.Create(transform, panelBase + 60);
            upgrade.GoldSpent += Refresh;
            customize.Closed += () => pilot.Refresh();
            settings.Closed += Refresh; // 닉네임을 바꿨을 수 있다

            ufoPanel = UfoPanel.Create(transform, panelBase);
            ufoPanel.Changed += () => { Refresh(); UfoTypes.Apply(ufoRenderer, UfoTypes.SelectedIndex); };

            // 꾸미기는 프로필 창(왼쪽 위 프로필 카드)에서 연다
            Menu("Upgrade", "ui.upgrade", new Vector2(-40f, -100f), new Color(0.85f, 0.55f, 0.3f), upgrade.Open);
            Menu("Ufo", "ui.ufo", new Vector2(40f, -100f), new Color(0.3f, 0.7f, 0.75f), ufoPanel.Open);
            Menu("Ranking", "ui.ranking", new Vector2(-40f, -124f), new Color(0.35f, 0.55f, 0.9f), ranking.Open);
            Menu("Settings", "ui.settings", new Vector2(40f, -124f), new Color(0.45f, 0.5f, 0.6f), settings.Open);

            Refresh();
        }

        private void Menu(string name, string key, Vector2 posPx, Color color, System.Action onClick)
        {
            var b = PixelButton.Create(name, transform, posPx, new Vector2(76f, 20f), key, color, SortOrder.Hud, 0, GameText.Style.Body);
            b.Clicked += onClick;
        }

        private void Refresh()
        {
            var d = SaveSystem.Data;
            var acc = Account.FromTotal(d.accountXp);
            GameText.SetPlain(nickname, Nickname.IsSet ? Nickname.Current : "-");
            GameText.SetPlain(level, "Lv " + acc.level);
            float k = acc.need > 0 ? Mathf.Clamp01((float)acc.xp / acc.need) : 0f;
            float px = GameAssets.Instance.pixelSprite.bounds.size.x;
            xpFill.localScale = new Vector3(xpWidth * k / px, U(3f) / px, 1f);
            xpFill.position = new Vector3(xpLeft + xpWidth * k * 0.5f, PlayArea.Top - U(23f), 0f);
            GameText.SetPlain(gold, d.gold.ToString());
            GameText.SetPlain(best, Localization.Get("ui.best") + "  " + d.bestScore);
        }

        private void OnEnable()
        {
            Localization.Changed += OnLanguageChanged;
            DevCheats.Changed += OnDevChanged;
        }

        private void OnDisable()
        {
            Localization.Changed -= OnLanguageChanged;
            DevCheats.Changed -= OnDevChanged;
        }

        /// <summary>개발 키(F1 골드)로 골드가 바뀌면 로비 숫자와 열린 창을 다시 그린다.</summary>
        private void OnDevChanged()
        {
            if (best == null) return;
            Refresh();
            if (upgrade.IsOpen) { upgrade.Close(); upgrade.Open(); }
            if (ufoPanel.IsOpen) { ufoPanel.Close(); ufoPanel.Open(); }
        }
        private void OnLanguageChanged() { if (best != null) Refresh(); }

        private void Update()
        {
            // UFO 통통
            float period = 0.7f;
            float t = Time.time;
            int bounce = Mathf.FloorToInt(t / period);
            float local = t / period - bounce;
            ufo.position = new Vector3(0f, U(34f) + Mathf.Sin(local * Mathf.PI) * U(8f), 0f);
            if (bounce != lastBounce) { lastBounce = bounce; ufoSquash.Punch(0.25f); }

            // 안드로이드 뒤로 버튼(= Esc): 맨 위 창부터 닫고, 열린 창이 없으면 "게임을 종료할까요?"
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (quitConfirm.IsOpen) quitConfirm.Close();
                else if (settings.Back()) { }
                else if (profile.Back()) { }
                else if (upgrade.IsOpen) upgrade.Close();
                else if (ufoPanel.IsOpen) ufoPanel.Close();
                else if (customize.IsOpen) customize.Close();
                else if (ranking.IsOpen) ranking.Close();
                else quitConfirm.Ask("ui.quit_confirm", AppQuit.Now);
            }
        }
    }
}
