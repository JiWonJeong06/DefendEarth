using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 지구 체력 바. 화면 맨 아래(지구 위)에 로딩바와 같은 그림 막대(PixelBar)로 그리고,
    /// 바로 위에 "75/100"처럼 숫자로도 보여 준다. 체력이 깎이면 막대가 잠깐 좌우로 떨린다.
    /// </summary>
    public sealed class EarthHealthBar : MonoBehaviour
    {
        [SerializeField] private Earth earth;
        [Tooltip("바 전체 너비(기준 픽셀). 높이는 틀 그림 그대로(16픽셀)")]
        [SerializeField] private float widthPx = 130f;
        [Tooltip("플레이 영역 아래쪽 끝에서 바 중심까지(기준 픽셀)")]
        [SerializeField] private float bottomOffsetPx = 9f;
        [Tooltip("바 중심에서 숫자 중심까지 위로(기준 픽셀)")]
        [SerializeField] private float numberAbovePx = 13f;

        private PixelBar bar;
        private TextMeshPro number;
        private TextMeshPro numberShadow;
        private Vector3 home;
        private float shakeTimer;
        private int lastHp = -1;

        private void Start()
        {
            var config = PlayArea.Config;
            home = new Vector3(0f, PlayArea.Bottom + config.PixelsToUnits(bottomOffsetPx), 0f);
            transform.position = home;
            bar = PixelBar.Create("Bar", transform, SortOrder.Hud, widthPx);

            // 지구 그림 위라 잘 보이게 1픽셀 그림자를 깐다
            float up = config.PixelsToUnits(numberAbovePx);
            float one = config.PixelsToUnits(1f);
            numberShadow = GameText.Create("HpShadow", transform, GameText.Style.Body, SortOrder.Hud + 4);
            numberShadow.color = new Color(0f, 0f, 0.05f, 0.85f);
            numberShadow.transform.localPosition = new Vector3(one, up - one, 0f);
            number = GameText.Create("Hp", transform, GameText.Style.Body, SortOrder.Hud + 5);
            number.transform.localPosition = new Vector3(0f, up, 0f);

            if (earth != null)
            {
                earth.HpChanged += OnHpChanged;
                OnHpChanged(earth.Hp, earth.MaxHp);
            }
        }

        private void OnDestroy()
        {
            if (earth != null) earth.HpChanged -= OnHpChanged;
        }

        private void OnHpChanged(int hp, int max)
        {
            bar.SetValue(max > 0 ? (float)hp / max : 0f);
            string text = hp + "/" + max;
            GameText.SetPlain(number, text);
            GameText.SetPlain(numberShadow, text);
            float k = max > 0 ? (float)hp / max : 0f;
            number.color = k > 0.5f ? Color.white : k > 0.25f ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.4f, 0.4f);
            if (lastHp >= 0 && hp < lastHp) shakeTimer = 0.2f;
            lastHp = hp;
        }

        private void Update()
        {
            if (shakeTimer <= 0f) return;
            shakeTimer -= Time.unscaledDeltaTime;
            float dx = shakeTimer > 0f ? (Mathf.FloorToInt(shakeTimer * 40f) % 2 == 0 ? 1f : -1f) : 0f;
            transform.position = home + new Vector3(PlayArea.Config.PixelsToUnits(dx), 0f, 0f);
        }
    }
}
