using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 레벨업 카드 고르기 화면. 게임이 멈춘 동안 카드 3장을 보여 주고, 누른 카드에서 손을 떼면 고른다.
    /// 카드에는 아이콘, 이름, 설명(3개 언어), 단계 점이 보인다.
    /// </summary>
    public sealed class CardChoiceUI : MonoBehaviour
    {
        [SerializeField] private TapInput input;
        [SerializeField] private LevelSystem levels;

        [Tooltip("카드 크기(기준 픽셀)")]
        [SerializeField] private Vector2 cardSizePx = new Vector2(56f, 96f);
        [Tooltip("카드 사이 간격(기준 픽셀)")]
        [SerializeField] private float gapPx = 4f;

        private const int Order = SortOrder.Overlay + 10;

        private sealed class CardView
        {
            public Transform root;
            public SpriteRenderer frame;
            public SpriteRenderer icon;
            public TextMeshPro name;
            public TextMeshPro desc;
            public readonly List<SpriteRenderer> pips = new List<SpriteRenderer>();
            public CardDefinition card;
            public int nextStack;
        }

        private readonly List<CardView> views = new List<CardView>();
        private SpriteRenderer dim;
        private PixelNumber levelNumber;
        private TextMeshPro title;
        private bool open;
        private float openedAt;
        private int pressedIndex = -1;
        private int chosenIndex = -1;
        private float chosenAt;

        private GameConfig Config => PlayArea.Config;
        private float PixelSize => GameAssets.Instance.pixelSprite.bounds.size.x;

        private void Start()
        {
            var assets = GameAssets.Instance;
            dim = assets.CreateSprite("Dim", assets.pixelSprite, Order, transform);
            dim.transform.localScale = new Vector3(200f / PixelSize, 200f / PixelSize, 1f);
            dim.color = new Color(0f, 0f, 0.05f, 0.6f);

            levelNumber = new GameObject("LevelNumber").AddComponent<PixelNumber>();
            levelNumber.transform.SetParent(transform, false);
            levelNumber.transform.localScale = Vector3.one * 2f;
            levelNumber.transform.position = new Vector3(0f, Config.PixelsToUnits(cardSizePx.y * 0.5f + 12f), 0f);
            levelNumber.Configure(PixelNumber.Align.Center, new Color(0.6f, 1f, 0.7f));
            levelNumber.SetSortingOrder(Order + 3);

            title = GameText.Create("Title", transform, GameText.Style.Title, Order + 3);
            GameText.SetKey(title, "ui.levelup");
            title.color = new Color(0.6f, 1f, 0.7f);
            title.transform.position = new Vector3(0f, Config.PixelsToUnits(cardSizePx.y * 0.5f + 30f), 0f);

            for (int i = 0; i < LevelSystem.ChoiceCount; i++) views.Add(CreateView(i));
            SetVisible(false);

            if (levels != null) levels.ChoiceRequested += OnChoiceRequested;
            if (input != null)
            {
                input.Pressed += OnPressed;
                input.Released += OnReleased;
            }
        }

        private void OnDestroy()
        {
            if (levels != null) levels.ChoiceRequested -= OnChoiceRequested;
            if (input != null)
            {
                input.Pressed -= OnPressed;
                input.Released -= OnReleased;
            }
        }

        private CardView CreateView(int index)
        {
            var assets = GameAssets.Instance;
            var v = new CardView { root = new GameObject("Card" + index).transform };
            v.root.SetParent(transform, false);

            float w = Config.PixelsToUnits(cardSizePx.x);
            float h = Config.PixelsToUnits(cardSizePx.y);
            v.frame = assets.CreateSprite("Frame", assets.pixelSprite, Order + 1, v.root);
            v.frame.transform.localScale = new Vector3(w / PixelSize, h / PixelSize, 1f);

            var inner = assets.CreateSprite("Inner", assets.pixelSprite, Order + 2, v.root);
            inner.color = new Color(0.1f, 0.1f, 0.18f);
            float border = Config.PixelsToUnits(2f);
            inner.transform.localScale = new Vector3((w - border * 2f) / PixelSize, (h - border * 2f) / PixelSize, 1f);

            v.icon = assets.CreateSprite("Icon", null, Order + 3, v.root);
            v.icon.transform.localScale = Vector3.one * 2f;
            v.icon.transform.localPosition = new Vector3(0f, Config.PixelsToUnits(cardSizePx.y * 0.5f - 20f), 0f);

            float textBox = cardSizePx.x - 6f;
            v.name = GameText.Create("Name", v.root, GameText.Style.Body, Order + 3, TextAlignmentOptions.Center, textBox, true);
            v.name.transform.localPosition = new Vector3(0f, Config.PixelsToUnits(cardSizePx.y * 0.5f - 42f), 0f);
            v.desc = GameText.Create("Desc", v.root, GameText.Style.Body, Order + 3, TextAlignmentOptions.Top, textBox, true);
            v.desc.color = new Color(0.78f, 0.82f, 0.95f);
            v.desc.transform.localPosition = new Vector3(0f, Config.PixelsToUnits(cardSizePx.y * 0.5f - 52f), 0f);
            v.desc.rectTransform.pivot = new Vector2(0.5f, 1f);
            return v;
        }

        private void OnChoiceRequested(int level, IReadOnlyList<CardDefinition> cards)
        {
            float w = Config.PixelsToUnits(cardSizePx.x);
            float gap = Config.PixelsToUnits(gapPx);
            float total = cards.Count * w + (cards.Count - 1) * gap;

            for (int i = 0; i < views.Count; i++)
            {
                var v = views[i];
                bool on = i < cards.Count;
                v.root.gameObject.SetActive(on);
                if (!on) continue;
                v.card = cards[i];
                v.nextStack = levels.StacksOf(v.card) + 1;
                v.root.localPosition = new Vector3(-total * 0.5f + w * 0.5f + (w + gap) * i, 0f, 0f);
                v.frame.color = v.card.color;
                v.icon.sprite = v.card.icon;
                GameText.SetKey(v.name, v.card.nameKey);
                v.name.color = v.card.color;
                GameText.SetKey(v.desc, v.card.descKey);
                BuildPips(v);
            }

            levelNumber.SetValue(level);
            open = true;
            openedAt = Time.unscaledTime;
            pressedIndex = -1;
            chosenIndex = -1;
            SetVisible(true);
        }

        /// <summary>카드 아래 점: 이미 고른 단계는 흰색, 이번에 오를 단계는 카드 색으로 깜빡.</summary>
        private void BuildPips(CardView v)
        {
            var assets = GameAssets.Instance;
            int max = v.card.maxStacks;
            foreach (var p in v.pips) p.gameObject.SetActive(false);
            if (max <= 0) return;

            float size = Config.PixelsToUnits(3f);
            float step = Config.PixelsToUnits(5f);
            float y = -Config.PixelsToUnits(cardSizePx.y * 0.5f - 6f);
            float start = -step * (max - 1) * 0.5f;
            for (int i = 0; i < max; i++)
            {
                if (v.pips.Count <= i)
                {
                    var sr = assets.CreateSprite("Pip", assets.pixelSprite, Order + 3, v.root);
                    v.pips.Add(sr);
                }
                var pip = v.pips[i];
                pip.gameObject.SetActive(true);
                pip.transform.localScale = new Vector3(size / PixelSize, size / PixelSize, 1f);
                pip.transform.localPosition = new Vector3(start + step * i, y, 0f);
                pip.color = i < v.nextStack - 1 ? Color.white : new Color(0.3f, 0.3f, 0.4f);
            }
        }

        private void Update()
        {
            if (!open) return;
            float t = Time.unscaledTime - openedAt;

            for (int i = 0; i < views.Count; i++)
            {
                var v = views[i];
                if (!v.root.gameObject.activeSelf) continue;

                // 하나씩 통 튀어나오는 등장 연출
                float k = Mathf.Clamp01((t - i * 0.06f) / 0.18f);
                float s = k < 1f ? Mathf.Lerp(0.5f, 1.08f, k) : 1f;
                if (i == pressedIndex) s *= 0.94f;
                if (chosenIndex >= 0) s = i == chosenIndex ? 1.15f : 0.85f;
                v.root.localScale = Vector3.one * s;

                // 이번에 오를 단계 점 깜빡임
                int next = v.nextStack - 1;
                if (next >= 0 && next < v.pips.Count && v.pips[next].gameObject.activeSelf)
                    v.pips[next].color = Color.Lerp(new Color(0.3f, 0.3f, 0.4f), v.card.color, Mathf.PingPong(Time.unscaledTime * 3f, 1f));
            }

            if (chosenIndex >= 0 && Time.unscaledTime - chosenAt > 0.15f)
            {
                var card = views[chosenIndex].card;
                open = false;
                SetVisible(false);
                levels.Pick(card);
            }
        }

        private int CardAt(Vector2 screenPos)
        {
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (cam == null) return -1;
            Vector2 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            Vector2 half = new Vector2(Config.PixelsToUnits(cardSizePx.x), Config.PixelsToUnits(cardSizePx.y)) * 0.5f;
            for (int i = 0; i < views.Count; i++)
            {
                if (!views[i].root.gameObject.activeSelf) continue;
                Vector2 d = world - (Vector2)views[i].root.position;
                if (Mathf.Abs(d.x) <= half.x && Mathf.Abs(d.y) <= half.y) return i;
            }
            return -1;
        }

        private bool AcceptingInput => open && chosenIndex < 0 && Time.unscaledTime - openedAt >= Config.cardInputDelay;

        private void OnPressed(Vector2 pos)
        {
            pressedIndex = AcceptingInput ? CardAt(pos) : -1;
        }

        private void OnReleased(Vector2 pos, float held)
        {
            if (!AcceptingInput) { pressedIndex = -1; return; }
            int index = CardAt(pos);
            if (index >= 0 && index == pressedIndex)
            {
                chosenIndex = index;
                chosenAt = Time.unscaledTime;
            }
            pressedIndex = -1;
        }

        private void SetVisible(bool on)
        {
            dim.enabled = on;
            levelNumber.gameObject.SetActive(on);
            title.gameObject.SetActive(on);
            foreach (var v in views) v.root.gameObject.SetActive(on && v.card != null && v.root.gameObject.activeSelf);
            if (!on) foreach (var v in views) v.root.gameObject.SetActive(false);
        }
    }
}
