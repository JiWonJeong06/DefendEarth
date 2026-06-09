using UnityEngine;
using TMPro;

public class ResultPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText;

    private void OnEnable()
    {
        if (scoreText != null)
            scoreText.text = $"점수: {GameData.LastScore:N0}";

        if (bestScoreText != null)
            bestScoreText.text = $"최고 점수: {GameData.BestScore:N0}";
    }

    public void SetTitle(string text)
    {
        if (titleText != null) titleText.text = text;
    }
}