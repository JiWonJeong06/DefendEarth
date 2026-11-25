using System;

public static class GameData
{
    public static Difficulty SelectedDifficulty = Difficulty.Easy;

    // ── 프로퍼티 (읽기/쓰기 시 자동 저장) ──────────────────
    public static int BestScore
    {
        get => UnityEngine.PlayerPrefs.GetInt("BestScore", 0);
        set { UnityEngine.PlayerPrefs.SetInt("BestScore", value); UnityEngine.PlayerPrefs.Save(); }
    }

    public static int LastScore
    {
        get => UnityEngine.PlayerPrefs.GetInt("LastScore", 0);
        set { UnityEngine.PlayerPrefs.SetInt("LastScore", value); UnityEngine.PlayerPrefs.Save(); }
    }

    public static int Money
    {
        get => UnityEngine.PlayerPrefs.GetInt("Money", 0);
        set { UnityEngine.PlayerPrefs.SetInt("Money", value); UnityEngine.PlayerPrefs.Save(); }
    }

    public static int UpgradeLevel
    {
        get => UnityEngine.PlayerPrefs.GetInt("UpgradeLevel", 1);
        set { UnityEngine.PlayerPrefs.SetInt("UpgradeLevel", value); UnityEngine.PlayerPrefs.Save(); }
    }

    // ── 업그레이드 비용 ──────────────────────────────────────
    public static int GetUpgradeCost()
    {
        if (UpgradeLevel >= 100) return 0;
        return (int)(500f * Math.Pow(1.08, UpgradeLevel - 1));
    }

    // ── 점수 → 돈 환산 (35%) ────────────────────────────────
    public static int ScoreToMoney(int score)
    {
        return (int)Math.Round(score * 0.35);
    }

    // ── 전체 초기화 (필요 시 사용) ──────────────────────────
    public static void ResetAll()
    {
        BestScore    = 0;
        LastScore    = 0;
        Money        = 0;
        UpgradeLevel = 1;
        UnityEngine.PlayerPrefs.Save();
    }
}