using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 몬스터를 실제로 화면에 내보내는 곳(언제·무엇을 내보낼지는 WaveDirector가 정한다).
    /// 몬스터끼리 가로 간격이 UFO의 최소 점프 거리보다 좁아지지 않게 한다
    /// (밀집하면 너무 섬세한 조작을 요구하기 때문).
    /// </summary>
    public sealed class MonsterSpawner : MonoBehaviour
    {
        private const int PlacementTries = 10;

        private ComponentPool<Monster> pool;
        private readonly List<MonsterDefinition> candidates = new List<MonsterDefinition>();

        private GameConfig Config => PlayArea.Config;

        private void Awake()
        {
            MonsterRegistry.Active.Clear();
        }

        private void Start()
        {
            pool = new ComponentPool<Monster>(CreateMonster, 32);
        }

        private Monster CreateMonster()
        {
            var assets = GameAssets.Instance;
            var sr = assets.CreateSprite("Monster", null, SortOrder.Monster, transform, assets.flashMaterial);
            var monster = sr.gameObject.AddComponent<Monster>();
            monster.Setup(sr, m => pool.Release(m));
            return monster;
        }

        private void Update()
        {
            var session = GameSession.Instance;
            if (session == null || !session.IsPlaying) return;
            KeepApart(Time.deltaTime);
        }

        /// <summary>이번 웨이브에 나올 수 있는 일반 몬스터 중 하나를 비중대로 고른다.</summary>
        public MonsterDefinition PickMonster(int wave)
        {
            var all = GameAssets.Instance.monsters;
            if (all == null) return null;
            candidates.Clear();
            float total = 0f;
            foreach (var d in all)
            {
                if (d == null || d.sprite == null || d.kind != MonsterDefinition.Kind.Monster) continue;
                if (wave < d.unlockWave || d.spawnWeight <= 0f) continue;
                candidates.Add(d);
                total += d.spawnWeight;
            }
            if (candidates.Count == 0) return null;

            float r = Random.value * total;
            foreach (var d in candidates)
            {
                r -= d.spawnWeight;
                if (r <= 0f) return d;
            }
            return candidates[candidates.Count - 1];
        }

        private static float RadiusOf(MonsterDefinition def) => PlayArea.Config.PixelsToUnits(def.hitRadiusPx) * def.scale;

        /// <summary>
        /// 무리를 내보낸다. 무리 안 간격과, 위쪽에 이미 있는 몬스터와의 간격을 모두 최소 간격 이상으로.
        /// 맞는 자리가 없으면 false(조금 뒤 다시 시도).
        /// </summary>
        public bool TrySpawnGroup(MonsterDefinition def, float hpMul = 1f, float speedMul = 1f)
        {
            if (def == null || pool == null) return false;
            float radius = RadiusOf(def);
            float gap = Config.MonsterGap(radius, radius);
            float margin = Mathf.Max(Config.PixelsToUnits(Config.spawnSideMarginPx), radius);
            float usable = PlayArea.Size.x - margin * 2f;

            // 무리 안 간격: 원래 퍼짐 폭과 최소 간격 중 큰 쪽. 화면에 다 안 들어가면 수를 줄인다.
            int count = Mathf.Max(1, def.groupSize);
            float spacing = count > 1 ? Mathf.Max(Config.PixelsToUnits(def.groupSpreadPx) / (count - 1), gap) : 0f;
            while (count > 1 && spacing * (count - 1) > usable) count--;
            float halfGroup = spacing * (count - 1) * 0.5f;

            float minCenter = PlayArea.Left + margin + halfGroup;
            float maxCenter = PlayArea.Right - margin - halfGroup;

            float bestCenter = Random.Range(minCenter, maxCenter);
            float bestScore = float.MinValue;
            for (int i = 0; i < PlacementTries; i++)
            {
                float center = Random.Range(minCenter, maxCenter);
                float score = ClearanceScore(center, spacing, count, radius);
                if (score >= 0f) { bestCenter = center; bestScore = score; break; }
                if (score > bestScore) { bestScore = score; bestCenter = center; }
            }
            if (bestScore < 0f) return false;

            float topY = PlayArea.Top + radius + Config.PixelsToUnits(4f);
            float phase = Random.value * Mathf.PI * 2f; // 무리는 같은 박자로 흔들려 간격 유지
            for (int i = 0; i < count; i++)
            {
                float x = bestCenter - halfGroup + spacing * i;
                float y = topY + (count > 1 ? Random.Range(0f, Config.PixelsToUnits(10f)) : 0f);
                pool.Get().Spawn(def, new Vector2(x, y), phase, hpMul, speedMul);
            }
            return true;
        }

        /// <summary>보스처럼 정해진 자리에 하나를 내보낸다(간격 검사 없음).</summary>
        public Monster SpawnAt(MonsterDefinition def, Vector2 position, float hpMul = 1f, float speedMul = 1f)
        {
            if (def == null || pool == null) return null;
            var m = pool.Get();
            m.Spawn(def, position, 0f, hpMul, speedMul);
            return m;
        }

        /// <summary>
        /// 위쪽(등장 구역) 몬스터들과의 가로 여유. 0 이상이면 모두 최소 간격을 지킨다.
        /// </summary>
        private float ClearanceScore(float center, float spacing, int count, float radius)
        {
            float bandBottom = PlayArea.Top - Config.spawnCheckBandFraction * PlayArea.Size.y;
            float halfGroup = spacing * (count - 1) * 0.5f;
            float worst = float.MaxValue;
            var active = MonsterRegistry.Active;
            for (int m = 0; m < active.Count; m++)
            {
                var other = active[m];
                if (other.transform.position.y < bandBottom) continue;
                float need = Config.MonsterGap(radius, other.HitRadius);
                for (int i = 0; i < count; i++)
                {
                    float x = center - halfGroup + spacing * i;
                    worst = Mathf.Min(worst, Mathf.Abs(x - other.transform.position.x) - need);
                }
            }
            return worst == float.MaxValue ? 1f : worst;
        }

        /// <summary>
        /// 내려오는 중에 속도 차이로 가까워진 몬스터들을 옆으로 천천히 벌린다.
        /// 세로로 가까운(같은 높이 근처) 몬스터끼리만 본다. 보스는 밀리지 않고 다른 몬스터가 비켜난다.
        /// </summary>
        private void KeepApart(float dt)
        {
            var active = MonsterRegistry.Active;
            int n = active.Count;
            if (n < 2) return;
            float maxStep = Config.monsterSeparationSpeed * PlayArea.Size.x * dt;

            for (int i = 0; i < n; i++)
            {
                var a = active[i];
                for (int j = i + 1; j < n; j++)
                {
                    var b = active[j];
                    Vector2 pa = a.transform.position;
                    Vector2 pb = b.transform.position;
                    float need = Config.MonsterGap(a.HitRadius, b.HitRadius);
                    if (Mathf.Abs(pa.y - pb.y) > need * Config.monsterGapVerticalRange) continue;
                    float dx = pb.x - pa.x;
                    float overlap = need - Mathf.Abs(dx);
                    if (overlap <= 0f) continue;

                    float dir = dx != 0f ? Mathf.Sign(dx) : (Random.value < 0.5f ? -1f : 1f); // a→b 방향
                    float step = Mathf.Min(overlap * 0.5f, maxStep);

                    if (a.IsBoss) { b.Shift(dir * step * 2f); continue; }
                    if (b.IsBoss) { a.Shift(-dir * step * 2f); continue; }

                    // 둘이 반씩 벌어진다. 한쪽이 벽에 막혀 덜 움직였으면 다른 쪽이 그만큼 더 움직인다.
                    float beforeA = a.transform.position.x;
                    a.Shift(-dir * step);
                    float movedA = Mathf.Abs(a.transform.position.x - beforeA);
                    float beforeB = b.transform.position.x;
                    b.Shift(dir * (step + (step - movedA)));
                    float movedB = Mathf.Abs(b.transform.position.x - beforeB);
                    if (movedB < step) a.Shift(-dir * (step - movedB));
                }
            }
        }
    }
}
