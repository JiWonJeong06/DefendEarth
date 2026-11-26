using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// UFO의 자동 발사 무기(기본형: 직선탄). 판 안 강화 카드가 아래 값을 바꾼다.
    /// </summary>
    public sealed class Weapon : MonoBehaviour
    {
        [SerializeField] private SquashStretch visual;

        // 판 안 강화로 바뀌는 값 (판 시작 때 GameConfig 값으로 초기화)
        public int Damage { get; set; }
        public float FireInterval { get; set; }
        public int ShotCount { get; set; } = 1;
        public int Pierce { get; set; }

        private ComponentPool<Bullet> pool;
        private float timer;

        private GameConfig Config => PlayArea.Config;

        private void Start()
        {
            Damage = Config.bulletDamage;
            // 로비 강화(연사)만큼 빨라진다
            FireInterval = Config.fireInterval * Mathf.Clamp(1f - Upgrades.Total(UpgradeDefinition.Effect.FireRate), 0.3f, 1f);
            ShotCount = 1;
            Pierce = 0;

            var holder = new GameObject("Bullets").transform;
            pool = new ComponentPool<Bullet>(() =>
            {
                var sr = GameAssets.Instance.CreateSprite("Bullet", GameAssets.Instance.bulletSprite, SortOrder.Bullet, holder);
                return sr.gameObject.AddComponent<Bullet>();
            }, 24);
        }

        private void Update()
        {
            var session = GameSession.Instance;
            if (session != null && !session.IsPlaying) return;

            timer += Time.deltaTime;
            float interval = Mathf.Max(0.05f, FireInterval);
            if (timer < interval) return;
            timer -= interval;
            Fire();
        }

        private void Fire()
        {
            Vector2 muzzle = (Vector2)transform.position + new Vector2(0f, Config.PixelsToUnits(Config.muzzleOffsetPx));
            float speed = Config.bulletSpeedFraction * PlayArea.Size.y;
            float spacing = Config.PixelsToUnits(Config.multiShotSpacingPx);
            float radius = Config.PixelsToUnits(Config.bulletHitRadiusPx);
            int count = Mathf.Max(1, ShotCount);

            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * spacing;
                pool.Get().Launch(this, muzzle + new Vector2(offset, 0f), new Vector2(0f, speed), Damage, radius, Pierce);
            }
            if (visual != null) visual.Nudge(-Config.squashOnFire);
        }

        public void ReleaseBullet(Bullet bullet) => pool.Release(bullet);
    }
}
