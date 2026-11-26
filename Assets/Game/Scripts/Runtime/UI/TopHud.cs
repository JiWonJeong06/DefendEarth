using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 위쪽 표시: 왼쪽에 골드(동전 + 숫자), 가운데 점수, 오른쪽(일시정지 버튼 왼쪽)에 "WAVE 3". 튜토리얼에서는 "TUTORIAL".
    /// 골드가 늘면 동전 아이콘이 통 튄다. 새 웨이브가 시작하면 화면 가운데에 크게 "WAVE 3"이 잠깐 뜬다.
    /// </summary>
    public sealed class TopHud : MonoBehaviour
    {
        [SerializeField] private WaveDirector waves;
        [Tooltip("플레이 영역 위쪽 끝에서 줄 중심까지(기준 픽셀)")]
        [SerializeField] private float rowOffsetPx = 8f;
        [Tooltip("좌우 끝에서 띄우는 거리(기준 픽셀)")]
        [SerializeField] private float sidePaddingPx = 6f;

        private const float BannerSeconds = 1.4f;

        private PixelNumber goldNumber;
        private PixelNumber diamondNumber;
        private SquashStretch gemBounce;
        private TextMeshPro waveText;
        private TextMeshPro banner;
        private float bannerTimer;
        private PixelNumber scoreNumber;
        private SquashStretch coinBounce;

        private void Start()
        {
            var assets = GameAssets.Instance;
            var config = PlayArea.Config;
            float y = PlayArea.Top - config.PixelsToUnits(rowOffsetPx);
            float pad = config.PixelsToUnits(sidePaddingPx);

            var coin = assets.CreateSprite("CoinIcon", assets.coinSprite, SortOrder.Hud + 5, transform);
            coin.transform.position = new Vector3(PlayArea.Left + pad + config.PixelsToUnits(3f), y, 0f);
            coinBounce = coin.gameObject.AddComponent<SquashStretch>();

            goldNumber = new GameObject("Gold").AddComponent<PixelNumber>();
            goldNumber.transform.SetParent(transform, false);
            goldNumber.transform.position = new Vector3(PlayArea.Left + pad + config.PixelsToUnits(8f), y, 0f);
            goldNumber.Configure(PixelNumber.Align.Left, new Color(1f, 0.88f, 0.4f));
            goldNumber.SetValue(0);

            // 2줄: 다이아(상점 재화, 이 판에서만)
            float y2 = y - config.PixelsToUnits(10f);
            var gem = assets.CreateSprite("DiamondIcon", assets.diamondSprite, SortOrder.Hud + 5, transform);
            gem.transform.position = new Vector3(PlayArea.Left + pad + config.PixelsToUnits(3f), y2, 0f);
            gemBounce = gem.gameObject.AddComponent<SquashStretch>();
            diamondNumber = new GameObject("Diamonds").AddComponent<PixelNumber>();
            diamondNumber.transform.SetParent(transform, false);
            diamondNumber.transform.position = new Vector3(PlayArea.Left + pad + config.PixelsToUnits(8f), y2, 0f);
            diamondNumber.Configure(PixelNumber.Align.Left, new Color(0.8f, 0.95f, 1f));
            diamondNumber.SetValue(0);

            waveText = GameText.Create("Wave", transform, GameText.Style.Body, SortOrder.Hud + 5, TextAlignmentOptions.MidlineRight, 60f);
            waveText.color = new Color(0.75f, 0.85f, 1f);
            // 오른쪽 끝은 일시정지 버튼(오른쪽 끝에서 16픽셀) 왼쪽에 맞춘다
            waveText.transform.position = new Vector3(PlayArea.Right - config.PixelsToUnits(20f + 30f), y, 0f);
            GameText.SetPlain(waveText, SceneFlow.IsTutorial ? "TUTORIAL" : "WAVE 0");

            banner = GameText.Create("WaveBanner", transform, GameText.Style.Title, SortOrder.Hud + 8, TextAlignmentOptions.Center, 0f, false, 2);
            banner.color = new Color(1f, 0.9f, 0.45f);
            banner.transform.position = new Vector3(0f, config.PixelsToUnits(40f), 0f);
            banner.gameObject.SetActive(false);

            scoreNumber = new GameObject("Score").AddComponent<PixelNumber>();
            scoreNumber.transform.SetParent(transform, false);
            scoreNumber.transform.position = new Vector3(0f, y, 0f);
            scoreNumber.Configure(PixelNumber.Align.Center, Color.white);
            scoreNumber.SetValue(0);

            var session = GameSession.Instance;
            if (session != null)
            {
                session.Stats.GoldChanged += OnGoldChanged;
                session.Stats.ScoreChanged += OnScoreChanged;
                session.Stats.DiamondsChanged += OnDiamondsChanged;
            }
            if (waves != null) waves.WaveStarted += OnWaveStarted;
        }

        private void OnDestroy()
        {
            var session = GameSession.Instance;
            if (session != null)
            {
                session.Stats.GoldChanged -= OnGoldChanged;
                session.Stats.ScoreChanged -= OnScoreChanged;
                session.Stats.DiamondsChanged -= OnDiamondsChanged;
            }
            if (waves != null) waves.WaveStarted -= OnWaveStarted;
        }

        private void OnGoldChanged(int total)
        {
            goldNumber.SetValue(total);
            coinBounce.Punch(0.35f);
        }

        private void OnDiamondsChanged(int total)
        {
            diamondNumber.SetValue(total);
            gemBounce.Punch(0.35f);
        }

        private void OnWaveStarted(int wave)
        {
            GameText.SetPlain(waveText, "WAVE " + wave);
            GameText.SetPlain(banner, "WAVE " + wave);
            banner.gameObject.SetActive(true);
            bannerTimer = BannerSeconds;
        }

        private void Update()
        {
            if (bannerTimer <= 0f) return;
            bannerTimer -= Time.deltaTime;
            if (bannerTimer <= 0f) { banner.gameObject.SetActive(false); return; }
            // 처음엔 통 튀어나오고, 끝에서 0.4초 동안 흐려진다
            float t = BannerSeconds - bannerTimer;
            banner.transform.localScale = Vector3.one * (t < 0.1f ? 3f : 2f); // 처음 잠깐 3배로 통, 그다음 2배(정수 배율만: 도트가 깨지지 않게)
            banner.alpha = Mathf.Clamp01(bannerTimer / 0.4f);
        }

        private void OnScoreChanged(int score) => scoreNumber.SetValue(score);
    }
}
