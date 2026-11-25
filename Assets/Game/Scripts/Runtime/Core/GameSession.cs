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

        public event Action GameOver;

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
            if (IsPlaying) Elapsed += Time.deltaTime;
        }

        private void OnEarthDied()
        {
            if (!IsPlaying) return;
            Current = State.GameOver;
            Time.timeScale = 0f;
            GameOver?.Invoke();
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
