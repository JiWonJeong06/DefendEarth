using UnityEngine;

[System.Serializable]
public class DifficultyData
{
    public string name;

    [Header("Time & Enemy Count")]
    public float timeLimit;       // 제한 시간 (초)
    public int totalEnemyCount;   // 총 적 수

    [Header("Enemy Base Stats")]
    public float enemyBaseHp;
    public float enemyBaseAtk;
    public int   enemyBaseScore;

    [Header("Stat Growth")]
    public float growthInterval;  // 몇 초마다 증가
    public float hpGrowthRate;    // 체력 증가율
    public float atkGrowthRate;   // 공격력 증가율
    public float scoreGrowthRate; // 점수 증가율

    [Header("Rush")]
    public int   rushEnemyCount;  // 러시 시 동시 스폰 수
    public float rushChance;      // 매 스폰마다 러시 발동 확률 (0~1)

    public static DifficultyData Easy() => new DifficultyData
    {
        name            = "Easy",
        timeLimit       = 480f,   // 8분
        totalEnemyCount = 50,
        enemyBaseHp     = 10f,
        enemyBaseAtk    = 3f,
        enemyBaseScore  = 50,
        growthInterval  = 20f,
        hpGrowthRate    = 0.08f,  // 8%
        atkGrowthRate   = 0.06f,  // 6%
        scoreGrowthRate = 0.10f,  // 10%
        rushEnemyCount  = 2,
        rushChance      = 0.10f,  // 10%
    };

    public static DifficultyData Normal() => new DifficultyData
    {
        name            = "Normal",
        timeLimit       = 390f,   // 6분 30초
        totalEnemyCount = 100,
        enemyBaseHp     = 20f,
        enemyBaseAtk    = 8f,
        enemyBaseScore  = 100,
        growthInterval  = 15f,
        hpGrowthRate    = 0.10f,  // 10%
        atkGrowthRate   = 0.08f,  // 8%
        scoreGrowthRate = 0.12f,  // 12%
        rushEnemyCount  = 2,
        rushChance      = 0.15f,  // 15%
    };

    public static DifficultyData Hard() => new DifficultyData
    {
        name            = "Hard",
        timeLimit       = 300f,   // 5분
        totalEnemyCount = 150,
        enemyBaseHp     = 30f,
        enemyBaseAtk    = 12f,
        enemyBaseScore  = 200,
        growthInterval  = 10f,
        hpGrowthRate    = 0.12f,  // 12%
        atkGrowthRate   = 0.10f,  // 10%
        scoreGrowthRate = 0.15f,  // 15%
        rushEnemyCount  = 3,
        rushChance      = 0.20f,  // 20%
    };
}