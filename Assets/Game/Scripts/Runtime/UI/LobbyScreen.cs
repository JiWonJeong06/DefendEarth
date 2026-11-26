using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BouncyUFO
{
    /// <summary>
    /// 로비 = 달 기지 마을(11/27, Village): 가운데에서 꾸민 캐릭터를 조이스틱으로 걷게 하고, 마을을 꾸민다.
    /// 위: 프로필 카드(누르면 프로필 창)·골드, 오른쪽 위 세로 아이콘(순위표·설정·마을 꾸미기). 아래: 시작 버튼만.
    /// 마을의 발사대 앞에 서면 출격 준비, 기록탑 앞에 서면 순위표. 뒤로 가기(안드로이드 뒤로 버튼)는 열린 창부터 닫는다.
    /// </summary>
    public sealed class LobbyScreen : MonoBehaviour
    {
        private const int PanelOrder = SortOrder.Overlay;

        private PilotView pilot;
        private TextMeshPro level;
        private TextMeshPro nickname;
        private TextMeshPro gold;
        private Transform xpFill;
        private float xpWidth;
        private float xpLeft;
        private Village village;
        private PixelButton startButton;

        private CustomizePanel customize;
        private LeaderboardPanel ranking;
        private SettingsPanel settings;
        private ProfilePanel profile;
        private LaunchPanel launch;
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

            // 가운데: 달 기지 마을
            village = Village.Create(transform);

            // 버튼
            var start = PixelButton.Create("Start", transform, new Vector2(0f, -142f), new Vector2(124f, 26f), "ui.start", new Color(0.3f, 0.75f, 0.45f), SortOrder.Hud, 0);
            start.Clicked += OpenLaunch;
            startButton = start;

            int panelBase = PanelOrder;
            customize = CustomizePanel.Create(transform, panelBase);
            ranking = LeaderboardPanel.Create(transform, panelBase);
            settings = SettingsPanel.Create(transform, panelBase, true);
            profile = ProfilePanel.Create(transform, panelBase);
            card.Clicked += profile.Open;
            profile.Closed += () => { pilot.Refresh(); Refresh(); };
            profile.CustomizeRequested += customize.Open;
            quitConfirm = ConfirmPanel.Create(transform, panelBase + 60);
            customize.Closed += () => pilot.Refresh();
            settings.Closed += Refresh; // 닉네임을 바꿨을 수 있다

            launch = LaunchPanel.Create(transform, panelBase);
            launch.Changed += () => { Refresh(); village.RefreshUfo(); };
            launch.Closed += village.StepBackFromDoor;
            ranking.Closed += village.StepBackFromDoor;
            customize.Closed += village.RefreshCharacter;
            village.LaunchPadEntered += OpenLaunch;
            village.TowerEntered += ranking.Open;
            village.GoldChanged += Refresh;
            village.DecorateChanged += on => startButton.gameObject.SetActive(!on); // 꾸밀 때는 아래 꾸미기 줄

            // 오른쪽 위 세로 아이콘 버튼(11/27): 순위표·설정. 시작 버튼만 아래에.
            // 꾸미기는 프로필 창(왼쪽 위 프로필 카드)에서, UFO·직업은 시작 → 출격 준비에서.
            var a2 = GameAssets.Instance;
            IconButton("Ranking", a2.iconRanking, 0, new Color(0.35f, 0.55f, 0.9f), ranking.Open);
            IconButton("Settings", a2.iconSettings, 1, new Color(0.45f, 0.5f, 0.6f), settings.Open);
            IconButton("Decorate", a2.iconDecorate, 2, new Color(0.75f, 0.5f, 0.3f), () => village.SetDecorating(!village.Decorating));

            Refresh();
        }

        private const float IconSizePx = 22f;   // 엄지로 누르기 좋은 크기(정사각형)
        private const float IconGapPx = 4f;

        /// <summary>오른쪽 위(골드 아래)에 정사각형 아이콘 버튼을 세로로 쌓는다. slot 0이 맨 위.</summary>
        private void IconButton(string name, Sprite icon, int slot, Color color, System.Action onClick)
        {
            float half = Config.playAreaPixels.x * 0.5f;
            float top = Config.playAreaPixels.y * 0.5f;
            var pos = new Vector2(half - 4f - IconSizePx * 0.5f, top - 30f - IconSizePx * 0.5f - slot * (IconSizePx + IconGapPx));
            var b = PixelButton.Create(name, transform, pos, new Vector2(IconSizePx, IconSizePx), null, color, SortOrder.Hud, 0);
            if (icon != null) GameAssets.Instance.CreateSprite("Icon", icon, SortOrder.Hud + 4, b.transform);
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
            if (village == null) return;
            Refresh();
            if (launch.IsOpen) { launch.Close(); launch.Open(); }
        }
        private void OnLanguageChanged() { if (village != null) Refresh(); }

        /// <summary>튜토리얼을 아직 안 했으면 바로 튜토리얼, 아니면 출격 준비(직업·UFO 고르기).</summary>
        private void OpenLaunch()
        {
            if (!SaveSystem.Data.tutorialDone) SceneFlow.StartGame();
            else if (!launch.IsOpen) launch.Open();
        }

        private void Update()
        {
            // 안드로이드 뒤로 버튼(= Esc): 맨 위 창부터 닫고, 열린 창이 없으면 "게임을 종료할까요?"
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (quitConfirm.IsOpen) quitConfirm.Close();
                else if (settings.Back()) { }
                else if (profile.Back()) { }
                else if (launch.IsOpen) launch.Close();
                else if (village.Decorating) village.SetDecorating(false);
                else if (customize.IsOpen) customize.Close();
                else if (ranking.IsOpen) ranking.Close();
                else quitConfirm.Ask("ui.quit_confirm", AppQuit.Now);
            }
        }
    }
}
