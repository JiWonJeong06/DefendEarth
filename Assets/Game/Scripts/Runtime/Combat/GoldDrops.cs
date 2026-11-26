using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 골드 동전. 운석을 부수거나 보스를 잡으면 동전이 튀어나왔다가 UFO로 빨려 들어간다(줍는 조작 없음).
    /// 닿는 순간 이번 판 골드에 더해진다. 판이 끝나면 날아가던 동전도 모두 더해 준다.
    /// </summary>
    public sealed class GoldDrops : MonoBehaviour
    {
        public enum Source { Meteor, Stage }

        private const int MaxCoinsPerBurst = 12;

        [SerializeField] private Transform target;

        private ComponentPool<Coin> pool;
        private readonly List<Coin> flying = new List<Coin>(64);

        private GameConfig Config => PlayArea.Config;

        private void Start()
        {
            pool = new ComponentPool<Coin>(() =>
            {
                var sr = GameAssets.Instance.CreateSprite("Coin", GameAssets.Instance.coinSprite, SortOrder.Effect - 1, transform);
                return sr.gameObject.AddComponent<Coin>();
            }, 24);
            Monster.Killed += OnKilled;
            if (GameSession.Instance != null) GameSession.Instance.GameOver += FlushAll;
        }

        private void OnDestroy()
        {
            Monster.Killed -= OnKilled;
            if (GameSession.Instance != null) GameSession.Instance.GameOver -= FlushAll;
        }

        private void OnKilled(Monster m)
        {
            if (m.Kind == MonsterDefinition.Kind.Meteor && m.Definition.gold > 0)
            {
                // 로비 강화(운석 골드)만큼 늘어난다
                int amount = Mathf.RoundToInt(m.Definition.gold * (1f + Upgrades.Total(UpgradeDefinition.Effect.MeteorGold)));
                Burst(m.transform.position, amount, Source.Meteor);
            }
        }

        /// <summary>amount 골드를 동전 여러 개로 나눠 튀긴다.</summary>
        public void Burst(Vector2 position, int amount, Source source)
        {
            if (amount <= 0 || pool == null) return;
            int coins = Mathf.Clamp(amount, 1, MaxCoinsPerBurst);
            int baseValue = amount / coins;
            int remainder = amount % coins;
            for (int i = 0; i < coins; i++)
            {
                int value = baseValue + (i < remainder ? 1 : 0);
                float angle = (i / (float)coins) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                float speed = Config.PixelsToUnits(Random.Range(40f, 70f));
                var coin = pool.Get();
                coin.Launch(this, position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + 0.4f) * speed, value, source);
                flying.Add(coin);
            }
        }

        internal Vector2 TargetPosition => target != null ? (Vector2)target.position : new Vector2(0f, PlayArea.Bottom);

        internal void Collect(Coin coin)
        {
            Deposit(coin);
            flying.Remove(coin);
            pool.Release(coin);
        }

        private static void Deposit(Coin coin)
        {
            var stats = GameSession.Instance != null ? GameSession.Instance.Stats : null;
            if (stats == null) return;
            if (coin.Source == Source.Meteor) stats.AddMeteorGold(coin.Value);
            else stats.AddStageGold(coin.Value);
        }

        private void FlushAll()
        {
            foreach (var coin in flying) Deposit(coin);
            flying.Clear();
        }

        public sealed class Coin : MonoBehaviour
        {
            private GoldDrops owner;
            private Vector2 velocity;
            private float age;
            public int Value { get; private set; }
            public Source Source { get; private set; }

            public void Launch(GoldDrops drops, Vector2 position, Vector2 initialVelocity, int value, Source source)
            {
                owner = drops;
                transform.position = position;
                velocity = initialVelocity;
                Value = value;
                Source = source;
                age = 0f;
                transform.localScale = Vector3.one;
            }

            private void Update()
            {
                float dt = Time.deltaTime;
                age += dt;
                var config = PlayArea.Config;
                Vector2 pos = transform.position;

                if (age < config.coinMagnetDelay)
                {
                    velocity *= Mathf.Exp(-6f * dt); // 튀어나왔다가 잠깐 멈칫
                }
                else
                {
                    // UFO가 빛줄기로 빨아들이듯 점점 빨라지며 날아간다
                    Vector2 to = owner.TargetPosition - pos;
                    float maxSpeed = config.coinMagnetSpeed * PlayArea.Size.y;
                    float t = Mathf.Clamp01((age - config.coinMagnetDelay) / 0.35f);
                    velocity = Vector2.Lerp(velocity, to.normalized * maxSpeed, t);
                    if (to.sqrMagnitude < 0.25f * 0.25f)
                    {
                        Effects.Hit(owner.TargetPosition, 0.5f, new Color(1f, 0.9f, 0.4f));
                        owner.Collect(this);
                        return;
                    }
                }
                transform.position = pos + velocity * dt;
            }
        }
    }
}
