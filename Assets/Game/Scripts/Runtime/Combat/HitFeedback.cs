using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 타격감 세기 한곳 모음: 맞을 때·처치할 때 역경직(시간을 잠깐 거의 멈춤), 화면 흔들림, 폰 진동.
    /// 파편(Effects.Burst)·하얗게 번쩍임은 Monster가 따로 한다.
    /// </summary>
    public static class HitFeedback
    {
        // 역경직(초) / 흔들림(픽셀, 초)
        private const float HitStop = 0.045f, HitShakePx = 1f, HitShakeTime = 0.1f;
        private const float KillStop = 0.07f, KillShakePx = 2f, KillShakeTime = 0.16f;
        private const float BossHitStop = 0.06f, BossHitShakePx = 2f;
        private const float BossKillStop = 0.18f, BossKillShakePx = 4f, BossKillShakeTime = 0.4f;

        // 폰 진동(ms, 세기 0~1)
        private const int HitBuzzMs = 18, KillBuzzMs = 35, BossHitBuzzMs = 30, BossKillBuzzMs = 120, EarthBuzzMs = 60;

        // ---- 콤보(한 번 날아가는 동안 맞힌 수, 연쇄 포함) ----
        private static readonly int[] Scale = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19 }; // 도레미파솔라시도…
        private static int combo;

        /// <summary>지금 연쇄 수(한 번 날아가는 동안 맞힌 수).</summary>
        public static int Combo => combo;

        /// <summary>새로 날아갈 때 콤보를 처음부터.</summary>
        public static void ResetCombo() => combo = 0;

        /// <summary>맞힐 때마다 부른다. 이번 소리의 높이(도→레→미…)를 돌려주고, 2번째부터 CHAIN 글자를 띄운다.</summary>
        public static float RegisterHit()
        {
            int step = Scale[Mathf.Min(combo, Scale.Length - 1)];
            combo++;
            if (combo >= 2) ComboHud.Show(combo);
            return Mathf.Pow(2f, step / 12f);
        }

        public static void Hit(bool killed, bool boss)
        {
            var session = GameSession.Instance;
            if (boss)
            {
                if (killed) { session?.HitStop(BossKillStop); PlayAreaCamera.Shake(BossKillShakePx, BossKillShakeTime); Haptics.Pulse(BossKillBuzzMs, 1f); }
                else { session?.HitStop(BossHitStop); PlayAreaCamera.Shake(BossHitShakePx, HitShakeTime); Haptics.Pulse(BossHitBuzzMs, 0.6f); }
                return;
            }
            if (killed) { session?.HitStop(KillStop); PlayAreaCamera.Shake(KillShakePx, KillShakeTime); Haptics.Pulse(KillBuzzMs, 0.6f); }
            else { session?.HitStop(HitStop); PlayAreaCamera.Shake(HitShakePx, HitShakeTime); Haptics.Pulse(HitBuzzMs, 0.35f); }
        }

        /// <summary>지구가 맞았을 때(묵직하게 한 번).</summary>
        public static void EarthHurt() => Haptics.Pulse(EarthBuzzMs, 0.8f);
    }
}
