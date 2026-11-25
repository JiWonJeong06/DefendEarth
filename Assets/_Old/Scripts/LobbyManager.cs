using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject upgradePanel;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI bestScoreText;

    private void Start()
    {
        difficultyPanel.SetActive(false);
        settingsPanel.SetActive(false);
        upgradePanel.SetActive(false);

        if (bestScoreText != null)
            bestScoreText.text = $"점수: {GameData.BestScore}";
    }

    public void OnClickPlay()     => difficultyPanel.SetActive(true);
    public void OnClickSettings() => settingsPanel.SetActive(true);
    public void OnClickUpgrade()  => upgradePanel.SetActive(true);

    public void OnClickPlayExit() => difficultyPanel.SetActive(false);
    public void OnClickSettingsExit() => settingsPanel.SetActive(false);
    public void OnClickUpgradeExit() => upgradePanel.SetActive(false);

    public void OnClickClosePanel(GameObject panel) => panel.SetActive(false);

    public void OnSelectDifficulty(int level)
    {
        GameData.SelectedDifficulty = (Difficulty)level;
        SceneManager.LoadScene(SceneNames.Load);
    }
}