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

        [Header("탭 한 칸 통통 이동")]
        [Tooltip("한 번 탭에 튀는 거리(플레이 영역 너비 비율). 0.2 = 5칸이면 끝에서 끝")]
        [Range(0.05f, 0.5f)] public float hopDistanceFraction = 0.2f;
        [Tooltip("한 칸 튀는 데 걸리는 시간(초). 짧을수록 빠르게 연타 가능")]
        [Range(0.05f, 0.4f)] public float hopDuration = 0.12f;
        [Tooltip("튈 때 위로 올라가는 높이(기준 픽셀)")]
        public float hopHeightPx = 6f;
        [Tooltip("튀는 중에 탭하면 이 시간(초) 안에 착지할 때 이어서 튄다")]
        public float hopBufferTime = 0.15f;

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

        [Header("몬스터 등장 (임시. 3번 묶음에서 웨이브로 바뀜)")]
        [Tooltip("처음 등장 간격(초)")]
        public float spawnIntervalStart = 1.4f;
        [Tooltip("가장 빠른 등장 간격(초)")]
        public float spawnIntervalMin = 0.55f;
        [Tooltip("이 시간(초)에 걸쳐 등장 간격이 가장 빠르게 줄어든다")]
        public float spawnRampSeconds = 180f;
        [Tooltip("플레이 영역 좌우 끝에서 이만큼(기준 픽셀) 안쪽에서 나온다")]
        public float spawnSideMarginPx = 14f;

        // ---- 계산 값 ----
        public float PixelsToUnits(float px) => px / pixelsPerUnit;
        public Vector2 PlayAreaSize => new Vector2(playAreaPixels.x, playAreaPixels.y) / pixelsPerUnit;
    }
}
