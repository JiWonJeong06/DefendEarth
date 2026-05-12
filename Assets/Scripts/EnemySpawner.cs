using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemyPrefab; // Reference to the enemy prefa
    public float spawnInterval = 5f; // Time interval between spawns
    private float timer; // Timer to track time between spawns
    public float transformminX; // Reference to the spawner's transform
    public float transformmaxX; // Reference to the spawner's transform

 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = spawnInterval;
    }

    // Update is called once per frame
    void Update()
    {
        float randomX = Random.Range(transformminX, transformmaxX);
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            // Spawn an enemy at the spawner's position
            Instantiate(enemyPrefab[Random.Range(0, enemyPrefab.Length)], new Vector3(randomX, transform.position.y, transform.position.z), Quaternion.identity);
            timer = spawnInterval;
        }
    }
}
