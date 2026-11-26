using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 몬스터(운석·보스 포함) 한 종류의 데이터. 수치는 여기서 조정한다(코드 수정 없이 밸런싱).
    /// 속도는 "플레이 영역 높이/초"라 모든 기기에서 똑같이 내려온다.
    /// </summary>
    [CreateAssetMenu(fileName = "Monster", menuName = "Bouncy UFO/Monster Definition")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        public enum Kind
        {
            /// <summary>일반 몬스터: 지구에 닿으면 피해, 잡으면 경험치·점수.</summary>
            Monster,
            /// <summary>재화 운석: 지나가도 지구 피해 없음, 부수면 골드.</summary>
            Meteor,
            /// <summary>보스: 일정 간격마다 등장, 잡으면 단계 보상 골드.</summary>
            Boss,
        }

        public enum MovePattern
        {
            /// <summary>곧게 내려온다(좌우로 살짝 흔들 수 있음).</summary>
            Straight,
            /// <summary>천천히 내려오다 멈칫한 뒤 빠르게 돌진한다.</summary>
            Charge,
            /// <summary>위쪽까지 내려와 좌우로 오가며 아주 천천히 내려온다(보스).</summary>
            Patrol,
        }

        public Kind kind = Kind.Monster;
        [Tooltip("화면에 보일 이름의 번역 키(나중에 3개 언어)")]
        public string nameKey = "monster.unknown";
        public Sprite sprite;
        [Tooltip("같은 몬스터의 다른 색 그림들. 나올 때마다 이 중 하나를 고른다(비어 있으면 sprite)")]
        public Sprite[] colorVariants = new Sprite[0];
        [Tooltip("그림 크기 배율. 도트가 깨지지 않게 1, 2 같은 정수로")]
        public float scale = 1f;
        [Tooltip("판정 반지름(기준 픽셀, 배율 적용 전)")]
        public float hitRadiusPx = 7f;

        [Header("능력치 (웨이브가 오를수록 체력·속도에 배율이 붙는다)")]
        public int maxHp = 2;
        [Tooltip("지구에 닿으면 깎는 지구 체력 (운석은 0)")]
        public int earthDamage = 3;
        [Tooltip("내려오는 속도(플레이 영역 높이/초)")]
        public float speed = 0.1f;
        [Tooltip("UFO에 받혔을 때 튕겨 나가는 정도(1 = 보통, 0.25 = 조금만 밀림). 보스·덩치는 작게")]
        public float knockbackScale = 1f;

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
        [Tooltip("[보스] 위에서 이 비율만큼 내려오면 좌우로 오가기 시작")]
        [Range(0f, 1f)] public float patrolStartFraction = 0.22f;
        [Tooltip("[보스] 좌우로 오가는 속도(플레이 영역 너비/초)")]
        public float patrolSpeed = 0.18f;
        [Tooltip("[보스] 오가는 동안 내려오는 속도(플레이 영역 높이/초)")]
        public float patrolDescendSpeed = 0.006f;

        [Header("등장")]
        [Tooltip("한 번에 함께 나오는 수")]
        public int groupSize = 1;
        [Tooltip("무리가 퍼지는 폭(기준 픽셀)")]
        public float groupSpreadPx = 24f;
        [Tooltip("다른 몬스터와 비교한 등장 확률 비중")]
        public float spawnWeight = 1f;
        [Tooltip("이 웨이브부터 등장(1부터)")]
        public int unlockWave = 1;

        [Header("보상")]
        public int exp = 1;
        public int score = 10;
        [Tooltip("[운석] 부수면 주는 골드")]
        public int gold = 0;
        [Tooltip("[다이아 운석] 부수면 주는 다이아(상점 재화)")]
        public int diamonds = 0;

        /// <summary>이번에 나올 그림: 색 그림 중 하나를 무작위로(없으면 기본 그림).</summary>
        public Sprite PickSprite()
        {
            if (colorVariants == null || colorVariants.Length == 0) return sprite;
            var pick = colorVariants[UnityEngine.Random.Range(0, colorVariants.Length)];
            return pick != null ? pick : sprite;
        }
    }
}
