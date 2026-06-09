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
        currentHealth  = Mathf.Max(currentHealth, 0f);
        Debug.Log($"[Earth] 피해! 남은 체력: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) GameOver();
    }

    private void GameOver()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        GameManager.Instance?.GameOver("지구 체력 0");
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                TakeDamage(enemy.GetAtk());
                enemy.TakeDamage(enemy.GetMaxHealth());
            }
        }
    }

    public float GetHealth()        => currentHealth;
    public float GetMaxHealth()     => maxHealth;
    public bool  IsDestroyed()      => isDestroyed;
    public float GetHealthPercent() => currentHealth / maxHealth;
}