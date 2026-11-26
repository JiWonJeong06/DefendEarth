using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 콤보 글자 "3 CHAIN!": 한 번 날아가는 동안(연쇄 포함) 2마리 이상 맞히면 화면 위쪽 가운데에 뜬다.
    /// 맞힐 때마다 통 커졌다 돌아오고, 5 이상이면 글자가 2배. 잠시 맞히지 않으면 깜빡이다 사라진다.
    /// 게임 씬에서 처음 필요할 때 스스로 생긴다.
    /// </summary>
    public sealed class ComboHud : MonoBehaviour
    {
        private const float ShowSeconds = 0.9f;
        private const float PopSeconds = 0.08f;
        private const float YPx = 56f;

        private static ComboHud instance;
        private TextMeshPro text;
        private TextMeshPro shadow;
        private float lastAt;
        private int baseScale = 1;

        public static void Show(int combo)
        {
            if (instance == null)
            {
                if (GameSession.Instance == null) return; // 게임 씬에서만
                instance = new GameObject("ComboHud").AddComponent<ComboHud>();
                instance.Build();
            }
            instance.Set(combo);
        }

        private void Build()
        {
            float y = PlayArea.Config.PixelsToUnits(YPx);
            float one = PlayArea.Config.PixelsToUnits(1f);
            shadow = GameText.Create("Shadow", transform, GameText.Style.Title, SortOrder.Hud + 7);
            shadow.color = new Color(0f, 0f, 0.05f, 0.85f);
            shadow.transform.localPosition = new Vector3(one, -one, 0f);
            text = GameText.Create("Combo", transform, GameText.Style.Title, SortOrder.Hud + 8);
            transform.position = new Vector3(0f, y, 0f);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Set(int combo)
        {
            string s = combo + " CHAIN!";
            GameText.SetPlain(text, s);
            GameText.SetPlain(shadow, s);
            // 많이 이을수록 노랑 → 주황 → 빨강
            text.color = combo >= 8 ? new Color(1f, 0.35f, 0.35f) : combo >= 5 ? new Color(1f, 0.6f, 0.25f) : new Color(1f, 0.92f, 0.4f);
            baseScale = combo >= 5 ? 2 : 1;
            lastAt = Time.unscaledTime;
            gameObject.SetActive(true);
            Apply();
        }

        private void Update() => Apply();

        private void Apply()
        {
            float t = Time.unscaledTime - lastAt;
            if (t >= ShowSeconds) { gameObject.SetActive(false); return; }
            // 맞힌 순간 한 단계 크게(정수 배율만)
            transform.localScale = Vector3.one * (t < PopSeconds ? baseScale + 1 : baseScale);
            bool off = t > ShowSeconds * 0.7f && Mathf.FloorToInt(t * 16f) % 2 == 0;
            text.alpha = off ? 0f : 1f;
            shadow.alpha = off ? 0f : 0.85f;
        }
    }
}
