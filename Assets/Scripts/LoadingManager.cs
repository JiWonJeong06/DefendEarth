using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider     progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI tipText;

    private readonly string[] tips =
    {
        "적이 지구에 닿으면 체력이 줄어요!",
        "레벨이 오를수록 총알이 강해져요.",
        "마지막 30초엔 적이 한꺼번에 몰려와요!",
        "어려움 난이도는 적이 빠르게 강해집니다.",
    };

    private void Start()
    {
        if (tipText != null)
            tipText.text = tips[Random.Range(0, tips.Length)];

        StartCoroutine(LoadInGame());
    }

    private IEnumerator LoadInGame()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneNames.InGame);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (progressBar != null)  progressBar.value      = progress;
            if (progressText != null) progressText.text      = $"{(int)(progress * 100)}%";

            if (op.progress >= 0.9f)
            {
                // 최소 0.5초 대기 후 씬 전환 (너무 빠른 로딩 방지)
                yield return new WaitForSeconds(0.5f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}