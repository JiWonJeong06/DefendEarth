using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 도트 버튼. 누른 뒤 같은 버튼 위에서 손을 떼면 눌린 것으로 본다(잘못 누름 방지).
    /// 창이 겹치면 맨 위 창의 버튼만 반응한다(UiLayer).
    /// </summary>
    public sealed class PixelButton : MonoBehaviour
    {
        private static readonly List<PixelButton> all = new List<PixelButton>();

        public event Action Clicked;

        public int Layer { get; set; }
        public TextMeshPro Label { get; private set; }
        public bool Interactable { get; set; } = true;

        private Vector2 sizeUnits;
        private SpriteRenderer face;
        private Color faceColor;
        private bool pressed;
        private TapInput input;

        /// <summary>이 화면 좌표에 반응할 수 있는 버튼이 있는지(UFO가 버튼 누름을 이동으로 착각하지 않게).</summary>
        public static bool AnyAt(Vector2 screenPos)
        {
            foreach (var b in all)
                if (b.CanReact && b.Contains(screenPos)) return true;
            return false;
        }

        /// <summary>
        /// 버튼을 만든다. labelKey가 있으면 번역 글자, 없으면 글자 없음(아이콘 버튼).
        /// </summary>
        public static PixelButton Create(string name, Transform parent, Vector2 localPosPx, Vector2 sizePx, string labelKey,
            Color color, int sortingOrder, int layer, GameText.Style style = GameText.Style.Title)
        {
            var assets = GameAssets.Instance;
            var config = PlayArea.Config;
            float px = assets.pixelSprite.bounds.size.x;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(config.PixelsToUnits(localPosPx.x), config.PixelsToUnits(localPosPx.y), 0f);

            var b = go.AddComponent<PixelButton>();
            b.Layer = layer;
            b.sizeUnits = new Vector2(config.PixelsToUnits(sizePx.x), config.PixelsToUnits(sizePx.y));

            var border = assets.CreateSprite("Border", assets.pixelSprite, sortingOrder, go.transform);
            border.color = new Color(0f, 0f, 0f, 0.85f);
            border.transform.localScale = new Vector3((b.sizeUnits.x + config.PixelsToUnits(2f)) / px, (b.sizeUnits.y + config.PixelsToUnits(2f)) / px, 1f);

            b.face = assets.CreateSprite("Face", assets.pixelSprite, sortingOrder + 1, go.transform);
            b.faceColor = color;
            b.face.color = color;
            b.face.transform.localScale = new Vector3(b.sizeUnits.x / px, b.sizeUnits.y / px, 1f);

            var shine = assets.CreateSprite("Shine", assets.pixelSprite, sortingOrder + 2, go.transform);
            shine.color = new Color(1f, 1f, 1f, 0.18f);
            shine.transform.localScale = new Vector3(b.sizeUnits.x / px, config.PixelsToUnits(2f) / px, 1f);
            shine.transform.localPosition = new Vector3(0f, b.sizeUnits.y * 0.5f - config.PixelsToUnits(1f), 0f);

            if (!string.IsNullOrEmpty(labelKey))
            {
                b.Label = GameText.Create("Label", go.transform, style, sortingOrder + 3, TextAlignmentOptions.Center, sizePx.x - 4f);
                GameText.SetKey(b.Label, labelKey);
            }
            return b;
        }

        public void SetColor(Color color)
        {
            faceColor = color;
            if (face != null) face.color = color;
        }

        private bool CanReact => Interactable && isActiveAndEnabled && Layer == UiLayer.Current;

        private bool Contains(Vector2 screenPos)
        {
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (cam == null) return false;
            Vector2 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            Vector2 d = world - (Vector2)transform.position;
            Vector2 half = new Vector2(sizeUnits.x * Mathf.Abs(transform.lossyScale.x), sizeUnits.y * Mathf.Abs(transform.lossyScale.y)) * 0.5f;
            return Mathf.Abs(d.x) <= half.x && Mathf.Abs(d.y) <= half.y;
        }

        private void OnEnable()
        {
            all.Add(this);
            Hook();
        }

        private void Start() => Hook();

        private void Hook()
        {
            if (input != null || TapInput.Main == null) return;
            input = TapInput.Main;
            input.Pressed += OnPressed;
            input.Released += OnReleased;
        }

        private void OnDisable()
        {
            all.Remove(this);
            pressed = false;
            Apply();
            if (input == null) return;
            input.Pressed -= OnPressed;
            input.Released -= OnReleased;
            input = null;
        }

        private void OnPressed(Vector2 pos)
        {
            pressed = CanReact && Contains(pos);
            Apply();
        }

        private void OnReleased(Vector2 pos, float held)
        {
            bool click = pressed && CanReact && Contains(pos);
            pressed = false;
            Apply();
            if (click) Clicked?.Invoke();
        }

        private void Apply()
        {
            if (face == null) return;
            face.color = !Interactable ? faceColor * new Color(0.5f, 0.5f, 0.5f, 1f)
                : pressed ? faceColor * new Color(0.75f, 0.75f, 0.75f, 1f) : faceColor;
            transform.localScale = pressed ? Vector3.one * 0.95f : Vector3.one;
        }

        private void LateUpdate()
        {
            if (input == null) Hook();
            Apply();
        }
    }

    /// <summary>
    /// 겹쳐 뜬 창 순서. 창을 열 때 Push, 닫을 때 Pop. 맨 위 층의 버튼만 반응한다.
    /// </summary>
    public static class UiLayer
    {
        private static readonly Stack<int> stack = new Stack<int>();
        private static int next = 1;

        public static int Current => stack.Count > 0 ? stack.Peek() : 0;

        public static int Push()
        {
            int id = next++;
            stack.Push(id);
            return id;
        }

        public static void Pop(int id)
        {
            if (stack.Count > 0 && stack.Peek() == id) stack.Pop();
        }

        /// <summary>장면이 바뀌면 처음부터.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            stack.Clear();
            next = 1;
        }
    }
}
