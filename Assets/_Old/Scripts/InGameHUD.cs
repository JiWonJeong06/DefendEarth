using UnityEngine;
using TMPro;

public class InGameHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner    spawner;
    [SerializeField] private GameManager     gameManager;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI scoreText;

    private void Update()
    {
        if (spawner != null)
        {
            float t = Mathf.Max(spawner.GetTimeRemaining(), 0f);
            int min = (int)(t / 60f);
            int sec = (int)(t % 60f);
            timeText.text      = $"시간 : {min:D2}:{sec:D2}";
            enemyCountText.text = $"남은 적 :  {spawner.GetEnemiesLeft()}";
        }

        if (gameManager != null)
            scoreText.text = $"점수 : {gameManager.GetScore():N0}";
    }
}