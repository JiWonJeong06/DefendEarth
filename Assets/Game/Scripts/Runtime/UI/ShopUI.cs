using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 상점 우주선에 닿으면 열리는 증강 상점(11/27). 게임은 멈춘다.
    /// - 증강 3개 진열. 가진 다이아보다 비싼 증강은 진열되지 않는다.
    /// - 다이아가 되는 만큼 여러 개 살 수 있다(같은 진열품은 한 번).
    /// - 새로고침: 다이아 1부터, 누를 때마다 +1(상점마다 1부터 다시). 새로 진열할 때도 남은 다이아 이하만.
    /// - "다음에 사기"로 닫는다. 다이아는 판 안에서만 쓰는 재화(유료 아님).
    /// </summary>
    public sealed class ShopUI : ModalPanel
    {
        private const int SlotCount = 3;
        private const float CardW = 52f;
        private const float CardH = 86f;
        private const float CardY = 22f;

        [SerializeField] private LevelSystem levels;

        private sealed class Slot
        {
            public Transform root;
            public SpriteRenderer frame;
            public SpriteRenderer icon;
            public TextMeshPro name;
            public TextMeshPro desc;
            public readonly List<SpriteRenderer> pips = new List<SpriteRenderer>();
            public PixelButton buy;
            public TextMeshPro priceLabel;
            public SpriteRenderer priceGem;
            public CardDefinition card;
            public bool sold;
        }

        private readonly List<Slot> slots = new List<Slot>();
        private readonly List<CardDefinition> offer = new List<CardDefinition>(SlotCount);
        private TextMeshPro diamonds;
        private TextMeshPro empty;
        private PixelButton refreshButton;
        private TextMeshPro refreshLabel;
        private int refreshCost;
        private Action onClosed;

        private RunStats Stats => GameSession.Instance != null ? GameSession.Instance.Stats : null;
        private int Diamonds => Stats != null ? Stats.Diamonds : 0;

        public static ShopUI Create(Transform parent, int sortingOrder, LevelSystem levels)
        {
            var go = new GameObject("ShopUI");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<ShopUI>();
            p.levels = levels;
            p.Build(new Vector2(176f, 214f), "shop.title", sortingOrder, false);
            p.BuildContent();
            return p;
        }

        private float Top => SizePx.y * 0.5f;

        private void BuildContent()
        {
            var a = GameAssets.Instance;
            float px = a.pixelSprite.bounds.size.x;

            // 가진 다이아
            var gem = CreateSprite("Gem", a.diamondSprite, new Vector2(-14f, Top - 28f));
            diamonds = CreateText("Diamonds", new Vector2(-8f + 30f, Top - 28f), GameText.Style.Title, TextAlignmentOptions.MidlineLeft, 60f);
            diamonds.color = new Color(0.85f, 0.95f, 1f);

            for (int i = 0; i < SlotCount; i++)
            {
                int index = i;
                var s = new Slot { root = new GameObject("Slot" + i).transform };
                s.root.SetParent(Content, false);
                s.root.localPosition = new Vector3(U(-(CardW + 4f) + (CardW + 4f) * i), U(CardY), 0f);

                s.frame = a.CreateSprite("Frame", a.pixelSprite, Order + 3, s.root);
                s.frame.transform.localScale = new Vector3(U(CardW) / px, U(CardH) / px, 1f);
                var inner = a.CreateSprite("Inner", a.pixelSprite, Order + 4, s.root);
                inner.color = new Color(0.1f, 0.1f, 0.18f);
                inner.transform.localScale = new Vector3(U(CardW - 4f) / px, U(CardH - 4f) / px, 1f);

                s.icon = a.CreateSprite("Icon", null, Order + 5, s.root);
                s.icon.transform.localScale = Vector3.one * 2f;
                s.icon.transform.localPosition = new Vector3(0f, U(CardH * 0.5f - 18f), 0f);

                s.name = GameText.Create("Name", s.root, GameText.Style.Body, Order + 5, TextAlignmentOptions.Center, CardW - 4f, true);
                s.name.transform.localPosition = new Vector3(0f, U(CardH * 0.5f - 38f), 0f);
                s.desc = GameText.Create("Desc", s.root, GameText.Style.Body, Order + 5, TextAlignmentOptions.Top, CardW - 4f, true);
                s.desc.color = new Color(0.78f, 0.82f, 0.95f);
                s.desc.rectTransform.pivot = new Vector2(0.5f, 1f);
                s.desc.transform.localPosition = new Vector3(0f, U(CardH * 0.5f - 46f), 0f);

                // 가격 버튼(카드 아래): 다이아 그림 + 숫자
                Vector2 buyPos = new Vector2(-(CardW + 4f) + (CardW + 4f) * i, CardY - CardH * 0.5f - 12f);
                s.buy = CreateButton("Buy" + i, buyPos, new Vector2(CardW - 4f, 16f), null, new Color(0.3f, 0.6f, 0.85f), GameText.Style.Body);
                s.priceGem = a.CreateSprite("Gem", a.diamondSprite, Order + 9, s.buy.transform);
                s.priceGem.transform.localPosition = new Vector3(-U(9f), 0f, 0f);
                s.priceLabel = GameText.Create("Price", s.buy.transform, GameText.Style.Body, Order + 9, TextAlignmentOptions.MidlineLeft, 30f);
                s.priceLabel.transform.localPosition = new Vector3(U(-4f + 15f), 0f, 0f);
                s.buy.Clicked += () => Buy(index);
                slots.Add(s);
            }

            empty = CreateText("Empty", new Vector2(0f, CardY), GameText.Style.Body, TextAlignmentOptions.Center, 150f, true);
            GameText.SetKey(empty, "shop.empty");
            empty.color = new Color(0.75f, 0.8f, 1f);

            refreshButton = CreateButton("Refresh", new Vector2(-40f, -Top + 22f), new Vector2(76f, 18f), null, new Color(0.45f, 0.4f, 0.75f), GameText.Style.Body);
            refreshLabel = GameText.Create("Label", refreshButton.transform, GameText.Style.Body, Order + 9, TextAlignmentOptions.Center, 74f);
            refreshButton.Clicked += Refresh;
            var next = CreateButton("Next", new Vector2(40f, -Top + 22f), new Vector2(76f, 18f), "shop.next", new Color(0.45f, 0.5f, 0.6f), GameText.Style.Body);
            next.Clicked += Close;
        }

        /// <summary>상점을 연다(게임 멈춤). 닫히면 closed.</summary>
        public void Open(Action closed)
        {
            onClosed = closed;
            refreshCost = PlayArea.Config.shopRefreshBaseCost;
            GameSession.Instance?.Pause();
            Open();
            Restock();
            Sound.Play(Sfx.Start);
        }

        protected override void OnClosed()
        {
            GameSession.Instance?.Resume();
            var done = onClosed;
            onClosed = null;
            done?.Invoke();
        }

        /// <summary>남은 다이아 이하 가격의 증강으로 새로 진열.</summary>
        private void Restock()
        {
            if (levels != null) levels.BuildOffer(offer, SlotCount, Diamonds);
            else offer.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].card = i < offer.Count ? offer[i] : null;
                slots[i].sold = false;
            }
            Redraw();
        }

        private void Buy(int index)
        {
            var s = slots[index];
            if (s.card == null || s.sold || Stats == null) return;
            if (!Stats.SpendDiamonds(s.card.price)) { Sound.Play(Sfx.Hit, 0.7f); return; }
            s.sold = true;
            levels.Add(s.card);
            Redraw();
        }

        private void Refresh()
        {
            if (Stats == null || !Stats.SpendDiamonds(refreshCost)) { Sound.Play(Sfx.Hit, 0.7f); return; }
            refreshCost++;
            Sound.Play(Sfx.CardPick);
            Restock();
        }

        private void Redraw()
        {
            GameText.SetPlain(diamonds, Diamonds.ToString());
            bool any = false;
            foreach (var s in slots)
            {
                bool show = s.card != null;
                s.root.gameObject.SetActive(show);
                s.buy.gameObject.SetActive(show);
                if (!show) continue;
                any = true;
                var c = s.card;
                s.frame.color = s.sold ? new Color(0.3f, 0.3f, 0.35f) : c.color;
                s.icon.sprite = c.icon;
                s.icon.color = s.sold ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                GameText.SetKey(s.name, c.nameKey);
                s.name.color = s.sold ? new Color(0.5f, 0.5f, 0.55f) : c.color;
                GameText.SetKey(s.desc, c.descKey);
                BuildPips(s);

                bool afford = Diamonds >= c.price;
                s.buy.Interactable = !s.sold && afford;
                if (s.sold)
                {
                    s.buy.SetColor(new Color(0.3f, 0.32f, 0.38f));
                    s.priceGem.gameObject.SetActive(false);
                    GameText.SetKey(s.priceLabel, "shop.sold");
                    s.priceLabel.alignment = TextAlignmentOptions.Center;
                    s.priceLabel.transform.localPosition = Vector3.zero;
                }
                else
                {
                    s.buy.SetColor(afford ? new Color(0.3f, 0.6f, 0.85f) : new Color(0.3f, 0.32f, 0.4f));
                    s.priceGem.gameObject.SetActive(true);
                    GameText.SetPlain(s.priceLabel, c.price.ToString());
                    s.priceLabel.alignment = TextAlignmentOptions.MidlineLeft;
                    s.priceLabel.transform.localPosition = new Vector3(U(-4f + 15f), 0f, 0f);
                }
            }
            empty.gameObject.SetActive(!any);

            bool canRefresh = Diamonds >= refreshCost;
            refreshButton.Interactable = canRefresh;
            refreshButton.SetColor(canRefresh ? new Color(0.45f, 0.4f, 0.75f) : new Color(0.3f, 0.3f, 0.4f));
            GameText.SetPlain(refreshLabel, Localization.Get("shop.refresh") + " " + refreshCost);
        }

        /// <summary>카드 아래 점: 이미 산 단계는 흰색, 이번에 오를 단계는 카드 색.</summary>
        private void BuildPips(Slot s)
        {
            var a = GameAssets.Instance;
            float px = a.pixelSprite.bounds.size.x;
            foreach (var p in s.pips) p.gameObject.SetActive(false);
            int max = s.card.maxStacks;
            if (max <= 0 || levels == null) return;
            int have = levels.StacksOf(s.card);
            float step = U(5f), size = U(3f);
            float start = -step * (max - 1) * 0.5f;
            for (int i = 0; i < max; i++)
            {
                if (s.pips.Count <= i) s.pips.Add(a.CreateSprite("Pip", a.pixelSprite, Order + 5, s.root));
                var pip = s.pips[i];
                pip.gameObject.SetActive(true);
                pip.transform.localScale = new Vector3(size / px, size / px, 1f);
                pip.transform.localPosition = new Vector3(start + step * i, -U(CardH * 0.5f - 6f), 0f);
                pip.color = i < have ? Color.white : new Color(0.3f, 0.3f, 0.4f);
            }
        }
    }
}
