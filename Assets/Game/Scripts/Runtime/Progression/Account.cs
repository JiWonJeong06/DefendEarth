using System;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 플레이어 계정 레벨(판이 끝나도 쌓이는 플레이 경험치). 데모에서는 보여주기용이고 보상은 추후 업데이트.
    /// </summary>
    public static class Account
    {
        /// <summary>레벨 level에서 다음 레벨까지 필요한 경험치.</summary>
        public static int NeedFor(int level)
        {
            var c = PlayArea.Config;
            float baseNeed = c != null ? c.accountXpBase : 60f;
            float step = c != null ? c.accountXpStep : 30f;
            return Mathf.Max(1, Mathf.RoundToInt(baseNeed + step * (level - 1)));
        }

        /// <summary>누적 경험치 → (레벨, 이번 레벨에서 쌓인 경험치, 다음 레벨까지 필요).</summary>
        public static (int level, int xp, int need) FromTotal(int totalXp)
        {
            int level = 1;
            int xp = Mathf.Max(0, totalXp);
            while (level < 999)
            {
                int need = NeedFor(level);
                if (xp < need) return (level, xp, need);
                xp -= need;
                level++;
            }
            return (level, xp, NeedFor(level));
        }

        /// <summary>한 판에서 얻는 플레이 경험치(처치·웨이브·버틴 시간).</summary>
        public static int XpForRun(int kills, int wave, float seconds)
        {
            var c = PlayArea.Config;
            if (c == null) return kills + wave * 5;
            return Mathf.Max(0, Mathf.RoundToInt(kills * c.accountXpPerKill + wave * c.accountXpPerWave + seconds / 60f * c.accountXpPerMinute));
        }
    }
}
