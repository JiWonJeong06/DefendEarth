using System;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// UFO 고르기 창(로비): 종류마다 그림·이름·설명, 버튼은 "사용 중" / "선택" / "골드 해금".
    /// 해금은 한 번 사면 계속 쓴다(뽑기 없음). 로비 강화는 모든 UFO에 공통.
    /// </summary>
    public sealed class UfoPanel : ModalPanel
    {
        private const float RowHeight = 52f;
        private TextMeshPro gold;
        private PixelButton[] buttons;
        private TextMeshPro[] buttonLabels;
        private SpriteRenderer[] frames;

        /// <summary>골드를 쓰거나 UFO를 바꿨을 때(로비 갱신).</summary>
        public event Action Changed;

        public static UfoPanel Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("UfoPanel");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<UfoPanel>();
            p.Build(new Vector2(172f, 64f + UfoTypes.All.Length * RowHeight), "ui.ufo", sortingOrder);
            p.BuildContent();
            return p;
        }

        private void BuildContent()
        {
            var a = GameAssets.Instance;
            float px = a.pixelSprite.bounds.size.x;
            float top = SizePx.y * 0.5f;
            CreateSprite("Coin", a.coinSprite, new Vector2(-24f, top - 29f));
            gold = CreateText("Gold", new Vector2(-18f + 40f, top - 29f), GameText.Style.Title, TextAlignmentOptions.MidlineLeft, 80f);
            gold.color = new Color(1f, 0.86f, 0.4f);

            int n = UfoTypes.All.Length;
            buttons = new PixelButton[n];
            buttonLabels = new TextMeshPro[n];
            frames = new SpriteRenderer[n];
            for (int i = 0; i < n; i++)
            {
                var t = UfoTypes.All[i];
                float y = top - 66f - i * RowHeight;

                // 고른 UFO 줄은 밝은 테두리
                var frame = CreateSprite("Frame" + i, a.pixelSprite, new Vector2(0f, y), 3);
                frame.transform.localScale = new Vector3(U(164f) / px, U(RowHeight - 4f) / px, 1f);
                frames[i] = frame;

                var icon = CreateSprite("Icon" + i, null, new Vector2(-66f, y), 5);
                UfoTypes.Apply(icon, i);

                var name = CreateText("Name" + i, new Vector2(-48f + 46f, y + 12f), GameText.Style.Title, TextAlignmentOptions.MidlineLeft, 92f);
                GameText.SetKey(name, t.nameKey);
                var desc = CreateText("Desc" + i, new Vector2(-48f + 46f, y - 7f), GameText.Style.Body, TextAlignmentOptions.MidlineLeft, 92f, true);
                GameText.SetKey(desc, t.descKey);
                desc.color = new Color(0.75f, 0.8f, 1f);

                int index = i;
                var b = CreateButton("Pick" + i, new Vector2(60f, y), new Vector2(40f, 16f), null, new Color(0.35f, 0.7f, 0.4f), GameText.Style.Body);
                buttonLabels[i] = GameText.Create("Label", b.transform, GameText.Style.Body, Order + 8, TextAlignmentOptions.Center, 38f);
                b.Clicked += () => Pick(index);
                buttons[i] = b;
            }
        }

        private void Pick(int index)
        {
            var save = SaveSystem.Data;
            if (!UfoTypes.IsOwned(index))
            {
                int price = UfoTypes.All[index].price;
                if (save.gold < price) { Sound.Play(Sfx.Hit, 0.7f); return; }
                save.gold -= price;
                save.ownedUfos.Add(index);
                Sound.Play(Sfx.LevelUp);
            }
            else Sound.Play(Sfx.CardPick);
            save.selectedUfo = index;
            SaveSystem.Save();
            Refresh();
            Changed?.Invoke();
        }

        private void Refresh()
        {
            var save = SaveSystem.Data;
            GameText.SetPlain(gold, save.gold.ToString());
            int selected = UfoTypes.SelectedIndex;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool owned = UfoTypes.IsOwned(i);
                bool using_ = i == selected;
                int price = UfoTypes.All[i].price;
                frames[i].color = using_ ? new Color(0.3f, 0.45f, 0.85f, 0.55f) : new Color(0.12f, 0.14f, 0.26f, 0.6f);
                buttons[i].Interactable = !using_;
                if (using_)
                {
                    buttons[i].SetColor(new Color(0.3f, 0.32f, 0.42f));
                    GameText.SetKey(buttonLabels[i], "ui.ufo.using");
                }
                else if (owned)
                {
                    buttons[i].SetColor(new Color(0.35f, 0.7f, 0.4f));
                    GameText.SetKey(buttonLabels[i], "ui.ufo.select");
                }
                else
                {
                    bool afford = save.gold >= price;
                    buttons[i].SetColor(afford ? new Color(0.85f, 0.6f, 0.25f) : new Color(0.4f, 0.35f, 0.3f));
                    GameText.SetPlain(buttonLabels[i], price.ToString());
                }
            }
        }

        protected override void OnOpened() => Refresh();
    }
}
