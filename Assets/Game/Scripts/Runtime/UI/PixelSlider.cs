using System;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 끌어서 값을 바꾸는 막대(소리 크기 등). 모양은 PixelBar(틀·검정·빨강)와 같다.
    /// 막대를 누르면 그 자리로 값이 가고, 누른 채로 좌우로 움직이면 따라간다. 손을 떼면 Released.
    /// 창(ModalPanel) 안에 두면 그 창이 맨 위일 때만 반응한다.
    /// </summary>
    public sealed class PixelSlider : MonoBehaviour
    {
        private const float TouchExtraPx = 6f; // 누르기 쉽게 막대보다 조금 넓게 받는다

        public ModalPanel Owner { get; set; }
        public float Value { get; private set; }

        /// <summary>끄는 동안 계속(0~1).</summary>
        public event Action<float> Changed;
        /// <summary>손을 뗐을 때(저장할 때).</summary>
        public event Action<float> Released;

        private PixelBar bar;
        private float widthPx;
        private float heightPx;
        private TapInput input;
        private bool dragging;

        public static PixelSlider Create(string name, Transform parent, int order, float widthPx, Vector2 localPosPx)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(PlayArea.Config.PixelsToUnits(localPosPx.x), PlayArea.Config.PixelsToUnits(localPosPx.y), 0f);
            var s = go.AddComponent<PixelSlider>();
            s.widthPx = widthPx;
            s.heightPx = 16f;
            s.bar = PixelBar.Create("Bar", go.transform, order, widthPx);
            return s;
        }

        public void SetValue(float v)
        {
            Value = Mathf.Clamp01(v);
            bar.SetValue(Value);
        }

        private void OnEnable()
        {
            input = TapInput.Main;
            if (input != null) input.Pressed += OnPressed;
        }

        private void OnDisable()
        {
            if (input != null) input.Pressed -= OnPressed;
            dragging = false;
        }

        private bool CanReact => isActiveAndEnabled && (Owner == null ? UiLayer.Current == 0 : Owner.IsOpen && UiLayer.Current == Owner.LayerId);

        private void OnPressed(Vector2 screenPos)
        {
            if (!CanReact) return;
            Vector2 local = ToLocalPx(screenPos);
            if (Mathf.Abs(local.x) > widthPx * 0.5f + TouchExtraPx || Mathf.Abs(local.y) > heightPx * 0.5f + TouchExtraPx) return;
            dragging = true;
            Follow(screenPos);
        }

        private void Update()
        {
            if (!dragging || input == null) return;
            if (!input.IsHolding)
            {
                dragging = false;
                Released?.Invoke(Value);
                return;
            }
            Follow(input.Position);
        }

        private void Follow(Vector2 screenPos)
        {
            // 막대 안쪽(틀 테두리 5픽셀 안) 기준으로 0~1
            float inner = Mathf.Max(1f, widthPx - 10f);
            float v = Mathf.Clamp01((ToLocalPx(screenPos).x + inner * 0.5f) / inner);
            v = Mathf.Round(v * 20f) / 20f; // 5% 단위
            if (Mathf.Approximately(v, Value)) return;
            SetValue(v);
            Changed?.Invoke(Value);
        }

        private Vector2 ToLocalPx(Vector2 screenPos)
        {
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (cam == null) return new Vector2(9999f, 9999f);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            Vector2 d = world - transform.position;
            float ppu = PlayArea.Config.pixelsPerUnit;
            return d * ppu;
        }
    }
}
