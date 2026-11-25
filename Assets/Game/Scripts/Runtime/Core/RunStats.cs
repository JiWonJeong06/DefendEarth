using System;

namespace BouncyUFO
{
    /// <summary>
    /// 한 판의 기록. 결과 화면(5번 묶음)에서 "운석 골드 + 단계 보상 = 합계"로 나눠 보여 준다.
    /// 판이 끝나도 골드는 유지된다(저장은 5번 묶음).
    /// </summary>
    public sealed class RunStats
    {
        public int Wave { get; set; }
        public int Kills { get; private set; }
        public int BossKills { get; private set; }
        public int MeteorGold { get; private set; }
        public int StageGold { get; private set; }
        public int TotalGold => MeteorGold + StageGold;

        public event Action<int> GoldChanged;

        public void AddKill(bool boss)
        {
            Kills++;
            if (boss) BossKills++;
        }

        public void AddMeteorGold(int amount)
        {
            if (amount <= 0) return;
            MeteorGold += amount;
            GoldChanged?.Invoke(TotalGold);
        }

        public void AddStageGold(int amount)
        {
            if (amount <= 0) return;
            StageGold += amount;
            GoldChanged?.Invoke(TotalGold);
        }
    }
}
