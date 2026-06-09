using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private Earth        earth;

    [Header("UI Panels")]
    [SerializeField] private GameObject   hud;
    [SerializeField] private GameObject   pausePanel;
    [SerializeField] private GameObject   clearPanel;
    [SerializeField] private GameObject   gameOverPanel;

    private int   totalScore  = 0;
    private bool  isGameEnd   = false;
    private bool  isPaused    = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (earth   == null) earth   = FindFirstObjectByType<Earth>();

        pausePanel.SetActive(false);
        clearPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        StartGame(GameData.SelectedDifficulty);
    }

    private void Update()
    {
        if (isGameEnd) return;

        // 시간 초과 → 패배
        if (spawner.GetTimeRemaining() <= 0)
        {
            // 아직 살아있는 적이 있으면 패배
            if (GameObject.FindGameObjectsWithTag("Enemy").Length > 0)
                GameOver("시간 초과!");
            else
                CheckClear(); // 혹시 동시에 0이 될 경우 대비
        }
    }

    public void StartGame(Difficulty difficulty)
    {
        DifficultyData data = difficulty switch
        {
            Difficulty.Easy   => DifficultyData.Easy(),
            Difficulty.Normal => DifficultyData.Normal(),
            Difficulty.Hard   => DifficultyData.Hard(),
            _                 => DifficultyData.Easy()
        };

        totalScore = 0;
        spawner.StartSpawning(data);
        Debug.Log($"[GameManager] 게임 시작: {data.name}");
    }

    public void AddScore(int value)
    {
        totalScore += value;
        CheckClear();
    }

    // 모든 적 처치 + 스폰 완료 → 승리
    private void CheckClear()
    {
        if (isGameEnd) return;
        if (spawner.GetEnemiesLeft() > 0) return;
        if (GameObject.FindGameObjectsWithTag("Enemy").Length > 0) return;

        GameClear();
    }

    public void GameClear()
    {
        if (isGameEnd) return;
        isGameEnd = true;

        GameData.LastScore = totalScore;
        if (totalScore > GameData.BestScore)
            GameData.BestScore = totalScore;

        Time.timeScale = 0f;
        hud.SetActive(false);
        clearPanel.SetActive(true);
        Debug.Log($"[GameManager] 클리어! 점수: {totalScore}");
    }

    public void GameOver(string reason = "")
    {
        if (isGameEnd) return;
        isGameEnd = true;

        GameData.LastScore = totalScore;

        Time.timeScale = 0f;
        hud.SetActive(false);
        gameOverPanel.SetActive(true);
        Debug.Log($"[GameManager] 게임 오버: {reason}");
    }

    // ── 버튼 연결 ───────────────────────────
    public void OnClickPause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pausePanel.SetActive(isPaused);
    }

    public void OnClickRetry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.Load);
    }

    public void OnClickGoLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneNames.Lobby);
    }

    public int GetScore() => totalScore;
}

public enum Difficulty { Easy, Normal, Hard }