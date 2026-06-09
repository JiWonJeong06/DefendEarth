using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Vector2 moveDirection = Vector2.down;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float atk = 5f;
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
        if (!isDead) Move();
    }

    private void Move()
    {
        rb.linearVelocity = moveDirection.normalized * moveSpeed;
    }

    /// <summary>
    /// 스포너에서 난이도 스탯 주입
    /// </summary>
    public void SetStats(float hp, float atkValue, int score)
    {
        maxHealth  = hp;
        currentHealth = hp;
        atk        = atkValue;
        scoreValue = score;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        GameManager.Instance?.AddScore(scoreValue);

        Destroy(gameObject);
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    public float GetHealth()    => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetAtk()       => atk;
    public bool  IsDead()       => isDead;
    public int   GetScoreValue() => scoreValue;
}