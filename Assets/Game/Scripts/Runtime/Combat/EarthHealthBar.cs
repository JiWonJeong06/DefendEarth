using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 게임 화면 맨 아래 플레이어 줄 (11/27):
    /// 제일 왼쪽 아래 프로필(로비에서 꾸민 조종사 얼굴) → 그 오른쪽에 지구 체력 바(PixelBar).
    /// 체력 숫자("75/100")는 체력 바 위에 겹쳐서, 체력 바 왼쪽 위에는 플레이 레벨과 닉네임("Lv.3 홍길동").
    /// 체력이 깎이면 체력 바가 잠깐 좌우로 떨린다.
    /// 위치는 모두 기준 픽셀(플레이 영역 가운데 x = 0, 아래 끝에서 y).
    /// </summary>
    public sealed class EarthHealthBar : MonoBehaviour
    {
        // 프로필 칸
        private const float ProfileCenterX = -74f;
        private const float ProfileCenterY = 20f;
        private const float ProfileWidth = 28f;
        private const float ProfileHeight = 40f;   // 캐릭터 24x36 + 테두리
        // 체력 바(프로필 오른쪽, 오른쪽 끝에서 2픽셀 띄움)
        private const float BarLeft = -56f;
        private const float BarRight = 88f;
        private const float BarCenterY = 9f;
        // 레벨·닉네임 줄
        private const float NameY = 25f;

        [SerializeField] private Earth earth;

        private PixelBar bar;
        private Transform barRoot;
        private Vector3 barHome;
        private TextMeshPro number;
        private TextMeshPro numberShadow;
        private float shakeTimer;
        private int lastHp = -1;

        private GameConfig Config => PlayArea.Config;
        private float U(float px) => Config.PixelsToUnits(px);
        private Vector3 At(float x, float yFromBottom) => new Vector3(U(x), PlayArea.Bottom + U(yFromBottom), 0f);

        private void Start()
        {
            transform.position = Vector3.zero;
            BuildProfile();

            barRoot = new GameObject("HpBar").transform;
            barRoot.SetParent(transform, false);
            barHome = At((BarLeft + BarRight) * 0.5f, BarCenterY);
            barRoot.position = barHome;
            bar = PixelBar.Create("Bar", barRoot, SortOrder.Hud, BarRight - BarLeft);

            // 체력 숫자: 체력 바(빨간 칸) 위에 겹쳐서, 1픽셀 그림자
            float one = U(1f);
            numberShadow = GameText.Create("HpShadow", barRoot, GameText.Style.Body, SortOrder.Hud + 4);
            numberShadow.color = new Color(0f, 0f, 0.05f, 0.9f);
            numberShadow.transform.localPosition = new Vector3(one, -one, 0f);
            number = GameText.Create("Hp", barRoot, GameText.Style.Body, SortOrder.Hud + 5);
            number.transform.localPosition = Vector3.zero;

            BuildNameLine();

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

        private void BuildProfile()
        {
            var a = GameAssets.Instance;
            float px = a.pixelSprite.bounds.size.x;
            var root = new GameObject("Profile").transform;
            root.SetParent(transform, false);
            root.position = At(ProfileCenterX, ProfileCenterY);

            var border = a.CreateSprite("Border", a.pixelSprite, SortOrder.Hud, root);
            border.color = new Color(0.97f, 0.97f, 0.97f); // 로딩바 틀처럼 흰 테두리
            border.transform.localScale = new Vector3(U(ProfileWidth) / px, U(ProfileHeight) / px, 1f);
            var inner = a.CreateSprite("Inner", a.pixelSprite, SortOrder.Hud + 1, root);
            inner.color = new Color(0.8f, 0.86f, 0.99f);
            inner.transform.localScale = new Vector3(U(ProfileWidth - 2f) / px, U(ProfileHeight - 2f) / px, 1f);
            var back = a.CreateSprite("Back", a.pixelSprite, SortOrder.Hud + 2, root);
            back.color = new Color(0.13f, 0.13f, 0.2f);
            back.transform.localScale = new Vector3(U(ProfileWidth - 4f) / px, U(ProfileHeight - 4f) / px, 1f);

            PilotView.Create(root, Vector3.zero, 1, SortOrder.Hud + 3);
        }

        private void BuildNameLine()
        {
            var level = GameText.Create("Level", transform, GameText.Style.Body, SortOrder.Hud + 5, TextAlignmentOptions.MidlineLeft, 40f);
            level.color = new Color(1f, 0.85f, 0.35f);
            int lv = Account.FromTotal(SaveSystem.Data.accountXp).level;
            GameText.SetPlain(level, "Lv." + lv);
            level.ForceMeshUpdate();
            // 칸 너비 40픽셀, 왼쪽 맞춤 → 칸 가운데를 바 왼쪽 끝 + 20에
            level.transform.position = At(BarLeft + 20f, NameY);

            float levelWidthPx = Mathf.Ceil(level.preferredWidth * Config.pixelsPerUnit);
            var name = GameText.Create("Nickname", transform, GameText.Style.Body, SortOrder.Hud + 5, TextAlignmentOptions.MidlineLeft, 100f);
            GameText.SetPlain(name, Nickname.IsSet ? Nickname.Current : "-");
            name.transform.position = At(BarLeft + levelWidthPx + 4f + 50f, NameY);
        }

        private void OnHpChanged(int hp, int max)
        {
            bar.SetValue(max > 0 ? (float)hp / max : 0f);
            string text = hp + "/" + max;
            GameText.SetPlain(number, text);
            GameText.SetPlain(numberShadow, text);
            float k = max > 0 ? (float)hp / max : 0f;
            number.color = k > 0.5f ? Color.white : k > 0.25f ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.5f, 0.5f);
            if (lastHp >= 0 && hp < lastHp) shakeTimer = 0.2f;
            lastHp = hp;
        }

        private void Update()
        {
            if (shakeTimer <= 0f) return;
            shakeTimer -= Time.unscaledDeltaTime;
            float dx = shakeTimer > 0f ? (Mathf.FloorToInt(shakeTimer * 40f) % 2 == 0 ? 1f : -1f) : 0f;
            barRoot.position = barHome + new Vector3(U(dx), 0f, 0f);
        }
    }
}
