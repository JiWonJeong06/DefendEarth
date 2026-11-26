using System;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 한 판의 상태. 지구 체력이 0이 되거나 일시정지에서 '나가기'를 누르면 판이 끝난다(시간 제한 없음).
    /// 끝나면 점수·최고 기록·골드·플레이 경험치를 저장하고 결과 씬으로 넘어간다.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class GameSession : MonoBehaviour
    {
        public enum State { Playing, GameOver }

        public static GameSession Instance { get; private set; }

        [SerializeField] private Earth earth;

        public State Current { get; private set; } = State.Playing;
        public bool IsPlaying => Current == State.Playing;
        /// <summary>이번 판에서 버틴 시간(초).</summary>
        public float Elapsed { get; private set; }
        public Earth Earth => earth;
        public RunStats Stats { get; } = new RunStats();

        /// <summary>판이 끝남(골드 정산 전). 날아가던 동전 등을 여기서 마무리한다.</summary>
        public event Action GameOver;
        /// <summary>정산·저장까지 끝남.</summary>
        public event Action<RunResult> Finished;

        private int pauseCount;
        private float resultAt = -1f;

        /// <summary>잠깐 멈춤 중인지(레벨업 카드 고르기, 일시정지 창). 판은 계속 진행 중이다.</summary>
        public bool IsPaused => pauseCount > 0;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            if (earth != null) earth.Died += OnEarthDied;
        }

        private void OnDisable()
        {
            if (earth != null) earth.Died -= OnEarthDied;
        }

        private void Update()
        {
            if (resultAt >= 0f && Time.unscaledTime >= resultAt)
            {
                resultAt = -1f;
                SceneFlow.GoResult();
                return;
            }
            if (!IsPlaying) return;
            Elapsed += Time.deltaTime;
            Stats.SetTimeScore(Mathf.FloorToInt(Elapsed * PlayArea.Config.scorePerSecond));
        }

        private void OnEarthDied() => End(false, PlayArea.Config.gameOverDelay);

        /// <summary>일시정지의 '나가기': 지금까지 기록을 그대로 인정하고 결과로.</summary>
        public void Quit() => End(true, 0f);

        private void End(bool quit, float delay)
        {
            if (!IsPlaying) return;
            Current = State.GameOver;
            Time.timeScale = 0f;
            GameOver?.Invoke();
            var result = FinalizeRun(quit);
            SceneFlow.LastResult = result;
            SceneFlow.HasResult = true;
            Finished?.Invoke(result);
            resultAt = Time.unscaledTime + delay;
        }

        /// <summary>최고 기록·골드·플레이 경험치·순위표를 기기에 저장한다. 판에서 져도(나가도) 유지.</summary>
        private RunResult FinalizeRun(bool quit)
        {
            var save = SaveSystem.Data;
            var before = Account.FromTotal(save.accountXp);
            int xpGained = Account.XpForRun(Stats.Kills, Stats.Wave, Elapsed);

            var r = new RunResult
            {
                quit = quit,
                score = Stats.Score,
                wave = Stats.Wave,
                seconds = Elapsed,
                kills = Stats.Kills,
                meteorGold = Stats.MeteorGold,
                stageGold = Stats.StageGold,
                totalGold = Stats.TotalGold,
                newBest = Stats.Score > save.bestScore,
                xpGained = xpGained,
                levelBefore = before.level,
            };
            if (r.newBest) save.bestScore = Stats.Score;
            if (Stats.Wave > save.bestWave) save.bestWave = Stats.Wave;
            save.gold += Stats.TotalGold;
            save.totalRuns++;
            save.accountXp += xpGained;
            if (Stats.Score > 0) SaveSystem.AddTopScore(Stats.Score, Stats.Wave);
            SaveSystem.Save();

            var after = Account.FromTotal(save.accountXp);
            r.bestScore = save.bestScore;
            r.walletGold = save.gold;
            r.levelAfter = after.level;
            r.xpAfter = after.xp;
            r.xpNeedAfter = after.need;
            return r;
        }

        /// <summary>게임을 멈춘다. 여러 곳에서 불러도 모두 Resume해야 다시 움직인다.</summary>
        public void Pause()
        {
            pauseCount++;
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            pauseCount = Mathf.Max(0, pauseCount - 1);
            if (pauseCount == 0 && IsPlaying) Time.timeScale = 1f;
        }

        /// <summary>같은 판을 처음부터(로딩 화면을 거쳐).</summary>
        public void Restart() => SceneFlow.StartGame();
    }
}
