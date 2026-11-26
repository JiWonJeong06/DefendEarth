using System;
using System.Collections.Generic;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 플레이어 UFO. 총알 없이 몸통으로 몬스터를 들이받는다. 체력은 없다.
    /// 조작(새총): 화면 아무 데나 꾹 누른 채로 뒤로 당기면 반대쪽으로 조준(게이지 없음, 세기는 늘 같음).
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
        /// <summary>돌진 속도 배율(클수록 멀리 날아감). UFO 종류·증강 '멀리 날기'.</summary>
        public float RangeMultiplier { get; set; } = 1f;
        /// <summary>몸통 판정 크기 배율.</summary>
        public float BodyScale { get; set; } = 1f;
        /// <summary>연쇄 피해 추가.</summary>
        public int ChainDamageBonus { get; set; }

        public bool IsDashing { get; private set; }
        /// <summary>이번 판에 타는 UFO 종류.</summary>
        public UfoType Type { get; private set; } = UfoTypes.All[0];
        /// <summary>true면 새로 조준할 수 없다(튜토리얼에서 몬스터가 강조되기 전).</summary>
        public bool InputLocked { get; set; }
        public bool IsAiming => aiming;

        /// <summary>돌진 시작(조준 방향). 튜토리얼이 듣는다.</summary>
        public event Action<Vector2> Dashed;

        private bool aiming;
        private Vector2 pressScreen;
        private float ghostTimer;
        private SpriteRenderer visualRenderer;
        private const float GhostInterval = 0.07f; // 잔상 간격(초). 11/27: 너무 진해서 간격을 넓히고 옅게
        private Vector2 velocity;
        private readonly List<Monster> hitThisDash = new List<Monster>(16);
        private readonly List<Monster> scratch = new List<Monster>(64);

        private bool hasBuffer;
        private Vector2 bufferedAim;
        private float bufferedUntil;

        private Transform guideRoot;
        private SpriteRenderer[] dots;
        // 조준 보조: 점선 길 위(맞을 몬스터)를 노란 네모로 표시
        private readonly List<Vector2> pathPoints = new List<Vector2>(64);
        private readonly List<SpriteRenderer[]> markers = new List<SpriteRenderer[]>();
        private float px;

        private GameConfig Config => PlayArea.Config;
        private float U(float pixels) => Config.PixelsToUnits(pixels);
        private float HalfWidth => U(Config.ufoHalfWidthPx);
        private float GroundY => PlayArea.Bottom + U(Config.ufoHeightFromBottomPx);
        private float BodyRadius => U(Config.ufoBodyRadiusPx) * BodyScale;
        /// <summary>몸통 판정 반지름(유닛). 상점 우주선 닿기 판정에 쓴다.</summary>
        public float BodyRadiusUnits => BodyRadius;

        private void Start()
        {
            transform.position = new Vector3(0f, GroundY, 0f);
            // 판 안 증강(상점)으로만 강해진다. 튜토리얼은 잠깐 세게(잡기 쉽게)
            RangeMultiplier = 1f;
            DamageBonus = SceneFlow.IsTutorial ? Config.tutorialDamageBonus : 0;
            // 고른 UFO 종류(넓은접시·자석·드릴…): 그림, 몸통 크기, 돌진 거리, 피해
            Type = UfoTypes.Selected;
            if (visual != null) UfoTypes.Apply(visual.GetComponent<SpriteRenderer>(), UfoTypes.SelectedIndex);
            BodyScale = Type.bodyScale;
            RangeMultiplier *= Type.dashSpeed;
            DamageBonus += Type.damage;
            // 튜토리얼은 몸통 판정을 크게(맞히기 쉽게)
            if (SceneFlow.IsTutorial) BodyScale *= Config.tutorialBodyScale;
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
            if (Time.timeScale == 0f || InputLocked) return;
            if (PixelButton.AnyAt(screenPos)) return; // 일시정지 버튼 등을 누른 것은 조작이 아님
            var session = GameSession.Instance;
            if (session != null && !session.IsPlaying) return;
            aiming = true;
            pressScreen = screenPos;
            if (visual != null) visual.SetHold(Config.squashOnCharge); // 당기는 동안 움츠림
        }

        private void OnReleased(Vector2 screenPos, float heldSeconds)
        {
            if (!aiming) return;
            aiming = false;
            ShowGuide(false);
            if (visual != null) visual.SetHold(0f);
            if (Time.timeScale == 0f || InputLocked) return;

            Vector2 aim = AimDirection(screenPos);
            if (aim == Vector2.zero) return; // 거의 안 당기고 뗐으면 취소
            if (IsDashing)
            {
                // 공중에서 뗐으면 곧 착지할 때 바로 돌진
                hasBuffer = true;
                bufferedAim = aim;
                bufferedUntil = Time.time + Config.dashBufferTime;
                return;
            }
            Launch(aim);
        }

        /// <summary>
        /// 새총: 처음 누른 곳에서 손가락을 당긴 반대쪽으로 날아간다. 아래쪽은 막고 위쪽 반원(최저 각도 이상)으로만.
        /// 거의 안 당겼으면 Vector2.zero(돌진 안 함).
        /// </summary>
        private Vector2 AimDirection(Vector2 screenPos)
        {
            var cam = PlayAreaCamera.Main != null ? PlayAreaCamera.Main.Camera : null;
            if (cam == null) return Vector2.up;
            float z = -cam.transform.position.z;
            Vector2 from = cam.ScreenToWorldPoint(new Vector3(pressScreen.x, pressScreen.y, z));
            Vector2 to = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
            Vector2 d = from - to;
            float minPull = U(Config.slingMinPullPx);
            if (d.sqrMagnitude < minPull * minPull) return Vector2.zero;

            float min = Config.aimMinElevationDeg;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; // -180~180
            if (angle < -90f) angle = 180f - min;       // 왼쪽 아래 → 왼쪽 최저 각도
            else if (angle < min) angle = min;          // 오른쪽 아래 → 오른쪽 최저 각도
            else if (angle > 180f - min) angle = 180f - min;
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private float DashSpeed => U(Config.dashSpeedPx) * RangeMultiplier;

        private void Launch(Vector2 aim)
        {
            hasBuffer = false;
            velocity = aim * DashSpeed;
            IsDashing = true;
            hitThisDash.Clear();
            if (visual != null) visual.Punch(-Config.squashOnDash);
            Sound.Play(Sfx.Dash, UnityEngine.Random.Range(0.95f, 1.05f));
            HitFeedback.ResetCombo();
            ghostTimer = 0f;
            Dashed?.Invoke(aim);
        }

        private void Land()
        {
            IsDashing = false;
            velocity = Vector2.zero;
            if (visual != null) visual.Punch(Config.squashOnLand); // 착지하며 납작하게
            Sound.Play(Sfx.Land, 0.6f);
            if (hasBuffer && Time.time <= bufferedUntil) Launch(bufferedAim);
            hasBuffer = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (aiming) UpdateGuide();

            if (IsDashing && dt > 0f)
            {
                // 잔상: 날아가는 동안 짧은 간격으로 옅은 하늘색 UFO를 남긴다
                ghostTimer -= dt;
                if (ghostTimer <= 0f && visual != null)
                {
                    ghostTimer = GhostInterval;
                    if (visualRenderer == null) visualRenderer = visual.GetComponent<SpriteRenderer>();
                    if (visualRenderer != null)
                        Effects.Ghost(visualRenderer.sprite, visual.transform.position, visual.transform.rotation, visual.transform.lossyScale,
                            new Color(0.55f, 0.85f, 1f, 0.28f), 0.1f, false, SortOrder.Ufo - 1);
                }
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
                int damage = Config.dashDamage + DamageBonus;
                if (m.IsHeavy) damage += Type.heavyDamage; // 드릴형: 덩치형·보스에 추가 피해

                // 맞은 쪽 + 날아가던 방향으로, 주로 위로 튕긴다
                Vector2 away = (mp - p).sqrMagnitude > 1e-6f ? (mp - p).normalized : Vector2.up;
                Vector2 dir = (away + velocity.normalized).normalized;
                if (dir.y < 0.35f) { dir.y = 0.35f; dir.Normalize(); }
                float speed = U(Config.knockSpeedPx);

                m.TakeHit(damage, Vector2.Lerp(p, mp, 0.5f));
                int depth = Type.chainDepth >= 0 ? Type.chainDepth : Config.chainMaxDepth; // 자석형은 연쇄 약하게
                if (m.IsAlive) m.Knock(dir * speed, Config.chainDamage + ChainDamageBonus, depth);
                if (visual != null) visual.Nudge(Config.squashOnHit);
            }
        }

        // ---- 조준 점선 ----

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
            Vector2 start = transform.position;

            // 점선: 실제로 날아갈 길을 미리 계산(벽·천장 튕김 포함)
            Vector2 p = start;
            Vector2 aim = AimDirection(input.Position);
            Vector2 v = aim * DashSpeed;
            if (aim == Vector2.zero)
            {
                // 아직 안 당겼으면 점선·조준 표시는 숨긴다
                for (int i = 0; i < dots.Length; i++) dots[i].gameObject.SetActive(false);
                for (int i = 0; i < markers.Count; i++)
                    foreach (var line in markers[i]) line.gameObject.SetActive(false);
                return;
            }
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
            MarkTargets(start, AimDirection(input.Position) * DashSpeed);
        }

        /// <summary>
        /// 조준 보조: 날아갈 길(점선보다 조금 더 멀리까지)을 따라가며 몸통에 닿을 몬스터를 찾아 노란 네모로 표시한다.
        /// 몬스터는 움직이므로 "지금 위치 기준" 예상이다.
        /// </summary>
        private void MarkTargets(Vector2 p, Vector2 v)
        {
            if (!SaveSystem.Data.aimAssist)
            {
                // 설정에서 껐으면 표시 안 함
                for (int i = 0; i < markers.Count; i++)
                    foreach (var line in markers[i]) line.gameObject.SetActive(false);
                return;
            }
            pathPoints.Clear();
            float g = U(Config.dashGravityPx);
            float half = HalfWidth;
            float dt = 0.01f;
            for (int i = 0; i < 160; i++) // 최대 1.6초
            {
                v.y -= g * dt;
                p += v * dt;
                if (p.x < PlayArea.Left + half) { p.x = PlayArea.Left + half; v.x = Mathf.Abs(v.x) * Config.wallBounce; }
                if (p.x > PlayArea.Right - half) { p.x = PlayArea.Right - half; v.x = -Mathf.Abs(v.x) * Config.wallBounce; }
                if (p.y > PlayArea.Top - half) { p.y = PlayArea.Top - half; v.y = -Mathf.Abs(v.y) * Config.wallBounce; }
                if (p.y < GroundY && v.y < 0f) break;
                pathPoints.Add(p);
            }

            int used = 0;
            var active = MonsterRegistry.Active;
            for (int m = 0; m < active.Count; m++)
            {
                var mon = active[m];
                if (!mon.IsAlive) continue;
                Vector2 mp = mon.transform.position;
                float r = BodyRadius + mon.HitRadius;
                bool hit = false;
                for (int i = 0; i < pathPoints.Count && !hit; i += 2)
                    hit = (pathPoints[i] - mp).sqrMagnitude <= r * r;
                if (!hit) continue;
                PlaceMarker(used++, mp, Mathf.Round(mon.HitRadius * Config.pixelsPerUnit) + 3f);
            }
            for (int i = used; i < markers.Count; i++)
                foreach (var line in markers[i]) line.gameObject.SetActive(false);
        }

        /// <summary>노란 네모 테두리(1픽셀) 하나를 몬스터에 맞춘다. 깜빡여서 잘 보이게.</summary>
        private void PlaceMarker(int index, Vector2 center, float halfPx)
        {
            var a = GameAssets.Instance;
            while (markers.Count <= index)
            {
                var lines = new SpriteRenderer[4];
                for (int k = 0; k < 4; k++) lines[k] = a.CreateSprite("Target", a.pixelSprite, SortOrder.Ufo + 1, guideRoot);
                markers.Add(lines);
            }
            float one = U(1f);
            float cx = Mathf.Round(center.x / one) * one, cy = Mathf.Round(center.y / one) * one;
            float h = U(halfPx), e = h + one;
            bool on = Mathf.PingPong(Time.unscaledTime * 4f, 1f) > 0.3f;
            var c = new Color(1f, 0.9f, 0.3f, on ? 1f : 0.35f);
            Line(markers[index][0], cx - e, cx + e, cy + h, cy + e, c);
            Line(markers[index][1], cx - e, cx + e, cy - e, cy - h, c);
            Line(markers[index][2], cx - e, cx - h, cy - h, cy + h, c);
            Line(markers[index][3], cx + h, cx + e, cy - h, cy + h, c);
        }

        private void Line(SpriteRenderer sr, float x0, float x1, float y0, float y1, Color c)
        {
            sr.gameObject.SetActive(true);
            sr.color = c;
            sr.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
            sr.transform.localScale = new Vector3((x1 - x0) / px, (y1 - y0) / px, 1f);
        }
    }
}
