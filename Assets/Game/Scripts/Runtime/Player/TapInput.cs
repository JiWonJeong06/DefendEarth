using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BouncyUFO
{
    /// <summary>
    /// 화면을 누르는 순간을 알린다(터치·마우스). 드래그는 쓰지 않는다.
    /// 여러 손가락으로 번갈아 탭해도 각각 한 번씩 알린다.
    /// </summary>
    public sealed class TapInput : MonoBehaviour
    {
        /// <summary>누른 위치(화면 좌표).</summary>
        public event Action<Vector2> Tapped;

        private void Update()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (touch.press.wasPressedThisFrame)
                        Tapped?.Invoke(touch.position.ReadValue());
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                Tapped?.Invoke(mouse.position.ReadValue());
        }
    }
}
