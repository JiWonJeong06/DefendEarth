using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Range")]
    public float spawnMinX;
    public float spawnMaxX;

    [Header("Rush Settings")]
    [SerializeField] private float rushSpawnDelay = 0.15f; // 러시 시 마리당 딜레이

    [Header("End Rush")]
    [SerializeField] private float endRushTimeThreshold = 30f; // 마지막 N초부터 잔여 적 소진
    [SerializeField] private float endRushInterval = 0.5f;     // 잔여 적 스폰 간격

    // 런타임
    private DifficultyData diff;
    private float timeRemaining;
    private int   enemiesLeft;      // 아직 스폰 안 된 적 수
    private float spawnTimer;
    private float growthTimer;
    private float currentSpawnInterval;

    private float currentHp;
    private float currentAtk;
    private int   currentScore;

    private bool isEndRush = false;
    private bool isRunning = false;

    public void StartSpawning(DifficultyData data)
    {
        diff        = data;
        timeRemaining = data.timeLimit;
        enemiesLeft   = data.totalEnemyCount;

        currentHp    = data.enemyBaseHp;
        currentAtk   = data.enemyBaseAtk;
        currentScore = data.enemyBaseScore;

        // 유효 스폰 시간 = 전체 - 마지막 버퍼(30초)
        float effectiveTime = Mathf.Max(data.timeLimit - endRushTimeThreshold, 1f);
        currentSpawnInterval = effectiveTime / data.totalEnemyCount;

        spawnTimer  = currentSpawnInterval;
        growthTimer = data.growthInterval;
        isRunning   = true;
        isEndRush   = false;

        Debug.Log($"[Spawner] {data.name} 시작 | 적:{data.totalEnemyCount} | 간격:{currentSpawnInterval:F2}s");
    }

    private void Update()
    {
        if (!isRunning || enemiesLeft <= 0) return;

        timeRemaining -= Time.deltaTime;
        growthTimer   -= Time.deltaTime;
        spawnTimer    -= Time.deltaTime;

        // 스탯 성장
        if (growthTimer <= 0f)
        {
            GrowStats();
            growthTimer = diff.growthInterval;
        }

        // 마지막 N초 진입 → 잔여 적 빠르게 소진
        if (!isEndRush && timeRemaining <= endRushTimeThreshold)
        {
            isEndRush = true;
            currentSpawnInterval = endRushInterval;
            spawnTimer = 0f;
            Debug.Log($"[Spawner] 마지막 {endRushTimeThreshold}초! 잔여 적 {enemiesLeft}마리 소진 시작");
        }

        if (spawnTimer <= 0f)
        {
            // 러시 발동 여부
            bool doRush = !isEndRush && (Random.value < diff.rushChance);

            if (doRush)
                StartCoroutine(SpawnRush(diff.rushEnemyCount));
            else
                SpawnOne();

            spawnTimer = currentSpawnInterval;
        }
    }

    private void SpawnOne()
    {
        if (enemiesLeft <= 0) return;

        float x = Random.Range(spawnMinX, spawnMaxX);
        Vector3 pos = new Vector3(x, transform.position.y, transform.position.z);
        GameObject go = Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Length)], pos, Quaternion.identity);

        InitEnemy(go);
        enemiesLeft--;

        if (enemiesLeft <= 0)
        {
            isRunning = false;
            Debug.Log("[Spawner] 모든 적 스폰 완료");
        }
    }

    private System.Collections.IEnumerator SpawnRush(int count)
    {
        int spawnCount = Mathf.Min(count, enemiesLeft);
        Debug.Log($"[Spawner] 러시! {spawnCount}마리 동시 스폰");

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnOne();
            if (i < spawnCount - 1)
                yield return new WaitForSeconds(rushSpawnDelay);
        }
    }

    private void InitEnemy(GameObject go)
    {
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null)
            enemy.SetStats(currentHp, currentAtk, currentScore);
    }

    private void GrowStats()
    {
        currentHp    *= (1f + diff.hpGrowthRate);
        currentAtk   *= (1f + diff.atkGrowthRate);
        currentScore  = Mathf.RoundToInt(currentScore * (1f + diff.scoreGrowthRate));

        Debug.Log($"[Spawner] 스탯 증가 → HP:{currentHp:F1} ATK:{currentAtk:F1} Score:{currentScore}");
    }

    public float GetTimeRemaining() => timeRemaining;
    public int   GetEnemiesLeft()   => enemiesLeft;
}