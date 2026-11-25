using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BouncyUFO
{
    /// <summary>
    /// 한 판의 상태. 지구 체력이 0이 될 때만 판이 끝난다(시간 제한 없음).
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
        /// <summary>정산·저장까지 끝남. 결과 화면은 이걸 듣는다.</summary>
        public event Action<RunResult> Finished;

        /// <summary>한 판의 최종 결과.</summary>
        public struct RunResult
        {
            public int score;
            public int bestScore;
            public bool newBest;
            public int wave;
            public float seconds;
            public int meteorGold;
            public int stageGold;
            public int totalGold;
            public int walletGold;
        }

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
            if (!IsPlaying) return;
            Elapsed += Time.deltaTime;
            Stats.SetTimeScore(Mathf.FloorToInt(Elapsed * PlayArea.Config.scorePerSecond));
        }

        private void OnEarthDied()
        {
            if (!IsPlaying) return;
            Current = State.GameOver;
            Time.timeScale = 0f;
            GameOver?.Invoke();
            Finished?.Invoke(FinalizeRun());
        }

        /// <summary>최고 기록·골드를 기기에 저장한다. 판에서 져도 골드는 유지.</summary>
        private RunResult FinalizeRun()
        {
            var save = SaveSystem.Data;
            var r = new RunResult
            {
                score = Stats.Score,
                wave = Stats.Wave,
                seconds = Elapsed,
                meteorGold = Stats.MeteorGold,
                stageGold = Stats.StageGold,
                totalGold = Stats.TotalGold,
                newBest = Stats.Score > save.bestScore,
            };
            if (r.newBest) save.bestScore = Stats.Score;
            if (Stats.Wave > save.bestWave) save.bestWave = Stats.Wave;
            save.gold += Stats.TotalGold;
            save.totalRuns++;
            SaveSystem.Save();
            r.bestScore = save.bestScore;
            r.walletGold = save.gold;
            return r;
        }

        private int pauseCount;

        /// <summary>잠깐 멈춤 중인지(레벨업 카드 고르기 등). 판은 계속 진행 중이다.</summary>
        public bool IsPaused => pauseCount > 0;

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

        public void Restart()
        {
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }
    }
}
