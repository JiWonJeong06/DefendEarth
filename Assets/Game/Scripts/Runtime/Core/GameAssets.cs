using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 코드에서 만드는 오브젝트(탄·몬스터·효과)가 쓰는 그림과 재질 모음.
    /// 장면 만들기 메뉴가 연결해 준다.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public sealed class GameAssets : MonoBehaviour
    {
        public static GameAssets Instance { get; private set; }

        public Sprite sparkSprite;
        public Sprite pixelSprite;
        [Tooltip("빛 영향 없는 기본 스프라이트 재질")]
        public Material spriteMaterial;
        [Tooltip("맞으면 하얗게 번쩍이는 재질(몬스터용)")]
        public Material flashMaterial;
        [Tooltip("일반 몬스터들")]
        public MonsterDefinition[] monsters;
        public MonsterDefinition boss;
        public MonsterDefinition meteor;
        [Tooltip("다이아 운석(부수면 다이아, 지구 피해 없음)")]
        public MonsterDefinition diamondMeteor;
        [Tooltip("다이아 아이콘(위쪽 표시·떨어지는 보석·상점 가격)")]
        public Sprite diamondSprite;
        [Tooltip("웨이브가 끝나면 오는 상점 우주선")]
        public Sprite shopShipSprite;
        public Sprite coinSprite;
        [Tooltip("숫자 0~9 도트 그림(점수·골드 표시)")]
        public Sprite[] digitSprites = new Sprite[10];

        [Tooltip("판 안 강화 카드들")]
        public CardDefinition[] cards;
        [Tooltip("번역표 (key,ko,en,ja)")]
        public TextAsset strings;

        [Header("글자 (갈무리 폰트)")]
        [Tooltip("Galmuri11 (12픽셀): 제목·숫자")]
        public Font titleFont;
        [Tooltip("Galmuri9 (10픽셀): 설명")]
        public Font bodyFont;
        [Tooltip("TextMeshPro/Mobile/Bitmap 셰이더 (빌드에 꼭 들어가게 여기서 참조)")]
        public Shader textShader;

        [Header("타이틀·로비용 (임시 그림)")]
        public Sprite ufoSprite;
        [Tooltip("UFO 종류별 그림(임시: 빨강·초록·파랑 UFO). UfoType.sprite 번호")]
        public Sprite[] ufoSprites = new Sprite[0];
        public Sprite earthSprite;
        public Sprite starSprite;

        [Header("로딩바 (9칸 나누기 그림)")]
        [Tooltip("틀(UI_3)")]
        public Sprite loadingBarFrame;
        [Tooltip("검정 안 채움(UI_5)")]
        public Sprite loadingBarEmpty;
        [Tooltip("빨강 채움(UI_4)")]
        public Sprite loadingBarFill;

        [Header("캐릭터 (SD 전신, 5부품, 모두 24x36 같은 캔버스. 임시 그림)")]
        [Tooltip("몸통(흰/회색, 피부색을 입힌다)")]
        public Sprite charBody;
        [Tooltip("뒷머리(머리 모양마다, 앞머리와 같은 순서)")]
        public Sprite[] charHairBack = new Sprite[0];
        [Tooltip("앞머리(머리 모양마다)")]
        public Sprite[] charHairFront = new Sprite[0];
        [Tooltip("눈 모양(흰 칸에 눈 색을 입힌다)")]
        public Sprite[] charEyes = new Sprite[0];
        [Tooltip("옷(그림 색 그대로)")]
        public Sprite[] charOutfits = new Sprite[0];
        [Tooltip("머리 장식(없음은 따로, 여기엔 1번부터)")]
        public Sprite[] charHeadwear = new Sprite[0];
        [Tooltip("머리 장식마다 머리색을 입히는지(동물 귀 등)")]
        public bool[] charHeadwearUsesHairColor = new bool[0];

        [Header("크레딧")]
        [Tooltip("갈무리 폰트 라이선스 원문 (OFL은 폰트와 함께 라이선스를 배포해야 한다)")]
        public TextAsset fontLicense;

        private void Awake()
        {
            Instance = this;
            Localization.Load(strings);
            Localization.SetLanguage(SaveSystem.Data.language);
        }

        private void LateUpdate() => GameText.KeepCrisp();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public SpriteRenderer CreateSprite(string name, Sprite sprite, int order, Transform parent = null, Material material = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            var mat = material != null ? material : spriteMaterial;
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }
    }

    /// <summary>그리는 순서(클수록 앞).</summary>
    public static class SortOrder
    {
        public const int Background = -100;
        public const int Earth = -50;
        /// <summary>몬스터는 한 마리마다 다른 순서(Monster ~ Monster+MonsterSlots-1)를 받아 겹쳐도 깜빡이지 않는다.</summary>
        public const int Monster = 0;
        public const int MonsterSlots = 200;
        /// <summary>몬스터 머리 위 작은 체력 바(모든 몬스터 위, 보스 아래).</summary>
        public const int MonsterHpBar = 250;
        public const int Boss = 300;
        public const int Ufo = 400;
        public const int Effect = 500;
        public const int Hud = 1000;
        public const int Overlay = 2000;
    }
}
