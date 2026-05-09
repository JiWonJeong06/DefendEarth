using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 10f;
    
    [Header("Stats")]
    private float bulletSpeed = 10f;
    private float atk = 1f;
    private Vector2 direction = Vector2.up;
    
    private Rigidbody2D rb;
    private float lifeTimer;
    private BulletPool bulletPool;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        lifeTimer = maxLifetime;
        
        if (rb != null)
            rb.linearVelocity = direction * bulletSpeed;
    }

    private void FixedUpdate()
    {
        lifeTimer -= Time.fixedDeltaTime;
        
        if (lifeTimer <= 0)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // 적에게 피해를 입힘
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)atk);
            }
            
            ReturnToPool();
        }
    }

    public void Initialize(BulletPool pool, Vector2 newDirection, float newSpeed, float newAtk)
    {
        bulletPool = pool;
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

    public void SetAtk(float newAtk)
    {
        atk = newAtk;
    }

    public float GetAtk() => atk;

    private void ReturnToPool()
    {
        if (bulletPool != null)
        {
            bulletPool.ReturnBullet(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}