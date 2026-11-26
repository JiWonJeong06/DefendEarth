using System;

namespace BouncyUFO
{
    /// <summary>
    /// 한 판의 기록. 결과 화면에서 "운석 골드 + 단계 보상 = 합계"로 나눠 보여 준다.
    /// 점수 = 무언가 처치·파괴할 때만 오른다(몬스터·보스·운석의 점수). 시간으로는 오르지 않는다(11/27).
    /// </summary>
    public sealed class RunStats
    {
        public int Wave { get; set; }
        public int Kills { get; private set; }
        public int BossKills { get; private set; }
        public int MeteorGold { get; private set; }
        public int StageGold { get; private set; }
        public int TotalGold => MeteorGold + StageGold;
        public int Score { get; private set; }

        public event Action<int> GoldChanged;
        public event Action<int> ScoreChanged;

        public void AddKill(bool boss, int score)
        {
            Kills++;
            if (boss) BossKills++;
            AddScore(score);
        }

        /// <summary>처치가 아닌 파괴(운석) 점수.</summary>
        public void AddScore(int score)
        {
            if (score <= 0) return;
            Score += score;
            ScoreChanged?.Invoke(Score);
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
