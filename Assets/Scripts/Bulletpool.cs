using UnityEngine;
using System.Collections.Generic;

public class BulletPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 50;
    
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private List<GameObject> allBullets = new List<GameObject>();
    private int createdCount = 0;

    private void Start()
    {
        InitializePool();
    }

    /// <summary>
    /// 풀 초기화 - 50개의 탄막 생성 (동시에 발사)
    /// </summary>
    private void InitializePool()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet Prefab이 설정되지 않았습니다!");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, transform);
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);
            allBullets.Add(bullet);
            createdCount++;
        }

        Debug.Log($"[BulletPool] {poolSize}개의 탄막 풀 생성 완료!");
    }

    /// <summary>
    /// 풀에서 탄막 꺼내기 (생성 중에도 바로 꺼낼 수 있음)
    /// </summary>
    public GameObject GetBullet(Vector3 position)
    {
        GameObject bullet;

        if (bulletPool.Count > 0)
        {
            bullet = bulletPool.Dequeue();
        }
        else
        {
            // 풀이 비어있으면 새로 생성
            bullet = Instantiate(bulletPrefab, transform);
            allBullets.Add(bullet);
            Debug.LogWarning("[BulletPool] 풀이 비어있어 새 탄막을 생성했습니다!");
        }

        bullet.transform.position = position;
        bullet.SetActive(true);

        return bullet;
    }

    /// <summary>
    /// 풀에 탄막 반환
    /// </summary>
    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        bullet.transform.position = Vector3.zero;
        
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        bulletPool.Enqueue(bullet);
    }

    /// <summary>
    /// 현재 풀에 남은 탄막 개수
    /// </summary>
    public int GetAvailableCount()
    {
        return bulletPool.Count;
    }
}