using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 10f;
    
    [Header("Stats")]
    public float bulletSpeed = 10f;
    public float atk = 1f;
    private Vector2 direction = Vector2.up;
    
    private Rigidbody2D rb;
    private float lifeTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lifeTimer = maxLifetime;
        rb.linearVelocity = direction * bulletSpeed;
    }

    private void FixedUpdate()
    {
        lifeTimer -= Time.fixedDeltaTime;
        
        if (lifeTimer <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)atk);
            }
            
            Destroy(gameObject);
        }
    }

    public void Initialize(Vector2 newDirection, float newSpeed, float newAtk)
    {
        direction = newDirection.normalized;
        bulletSpeed = newSpeed;
        atk = newAtk;
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        if (rb != null)
            rb.linearVelocity = direction * bulletSpeed;
    }

    public void SetSpeed(float newSpeed)
    {
        bulletSpeed = newSpeed;
        if (rb != null)
            rb.linearVelocity = direction * bulletSpeed;
    }

    public void SetAtk(float newAtk) => atk = newAtk;
    public float GetAtk() => atk;
}