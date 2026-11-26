using System;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 웨이브가 끝나면 오는 상점 우주선(11/27). 위에서 천천히 내려와 화면 위쪽 1/3에 멈추면 **바로 상점(ShopUI)이 열린다**
    /// (11/27 사용자 수정: 점프로 닿기 어려운 자리일 수 있어서 부딪힐 필요 없음). 상점을 닫으면 오른쪽으로 떠나고,
    /// 화면 밖으로 완전히 나가면 onGone(다음 웨이브).
    /// </summary>
    public sealed class ShopShip : MonoBehaviour
    {
        private enum State { Descend, Wait, Shopping, Leave }

        private const float HoldBeforeOpen = 0.35f;
        private const float TimerWidthPx = 30f;

        private UfoController ufo;
        private ShopUI shop;
        private bool noTimeout;
        private Action onGone;
        private State state;
        private float waitLeft;
        private float stopY;
        private float bob;
        private SpriteRenderer sprite;
        private SpriteRenderer timerBack;
        private SpriteRenderer timerFill;

        /// <summary>지금 상점 우주선이 있는지(튜토리얼 강조용).</summary>
        public static ShopShip Current { get; private set; }
        public bool IsWaiting => state == State.Wait;
        /// <summary>true인 동안 다 내려와도 상점을 열지 않는다(튜토리얼 설명 중).</summary>
        public bool HoldOpen { get; set; }

        private GameConfig Config => PlayArea.Config;
        private float U(float px) => Config.PixelsToUnits(px);

        public static ShopShip Spawn(Transform parent, UfoController ufo, ShopUI shop, bool noTimeout, Action onGone)
        {
            var go = new GameObject("ShopShip");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<ShopShip>();
            s.ufo = ufo;
            s.shop = shop;
            s.noTimeout = noTimeout;
            s.onGone = onGone;
            s.Build();
            Current = s;
            return s;
        }

        private void Build()
        {
            var a = GameAssets.Instance;
            sprite = a.CreateSprite("Ship", a.shopShipSprite != null ? a.shopShipSprite : a.ufoSprite, SortOrder.Ufo - 2, transform);
            float px = a.pixelSprite.bounds.size.x;
            timerBack = a.CreateSprite("TimerBack", a.pixelSprite, SortOrder.Hud, transform);
            timerBack.color = new Color(0.05f, 0.05f, 0.12f, 0.85f);
            timerBack.transform.localScale = new Vector3(U(TimerWidthPx + 2f) / px, U(4f) / px, 1f);
            timerBack.transform.localPosition = new Vector3(0f, -U(17f), 0f);
            timerFill = a.CreateSprite("TimerFill", a.pixelSprite, SortOrder.Hud + 1, transform);
            timerFill.color = new Color(1f, 0.85f, 0.35f);
            SetTimer(1f, false);

            stopY = PlayArea.Top - PlayArea.Size.y / 3f;
            transform.position = new Vector3(0f, PlayArea.Top + U(16f), 0f);
            state = State.Descend;
            Sound.Play(Sfx.Warning, 1.4f, 0.6f); // 상점이 왔어요
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector3 p = transform.position;
            bob += dt;
            switch (state)
            {
                case State.Descend:
                    p.y = Mathf.MoveTowards(p.y, stopY, U(Config.shopShipDescendPx) * dt);
                    if (Mathf.Approximately(p.y, stopY))
                    {
                        state = State.Wait;
                        waitLeft = HoldBeforeOpen;
                    }
                    break;
                case State.Wait:
                    // 멈춘 뒤 아주 잠깐(멈춘 게 보이게) 뒤 상점을 연다. 튜토리얼은 설명이 끝날 때까지 기다린다(HoldOpen)
                    waitLeft -= dt;
                    if (waitLeft <= 0f && !HoldOpen) OpenShop();
                    break;
                case State.Leave:
                    p.x += U(Config.shopShipLeavePx) * dt;
                    if (p.x > PlayArea.Right + U(30f))
                    {
                        var done = onGone;
                        Destroy(gameObject);
                        done?.Invoke();
                        return;
                    }
                    break;
            }
            // 둥실둥실(1픽셀 단위)
            float one = U(1f);
            float yOffset = state == State.Shopping ? 0f : Mathf.Round(Mathf.Sin(bob * 3f) * 1.5f) * one;
            transform.position = p;
            sprite.transform.localPosition = new Vector3(0f, yOffset, 0f);
        }

        private void OpenShop()
        {
            state = State.Shopping;
            SetTimer(0f, false);
            Effects.Ring(transform.position, 22, 0.25f);
            if (shop != null) shop.Open(StartLeaving);
            else StartLeaving();
        }

        private void StartLeaving()
        {
            state = State.Leave;
            SetTimer(0f, false);
        }

        private void SetTimer(float k, bool show)
        {
            timerBack.gameObject.SetActive(show);
            timerFill.gameObject.SetActive(show && k > 0f);
            if (!show) return;
            float px = GameAssets.Instance.pixelSprite.bounds.size.x;
            float w = Mathf.Round(TimerWidthPx * Mathf.Clamp01(k));
            timerFill.transform.localScale = new Vector3(U(w) / px, U(2f) / px, 1f);
            timerFill.transform.localPosition = new Vector3(U(-TimerWidthPx * 0.5f + w * 0.5f), -U(17f), 0f);
            timerFill.color = k > 0.3f ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.4f, 0.35f);
        }
    }
}
