using UnityEngine;
using UnityEngine.InputSystem;

namespace BouncyUFO
{
    /// <summary>
    /// 게임 중 일시정지: 오른쪽 위 버튼(또는 안드로이드 뒤로 버튼, 앱이 뒤로 가면 자동).
    /// 계속하기 / 설정 / 나가기. 나가기는 판을 끝내고 결과로(기록·골드·경험치는 그대로 인정).
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private const int PanelOrder = SortOrder.Overlay + 40;

        private PixelButton pauseButton;
        private ModalPanel panel;
        private SettingsPanel settings;
        private bool paused;

        private GameConfig Config => PlayArea.Config;

        private void Start()
        {
            var a = GameAssets.Instance;
            float px = a.pixelSprite.bounds.size.x;

            // 오른쪽 위 일시정지 버튼(지구 체력 바 오른쪽 빈 곳). "II" 모양
            pauseButton = PixelButton.Create("PauseButton", transform,
                new Vector2(Config.playAreaPixels.x * 0.5f - 9f, Config.playAreaPixels.y * 0.5f - 8f),
                new Vector2(13f, 11f), null, new Color(0.3f, 0.35f, 0.55f), SortOrder.Hud + 6, 0);
            for (int i = -1; i <= 1; i += 2)
            {
                var bar = a.CreateSprite("Bar", a.pixelSprite, SortOrder.Hud + 9, pauseButton.transform);
                bar.transform.localScale = new Vector3(Config.PixelsToUnits(2f) / px, Config.PixelsToUnits(6f) / px, 1f);
                bar.transform.localPosition = new Vector3(Config.PixelsToUnits(2f) * i, 0f, 0f);
            }
            pauseButton.Clicked += Open;

            var go = new GameObject("PausePanel");
            go.transform.SetParent(transform, false);
            panel = go.AddComponent<ModalPanel>();
            panel.Build(new Vector2(130f, 130f), "ui.pause", PanelOrder);
            var resume = panel.CreateButton("Resume", new Vector2(0f, 22f), new Vector2(100f, 20f), "ui.pause.resume", new Color(0.3f, 0.75f, 0.45f));
            resume.Clicked += Close;
            var set = panel.CreateButton("Settings", new Vector2(0f, -4f), new Vector2(100f, 20f), "ui.settings", new Color(0.45f, 0.5f, 0.6f));
            var quit = panel.CreateButton("Quit", new Vector2(0f, -30f), new Vector2(100f, 20f), "ui.pause.quit", new Color(0.8f, 0.35f, 0.35f));
            quit.Clicked += Quit;
            panel.Closed += OnPanelClosed;

            settings = SettingsPanel.Create(transform, PanelOrder + 20);
            set.Clicked += settings.Open;

            if (GameSession.Instance != null) GameSession.Instance.Finished += ShowFallenBanner;
        }

        private void OnDestroy()
        {
            if (GameSession.Instance != null) GameSession.Instance.Finished -= ShowFallenBanner;
        }

        /// <summary>지구가 함락되면 결과 씬으로 넘어가기 전 잠깐 보여 준다(나가기일 때는 바로 넘어감).</summary>
        private void ShowFallenBanner(RunResult r)
        {
            if (r.quit) return;
            var a = GameAssets.Instance;
            float px = a.pixelSprite.bounds.size.x;
            var dim = a.CreateSprite("FallenDim", a.pixelSprite, SortOrder.Overlay, transform);
            dim.transform.localScale = new Vector3(200f / px, 200f / px, 1f);
            dim.color = new Color(0.15f, 0f, 0.02f, 0.55f);
            var text = GameText.Create("Fallen", transform, GameText.Style.Title, SortOrder.Overlay + 1, TMPro.TextAlignmentOptions.Center, 0f, false, 1);
            GameText.SetKey(text, "ui.result.title");
            text.color = new Color(1f, 0.6f, 0.55f);
        }

        private bool CanPause
        {
            get
            {
                var s = GameSession.Instance;
                return s != null && s.IsPlaying && !s.IsPaused && !paused;
            }
        }

        public void Open()
        {
            if (!CanPause) return;
            paused = true;
            GameSession.Instance.Pause();
            panel.Open();
        }

        private void Close()
        {
            if (settings.IsOpen) settings.Close();
            panel.Close(); // OnPanelClosed에서 다시 움직인다
        }

        private void OnPanelClosed()
        {
            if (!paused) return;
            paused = false;
            GameSession.Instance?.Resume();
        }

        private void Quit()
        {
            if (settings.IsOpen) settings.Close();
            paused = false;
            panel.Close();
            var s = GameSession.Instance;
            if (s == null) return;
            s.Resume();
            s.Quit();
        }

        private void Update()
        {
            var s = GameSession.Instance;
            pauseButton.Interactable = s != null && s.IsPlaying && (!s.IsPaused || paused);
            pauseButton.gameObject.SetActive(s != null && s.IsPlaying);

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // 안드로이드 뒤로 버튼: 설정(크레딧)부터 닫고, 일시정지 창이면 계속하기, 게임 중이면 일시정지
                if (settings.Back()) { }
                else if (paused) Close();
                else Open();
            }
        }

        // 홈 버튼 등으로 앱이 뒤로 가면 자동으로 멈춘다
        private void OnApplicationPause(bool pause)
        {
            if (pause) Open();
        }
    }
}
