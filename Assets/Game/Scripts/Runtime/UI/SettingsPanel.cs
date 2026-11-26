using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 설정 창(로비·일시정지 공용). 위쪽 탭 4개로 나눈다(11/27):
    /// [게임] 조준 보조·화면 흔들림·진동·튜토리얼 하기 / [소리] 소리 켜기·볼륨 /
    /// [계정] 닉네임 바꾸기·언어·데이터 초기화(로비에서만) / [정보] 크레딧·라이선스·버전·게임 종료(로비에서만).
    /// 진동 = 폰 진동(Haptics), 화면 흔들림·조준 보조는 각각 따로 끈다.
    /// </summary>
    public sealed class SettingsPanel : ModalPanel
    {
        private const float Height = 172f;
        private static readonly string[] TabKeys = { "ui.settings.tab.game", "ui.settings.tab.sound", "ui.settings.tab.account", "ui.settings.tab.info" };
        private static readonly string[] LanguageNames = { "한국어", "English", "日本語" };
        private static readonly Color On = new Color(0.35f, 0.75f, 0.45f);
        private static readonly Color Off = new Color(0.35f, 0.35f, 0.45f);
        private static readonly Color Normal = new Color(0.3f, 0.4f, 0.7f);
        private static readonly Color TabOff = new Color(0.2f, 0.24f, 0.4f);

        private readonly PixelButton[] tabButtons = new PixelButton[4];
        private readonly Transform[] tabRoots = new Transform[4];
        private int currentTab;

        private readonly PixelButton[] languageButtons = new PixelButton[3];
        private PixelButton aimButton;
        private PixelButton shakeButton;
        private PixelButton vibrationButton;
        private PixelButton soundButton;
        private PixelSlider volumeSlider;
        private TextMeshPro nicknameLabel;

        private NicknamePanel nickname;
        private CreditsPanel credits;
        private ConfirmPanel confirm;
        private bool inLobby;

        /// <summary>inLobby: 로비에서만 "데이터 초기화"와 "게임 종료"(앱 끝내기)를 보여 준다. 게임 중(일시정지)에는 숨긴다.</summary>
        public static SettingsPanel Create(Transform parent, int sortingOrder, bool inLobby = false)
        {
            var go = new GameObject("SettingsPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<SettingsPanel>();
            p.inLobby = inLobby;
            p.Build(new Vector2(164f, Height), "ui.settings", sortingOrder);
            p.credits = CreditsPanel.Create(parent, sortingOrder + 20);
            p.nickname = NicknamePanel.Create(parent, sortingOrder + 30);
            p.confirm = ConfirmPanel.Create(parent, sortingOrder + 40);
            p.BuildContent();
            return p;
        }

        public bool IsConfirmOpen => confirm != null && confirm.IsOpen;
        public bool IsNicknameOpen => nickname != null && nickname.IsOpen;
        public bool IsCreditsOpen => credits != null && credits.IsOpen;

        /// <summary>뒤로 가기: 맨 위 창부터 하나씩 닫는다. 닫은 게 있으면 true.</summary>
        public bool Back()
        {
            if (IsConfirmOpen) { confirm.Close(); return true; }
            if (IsNicknameOpen) { nickname.Close(); return true; }
            if (IsCreditsOpen) { credits.Close(); return true; }
            if (IsOpen) { Close(); return true; }
            return false;
        }

        // ---------------- 만들기 ----------------

        private float Top => Height * 0.5f;

        private void BuildContent()
        {
            // 탭 버튼
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var b = CreateButton("Tab" + i, new Vector2(-57f + 38f * i, Top - 32f), new Vector2(36f, 16f), TabKeys[i], TabOff, GameText.Style.Body);
                b.Clicked += () => ShowTab(index);
                tabButtons[i] = b;
                var root = new GameObject("TabContent" + i).transform;
                root.SetParent(Content, false);
                tabRoots[i] = root;
            }

            BuildGameTab(tabRoots[0]);
            BuildSoundTab(tabRoots[1]);
            BuildAccountTab(tabRoots[2]);
            BuildInfoTab(tabRoots[3]);
            ShowTab(0);
        }

        private void BuildGameTab(Transform tab)
        {
            float y = Top - 56f;
            aimButton = Toggle(tab, "ui.settings.aim", "Aim", y, () => { SaveSystem.Data.aimAssist = !SaveSystem.Data.aimAssist; });
            y -= 22f;
            shakeButton = Toggle(tab, "ui.settings.shake", "Shake", y, () => { SaveSystem.Data.screenShake = !SaveSystem.Data.screenShake; });
            y -= 22f;
            vibrationButton = Toggle(tab, "ui.settings.vibration", "Vibration", y, () =>
            {
                SaveSystem.Data.vibration = !SaveSystem.Data.vibration;
                Haptics.Pulse(40, 0.7f); // 켰으면 바로 한 번 울려서 느낌을 보여 준다
            });
            y -= 30f;
            var tutorial = Button(tab, "Tutorial", new Vector2(0f, y), new Vector2(140f, 18f), "ui.settings.tutorial", Normal);
            tutorial.Clicked += StartTutorial;
        }

        private void BuildSoundTab(Transform tab)
        {
            float y = Top - 56f;
            soundButton = Toggle(tab, "ui.settings.sound", "Sound", y, () => { SaveSystem.Data.sound = !SaveSystem.Data.sound; });
            y -= 24f;
            Label(tab, "ui.settings.volume", y);
            volumeSlider = PixelSlider.Create("Volume", tab, Order + 5, 100f, new Vector2(20f, y));
            volumeSlider.Owner = this;
            volumeSlider.Changed += v => SaveSystem.Data.volume = v;
            volumeSlider.Released += v => { SaveSystem.Data.volume = v; SaveSystem.Save(); Sound.Play(Sfx.Coin); };
        }

        private void BuildAccountTab(Transform tab)
        {
            float y = Top - 56f;
            nicknameLabel = Text(tab, "NicknameLabel", new Vector2(0f, y), TextAlignmentOptions.MidlineLeft, 140f);
            nicknameLabel.color = new Color(0.75f, 0.8f, 1f);
            var rename = Button(tab, "Rename", new Vector2(44f, y), new Vector2(54f, 16f), "ui.settings.rename", Normal);
            rename.Clicked += () => nickname.Ask(true, _ => RefreshAll());

            y -= 22f;
            Label(tab, "ui.settings.language", y);
            y -= 16f;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var b = Button(tab, "Lang" + i, new Vector2(-50f + 50f * i, y), new Vector2(46f, 16f), null, Normal);
                var label = GameText.Create("Label", b.transform, GameText.Style.Body, Order + 8);
                GameText.SetPlain(label, LanguageNames[i]);
                b.Clicked += () => SetLanguage(index);
                languageButtons[i] = b;
            }

            if (inLobby)
            {
                y -= 28f;
                var reset = Button(tab, "Reset", new Vector2(0f, y), new Vector2(140f, 18f), "ui.settings.reset", new Color(0.8f, 0.35f, 0.35f));
                reset.Clicked += AskReset;
            }
        }

        private void BuildInfoTab(Transform tab)
        {
            float y = Top - 56f;
            var creditsButton = Button(tab, "Credits", new Vector2(0f, y), new Vector2(140f, 18f), "ui.settings.credits", Normal);
            creditsButton.Clicked += () => credits.Open();
            y -= 24f;
            var version = Text(tab, "Version", new Vector2(0f, y), TextAlignmentOptions.Center, 140f);
            GameText.SetPlain(version, "Bouncy UFO  v" + Application.version + " demo");
            version.color = new Color(0.6f, 0.65f, 0.8f);
            if (inLobby)
            {
                y -= 26f;
                var quit = Button(tab, "QuitGame", new Vector2(0f, y), new Vector2(140f, 18f), "ui.quit_game", new Color(0.8f, 0.35f, 0.35f));
                quit.Clicked += () => confirm.Ask("ui.quit_confirm", AppQuit.Now);
            }
        }

        // ---------------- 작은 도우미 ----------------

        /// <summary>탭 안에 버튼(창의 버튼으로 등록한 뒤 탭으로 옮긴다: 숨긴 탭의 버튼은 반응하지 않는다).</summary>
        private PixelButton Button(Transform tab, string name, Vector2 pos, Vector2 size, string key, Color color)
        {
            var b = CreateButton(name, pos, size, key, color, GameText.Style.Body);
            b.transform.SetParent(tab, true);
            return b;
        }

        private TextMeshPro Text(Transform tab, string name, Vector2 pos, TextAlignmentOptions align, float width)
        {
            var t = CreateText(name, pos, GameText.Style.Body, align, width);
            t.transform.SetParent(tab, true);
            return t;
        }

        private void Label(Transform tab, string key, float y)
        {
            var t = Text(tab, "Label", new Vector2(0f, y), TextAlignmentOptions.MidlineLeft, 140f);
            GameText.SetKey(t, key);
            t.color = new Color(0.75f, 0.8f, 1f);
        }

        /// <summary>왼쪽 글자 + 오른쪽 켜기/끄기 버튼 한 줄. 누르면 change 후 저장·새로고침.</summary>
        private PixelButton Toggle(Transform tab, string key, string name, float y, System.Action change)
        {
            Label(tab, key, y);
            var b = Button(tab, name, new Vector2(44f, y), new Vector2(54f, 16f), "ui.on", On);
            b.Clicked += () =>
            {
                change();
                SaveSystem.Save();
                RefreshAll();
            };
            return b;
        }

        private static void SetToggle(PixelButton b, bool on)
        {
            b.SetColor(on ? On : Off);
            GameText.SetKey(b.Label, on ? "ui.on" : "ui.off");
        }

        // ---------------- 동작 ----------------

        private void ShowTab(int index)
        {
            currentTab = index;
            for (int i = 0; i < 4; i++)
            {
                tabRoots[i].gameObject.SetActive(i == index);
                tabButtons[i].SetColor(i == index ? Normal : TabOff);
            }
            RefreshAll();
        }

        /// <summary>튜토리얼을 지금 바로 한다. 게임 중이면 판을 끝내도 되는지 먼저 묻는다(기록·골드는 저장).</summary>
        private void StartTutorial()
        {
            var session = GameSession.Instance;
            if (session != null && session.IsPlaying)
                confirm.Ask("ui.settings.tutorial_confirm", session.LeaveForTutorial);
            else
                SceneFlow.StartTutorial();
        }

        /// <summary>데이터 초기화: 되돌릴 수 없으니 두 번 묻는다. 예 → 모든 저장(설정 포함)을 지우고 타이틀로.</summary>
        private void AskReset()
        {
            confirm.Ask("ui.settings.reset_confirm1", () => confirm.Ask("ui.settings.reset_confirm2", () =>
            {
                SaveSystem.ResetAll();
                Localization.SetLanguage(SaveSystem.Data.language);
                SceneFlow.GoTitle();
            }));
        }

        private void SetLanguage(int index)
        {
            SaveSystem.Data.language = index;
            SaveSystem.Save();
            Localization.SetLanguage(index);
            RefreshAll();
        }

        private void RefreshAll()
        {
            var d = SaveSystem.Data;
            int current = (int)Localization.Current;
            for (int i = 0; i < 3; i++) if (languageButtons[i] != null) languageButtons[i].SetColor(i == current ? On : Normal);
            SetToggle(aimButton, d.aimAssist);
            SetToggle(shakeButton, d.screenShake);
            SetToggle(vibrationButton, d.vibration);
            SetToggle(soundButton, d.sound);
            volumeSlider.SetValue(d.volume);
            GameText.SetPlain(nicknameLabel, Nickname.IsSet ? Nickname.Current : "-");
        }

        protected override void OnOpened()
        {
            ShowTab(currentTab);
        }
    }

    /// <summary>
    /// 크레딧·라이선스. 갈무리 폰트(OFL 1.1)는 저작권 문구와 라이선스 원문을 함께 배포해야 해서 여기서 보여 준다.
    /// </summary>
    public sealed class CreditsPanel : ModalPanel
    {
        private TextMeshPro body;
        private TextMeshPro pageLabel;
        private bool showingLicense;
        private int page = 1;
        private PixelButton licenseButton;

        public static CreditsPanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("CreditsPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<CreditsPanel>();
            p.Build(new Vector2(170f, 280f), "ui.settings.credits", sortingOrder);
            p.BuildContent();
            return p;
        }

        private void BuildContent()
        {
            body = CreateText("Body", new Vector2(0f, 110f), GameText.Style.Body, TextAlignmentOptions.Top, 156f, true);
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.rectTransform.sizeDelta = new Vector2(U(156f), U(200f));
            body.overflowMode = TextOverflowModes.Page;

            pageLabel = CreateText("Page", new Vector2(0f, -112f), GameText.Style.Body);
            var prev = CreateButton("Prev", new Vector2(-56f, -112f), new Vector2(30f, 16f), null, new Color(0.3f, 0.4f, 0.7f));
            GameText.SetPlain(GameText.Create("L", prev.transform, GameText.Style.Title, Order + 8), "<");
            prev.Clicked += () => Turn(-1);
            var next = CreateButton("Next", new Vector2(56f, -112f), new Vector2(30f, 16f), null, new Color(0.3f, 0.4f, 0.7f));
            GameText.SetPlain(GameText.Create("R", next.transform, GameText.Style.Title, Order + 8), ">");
            next.Clicked += () => Turn(1);

            licenseButton = CreateButton("License", new Vector2(0f, -88f), new Vector2(120f, 16f), "ui.credits.license", new Color(0.3f, 0.4f, 0.7f), GameText.Style.Body);
            licenseButton.Clicked += () => { showingLicense = !showingLicense; page = 1; Refresh(); };
        }

        private void Turn(int delta)
        {
            int pages = Mathf.Max(1, body.textInfo != null ? body.textInfo.pageCount : 1);
            page = Mathf.Clamp(page + delta, 1, pages);
            Refresh();
        }

        private void Refresh()
        {
            if (showingLicense)
            {
                var license = GameAssets.Instance.fontLicense;
                GameText.SetPlain(body, license != null ? license.text.Replace("\r", "") : "SIL Open Font License 1.1");
                GameText.SetKey(licenseButton.Label, "ui.credits.back");
            }
            else
            {
                GameText.SetKey(body, "ui.credits.body");
                GameText.SetKey(licenseButton.Label, "ui.credits.license");
            }
            body.pageToDisplay = page;
            body.ForceMeshUpdate();
            int pages = Mathf.Max(1, body.textInfo.pageCount);
            if (page > pages) { page = pages; body.pageToDisplay = page; }
            GameText.SetPlain(pageLabel, page + " / " + pages);
        }

        protected override void OnOpened()
        {
            showingLicense = false;
            page = 1;
            Refresh();
        }
    }
}
