namespace BouncyUFO
{
    /// <summary>한 판의 최종 결과(결과 씬에 보여 줄 것). 정산·저장이 끝난 값이다.</summary>
    public struct RunResult
    {
        /// <summary>일시정지에서 '나가기'로 끝냈는지(지구 함락이 아니라).</summary>
        public bool quit;
        public int score;
        public int bestScore;
        public bool newBest;
        public int wave;
        public float seconds;
        public int kills;
        public int meteorGold;
        public int stageGold;
        public int totalGold;
        public int walletGold;

        // 플레이어 계정 레벨(플레이 경험치). 데모는 보여주기용
        public int xpGained;
        public int levelBefore;
        public int levelAfter;
        public int xpAfter;
        public int xpNeedAfter;
    }
}
