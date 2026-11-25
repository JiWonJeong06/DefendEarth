using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 몬스터를 위에서 내려보낸다. 시간이 갈수록 자주 나온다.
    /// 몬스터끼리 가로 간격이 UFO의 최소 점프 거리보다 좁아지지 않게 한다
    /// (밀집하면 너무 섬세한 조작을 요구하기 때문).
    /// (등장 방식은 임시. 3번 묶음에서 웨이브·러시·보스로 바뀐다)
    /// </summary>
    public sealed class MonsterSpawner : MonoBehaviour
    {
        private const int PlacementTries = 10;

        private ComponentPool<Monster> pool;
        private float timer;
        private readonly List<MonsterDefinition> candidates = new List<MonsterDefinition>();
        private readonly List<float> groupXs = new List<float>(8);

        private GameConfig Config => PlayArea.Config;

        private void Start()
        {
            MonsterRegistry.Active.Clear();
            pool = new ComponentPool<Monster>(CreateMonster, 32);
            timer = Config.spawnIntervalStart * 0.5f; // 첫 몬스터는 조금 빨리
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

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            float k = Config.spawnRampSeconds > 0f ? Mathf.Clamp01(session.Elapsed / Config.spawnRampSeconds) : 1f;
            float interval = Mathf.Lerp(Config.spawnIntervalStart, Config.spawnIntervalMin, k);

            var def = PickDefinition(session.Elapsed);
            if (def != null && !TrySpawnGroup(def))
            {
                timer = 0.15f; // 자리가 없으면 잠깐 뒤에 다시 시도
                return;
            }
            timer += interval;
        }

        private MonsterDefinition PickDefinition(float elapsed)
        {
            var all = GameAssets.Instance.monsters;
            if (all == null) return null;
            candidates.Clear();
            float total = 0f;
            foreach (var d in all)
            {
                if (d == null || d.sprite == null || elapsed < d.unlockTime || d.spawnWeight <= 0f) continue;
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
        private bool TrySpawnGroup(MonsterDefinition def)
        {
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

            float topY = PlayArea.Top + Config.PixelsToUnits(16f);
            float phase = Random.value * Mathf.PI * 2f; // 무리는 같은 박자로 흔들려 간격 유지
            for (int i = 0; i < count; i++)
            {
                float x = bestCenter - halfGroup + spacing * i;
                float y = topY + (count > 1 ? Random.Range(0f, Config.PixelsToUnits(10f)) : 0f);
                pool.Get().Spawn(def, new Vector2(x, y), phase);
            }
            return true;
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
        /// 세로로 가까운(같은 높이 근처) 몬스터끼리만 본다.
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
                Vector2 pa = a.transform.position;
                for (int j = i + 1; j < n; j++)
                {
                    var b = active[j];
                    Vector2 pb = b.transform.position;
                    float need = Config.MonsterGap(a.HitRadius, b.HitRadius);
                    if (Mathf.Abs(pa.y - pb.y) > need * Config.monsterGapVerticalRange) continue;
                    float dx = pb.x - pa.x;
                    float overlap = need - Mathf.Abs(dx);
                    if (overlap <= 0f) continue;

                    float dir = dx != 0f ? Mathf.Sign(dx) : (Random.value < 0.5f ? -1f : 1f); // a→b 방향
                    float step = Mathf.Min(overlap * 0.5f, maxStep);

                    // 둘이 반씩 벌어진다. 한쪽이 벽에 막혀 덜 움직였으면 다른 쪽이 그만큼 더 움직인다.
                    float beforeA = a.transform.position.x;
                    a.Shift(-dir * step);
                    float movedA = Mathf.Abs(a.transform.position.x - beforeA);
                    float beforeB = b.transform.position.x;
                    b.Shift(dir * (step + (step - movedA)));
                    float movedB = Mathf.Abs(b.transform.position.x - beforeB);
                    if (movedB < step) a.Shift(-dir * (step - movedB));

                    pa = a.transform.position;
                }
            }
        }
    }
}
