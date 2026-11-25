using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip   bgmClip;

    [Range(0f, 1f)]
    private float bgmVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환해도 유지
    }

    private void Start()
    {
        bgmSource.clip   = bgmClip;
        bgmSource.loop   = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    // Slider onValueChanged 연결 (슬라이더 값 0~100)
    public void OnBGMSliderChanged(float value)
    {
        bgmVolume        = value / 100f;
        bgmSource.volume = bgmVolume;
    }

    public float GetBGMVolume() => bgmVolume * 100f; // 슬라이더 초기값용
}