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

        [Header("통통 이동: 짧게 누르면 조금, 길게 누를수록 멀리 (최대치 있음)")]
        [Tooltip("짧게 탭했을 때 튀는 거리(플레이 영역 너비 비율)")]
        [Range(0.02f, 0.3f)] public float hopMinDistanceFraction = 0.08f;
        [Tooltip("가장 길게 눌렀을 때 튀는 최대 거리(플레이 영역 너비 비율)")]
        [Range(0.1f, 1f)] public float hopMaxDistanceFraction = 0.35f;
        [Tooltip("이 시간(초)보다 짧게 누르면 '짧은 탭'(최소 거리)")]
        public float hopShortPressTime = 0.1f;
        [Tooltip("이 시간(초)까지 누르면 최대 거리. 그 이상은 더 늘지 않는다")]
        public float hopFullChargeTime = 0.45f;
        [Tooltip("짧은 통/가장 먼 통에 걸리는 시간(초)")]
        public Vector2 hopDurationMinMax = new Vector2(0.08f, 0.17f);
        [Tooltip("짧은 통/가장 먼 통의 튀는 높이(기준 픽셀)")]
        public Vector2 hopHeightMinMaxPx = new Vector2(3f, 9f);
        [Tooltip("튀는 중에 손을 떼면 이 시간(초) 안에 착지할 때 이어서 튄다")]
        public float hopBufferTime = 0.15f;
        [Tooltip("누르고 있는 동안 최대로 움츠리는 정도(모으는 느낌)")]
        public float squashOnCharge = 0.2f;

        [Header("통통 연출 (스쿼시·스트레치)")]
        [Tooltip("스프링 세기. 클수록 빨리 원래 모양으로 돌아온다")]
        public float squashStiffness = 260f;
        [Tooltip("스프링 감쇠. 작을수록 더 출렁인다")]
        public float squashDamping = 12f;
        [Tooltip("튀어 오를 때 위로 늘어나는 정도")]
        public float squashOnHopStart = 0.22f;
        [Tooltip("착지할 때 납작하게 눌리는 정도")]
        public float squashOnHopLand = 0.3f;

        [Tooltip("탄을 쏠 때 살짝 들썩이는 정도")]
        public float squashOnFire = 0.05f;

        [Header("기본 무기 (기본형 UFO: 직선탄)")]
        [Tooltip("발사 간격(초)")]
        public float fireInterval = 0.22f;
        [Tooltip("탄 속도(플레이 영역 높이/초)")]
        public float bulletSpeedFraction = 1.0f;
        public int bulletDamage = 1;
        [Tooltip("탄 판정 반지름(기준 픽셀). 그림(6×10)보다 살짝 크게")]
        public float bulletHitRadiusPx = 5f;
        [Tooltip("UFO 중심에서 탄이 나가는 높이(기준 픽셀)")]
        public float muzzleOffsetPx = 10f;
        [Tooltip("탄 갈래가 여러 개일 때 옆 간격(기준 픽셀)")]
        public float multiShotSpacingPx = 7f;

        [Header("지구")]
        public int earthMaxHp = 100;
        [Tooltip("몬스터가 이 높이(플레이 영역 아래 끝에서 기준 픽셀)에 닿으면 지구 피해")]
        public float earthLinePx = 22f;

        [Header("웨이브 (몬스터가 무리로 몰려온다)")]
        [Tooltip("웨이브 하나의 길이(초)")]
        public float waveDuration = 18f;
        [Tooltip("웨이브 사이 쉬는 시간(초)")]
        public float waveBreak = 2.5f;
        [Tooltip("1웨이브에 나오는 무리 수")]
        public int groupsPerWaveBase = 7;
        [Tooltip("웨이브마다 늘어나는 무리 수")]
        public float groupsPerWaveStep = 1.5f;
        [Tooltip("웨이브마다 몬스터 체력 증가(0.1 = +10%씩)")]
        public float hpGrowthPerWave = 0.1f;
        [Tooltip("웨이브마다 몬스터 속도 증가(0.03 = +3%씩)")]
        public float speedGrowthPerWave = 0.03f;
        [Tooltip("몬스터 속도 배율 최대치")]
        public float speedMaxMultiplier = 1.6f;
        [Tooltip("플레이 영역 좌우 끝에서 이만큼(기준 픽셀) 안쪽에서 나온다")]
        public float spawnSideMarginPx = 14f;

        [Header("러시 (가끔 한꺼번에 몰려옴)")]
        [Tooltip("몇 웨이브마다 러시가 있는지 (보스 웨이브는 제외)")]
        public int rushEveryWaves = 3;
        [Tooltip("러시 길이(초). 웨이브 끝부분에 온다")]
        public float rushDuration = 4f;
        [Tooltip("러시 중 무리 간격(초)")]
        public float rushInterval = 0.35f;
        [Tooltip("러시 전 경고 시간(초)")]
        public float rushWarning = 1.2f;

        [Header("보스")]
        [Tooltip("몇 웨이브마다 보스 웨이브인지 (4 = 4, 8, 12…웨이브)")]
        public int bossEveryWaves = 4;
        [Tooltip("보스 회차마다 체력 증가(0.6 = +60%씩)")]
        public float bossHpGrowth = 0.6f;
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

        [Header("점수")]
        [Tooltip("버틴 시간 1초당 점수 (몬스터 점수는 몬스터 데이터의 score)")]
        public float scorePerSecond = 2f;

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
            new UpgradeDefinition { nameKey = "upgrade.firerate.name", descKey = "upgrade.firerate.desc", effect = UpgradeDefinition.Effect.FireRate, valuePerLevel = 0.05f, costs = new[] { 60, 120, 220, 350, 500 } },
            new UpgradeDefinition { nameKey = "upgrade.meteor.name", descKey = "upgrade.meteor.desc", effect = UpgradeDefinition.Effect.MeteorGold, valuePerLevel = 0.1f, costs = new[] { 50, 100, 180, 300, 450 } },
        };

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
        [Tooltip("몬스터끼리 가로 간격의 최소치 = UFO 최소 점프 거리 × 이 배율. 1이면 최소 점프만큼")]
        [Range(0.5f, 3f)] public float monsterGapHopMultiplier = 1f;
        [Tooltip("두 몬스터 판정이 닿지 않게 더 띄우는 여유(기준 픽셀)")]
        public float monsterExtraGapPx = 2f;
        [Tooltip("이 세로 거리(최소 간격 배율) 안에 있는 몬스터끼리만 가로 간격을 맞춘다")]
        public float monsterGapVerticalRange = 1.2f;
        [Tooltip("너무 가까우면 옆으로 벌어지는 속도(플레이 영역 너비/초)")]
        public float monsterSeparationSpeed = 0.35f;
        [Tooltip("새로 나올 때 이 높이(플레이 영역 높이 비율)까지의 몬스터와 겹치지 않는 자리를 찾는다")]
        [Range(0f, 1f)] public float spawnCheckBandFraction = 0.3f;

        // ---- 계산 값 ----
        /// <summary>UFO가 가장 짧게 튀는 거리(유닛).</summary>
        public float MinHopDistance => hopMinDistanceFraction * PlayAreaSize.x;

        /// <summary>두 몬스터 사이에 필요한 가로 간격(중심끼리, 유닛).</summary>
        public float MonsterGap(float radiusA, float radiusB)
        {
            return Mathf.Max(MinHopDistance * monsterGapHopMultiplier, radiusA + radiusB + PixelsToUnits(monsterExtraGapPx));
        }

        public float PixelsToUnits(float px) => px / pixelsPerUnit;
        public Vector2 PlayAreaSize => new Vector2(playAreaPixels.x, playAreaPixels.y) / pixelsPerUnit;
    }
}
