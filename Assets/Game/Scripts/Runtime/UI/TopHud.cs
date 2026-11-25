using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 위쪽 표시: 왼쪽에 골드(동전 + 숫자), 오른쪽에 웨이브 번호. 지구 체력 바 바로 아래.
    /// 골드가 늘면 동전 아이콘이 통 튄다.
    /// </summary>
    public sealed class TopHud : MonoBehaviour
    {
        [SerializeField] private WaveDirector waves;
        [Tooltip("플레이 영역 위쪽 끝에서 줄 중심까지(기준 픽셀)")]
        [SerializeField] private float rowOffsetPx = 16f;
        [Tooltip("좌우 끝에서 띄우는 거리(기준 픽셀)")]
        [SerializeField] private float sidePaddingPx = 6f;

        private PixelNumber goldNumber;
        private PixelNumber waveNumber;
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

            waveNumber = new GameObject("Wave").AddComponent<PixelNumber>();
            waveNumber.transform.SetParent(transform, false);
            waveNumber.transform.position = new Vector3(PlayArea.Right - pad, y, 0f);
            waveNumber.Configure(PixelNumber.Align.Right, new Color(0.75f, 0.85f, 1f));
            waveNumber.SetValue(0);

            var session = GameSession.Instance;
            if (session != null) session.Stats.GoldChanged += OnGoldChanged;
            if (waves != null) waves.WaveStarted += OnWaveStarted;
        }

        private void OnDestroy()
        {
            var session = GameSession.Instance;
            if (session != null) session.Stats.GoldChanged -= OnGoldChanged;
            if (waves != null) waves.WaveStarted -= OnWaveStarted;
        }

        private void OnGoldChanged(int total)
        {
            goldNumber.SetValue(total);
            coinBounce.Punch(0.35f);
        }

        private void OnWaveStarted(int wave) => waveNumber.SetValue(wave);
    }
}
