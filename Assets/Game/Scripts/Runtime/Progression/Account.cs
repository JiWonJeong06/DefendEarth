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

    /// <summary>판 밖 강화 한 종류(로비에서 골드로 올린다).</summary>
    [Serializable]
    public sealed class UpgradeDefinition
    {
        public enum Effect
        {
            /// <summary>지구 최대 체력 +value</summary>
            EarthHp,
            /// <summary>게이지 차는 시간 × (1 - value). (숫자 1은 옛 '연사' 자리, 저장된 강화 단계를 그대로 이어 쓴다)</summary>
            DashRange = 1, // 옛 ChargeSpeed 자리(저장된 숫자 그대로)
            /// <summary>운석 골드 × (1 + value)</summary>
            MeteorGold,
        }

        public string nameKey;
        public string descKey;
        public Effect effect;
        [Tooltip("단계마다 늘어나는 값")]
        public float valuePerLevel;
        [Tooltip("단계별 가격(골드). 개수 = 최대 단계")]
        public int[] costs;

        public int MaxLevel => costs != null ? costs.Length : 0;
    }

    /// <summary>판 밖 강화를 게임에 적용할 때 쓰는 계산.</summary>
    public static class Upgrades
    {
        /// <summary>
        /// 이 효과의 로비 강화 합계. 튜토리얼 판에서는 임시로 모든 강화를 최대 단계로 친다(쉽게 깨도록).
        /// </summary>
        public static float Total(UpgradeDefinition.Effect effect)
        {
            var c = PlayArea.Config;
            if (c == null || c.upgrades == null) return 0f;
            float sum = 0f;
            for (int i = 0; i < c.upgrades.Length; i++)
            {
                var u = c.upgrades[i];
                if (u == null || u.effect != effect) continue;
                int level = SceneFlow.IsTutorial ? u.MaxLevel : Mathf.Min(SaveSystem.Data.UpgradeLevel(i), u.MaxLevel);
                sum += u.valuePerLevel * level;
            }
            return sum;
        }
    }
}
