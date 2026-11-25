using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>지금 화면에 살아 있는 몬스터 목록(탄 판정용).</summary>
    public static class MonsterRegistry
    {
        public static readonly List<Monster> Active = new List<Monster>(128);
    }

    /// <summary>
    /// 몬스터 하나. 아래로 내려오다 지구선에 닿으면 지구 체력을 깎고 사라진다.
    /// 맞으면 하얗게 번쩍이고, 체력이 0이 되면 터진다.
    /// </summary>
    public sealed class Monster : MonoBehaviour
    {
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private const float FlashTime = 0.08f;

        public MonsterDefinition Definition { get; private set; }
        public bool IsAlive { get; private set; }
        public float HitRadius { get; private set; }

        /// <summary>처치됨(위치). 4번 묶음에서 경험치·점수에 쓴다.</summary>
        public static event Action<Monster> Killed;

        private SpriteRenderer sr;
        private MaterialPropertyBlock block;
        private Action<Monster> release;
        private int hp;
        private float flashTimer;
        private float age;
        private float baseX;
        private float wobblePhase;
        private int chargeState; // 0 접근, 1 멈칫, 2 돌진
        private float chargeTimer;
        private Vector3 baseScale;

        public void Setup(SpriteRenderer renderer, Action<Monster> onRelease)
        {
            sr = renderer;
            release = onRelease;
            block = new MaterialPropertyBlock();
        }

        public void Spawn(MonsterDefinition def, Vector2 position)
        {
            Definition = def;
            hp = Mathf.Max(1, def.maxHp);
            sr.sprite = def.sprite;
            baseScale = Vector3.one * def.scale;
            transform.localScale = baseScale;
            transform.position = position;
            HitRadius = PlayArea.Config.PixelsToUnits(def.hitRadiusPx) * def.scale;
            baseX = position.x;
            wobblePhase = UnityEngine.Random.value * Mathf.PI * 2f;
            age = 0f;
            chargeState = 0;
            chargeTimer = 0f;
            SetFlash(0f);
            flashTimer = 0f;
            IsAlive = true;
            MonsterRegistry.Active.Add(this);
        }

        private void Update()
        {
            if (!IsAlive) return;
            float dt = Time.deltaTime;
            age += dt;
            var config = PlayArea.Config;
            float height = PlayArea.Size.y;

            Vector3 p = transform.position;
            switch (Definition.pattern)
            {
                case MonsterDefinition.MovePattern.Charge:
                    p.y -= ChargeSpeed(height, dt) * dt;
                    break;
                default:
                    p.y -= Definition.speed * height * dt;
                    break;
            }
            if (Definition.wobbleAmplitudePx > 0f && chargeState != 2)
            {
                float amp = config.PixelsToUnits(Definition.wobbleAmplitudePx);
                p.x = baseX + Mathf.Sin(age * Definition.wobbleFrequency * Mathf.PI * 2f + wobblePhase) * amp;
            }
            p.x = PlayArea.ClampX(p.x, HitRadius * 0.5f);
            transform.position = p;

            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                SetFlash(flashTimer > 0f ? 1f : 0f);
            }

            float earthLine = PlayArea.Bottom + config.PixelsToUnits(config.earthLinePx);
            if (p.y <= earthLine)
            {
                var session = GameSession.Instance;
                if (session != null && session.Earth != null) session.Earth.TakeDamage(Definition.earthDamage);
                Effects.Hit(p, 1.4f, new Color(1f, 0.45f, 0.35f));
                Despawn();
            }
        }

        private float ChargeSpeed(float height, float dt)
        {
            switch (chargeState)
            {
                case 0:
                    float stopY = PlayArea.Top - Definition.chargeTriggerFraction * height;
                    if (transform.position.y <= stopY) { chargeState = 1; chargeTimer = 0f; }
                    return Definition.speed * height;
                case 1:
                    chargeTimer += dt;
                    // 멈칫하는 동안 몸을 움츠렸다가(예고) 돌진
                    float k = Mathf.Clamp01(chargeTimer / Mathf.Max(0.01f, Definition.chargeWindup));
                    transform.localScale = new Vector3(baseScale.x * (1f + 0.2f * k), baseScale.y * (1f - 0.2f * k), 1f);
                    if (k >= 1f) { chargeState = 2; transform.localScale = new Vector3(baseScale.x * 0.85f, baseScale.y * 1.2f, 1f); }
                    return 0f;
                default:
                    return Definition.chargeSpeed * height;
            }
        }

        public void TakeHit(int damage, Vector2 hitPoint)
        {
            if (!IsAlive) return;
            hp -= damage;
            flashTimer = FlashTime;
            SetFlash(1f);
            Effects.Hit(hitPoint, 0.8f);
            if (hp > 0) return;

            Effects.Hit(transform.position, 1.6f * Definition.scale, new Color(1f, 0.9f, 0.5f));
            Killed?.Invoke(this);
            Despawn();
        }

        private void Despawn()
        {
            if (!IsAlive) return;
            IsAlive = false;
            MonsterRegistry.Active.Remove(this);
            release(this);
        }

        private void SetFlash(float amount)
        {
            sr.GetPropertyBlock(block);
            block.SetFloat(FlashId, amount);
            sr.SetPropertyBlock(block);
        }

        private void OnDisable()
        {
            if (IsAlive)
            {
                IsAlive = false;
                MonsterRegistry.Active.Remove(this);
            }
        }
    }
}
