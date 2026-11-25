using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BouncyUFO
{
    /// <summary>
    /// 화면 아무 데나 누르는 입력을 "드래그"와 "탭"으로 나눈다(터치·마우스 모두).
    /// - 드래그: 손가락이 움직인 만큼을 플레이 영역 너비 비율로 바꿔 알린다.
    /// - 탭: 짧게(기본 0.15초 이내) 누르고 거의 안 움직였으면 손을 뗄 때 알린다.
    /// </summary>
    public sealed class GestureInput : MonoBehaviour
    {
        /// <summary>좌우 드래그 양(플레이 영역 너비 비율, 오른쪽 +).</summary>
        public event Action<float> Dragged;
        /// <summary>탭한 위치(화면 좌표).</summary>
        public event Action<Vector2> Tapped;

        private bool pressing;
        private float pressTime;
        private Vector2 pressStartPos;
        private Vector2 lastPos;
        private float maxMoveFraction;

        private void OnDisable() => pressing = false;

        private void Update()
        {
            var config = PlayArea.Config;
            var playCam = PlayAreaCamera.Main;
            if (config == null || playCam == null) return;

            Pointer pointer = Pointer.current;
            if (pointer == null) return;

            bool isPressed = pointer.press.isPressed;
            Vector2 pos = pointer.position.ReadValue();
            float areaWidthPx = Mathf.Max(1f, playCam.PlayAreaWidthInScreenPixels);

            if (isPressed && !pressing)
            {
                pressing = true;
                pressTime = Time.unscaledTime;
                pressStartPos = pos;
                lastPos = pos;
                maxMoveFraction = 0f;
                return;
            }

            if (!pressing) return;

            if (isPressed)
            {
                float dxFraction = (pos.x - lastPos.x) / areaWidthPx;
                lastPos = pos;
                maxMoveFraction = Mathf.Max(maxMoveFraction, (pos - pressStartPos).magnitude / areaWidthPx);
                if (dxFraction != 0f) Dragged?.Invoke(dxFraction);
            }
            else
            {
                pressing = false;
                float duration = Time.unscaledTime - pressTime;
                if (duration <= config.tapMaxDuration && maxMoveFraction <= config.tapMaxMoveFraction)
                    Tapped?.Invoke(pressStartPos);
            }
        }
    }
}
