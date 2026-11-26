using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 판 안 성장. 몬스터를 잡으면 경험치가 쌓이고, 레벨업하면 게임을 멈추고 카드 3장을 보여 준다.
    /// 고른 카드는 UFO·지구에 바로 적용된다. 판이 끝나면 모두 초기화(장면을 다시 불러오므로).
    /// </summary>
    public sealed class LevelSystem : MonoBehaviour
    {
        public const int ChoiceCount = 3;

        [SerializeField] private UfoController ufo;
        [SerializeField] private SquashStretch ufoVisual;

        public int Level { get; private set; } = 1;
        public float Xp { get; private set; }
        public float XpToNext => NeedFor(Level);

        /// <summary>경험치가 바뀜 (현재, 다음 레벨까지 필요).</summary>
        public event Action<float, float> XpChanged;
        /// <summary>레벨업해서 카드를 골라야 함 (새 레벨, 보여 줄 카드들).</summary>
        public event Action<int, IReadOnlyList<CardDefinition>> ChoiceRequested;
        /// <summary>카드를 골랐음 (카드, 그 카드를 고른 횟수).</summary>
        public event Action<CardDefinition, int> CardPicked;

        private readonly Dictionary<CardDefinition, int> stacks = new Dictionary<CardDefinition, int>();
        private readonly List<CardDefinition> offer = new List<CardDefinition>(ChoiceCount);
        private readonly List<CardDefinition> pool = new List<CardDefinition>();
        private int pendingLevelUps;
        private bool choosing;

        private GameConfig Config => PlayArea.Config;

        private void OnEnable() => Monster.Killed += OnKilled;
        private void OnDisable() => Monster.Killed -= OnKilled;

        private void Start() => XpChanged?.Invoke(Xp, XpToNext);

        public float NeedFor(int level)
        {
            int n = Mathf.Max(0, level - 1);
            return Mathf.Max(1f, Mathf.Round(Config.xpBase + Config.xpStep * n + Config.xpQuadratic * n * n));
        }

        public int StacksOf(CardDefinition card) => card != null && stacks.TryGetValue(card, out int s) ? s : 0;

        private void OnKilled(Monster m)
        {
            if (m.Definition.exp <= 0) return;
            var session = GameSession.Instance;
            if (session != null && !session.IsPlaying) return;
            AddXp(m.Definition.exp);
        }

        public void AddXp(float amount)
        {
            Xp += amount;
            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                pendingLevelUps++;
            }
            XpChanged?.Invoke(Xp, XpToNext);
            if (pendingLevelUps > 0 && !choosing) OpenChoice();
        }

        private void OpenChoice()
        {
            BuildOffer();
            if (offer.Count == 0) { pendingLevelUps = 0; return; }

            pendingLevelUps--;
            choosing = true;
            if (ufoVisual != null) ufoVisual.Punch(-Config.squashOnLevelUp); // 레벨업: 크게 통!
            Effects.Hit(ufo != null ? (Vector2)ufo.transform.position : Vector2.zero, 2.5f, new Color(0.6f, 1f, 0.7f));
            GameSession.Instance?.Pause();
            ChoiceRequested?.Invoke(Level, offer);
        }

        /// <summary>최대 단계가 아닌 카드 중에서 겹치지 않게 비중대로 3장.</summary>
        private void BuildOffer()
        {
            offer.Clear();
            pool.Clear();
            var all = GameAssets.Instance.cards;
            if (all == null) return;
            foreach (var c in all)
            {
                if (c == null || c.weight <= 0f) continue;
                if (c.maxStacks > 0 && StacksOf(c) >= c.maxStacks) continue;
                pool.Add(c);
            }
            while (offer.Count < ChoiceCount && pool.Count > 0)
            {
                float total = 0f;
                foreach (var c in pool) total += c.weight;
                float r = UnityEngine.Random.value * total;
                int pick = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    r -= pool[i].weight;
                    if (r <= 0f) { pick = i; break; }
                }
                offer.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
        }

        /// <summary>카드 화면에서 하나를 고르면 부른다.</summary>
        public void Pick(CardDefinition card)
        {
            if (!choosing || card == null) return;
            choosing = false;
            int count = StacksOf(card) + 1;
            stacks[card] = count;
            Apply(card);
            CardPicked?.Invoke(card, count);
            GameSession.Instance?.Resume();

            if (pendingLevelUps > 0) OpenChoice(); // 한 번에 여러 레벨이 올랐으면 이어서
        }

        private void Apply(CardDefinition card)
        {
            switch (card.effect)
            {
                case CardDefinition.Effect.DashPower:
                    if (ufo != null) ufo.DamageBonus += Mathf.RoundToInt(card.value);
                    break;
                case CardDefinition.Effect.DashRange:
                    if (ufo != null) ufo.RangeMultiplier += card.value;
                    break;
                case CardDefinition.Effect.BodySize:
                    if (ufo != null) ufo.BodyScale += card.value;
                    break;
                case CardDefinition.Effect.ChainDamage:
                    if (ufo != null) ufo.ChainDamageBonus += Mathf.RoundToInt(card.value);
                    break;
                case CardDefinition.Effect.HealEarth:
                    GameSession.Instance?.Earth?.Heal(Mathf.RoundToInt(card.value));
                    break;
            }
        }
    }
}
