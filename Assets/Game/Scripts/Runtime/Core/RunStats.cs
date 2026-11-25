using System;

namespace BouncyUFO
{
    /// <summary>
    /// 한 판의 기록. 결과 화면에서 "운석 골드 + 단계 보상 = 합계"로 나눠 보여 준다.
    /// 점수 = 잡은 몬스터 점수 + 버틴 시간 점수 (보스 보너스는 보스의 점수로 들어감).
    /// </summary>
    public sealed class RunStats
    {
        public int Wave { get; set; }
        public int Kills { get; private set; }
        public int BossKills { get; private set; }
        public int MeteorGold { get; private set; }
        public int StageGold { get; private set; }
        public int TotalGold => MeteorGold + StageGold;
        public int KillScore { get; private set; }
        public int TimeScore { get; private set; }
        public int Score => KillScore + TimeScore;

        public event Action<int> GoldChanged;
        public event Action<int> ScoreChanged;

        public void AddKill(bool boss, int score)
        {
            Kills++;
            if (boss) BossKills++;
            if (score > 0)
            {
                KillScore += score;
                ScoreChanged?.Invoke(Score);
            }
        }

        public void SetTimeScore(int value)
        {
            if (value == TimeScore) return;
            TimeScore = value;
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
