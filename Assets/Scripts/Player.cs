using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float inputSensitivity = 0.5f;
    
    [Header("Boundary")]
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    
    [Header("Input")]
    [SerializeField] private JoyStick joyStick;
    
    [Header("Shooting")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootInterval = 0.2f;
    [SerializeField] private BulletPool bulletPool;
    
    [Header("Level & Stats")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int maxLevel = 100;
    [SerializeField] private float baseSpeed = 10f;
    [SerializeField] private float baseAtk = 1f;
    
    private Rigidbody2D rb;
    private float shootTimer = 0f;
    private float currentSpeed;
    private float currentAtk;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (joyStick == null)
            joyStick = FindFirstObjectByType<JoyStick>();
        
        if (joyStick == null)
            Debug.LogError("JoyStick을 찾을 수 없습니다!");
        
        if (bulletPool == null)
            bulletPool = GetComponentInChildren<BulletPool>();
        
        if (bulletPool == null)
            Debug.LogError("BulletPool을 찾을 수 없습니다!");
        
        shootTimer = 0f;
        UpdateStats();
    }

    private void FixedUpdate()
    {
        float moveInput = joyStick.GetHorizontalInput();
        moveInput *= inputSensitivity;
        MovePlayer(moveInput);
    }

    private void Update()
    {
        shootTimer -= Time.deltaTime;
        
        if (shootTimer <= 0)
        {
            Shoot();
            shootTimer = shootInterval;
        }
    }

    private void MovePlayer(float moveInput)
    {
        Vector2 newPos = rb.position + new Vector2(moveInput * moveSpeed * Time.fixedDeltaTime, 0f);
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        rb.position = newPos;
    }

    private void Shoot()
    {
        if (bulletPool == null)
            return;

        Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position;
        GameObject bullet = bulletPool.GetBullet(spawnPos);
        
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(bulletPool, Vector2.up, currentSpeed, currentAtk);
        }
    }

    /// <summary>
    /// 레벨업 (1~100)
    /// </summary>
    public void LevelUp()
    {
        if (currentLevel < maxLevel)
        {
            currentLevel++;
            UpdateStats();
            Debug.Log($"[Player] Level Up! 현재 레벨: {currentLevel}");
        }
    }

    /// <summary>
    /// 스탯 업데이트 (레벨에 따라 자동 계산)
    /// </summary>
    private void UpdateStats()
    {
        // 레벨에 따른 속도 증가 (레벨당 5% 증가)
        currentSpeed = baseSpeed * (1f + (currentLevel - 1) * 0.05f);
        
        // 레벨에 따른 공격력 증가 (레벨당 10% 증가)
        currentAtk = baseAtk * (1f + (currentLevel - 1) * 0.1f);
    }

    public int GetCurrentLevel() => currentLevel;
    public float GetCurrentSpeed() => currentSpeed;
    public float GetCurrentAtk() => currentAtk;

    public Vector3 GetShootPosition()
    {
        return transform.position;
    }

    public Vector2 GetShootDirection()
    {
        return Vector2.up;
    }

    public Vector3 GetPosition() => transform.position;
}