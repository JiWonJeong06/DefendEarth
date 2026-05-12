using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HpBar : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMPro.TextMeshProUGUI hpText;
    private Earth earth;

    private void Start()
    {
        earth = FindFirstObjectByType<Earth>();
        
        if (earth == null)
            Debug.LogError("Earth를 찾을 수 없습니다!");
        
        if (hpSlider == null)
            hpSlider = GetComponent<Slider>();
        
        // 초기 HP 설정
        hpSlider.maxValue = earth.GetMaxHealth();
        hpSlider.value = earth.GetHealth();
        hpText.text = earth.GetHealth().ToString() + " / " + earth.GetMaxHealth().ToString();
    }

    private void Update()
    {
        hpSlider.maxValue = earth.GetMaxHealth();
        hpSlider.value = earth.GetHealth();
        hpText.text = earth.GetHealth().ToString() + " / " + earth.GetMaxHealth().ToString();
    }
}