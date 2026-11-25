using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 몬스터 한 종류의 데이터. 수치는 여기서 조정한다(코드 수정 없이 밸런싱).
    /// 속도는 "플레이 영역 높이/초"라 모든 기기에서 똑같이 내려온다.
    /// </summary>
    [CreateAssetMenu(fileName = "Monster", menuName = "Bouncy UFO/Monster Definition")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        public enum MovePattern
        {
            /// <summary>곧게 내려온다(좌우로 살짝 흔들 수 있음).</summary>
            Straight,
            /// <summary>천천히 내려오다 멈칫한 뒤 빠르게 돌진한다.</summary>
            Charge,
        }

        [Tooltip("화면에 보일 이름의 번역 키(나중에 3개 언어)")]
        public string nameKey = "monster.unknown";
        public Sprite sprite;
        [Tooltip("그림 크기 배율")]
        public float scale = 1f;
        [Tooltip("판정 반지름(기준 픽셀, 배율 적용 전)")]
        public float hitRadiusPx = 7f;

        [Header("능력치")]
        public int maxHp = 2;
        [Tooltip("지구에 닿으면 깎는 지구 체력")]
        public int earthDamage = 3;
        [Tooltip("내려오는 속도(플레이 영역 높이/초)")]
        public float speed = 0.1f;

        [Header("움직임")]
        public MovePattern pattern = MovePattern.Straight;
        [Tooltip("좌우 흔들림 폭(기준 픽셀). 0이면 똑바로")]
        public float wobbleAmplitudePx = 0f;
        [Tooltip("좌우 흔들림 빠르기(초당 횟수)")]
        public float wobbleFrequency = 1f;
        [Tooltip("[돌진] 위에서 이 비율만큼 내려오면 멈칫한다(0~1)")]
        [Range(0f, 1f)] public float chargeTriggerFraction = 0.3f;
        [Tooltip("[돌진] 멈칫하는 시간(초)")]
        public float chargeWindup = 0.45f;
        [Tooltip("[돌진] 돌진 속도(플레이 영역 높이/초)")]
        public float chargeSpeed = 0.55f;

        [Header("등장")]
        [Tooltip("한 번에 함께 나오는 수")]
        public int groupSize = 1;
        [Tooltip("무리가 퍼지는 폭(기준 픽셀)")]
        public float groupSpreadPx = 24f;
        [Tooltip("다른 몬스터와 비교한 등장 확률 비중")]
        public float spawnWeight = 1f;
        [Tooltip("판 시작 후 이 시간(초)부터 등장")]
        public float unlockTime = 0f;

        [Header("보상 (4번 묶음에서 사용)")]
        public int exp = 1;
        public int score = 10;
    }
}
