using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 플레이어 UFO. 총알 없이 몸통으로 몬스터를 들이받는다. 체력은 없다.
    /// 조작: 화면을 꾹 누르면 게이지가 차고, UFO에서 손가락 쪽으로 조준(누른 채로 움직이면 각도가 바뀜).
    /// 손을 떼면 그 방향으로 돌진하고, 중력으로 다시 땅에 떨어진다. 착지해야 다음 돌진.
    /// 날아가는 동안 닿은 몬스터만 피해를 입고 튕겨 나간다(서 있을 때는 그냥 통과).
    /// </summary>
    public sealed class UfoController : MonoBehaviour
    {
        [SerializeField] private TapInput input;
        [SerializeField] private SquashStretch visual;

        // ---- 판 안 카드로 바뀌는 값 (판마다 초기화) ----
        /// <summary>돌진 피해 추가.</summary>
        public int DamageBonus { get; set; }
        /// <summary>게이지가 가득 차는 시간 배율(작을수록 빠름).</summary>
        public float ChargeTimeMultiplier { get; set; } = 1f;
        /// <summary>몸통 판정 크기 배율.</summary>
        public float BodyScale { get; set; } = 1f;
        /// <summary>연쇄 피해 추가.</summary>
        public int ChainDamageBonus { get; set; }

        public bool IsDashing { get; private set; }
        public bool IsCharging => charging;

        /// <summary>돌진 시작(조준 방향). 튜토리얼이 듣는다.</summary>
        public event Action<Vector2> Dashed;

        private bool charging;
        private float chargeTime;
        private Vector2 velocity;
        private float dashCharge;
        private readonly List<Monster> hitThisDash = new List<Monster>(16);
        private readonly List<Monster> scratch = new List<Monster>(64);

        private bool hasBuffer;
        private Vector2 bufferedAim;
        private float bufferedCharge;
        private float bufferedUntil;

        private Transform guideRoot;
        private SpriteRenderer[] dots;
        private SpriteRenderer gaugeBack;
        private SpriteRenderer gaugeFill;
        private float px;

        private GameConfig Config => PlayArea.Config;
        private float U(float pixels) => Config.PixelsToUnits(pixels);
        private float HalfWidth => U(Config.ufoHalfWidthPx);
        private float GroundY => PlayArea.Bottom + U(Config.ufoHeightFromBottomPx);
        private float BodyRadius => U(Config.ufoBodyRadiusPx) * BodyScale;

        /// <summary>지금 게이지 0~1.</summary>
        public float Charge
        {
            get
            {
                float full = Mathf.Max(Config.chargeMinTime + 0.05f, Config.chargeFullTime * ChargeTimeMultiplier);
                return Mathf.Clamp01((chargeTime - Config.chargeMinTime) / (full - Config.chargeMinTime));
            }
        }

        private void Start()
        {
            transform.position = new Vector3(0f, GroundY, 0f);
            // 로비 강화(충전 속도)만큼 게이지가 빨리 찬다
            ChargeTimeMultiplier = Mathf.Clamp(1f - Upgrades.Total(UpgradeDefinition.Effect.ChargeSpeed), 0.3f, 1f);
            BuildGuide();
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

        private void OnPressed(Vector2 screenPos)
        {
            if (Time.timeScale == 0f) return;
            if (PixelButton.AnyAt(screenPos)) return; // 일시정지 버튼 등을 누른 것은 조작이 아님
            var session = GameSession.Instance;
            if (session != null && !session.IsPlaying) return;
            charging = true;
            chargeTime = 0f;
        }

        private void OnReleased(Vector2 screenPos, float heldSeconds)
        {
            if (!charging) return;
            charging = false;
            ShowGuide(false);
            if (visual != null) visual.SetHold(0f);
            if (Time.timeScale == 0f) return;

            Vector2 aim = AimDirection(screenPos);
            float charge = Charge;
            if (IsDashing)
            {
                // 공중에서 뗐으면 곧 착지할 때 바로 돌진
                hasBuffer = true;
                bufferedAim = aim;
                bufferedCharge = charge;
                bufferedUntil = Time.time + Config.dashBufferTime;
                return;
            }
            Launch(aim, charge);
        }

        /// <summary>UFO에서 손가락 쪽 방향. 아래쪽은 막고 위쪽 반원(최저 각도 이상)으로만.</summary>
        private Vector2 AimDirection(Vector2 screenPos)
        {
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (cam == null) return Vector2.up;
            Vector2 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            Vector2 d = world - (Vector2)transform.position;
            if (d.sqrMagnitude < 1e-4f) return Vector2.up;

            float min = Config.aimMinElevationDeg;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; // -180~180
            if (angle < -90f) angle = 180f - min;       // 왼쪽 아래 → 왼쪽 최저 각도
            else if (angle < min) angle = min;          // 오른쪽 아래 → 오른쪽 최저 각도
            else if (angle > 180f - min) angle = 180f - min;
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private float SpeedFor(float charge) => U(Mathf.Lerp(Config.dashSpeedMinMaxPx.x, Config.dashSpeedMinMaxPx.y, charge));

        private void Launch(Vector2 aim, float charge)
        {
            hasBuffer = false;
            velocity = aim * SpeedFor(charge);
            dashCharge = charge;
            IsDashing = true;
            hitThisDash.Clear();
            if (visual != null) visual.Punch(-Config.squashOnDash * Mathf.Lerp(0.7f, 1.3f, charge));
            Dashed?.Invoke(aim);
        }

        private void Land()
        {
            IsDashing = false;
            velocity = Vector2.zero;
            if (visual != null) visual.Punch(Config.squashOnLand); // 착지하며 납작하게
            if (hasBuffer && Time.time <= bufferedUntil) Launch(bufferedAim, bufferedCharge);
            hasBuffer = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (charging)
            {
                chargeTime += dt; // 일시정지 동안은 차지 않는다
                if (visual != null) visual.SetHold(Charge * Config.squashOnCharge);
                UpdateGuide();
            }

            if (IsDashing && dt > 0f)
            {
                // 빠를 때 몬스터를 건너뛰지 않게 잘게 나눠 움직인다
                float step = Mathf.Max(0.01f, BodyRadius * 0.6f);
                int n = Mathf.Clamp(Mathf.CeilToInt(velocity.magnitude * dt / step), 1, 8);
                for (int i = 0; i < n && IsDashing; i++) Step(dt / n);
            }

            // 날아가는 방향으로 살짝 기울기
            if (visual != null)
            {
                float target = IsDashing ? Mathf.Clamp(-velocity.x * 4f, -25f, 25f) : 0f;
                float z = Mathf.DeltaAngle(0f, visual.transform.localEulerAngles.z);
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(z, target, 1f - Mathf.Exp(-15f * dt)));
            }
        }

        private void Step(float dt)
        {
            velocity.y -= U(Config.dashGravityPx) * dt;
            Vector2 p = (Vector2)transform.position + velocity * dt;

            float half = HalfWidth;
            float bounce = Config.wallBounce;
            if (p.x < PlayArea.Left + half) { p.x = PlayArea.Left + half; velocity.x = Mathf.Abs(velocity.x) * bounce; }
            if (p.x > PlayArea.Right - half) { p.x = PlayArea.Right - half; velocity.x = -Mathf.Abs(velocity.x) * bounce; }
            if (p.y > PlayArea.Top - half) { p.y = PlayArea.Top - half; velocity.y = -Mathf.Abs(velocity.y) * bounce; }

            transform.position = new Vector3(p.x, Mathf.Max(p.y, GroundY), 0f);
            HitMonsters(p);

            if (p.y <= GroundY && velocity.y <= 0f) Land();
        }

        private void HitMonsters(Vector2 p)
        {
            scratch.Clear();
            scratch.AddRange(MonsterRegistry.Active);
            for (int i = 0; i < scratch.Count; i++)
            {
                var m = scratch[i];
                if (!m.IsAlive || hitThisDash.Contains(m)) continue;
                Vector2 mp = m.transform.position;
                float r = BodyRadius + m.HitRadius;
                if ((mp - p).sqrMagnitude > r * r) continue;

                hitThisDash.Add(m);
                int damage = Mathf.RoundToInt(Mathf.Lerp(Config.dashDamageMinMax.x, Config.dashDamageMinMax.y, dashCharge)) + DamageBonus;

                // 맞은 쪽 + 날아가던 방향으로, 주로 위로 튕긴다
                Vector2 away = (mp - p).sqrMagnitude > 1e-6f ? (mp - p).normalized : Vector2.up;
                Vector2 dir = (away + velocity.normalized).normalized;
                if (dir.y < 0.35f) { dir.y = 0.35f; dir.Normalize(); }
                float speed = U(Mathf.Lerp(Config.knockSpeedMinMaxPx.x, Config.knockSpeedMinMaxPx.y, dashCharge));

                m.TakeHit(damage, Vector2.Lerp(p, mp, 0.5f));
                if (m.IsAlive) m.Knock(dir * speed, Config.chainDamage + ChainDamageBonus, Config.chainMaxDepth);
                if (visual != null) visual.Nudge(Config.squashOnHit);
            }
        }

        // ---- 조준 점선 + 게이지 ----

        private void BuildGuide()
        {
            var a = GameAssets.Instance;
            if (a == null || a.pixelSprite == null) return;
            px = a.pixelSprite.bounds.size.x;
            guideRoot = new GameObject("AimGuide").transform;
            guideRoot.SetParent(transform.parent, false);

            dots = new SpriteRenderer[Mathf.Max(1, Config.aimDots)];
            for (int i = 0; i < dots.Length; i++)
            {
                var d = a.CreateSprite("Dot", a.pixelSprite, SortOrder.Ufo + 1, guideRoot);
                d.transform.localScale = new Vector3(U(2f) / px, U(2f) / px, 1f);
                dots[i] = d;
            }
            gaugeBack = a.CreateSprite("GaugeBack", a.pixelSprite, SortOrder.Ufo + 1, guideRoot);
            gaugeBack.color = new Color(0.05f, 0.06f, 0.15f, 0.9f);
            gaugeBack.transform.localScale = new Vector3(U(26f) / px, U(4f) / px, 1f);
            gaugeFill = a.CreateSprite("GaugeFill", a.pixelSprite, SortOrder.Ufo + 2, guideRoot);
            ShowGuide(false);
        }

        private void ShowGuide(bool show)
        {
            if (guideRoot != null) guideRoot.gameObject.SetActive(show);
        }

        private void UpdateGuide()
        {
            if (guideRoot == null || input == null) return;
            ShowGuide(true);
            float c = Charge;
            Vector2 start = transform.position;

            // 게이지: UFO 아래
            Vector2 gaugePos = start + new Vector2(0f, -U(14f));
            gaugeBack.transform.position = gaugePos;
            float w = U(24f) * c;
            gaugeFill.transform.localScale = new Vector3(Mathf.Max(0.0001f, w) / px, U(2f) / px, 1f);
            gaugeFill.transform.position = gaugePos + new Vector2(-U(12f) + w * 0.5f, 0f);
            bool full = c >= 1f;
            gaugeFill.color = full && Mathf.PingPong(Time.time * 8f, 1f) > 0.5f
                ? Color.white
                : Color.Lerp(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.35f, 0.3f), c);

            // 점선: 실제로 날아갈 길을 미리 계산(벽·천장 튕김 포함)
            Vector2 p = start;
            Vector2 v = AimDirection(input.Position) * SpeedFor(c);
            float g = U(Config.dashGravityPx);
            float half = HalfWidth;
            bool landed = false;
            for (int i = 0; i < dots.Length; i++)
            {
                for (int s = 0; s < 4 && !landed; s++)
                {
                    float dt = Config.aimDotStep / 4f;
                    v.y -= g * dt;
                    p += v * dt;
                    if (p.x < PlayArea.Left + half) { p.x = PlayArea.Left + half; v.x = Mathf.Abs(v.x) * Config.wallBounce; }
                    if (p.x > PlayArea.Right - half) { p.x = PlayArea.Right - half; v.x = -Mathf.Abs(v.x) * Config.wallBounce; }
                    if (p.y > PlayArea.Top - half) { p.y = PlayArea.Top - half; v.y = -Mathf.Abs(v.y) * Config.wallBounce; }
                    if (p.y < GroundY && v.y < 0f) landed = true;
                }
                var dot = dots[i];
                dot.gameObject.SetActive(!landed);
                if (landed) continue;
                dot.transform.position = p;
                dot.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0.25f, (float)i / dots.Length));
            }
        }
    }
}
