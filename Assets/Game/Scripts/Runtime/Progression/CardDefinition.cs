using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 판 안 강화 카드 한 장의 데이터. 레벨업 때 3장 중 1장을 고른다. 판이 끝나면 초기화.
    /// 같은 카드를 여러 번 고르면 쌓인다(최대 단계까지).
    /// </summary>
    [CreateAssetMenu(fileName = "Card", menuName = "Bouncy UFO/Card Definition")]
    public sealed class CardDefinition : ScriptableObject
    {
        public enum Effect
        {
            // 숫자 순서는 카드 데이터에 저장돼 있으니 바꾸지 않는다
            /// <summary>돌진 피해 +value</summary>
            DashPower = 0,
            /// <summary>게이지 차는 시간 × (1 - value). 0.15 = 15% 빨라짐</summary>
            DashRange = 1, // 옛 ChargeSpeed 자리(숫자 그대로)
            /// <summary>몸통 판정 크기 +value (0.2 = 20% 커짐)</summary>
            BodySize = 2,
            /// <summary>연쇄 피해(튕겨 나간 몬스터가 다른 몬스터에 주는 피해) +value</summary>
            ChainDamage = 3,
            /// <summary>지구 체력 +value 회복</summary>
            HealEarth,
        }

        [Tooltip("이름 번역 키 (strings.csv)")]
        public string nameKey;
        [Tooltip("설명 번역 키 (strings.csv)")]
        public string descKey;
        public Sprite icon;
        [Tooltip("카드 테두리 색")]
        public Color color = Color.white;

        public Effect effect;
        public float value = 1f;
        [Tooltip("이 판에서 고를 수 있는 최대 횟수. 0이면 제한 없음")]
        public int maxStacks = 5;
        [Tooltip("다른 카드와 비교한 등장 비중")]
        public float weight = 1f;
    }
}
