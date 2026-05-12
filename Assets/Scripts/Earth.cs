using UnityEngine;

public class Earth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    
     [SerializeField] private float currentHealth;
    private bool isDestroyed = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        
        currentHealth -= damage;
        Debug.Log($"[Earth] 피해 입음! 남은 체력: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        Debug.Log("[Earth] 지구가 파괴되었습니다! 게임 오버!");
        
        // GameManager에 게임 오버 신호
        // GameManager.Instance.GameOver();
        
        // 지구 제거 (선택사항)
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 적과 충돌
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 적의 공격력만큼 지구가 피해 입음
                float damage = enemy.GetScoreValue() / 10f; // 적의 스코어값을 공격력으로 사용
                TakeDamage(damage);
                
                // 지구와 충돌한 적은 사라짐
                enemy.TakeDamage(enemy.GetMaxHealth());
            }
        }
    }

    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDestroyed() => isDestroyed;
    public float GetHealthPercent() => currentHealth / maxHealth;
}