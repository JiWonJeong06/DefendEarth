using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI costText;
   // [SerializeField] private TextMeshProUGUI atkText;
  //  [SerializeField] private TextMeshProUGUI speedText;
   // [SerializeField] private TextMeshProUGUI fireRateText;
    [SerializeField] private Button          upgradeButton;

    // 스탯 미리보기용 (Player.cs 와 동일한 공식)
    private const float BaseAtk        = 5f;
    private const float BaseBulletSpeed = 8f;
    private const float BaseFireRate   = 0.85f;
    private const float AtkGrowth      = 0.03f;
    private const float SpeedGrowth    = 0.0105f;
    private const float FireGrowth     = 0.0125f;

    private void OnEnable() => Refresh();

    public void OnClickUpgrade()
    {
        if (GameData.UpgradeLevel >= 100) return;

        int cost = GameData.GetUpgradeCost();
        if (GameData.Money < cost)
        {
            Debug.Log("[Upgrade] 돈이 부족합니다!");
            return;
        }

        GameData.Money        -= cost;
        GameData.UpgradeLevel += 1;
        Debug.Log($"[Upgrade] Lv.{GameData.UpgradeLevel} 달성! 잔액:{GameData.Money}원");

        Refresh();
    }

    private void Refresh()
    {
        int lv   = GameData.UpgradeLevel;
        int cost = GameData.GetUpgradeCost();
        int n    = lv - 1;

        float atk   = BaseAtk         * Mathf.Pow(1f + AtkGrowth,   n);
        float speed = BaseBulletSpeed * Mathf.Pow(1f + SpeedGrowth,  n);
        float fire  = BaseFireRate    * Mathf.Pow(1f + FireGrowth,   n);

        levelText.text   = $"Lv. {lv} / 100";
        moneyText.text   = $"보유 금액: {GameData.Money:N0}원";
        costText.text    = lv >= 100 ? "MAX" : $"업그레이드: {cost:N0}원";
        // atkText.text     = $"공격력: {atk:F1}";
        // speedText.text   = $"총알 속도: {speed:F1}";
        // fireRateText.text = $"공격 속도: {(1f / fire):F2}초";

        if (upgradeButton != null)
            upgradeButton.interactable = lv < 100 && GameData.Money >= cost;
    }
}