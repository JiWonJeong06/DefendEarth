using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 플레이어 UFO. 좌우로만 움직이고 체력은 없다.
    /// 조작은 탭 하나: UFO보다 왼쪽을 탭하면 왼쪽으로, 오른쪽을 탭하면 오른쪽으로 한 칸 통 튄다.
    /// 튀는 중에 또 탭하면 착지하자마자 이어서 튄다(빠른 연타 가능).
    /// </summary>
    public sealed class UfoController : MonoBehaviour
    {
        [SerializeField] private TapInput input;
        [SerializeField] private SquashStretch visual;

        private float hopTimer = -1f;
        private float hopStartX;
        private float hopTargetX;
        private int bufferedDir;
        private float bufferedUntil;

        public bool IsHopping => hopTimer >= 0f;

        private GameConfig Config => PlayArea.Config;
        private float HalfWidth => Config.PixelsToUnits(Config.ufoHalfWidthPx);

        private void Start()
        {
            float y = PlayArea.Bottom + Config.PixelsToUnits(Config.ufoHeightFromBottomPx);
            transform.position = new Vector3(0f, y, 0f);
        }

        private void OnEnable()
        {
            if (input != null) input.Tapped += OnTapped;
        }

        private void OnDisable()
        {
            if (input != null) input.Tapped -= OnTapped;
        }

        private void OnTapped(Vector2 screenPos)
        {
            if (Time.timeScale == 0f) return;
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (cam == null) return;

            // 방향은 탭한 순간의 UFO 위치(튀는 중이면 착지할 자리) 기준
            float referenceX = IsHopping ? hopTargetX : transform.position.x;
            float refScreenX = cam.WorldToScreenPoint(new Vector3(referenceX, transform.position.y, 0f)).x;
            int dir = screenPos.x < refScreenX ? -1 : 1;

            if (IsHopping)
            {
                bufferedDir = dir;
                bufferedUntil = Time.time + Config.hopBufferTime;
                return;
            }
            StartHop(dir);
        }

        private void StartHop(int dir)
        {
            float from = transform.position.x;
            float target = PlayArea.ClampX(from + dir * Config.hopDistanceFraction * PlayArea.Size.x, HalfWidth);
            if (Mathf.Abs(target - from) < 0.01f)
            {
                // 벽에 붙어 있음: 제자리에서 살짝 움찔
                if (visual != null) visual.Punch(-Config.squashOnHopStart * 0.5f);
                return;
            }

            hopStartX = from;
            hopTargetX = target;
            hopTimer = 0f;
            if (visual != null) visual.Punch(-Config.squashOnHopStart); // 위로 늘어나며 출발
        }

        private void Update()
        {
            if (!IsHopping) return;

            hopTimer += Time.deltaTime;
            float t = Mathf.Clamp01(hopTimer / Mathf.Max(0.01f, Config.hopDuration));
            float eased = 1f - (1f - t) * (1f - t); // 빠르게 출발해서 부드럽게 착지
            SetX(Mathf.Lerp(hopStartX, hopTargetX, eased));
            if (visual != null) visual.SetHop(Mathf.Sin(t * Mathf.PI) * Config.PixelsToUnits(Config.hopHeightPx));

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
                StartHop(dir);
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
