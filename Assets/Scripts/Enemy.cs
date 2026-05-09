using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Vector2 moveDirection = Vector2.down;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float atk = 5f; // 공격력
    [SerializeField] private int scoreValue = 100;
    
    private Rigidbody2D rb;
    private float currentHealth;
    private bool isDead = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (!isDead)
        {
            Move();
        }
    }

    private void Move()
    {
        rb.linearVelocity = moveDirection.normalized * moveSpeed;
    }

    /// <summary>
    /// 적이 피해를 입음
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 적 사망
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // 스코어 추가 (GameManager가 있으면)
        // GameManager.Instance.AddScore(scoreValue);
        
        // 적 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 화면 밖으로 나가면 제거
    /// </summary>
    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetAtk() => atk;
    public bool IsDead() => isDead;
    public int GetScoreValue() => scoreValue;
}