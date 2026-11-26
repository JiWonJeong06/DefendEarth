using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 설정 창(로비·일시정지 공용): 언어, 소리, 진동, 튜토리얼 다시 보기, 크레딧·라이선스.
    /// 소리·진동은 아직 없어서 설정만 저장한다(진동을 실제로 쓰면 VIBRATE 권한이 추가되므로 그때 확인).
    /// </summary>
    public sealed class SettingsPanel : ModalPanel
    {
        private static readonly string[] LanguageNames = { "한국어", "English", "日本語" };
        private static readonly Color On = new Color(0.35f, 0.75f, 0.45f);
        private static readonly Color Off = new Color(0.35f, 0.35f, 0.45f);
        private static readonly Color Normal = new Color(0.3f, 0.4f, 0.7f);

        private readonly PixelButton[] languageButtons = new PixelButton[3];
        private PixelButton soundButton;
        private PixelButton vibrationButton;
        private TextMeshPro toast;
        private float toastUntil;
        private CreditsPanel credits;

        private ConfirmPanel confirm;

        /// <summary>showQuitGame: 로비에서는 "게임 종료"(앱 끝내기) 버튼을 보여 준다. 게임 중(일시정지)에는 '나가기'가 따로 있어서 숨긴다.</summary>
        public static SettingsPanel Create(Transform parent, int sortingOrder, bool showQuitGame = false)
        {
            var go = new GameObject("SettingsPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<SettingsPanel>();
            p.Build(new Vector2(164f, showQuitGame ? 240f : 214f), "ui.settings", sortingOrder);
            p.BuildContent(showQuitGame ? 13f : 0f);
            p.credits = CreditsPanel.Create(parent, sortingOrder + 20);
            if (showQuitGame)
            {
                p.confirm = ConfirmPanel.Create(parent, sortingOrder + 40);
                var quit = p.CreateButton("QuitGame", new Vector2(0f, -100f), new Vector2(140f, 18f), "ui.quit_game", new Color(0.8f, 0.35f, 0.35f), GameText.Style.Body);
                quit.Clicked += () => p.confirm.Ask("ui.quit_confirm", AppQuit.Now);
            }
            return p;
        }

        public bool IsConfirmOpen => confirm != null && confirm.IsOpen;
        public bool IsCreditsOpen => credits != null && credits.IsOpen;

        /// <summary>뒤로 가기: 맨 위 창부터 하나씩 닫는다. 닫은 게 있으면 true.</summary>
        public bool Back()
        {
            if (IsConfirmOpen) { confirm.Close(); return true; }
            if (IsCreditsOpen) { credits.Close(); return true; }
            if (IsOpen) { Close(); return true; }
            return false;
        }

        private void BuildContent(float shiftUpPx)
        {
            Content.localPosition = new Vector3(0f, U(shiftUpPx), 0f);
            float y = 72f;
            Row("ui.settings.language", y);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var b = CreateButton("Lang" + i, new Vector2(-50f + 50f * i, y - 16f), new Vector2(46f, 16f), null, Normal);
                var label = GameText.Create("Label", b.transform, GameText.Style.Body, Order + 8);
                GameText.SetPlain(label, LanguageNames[i]);
                b.Clicked += () => SetLanguage(index);
                languageButtons[i] = b;
            }

            y -= 44f;
            Row("ui.settings.sound", y);
            soundButton = CreateButton("Sound", new Vector2(44f, y), new Vector2(54f, 16f), "ui.on", On, GameText.Style.Body);
            soundButton.Clicked += () => { SaveSystem.Data.sound = !SaveSystem.Data.sound; SaveSystem.Save(); RefreshToggles(); };

            y -= 24f;
            Row("ui.settings.vibration", y);
            vibrationButton = CreateButton("Vibration", new Vector2(44f, y), new Vector2(54f, 16f), "ui.on", On, GameText.Style.Body);
            vibrationButton.Clicked += () => { SaveSystem.Data.vibration = !SaveSystem.Data.vibration; SaveSystem.Save(); RefreshToggles(); };

            y -= 30f;
            var tutorial = CreateButton("Tutorial", new Vector2(0f, y), new Vector2(140f, 18f), "ui.settings.tutorial", Normal, GameText.Style.Body);
            tutorial.Clicked += () =>
            {
                SaveSystem.Data.tutorialDone = false;
                SaveSystem.Save();
                ShowToast("ui.settings.tutorial_done");
            };

            y -= 24f;
            var creditsButton = CreateButton("Credits", new Vector2(0f, y), new Vector2(140f, 18f), "ui.settings.credits", Normal, GameText.Style.Body);
            creditsButton.Clicked += () => credits.Open();

            toast = CreateText("Toast", new Vector2(0f, y - 26f), GameText.Style.Body, TextAlignmentOptions.Center, 150f, true);
            toast.color = new Color(1f, 0.9f, 0.5f);
            toast.gameObject.SetActive(false);
        }

        private void Row(string key, float y)
        {
            var t = CreateText("Label", new Vector2(0f, y), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 140f);
            GameText.SetKey(t, key);
            t.color = new Color(0.75f, 0.8f, 1f);
        }

        private void SetLanguage(int index)
        {
            SaveSystem.Data.language = index;
            SaveSystem.Save();
            Localization.SetLanguage(index);
            RefreshToggles();
        }

        private void ShowToast(string key)
        {
            GameText.SetKey(toast, key);
            toast.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + 2.5f;
        }

        private void RefreshToggles()
        {
            var d = SaveSystem.Data;
            int current = (int)Localization.Current;
            for (int i = 0; i < 3; i++) languageButtons[i].SetColor(i == current ? On : Normal);
            soundButton.SetColor(d.sound ? On : Off);
            GameText.SetKey(soundButton.Label, d.sound ? "ui.on" : "ui.off");
            vibrationButton.SetColor(d.vibration ? On : Off);
            GameText.SetKey(vibrationButton.Label, d.vibration ? "ui.on" : "ui.off");
        }

        protected override void OnOpened()
        {
            RefreshToggles();
            toast.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (toast != null && toast.gameObject.activeSelf && Time.unscaledTime > toastUntil) toast.gameObject.SetActive(false);
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
