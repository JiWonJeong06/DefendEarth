using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>지금 화면에 살아 있는 몬스터·운석·보스 목록(박치기·연쇄 판정, 간격 유지용).</summary>
    public static class MonsterRegistry
    {
        public static readonly List<Monster> Active = new List<Monster>(128);
    }

    /// <summary>
    /// 몬스터 하나(재화 운석·보스도 같은 부품을 쓴다).
    /// 아래로 내려오다 지구선에 닿으면 지구 체력을 깎고 사라진다(운석은 피해 없이 사라짐).
    /// 맞으면 하얗게 번쩍이고, 체력이 0이 되면 터진다.
    /// </summary>
    public sealed class Monster : MonoBehaviour
    {
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private const float FlashTime = 0.08f;

        public MonsterDefinition Definition { get; private set; }
        public MonsterDefinition.Kind Kind => Definition.kind;
        public bool IsBoss => Definition != null && Definition.kind == MonsterDefinition.Kind.Boss;
        public bool IsAlive { get; private set; }
        public float HitRadius { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }

        /// <summary>처치됨. 골드·경험치·점수가 여기에 붙는다.</summary>
        public static event Action<Monster> Killed;
        /// <summary>지구선에 닿아 사라짐(처치 아님).</summary>
        public static event Action<Monster> Escaped;

        private SpriteRenderer sr;
        private MaterialPropertyBlock block;
        private Action<Monster> release;
        private float flashTimer;
        private float age;
        private float baseX;
        private float wobblePhase;
        private float speedMul = 1f;
        private int chargeState; // 0 접근, 1 멈칫, 2 돌진
        private float chargeTimer;
        private int patrolDir = 1;
        private bool patrolling;
        private Vector3 baseScale;
        private float patrolY;

        // 튕겨 나가는 중 (UFO에 받히거나 연쇄로 맞음)
        private bool knocked;
        private Vector2 knockVelocity;
        private int chainDamage;
        private int chainDepth;
        private readonly List<Monster> chainHit = new List<Monster>(4);
        private static readonly List<Monster> Scratch = new List<Monster>(64);

        /// <summary>튕겨 나가는 중이면 true(그동안은 내려오지 않는다).</summary>
        public bool IsKnocked => knocked;

        public void Setup(SpriteRenderer renderer, Action<Monster> onRelease)
        {
            sr = renderer;
            release = onRelease;
            block = new MaterialPropertyBlock();
        }

        /// <summary>
        /// 내보낸다. 무리는 같은 흔들림 박자(wobblePhase)를 써서 서로 가까워지지 않게 한다.
        /// hpMultiplier·speedMultiplier는 웨이브가 오를수록 커진다.
        /// </summary>
        public void Spawn(MonsterDefinition def, Vector2 position, float wobblePhase, float hpMultiplier = 1f, float speedMultiplier = 1f)
        {
            Definition = def;
            MaxHp = Mathf.Max(1, Mathf.RoundToInt(def.maxHp * hpMultiplier));
            Hp = MaxHp;
            speedMul = speedMultiplier;
            sr.sprite = def.sprite;
            sr.sortingOrder = def.kind == MonsterDefinition.Kind.Boss ? SortOrder.Monster + 2 : SortOrder.Monster;
            baseScale = Vector3.one * def.scale;
            transform.localScale = baseScale;
            transform.position = position;
            HitRadius = PlayArea.Config.PixelsToUnits(def.hitRadiusPx) * def.scale;
            baseX = position.x;
            this.wobblePhase = wobblePhase;
            age = 0f;
            chargeState = 0;
            chargeTimer = 0f;
            patrolling = false;
            knocked = false;
            knockVelocity = Vector2.zero;
            patrolDir = UnityEngine.Random.value < 0.5f ? -1 : 1;
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
            if (knocked)
            {
                UpdateKnock(ref p, dt);
            }
            else switch (Definition.pattern)
            {
                case MonsterDefinition.MovePattern.Charge:
                    p.y -= ChargeSpeed(height, dt) * speedMul * dt;
                    break;
                case MonsterDefinition.MovePattern.Patrol:
                    UpdatePatrol(ref p, dt);
                    break;
                default:
                    p.y -= Definition.speed * speedMul * height * dt;
                    break;
            }
            if (!knocked && Definition.wobbleAmplitudePx > 0f && chargeState != 2 && !patrolling)
                p.x = baseX + WobbleOffset();
            p.x = PlayArea.ClampX(p.x, HitRadius);
            transform.position = p;

            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                SetFlash(flashTimer > 0f ? 1f : 0f);
            }

            float earthLine = PlayArea.Bottom + config.PixelsToUnits(config.earthLinePx);
            if (p.y - (IsBoss ? HitRadius * 0.5f : 0f) <= earthLine) ReachEarth(p);
        }

        private float WobbleOffset()
        {
            if (Definition.wobbleAmplitudePx <= 0f) return 0f;
            float amp = PlayArea.Config.PixelsToUnits(Definition.wobbleAmplitudePx);
            return Mathf.Sin(age * Definition.wobbleFrequency * Mathf.PI * 2f + wobblePhase) * amp;
        }

        /// <summary>
        /// 튕겨 낸다(UFO 몸통에 받히거나 연쇄로 맞았을 때). velocity는 유닛/초, 몬스터의 knockbackScale이 곱해진다.
        /// 날아가는 동안 다른 몬스터와 부딪히면 damage만큼 피해를 주고 그 몬스터도 튕긴다(depth 단계까지).
        /// </summary>
        public void Knock(Vector2 velocity, int damage, int depth)
        {
            if (!IsAlive) return;
            knockVelocity = velocity * Definition.knockbackScale;
            chainDamage = damage;
            chainDepth = depth;
            chainHit.Clear();
            knocked = knockVelocity.magnitude > PlayArea.Config.PixelsToUnits(PlayArea.Config.knockStopSpeedPx);
            if (Definition.pattern == MonsterDefinition.MovePattern.Charge && chargeState != 0)
            {
                chargeState = 0; // 처음부터 다시 다가와서 멈칫·돌진
                transform.localScale = baseScale;
            }
        }

        private void UpdateKnock(ref Vector3 p, float dt)
        {
            var config = PlayArea.Config;
            p += (Vector3)(knockVelocity * dt);
            knockVelocity *= Mathf.Exp(-config.knockDrag * dt);

            // 벽·천장에서 튕김
            if (p.x < PlayArea.Left + HitRadius) { p.x = PlayArea.Left + HitRadius; knockVelocity.x = Mathf.Abs(knockVelocity.x) * 0.6f; }
            if (p.x > PlayArea.Right - HitRadius) { p.x = PlayArea.Right - HitRadius; knockVelocity.x = -Mathf.Abs(knockVelocity.x) * 0.6f; }
            if (p.y > PlayArea.Top - HitRadius) { p.y = PlayArea.Top - HitRadius; knockVelocity.y = -Mathf.Abs(knockVelocity.y) * 0.5f; }

            if (chainDepth > 0 && chainDamage > 0) HitOthers(p);

            if (knockVelocity.magnitude <= config.PixelsToUnits(config.knockStopSpeedPx))
            {
                knocked = false;
                baseX = p.x - WobbleOffset(); // 흔들림이 튀지 않게 이어 붙인다
            }
        }

        /// <summary>연쇄: 날아가다 다른 몬스터와 부딪히면 피해를 주고 그 몬스터도 튕긴다.</summary>
        private void HitOthers(Vector3 p)
        {
            Scratch.Clear();
            Scratch.AddRange(MonsterRegistry.Active);
            for (int i = 0; i < Scratch.Count; i++)
            {
                var o = Scratch[i];
                if (o == this || !o.IsAlive || chainHit.Contains(o)) continue;
                Vector2 d = o.transform.position - p;
                float r = HitRadius + o.HitRadius;
                if (d.sqrMagnitude > r * r) continue;

                chainHit.Add(o);
                o.chainHit.Add(this); // 되받아치지 않게
                Vector2 dir = d.sqrMagnitude > 1e-6f ? d.normalized : Vector2.up;
                if (dir.y < 0.2f) { dir.y = 0.2f; dir.Normalize(); }
                float speed = knockVelocity.magnitude * PlayArea.Config.chainKnockRatio;
                o.TakeHit(chainDamage, (Vector2)p + d * 0.5f);
                if (o.IsAlive) o.Knock(dir * speed, chainDamage, chainDepth - 1);
                knockVelocity *= 0.7f; // 부딪힌 쪽은 조금 느려진다
            }
        }

        private void ReachEarth(Vector3 p)
        {
            if (Definition.kind == MonsterDefinition.Kind.Meteor)
            {
                // 운석은 그냥 지나간다: 지구 피해 없음, 골드만 놓침
                Effects.Hit(p, 0.9f, new Color(0.7f, 0.7f, 0.8f, 0.8f));
            }
            else
            {
                var session = GameSession.Instance;
                if (session != null && session.Earth != null) session.Earth.TakeDamage(Definition.earthDamage);
                Effects.Hit(p, IsBoss ? 3f : 1.4f, new Color(1f, 0.45f, 0.35f));
            }
            Escaped?.Invoke(this);
            Despawn();
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

        private void UpdatePatrol(ref Vector3 p, float dt)
        {
            float startY = PlayArea.Top - Definition.patrolStartFraction * PlayArea.Size.y;
            if (!patrolling)
            {
                p.y -= Definition.speed * speedMul * PlayArea.Size.y * dt;
                if (p.y <= startY) { patrolling = true; patrolY = p.y; }
                return;
            }
            // 오가는 높이(patrolY)는 아주 천천히 내려온다. 튕겨 올라갔으면 그 높이까지 다시 내려온다
            patrolY -= Definition.patrolDescendSpeed * speedMul * PlayArea.Size.y * dt;
            p.y = p.y > patrolY ? Mathf.MoveTowards(p.y, patrolY, Definition.speed * PlayArea.Size.y * dt) : patrolY;
            p.x += patrolDir * Definition.patrolSpeed * PlayArea.Size.x * dt;
            float limit = PlayArea.Right - HitRadius;
            if (p.x > limit) { p.x = limit; patrolDir = -1; }
            if (p.x < -limit) { p.x = -limit; patrolDir = 1; }
            baseX = p.x;
            // 둥실둥실
            float bob = Mathf.Sin(age * 3f) * 0.04f;
            transform.localScale = new Vector3(baseScale.x * (1f + bob), baseScale.y * (1f - bob), 1f);
        }

        /// <summary>너무 가까운 몬스터와 벌어지도록 옆으로 민다.</summary>
        public void Shift(float dx)
        {
            if (!IsAlive || dx == 0f) return;
            baseX = PlayArea.ClampX(baseX + dx, HitRadius);
            var p = transform.position;
            p.x = PlayArea.ClampX(p.x + dx, HitRadius);
            transform.position = p;
        }

        public void TakeHit(int damage, Vector2 hitPoint)
        {
            if (!IsAlive) return;
            Hp -= damage;
            flashTimer = FlashTime;
            SetFlash(1f);
            Effects.Hit(hitPoint, 0.8f);
            if (Hp > 0) return;

            Color burst = Definition.kind == MonsterDefinition.Kind.Meteor ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.9f, 0.5f);
            Effects.Hit(transform.position, (IsBoss ? 3.5f : 1.6f) * Definition.scale, burst);
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
