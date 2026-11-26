using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 타이틀: 짧은 로고 연출(별 배경, UFO가 통통 튀어 들어오고 게임 제목) → "화면을 터치하세요".
    /// 연출 중에 탭하면 바로 끝 장면으로, 그다음 탭하면 로비로. (아트가 나오면 그림만 바꾼다)
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        private const float HopsEnd = 1.6f;
        private const float TitleEnd = 2.1f;

        private Transform ufo;
        private SquashStretch ufoSquash;
        private TextMeshPro title;
        private TextMeshPro subtitle;
        private TextMeshPro touch;
        private float startedAt;
        private bool skipped;
        private int lastHop = -1;
        private TapInput input;
        private ConfirmPanel quitConfirm;
        private NicknamePanel nicknamePanel;
        private bool leaving;

        private GameConfig Config => PlayArea.Config;
        private float U(float px) => Config.PixelsToUnits(px);

        private void Start()
        {
            var a = GameAssets.Instance;
            StarField.Create(transform, 40);

            EarthGlobe.CreateHorizon("Earth", transform, SortOrder.Earth, PlayArea.Bottom);

            var ufoRoot = new GameObject("UFO").transform;
            ufoRoot.SetParent(transform, false);
            var ufoSprite = a.CreateSprite("Visual", a.ufoSprite, SortOrder.Ufo, ufoRoot);
            ufoSprite.transform.localScale = Vector3.one * 2f;
            ufoSquash = ufoSprite.gameObject.AddComponent<SquashStretch>();
            ufo = ufoRoot;

            title = GameText.Create("Title", transform, GameText.Style.Title, SortOrder.Hud + 5, TextAlignmentOptions.Center, 0f, false, 2);
            GameText.SetKey(title, "game.title");
            title.color = new Color(0.65f, 0.95f, 1f);
            title.transform.position = new Vector3(0f, U(70f), 0f);

            subtitle = GameText.Create("Subtitle", transform, GameText.Style.Body, SortOrder.Hud + 5);
            GameText.SetKey(subtitle, "game.subtitle");
            subtitle.color = new Color(1f, 0.85f, 0.45f);
            subtitle.transform.position = new Vector3(0f, U(46f), 0f);

            touch = GameText.Create("Touch", transform, GameText.Style.Body, SortOrder.Hud + 5);
            GameText.SetKey(touch, "ui.touch_to_start");
            touch.transform.position = new Vector3(0f, U(-70f), 0f);

            var version = GameText.Create("Version", transform, GameText.Style.Body, SortOrder.Hud + 5, TextAlignmentOptions.MidlineRight, 170f);
            GameText.SetPlain(version, "v" + Application.version + " demo");
            version.color = new Color(1f, 1f, 1f, 0.4f);
            version.transform.position = new Vector3(0f, PlayArea.Bottom + U(6f), 0f);

            quitConfirm = ConfirmPanel.Create(transform, SortOrder.Overlay);
            nicknamePanel = NicknamePanel.Create(transform, SortOrder.Overlay + 20);

            input = TapInput.Main;
            if (input != null) input.Released += OnReleased;
            startedAt = Time.time;
            Apply(0f);
        }

        private void OnDestroy()
        {
            if (input != null) input.Released -= OnReleased;
        }

        private void OnReleased(Vector2 pos, float held)
        {
            if (leaving) return;
            if (quitConfirm != null && quitConfirm.IsOpen) return; // 확인 창의 버튼이 처리
            if (nicknamePanel != null && nicknamePanel.IsOpen) return; // 닉네임 창의 버튼이 처리
            if (!skipped && Time.time - startedAt < TitleEnd)
            {
                skipped = true; // 연출 건너뛰기
                return;
            }
            Sound.Play(Sfx.Start);
            // 처음 켰으면 닉네임부터 정한다(취소 없음)
            if (!Nickname.IsSet) { nicknamePanel.Ask(false, _ => GoLobby()); return; }
            GoLobby();
        }

        private void GoLobby()
        {
            if (leaving) return;
            leaving = true;
            SceneFlow.GoLobby();
        }

        private void Update()
        {
            float t = skipped ? TitleEnd + (Time.time - startedAt) : Time.time - startedAt;
            Apply(t);

            // 안드로이드 뒤로 버튼(= Esc): "게임을 종료할까요?"
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (nicknamePanel.IsOpen) { /* 처음 닉네임은 꼭 정해야 해서 닫지 않는다 */ }
                else if (quitConfirm.IsOpen) quitConfirm.Close();
                else quitConfirm.Ask("ui.quit_confirm", AppQuit.Now);
            }
        }

        private void Apply(float t)
        {
            // 1) UFO가 왼쪽 밖에서 세 번 통통 튀어 가운데로
            float hopsK = Mathf.Clamp01(t / HopsEnd);
            const int hops = 3;
            float hopPos = hopsK * hops;
            int hopIndex = Mathf.Min(hops - 1, Mathf.FloorToInt(hopPos));
            float local = hopPos - hopIndex;
            float x = Mathf.Lerp(PlayArea.Left - U(20f), 0f, hopsK);
            float height = U(26f - hopIndex * 6f);
            float y = U(-10f) + (hopsK < 1f ? Mathf.Sin(local * Mathf.PI) * height : 0f);
            ufo.position = new Vector3(x, y + Mathf.Sin(Time.time * 2f) * U(1.5f) * (hopsK >= 1f ? 1f : 0f), 0f);
            if (hopsK < 1f && hopIndex != lastHop)
            {
                if (lastHop >= 0) ufoSquash.Punch(0.3f); // 착지할 때 납작
                lastHop = hopIndex;
            }
            else if (hopsK >= 1f && lastHop != hops)
            {
                ufoSquash.Punch(0.35f);
                lastHop = hops;
            }

            // 2) 제목이 통 튀어나옴
            float titleK = Mathf.Clamp01((t - HopsEnd) / (TitleEnd - HopsEnd));
            title.gameObject.SetActive(titleK > 0f);
            subtitle.gameObject.SetActive(titleK >= 1f);
            if (titleK > 0f)
            {
                float s = titleK < 1f ? Mathf.Lerp(0.3f, 1.15f, titleK) : 1f;
                title.transform.localScale = Vector3.one * 2f * s;
            }

            // 3) 터치하세요 깜빡임
            bool ready = t >= TitleEnd;
            touch.gameObject.SetActive(ready);
            if (ready) touch.alpha = 0.35f + 0.65f * Mathf.PingPong(Time.time * 1.2f, 1f);
        }
    }

    /// <summary>반짝이는 별 배경(타이틀·로비·로딩·결과 공용).</summary>
    public static class StarField
    {
        public static void Create(Transform parent, int count)
        {
            var a = GameAssets.Instance;
            var root = new GameObject("Stars").transform;
            root.SetParent(parent, false);
            var rng = new System.Random(7);
            for (int i = 0; i < count; i++)
            {
                var sr = a.CreateSprite("Star", a.starSprite != null ? a.starSprite : a.pixelSprite, SortOrder.Background + 1, root);
                float x = Mathf.Lerp(PlayArea.Left, PlayArea.Right, (float)rng.NextDouble());
                float y = Mathf.Lerp(PlayArea.Bottom, PlayArea.Top, (float)rng.NextDouble());
                sr.transform.position = new Vector3(x, y, 0f);
                if (a.starSprite == null) sr.transform.localScale = Vector3.one * 0.25f;
                var tw = sr.gameObject.AddComponent<Twinkle>();
                tw.phase = (float)rng.NextDouble() * 10f;
                tw.speed = 0.5f + (float)rng.NextDouble() * 1.5f;
            }
        }

        private sealed class Twinkle : MonoBehaviour
        {
            public float phase;
            public float speed;
            private SpriteRenderer sr;

            private void Awake() => sr = GetComponent<SpriteRenderer>();

            private void Update()
            {
                var c = sr.color;
                c.a = 0.3f + 0.7f * Mathf.PingPong(Time.unscaledTime * speed + phase, 1f);
                sr.color = c;
            }
        }
    }
}
