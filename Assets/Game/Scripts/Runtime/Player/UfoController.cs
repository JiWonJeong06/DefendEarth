using System;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 플레이어 UFO. 좌우로만 움직이고 체력은 없다.
    /// 조작: UFO보다 왼쪽/오른쪽을 누르면 그쪽으로 통 튄다.
    /// 짧게 누르면 조금, 길게 누를수록 멀리(최대 거리까지). 누르는 동안 UFO가 움츠리며 힘을 모은다.
    /// 튀는 중에 손을 떼면 착지하자마자 이어서 튄다.
    /// </summary>
    public sealed class UfoController : MonoBehaviour
    {
        [SerializeField] private TapInput input;
        [SerializeField] private SquashStretch visual;

        private float hopTimer = -1f;
        private float hopStartX;
        private float hopTargetX;
        private float hopDuration;
        private float hopHeight;

        private bool charging;
        private int chargeDir;

        private int bufferedDir;
        private float bufferedCharge;
        private float bufferedUntil;

        public bool IsHopping => hopTimer >= 0f;

        /// <summary>통 튀기 시작(방향 -1 왼쪽 / 1 오른쪽). 튜토리얼이 듣는다.</summary>
        public event Action<int> Hopped;

        private GameConfig Config => PlayArea.Config;
        private float HalfWidth => Config.PixelsToUnits(Config.ufoHalfWidthPx);

        private void Start()
        {
            float y = PlayArea.Bottom + Config.PixelsToUnits(Config.ufoHeightFromBottomPx);
            transform.position = new Vector3(0f, y, 0f);
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.Pressed += OnPressed;
            input.Released += OnReleased;
        }

        private void OnDisable()
        {
            if (input == null) return;
            input.Pressed -= OnPressed;
            input.Released -= OnReleased;
        }

        /// <summary>누른 시간 → 0(짧은 탭)~1(최대 거리).</summary>
        private float ChargeOf(float heldSeconds)
        {
            float span = Mathf.Max(0.01f, Config.hopFullChargeTime - Config.hopShortPressTime);
            return Mathf.Clamp01((heldSeconds - Config.hopShortPressTime) / span);
        }

        private void OnPressed(Vector2 screenPos)
        {
            if (Time.timeScale == 0f) return;
            if (PixelButton.AnyAt(screenPos)) return; // 일시정지 버튼 등을 누른 것은 이동이 아님
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (cam == null) return;

            // 방향은 누른 순간의 UFO 위치(튀는 중이면 착지할 자리) 기준
            float referenceX = IsHopping ? hopTargetX : transform.position.x;
            float refScreenX = cam.WorldToScreenPoint(new Vector3(referenceX, transform.position.y, 0f)).x;
            chargeDir = screenPos.x < refScreenX ? -1 : 1;
            charging = true;
        }

        private void OnReleased(Vector2 screenPos, float heldSeconds)
        {
            if (!charging) return;
            charging = false;
            if (visual != null) visual.SetHold(0f);
            if (Time.timeScale == 0f) return;

            float charge = ChargeOf(heldSeconds);
            if (IsHopping)
            {
                bufferedDir = chargeDir;
                bufferedCharge = charge;
                bufferedUntil = Time.time + Config.hopBufferTime;
                return;
            }
            StartHop(chargeDir, charge);
        }

        private void StartHop(int dir, float charge)
        {
            float distance = Mathf.Lerp(Config.hopMinDistanceFraction, Config.hopMaxDistanceFraction, charge) * PlayArea.Size.x;
            float from = transform.position.x;
            float target = PlayArea.ClampX(from + dir * distance, HalfWidth);
            if (Mathf.Abs(target - from) < 0.01f)
            {
                // 벽에 붙어 있음: 제자리에서 살짝 움찔
                if (visual != null) visual.Punch(-Config.squashOnHopStart * 0.5f);
                return;
            }

            hopStartX = from;
            hopTargetX = target;
            hopDuration = Mathf.Lerp(Config.hopDurationMinMax.x, Config.hopDurationMinMax.y, charge);
            hopHeight = Config.PixelsToUnits(Mathf.Lerp(Config.hopHeightMinMaxPx.x, Config.hopHeightMinMaxPx.y, charge));
            hopTimer = 0f;
            if (visual != null) visual.Punch(-Config.squashOnHopStart * Mathf.Lerp(0.7f, 1.3f, charge)); // 위로 늘어나며 출발
            Hopped?.Invoke(dir);
        }

        private void Update()
        {
            if (charging && visual != null && input != null)
                visual.SetHold(ChargeOf(input.HeldTime) * Config.squashOnCharge);

            if (!IsHopping) return;

            hopTimer += Time.deltaTime;
            float t = Mathf.Clamp01(hopTimer / Mathf.Max(0.01f, hopDuration));
            float eased = 1f - (1f - t) * (1f - t); // 빠르게 출발해서 부드럽게 착지
            SetX(Mathf.Lerp(hopStartX, hopTargetX, eased));
            if (visual != null) visual.SetHop(Mathf.Sin(t * Mathf.PI) * hopHeight);

            if (t < 1f) return;

            hopTimer = -1f;
            if (visual != null)
            {
                visual.SetHop(0f);
                visual.Punch(Config.squashOnHopLand); // 착지하며 납작하게
            }
            if (bufferedDir != 0 && Time.time <= bufferedUntil)
            {
                int dir = bufferedDir;
                bufferedDir = 0;
                StartHop(dir, bufferedCharge);
            }
            bufferedDir = 0;
        }

        private void SetX(float x)
        {
            var p = transform.position;
            p.x = PlayArea.ClampX(x, HalfWidth);
            transform.position = p;
        }
    }
}
