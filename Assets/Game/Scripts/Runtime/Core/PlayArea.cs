using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 고정 크기 플레이 영역(기본 180×320 픽셀 = 11.25×20 유닛, 원점 중심).
    /// 판정·이동 범위·스폰 위치는 모두 이 영역 기준이라 기기마다 똑같다.
    /// 화면에 남는 위아래/양옆은 배경·UI 전용이고 판정이 없다.
    /// </summary>
    public static class PlayArea
    {
        public static GameConfig Config { get; private set; }
        public static Vector2 Size { get; private set; } = new Vector2(11.25f, 20f);
        public static Rect WorldRect => new Rect(-Size * 0.5f, Size);
        public static float Left => -Size.x * 0.5f;
        public static float Right => Size.x * 0.5f;
        public static float Bottom => -Size.y * 0.5f;
        public static float Top => Size.y * 0.5f;

        public static void Init(GameConfig config)
        {
            Config = config;
            Size = config.PlayAreaSize;
        }

        public static float ClampX(float x, float halfWidth)
        {
            return Mathf.Clamp(x, Left + halfWidth, Right - halfWidth);
        }
    }
}
