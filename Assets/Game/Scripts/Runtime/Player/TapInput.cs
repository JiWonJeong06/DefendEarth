using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BouncyUFO
{
    /// <summary>
    /// 화면을 누른 순간과 뗀 순간을 알린다(터치·마우스). 누르는 동안 손가락 위치(Position)도 따라간다.
    /// 한 번에 손가락 하나만 따라간다. 누르고 있는 동안 다른 손가락은 무시한다.
    /// </summary>
    public sealed class TapInput : MonoBehaviour
    {
        private const int MouseId = -1;
        private const int NoPress = int.MinValue;

        /// <summary>누른 위치(화면 좌표).</summary>
        public event Action<Vector2> Pressed;
        /// <summary>뗀 위치(화면 좌표), 누르고 있던 시간(초, 실제 시간).</summary>
        public event Action<Vector2, float> Released;

        /// <summary>지금 장면의 입력(버튼들이 듣는다).</summary>
        public static TapInput Main { get; private set; }

        private int activeId = NoPress;
        private float pressedAt;

        /// <summary>누르고 있는 손가락의 지금 위치(화면 좌표). 조준에 쓴다.</summary>
        public Vector2 Position { get; private set; }

        public bool IsHolding => activeId != NoPress;
        public float HeldTime => IsHolding ? Time.unscaledTime - pressedAt : 0f;

        private void OnEnable() => Main = this;

        private void OnDisable()
        {
            activeId = NoPress;
            if (Main == this) Main = null;
        }

        private void Update()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    int id = touch.touchId.ReadValue();
                    if (id == activeId && touch.press.isPressed) Position = touch.position.ReadValue();
                    if (touch.press.wasPressedThisFrame) Begin(id, touch.position.ReadValue());
                    if (touch.press.wasReleasedThisFrame) End(id, touch.position.ReadValue());
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (activeId == MouseId) Position = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame) Begin(MouseId, mouse.position.ReadValue());
                if (mouse.leftButton.wasReleasedThisFrame) End(MouseId, mouse.position.ReadValue());
            }
        }

        private void Begin(int id, Vector2 pos)
        {
            if (IsHolding) return;
            activeId = id;
            pressedAt = Time.unscaledTime;
            Position = pos;
            Pressed?.Invoke(pos);
        }

        private void End(int id, Vector2 pos)
        {
            if (id != activeId) return;
            float held = Time.unscaledTime - pressedAt;
            activeId = NoPress;
            Released?.Invoke(pos, held);
        }
    }
}
