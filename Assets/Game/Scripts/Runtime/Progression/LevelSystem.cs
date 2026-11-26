using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 판 안 증강(능력치 카드) 관리. 11/27부터 경험치·레벨업은 없고, 웨이브가 끝날 때 오는 상점 우주선에서
    /// 다이아로 산다(ShopUI). 산 증강은 UFO·지구에 바로 적용되고, 판이 끝나면 모두 초기화(장면을 다시 불러오므로).
    /// (클래스 이름은 옛 "레벨 시스템" 그대로: 씬·장면 만들기에서 이 이름으로 연결돼 있다)
    /// </summary>
    public sealed class LevelSystem : MonoBehaviour
    {
        [SerializeField] private UfoController ufo;
        [SerializeField] private SquashStretch ufoVisual;

        /// <summary>증강을 샀음 (증강, 산 횟수).</summary>
        public event Action<CardDefinition, int> CardPicked;

        private readonly Dictionary<CardDefinition, int> stacks = new Dictionary<CardDefinition, int>();
        private readonly List<CardDefinition> pool = new List<CardDefinition>();

        private GameConfig Config => PlayArea.Config;

        public int StacksOf(CardDefinition card) => card != null && stacks.TryGetValue(card, out int s) ? s : 0;

        public bool IsMaxed(CardDefinition card) => card != null && card.maxStacks > 0 && StacksOf(card) >= card.maxStacks;

        /// <summary>
        /// 상점 진열: 최대 단계가 아니고 가격이 maxPrice(가진 다이아) 이하인 증강 중에서 겹치지 않게 비중대로 count개.
        /// (11/27 사용자 결정: 가진 다이아보다 비싼 증강은 나오지 않는다)
        /// </summary>
        public void BuildOffer(List<CardDefinition> offer, int count, int maxPrice)
        {
            offer.Clear();
            pool.Clear();
            var all = GameAssets.Instance.cards;
            if (all == null) return;
            foreach (var c in all)
            {
                if (c == null || c.weight <= 0f || IsMaxed(c)) continue;
                if (c.price > maxPrice) continue;
                pool.Add(c);
            }
            while (offer.Count < count && pool.Count > 0)
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

        /// <summary>상점에서 산 증강을 적용한다(다이아는 부르는 쪽에서 뺀다).</summary>
        public void Add(CardDefinition card)
        {
            if (card == null) return;
            int count = StacksOf(card) + 1;
            stacks[card] = count;
            Apply(card);
            if (ufoVisual != null) ufoVisual.Punch(-Config.squashOnLevelUp); // 강해졌다: 크게 통!
            Sound.Play(Sfx.LevelUp);
            CardPicked?.Invoke(card, count);
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
