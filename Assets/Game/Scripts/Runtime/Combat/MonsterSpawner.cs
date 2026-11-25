using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 몬스터를 위에서 내려보낸다. 시간이 갈수록 자주 나온다.
    /// (임시 방식. 3번 묶음에서 웨이브·러시·보스로 바뀐다)
    /// </summary>
    public sealed class MonsterSpawner : MonoBehaviour
    {
        private ComponentPool<Monster> pool;
        private float timer;
        private readonly List<MonsterDefinition> candidates = new List<MonsterDefinition>();

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

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            float k = Config.spawnRampSeconds > 0f ? Mathf.Clamp01(session.Elapsed / Config.spawnRampSeconds) : 1f;
            timer += Mathf.Lerp(Config.spawnIntervalStart, Config.spawnIntervalMin, k);

            var def = PickDefinition(session.Elapsed);
            if (def != null) SpawnGroup(def);
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

        private void SpawnGroup(MonsterDefinition def)
        {
            float margin = Config.PixelsToUnits(Config.spawnSideMarginPx);
            float spread = Config.PixelsToUnits(def.groupSpreadPx);
            int count = Mathf.Max(1, def.groupSize);
            float halfGroup = count > 1 ? spread * 0.5f : 0f;
            float centerX = Random.Range(PlayArea.Left + margin + halfGroup, PlayArea.Right - margin - halfGroup);
            float topY = PlayArea.Top + Config.PixelsToUnits(16f);

            for (int i = 0; i < count; i++)
            {
                Vector2 pos = new Vector2(centerX, topY);
                if (count > 1)
                {
                    pos.x += Mathf.Lerp(-halfGroup, halfGroup, i / (float)(count - 1));
                    pos.y += Random.Range(0f, Config.PixelsToUnits(18f)); // 살짝 흩어지게
                }
                pool.Get().Spawn(def, pos);
            }
        }
    }
}
