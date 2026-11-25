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

        // ---- 계산 값 ----
        public float PixelsToUnits(float px) => px / pixelsPerUnit;
        public Vector2 PlayAreaSize => new Vector2(playAreaPixels.x, playAreaPixels.y) / pixelsPerUnit;
    }
}
