using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 밸런싱 수치를 한곳에 모은 데이터. 코드에 숫자를 박지 않고 여기서 조정한다.
    /// 거리·크기는 "기준 픽셀"(플레이 영역 180×320 기준) 또는 "플레이 영역 너비 비율"로 적는다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Bouncy UFO/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("플레이 영역 (모든 기기에서 동일)")]
        [Tooltip("플레이 영역 크기(기준 픽셀). 9:16")]
        public Vector2Int playAreaPixels = new Vector2Int(180, 320);
        [Tooltip("모든 스프라이트의 Pixels Per Unit")]
        public int pixelsPerUnit = 16;

        [Header("UFO 위치·크기 (기준 픽셀)")]
        [Tooltip("플레이 영역 아래쪽 끝에서 UFO 중심까지 높이")]
        public float ufoHeightFromBottomPx = 56f;
        [Tooltip("UFO 몸체 반 너비. 화면 밖으로 나가지 않게 막는 데 쓴다")]
        public float ufoHalfWidthPx = 14f;

        [Header("조준·돌진(새총): 꾹 누른 채 뒤로 당긴 반대쪽으로 조준, 떼면 돌진. 게이지 없음")]
        [Tooltip("이만큼(기준 픽셀)보다 덜 당기고 떼면 돌진하지 않는다(취소)")]
        public float slingMinPullPx = 6f;
        [Tooltip("돌진 출발 속도(기준 픽셀/초). 클수록 멀리 날아간다. 로비 강화·카드로 배율이 붙는다")]
        public float dashSpeedPx = 460f;
        [Tooltip("중력(기준 픽셀/초²). 클수록 빨리 떨어진다")]
        public float dashGravityPx = 750f;
        [Tooltip("조준 각도의 최저치(수평에서 위로, 도). 이보다 아래로는 조준할 수 없다")]
        [Range(0f, 60f)] public float aimMinElevationDeg = 12f;
        [Tooltip("공중에서 손을 떼면 이 시간(초) 안에 착지할 때 바로 돌진")]
        public float dashBufferTime = 0.2f;
        [Tooltip("벽·천장에 부딪히면 남는 속도 비율")]
        [Range(0f, 1f)] public float wallBounce = 0.6f;
        [Tooltip("당기고 있는 동안 움츠리는 정도(모으는 느낌)")]
        public float squashOnCharge = 0.25f;
        [Tooltip("조준 점선 점 개수 / 점 사이 시간(초)")]
        public int aimDots = 12;
        public float aimDotStep = 0.04f;

        [Header("몸통 박치기")]
        [Tooltip("UFO 몸통 판정 반지름(기준 픽셀). 그림보다 살짝 크게")]
        public float ufoBodyRadiusPx = 11f;
        [Tooltip("돌진 피해(카드 '돌진 힘'으로 늘어남)")]
        public int dashDamage = 1;
        [Tooltip("튕겨 나가는 몬스터 속도(기준 픽셀/초)")]
        public float knockSpeedPx = 380f;
        [Tooltip("튕겨 나간 몬스터가 느려지는 정도(초당 감속 비율)")]
        public float knockDrag = 2.2f;
        [Tooltip("이 속도(기준 픽셀/초) 아래로 느려지면 다시 내려오기 시작")]
        public float knockStopSpeedPx = 40f;
        [Tooltip("튕겨 나간 몬스터가 다른 몬스터와 부딪힐 때 주는 기본 피해(연쇄)")]
        public int chainDamage = 1;
        [Tooltip("연쇄가 이어지는 최대 단계(1이면 튕긴 몬스터가 한 번만 다른 몬스터를 침)")]
        public int chainMaxDepth = 2;
        [Tooltip("연쇄로 맞은 몬스터가 받는 튕김 비율")]
        [Range(0f, 1f)] public float chainKnockRatio = 0.6f;

        [Header("통통 연출 (스쿼시·스트레치)")]
        [Tooltip("스프링 세기. 클수록 빨리 원래 모양으로 돌아온다")]
        public float squashStiffness = 260f;
        [Tooltip("스프링 감쇠. 작을수록 더 출렁인다")]
        public float squashDamping = 12f;
        [Tooltip("돌진할 때 늘어나는 정도")]
        public float squashOnDash = 0.3f;
        [Tooltip("착지할 때 납작하게 눌리는 정도")]
        public float squashOnLand = 0.3f;
        [Tooltip("몬스터를 들이받을 때 출렁이는 정도")]
        public float squashOnHit = 0.15f;

        [Header("지구")]
        public int earthMaxHp = 100;
        [Tooltip("지구 반지름(기준 픽셀). 화면보다 커서 윗부분만 보인다. 몬스터가 이 표면에 닿으면 지구 피해")]
        public float earthRadiusPx = 150f;
        [Tooltip("화면 아래 끝에서 지구 꼭대기까지 높이(기준 픽셀)")]
        public float earthVisiblePx = 44f;
        [Tooltip("지구가 한 바퀴 도는 시간(초)")]
        public float earthSpinSeconds = 90f;

        [Header("웨이브 (몬스터가 한 마리씩 내려온다)")]
        [Tooltip("웨이브 하나의 길이(초)")]
        public float waveDuration = 18f;
        [Tooltip("웨이브 사이 쉬는 시간(초)")]
        public float waveBreak = 2.5f;
        [Tooltip("1웨이브에 나오는 몬스터 수")]
        public int groupsPerWaveBase = 5;
        [Tooltip("웨이브마다 늘어나는 몬스터 수")]
        public float groupsPerWaveStep = 1f;
        [Tooltip("웨이브마다 몬스터 체력 증가(0.1 = +10%씩)")]
        public float hpGrowthPerWave = 0.1f;
        [Tooltip("웨이브마다 몬스터 속도 증가(0.03 = +3%씩)")]
        public float speedGrowthPerWave = 0.03f;
        [Tooltip("초반 웨이브 속도 배율(1웨이브, 2웨이브…). 그 뒤로는 1부터 웨이브마다 증가. 처음 하는 사람이 막히지 않게")]
        public float[] earlyWaveSpeed = { 0.75f, 0.88f };
        [Tooltip("몬스터 속도 배율 최대치")]
        public float speedMaxMultiplier = 1.6f;
        [Tooltip("플레이 영역 좌우 끝에서 이만큼(기준 픽셀) 안쪽에서 나온다")]
        public float spawnSideMarginPx = 14f;

        [Header("러시 (가끔 한꺼번에 몰려옴)")]
        [Tooltip("몇 웨이브마다 러시가 있는지 (보스 웨이브는 제외)")]
        public int rushEveryWaves = 3;
        [Tooltip("러시 길이(초). 웨이브 끝부분에 온다")]
        public float rushDuration = 4f;
        [Tooltip("러시 중 몬스터 간격(초)")]
        public float rushInterval = 0.8f;
        [Tooltip("러시 전 경고 시간(초)")]
        public float rushWarning = 1.2f;

        [Header("보스")]
        [Tooltip("몇 웨이브마다 보스 웨이브인지 (4 = 4, 8, 12…웨이브)")]
        public int bossEveryWaves = 4;
        [Tooltip("보스 회차마다 체력 증가(0.6 = +60%씩)")]
        public float bossHpGrowth = 0.6f;
        [Tooltip("튜토리얼: 몬스터가 강조된 뒤(직접 잡을 때) 내려오는 속도 배율(느리게)")]
        public float tutorialMonsterSpeed = 0.5f;
        [Tooltip("튜토리얼: UFO 몸통 판정 크기 배율(맞히기 쉽게)")]
        public float tutorialBodyScale = 1.3f;
        [Tooltip("튜토리얼 보스 체력 배율(약하게)")]
        public float tutorialBossHpMultiplier = 0.3f;
        [Tooltip("보스 등장 전 경고 시간(초)")]
        public float bossWarning = 2f;
        [Tooltip("보스가 부하(떼거리형)를 부르는 간격(초)")]
        public float bossSummonInterval = 6f;
        [Tooltip("보스 처치 단계 보상 골드: 첫 회")]
        public int bossRewardBase = 50;
        [Tooltip("보스 회차마다 늘어나는 보상 골드")]
        public int bossRewardStep = 30;

        [Header("재화 운석")]
        [Tooltip("운석이 나오는 간격(초) 최소~최대 (보스 웨이브 포함)")]
        public Vector2 meteorIntervalRange = new Vector2(7f, 12f);
        [Tooltip("골드 동전이 UFO로 빨려 들어가기 시작하는 시간(초)")]
        public float coinMagnetDelay = 0.25f;
        [Tooltip("동전이 빨려 들어가는 최고 속도(플레이 영역 높이/초)")]
        public float coinMagnetSpeed = 1.6f;


        [Header("플레이어 계정 레벨 (플레이 경험치, 데모는 보여주기용)")]
        public float accountXpPerKill = 1f;
        public float accountXpPerWave = 5f;
        [Tooltip("버틴 1분당")]
        public float accountXpPerMinute = 10f;
        [Tooltip("레벨 1→2에 필요한 경험치")]
        public float accountXpBase = 60f;
        [Tooltip("레벨마다 더 필요한 경험치")]
        public float accountXpStep = 30f;

        [Header("판 밖 강화 (로비, 골드 사용)")]
        public UpgradeDefinition[] upgrades =
        {
            new UpgradeDefinition { nameKey = "upgrade.earth.name", descKey = "upgrade.earth.desc", effect = UpgradeDefinition.Effect.EarthHp, valuePerLevel = 10f, costs = new[] { 40, 80, 150, 250, 400 } },
            new UpgradeDefinition { nameKey = "upgrade.range.name", descKey = "upgrade.range.desc", effect = UpgradeDefinition.Effect.DashRange, valuePerLevel = 0.04f, costs = new[] { 60, 120, 220, 350, 500 } },
            new UpgradeDefinition { nameKey = "upgrade.meteor.name", descKey = "upgrade.meteor.desc", effect = UpgradeDefinition.Effect.MeteorGold, valuePerLevel = 0.1f, costs = new[] { 50, 100, 180, 300, 450 } },
            // 11/27 추가(저장된 강화 단계는 배열 순서로 맞추므로 항상 맨 뒤에 붙인다)
            new UpgradeDefinition { nameKey = "upgrade.attack.name", descKey = "upgrade.attack.desc", effect = UpgradeDefinition.Effect.DashDamage, valuePerLevel = 1f, costs = new[] { 120, 300, 600 } },
        };

        [Header("연쇄 보너스 (한 번 날아가는 동안 여러 마리)")]
        [Tooltip("연쇄 한 단계마다 처치 점수 +비율(0.1 = +10%)")]
        public float chainScoreBonusPerStep = 0.1f;
        [Tooltip("처치 점수 보너스 최대(1 = +100%)")]
        public float chainScoreBonusMax = 1f;
        [Tooltip("이 연쇄 수부터 처치할 때마다 골드 동전")]
        public int chainGoldFrom = 3;
        [Tooltip("연쇄 골드 최대(한 마리당)")]
        public int chainGoldMax = 5;

        [Header("판 끝")]
        [Tooltip("지구가 함락된 뒤 결과 씬으로 넘어가기까지(초)")]
        public float gameOverDelay = 1.6f;

        [Header("판 안 성장 (경험치·레벨업)")]
        [Tooltip("레벨 1→2에 필요한 경험치")]
        public float xpBase = 6f;
        [Tooltip("레벨마다 더 필요한 경험치")]
        public float xpStep = 4f;
        [Tooltip("레벨이 높을수록 더 가파르게 (레벨-1)² × 이 값")]
        public float xpQuadratic = 0.3f;
        [Tooltip("카드 화면이 뜬 뒤 이 시간(초) 동안은 탭을 받지 않는다(이동하던 손가락으로 잘못 고르지 않게)")]
        public float cardInputDelay = 0.4f;
        [Tooltip("레벨업 때 UFO가 크게 통 튀는 정도")]
        public float squashOnLevelUp = 0.45f;

        [Header("몬스터 간격 (너무 밀집하지 않게)")]
        [Tooltip("몬스터끼리 가로 간격의 최소치(플레이 영역 너비 비율). 옛 '최소 점프 거리'와 같은 8%")]
        [Range(0.02f, 0.3f)] public float monsterMinGapFraction = 0.08f;
        [Tooltip("두 몬스터 판정이 닿지 않게 더 띄우는 여유(기준 픽셀)")]
        public float monsterExtraGapPx = 2f;
        [Tooltip("이 세로 거리(최소 간격 배율) 안에 있는 몬스터끼리만 가로 간격을 맞춘다")]
        public float monsterGapVerticalRange = 1.2f;
        [Tooltip("너무 가까우면 옆으로 벌어지는 속도(플레이 영역 너비/초)")]
        public float monsterSeparationSpeed = 0.35f;
        [Tooltip("새로 나올 때 이 높이(플레이 영역 높이 비율)까지의 몬스터와 겹치지 않는 자리를 찾는다")]
        [Range(0f, 1f)] public float spawnCheckBandFraction = 0.3f;

        // ---- 계산 값 ----
        /// <summary>두 몬스터 사이에 필요한 가로 간격(중심끼리, 유닛).</summary>
        public float MonsterGap(float radiusA, float radiusB)
        {
            return Mathf.Max(monsterMinGapFraction * PlayAreaSize.x, radiusA + radiusB + PixelsToUnits(monsterExtraGapPx));
        }

        public float PixelsToUnits(float px) => px / pixelsPerUnit;
        public Vector2 PlayAreaSize => new Vector2(playAreaPixels.x, playAreaPixels.y) / pixelsPerUnit;
    }
}
