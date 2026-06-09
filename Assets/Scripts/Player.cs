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
    [SerializeField] private Transform  shootPoint;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Level")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int maxLevel = 100;

    [Header("Base Stats (Lv1)")]
    [SerializeField] private float baseAtk        = 5f;
    [SerializeField] private float baseBulletSpeed = 8f;
    [SerializeField] private float baseFireRate    = 0.85f;

    [Header("Growth Rate Per Level")]
    [SerializeField] private float atkGrowth         = 0.03f;
    [SerializeField] private float bulletSpeedGrowth = 0.0105f;
    [SerializeField] private float fireRateGrowth    = 0.0125f;

    [Header("Player Sprites (20개, 5레벨마다)")]
    [SerializeField] private Sprite[] playerSprites; // 인덱스 0 = Lv1~5, 1 = Lv6~10 ...

    [Header("Bullet Sprites (5개, 20레벨마다)")]
    [SerializeField] private Sprite[] bulletSprites; // 인덱스 0 = Lv1~19, 1 = Lv20~39 ...

    private Rigidbody2D  rb;
    private SpriteRenderer spriteRenderer;
    private float shootTimer = 0f;

    private float currentAtk;
    private float currentBulletSpeed;
    private float currentFireRate;
    private float currentShootInterval;

    // 현재 적용된 스프라이트 인덱스 (변경 감지용)
    private int lastPlayerSpriteIndex = -1;
    private int lastBulletSpriteIndex = -1;

    private void Start()
    {
        rb             = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (joyStick == null)
            joyStick = FindFirstObjectByType<JoyStick>();

        if (joyStick == null)
            Debug.LogError("JoyStick을 찾을 수 없습니다!");

        shootTimer = 0f;
        UpdateStats();
        UpdateSprites();
    }

    private void FixedUpdate()
    {
        float moveInput = joyStick.GetHorizontalInput() * inputSensitivity;
        MovePlayer(moveInput);
    }

    private void Update()
    {
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            Shoot();
            shootTimer = currentShootInterval;
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
        if (bulletPrefab == null) return;

        Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // 총알 스프라이트 적용
        int bulletIndex = GetBulletSpriteIndex();
        if (bulletSprites != null && bulletIndex < bulletSprites.Length)
        {
            SpriteRenderer bulletSr = bullet.GetComponent<SpriteRenderer>();
            if (bulletSr != null)
                bulletSr.sprite = bulletSprites[bulletIndex];
        }

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.Initialize(Vector2.up, currentBulletSpeed, currentAtk);
    }

    public void LevelUp()
    {
        if (currentLevel >= maxLevel) return;

        currentLevel++;
        UpdateStats();
        UpdateSprites();

        Debug.Log($"[Player] Lv.{currentLevel} | ATK:{currentAtk:F2} | Speed:{currentBulletSpeed:F2} | Interval:{currentShootInterval:F3}s");
    }

    private void UpdateStats()
    {
        int n = currentLevel - 1;
        currentAtk           = baseAtk          * Mathf.Pow(1f + atkGrowth,         n);
        currentBulletSpeed   = baseBulletSpeed  * Mathf.Pow(1f + bulletSpeedGrowth, n);
        currentFireRate      = baseFireRate      * Mathf.Pow(1f + fireRateGrowth,    n);
        currentShootInterval = 1f / currentFireRate;
    }

    private void UpdateSprites()
    {
        UpdatePlayerSprite();
        // 총알은 Shoot() 시점에 적용되므로 별도 호출 불필요
    }

    private void UpdatePlayerSprite()
    {
        if (playerSprites == null || playerSprites.Length == 0) return;

        int index = GetPlayerSpriteIndex();
        if (index == lastPlayerSpriteIndex) return; // 변경 없으면 스킵

        lastPlayerSpriteIndex = index;

        if (spriteRenderer != null)
            spriteRenderer.sprite = playerSprites[index];

        Debug.Log($"[Player] 스프라이트 변경 → 인덱스 {index} (Lv.{currentLevel})");
    }

    // Lv1~5 → 0, Lv6~10 → 1, ... Lv96~100 → 19
    private int GetPlayerSpriteIndex()
    {
        int index = (currentLevel - 1) / 5;
        return Mathf.Clamp(index, 0, playerSprites.Length - 1);
    }

    // Lv1~19 → 0, Lv20~39 → 1, ... Lv80~100 → 4
    private int GetBulletSpriteIndex()
    {
        int index = (currentLevel - 1) / 20;
        return Mathf.Clamp(index, 0, bulletSprites != null ? bulletSprites.Length - 1 : 0);
    }

    public int   GetCurrentLevel()       => currentLevel;
    public float GetCurrentAtk()         => currentAtk;
    public float GetCurrentBulletSpeed() => currentBulletSpeed;
    public float GetCurrentFireRate()    => currentFireRate;
    public float GetShootInterval()      => currentShootInterval;
    public Vector3 GetPosition()         => transform.position;
}