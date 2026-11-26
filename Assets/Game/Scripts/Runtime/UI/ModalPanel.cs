using System;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 화면 위에 뜨는 창(설정·강화·꾸미기·순위표·일시정지 등). 열면 뒤의 버튼은 반응하지 않는다.
    /// 창 안의 버튼은 Layer를 이 창의 층으로 만든다(CreateButton 사용).
    /// </summary>
    public class ModalPanel : MonoBehaviour
    {
        public const float TitleOffsetFromTopPx = 12f;

        public Transform Content { get; private set; }
        public bool IsOpen { get; private set; }
        public int LayerId { get; private set; } = -1;
        public int Order { get; private set; }
        public Vector2 SizePx { get; private set; }

        public event Action Closed;

        private TextMeshPro title;

        protected GameConfig Config => PlayArea.Config;
        protected float U(float px) => Config.PixelsToUnits(px);

        /// <summary>창 틀을 만든다. 한 번만 부른다.</summary>
        public void Build(Vector2 sizePx, string titleKey, int sortingOrder, bool withCloseButton = true)
        {
            var assets = GameAssets.Instance;
            float px = assets.pixelSprite.bounds.size.x;
            SizePx = sizePx;
            Order = sortingOrder;

            var dim = assets.CreateSprite("Dim", assets.pixelSprite, sortingOrder, transform);
            dim.transform.localScale = new Vector3(200f / px, 200f / px, 1f);
            dim.color = new Color(0f, 0f, 0.05f, 0.65f);

            var border = assets.CreateSprite("Border", assets.pixelSprite, sortingOrder + 1, transform);
            border.color = new Color(0.45f, 0.6f, 1f);
            border.transform.localScale = new Vector3(U(sizePx.x) / px, U(sizePx.y) / px, 1f);
            var back = assets.CreateSprite("Back", assets.pixelSprite, sortingOrder + 2, transform);
            back.color = new Color(0.07f, 0.08f, 0.16f);
            back.transform.localScale = new Vector3(U(sizePx.x - 4f) / px, U(sizePx.y - 4f) / px, 1f);

            Content = new GameObject("Content").transform;
            Content.SetParent(transform, false);

            if (!string.IsNullOrEmpty(titleKey))
            {
                title = GameText.Create("Title", transform, GameText.Style.Title, sortingOrder + 3);
                GameText.SetKey(title, titleKey);
                title.color = new Color(0.75f, 0.85f, 1f);
                title.transform.localPosition = new Vector3(0f, U(sizePx.y * 0.5f - TitleOffsetFromTopPx), 0f);
            }

            if (withCloseButton)
            {
                var close = PixelButton.Create("Close", transform, new Vector2(sizePx.x * 0.5f - 11f, sizePx.y * 0.5f - 11f),
                    new Vector2(14f, 14f), null, new Color(0.75f, 0.3f, 0.3f), sortingOrder + 5, -1);
                var x = GameText.Create("X", close.transform, GameText.Style.Title, sortingOrder + 8);
                GameText.SetPlain(x, "x");
                close.Clicked += Close;
                RegisterButton(close);
            }
            gameObject.SetActive(false);
        }

        private readonly System.Collections.Generic.List<PixelButton> buttons = new System.Collections.Generic.List<PixelButton>();

        /// <summary>이 창 안에 버튼을 만든다(위치는 창 가운데 기준, 기준 픽셀).</summary>
        public PixelButton CreateButton(string name, Vector2 posPx, Vector2 sizePx, string labelKey, Color color,
            GameText.Style style = GameText.Style.Title)
        {
            var b = PixelButton.Create(name, Content, posPx, sizePx, labelKey, color, Order + 5, -1, style);
            RegisterButton(b);
            return b;
        }

        public void RegisterButton(PixelButton b)
        {
            b.Layer = LayerId;
            buttons.Add(b);
        }

        public TextMeshPro CreateText(string name, Vector2 posPx, GameText.Style style, TextAlignmentOptions align = TextAlignmentOptions.Center,
            float boxPx = 0f, bool wrap = false, int scale = 1)
        {
            var t = GameText.Create(name, Content, style, Order + 4, align, boxPx, wrap, scale);
            t.transform.localPosition = new Vector3(U(posPx.x), U(posPx.y), 0f);
            return t;
        }

        public SpriteRenderer CreateSprite(string name, Sprite sprite, Vector2 posPx, int orderOffset = 4)
        {
            var sr = GameAssets.Instance.CreateSprite(name, sprite, Order + orderOffset, Content);
            sr.transform.localPosition = new Vector3(U(posPx.x), U(posPx.y), 0f);
            return sr;
        }

        public virtual void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            LayerId = UiLayer.Push();
            foreach (var b in buttons) if (b != null) b.Layer = LayerId;
            gameObject.SetActive(true);
            OnOpened();
        }

        public virtual void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            UiLayer.Pop(LayerId);
            gameObject.SetActive(false);
            OnClosed();
            Closed?.Invoke();
        }

        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
    }
}
