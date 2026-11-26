using System;
using UnityEngine;

namespace BouncyUFO
{
    /// <summary>
    /// 지구. 이 게임에서 체력이 있는 건 지구뿐이다. 0이 되면 판이 끝난다.
    /// 그림은 판이 시작할 때 스스로 도는 큰 지구(EarthGlobe)를 만들어 쓴다.
    /// </summary>
    public sealed class Earth : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SquashStretch visual;

        public int MaxHp { get; private set; }
        public int Hp { get; private set; }

        /// <summary>(현재 체력, 최대 체력)</summary>
        public event Action<int, int> HpChanged;
        public event Action Died;

        private float hurtTimer;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                var globe = EarthGlobe.CreateHorizon("Globe", transform, SortOrder.Earth, PlayArea.Bottom);
                spriteRenderer = globe.GetComponent<SpriteRenderer>();
            }
            // 로비 강화(지구 체력)만큼 더한다
            MaxHp = Mathf.Max(1, PlayArea.Config.earthMaxHp + Mathf.RoundToInt(Upgrades.Total(UpgradeDefinition.Effect.EarthHp)));
            Hp = MaxHp;
        }

        private void Start() => HpChanged?.Invoke(Hp, MaxHp);

        public void TakeDamage(int amount)
        {
            if (Hp <= 0 || amount <= 0) return;
            Hp = Mathf.Max(SceneFlow.IsTutorial ? 1 : 0, Hp - amount); // 튜토리얼에서는 지구가 함락되지 않는다
            hurtTimer = 0.15f;
            Sound.Play(Sfx.EarthHurt);
            HitFeedback.EarthHurt();
            if (visual != null) visual.Punch(0.12f);
            HpChanged?.Invoke(Hp, MaxHp);
            if (Hp == 0) Died?.Invoke();
        }

        public void Heal(int amount)
        {
            if (Hp <= 0 || amount <= 0) return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            HpChanged?.Invoke(Hp, MaxHp);
        }

        private void Update()
        {
            if (spriteRenderer == null) return;
            if (hurtTimer > 0f) hurtTimer -= Time.unscaledDeltaTime;
            spriteRenderer.color = hurtTimer > 0f ? new Color(1f, 0.55f, 0.55f) : Color.white;
        }
    }
}
