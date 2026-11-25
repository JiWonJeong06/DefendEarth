using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 플레이어 UFO. 좌우로만 움직인다. 체력은 없다.
    /// - 드래그: 손가락이 플레이 영역 너비의 몇 %를 움직였는지만큼 따라간다(기기마다 같음).
    /// - 탭: 탭한 쪽(UFO 기준 왼쪽/오른쪽)으로 통 튀며 짧게 대시. 재사용 대기 있음.
    /// </summary>
    public sealed class UfoController : MonoBehaviour
    {
        [SerializeField] private GestureInput input;
        [SerializeField] private SquashStretch visual;

        private float dashTimer = -1f;
        private float dashStartX;
        private float dashTargetX;
        private float cooldownUntil;
        private float lastX;
        private bool wasMoving;

        public bool IsDashing => dashTimer >= 0f;

        private GameConfig Config => PlayArea.Config;

        private void Start()
        {
            float y = PlayArea.Bottom + Config.PixelsToUnits(Config.ufoHeightFromBottomPx);
            transform.position = new Vector3(0f, y, 0f);
            lastX = 0f;
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.Dragged += OnDragged;
            input.Tapped += OnTapped;
        }

        private void OnDisable()
        {
            if (input == null) return;
            input.Dragged -= OnDragged;
            input.Tapped -= OnTapped;
        }

        private float HalfWidth => Config.PixelsToUnits(Config.ufoHalfWidthPx);

        private void OnDragged(float fractionOfPlayWidth)
        {
            if (IsDashing || Time.timeScale == 0f) return;
            float dx = fractionOfPlayWidth * PlayArea.Size.x * Config.dragSensitivity;
            SetX(transform.position.x + dx);
        }

        private void OnTapped(Vector2 screenPos)
        {
            if (IsDashing || Time.timeScale == 0f || Time.time < cooldownUntil) return;
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (cam == null) return;

            float ufoScreenX = cam.WorldToScreenPoint(transform.position).x;
            float dir = Mathf.Sign(screenPos.x - ufoScreenX);
            float target = PlayArea.ClampX(transform.position.x + dir * Config.dashDistanceFraction * PlayArea.Size.x, HalfWidth);
            if (Mathf.Approximately(target, transform.position.x)) return; // 이미 벽에 붙어 있음

            dashStartX = transform.position.x;
            dashTargetX = target;
            dashTimer = 0f;
            cooldownUntil = Time.time + Config.dashCooldown;
            if (visual != null) visual.Punch(Config.squashOnDashStart);
        }

        private void Update()
        {
            if (IsDashing) UpdateDash();
            UpdateMoveSquash();
        }

        private void UpdateDash()
        {
            dashTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dashTimer / Mathf.Max(0.01f, Config.dashDuration));
            float eased = 1f - (1f - t) * (1f - t); // 빠르게 출발해서 부드럽게 멈춤
            SetX(Mathf.Lerp(dashStartX, dashTargetX, eased));
            if (visual != null) visual.SetHop(Mathf.Sin(t * Mathf.PI) * Config.PixelsToUnits(Config.dashHopPx));

            if (t >= 1f)
            {
                dashTimer = -1f;
                if (visual != null)
                {
                    visual.SetHop(0f);
                    visual.Punch(Config.squashOnDashLand);
                }
            }
        }

        private void UpdateMoveSquash()
        {
            float x = transform.position.x;
            float speed = Time.deltaTime > 0f ? Mathf.Abs(x - lastX) / Time.deltaTime : 0f;
            lastX = x;
            if (IsDashing) { wasMoving = false; return; } // 착지 연출을 '멈춤' 연출이 덮지 않게

            bool moving = speed >= Config.movingSpeedThreshold * PlayArea.Size.x;
            if (moving != wasMoving && visual != null)
                visual.Punch(moving ? Config.squashOnMoveStartStop : -Config.squashOnMoveStartStop * 0.6f);
            wasMoving = moving;
        }

        private void SetX(float x)
        {
            var p = transform.position;
            p.x = PlayArea.ClampX(x, HalfWidth);
            transform.position = p;
        }
    }
}
