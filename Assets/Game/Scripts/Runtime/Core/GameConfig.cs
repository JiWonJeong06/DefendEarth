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

        [Header("드래그 이동")]
        [Tooltip("1 = 플레이 영역 너비만큼 밀면 UFO도 그만큼 이동. 설정 화면 감도(0.7~1.5)는 여기에 곱한다")]
        [Range(0.3f, 2f)] public float dragSensitivity = 1f;

        [Header("탭 판정")]
        [Tooltip("이 시간(초) 안에 손을 떼면 탭 후보")]
        public float tapMaxDuration = 0.15f;
        [Tooltip("누른 동안 이 거리(플레이 영역 너비 비율)보다 덜 움직였으면 탭")]
        [Range(0f, 0.2f)] public float tapMaxMoveFraction = 0.02f;

        [Header("통통 대시")]
        [Tooltip("대시 거리(플레이 영역 너비 비율)")]
        [Range(0.05f, 0.6f)] public float dashDistanceFraction = 0.25f;
        [Tooltip("대시에 걸리는 시간(초)")]
        public float dashDuration = 0.1f;
        [Tooltip("대시 재사용 대기(초)")]
        public float dashCooldown = 0.5f;
        [Tooltip("대시 중 위로 통 튀는 높이(기준 픽셀)")]
        public float dashHopPx = 6f;

        [Header("통통 연출 (스쿼시·스트레치)")]
        [Tooltip("스프링 세기. 클수록 빨리 원래 모양으로 돌아온다")]
        public float squashStiffness = 260f;
        [Tooltip("스프링 감쇠. 작을수록 더 출렁인다")]
        public float squashDamping = 12f;
        [Tooltip("움직이기 시작/멈출 때 늘어나는 정도")]
        public float squashOnMoveStartStop = 0.12f;
        [Tooltip("대시 출발 때 늘어나는 정도")]
        public float squashOnDashStart = 0.28f;
        [Tooltip("대시 착지 때 눌리는 정도")]
        public float squashOnDashLand = 0.3f;
        [Tooltip("이 속도(플레이 영역 너비/초) 이상이면 '움직이는 중'으로 본다")]
        public float movingSpeedThreshold = 0.15f;

        // ---- 계산 값 ----
        public float PixelsToUnits(float px) => px / pixelsPerUnit;
        public Vector2 PlayAreaSize => new Vector2(playAreaPixels.x, playAreaPixels.y) / pixelsPerUnit;
    }
}
